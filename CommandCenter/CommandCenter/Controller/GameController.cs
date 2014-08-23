using CommandCenter.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Protocol
{
    class GameController
    {

        MainWindow parent;
        UDPCommunication comm;

        public int counter = 0;

        public GameController(MainWindow parent, UDPCommunication comm)
        {
            this.parent = parent;
            this.comm = comm;
        }

        public void handlePacket(IPAddress address, JSONPacket inPacket)
        {
            String type = inPacket.getParameter("type");
            if (type.Equals("register"))
            {
                if (inPacket.getParameter("gameid").Equals(parent.gameId))
                {
                    // TODO Register
                    JSONPacket outPacket = new JSONPacket("confirm");
                    outPacket.addParameter("androidId", "" + (counter++));
                    comm.send(address, outPacket);
                }
                else
                {
                    parent.writeLog("Registration from " + address + " with game id " + inPacket.getParameter("gameid") + " is ignored");
                }
            } else {
                parent.writeLog("Unknown type: " + type);
            }
        }
    }
}