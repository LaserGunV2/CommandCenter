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

namespace CommandCenter.Model.Protocol
{
    class GameController
    {
        enum State { IDLE, REGISTRATION, PLAYING };

        MainWindow parent;
        UDPCommunication communication;
        List<Prajurit> prajurits;
        Dictionary<int, Senjata> senjatas;
        PrajuritDatabase prajuritDatabase;
        State state;
        String gameId = null;

        public GameController(MainWindow parent)
        {
            this.parent = parent;
            this.communication = new UDPCommunication(parent);
            this.prajurits = parent.prajurits;
            this.senjatas = parent.senjatas;
            this.prajuritDatabase = new PrajuritDatabase();
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

            communication.listenAsync(this);
            this.state = State.REGISTRATION;
            parent.writeLog("Pendaftaran dibuka, game id = " + gameId);
            return gameId;
        }

        public void startPlaying()
        {
            this.state = State.PLAYING;
            parent.mapDrawer.showEveryone();
            parent.writeLog("Permainan dimulai");
        }

        public void stopPlaying()
        {
            // Stop incoming communication
            communication.stopListenAsync();
            this.state = State.IDLE;

            // Send endgame signal
            JSONPacket outPacket = new JSONPacket("endgame");
            foreach (Prajurit prajurit in prajurits)
            {
                communication.send(prajurit.ipAddress, outPacket);
            }

            // Remove any references and members.
            prajurits.Clear();
            parent.mapDrawer.clearMap();
            gameId = null;

            parent.refreshTable();
            parent.writeLog("Permainan diakhiri");
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
                                Prajurit newPrajurit = new Prajurit(nomerUrut, inPacket.getParameter("nomerInduk"), address, "A", null);
                                prajurits.Add(newPrajurit);
                                parent.refreshTable();

                                // Confirm
                                JSONPacket outPacket = new JSONPacket("confirm");
                                outPacket.addParameter("androidId", "" + (nomerUrut++));
                                communication.send(address, outPacket);
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
                    if (state != State.IDLE)
                    {
                        int index = Int32.Parse(inPacket.getParameter("androidId")) - 1;
                        Prajurit prajurit = prajurits[index];
                        prajurit.setLocation(inPacket.getParameter("location"));
                        prajurit.heading = Double.Parse(inPacket.getParameter("heading"));
                        try
                        {
                            string[] prajuritState = inPacket.getParameter("state").Split('/');
                            prajurit.state = (prajuritState[0].Equals("alive") ? Prajurit.State.NORMAL : Prajurit.State.DEAD);
                            prajurit.posture = (prajuritState[1].Equals("stand") ? Prajurit.Posture.STAND : Prajurit.Posture.CRAWL);
                        }
                        catch (KeyNotFoundException)
                        {
                            parent.writeLog("WARNING: you didn't send state attribute. [TODO remove after everyone implemented]");
                        }
                        if (inPacket.getParameter("action").Equals("hit"))
                        {
                            prajurit.state = Prajurit.State.HIT;
                            if (state == State.REGISTRATION) {
                                // Registration phase, senjata assign to prajurit.
                                Senjata newSenjata = new Senjata(Int32.Parse(inPacket.getParameter("idsenjata")), prajurit, Int32.Parse(inPacket.getParameter("counter")));
                                prajurit.senjata = newSenjata;
                                senjatas.Add(newSenjata.idSenjata, newSenjata);
                                parent.refreshTable();
                            } else if (state == State.PLAYING) {
                                // TODO check ammo
                                communication.send(prajurit.ipAddress, new JSONPacket("killed"));
                            }
                            
                        } else if (inPacket.getParameter("action").Equals("shoot"))
                        {
                            prajurit.state = Prajurit.State.SHOOT;
                        }
                        parent.mapDrawer.updateMap(prajurit);
                        parent.refreshTable();
                    }
                    else
                    {
                        parent.writeLog("Update event is ignored when state is idle.");
                    }
                }
                else if (type.Equals("ping"))
                {
                    communication.send(address, new JSONPacket("pong"));
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