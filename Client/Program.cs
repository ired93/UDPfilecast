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
        private static Int32 clientDataPort = 5006;
        private const int packetSize = 1400;
        private static UdpClient receive = new UdpClient(clientDataPort);
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
            ReceiveCrc(client);
            Thread.Sleep(10);
            //SendAck(client);
            client.Close();
   
        }
        private static void SendAck(TcpClient client)
        {
            int number = 0;
            byte[] ready = Encoding.ASCII.GetBytes("READY");
            NetworkStream stream = client.GetStream();
            stream.Write(ready, 0, 5);
            stream.Write(BitConverter.GetBytes(number), 0, 4);
            ReceiveCrc(client);
        }
        private static void ReceiveCrc(TcpClient client)
        {
            byte[] receiveBytes = new byte[11];
            byte[] intBytes = new byte[1];
            byte[] stringBytes = new byte[10];
            NetworkStream stream = client.GetStream();
            int bytesRead = 0;
            int offset  = 0;
            do
            {
                bytesRead = stream.Read(receiveBytes, offset, receiveBytes.Length-offset);
                offset += bytesRead;
            } while (offset < 11);
            Array.Copy(receiveBytes, intBytes, 1);
            Array.Copy(receiveBytes, 1, stringBytes, 0, receiveBytes.Length - 1);
            string m = Encoding.ASCII.GetString(intBytes);
            string crc = Encoding.ASCII.GetString(stringBytes);
            long M = Convert.ToInt64(m);
            Console.WriteLine(M);
            Console.WriteLine(crc + "\n");
            ReceiveFile(M, crc, client);
            
        }
        public static void ReceiveFile(long _M, string _crc, TcpClient client)
        {
            long M = _M;
            string crc = _crc;
            int packetCount = Convert.ToInt16(Math.Ceiling((double)53875 / packetSize));
            long lastPacket = 0;
            while (lastPacket < packetCount)
            {
                M = 6;
                if (lastPacket + M > packetCount)
                    M = packetCount - lastPacket;
                ReceivePortion(lastPacket, M, crc, client);
                lastPacket += M;
            }
        }
        public static void ReceivePortion(long lastPacket, long M, string _crc, TcpClient client)
        {
            CRC32 crc32 = new CRC32();
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, serverUdpPort);
            FileStream fs = new FileStream("virtus.jpg", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            string crc = _crc;
            byte[] receiveBytes = new byte[packetSize];
            byte[] crcBytes = new byte[M * packetSize];
            long startByte = lastPacket * packetSize;
            int offset = 0;
            for (int i = 1; i <= M; i++)
            {
                receiveBytes = receive.Receive(ref RemoteIpEndPoint);
                Array.Copy(receiveBytes, 0, crcBytes, offset, receiveBytes.Length);
                offset += receiveBytes.Length;
            }
            string crcKey = crc32.Compute(crcBytes);
            Console.WriteLine(crcKey);
            fs.Position = startByte;
            fs.Write(crcBytes, 0, crcBytes.Length);
            SendAck(client);
            
            
        }
    }
}
