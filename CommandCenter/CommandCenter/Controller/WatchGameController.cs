using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Controller
{
    class WatchGameController : AbstractGameController
    {
        public WatchGameController(MainWindow parent)
            : base(parent, new WatchSilentUDPCommunication(parent), new WatchSilentEventsRecorder())
        {
        }
    }

    class WatchSilentUDPCommunication : UDPCommunication
    {
        public MainWindow parent;

        public WatchSilentUDPCommunication(MainWindow parent)
            : base(parent)
        {
            this.parent = parent;
        }

        public override void send(IPAddress address, JSONPacket outPacket)
        {
            string sendString = outPacket.ToString();
            this.parent.writeLog("Pura-pura kirim ke " + address + ": " + sendString);
        }
    }

    class WatchSilentEventsRecorder : EventsRecorder
    {
        public override void startRecording()
        {
            // silenced
        }

        public override void record(IPAddress sender, string eventText)
        {
            // silenced
        }

        public override void stopRecording()
        {
            // silenced
        }

        public override void setProperty(string name, string value)
        {
            // silenced
        }
    }
}
