using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MultiReader = Lucene.Net.Index.MultiReader;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    [TestFixture]
    public class TestMultiTermQueryRewrites : LuceneTestCase
    {
        private static Directory dir, sdir1, sdir2;
        private static IndexReader reader, multiReader, multiReaderDupls;
        private static IndexSearcher searcher, multiSearcher, multiSearcherDupls;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because Similarity and TimeZone are not static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            dir = NewDirectory();
            sdir1 = NewDirectory();
            sdir2 = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, new MockAnalyzer(Random));
            RandomIndexWriter swriter1 = new RandomIndexWriter(Random, sdir1, new MockAnalyzer(Random));
            RandomIndexWriter swriter2 = new RandomIndexWriter(Random, sdir2, new MockAnalyzer(Random));

            for (int i = 0; i < 10; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("data", Convert.ToString(i), Field.Store.NO));
                writer.AddDocument(doc);
                ((i % 2 == 0) ? swriter1 : swriter2).AddDocument(doc);
            }
            writer.ForceMerge(1);
            swriter1.ForceMerge(1);
            swriter2.ForceMerge(1);
            writer.Dispose();
            swriter1.Dispose();
            swriter2.Dispose();

            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);

            multiReader = new MultiReader(new IndexReader[] { DirectoryReader.Open(sdir1), DirectoryReader.Open(sdir2) }, true);
            multiSearcher = NewSearcher(multiReader);

            multiReaderDupls = new MultiReader(new IndexReader[] { DirectoryReader.Open(sdir1), DirectoryReader.Open(dir) }, true);
            multiSearcherDupls = NewSearcher(multiReaderDupls);
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            reader.Dispose();
            multiReader.Dispose();
            multiReaderDupls.Dispose();
            dir.Dispose();
            sdir1.Dispose();
            sdir2.Dispose();
            reader = multiReader = multiReaderDupls = null;
            searcher = multiSearcher = multiSearcherDupls = null;
            dir = sdir1 = sdir2 = null;
            base.AfterClass();
        }

        private Query ExtractInnerQuery(Query q)
        {
            if (q is ConstantScoreQuery)
            {
                // wrapped as ConstantScoreQuery
                q = ((ConstantScoreQuery)q).Query;
            }
            return q;
        }

        private Term ExtractTerm(Query q)
        {
            q = ExtractInnerQuery(q);
            return ((TermQuery)q).Term;
        }

        private void CheckBooleanQueryOrder(Query q)
        {
            q = ExtractInnerQuery(q);
            BooleanQuery bq = (BooleanQuery)q;
            Term last = null, act;
            foreach (BooleanClause clause in bq.Clauses)
            {
                act = ExtractTerm(clause.Query);
                if (last != null)
                {
                    Assert.IsTrue(last.CompareTo(act) < 0, "sort order of terms in BQ violated");
                }
                last = act;
            }
        }

        private void CheckDuplicateTerms(MultiTermQuery.RewriteMethod method)
        {
            MultiTermQuery mtq = TermRangeQuery.NewStringRange("data", "2", "7", true, true);
            mtq.MultiTermRewriteMethod = (method);
            Query q1 = searcher.Rewrite(mtq);
            Query q2 = multiSearcher.Rewrite(mtq);
            Query q3 = multiSearcherDupls.Rewrite(mtq);
            if (Verbose)
            {
                Console.WriteLine();
                Console.WriteLine("single segment: " + q1);
                Console.WriteLine("multi segment: " + q2);
                Console.WriteLine("multi segment with duplicates: " + q3);
            }
            Assert.IsTrue(q1.Equals(q2), "The multi-segment case must produce same rewritten query");
            Assert.IsTrue(q1.Equals(q3), "The multi-segment case with duplicates must produce same rewritten query");
            CheckBooleanQueryOrder(q1);
            CheckBooleanQueryOrder(q2);
            CheckBooleanQueryOrder(q3);
        }

        [Test]
        public virtual void TestRewritesWithDuplicateTerms()
        {
            CheckDuplicateTerms(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);

            CheckDuplicateTerms(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);

            // use a large PQ here to only test duplicate terms and dont mix up when all scores are equal
            CheckDuplicateTerms(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(1024));
            CheckDuplicateTerms(new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(1024));

            // Test auto rewrite (but only boolean mode), so we set the limits to large values to always get a BQ
            ConstantScoreAutoRewrite rewrite = new ConstantScoreAutoRewrite();
            rewrite.TermCountCutoff = int.MaxValue;
            rewrite.DocCountPercent = 100.0;
            CheckDuplicateTerms(rewrite);
        }

        private void CheckBooleanQueryBoosts(BooleanQuery bq)
        {
            foreach (BooleanClause clause in bq.Clauses)
            {
                TermQuery mtq = (TermQuery)clause.Query;
                Assert.AreEqual(Convert.ToSingle(mtq.Term.Text), mtq.Boost, 0, "Parallel sorting of boosts in rewrite mode broken");
            }
        }

        private void CheckBoosts(MultiTermQuery.RewriteMethod method)
        {
            MultiTermQuery mtq = new MultiTermQueryAnonymousClass(this);
            mtq.MultiTermRewriteMethod = (method);
            Query q1 = searcher.Rewrite(mtq);
            Query q2 = multiSearcher.Rewrite(mtq);
            Query q3 = multiSearcherDupls.Rewrite(mtq);
            if (Verbose)
            {
                Console.WriteLine();
                Console.WriteLine("single segment: " + q1);
                Console.WriteLine("multi segment: " + q2);
                Console.WriteLine("multi segment with duplicates: " + q3);
            }
            Assert.IsTrue(q1.Equals(q2), "The multi-segment case must produce same rewritten query");
            Assert.IsTrue(q1.Equals(q3), "The multi-segment case with duplicates must produce same rewritten query");
            CheckBooleanQueryBoosts((BooleanQuery)q1);
            CheckBooleanQueryBoosts((BooleanQuery)q2);
            CheckBooleanQueryBoosts((BooleanQuery)q3);
        }

        private sealed class MultiTermQueryAnonymousClass : MultiTermQuery
        {
            private readonly TestMultiTermQueryRewrites outerInstance;

            public MultiTermQueryAnonymousClass(TestMultiTermQueryRewrites outerInstance)
                : base("data")
            {
                this.outerInstance = outerInstance;
            }

            protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
            {
                return new TermRangeTermsEnumAnonymousClass(this, terms.GetEnumerator(), new BytesRef("2"), new BytesRef("7"));
            }

            private sealed class TermRangeTermsEnumAnonymousClass : TermRangeTermsEnum
            {
                private readonly MultiTermQueryAnonymousClass outerInstance;

                public TermRangeTermsEnumAnonymousClass(MultiTermQueryAnonymousClass outerInstance, TermsEnum iterator, BytesRef bref1, BytesRef bref2)
                    : base(iterator, bref1, bref2, true, true)
                {
                    this.outerInstance = outerInstance;
                    boostAtt = Attributes.AddAttribute<IBoostAttribute>();
                }

                internal readonly IBoostAttribute boostAtt;

                protected override AcceptStatus Accept(BytesRef term)
                {
                    boostAtt.Boost = Convert.ToSingle(term.Utf8ToString());
                    return base.Accept(term);
                }
            }

            public override string ToString(string field)
            {
                return "dummy";
            }
        }

        [Test]
        public virtual void TestBoosts()
        {
            CheckBoosts(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);

            // use a large PQ here to only test boosts and dont mix up when all scores are equal
            CheckBoosts(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(1024));
        }

        private void CheckMaxClauseLimitation(MultiTermQuery.RewriteMethod method, [CallerMemberName] string memberName = "")
        {
            int savedMaxClauseCount = BooleanQuery.MaxClauseCount;
            BooleanQuery.MaxClauseCount = 3;

            MultiTermQuery mtq = TermRangeQuery.NewStringRange("data", "2", "7", true, true);
            mtq.MultiTermRewriteMethod = (method);
            try
            {
                multiSearcherDupls.Rewrite(mtq);
                Assert.Fail("Should throw BooleanQuery.TooManyClauses");
            }
            catch (BooleanQuery.TooManyClausesException e)
            {
                //  Maybe remove this assert in later versions, when internal API changes:
                Assert.AreEqual("CheckMaxClauseCount", new StackTrace(e, false).GetFrames()[0].GetMethod().Name); //, "Should throw BooleanQuery.TooManyClauses with a stacktrace containing checkMaxClauseCount()");
            }
            finally
            {
                BooleanQuery.MaxClauseCount = savedMaxClauseCount;
            }
        }

        private void CheckNoMaxClauseLimitation(MultiTermQuery.RewriteMethod method)
        {
            int savedMaxClauseCount = BooleanQuery.MaxClauseCount;
            BooleanQuery.MaxClauseCount = 3;

            MultiTermQuery mtq = TermRangeQuery.NewStringRange("data", "2", "7", true, true);
            mtq.MultiTermRewriteMethod = (method);
            try
            {
                multiSearcherDupls.Rewrite(mtq);
            }
            finally
            {
                BooleanQuery.MaxClauseCount = savedMaxClauseCount;
            }
        }

        [Test]
        public virtual void TestMaxClauseLimitations()
        {
            CheckMaxClauseLimitation(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
            CheckMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);

            CheckNoMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
            CheckNoMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
            CheckNoMaxClauseLimitation(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(1024));
            CheckNoMaxClauseLimitation(new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(1024));
        }
    }
}