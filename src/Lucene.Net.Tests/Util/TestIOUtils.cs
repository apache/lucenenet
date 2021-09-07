using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
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

    [TestFixture]
    public class TestIOUtils : LuceneTestCase
    {
        internal sealed class BrokenIDisposable : IDisposable
        {
            internal readonly int i;

            public BrokenIDisposable(int i)
            {
                this.i = i;
            }

            public void Dispose()
            {
                throw new IOException("TEST-IO-EXCEPTION-" + i);
            }
        }

        internal sealed class TestException : Exception
        {
            public TestException()
                : base("BASE-EXCEPTION")
            {
            }
        }

        [Test]
        public virtual void TestSuppressedExceptions()
        {
            // test with prior exception
            try
            {
                TestException t = new TestException();
                IOUtils.DisposeWhileHandlingException(t, new BrokenIDisposable(1), new BrokenIDisposable(2));
            }
            catch (TestException e1)
            {
                assertEquals("BASE-EXCEPTION", e1.Message);
                assertEquals(2, e1.GetSuppressed().Length);
                assertEquals("TEST-IO-EXCEPTION-1", e1.GetSuppressed()[0].Message);
                assertEquals("TEST-IO-EXCEPTION-2", e1.GetSuppressed()[1].Message);
            }
            catch (Exception e2) when (e2.IsIOException())
            {
                Assert.Fail("IOException should not be thrown here");
            }

            // test without prior exception
            try
            {
                IOUtils.DisposeWhileHandlingException((TestException)null, new BrokenIDisposable(1), new BrokenIDisposable(2));
            }
#pragma warning disable 168
            catch (TestException e1)
#pragma warning restore 168
            {
                fail("TestException should not be thrown here");
            }
            catch (Exception e2) when (e2.IsIOException())
            {
                assertEquals("TEST-IO-EXCEPTION-1", e2.Message);
                assertEquals(1, e2.GetSuppressed().Length);
                assertEquals("TEST-IO-EXCEPTION-2", e2.GetSuppressed()[0].Message);
            }
#pragma warning disable 168
            catch (Exception e2)
#pragma warning restore 168
            {
                fail("Exception should not be thrown here");
            }
        }
    }
}