using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public abstract class DataOutput
    {
        public abstract void WriteByte(byte b);

        public void WriteBytes(sbyte[] b, int length)
        {
            // helper method to account for java's byte being signed
            WriteBytes(b, 0, length);
        }

        public void WriteBytes(sbyte[] b, int offset, int length)
        {
            // helper method to account for java's byte being signed
            byte[] ubytes = new byte[b.Length];
            Support.Buffer.BlockCopy(b, 0, ubytes, 0, b.Length);

            WriteBytes(ubytes, offset, length);
        }

        public virtual void WriteBytes(byte[] b, int length)
        {
            WriteBytes(b, 0, length);
        }

        public abstract void WriteBytes(byte[] b, int offset, int length);

        public void WriteByte(sbyte b)
        {
            // helper method to account for java's byte being signed
            WriteByte((sbyte)b);
        }

        public virtual void WriteInt(int i)
        {
            WriteByte((byte)(i >> 24));
            WriteByte((byte)(i >> 16));
            WriteByte((byte)(i >> 8));
            WriteByte((byte)i);
        }

        public virtual void WriteShort(short i)
        {
            WriteByte((byte)(i >> 8));
            WriteByte((byte)i);
        }

        public void WriteVInt(int i)
        {
            while ((i & ~0x7F) != 0)
            {
                WriteByte((byte)((i & 0x7F) | 0x80));
                i = Number.URShift(i, 7);
            }
            WriteByte((byte)i);
        }

        public virtual void WriteLong(long i)
        {
            WriteInt((int)(i >> 32));
            WriteInt((int)i);
        }

        public void WriteVLong(long i)
        {
            //assert i >= 0L;
            while ((i & ~0x7FL) != 0L)
            {
                WriteByte((byte)((i & 0x7FL) | 0x80L));
                i = Number.URShift(i, 7);
            }
            WriteByte((byte)i);
        }

        public virtual void WriteString(string s)
        {
            BytesRef utf8Result = new BytesRef(10);
            UnicodeUtil.UTF16toUTF8(s.ToCharArray(), 0, s.Length, utf8Result);
            WriteVInt(utf8Result.length);
            WriteBytes(utf8Result.bytes, 0, utf8Result.length);
        }

        private static int COPY_BUFFER_SIZE = 16384;
        private sbyte[] copyBuffer;

        public virtual void CopyBytes(DataInput input, long numBytes)
        {
            //assert numBytes >= 0: "numBytes=" + numBytes;
            long left = numBytes;
            if (copyBuffer == null)
                copyBuffer = new sbyte[COPY_BUFFER_SIZE];
            while (left > 0)
            {
                int toCopy;
                if (left > COPY_BUFFER_SIZE)
                    toCopy = COPY_BUFFER_SIZE;
                else
                    toCopy = (int)left;
                input.ReadBytes(copyBuffer, 0, toCopy);
                WriteBytes(copyBuffer, 0, toCopy);
                left -= toCopy;
            }
        }

        public virtual void WriteStringStringMap(IDictionary<string, string> map)
        {
            if (map == null)
            {
                WriteInt(0);
            }
            else
            {
                WriteInt(map.Count);
                foreach (KeyValuePair<String, String> entry in map)
                {
                    WriteString(entry.Key);
                    WriteString(entry.Value);
                }
            }
        }

        public virtual void WriteStringSet(ISet<string> set)
        {
            if (set == null)
            {
                WriteInt(0);
            }
            else
            {
                WriteInt(set.Count);
                foreach (String value in set)
                {
                    WriteString(value);
                }
            }
        }
    }
}
