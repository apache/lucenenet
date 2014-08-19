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

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    ///     <see cref="BytesRef" /> represents a byte array as a slice of an existing byte array.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <seealso cref="Bytes" /> property should never be null;
    ///         Use <seealso cref="EMPTY_BYTES" /> if necessary.
    ///     </para>
    ///     <para>
    ///         <strong>Important note</strong>: Unless otherwise noted, Lucene uses this class to
    ///         represent terms that are encoded as <b>UTF8</b> bytes in the index. To
    ///         convert them to a Java <seealso cref="string" /> (which is UTF16), use <seealso cref="Utf8ToString" />.
    ///         Using code like <c>new String(bytes, offset, length)</c> to do this
    ///         is <b>wrong</b>. It does not respect the correct character set
    ///         and may return wrong results (depending on the platform's defaults)!
    ///     </para>
    /// </remarks>
    // ReSharper disable CSharpWarnings::CS1574
    public class BytesRef :
        IComparable,
        Support.ICloneable,
        IEnumerable<Byte>
    {
        /// <summary>
        ///     An empty byte array for convenience
        /// </summary>
        public static readonly byte[] EMPTY_BYTES = new byte[0];

        /// <summary>
        ///     The contents of <see cref="BytesRef" />
        /// </summary>
        public byte[] Bytes { get; internal set; }

        /// <summary>
        ///     Offset of first valid byte.
        /// </summary>
        public int Offset { get; internal set; }

        /// <summary>
        ///     Length of used bytes.
        /// </summary>
        public virtual int Length { get; internal protected set; }

        /// <summary>
        ///     Create a BytesRef with <seealso cref="EMPTY_BYTES" />
        /// </summary>
        public BytesRef()
            : this(EMPTY_BYTES)
        {
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="BytesRef" /> that
        ///     references the <paramref name="bytes" /> instead of making
        ///     a copy.
        /// </summary>
        /// <param name="bytes">The array of bytes to be references.</param>
        /// <param name="offset">The starting position of the first valid byte.</param>
        /// <param name="length">The number of bytes to use.</param>
        public BytesRef(byte[] bytes, int offset, int length)
        {
            this.Bytes = bytes;
            this.Offset = offset;
            this.Length = length;

            Debug.Assert(this.Valid());
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="BytesRef" /> that
        ///     references the <paramref name="bytes" /> instead of making
        ///     a copy.
        /// </summary>
        /// <param name="bytes">The array of bytes to be references.</param>
        public BytesRef(byte[] bytes)
            : this(bytes, 0, bytes.Length)
        {
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="BytesRef" /> that creates an empty array
        ///     with the specified <paramref name="capacity" />.
        /// </summary>
        public BytesRef(int capacity)
        {
            this.Bytes = new byte[capacity];
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="BytesRef" /> from the UTF8 bytes
        ///     from the given <see cref="CharsRef" />.
        /// </summary>
        /// <param name="text">
        ///     this must be well-formed
        ///     unicode text, with no unpaired surrogates.
        /// </param>
        public BytesRef(CharsRef text)
            : this()
        {
            CopyChars(text);
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="BytesRef" /> from the UTF8 bytes
        ///     from the given <see cref="string" />.
        /// </summary>
        /// <param name="text">
        ///     this must be well-formed
        ///     unicode text, with no unpaired surrogates.
        /// </param>
        public BytesRef(string text)
            : this()
        {
            CopyChars(text);
        }

        /// <summary>
        ///     Copies the UTF8 bytes for this string.
        /// </summary>
        /// <param name="text">
        ///     Must be well-formed unicode text, with no
        ///     unpaired surrogates or invalid UTF16 code units.
        /// </param>
        public void CopyChars(CharsRef text)
        {
            Debug.Assert(this.Offset == 0);
            UnicodeUtil.Utf16ToUtf8(text, 0, text.Length, this);
        }

        /// <summary>
        ///     Copies the UTF8 bytes for this string.
        /// </summary>
        /// <param name="text">
        ///     Must be well-formed unicode text, with no
        ///     unpaired surrogates or invalid UTF16 code units.
        /// </param>
        public void CopyChars(string text)
        {
            Debug.Assert(this.Offset == 0);
            UnicodeUtil.Utf16ToUtf8(text.ToCharArray(0, text.Length), 0, text.Length, this);
        }

        /// <summary>
        ///     Compares the bytes against another BytesRef,
        ///     returning true if the bytes are equal.
        /// </summary>
        /// <param name="other">
        ///     Another BytesRef, should not be null.
        ///     @lucene.internal
        /// </param>
        public bool BytesEquals(BytesRef other)
        {
            Debug.Assert(other != null);
            if (Length == other.Length)
            {
                var otherOffset = other.Offset;
                var otherBytes = other.Bytes;
                var end = this.Offset + this.Length;
                for (var offset = Offset; offset < end; offset++, otherOffset++)
                {
                    if (this.Bytes[offset] != otherBytes[otherOffset])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Returns a shallow clone of this instance (the underlying bytes are
        ///     <b>not</b> copied and will be shared by both the returned object and this
        ///     object.
        /// </summary>
        /// <param name="deepClone">Instructs <see cref="Clone" /> to perform a deep clone when true.</param>
        /// <returns>A clone copy of this instance.</returns>
        public object Clone(bool deepClone = false)
        {
            if (deepClone)
            {
                var bytesRef = new BytesRef();
                bytesRef.CopyBytes(this);
                return bytesRef;
            }

            return new BytesRef(this.Bytes, this.Offset, this.Length);
        }

        /// <summary>
        ///     Calculates the hash code as required by TermsHash during indexing.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is currently implemented as MurmurHash3 (32bit),
        ///         using the seed from <see cref="StringHelper.GOOD_FAST_HASH_SEED" />, but is subject to
        ///         change from release to release.
        ///     </para>
        /// </remarks>
        public override int GetHashCode()
        {
            return StringHelper.MurmurHash3_x86_32(this.Bytes, this.Offset, this.Length,
                StringHelper.GOOD_FAST_HASH_SEED);
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }
            if (other is BytesRef)
            {
                return this.BytesEquals((BytesRef) other);
            }
            return false;
        }

        /// <summary>
        ///     Interprets stored bytes as UTF8 bytes.
        /// </summary>
        /// <returns>A utf16 string.</returns>
        public string Utf8ToString()
        {
            var @ref = new CharsRef(Length);
            UnicodeUtil.Utf8ToUtf16(this.Bytes, this.Offset, this.Length, @ref);
            return @ref.ToString();
        }

        /// <summary>
        ///     Returns hex encoded bytes, eg [0x6c 0x75 0x63 0x65 0x6e 0x65]
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            var end = this.Offset + Length;
            for (var i = this.Offset; i < end; i++)
            {
                if (i > this.Offset)
                {
                    sb.Append(' ');
                }
                sb.Append((this.Bytes[i] & 0xff).ToString("x"));
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        ///     Copies the bytes from the given <seealso cref="BytesRef" />
        /// </summary>
        /// <param name="other">The <see cref="BytesRef" /> to copy into this instance.</param>
        /// <remarks>
        ///     <para>
        ///         NOTE: if this would exceed the array size, this method creates a
        ///         new reference array.
        ///     </para>
        /// </remarks>
        public void CopyBytes(BytesRef other)
        {
            if (this.Bytes.Length - this.Offset < other.Length)
            {
                this.Bytes = new byte[other.Length];
                this.Offset = 0;
            }
            Array.Copy(other.Bytes, other.Offset, Bytes, Offset, other.Length);
            this.Length = other.Length;
        }

        /// <summary>
        ///     Appends the bytes from the given <seealso cref="BytesRef" />
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         NOTE: if this would exceed the array size, this method creates a
        ///         new reference array.
        ///     </para>
        /// </remarks>
        public void Append(BytesRef other)
        {
            var newLen = Length + other.Length;
            if (this.Bytes.Length - this.Offset < newLen)
            {
                var newBytes = new byte[newLen];
                Array.Copy(this.Bytes, this.Offset, newBytes, 0, Length);
                Offset = 0;
                Bytes = newBytes;
            }
            Array.Copy(other.Bytes, other.Offset, Bytes, Length + Offset, other.Length);
            this.Length = newLen;
        }

        /// <summary>
        ///     Used to grow the reference array.
        /// </summary>
        internal protected virtual void Grow(int newLength)
        {
            Debug.Assert(this.Offset == 0); // NOTE: senseless if offset != 0
            this.Bytes = ArrayUtil.Grow(this.Bytes, newLength);
        }

        /// <summary>
        ///     Unsigned byte order comparison.
        /// </summary>
        public int CompareTo(object other)
        {
            var br = other as BytesRef;
            Debug.Assert(br != null);
            return UTF8_SORTED_AS_UNICODE_SORT_ORDER.Compare(this, br);
        }

        private static readonly IComparer<BytesRef> UTF8_SORTED_AS_UNICODE_SORT_ORDER = new Utf8SortedAsUnicodeComparator();

        /// <summary>
        ///     Gets a a comparer for <see cref="BytesRef" /> to sort Utf8 as Unicode.
        /// </summary>
        public static IComparer<BytesRef> Utf8SortedAsUnicodeComparer
        {
            get { return UTF8_SORTED_AS_UNICODE_SORT_ORDER; }
        }

        private class Utf8SortedAsUnicodeComparator : IComparer<BytesRef>
        {
            // Only singleton

            public int Compare(BytesRef a, BytesRef b)
            {
                var aBytes = a.Bytes;
                var aOffset = a.Offset;
                var bBytes = b.Bytes;
                var bOffset = b.Offset;

                var aStop = aOffset + Math.Min(a.Length, b.Length);
                while (aOffset < aStop)
                {
                    var aByte = aBytes[aOffset++] & 0xff;
                    var bByte = bBytes[bOffset++] & 0xff;

                    var diff = aByte - bByte;
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
#pragma warning disable 0612, 0618
        private static readonly IComparer<BytesRef> UTF8_SORTED_AS_UTF16_SORT_ORDER = new Utf8SortedAsUtf16Comparator();
#pragma warning restore 0612, 0618

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        public static IComparer<BytesRef> Utf8SortedAsUtf16Comparer
        {
            get { return UTF8_SORTED_AS_UTF16_SORT_ORDER; }
        }

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        private class Utf8SortedAsUtf16Comparator : IComparer<BytesRef>
        {
            // Only singleton

            public int Compare(BytesRef a, BytesRef b)
            {
                var aBytes = a.Bytes;
                var aOffset = a.Offset;
                var bBytes = b.Bytes;
                var bOffset = b.Offset;

                int aStop;
                if (a.Length < b.Length)
                {
                    aStop = aOffset + a.Length;
                }
                else
                {
                    aStop = aOffset + b.Length;
                }

                while (aOffset < aStop)
                {
                    var aByte = aBytes[aOffset++] & 0xff;
                    var bByte = bBytes[bOffset++] & 0xff;

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
        ///     Performs internal consistency checks.
        /// </summary>
        /// <returns>True</returns>
        /// <exception cref="System.InvalidOperationException">
        ///     <list type="bullet">
        ///         <item>Thrown when <see cref="BytesRef.Bytes" /> is null.</item>
        ///         <item>Thrown when <see cref="BytesRef.Length" /> is less than zero.</item>
        ///         <item>Thrown when <see cref="BytesRef.Length" /> is greater than <see cref="BytesRef.Bytes" />.Length.</item>
        ///         <item>Thrown when <see cref="BytesRef.Offset" /> is less than zero.</item>
        ///         <item>Thrown when <see cref="BytesRef.Offset" /> is greater than <see cref="BytesRef.Bytes" />.Length.</item>
        ///         <item>Thrown when <see cref="BytesRef.Offset" /> and <see cref="BytesRef.Length" /> is less than zero.</item>
        ///         <item>
        ///             Thrown when <see cref="BytesRef.Offset" /> and <see cref="BytesRef.Length" /> is greater than
        ///             <see cref="BytesRef.Bytes" />.Length.
        ///         </item>
        ///     </list>
        /// </exception>
        // this should be a method instead of a property due to the exceptions thrown. 
        public bool Valid()
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
                throw new InvalidOperationException("length is out of bounds: " + Length + ",bytes.length=" +
                                                    Bytes.Length);
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
                throw new InvalidOperationException("offset+length out of bounds: offset=" + Offset + ",length=" +
                                                    Length + ",bytes.length=" + Bytes.Length);
            }
            return true;
        }

        /// <summary>
        ///     Custom enumerator for <see cref="BytesRef" /> that accounts
        ///     for the the custom Length and Offset.
        /// </summary>
        public class ByteEnumerator : IEnumerator<byte>
        {
            private byte[] bytes;
            private int length;
            private int offset;
            private int position = -1;

            public ByteEnumerator(byte[] bytes, int offset = 0, int length = 0)
            {
                if (length == 0 || length > bytes.Length)
                    length = bytes.Length;

                this.bytes = bytes;
                this.length = length;
                this.offset = offset;
                this.position = (this.offset - 1);
            }

            public int Length
            {
                get { return this.length; }
            }


            public byte Current
            {
                get { return this.bytes[this.position]; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                this.position = this.position + 1;
                return this.position < this.length;
            }

            public void Reset()
            {
                this.position = this.offset - 1;
            }

            public void Dispose()
            {
                this.Dispose(true);
            }

            protected virtual void Dispose(bool dispose)
            {
                if (dispose)
                {
                    this.bytes = null;
                    this.offset = 0;
                    this.length = 0;
                    this.position = -1;
                }
            }

            ~ByteEnumerator()
            {
                this.Dispose(false);
            }
        }


        public IEnumerator<byte> GetEnumerator()
        {
            return new ByteEnumerator(this.Bytes, this.Offset, this.Length);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}