using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class LongsRef : IComparable<LongsRef>, ICloneable
    {
        public static readonly long[] EMPTY_LONGS = new long[0];

        /** The contents of the LongsRef. Should never be {@code null}. */
        public long[] longs;
        /** Offset of first valid long. */
        public int offset;
        /** Length of used longs. */
        public int length;

        /** Create a LongsRef with {@link #EMPTY_LONGS} */
        public LongsRef()
        {
            longs = EMPTY_LONGS;
        }

        /** 
         * Create a LongsRef pointing to a new array of size <code>capacity</code>.
         * Offset and length will both be zero.
         */
        public LongsRef(int capacity)
        {
            longs = new long[capacity];
        }

        /** This instance will directly reference longs w/o making a copy.
         * longs should not be null */
        public LongsRef(long[] longs, int offset, int length)
        {
            this.longs = longs;
            this.offset = offset;
            this.length = length;
            //assert isValid();
        }

        public object Clone()
        {
            return new LongsRef(longs, offset, length);
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 0;
            long end = offset + length;
            for (int i = offset; i < end; i++)
            {
                result = prime * result + (int)(longs[i] ^ Number.URShift(longs[i], 32));
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
            if (length == other.length)
            {
                int otherUpto = other.offset;
                long[] otherInts = other.longs;
                long end = offset + length;
                for (int upto = offset; upto < end; upto++, otherUpto++)
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

        public int CompareTo(LongsRef other)
        {
            if (this == other) return 0;

            long[] aInts = this.longs;
            int aUpto = this.offset;
            long[] bInts = other.longs;
            int bUpto = other.offset;

            long aStop = aUpto + Math.Min(this.length, other.length);

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
            return this.length - other.length;
        }

        public void CopyLongs(LongsRef other)
        {
            if (longs.Length - offset < other.length)
            {
                longs = new long[other.length];
                offset = 0;
            }
            Array.Copy(other.longs, other.offset, longs, offset, other.length);
            length = other.length;
        }

        public void Grow(int newLength)
        {
            //assert offset == 0;
            if (longs.Length < newLength)
            {
                longs = ArrayUtil.Grow(longs, newLength);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            long end = offset + length;
            for (int i = offset; i < end; i++)
            {
                if (i > offset)
                {
                    sb.Append(' ');
                }
                sb.Append(longs[i].ToString("X"));
            }
            sb.Append(']');
            return sb.ToString();
        }

        public static LongsRef DeepCopyOf(LongsRef other)
        {
            LongsRef clone = new LongsRef();
            clone.CopyLongs(other);
            return clone;
        }

        public bool isValid()
        {
            if (longs == null)
            {
                throw new InvalidOperationException("longs is null");
            }
            if (length < 0)
            {
                throw new InvalidOperationException("length is negative: " + length);
            }
            if (length > longs.Length)
            {
                throw new InvalidOperationException("length is out of bounds: " + length + ",longs.length=" + longs.Length);
            }
            if (offset < 0)
            {
                throw new InvalidOperationException("offset is negative: " + offset);
            }
            if (offset > longs.Length)
            {
                throw new InvalidOperationException("offset out of bounds: " + offset + ",longs.length=" + longs.Length);
            }
            if (offset + length < 0)
            {
                throw new InvalidOperationException("offset+length is negative: offset=" + offset + ",length=" + length);
            }
            if (offset + length > longs.Length)
            {
                throw new InvalidOperationException("offset+length out of bounds: offset=" + offset + ",length=" + length + ",longs.length=" + longs.Length);
            }
            return true;
        }
    }
}
