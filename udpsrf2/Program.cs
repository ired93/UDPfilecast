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
        [Serializable]
        public class FileHeader
        {
            // упаковывает информацию о файле в байты
            private FileInfo Info;
            public UInt32 Length = 0;
            public byte[] byteData;
            
            public FileHeader(FileInfo Info)
            {
                this.Info = Info;
                DateTime lastWriteTime = Info.LastWriteTime;
                long fileSize = Info.Length;

                byte[] nameBytes = Encoding.UTF8.GetBytes(Info.Name);
                byte[] sizeBytes = BitConverter.GetBytes(fileSize);
                byte[] dateBytes = BitConverter.GetBytes(lastWriteTime.ToBinary());

                this.Length = Info.Name.Length + sizeBytes.Length + dateBytes.Length;
                this.byteData = new byte[this.Length];

                Array.Copy(nameBytes, 0, byteData, 0, nameBytes.Length);
                Array.Copy(sizeBytes, 0, byteData, nameBytes.Length, 8);
                Array.Copy(dateBytes, 0, byteData, nameBytes.Length + 8, dateBytes.Length);
           }            
        }
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

        private static const string HANDSHAKE = "Hello";
        private static const Int32 serverUdpPort = 5001;
        private static const Int32 clientUdpPort = 5002;
        private static const Int32 serverTcpPort = 5003;
        private static const Int32 clientTcpPort = 5004;
        private static const Int32 serverDataPort = 5005; 
        
        private static TcpListener server = null;
        private static List<TcpClient> clients = new List<TcpClient>();
        private static List<NetworkStream> streams = new List<NetworkStream>();
        private static FileStream fs;
        private static ServerStates ServerState;
        private static UInt32 number = 0;


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

            /*
                       UdpClient sender = new UdpClient();
                       endPoint = new IPEndPoint(IPAddress.Broadcast, serverDataPort);
                       fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                       int countSize = 1490;
                       int fileCount = (int)fs.Length / countSize;
                       Byte[] bytes = new Byte[countSize];
                       fs.Position = 0;
                       for (int i = 1; i <= fileCount; i++)
                       {
                           fs.Read(bytes, 0, bytes.Length);
                           sender.SendAsync(bytes, bytes.Length, endPoint);
                           fs.Position = i * countSize;
                       }
                       long lastCount = fs.Length - fileCount * countSize;
                       Byte[] lastBytes = new Byte[lastCount];
                       fs.Read(lastBytes, 0, lastBytes.Length);
                       sender.Send(lastBytes, lastBytes.Length, endPoint);
             * */
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
        }
    }
}
