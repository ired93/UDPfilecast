using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Timers;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Security.Cryptography;

namespace udpsrf2
{
    class Program
    {
        [Serializable]
        public class FileDetails
        {
            public string FILETYPE = "";
            public long FILESIZE = 0;
        }
        private static FileDetails fileDet = new FileDetails();
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
        private static FileStream fs;

        public static void ClientAccepter()
        {
            // принимает подключения, добавляя клиентов в глобальный список
            while (true)
            {
                clients.Add(server.AcceptTcpClient());
                Console.WriteLine("Client connected from " + clients.Last().Client.RemoteEndPoint.ToString());
            }
        }
        

        static void GetClients()
        {
            // готовим TCP-сервер
            server = new TcpListener(IPAddress.Any, serverTcpPort);
            server.Start();
            clients = new List<TcpClient>();
            Thread accepter = new Thread(new ThreadStart(ClientAccepter));
            accepter.Start();  
            

            // Начинаем перекличку по UDP
            remoteIPAddress = IPAddress.Broadcast;
            endPoint = new IPEndPoint(remoteIPAddress, clientUdpPort);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(HANDSHAKE);
            udp.Send(sendBytes, sendBytes.Length, endPoint);
            udp.Close();
            Console.WriteLine("Жду клиентов");
            Thread.Sleep(2000);
            accepter.Interrupt();
            
            
        }
        
        private static void SendFile()
        {
            UdpClient sender = new UdpClient();
            UdpClient sendinfo = new UdpClient();
            MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
            endPoint = new IPEndPoint(IPAddress.Broadcast, clientUdpPort);
            
            Console.WriteLine("Введите путь к файлу и его имя");
            fs = new FileStream(@Console.ReadLine().ToString(), FileMode.Open, FileAccess.Read);
            int countSize = 8192;
            int fileCount = (int)fs.Length / countSize;
            int M = fileCount / 2;
            Byte[] bytes = new Byte[countSize];
            string hash = string.Empty;

            Console.WriteLine("Отправка " + M + " кусков");
            Byte[] sendBytes = Encoding.ASCII.GetBytes(Convert.ToString(M));
            sendinfo.Send(sendBytes, sendBytes.Length, endPoint);
            sendinfo.Close();

            Thread.Sleep(1000);

            fs.Position = 0;
            for (int i = 1; i <= M; i++)
            {
                fs.Read(bytes, 0, bytes.Length);
                sender.SendAsync(bytes, bytes.Length, endPoint);
                byte[] byteHash = csp.ComputeHash(bytes);
                foreach (byte b in byteHash)
                {
                    hash += string.Format("{0:x2}", b);
                }
                
                fs.Position = i * countSize;
            }
            Console.WriteLine(hash);
            Byte[] hashBytes = Encoding.ASCII.GetBytes(hash);
            sender.Send(hashBytes, hashBytes.Length, endPoint);
            sender.Close();
            fs.Close();
            
          //  announce(filename);
        }
        

        static void Main(string[] args)
        {
            GetClients();

            SendFile();

        }
    }
}
