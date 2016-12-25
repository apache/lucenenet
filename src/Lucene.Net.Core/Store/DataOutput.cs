using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Store
{
    using System;

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
    /// <p>{@code DataOutput} may only be used from one thread, because it is not
    /// thread safe (it keeps internal state like file position).
    /// </summary>
    public abstract class DataOutput
    {
        /// <summary>
        /// Writes a single byte.
        /// <p>
        /// The most primitive data type is an eight-bit byte. Files are
        /// accessed as sequences of bytes. All other data types are defined
        /// as sequences of bytes, so file formats are byte-order independent.
        /// </summary>
        /// <seealso cref= IndexInput#readByte() </seealso>
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
        /// <seealso cref= DataInput#readBytes(byte[],int,int) </seealso>
        public abstract void WriteBytes(byte[] b, int offset, int length);

        /// <summary>
        /// Writes an int as four bytes.
        /// <p>
        /// 32-bit unsigned integer written as four bytes, high-order bytes first.
        /// </summary>
        /// <seealso cref= DataInput#readInt() </seealso>
        public virtual void WriteInt(int i) // LUCENENET TODO: Rename WriteInt32() ?
        {
            WriteByte((byte)(sbyte)(i >> 24));
            WriteByte((byte)(sbyte)(i >> 16));
            WriteByte((byte)(sbyte)(i >> 8));
            WriteByte((byte)(sbyte)i);
        }

        /// <summary>
        /// Writes a short as two bytes. </summary>
        /// <seealso cref= DataInput#readShort() </seealso>
        public virtual void WriteShort(short i) // LUCENENET TODO: Rename WriteInt16() ?
        {
            WriteByte((byte)(sbyte)((ushort)i >> 8));
            WriteByte((byte)(sbyte)(ushort)i);
        }

        /// <summary>
        /// Writes an int in a variable-length format.  Writes between one and
        /// five bytes.  Smaller values take fewer bytes.  Negative numbers are
        /// supported, but should be avoided.
        /// <p>VByte is a variable-length format for positive integers is defined where the
        /// high-order bit of each byte indicates whether more bytes remain to be read. The
        /// low-order seven bits are appended as increasingly more significant bits in the
        /// resulting integer value. Thus values from zero to 127 may be stored in a single
        /// byte, values from 128 to 16,383 may be stored in two bytes, and so on.</p>
        /// <p>VByte Encoding Example</p>
        /// <table cellspacing="0" cellpadding="2" border="0">
        /// <col width="64*">
        /// <col width="64*">
        /// <col width="64*">
        /// <col width="64*">
        /// <tr valign="top">
        ///   <th align="left" width="25%">Value</th>
        ///   <th align="left" width="25%">Byte 1</th>
        ///   <th align="left" width="25%">Byte 2</th>
        ///   <th align="left" width="25%">Byte 3</th>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">0</td>
        ///   <td width="25%"><kbd>00000000</kbd></td>
        ///   <td width="25%"></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">1</td>
        ///   <td width="25%"><kbd>00000001</kbd></td>
        ///   <td width="25%"></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">2</td>
        ///   <td width="25%"><kbd>00000010</kbd></td>
        ///   <td width="25%"></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr>
        ///   <td valign="top" width="25%">...</td>
        ///   <td valign="bottom" width="25%"></td>
        ///   <td valign="bottom" width="25%"></td>
        ///   <td valign="bottom" width="25%"></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">127</td>
        ///   <td width="25%"><kbd>01111111</kbd></td>
        ///   <td width="25%"></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">128</td>
        ///   <td width="25%"><kbd>10000000</kbd></td>
        ///   <td width="25%"><kbd>00000001</kbd></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">129</td>
        ///   <td width="25%"><kbd>10000001</kbd></td>
        ///   <td width="25%"><kbd>00000001</kbd></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">130</td>
        ///   <td width="25%"><kbd>10000010</kbd></td>
        ///   <td width="25%"><kbd>00000001</kbd></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr>
        ///   <td valign="top" width="25%">...</td>
        ///   <td width="25%"></td>
        ///   <td width="25%"></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">16,383</td>
        ///   <td width="25%"><kbd>11111111</kbd></td>
        ///   <td width="25%"><kbd>01111111</kbd></td>
        ///   <td width="25%"></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">16,384</td>
        ///   <td width="25%"><kbd>10000000</kbd></td>
        ///   <td width="25%"><kbd>10000000</kbd></td>
        ///   <td width="25%"><kbd>00000001</kbd></td>
        /// </tr>
        /// <tr valign="bottom">
        ///   <td width="25%">16,385</td>
        ///   <td width="25%"><kbd>10000001</kbd></td>
        ///   <td width="25%"><kbd>10000000</kbd></td>
        ///   <td width="25%"><kbd>00000001</kbd></td>
        /// </tr>
        /// <tr>
        ///   <td valign="top" width="25%">...</td>
        ///   <td valign="bottom" width="25%"></td>
        ///   <td valign="bottom" width="25%"></td>
        ///   <td valign="bottom" width="25%"></td>
        /// </tr>
        /// </table>
        /// <p>this provides compression while still being efficient to decode.</p>
        /// </summary>
        /// <param name="i"> Smaller values take fewer bytes.  Negative numbers are
        /// supported, but should be avoided. </param>
        /// <exception cref="System.IO.IOException"> If there is an I/O error writing to the underlying medium. </exception>
        /// <seealso cref= DataInput#readVInt() </seealso>
        public void WriteVInt(int i) // LUCENENET TODO: Rename WriteVInt32() ?
        {
            while ((i & ~0x7F) != 0)
            {
                WriteByte((byte)unchecked((sbyte)((i & 0x7F) | 0x80)));
                i = (int)((uint)i >> 7);
            }
            WriteByte((byte)(sbyte)i);
        }

        /// <summary>
        /// Writes a long as eight bytes.
        /// <p>
        /// 64-bit unsigned integer written as eight bytes, high-order bytes first.
        /// </summary>
        /// <seealso cref= DataInput#readLong() </seealso>
        public virtual void WriteLong(long i) // LUCENENET TODO: Rename WriteInt64() ?
        {
            WriteInt((int)(i >> 32));
            WriteInt((int)i);
        }

        /// <summary>
        /// Writes an long in a variable-length format.  Writes between one and nine
        /// bytes.  Smaller values take fewer bytes.  Negative numbers are not
        /// supported.
        /// <p>
        /// The format is described further in <seealso cref="DataOutput#writeVInt(int)"/>. </summary>
        /// <seealso cref= DataInput#readVLong() </seealso>
        public void WriteVLong(long i) // LUCENENET TODO: Rename WriteVInt64() ?
        {
            Debug.Assert(i >= 0L);
            while ((i & ~0x7FL) != 0L)
            {
                WriteByte((byte)unchecked((sbyte)((i & 0x7FL) | 0x80L)));
                i = (long)((ulong)i >> 7);
            }
            WriteByte((byte)(sbyte)i);
        }

        /// <summary>
        /// Writes a string.
        /// <p>
        /// Writes strings as UTF-8 encoded bytes. First the length, in bytes, is
        /// written as a <seealso cref="#writeVInt VInt"/>, followed by the bytes.
        /// </summary>
        /// <seealso cref= DataInput#readString() </seealso>
        public virtual void WriteString(string s)
        {
            var utf8Result = new BytesRef(10);
            UnicodeUtil.UTF16toUTF8(s.ToCharArray(), 0, s.Length, utf8Result);
            WriteVInt(utf8Result.Length);
            WriteBytes(utf8Result.Bytes, 0, utf8Result.Length);
        }

        // LUCENENET: This helper method is here because OfflineSorter
        // uses java.io.DataOutput in Lucene, but Lucene.Net uses this class
        public void Write(byte[] b, int off, int len)
        {
            WriteBytes(b, off, len);
        }

        private const int COPY_BUFFER_SIZE = 16384;
        private byte[] CopyBuffer;

        /// <summary>
        /// Copy numBytes bytes from input to ourself. </summary>
        public virtual void CopyBytes(DataInput input, long numBytes)
        {
            Debug.Assert(numBytes >= 0, "numBytes=" + numBytes);
            long left = numBytes;
            if (CopyBuffer == null)
            {
                CopyBuffer = new byte[COPY_BUFFER_SIZE];
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
                input.ReadBytes(CopyBuffer, 0, toCopy);
                WriteBytes(CopyBuffer, 0, toCopy);
                left -= toCopy;
            }
        }

        /// <summary>
        /// Writes a String map.
        /// <p>
        /// First the size is written as an <seealso cref="#writeInt(int) Int32"/>,
        /// followed by each key-value pair written as two consecutive
        /// <seealso cref="#writeString(String) String"/>s.
        /// </summary>
        /// <param name="map"> Input map. May be null (equivalent to an empty map) </param>
        public virtual void WriteStringStringMap(IDictionary<string, string> map)
        {
            if (map == null)
            {
                WriteInt(0);
            }
            else
            {
                WriteInt(map.Count);
                foreach (KeyValuePair<string, string> entry in map)
                {
                    WriteString(entry.Key);
                    WriteString(entry.Value);
                }
            }
        }

        /// <summary>
        /// Writes a String set.
        /// <p>
        /// First the size is written as an <seealso cref="#writeInt(int) Int32"/>,
        /// followed by each value written as a
        /// <seealso cref="#writeString(String) String"/>.
        /// </summary>
        /// <param name="set"> Input set. May be null (equivalent to an empty set) </param>
        public virtual void WriteStringSet(ISet<string> set)
        {
            if (set == null)
            {
                WriteInt(0);
            }
            else
            {
                WriteInt(set.Count);
                foreach (string value in set)
                {
                    WriteString(value);
                }
            }
        }
    }
}