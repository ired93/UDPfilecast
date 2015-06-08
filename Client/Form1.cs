using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;
using System.Diagnostics;

namespace Client
{
    public partial class Form1 : Form
    {
        private static FileDetails fileDet;

        // Поля, связанные с UdpClient
        private static int localPort = 5001;
        private static UdpClient receivingUdpClient = new UdpClient(localPort);
        private static IPEndPoint RemoteIpEndPoint = null;

        private static FileStream fs;
        private static Byte[] receiveBytes = new Byte[0];
        public static Thread thUdp;
        public Form1()
        {
            InitializeComponent();
            richTextBox1.Text += "";
            thUdp = new Thread(new ThreadStart(GetFileDetails));
            thUdp.Start();
        }
        // Детали файла
        [Serializable]
        public class FileDetails
        {
            public string FILETYPE = "";
            public long FILESIZE = 0;
        }


        [STAThread]
       
 
        public void GetFileDetails()
        {

            try
            {
                richTextBox1.Text += "-----------*******Ожидание информации о файле*******-----------";

                // Получаем информацию о файле
                receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                richTextBox1.Text += "----Информация о файле получена!";

                XmlSerializer fileSerializer = new XmlSerializer(typeof(FileDetails));
                MemoryStream stream1 = new MemoryStream();

                // Считываем информацию о файле
                stream1.Write(receiveBytes, 0, receiveBytes.Length);
                stream1.Position = 0;

                // Вызываем метод Deserialize
                fileDet = (FileDetails)fileSerializer.Deserialize(stream1);
                richTextBox1.Text += "Файл типа ." + fileDet.FILETYPE +
                    " имеющий размер " + fileDet.FILESIZE.ToString() + " байт";
                ReceiveFile();
            }
            catch (Exception eR)
            {
                richTextBox1.Text += eR.Message;
                richTextBox1.Text += "\n";
            }

        }
        public static bool messageReceived = false;
        
        public void ReceiveFile()
        {
            string data = Encoding.ASCII.GetString(receiveBytes);
            int buff_size = 8192;
            int file_count = ((int)fileDet.FILESIZE / buff_size);
            
 
                try
                {
                    richTextBox1.Text += "-----------*******Ожидайте получение файла*******-----------";

                    // Получаем файл
                    //receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);

                    // Преобразуем и отображаем данные
                   // richTextBox1.Text += "----Файл получен...Сохраняем...";

                    // Создаем временный файл с полученным расширением
                    fs = new FileStream("temp." + fileDet.FILETYPE, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                    fs.Position = 0;
                    for (int i = 1; i <= file_count + 1; i++)
                    {
                        receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                        fs.Write(receiveBytes, 0, receiveBytes.Length);
                        fs.Position = buff_size * i;
                        messageReceived = true;
                    }
                        
                    
                    while (!messageReceived)
                    {
                        Thread.Sleep(10);
                    }
                        
                        
                    
                //    receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                 //   fs.Write(receiveBytes, 0, receiveBytes.Length);

                    richTextBox1.Text += "----Файл сохранен...";
                    /*
                    while (file_count > 0)
                    {
                        receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                        fs.Write(receiveBytes, 0, receiveBytes.Length);
                        fs.Position += 8192;
                        file_count--;
                    }
                     */
                   
                   
                    

                    //Console.WriteLine("-------Открытие файла------");

                    // Открываем файл связанной с ним программой
                    // Process.Start(fs.Name); 


                }
            
                catch (Exception eR)
                {
                    richTextBox1.Text += eR.Message;
                    richTextBox1.Text += "\n";
                }
                finally
                {
                    fs.Close();
                    receivingUdpClient.Close();
                }


        }

        
        private void button1_Click_1(object sender, EventArgs e)
        {
           // Process.Start("Client.exe");
            ReceiveFile();          
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Process.Start(fs.Name);
            
        }

       

        public void button3_Click_1(object sender, EventArgs e)
        {
            //Close();
            Process.GetCurrentProcess().Kill();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }
}

