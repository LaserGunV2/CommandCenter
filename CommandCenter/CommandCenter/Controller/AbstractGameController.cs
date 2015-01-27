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
using CommandCenter.Model;
using CommandCenter.Model.Protocol;
using NLog;
using System.Threading;

namespace CommandCenter.Controller
{
    public abstract class AbstractGameController
    {
        public enum State { IDLE, REGISTRATION, EXERCISE };

        protected MainWindow parent;
        protected UDPCommunication communication;
        protected State state;
        public String gameId = null;
        protected List<Prajurit> prajurits;
        protected Dictionary<int, Senjata> senjatas;
        protected EventsRecorder recorder;
        protected PrajuritDatabase prajuritDatabase;
        protected int initialAmmo;
        protected List<IPAddress> watchers;

        public AbstractGameController(MainWindow parent, UDPCommunication communication, EventsRecorder recorder)
        {
            this.communication = communication;
            this.parent = parent;
            this.prajurits = parent.prajurits;
            this.senjatas = parent.senjatas;
            this.recorder = recorder;
            this.prajuritDatabase = parent.prajuritDatabase;
            this.watchers = new List<IPAddress>();
        }

        public String startRegistration(string gameId, int initialAmmo)
        {
            this.gameId = gameId;
            this.initialAmmo = initialAmmo;

            try
            {
                prajurits.Clear();
                senjatas.Clear();
                parent.refreshTable();
                parent.mapDrawer.clearMap();
                this.recorder.startRecording();
                this.recorder.setProperty(EventsRecorder.PROP_GAMEID, gameId);
                this.recorder.setProperty(EventsRecorder.PROP_AMMO, "" + initialAmmo);
                communication.listenAsync(this);
                watchers.Clear();
                this.state = State.REGISTRATION;
            }
            catch (Exception e)
            {
                parent.writeLog(LogLevel.Error, e.ToString());
                return null;
            }
            parent.writeLog(LogLevel.Info, "Pendaftaran dibuka, game id = " + gameId);
            return gameId;
        }

        public virtual void startExercise()
        {
            string prajuritGroups = "";
            for (int i = 0; i < prajurits.Count; i++)
            {
                prajuritGroups += prajurits[i].group;
            }

            this.state = State.EXERCISE;
            parent.mapDrawer.showEveryone();
            recorder.record(null, EventsRecorder.START + "/" + prajuritGroups);
            parent.writeLog(LogLevel.Info, "Permainan dimulai");
        }

        public virtual void stopExercise(bool force)
        {
            if (state == State.IDLE)
            {
                return;
            }

            // Stop incoming communication
            communication.stopListenAsync(force);
            this.state = State.IDLE;

            // Send endgame signal
            new Task(() => { sendEndGameSignals(); }).Start();

            // Remove any references and members.
            this.recorder.stopRecording();
            gameId = null;

            parent.refreshTable();
            parent.writeLog(LogLevel.Info, "Permainan diakhiri");
        }

        private void sendEndGameSignals()
        {
            JSONPacket outPacket = new JSONPacket("endgame");
            foreach (Prajurit prajurit in prajurits)
            {
                communication.send(prajurit.ipAddress, outPacket);
                Thread.Sleep(100);
            }
        }

        public virtual void handlePacket(IPAddress address, JSONPacket inPacket)
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
                            int nomerUrut = Prajurit.findPrajuritIndexByNomerInduk(prajurits, inPacket.getParameter("nomerInduk"));
                            if (nomerUrut == -1)
                            {
                                // Register
                                nomerUrut = prajurits.Count + 1;
                                Prajurit newPrajurit = new Prajurit(nomerUrut, inPacket.getParameter("nomerInduk"), address, "A", null);
                                prajuritDatabase.retrieveNameFromDatabase(newPrajurit);
                                prajurits.Add(newPrajurit);
                                parent.refreshTable();
                            } else {
                                nomerUrut = nomerUrut + 1;
                            }

