using System;
using System.Collections.Generic;
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
    /// Represents byte[], as a slice (offset + length) into an
    ///  existing byte[].  The <seealso cref="#bytes"/> member should never be null;
    ///  use <seealso cref="#EMPTY_BYTES"/> if necessary.
    ///
    /// <p><b>Important note:</b> Unless otherwise noted, Lucene uses this class to
    /// represent terms that are encoded as <b>UTF8</b> bytes in the index. To
    /// convert them to a Java <seealso cref="String"/> (which is UTF16), use <seealso cref="#utf8ToString"/>.
    /// Using code like {@code new String(bytes, offset, length)} to do this
    /// is <b>wrong</b>, as it does not respect the correct character set
    /// and may return wrong results (depending on the platform's defaults)!
    /// </summary>
    public sealed class BytesRef : IComparable
    {
        /// <summary>
        /// An empty byte array for convenience </summary>
        public static readonly byte[] EMPTY_BYTES = new byte[0];

        /// <summary>
        /// The contents of the BytesRef. Should never be {@code null}.
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// Offset of first valid byte.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Length of used bytes.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Create a BytesRef with <seealso cref="#EMPTY_BYTES"/> </summary>
        public BytesRef()
            : this(EMPTY_BYTES)
        {
        }

        /// <summary>
        /// this instance will directly reference bytes w/o making a copy.
        /// bytes should not be null.
        /// </summary>
        public BytesRef(byte[] bytes, int offset, int length)
        {
            this.Bytes = bytes;
            this.Offset = offset;
            this.Length = length;
            Debug.Assert(Valid);
        }

        /// <summary>
        /// this instance will directly reference bytes w/o making a copy.
        /// bytes should not be null
        /// </summary>
        public BytesRef(byte[] bytes)
            : this(bytes, 0, bytes.Length)
        {
        }

        /// <summary>
        /// Create a BytesRef pointing to a new array of size <code>capacity</code>.
        /// Offset and length will both be zero.
        /// </summary>
        public BytesRef(int capacity)
        {
            this.Bytes = new byte[capacity];
        }

        /// <summary>
        /// Initialize the byte[] from the UTF8 bytes
        /// for the provided String.
        /// </summary>
        /// <param name="text"> this must be well-formed
        /// unicode text, with no unpaired surrogates. </param>
        public BytesRef(CharsRef text)
            : this()
        {
            CopyChars(text);
        }

        /// <summary>
        /// Initialize the byte[] from the UTF8 bytes
        /// for the provided String.
        /// </summary>
        /// <param name="text"> this must be well-formed
        /// unicode text, with no unpaired surrogates. </param>
        public BytesRef(string text)
            : this()
        {
            CopyChars(text);
        }

        /// <summary>
        /// Copies the UTF8 bytes for this string.
        /// </summary>
        /// <param name="text"> Must be well-formed unicode text, with no
        /// unpaired surrogates or invalid UTF16 code units. </param>
        public void CopyChars(CharsRef text)
        {
            Debug.Assert(Offset == 0); // TODO broken if offset != 0
            UnicodeUtil.UTF16toUTF8(text, 0, text.Length, this);
        }

        /// <summary>
        /// Copies the UTF8 bytes for this string.
        /// </summary>
        /// <param name="text"> Must be well-formed unicode text, with no
        /// unpaired surrogates or invalid UTF16 code units. </param>
        public void CopyChars(string text)
        {
            Debug.Assert(Offset == 0); // TODO broken if offset != 0
            UnicodeUtil.UTF16toUTF8(text.ToCharArray(0, text.Length), 0, text.Length, this);
        }

        /// <summary>
        /// Expert: compares the bytes against another BytesRef,
        /// returning true if the bytes are equal.
        /// </summary>
        /// <param name="other"> Another BytesRef, should not be null.
        /// @lucene.internal </param>
        public bool BytesEquals(BytesRef other)
        {
            Debug.Assert(other != null);
            if (Length == other.Length)
            {
                var otherUpto = other.Offset;
                var otherBytes = other.Bytes;
                var end = Offset + Length;
                for (int upto = Offset; upto < end; upto++, otherUpto++)
                {
                    if (Bytes[upto] != otherBytes[otherUpto])
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
        /// Returns a shallow clone of this instance (the underlying bytes are
        /// <b>not</b> copied and will be shared by both the returned object and this
        /// object.
        /// </summary>
        /// <seealso cref= #deepCopyOf </seealso>
        public object Clone()
        {
            return new BytesRef(Bytes, Offset, Length);
        }

        /// <summary>
        /// Calculates the hash code as required by TermsHash during indexing.
        ///  <p> this is currently implemented as MurmurHash3 (32
        ///  bit), using the seed from {@link
        ///  StringHelper#GOOD_FAST_HASH_SEED}, but is subject to
        ///  change from release to release.
        /// </summary>
        public override int GetHashCode()
        {
            return StringHelper.Murmurhash3_x86_32(this, StringHelper.GOOD_FAST_HASH_SEED);
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }
            if (other is BytesRef)
            {
                return this.BytesEquals((BytesRef)other);
            }
            return false;
        }

        /// <summary>
        /// Interprets stored bytes as UTF8 bytes, returning the
        ///  resulting string
        /// </summary>
        public string Utf8ToString()
        {
            CharsRef @ref = new CharsRef(Length);
            UnicodeUtil.UTF8toUTF16(Bytes, Offset, Length, @ref);
            return @ref.ToString();
        }

        /// <summary>
        /// Returns hex encoded bytes, eg [0x6c 0x75 0x63 0x65 0x6e 0x65] </summary>
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
                sb.Append((Bytes[i] & 0xff).ToString("x"));
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Copies the bytes from the given <seealso cref="BytesRef"/>
        /// <p>
        /// NOTE: if this would exceed the array size, this method creates a
        /// new reference array.
        /// </summary>
        public void CopyBytes(BytesRef other)
        {
            if (Bytes.Length - Offset < other.Length)
            {
                Bytes = new byte[other.Length];
                Offset = 0;
            }
            Array.Copy(other.Bytes, other.Offset, Bytes, Offset, other.Length);
            Length = other.Length;
        }

        /// <summary>
        /// Appends the bytes from the given <seealso cref="BytesRef"/>
        /// <p>
        /// NOTE: if this would exceed the array size, this method creates a
        /// new reference array.
        /// </summary>
        public void Append(BytesRef other)
        {
            int newLen = Length + other.Length;
            if (Bytes.Length - Offset < newLen)
            {
                var newBytes = new byte[newLen];
                Array.Copy(Bytes, Offset, newBytes, 0, Length);
                Offset = 0;
                Bytes = newBytes;
            }
            Array.Copy(other.Bytes, other.Offset, Bytes, Length + Offset, other.Length);
            Length = newLen;
        }

        /// <summary>
        /// Used to grow the reference array.
        ///
        /// In general this should not be used as it does not take the offset into account.
        /// @lucene.internal
        /// </summary>
        public void Grow(int newLength)
        {
            Debug.Assert(Offset == 0); // NOTE: senseless if offset != 0
            Bytes = ArrayUtil.Grow(Bytes, newLength);
        }

        /// <summary>
        /// Unsigned byte order comparison </summary>
        public int CompareTo(object other)
        {
            BytesRef br = other as BytesRef;
            Debug.Assert(br != null);
            return Utf8SortedAsUnicodeSortOrder.Compare(this, br);
        }

        private static readonly IComparer<BytesRef> Utf8SortedAsUnicodeSortOrder = UTF8SortedAsUnicodeComparator.Instance;

        public static IComparer<BytesRef> UTF8SortedAsUnicodeComparer
        {
            get
            {
                return Utf8SortedAsUnicodeSortOrder;
            }
        }

        internal class UTF8SortedAsUnicodeComparator : IComparer<BytesRef>
        {
            internal static UTF8SortedAsUnicodeComparator Instance = new UTF8SortedAsUnicodeComparator();

            // Only singleton
            private UTF8SortedAsUnicodeComparator()
            {
            }

            public virtual int Compare(BytesRef a, BytesRef b)
            {
                var aBytes = a.Bytes;
                int aUpto = a.Offset;
                var bBytes = b.Bytes;
                int bUpto = b.Offset;

                int aStop = aUpto + Math.Min(a.Length, b.Length);
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
                return a.Length - b.Length;
            }
        }

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        private static readonly IComparer<BytesRef> Utf8SortedAsUTF16SortOrder = new UTF8SortedAsUTF16Comparator();

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        public static IComparer<BytesRef> UTF8SortedAsUTF16Comparer
        {
            get
            {
                return Utf8SortedAsUTF16SortOrder;
            }
        }

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        private class UTF8SortedAsUTF16Comparator : IComparer<BytesRef>
        {
            // Only singleton
            internal UTF8SortedAsUTF16Comparator()
            {
            }

            public virtual int Compare(BytesRef a, BytesRef b)
            {
                var aBytes = a.Bytes;
                int aUpto = a.Offset;
                var bBytes = b.Bytes;
                int bUpto = b.Offset;

                int aStop;
                if (a.Length < b.Length)
                {
                    aStop = aUpto + a.Length;
                }
                else
                {
                    aStop = aUpto + b.Length;
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
                return a.Length - b.Length;
            }
        }

        /// <summary>
        /// Creates a new BytesRef that points to a copy of the bytes from
        /// <code>other</code>
        /// <p>
        /// The returned BytesRef will have a length of other.length
        /// and an offset of zero.
        /// </summary>
        public static BytesRef DeepCopyOf(BytesRef other)
        {
            BytesRef copy = new BytesRef();
            copy.CopyBytes(other);
            return copy;
        }

        /// <summary>
        /// Performs internal consistency checks.
        /// Always returns true (or throws InvalidOperationException)
        /// </summary>
        public bool Valid
        {
            get
            {
                if (Bytes == null)
                {
                    throw new InvalidOperationException("bytes is null");
                }
                if (Length < 0)
                {
                    throw new InvalidOperationException("length is negative: " + Length);
                }
                if (Length > Bytes.Length)
                {
                    throw new InvalidOperationException("length is out of bounds: " + Length + ",bytes.length=" + Bytes.Length);
                }
                if (Offset < 0)
                {
                    throw new InvalidOperationException("offset is negative: " + Offset);
                }
                if (Offset > Bytes.Length)
                {
                    throw new InvalidOperationException("offset out of bounds: " + Offset + ",bytes.length=" + Bytes.Length);
                }
                if (Offset + Length < 0)
                {
                    throw new InvalidOperationException("offset+length is negative: offset=" + Offset + ",length=" + Length);
                }
                if (Offset + Length > Bytes.Length)
                {
                    throw new InvalidOperationException("offset+length out of bounds: offset=" + Offset + ",length=" + Length + ",bytes.length=" + Bytes.Length);
                }
                return true;
            }
        }
    }
}