// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;
using System.IO;
using System.Runtime.CompilerServices;

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
    /// Java's DataOutputStream is similar to .NET's BinaryWriter. However, it writes
    /// in a modified UTF-8 format that cannot be read (or duplicated) using BinaryWriter.
    /// This is a port of DataOutputStream that is fully compatible with Java's DataInputStream.
    /// <para>
    /// Usage Note: Always favor BinaryWriter over DataOutputStream unless you specifically need
    /// the modified UTF-8 format and/or the <see cref="WriteUTF(string)"/> method.
    /// </para>
    /// </summary>
    public class DataOutputStream : IDataOutput, IDisposable
    {
        private readonly object _lock = new object();

        /// <summary>
        /// The number of bytes written out so far.
        /// </summary>
        protected int written;
        private byte[] buff;


        private readonly Stream @out;

        /// <summary>
        /// Constructs a new <see cref="DataOutputStream"/> on the <see cref="Stream"/>
        /// <paramref name="out"/>. Note that data written by this stream is not in a human
        /// readable form but can be reconstructed by using a <see cref="DataInputStream"/>
        /// on the resulting output.
        /// </summary>
        /// <param name="out">the target stream for writing.</param>
        /// <seealso cref="DataInputStream"/>
        public DataOutputStream(Stream @out)
        {
            this.@out = @out;
            buff = new byte[8];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void Flush()
        {
            @out.Flush();
        }

        public int Length
        {
            get
            {
                if (written < 0)
                {
                    written = int.MaxValue;
                }
                return written;
            }
        }

        public virtual void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            lock (_lock)
            {
                @out.Write(buffer, offset, count);
                written += count;
            }
        }

        public virtual void Write(int oneByte)
        {
            lock (_lock)
            {
                @out.WriteByte((byte)oneByte);
                written++;
            }
        }

        public void WriteBoolean(bool val)
        {
            lock (_lock)
            {
                @out.WriteByte((byte)(val ? 1 : 0));
                written++;
            }
        }

        public void WriteByte(int val)
        {
            lock (_lock)
            {
                @out.WriteByte((byte)val);
                written++;
            }
        }

        public void WriteBytes(string str)
        {
            lock (_lock)
            {
                if (str.Length == 0)
                {
                    return;
                }
                byte[] bytes = new byte[str.Length];
                for (int index = 0; index < str.Length; index++)
                {
                    bytes[index] = (byte)str[index];
                }
                @out.Write(bytes, 0, bytes.Length);
                written += bytes.Length;
            }
        }

        public void WriteChar(int val)
        {
            lock (_lock)
            {
                buff[0] = (byte)(val >> 8);
                buff[1] = (byte)val;
                @out.Write(buff, 0, 2);
                written += 2;
            }
        }

        public void WriteChars(string str)
        {
            lock (_lock)
            {
                byte[] newBytes = new byte[str.Length * 2];
                for (int index = 0; index < str.Length; index++)
                {
                    int newIndex = index == 0 ? index : index * 2;
                    newBytes[newIndex] = (byte)(str[index] >> 8);
                    newBytes[newIndex + 1] = (byte)str[index];
                }
                @out.Write(newBytes, 0, newBytes.Length);
                written += newBytes.Length;
            }
        }

        public void WriteDouble(double val)
        {
            WriteInt64(Number.DoubleToInt64Bits(val));
        }

        /// <summary>
        /// NOTE: This was writeFloat() in Java
        /// </summary>
        public void WriteSingle(float val)
        {
            WriteInt32(Number.SingleToInt32Bits(val));
        }

        /// <summary>
        /// NOTE: This was writeInt() in Java
        /// </summary>
        public void WriteInt32(int val)
        {
            lock (_lock)
            {
                buff[0] = (byte)(val >> 24);
                buff[1] = (byte)(val >> 16);
                buff[2] = (byte)(val >> 8);
                buff[3] = (byte)val;
                @out.Write(buff, 0, 4);
                written += 4;
            }
        }

        /// <summary>
        /// NOTE: This was writeLong() in Java
        /// </summary>
        public void WriteInt64(long val)
        {
            lock (_lock)
            {
                buff[0] = (byte)(val >> 56);
                buff[1] = (byte)(val >> 48);
                buff[2] = (byte)(val >> 40);
                buff[3] = (byte)(val >> 32);
                buff[4] = (byte)(val >> 24);
                buff[5] = (byte)(val >> 16);
                buff[6] = (byte)(val >> 8);
                buff[7] = (byte)val;
                @out.Write(buff, 0, 8);
                written += 8;
            }
        }

        private int WriteInt64ToBuffer(long val,
                          byte[] buffer, int offset)
        {
            buffer[offset++] = (byte)(val >> 56);
            buffer[offset++] = (byte)(val >> 48);
            buffer[offset++] = (byte)(val >> 40);
            buffer[offset++] = (byte)(val >> 32);
            buffer[offset++] = (byte)(val >> 24);
            buffer[offset++] = (byte)(val >> 16);
            buffer[offset++] = (byte)(val >> 8);
            buffer[offset++] = (byte)val;
            return offset;
        }

        /// <summary>
        /// NOTE: This was writeShort() in Java
        /// </summary>
        public void WriteInt16(int val)
        {
            lock (_lock)
            {
                buff[0] = (byte)(val >> 8);
                buff[1] = (byte)val;
                @out.Write(buff, 0, 2);
                written += 2;
            }
        }

        private int WriteInt16ToBuffer(int val,
                           byte[] buffer, int offset)
        {
            buffer[offset++] = (byte)(val >> 8);
            buffer[offset++] = (byte)val;
            return offset;
        }

        public void WriteUTF(string str)
        {
            long utfCount = CountUTFBytes(str);
            if (utfCount > 65535)
            {
                throw new FormatException("data format too long"); //$NON-NLS-1$
            }
            byte[] buffer = new byte[(int)utfCount + 2];
            int offset = 0;
            offset = WriteInt16ToBuffer((int)utfCount, buffer, offset);
            offset = WriteUTFBytesToBuffer(str, (int)utfCount, buffer, offset);
            Write(buffer, 0, offset);
        }

        private long CountUTFBytes(string str)
        {
            int utfCount = 0, length = str.Length;
            for (int i = 0; i < length; i++)
            {
                int charValue = str[i];
                if (charValue > 0 && charValue <= 127)
                {
                    utfCount++;
                }
                else if (charValue <= 2047)
                {
                    utfCount += 2;
                }
                else
                {
                    utfCount += 3;
                }
            }
            return utfCount;
        }

        private int WriteUTFBytesToBuffer(string str, long count,
                              byte[] buffer, int offset)
        {
            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                int charValue = str[i];
                if (charValue > 0 && charValue <= 127)
                {
                    buffer[offset++] = (byte)charValue;
                }
                else if (charValue <= 2047)
                {
                    buffer[offset++] = (byte)(0xc0 | (0x1f & (charValue >> 6)));
                    buffer[offset++] = (byte)(0x80 | (0x3f & charValue));
                }
                else
                {
                    buffer[offset++] = (byte)(0xe0 | (0x0f & (charValue >> 12)));
                    buffer[offset++] = (byte)(0x80 | (0x3f & (charValue >> 6)));
                    buffer[offset++] = (byte)(0x80 | (0x3f & charValue));
                }
            }
            return offset;
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