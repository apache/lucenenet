﻿using J2N.Collections.Generic.Extensions;
using J2N.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using JCG = J2N.Collections.Generic;
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
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
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
        [Slow]
        public virtual void Test()
        {
            IList<string> postingsList = new JCG.List<string>();
            int numTerms = AtLeast(300);
            int maxTermsPerDoc = TestUtil.NextInt32(Random, 10, 20);
            bool isSimpleText = "SimpleText".Equals(TestUtil.GetPostingsFormat("field"), StringComparison.Ordinal);

            IndexWriterConfig iwc = NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            if ((isSimpleText || iwc.MergePolicy is MockRandomMergePolicy) && (TestNightly || RandomMultiplier > 1))
            {
                // Otherwise test can take way too long (> 2 hours)
                //numTerms /= 2;
                // LUCENENET specific - To keep this under the 1 hour free limit
                // of Azure DevOps, this was reduced from /2 to /6.
                numTerms /= 6;
            }
            if (Verbose)
            {
                Console.WriteLine("maxTermsPerDoc=" + maxTermsPerDoc);
                Console.WriteLine("numTerms=" + numTerms);
            }
            for (int i = 0; i < numTerms; i++)
            {
                string term = Convert.ToString(i, CultureInfo.InvariantCulture);
                for (int j = 0; j < i; j++)
                {
                    postingsList.Add(term);
                }
            }

            postingsList.Shuffle(Random);

            ConcurrentQueue<string> postings = new ConcurrentQueue<string>(postingsList);

            Directory dir = NewFSDirectory(CreateTempDir(GetFullMethodName()));

            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwc);

            int threadCount = TestUtil.NextInt32(Random, 1, 5);
            if (Verbose)
            {
                Console.WriteLine("config: " + iw.IndexWriter.Config);
                Console.WriteLine("threadCount=" + threadCount);
            }

            Field prototype = NewTextField("field", "", Field.Store.NO);
            FieldType fieldType = new FieldType(prototype.FieldType);
            if (Random.NextBoolean())
            {
                fieldType.OmitNorms = true;
            }
            int options = Random.Next(3);
            if (options == 0)
            {
                fieldType.IndexOptions = IndexOptions.DOCS_AND_FREQS; // we dont actually need positions
                fieldType.StoreTermVectors = true; // but enforce term vectors when we do this so we check SOMETHING
            }
            else if (options == 1 && !DoesntSupportOffsets.Contains(TestUtil.GetPostingsFormat("field")))
            {
                fieldType.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            }
            // else just positions

            ThreadJob[] threads = new ThreadJob[threadCount];
            CountdownEvent startingGun = new CountdownEvent(1);

            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                Random threadRandom = new J2N.Randomizer(Random.NextInt64());
                Document document = new Document();
                Field field = new Field("field", "", fieldType);
                document.Add(field);
                threads[threadID] = new ThreadAnonymousClass(maxTermsPerDoc, postings, iw, startingGun, threadRandom, document, field);
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
            Assert.AreEqual(numTerms - 1, terms.Count);
            TermsEnum termsEnum = terms.GetEnumerator();
            while (termsEnum.MoveNext())
            {
                int value = Convert.ToInt32(termsEnum.Term.Utf8ToString(), CultureInfo.InvariantCulture);
                Assert.AreEqual(value, termsEnum.TotalTermFreq);
                // don't really need to check more than this, as CheckIndex
                // will verify that totalTermFreq == total number of positions seen
                // from a docsAndPositionsEnum.
            }
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly int maxTermsPerDoc;
            private readonly ConcurrentQueue<string> postings;
            private readonly RandomIndexWriter iw;
            private readonly CountdownEvent startingGun;
            private readonly Random threadRandom;
            private readonly Document document;
            private readonly Field field;

            public ThreadAnonymousClass(int maxTermsPerDoc, ConcurrentQueue<string> postings, RandomIndexWriter iw, CountdownEvent startingGun, Random threadRandom, Document document, Field field)
            {
                this.maxTermsPerDoc = maxTermsPerDoc;
                this.postings = postings;
                this.iw = iw;
                this.startingGun = startingGun;
                this.threadRandom = threadRandom;
                this.document = document;
                this.field = field;
            }

            public override void Run()
            {
                try
                {
                    startingGun.Wait();
                    while (!postings.IsEmpty)
                    {
                        StringBuilder text = new StringBuilder();
                        int numTerms = threadRandom.Next(maxTermsPerDoc);
                        for (int i = 0; i < numTerms; i++)
                        {
                            if (!postings.TryDequeue(out string token))
                            {
                                break;
                            }
                            text.Append(' ');
                            text.Append(token);
                        }
                        field.SetStringValue(text.ToString());
                        iw.AddDocument(document);
                    }
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }
    }
}
