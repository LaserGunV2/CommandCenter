using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using NLog;

namespace CommandCenter.Controller
{
    class LiveGameController : AbstractGameController
    {

        public LiveGameController(MainWindow parent)
            : base(parent, new UDPCommunication(parent), parent.recorder)
        {
            // void
        }

        public String startRegistration(int initialAmmo)
        {
            Random random = new Random();
            String gameId = "";
            for (int i = 0; i < 3; i++)
            {
                gameId += random.Next(10);
            }

            return startRegistration(gameId, initialAmmo);
        }

        public override void handlePacket(IPAddress address, JSONPacket inPacket)
        {
            base.handlePacket(address, inPacket);
            if (!inPacket.getParameter("type").StartsWith("pantau/"))
            {
                foreach (IPAddress watcher in watchers)
                {
                    parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + inPacket);
                    communication.send(watcher, inPacket, UDPCommunication.IN_PORT);
                }
            }
        }

        public override void startExercise()
        {
            base.startExercise();
            foreach (IPAddress watcher in watchers)
            {
                JSONPacket packet = new JSONPacket("pantau/state");
                packet.setParameter("state", "START");
                parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + packet);
                communication.send(watcher, packet, UDPCommunication.IN_PORT);
            }
        }

        public override void stopExercise(bool force)
        {
            base.stopExercise(force);
            foreach (IPAddress watcher in watchers)
            {
                JSONPacket packet = new JSONPacket("pantau/state");
                packet.setParameter("state", "STOP");
                parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + packet);
                communication.send(watcher, packet, UDPCommunication.IN_PORT);
            }
        }
    }
}