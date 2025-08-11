using Rtos_Comm;
using System;
using System.Linq;
using System.Windows.Forms;

public class BatterySimulator : IDisposable
{
    public event Action<BatteryState> StateUpdated;

    private const int CELL_COUNT = 10;
    private const double FULL_CELL_VOLTAGE_V = 4.2;
    private const double EMPTY_CELL_VOLTAGE_V = 3.0;
    private const double TOTAL_CAPACITY_MAH = 5000.0;

    private readonly BatteryState _currentState;
    private double _currentCapacityMah;
    private readonly Timer _simulationTimer;

    // Stores manually overridden cell voltages from the UI.
    // A null value means the cell is not manually controlled.
    private readonly float?[] _manualCellVoltages = new float?[CELL_COUNT];

    public BatterySimulator()
    {
        _currentState = new BatteryState();
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

    // --- Public methods for UI interaction ---
    public void SetCurrent(int currentInMa)
    {
        _currentState.Current_A = currentInMa / 1000.0f;
        UpdateStatusFromCurrent();
    }
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

    public void SetTemperature(int tempInC) => _currentState.Temperature_C = tempInC;

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

        UpdateStatusFromCurrent();
        UpdateRegistersFromState(); // This translates the state to BQ78350 format.
    }

    /// <summary>
    /// Translates the current high-level state into the raw BQ78350 register format.
    /// </summary>
    /// 
    private void UpdateStatusFromCurrent()
    {
        if (_currentState.Current_A > 0.05)
        {
            _currentState.Status = "CHARGING";
        }
        else if (_currentState.Current_A < -0.05)
        {
            _currentState.Status = "DISCHARGING";
        }
        else
        {
            _currentState.Status = "IDLE";
        }
    }
    private void UpdateRegistersFromState()
    {
        var regs = _currentState.Registers;

        // --- Populate Single Word Registers ---
        regs.PackVoltage = (ushort)(_currentState.PackVoltage_V * 1000);
        regs.Current = (short)(_currentState.Current_A * 1000);
        regs.RelativeSoC = (byte)_currentState.SoC;
        regs.StateOfHealth = (byte)_currentState.SoH;
        regs.CycleCount = 15;
        regs.RemainingCapacity = (ushort)_currentCapacityMah;
        regs.FullChargeCapacity = (ushort)TOTAL_CAPACITY_MAH;
        regs.Temperature = (ushort)((_currentState.Temperature_C + 273.15) * 10);

        for (int i = 0; i < 15; i++)
        {
            regs.CellVoltages[i] = (i < _currentState.CellVoltages_V.Length)
                ? (ushort)(_currentState.CellVoltages_V[i] * 1000)
                : (ushort)0;
        }

        // --- Populate Block Registers (Status Flags) ---

        // SafetyStatus simulation
        SafetyStatusFlags safetyFlags = SafetyStatusFlags.None;
        if (_currentState.CellVoltages_V.Any(v => v > 4.25)) safetyFlags |= SafetyStatusFlags.COV;
        if (_currentState.CellVoltages_V.Any(v => v < 2.90)) safetyFlags |= SafetyStatusFlags.CUV;
        if (_currentState.Current_A > 5.1) safetyFlags |= SafetyStatusFlags.OCC;
        if (_currentState.Current_A < -5.1) safetyFlags |= SafetyStatusFlags.OCD;
        if (_currentState.Temperature_C > 60) safetyFlags |= SafetyStatusFlags.OTD;
        if (_currentState.Temperature_C < -10) safetyFlags |= SafetyStatusFlags.UTC;

        // Convert the 32-bit flag enum to a 4-byte array
        regs.SafetyStatus = BitConverter.GetBytes((uint)safetyFlags);
        if (!BitConverter.IsLittleEndian) Array.Reverse(regs.SafetyStatus);

        // ChargingStatus simulation
        ChargingStatusFlags chargingFlags = ChargingStatusFlags.None;
        if (_currentState.Temperature_C < 0) chargingFlags |= ChargingStatusFlags.UT;
        else if (_currentState.Temperature_C < 10) chargingFlags |= ChargingStatusFlags.LT;
        else if (_currentState.Temperature_C > 45) chargingFlags |= ChargingStatusFlags.HT;
        else chargingFlags |= ChargingStatusFlags.ST;

        // Convert the 16-bit flag enum to a 2-byte array
        regs.ChargingStatus = BitConverter.GetBytes((ushort)chargingFlags);
        if (!BitConverter.IsLittleEndian) Array.Reverse(regs.ChargingStatus);

        // OperationStatus simulation (example placeholder)
        regs.OperationStatus = new byte[] { 0x00, 0x00, 0x00, 0x00 };
    }
}