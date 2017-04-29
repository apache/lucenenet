// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

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
    /// A buffer of longs.
    /// <para/>
    /// A long buffer can be created in either of the following ways:
    /// <list type="bullet">
    ///     <item><see cref="Allocate(int)"/> a new long array and create a buffer
    ///     based on it</item>
    ///     <item><see cref="Wrap(long[])"/> an existing long array to create a new
    ///     buffer</item>
    /// </list>
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class Int64Buffer : Buffer, IComparable<Int64Buffer>
    {
        /// <summary>
        /// Creates a long buffer based on a newly allocated long array.
        /// </summary>
        /// <param name="capacity">the capacity of the new buffer.</param>
        /// <returns>the created long buffer.</returns>
        /// <exception cref="ArgumentException">if <paramref name="capacity"/> is less than zero.</exception>
        public static Int64Buffer Allocate(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException();
            }
            return new ReadWriteInt64ArrayBuffer(capacity);
        }

        /// <summary>
        /// Creates a new long buffer by wrapping the given long array.
        /// <para/>
        /// Calling this method has the same effect as
        /// <c>Wrap(array, 0, array.Length)</c>.
        /// </summary>
        /// <param name="array">the long array which the new buffer will be based on.</param>
        /// <returns>the created long buffer.</returns>
        public static Int64Buffer Wrap(long[] array)
        {
            return Wrap(array, 0, array != null ? array.Length : 0);
        }

        public static Int64Buffer Wrap(long[] array, int start, int len)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }
            if (start < 0 || len < 0 || (long)len + (long)start > array.Length)
            {
                throw new IndexOutOfRangeException();
            }

            Int64Buffer buf = new ReadWriteInt64ArrayBuffer(array);
            buf.position = start;
            buf.limit = start + len;

            return buf;
        }


        /// <summary>
        /// Constructs a <see cref="Int64Buffer"/> with given capacity.
        /// </summary>
        internal Int64Buffer(int capacity)
            : base(capacity)
        {
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public long[] Array
        {
            get { return ProtectedArray; }
        }

        public int ArrayOffset
        {
            get { return ProtectedArrayOffset; }
        }

        public abstract Int64Buffer AsReadOnlyBuffer();

        public abstract Int64Buffer Compact();

        public int CompareTo(Int64Buffer otherBuffer)
        {
            int compareRemaining = (Remaining < otherBuffer.Remaining) ? Remaining
                : otherBuffer.Remaining;
            int thisPos = position;
            int otherPos = otherBuffer.position;
            long thisByte, otherByte;
            while (compareRemaining > 0)
            {
                thisByte = Get(thisPos);
                otherByte = otherBuffer.Get(otherPos);
                if (thisByte != otherByte)
                {
                    return thisByte < otherByte ? -1 : 1;
                }
                thisPos++;
                otherPos++;
                compareRemaining--;
            }
            return Remaining - otherBuffer.Remaining;
        }

        public abstract Int64Buffer Duplicate();

        public override bool Equals(object other)
        {
            if (!(other is Int64Buffer)) {
                return false;
            }
            Int64Buffer otherBuffer = (Int64Buffer)other;

            if (Remaining != otherBuffer.Remaining)
            {
                return false;
            }

            int myPosition = position;
            int otherPosition = otherBuffer.position;
            bool equalSoFar = true;
            while (equalSoFar && (myPosition < limit))
            {
                equalSoFar = Get(myPosition++) == otherBuffer.Get(otherPosition++);
            }

            return equalSoFar;
        }

        public abstract long Get();

        public virtual Int64Buffer Get(long[] dest)
        {
            return Get(dest, 0, dest.Length);
        }

        public virtual Int64Buffer Get(long[] dest, int off, int len)
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
            for (int i = off; i < off + len; i++)
            {
                dest[i] = Get();
            }
            return this;
        }

        public abstract long Get(int index);

        public bool HasArray
        {
            get { return ProtectedHasArray; }
        }

        public override int GetHashCode()
        {
            int myPosition = position;
            int hash = 0;
            long l;
            while (myPosition < limit)
            {
                l = Get(myPosition++);
                hash = hash + ((int)l) ^ ((int)(l >> 32));
            }
            return hash;
        }

        public abstract bool IsDirect { get; }

        public abstract ByteOrder Order { get; }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        protected abstract long[] ProtectedArray { get; }

        protected abstract int ProtectedArrayOffset { get; }

        protected abstract bool ProtectedHasArray { get; }

        public abstract Int64Buffer Put(long l);

        public Int64Buffer Put(long[] src)
        {
            return Put(src, 0, src.Length);
        }

        public virtual Int64Buffer Put(long[] src, int off, int len)
        {
            int length = src.Length;
            if (off < 0 || len < 0 || (long)len + (long)off > length)
            {
                throw new IndexOutOfRangeException();
            }

            if (len > Remaining)
            {
                throw new BufferOverflowException();
            }
            for (int i = off; i < off + len; i++)
            {
                Put(src[i]);
            }
            return this;
        }

        public virtual Int64Buffer Put(Int64Buffer src)
        {
            if (src == this)
            {
                throw new ArgumentException();
            }
            if (src.Remaining > Remaining)
            {
                throw new BufferOverflowException();
            }
            long[] contents = new long[src.Remaining];
            src.Get(contents);
            Put(contents);
            return this;
        }

        public abstract Int64Buffer Put(int index, long l);

        public abstract Int64Buffer Slice();

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append(GetType().GetTypeInfo().Name);
            buf.Append(", status: capacity="); //$NON-NLS-1$
            buf.Append(Capacity);
            buf.Append(" position="); //$NON-NLS-1$
            buf.Append(Position);
            buf.Append(" limit="); //$NON-NLS-1$
            buf.Append(Limit);
            return buf.ToString();
        }
    }
}