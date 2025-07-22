using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Rtos_Comm.application.JSON
{
    public class Json_Class
    {
        public string Json_Processor(List<Message_Format> data)
        {
            return JsonSerializer.Serialize(data);
        }
        public List<Message_Format> Json_Parser(string data)
        {
            return JsonSerializer.Deserialize<List<Message_Format>>(data); ;
        }

    }
}
