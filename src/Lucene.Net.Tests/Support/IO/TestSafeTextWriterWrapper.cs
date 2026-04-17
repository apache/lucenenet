using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lucene.Net.Support.IO
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

        /// <summary>
        /// LUCENENET specific. When <see cref="SafeTextWriterWrapper"/> wraps a
        /// non-thread-safe <see cref="TextWriter"/> (such as <see cref="StringWriter"/>),
        /// concurrent writes from multiple threads must not throw. This reproduces
        /// https://github.com/apache/lucenenet/issues/1246 where the <see cref="FieldCache"/>
        /// info stream was written concurrently from <see cref="Search.IndexSearcher"/>'s
        /// executor threads, causing <see cref="ArgumentException"/> ("Destination is too
        /// short") from <see cref="System.Text.StringBuilder"/>.
        /// </summary>
        [Test]
        public void TestConcurrentWrites()
        {
            using var wrapped = new StringWriter();
            using var safe = new SafeTextWriterWrapper(wrapped);

            const int threadCount = 8;
            const int iterationsPerThread = 2000;
            const string line = "WARNING: new FieldCache insanity created. Details: some long-ish message for buffer growth.";

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    safe.WriteLine(line);
                }
            });
        }
    }
}
