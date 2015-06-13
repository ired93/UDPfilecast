using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

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
            ReceiveFile();
            
        }
        public static void Accept(IPEndPoint rmt)
        {
            IPEndPoint server = new IPEndPoint(rmt.Address, serverTcpPort);
            TcpClient client = new TcpClient();
            string apt = "Ok";
            client.Connect(server);
            byte[] byteString = Encoding.ASCII.GetBytes(apt);
            
            client.Client.SendTo(byteString, server);
            client.Close();
        }
        public static void ReceiveFile()
        {
            int countSize = 8192;
            UdpClient receivinfo = new UdpClient(clientUdpPort);
            MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, serverUdpPort);
            byte[] byteCount = receivinfo.Receive(ref RemoteIpEndPoint);
            string str = Encoding.ASCII.GetString(byteCount);
            int M = Convert.ToInt32(str);      
            receivinfo.Close();
            Thread.Sleep(1000);
            FileStream fs;
            string hash = string.Empty;
            UdpClient receive = new UdpClient(clientUdpPort);   
            fs = new FileStream("temp.jpg", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs.Position = 0;
            for (int i = 1; i <= M; i++)
            {
                byte[] receiveBytes = receive.Receive(ref RemoteIpEndPoint);
                fs.Write(receiveBytes, 0, receiveBytes.Length);
                
                byte[] byteHash = csp.ComputeHash(receiveBytes);
                foreach (byte b in byteHash)
                    hash += string.Format("{0:x2}", b);
                fs.Position = i * countSize;
            }
            Console.WriteLine(hash);
            Console.WriteLine("Данные получены ");
            byte[] receiveHash = receive.Receive(ref RemoteIpEndPoint);
            string receivedHash = Encoding.ASCII.GetString(receiveHash);
            Console.WriteLine(receivedHash);
            IPEndPoint rmt = RemoteIpEndPoint;
            if (receivedHash == hash)
                Accept(rmt);
            receive.Close();
            fs.Close();
        }
    }
}
