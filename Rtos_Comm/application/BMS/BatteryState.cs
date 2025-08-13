using System;
using Rtos_Comm.application.JSON;

namespace Rtos_Comm
{
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
        public float[] CellVoltages_V { get; set; }

        // This object holds all the detailed register data to be sent over the pipe.
        public BMSData BmsRegisters { get; set; }

        public BatteryState()
        {
            CellVoltages_V = new float[10]; // Our simulation has 10 cells
            BmsRegisters = new BMSData();
        }

    }
}