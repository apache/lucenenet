using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Store
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Abstract base class for performing write operations of Lucene's low-level
    /// data types.
    ///
    /// <para/><see cref="DataOutput"/> may only be used from one thread, because it is not
    /// thread safe (it keeps internal state like file position).
    /// </summary>
    public abstract class DataOutput
    {
        /// <summary>
        /// Writes a single byte.
        /// <para/>
        /// The most primitive data type is an eight-bit byte. Files are
        /// accessed as sequences of bytes. All other data types are defined
        /// as sequences of bytes, so file formats are byte-order independent.
        /// </summary>
        /// <seealso cref="DataInput.ReadByte()"/>
        public abstract void WriteByte(byte b);

        /// <summary>
        /// Writes an array of bytes.
        /// </summary>
        /// <param name="b">the bytes to write</param>
        /// <param name="length">the number of bytes to write</param>
        /// <seealso cref="DataInput.ReadBytes(byte[], int, int)"/>
        public virtual void WriteBytes(byte[] b, int length)
        {
            WriteBytes(b, 0, length);
        }

        /// <summary>
        /// Writes an array of bytes. </summary>
        /// <param name="b"> the bytes to write </param>
        /// <param name="offset"> the offset in the byte array </param>
        /// <param name="length"> the number of bytes to write </param>
        /// <seealso cref="DataInput.ReadBytes(byte[], int, int)"/>
        public abstract void WriteBytes(byte[] b, int offset, int length);

        /// <summary>
        /// Writes an <see cref="int"/> as four bytes.
        /// <para/>
        /// 32-bit unsigned integer written as four bytes, high-order bytes first.
        /// <para/>
        /// NOTE: this was writeInt() in Lucene
        /// </summary>
        /// <seealso cref="DataInput.ReadInt32()"/>
        public virtual void WriteInt32(int i)
        {
            WriteByte((byte)(i >> 24));
            WriteByte((byte)(i >> 16));
            WriteByte((byte)(i >> 8));
            WriteByte((byte)i);
        }

        /// <summary>
        /// Writes a short as two bytes. 
        /// <para/>
        /// NOTE: this was writeShort() in Lucene
        /// </summary>
        /// <seealso cref="DataInput.ReadInt16()"/>
        public virtual void WriteInt16(short i)
        {
            WriteByte((byte)((ushort)i >> 8));
            WriteByte((byte)(ushort)i);
        }

        /// <summary>
        /// Writes an <see cref="int"/> in a variable-length format.  Writes between one and
        /// five bytes.  Smaller values take fewer bytes.  Negative numbers are
        /// supported, but should be avoided.
        /// <para>VByte is a variable-length format for positive integers is defined where the
        /// high-order bit of each byte indicates whether more bytes remain to be read. The
        /// low-order seven bits are appended as increasingly more significant bits in the
        /// resulting integer value. Thus values from zero to 127 may be stored in a single
        /// byte, values from 128 to 16,383 may be stored in two bytes, and so on.</para>
        /// <para>VByte Encoding Example</para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Value</term>
        ///         <term>Byte 1</term>
        ///         <term>Byte 2</term>
        ///         <term>Byte 3</term>
        ///     </listheader>
        ///     <item>
        ///         <term>0</term>
        ///         <term>00000000</term>
        ///         <term></term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>1</term>
        ///         <term>00000001</term>
        ///         <term></term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>2</term>
        ///         <term>00000010</term>
        ///         <term></term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>...</term>
        ///         <term></term>
        ///         <term></term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>127</term>
        ///         <term>01111111</term>
        ///         <term></term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>128</term>
        ///         <term>10000000</term>
        ///         <term>00000001</term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>129</term>
        ///         <term>10000001</term>
        ///         <term>00000001</term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>130</term>
        ///         <term>10000010</term>
        ///         <term>00000001</term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>...</term>
        ///         <term></term>
        ///         <term></term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>16,383</term>
        ///         <term>11111111</term>
        ///         <term>01111111</term>
        ///         <term></term>
        ///     </item>
        ///     <item>
        ///         <term>16,384</term>
        ///         <term>10000000</term>
        ///         <term>10000000</term>
        ///         <term>00000001</term>
        ///     </item>
        ///     <item>
        ///         <term>16,385</term>
        ///         <term>10000001</term>
        ///         <term>10000000</term>
        ///         <term>00000001</term>
        ///     </item>
        ///     <item>
        ///         <term>...</term>
        ///         <term></term>
        ///         <term></term>
        ///         <term></term>
        ///     </item>
        /// </list>
        /// 
        /// <para>this provides compression while still being efficient to decode.</para>
        /// <para/>
        /// NOTE: this was writeVInt() in Lucene
        /// </summary>
        /// <param name="i"> Smaller values take fewer bytes.  Negative numbers are
        /// supported, but should be avoided. </param>
        /// <exception cref="IOException"> If there is an I/O error writing to the underlying medium. </exception>
        /// <seealso cref="DataInput.ReadVInt32()"/>
        public void WriteVInt32(int i)
        {
            while ((i & ~0x7F) != 0)
            {
                WriteByte((byte)((i & 0x7F) | 0x80));
                i = i.TripleShift(7);
            }
            WriteByte((byte)i);
        }

        /// <summary>
        /// Writes a <see cref="long"/> as eight bytes.
        /// <para/>
        /// 64-bit unsigned integer written as eight bytes, high-order bytes first.
        /// <para/>
        /// NOTE: this was writeLong() in Lucene
        /// </summary>
        /// <seealso cref="DataInput.ReadInt64()"/>
        public virtual void WriteInt64(long i)
        {
            WriteInt32((int)(i >> 32));
            WriteInt32((int)i);
        }

        /// <summary>
        /// Writes an <see cref="long"/> in a variable-length format.  Writes between one and nine
        /// bytes.  Smaller values take fewer bytes.  Negative numbers are not
        /// supported.
        /// <para/>
        /// The format is described further in <see cref="DataOutput.WriteVInt32(int)"/>. 
        /// <para/>
        /// NOTE: this was writeVLong() in Lucene
        /// </summary>
        /// <seealso cref="DataInput.ReadVInt64()"/>
        public void WriteVInt64(long i)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(i >= 0L);
            while ((i & ~0x7FL) != 0L)
            {
                WriteByte((byte)((i & 0x7FL) | 0x80L));
                i = i.TripleShift(7);
            }
            WriteByte((byte)i);
        }

        /// <summary>
        /// Writes a string.
        /// <para/>
        /// Writes strings as UTF-8 encoded bytes. First the length, in bytes, is
        /// written as a <see cref="WriteVInt32"/>, followed by the bytes.
        /// </summary>
        /// <seealso cref="DataInput.ReadString()"/>
        public virtual void WriteString(string s)
        {
            var utf8Result = new BytesRef(10);
            UnicodeUtil.UTF16toUTF8(s, 0, s.Length, utf8Result);
            WriteVInt32(utf8Result.Length);
            WriteBytes(utf8Result.Bytes, 0, utf8Result.Length);
        }

        private const int COPY_BUFFER_SIZE = 16384;
        private byte[] copyBuffer;

        /// <summary>
        /// Copy numBytes bytes from input to ourself. </summary>
        public virtual void CopyBytes(DataInput input, long numBytes)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(numBytes >= 0,"numBytes={0}", numBytes);
            long left = numBytes;
            if (copyBuffer is null)
            {
                copyBuffer = new byte[COPY_BUFFER_SIZE];
            }
            while (left > 0)
            {
                int toCopy;
                if (left > COPY_BUFFER_SIZE)
                {
                    toCopy = COPY_BUFFER_SIZE;
                }
                else
                {
                    toCopy = (int)left;
                }
                input.ReadBytes(copyBuffer, 0, toCopy);
                WriteBytes(copyBuffer, 0, toCopy);
                left -= toCopy;
            }
        }

        /// <summary>
        /// Writes a <see cref="T:IDictionary{string, string}"/>.
        /// <para/>
        /// First the size is written as an <see cref="WriteInt32(int)"/>,
        /// followed by each key-value pair written as two consecutive
        /// <see cref="WriteString(string)"/>s.
        /// </summary>
        /// <param name="map"> Input <see cref="T:IDictionary{string, string}"/>. May be <c>null</c> (equivalent to an empty dictionary) </param>
        public virtual void WriteStringStringMap(IDictionary<string, string> map)
        {
            if (map is null)
            {
                WriteInt32(0);
            }
            else
            {
                WriteInt32(map.Count);
                foreach (KeyValuePair<string, string> entry in map)
                {
                    WriteString(entry.Key);
                    WriteString(entry.Value);
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="string"/> set.
        /// <para/>
        /// First the size is written as an <see cref="WriteInt32(int)"/>,
        /// followed by each value written as a
        /// <see cref="WriteString(string)"/>.
        /// </summary>
        /// <param name="set"> Input <see cref="T:ISet{string}"/>. May be <c>null</c> (equivalent to an empty set) </param>
        public virtual void WriteStringSet(ISet<string> set)
        {
            if (set is null)
            {
                WriteInt32(0);
            }
            else
            {
                WriteInt32(set.Count);
                foreach (string value in set)
                {
                    WriteString(value);
                }
            }
        }
    }
}