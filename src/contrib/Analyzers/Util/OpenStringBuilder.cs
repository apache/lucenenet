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

using System;
using Lucene.Net.Analysis.Support;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.Util
{
    public class OpenStringBuilder : IAppendable, ICharSequence
    {
        protected char[] buf;
        protected int len;

        public OpenStringBuilder() : this(32) { }

        public OpenStringBuilder(int size)
        {
            buf = new char[size];
        }

        public OpenStringBuilder(char[] arr, int len)
        {
            Set(arr, len);
        }

        public int Length
        {
            get
            {
                return this.len;
            }
            set
            {
                this.len = value;
            }
        }

        public void Set(char[] arr, int end)
        {
            this.buf = arr;
            this.len = end;
        }

        public char[] GetArray()
        {
            return buf;
        }

        public int Size { get { return len; }}

        public int Capacity  { get { return buf.Length; }}

        public IAppendable Append(ICharSequence csq)
        {
            return Append(csq, 0, csq.Length);
        }

        public IAppendable Append(ICharSequence csq, int start, int end)
        {
            Reserve(end - start);
            for (var i = start; i < end; i++)
            {
                UnsafeWrite(csq.CharAt(i));
            }
            return this;
        }

        public IAppendable Append(char c)
        {
            Write(c);
            return this;
        }

        public char CharAt(int index)
        {
            return this[index];
        }

        public char this[int index]
        {
            get
            {
                return buf[index];
            }
            set
            {
                buf[index] = value;
            }
        }

        public ICharSequence SubSequence(int start, int end)
        {
            throw new NotSupportedException(); // todo
        }

        public void UnsafeWrite(char b)
        {
            buf[len++] = b;
        }

        public void UnsafeWrite(int b)
        {
            UnsafeWrite((char) b);
        }

        public void UnsafeWrite(char[] b, int off, int len)
        {
            Array.Copy(b, off, buf, this.len, len);
            this.len += len;
        }

        public void Resize(int len)
        {
            var newBuf = new char[Math.Max(buf.Length << 1, len)];
            Array.Copy(buf, 0, newBuf, 0, Size);
            buf = newBuf;
        }

        public void Reserve(int num)
        {
            if (len + num > buf.Length) Resize(len + num);
        }

        public void Write(char b)
        {
            if (len >= buf.Length)
            {
                Resize(len + 1);
            }
            UnsafeWrite(b);
        }

        public virtual void Write(int b)
        {
            Write((char) b);
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

        public virtual void Write(OpenStringBuilder arr)
        {
            Write(arr.buf, 0, len);
        }

        public virtual void Write(string s)
        {
            Reserve(s.Length);
            s.GetChars(0, s.Length, buf, len);
            len += s.Length;
        }

        public virtual void Flush()
        {
            // no-op
        }

        public void Reset()
        {
            len = 0;
        }

        public char[] ToCharArray()
        {
            var newBuf = new char[Size];
            Array.Copy(buf, 0, newBuf, 0, Size);
            return newBuf;
        }

        public override string ToString()
        {
            return new string(buf, 0, Size);
        }
    }
}
