using Lucene.Net.Support;
using System;

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
    public class OpenStringBuilder : ICharSequence
    {
        protected internal char[] buf;
        protected internal int len;

        public OpenStringBuilder() : this(32)
        {
        }

        public OpenStringBuilder(int size)
        {
            buf = new char[size];
        }

        public OpenStringBuilder(char[] arr, int len)
        {
            Set(arr, len);
        }

        public virtual int Length
        {
            set
            {
                this.len = value;
            }
            get { return len; }
        }

        public virtual void Set(char[] arr, int end)
        {
            this.buf = arr;
            this.len = end;
        }

        public virtual char[] Array
        {
            get
            {
                return buf;
            }
        }
        public virtual int Size()
        {
            return len;
        }

        public virtual int Capacity()
        {
            return buf.Length;
        }

        public virtual OpenStringBuilder Append(string csq)
        {
            return Append(csq, 0, csq.Length);
        }

        public virtual OpenStringBuilder Append(string csq, int start, int end)
        {
            Reserve(end - start);
            for (int i = start; i < end; i++)
            {
                UnsafeWrite(csq[i]);
            }
            return this;
        }

        public virtual OpenStringBuilder Append(char c)
        {
            Write(c);
            return this;
        }

        public virtual char CharAt(int index)
        {
            return buf[index];
        }

        public virtual void SetCharAt(int index, char ch)
        {
            buf[index] = ch;
        }

        // LUCENENET specific - added to .NETify
        public virtual char this[int index]
        {
            get { return buf[index]; }
            set { buf[index] = value; }
        }

        public virtual ICharSequence SubSequence(int start, int end)
        {
            throw new System.NotSupportedException(); // todo
        }

        public virtual void UnsafeWrite(char b)
        {
            buf[len++] = b;
        }

        public virtual void UnsafeWrite(int b)
        {
            UnsafeWrite((char)b);
        }

        public virtual void UnsafeWrite(char[] b, int off, int len)
        {
            System.Array.Copy(b, off, buf, this.len, len);
            this.len += len;
        }

        protected internal virtual void Resize(int len)
        {
            char[] newbuf = new char[Math.Max(buf.Length << 1, len)];
            System.Array.Copy(buf, 0, newbuf, 0, Size());
            buf = newbuf;
        }

        public virtual void Reserve(int num)
        {
            if (len + num > buf.Length)
            {
                Resize(len + num);
            }
        }

        public virtual void Write(char b)
        {
            if (len >= buf.Length)
            {
                Resize(len + 1);
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
            Reserve(len);
            UnsafeWrite(b, off, len);
        }

        public void Write(OpenStringBuilder arr)
        {
            Write(arr.buf, 0, len);
        }

        public virtual void Write(string s)
        {
            Reserve(s.Length);
            s.CopyTo(0, buf, len, s.Length - 0);
            len += s.Length;
        }

        public virtual void Flush()
        {
        }

        public void Reset()
        {
            len = 0;
        }

        public virtual char[] ToCharArray()
        {
            char[] newbuf = new char[Size()];
            System.Array.Copy(buf, 0, newbuf, 0, Size());
            return newbuf;
        }

        public override string ToString()
        {
            return new string(buf, 0, Size());
        }
    }
}