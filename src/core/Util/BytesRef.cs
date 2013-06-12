using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class BytesRef : IComparable<BytesRef>, ICloneable
    {
        public static readonly sbyte[] EMPTY_BYTES = new sbyte[0];

        public sbyte[] bytes;

        public int offset;

        public int length;

        public BytesRef()
            : this(EMPTY_BYTES)
        {
        }

        public BytesRef(sbyte[] bytes, int offset, int length)
        {
            this.bytes = bytes;
            this.offset = offset;
            this.length = length;
            Trace.Assert(IsValid());
        }

        public BytesRef(sbyte[] bytes)
            : this(bytes, 0, bytes.Length)
        {
        }

        public BytesRef(int capacity)
        {
            this.bytes = new sbyte[capacity];
        }

        public BytesRef(string text)
            : this()
        {
            CopyChars(text);
        }

        public BytesRef(CharsRef text)
            : this()
        {
            CopyChars(text);
        }

        public void CopyChars(string text)
        {
            Trace.Assert(offset == 0);
            UnicodeUtil.UTF16toUTF8(text, 0, text.Length, this);
        }
        
        public void CopyChars(CharsRef text)
        {
            Trace.Assert(offset == 0);
            UnicodeUtil.UTF16toUTF8(text, 0, text.Length, this);
        }

        public bool BytesEquals(BytesRef other)
        {
            Trace.Assert(other != null);

            if (length == other.length)
            {
                int otherUpto = other.offset;
                sbyte[] otherBytes = other.bytes;
                int end = offset + length;

                for (int upto = offset; upto < end; upto++, otherUpto++)
                {
                    if (bytes[upto] != otherBytes[otherUpto])
                        return false;
                }

                return true;
            }

            return false;
        }

        public object Clone()
        {
            return new BytesRef(bytes, offset, length);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                hash = 31 * hash + bytes[i];
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var br = obj as BytesRef;

            if (br != null)
                return this.BytesEquals(br);

            return false;
        }

        public string Utf8ToString()
        {
            CharsRef @ref = new CharsRef(length);
            UnicodeUtil.UTF8toUTF16(bytes, offset, length, @ref);
            return @ref.ToString();
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
                sb.Append((bytes[i] & 0xff).ToString("X"));
            }
            sb.Append(']');

            return sb.ToString();
        }

        public void CopyBytes(BytesRef other)
        {
            if (bytes.Length - offset < other.length)
            {
                bytes = new sbyte[other.length];
                offset = 0;
            }

            Array.Copy(other.bytes, other.offset, bytes, offset, other.length);
            length = other.length;
        }

        public void Append(BytesRef other)
        {
            int newLen = length + other.length;

            if (bytes.Length - offset < newLen)
            {
                sbyte[] newBytes = new sbyte[newLen];
                Array.Copy(bytes, offset, newBytes, 0, length);
                offset = 0;
                bytes = newBytes;
            }

            Array.Copy(other.bytes, other.offset, bytes, length + offset, other.length);
            length = newLen;
        }

        public void Grow(int newLength)
        {
            Trace.Assert(offset == 0);

            bytes = ArrayUtil.Grow(bytes, newLength);
        }

        public int CompareTo(BytesRef other)
        {
            return utf8SortedAsUnicodeSortOrder.Compare(this, other);
        }

        private static readonly Comparer<BytesRef> utf8SortedAsUnicodeSortOrder = new UTF8SortedAsUnicodeComparerImpl();

        public static Comparer<BytesRef> UTF8SortedAsUnicodeComparer
        {
            get
            {
                return utf8SortedAsUnicodeSortOrder;
            }
        }

        private sealed class UTF8SortedAsUnicodeComparerImpl : Comparer<BytesRef>
        {
            public UTF8SortedAsUnicodeComparerImpl()
            {
            }

            public override int Compare(BytesRef a, BytesRef b)
            {
                sbyte[] aBytes = a.bytes;
                int aUpto = a.offset;
                sbyte[] bBytes = b.bytes;
                int bUpto = b.offset;

                int aStop = aUpto + Math.Min(a.length, b.length);
                while (aUpto < aStop)
                {
                    int aByte = aBytes[aUpto++] & 0xff;
                    int bByte = bBytes[bUpto++] & 0xff;

                    int diff = aByte - bByte;
                    if (diff != 0)
                    {
                        return diff;
                    }
                }

                // One is a prefix of the other, or, they are equal:
                return a.length - b.length;
            }
        }

        [Obsolete("This comparer is only a transition mechanism")]
        private static readonly Comparer<BytesRef> utf8SortedAsUTF16SortOrder = new UTF8SortedAsUTF16ComparerImpl();

        [Obsolete("This comparer is only a transition mechanism")]
        public static Comparer<BytesRef> UTF8SortedAsUTF16Comparer
        {
            get
            {
                return utf8SortedAsUTF16SortOrder;
            }
        }

        [Obsolete("This comparer is only a transition mechanism")]
        private sealed class UTF8SortedAsUTF16ComparerImpl : Comparer<BytesRef>
        {
            public UTF8SortedAsUTF16ComparerImpl()
            {
            }

            public override int Compare(BytesRef a, BytesRef b)
            {
                sbyte[] aBytes = a.bytes;
                int aUpto = a.offset;
                sbyte[] bBytes = b.bytes;
                int bUpto = b.offset;

                int aStop;
                if (a.length < b.length)
                {
                    aStop = aUpto + a.length;
                }
                else
                {
                    aStop = aUpto + b.length;
                }

                while (aUpto < aStop)
                {
                    int aByte = aBytes[aUpto++] & 0xff;
                    int bByte = bBytes[bUpto++] & 0xff;

                    if (aByte != bByte)
                    {

                        // See http://icu-project.org/docs/papers/utf16_code_point_order.html#utf-8-in-utf-16-order

                        // We know the terms are not equal, but, we may
                        // have to carefully fixup the bytes at the
                        // difference to match UTF16's sort order:

                        // NOTE: instead of moving supplementary code points (0xee and 0xef) to the unused 0xfe and 0xff, 
                        // we move them to the unused 0xfc and 0xfd [reserved for future 6-byte character sequences]
                        // this reserves 0xff for preflex's term reordering (surrogate dance), and if unicode grows such
                        // that 6-byte sequences are needed we have much bigger problems anyway.
                        if (aByte >= 0xee && bByte >= 0xee)
                        {
                            if ((aByte & 0xfe) == 0xee)
                            {
                                aByte += 0xe;
                            }
                            if ((bByte & 0xfe) == 0xee)
                            {
                                bByte += 0xe;
                            }
                        }
                        return aByte - bByte;
                    }
                }

                // One is a prefix of the other, or, they are equal:
                return a.length - b.length;
            }
        }

        public static BytesRef DeepCopyOf(BytesRef other)
        {
            BytesRef copy = new BytesRef();
            copy.CopyBytes(other);
            return copy;
        }

        private bool IsValid()
        {
            if (bytes == null)
                throw new InvalidOperationException("bytes is null");

            if (length < 0)
                throw new InvalidOperationException("length is negative: " + length);

            if (length > bytes.Length)
                throw new InvalidOperationException("length is out of bounds: " + length + ", bytes.Length=" + bytes.Length);

            if (offset < 0)
                throw new InvalidOperationException("offset is negative: " + offset);

            if (offset > bytes.Length)
                throw new InvalidOperationException("offset is out of bounds: " + offset + ", bytes.Length=" + bytes.Length);

            if (unchecked(offset + length < 0))
                throw new InvalidOperationException("offset+length is negative: offset=" + offset + ", length=" + length);

            if (offset + length > bytes.Length)
                throw new InvalidOperationException("offset+length out of bounds: offset=" + offset + ", length=" + length + ", bytes.Length=" + bytes.Length);

            return true;
        }
    }
}
