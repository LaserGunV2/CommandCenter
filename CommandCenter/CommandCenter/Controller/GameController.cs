using CommandCenter.View;
using System;
using System.Collections;
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
        List<Prajurit> prajurits;

        public int counter = 0;

        public GameController(MainWindow parent, UDPCommunication comm)
        {
            this.parent = parent;
            this.comm = comm;
            this.prajurits = parent.prajurits;
        }

        public void handlePacket(IPAddress address, JSONPacket inPacket)
        {
            String type = inPacket.getParameter("type");
            if (type.Equals("register"))
            {
                if (inPacket.getParameter("gameid").Equals(parent.gameId))
                {
                    if (Prajurit.findPrajuritByNomerInduk(prajurits, inPacket.getParameter("nomerInduk")) == -1)
                    {
                        // Register
                        int nomerUrut = prajurits.Count + 1;
                        Prajurit newPrajurit = new Prajurit(nomerUrut, inPacket.getParameter("nomerInduk"), address, null);
                        prajurits.Add(newPrajurit);
                        parent.refreshTable();

                        // Confirm
                        JSONPacket outPacket = new JSONPacket("confirm");
                        outPacket.addParameter("androidId", "" + (counter++));
                        comm.send(address, outPacket);
                    }
                    else
                    {
                        parent.writeLog(inPacket.getParameter("nomerInduk") + " has already registered, hence ignored.");
                    }
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