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
using System.Runtime.Serialization.Formatters.Binary;

namespace udpsrf2
{
    class Program
    {
 
        public class ackReceiverState
        {   
            public NetworkStream stream;
            public UInt32 number;
            public ManualResetEvent manualEvent;
            public ackReceiverState(NetworkStream stream, UInt32 number, ManualResetEvent manualEvent)
            {
                this.stream = stream;
                this.number = number;
                this.manualEvent = manualEvent;
            }
        }
        
        enum ServerStates {ACCEPTING, FILE_ANNOUNCE, WAIT_ACK};

        private const string HANDSHAKE = "Hello";
        private const Int32 serverUdpPort = 5001;
        private const Int32 clientUdpPort = 5002;
        private const Int32 serverTcpPort = 5003;
        private const Int32 clientTcpPort = 5004;
        private const Int32 serverDataPort = 5005;
        private const Int32 clientDataPort = 5006;
        private const int packetSize = 1400;
        private static TcpListener server = null;
        private static List<TcpClient> clients = new List<TcpClient>();
        private static List<NetworkStream> streams = new List<NetworkStream>();
        private static FileStream fs;
        private static ServerStates ServerState;
        private static UInt32 number = 0;
        private static UdpClient dataclient;

        static void Main(string[] args)
        {
            // Запускаем в отдельном потоке сбор подключений от клиентов
            GetClients();

            // Формируем список файлов для вещания, пока заглушка
            List<String> filenames = new List<String> { "C:\\Users\\iRED\\Desktop\\Broadcast UDP\\virtus.jpg" };
            
            // Раздаем файлы
            foreach (String filename in filenames)
            {
                ServerState = ServerStates.FILE_ANNOUNCE;        
                SendFile(filename);
                number++;
            }

            CloseClients();
        }

        

        private static void GetClients()
        {
            // TCP-сервер для подключений от клиентов
            server = new TcpListener(IPAddress.Any, serverTcpPort);
            server.Start();
            Thread accepter = new Thread(new ThreadStart(ClientAccepter));
            ServerState = ServerStates.ACCEPTING;
            accepter.Start();

            // Отправляем широковещательное сообщение, 
            // получив которое, клиенты должны подключиться к серверу
            UdpClient udp = new UdpClient();
            IPEndPoint everyone = new IPEndPoint(IPAddress.Broadcast, clientUdpPort);
            Byte[] handshake = Encoding.ASCII.GetBytes(HANDSHAKE);
            udp.Send(handshake, handshake.Length, everyone);
            udp.Close();

            Console.WriteLine("Жду клиентов");
            Thread.Sleep(2000);
            server.Stop();
        }
        

        public static void ClientAccepter()
        {
            while (true)
            {
                if (ServerState != ServerStates.ACCEPTING) break;
                
                // принимает подключения, добавляя клиентов в глобальный список
                while (server.Pending() == true)
                {
                    clients.Add(server.AcceptTcpClient());
                    streams.Add(clients.Last().GetStream());
                    Console.WriteLine("Client connected from " + clients.Last().Client.RemoteEndPoint.ToString());
                    if (ServerState != ServerStates.ACCEPTING) break;
                }
                Thread.Sleep(50);
            }
        }


        private static void CloseClients()
        {
            foreach (TcpClient client in clients)
            {
                client.Close();
            }
        }

       
        private static void SendFile(String filename)
        {
            ServerState = ServerStates.FILE_ANNOUNCE;
            FileInfo fDet = new FileInfo(filename);
            
            AnnounceFile(fDet);
            ServerState = ServerStates.WAIT_ACK;
            WaitAnnounceAck();


            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, clientDataPort);
            
