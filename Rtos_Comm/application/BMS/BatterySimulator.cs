using Rtos_Comm;
using System;
using System.Linq;
using System.Windows.Forms;
using Rtos_Comm.application.JSON;

// Enums to make BQ78350 status flags more readable in C#
[Flags]
public enum SafetyStatusFlags : uint
{
    None = 0,
    CUV = 1 << 0,  // Cell Undervoltage
    COV = 1 << 1,  // Cell Overvoltage
    OCC = 1 << 2,  // Overcurrent in Charge
    OCD = 1 << 3,  // Overcurrent in Discharge
    AOLD = 1 << 4, // Overload in Discharge
    ASCD = 1 << 6, // Short Circuit in Discharge
    OTC = 1 << 8,  // Overtemperature for Charge
    OTD = 1 << 9,  // Overtemperature for Discharge
    UTC = 1 << 10, // Undertemperature for Charge
    UTD = 1 << 11, // Undertemperature for Discharge
    OTF = 1 << 13  // Overtemperature for FET
}

[Flags]
public enum ChargingStatusFlags : ushort
{
    None = 0,
    UT = 1 << 0,   // Undertemperature
    LT = 1 << 1,   // Low Temperature
    ST = 1 << 2,   // Standard Temperature
    HT = 1 << 3,   // High Temperature
    OT = 1 << 4,   // Overtemperature
    IN = 1 << 12,  // Charge Inhibit
    SU = 1 << 13,  // Suspend Charge
    VCT = 1 << 15, // VCT (Voltage Controlled Termination) detected
}
public class BatterySimulator : IDisposable
{
    public event Action<BatteryState> StateUpdated;  
    public BatteryState CurrentState => _currentState;

    private const int CELL_COUNT = 10;
    private const double FULL_CELL_VOLTAGE_V = 4.2;
    private const double EMPTY_CELL_VOLTAGE_V = 3.0;
    private const double TOTAL_CAPACITY_MAH = 5000.0;
    
    private readonly BatteryState _currentState;
    private double _currentCapacityMah;
    private readonly Timer _simulationTimer;
    private readonly BmsConfigData _configData;

    // Stores manually overridden cell voltages from the UI.
    // A null value means the cell is not manually controlled.
    private readonly float?[] _manualCellVoltages = new float?[CELL_COUNT];

    public BatterySimulator()
    {
        _currentState = new BatteryState();
        _configData = new BmsConfigData();

        InitializeLifetimeData();

        _currentCapacityMah = TOTAL_CAPACITY_MAH / 2;

        // Initial state calculation
        RecalculateStateFromCapacity();

        _simulationTimer = new Timer { Interval = 1000 };
        _simulationTimer.Tick += SimulationTick;
        _simulationTimer.Start();
    }

    public void Dispose()
    {
        _simulationTimer.Stop();
        _simulationTimer.Dispose();
    }
    private void InitializeLifetimeData()
    {
        for (int i = 0; i < 15; i++)
        {
            _currentState.BmsRegisters.lifetime_min_cell_v[i] = 2850; // Example: 2.85V
            _currentState.BmsRegisters.lifetime_max_cell_v[i] = 4250; // Example: 4.25V
        }
        _currentState.BmsRegisters.lifetime_max_charge_current = 4800;   // Example: 4.8A
        _currentState.BmsRegisters.lifetime_max_discharge_current = -6500; // Example: -6.5A
        _currentState.BmsRegisters.lifetime_max_temp_cell = 3200;        // Example: ~47°C in 0.1K
        _currentState.BmsRegisters.lifetime_min_temp_cell = 2780;        // Example: ~5°C in 0.1K
    }


    // --- Public methods for UI interaction ---
    public void SetCurrent(int currentInMa) => _currentState.Current_A = currentInMa / 1000.0f;
    public void SetTemperature(int tempInC) => _currentState.Temperature_C = tempInC;

    public void SetSoC(int soc)
    {
        // 1. Clamp the value between 0 and 100.
        int newSoC = Math.Max(0, Math.Min(100, soc));

        // 2. Update the master capacity based on this new SoC.
        _currentCapacityMah = TOTAL_CAPACITY_MAH * (newSoC / 100.0);

        // 3. Call the existing main calculation method to update everything consistently.
        RecalculateStateFromCapacity();

        // 4. Notify the UI of the change.
        StateUpdated?.Invoke(_currentState);
    }

    public void SetSoH(int soh)
    {
        // 1. Clamp the value and update the state.
        _currentState.SoH = Math.Max(0, Math.Min(100, soh));

        // 2. Call the main calculation method. It will re-populate registers with the new SoH.
        RecalculateStateFromCapacity();

        // 3. Notify the UI of the change.
        StateUpdated?.Invoke(_currentState);
    }

    /// <summary>
    /// Sets an individual cell voltage from the UI and recalculates the entire system state.
    /// </summary>
    public void SetIndividualCellVoltage(int cellIndex, int voltageInMv)
    {
        if (cellIndex >= 0 && cellIndex < CELL_COUNT)
        {
            // Store the manual override value.
            _manualCellVoltages[cellIndex] = voltageInMv / 1000.0f;

            // Recalculate the current state with this new override and trigger an immediate update.
            RecalculateStateFromCapacity();
            StateUpdated?.Invoke(_currentState);
        }
    }

    /// <summary>
    /// The main simulation loop, executed every second.
    /// </summary>
    private void SimulationTick(object sender, EventArgs e)
    {
        double hoursElapsed = _simulationTimer.Interval / 3600000.0;
        double chargeChangeMah = _currentState.Current_A * 1000 * hoursElapsed;

        _currentCapacityMah += chargeChangeMah;
        _currentCapacityMah = Math.Max(0, Math.Min(_currentCapacityMah, TOTAL_CAPACITY_MAH));

        // Update state based on the new capacity, respecting manual overrides
        RecalculateStateFromCapacity();

        StateUpdated?.Invoke(_currentState);
    }

