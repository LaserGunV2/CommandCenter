using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
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
        enum State { IDLE, REGISTRATION, PLAYING };

        MainWindow parent;
        UDPCommunication comm;
        List<Prajurit> prajurits;
        State state;
        String gameId = null;

        public GameController(MainWindow parent)
        {
            this.parent = parent;
            this.comm = new UDPCommunication(parent);
            this.prajurits = parent.prajurits;
            this.state = State.IDLE;
        }

        public String startRegistration()
        {
            Random random = new Random();
            gameId = "";
            for (int i = 0; i < 3; i++)
            {
                gameId += random.Next(10);
            }

            comm.listenAsync(this);
            this.state = State.REGISTRATION;
            parent.writeLog("Pendaftaran dibuka, game id = " + gameId);
            return gameId;
        }

        public void startPlaying()
        {
            this.state = State.PLAYING;
            parent.writeLog("Permainan dimulai");
        }

        public void stopPlaying()
        {
            this.state = State.IDLE;
            comm.stopListenAsync();
            gameId = null;
        }

        public void handlePacket(IPAddress address, JSONPacket inPacket)
        {
            try
            {
                String type = inPacket.getParameter("type");
                if (type.Equals("register"))
                {
                    if (inPacket.getParameter("gameid").Equals(gameId))
                    {
                        if (state == State.REGISTRATION)
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
                                outPacket.addParameter("androidId", "" + (nomerUrut++));
                                comm.send(address, outPacket);
                            }
                            else
                            {
                                parent.writeLog(inPacket.getParameter("nomerInduk") + " has already registered, hence ignored.");
                            }
                        }
                        else
                        {
                            parent.writeLog("Registration from " + address + " is ignored as we are not in registration phase");
                        }
                    }
                    else
                    {
                        parent.writeLog("Registration from " + address + " with game id " + inPacket.getParameter("gameid") + " is ignored");
                    }
                }
                else if (type.Equals("event/update"))
                {
                    int index = Int32.Parse(inPacket.getParameter("androidId")) - 1;
                    Prajurit prajurit = prajurits[index];
                    prajurit.setLocation(inPacket.getParameter("location"));
                    prajurit.heading = Double.Parse(inPacket.getParameter("heading"));
                    // FIXME implement accuracy
                    parent.mapDrawer.updateMap();
                }
                else
                {
                    parent.writeLog("Unknown type: " + type);
                }
            }
            catch (Exception e)
            {
                parent.writeLog("Unhandled exception: " + e);
            }
        }
    }
}