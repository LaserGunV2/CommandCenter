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
            foreach(IPAddress watcher in watchers) {
                parent.writeLog("Kirim ke pemantau " + watcher + " pesan " + inPacket);
                communication.send(watcher, inPacket);
            }
        }
    }
}