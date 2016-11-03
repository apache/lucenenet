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
    /// Represents int[], as a slice (offset + length) into an
    ///  existing int[].  The <seealso cref="#ints"/> member should never be null; use
    ///  <seealso cref="#EMPTY_INTS"/> if necessary.
    ///
    ///  @lucene.internal
    /// </summary>
    public sealed class IntsRef : IComparable<IntsRef>
    {
        /// <summary>
        /// An empty integer array for convenience </summary>
        public static readonly int[] EMPTY_INTS = new int[0];

        /// <summary>
        /// The contents of the IntsRef. Should never be {@code null}. </summary>
        public int[] Ints;

        /// <summary>
        /// Offset of first valid integer. </summary>
        public int Offset;

        /// <summary>
        /// Length of used ints. </summary>
        public int Length;

        /// <summary>
        /// Create a IntsRef with <seealso cref="#EMPTY_INTS"/> </summary>
        public IntsRef()
        {
            Ints = EMPTY_INTS;
        }

        /// <summary>
        /// Create a IntsRef pointing to a new array of size <code>capacity</code>.
        /// Offset and length will both be zero.
        /// </summary>
        public IntsRef(int capacity)
        {
            Ints = new int[capacity];
        }

        /// <summary>
        /// this instance will directly reference ints w/o making a copy.
        /// ints should not be null.
        /// </summary>
        public IntsRef(int[] ints, int offset, int length)
        {
            this.Ints = ints;
            this.Offset = offset;
            this.Length = length;
            Debug.Assert(Valid);
        }

        /// <summary>
        /// Returns a shallow clone of this instance (the underlying ints are
        /// <b>not</b> copied and will be shared by both the returned object and this
        /// object.
        /// </summary>
        /// <seealso cref= #deepCopyOf </seealso>
        public object Clone()
        {
            return new IntsRef(Ints, Offset, Length);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 0;
            int end = Offset + Length;
            for (int i = Offset; i < end; i++)
            {
                result = prime * result + Ints[i];
            }
            return result;
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }
            if (other is IntsRef)
            {
                return this.IntsEquals((IntsRef)other);
            }
            return false;
        }

        public bool IntsEquals(IntsRef other)
        {
            if (Length == other.Length)
            {
                int otherUpto = other.Offset;
                int[] otherInts = other.Ints;
                int end = Offset + Length;
                for (int upto = Offset; upto < end; upto++, otherUpto++)
                {
                    if (Ints[upto] != otherInts[otherUpto])
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
        public int CompareTo(IntsRef other)
        {
            if (this == other)
            {
                return 0;
            }

            int[] aInts = this.Ints;
            int aUpto = this.Offset;
            int[] bInts = other.Ints;
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

        public void CopyInts(IntsRef other)
        {
            if (Ints.Length - Offset < other.Length)
            {
                Ints = new int[other.Length];
                Offset = 0;
            }
            Array.Copy(other.Ints, other.Offset, Ints, Offset, other.Length);
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
            if (Ints.Length < newLength)
            {
                Ints = ArrayUtil.Grow(Ints, newLength);
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
                sb.Append(Ints[i].ToString("x"));
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Creates a new IntsRef that points to a copy of the ints from
        /// <code>other</code>
        /// <p>
        /// The returned IntsRef will have a length of other.length
        /// and an offset of zero.
        /// </summary>
        public static IntsRef DeepCopyOf(IntsRef other)
        {
            IntsRef clone = new IntsRef();
            clone.CopyInts(other);
            return clone;
        }

        /// <summary>
        /// Performs internal consistency checks.
        /// Always returns true (or throws InvalidOperationException)
        /// </summary>
        public bool Valid
        {
            get
            {
                if (Ints == null)
                {
                    throw new InvalidOperationException("ints is null");
                }
                if (Length < 0)
                {
                    throw new InvalidOperationException("length is negative: " + Length);
                }
                if (Length > Ints.Length)
                {
                    throw new InvalidOperationException("length is out of bounds: " + Length + ",ints.length=" + Ints.Length);
                }
                if (Offset < 0)
                {
                    throw new InvalidOperationException("offset is negative: " + Offset);
                }
                if (Offset > Ints.Length)
                {
                    throw new InvalidOperationException("offset out of bounds: " + Offset + ",ints.length=" + Ints.Length);
                }
                if (Offset + Length < 0)
                {
                    throw new InvalidOperationException("offset+length is negative: offset=" + Offset + ",length=" + Length);
                }
                if (Offset + Length > Ints.Length)
                {
                    throw new InvalidOperationException("offset+length out of bounds: offset=" + Offset + ",length=" + Length + ",ints.length=" + Ints.Length);
                }
                return true;
            }
        }
    }
}