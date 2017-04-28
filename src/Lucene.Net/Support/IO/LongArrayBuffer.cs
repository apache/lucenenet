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
    /// LongArrayBuffer implements all the shared readonly methods and is extended by
    /// the other two classes.
    /// <para/>
    /// All methods are marked final for runtime performance.
    /// </summary>
    internal abstract class Int64ArrayBuffer : Int64Buffer
    {
        protected internal readonly long[] backingArray;

        protected internal readonly int offset;

        internal Int64ArrayBuffer(long[] array)
                : this(array.Length, array, 0)
        {
        }

        internal Int64ArrayBuffer(int capacity)
                : this(capacity, new long[capacity], 0)
        {
        }

        internal Int64ArrayBuffer(int capacity, long[] backingArray, int offset)
            : base(capacity)
        {
            this.backingArray = backingArray;
            this.offset = offset;
        }


        public override sealed long Get()
        {
            if (position == limit)
            {
                throw new BufferUnderflowException();
            }
            return backingArray[offset + position++];
        }


        public override sealed long Get(int index)
        {
            if (index < 0 || index >= limit)
            {
                throw new IndexOutOfRangeException();
            }
            return backingArray[offset + index];
        }


        public override sealed Int64Buffer Get(long[] dest, int off, int len)
        {
            int length = dest.Length;
            if (off < 0 || len < 0 || (long)len + (long)off > length)
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


        public override sealed bool IsDirect
        {
            get { return false; }
        }


        public override sealed ByteOrder Order
        {
            get { return ByteOrder.NativeOrder; }
        }
    }
}
