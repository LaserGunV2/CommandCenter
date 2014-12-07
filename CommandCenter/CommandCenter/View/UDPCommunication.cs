using CommandCenter.Controller;
using CommandCenter.Model.Protocol;
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

namespace CommandCenter.View
{
    public class UDPCommunication
    {
        public const int IN_PORT = 21500;
        public const int OUT_PORT = 21501;

        protected MainWindow parent;
        protected AbstractGameController controller = null;
        private bool softAbort;
        Thread thread = null;

        protected String ipBroadcast;

        public UDPCommunication(MainWindow parent)
        {
            this.parent = parent;
            var card = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();
            if (card != null)
            {
                var addss = card.GetIPProperties().GatewayAddresses.FirstOrDefault();
                String address = addss.Address + "";
                // make broadcast address 
                while (!address[address.Length - 1].Equals('.'))
                {
                    address = address.Remove(address.Length - 1); //Remove last digit gateway address 
                }
                ipBroadcast = address + "255";//add 255 broadcast
            }
        }

        private void listen()
        {
            UdpClient client = null;
            try
            {
                client = new UdpClient(IN_PORT);
                client.Client.ReceiveTimeout = 1000;
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                softAbort = false;
                while (!softAbort)
                {
                    try
                    {
                        byte[] receivedBytes = client.Receive(ref endPoint);
                        parent.writeLog(LogLevel.Info, "Terima dari " + endPoint + ": " + Encoding.ASCII.GetString(receivedBytes));
                        JSONPacket inPacket = JSONPacket.createFromJSONBytes(receivedBytes);
                        controller.handlePacket(endPoint.Address, inPacket);
                    }
                    catch (SocketException)
                    {
                        // void
                    }
                    catch (JsonReaderException jre)
                    {
                        parent.writeLog(LogLevel.Error, "Error: " + jre);
                    }
                }
                client.Close();
                parent.writeLog(LogLevel.Info, "Communcation soft-closed");
            }
            catch (ThreadAbortException)
            {
                client.Close();
                parent.writeLog(LogLevel.Info, "Communcation hard-closed");

                return;
            }
            catch (Exception e)
            {
                parent.writeLog(LogLevel.Error, "Error: " + e);
            }
        }

        public virtual void listenAsync(AbstractGameController controller)
        {
            this.controller = controller;
            thread = new Thread(listen);
            thread.Start();
        }

        public virtual void send(IPAddress address, JSONPacket outPacket)
        {
            send(address, outPacket, OUT_PORT);
        }

        public void send(IPAddress address, JSONPacket outPacket, int port)
        {
            UdpClient client = new UdpClient(address + "", port);
            string sendString = outPacket.ToString();
            Byte[] sendBytes = Encoding.UTF8.GetBytes(sendString);
            try
            {
                client.Send(sendBytes, sendBytes.Length);
                parent.writeLog(LogLevel.Info, "Kirim ke " + address + ": " + sendString);
            }
            catch (Exception e)
            {
                parent.writeLog(LogLevel.Error, "Error: " + e);
            }
        }

        public void stopListenAsync(bool force)
        {
            if (force)
            {
                if (thread != null)
                {
                    thread.Abort();
                }
            }
            else
            {
                softAbort = true;
            }
        }
    }
}
