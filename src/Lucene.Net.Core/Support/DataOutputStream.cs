using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Java's DataOutputStream is similar to .NET's BinaryWriter. However, it writes
    /// in a modified UTF-8 format that cannot be read (or duplicated) using BinaryWriter.
    /// This is a port of DataOutputStream that is fully compatible with Java's DataInputStream.
    /// <para>
    /// Usage Note: Always favor BinaryWriter over DataOutputStream unless you specifically need
    /// the modified UTF-8 format and/or the <see cref="WriteUTF(IDataOutput)"/> method.
    /// </para>
    /// </summary>
    public class DataOutputStream : IDataOutput, IDisposable
    {
        
        /// <summary>
        /// The number of bytes written to the data output stream so far.
        /// If this counter overflows, it will be wrapped to <see cref="int.MaxValue"/>.
        /// </summary>
        protected int written;

        /// <summary>
        /// bytearr is initialized on demand by writeUTF
        /// </summary>
        private byte[] bytearr = null;


        private readonly Stream @out;

        /// <summary>
        /// Creates a new data output stream to write data to the specified
        /// underlying output stream. The counter <code>written</code> is
        /// set to zero.
        /// </summary>
        /// <param name="out">the underlying output stream, to be saved for later use.</param>
        public DataOutputStream(Stream @out)
        {
            this.@out = @out;
        }

        /// <summary>
        /// Increases the written counter by the specified value
        /// until it reaches <see cref="int.MaxValue"/>.
        /// </summary>
        private void IncCount(int value)
        {
            int temp = written + value;
            if (temp < 0)
            {
                temp = int.MaxValue;
            }
            written = temp;
        }

        /// <summary>
        /// Writes the specified byte (the low eight bits of the argument
        /// <code>b</code>) to the underlying output stream.If no exception
        /// is thrown, the counter<code>written</code> is incremented by
        /// <code>1</code>.
        /// </summary>
        /// <param name="b">the <code>byte</code> to be written.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void Write(int b) 
        {
            @out.WriteByte((byte)b);
            IncCount(1);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void Write(byte[] b, int off, int len)
        {
            @out.Write(b, off, len);
            IncCount(len);
        }

        public virtual void Flush() 
        {
            @out.Flush();
        }

        public void WriteBoolean(bool v)
        {
            @out.WriteByte((byte)(v ? 1 : 0));
            IncCount(1);
        }

        public void WriteByte(int v)
        {
            @out.WriteByte((byte)v);
            IncCount(1);
        }

        public void WriteShort(int v)
        {
            @out.WriteByte((byte)((int)((uint)v >> 8) & 0xFF));
            @out.WriteByte((byte)((int)((uint)v >> 0) & 0xFF));
            IncCount(2);
        }

        public void WriteChar(int v)
        {
            @out.WriteByte((byte)((int)((uint)v >> 8) & 0xFF));
            @out.WriteByte((byte)((int)((uint)v >> 0) & 0xFF));
            IncCount(2);
        }

        public void WriteInt(int v)
        {
            @out.WriteByte((byte)(int)(((uint)v >> 24) & 0xFF));
            @out.WriteByte((byte)(int)(((uint)v >> 16) & 0xFF));
            @out.WriteByte((byte)(int)(((uint)v >>  8) & 0xFF));
            @out.WriteByte((byte)(int)(((uint)v >>  0) & 0xFF));
            IncCount(4);
        }

        private byte[] writeBuffer = new byte[8];

        public void WriteLong(long v)
        {
            writeBuffer[0] = (byte)(long)((ulong)v >> 56);
            writeBuffer[1] = (byte)(long)((ulong)v >> 48);
            writeBuffer[2] = (byte)(long)((ulong)v >> 40);
            writeBuffer[3] = (byte)(long)((ulong)v >> 32);
            writeBuffer[4] = (byte)(long)((ulong)v >> 24);
            writeBuffer[5] = (byte)(long)((ulong)v >> 16);
            writeBuffer[6] = (byte)(long)((ulong)v >> 8);
            writeBuffer[7] = (byte)(long)((ulong)v >> 0);
            @out.Write(writeBuffer, 0, 8);
            IncCount(8);
        }

        public void WriteFloat(float v)
        {
            WriteInt(Number.FloatToIntBits(v));
        }

        public void WriteDouble(double v)
        {
            WriteLong(Number.DoubleToLongBits(v));
        }

        public void WriteBytes(string s)
        {
            int len = s.Length;
            for (int i = 0; i < len; i++)
            {
                @out.WriteByte((byte)s[i]);
            }
            IncCount(len);
        }

        public void WriteChars(string s)
        {
            int len = s.Length;
            for (int i = 0; i < len; i++)
            {
                int v = s[i];
                @out.WriteByte((byte)(int)(((uint)v >> 8) & 0xFF));
                @out.WriteByte((byte)(int)(((uint)v >> 0) & 0xFF));
            }
            IncCount(len * 2);
        }

        public void WriteUTF(string str) 
        {
            WriteUTF(str, this);
        }

        internal static int WriteUTF(string str, IDataOutput @out)
        {
            int strlen = str.Length;
            int utflen = 0;
            int c, count = 0;

            /* use charAt instead of copying String to char array */
            for (int i = 0; i < strlen; i++)
            {
                c = str[i];
                if ((c >= 0x0001) && (c <= 0x007F))
                {
                    utflen++;
                }
                else if (c > 0x07FF)
                {
                    utflen += 3;
                }
                else
                {
                    utflen += 2;
                }
            }

            if (utflen > 65535)
                throw new FormatException(
                    "encoded string too long: " + utflen + " bytes");

            byte[] bytearr = null;
            if (@out is DataOutputStream) {
                DataOutputStream dos = (DataOutputStream)@out;
                if (dos.bytearr == null || (dos.bytearr.Length < (utflen + 2)))
                    dos.bytearr = new byte[(utflen * 2) + 2];
                bytearr = dos.bytearr;
            } else {
                bytearr = new byte[utflen + 2];
            }

            bytearr[count++] = (byte)(int)(((uint)utflen >> 8) & 0xFF);
            bytearr[count++] = (byte)(int)(((uint)utflen >> 0) & 0xFF);

            int i2 = 0;
            for (i2 = 0; i2 < strlen; i2++)
            {
                c = str[i2];
                if (!((c >= 0x0001) && (c <= 0x007F))) break;
                bytearr[count++] = (byte)c;
            }

            for (; i2 < strlen; i2++)
            {
                c = str[i2];
                if ((c >= 0x0001) && (c <= 0x007F))
                {
                    bytearr[count++] = (byte)c;

                }
                else if (c > 0x07FF)
                {
                    bytearr[count++] = (byte)(0xE0 | ((c >> 12) & 0x0F));
                    bytearr[count++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                    bytearr[count++] = (byte)(0x80 | ((c >> 0) & 0x3F));
                }
                else
                {
                    bytearr[count++] = (byte)(0xC0 | ((c >> 6) & 0x1F));
                    bytearr[count++] = (byte)(0x80 | ((c >> 0) & 0x3F));
                }
            }
            @out.Write(bytearr, 0, utflen + 2);
            return utflen + 2;
        }

        public int Length
        {
            get { return written; }
        }


        #region From FilterOutputStream

        public void Write(byte[] b)
        {
            Write(b, 0, b.Length);
        }

        public void Dispose()
        {
            @out.Dispose();
        }

        #endregion
    }
}
