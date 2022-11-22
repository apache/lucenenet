using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    /// Represents <see cref="T:long[]"/>, as a slice (offset + length) into an
    /// existing <see cref="T:long[]"/>.  The <see cref="Int64s"/> member should never be <c>null</c>; use
    /// <see cref="EMPTY_INT64S"/> if necessary.
    /// <para/>
    /// NOTE: This was LongsRef in Lucene
    /// <para/>
    /// @lucene.internal
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public sealed class Int64sRef : IComparable<Int64sRef> // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// An empty <see cref="long"/> array for convenience
        /// <para/>
        /// NOTE: This was EMPTY_LONGS in Lucene
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly long[] EMPTY_INT64S = Arrays.Empty<long>();

        /// <summary>
        /// The contents of the <see cref="Int64sRef"/>. Should never be <c>null</c>. 
        /// <para/>
        /// NOTE: This was longs (field) in Lucene
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public long[] Int64s
        {
            get => longs;
            set => longs = value ?? throw new ArgumentNullException(nameof(Int64s)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }
        private long[] longs;

        /// <summary>
        /// Offset of first valid long. </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Length of used longs. </summary>
        public int Length { get; set; }

        /// <summary>
        /// Create a <see cref="Int64sRef"/> with <see cref="EMPTY_INT64S"/> </summary>
        public Int64sRef()
        {
            longs = EMPTY_INT64S;
        }

        /// <summary>
        /// Create a <see cref="Int64sRef"/> pointing to a new array of size <paramref name="capacity"/>.
        /// Offset and length will both be zero.
        /// </summary>
        public Int64sRef(int capacity)
        {
            longs = new long[capacity];
        }

        /// <summary>
        /// This instance will directly reference <paramref name="longs"/> w/o making a copy.
        /// <paramref name="longs"/> should not be <c>null</c>.
        /// </summary>
        public Int64sRef(long[] longs, int offset, int length)
        {
            this.longs = longs;
            this.Offset = offset;
            this.Length = length;
            if (Debugging.AssertsEnabled) Debugging.Assert(IsValid());
        }

        /// <summary>
        /// Returns a shallow clone of this instance (the underlying <see cref="long"/>s are
        /// <b>not</b> copied and will be shared by both the returned object and this
        /// object.
        /// </summary>
        /// <seealso cref="DeepCopyOf(Int64sRef)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Clone()
        {
            return new Int64sRef(longs, Offset, Length);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 0;
            long end = Offset + Length;
            for (int i = Offset; i < end; i++)
            {
                result = prime * result + (int)(longs[i] ^ (longs[i].TripleShift(32)));
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            if (obj is Int64sRef other)
            {
                return Int64sEquals(other);
            }
            return false;
        }

        /// <summary>
        /// NOTE: This was longsEquals() in Lucene
        /// </summary>
        public bool Int64sEquals(Int64sRef other)
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
        /// Signed <see cref="int"/> order comparison </summary>
        public int CompareTo(Int64sRef other)
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

        /// <summary>
        /// NOTE: This was copyLongs() in Lucene
        /// </summary>
        public void CopyInt64s(Int64sRef other)
        {
            if (longs.Length - Offset < other.Length)
            {
                longs = new long[other.Length];
                Offset = 0;
            }
            Arrays.Copy(other.longs, other.Offset, longs, Offset, other.Length);
            Length = other.Length;
        }

        /// <summary>
        /// Used to grow the reference array.
        /// <para/>
        /// In general this should not be used as it does not take the offset into account.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public void Grow(int newLength)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Offset == 0);
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
        /// Creates a new <see cref="Int64sRef"/> that points to a copy of the <see cref="long"/>s from
        /// <paramref name="other"/>.
        /// <para/>
        /// The returned <see cref="Int64sRef"/> will have a length of <c>other.Length</c>
        /// and an offset of zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int64sRef DeepCopyOf(Int64sRef other)
        {
            Int64sRef clone = new Int64sRef();
            clone.CopyInt64s(other);
            return clone;
        }

        /// <summary>
        /// Performs internal consistency checks.
        /// Always returns <c>true</c> (or throws <see cref="InvalidOperationException"/>)
        /// </summary>
        public bool IsValid()
        {
            if (longs is null)
            {
                throw IllegalStateException.Create("longs is null");
            }
            if (Length < 0)
            {
                throw IllegalStateException.Create("length is negative: " + Length);
            }
            if (Length > longs.Length)
            {
                throw IllegalStateException.Create("length is out of bounds: " + Length + ",longs.length=" + longs.Length);
            }
            if (Offset < 0)
            {
                throw IllegalStateException.Create("offset is negative: " + Offset);
            }
            if (Offset > longs.Length)
            {
                throw IllegalStateException.Create("offset out of bounds: " + Offset + ",longs.length=" + longs.Length);
            }
            if (Offset + Length < 0)
            {
                throw IllegalStateException.Create("offset+length is negative: offset=" + Offset + ",length=" + Length);
            }
            if (Offset + Length > longs.Length)
            {
                throw IllegalStateException.Create("offset+length out of bounds: offset=" + Offset + ",length=" + Length + ",longs.length=" + longs.Length);
            }
            return true;
        }
    }
}