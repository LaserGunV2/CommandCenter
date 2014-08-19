using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.View
{
    class UDPListener
    {
        const int PORT = 12345;

        public void listen()
        {
            UdpClient client = new UdpClient(PORT);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                byte[] receivedBytes = client.Receive(ref endPoint);
                string receivedString = Encoding.ASCII.GetString(receivedBytes);
                Console.WriteLine(receivedString);
            }
        }
    }
}
