using Lucene.Net.Support;
using System;
using System.Linq;
using System.Text;

// LUCENENET TODO: Rename this namespace to TokenAttributes (.NETify)
namespace Lucene.Net.Analysis.Tokenattributes
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
    {
        private static int MIN_BUFFER_SIZE = 10;

        private char[] TermBuffer = CreateBuffer(MIN_BUFFER_SIZE);
        private int TermLength = 0;

        /// <summary>
        /// Initialize this attribute with empty term text </summary>
        public CharTermAttribute()
        {
        }

        public void CopyBuffer(char[] buffer, int offset, int length)
        {
            GrowTermBuffer(length);
            Array.Copy(buffer, offset, TermBuffer, 0, length);
            TermLength = length;
        }

        public char[] Buffer()
        {
            return TermBuffer;
        }

        public char[] ResizeBuffer(int newSize)
        {
            if (TermBuffer.Length < newSize)
            {
                // Not big enough; create a new array with slight
                // over allocation and preserve content
                char[] newCharBuffer = new char[ArrayUtil.Oversize(newSize, RamUsageEstimator.NUM_BYTES_CHAR)];
                Array.Copy(TermBuffer, 0, newCharBuffer, 0, TermBuffer.Length);
                TermBuffer = newCharBuffer;
            }
            return TermBuffer;
        }

        private void GrowTermBuffer(int newSize)
        {
            if (TermBuffer.Length < newSize)
            {
                // Not big enough; create a new array with slight
                // over allocation:
                TermBuffer = new char[ArrayUtil.Oversize(newSize, RamUsageEstimator.NUM_BYTES_CHAR)];
            }
        }

        public int Length
        {
            get { return this.TermLength; }
            set { this.SetLength(value); }
        }

        public ICharTermAttribute SetLength(int length)
        {
            if (length > TermBuffer.Length)
            {
                throw new System.ArgumentException("length " + length + " exceeds the size of the termBuffer (" + TermBuffer.Length + ")");
            }
            TermLength = length;
            return this;
        }

        public ICharTermAttribute SetEmpty()
        {
            TermLength = 0;
            return this;
        }

        // *** TermToBytesRefAttribute interface ***
        private BytesRef Bytes = new BytesRef(MIN_BUFFER_SIZE);

        public virtual void FillBytesRef()
        {
            UnicodeUtil.UTF16toUTF8(TermBuffer, 0, TermLength, Bytes);
        }

        public BytesRef BytesRef
        {
            get
            {
                return Bytes;
            }
        }

        // *** CharSequence interface ***

        public char CharAt(int index)
        {
            if (index >= TermLength)
            {
                throw new IndexOutOfRangeException();
            }
            return TermBuffer[index];
        }

        // LUCENENET specific indexer to make CharTermAttribute act more like a .NET type
        public char this[int index]
        {
            get
            {
                if (index >= TermLength)
                {
                    throw new IndexOutOfRangeException("index");
                }

                return TermBuffer[index];
            }
            set
            {
                if (index >= TermLength)
                {
                    throw new IndexOutOfRangeException("index");
                }

                TermBuffer[index] = value;
            }
        }

        public ICharSequence SubSequence(int start, int end)
        {
            if (start > TermLength || end > TermLength)
            {
                throw new IndexOutOfRangeException();
            }
            return new StringCharSequenceWrapper(new string(TermBuffer, start, end - start));
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

            csq.CopyTo(start, InternalResizeBuffer(TermLength + len), TermLength, len);
            Length += len;

            return this;
        }

        public ICharTermAttribute Append(char c)
        {
            ResizeBuffer(TermLength + 1)[TermLength++] = c;
            return this;
        }

        public ICharTermAttribute Append(char[] chars)
        {
            if (chars == null)
                return AppendNull();

            int len = chars.Length;
            chars.CopyTo(InternalResizeBuffer(TermLength + len), TermLength);
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

            chars.Skip(start).Take(len).ToArray().CopyTo(InternalResizeBuffer(TermLength + len), TermLength);
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
            Array.Copy(ta.Buffer(), 0, ResizeBuffer(TermLength + len), TermLength, len);
            TermLength += len;
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

            ResizeBuffer(TermLength + len);

            while (start < end)
                TermBuffer[TermLength++] = csq.CharAt(start++);

            return this;
        }

        private char[] InternalResizeBuffer(int length)
        {
            if (TermBuffer.Length < length)
            {
                char[] newBuffer = CreateBuffer(length);
                Array.Copy(TermBuffer, 0, newBuffer, 0, TermBuffer.Length);
                this.TermBuffer = newBuffer;
            }

            return TermBuffer;
        }

        private static char[] CreateBuffer(int length)
        {
            return new char[ArrayUtil.Oversize(length, RamUsageEstimator.NUM_BYTES_CHAR)];
        }

        private CharTermAttribute AppendNull()
        {
            ResizeBuffer(TermLength + 4);
            TermBuffer[TermLength++] = 'n';
            TermBuffer[TermLength++] = 'u';
            TermBuffer[TermLength++] = 'l';
            TermBuffer[TermLength++] = 'l';
            return this;
        }

        // *** Attribute ***

        public override int GetHashCode()
        {
            int code = TermLength;
            code = code * 31 + ArrayUtil.GetHashCode(TermBuffer, 0, TermLength);
            return code;
        }

        public override void Clear()
        {
            TermLength = 0;
        }

        public override object Clone()
        {
            CharTermAttribute t = (CharTermAttribute)base.Clone();
            // Do a deep clone
            t.TermBuffer = new char[this.TermLength];
            Array.Copy(this.TermBuffer, 0, t.TermBuffer, 0, this.TermLength);
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
                if (TermLength != o.TermLength)
                {
                    return false;
                }
                for (int i = 0; i < TermLength; i++)
                {
                    if (TermBuffer[i] != o.TermBuffer[i])
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
            return new string(TermBuffer, 0, TermLength);
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
            t.CopyBuffer(TermBuffer, 0, TermLength);
        }
    }
}