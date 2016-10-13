using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
    using Directory = Lucene.Net.Store.Directory;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestIndexReaderClose : LuceneTestCase
    {
        [Test]
        public virtual void TestCloseUnderException()
        {
            int iters = 1000 + 1 + Random().nextInt(20);
            for (int j = 0; j < iters; j++)
            {
                Directory dir = NewDirectory();
                IndexWriter writer = new IndexWriter(dir,
                    NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                writer.Commit();
                writer.Dispose();
                DirectoryReader open = DirectoryReader.Open(dir);
                bool throwOnClose = !Rarely();
                AtomicReader wrap = SlowCompositeReaderWrapper.Wrap(open);
                FilterAtomicReader reader = new FilterAtomicReaderAnonymousInnerClassHelper(this, wrap, throwOnClose);
                IList<IndexReader.ReaderClosedListener> listeners = new List<IndexReader.ReaderClosedListener>();
                int listenerCount = Random().Next(20);
                AtomicInteger count = new AtomicInteger();
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
                catch (AlreadyClosedException ex)
                {
                }

                if (Random().NextBoolean())
                {
                    reader.Dispose(); // call it again
                }
                Assert.AreEqual(0, count.Get());
                wrap.Dispose();
                dir.Dispose();
            }
        }

        private class FilterAtomicReaderAnonymousInnerClassHelper : FilterAtomicReader
        {
            private readonly TestIndexReaderClose OuterInstance;

            private bool ThrowOnClose;

            public FilterAtomicReaderAnonymousInnerClassHelper(TestIndexReaderClose outerInstance, AtomicReader wrap, bool throwOnClose)
                : base(wrap)
            {
                this.OuterInstance = outerInstance;
                this.ThrowOnClose = throwOnClose;
            }

            protected internal override void DoClose()
            {
                base.DoClose();
                if (ThrowOnClose)
                {
                    throw new InvalidOperationException("BOOM!");
                }
            }
        }

        private sealed class CountListener : IndexReader.ReaderClosedListener
        {
            internal readonly AtomicInteger Count;

            public CountListener(AtomicInteger count)
            {
                this.Count = count;
            }

            public void OnClose(IndexReader reader)
            {
                Count.DecrementAndGet();
            }
        }

        private sealed class FaultyListener : IndexReader.ReaderClosedListener
        {
            public void OnClose(IndexReader reader)
            {
                throw new InvalidOperationException("GRRRRRRRRRRRR!");
            }
        }
    }
}