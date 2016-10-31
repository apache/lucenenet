using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BooleanWeight = Lucene.Net.Search.BooleanQuery.BooleanWeight;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;

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
        internal static Directory Dir;
        internal static DirectoryReader r;
        internal static AtomicReader atomicReader;
        internal static IndexSearcher Searcher;

        internal static readonly string[] AlwaysTerms = new string[] { "a" };
        internal static readonly string[] CommonTerms = new string[] { "b", "c", "d" };
        internal static readonly string[] MediumTerms = new string[] { "e", "f", "g" };
        internal static readonly string[] RareTerms = new string[] { "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because Similarity and TimeZone are not static.
        /// </summary>
        [OneTimeSetUp]
        public void BeforeClass()
        {
            Dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), Dir, Similarity, TimeZone);
            int numDocs = AtLeast(300);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();

                AddSome(doc, AlwaysTerms);

                if (Random().Next(100) < 90)
                {
                    AddSome(doc, CommonTerms);
                }
                if (Random().Next(100) < 50)
                {
                    AddSome(doc, MediumTerms);
                }
                if (Random().Next(100) < 10)
                {
                    AddSome(doc, RareTerms);
                }
                iw.AddDocument(doc);
            }
            iw.ForceMerge(1);
            iw.Dispose();
            r = DirectoryReader.Open(Dir);
            atomicReader = GetOnlySegmentReader(r);
            Searcher = new IndexSearcher(atomicReader);
            Searcher.Similarity = new DefaultSimilarityAnonymousInnerClassHelper();
        }

        private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
        {
            public DefaultSimilarityAnonymousInnerClassHelper()
            {
            }

            public override float QueryNorm(float sumOfSquaredWeights)
            {
                return 1; // we disable queryNorm, both for debugging and ease of impl
            }
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            atomicReader.Dispose();
            Dir.Dispose();
            Searcher = null;
            atomicReader = null;
            r = null;
            Dir = null;
        }

        private static void AddSome(Document doc, string[] values)
        {
            IList<string> list = Arrays.AsList(values);
            list = CollectionsHelper.Shuffle(list);
            int howMany = TestUtil.NextInt(Random(), 1, list.Count);
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
                bq.Add(new TermQuery(new Term("field", value)), BooleanClause.Occur.SHOULD);
            }
            bq.MinimumNumberShouldMatch = minShouldMatch;

            BooleanWeight weight = (BooleanWeight)Searcher.CreateNormalizedWeight(bq);

            if (slow)
            {
                return new SlowMinShouldMatchScorer(weight, atomicReader, Searcher);
            }
            else
            {
                return weight.Scorer((AtomicReaderContext)atomicReader.Context, null);
            }
        }

        private void AssertNext(Scorer expected, Scorer actual)
        {
            if (actual == null)
            {
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, expected.NextDoc());
                return;
            }
            int doc;
            while ((doc = expected.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(doc, actual.NextDoc());
                Assert.AreEqual(expected.Freq(), actual.Freq());
                float expectedScore = expected.Score();
                float actualScore = actual.Score();
                Assert.AreEqual(expectedScore, actualScore, CheckHits.ExplainToleranceDelta(expectedScore, actualScore));
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, actual.NextDoc());
        }

        private void AssertAdvance(Scorer expected, Scorer actual, int amount)
        {
            if (actual == null)
            {
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, expected.NextDoc());
                return;
            }
            int prevDoc = 0;
            int doc;
            while ((doc = expected.Advance(prevDoc + amount)) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(doc, actual.Advance(prevDoc + amount));
                Assert.AreEqual(expected.Freq(), actual.Freq());
                float expectedScore = expected.Score();
                float actualScore = actual.Score();
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
            for (int common = 0; common < CommonTerms.Length; common++)
            {
                for (int medium = 0; medium < MediumTerms.Length; medium++)
                {
                    for (int rare = 0; rare < RareTerms.Length; rare++)
                    {
                        Scorer expected = Scorer(new string[] { CommonTerms[common], MediumTerms[medium], RareTerms[rare] }, 2, true);
                        Scorer actual = Scorer(new string[] { CommonTerms[common], MediumTerms[medium], RareTerms[rare] }, 2, false);
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
                for (int common = 0; common < CommonTerms.Length; common++)
                {
                    for (int medium = 0; medium < MediumTerms.Length; medium++)
                    {
                        for (int rare = 0; rare < RareTerms.Length; rare++)
                        {
                            Scorer expected = Scorer(new string[] { CommonTerms[common], MediumTerms[medium], RareTerms[rare] }, 2, true);
                            Scorer actual = Scorer(new string[] { CommonTerms[common], MediumTerms[medium], RareTerms[rare] }, 2, false);
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
            IList<string> termsList = new List<string>();
            termsList.AddRange(Arrays.AsList(CommonTerms));
            termsList.AddRange(Arrays.AsList(MediumTerms));
            termsList.AddRange(Arrays.AsList(RareTerms));
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
            IList<string> termsList = new List<string>();
            termsList.AddRange(Arrays.AsList(CommonTerms));
            termsList.AddRange(Arrays.AsList(MediumTerms));
            termsList.AddRange(Arrays.AsList(RareTerms));
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
            IList<string> termsList = new List<string>();
            termsList.AddRange(Arrays.AsList(CommonTerms));
            termsList.AddRange(Arrays.AsList(MediumTerms));
            termsList.AddRange(Arrays.AsList(RareTerms));
            termsList = CollectionsHelper.Shuffle(termsList);

            for (int numTerms = 2; numTerms <= termsList.Count; numTerms++)
            {
                string[] terms = termsList.SubList(0, numTerms).ToArray(/*new string[0]*/);
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
            IList<string> termsList = new List<string>();
            termsList.AddRange(Arrays.AsList(CommonTerms));
            termsList.AddRange(Arrays.AsList(MediumTerms));
            termsList.AddRange(Arrays.AsList(RareTerms));
            termsList = CollectionsHelper.Shuffle(termsList);

            for (int amount = 25; amount < 200; amount += 25)
            {
                for (int numTerms = 2; numTerms <= termsList.Count; numTerms++)
                {
                    string[] terms = termsList.SubList(0, numTerms).ToArray(/*new string[0]*/);
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
            internal int CurrentDoc = -1; // current docid
            internal int CurrentMatched = -1; // current number of terms matched

            internal readonly SortedSetDocValues Dv;
            internal readonly int MaxDoc;

            internal readonly HashSet<long?> Ords = new HashSet<long?>();
            internal readonly SimScorer[] Sims;
            internal readonly int MinNrShouldMatch;

            internal double Score_Renamed = float.NaN;

            internal SlowMinShouldMatchScorer(BooleanWeight weight, AtomicReader reader, IndexSearcher searcher)
                : base(weight)
            {
                this.Dv = reader.GetSortedSetDocValues("dv");
                this.MaxDoc = reader.MaxDoc;
                BooleanQuery bq = (BooleanQuery)weight.Query;
                this.MinNrShouldMatch = bq.MinimumNumberShouldMatch;
                this.Sims = new SimScorer[(int)Dv.ValueCount];
                foreach (BooleanClause clause in bq.Clauses)
                {
                    Debug.Assert(!clause.Prohibited);
                    Debug.Assert(!clause.Required);
                    Term term = ((TermQuery)clause.Query).Term;
                    long ord = Dv.LookupTerm(term.Bytes);
                    if (ord >= 0)
                    {
                        bool success = Ords.Add(ord);
                        Debug.Assert(success); // no dups
                        TermContext context = TermContext.Build(reader.Context, term);
                        SimWeight w = weight.Similarity.ComputeWeight(1f, searcher.CollectionStatistics("field"), searcher.TermStatistics(term, context));
                        var dummy = w.ValueForNormalization; // ignored
                        w.Normalize(1F, 1F);
                        Sims[(int)ord] = weight.Similarity.DoSimScorer(w, (AtomicReaderContext)reader.Context);
                    }
                }
            }

            public override float Score()
            {
                Debug.Assert(Score_Renamed != 0, CurrentMatched.ToString());
                return (float)Score_Renamed * ((BooleanWeight)weight).Coord(CurrentMatched, ((BooleanWeight)weight).MaxCoord);
            }

            public override int Freq()
            {
                return CurrentMatched;
            }

            public override int DocID()
            {
                return CurrentDoc;
            }

            public override int NextDoc()
            {
                Debug.Assert(CurrentDoc != NO_MORE_DOCS);
                for (CurrentDoc = CurrentDoc + 1; CurrentDoc < MaxDoc; CurrentDoc++)
                {
                    CurrentMatched = 0;
                    Score_Renamed = 0;
                    Dv.Document = CurrentDoc;
                    long ord;
                    while ((ord = Dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        if (Ords.Contains(ord))
                        {
                            CurrentMatched++;
                            Score_Renamed += Sims[(int)ord].Score(CurrentDoc, 1);
                        }
                    }
                    if (CurrentMatched >= MinNrShouldMatch)
                    {
                        return CurrentDoc;
                    }
                }
                return CurrentDoc = NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                int doc;
                while ((doc = NextDoc()) < target)
                {
                }
                return doc;
            }

            public override long Cost()
            {
                return MaxDoc;
            }
        }
    }
}