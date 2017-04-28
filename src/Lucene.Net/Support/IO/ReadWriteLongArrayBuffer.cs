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
    /// LongArrayBuffer, ReadWriteLongArrayBuffer and ReadOnlyLongArrayBuffer compose
    /// the implementation of array based long buffers.
    /// <para/>
    /// ReadWriteLongArrayBuffer extends LongArrayBuffer with all the write methods.
    /// <para/>
    /// This class is marked final for runtime performance.
    /// </summary>
    internal sealed class ReadWriteInt64ArrayBuffer : Int64ArrayBuffer
    {
        internal static ReadWriteInt64ArrayBuffer Copy(Int64ArrayBuffer other, int markOfOther)
        {
            ReadWriteInt64ArrayBuffer buf = new ReadWriteInt64ArrayBuffer(other
                    .Capacity, other.backingArray, other.offset);
            buf.limit = other.Limit;
            buf.position = other.Position;
            buf.mark = markOfOther;
            return buf;
        }

        internal ReadWriteInt64ArrayBuffer(long[] array)
            : base(array)
        {
        }

        internal ReadWriteInt64ArrayBuffer(int capacity)
            : base(capacity)
        {
        }

        internal ReadWriteInt64ArrayBuffer(int capacity, long[] backingArray, int arrayOffset)
            : base(capacity, backingArray, arrayOffset)
        {
        }


        public override Int64Buffer AsReadOnlyBuffer()
        {
            throw new NotImplementedException();
            //return ReadOnlyLongArrayBuffer.copy(this, mark);
        }

        public override Int64Buffer Compact()
        {
            System.Array.Copy(backingArray, position + offset, backingArray, offset,
                    Remaining);
            position = limit - position;
            limit = capacity;
            mark = UNSET_MARK;
            return this;
        }

        public override Int64Buffer Duplicate()
        {
            return Copy(this, mark);
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        protected override long[] ProtectedArray
        {
            get { return backingArray; }
        }

        protected override int ProtectedArrayOffset
        {
            get { return offset; }
        }

        protected override bool ProtectedHasArray
        {
            get { return true; }
        }

        public override Int64Buffer Put(long c)
        {
            if (position == limit)
            {
                throw new BufferOverflowException();
            }
            backingArray[offset + position++] = c;
            return this;
        }

        public override Int64Buffer Put(int index, long c)
        {
            if (index < 0 || index >= limit)
            {
                throw new IndexOutOfRangeException();
            }
            backingArray[offset + index] = c;
            return this;
        }

        public override Int64Buffer Put(long[] src, int off, int len)
        {
            int length = src.Length;
            if (off < 0 || len < 0 || (long)off + (long)len > length)
            {
                throw new IndexOutOfRangeException();
            }
            if (len > Remaining)
            {
                throw new BufferOverflowException();
            }
            System.Array.Copy(src, off, backingArray, offset + position, len);
            position += len;
            return this;
        }

        public override Int64Buffer Slice()
        {
            return new ReadWriteInt64ArrayBuffer(Remaining, backingArray, offset
                    + position);
        }
    }
}
