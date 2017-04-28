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
        private bool bigEndian = true; //!BitConverter.IsLittleEndian;

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

        //public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int mark, int pos, int lim, int cap)
        //    : base(mark, pos, lim, cap)
        //{
        //    _accessor = accessor;
        //}

        //public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int mark, int pos, int lim, int cap, int offset)
        //    : this(accessor, mark, pos, lim, cap)
        //{
        //    this.offset = offset;
        //}

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
            get { return false; }
        }

        public override ByteBuffer Put(byte b)
        {
            accessor.Write(Ix(NextPutIndex()), b);
            return this;
        }

        public override ByteBuffer Put(int index, byte b)
        {
            accessor.Write(Ix(CheckIndex(index)), b);
            return this;
        }

#if !NETSTANDARD
        // Implementation provided by Vincent Van Den Berghe: http://git.net/ml/general/2017-02/msg31639.html
        public override ByteBuffer Put(byte[] src, int offset, int length)
        {
            if ((offset | length | (offset + length) | (src.Length - (offset + length))) < 0)
            {
                throw new IndexOutOfRangeException();
            }
            if (length > Remaining)
            {
                throw new BufferOverflowException();
            }
            // we need to check for 0-length writes, since 
            // ReadArray will throw an ArgumentOutOfRange exception if position is at 
            // the end even when nothing is read
            if (length > 0)
            {
                accessor.WriteArray(Ix(NextPutIndex(length)), src, offset, length);
            }
            return this;
        }
