/*
 * Copyright (c) 1999, 2008, Oracle and/or its affiliates. All rights reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Oracle designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Oracle in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Oracle, 500 Oracle Parkway, Redwood Shores, CA 94065 USA
 * or visit www.oracle.com if you need additional information or have any
 * questions.
 */

using System;
using System.IO;

namespace Lucene.Net.Support
{
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
        private readonly Stream @in;

        /// <summary>
        /// Creates a DataInputStream that uses the specified
        /// underlying InputStream.
        /// </summary>
        /// <param name="in">the specified input stream</param>
        public DataInputStream(Stream @in)
        {
            this.@in = @in;
        }

        /// <summary>
        /// working arrays initialized on demand by readUTF
        /// </summary>
        private byte[] bytearr = new byte[80];
        private char[] chararr = new char[80];

        public int Read(byte[] b)
        {
            return @in.Read(b, 0, b.Length);
        }

        public int Read(byte[] b, int off, int len)
        {
            return @in.Read(b, off, len);
        }

        public void ReadFully(byte[] b)
        {
            ReadFully(b, 0, b.Length);
        }

        public void ReadFully(byte[] b, int off, int len)
        {
            if (len < 0)
            {
                throw new IndexOutOfRangeException();
            }
            int n = 0;
            while (n < len)
            {
                int count = @in.Read(b, off + n, len - n);
                if (count == 0)
                {
                    throw new EndOfStreamException();
                }
                n += count;
            }
        }

