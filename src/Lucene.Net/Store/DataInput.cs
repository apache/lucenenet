using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// Abstract base class for performing read operations of Lucene's low-level
    /// data types.
    ///
    /// <para/><see cref="DataInput"/> may only be used from one thread, because it is not
    /// thread safe (it keeps internal state like file position). To allow
    /// multithreaded use, every <see cref="DataInput"/> instance must be cloned before
    /// used in another thread. Subclasses must therefore implement <see cref="Clone()"/>,
    /// returning a new <see cref="DataInput"/> which operates on the same underlying
    /// resource, but positioned independently.
    /// </summary>
    public abstract class DataInput // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private const int SKIP_BUFFER_SIZE = 1024;

        /// <summary>
        /// This buffer is used to skip over bytes with the default implementation of
        /// skipBytes. The reason why we need to use an instance member instead of
        /// sharing a single instance across threads is that some delegating
        /// implementations of DataInput might want to reuse the provided buffer in
        /// order to eg.update the checksum. If we shared the same buffer across
        /// threads, then another thread might update the buffer while the checksum is
        /// being computed, making it invalid. See LUCENE-5583 for more information.
        /// </summary>
        private byte[] skipBuffer;

        /// <summary>
        /// Reads and returns a single byte. </summary>
        /// <seealso cref="DataOutput.WriteByte(byte)"/>
        public abstract byte ReadByte();

        /// <summary>
        /// Reads a specified number of bytes into an array at the specified offset. </summary>
        /// <param name="b"> the array to read bytes into </param>
        /// <param name="offset"> the offset in the array to start storing bytes </param>
        /// <param name="len"> the number of bytes to read </param>
        /// <seealso cref="DataOutput.WriteBytes(byte[], int)"/>
        public abstract void ReadBytes(byte[] b, int offset, int len);

        /// <summary>
        /// Reads a specified number of bytes into an array at the
        /// specified offset with control over whether the read
        /// should be buffered (callers who have their own buffer
        /// should pass in "false" for <paramref name="useBuffer"/>).  Currently only
        /// <see cref="BufferedIndexInput"/> respects this parameter. </summary>
        /// <param name="b"> the array to read bytes into </param>
        /// <param name="offset"> the offset in the array to start storing bytes </param>
        /// <param name="len"> the number of bytes to read </param>
        /// <param name="useBuffer"> set to false if the caller will handle
        /// buffering. </param>
        /// <seealso cref="DataOutput.WriteBytes(byte[],int)"/>
        public virtual void ReadBytes(byte[] b, int offset, int len, bool useBuffer)
        {
            // Default to ignoring useBuffer entirely
            ReadBytes(b, offset, len);
        }

        /// <summary>
        /// Reads two bytes and returns a <see cref="short"/>. 
        /// <para/>
        /// LUCENENET NOTE: Important - always cast to ushort (System.UInt16) before using to ensure
        /// the value is positive!
        /// <para/>
        /// NOTE: this was readShort() in Lucene
        /// </summary>
        /// <seealso cref="DataOutput.WriteInt16(short)"/>
        public virtual short ReadInt16()
        {
            return (short)(((ReadByte() & 0xFF) << 8) | (ReadByte() & 0xFF));
        }

        /// <summary>
        /// Reads four bytes and returns an <see cref="int"/>. 
        /// <para/>
        /// NOTE: this was readInt() in Lucene
        /// </summary>
        /// <seealso cref="DataOutput.WriteInt32(int)"/>
        public virtual int ReadInt32()
        {
            return ((ReadByte() & 0xFF) << 24) | ((ReadByte() & 0xFF) << 16) 
                | ((ReadByte() & 0xFF) << 8) | (ReadByte() & 0xFF);
        }

        /// <summary>
        /// Reads an <see cref="int"/> stored in variable-length format.  Reads between one and
        /// five bytes.  Smaller values take fewer bytes.  Negative numbers are not
        /// supported.
        /// <para/>
        /// The format is described further in <see cref="DataOutput.WriteVInt32(int)"/>.
        /// <para/>
        /// NOTE: this was readVInt() in Lucene
        /// </summary>
        /// <seealso cref="DataOutput.WriteVInt32(int)"/>
        public virtual int ReadVInt32()
        {
            byte b = ReadByte();
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return b;
            }
            int i = b & 0x7F;
            b = ReadByte();
            i |= (b & 0x7F) << 7;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7F) << 14;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7F) << 21;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            // Warning: the next ands use 0x0F / 0xF0 - beware copy/paste errors:
            i |= (b & 0x0F) << 28;
            if (((sbyte)b & 0xF0) == 0)
            {
                return i;
            }
            throw new IOException("Invalid VInt32 detected (too many bits)");
        }

        /// <summary>
        /// Reads eight bytes and returns a <see cref="long"/>. 
        /// <para/>
        /// NOTE: this was readLong() in Lucene
        /// </summary>
        /// <seealso cref="DataOutput.WriteInt64(long)"/>
        public virtual long ReadInt64()
        {
            return (((long)ReadInt32()) << 32) | (ReadInt32() & 0xFFFFFFFFL);
        }

        /// <summary>
        /// Reads a <see cref="long"/> stored in variable-length format.  Reads between one and
        /// nine bytes.  Smaller values take fewer bytes.  Negative numbers are not
        /// supported.
        /// <para/>
        /// The format is described further in <seealso cref="DataOutput.WriteVInt32(int)"/>.
        /// <para/>
        /// NOTE: this was readVLong() in Lucene
        /// </summary>
        /// <seealso cref="DataOutput.WriteVInt64(long)"/>
        public virtual long ReadVInt64()
        {
            /* This is the original code of this method,
             * but a Hotspot bug (see LUCENE-2975) corrupts the for-loop if
             * readByte() is inlined. So the loop was unwinded!
            byte b = readByte();
            long i = b & 0x7F;
            for (int shift = 7; (b & 0x80) != 0; shift += 7) {
              b = readByte();
              i |= (b & 0x7FL) << shift;
            }
            return i;
            */
            byte b = ReadByte();
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return b;
            }
            long i = b & 0x7FL;
            b = ReadByte();
            i |= (b & 0x7FL) << 7;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7FL) << 14;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7FL) << 21;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7FL) << 28;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7FL) << 35;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7FL) << 42;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7FL) << 49;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = ReadByte();
            i |= (b & 0x7FL) << 56;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            throw new IOException("Invalid VInt64 detected (negative values disallowed)");
        }

        /// <summary>
        /// Reads a <see cref="string"/>. </summary>
        /// <seealso cref="DataOutput.WriteString(string)"/>
        public virtual string ReadString()
        {
            int length = ReadVInt32();
            byte[] bytes = new byte[length];
            ReadBytes(bytes, 0, length);

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Returns a clone of this stream.
        ///
        /// <para/>Clones of a stream access the same data, and are positioned at the same
        /// point as the stream they were cloned from.
        ///
        /// <para/>Expert: Subclasses must ensure that clones may be positioned at
        /// different points in the input from each other and from the stream they
        /// were cloned from.
        /// </summary>
        public virtual object Clone()
        {
            return base.MemberwiseClone();
        }

        /// <summary>
        /// Reads a IDictionary&lt;string,string&gt; previously written
        ///  with <see cref="DataOutput.WriteStringStringMap(IDictionary{string, string})"/>.
        /// </summary>
        public virtual IDictionary<string, string> ReadStringStringMap()
        {
            IDictionary<string, string> map = new Dictionary<string, string>();
            int count = ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string key = ReadString();
                string val = ReadString();
                map[key] = val;
            }

            return map;
        }

        /// <summary>
        /// Reads a ISet&lt;string&gt; previously written
        /// with <see cref="DataOutput.WriteStringSet(ISet{string})"/>.
        /// </summary>
        public virtual ISet<string> ReadStringSet()
        {
            ISet<string> set = new JCG.HashSet<string>();
            int count = ReadInt32();
            for (int i = 0; i < count; i++)
            {
                set.Add(ReadString());
            }

            return set;
        }

        /// <summary>
        /// Skip over <paramref name="numBytes"/> bytes. The contract on this method is that it
        /// should have the same behavior as reading the same number of bytes into a
        /// buffer and discarding its content. Negative values of <paramref name="numBytes"/>
        /// are not supported.
        /// </summary>
        public virtual void SkipBytes(long numBytes)
        {
            if (numBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numBytes), "numBytes must be >= 0, got " + numBytes); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (skipBuffer is null)
            {
                skipBuffer = new byte[SKIP_BUFFER_SIZE];
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(skipBuffer.Length == SKIP_BUFFER_SIZE);
            for (long skipped = 0; skipped < numBytes; )
            {
                var step = (int)Math.Min(SKIP_BUFFER_SIZE, numBytes - skipped);
                ReadBytes(skipBuffer, 0, step, false);
                skipped += step;
            }
        }
    }
}