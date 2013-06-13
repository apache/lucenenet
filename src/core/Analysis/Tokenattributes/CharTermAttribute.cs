using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Tokenattributes
{
    public class CharTermAttribute : Lucene.Net.Util.Attribute, ICharTermAttribute, ITermToBytesRefAttribute, ICloneable
    {
        private const int MIN_BUFFER_SIZE = 10;

        private char[] termBuffer = new char[ArrayUtil.Oversize(MIN_BUFFER_SIZE, RamUsageEstimator.NUM_BYTES_CHAR)];
        private int termLength = 0;

        public CharTermAttribute()
        {
        }

        public void CopyBuffer(char[] buffer, int offset, int length)
        {
            GrowTermBuffer(length);
            Array.Copy(buffer, offset, termBuffer, 0, length);
            termLength = length;
        }

        public char[] Buffer
        {
            get { return termBuffer; }
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

        public ICharTermAttribute SetLength(int length)
        {
            if (length > termBuffer.Length)
                throw new ArgumentException("length " + length + " exceeds the size of the termBuffer (" + termBuffer.Length + ")");

            termLength = length;
            return this;
        }

        public ICharTermAttribute SetEmpty()
        {
            termLength = 0;
            return this;
        }

        private BytesRef bytes = new BytesRef(MIN_BUFFER_SIZE);

        public int FillBytesRef()
        {
            return UnicodeUtil.UTF16toUTF8WithHash(termBuffer, 0, termLength, bytes);
        }

        public Util.BytesRef BytesRef
        {
            get { return bytes; }
        }

        public int Length
        {
            get { return termLength; }
        }

        public char CharAt(int index)
        {
            if (index >= termLength)
                throw new IndexOutOfRangeException();

            return termBuffer[index];
        }

        public ICharSequence SubSequence(int start, int end)
        {
            if (start > termLength || end > termLength)
                throw new IndexOutOfRangeException();

            return new StringCharSequenceWrapper(new String(termBuffer, start, end - start));
        }

        public ICharTermAttribute Append(string s)
        {
            if (s == null) // needed for Appendable compliance
                return AppendNull();

            int len = s.Length;
            TextSupport.GetCharsFromString(s, 0, len, ResizeBuffer(termLength + len), termLength);
            termLength += len;
            return this;
        }

        public ICharTermAttribute Append(string s, int start, int end)
        {
            if (s == null) // needed for Appendable compliance
                s = "null";

            int len = end - start, slen = s.Length;

            if (len < 0 || start > slen || end > slen)
                throw new IndexOutOfRangeException();

            if (len == 0)
                return this;

            ResizeBuffer(termLength + len);

            if (len > 4)
            {
                TextSupport.GetCharsFromString(s, start, end, termBuffer, termLength);
                termLength += len;
            }
            else
            {
                while (start < end)
                    termBuffer[termLength++] = s[start++];

                return this;
            }
            return this;
        }

        public ICharTermAttribute Append(char c)
        {
            ResizeBuffer(termLength + 1)[termLength++] = c;
            return this;
        }

        public ICharTermAttribute Append(StringBuilder sb)
        {
            if (sb == null) // needed for Appendable compliance
                return AppendNull();

            int len = sb.Length;
            sb.CopyTo(0, ResizeBuffer(termLength + len), termLength, len);
            termLength += len;
            return this;
        }

        public ICharTermAttribute Append(ICharTermAttribute ta)
        {
            if (ta == null) // needed for Appendable compliance
                return AppendNull();

            int len = ta.Length;
            Array.Copy(ta.Buffer, 0, ResizeBuffer(termLength + len), termLength, len);
            termLength += len;
            return this;
        }

        public ICharTermAttribute Append(StringBuilder sb, int start, int end)
        {
            if (sb == null) // needed for Appendable compliance
                sb = new StringBuilder("null");

            int len = end - start, sblen = sb.Length;

            if (len < 0 || start > sblen || end > sblen)
                throw new IndexOutOfRangeException();

            if (len == 0)
                return this;

            ResizeBuffer(termLength + len);

            if (len > 4)
            {
                sb.CopyTo(start, termBuffer, termLength, end);
                termLength += len;
            }
            else
            {
                while (start < end)
                    termBuffer[termLength++] = sb[start++];

                return this;
            }
            return this;
        }

        public ICharTermAttribute Append(ICharSequence csq)
        {
            if (csq == null) // needed for Appendable compliance
                return AppendNull();

            return Append(csq, 0, csq.Length);
        }

        public ICharTermAttribute Append(ICharSequence csq, int start, int end)
        {
            if (csq == null) // needed for Appendable compliance
                csq = new StringCharSequenceWrapper("null");

            int len = end - start, csqlen = csq.Length;

            if (len < 0 || start > csqlen || end > csqlen)
                throw new IndexOutOfRangeException();

            if (len == 0)
                return this;

            ResizeBuffer(termLength + len);

            while (start < end)
                termBuffer[termLength++] = csq.CharAt(start++);
            // no fall-through here, as termLength is updated!
            return this;
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

        public override int GetHashCode()
        {
            int code = termLength;
            code = code * 31 + ArrayUtil.HashCode(termBuffer, 0, termLength);
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
            t.bytes = BytesRef.DeepCopyOf(bytes);
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
                    return false;
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

        public override string ToString()
        {
            return new String(termBuffer, 0, termLength);
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            reflector.Reflect<ICharTermAttribute>("term", ToString());
            FillBytesRef();
            reflector.Reflect<ITermToBytesRefAttribute>("bytes", BytesRef.DeepCopyOf(bytes));
        }

        public override void CopyTo(Lucene.Net.Util.Attribute target)
        {
            CharTermAttribute t = (CharTermAttribute)target;
            t.CopyBuffer(termBuffer, 0, termLength);
        }
    }
}
