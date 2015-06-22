using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace udpsrf2
{
    public class FileHeader
    {
        // упаковывает информацию о файле в байты
        private FileInfo Info;
        public Int32 Length = 0;
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
}
