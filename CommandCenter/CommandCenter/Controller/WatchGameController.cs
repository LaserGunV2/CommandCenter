using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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
            this.parent.writeLog(LogLevel.Info, "Pura-pura kirim ke " + address + ": " + sendString);
        }

        public void broadcast(JSONPacket outPacket)
        {
            //Get IP, subnet, and broadcast all network
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST
            string[] myIP = new string[Dns.GetHostByName(hostName).AddressList.Length];
            IPAddress[] ipBroadcast = new IPAddress[Dns.GetHostByName(hostName).AddressList.Length];
            for (int c = 0; c < myIP.Length; c++) // Broadcast to all 
            {
                myIP[c] = Dns.GetHostByName(hostName).AddressList[c].ToString();

                IPAddress address = IPAddress.Parse(myIP[c]);
                var card = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();
                string sMask = card.GetIPProperties().UnicastAddresses.LastOrDefault().IPv4Mask.ToString(); //Retrive subnet mask IPv4
                IPAddress subnetMask = IPAddress.Parse(sMask);
                byte[] ipAdressBytes = address.GetAddressBytes();
                byte[] subnetMaskBytes = subnetMask.GetAddressBytes();
                byte[] broadcastAddress = new byte[ipAdressBytes.Length];
                for (int i = 0; i < broadcastAddress.Length; i++)
                {
                    broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255)); // OR ip address dengan subnet
                }
                String ipBroadcastStr = "";
                for (int i = 0; i < broadcastAddress.Length; i++) //for print only
                {
                    ipBroadcastStr += broadcastAddress[i] + ".";
                }
                ipBroadcast[c] = IPAddress.Parse(ipBroadcastStr.Substring(0, ipBroadcastStr.Length - 1));

                base.send(ipBroadcast[c], outPacket, UDPCommunication.IN_PORT);
                parent.writeLog(LogLevel.Info, "Broadcast ke " + ipBroadcast[c].ToString());
            }
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
                        parent.writeLog(LogLevel.Info, "Terima dari " + endPoint + ": " + Encoding.ASCII.GetString(receivedBytes));
                        JSONPacket inPacket = JSONPacket.createFromJSONBytes(receivedBytes);
                        if (inPacket.getParameter("type").Equals("pantau/confirm"))
                        {
                            if (inPacket.getParameter("status").Equals("ok") && inPacket.getParameter("gameid").Equals(controller.gameId))
                            {
                                String gameId = inPacket.getParameter("gameid");
                                int ammo = Int32.Parse(inPacket.getParameter("ammo"));
                                client.Close();
                                this.controller.startRegistration(gameId, ammo);
                            }
                            else
                            {
                                client.Close();
                                parent.showError("Pantau ditolak: " + inPacket.getParameter("status"));
                                parent.setWatchingEnabled(false);
                            }
                            return;
                        }
                        else
                        {
                            parent.writeLog(LogLevel.Warn, "Paket diabaikan karena belum memantau: " + inPacket);
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
                parent.writeLog(LogLevel.Error, "Error: " + e);
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
