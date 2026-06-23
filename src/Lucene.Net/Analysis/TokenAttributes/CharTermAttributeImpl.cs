using J2N.Text;
using Lucene.Net.Support;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using WritableArrayAttribute = Lucene.Net.Support.WritableArrayAttribute;

namespace Lucene.Net.Analysis.TokenAttributes
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using Attribute = Lucene.Net.Util.Attribute;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IAttribute = Lucene.Net.Util.IAttribute;
    using IAttributeReflector = Lucene.Net.Util.IAttributeReflector;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Default implementation of <see cref="ICharTermAttribute"/>.
    /// </summary>
    /// <remarks>
    /// LUCENENET specific: This type implements <see cref="IBufferWriter{T}"/> to allow for efficient access to the underlying buffer.
    /// Note that if you hold a view into the buffer via either <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/>,
    /// this view can be invalidated by any operation that changes the position or length of the buffer.
    /// It is recommended to avoid any non-<see cref="IBufferWriter{T}"/> operations while holding a view into the buffer.
    /// <para />
    /// This type also implements <see cref="ISpanAppendable"/> to allow for efficient appending of <see cref="ReadOnlySpan{T}"/>.
    /// </remarks>
    public class CharTermAttribute : Attribute, ICharTermAttribute, ITermToBytesRefAttribute, IAppendable, // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
        ISpanAppendable /* LUCENENET specific */
    {
        private const int MIN_BUFFER_SIZE = 10;

        internal char[] termBuffer = CreateBuffer(MIN_BUFFER_SIZE);
        private int termLength = 0;

        /// <summary>
        /// Initialize this attribute with empty term text </summary>
        public CharTermAttribute()
        {
        }

        public void CopyBuffer(char[] buffer, int offset, int length)
        {
            // LUCENENET: Added guard clauses.
            // Note that this is the order the Apache Harmony tests expect it to be checked in.
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(offset)} must not be negative.");
            // LUCENENET specific - Added guard clause for null
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset > buffer.Length - length) // LUCENENET: Checks for int overflow
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(offset)} + {nameof(length)} may not be greater than the size of {nameof(buffer)}");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} must not be negative.");

            GrowTermBuffer(length);
            Arrays.Copy(buffer, offset, termBuffer, 0, length);
            termLength = length;
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public char[] Buffer => termBuffer;

        public char[] ResizeBuffer(int newSize)
        {
            // LUCENENET: added guard clause
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize), newSize, $"{nameof(newSize)} must not be negative.");

            if (termBuffer.Length < newSize)
            {
                // Not big enough; create a new array with slight
                // over allocation and preserve content

                // LUCENENET: Resize rather than copy
                Array.Resize(ref termBuffer, ArrayUtil.Oversize(newSize, RamUsageEstimator.NUM_BYTES_CHAR));
            }
            return termBuffer;
        }

        private void GrowTermBuffer(int newSize)
        {
            // LUCENENET: added guard clause
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize), newSize, $"{nameof(newSize)} must not be negative.");

            if (termBuffer.Length < newSize)
            {
                // Not big enough; create a new array with slight
                // over allocation:
                termBuffer = new char[ArrayUtil.Oversize(newSize, RamUsageEstimator.NUM_BYTES_CHAR)];
            }
        }

        public int Length
        {
            get => termLength;
            set
            {
                // LUCENENET: added guard clause
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must not be negative.");
                if (value > termBuffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"length {value} exceeds the size of the termBuffer ({termBuffer.Length})");

                termLength = value;
            }
        }

        // *** TermToBytesRefAttribute interface ***
        private BytesRef bytes = new BytesRef(MIN_BUFFER_SIZE);

        public virtual void FillBytesRef()
        {
            UnicodeUtil.UTF16toUTF8(termBuffer, 0, termLength, bytes);
        }

        public virtual BytesRef BytesRef => bytes;

        // *** CharSequence interface ***

        // LUCENENET specific indexer to make CharTermAttribute act more like a .NET type
        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= termLength) // LUCENENET: Added better bounds checking
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return termBuffer[index];
            }
            set
            {
                if (index < 0 || index >= termLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(index)); // LUCENENET: Added better bounds checking
                }

                termBuffer[index] = value;
            }
        }

        // *** Appendable interface ***

        public CharTermAttribute Append(string value, int startIndex, int charCount)
        {
            // LUCENENET: Changed semantics to be the same as the StringBuilder in .NET
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), $"{nameof(charCount)} must not be negative.");

            if (value is null)
            {
                if (startIndex == 0 && charCount == 0)
                    return this;
                throw new ArgumentNullException(nameof(value));
            }
            if (charCount == 0)
                return this;
            if (startIndex > value.Length - charCount)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(charCount)} <= {nameof(Length)}.");

            value.CopyTo(startIndex, InternalResizeBuffer(termLength + charCount), termLength, charCount);
            Length += charCount;

            return this;
        }

        public CharTermAttribute Append(char value)
        {
            ResizeBuffer(termLength + 1)[termLength++] = value;
            return this;
        }

        public CharTermAttribute Append(char[] value)
        {
            if (value is null)
                //return AppendNull();
                return this; // No-op

            int len = value.Length;
            value.CopyTo(InternalResizeBuffer(termLength + len), termLength);
            Length += len;

            return this;
        }

        public CharTermAttribute Append(char[] value, int startIndex, int charCount)
        {
            // LUCENENET: Changed semantics to be the same as the StringBuilder in .NET
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), $"{nameof(charCount)} must not be negative.");

            if (value is null)
            {
                if (startIndex == 0 && charCount == 0)
                    return this;
                throw new ArgumentNullException(nameof(value));
            }
            if (charCount == 0)
                return this;
            if (startIndex > value.Length - charCount)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(charCount)} <= {nameof(Length)}.");

            Arrays.Copy(value, startIndex, InternalResizeBuffer(termLength + charCount), termLength, charCount);
            Length += charCount;

            return this;
        }

        public CharTermAttribute Append(string value)
        {
            return Append(value, 0, value?.Length ?? 0);
        }

        public CharTermAttribute Append(StringBuilder value)
        {
            if (value is null) // needed for Appendable compliance
            {
                //return AppendNull();
                return this; // No-op
            }

            int len = value.Length;
            value.CopyTo(0, InternalResizeBuffer(termLength + len), termLength, len);
            Length += len;
            return this;
        }

        public CharTermAttribute Append(StringBuilder value, int startIndex, int charCount)
        {
            // LUCENENET: Changed semantics to be the same as the StringBuilder in .NET
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), $"{nameof(charCount)} must not be negative.");

            if (value is null)
            {
                if (startIndex == 0 && charCount == 0)
                    return this;
                throw new ArgumentNullException(nameof(value));
            }
            if (charCount == 0)
                return this;
            if (startIndex > value.Length - charCount)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(charCount)} <= {nameof(Length)}.");

            value.CopyTo(startIndex, InternalResizeBuffer(termLength + charCount), termLength, charCount);
            Length += charCount;
            return this;
        }

        public CharTermAttribute Append(ICharTermAttribute value)
        {
            if (value is null) // needed for Appendable compliance
            {
                //return AppendNull();
                return this; // No-op
            }
            int len = value.Length;
            Arrays.Copy(value.Buffer, 0, ResizeBuffer(termLength + len), termLength, len);
            termLength += len;
            return this;
        }

        public CharTermAttribute Append(ICharSequence value)
        {
            if (value is null)
                //return AppendNull();
                return this; // No-op

            return Append(value, 0, value.Length);
        }

        public CharTermAttribute Append(ICharSequence value, int startIndex, int charCount)
        {
            // LUCENENET: Changed semantics to be the same as the StringBuilder in .NET
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount), $"{nameof(charCount)} must not be negative.");

            if (value is null)
            {
                if (startIndex == 0 && charCount == 0)
                    return this;
                throw new ArgumentNullException(nameof(value));
            }
            if (charCount == 0)
                return this;
            if (startIndex > value.Length - charCount)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(charCount)} <= {nameof(Length)}.");

            ResizeBuffer(termLength + charCount);

            for (int i = 0; i < charCount; i++)
                termBuffer[termLength++] = value[startIndex + i];

            return this;
        }

        public CharTermAttribute Append(ReadOnlySpan<char> value)
        {
            if (value.Length == 0)
                return this;

            value.CopyTo(InternalResizeBuffer(termLength + value.Length).AsSpan(termLength));
            Length += value.Length;

            return this;
        }

        private char[] InternalResizeBuffer(int length)
        {
            if (termBuffer.Length < length)
            {
                char[] newBuffer = CreateBuffer(length);
                Arrays.Copy(termBuffer, 0, newBuffer, 0, termBuffer.Length);
                this.termBuffer = newBuffer;
            }

            return termBuffer;
        }

        private static char[] CreateBuffer(int length)
        {
            return new char[ArrayUtil.Oversize(length, RamUsageEstimator.NUM_BYTES_CHAR)];
        }

        // LUCENENET: Not used - we are doing a no-op when the value is null
        //private CharTermAttribute AppendNull()
        //{
        //    ResizeBuffer(termLength + 4);
        //    termBuffer[termLength++] = 'n';
        //    termBuffer[termLength++] = 'u';
        //    termBuffer[termLength++] = 'l';
        //    termBuffer[termLength++] = 'l';
        //    return this;
        //}

        // *** Attribute ***

        public override int GetHashCode()
        {
            unchecked
            {
                int code = termLength;
                code = code * 31 + ArrayUtil.GetHashCode(termBuffer, 0, termLength);
                return code;
            }
        }

        public override void Clear()
        {
            termLength = 0;
        }

        public override object Clone()
        {
            CharTermAttribute t = (CharTermAttribute)base.Clone();
            // Do a deep clone
            t.termBuffer = new char[this.termLength];
            Arrays.Copy(this.termBuffer, 0, t.termBuffer, 0, this.termLength);
            t.bytes = BytesRef.DeepCopyOf(bytes);
            return t;
        }

        public override bool Equals(object other)
        {
            if (other == this)
                return true;
            if (other is null) // LUCENENET: Added null check for better performance
                return false;

            if (other is CharTermAttribute o)
            {
                if (termLength != o.termLength)
                {
                    return false;
                }
                for (int i = 0; i < termLength; i++)
                {
                    if (termBuffer[i] != o.termBuffer[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns solely the term text as specified by the
        /// <see cref="ICharSequence"/> interface.
        /// <para/>
        /// this method changed the behavior with Lucene 3.1,
        /// before it returned a String representation of the whole
        /// term with all attributes.
        /// this affects especially the
        /// <see cref="Token"/> subclass.
        /// </summary>
        public override string ToString()
        {
            return new string(termBuffer, 0, termLength);
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            // LUCENENET: Added guard clause
            if (reflector is null)
                throw new ArgumentNullException(nameof(reflector));

            reflector.Reflect(typeof(ICharTermAttribute), "term", ToString());
            FillBytesRef();
            reflector.Reflect(typeof(ITermToBytesRefAttribute), "bytes", BytesRef.DeepCopyOf(bytes));
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not ICharTermAttribute t)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(ICharTermAttribute)}", nameof(target));
            t.CopyBuffer(termBuffer, 0, termLength);
        }

        #region ICharTermAttribute Members

        ICharTermAttribute ICharTermAttribute.Append(ICharSequence value) => Append(value);

        ICharTermAttribute ICharTermAttribute.Append(ICharSequence value, int startIndex, int count) => Append(value, startIndex, count);

        ICharTermAttribute ICharTermAttribute.Append(char value) => Append(value);

        ICharTermAttribute ICharTermAttribute.Append(char[] value) => Append(value);

        ICharTermAttribute ICharTermAttribute.Append(char[] value, int startIndex, int count) => Append(value, startIndex, count);

        ICharTermAttribute ICharTermAttribute.Append(string value) => Append(value);

        ICharTermAttribute ICharTermAttribute.Append(string value, int startIndex, int count) => Append(value, startIndex, count);

        ICharTermAttribute ICharTermAttribute.Append(StringBuilder value) => Append(value);

        ICharTermAttribute ICharTermAttribute.Append(StringBuilder value, int startIndex, int count) => Append(value, startIndex, count);

        ICharTermAttribute ICharTermAttribute.Append(ICharTermAttribute value) => Append(value);

        ICharTermAttribute ICharTermAttribute.Append(ReadOnlySpan<char> value) => Append(value);

        #endregion

        #region IAppendable Members

        IAppendable IAppendable.Append(char value) => Append(value);

        IAppendable IAppendable.Append(string value) => Append(value);

        IAppendable IAppendable.Append(string value, int startIndex, int count) => Append(value, startIndex, count);

        IAppendable IAppendable.Append(StringBuilder value) => Append(value);

        IAppendable IAppendable.Append(StringBuilder value, int startIndex, int count) => Append(value, startIndex, count);

        IAppendable IAppendable.Append(char[] value) => Append(value);

        IAppendable IAppendable.Append(char[] value, int startIndex, int count) => Append(value, startIndex, count);

        IAppendable IAppendable.Append(ICharSequence value) => Append(value);

        IAppendable IAppendable.Append(ICharSequence value, int startIndex, int count) => Append(value, startIndex, count);

        #endregion

        #region ISpanAppendable Members

        ISpanAppendable ISpanAppendable.Append(ReadOnlySpan<char> value) => Append(value);

        #endregion

        #region IBufferWriter<char> members and implementation support

        // LUCENENET-specific: IBufferWriter<char> support.
        // See ArrayBufferWriter for an inspiration and reference implementation:
        // https://github.com/dotnet/runtime/blob/v10.0.6/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs

        /// <summary>
        /// Notifies this instance that <paramref name="count"/> characters were
        /// written to the buffer returned by the most recent call to
        /// <see cref="GetSpan(int)"/> or <see cref="GetMemory(int)"/>, advancing
        /// the term length accordingly.
        /// <para/>
        /// LUCENENET specific - implements <see cref="System.Buffers.IBufferWriter{T}"/>.
        /// </summary>
        /// <param name="count">The number of characters written to the buffer.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Advancing by <paramref name="count"/> would move past the end of the
        /// available buffer space.
        /// </exception>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data
        /// and cannot write to a previously acquired buffer.
        /// </remarks>
        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count > termBuffer.Length - termLength)
                throw new InvalidOperationException($"Cannot advance {count} characters beyond the end of the buffer.");

            termLength += count;
        }

        /// <summary>
        /// Returns a <see cref="Memory{T}"/> to write to, starting at the current
        /// term length, that is at least <paramref name="sizeHint"/> characters in
        /// length (or 10, if not provided or <c>0</c>). The buffer is grown if necessary;
        /// the returned memory is never empty.
        /// <para/>
        /// LUCENENET specific - implements <see cref="System.Buffers.IBufferWriter{T}"/>.
        /// </summary>
        /// <param name="sizeHint">
        /// The minimum requested length of the returned <see cref="Memory{T}"/>.
        /// A value of <c>0</c> requests a buffer of minimum length 10.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sizeHint"/> is negative.
        /// </exception>
        public Memory<char> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBufferForBufferWriter(sizeHint);
            Debug.Assert(termBuffer.Length > termLength);
            return termBuffer.AsMemory(termLength);
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> to write to, starting at the current
        /// term length, that is at least <paramref name="sizeHint"/> characters in
        /// length (or 10, if not provided or <c>0</c>). The buffer is grown if necessary;
        /// the returned span is never empty.
        /// <para/>
        /// LUCENENET specific - implements <see cref="System.Buffers.IBufferWriter{T}"/>.
        /// </summary>
        /// <param name="sizeHint">
        /// The minimum requested length of the returned <see cref="Span{T}"/>.
        /// A value of <c>0</c> requests a buffer of minimum length 10.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sizeHint"/> is negative.
        /// </exception>
        public Span<char> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBufferForBufferWriter(sizeHint);
            Debug.Assert(termBuffer.Length > termLength);
            return termBuffer.AsSpan(termLength);
        }

        private void CheckAndResizeBufferForBufferWriter(int sizeHint)
        {
            if (sizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint), "sizeHint must be non-negative");
            }

            if (sizeHint == 0)
            {
                // LUCENENET NOTE: 10 is a reasonable, arbitrary minimum for text.
                // ArrayBufferWriter uses 1, just to ensure that it does not return
                // an empty buffer. If the caller does not care what size they get
                // back (by not providing a sizeHint), they have to work with what
                // is returned. This value should be enough for most short strings.
                sizeHint = MIN_BUFFER_SIZE;
            }

            _ = InternalResizeBuffer(termLength + sizeHint);
        }

        /// <summary>
        /// Returns the total amount of space within the underlying buffer.
        /// </summary>
        public int Capacity => termBuffer.Length;

        /// <summary>
        /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
        /// </summary>
        public int FreeCapacity => termBuffer.Length - termLength;

        #endregion
    }
}
