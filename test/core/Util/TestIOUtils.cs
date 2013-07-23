using System;
using System.IO;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestIOUtils : LuceneTestCase
    {
        internal sealed class BrokenCloseable : IDisposable
        {
            internal readonly int i;

            public BrokenCloseable(int i)
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
                : base("BASE-EXCEPTION") { }
        }

        [Test]
        public virtual void TestSuppressedExceptions()
        {
            if (!Constants.JRE_IS_MINIMUM_JAVA7)
            {
                Console.Error.WriteLine("WARNING: TestIOUtils.TestSuppressedExceptions: Full test coverage only with Java 7, as suppressed exception recording is not supported before.");
            }

            // test with prior exception
            try
            {
                var t = new TestException();
                IOUtils.CloseWhileHandlingException(t, new BrokenCloseable(1), new BrokenCloseable(2));
            }
            catch (TestException e1)
            {
                assertEquals("BASE-EXCEPTION", e1.Message);
                var sw = new StringWriter();
                //PrintWriter pw = new PrintWriter(sw);
                sw.Write(e1.StackTrace);
                //e1.PrintStackTrace(pw);
                //pw.Flush();
                var trace = sw.ToString();
                if (VERBOSE)
                {
                    Console.WriteLine("TestIOUtils.testSuppressedExceptions: Thrown Exception stack trace:");
                    Console.WriteLine(trace);
                }
                if (Constants.JRE_IS_MINIMUM_JAVA7)
                {
                    assertTrue("Stack trace does not contain first suppressed Exception: " + trace,
                      trace.Contains("java.io.IOException: TEST-IO-EXCEPTION-1"));
                    assertTrue("Stack trace does not contain second suppressed Exception: " + trace,
                      trace.Contains("java.io.IOException: TEST-IO-EXCEPTION-2"));
                }
            }
            catch (IOException e2)
            {
                Fail("IOException should not be thrown here");
            }

            // test without prior exception
            try
            {
                IOUtils.CloseWhileHandlingException((TestException)null, new BrokenCloseable(1), new BrokenCloseable(2));
            }
            catch (TestException e1)
            {
                Fail("TestException should not be thrown here");
            }
            catch (IOException e2)
            {
                assertEquals("TEST-IO-EXCEPTION-1", e2.Message);
                var sw = new StringWriter();
                //PrintWriter pw = new PrintWriter(sw);
                sw.Write(e2.StackTrace);
                //e2.printStackTrace(pw);
                //pw.Flush();
                var trace = sw.ToString();
                if (VERBOSE)
                {
                    Console.WriteLine("TestIOUtils.testSuppressedExceptions: Thrown Exception stack trace:");
                    Console.WriteLine(trace);
                }
                if (Constants.JRE_IS_MINIMUM_JAVA7)
                {
                    assertTrue("Stack trace does not contain suppressed Exception: " + trace,
                      trace.Contains("java.io.IOException: TEST-IO-EXCEPTION-2"));
                }
            }
        }
    }
}
