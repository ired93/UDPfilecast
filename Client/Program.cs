using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


namespace client
{
    class Program
    {
        private static string HANDSHAKE = "Hello";
        private static Int32 serverUdpPort = 5001;
        private static Int32 clientUdpPort = 5002;
        private static Int32 serverTcpPort = 5003;
     
   
        static void Main(string[] args)
        {
            UdpClient receivingUdpClient = new UdpClient(clientUdpPort);
            
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, serverUdpPort);
            string stringData;
            do
            {
                byte[] byteData = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                stringData = Encoding.ASCII.GetString(byteData);
            } while (stringData != HANDSHAKE);
            
            Console.WriteLine(stringData);
            Console.WriteLine(RemoteIpEndPoint.Address.ToString());
            receivingUdpClient.Close();
            
            IPEndPoint server = new IPEndPoint(RemoteIpEndPoint.Address, serverTcpPort);
            TcpClient client = new TcpClient();
            client.Connect(server);
            client.Close();
            
        }
    }
}
