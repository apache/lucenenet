using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.Threading;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
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
    /// totalTermFreq of its integer value, and checks that the totalTermFreq is correct.
    /// </summary>
    // TODO: somehow factor this with BagOfPostings? its almost the same
    [SuppressCodecs("Direct", "Memory", "Lucene3x")] // at night this makes like 200k/300k docs and will make Direct's heart beat!
                                                     // Lucene3x doesnt have totalTermFreq, so the test isn't interesting there.
    [TestFixture]
    public class TestBagOfPositions : LuceneTestCase
    
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

            Directory dir = NewFSDirectory(CreateTempDir(GetFullMethodName()));

            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);

            int threadCount = TestUtil.NextInt(Random(), 1, 5);
            if (VERBOSE)
            {
                Console.WriteLine("config: " + iw.w.Config);
                Console.WriteLine("threadCount=" + threadCount);
            }

            Field prototype = NewTextField("field", "", Field.Store.NO);
            FieldType fieldType = new FieldType((FieldType)prototype.FieldType);
            if (Random().NextBoolean())
            {
                fieldType.OmitNorms = true;
            }
            int options = Random().Next(3);
            if (options == 0)
            {
                fieldType.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS; // we dont actually need positions
                fieldType.StoreTermVectors = true; // but enforce term vectors when we do this so we check SOMETHING
            }
            else if (options == 1 && !DoesntSupportOffsets.Contains(TestUtil.GetPostingsFormat("field")))
            {
                fieldType.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            }
            // else just positions

            ThreadClass[] threads = new ThreadClass[threadCount];
            CountdownEvent startingGun = new CountdownEvent(1);

            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                Random threadRandom = new Random(Random().Next());
                Document document = new Document();
                Field field = new Field("field", "", fieldType);
                document.Add(field);
                threads[threadID] = new ThreadAnonymousInnerClassHelper(this, numTerms, maxTermsPerDoc, postings, iw, startingGun, threadRandom, document, field);
                threads[threadID].Start();
            }
            startingGun.Signal();
            foreach (ThreadClass t in threads)
            {
                t.Join();
            }

            iw.ForceMerge(1);
            DirectoryReader ir = iw.Reader;
            Assert.AreEqual(1, ir.Leaves.Count);
            AtomicReader air = (AtomicReader)ir.Leaves[0].Reader;
            Terms terms = air.Terms("field");
            // numTerms-1 because there cannot be a term 0 with 0 postings:
            Assert.AreEqual(numTerms - 1, terms.Size());
            TermsEnum termsEnum = terms.Iterator(null);
            BytesRef termBR;
            while ((termBR = termsEnum.Next()) != null)
            {
                int value = Convert.ToInt32(termBR.Utf8ToString());
                Assert.AreEqual(value, termsEnum.TotalTermFreq());
                // don't really need to check more than this, as CheckIndex
                // will verify that totalTermFreq == total number of positions seen
                // from a docsAndPositionsEnum.
            }
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestBagOfPositions OuterInstance;

            private int NumTerms;
            private int MaxTermsPerDoc;
            private ConcurrentQueue<string> Postings;
            private RandomIndexWriter Iw;
            private CountdownEvent StartingGun;
            private Random ThreadRandom;
            private Document Document;
            private Field Field;

            public ThreadAnonymousInnerClassHelper(TestBagOfPositions outerInstance, int numTerms, int maxTermsPerDoc, ConcurrentQueue<string> postings, RandomIndexWriter iw, CountdownEvent startingGun, Random threadRandom, Document document, Field field)
            {
                this.OuterInstance = outerInstance;
                this.NumTerms = numTerms;
                this.MaxTermsPerDoc = maxTermsPerDoc;
                this.Postings = postings;
                this.Iw = iw;
                this.StartingGun = startingGun;
                this.ThreadRandom = threadRandom;
                this.Document = document;
                this.Field = field;
            }

            public override void Run()
            {
                try
                {
                    StartingGun.Wait();
                    while (!(Postings.Count == 0))
                    {
                        StringBuilder text = new StringBuilder();
                        int numTerms = ThreadRandom.Next(MaxTermsPerDoc);
                        for (int i = 0; i < numTerms; i++)
                        {
                            string token;
                            if (!Postings.TryDequeue(out token))
                            {
                                break;
                            }
                            text.Append(' ');
                            text.Append(token);
                        }
                        Field.StringValue = text.ToString();
                        Iw.AddDocument(Document);
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