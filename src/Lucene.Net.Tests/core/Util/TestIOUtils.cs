using NUnit.Framework;
using System;
using System.IO;
using System.Text;

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
                IOUtils.CloseWhileHandlingException(t, new BrokenIDisposable(1), new BrokenIDisposable(2));
            }
            catch (TestException e1)
            {
                Assert.AreEqual("BASE-EXCEPTION", e1.Message);
                StringBuilder sb = new StringBuilder();
                //StreamWriter pw = new StreamWriter(sw);
                //e1.printStackTrace(pw);
                sb.Append(e1.StackTrace);
                //pw.Flush();
                string trace = sb.ToString();
                if (VERBOSE)
                {
                    Console.WriteLine("TestIOUtils.testSuppressedExceptions: Thrown Exception stack trace:");
                    Console.WriteLine(trace);
                }
                Assert.IsTrue(trace.Contains("IOException: TEST-IO-EXCEPTION-1"), "Stack trace does not contain first suppressed Exception: " + trace);
                Assert.IsTrue(trace.Contains("IOException: TEST-IO-EXCEPTION-2"), "Stack trace does not contain second suppressed Exception: " + trace);
            }
            catch (IOException e2)
            {
                Assert.Fail("IOException should not be thrown here");
            }

            // test without prior exception
            try
            {
                IOUtils.CloseWhileHandlingException((TestException)null, new BrokenIDisposable(1), new BrokenIDisposable(2));
            }
            catch (TestException e1)
            {
                Assert.Fail("TestException should not be thrown here");
            }
            catch (IOException e2)
            {
                Assert.AreEqual("TEST-IO-EXCEPTION-1", e2.Message);
                StringBuilder sb = new StringBuilder();
                sb.Append(e2.StackTrace);
                //StreamWriter pw = new StreamWriter(sw);
                //e2.printStackTrace(pw);
                //pw.Flush();
                string trace = sb.ToString();
                if (VERBOSE)
                {
                    Console.WriteLine("TestIOUtils.TestSuppressedExceptions: Thrown Exception stack trace:");
                    Console.WriteLine(trace);
                }
                Assert.IsTrue(trace.Contains("IOException: TEST-IO-EXCEPTION-2"), "Stack trace does not contain suppressed Exception: " + trace);
            }
        }
    }
}