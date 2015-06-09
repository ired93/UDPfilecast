using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Timers;
using System.Threading;

namespace udpsrf2
{
    class Program
    {
        // Поля, связанные с UdpClient
        private static IPAddress remoteIPAddress;
        private const int remotePort = 5003;
        private static IPEndPoint endPoint;
        
        public static UdpClient udp = new UdpClient();
        private static string HANDSHAKE = "Hello";
        private static Int32 serverUdpPort = 5001;
        private static Int32 clientUdpPort = 5002;
        private static Int32 serverTcpPort = 5003;
        private static Int32 clientTcpPort = 5004;
        private static TcpListener server = null;
        private static List<TcpClient> clients;

        public static void AddClients()
        {
            while (true)
            {
                clients.Add(server.AcceptTcpClient());
                Console.WriteLine("Connected " + clients.Last().Client.RemoteEndPoint.ToString());
            }
        }
        

        static void GetClients()
        {
            

            
            // Ждем подключений клиентов по TCP
            clients = new List<TcpClient>();
            server = new TcpListener(IPAddress.Any, serverTcpPort);
            server.Start();
            Thread accepter = new Thread(new ThreadStart(AddClients));
            accepter.Start();

            // Начинаем перекличку по UDP
            remoteIPAddress = IPAddress.Broadcast;
            endPoint = new IPEndPoint(remoteIPAddress, clientUdpPort);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(HANDSHAKE);
            udp.Send(sendBytes, sendBytes.Length, endPoint);
            udp.Close();
            Console.WriteLine("Жду клиентов");
            Thread.Sleep(2000);


        }

        static void SendFile(string filename)
        {
          //  announce(filename);
        }

        static void Main(string[] args)
        {
            GetClients();
        }
    }
}
