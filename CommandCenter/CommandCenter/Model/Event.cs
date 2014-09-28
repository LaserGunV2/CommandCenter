using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
    public class Event
    {
        public Int64 timeOffset;
        public IPAddress sender;
        public string packet;

        public Event(Int64 timeOffset, IPAddress sender, string packet)
        {
            this.timeOffset = timeOffset;
            this.sender = sender;
            this.packet = packet;
        }
    }
}
