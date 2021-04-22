using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis
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
    /// Wraps a <see cref="TextReader"/>, and can throw random or fixed
    /// exceptions, and spoon feed read chars.
    /// </summary>
    public class MockReaderWrapper : TextReader
    {
        private readonly TextReader input;
        private readonly Random random;

        private int excAtChar = -1;
        private int readSoFar;
        private bool throwExcNext;

        public MockReaderWrapper(Random random, TextReader input)
        {
            this.input = input;
            this.random = random;
        }

        /// <summary>
        /// Throw an exception after reading this many chars. </summary>
        public virtual void ThrowExcAfterChar(int charUpto)
        {
            excAtChar = charUpto;
            // You should only call this on init!:
            if (Debugging.AssertsEnabled) Debugging.Assert(0 == readSoFar);
        }

        public virtual void ThrowExcNext()
        {
            throwExcNext = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                input?.Dispose();
            }
        }

        public override int Read()
        {
            ThrowExceptionIfApplicable();

            var c = input.Read();
            readSoFar += 1;
            return c;
        }

        public override int Read(char[] cbuf, int off, int len)
        {
            ThrowExceptionIfApplicable();
            int read;
            int realLen;
            if (len == 1)
            {
                realLen = 1;
            }
            else
            {
                // Spoon-feed: intentionally maybe return less than
                // the consumer asked for
                realLen = TestUtil.NextInt32(random, 1, len);
            }
            if (excAtChar != -1)
            {
                int left = excAtChar - readSoFar;
                if (Debugging.AssertsEnabled) Debugging.Assert(left != 0);
                read = input.Read(cbuf, off, Math.Min(realLen, left));
                //Characters are left
                if (Debugging.AssertsEnabled) Debugging.Assert(read != 0);
                readSoFar += read;
            }
            else
            {
                // LUCENENET NOTE: In Java this returns -1 when done reading,
                // but in .NET it returns 0. We are sticking with the .NET behavior
                // for compatibility reasons, but all Java-ported tests need to be fixed
                // to compensate for this (i.e. instead of checking x == -1, we should 
                // check x <= 0 which covers both cases)
                read = input.Read(cbuf, off, realLen);
            }
            return read;
        }

        private void ThrowExceptionIfApplicable()
        {
            if (throwExcNext || (excAtChar != -1 && readSoFar >= excAtChar))
            {
                throw RuntimeException.Create("fake exception now!");
            }
        }

        // LUCENENET: These are not supported by TextReader, so doesn't make much sense to include them.
        // These were basically just to override the Java Reader class. In .NET, there is no Mark() method 
        // to support, nor is there an IsReady. TextReader works happily without these.
        //public virtual bool IsMarkSupported // LUCENENET specific - renamed from markSupported()
        //{
        //    get { return false; }
        //}

        //public virtual bool IsReady // LUCENENET specific - renamed from ready()
        //{
        //    get { return false; }
        //}

        public static bool IsMyEvilException(Exception t)
        {
            return (t != null) && "fake exception now!".Equals(t.Message, StringComparison.Ordinal);
        }
    }
}