            long M = 6;
            fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);
            int packetCount = Convert.ToInt16(Math.Ceiling((double)fs.Length / packetSize));
            long lastPacket = 0;
            while (lastPacket < packetCount)
            {
                M = 6;
                if (lastPacket + M > packetCount)
                    M = packetCount - lastPacket;
                SendPortion(lastPacket, M);
                lastPacket += M;
            }
        }

        private static void SendPortion(long lastPacket, long M)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, clientDataPort);
            CRC32 crc = new CRC32();
            dataclient = new UdpClient();
            long startByte = lastPacket * packetSize;
            byte[] sendBytes = new byte[packetSize];
            byte[] crcBytes = new byte[M * packetSize];
            int offset = 0;
            fs.Position = startByte;
            for (int i = 1; i <= M; i++)
            {
                fs.Read(sendBytes, 0, sendBytes.Length);
                Array.Copy(sendBytes, 0, crcBytes, offset, sendBytes.Length);
                offset += sendBytes.Length;
            }
            string crcKey = crc.Compute(crcBytes);
            SendCrc(crcKey, M);
            Thread.Sleep(10);
            fs.Position = startByte;
            for (int i = 1; i <= M; i++)
            {
                fs.Read(sendBytes, 0, sendBytes.Length);
                dataclient.Send(sendBytes, sendBytes.Length, endPoint);
            }
            WaitSendAck();
        }
        private static void WaitSendAck()
        {
            ackReceiverState stateInfo;
            ManualResetEvent[] ackReceived = new ManualResetEvent[streams.Count];

            int i = 0;
            foreach (NetworkStream stream in streams)
            {
                ackReceived[i] = new ManualResetEvent(false);
                stateInfo = new ackReceiverState(stream, number, ackReceived[i]);
                Thread receiverThread = new Thread(new ParameterizedThreadStart(GetSendAck));
                receiverThread.Start(stateInfo);
                i++;
            }
            WaitHandle.WaitAll(ackReceived);
        }
        private static void GetSendAck(object _stateinfo)
        {
            ackReceiverState stateinfo = (ackReceiverState)_stateinfo;
            NetworkStream stream = stateinfo.stream;
            byte[] ackBytes = new byte[9];
            int offset = 0, bytesRead = 0;
            do
            {
                bytesRead = stream.Read(ackBytes, offset, ackBytes.Length - offset);
                offset += bytesRead;
            } while (offset < 9);
            if (BitConverter.ToUInt32(ackBytes, 5) == number)
                stateinfo.manualEvent.Set();
            
        }
        private static void SendCrc(string crcKey, long M)
        {
            byte[] receiveBytes = new byte[11];
            byte[] longBytes = Encoding.ASCII.GetBytes(Convert.ToString(M));
            byte[] stringBytes = Encoding.ASCII.GetBytes(crcKey);
            Array.Copy(longBytes, 0, receiveBytes, 0, longBytes.Length);
            Array.Copy(stringBytes, 0, receiveBytes, longBytes.Length, stringBytes.Length);
            Console.WriteLine(Encoding.ASCII.GetString(receiveBytes));
            foreach (NetworkStream stream in streams)
            {
                stream.Write(receiveBytes, 0, receiveBytes.Length);
            }
        }
        
        private static void AnnounceFile(FileInfo fDet)
        {
            FileHeader header = new FileHeader(fDet);

            foreach (NetworkStream stream in streams)
            {
                stream.Write(BitConverter.GetBytes(number), 0, 4);
                stream.Write(BitConverter.GetBytes(header.Length), 0, 4);
                stream.Write(header.byteData, 0, header.byteData.Length);
            }
        }

        
        // запускает потоковые функции для получения подтверждений объявления
        // о раздаче файлов и ожидает их завершения
        private static void WaitAnnounceAck()
        {
            ackReceiverState stateInfo;
            ManualResetEvent[] ackReceived = new ManualResetEvent[streams.Count];
            
            int i = 0;
            foreach (NetworkStream stream in streams)
            {
                ackReceived[i] = new ManualResetEvent(false);
                stateInfo = new ackReceiverState(stream, number, ackReceived[i]);
                Thread receiverThread = new Thread(new ParameterizedThreadStart(GetAnnounceAck));
                receiverThread.Start(stateInfo);
                i++;
            }
            WaitHandle.WaitAll(ackReceived);
        }


        // Запускается в отдельном потоке для каждого клиента и сигнализирует 
        // в главный поток получение сообщения о готовности клиента к получению файла.
        // Сообщение должно иметь вид READYxxxx где xxxx - 32-бит номер файла
        private static void GetAnnounceAck(object _stateinfo)
        {
            ackReceiverState stateinfo = (ackReceiverState)_stateinfo;
            NetworkStream stream = stateinfo.stream;
            byte[] ackBytes = new byte[9];
            int offset = 0, bytesRead = 0;
            do
            {
                bytesRead = stream.Read(ackBytes, offset, ackBytes.Length-offset);
                offset += bytesRead;
            } while (offset < 9);
            if (BitConverter.ToUInt32(ackBytes, 5) == number)
                stateinfo.manualEvent.Set();
            Console.WriteLine(number);
        }
    }
}
