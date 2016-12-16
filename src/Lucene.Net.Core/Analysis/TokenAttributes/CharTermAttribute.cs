using Lucene.Net.Support;
using System;
using System.Linq;
using System.Text;

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
    using IAttributeReflector = Lucene.Net.Util.IAttributeReflector;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Default implementation of <seealso cref="CharTermAttribute"/>. </summary>
    public class CharTermAttribute : Attribute, ICharTermAttribute, ITermToBytesRefAttribute
#if FEATURE_CLONEABLE
        , ICloneable
#endif
    {
        private static int MIN_BUFFER_SIZE = 10;

        private char[] termBuffer = CreateBuffer(MIN_BUFFER_SIZE);
        private int termLength = 0;

        /// <summary>
        /// Initialize this attribute with empty term text </summary>
        public CharTermAttribute()
        {
        }

        public void CopyBuffer(char[] buffer, int offset, int length)
        {
            GrowTermBuffer(length);
            Array.Copy(buffer, offset, termBuffer, 0, length);
            termLength = length;
        }

        public char[] Buffer()
        {
            return termBuffer;
        }

        public char[] ResizeBuffer(int newSize)
        {
            if (termBuffer.Length < newSize)
            {
                // Not big enough; create a new array with slight
                // over allocation and preserve content
                char[] newCharBuffer = new char[ArrayUtil.Oversize(newSize, RamUsageEstimator.NUM_BYTES_CHAR)];
                Array.Copy(termBuffer, 0, newCharBuffer, 0, termBuffer.Length);
                termBuffer = newCharBuffer;
            }
            return termBuffer;
        }

        private void GrowTermBuffer(int newSize)
        {
            if (termBuffer.Length < newSize)
            {
                // Not big enough; create a new array with slight
                // over allocation:
                termBuffer = new char[ArrayUtil.Oversize(newSize, RamUsageEstimator.NUM_BYTES_CHAR)];
            }
        }

        public int Length
        {
            get { return this.termLength; }
            set { this.SetLength(value); }
        }

        public ICharTermAttribute SetLength(int length)
        {
            if (length > termBuffer.Length)
            {
                throw new System.ArgumentException("length " + length + " exceeds the size of the termBuffer (" + termBuffer.Length + ")");
            }
            termLength = length;
            return this;
        }

        public ICharTermAttribute SetEmpty()
        {
            termLength = 0;
            return this;
        }

        // *** TermToBytesRefAttribute interface ***
        private BytesRef Bytes = new BytesRef(MIN_BUFFER_SIZE);

        public virtual void FillBytesRef()
        {
            UnicodeUtil.UTF16toUTF8(termBuffer, 0, termLength, Bytes);
        }

        public virtual BytesRef BytesRef
        {
            get
            {
                return Bytes;
            }
        }

        // *** CharSequence interface ***

        // LUCENENET specific: Replaced with this[int] to .NETify
        //public char CharAt(int index)
        //{
        //    if (index >= TermLength)
        //    {
        //        throw new IndexOutOfRangeException();
        //    }
        //    return TermBuffer[index];
        //}

        // LUCENENET specific indexer to make CharTermAttribute act more like a .NET type
        public char this[int index]
        {
            get
            {
                if (index >= termLength)
                {
                    throw new IndexOutOfRangeException("index");
                }

                return termBuffer[index];
            }
            set
            {
                if (index >= termLength)
                {
                    throw new IndexOutOfRangeException("index");
                }

                termBuffer[index] = value;
            }
        }

        public ICharSequence SubSequence(int start, int end)
        {
            if (start > termLength || end > termLength)
            {
                throw new IndexOutOfRangeException();
            }
            return new StringCharSequenceWrapper(new string(termBuffer, start, end - start));
        }

        // *** Appendable interface ***

        public ICharTermAttribute Append(string csq, int start, int end)
        {
            if (csq == null)
                return AppendNull();

            int len = end - start, csqlen = csq.Length;
            if (len < 0 || start > csqlen || end > csqlen)
                throw new IndexOutOfRangeException();
            if (len == 0)
                return this;

            csq.CopyTo(start, InternalResizeBuffer(termLength + len), termLength, len);
            Length += len;

            return this;
        }

        public ICharTermAttribute Append(char c)
        {
            ResizeBuffer(termLength + 1)[termLength++] = c;
            return this;
        }

        public ICharTermAttribute Append(char[] chars)
        {
            if (chars == null)
                return AppendNull();

            int len = chars.Length;
            chars.CopyTo(InternalResizeBuffer(termLength + len), termLength);
            Length += len;

            return this;
        }

        public ICharTermAttribute Append(char[] chars, int start, int end)
        {
            if (chars == null)
                return AppendNull();

            int len = end - start, csqlen = chars.Length;
            if (len < 0 || start > csqlen || end > csqlen)
                throw new IndexOutOfRangeException();
            if (len == 0)
                return this;

            chars.Skip(start).Take(len).ToArray().CopyTo(InternalResizeBuffer(termLength + len), termLength);
            Length += len;

            return this;
        }

        public ICharTermAttribute Append(string s)
        {
            return Append(s, 0, s == null ? 0 : s.Length);
        }

        public ICharTermAttribute Append(StringBuilder s)
        {
            if (s == null) // needed for Appendable compliance
            {
                return AppendNull();
            }

            return Append(s.ToString());
        }

        public ICharTermAttribute Append(StringBuilder s, int start, int end)
        {
            if (s == null) // needed for Appendable compliance
            {
                return AppendNull();
            }

            int len = end - start, csqlen = s.Length;
            if (len < 0 || start > csqlen || end > csqlen)
                throw new IndexOutOfRangeException();
            if (len == 0)
                return this;

            return Append(s.ToString(start, end - start));
        }

        public ICharTermAttribute Append(ICharTermAttribute ta)
        {
            if (ta == null) // needed for Appendable compliance
            {
                return AppendNull();
            }
            int len = ta.Length;
            Array.Copy(ta.Buffer(), 0, ResizeBuffer(termLength + len), termLength, len);
            termLength += len;
            return this;
        }

        public ICharTermAttribute Append(ICharSequence csq)
        {
            if (csq == null)
                return AppendNull();

            return Append(csq, 0, csq.Length);
        }

        public ICharTermAttribute Append(ICharSequence csq, int start, int end)
        {
            if (csq == null)
                csq = new StringCharSequenceWrapper("null");

            int len = end - start, csqlen = csq.Length;

            if (len < 0 || start > csqlen || end > csqlen)
                throw new IndexOutOfRangeException();

            if (len == 0)
                return this;

            ResizeBuffer(termLength + len);

            while (start < end)
                termBuffer[termLength++] = csq[start++];

            return this;
        }

        private char[] InternalResizeBuffer(int length)
        {
            if (termBuffer.Length < length)
            {
                char[] newBuffer = CreateBuffer(length);
                Array.Copy(termBuffer, 0, newBuffer, 0, termBuffer.Length);
                this.termBuffer = newBuffer;
            }

            return termBuffer;
        }

        private static char[] CreateBuffer(int length)
        {
            return new char[ArrayUtil.Oversize(length, RamUsageEstimator.NUM_BYTES_CHAR)];
        }

        private CharTermAttribute AppendNull()
        {
            ResizeBuffer(termLength + 4);
            termBuffer[termLength++] = 'n';
            termBuffer[termLength++] = 'u';
            termBuffer[termLength++] = 'l';
            termBuffer[termLength++] = 'l';
            return this;
        }

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
            Array.Copy(this.termBuffer, 0, t.termBuffer, 0, this.termLength);
            t.Bytes = BytesRef.DeepCopyOf(Bytes);
            return t;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other is CharTermAttribute)
            {
                CharTermAttribute o = ((CharTermAttribute)other);
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
        /// <seealso cref="CharSequence"/> interface.
        /// <p>this method changed the behavior with Lucene 3.1,
        /// before it returned a String representation of the whole
        /// term with all attributes.
        /// this affects especially the
        /// <seealso cref="Lucene.Net.Analysis.Token"/> subclass.
        /// </summary>
        public override string ToString()
        {
            return new string(termBuffer, 0, termLength);
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            reflector.Reflect(typeof(ICharTermAttribute), "term", ToString());
            FillBytesRef();
            reflector.Reflect(typeof(ITermToBytesRefAttribute), "bytes", BytesRef.DeepCopyOf(Bytes));
        }

        public override void CopyTo(Attribute target)
        {
            CharTermAttribute t = (CharTermAttribute)target;
            t.CopyBuffer(termBuffer, 0, termLength);
        }
    }
}