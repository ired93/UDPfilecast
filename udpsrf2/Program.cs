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
        public class FileDetails
        {
            public long fileSize = 0;
            public string fileName = "";
            public DateTime lastWriteDate;
            public FileDetails(string _fileName)
            {
                fileName = _fileName;
                FileInfo info = new FileInfo(fileName);
                lastWriteDate = info.LastWriteTime;
                fileSize = info.Length;
            }
            public byte[] byteData()
            {
                
                byte[] dateBytes = BitConverter.GetBytes(lastWriteDate.ToBinary());            
                byte[] sizeBytes = BitConverter.GetBytes(fileSize);      
                byte[] stringBytes = Encoding.UTF8.GetBytes(fileName);
                int size = 16 + stringBytes.Length;
                byte[] fileInfo = new byte[size];
                Array.Copy(stringBytes, fileInfo, stringBytes.Length);
                Array.Copy(sizeBytes, 0, fileInfo, stringBytes.Length, 8);
                Array.Copy(dateBytes, 0, fileInfo, stringBytes.Length + 8, 8);
                return fileInfo;
            }
        }
        public class State
        {
            public NetworkStream stream;
            public UInt32 number;
            public ManualResetEvent manualEvent;
            public State(NetworkStream stream, UInt32 number, ManualResetEvent manualEvent)
            {
                this.stream = stream;
                this.number = number;
                this.manualEvent = manualEvent;
            }
        }
        
 
        // Поля, связанные с UdpClient
        private static IPAddress remoteIPAddress;
        private const int remotePort = 5003;
        private static IPEndPoint endPoint;
        private static Int32 ACCEPTING = 1;
        private static Int32 FILE_ANNOUNCE = 2;
        private static Int32 WAIT_ACK = 3;

        public static UdpClient udp = new UdpClient();
        private static string HANDSHAKE = "Hello";
        private static Int32 serverUdpPort = 5001;
        private static Int32 clientUdpPort = 5002;
        private static Int32 serverTcpPort = 5003;
        private static Int32 clientTcpPort = 5004;
        private static Int32 serverDataPort = 5005; 
        private static TcpListener server = null;
        private static List<TcpClient> clients;
        private static List<NetworkStream> streams;
        private static FileStream fs;
        private static int state;
        private static UInt32 number = 0;

       
        public static void ClientAccepter()
        {
            while (true)
            {
                if (state != ACCEPTING)
                    break;
                // принимает подключения, добавляя клиентов в глобальный список
                while (server.Pending() == true)
                {
                    clients.Add(server.AcceptTcpClient());
                    streams.Add(clients.Last().GetStream());
                    Console.WriteLine("Client connected from " + clients.Last().Client.RemoteEndPoint.ToString());
                    if (state == 2) break;
                }
                Thread.Sleep(100);
            }
        }
        

        static void GetClients()
        {
            // Начинаем перекличку по UDP
            Thread accepter = new Thread(new ThreadStart(ClientAccepter));
            state = ACCEPTING;
            accepter.Start(); remoteIPAddress = IPAddress.Broadcast;
            endPoint = new IPEndPoint(remoteIPAddress, clientUdpPort);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(HANDSHAKE);
            udp.Send(sendBytes, sendBytes.Length, endPoint);
            udp.Close();
        }

        
        private static void Announce(FileDetails fDet)
        {
            byte[] sizeBytes = BitConverter.GetBytes(fDet.byteData().Length);

            foreach (NetworkStream stream in streams)
            {
                stream.Write(BitConverter.GetBytes(number), 0, 4);
                stream.Write(sizeBytes, 0, sizeBytes.Length);
                stream.Write(fDet.byteData(), 0, fDet.byteData().Length);
            }
        }


        private static void GetAck(object _stateinfo)
        {
            State stateinfo = (State)_stateinfo;
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

        private static void WaitAck()
        {
            
            ManualResetEvent[] manualEvents = new ManualResetEvent[streams.Count];
            
            State stateInfo;
            int i = 0;
            foreach (NetworkStream stream in streams)
            {
                manualEvents[i] = new ManualResetEvent(false);
                stateInfo = new State(stream, number, manualEvents[i]);
                Thread newThread = new Thread(new ParameterizedThreadStart(GetAck));
                newThread.Start(stateInfo);
                i++;
            }
            WaitHandle.WaitAll(manualEvents);
        }
        
        private static void SendFile(String filename)
        {
            state = FILE_ANNOUNCE;            
            FileDetails fDet = new FileDetails(filename);
            Announce(fDet);

            state = WAIT_ACK;
            WaitAck();


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
  
        

        static void Main(string[] args)
        {

            // готовим TCP-сервер
            clients = new List<TcpClient>();
            streams = new List<NetworkStream>();
            server = new TcpListener(IPAddress.Any, serverTcpPort);
            server.Start();

            GetClients();

            Console.WriteLine("Жду клиентов");
            Thread.Sleep(2000);
            server.Stop();

            state = FILE_ANNOUNCE;
            List<String> filenames = new List<String> { "C:\\Users\\iRED\\Desktop\\Broadcast UDP\\virtus.jpg" };
            foreach (String filename in filenames)
            {
                SendFile(filename);
                number++;
            }

            foreach (TcpClient client in clients)
            {
                client.Close();
            }
        }
    }
}
