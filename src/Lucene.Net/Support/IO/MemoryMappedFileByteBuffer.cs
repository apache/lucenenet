using System;
using System.IO.MemoryMappedFiles;

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

    internal sealed class MemoryMappedFileByteBuffer : ByteBuffer, IDisposable
    {
        private MemoryMappedViewAccessor accessor;
        private readonly int offset;

        public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int capacity)
            : base(capacity)
        {
            this.accessor = accessor;
        }

        public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int capacity, int offset)
            : this(accessor, capacity)
        {
            this.offset = offset;
        }

        public override ByteBuffer Slice()
        {
            var buffer = new MemoryMappedFileByteBuffer(accessor, Remaining, offset + position);
            buffer.order = this.order;
            return buffer;
        }

        public override ByteBuffer Duplicate()
        {
            var buffer = new MemoryMappedFileByteBuffer(accessor, Capacity, offset);
            buffer.mark = this.mark;
            buffer.position = this.Position;
            buffer.limit = this.Limit;
            return buffer;
        }

        public override ByteBuffer AsReadOnlyBuffer()
        {
            throw new NotImplementedException();
        }

        private int Ix(int i)
        {
            return i + offset;
        }

        public override byte Get()
        {
            return accessor.ReadByte(Ix(NextGetIndex()));
        }

        public override byte Get(int index)
        {
            return accessor.ReadByte(Ix(CheckIndex(index)));
        }

#if !NETSTANDARD
        // Implementation provided by Vincent Van Den Berghe: http://git.net/ml/general/2017-02/msg31639.html
        public override ByteBuffer Get(byte[] dst, int offset, int length)
        {
            if ((offset | length | (offset + length) | (dst.Length - (offset + length))) < 0)
            {
                throw new IndexOutOfRangeException();
            }
            if (length > Remaining)
            {
                throw new BufferUnderflowException();
            }
            // we need to check for 0-length reads, since 
            // ReadArray will throw an ArgumentOutOfRange exception if position is at
            // the end even when nothing is read
            if (length > 0)
            {
                accessor.ReadArray(Ix(NextGetIndex(length)), dst, offset, length);
            }

            return this;
        }
