using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Controller
{
    class WatchGameController : AbstractGameController
    {
        WatchSilentUDPCommunication modifiedCommunication;
        public WatchGameController(MainWindow parent)
            : base(parent, new WatchSilentUDPCommunication(parent), new WatchSilentEventsRecorder())
        {
            modifiedCommunication = (WatchSilentUDPCommunication)this.communication;
        }

        public void watchExercise(String gameId)
        {
            JSONPacket packet = new JSONPacket("pantau/register");
            packet.setParameter("gameid", gameId);
            modifiedCommunication.broadcast(packet);
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

        public void broadcast(JSONPacket outPacket)
        {
            UdpClient client = new UdpClient(IPAddress.Broadcast + "", UDPCommunication.IN_PORT);
            string sendString = outPacket.ToString();
            Byte[] sendBytes = Encoding.UTF8.GetBytes(sendString);
            try
            {
                client.Send(sendBytes, sendBytes.Length);
                parent.writeLog("Broadcast " + sendString);
            }
            catch (Exception e)
            {
                parent.writeLog("Error: " + e);
            }
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
