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

namespace CommandCenter.Model.Protocol
{
    abstract class AbstractGameController
    {
        public enum State { IDLE, REGISTRATION, EXERCISE };

        protected MainWindow parent;
        protected UDPCommunication communication;
        protected State state;
        protected String gameId = null;
        protected List<Prajurit> prajurits;
        protected Dictionary<int, Senjata> senjatas;
        protected EventsRecorder recorder;
        protected PrajuritDatabase prajuritDatabase;

        public AbstractGameController(MainWindow parent, UDPCommunication communication, EventsRecorder recorder)
        {
            this.communication = communication;
            this.parent = parent;
            this.prajurits = parent.prajurits;
            this.senjatas = parent.senjatas;
            this.recorder = recorder;
            this.prajuritDatabase = parent.prajuritDatabase;
        }

        public String startRegistration()
        {
            Random random = new Random();
            String gameId = "";
            for (int i = 0; i < 3; i++)
            {
                gameId += random.Next(10);
            }

            return startRegistration(gameId);
        }

        public String startRegistration(string gameId)
        {
            this.gameId = gameId;

            try
            {
                prajurits.Clear();
                parent.refreshTable();
                parent.mapDrawer.clearMap();
                this.recorder.startRecording(gameId);
                communication.listenAsync(this);
                this.state = State.REGISTRATION;
            }
            catch (Exception e)
            {
                parent.writeLog(e.ToString());
                return null;
            }
            parent.writeLog("Pendaftaran dibuka, game id = " + gameId);
            return gameId;
        }


        public void startExercise()
        {
            this.state = State.EXERCISE;
            parent.mapDrawer.showEveryone();
            recorder.record(null, EventsRecorder.START);
            parent.writeLog("Permainan dimulai");
        }

        public void stopExercise()
        {
            if (state == State.IDLE)
            {
                return;
            }

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
            this.recorder.stopRecording();
            gameId = null;

            parent.refreshTable();
            parent.writeLog("Permainan diakhiri");
        }

        public void handlePacket(IPAddress address, JSONPacket inPacket)
        {
            try
            {
                this.recorder.record(address, inPacket.ToString());
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
                                prajuritDatabase.retrieveNameFromDatabase(newPrajurit);
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
                            prajurit.alive = prajuritState[0].Equals("alive");
                            prajurit.posture = prajuritState[1];
                        }
                        catch (KeyNotFoundException)
                        {
                            parent.writeLog("WARNING: you didn't send state attribute. [TODO remove after everyone implemented]");
                        }
                        if (inPacket.getParameter("action").Equals("hit"))
                        {
                            if (state == State.REGISTRATION) {
                                // Registration phase, senjata assign to prajurit.
                                Senjata newSenjata = new Senjata(Int32.Parse(inPacket.getParameter("idsenjata")), prajurit, Int32.Parse(inPacket.getParameter("counter")));
                                prajurit.senjata = newSenjata;
                                senjatas.Add(newSenjata.idSenjata, newSenjata);
                                parent.refreshTable();
                            } else if (state == State.EXERCISE) {
                                // TODO check ammo
                                communication.send(prajurit.ipAddress, new JSONPacket("killed"));
                            }
                            
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