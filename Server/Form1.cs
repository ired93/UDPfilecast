using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Threading;



namespace server_
{

    public partial class Form1 : Form
    {
        // Информация о файле (требуется для получателя)
        [Serializable]
        public class FileDetails
        {
            public string FILETYPE = "";
            public long FILESIZE = 0;
        }
        private static FileDetails fDet = new FileDetails();

        // Поля, связанные с UdpClient
        private static IPAddress remoteIPAddress;
        private const int remotePort = 5001;
        private static UdpClient sender = new UdpClient();
        private static IPEndPoint endPoint;

        //filestream object
        private static FileStream fs;
        public string file = "";
        public int size = 0;
        public Form1()
        {
            InitializeComponent();
            maskedTextBox1.Mask = "255.255.255.255";
            label3.Text = remotePort.ToString();
            richTextBox1.Text += "";
            richTextBox3.Text += "";
        }
        [STAThread]
        public void Maina()
        {
            try
            {
                // Получаем удаленный IP-адрес и создаем IPEndPoint
                remoteIPAddress = IPAddress.Parse(maskedTextBox1.Mask);
                endPoint = new IPEndPoint(remoteIPAddress, remotePort);
                fs = new FileStream(@openFileDialog1.FileName, FileMode.Open, FileAccess.ReadWrite);
                SendFileInfo();
                richTextBox4.Text += "Информация отправлена успешно\n";
                richTextBox4.Text += "Теперь можете оправить файл...\n";

            }
            catch (Exception e)
            {
                richTextBox3.Text += e.Message;
                richTextBox3.Text += "\n";
            }
        }
        public void MenuFileOpen()
        {

            if (openFileDialog1.ShowDialog() ==
               System.Windows.Forms.DialogResult.OK &&
               openFileDialog1.FileName.Length > 0)
            {
                richTextBox1.Text += openFileDialog1.FileName;
                file = openFileDialog1.FileName;
            }
        }
        public void SendFileInfo()
        {
            // Получаем тип и расширение файла
            fDet.FILETYPE = openFileDialog1.FileName.Substring((int)openFileDialog1.FileName.Length - 3, 3);
            // Получаем длину файла
            //fDet.FILESIZE = size;
            fDet.FILESIZE = fs.Length;
            XmlSerializer fileSerializer = new XmlSerializer(typeof(FileDetails));
            MemoryStream stream = new MemoryStream();

            // Сериализуем объект
            fileSerializer.Serialize(stream, fDet);

            // Считываем поток в байты
            stream.Position = 0;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, Convert.ToInt32(stream.Length));

            // Отправляем информацию о файле
            richTextBox4.Text += "Отправка сведений о файле...\n";
            sender.Send(bytes, bytes.Length, endPoint);
            stream.Close();
        }
        public static bool messageSent = false;
      
        private void SendFile()
        {
            // Создаем файловый поток и переводим его в байты
        
        
        
        richTextBox4.Text += "Отправка файла размером " + fs.Length + " байт";

                try
            {
                int buff_size = 8192;
                Byte[] bytes = new Byte[buff_size];
                int file_count = (int)fDet.FILESIZE / buff_size;
                int a = (int)fDet.FILESIZE / buff_size;
                int rest = (int)fDet.FILESIZE - file_count * buff_size;
                Byte[] bytes2 = new Byte[rest];
                fs.Position = 0;
                for (int i = 1; i <= file_count; i++)
                {
                    fs.Read(bytes, 0, bytes.Length);
                    sender.Send(bytes, bytes.Length, endPoint);
                    Thread.Sleep(10);
                    fs.Position = buff_size * i;       
                }
                fs.Read(bytes2, 0, bytes2.Length);
                sender.Send(bytes2, bytes2.Length, endPoint);
                
             /*   int rest = (int)fDet.FILESIZE - file_count * buff_size;
                Byte[] bytes2 = new Byte[rest];
                fs.Read(bytes2, 0, rest);
                sender.Send(bytes2, bytes2.Length, endPoint);
                */
                
             /*   while (file_count > 0)
                {                  
                    fs.Read(bytes, 0, bytes.Length);
                    sender.Send(bytes, bytes.Length, endPoint);
                    Thread.Sleep(1000);
                    fs.Position += buff_size;
                    file_count--;
                    
                    
                }
               /* int a = (int)fDet.FILESIZE - file_count * 8192;
                Byte[] bytes2 = new Byte[a];
                fs.Read(bytes2, 0, a);
                sender.Send(bytes2, bytes2.Length, endPoint);
                fs.Flush();
                    */
                    //sender.Send(bytes, numBytesToRead, endPoint);
                    
                    //Отправляем файл

                    //sender.Send(bytes, bytes.Length, endPoint);
                

            }
            catch (Exception e)
            {
                richTextBox4.Text += e.Message;
            }
            finally
            {
                // Закрываем соединение и очищаем поток
                fs.Close();
                sender.Close();
            }
            richTextBox4.Text += "\n";
            richTextBox4.Text += "Файл успешно отправлен.";
            
        
        }
        private void button1_Click(object sender, EventArgs e)
        {
            MenuFileOpen();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SendFile();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Maina();
        }


        private void richTextBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {
            
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox3_TextChanged(object sender, EventArgs e)
        {

        }
    }
    
}
