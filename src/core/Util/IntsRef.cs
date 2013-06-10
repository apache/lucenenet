using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class IntsRef : IComparable<IntsRef>, ICloneable
    {
        public static readonly int[] EMPTY_INTS = new int[0];

        /** The contents of the IntsRef. Should never be {@code null}. */
        public int[] ints;
        /** Offset of first valid integer. */
        public int offset;
        /** Length of used ints. */
        public int length;

        /** Create a IntsRef with {@link #EMPTY_INTS} */
        public IntsRef()
        {
            ints = EMPTY_INTS;
        }

        /** 
        * Create a IntsRef pointing to a new array of size <code>capacity</code>.
        * Offset and length will both be zero.
        */
        public IntsRef(int capacity)
        {
            ints = new int[capacity];
        }

        /** This instance will directly reference ints w/o making a copy.
         * ints should not be null.
         */
        public IntsRef(int[] ints, int offset, int length)
        {
            this.ints = ints;
            this.offset = offset;
            this.length = length;
            //assert isValid();
        }

        public object Clone()
        {
            return new IntsRef(ints, offset, length);
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 0;
            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                result = prime * result + ints[i];
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
            if (length == other.length)
            {
                int otherUpto = other.offset;
                int[] otherInts = other.ints;
                int end = offset + length;
                for (int upto = offset; upto < end; upto++, otherUpto++)
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

        public int CompareTo(IntsRef other)
        {
            if (this == other) return 0;

            int[] aInts = this.ints;
            int aUpto = this.offset;
            int[] bInts = other.ints;
            int bUpto = other.offset;

            int aStop = aUpto + Math.Min(this.length, other.length);

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
            return this.length - other.length;
        }

        public void CopyInts(IntsRef other)
        {
            if (ints.Length - offset < other.length)
            {
                ints = new int[other.length];
                offset = 0;
            }
            Array.Copy(other.ints, other.offset, ints, offset, other.length);
            length = other.length;
        }

        public void Grow(int newLength)
        {
            //assert offset == 0;
            if (ints.Length < newLength)
            {
                ints = ArrayUtil.Grow(ints, newLength);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                if (i > offset)
                {
                    sb.Append(' ');
                }
                sb.Append(ints[i].ToString("X"));
            }
            sb.Append(']');
            return sb.ToString();
        }

        public static IntsRef DeepCopyOf(IntsRef other)
        {
            IntsRef clone = new IntsRef();
            clone.CopyInts(other);
            return clone;
        }

        public bool IsValid()
        {
            if (ints == null)
            {
                throw new InvalidOperationException("ints is null");
            }
            if (length < 0)
            {
                throw new InvalidOperationException("length is negative: " + length);
            }
            if (length > ints.Length)
            {
                throw new InvalidOperationException("length is out of bounds: " + length + ",ints.length=" + ints.Length);
            }
            if (offset < 0)
            {
                throw new InvalidOperationException("offset is negative: " + offset);
            }
            if (offset > ints.Length)
            {
                throw new InvalidOperationException("offset out of bounds: " + offset + ",ints.length=" + ints.Length);
            }
            if (offset + length < 0)
            {
                throw new InvalidOperationException("offset+length is negative: offset=" + offset + ",length=" + length);
            }
            if (offset + length > ints.Length)
            {
                throw new InvalidOperationException("offset+length out of bounds: offset=" + offset + ",length=" + length + ",ints.length=" + ints.Length);
            }
            return true;
        }
    }
}