#endif

        public override ByteBuffer Compact()
        {
            throw new NotSupportedException();
        }


        public override char GetChar()
        {
            var littleEndian = accessor.ReadChar(Ix(NextGetIndex(2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override char GetChar(int index)
        {
            var littleEndian = accessor.ReadChar(Ix(CheckIndex(index, 2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override ByteBuffer PutChar(char value)
        {
            accessor.Write(Ix(NextPutIndex(2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }



        public override ByteBuffer PutChar(int index, char value)
        {
            accessor.Write(Ix(CheckIndex(index, 2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was getShort() in the JDK
        /// </summary>
        public override short GetInt16()
        {
            var littleEndian = accessor.ReadInt16(Ix(NextGetIndex(2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was getShort() in the JDK
        /// </summary>
        public override short GetInt16(int index)
        {
            var littleEndian = accessor.ReadInt16(Ix(CheckIndex(index, 2)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was putShort() in the JDK
        /// </summary>
        public override ByteBuffer PutInt16(short value)
        {
            accessor.Write(Ix(NextPutIndex(2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was putShort() in the JDK
        /// </summary>
        public override ByteBuffer PutInt16(int index, short value)
        {
            accessor.Write(Ix(CheckIndex(index, 2)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was getInt() in the JDK
        /// </summary>
        public override int GetInt32()
        {
            var littleEndian = accessor.ReadInt32(Ix(NextGetIndex(4)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was getInt() in the JDK
        /// </summary>
        public override int GetInt32(int index)
        {
            var littleEndian = accessor.ReadInt32(Ix(CheckIndex(index, 4)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was putInt() in the JDK
        /// </summary>
        public override ByteBuffer PutInt32(int value)
        {
            accessor.Write(Ix(NextPutIndex(4)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }


        /// <summary>
        /// NOTE: This was putInt() in the JDK
        /// </summary>
        public override ByteBuffer PutInt32(int index, int value)
        {
            accessor.Write(Ix(CheckIndex(index, 4)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was getLong() in the JDK
        /// </summary>
        public override long GetInt64()
        {
            var littleEndian = accessor.ReadInt64(Ix(NextGetIndex(8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was getLong() in the JDK
        /// </summary>
        public override long GetInt64(int index)
        {
            var littleEndian = accessor.ReadInt64(Ix(CheckIndex(index, 8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        /// <summary>
        /// NOTE: This was putLong() in the JDK
        /// </summary>
        public override ByteBuffer PutInt64(long value)
        {
            accessor.Write(Ix(NextPutIndex(8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was putLong() in the JDK
        /// </summary>
        public override ByteBuffer PutInt64(int index, long value)
        {
            accessor.Write(Ix(CheckIndex(index, 8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        /// <summary>
        /// NOTE: This was getFloat() in the JDK
        /// </summary>
        public override float GetSingle()
        {
            byte[] temp = new byte[4];
            temp[0] = accessor.ReadByte(Ix(NextGetIndex()));
            temp[1] = accessor.ReadByte(Ix(NextGetIndex()));
            temp[2] = accessor.ReadByte(Ix(NextGetIndex()));
            temp[3] = accessor.ReadByte(Ix(NextGetIndex()));
            if (bigEndian)
            {
                System.Array.Reverse(temp);
            }
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>
        /// NOTE: This was getFloat() in the JDK
        /// </summary>
        public override float GetSingle(int index)
        {
            byte[] temp = new byte[4];
            temp[0] = accessor.ReadByte(Ix(NextGetIndex(index)));
            temp[1] = accessor.ReadByte(Ix(NextGetIndex()));
            temp[2] = accessor.ReadByte(Ix(NextGetIndex()));
            temp[3] = accessor.ReadByte(Ix(NextGetIndex()));
            if (bigEndian)
            {
                System.Array.Reverse(temp);
            }
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>
        /// NOTE: This was putFloat() in the JDK
        /// </summary>
        public override ByteBuffer PutSingle(float value)
        {
            var bytes = BitConverter.GetBytes(value);

            if (bigEndian)
            {
                System.Array.Reverse(bytes);
            }

            accessor.Write(Ix(NextPutIndex()), bytes[0]);
            accessor.Write(Ix(NextPutIndex()), bytes[1]);
            accessor.Write(Ix(NextPutIndex()), bytes[2]);
            accessor.Write(Ix(NextPutIndex()), bytes[3]);
            return this;
        }

        /// <summary>
        /// NOTE: This was putFloat() in the JDK
        /// </summary>
        public override ByteBuffer PutSingle(int index, float value)
        {
            var bytes = BitConverter.GetBytes(value);

            if (bigEndian)
            {
                System.Array.Reverse(bytes);
            }

            accessor.Write(Ix(NextPutIndex(index)), bytes[0]);
            accessor.Write(Ix(NextPutIndex()), bytes[1]);
            accessor.Write(Ix(NextPutIndex()), bytes[2]);
            accessor.Write(Ix(NextPutIndex()), bytes[3]);
            return this;
        }

        public override double GetDouble()
        {
            var littleEndian = accessor.ReadDouble(Ix(NextGetIndex(8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override double GetDouble(int index)
        {
            var littleEndian = accessor.ReadDouble(Ix(CheckIndex(index, 8)));
            if (bigEndian)
            {
                return Number.FlipEndian(littleEndian);
            }
            return littleEndian;
        }

        public override ByteBuffer PutDouble(double value)
        {
            accessor.Write(Ix(NextPutIndex(8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public override ByteBuffer PutDouble(int index, double value)
        {
            accessor.Write(Ix(CheckIndex(index, 8)), bigEndian ? Number.FlipEndian(value) : value);
            return this;
        }

        public void Dispose()
        {
            if (accessor != null)
                accessor.Dispose();

            accessor = null;
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
            int p = position++;
            UnsetMarkIfNecessary();
            return p;
        }

        internal int NextGetIndex(int nb)
        {
            if (limit - position < nb)
            {
                throw new BufferUnderflowException();
            }
            int p = position;
            position += nb;
            UnsetMarkIfNecessary();
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
            int p = position++;
            UnsetMarkIfNecessary();
            return p;
        }

        internal int NextPutIndex(int nb)
        {
            if (limit - position < nb)
            {
                throw new BufferOverflowException();
            }
            int p = position;
            position += nb;
            UnsetMarkIfNecessary();
            return p;
        }

        internal void UnsetMarkIfNecessary()
        {
            if ((mark != UNSET_MARK) && (mark > position))
            {
                mark = UNSET_MARK;
            }
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
    }
}



//using Lucene.Net.Support.IO;
//using System;
//using System.IO.MemoryMappedFiles;

//namespace Lucene.Net.Support
//{
//    /*
//	 * Licensed to the Apache Software Foundation (ASF) under one or more
//	 * contributor license agreements.  See the NOTICE file distributed with
//	 * this work for additional information regarding copyright ownership.
//	 * The ASF licenses this file to You under the Apache License, Version 2.0
//	 * (the "License"); you may not use this file except in compliance with
//	 * the License.  You may obtain a copy of the License at
//	 *
//	 *     http://www.apache.org/licenses/LICENSE-2.0
//	 *
//	 * Unless required by applicable law or agreed to in writing, software
//	 * distributed under the License is distributed on an "AS IS" BASIS,
//	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	 * See the License for the specific language governing permissions and
//	 * limitations under the License.
//	 */

//    internal sealed class MemoryMappedFileByteBuffer : ByteBuffer, IDisposable
//    {
//        private MemoryMappedViewAccessor _accessor;
//        private readonly int offset;
//        new private bool bigEndian = true;

//        public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int mark, int pos, int lim, int cap)
//            : base(mark, pos, lim, cap)
//        {
//            _accessor = accessor;
//        }

//        public MemoryMappedFileByteBuffer(MemoryMappedViewAccessor accessor, int mark, int pos, int lim, int cap, int offset)
//            : this(accessor, mark, pos, lim, cap)
//        {
//            this.offset = offset;
//        }

//        public override ByteBuffer Slice()
//        {
//            return new MemoryMappedFileByteBuffer(_accessor, -1, 0, Remaining, Remaining);
//        }

//        public override ByteBuffer Duplicate()
//        {
//            return new MemoryMappedFileByteBuffer(_accessor, MarkValue, Position, Limit, Capacity);
//        }

//        public override ByteBuffer AsReadOnlyBuffer()
//        {
//            throw new NotImplementedException();
//        }


//        private int Ix(int i)
//        {
//            return i + offset;
//        }

//        public override byte Get()
//        {
//            return _accessor.ReadByte(Ix(NextGetIndex()));
//        }

//        public override byte Get(int index)
//        {
//            return _accessor.ReadByte(Ix(CheckIndex(index)));
//        }

//#if !NETSTANDARD
//        // Implementation provided by Vincent Van Den Berghe: http://git.net/ml/general/2017-02/msg31639.html
//        public override ByteBuffer Get(byte[] dst, int offset, int length)
//        {
//            CheckBounds(offset, length, dst.Length);
//            if (length > Remaining)
//            {
//                throw new BufferUnderflowException();
//            }
//            // we need to check for 0-length reads, since 
//            // ReadArray will throw an ArgumentOutOfRange exception if position is at
//            // the end even when nothing is read
//            if (length > 0)
//            {
//                _accessor.ReadArray(Ix(NextGetIndex(length)), dst, offset, length);
//            }

//            return this;
//        }
//#endif

//        public override bool IsDirect
//        {
//            get { return false; }
//        }

//        public override bool IsReadOnly
//        {
//            get { return false; }
//        }

//        public override ByteBuffer Put(byte b)
//        {
//            _accessor.Write(Ix(NextPutIndex()), b);
//            return this;
//        }

//        public override ByteBuffer Put(int index, byte b)
//        {
//            _accessor.Write(Ix(CheckIndex(index)), b);
//            return this;
//        }

//#if !NETSTANDARD
//        // Implementation provided by Vincent Van Den Berghe: http://git.net/ml/general/2017-02/msg31639.html
//        public override ByteBuffer Put(byte[] src, int offset, int length)
//        {
//            CheckBounds(offset, length, src.Length);
//            if (length > Remaining)
//            {
//                throw new BufferOverflowException();
//            }
//            // we need to check for 0-length writes, since 
//            // ReadArray will throw an ArgumentOutOfRange exception if position is at 
//            // the end even when nothing is read
//            if (length > 0)
//            {
//                _accessor.WriteArray(Ix(NextPutIndex(length)), src, offset, length);
//            }
//            return this;
//        }
//#endif

//        public override ByteBuffer Compact()
//        {
//            throw new NotSupportedException();
//        }

//        internal override byte _get(int i)
//        {
//            throw new NotSupportedException();
//        }

//        internal override void _put(int i, byte b)
//        {
//            throw new NotSupportedException();
//        }


//        public override char GetChar()
//        {
//            var littleEndian = _accessor.ReadChar(Ix(NextGetIndex(2)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        public override char GetChar(int index)
//        {
//            var littleEndian = _accessor.ReadChar(Ix(CheckIndex(index, 2)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        public override ByteBuffer PutChar(char value)
//        {
//            _accessor.Write(Ix(NextPutIndex(2)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }



//        public override ByteBuffer PutChar(int index, char value)
//        {
//            _accessor.Write(Ix(CheckIndex(index, 2)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }

//        /// <summary>
//        /// NOTE: This was getShort() in the JDK
//        /// </summary>
//        public override short GetInt16()
//        {
//            var littleEndian = _accessor.ReadInt16(Ix(NextGetIndex(2)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        /// <summary>
//        /// NOTE: This was getShort() in the JDK
//        /// </summary>
//        public override short GetInt16(int index)
//        {
//            var littleEndian = _accessor.ReadInt16(Ix(CheckIndex(index, 2)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        /// <summary>
//        /// NOTE: This was putShort() in the JDK
//        /// </summary>
//        public override ByteBuffer PutInt16(short value)
//        {
//            _accessor.Write(Ix(NextPutIndex(2)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }

//        /// <summary>
//        /// NOTE: This was putShort() in the JDK
//        /// </summary>
//        public override ByteBuffer PutInt16(int index, short value)
//        {
//            _accessor.Write(Ix(CheckIndex(index, 2)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }

//        /// <summary>
//        /// NOTE: This was getInt() in the JDK
//        /// </summary>
//        public override int GetInt32()
//        {
//            var littleEndian = _accessor.ReadInt32(Ix(NextGetIndex(4)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        /// <summary>
//        /// NOTE: This was getInt() in the JDK
//        /// </summary>
//        public override int GetInt32(int index)
//        {
//            var littleEndian = _accessor.ReadInt32(Ix(CheckIndex(index, 4)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        /// <summary>
//        /// NOTE: This was putInt() in the JDK
//        /// </summary>
//        public override ByteBuffer PutInt32(int value)
//        {
//            _accessor.Write(Ix(NextPutIndex(4)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }


//        /// <summary>
//        /// NOTE: This was putInt() in the JDK
//        /// </summary>
//        public override ByteBuffer PutInt32(int index, int value)
//        {
//            _accessor.Write(Ix(CheckIndex(index, 4)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }

//        /// <summary>
//        /// NOTE: This was getLong() in the JDK
//        /// </summary>
//        public override long GetInt64()
//        {
//            var littleEndian = _accessor.ReadInt64(Ix(NextGetIndex(8)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        /// <summary>
//        /// NOTE: This was getLong() in the JDK
//        /// </summary>
//        public override long GetInt64(int index)
//        {
//            var littleEndian = _accessor.ReadInt64(Ix(CheckIndex(index, 8)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        /// <summary>
//        /// NOTE: This was putLong() in the JDK
//        /// </summary>
//        public override ByteBuffer PutInt64(long value)
//        {
//            _accessor.Write(Ix(NextPutIndex(8)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }

//        /// <summary>
//        /// NOTE: This was putLong() in the JDK
//        /// </summary>
//        public override ByteBuffer PutInt64(int index, long value)
//        {
//            _accessor.Write(Ix(CheckIndex(index, 8)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }

//        /// <summary>
//        /// NOTE: This was getFloat() in the JDK
//        /// </summary>
//        public override float GetSingle()
//        {
//            byte[] temp = new byte[4];
//            temp[0] = _accessor.ReadByte(Ix(NextGetIndex()));
//            temp[1] = _accessor.ReadByte(Ix(NextGetIndex()));
//            temp[2] = _accessor.ReadByte(Ix(NextGetIndex()));
//            temp[3] = _accessor.ReadByte(Ix(NextGetIndex()));
//            if (bigEndian)
//            {
//                System.Array.Reverse(temp);
//            }
//            return BitConverter.ToSingle(temp, 0);
//        }

//        /// <summary>
//        /// NOTE: This was getFloat() in the JDK
//        /// </summary>
//        public override float GetSingle(int index)
//        {
//            byte[] temp = new byte[4];
//            temp[0] = _accessor.ReadByte(Ix(NextGetIndex(index)));
//            temp[1] = _accessor.ReadByte(Ix(NextGetIndex()));
//            temp[2] = _accessor.ReadByte(Ix(NextGetIndex()));
//            temp[3] = _accessor.ReadByte(Ix(NextGetIndex()));
//            if (bigEndian)
//            {
//                System.Array.Reverse(temp);
//            }
//            return BitConverter.ToSingle(temp, 0);
//        }

//        /// <summary>
//        /// NOTE: This was putFloat() in the JDK
//        /// </summary>
//        public override ByteBuffer PutSingle(float value)
//        {
//            var bytes = BitConverter.GetBytes(value);

//            if (bigEndian)
//            {
//                System.Array.Reverse(bytes);
//            }

//            _accessor.Write(Ix(NextPutIndex()), bytes[0]);
//            _accessor.Write(Ix(NextPutIndex()), bytes[1]);
//            _accessor.Write(Ix(NextPutIndex()), bytes[2]);
//            _accessor.Write(Ix(NextPutIndex()), bytes[3]);
//            return this;
//        }

//        /// <summary>
//        /// NOTE: This was putFloat() in the JDK
//        /// </summary>
//        public override ByteBuffer PutSingle(int index, float value)
//        {
//            var bytes = BitConverter.GetBytes(value);

//            if (bigEndian)
//            {
//                System.Array.Reverse(bytes);
//            }

//            _accessor.Write(Ix(NextPutIndex(index)), bytes[0]);
//            _accessor.Write(Ix(NextPutIndex()), bytes[1]);
//            _accessor.Write(Ix(NextPutIndex()), bytes[2]);
//            _accessor.Write(Ix(NextPutIndex()), bytes[3]);
//            return this;
//        }

//        public override double GetDouble()
//        {
//            var littleEndian = _accessor.ReadDouble(Ix(NextGetIndex(8)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        public override double GetDouble(int index)
//        {
//            var littleEndian = _accessor.ReadDouble(Ix(CheckIndex(index, 8)));
//            if (bigEndian)
//            {
//                return Number.FlipEndian(littleEndian);
//            }
//            return littleEndian;
//        }

//        public override ByteBuffer PutDouble(double value)
//        {
//            _accessor.Write(Ix(NextPutIndex(8)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }

//        public override ByteBuffer PutDouble(int index, double value)
//        {
//            _accessor.Write(Ix(CheckIndex(index, 8)), bigEndian ? Number.FlipEndian(value) : value);
//            return this;
//        }

//        public void Dispose()
//        {
//            if (_accessor != null)
//                _accessor.Dispose();

//            _accessor = null;
//        }

//        /// <summary>
//        /// NOTE: This was asLongBuffer() in the JDK
//        /// </summary>
//        public override Int64Buffer AsInt64Buffer()
//        {
//            throw new NotSupportedException();
//        }
//    }
//}