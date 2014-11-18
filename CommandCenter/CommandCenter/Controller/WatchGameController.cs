using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
            this.gameId = gameId;
            packet.setParameter("gameid", gameId);
            modifiedCommunication.broadcast(packet);
            modifiedCommunication.listenWatchConfirmationAsync(this);
            parent.setWatchingEnabled(true);
        }

        public override void stopExercise(bool force)
        {
            base.stopExercise(force);
            modifiedCommunication.stopListenWatchConfirmationAsync();
            parent.setWatchingEnabled(false);
        }
    }

    class WatchSilentUDPCommunication : UDPCommunication
    {
        private Thread watchConfirmationThread = null;

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
            base.send(IPAddress.Broadcast, outPacket, UDPCommunication.IN_PORT);
        }

        private void listenWatchConfirmation()
        {
            UdpClient client = null;
            try
            {
                client = new UdpClient(IN_PORT);
                client.Client.ReceiveTimeout = 1000;
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    try
                    {
                        byte[] receivedBytes = client.Receive(ref endPoint);
                        parent.writeLog("Terima dari " + endPoint + ": " + Encoding.ASCII.GetString(receivedBytes));
                        JSONPacket inPacket = JSONPacket.createFromJSONBytes(receivedBytes);
                        if (inPacket.getParameter("type").Equals("pantau/confirm") && inPacket.getParameter("gameid").Equals(controller.gameId))
                        {
                            if (inPacket.getParameter("status").Equals("ok"))
                            {
                                String gameId = inPacket.getParameter("gameid");
                                int ammo = Int32.Parse(inPacket.getParameter("ammo"));
                                client.Close();
                                this.controller.startRegistration(gameId, ammo);
                            }
                            else
                            {
                                client.Close();
                                parent.writeLog("Pantau ditolak: " + inPacket.getParameter("status"));
                            }
                            return;
                        }
                        else
                        {
                            parent.writeLog("Paket diabaikan: " + inPacket);
                        }
                    }
                    catch (SocketException)
                    {
                        // void
                    }
                    catch (ThreadAbortException)
                    {
                        client.Close();
                        return;
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception e)
            {
                parent.writeLog("Error: " + e);
            }
        }

        public void listenWatchConfirmationAsync(WatchGameController controller)
        {
            this.controller = controller;
            watchConfirmationThread = new Thread(listenWatchConfirmation);
            watchConfirmationThread.Start();
        }

        public void stopListenWatchConfirmationAsync()
        {
            if (watchConfirmationThread != null)
            {
                watchConfirmationThread.Abort();
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
