﻿using CommandCenter.Controller;
using CommandCenter.Model.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        Thread thread = null;

        public UDPCommunication(MainWindow parent)
        {
            this.parent = parent;
        }

        private void listen()
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
                        controller.handlePacket(endPoint.Address, inPacket);
                    }
                    catch (SocketException)
                    {
                        // void
                    }
                    catch (JsonReaderException jre)
                    {
                        parent.writeLog(jre.Message);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                client.Close();
                return;
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
                parent.writeLog("Kirim ke " + address + ": " + sendString);
            }
            catch (Exception e)
            {
                parent.writeLog("Error: " + e);
            }
        }

        public void stopListenAsync()
        {
            if (thread != null)
            { 
                thread.Abort();
            }
        }
    }
}
