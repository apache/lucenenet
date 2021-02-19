using J2N.Threading.Atomic;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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

    using Directory = Lucene.Net.Store.Directory;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestIndexReaderClose : LuceneTestCase
    {
        [Test]
        public virtual void TestCloseUnderException()
        {
            int iters = 1000 + 1 + Random.nextInt(20);
            for (int j = 0; j < iters; j++)
            {
                Directory dir = NewDirectory();
                IndexWriter writer = new IndexWriter(dir,
                    NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                writer.Commit();
                writer.Dispose();
                DirectoryReader open = DirectoryReader.Open(dir);
                bool throwOnClose = !Rarely();
                AtomicReader wrap = SlowCompositeReaderWrapper.Wrap(open);
                FilterAtomicReader reader = new FilterAtomicReaderAnonymousClass(this, wrap, throwOnClose);
                IList<IndexReader.IReaderClosedListener> listeners = new List<IndexReader.IReaderClosedListener>();
                int listenerCount = Random.Next(20);
                AtomicInt32 count = new AtomicInt32();
                bool faultySet = false;
                for (int i = 0; i < listenerCount; i++)
                {
                    if (Rarely())
                    {
                        faultySet = true;
                        reader.AddReaderClosedListener(new FaultyListener());
                    }
                    else
                    {
                        count.IncrementAndGet();
                        reader.AddReaderClosedListener(new CountListener(count));
                    }
                }
                if (!faultySet && !throwOnClose)
                {
                    reader.AddReaderClosedListener(new FaultyListener());
                }
                try
                {
                    reader.Dispose();
                    Assert.Fail("expected Exception");
                }
                catch (InvalidOperationException ex)
                {
                    if (throwOnClose)
                    {
                        Assert.AreEqual("BOOM!", ex.Message);
                    }
                    else
                    {
                        Assert.AreEqual("GRRRRRRRRRRRR!", ex.Message);
                    }
                }

                try
                {
                    var aaa = reader.Fields;
                    Assert.Fail("we are closed");
                }
#pragma warning disable 168
                catch (ObjectDisposedException ex)
#pragma warning restore 168
                {
                }

                if (Random.NextBoolean())
                {
                    reader.Dispose(); // call it again
                }
                Assert.AreEqual(0, count);
                wrap.Dispose();
                dir.Dispose();
            }
        }

        private class FilterAtomicReaderAnonymousClass : FilterAtomicReader
        {
            private readonly TestIndexReaderClose outerInstance;

            private bool throwOnClose;

            public FilterAtomicReaderAnonymousClass(TestIndexReaderClose outerInstance, AtomicReader wrap, bool throwOnClose)
                : base(wrap)
            {
                this.outerInstance = outerInstance;
                this.throwOnClose = throwOnClose;
            }

            protected internal override void DoClose()
            {
                base.DoClose();
                if (throwOnClose)
                {
                    throw new InvalidOperationException("BOOM!");
                }
            }
        }

        private sealed class CountListener : IndexReader.IReaderClosedListener
        {
            internal readonly AtomicInt32 count;

            public CountListener(AtomicInt32 count)
            {
                this.count = count;
            }

            public void OnClose(IndexReader reader)
            {
                count.DecrementAndGet();
            }
        }

        private sealed class FaultyListener : IndexReader.IReaderClosedListener
        {
            public void OnClose(IndexReader reader)
            {
                throw new InvalidOperationException("GRRRRRRRRRRRR!");
            }
        }
    }
}