        public int SkipBytes(int n)
        {
            int total = 0;
            int cur = 0;

            while ((total < n) && ((cur = Skip(@in, n - total)) > 0))
            {
                total += cur;
            }

            return total;
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

        public bool ReadBoolean()
        {
            int ch = @in.ReadByte();
            if (ch < 0)
            {
                throw new EndOfStreamException();
            }
            return (ch != 0);
        }

        /// <summary>
        /// NOTE: This was readByte() in the JDK
        /// </summary>
        public int ReadSByte()
        {
            int ch = @in.ReadByte();
            if (ch < 0)
            {
                throw new EndOfStreamException();
            }
            return ch;
        }

        /// <summary>
        /// NOTE: This was readUnsignedByte() in the JDK
        /// </summary>
        public byte ReadByte()
        {
            int ch = @in.ReadByte();
            if (ch < 0)
            {
                throw new EndOfStreamException();
            }
            return (byte)ch;
        }

        /// <summary>
        /// NOTE: This was readShort() in the JDK
        /// </summary>
        public short ReadInt16()
        {
            int ch1 = @in.ReadByte();
            int ch2 = @in.ReadByte();
            if ((ch1 | ch2) < 0)
            {
                throw new EndOfStreamException();
            }
            return (short)((ch1 << 8) + (ch2 << 0));
        }

        /// <summary>
        /// NOTE: This was readUnsignedShort() in the JDK
        /// </summary>
        public int ReadUInt16()
        {
            int ch1 = @in.ReadByte();
            int ch2 = @in.ReadByte();
            if ((ch1 | ch2) < 0)
            {
                throw new EndOfStreamException();
            }
            return (ch1 << 8) + (ch2 << 0);
        }

        public char ReadChar()
        {
            int ch1 = @in.ReadByte();
            int ch2 = @in.ReadByte();
            if ((ch1 | ch2) < 0)
            {
                throw new EndOfStreamException();
            }
            return (char)((ch1 << 8) + (ch2 << 0));
        }

        /// <summary>
        /// NOTE: This was readInt() in the JDK
        /// </summary>
        public int ReadInt32()
        {
            int ch1 = @in.ReadByte();
            int ch2 = @in.ReadByte();
            int ch3 = @in.ReadByte();
            int ch4 = @in.ReadByte();
            if ((ch1 | ch2 | ch3 | ch4) < 0)
            {
                throw new EndOfStreamException();
            }
            return ((ch1 << 24) + (ch2 << 16) + (ch3 << 8) + (ch4 << 0));
        }

        private byte[] readBuffer = new byte[8];

        /// <summary>
        /// NOTE: This was readLong() in the JDK
        /// </summary>
        public long ReadInt64()
        {
            ReadFully(readBuffer, 0, 8);
            return (((long)readBuffer[0] << 56) +
                    ((long)(readBuffer[1] & 255) << 48) +
                    ((long)(readBuffer[2] & 255) << 40) +
                    ((long)(readBuffer[3] & 255) << 32) +
                    ((long)(readBuffer[4] & 255) << 24) +
                    ((readBuffer[5] & 255) << 16) +
                    ((readBuffer[6] & 255) << 8) +
                    ((readBuffer[7] & 255) << 0));
        }

        /// <summary>
        /// NOTE: This was readFloat() in the JDK
        /// </summary>
        public float ReadSingle()
        {
            return Number.Int32BitsToSingle(ReadInt32());
        }

        public double ReadDouble()
        {
            throw new NotImplementedException();
            //return Number.LongBitsToDouble(ReadLong());
        }

        private char[] lineBuffer;

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

        public string ReadUTF()
        {
            return ReadUTF(this);
        }

        public static string ReadUTF(IDataInput @in)
        {
            int utflen = @in.ReadUInt16();
            byte[] bytearr = null;
            char[] chararr = null;
            if (@in is DataInputStream)
            {
                DataInputStream dis = (DataInputStream)@in;
                if (dis.bytearr.Length < utflen)
                {
                    dis.bytearr = new byte[utflen * 2];
                    dis.chararr = new char[utflen * 2];
                }
                chararr = dis.chararr;
                bytearr = dis.bytearr;
            }
            else
            {
                bytearr = new byte[utflen];
                chararr = new char[utflen];
            }

            int c, char2, char3;
            int count = 0;
            int chararr_count = 0;

            @in.ReadFully(bytearr, 0, utflen);

            while (count < utflen)
            {
                c = (int)bytearr[count] & 0xff;
                if (c > 127) break;
                count++;
                chararr[chararr_count++] = (char)c;
            }

            while (count < utflen)
            {
                c = (int)bytearr[count] & 0xff;
                switch (c >> 4)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        /* 0xxxxxxx*/
                        count++;
                        chararr[chararr_count++] = (char)c;
                        break;
                    case 12:
                    case 13:
                        /* 110x xxxx   10xx xxxx*/
                        count += 2;
                        if (count > utflen)
                        {
                            throw new FormatException(
                                "malformed input: partial character at end");
                        }
                        char2 = (int)bytearr[count - 1];
                        if ((char2 & 0xC0) != 0x80)
                        {
                            throw new FormatException(
                                "malformed input around byte " + count);
                        }
                        chararr[chararr_count++] = (char)(((c & 0x1F) << 6) |
                                                        (char2 & 0x3F));
                        break;
                    case 14:
                        /* 1110 xxxx  10xx xxxx  10xx xxxx */
                        count += 3;
                        if (count > utflen)
                        {
                            throw new FormatException(
                                "malformed input: partial character at end");
                        }
                        char2 = (int)bytearr[count - 2];
                        char3 = (int)bytearr[count - 1];
                        if (((char2 & 0xC0) != 0x80) || ((char3 & 0xC0) != 0x80))
                        {
                            throw new FormatException(
                                "malformed input around byte " + (count - 1));
                        }
                        chararr[chararr_count++] = (char)(((c & 0x0F) << 12) |
                                                        ((char2 & 0x3F) << 6) |
                                                        ((char3 & 0x3F) << 0));
                        break;
                    default:
                        /* 10xx xxxx,  1111 xxxx */
                        throw new FormatException(
                            "malformed input around byte " + count);
                }
            }
            // The number of chars produced may be less than utflen
            return new string(chararr, 0, chararr_count);
        }

        public void Dispose()
        {
            @in.Dispose();
        }
    }
}
