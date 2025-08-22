// FILE: BatterySimulator.cs (Complete, final, fully implemented, and flat structure version)

using Rtos_Comm;
using System;
using System.Linq;
using System.Windows.Forms;
using Rtos_Comm.application.JSON;

// Enums for status flags, kept for readability in this class.
[Flags]
public enum OperationStatus : uint { None = 0, PRES = 1 << 0, CHG = 1 << 1, DSG = 1 << 2, SEC1 = 1 << 8, SEC0 = 1 << 9, SS = 1 << 11, PF = 1 << 12, XDSG = 1 << 13, XCHG = 1 << 14, SLEEP = 1 << 15, SDM = 1 << 16, LED = 1 << 17, AUTH = 1 << 18, CAL = 1 << 20, CAL_OFFSET = 1 << 21, XL = 1 << 22, SLEEPM = 1 << 23, INIT = 1 << 24, SLPAD = 1 << 26, SLPCC = 1 << 27, CB = 1 << 28, KEYIN = 1U << 31 }
[Flags]
public enum ChargingStatus : ushort { None = 0, UT = 1 << 0, LT = 1 << 1, ST = 1 << 2, HT = 1 << 3, OT = 1 << 4, PCHG = 1 << 8, FCHG = 1 << 9, IN = 1 << 12, SU = 1 << 13, VCT = 1 << 15 }
[Flags]
public enum GaugingStatus : ushort { None = 0, FD = 1 << 0, FC = 1 << 1, DSG = 1 << 6, CF = 1 << 7, RST = 1 << 8, OCVFR = 1 << 9, FCCX = 1 << 10, EDV1 = 1 << 13, EDV2 = 1 << 14, VDQ = 1 << 15 }
[Flags]
public enum SafetyStatus : uint { None = 0, CUV = 1 << 0, COV = 1 << 1, OCC = 1 << 2, OCD = 1 << 3, AOLD = 1 << 4, AOLDL = 1 << 5, ASCD = 1 << 6, ASCDL = 1 << 7, OTC = 1 << 8, OTD = 1 << 9, UTC = 1 << 10, UTD = 1 << 11, AFE_OVRD = 1 << 12, OTF = 1 << 13, OCDL = 1 << 14, PTO = 1 << 16, CTO = 1 << 18, OC = 1 << 20 }
[Flags]
public enum PFStatus : uint { None = 0, SUV = 1 << 0, SOV = 1 << 1, SOCC = 1 << 2, SOCD = 1 << 3, SOT = 1 << 4, VIMR = 1 << 5, CFETF = 1 << 6, DFETF = 1 << 7, AFER = 1 << 8, AFEC = 1 << 9, AFE_OVRD = 1 << 10, AFE_XRDY = 1 << 11, TS1 = 1 << 12, TS2 = 1 << 13, TS3 = 1 << 14, SOTF = 1 << 15, IFC = 1 << 16, DFW = 1 << 17 }
[Flags]
public enum BatteryStatus : ushort { None = 0, EC0 = 1 << 0, EC1 = 1 << 1, EC2 = 1 << 2, EC3 = 1 << 3, FD = 1 << 4, FC = 1 << 5, DSG = 1 << 6, INIT = 1 << 7, RTA = 1 << 8, RCA = 1 << 9, TDA = 1 << 11, OTA = 1 << 12, TCA = 1 << 14, OCA = 1 << 15 }
[Flags]
public enum ManufacturingStatus : ushort { None = 0, PCHG_TEST = 1 << 0, CHG_TEST = 1 << 1, DSG_TEST = 1 << 2, FET_EN = 1 << 4, LF_EN = 1 << 5, PF_EN = 1 << 6, BBR_EN = 1 << 7, SAFE_EN = 1 << 8, LED_EN = 1 << 9, AFE_DD_TEST = 1 << 12, CB_TEST = 1 << 13, LF_TEST = 1 << 14, CAL_EN = 1 << 15 }

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

    private readonly float?[] _manualCellVoltages = new float?[CELL_COUNT];

    private ulong _manualSafetyFlags, _manualChargingFlags, _manualOperationFlags, _manualGaugingFlags = 0;
    private ulong _manualBatteryFlags, _manualManuFlags, _manualPfFlags = 0;

    public BatterySimulator()
    {
        _currentState = new BatteryState();
        InitializeDefaultState();

        _currentCapacityMah = TOTAL_CAPACITY_MAH * (_currentState.SoC / 100.0);
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

    private void InitializeDefaultState()
    {
        _currentState.SoC = 50;
        _currentState.SoH = 98;
        _currentState.Temperature_C = 25.0f;
        _currentState.Current_A = 0.0f;

        var regs = _currentState.BmsRegisters;

        regs.cuv_threshold = 2900;
        regs.cuv_recovery = 3200;
        regs.cov_threshold = 4280;
        regs.cov_recovery = 4100;
        regs.occ_threshold = 5000;
        regs.occ_recovery = 4500;
        regs.ocd_threshold = -7000;
        regs.ocd_recovery = -6500;
        regs.utc_threshold = 2731;
        regs.utc_recovery = 2781;
        regs.otc_threshold = 3281;
        regs.otc_recovery = 3231;
        regs.utd_threshold = 2631;
        regs.utd_recovery = 2581;
        regs.otd_threshold = 3421;
        regs.otd_recovery = 3281;
        regs.otf_threshold = 3581;
        regs.otf_recovery = 3531;

        for (int i = 0; i < 15; i++)
        {
            regs.lifetime_min_cell_v[i] = 2850;
            regs.lifetime_max_cell_v[i] = 4215;
        }
        regs.lifetime_max_charge_current = 4500;
        regs.lifetime_max_discharge_current = -15000;
        regs.lifetime_max_temp_cell = 3231;
        regs.lifetime_min_temp_cell = 2781;
        regs.lifetime_cov_events_count = 5;
        regs.lifetime_cuv_events_count = 12;
        regs.lifetime_ocd_events_count = 35;
    }

    public void SetStatusFlags(string registerName, ulong flags)
    {
        if (registerName == "Safety Status") _manualSafetyFlags = flags;
        else if (registerName == "Charging Status") _manualChargingFlags = flags;
        else if (registerName == "Operation Status") _manualOperationFlags = flags;
        else if (registerName == "Gauging Status") _manualGaugingFlags = flags;
        else if (registerName == "Battery Status") _manualBatteryFlags = flags;
        else if (registerName == "Manufacturing Status") _manualManuFlags = flags;
        else if (registerName == "PF Status") _manualPfFlags = flags;

        RecalculateStateFromCapacity();
        StateUpdated?.Invoke(_currentState);
    }

    public void SetCurrent(int currentInMa) => _currentState.Current_A = currentInMa / 1000.0f;
    public void SetTemperature(int tempInC) => _currentState.Temperature_C = tempInC;

    public void SetSoC(int soc)
    {
        int newSoC = Math.Max(0, Math.Min(100, soc));
        _currentCapacityMah = TOTAL_CAPACITY_MAH * (newSoC / 100.0);
        RecalculateStateFromCapacity();
        StateUpdated?.Invoke(_currentState);
    }

    public void SetSoH(int soh)
    {
        _currentState.SoH = Math.Max(0, Math.Min(100, soh));
        RecalculateStateFromCapacity();
        StateUpdated?.Invoke(_currentState);
    }

    public void SetIndividualCellVoltage(int cellIndex, int voltageInMv)
    {
        if (cellIndex >= 0 && cellIndex < CELL_COUNT)
        {
            _manualCellVoltages[cellIndex] = voltageInMv / 1000.0f;
            RecalculateStateFromCapacity();
            StateUpdated?.Invoke(_currentState);
        }
    }

    private void SimulationTick(object sender, EventArgs e)
    {
        double hoursElapsed = _simulationTimer.Interval / 3600000.0;
        double chargeChangeMah = _currentState.Current_A * 1000 * hoursElapsed;
        _currentCapacityMah += chargeChangeMah;
        _currentCapacityMah = Math.Max(0, Math.Min(TOTAL_CAPACITY_MAH, _currentCapacityMah));
        RecalculateStateFromCapacity();
        StateUpdated?.Invoke(_currentState);
    }

    private void RecalculateStateFromCapacity()
    {
        _currentState.SoC = (int)((_currentCapacityMah / TOTAL_CAPACITY_MAH) * 100);
        float baseCellVoltage = (float)(EMPTY_CELL_VOLTAGE_V + (_currentState.SoC / 100.0) * (FULL_CELL_VOLTAGE_V - EMPTY_CELL_VOLTAGE_V));
        for (int i = 0; i < CELL_COUNT; i++)
        {
            _currentState.CellVoltages_V[i] = _manualCellVoltages[i] ?? baseCellVoltage;
        }
        _currentState.PackVoltage_V = _currentState.CellVoltages_V.Sum();
        UpdateStatusFromCurrent();
        PopulateBmsRegisters();
    }

    private void UpdateStatusFromCurrent()
    {
        if (_currentState.Current_A > 0.05) { _currentState.Status = "CHARGING"; }
        else if (_currentState.Current_A < -0.05) { _currentState.Status = "DISCHARGING"; }
        else { _currentState.Status = "IDLE"; }
    }

    private void PopulateBmsRegisters()
    {
        var regs = _currentState.BmsRegisters;

        // --- 1. Populate Real-time Values ---
        regs.current = (short)(_currentState.Current_A * 1000);
        regs.pack_voltage = (ushort)(_currentState.PackVoltage_V * 1000);
        regs.temperature = (ushort)((_currentState.Temperature_C + 273.15) * 10);
        regs.relative_soc = (byte)_currentState.SoC;
        regs.state_of_health = (byte)_currentState.SoH;
        regs.remaining_capacity = (ushort)_currentCapacityMah;
        regs.full_charge_capacity = (ushort)TOTAL_CAPACITY_MAH;
        regs.average_time_to_empty = (ushort)(_currentState.Current_A < -0.1 ? (_currentCapacityMah / (-_currentState.Current_A * 1000)) * 60 : 65535);
        regs.average_time_to_full = (ushort)(_currentState.Current_A > 0.1 ? ((TOTAL_CAPACITY_MAH - _currentCapacityMah) / (_currentState.Current_A * 1000)) * 60 : 65535);
        regs.cycle_count = 15;
        for (int i = 0; i < 15; i++) { regs.cell_voltages[i] = (i < _currentState.CellVoltages_V.Length) ? (ushort)(_currentState.CellVoltages_V[i] * 1000) : (ushort)0; }

        // --- 2. Calculate Automatic Flags ---
        SafetyStatus autoSafetyFlags = CalculateSafetyFlags();
        ChargingStatus autoChargingFlags = CalculateChargingFlags(autoSafetyFlags);
        GaugingStatus autoGaugingFlags = CalculateGaugingFlags();
        BatteryStatus autoBatteryStatus = CalculateBatteryStatus();
        OperationStatus autoOperationFlags = CalculateOperationStatus(autoSafetyFlags);
        ManufacturingStatus autoManuFlags = CalculateManufacturingStatus(autoSafetyFlags);
        PFStatus autoPfFlags = CalculatePfStatus();

        // --- 3. Combine Automatic Flags with Manual Overrides ---
        regs.safety_status = (uint)autoSafetyFlags | (uint)_manualSafetyFlags;
        regs.charging_status = (ushort)((ushort)autoChargingFlags | (ushort)_manualChargingFlags);
        regs.gauging_status = (ushort)((ushort)autoGaugingFlags | (ushort)_manualGaugingFlags);
        regs.battery_status = (ushort)((ushort)autoBatteryStatus | (ushort)_manualBatteryFlags);
        regs.operation_status = (uint)autoOperationFlags | (uint)_manualOperationFlags;
        regs.manufacturing_status = (ushort)((ushort)autoManuFlags | (ushort)_manualManuFlags);
        regs.pf_status = (uint)autoPfFlags | (uint)_manualPfFlags;

        // --- 4. Update Lifetime Counters based on FINAL flags ---
        if (((SafetyStatus)regs.safety_status).HasFlag(SafetyStatus.COV)) regs.lifetime_cov_events_count++;
        if (((SafetyStatus)regs.safety_status).HasFlag(SafetyStatus.CUV)) regs.lifetime_cuv_events_count++;
        if (((SafetyStatus)regs.safety_status).HasFlag(SafetyStatus.OCD)) regs.lifetime_ocd_events_count++;
        if (((SafetyStatus)regs.safety_status).HasFlag(SafetyStatus.OCC)) regs.lifetime_occ_events_count++;

        // --- 5. Set Static Info ---
        regs.manufacturer_name = "VESTEL-SIM";
        regs.device_name = "BQ78350-R1-SIM";
        regs.firmware_version = "v1.2.3";
        regs.design_capacity = (ushort)TOTAL_CAPACITY_MAH;
        regs.design_voltage = 37000;
        regs.chemical_id = 0x0403;
    }

    // --- Helper methods for calculating flags ---
    private SafetyStatus CalculateSafetyFlags()
    {
        var regs = _currentState.BmsRegisters;
        SafetyStatus flags = SafetyStatus.None;
        if (_currentState.CellVoltages_V.Any(v => v > regs.cov_threshold / 1000.0f)) flags |= SafetyStatus.COV;
        if (_currentState.CellVoltages_V.Any(v => v < regs.cuv_threshold / 1000.0f)) flags |= SafetyStatus.CUV;
        if (_currentState.Current_A * 1000 > regs.occ_threshold) flags |= SafetyStatus.OCC;
        if (_currentState.Current_A * 1000 < regs.ocd_threshold) flags |= SafetyStatus.OCD;
        if (_currentState.Current_A * 1000 < regs.ocd_threshold * 1.5) flags |= SafetyStatus.AOLD;
        if (_currentState.Current_A * 1000 < regs.ocd_threshold * 2.0) flags |= SafetyStatus.ASCD;
        if (_currentState.Current_A > 0 && _currentState.Temperature_C > (_currentState.BmsRegisters.otc_threshold - 2731.5) / 10.0) flags |= SafetyStatus.OTC;
        if (_currentState.Current_A < 0 && _currentState.Temperature_C > (_currentState.BmsRegisters.otd_threshold - 2731.5) / 10.0) flags |= SafetyStatus.OTD;
        if (_currentState.Current_A > 0 && _currentState.Temperature_C < (_currentState.BmsRegisters.utc_threshold - 2731.5) / 10.0) flags |= SafetyStatus.UTC;
        if (_currentState.Current_A < 0 && _currentState.Temperature_C < (_currentState.BmsRegisters.utd_threshold - 2731.5) / 10.0) flags |= SafetyStatus.UTD;
        if (_currentState.Temperature_C > (_currentState.BmsRegisters.otf_threshold - 2731.5) / 10.0) flags |= SafetyStatus.OTF;
        return flags;
    }

    private ChargingStatus CalculateChargingFlags(SafetyStatus currentSafetyFlags)
    {
        var regs = _currentState.BmsRegisters;
        ChargingStatus flags = ChargingStatus.None;
        if (_currentState.Current_A > 0.05)
        {
            if (_currentState.Temperature_C < (regs.utc_threshold - 2731.5) / 10.0) flags |= ChargingStatus.UT;
            else if (_currentState.Temperature_C > (regs.otc_threshold - 2731.5) / 10.0) flags |= ChargingStatus.OT;
            else if (_currentState.Temperature_C < 10) flags |= ChargingStatus.LT;
            else if (_currentState.Temperature_C > 45) flags |= ChargingStatus.HT;
            else flags |= ChargingStatus.ST;
        }
        if (currentSafetyFlags.HasFlag(SafetyStatus.COV) || currentSafetyFlags.HasFlag(SafetyStatus.OTC)) flags |= ChargingStatus.IN;
        if (_currentState.SoC >= 100) flags |= (ChargingStatus.FCHG | ChargingStatus.VCT);
        return flags;
    }

    private GaugingStatus CalculateGaugingFlags()
    {
        GaugingStatus flags = GaugingStatus.VDQ;
        if (_currentState.Current_A < -0.05) flags |= GaugingStatus.DSG;
        if (_currentState.SoC >= 100) flags |= GaugingStatus.FC;
        if (_currentState.SoC <= 0) flags |= GaugingStatus.FD;
        return flags;
    }

    private BatteryStatus CalculateBatteryStatus()
    {
        BatteryStatus flags = BatteryStatus.None;
        if (_currentState.Current_A < -0.05) flags |= BatteryStatus.DSG;
        if (_currentState.SoC >= 100) flags |= BatteryStatus.FC;
        if (_currentState.SoC <= 0) flags |= BatteryStatus.FD;
        return flags;
    }

    private OperationStatus CalculateOperationStatus(SafetyStatus currentSafetyFlags)
    {
        OperationStatus flags = OperationStatus.PRES | OperationStatus.INIT | OperationStatus.XCHG | OperationStatus.XDSG;
        bool isSafetyErrorActive = (currentSafetyFlags & (SafetyStatus.COV | SafetyStatus.CUV | SafetyStatus.OCD | SafetyStatus.OTC | SafetyStatus.OTD
            | SafetyStatus.UTC | SafetyStatus.UTD | SafetyStatus.OTF)) != 0;
        if (!isSafetyErrorActive)
        {
            if (_currentState.Current_A < -0.05) flags |= OperationStatus.DSG;
            else if (_currentState.Current_A > 0.05) flags |= OperationStatus.CHG;
        }
        return flags;
    }

    private ManufacturingStatus CalculateManufacturingStatus(SafetyStatus currentSafetyFlags)
    {
        ManufacturingStatus flags = ManufacturingStatus.None;
        bool isSafetyErrorActive = (currentSafetyFlags & (SafetyStatus.COV | SafetyStatus.CUV | SafetyStatus.OCD | SafetyStatus.OTC | SafetyStatus.OTD
    | SafetyStatus.UTC | SafetyStatus.UTD | SafetyStatus.OTF)) != 0;

        if (!isSafetyErrorActive)
            flags |= ManufacturingStatus.FET_EN | ManufacturingStatus.SAFE_EN;
        else
            flags |= ManufacturingStatus.PF_EN;

        return flags;
    }

    private PFStatus CalculatePfStatus()
    {
        // Permanent Failures are not automatically simulated for now.
        return PFStatus.None;
    }
}