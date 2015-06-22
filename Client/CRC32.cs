using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client
{
    public class CRC32
    {
        public string Compute(byte[] crcBytes)
        {
            byte[] source = crcBytes;

            UInt32[] crc_table = new UInt32[256];
            UInt32 crc;

            for (UInt32 i = 0; i < 256; i++)
            {
                crc = i;
                for (UInt32 j = 0; j < 8; j++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;

                crc_table[i] = crc;
            };

            crc = 0xFFFFFFFF;


            foreach (byte s in source)
            {
                crc = crc_table[(crc ^ s) & 0xFF] ^ (crc >> 8);
            }

             crc ^= 0xFFFFFFFF;
             return Convert.ToString(crc);
        }
    }
}
