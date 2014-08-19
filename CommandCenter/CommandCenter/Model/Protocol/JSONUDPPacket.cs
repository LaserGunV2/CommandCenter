using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Protocol
{
    class JSONUDPPacket
    {
        private Dictionary<string, string> parameters = new Dictionary<string,string>();

        public JSONUDPPacket(string type)
        {
            parameters.Add("type", type);
        }

        protected JSONUDPPacket()
        {
            // void
        }

        public static JSONUDPPacket createFromJSON(string json)
        {
            JSONUDPPacket packet = new JSONUDPPacket();
            packet.parameters = JsonConvert.DeserializeObject<Dictionary<string,string>>(json);
            return packet;
        }

        public void addParameter(string name, string value)
        {
            parameters.Add(name, value);
        }

        public string getParameter(string name)
        {
            return parameters[name];
        }
    }
}
