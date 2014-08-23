using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Protocol
{
    class JSONPacket
    {
        private Dictionary<string, string> parameters = new Dictionary<string,string>();

        public JSONPacket(string type)
        {
            parameters.Add("type", type);
        }

        protected JSONPacket()
        {
            // void
        }

        public static JSONPacket createFromJSON(string json)
        {
            JSONPacket packet = new JSONPacket();
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

        public string ToString()
        {
            return JsonConvert.SerializeObject(parameters);
        }
    }
}
