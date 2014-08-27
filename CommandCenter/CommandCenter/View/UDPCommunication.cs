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
    class UDPCommunication
    {
        const int IN_PORT = 21500;
        const int OUT_PORT = 21501;

        MainWindow parent;
        GameController controller = null;
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
                        string receivedString = Encoding.UTF8.GetString(receivedBytes);
                        parent.writeLog("Terima dari " + endPoint + ": " + receivedString);
                        controller.handlePacket(endPoint.Address, JSONPacket.createFromJSON(receivedString));
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

        public void listenAsync(GameController controller)
        {
            this.controller = controller;
            thread = new Thread(listen);
            thread.Start();
        }

        public void send(IPAddress address, JSONPacket outPacket)
        {
            UdpClient client = new UdpClient(address + "", OUT_PORT);
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
