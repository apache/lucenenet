// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using WritableArrayAttribute = Lucene.Net.Support.WritableArrayAttribute;

namespace Lucene.Net.Analysis.Util
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
    /// A StringBuilder that allows one to access the array.
    /// </summary>
    public class OpenStringBuilder : IAppendable, ICharSequence
    {
        protected char[] m_buf;
        protected int m_len;

        public OpenStringBuilder()
            : this(32)
        {
        }

        bool ICharSequence.HasValue => m_buf != null;

        public OpenStringBuilder(int size)
        {
            m_buf = new char[size];
        }

        public OpenStringBuilder(char[] arr, int len)
        {
            // LUCENENET specific - calling private method instead of public virtual
            SetInternal(arr, len);
        }

        public virtual int Length
        {
            get => m_len;
            set => m_len = value;
        }

        public virtual void Set(char[] arr, int end) => SetInternal(arr, end);

        // LUCENENET specific - S1699 - introduced this to allow the constructor to
        // still call "Set" functionality without having to call the virtual method
        // that could be overridden by a subclass and don't have the state it expects
        private void SetInternal(char[] arr, int end)
        {
            this.m_buf = arr;
            this.m_len = end;
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] Array => m_buf;

        // LUCENENE NOTE: This is essentially a duplicate of Length (except that property can be set).
        // .NET uses Length for StringBuilder anyway, so that property is preferable to this one.
        //public virtual int Count // LUCENENET NOTE: This was size() in Lucene.
        //{
        //    get{ return m_len; }
        //}

        public virtual int Capacity => m_buf.Length;

        public virtual OpenStringBuilder Append(ICharSequence csq)
        {
            return Append(csq, 0, csq.Length);
        }

        public virtual OpenStringBuilder Append(ICharSequence csq, int startIndex, int count) // LUCENENET specific: changed to startIndex/length to match .NET
        {
            EnsureCapacity(count);
            UnsafeWrite(csq, startIndex, count);
            return this;
        }

        // LUCENENET specific - overload for string (more common in .NET than ICharSequence)
        public virtual OpenStringBuilder Append(string csq)
        {
            return Append(csq, 0, csq.Length);
        }

        // LUCENENET specific - overload for string (more common in .NET than ICharSequence)
        public virtual OpenStringBuilder Append(string csq, int startIndex, int count) // LUCENENET specific: changed to startIndex/length to match .NET
        {
            EnsureCapacity(count);
            UnsafeWrite(csq, startIndex, count);
            return this;
        }

        // LUCENENET specific - char sequence overload for StringBuilder
        public virtual OpenStringBuilder Append(StringBuilder csq)
        {
            return Append(csq, 0, csq.Length);
        }

        // LUCENENET specific - char sequence overload for StringBuilder
        public virtual OpenStringBuilder Append(StringBuilder csq, int startIndex, int count) // LUCENENET specific: changed to startIndex/length to match .NET
        {
            EnsureCapacity(count);
            UnsafeWrite(csq, startIndex, count);
            return this;
        }

        // LUCENENET specific - char sequence overload for char[]
        public virtual OpenStringBuilder Append(char[] value)
        {
            Write(value);
            return this;
        }

        // LUCENENET specific - char sequence overload for char[]
        public virtual OpenStringBuilder Append(char[] value, int startIndex, int count)
        {
            EnsureCapacity(count);
            UnsafeWrite(value, startIndex, count);
            return this;
        }

        public virtual OpenStringBuilder Append(char c)
        {
            Write(c);
            return this;
        }

        // LUCENENET specific - removed (replaced with this[])
        //public virtual char CharAt(int index)
        //{
        //    return m_buf[index];
        //}

        // LUCENENET specific - removed (replaced with this[])
        //public virtual void SetCharAt(int index, char ch)
        //{
        //    m_buf[index] = ch;
        //}

        // LUCENENET specific - added to .NETify
        public virtual char this[int index]
        {
            get => m_buf[index];
            set => m_buf[index] = value;
        }

        public virtual ICharSequence Subsequence(int startIndex, int length)
        {
            // From Apache Harmony String class
            if (m_buf is null || (startIndex == 0 && length == m_buf.Length))
            {
                return new CharArrayCharSequence(m_buf);
            }
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(length)} may not be negative.");
            if (startIndex > m_buf.Length - length) // LUCENENET: Checks for int overflow
                throw new ArgumentOutOfRangeException(nameof(length), $"Index and length must refer to a location within the string. For example {nameof(startIndex)} + {nameof(length)} <= {nameof(Length)}.");

            char[] result = new char[length];
            for (int i = 0, j = startIndex; i < length; i++, j++)
                result[i] = m_buf[j];

            return new CharArrayCharSequence(result);
        }

        public virtual void UnsafeWrite(char b)
        {
            m_buf[m_len++] = b;
        }

        public virtual void UnsafeWrite(int b)
        {
            UnsafeWrite((char)b);
        }

        public virtual void UnsafeWrite(char[] b, int off, int len)
        {
            Arrays.Copy(b, off, m_buf, this.m_len, len);
            this.m_len += len;
        }

        // LUCENENET specific overload for StringBuilder
        public virtual void UnsafeWrite(StringBuilder b, int off, int len)
        {
            b.CopyTo(off, m_buf, this.m_len, len);
            this.m_len += len;
        }

        // LUCENENET specific overload for string
        public virtual void UnsafeWrite(string b, int off, int len)
        {
            b.CopyTo(off, m_buf, this.m_len, len);
            this.m_len += len;
        }

        // LUCENENET specific overload for ICharSequence
        public virtual void UnsafeWrite(ICharSequence b, int off, int len)
        {
            if (!b.HasValue) return;

            if (b is StringBuilderCharSequence sb)
            {
                UnsafeWrite(sb.Value, off, len);
                return;
            }
            if (b is StringCharSequence str)
            {
                UnsafeWrite(str.Value, off, len);
                return;
            }
            if (b is CharArrayCharSequence chars)
            {
                UnsafeWrite(chars.Value, off, len);
                return;
            }

            for (int i = off; i < off + len; i++)
            {
                UnsafeWrite(b[i]);
            }
        }

        protected virtual void Resize(int len)
        {
            char[] newbuf = new char[Math.Max(m_buf.Length << 1, len)];
            Arrays.Copy(m_buf, 0, newbuf, 0, Length);
            m_buf = newbuf;
        }

        public virtual void EnsureCapacity(int capacity) // LUCENENET NOTE: renamed from reserve() in Lucene to match .NET StringBuilder
        {
            if (m_len + capacity > m_buf.Length)
            {
                Resize(m_len + capacity);
            }
        }

        public virtual void Write(char b)
        {
            if (m_len >= m_buf.Length)
            {
                Resize(m_len + 1);
            }
            UnsafeWrite(b);
        }

        public virtual void Write(int b)
        {
            Write((char)b);
        }

        public void Write(char[] b)
        {
            Write(b, 0, b.Length);
        }

        public virtual void Write(char[] b, int off, int len)
        {
            EnsureCapacity(len);
            UnsafeWrite(b, off, len);
        }

        public void Write(OpenStringBuilder arr)
        {
            Write(arr.m_buf, 0, arr.Length); // LUCENENET specific - changed to arr.m_len (original was just len - appears to be a bug)
        }

        // LUCENENET specific overload for StringBuilder
        public void Write(StringBuilder arr)
        {
            EnsureCapacity(arr.Length);
            UnsafeWrite(arr, 0, arr.Length);
        }

        public virtual void Write(string s)
        {
            EnsureCapacity(s.Length);
            s.CopyTo(0, m_buf, m_len, s.Length - 0);
            m_len += s.Length;
        }

        //public virtual void Flush() // LUCENENET specific - removed because this doesn't make much sense on a StringBuilder in .NET, and it is not used
        //{
        //}

        public void Reset()
        {
            m_len = 0;
        }

        public virtual char[] ToCharArray()
        {
            char[] newbuf = new char[Length];
            Arrays.Copy(m_buf, 0, newbuf, 0, Length);
            return newbuf;
        }

        public override string ToString()
        {
            return new string(m_buf, 0, Length);
        }

        #region IAppendable Members

        IAppendable IAppendable.Append(char value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(string value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(string value, int startIndex, int count)
        {
            return Append(value, startIndex, count);
        }

        IAppendable IAppendable.Append(StringBuilder value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(StringBuilder value, int startIndex, int count)
        {
            return Append(value, startIndex, count);
        }

        IAppendable IAppendable.Append(char[] value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(char[] value, int startIndex, int count)
        {
            return Append(value, startIndex, count);
        }

        IAppendable IAppendable.Append(ICharSequence value)
        {
            return Append(value);
        }

        IAppendable IAppendable.Append(ICharSequence value, int startIndex, int count)
        {
            return Append(value, startIndex, count);
        }

        #endregion IAppendable Members
    }
}