using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rtos_Comm.application.JSON;

namespace Rtos_Comm.application.Driver
{
    public class CAN_Simulator
    {
        public Message_Format can_data_processor(uint id, TextBox[] dataTextBoxes, string dlcText)
        {
            var dataBytes = new ushort[8];
            for (int i = 0; i < dataTextBoxes.Length && i < 8; i++)
                if (!string.IsNullOrEmpty(dataTextBoxes[i].Text))
                    ushort.TryParse(dataTextBoxes[i].Text, System.Globalization.NumberStyles.HexNumber, null, out dataBytes[i]);

            var canDataPayload = new CanData
            {
                id = id,
                can_buffer = dataBytes,
                dlc = Convert.ToByte(dlcText)
            };
            var message = new Message_Format
            {
                driver = "can",
                data = canDataPayload
            };
            return message;
        }
    }
}
