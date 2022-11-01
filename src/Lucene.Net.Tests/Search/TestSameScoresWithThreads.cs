using J2N.Collections.Generic.Extensions;
using J2N.Threading;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestSameScoresWithThreads : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            Directory dir = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(Random, 1, IndexWriter.MAX_TERM_LENGTH);
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, analyzer);
            LineFileDocs docs = new LineFileDocs(Random, DefaultCodecSupportsDocValues);
            int charsToIndex = AtLeast(100000);
            int charsIndexed = 0;
            //System.out.println("bytesToIndex=" + charsToIndex);
            while (charsIndexed < charsToIndex)
            {
                Document doc = docs.NextDoc();
                charsIndexed += doc.Get("body").Length;
                w.AddDocument(doc);
                //System.out.println("  bytes=" + charsIndexed + " add: " + doc);
            }
            IndexReader r = w.GetReader();
            //System.out.println("numDocs=" + r.NumDocs);
            w.Dispose();

            IndexSearcher s = NewSearcher(r);
            Terms terms = MultiFields.GetFields(r).GetTerms("body");
            int termCount = 0;
            TermsEnum termsEnum = terms.GetEnumerator();
            while (termsEnum.MoveNext())
            {
                termCount++;
            }
            Assert.IsTrue(termCount > 0);

            // Target ~10 terms to search:
            double chance = 10.0 / termCount;
            termsEnum = terms.GetEnumerator(termsEnum);
            IDictionary<BytesRef, TopDocs> answers = new Dictionary<BytesRef, TopDocs>();
            while (termsEnum.MoveNext())
            {
                if (Random.NextDouble() <= chance)
                {
                    BytesRef term = BytesRef.DeepCopyOf(termsEnum.Term);
                    answers[term] = s.Search(new TermQuery(new Term("body", term)), 100);
                }
            }

            if (answers.Count > 0)
            {
                CountdownEvent startingGun = new CountdownEvent(1);
                int numThreads = TestUtil.NextInt32(Random, 2, 5);
                ThreadJob[] threads = new ThreadJob[numThreads];
                for (int threadID = 0; threadID < numThreads; threadID++)
                {
                    ThreadJob thread = new ThreadAnonymousClass(this, s, answers, startingGun);
                    threads[threadID] = thread;
                    thread.Start();
                }
                startingGun.Signal();
                foreach (ThreadJob thread in threads)
                {
                    thread.Join();
                }
            }
            r.Dispose();
            dir.Dispose();
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestSameScoresWithThreads outerInstance;

            private readonly IndexSearcher s;
            private readonly IDictionary<BytesRef, TopDocs> answers;
            private readonly CountdownEvent startingGun;

            public ThreadAnonymousClass(TestSameScoresWithThreads outerInstance, IndexSearcher s, IDictionary<BytesRef, TopDocs> answers, CountdownEvent startingGun)
            {
                this.outerInstance = outerInstance;
                this.s = s;
                this.answers = answers;
                this.startingGun = startingGun;
            }

            public override void Run()
            {
                try
                {
                    startingGun.Wait();
                    for (int i = 0; i < 20; i++)
                    {
                        IList<KeyValuePair<BytesRef, TopDocs>> shuffled = new JCG.List<KeyValuePair<BytesRef, TopDocs>>(answers);
                        shuffled.Shuffle(Random);
                        foreach (KeyValuePair<BytesRef, TopDocs> ent in shuffled)
                        {
                            TopDocs actual = s.Search(new TermQuery(new Term("body", ent.Key)), 100);
                            TopDocs expected = ent.Value;
                            Assert.AreEqual(expected.TotalHits, actual.TotalHits);
                            Assert.AreEqual(expected.ScoreDocs.Length, actual.ScoreDocs.Length, "query=" + ent.Key.Utf8ToString());
                            for (int hit = 0; hit < expected.ScoreDocs.Length; hit++)
                            {
                                Assert.AreEqual(expected.ScoreDocs[hit].Doc, actual.ScoreDocs[hit].Doc);
                                // Floats really should be identical:
                                Assert.IsTrue(expected.ScoreDocs[hit].Score == actual.ScoreDocs[hit].Score);
                            }
                        }
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