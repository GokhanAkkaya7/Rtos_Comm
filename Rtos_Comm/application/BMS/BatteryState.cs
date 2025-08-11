// FILE: BatteryState.cs

using System;

namespace Rtos_Comm
{
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

    /// <summary>
    /// Holds the raw register data in the format expected by the C project.
    /// This object is what will be serialized to JSON.
    /// </summary>
    public class Bq78350Registers
    {
        // Single Word (uint16_t) returning registers
        public short Current { get; set; }                // mA (signed)
        public ushort PackVoltage { get; set; }           // mV
        public ushort Temperature { get; set; }           // 0.1K
        public ushort[] CellVoltages { get; set; } = new ushort[15]; // mV
        public byte RelativeSoC { get; set; }             // %
        public byte StateOfHealth { get; set; }           // %
        public ushort CycleCount { get; set; }
        public ushort RemainingCapacity { get; set; }     // mAh
        public ushort FullChargeCapacity { get; set; }    // mAh

        // Block returning registers (represented as byte arrays for JSON)
        public byte[] OperationStatus { get; set; }
        public byte[] ChargingStatus { get; set; }
        public byte[] SafetyStatus { get; set; }
        // Other blocks can be added here...
    }

    /// <summary>
    /// Encapsulates the complete state of the simulated BQ78350 chip.
    /// </summary>
    public class BatteryState
    {
        // High-level values for UI and general simulation logic (using standard units)
        public float PackVoltage_V { get; set; }
        public float Current_A { get; set; }
        public float Temperature_C { get; set; }
        public int SoC { get; set; }
        public int SoH { get; set; }
        public float[] CellVoltages_V { get; set; } = new float[10]; // Our simulation has 10 cells
        public string Status { get; set; }

        // This object holds the data in the raw register format.
        public Bq78350Registers Registers { get; set; }

        public BatteryState()
        {
            Registers = new Bq78350Registers();
        }
    }
}