    /// <summary>
    /// Primary state calculation method. Updates all state values based on current capacity.
    /// Manually set cell voltages are preserved.
    /// </summary>
    private void RecalculateStateFromCapacity()
    {
        // 1. Calculate SoC directly from capacity. This is stable.
        _currentState.SoC = (int)((_currentCapacityMah / TOTAL_CAPACITY_MAH) * 100);

        // 2. Determine the "base" voltage from the stable SoC.
        float baseCellVoltage = (float)(EMPTY_CELL_VOLTAGE_V + (_currentState.SoC / 100.0) * (FULL_CELL_VOLTAGE_V - EMPTY_CELL_VOLTAGE_V));

        // 3. Apply the base voltage or the manual override to each cell.
        for (int i = 0; i < CELL_COUNT; i++)
        {
            // If there's a manual override for this cell, use it. Otherwise, use the base voltage.
            _currentState.CellVoltages_V[i] = _manualCellVoltages[i] ?? baseCellVoltage;
        }

        // 4. Calculate all other dependent values.
        _currentState.PackVoltage_V = _currentState.CellVoltages_V.Sum();

        UpdateRegistersFromState(); // This translates the state to BQ78350 format.
    }

    private void UpdateRegistersFromState()
    {
        var regs = _currentState.BmsRegisters;

        // --- Part 1: Populate Real-time Single Word Registers (Mostly Unchanged) ---
        regs.pack_voltage = (ushort)(_currentState.PackVoltage_V * 1000);
        regs.current = (short)(_currentState.Current_A * 1000);
        regs.relative_soc = (byte)_currentState.SoC;
        regs.state_of_health = (byte)_currentState.SoH;
        regs.cycle_count = 15;
        regs.remaining_capacity = (ushort)_currentCapacityMah;
        regs.full_charge_capacity = (ushort)TOTAL_CAPACITY_MAH;
        regs.temperature = (ushort)((_currentState.Temperature_C + 273.15) * 10);
        for (int i = 0; i < 15; i++)
        {
            regs.cell_voltages[i] = (i < _currentState.CellVoltages_V.Length)
                ? (ushort)(_currentState.CellVoltages_V[i] * 1000)
                : (ushort)0;
        }

        // --- Part 2: Populate Status Block Registers (with dynamic thresholds) ---

        // SafetyStatus simulation using dynamic thresholds from config
        SafetyStatusFlags safetyFlags = SafetyStatusFlags.None;
        if (_currentState.CellVoltages_V.Any(v => v > _configData.CovThreshold / 1000.0f)) safetyFlags |= SafetyStatusFlags.COV;
        if (_currentState.CellVoltages_V.Any(v => v < _configData.CuvThreshold / 1000.0f)) safetyFlags |= SafetyStatusFlags.CUV;
        if (regs.current > _configData.OccThreshold) safetyFlags |= SafetyStatusFlags.OCC;
        if (regs.current < _configData.OcdThreshold) safetyFlags |= SafetyStatusFlags.OCD;
        if (regs.temperature > _configData.OtdThreshold) safetyFlags |= SafetyStatusFlags.OTD;
        if (regs.remaining_capacity < _configData.UtcThreshold && regs.current > 0) safetyFlags |= SafetyStatusFlags.UTC;
        // ... add other flags based on _configData ...

        regs.safety_status = BitConverter.GetBytes((uint)safetyFlags);
        if (!BitConverter.IsLittleEndian) Array.Reverse(regs.safety_status);

        // --- Update lifetime data based on current flags ---
        if (safetyFlags.HasFlag(SafetyStatusFlags.COV)) regs.lifetime_cov_events_count++;
        if (safetyFlags.HasFlag(SafetyStatusFlags.CUV)) regs.lifetime_cuv_events_count++;
        // ... add other lifetime event counters here ...

        // ChargingStatus simulation
        ChargingStatusFlags chargingFlags = ChargingStatusFlags.None;
        if (_currentState.Current_A > 0.05)
        {
            if (regs.temperature < _configData.UtcThreshold) chargingFlags |= ChargingStatusFlags.UT;
            else if (regs.temperature < 2831) chargingFlags |= ChargingStatusFlags.LT; // ~10°C
            else if (regs.temperature > _configData.OtcThreshold) chargingFlags |= ChargingStatusFlags.OT;
            else if (regs.temperature > 3181) chargingFlags |= ChargingStatusFlags.HT; // ~45°C
            else chargingFlags |= ChargingStatusFlags.ST;
        }
        regs.charging_status = BitConverter.GetBytes((ushort)chargingFlags);
        if (!BitConverter.IsLittleEndian) Array.Reverse(regs.charging_status);

        // Other status blocks (with example placeholder data)
        regs.operation_status = new byte[] { 0x01, 0x80, 0x00, 0x00 };
        regs.gauging_status = new byte[] { 0x0C, 0x00 };
        regs.manufacturing_status = new byte[] { 0x00, 0x00 };
        regs.pf_status = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        // --- Part 3: Populate String, Info, and Full Data Blocks ---
        regs.manufacturer_name = "VESTEL-SIM";
        regs.device_name = "BQ78350-R1-SIM";
        regs.firmware_version = "v1.2.3";

        regs.cov_threshold = _configData.CovThreshold;
        regs.cuv_threshold = _configData.CuvThreshold;
        regs.occ_threshold = _configData.OccThreshold;
        regs.ocd_threshold = _configData.OcdThreshold;
        regs.utc_threshold = _configData.UtdRecovery;
        regs.otd_threshold = _configData.OtdThreshold;
    }
}