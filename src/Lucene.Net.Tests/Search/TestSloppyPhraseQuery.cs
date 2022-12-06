using J2N.Text;
using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    [TestFixture]
    public class TestSloppyPhraseQuery : LuceneTestCase
    {
        private static readonly Regex SPACES = new Regex(" +", RegexOptions.Compiled);

        private const string S_1 = "A A A";
        private const string S_2 = "A 1 2 3 A 4 5 6 A";

        private static readonly Document DOC_1 = MakeDocument("X " + S_1 + " Y");
        private static readonly Document DOC_2 = MakeDocument("X " + S_2 + " Y");
        private static readonly Document DOC_3 = MakeDocument("X " + S_1 + " A Y");
        private static readonly Document DOC_1_B = MakeDocument("X " + S_1 + " Y N N N N " + S_1 + " Z");
        private static readonly Document DOC_2_B = MakeDocument("X " + S_2 + " Y N N N N " + S_2 + " Z");
        private static readonly Document DOC_3_B = MakeDocument("X " + S_1 + " A Y N N N N " + S_1 + " A Y");
        private static readonly Document DOC_4 = MakeDocument("A A X A X B A X B B A A X B A A");
        private static readonly Document DOC_5_3 = MakeDocument("H H H X X X H H H X X X H H H");
        private static readonly Document DOC_5_4 = MakeDocument("H H H H");

        private static readonly PhraseQuery QUERY_1 = MakePhraseQuery(S_1);
        private static readonly PhraseQuery QUERY_2 = MakePhraseQuery(S_2);
        private static readonly PhraseQuery QUERY_4 = MakePhraseQuery("X A A");
        private static readonly PhraseQuery QUERY_5_4 = MakePhraseQuery("H H H H");

        /// <summary>
        /// Test DOC_4 and QUERY_4.
        /// QUERY_4 has a fuzzy (len=1) match to DOC_4, so all slop values > 0 should succeed.
        /// But only the 3rd sequence of A's in DOC_4 will do.
        /// </summary>
        [Test]
        public virtual void TestDoc4_Query4_All_Slops_Should_match()
        {
            for (int slop = 0; slop < 30; slop++)
            {
                int numResultsExpected = slop < 1 ? 0 : 1;
                CheckPhraseQuery(DOC_4, QUERY_4, slop, numResultsExpected);
            }
        }

        /// <summary>
        /// Test DOC_1 and QUERY_1.
        /// QUERY_1 has an exact match to DOC_1, so all slop values should succeed.
        /// Before LUCENE-1310, a slop value of 1 did not succeed.
        /// </summary>
        [Test]
        public virtual void TestDoc1_Query1_All_Slops_Should_match()
        {
            for (int slop = 0; slop < 30; slop++)
            {
                float freq1 = CheckPhraseQuery(DOC_1, QUERY_1, slop, 1);
                float freq2 = CheckPhraseQuery(DOC_1_B, QUERY_1, slop, 1);
                Assert.IsTrue(freq2 > freq1, "slop=" + slop + " freq2=" + freq2 + " should be greater than score1 " + freq1);
            }
        }

        /// <summary>
        /// Test DOC_2 and QUERY_1.
        /// 6 should be the minimum slop to make QUERY_1 match DOC_2.
        /// Before LUCENE-1310, 7 was the minimum.
        /// </summary>
        [Test]
        public virtual void TestDoc2_Query1_Slop_6_or_more_Should_match()
        {
            for (int slop = 0; slop < 30; slop++)
            {
                int numResultsExpected = slop < 6 ? 0 : 1;
                float freq1 = CheckPhraseQuery(DOC_2, QUERY_1, slop, numResultsExpected);
                if (numResultsExpected > 0)
                {
                    float freq2 = CheckPhraseQuery(DOC_2_B, QUERY_1, slop, 1);
                    Assert.IsTrue(freq2 > freq1, "slop=" + slop + " freq2=" + freq2 + " should be greater than freq1 " + freq1);
                }
            }
        }

        /// <summary>
        /// Test DOC_2 and QUERY_2.
        /// QUERY_2 has an exact match to DOC_2, so all slop values should succeed.
        /// Before LUCENE-1310, 0 succeeds, 1 through 7 fail, and 8 or greater succeeds.
        /// </summary>
        [Test]
        public virtual void TestDoc2_Query2_All_Slops_Should_match()
        {
            for (int slop = 0; slop < 30; slop++)
            {
                float freq1 = CheckPhraseQuery(DOC_2, QUERY_2, slop, 1);
                float freq2 = CheckPhraseQuery(DOC_2_B, QUERY_2, slop, 1);
                Assert.IsTrue(freq2 > freq1, "slop=" + slop + " freq2=" + freq2 + " should be greater than freq1 " + freq1);
            }
        }

        /// <summary>
        /// Test DOC_3 and QUERY_1.
        /// QUERY_1 has an exact match to DOC_3, so all slop values should succeed.
        /// </summary>
        [Test]
        public virtual void TestDoc3_Query1_All_Slops_Should_match()
        {
            for (int slop = 0; slop < 30; slop++)
            {
                float freq1 = CheckPhraseQuery(DOC_3, QUERY_1, slop, 1);
                float freq2 = CheckPhraseQuery(DOC_3_B, QUERY_1, slop, 1);
                Assert.IsTrue(freq2 > freq1, "slop=" + slop + " freq2=" + freq2 + " should be greater than freq1 " + freq1);
            }
        }

        /// <summary>
        /// LUCENE-3412 </summary>
        [Test]
        public virtual void TestDoc5_Query5_Any_Slop_Should_be_consistent()
        {
            int nRepeats = 5;
            for (int slop = 0; slop < 3; slop++)
            {
                for (int trial = 0; trial < nRepeats; trial++)
                {
                    // should steadily always find this one
                    CheckPhraseQuery(DOC_5_4, QUERY_5_4, slop, 1);
                }
                for (int trial = 0; trial < nRepeats; trial++)
                {
                    // should steadily never find this one
                    CheckPhraseQuery(DOC_5_3, QUERY_5_4, slop, 0);
                }
            }
        }

        private float CheckPhraseQuery(Document doc, PhraseQuery query, int slop, int expectedNumResults)
        {
            query.Slop = slop;

            Directory ramDir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, ramDir, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));
            writer.AddDocument(doc);

            IndexReader reader = writer.GetReader();

            IndexSearcher searcher = NewSearcher(reader);
            MaxFreqCollector c = new MaxFreqCollector();
            searcher.Search(query, c);
            Assert.AreEqual(expectedNumResults, c.totalHits, "slop: " + slop + "  query: " + query + "  doc: " + doc + "  Wrong number of hits");

            //QueryUtils.Check(query,searcher);
            writer.Dispose();
            reader.Dispose();
            ramDir.Dispose();

            // returns the max Scorer.Freq() found, because even though norms are omitted, many index stats are different
            // with these different tokens/distributions/lengths.. otherwise this test is very fragile.
            return c.max;
        }

        private static Document MakeDocument(string docText)
        {
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            Field f = new Field("f", docText, customType);
            doc.Add(f);
            return doc;
        }

        

        private static PhraseQuery MakePhraseQuery(string terms)
        {
            PhraseQuery query = new PhraseQuery();
            string[] t = SPACES.Split(terms).TrimEnd();
            for (int i = 0; i < t.Length; i++)
            {
                query.Add(new Term("f", t[i]));
            }
            return query;
        }

        internal class MaxFreqCollector : ICollector
        {
            internal float max;
            internal int totalHits;
            internal Scorer scorer;

            public virtual void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public virtual void Collect(int doc)
            {
                totalHits++;
                max = Math.Max(max, scorer.Freq);
            }

            public virtual void SetNextReader(AtomicReaderContext context)
            {
            }

            public virtual bool AcceptsDocsOutOfOrder => false;
        }

        /// <summary>
        /// checks that no scores or freqs are infinite </summary>
        private void AssertSaneScoring(PhraseQuery pq, IndexSearcher searcher)
        {
            searcher.Search(pq, new CollectorAnonymousClass(this));
            QueryUtils.Check(Random, pq, searcher);
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly TestSloppyPhraseQuery outerInstance;

            public CollectorAnonymousClass(TestSloppyPhraseQuery outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal Scorer scorer;

            public void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public void Collect(int doc)
            {
                Assert.IsFalse(float.IsInfinity(scorer.Freq));
                Assert.IsFalse(float.IsInfinity(scorer.GetScore()));
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                // do nothing
            }

            public bool AcceptsDocsOutOfOrder => false;
        }

        // LUCENE-3215
        [Test]
        public virtual void TestSlopWithHoles()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            Field f = new Field("lyrics", "", customType);
            Document doc = new Document();
            doc.Add(f);
            f.SetStringValue("drug drug");
            iw.AddDocument(doc);
            f.SetStringValue("drug druggy drug");
            iw.AddDocument(doc);
            f.SetStringValue("drug druggy druggy drug");
            iw.AddDocument(doc);
            f.SetStringValue("drug druggy drug druggy drug");
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            PhraseQuery pq = new PhraseQuery();
            // "drug the drug"~1
            pq.Add(new Term("lyrics", "drug"), 1);
            pq.Add(new Term("lyrics", "drug"), 4);
            pq.Slop = 0;
            Assert.AreEqual(0, @is.Search(pq, 4).TotalHits);
            pq.Slop = 1;
            Assert.AreEqual(3, @is.Search(pq, 4).TotalHits);
            pq.Slop = 2;
            Assert.AreEqual(4, @is.Search(pq, 4).TotalHits);
            ir.Dispose();
            dir.Dispose();
        }

        // LUCENE-3215
        [Test]
        public virtual void TestInfiniteFreq1()
        {
            string document = "drug druggy drug drug drug";

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewField("lyrics", document, new FieldType(TextField.TYPE_NOT_STORED)));
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher @is = NewSearcher(ir);
            PhraseQuery pq = new PhraseQuery();
            // "drug the drug"~1
            pq.Add(new Term("lyrics", "drug"), 1);
            pq.Add(new Term("lyrics", "drug"), 3);
            pq.Slop = 1;
            AssertSaneScoring(pq, @is);
            ir.Dispose();
            dir.Dispose();
        }

        // LUCENE-3215
        [Test]
        public virtual void TestInfiniteFreq2()
        {
            string document = "So much fun to be had in my head " + "No more sunshine " + "So much fun just lying in my bed " + "No more sunshine " + "I can't face the sunlight and the dirt outside " + "Wanna stay in 666 where this darkness don't lie " + "Drug drug druggy " + "Got a feeling sweet like honey " + "Drug drug druggy " + "Need sensation like my baby " + "Show me your scars you're so aware " + "I'm not barbaric I just care " + "Drug drug drug " + "I need a reflection to prove I exist " + "No more sunshine " + "I am a victim of designer blitz " + "No more sunshine " + "Dance like a robot when you're chained at the knee " + "The C.I.A say you're all they'll ever need " + "Drug drug druggy " + "Got a feeling sweet like honey " + "Drug drug druggy " + "Need sensation like my baby " + "Snort your lines you're so aware " + "I'm not barbaric I just care " + "Drug drug druggy " + "Got a feeling sweet like honey " + "Drug drug druggy " + "Need sensation like my baby";

            Directory dir = NewDirectory();

            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewField("lyrics", document, new FieldType(TextField.TYPE_NOT_STORED)));
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();

            IndexSearcher @is = NewSearcher(ir);

            PhraseQuery pq = new PhraseQuery();
            // "drug the drug"~5
            pq.Add(new Term("lyrics", "drug"), 1);
            pq.Add(new Term("lyrics", "drug"), 3);
            pq.Slop = 5;
            AssertSaneScoring(pq, @is);
            ir.Dispose();
            dir.Dispose();
        }
    }
}