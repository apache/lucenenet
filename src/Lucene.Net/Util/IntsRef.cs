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
    /// Represents <see cref="T:int[]"/>, as a slice (offset + length) into an
    /// existing <see cref="T:int[]"/>.  The <see cref="Int32s"/> member should never be <c>null</c>; use
    /// <see cref="EMPTY_INT32S"/> if necessary.
    /// <para/>
    /// NOTE: This was IntsRef in Lucene
    /// <para/>
    /// @lucene.internal
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public sealed class Int32sRef : IComparable<Int32sRef> // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// An empty integer array for convenience.
        /// <para/>
        /// NOTE: This was EMPTY_INTS in Lucene
        /// </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly int[] EMPTY_INT32S = Arrays.Empty<int>();

        /// <summary>
        /// The contents of the <see cref="Int32sRef"/>. Should never be <c>null</c>. 
        /// <para/>
        /// NOTE: This was ints (field) in Lucene
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public int[] Int32s // LUCENENET TODO: API - change to indexer
        {
            get => ints;
            set => ints = value ?? throw new ArgumentNullException(nameof(Int32s), "Int32s should never be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }
        private int[] ints;

        /// <summary>
        /// Offset of first valid integer. </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Length of used <see cref="int"/>s. </summary>
        public int Length { get; set; }

        /// <summary>
        /// Create a <see cref="Int32sRef"/> with <see cref="EMPTY_INT32S"/>. </summary>
        public Int32sRef()
        {
            ints = EMPTY_INT32S;
        }

        /// <summary>
        /// Create a <see cref="Int32sRef"/> pointing to a new array of size <paramref name="capacity"/>.
        /// Offset and length will both be zero.
        /// </summary>
        public Int32sRef(int capacity)
        {
            ints = new int[capacity];
        }

        /// <summary>
        /// This instance will directly reference <paramref name="ints"/> w/o making a copy.
        /// <paramref name="ints"/> should not be <c>null</c>.
        /// </summary>
        public Int32sRef(int[] ints, int offset, int length)
        {
            this.ints = ints;
            this.Offset = offset;
            this.Length = length;
            if (Debugging.AssertsEnabled) Debugging.Assert(IsValid());
        }

        /// <summary>
        /// Returns a shallow clone of this instance (the underlying <see cref="int"/>s are
        /// <b>not</b> copied and will be shared by both the returned object and this
        /// object.
        /// </summary>
        /// <seealso cref="DeepCopyOf(Int32sRef)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Clone()
        {
            return new Int32sRef(ints, Offset, Length);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 0;
            int end = Offset + Length;
            for (int i = Offset; i < end; i++)
            {
                result = prime * result + ints[i];
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            if (obj is Int32sRef other)
            {
                return this.Int32sEquals(other);
            }
            return false;
        }

        /// <summary>
        /// NOTE: This was intsEquals() in Lucene
        /// </summary>
        public bool Int32sEquals(Int32sRef other)
        {
            if (Length == other.Length)
            {
                int otherUpto = other.Offset;
                int[] otherInts = other.ints;
                int end = Offset + Length;
                for (int upto = Offset; upto < end; upto++, otherUpto++)
                {
                    if (ints[upto] != otherInts[otherUpto])
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
        /// Signed <see cref="int"/> order comparison. </summary>
        public int CompareTo(Int32sRef other)
        {
            if (this == other)
            {
                return 0;
            }

            int[] aInts = this.ints;
            int aUpto = this.Offset;
            int[] bInts = other.ints;
            int bUpto = other.Offset;

            int aStop = aUpto + Math.Min(this.Length, other.Length);

            while (aUpto < aStop)
            {
                int aInt = aInts[aUpto++];
                int bInt = bInts[bUpto++];
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
        /// NOTE: This was copyInts() in Lucene
        /// </summary>
        public void CopyInt32s(Int32sRef other)
        {
            if (ints.Length - Offset < other.Length)
            {
                ints = new int[other.Length];
                Offset = 0;
            }
            Arrays.Copy(other.ints, other.Offset, ints, Offset, other.Length);
            Length = other.Length;
        }

        /// <summary>
        /// Used to grow the reference array.
        /// <para/>
        /// In general this should not be used as it does not take the offset into account.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Grow(int newLength)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Offset == 0);
            if (ints.Length < newLength)
            {
                ints = ArrayUtil.Grow(ints, newLength);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            int end = Offset + Length;
            for (int i = Offset; i < end; i++)
            {
                if (i > Offset)
                {
                    sb.Append(' ');
                }
                sb.Append(ints[i].ToString("x"));
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Creates a new <see cref="Int32sRef"/> that points to a copy of the <see cref="int"/>s from
        /// <paramref name="other"/>
        /// <para/>
        /// The returned <see cref="Int32sRef"/> will have a length of <c>other.Length</c>
        /// and an offset of zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int32sRef DeepCopyOf(Int32sRef other)
        {
            Int32sRef clone = new Int32sRef();
            clone.CopyInt32s(other);
            return clone;
        }

        /// <summary>
        /// Performs internal consistency checks.
        /// Always returns true (or throws <see cref="InvalidOperationException"/>)
        /// </summary>
        public bool IsValid()
        {
            if (ints is null)
            {
                throw IllegalStateException.Create("ints is null");
            }
            if (Length < 0)
            {
                throw IllegalStateException.Create("length is negative: " + Length);
            }
            if (Length > ints.Length)
            {
                throw IllegalStateException.Create("length is out of bounds: " + Length + ",ints.length=" + Int32s.Length);
            }
            if (Offset < 0)
            {
                throw IllegalStateException.Create("offset is negative: " + Offset);
            }
            if (Offset > ints.Length)
            {
                throw IllegalStateException.Create("offset out of bounds: " + Offset + ",ints.length=" + Int32s.Length);
            }
            if (Offset + Length < 0)
            {
                throw IllegalStateException.Create("offset+length is negative: offset=" + Offset + ",length=" + Length);
            }
            if (Offset + Length > Int32s.Length)
            {
                throw IllegalStateException.Create("offset+length out of bounds: offset=" + Offset + ",length=" + Length + ",ints.length=" + Int32s.Length);
            }
            return true;
        }
    }
}