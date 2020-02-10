using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using J2N.Collections.Generic.Extensions;

namespace Lucene.Net.Index
{
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
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
    [SuppressCodecs("Direct", "Memory")]
    [TestFixture]
    public class TestBagOfPostings : LuceneTestCase // at night this makes like 200k/300k docs and will make Direct's heart beat!
    {
        [Test]
        public virtual void Test()
        {
            IList<string> postingsList = new List<string>();
            int numTerms = AtLeast(300);
            int maxTermsPerDoc = TestUtil.NextInt32(Random, 10, 20);

            bool isSimpleText = "SimpleText".Equals(TestUtil.GetPostingsFormat("field"), StringComparison.Ordinal);

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, new MockAnalyzer(Random));

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
            postingsList.Shuffle(Random);

            ConcurrentQueue<string> postings = new ConcurrentQueue<string>(postingsList);

            Directory dir = NewFSDirectory(CreateTempDir("bagofpostings"));
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int threadCount = TestUtil.NextInt32(Random, 1, 5);
            if (VERBOSE)
            {
                Console.WriteLine("config: " + iw.IndexWriter.Config);
                Console.WriteLine("threadCount=" + threadCount);
            }

            ThreadJob[] threads = new ThreadJob[threadCount];
            CountdownEvent startingGun = new CountdownEvent(1);

            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                threads[threadID] = new ThreadAnonymousInnerClassHelper(this, maxTermsPerDoc, postings, iw, startingGun);
                threads[threadID].Start();
            }
            startingGun.Signal();
            foreach (ThreadJob t in threads)
            {
                t.Join();
            }

            iw.ForceMerge(1);
            DirectoryReader ir = iw.GetReader();
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader air = (AtomicReader)ir.Leaves[0].Reader;
            Terms terms = air.GetTerms("field");
            // numTerms-1 because there cannot be a term 0 with 0 postings:
#pragma warning disable 612, 618
            Assert.AreEqual(numTerms - 1, air.Fields.UniqueTermCount);
            if (iwc.Codec is Lucene3xCodec == false)
#pragma warning restore 612, 618
            {
                Assert.AreEqual(numTerms - 1, terms.Count);
            }
            TermsEnum termsEnum = terms.GetIterator(null);
            BytesRef term_;
            while ((term_ = termsEnum.Next()) != null)
            {
                int value = Convert.ToInt32(term_.Utf8ToString());
                Assert.AreEqual(value, termsEnum.DocFreq);
                // don't really need to check more than this, as CheckIndex
                // will verify that docFreq == actual number of documents seen
                // from a docsAndPositionsEnum.
            }
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadJob
        {
            private readonly TestBagOfPostings OuterInstance;

            private int MaxTermsPerDoc;
            private ConcurrentQueue<string> Postings;
            private RandomIndexWriter Iw;
            private CountdownEvent StartingGun;

            public ThreadAnonymousInnerClassHelper(TestBagOfPostings outerInstance, int maxTermsPerDoc, ConcurrentQueue<string> postings, RandomIndexWriter iw, CountdownEvent startingGun)
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
                    StartingGun.Wait();
                    while (!(Postings.Count == 0))
                    {
                        StringBuilder text = new StringBuilder();
                        ISet<string> visited = new JCG.HashSet<string>();
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
                        field.SetStringValue(text.ToString());
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