                            // Confirm
                            JSONPacket outPacket = new JSONPacket("confirm");
                            outPacket.setParameter("androidId", "" + nomerUrut);
                            communication.send(address, outPacket);
                        }
                        else
                        {
                            parent.writeLog(LogLevel.Warn, "Registration from " + address + " is ignored as we are not in registration phase");
                        }
                    }
                    else
                    {
                        parent.writeLog(LogLevel.Warn, "Registration from " + address + " with game id " + inPacket.getParameter("gameid") + " is ignored");
                    }
                }
                else if (type.Equals("event/update"))
                {
                    if (inPacket.getParameter("gameid").Equals(gameId))
                    {
                        if (state != State.IDLE)
                        {
                            int index = Int32.Parse(inPacket.getParameter("androidId")) - 1;
                            Prajurit prajurit = prajurits[index];
                            prajurit.setLocation(inPacket.getParameter("location"));
                            prajurit.heading = Double.Parse(inPacket.getParameter("heading"));
                            prajurit.accuracy = Int32.Parse(inPacket.getParameter("accuracy"));
                            string[] prajuritState = inPacket.getParameter("state").Split('/');
                            prajurit.state = (prajuritState[0].Equals("alive") ? Prajurit.State.NORMAL : Prajurit.State.DEAD);
                            prajurit.posture = (prajuritState[1].Equals("stand") ? Prajurit.Posture.STAND : Prajurit.Posture.CRAWL);
                            if (inPacket.getParameter("action").Equals("hit"))
                            {
                                int idSenjata = Int32.Parse(inPacket.getParameter("idsenjata"));
                                int counter = Int32.Parse(inPacket.getParameter("counter"));
                                if (state == State.REGISTRATION) {
                                    // Registration phase, senjata assign to prajurit.
                                    Senjata newSenjata = new Senjata(idSenjata, prajurit, counter, this.initialAmmo);
                                    prajurit.senjata = newSenjata;
                                    senjatas.Add(newSenjata.idSenjata, newSenjata);
                                } else if (state == State.EXERCISE) {
                                    Senjata senjataPenembak = senjatas[idSenjata];
                                    senjataPenembak.currentCounter = counter;
                                    if (senjataPenembak.getRemainingAmmo() > 0) {
                                        prajurit.state = Prajurit.State.HIT;
                                        communication.send(prajurit.ipAddress, new JSONPacket("killed"));
                                    }
                                }
                                parent.refreshTable();
                            } else if (inPacket.getParameter("action").Equals("shoot"))
                            {
                                prajurit.state = Prajurit.State.SHOOT;
                            }
                            parent.mapDrawer.updateMap(prajurit);
                            parent.refreshTable();
                        }
                        else
                        {
                            parent.writeLog(LogLevel.Warn, "Update event is ignored when state is idle.");
                        }
                    }
                    else
                    {
                        parent.writeLog(LogLevel.Warn, "Event update from " + address + " with game id " + inPacket.getParameter("gameid") + " is ignored");
                    }
                }
                else if (type.Equals("ping"))
                {
                    // Reply with pong, and let the rest of parameters be the same.
                    inPacket.setParameter("type", "pong");
                    communication.send(address, inPacket);
                }
                else if (type.Equals("pantau/register"))
                {
                    if (inPacket.getParameter("gameid").Equals(gameId))
                    {
                        JSONPacket outPacket = new JSONPacket("pantau/confirm");
                        if (state == State.REGISTRATION && prajurits.Count == 0)
                        {
                            watchers.Add(address);
                            outPacket.setParameter("status", "ok");
                            outPacket.setParameter("gameid", gameId);
                            outPacket.setParameter("ammo", "" + initialAmmo);
                        }
                        else
                        {
                            outPacket.setParameter("status", "Tidak bisa pantau karena sudah ada prajurit yang bergabung atau latihan sudah dimulai.");
                        }
                        communication.send(address, outPacket, UDPCommunication.IN_PORT);
                        parent.writeLog(LogLevel.Info, address + " joined as watcher");
                    }
                    else
                    {
                        parent.writeLog(LogLevel.Warn, "Watch request from " + address + " is ignored, as the game id " + inPacket.getParameter("gameid") + " mismatched");
                    }
                }
                else if (type.Equals("pantau/state"))
                {
                    if (inPacket.getParameter("state").StartsWith("START"))
                    {
                        string[] tokens = inPacket.getParameter("state").Split('/');
                        for (int i = 0; i < tokens[1].Length; i++)
                        {
                            prajurits[i].group = "" + tokens[1][i];
                        }
                        startExercise();
                    }
                    else if (inPacket.getParameter("state").Equals("STOP"))
                    {
                        stopExercise(false);
                    }
                    else
                    {
                        parent.writeLog(LogLevel.Error, "Unknown state set: " + inPacket.getParameter("state"));
                    }
                }
                else
                {
                    parent.writeLog(LogLevel.Error, "Unknown type: " + type);
                }
            }
            catch (Exception e)
            {
                parent.writeLog(LogLevel.Error, "Unhandled exception: " + e);
            }
        }

    }
}