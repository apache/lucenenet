using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using WritableArrayAttribute = Lucene.Net.Support.WritableArrayAttribute;

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
    /// Represents <see cref="T:byte[]"/>, as a slice (offset + length) into an
    /// existing <see cref="T:byte[]"/>.  The <see cref="Bytes"/> property should never be <c>null</c>;
    /// use <see cref="EMPTY_BYTES"/> if necessary.
    ///
    /// <para/><b>Important note:</b> Unless otherwise noted, Lucene uses this class to
    /// represent terms that are encoded as <b>UTF8</b> bytes in the index. To
    /// convert them to a .NET <see cref="string"/> (which is UTF16), use <see cref="Utf8ToString()"/>.
    /// Using code like <c>new String(bytes, offset, length)</c> to do this
    /// is <b>wrong</b>, as it does not respect the correct character set
    /// and may return wrong results (depending on the platform's defaults)!
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    [DebuggerDisplay("{ToString()} {Utf8ToString()}")]
    public sealed class BytesRef : IComparable<BytesRef>, IComparable, IEquatable<BytesRef> // LUCENENET specific - implemented IComparable for FieldComparator, IEquatable<BytesRef>
    {
        /// <summary>
        /// An empty byte array for convenience </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly byte[] EMPTY_BYTES = Arrays.Empty<byte>();

        /// <summary>
        /// The contents of the BytesRef. Should never be <c>null</c>.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public byte[] Bytes
        {
            get => bytes;
            set => bytes = value; // LUCENENET NOTE: Although the comments state this cannot be null, some of the tests depend on setting it to null!
        }
        private byte[] bytes;

        /// <summary>
        /// Offset of first valid byte.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Length of used bytes.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Create a <see cref="BytesRef"/> with <see cref="EMPTY_BYTES"/> </summary>
        public BytesRef()
            : this(EMPTY_BYTES)
        {
        }

        /// <summary>
        /// This instance will directly reference <paramref name="bytes"/> w/o making a copy.
        /// <paramref name="bytes"/> should not be <c>null</c>.
        /// </summary>
        public BytesRef(byte[] bytes, int offset, int length)
        {
            this.bytes = bytes;
            this.Offset = offset;
            this.Length = length;
            if (Debugging.AssertsEnabled) Debugging.Assert(IsValid());
        }

        /// <summary>
        /// This instance will directly reference <paramref name="bytes"/> w/o making a copy.
        /// <paramref name="bytes"/> should not be <c>null</c>.
        /// </summary>
        public BytesRef(byte[] bytes)
            : this(bytes, 0, bytes.Length)
        {
        }

        /// <summary>
        /// Create a <see cref="BytesRef"/> pointing to a new array of size <paramref name="capacity"/>.
        /// Offset and length will both be zero.
        /// </summary>
        public BytesRef(int capacity)
        {
            this.bytes = new byte[capacity];
        }

        /// <summary>
        /// Initialize the <see cref="T:byte[]"/> from the UTF8 bytes
        /// for the provided <see cref="ICharSequence"/>.
        /// </summary>
        /// <param name="text"> This must be well-formed
        /// unicode text, with no unpaired surrogates. </param>
        public BytesRef(ICharSequence text)
            : this()
        {
            CopyChars(text);
        }

        /// <summary>
        /// Initialize the <see cref="T:byte[]"/> from the UTF8 bytes
        /// for the provided <see cref="string"/>.
        /// </summary>
        /// <param name="text"> This must be well-formed
        /// unicode text, with no unpaired surrogates. </param>
        public BytesRef(string text)
            : this()
        {
            CopyChars(text);
        }

        /// <summary>
        /// Copies the UTF8 bytes for this <see cref="ICharSequence"/>.
        /// </summary>
        /// <param name="text"> Must be well-formed unicode text, with no
        /// unpaired surrogates or invalid UTF16 code units. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyChars(ICharSequence text)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Offset == 0); // TODO broken if offset != 0
            UnicodeUtil.UTF16toUTF8(text, 0, text.Length, this);
        }

        /// <summary>
        /// Copies the UTF8 bytes for this <see cref="string"/>.
        /// </summary>
        /// <param name="text"> Must be well-formed unicode text, with no
        /// unpaired surrogates or invalid UTF16 code units. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyChars(string text)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Offset == 0); // TODO broken if offset != 0
            UnicodeUtil.UTF16toUTF8(text, 0, text.Length, this);
        }

        /// <summary>
        /// Expert: Compares the bytes against another <see cref="BytesRef"/>,
        /// returning <c>true</c> if the bytes are equal.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="other"> Another <see cref="BytesRef"/>, should not be <c>null</c>. </param>
        public bool BytesEquals(BytesRef other)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(other != null);
            if (Length == other.Length)
            {
                var otherUpto = other.Offset;
                var otherBytes = other.bytes;
                var end = Offset + Length;
                for (int upto = Offset; upto < end; upto++, otherUpto++)
                {
                    if (bytes[upto] != otherBytes[otherUpto])
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
        /// <seealso cref="DeepCopyOf(BytesRef)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Clone()
        {
            return new BytesRef(bytes, Offset, Length);
        }

        /// <summary>
        /// Calculates the hash code as required by <see cref="Index.TermsHash"/> during indexing.
        /// <para/> This is currently implemented as MurmurHash3 (32
        /// bit), using the seed from 
        /// <see cref="StringHelper.GoodFastHashSeed"/>, but is subject to
        /// change from release to release.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return StringHelper.Murmurhash3_x86_32(this, StringHelper.GoodFastHashSeed);
        }

        public override bool Equals(object other)
        {
            if (other is null)
                return false;

            if (other is BytesRef otherBytes)
                return this.BytesEquals(otherBytes);

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IEquatable<BytesRef>.Equals(BytesRef other) // LUCENENET specific - implemented IEquatable<BytesRef>
            => BytesEquals(other);

        /// <summary>
        /// Interprets stored bytes as UTF8 bytes, returning the
        /// resulting <see cref="string"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Utf8ToString()
        {
            CharsRef @ref = new CharsRef(Length);
            UnicodeUtil.UTF8toUTF16(bytes, Offset, Length, @ref);
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
                sb.Append((bytes[i] & 0xff).ToString("x"));
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Copies the bytes from the given <see cref="BytesRef"/>
        /// <para/>
        /// NOTE: if this would exceed the array size, this method creates a
        /// new reference array.
        /// </summary>
        public void CopyBytes(BytesRef other)
        {
            if (Bytes.Length - Offset < other.Length)
            {
                bytes = new byte[other.Length];
                Offset = 0;
            }
            Arrays.Copy(other.bytes, other.Offset, bytes, Offset, other.Length);
            Length = other.Length;
        }

        /// <summary>
        /// Appends the bytes from the given <see cref="BytesRef"/>
        /// <para/>
        /// NOTE: if this would exceed the array size, this method creates a
        /// new reference array.
        /// </summary>
        public void Append(BytesRef other)
        {
            int newLen = Length + other.Length;
            if (bytes.Length - Offset < newLen)
            {
                var newBytes = new byte[newLen];
                Arrays.Copy(bytes, Offset, newBytes, 0, Length);
                Offset = 0;
                bytes = newBytes;
            }
            Arrays.Copy(other.bytes, other.Offset, bytes, Length + Offset, other.Length);
            Length = newLen;
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
            if (Debugging.AssertsEnabled) Debugging.Assert(Offset == 0); // NOTE: senseless if offset != 0
            bytes = ArrayUtil.Grow(bytes, newLength);
        }

        /// <summary>
        /// Unsigned byte order comparison </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object other) // LUCENENET specific: Implemented IComparable for FieldComparer
        {
            BytesRef br = other as BytesRef;
            if (Debugging.AssertsEnabled) Debugging.Assert(br != null);
            return utf8SortedAsUnicodeSortOrder.Compare(this, br);
        }

        /// <summary>
        /// Unsigned byte order comparison </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(BytesRef other)
        {
            return utf8SortedAsUnicodeSortOrder.Compare(this, other);
        }

        private static readonly IComparer<BytesRef> utf8SortedAsUnicodeSortOrder = Utf8SortedAsUnicodeComparer.Instance;

        public static IComparer<BytesRef> UTF8SortedAsUnicodeComparer => utf8SortedAsUnicodeSortOrder;

        // LUCENENET NOTE: De-nested Utf8SortedAsUnicodeComparer class to prevent naming conflict

        /// @deprecated this comparer is only a transition mechanism
        [Obsolete("this comparer is only a transition mechanism")]
        private static readonly IComparer<BytesRef> utf8SortedAsUTF16SortOrder = new Utf8SortedAsUtf16Comparer();

        /// @deprecated this comparer is only a transition mechanism
        [Obsolete("this comparer is only a transition mechanism")]
        public static IComparer<BytesRef> UTF8SortedAsUTF16Comparer => utf8SortedAsUTF16SortOrder;

        // LUCENENET NOTE: De-nested Utf8SortedAsUtf16Comparer class to prevent naming conflict



        /// <summary>
        /// Creates a new <see cref="BytesRef"/> that points to a copy of the bytes from
        /// <paramref name="other"/>.
        /// <para/>
        /// The returned <see cref="BytesRef"/> will have a length of <c>other.Length</c>
        /// and an offset of zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BytesRef DeepCopyOf(BytesRef other)
        {
            BytesRef copy = new BytesRef();
            copy.CopyBytes(other);
            return copy;
        }

        /// <summary>
        /// Performs internal consistency checks.
        /// Always returns true (or throws <see cref="InvalidOperationException"/>)
        /// </summary>
        public bool IsValid()
        {
            if (Bytes is null)
            {
                throw IllegalStateException.Create("bytes is null");
            }
            if (Length < 0)
            {
                throw IllegalStateException.Create("length is negative: " + Length);
            }
            if (Length > Bytes.Length)
            {
                throw IllegalStateException.Create("length is out of bounds: " + Length + ",bytes.length=" + Bytes.Length);
            }
            if (Offset < 0)
            {
                throw IllegalStateException.Create("offset is negative: " + Offset);
            }
            if (Offset > Bytes.Length)
            {
                throw IllegalStateException.Create("offset out of bounds: " + Offset + ",bytes.length=" + Bytes.Length);
            }
            if (Offset + Length < 0)
            {
                throw IllegalStateException.Create("offset+length is negative: offset=" + Offset + ",length=" + Length);
            }
            if (Offset + Length > Bytes.Length)
            {
                throw IllegalStateException.Create("offset+length out of bounds: offset=" + Offset + ",length=" + Length + ",bytes.length=" + Bytes.Length);
            }
            return true;
        }
    }

    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal class Utf8SortedAsUnicodeComparer : IComparer<BytesRef>
    {
        public static Utf8SortedAsUnicodeComparer Instance = new Utf8SortedAsUnicodeComparer();

        // Only singleton
        private Utf8SortedAsUnicodeComparer()
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

    /// @deprecated this comparer is only a transition mechanism
    [Obsolete("this comparer is only a transition mechanism")]
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal class Utf8SortedAsUtf16Comparer : IComparer<BytesRef>
    {
        // Only singleton
        internal Utf8SortedAsUtf16Comparer()
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

    // LUCENENET specific
    internal enum BytesRefFormat // For assert/test/logging
    {
        UTF8,
        UTF8AsHex
    }

    // LUCENENET specific - when this object is a parameter of 
    // a method that calls string.Format(),
    // defers execution of building a string until
    // string.Format() is called.
    // This struct is meant to wrap a directory parameter when passed as a string.Format() argument.
    internal struct BytesRefFormatter // For assert/test/logging
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private BytesRef bytesRef;
        private BytesRefFormat format;
#pragma warning restore IDE0044 // Add readonly modifier
        public BytesRefFormatter(BytesRef bytesRef, BytesRefFormat format)
        {
            this.bytesRef = bytesRef; // Allow null
            this.format = format;
        }

        public override string ToString()
        {
            // Special case: null
            if (bytesRef is null)
                return "null";

            switch (format)
            {
                case BytesRefFormat.UTF8:
                    try
                    {
                        return bytesRef.Utf8ToString();
                    }
                    catch (Exception e) when (e.IsIndexOutOfBoundsException())
                    {
                        return bytesRef.ToString();
                    }
                case BytesRefFormat.UTF8AsHex:
                    return UnicodeUtil.ToHexString(bytesRef.Utf8ToString());
                default:
                    return bytesRef.ToString();
            }
        }
    }
}