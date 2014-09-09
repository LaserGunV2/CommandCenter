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

        public static JSONPacket createFromJSONBytes(byte[] jsonBytes)
        {
            JSONPacket packet = new JSONPacket();
            string jsonString = Encoding.UTF8.GetString(jsonBytes);
            packet.parameters = JsonConvert.DeserializeObject<Dictionary<string,string>>(jsonString);
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

        public override string ToString()
        {
            return JsonConvert.SerializeObject(parameters);
        }

        public byte[] toBytes()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
    }
}