#endif

        public override bool IsDirect
        {
            get { return false; }
        }

        public override bool IsReadOnly
        {
            get { return true; }
        }

        public override ByteBuffer Put(byte b)
        {
            throw new NotSupportedException();
        }

        public override ByteBuffer Put(int index, byte b)
        {
            throw new NotSupportedException();
        }

        public override ByteBuffer Compact()
        {
            throw new NotSupportedException();
        }

        public override char GetChar()
        {
            return (char)GetInt16();
        }

        public override char GetChar(int index)
        {
            return (char)GetInt16(index);
        }

        public override ByteBuffer PutChar(char value)
        {
            throw new NotSupportedException();
        }

        public override ByteBuffer PutChar(int index, char value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// NOTE: This was getShort() in the JDK
        /// </summary>
        public override short GetInt16()
        {
            return LoadInt16(NextGetIndex(2));
        }

        /// <summary>
        /// NOTE: This was getShort() in the JDK
        /// </summary>
        public override short GetInt16(int index)
        {
            return LoadInt16(CheckIndex(index, 2));
        }

        /// <summary>
        /// NOTE: This was putShort() in the JDK
        /// </summary>
        public override ByteBuffer PutInt16(short value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// NOTE: This was putShort() in the JDK
        /// </summary>
        public override ByteBuffer PutInt16(int index, short value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// NOTE: This was getInt() in the JDK
        /// </summary>
        public override int GetInt32()
        {
            return LoadInt32(NextGetIndex(4));
        }

        /// <summary>
        /// NOTE: This was getInt() in the JDK
        /// </summary>
        public override int GetInt32(int index)
        {
            return LoadInt32(CheckIndex(index, 4));
        }

        /// <summary>
        /// NOTE: This was putInt() in the JDK
        /// </summary>
        public override ByteBuffer PutInt32(int value)
        {
            throw new NotSupportedException();
        }


        /// <summary>
        /// NOTE: This was putInt() in the JDK
        /// </summary>
        public override ByteBuffer PutInt32(int index, int value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// NOTE: This was getLong() in the JDK
        /// </summary>
        public override long GetInt64()
        {
            return LoadInt64(NextGetIndex(8));
        }

        /// <summary>
        /// NOTE: This was getLong() in the JDK
        /// </summary>
        public override long GetInt64(int index)
        {
            return LoadInt64(CheckIndex(index, 8));
        }

        /// <summary>
        /// NOTE: This was putLong() in the JDK
        /// </summary>
        public override ByteBuffer PutInt64(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// NOTE: This was putLong() in the JDK
        /// </summary>
        public override ByteBuffer PutInt64(int index, long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// NOTE: This was getFloat() in the JDK
        /// </summary>
        public override float GetSingle()
        {
            if (order == Endianness.BIG_ENDIAN)
            {
                byte[] bytes = new byte[4];
                for (int i = 3; i >= 0; i--)
                {
                    bytes[i] = accessor.ReadByte(Ix(NextGetIndex()));
                }
                return BitConverter.ToSingle(bytes, 0);
            }
            else
            {
                // LUCENENET NOTE: This assumes that .NET core will continue
                // making the format little endian on big endian platforms
                return accessor.ReadSingle(Ix(NextGetIndex(4)));
            }
        }

        /// <summary>
        /// NOTE: This was getFloat() in the JDK
        /// </summary>
        public override float GetSingle(int index)
        {
            if (order == Endianness.BIG_ENDIAN)
            {
                byte[] bytes = new byte[4];
                for (int i = 3; i >= 0; i--)
                {
                    bytes[i] = accessor.ReadByte(Ix(CheckIndex(index)));
                }
                return BitConverter.ToSingle(bytes, 0);
            }
            else
            {
                // LUCENENET NOTE: This assumes that .NET core will continue
                // making the format little endian on big endian platforms
                return accessor.ReadSingle(Ix(CheckIndex(index, 4)));
            }
        }

        /// <summary>
        /// NOTE: This was putFloat() in the JDK
        /// </summary>
        public override ByteBuffer PutSingle(float value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// NOTE: This was putFloat() in the JDK
        /// </summary>
        public override ByteBuffer PutSingle(int index, float value)
        {
            throw new NotSupportedException();
        }

        public override double GetDouble()
        {
            return BitConverter.Int64BitsToDouble(GetInt64());
        }

        public override double GetDouble(int index)
        {
            return BitConverter.Int64BitsToDouble(GetInt64(index));
        }

        public override ByteBuffer PutDouble(double value)
        {
            throw new NotSupportedException();
        }

        public override ByteBuffer PutDouble(int index, double value)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            if (accessor != null)
            {
                accessor.Dispose();
                accessor = null;
            }
        }

        /// <summary>
        /// NOTE: This was asLongBuffer() in the JDK
        /// </summary>
        public override Int64Buffer AsInt64Buffer()
        {
            throw new NotSupportedException();
        }

        protected override byte[] ProtectedArray
        {
            get { throw new NotSupportedException(); }
        }

        protected override int ProtectedArrayOffset
        {
            get { throw new NotSupportedException(); }
        }

        protected override bool ProtectedHasArray
        {
            get { return false; }
        }

        /// <summary>
        /// Checks the current position against the limit, throwing a
        /// <see cref="BufferUnderflowException"/> if it is not smaller than the limit, and then
        /// increments the position.
        /// </summary>
        /// <returns>The current position value, before it is incremented</returns>
        internal int NextGetIndex()
        {
            if (position >= limit)
            {
                throw new BufferUnderflowException();
            }
            return position++;
        }

        internal int NextGetIndex(int nb)
        {
            if (limit - position < nb)
            {
                throw new BufferUnderflowException();
            }
            int p = position;
            position += nb;
            return p;
        }

        /// <summary>
        /// Checks the current position against the limit, throwing a <see cref="BufferOverflowException"/>
        /// if it is not smaller than the limit, and then
        /// increments the position.
        /// </summary>
        /// <returns>The current position value, before it is incremented</returns>
        internal int NextPutIndex()
        {
            if (position >= limit)
            {
                throw new BufferOverflowException();
            }
            return position++;
        }

        internal int NextPutIndex(int nb)
        {
            if (limit - position < nb)
            {
                throw new BufferOverflowException();
            }
            int p = position;
            position += nb;
            return p;
        }

        /// <summary>
        /// Checks the given index against the limit, throwing an <see cref="IndexOutOfRangeException"/> 
        /// if it is not smaller than the limit or is smaller than zero.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        internal int CheckIndex(int i)
        {
            if ((i < 0) || (i >= limit))
            {
                throw new IndexOutOfRangeException();
            }
            return i;
        }

        internal int CheckIndex(int i, int nb)
        {
            if ((i < 0) || (nb > limit - i))
            {
                throw new IndexOutOfRangeException();
            }
            return i;
        }

        private int LoadInt32(int index)
        {
            int baseOffset = offset + index;
            int bytes = 0;
            if (order == Endianness.BIG_ENDIAN)
            {
                for (int i = 0; i < 4; i++)
                {
                    bytes = bytes << 8;
                    bytes = bytes | (accessor.ReadByte(baseOffset + i) & 0xFF);
                }
            }
            else
            {
                for (int i = 3; i >= 0; i--)
                {
                    bytes = bytes << 8;
                    bytes = bytes | (accessor.ReadByte(baseOffset + i) & 0xFF);
                }
            }
            return bytes;
        }

        private long LoadInt64(int index)
        {
            int baseOffset = offset + index;
            long bytes = 0;
            if (order == Endianness.BIG_ENDIAN)
            {
                for (int i = 0; i < 8; i++)
                {
                    bytes = bytes << 8;
                    bytes = bytes | (uint)(accessor.ReadByte(baseOffset + i) & 0xFF);
                }
            }
            else
            {
                for (int i = 7; i >= 0; i--)
                {
                    bytes = bytes << 8;
                    bytes = bytes | (uint)(accessor.ReadByte(baseOffset + i) & 0xFF);
                }
            }
            return bytes;
        }

        private short LoadInt16(int index)
        {
            int baseOffset = offset + index;
            short bytes = 0;
            if (order == Endianness.BIG_ENDIAN)
            {
                bytes = (short)(accessor.ReadByte(baseOffset) << 8);
                bytes |= (short)(accessor.ReadByte(baseOffset + 1) & 0xFF);
            }
            else
            {
                bytes = (short)(accessor.ReadByte(baseOffset + 1) << 8);
                bytes |= (short)(accessor.ReadByte(baseOffset) & 0xFF);
            }
            return bytes;
        }
    }
}
