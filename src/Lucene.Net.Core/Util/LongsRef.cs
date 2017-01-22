using Lucene.Net.Support;
using System;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Util
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
    /// Represents long[], as a slice (offset + length) into an
    ///  existing long[].  The <seealso cref="#longs"/> member should never be null; use
    ///  <seealso cref="#EMPTY_LONGS"/> if necessary.
    ///
    ///  @lucene.internal
    /// </summary>
    public sealed class LongsRef : IComparable<LongsRef> // LUCENENET TODO: Rename Int64sRef ?
    {
        /// <summary>
        /// An empty long array for convenience </summary>
        public static readonly long[] EMPTY_LONGS = new long[0];

        /// <summary>
        /// The contents of the LongsRef. Should never be {@code null}. </summary>
        [WritableArray]
        public long[] Longs
        {
            get { return longs; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                longs = value;
            }
        }
        private long[] longs;

        /// <summary>
        /// Offset of first valid long. </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Length of used longs. </summary>
        public int Length { get; set; }

        /// <summary>
        /// Create a LongsRef with <seealso cref="#EMPTY_LONGS"/> </summary>
        public LongsRef()
        {
            longs = EMPTY_LONGS;
        }

        /// <summary>
        /// Create a LongsRef pointing to a new array of size <code>capacity</code>.
        /// Offset and length will both be zero.
        /// </summary>
        public LongsRef(int capacity)
        {
            longs = new long[capacity];
        }

        /// <summary>
        /// this instance will directly reference longs w/o making a copy.
        /// longs should not be null
        /// </summary>
        public LongsRef(long[] longs, int offset, int length)
        {
            this.longs = longs;
            this.Offset = offset;
            this.Length = length;
            Debug.Assert(IsValid());
        }

        /// <summary>
        /// Returns a shallow clone of this instance (the underlying longs are
        /// <b>not</b> copied and will be shared by both the returned object and this
        /// object.
        /// </summary>
        /// <seealso cref= #deepCopyOf </seealso>
        public object Clone()
        {
            return new LongsRef(longs, Offset, Length);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 0;
            long end = Offset + Length;
            for (int i = Offset; i < end; i++)
            {
                result = prime * result + (int)(longs[i] ^ ((long)((ulong)longs[i] >> 32)));
            }
            return result;
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }
            if (other is LongsRef)
            {
                return this.LongsEquals((LongsRef)other);
            }
            return false;
        }

        public bool LongsEquals(LongsRef other)
        {
            if (Length == other.Length)
            {
                int otherUpto = other.Offset;
                long[] otherInts = other.longs;
                long end = Offset + Length;
                for (int upto = Offset; upto < end; upto++, otherUpto++)
                {
                    if (longs[upto] != otherInts[otherUpto])
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Signed int order comparison </summary>
        public int CompareTo(LongsRef other)
        {
            if (this == other)
            {
                return 0;
            }

            long[] aInts = this.longs;
            int aUpto = this.Offset;
            long[] bInts = other.longs;
            int bUpto = other.Offset;

            long aStop = aUpto + Math.Min(this.Length, other.Length);

            while (aUpto < aStop)
            {
                long aInt = aInts[aUpto++];
                long bInt = bInts[bUpto++];
                if (aInt > bInt)
                {
                    return 1;
                }
                else if (aInt < bInt)
                {
                    return -1;
                }
            }

            // One is a prefix of the other, or, they are equal:
            return this.Length - other.Length;
        }

        public void CopyLongs(LongsRef other)
        {
            if (longs.Length - Offset < other.Length)
            {
                longs = new long[other.Length];
                Offset = 0;
            }
            Array.Copy(other.longs, other.Offset, longs, Offset, other.Length);
            Length = other.Length;
        }

        /// <summary>
        /// Used to grow the reference array.
        ///
        /// In general this should not be used as it does not take the offset into account.
        /// @lucene.internal
        /// </summary>
        public void Grow(int newLength)
        {
            Debug.Assert(Offset == 0);
            if (longs.Length < newLength)
            {
                longs = ArrayUtil.Grow(longs, newLength);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            long end = Offset + Length;
            for (int i = Offset; i < end; i++)
            {
                if (i > Offset)
                {
                    sb.Append(' ');
                }
                sb.Append(longs[i].ToString("x"));
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Creates a new IntsRef that points to a copy of the longs from
        /// <code>other</code>
        /// <p>
        /// The returned IntsRef will have a length of other.length
        /// and an offset of zero.
        /// </summary>
        public static LongsRef DeepCopyOf(LongsRef other)
        {
            LongsRef clone = new LongsRef();
            clone.CopyLongs(other);
            return clone;
        }

        /// <summary>
        /// Performs internal consistency checks.
        /// Always returns true (or throws InvalidOperationException)
        /// </summary>
        public bool IsValid()
        {
            if (longs == null)
            {
                throw new InvalidOperationException("longs is null");
            }
            if (Length < 0)
            {
                throw new InvalidOperationException("length is negative: " + Length);
            }
            if (Length > longs.Length)
            {
                throw new InvalidOperationException("length is out of bounds: " + Length + ",longs.length=" + longs.Length);
            }
            if (Offset < 0)
            {
                throw new InvalidOperationException("offset is negative: " + Offset);
            }
            if (Offset > longs.Length)
            {
                throw new InvalidOperationException("offset out of bounds: " + Offset + ",longs.length=" + longs.Length);
            }
            if (Offset + Length < 0)
            {
                throw new InvalidOperationException("offset+length is negative: offset=" + Offset + ",length=" + Length);
            }
            if (Offset + Length > longs.Length)
            {
                throw new InvalidOperationException("offset+length out of bounds: offset=" + Offset + ",length=" + Length + ",longs.length=" + longs.Length);
            }
            return true;
        }
    }
}