using Rtos_Comm.application.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rtos_Comm.application.JSON
{
    // NEW DATA: add related driver class here.
    public class Message_Format
    {
        public string driver { get; set; }
        public object data { get; set; }
    }
    public class AdcData
    {
        public int index { get; set; }
        public ushort[] buffer { get; set; }
    }
    public class UartData
    {
        public string value { get; set; }
    }
    public class IoData
    {
        public UInt16 pin { get; set; }
        public app_io_level_t state { get; set; }
    }
    public class IrqData
    {
        public int channel_count { get; set; }
        public List<ushort> channel_list { get; set; }

        public IrqData()
        {
            channel_list = new List<ushort>();
        }
        public IrqData Clone()
        {
            var clone = new IrqData();
            clone.channel_count = this.channel_count;
            if (this.channel_list != null)
            {
                clone.channel_list = new List<ushort>(this.channel_list);
            }
            else
            {
                clone.channel_list = new List<ushort>();
            }
            return clone;
        }
    }

    public class CanData
    {
        public UInt32 id { get; set; }

        public int dlc { get; set; }
        public  ushort[] can_buffer { get; set; }
    }

    public class GPTData
    {
        public int channel_count { get; set; }
        public int[] Channel { get; set; }
        public int[] Period { get; set; }
        public GptUnit[] Unit { get; set; }
    }

    public class RTCData
    {
        public rtc_event_t rtc_event { get; set; }
        public int second { get; set; }
        public int minute { get; set; }
        public int hour { get; set; }
        public int day { get; set; }
        public int month { get; set; }
        public int year { get; set; }
    }

    #region BMS Data Structures

    public class BMSData
    {
        // CATEGORY 1: REAL-TIME SBS COMMANDS
        public short current { get; set; }
        public ushort pack_voltage { get; set; }
        public ushort temperature { get; set; }
        public ushort[] cell_voltages { get; set; } = new ushort[15];
        public byte relative_soc { get; set; }
        public byte state_of_health { get; set; }
        public ushort remaining_capacity { get; set; }
        public ushort full_charge_capacity { get; set; }
        public ushort average_time_to_empty { get; set; }
        public ushort average_time_to_full { get; set; }
        public ushort cycle_count { get; set; }

        // CATEGORY 2: STATUS BLOCKS
        public byte[] safety_status { get; set; }
        public byte[] charging_status { get; set; }
        public byte[] operation_status { get; set; }
        public byte[] gauging_status { get; set; }
        public byte[] battery_status { get; set; }
        public byte[] manufacturing_status { get; set; }
        public byte[] pf_status { get; set; }

        // CATEGORY 3: STRING AND INFO BLOCKS
        public string manufacturer_name { get; set; }
        public string device_name { get; set; }
        public string firmware_version { get; set; }
        public ushort design_capacity { get; set; }
        public ushort design_voltage { get; set; }
        public ushort manufacture_date { get; set; }
        public ushort chemical_id { get; set; }

        // CATEGORY 4: CONFIGURATION DATA
        public ushort cuv_threshold { get; set; }
        public ushort cuv_recovery { get; set; }
        public ushort cov_threshold { get; set; }
        public ushort cov_recovery { get; set; }
        public short occ_threshold { get; set; }
        public short occ_recovery { get; set; }
        public short ocd_threshold { get; set; }
        public short ocd_recovery { get; set; }
        public short utc_threshold { get; set; }
        public short utc_recovery { get; set; }
        public short otc_threshold { get; set; }
        public short otc_recovery { get; set; }
        public short utd_threshold { get; set; }
        public short utd_recovery { get; set; }
        public short otd_threshold { get; set; }
        public short otd_recovery { get; set; }
        public short otf_threshold { get; set; }
        public short otf_recovery { get; set; }

        // CATEGORY 5: LIFETIME DATA
        public ushort[] lifetime_max_cell_v { get; set; } = new ushort[15];
        public ushort[] lifetime_min_cell_v { get; set; } = new ushort[15];
        public ushort lifetime_max_delta_cell_v { get; set; }
        public short lifetime_max_charge_current { get; set; }
        public short lifetime_max_discharge_current { get; set; }
        public short lifetime_max_avg_dsg_current { get; set; }
        public short lifetime_max_avg_dsg_power { get; set; }
        public short lifetime_max_temp_cell { get; set; }
        public short lifetime_min_temp_cell { get; set; }
        public ushort lifetime_max_delta_temp_cell { get; set; }
        public short lifetime_max_temp_fet { get; set; }
        public ushort lifetime_shutdowns_count { get; set; }
        public byte[] lifetime_cell_balance_time { get; set; } = new byte[15];
        public ushort lifetime_total_fw_runtime_days { get; set; }
        public ushort lifetime_time_spent_ut_days { get; set; }
        public ushort lifetime_time_spent_lt_days { get; set; }
        public ushort lifetime_time_spent_st_days { get; set; }
        public ushort lifetime_time_spent_ht_days { get; set; }
        public ushort lifetime_time_spent_ot_days { get; set; }
        public ushort lifetime_cov_events_count { get; set; }
        public ushort lifetime_cuv_events_count { get; set; }
        public ushort lifetime_ocd_events_count { get; set; }
        public ushort lifetime_occ_events_count { get; set; }
        public ushort lifetime_otf_events_count { get; set; }
        public ushort lifetime_valid_charge_term_count { get; set; }
        public ushort lifetime_fcc_update_count { get; set; }
    }

    #endregion
    public class BmsConfigData
    {
        // --- Cell Voltage Protections ---
        public ushort CuvThreshold { get; set; } = 2800;    // Cell Undervoltage Threshold (mV)
        public ushort CuvRecovery { get; set; } = 3100;     // Cell Undervoltage Recovery (mV)
        public ushort CovThreshold { get; set; } = 4280;    // Cell Overvoltage Threshold (mV)
        public ushort CovRecovery { get; set; } = 4150;     // Cell Overvoltage Recovery (mV)

        // --- Current Protections ---
        public short OccThreshold { get; set; } = 5000;     // Overcurrent in Charge Threshold (mA)
        public short OccRecovery { get; set; } = 4500;      // Overcurrent in Charge Recovery (mA)
        public short OcdThreshold { get; set; } = -7000;    // Overcurrent in Discharge Threshold (mA)
        public short OcdRecovery { get; set; } = -6500;     // Overcurrent in Discharge Recovery (mA)

        // --- Temperature Protections (Charge) ---
        // Values are in 0.1 Kelvin. To convert from Celsius: (Celsius + 273.15) * 10
        public short UtcThreshold { get; set; } = 2731;     // Undertemperature for Charge Threshold (~0°C)
        public short UtcRecovery { get; set; } = 2781;      // Undertemperature for Charge Recovery (~5°C)
        public short OtcThreshold { get; set; } = 3281;     // Overtemperature for Charge Threshold (~55°C)
        public short OtcRecovery { get; set; } = 3231;      // Overtemperature for Charge Recovery (~50°C)

        // --- Temperature Protections (Discharge) ---
        public short UtdThreshold { get; set; } = 2531;     // Undertemperature for Discharge Threshold (~-20°C)
        public short UtdRecovery { get; set; } = 2581;      // Undertemperature for Discharge Recovery (~-15°C)
        public short OtdThreshold { get; set; } = 3331;     // Overtemperature for Discharge Threshold (~60°C)
        public short OtdRecovery { get; set; } = 3281;      // Overtemperature for Discharge Recovery (~55°C)

        // --- FET Temperature Protection ---
        public short OtfThreshold { get; set; } = 3581;     // Overtemperature for FET Threshold (~85°C)
        public short OtfRecovery { get; set; } = 3531;      // Overtemperature for FET Recovery (~80°C)

        // --- Other Configurations ---
        public byte CurrentDeadband { get; set; } = 5;       // mA
    }

}
