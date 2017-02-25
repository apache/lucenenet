using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Support
{
    public class TestSafeTextWriterWrapper : LuceneTestCase
    {
        [Test]
        public void TestWrite()
        {
            SafeTextWriterWrapper safe;
            using (TextWriter wrapped = new StringWriter())
            {
                safe = new SafeTextWriterWrapper(wrapped);

                safe.Write('a');
                assertEquals("a", wrapped.ToString());
            }

            Assert.DoesNotThrow(() => safe.Write('a'));
            Assert.DoesNotThrow(() => safe.Write('a'));
            Assert.DoesNotThrow(() => safe.Write("a"));
        }

        [Test]
        public void TestWriteLine()
        {
            SafeTextWriterWrapper safe;
            using (TextWriter wrapped = new StringWriter())
            {
                safe = new SafeTextWriterWrapper(wrapped);

                safe.WriteLine('a');
                assertEquals("a" + Environment.NewLine, wrapped.ToString());

                safe.WriteLine("This is a test");
                assertEquals("a" + Environment.NewLine + "This is a test" + Environment.NewLine, wrapped.ToString());
            }

            Assert.DoesNotThrow(() => safe.WriteLine('a'));
            Assert.DoesNotThrow(() => safe.WriteLine("Testing"));
            Assert.DoesNotThrow(() => safe.WriteLine("Testing"));
        }
    }
}
