using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Lucene.Net.Analysis
{
    using System.IO;

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

    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Wraps a <see cref="TextReader"/>, and can throw random or fixed
    /// exceptions, and spoon feed read chars.
    /// </summary>
    public class MockReaderWrapper : StringReader
    {
        private readonly Random random;

        private int excAtChar = -1;
        private int readSoFar;
        private bool throwExcNext;

        public MockReaderWrapper(Random random, string text)
            : base(text)
        {
            this.random = random;
        }

        /// <summary>
        /// Throw an exception after reading this many chars. </summary>
        public virtual void ThrowExcAfterChar(int charUpto)
        {
            excAtChar = charUpto;
            // You should only call this on init!:
            Assert.AreEqual(0, readSoFar); // LUCENENET TODO: This should be Debug.Assert
        }

        public virtual void ThrowExcNext()
        {
            throwExcNext = true;
        }

        public override int Read()
        {
            ThrowExceptionIfApplicable();

            var c = base.Read();
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
                realLen = TestUtil.NextInt(random, 1, len);
            }
            if (excAtChar != -1)
            {
                int left = excAtChar - readSoFar;
                Assert.True(left != 0);
                read = base.Read(cbuf, off, Math.Min(realLen, left));
                //Characters are left
                Assert.True(read != 0);
                readSoFar += read;
            }
            else
            {
                read = base.Read(cbuf, off, realLen);
                //Terrible TextReader::Read semantics
                if (read == 0)
                {
                    read = -1;
                }
            }
            return read;
        }

        private void ThrowExceptionIfApplicable()
        {
            if (throwExcNext || (excAtChar != -1 && readSoFar >= excAtChar))
            {
                throw new Exception("fake exception now!");
            }
        }

        public bool MarkSupported() // LUCENENET TODO: API - property?
        {
            return false;
        }

        public bool Ready() // LUCENENET TODO: API - property?
        {
            return false;
        }

        public static bool IsMyEvilException(Exception t)
        {
            return (t != null) && "fake exception now!".Equals(t.Message, StringComparison.Ordinal);
        }
    }
}