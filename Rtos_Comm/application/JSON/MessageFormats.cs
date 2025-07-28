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
}
