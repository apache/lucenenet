using System;

namespace Lucene.Net.Support
{
    public class CRC32 : IChecksum
    {
        private static readonly UInt32[] crcTable = InitializeCRCTable();

        private static UInt32[] InitializeCRCTable()
        {
            UInt32[] crcTable = new UInt32[256];
            for (UInt32 n = 0; n < 256; n++)
            {
                UInt32 c = n;
                for (int k = 8; --k >= 0; )
                {
                    if ((c & 1) != 0)
                        c = 0xedb88320 ^ (c >> 1);
                    else
                        c = c >> 1;
                }
                crcTable[n] = c;
            }
            return crcTable;
        }

        private UInt32 crc = 0;

        public long Value
        {
            get
            {
                return crc & 0xffffffffL;
            }
        }

        public Int64 GetValue()
        {
            return Value;
        }

        public void Reset()
        {
            crc = 0;
        }

        public void Update(int bval)
        {
            UInt32 c = ~crc;
            c = crcTable[(c ^ bval) & 0xff] ^ (c >> 8);
            crc = ~c;
        }

        public void Update(byte[] buf, int off, int len)
        {
            UInt32 c = ~crc;
            while (--len >= 0)
                c = crcTable[(c ^ buf[off++]) & 0xff] ^ (c >> 8);
            crc = ~c;
        }

        public void Update(byte[] buf)
        {
            Update(buf, 0, buf.Length);
        }
    }
}