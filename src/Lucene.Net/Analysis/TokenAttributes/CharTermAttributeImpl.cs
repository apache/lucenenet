using J2N.Text;
using Lucene.Net.Support;
using System;
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
    /// Default implementation of <see cref="ICharTermAttribute"/>. </summary>
    public class CharTermAttribute : Attribute, ICharTermAttribute, ITermToBytesRefAttribute, IAppendable // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private const int MIN_BUFFER_SIZE = 10;

        private char[] termBuffer = CreateBuffer(MIN_BUFFER_SIZE);
        private int termLength = 0;

        /// <summary>
        /// Initialize this attribute with empty term text </summary>
        public CharTermAttribute()
        {
        }

        // LUCENENET specific - ICharSequence member from J2N
        bool ICharSequence.HasValue => termBuffer != null;

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

        char[] ICharTermAttribute.Buffer => termBuffer;

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

        int ICharTermAttribute.Length { get => Length; set => SetLength(value); }

        int ICharSequence.Length => Length;

        public int Length
        {
            get => termLength;
            set => SetLength(value);
        }

        public CharTermAttribute SetLength(int length)
        {
            // LUCENENET: added guard clause
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} must not be negative.");
            if (length > termBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(length), length, "length " + length + " exceeds the size of the termBuffer (" + termBuffer.Length + ")");

            termLength = length;
            return this;
        }

        public CharTermAttribute SetEmpty()
        {
            termLength = 0;
            return this;
        }

        // *** TermToBytesRefAttribute interface ***
        private BytesRef bytes = new BytesRef(MIN_BUFFER_SIZE);

        public virtual void FillBytesRef()
        {
            UnicodeUtil.UTF16toUTF8(termBuffer, 0, termLength, bytes);
        }

        public virtual BytesRef BytesRef => bytes;

        // *** CharSequence interface ***

        // LUCENENET specific: Replaced CharAt(int) with this[int] to .NETify

        char ICharSequence.this[int index] => this[index];

        char ICharTermAttribute.this[int index] { get => this[index]; set => this[index] = value; }

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

        public ICharSequence Subsequence(int startIndex, int length)
        {
            // From Apache Harmony String class
            if (termBuffer is null || (startIndex == 0 && length == termBuffer.Length))
            {
                return new CharArrayCharSequence(termBuffer);
            }
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"{nameof(startIndex)} must not be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} must not be negative.");
            if (startIndex > Length - length) // Checks for int overflow
                throw new ArgumentOutOfRangeException(nameof(length), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(length)} <= {nameof(Length)}.");

            char[] result = new char[length];
            for (int i = 0, j = startIndex; i < length; i++, j++)
                result[i] = termBuffer[j];

            return new CharArrayCharSequence(result);
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

            return Append(value.ToString());
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

            return Append(value.ToString(startIndex, charCount));
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
            int code = termLength;
            code = code * 31 + ArrayUtil.GetHashCode(termBuffer, 0, termLength);
            return code;
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

        void ICharTermAttribute.CopyBuffer(char[] buffer, int offset, int length) => CopyBuffer(buffer, offset, length);

        char[] ICharTermAttribute.ResizeBuffer(int newSize) => ResizeBuffer(newSize);

        ICharTermAttribute ICharTermAttribute.SetLength(int length) => SetLength(length);

        ICharTermAttribute ICharTermAttribute.SetEmpty() => SetEmpty();

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
    }
}