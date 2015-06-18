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
            NetworkStream stream = client.GetStream();
            byte[] ackBytes = new byte[4];
            byte[] ready = Encoding.ASCII.GetBytes("READY");
            int offset = 0, bytesRead = 0;
            do
            {
                bytesRead = stream.Read(ackBytes, offset, ackBytes.Length - offset);
                offset += bytesRead;
            } while (offset < 4);
            UInt32 number = BitConverter.ToUInt32(ackBytes, 0);
            
            offset = 0; bytesRead = 0;
            do
            {
                bytesRead = stream.Read(ackBytes, offset, ackBytes.Length - offset);
                offset += bytesRead;
            } while (offset < 4);
            UInt32 announseSize = BitConverter.ToUInt32(ackBytes, 0);
            
            offset = 0;
            bytesRead = 0;
            byte[] byteAnnounces = new byte[announseSize];
            do
            {
                bytesRead = stream.Read(byteAnnounces, offset, byteAnnounces.Length - offset);
                offset += bytesRead;
            } while (offset < announseSize);
            stream.Write(ready, 0, 5);
            stream.Write(BitConverter.GetBytes(number), 0, 4);
            Console.WriteLine(number);
            Console.WriteLine(Encoding.UTF8.GetString(byteAnnounces));
           client.Close();
            //ReceiveFile();
            
            
        }

        
        public static void ReceiveFile()
        {
            
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, serverUdpPort);
            UdpClient receive = new UdpClient(clientUdpPort);
            int countSize = 8192;
 
            Byte[] receiveBytes = new Byte[countSize];
            
            receiveBytes = receive.Receive(ref RemoteIpEndPoint);
            
            
           
         
            
             /*   
                
                byte[] receiveHash = receive.Receive(ref RemoteIpEndPoint);
                string receivedHash = Encoding.ASCII.GetString(receiveHash);
                Console.WriteLine(receivedHash);       
                Thread.Sleep(1000);
                string apt = "Ok";
                byte[] byteString = Encoding.ASCII.GetBytes(apt);
                IPEndPoint server = new IPEndPoint(RemoteIpEndPoint.Address, serverUdpPort);
                UdpClient udp = new UdpClient();
                udp.Send(byteString, byteString.Length, server);
                udp.Close();
                    a += M;
                    c += M;
                    receivedHash = "";
                    Console.WriteLine(a + " " + c);
     
            receive.Close();
            fs.Close(); */
        } 
    }
}
