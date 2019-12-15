// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;

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
    /// HeapByteBuffer, ReadWriteHeapByteBuffer and ReadOnlyHeapByteBuffer compose
    /// the implementation of array based byte buffers.
    /// <para/>
    /// HeapByteBuffer implements all the shared readonly methods and is extended by
    /// the other two classes.
    /// <para/>
    /// All methods are sealed for runtime performance.
    /// </summary>
    internal abstract class HeapByteBuffer : ByteBuffer
    {
        protected internal readonly byte[] backingArray;

        protected internal readonly int offset;

        internal HeapByteBuffer(byte[] backingArray)
            : this(backingArray, backingArray.Length, 0)
        {
        }

        internal HeapByteBuffer(int capacity)
            : this(new byte[capacity], capacity, 0)
        {
        }

        internal HeapByteBuffer(byte[] backingArray, int capacity, int offset)
            : base(capacity)
        {
            this.backingArray = backingArray;
            this.offset = offset;

            if (offset + capacity > backingArray.Length)
            {
                throw new IndexOutOfRangeException();
            }
        }

        /*
         * Override ByteBuffer.get(byte[], int, int) to improve performance.
         * 
         * (non-Javadoc)
         * 
         * @see java.nio.ByteBuffer#get(byte[], int, int)
         */

        public override sealed ByteBuffer Get(byte[] dest, int off, int len)
        {
            int length = dest.Length;
            if (off < 0 || len < 0 || (long)off + (long)len > length)
            {
                throw new IndexOutOfRangeException();
            }
            if (len > Remaining)
            {
                throw new BufferUnderflowException();
            }
            System.Array.Copy(backingArray, offset + position, dest, off, len);
            position += len;
            return this;
        }

        public override sealed byte Get()
        {
            if (position == limit)
            {
                throw new BufferUnderflowException();
            }
            return backingArray[offset + position++];
        }


        public override sealed byte Get(int index)
        {
            if (index < 0 || index >= limit)
            {
                throw new IndexOutOfRangeException();
            }
            return backingArray[offset + index];
        }


        public override sealed double GetDouble()
        {
            return BitConverter.Int64BitsToDouble(GetInt64());
        }


        public override sealed double GetDouble(int index)
        {
            return BitConverter.Int64BitsToDouble(GetInt64(index));
        }


        public override sealed float GetSingle()
        {
            return Number.Int32BitsToSingle(GetInt32());
        }


        public override sealed float GetSingle(int index)
        {
            return Number.Int32BitsToSingle(GetInt32(index));
        }


        public override sealed int GetInt32()
        {
            int newPosition = position + 4;
            if (newPosition > limit)
            {
                throw new BufferUnderflowException();
            }
            int result = LoadInt32(position);
            position = newPosition;
            return result;
        }


        public override sealed int GetInt32(int index)
        {
            if (index < 0 || index + 4 > limit)
            {
                throw new IndexOutOfRangeException();
            }
            return LoadInt32(index);
        }


        public override sealed long GetInt64()
        {
            int newPosition = position + 8;
            if (newPosition > limit)
            {
                throw new BufferUnderflowException();
            }
            long result = LoadInt64(position);
            position = newPosition;
            return result;
        }


        public override sealed long GetInt64(int index)
        {
            if (index < 0 || index + 8 > limit)
            {
                throw new IndexOutOfRangeException();
            }
            return LoadInt64(index);
        }


        public override sealed short GetInt16()
        {
            int newPosition = position + 2;
            if (newPosition > limit)
            {
                throw new BufferUnderflowException();
            }
            short result = LoadInt16(position);
            position = newPosition;
            return result;
        }


        public override sealed short GetInt16(int index)
        {
            if (index < 0 || index + 2 > limit)
            {
                throw new IndexOutOfRangeException();
            }
            return LoadInt16(index);
        }


        public override sealed bool IsDirect
        {
            get { return false; }
        }

        protected int LoadInt32(int index)
        {
            int baseOffset = offset + index;
            int bytes = 0;
            if (order == Endianness.BIG_ENDIAN)
            {
                for (int i = 0; i < 4; i++)
                {
                    bytes = bytes << 8;
                    bytes = bytes | (backingArray[baseOffset + i] & 0xFF);
                }
            }
            else
            {
                for (int i = 3; i >= 0; i--)
                {
                    bytes = bytes << 8;
                    bytes = bytes | (backingArray[baseOffset + i] & 0xFF);
                }
            }
            return bytes;
        }

        protected long LoadInt64(int index)
        {
            int baseOffset = offset + index;
            long bytes = 0;
            if (order == Endianness.BIG_ENDIAN)
            {
                for (int i = 0; i < 8; i++)
                {
                    bytes = bytes << 8;
                    bytes = bytes | (uint)(backingArray[baseOffset + i] & 0xFF);
                }
            }
            else
            {
                for (int i = 7; i >= 0; i--)
                {
                    bytes = bytes << 8;
                    bytes = bytes | (uint)(backingArray[baseOffset + i] & 0xFF);
                }
            }
            return bytes;
        }

        protected short LoadInt16(int index)
        {
            int baseOffset = offset + index;
            short bytes = 0;
            if (order == Endianness.BIG_ENDIAN)
            {
                bytes = (short)(backingArray[baseOffset] << 8);
                bytes |= (short)(backingArray[baseOffset + 1] & 0xFF);
            }
            else
            {
                bytes = (short)(backingArray[baseOffset + 1] << 8);
                bytes |= (short)(backingArray[baseOffset] & 0xFF);
            }
            return bytes;
        }

        protected void Store(int index, int value)
        {
            int baseOffset = offset + index;
            if (order == Endianness.BIG_ENDIAN)
            {
                for (int i = 3; i >= 0; i--)
                {
                    backingArray[baseOffset + i] = (byte)(value & 0xFF);
                    value = value >> 8;
                }
            }
            else
            {
                for (int i = 0; i <= 3; i++)
                {
                    backingArray[baseOffset + i] = (byte)(value & 0xFF);
                    value = value >> 8;
                }
            }
        }

        protected void Store(int index, long value)
        {
            int baseOffset = offset + index;
            if (order == Endianness.BIG_ENDIAN)
            {
                for (int i = 7; i >= 0; i--)
                {
                    backingArray[baseOffset + i] = (byte)(value & 0xFF);
                    value = value >> 8;
                }
            }
            else
            {
                for (int i = 0; i <= 7; i++)
                {
                    backingArray[baseOffset + i] = (byte)(value & 0xFF);
                    value = value >> 8;
                }
            }
        }

        protected void Store(int index, short value)
        {
            int baseOffset = offset + index;
            if (order == Endianness.BIG_ENDIAN)
            {
                backingArray[baseOffset] = (byte)((value >> 8) & 0xFF);
                backingArray[baseOffset + 1] = (byte)(value & 0xFF);
            }
            else
            {
                backingArray[baseOffset + 1] = (byte)((value >> 8) & 0xFF);
                backingArray[baseOffset] = (byte)(value & 0xFF);
            }
        }


        //public override sealed CharBuffer AsCharBuffer()
        //    {
        //        return CharToByteBufferAdapter.Wrap(this);
        //    }


        //public override sealed DoubleBuffer AsDoubleBuffer()
        //    {
        //        return DoubleToByteBufferAdapter.Wrap(this);
        //    }


        //public override sealed SingleBuffer AsSingleBuffer()
        //    {
        //        return SingleToByteBufferAdapter.Wrap(this);
        //    }


        //public override sealed Int32Buffer AsInt32Buffer()
        //    {
        //        return Int32ToByteBufferAdapter.Wrap(this);
        //    }


        //public override sealed Int64Buffer AsInt64Buffer()
        //{
        //    return Int64ToByteBufferAdapter.Wrap(this);
        //}


        //public override sealed Int16Buffer AsInt16Buffer()
        //    {
        //        return Int16ToByteBufferAdapter.Wrap(this);
        //    }


        public override sealed char GetChar()
        {
            return (char)GetInt16();
        }


        public override sealed char GetChar(int index)
        {
            return (char)GetInt16(index);
        }


        public override sealed ByteBuffer PutChar(char value)
        {
            return PutInt16((short)value);
        }


        public override sealed ByteBuffer PutChar(int index, char value)
        {
            return PutInt16(index, (short)value);
        }
    }
}
