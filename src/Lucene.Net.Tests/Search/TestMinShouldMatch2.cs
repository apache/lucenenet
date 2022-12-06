using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BooleanWeight = Lucene.Net.Search.BooleanQuery.BooleanWeight;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SimScorer = Lucene.Net.Search.Similarities.Similarity.SimScorer;
    using SimWeight = Lucene.Net.Search.Similarities.Similarity.SimWeight;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// tests BooleanScorer2's minShouldMatch </summary>
    [SuppressCodecs("Appending", "Lucene3x", "Lucene40", "Lucene41")]
    [TestFixture]
    public class TestMinShouldMatch2 : LuceneTestCase
    {
        internal static Directory dir;
        internal static DirectoryReader r;
        internal static AtomicReader atomicReader;
        internal static IndexSearcher searcher;

        internal static readonly string[] alwaysTerms = new string[] { "a" };
        internal static readonly string[] commonTerms = new string[] { "b", "c", "d" };
        internal static readonly string[] mediumTerms = new string[] { "e", "f", "g" };
        internal static readonly string[] rareTerms = new string[] { "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because Similarity and TimeZone are not static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();

                AddSome(doc, alwaysTerms);

                if (Random.Next(100) < 90)
                {
                    AddSome(doc, commonTerms);
                }
                if (Random.Next(100) < 50)
                {
                    AddSome(doc, mediumTerms);
                }
                if (Random.Next(100) < 10)
                {
                    AddSome(doc, rareTerms);
                }
                iw.AddDocument(doc);
            }
            iw.ForceMerge(1);
            iw.Dispose();
            r = DirectoryReader.Open(dir);
            atomicReader = GetOnlySegmentReader(r);
            searcher = new IndexSearcher(atomicReader);
            searcher.Similarity = new DefaultSimilarityAnonymousClass();
        }

        private sealed class DefaultSimilarityAnonymousClass : DefaultSimilarity
        {
            public DefaultSimilarityAnonymousClass()
            {
            }

            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 1; // we disable queryNorm, both for debugging and ease of impl
            }
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            atomicReader.Dispose();
            dir.Dispose();
            searcher = null;
            atomicReader = null;
            r = null;
            dir = null;
            base.AfterClass();
        }

        private static void AddSome(Document doc, string[] values)
        {
            IList<string> list = values.ToArray();
            list.Shuffle(Random);
            int howMany = TestUtil.NextInt32(Random, 1, list.Count);
            for (int i = 0; i < howMany; i++)
            {
                doc.Add(new StringField("field", list[i], Field.Store.NO));
                doc.Add(new SortedSetDocValuesField("dv", new BytesRef(list[i])));
            }
        }

        private Scorer Scorer(string[] values, int minShouldMatch, bool slow)
        {
            BooleanQuery bq = new BooleanQuery();
            foreach (string value in values)
            {
                bq.Add(new TermQuery(new Term("field", value)), Occur.SHOULD);
            }
            bq.MinimumNumberShouldMatch = minShouldMatch;

            BooleanWeight weight = (BooleanWeight)searcher.CreateNormalizedWeight(bq);

            if (slow)
            {
                return new SlowMinShouldMatchScorer(weight, atomicReader, searcher);
            }
            else
            {
                return weight.GetScorer((AtomicReaderContext)atomicReader.Context, null);
            }
        }

        private void AssertNext(Scorer expected, Scorer actual)
        {
            if (actual is null)
            {
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, expected.NextDoc());
                return;
            }
            int doc;
            while ((doc = expected.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(doc, actual.NextDoc());
                Assert.AreEqual(expected.Freq, actual.Freq);
                float expectedScore = expected.GetScore();
                float actualScore = actual.GetScore();
                Assert.AreEqual(expectedScore, actualScore, CheckHits.ExplainToleranceDelta(expectedScore, actualScore));
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, actual.NextDoc());
        }

        private void AssertAdvance(Scorer expected, Scorer actual, int amount)
        {
            if (actual is null)
            {
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, expected.NextDoc());
                return;
            }
            int prevDoc = 0;
            int doc;
            while ((doc = expected.Advance(prevDoc + amount)) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(doc, actual.Advance(prevDoc + amount));
                Assert.AreEqual(expected.Freq, actual.Freq);
                float expectedScore = expected.GetScore();
                float actualScore = actual.GetScore();
                Assert.AreEqual(expectedScore, actualScore, CheckHits.ExplainToleranceDelta(expectedScore, actualScore));
                prevDoc = doc;
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, actual.Advance(prevDoc + amount));
        }

        /// <summary>
        /// simple test for next(): minShouldMatch=2 on 3 terms (one common, one medium, one rare) </summary>
        [Test]
        public virtual void TestNextCMR2()
        {
            for (int common = 0; common < commonTerms.Length; common++)
            {
                for (int medium = 0; medium < mediumTerms.Length; medium++)
                {
                    for (int rare = 0; rare < rareTerms.Length; rare++)
                    {
                        Scorer expected = Scorer(new string[] { commonTerms[common], mediumTerms[medium], rareTerms[rare] }, 2, true);
                        Scorer actual = Scorer(new string[] { commonTerms[common], mediumTerms[medium], rareTerms[rare] }, 2, false);
                        AssertNext(expected, actual);
                    }
                }
            }
        }

        /// <summary>
        /// simple test for advance(): minShouldMatch=2 on 3 terms (one common, one medium, one rare) </summary>
        [Test]
        public virtual void TestAdvanceCMR2()
        {
            for (int amount = 25; amount < 200; amount += 25)
            {
                for (int common = 0; common < commonTerms.Length; common++)
                {
                    for (int medium = 0; medium < mediumTerms.Length; medium++)
                    {
                        for (int rare = 0; rare < rareTerms.Length; rare++)
                        {
                            Scorer expected = Scorer(new string[] { commonTerms[common], mediumTerms[medium], rareTerms[rare] }, 2, true);
                            Scorer actual = Scorer(new string[] { commonTerms[common], mediumTerms[medium], rareTerms[rare] }, 2, false);
                            AssertAdvance(expected, actual, amount);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// test next with giant bq of all terms with varying minShouldMatch </summary>
        [Test]
        public virtual void TestNextAllTerms()
        {
            IList<string> termsList = new JCG.List<string>(commonTerms.Length + mediumTerms.Length + rareTerms.Length);
            termsList.AddRange(commonTerms);
            termsList.AddRange(mediumTerms);
            termsList.AddRange(rareTerms);
            string[] terms = termsList.ToArray();

            for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch++)
            {
                Scorer expected = Scorer(terms, minNrShouldMatch, true);
                Scorer actual = Scorer(terms, minNrShouldMatch, false);
                AssertNext(expected, actual);
            }
        }

        /// <summary>
        /// test advance with giant bq of all terms with varying minShouldMatch </summary>
        [Test]
        public virtual void TestAdvanceAllTerms()
        {
            IList<string> termsList = new JCG.List<string>(commonTerms.Length + mediumTerms.Length + rareTerms.Length);
            termsList.AddRange(commonTerms);
            termsList.AddRange(mediumTerms);
            termsList.AddRange(rareTerms);
            string[] terms = termsList.ToArray();

            for (int amount = 25; amount < 200; amount += 25)
            {
                for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch++)
                {
                    Scorer expected = Scorer(terms, minNrShouldMatch, true);
                    Scorer actual = Scorer(terms, minNrShouldMatch, false);
                    AssertAdvance(expected, actual, amount);
                }
            }
        }

        /// <summary>
        /// test next with varying numbers of terms with varying minShouldMatch </summary>
        [Test]
        public virtual void TestNextVaryingNumberOfTerms()
        {
            IList<string> termsList = new JCG.List<string>(commonTerms.Length + mediumTerms.Length + rareTerms.Length);
            termsList.AddRange(commonTerms);
            termsList.AddRange(mediumTerms);
            termsList.AddRange(rareTerms);
            termsList.Shuffle(Random);

            for (int numTerms = 2; numTerms <= termsList.Count; numTerms++)
            {
                string[] terms = termsList.GetView(0, numTerms).ToArray(/*new string[0]*/); // LUCENENET: Checked length of GetView() for correctness
                for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch++)
                {
                    Scorer expected = Scorer(terms, minNrShouldMatch, true);
                    Scorer actual = Scorer(terms, minNrShouldMatch, false);
                    AssertNext(expected, actual);
                }
            }
        }

        /// <summary>
        /// test advance with varying numbers of terms with varying minShouldMatch </summary>
        [Test]
        public virtual void TestAdvanceVaryingNumberOfTerms()
        {
            IList<string> termsList = new JCG.List<string>(commonTerms.Length + mediumTerms.Length + rareTerms.Length);
            termsList.AddRange(commonTerms);
            termsList.AddRange(mediumTerms);
            termsList.AddRange(rareTerms);
            termsList.Shuffle(Random);

            for (int amount = 25; amount < 200; amount += 25)
            {
                for (int numTerms = 2; numTerms <= termsList.Count; numTerms++)
                {
                    string[] terms = termsList.GetView(0, numTerms).ToArray(/*new string[0]*/); // LUCENENET: Checked length of GetView() for correctness
                    for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch++)
                    {
                        Scorer expected = Scorer(terms, minNrShouldMatch, true);
                        Scorer actual = Scorer(terms, minNrShouldMatch, false);
                        AssertAdvance(expected, actual, amount);
                    }
                }
            }
        }

        // TODO: more tests

        // a slow min-should match scorer that uses a docvalues field.
        // later, we can make debugging easier as it can record the set of ords it currently matched
        // and e.g. print out their values and so on for the document
        internal class SlowMinShouldMatchScorer : Scorer
        {
            internal int currentDoc = -1; // current docid
            internal int currentMatched = -1; // current number of terms matched

            internal readonly SortedSetDocValues dv;
            internal readonly int maxDoc;

            internal readonly ISet<long> ords = new JCG.HashSet<long>();
            internal readonly SimScorer[] sims;
            internal readonly int minNrShouldMatch;

            internal double score = float.NaN;

            internal SlowMinShouldMatchScorer(BooleanWeight weight, AtomicReader reader, IndexSearcher searcher)
                : base(weight)
            {
                this.dv = reader.GetSortedSetDocValues("dv");
                this.maxDoc = reader.MaxDoc;
                BooleanQuery bq = (BooleanQuery)weight.Query;
                this.minNrShouldMatch = bq.MinimumNumberShouldMatch;
                this.sims = new SimScorer[(int)dv.ValueCount];
                foreach (BooleanClause clause in bq.GetClauses())
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(!clause.IsProhibited);
                    if (Debugging.AssertsEnabled) Debugging.Assert(!clause.IsRequired);
                    Term term = ((TermQuery)clause.Query).Term;
                    long ord = dv.LookupTerm(term.Bytes);
                    if (ord >= 0)
                    {
                        bool success = ords.Add(ord);
                        if (Debugging.AssertsEnabled) Debugging.Assert(success); // no dups
                        TermContext context = TermContext.Build(reader.Context, term);
                        SimWeight w = weight.Similarity.ComputeWeight(1f, searcher.CollectionStatistics("field"), searcher.TermStatistics(term, context));
                        var dummy = w.GetValueForNormalization(); // ignored
                        w.Normalize(1F, 1F);
                        sims[(int)ord] = weight.Similarity.GetSimScorer(w, (AtomicReaderContext)reader.Context);
                    }
                }
            }

            public override float GetScore()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(score != 0, currentMatched.ToString());
                return (float)score * ((BooleanWeight)m_weight).Coord(currentMatched, ((BooleanWeight)m_weight).MaxCoord);
            }

            public override int Freq => currentMatched;

            public override int DocID => currentDoc;

            public override int NextDoc()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(currentDoc != NO_MORE_DOCS);
                for (currentDoc = currentDoc + 1; currentDoc < maxDoc; currentDoc++)
                {
                    currentMatched = 0;
                    score = 0;
                    dv.SetDocument(currentDoc);
                    long ord;
                    while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        if (ords.Contains(ord))
                        {
                            currentMatched++;
                            score += sims[(int)ord].Score(currentDoc, 1);
                        }
                    }
                    if (currentMatched >= minNrShouldMatch)
                    {
                        return currentDoc;
                    }
                }
                return currentDoc = NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                int doc;
                while ((doc = NextDoc()) < target)
                {
                }
                return doc;
            }

            public override long GetCost()
            {
                return maxDoc;
            }
        }
    }
}