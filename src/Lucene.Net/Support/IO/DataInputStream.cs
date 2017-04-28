// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;
using System.IO;

namespace Lucene.Net.Support.IO
{
    /*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

    /// <summary>
    /// Java's DataInputStream is similar to .NET's BinaryReader. However, it reads
    /// using a modified UTF-8 format that cannot be read using BinaryReader.
    /// This is a port of DataInputStream that is fully compatible with Java's DataOutputStream.
    /// <para>
    /// Usage Note: Always favor BinaryReader over DataInputStream unless you specifically need
    /// the modified UTF-8 format and/or the <see cref="ReadUTF(IDataInput)"/> method.
    /// </para>
    /// </summary>
    public class DataInputStream : IDataInput, IDisposable
    {
        private byte[] buff;

        private readonly Stream @in;
        private char[] lineBuffer;

        /// <summary>
        /// Constructs a new <see cref="DataInputStream"/> on the <see cref="Stream"/> <paramref name="in"/>. All
        /// reads are then filtered through this stream. Note that data read by this
        /// stream is not in a human readable format and was most likely created by a
        /// <see cref="DataOutputStream"/>.
        /// </summary>
        /// <param name="in">the source <see cref="Stream"/> the filter reads from.</param>
        /// <seealso cref="DataOutputStream"/>
        public DataInputStream(Stream @in)
        {
            this.@in = @in;
            buff = new byte[8];
        }

        public int Read(byte[] buffer)
        {
            return @in.Read(buffer, 0, buffer.Length);
        }

        public int Read(byte[] buffer, int offset, int length)
        {
            return @in.Read(buffer, offset, length);
        }

        public bool ReadBoolean()
        {
            int temp = @in.ReadByte();
            if (temp < 0)
            {
                throw new EndOfStreamException();
            }
            return (temp != 0);
        }

        /// <summary>
        /// NOTE: This was readByte() in Java
        /// </summary>
        public int ReadSByte()
        {
            int temp = @in.ReadByte();
            if (temp < 0)
            {
                throw new EndOfStreamException();
            }
            return temp;
        }

        private int ReadToBuff(int count)
        {
            int offset = 0;

            while (offset < count)
            {
                int bytesRead = @in.Read(buff, offset, count - offset);
                if (bytesRead <= 0) return bytesRead;
                offset += bytesRead;
            }
            return offset;
        }

        public char ReadChar()
        {
            if (ReadToBuff(2) <= 0)
            {
                throw new EndOfStreamException();
            }
            return (char)(((buff[0] & 0xff) << 8) | (buff[1] & 0xff));
        }

        public double ReadDouble()
        {
            return BitConverter.Int64BitsToDouble(ReadInt64());
        }

        /// <summary>
        /// NOTE: This was readFloat() in Java
        /// </summary>
        public float ReadSingle()
        {
            return Number.Int32BitsToSingle(ReadInt32());
        }

        public void ReadFully(byte[] buffer)
        {
            ReadFully(buffer, 0, buffer.Length);
        }

        public void ReadFully(byte[] buffer, int offset, int length)
        {
            if (length < 0)
            {
                throw new IndexOutOfRangeException();
            }
            if (length == 0)
            {
                return;
            }
            if (@in == null)
            {
                throw new NullReferenceException("Input Stream is null"); //$NON-NLS-1$
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer"); //$NON-NLS-1$
            }
            if (offset < 0 || offset > buffer.Length - length)
            {
                throw new IndexOutOfRangeException();
            }
            while (length > 0)
            {
                int result = @in.Read(buffer, offset, length);
                if (result <= 0)
                {
                    throw new EndOfStreamException();
                }
                offset += result;
                length -= result;
            }
        }

        /// <summary>
        /// NOTE: This was readInt() in Java
        /// </summary>
        public int ReadInt32()
        {
            if (ReadToBuff(4) <= 0)
            {
                throw new EndOfStreamException();
            }
            return ((buff[0] & 0xff) << 24) | ((buff[1] & 0xff) << 16) |
                ((buff[2] & 0xff) << 8) | (buff[3] & 0xff);
        }

        [Obsolete]
        public string ReadLine()
        {
            char[] buf = lineBuffer;

            if (buf == null)
            {
                buf = lineBuffer = new char[128];
            }

            int room = buf.Length;
            int offset = 0;
            int c;

            while (true)
            {
                switch (c = @in.ReadByte())
                {
                    case -1:
                    case '\n':
                        goto loop;

                    case '\r':
                        int c2 = @in.ReadByte();
                        if ((c2 != '\n') && (c2 != -1))
                        {
                            using (StreamReader reader = new StreamReader(@in))
                            {
                                c2 = reader.Peek();
                            }
                            // http://stackoverflow.com/a/8021738/181087
                            //if (!(in is PushbackInputStream)) {
                            //    this.in = new PushbackInputStream(in);
                            //}
                            //((PushbackInputStream)in).unread(c2);
                        }
                        goto loop;

                    default:
                        if (--room < 0)
                        {
                            buf = new char[offset + 128];
                            room = buf.Length - offset - 1;
                            System.Array.Copy(lineBuffer, 0, buf, 0, offset);
                            lineBuffer = buf;
                        }
                        buf[offset++] = (char)c;
                        break;
                }
            }
            loop:
            if ((c == -1) && (offset == 0))
            {
                return null;
            }
            return new string(buf, 0, offset);
        }

        /// <summary>
        /// NOTE: This was readLong() in Java
        /// </summary>
        public long ReadInt64()
        {
            if (ReadToBuff(8) <= 0)
            {
                throw new EndOfStreamException();
            }
            int i1 = ((buff[0] & 0xff) << 24) | ((buff[1] & 0xff) << 16) |
                ((buff[2] & 0xff) << 8) | (buff[3] & 0xff);
            int i2 = ((buff[4] & 0xff) << 24) | ((buff[5] & 0xff) << 16) |
                ((buff[6] & 0xff) << 8) | (buff[7] & 0xff);

            return ((i1 & 0xffffffffL) << 32) | (i2 & 0xffffffffL);
        }

        /// <summary>
        /// NOTE: This was readShort() in Java
        /// </summary>
        public short ReadInt16()
        {
            if (ReadToBuff(2) <= 0)
            {
                throw new EndOfStreamException();
            }
            return (short)(((buff[0] & 0xff) << 8) | (buff[1] & 0xff));
        }

        /// <summary>
        /// NOTE: This was readUnsignedByte() in Java
        /// </summary>
        public int ReadByte()
        {
            int temp = @in.ReadByte();
            if (temp < 0)
            {
                throw new EndOfStreamException();
            }
            return temp;
        }

        /// <summary>
        /// NOTE: This was readUnsignedShort() in Java
        /// </summary>
        public int ReadUInt16()
        {
            if (ReadToBuff(2) <= 0)
            {
                throw new EndOfStreamException();
            }
            return (char)(((buff[0] & 0xff) << 8) | (buff[1] & 0xff));
        }

        public string ReadUTF()
        {
            return DecodeUTF(ReadUInt16());
        }

        private string DecodeUTF(int utfSize)
        {
            return DecodeUTF(utfSize, this);
        }

        private static string DecodeUTF(int utfSize, IDataInput @in)
        {
            byte[] buf = new byte[utfSize];
            char[] @out = new char[utfSize];
            @in.ReadFully(buf, 0, utfSize);

            return ConvertUTF8WithBuf(buf, @out, 0, utfSize);
        }

        private static string ConvertUTF8WithBuf(byte[] buf, char[] @out, int offset,
            int utfSize)
        {
            int count = 0, s = 0, a;
            while (count < utfSize)
            {
                if ((@out[s] = (char)buf[offset + count++]) < '\u0080')
                    s++;
                else if (((a = @out[s]) & 0xe0) == 0xc0)
                {
                    if (count >= utfSize)
                        throw new FormatException(string.Format("Second byte at {0} does not match UTF8 Specification",
                                count));
                    int b = buf[count++];
                    if ((b & 0xC0) != 0x80)
                        throw new FormatException(string.Format("Second byte at {0} does not match UTF8 Specification",
                                (count - 1)));
                    @out[s++] = (char)(((a & 0x1F) << 6) | (b & 0x3F));
                }
                else if ((a & 0xf0) == 0xe0)
                {
                    if (count + 1 >= utfSize)
                        throw new FormatException(string.Format("Third byte at {0} does not match UTF8 Specification",
                                (count + 1)));
                    int b = buf[count++];
                    int c = buf[count++];
                    if (((b & 0xC0) != 0x80) || ((c & 0xC0) != 0x80))
                        throw new FormatException(string.Format("Second or third byte at {0} does not match UTF8 Specification",
                                (count - 2)));
                    @out[s++] = (char)(((a & 0x0F) << 12) | ((b & 0x3F) << 6) | (c & 0x3F));
                }
                else
                {
                    throw new FormatException(string.Format("Input at {0} does not match UTF8 Specification",
                            (count - 1)));
                }
            }
            return new string(@out, 0, s);
        }

        public int SkipBytes(int count)
        {
            int skipped = 0;
            int skip;
            while (skipped < count && (skip = Skip(@in, count - skipped)) > 0) {
                skipped += skip;
            }
            if (skipped < 0)
            {
                throw new EndOfStreamException();
            }
            return skipped;
        }

        /// <summary>
        /// Helper method for SkipBytes, since Position and Seek do not work on
        /// non-seekable streams.
        /// </summary>
        private static int Skip(Stream stream, int n)
        {
            int total = 0;
            while (total < n && stream.ReadByte() > -1)
            {
                total++;
            }
            return total;
        }

        public void Dispose()
        {
            @in.Dispose();
        }
    }
}
