using Apache.NMS.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Lucene.Net.Document.Document;
    using Field = Lucene.Net.Document.Field;
    using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
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
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Simple test that adds numeric terms, where each term has the
    /// docFreq of its integer value, and checks that the docFreq is correct.
    /// </summary>
    [TestFixture]
    public class TestBagOfPostings : LuceneTestCase // at night this makes like 200k/300k docs and will make Direct's heart beat!
    {
        [Test]
        public virtual void Test()
        {
            IList<string> postingsList = new List<string>();
            int numTerms = AtLeast(300);
            int maxTermsPerDoc = TestUtil.NextInt(Random(), 10, 20);

            bool isSimpleText = "SimpleText".Equals(TestUtil.GetPostingsFormat("field"));

            IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, new MockAnalyzer(Random()));

            if ((isSimpleText || iwc.MergePolicy is MockRandomMergePolicy) && (TEST_NIGHTLY || RANDOM_MULTIPLIER > 1))
            {
                // Otherwise test can take way too long (> 2 hours)
                numTerms /= 2;
            }

            if (VERBOSE)
            {
                Console.WriteLine("maxTermsPerDoc=" + maxTermsPerDoc);
                Console.WriteLine("numTerms=" + numTerms);
            }

            for (int i = 0; i < numTerms; i++)
            {
                string term = Convert.ToString(i);
                for (int j = 0; j < i; j++)
                {
                    postingsList.Add(term);
                }
            }
            postingsList = CollectionsHelper.Shuffle(postingsList);

            ConcurrentQueue<string> postings = new ConcurrentQueue<string>(postingsList);

            Directory dir = NewFSDirectory(CreateTempDir("bagofpostings"));
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);

            int threadCount = TestUtil.NextInt(Random(), 1, 5);
            if (VERBOSE)
            {
                Console.WriteLine("config: " + iw.w.Config);
                Console.WriteLine("threadCount=" + threadCount);
            }

            ThreadClass[] threads = new ThreadClass[threadCount];
            CountDownLatch startingGun = new CountDownLatch(1);

            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                threads[threadID] = new ThreadAnonymousInnerClassHelper(this, maxTermsPerDoc, postings, iw, startingGun);
                threads[threadID].Start();
            }
            startingGun.countDown();
            foreach (ThreadClass t in threads)
            {
                t.Join();
            }

            iw.ForceMerge(1);
            DirectoryReader ir = iw.Reader;
            Assert.AreEqual(1, ir.Leaves().Count);
            AtomicReader air = (AtomicReader)ir.Leaves()[0].Reader();
            Terms terms = air.Terms("field");
            // numTerms-1 because there cannot be a term 0 with 0 postings:
            Assert.AreEqual(numTerms - 1, air.Fields().UniqueTermCount);
            if (iwc.Codec is Lucene3xCodec == false)
            {
                Assert.AreEqual(numTerms - 1, terms.Size());
            }
            TermsEnum termsEnum = terms.Iterator(null);
            BytesRef term_;
            while ((term_ = termsEnum.Next()) != null)
            {
                int value = Convert.ToInt32(term_.Utf8ToString());
                Assert.AreEqual(value, termsEnum.DocFreq());
                // don't really need to check more than this, as CheckIndex
                // will verify that docFreq == actual number of documents seen
                // from a docsAndPositionsEnum.
            }
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestBagOfPostings OuterInstance;

            private int MaxTermsPerDoc;
            private ConcurrentQueue<string> Postings;
            private RandomIndexWriter Iw;
            private CountDownLatch StartingGun;

            public ThreadAnonymousInnerClassHelper(TestBagOfPostings outerInstance, int maxTermsPerDoc, ConcurrentQueue<string> postings, RandomIndexWriter iw, CountDownLatch startingGun)
            {
                this.OuterInstance = outerInstance;
                this.MaxTermsPerDoc = maxTermsPerDoc;
                this.Postings = postings;
                this.Iw = iw;
                this.StartingGun = startingGun;
            }

            public override void Run()
            {
                try
                {
                    Document document = new Document();
                    Field field = NewTextField("field", "", Field.Store.NO);
                    document.Add(field);
                    StartingGun.@await();
                    while (!(Postings.Count == 0))
                    {
                        StringBuilder text = new StringBuilder();
                        HashSet<string> visited = new HashSet<string>();
                        for (int i = 0; i < MaxTermsPerDoc; i++)
                        {
                            string token;
                            if (!Postings.TryDequeue(out token))
                            {
                                break;
                            }
                            if (visited.Contains(token))
                            {
                                // Put it back:
                                Postings.Enqueue(token);
                                break;
                            }
                            text.Append(' ');
                            text.Append(token);
                            visited.Add(token);
                        }
                        field.StringValue = text.ToString();
                        Iw.AddDocument(document);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }
    }
}