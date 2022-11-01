﻿using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using OpenMode = Lucene.Net.Index.OpenMode;

    /// <summary>
    /// Tests <seealso cref="PhraseQuery"/>.
    /// </summary>
    /// <seealso cref= TestPositionIncrement </seealso>
    /*
     * Remove ThreadLeaks and run with (Eclipse or command line):
     * -ea -Drt.seed=AFD1E7E84B35D2B1
     * to get leaked thread errors.
     */

    [TestFixture]
    public class TestPhraseQuery : LuceneTestCase
    {
        /// <summary>
        /// threshold for comparing floats </summary>
        public const float SCORE_COMP_THRESH = 1e-6f;

        private static IndexSearcher searcher;
        private static IndexReader reader;
        private PhraseQuery query;
        private static Directory directory;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            directory = NewDirectory();
            Analyzer analyzer = new AnalyzerAnonymousClass();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, analyzer);

            Documents.Document doc = new Documents.Document();
            doc.Add(NewTextField("field", "one two three four five", Field.Store.YES));
            doc.Add(NewTextField("repeated", "this is a repeated field - first part", Field.Store.YES));
            IIndexableField repeatedField = NewTextField("repeated", "second part of a repeated field", Field.Store.YES);
            doc.Add(repeatedField);
            doc.Add(NewTextField("palindrome", "one two three two one", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Documents.Document();
            doc.Add(NewTextField("nonexist", "phrase exist notexist exist found", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Documents.Document();
            doc.Add(NewTextField("nonexist", "phrase exist notexist exist found", Field.Store.YES));
            writer.AddDocument(doc);

            reader = writer.GetReader();
            writer.Dispose();

            searcher = NewSearcher(reader);
        }

        private sealed class AnalyzerAnonymousClass : Analyzer
        {
            public AnalyzerAnonymousClass()
            {
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, false));
            }

            public override int GetPositionIncrementGap(string fieldName)
            {
                return 100;
            }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            query = new PhraseQuery();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            searcher = null;
            reader.Dispose();
            reader = null;
            directory.Dispose();
            directory = null;

            base.AfterClass();
        }

        [Test]
        public virtual void TestNotCloseEnough()
        {
            query.Slop = 2;
            query.Add(new Term("field", "one"));
            query.Add(new Term("field", "five"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);
            QueryUtils.Check(Random, query, searcher);
        }

        [Test]
        public virtual void TestBarelyCloseEnough()
        {
            query.Slop = 3;
            query.Add(new Term("field", "one"));
            query.Add(new Term("field", "five"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            QueryUtils.Check(Random, query, searcher);
        }

        /// <summary>
        /// Ensures slop of 0 works for exact matches, but not reversed
        /// </summary>
        [Test]
        public virtual void TestExact()
        {
            // slop is zero by default
            query.Add(new Term("field", "four"));
            query.Add(new Term("field", "five"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "exact match");
            QueryUtils.Check(Random, query, searcher);

            query = new PhraseQuery();
            query.Add(new Term("field", "two"));
            query.Add(new Term("field", "one"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "reverse not exact");
            QueryUtils.Check(Random, query, searcher);
        }

        [Test]
        public virtual void TestSlop1()
        {
            // Ensures slop of 1 works with terms in order.
            query.Slop = 1;
            query.Add(new Term("field", "one"));
            query.Add(new Term("field", "two"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "in order");
            QueryUtils.Check(Random, query, searcher);

            // Ensures slop of 1 does not work for phrases out of order;
            // must be at least 2.
            query = new PhraseQuery();
            query.Slop = 1;
            query.Add(new Term("field", "two"));
            query.Add(new Term("field", "one"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "reversed, slop not 2 or more");
            QueryUtils.Check(Random, query, searcher);
        }

        /// <summary>
        /// As long as slop is at least 2, terms can be reversed
        /// </summary>
        [Test]
        public virtual void TestOrderDoesntMatter()
        {
            query.Slop = 2; // must be at least two for reverse order match
            query.Add(new Term("field", "two"));
            query.Add(new Term("field", "one"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "just sloppy enough");
            QueryUtils.Check(Random, query, searcher);

            query = new PhraseQuery();
            query.Slop = 2;
            query.Add(new Term("field", "three"));
            query.Add(new Term("field", "one"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "not sloppy enough");
            QueryUtils.Check(Random, query, searcher);
        }

        /// <summary>
        /// slop is the total number of positional moves allowed
        /// to line up a phrase
        /// </summary>
        [Test]
        public virtual void TestMulipleTerms()
        {
            query.Slop = 2;
            query.Add(new Term("field", "one"));
            query.Add(new Term("field", "three"));
            query.Add(new Term("field", "five"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "two total moves");
            QueryUtils.Check(Random, query, searcher);

            query = new PhraseQuery();
            query.Slop = 5; // it takes six moves to match this phrase
            query.Add(new Term("field", "five"));
            query.Add(new Term("field", "three"));
            query.Add(new Term("field", "one"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "slop of 5 not close enough");
            QueryUtils.Check(Random, query, searcher);

            query.Slop = 6;
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "slop of 6 just right");
            QueryUtils.Check(Random, query, searcher);
        }

        [Test]
        public virtual void TestPhraseQueryWithStopAnalyzer()
        {
            Directory directory = NewDirectory();
            Analyzer stopAnalyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, stopAnalyzer));
            Documents.Document doc = new Documents.Document();
            doc.Add(NewTextField("field", "the stop words are here", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            // valid exact phrase query
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("field", "stop"));
            query.Add(new Term("field", "words"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            QueryUtils.Check(Random, query, searcher);

            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestPhraseQueryInConjunctionScorer()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);

            Documents.Document doc = new Documents.Document();
            doc.Add(NewTextField("source", "marketing info", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Documents.Document();
            doc.Add(NewTextField("contents", "foobar", Field.Store.YES));
            doc.Add(NewTextField("source", "marketing info", Field.Store.YES));
            writer.AddDocument(doc);

            IndexReader reader = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term("source", "marketing"));
            phraseQuery.Add(new Term("source", "info"));
            ScoreDoc[] hits = searcher.Search(phraseQuery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random, phraseQuery, searcher);

            TermQuery termQuery = new TermQuery(new Term("contents", "foobar"));
            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Add(termQuery, Occur.MUST);
            booleanQuery.Add(phraseQuery, Occur.MUST);
            hits = searcher.Search(booleanQuery, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            QueryUtils.Check(Random, termQuery, searcher);

            reader.Dispose();

            writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE));
            doc = new Documents.Document();
            doc.Add(NewTextField("contents", "map entry woo", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Documents.Document();
            doc.Add(NewTextField("contents", "woo map entry", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Documents.Document();
            doc.Add(NewTextField("contents", "map foobarword entry woo", Field.Store.YES));
            writer.AddDocument(doc);

            reader = writer.GetReader();
            writer.Dispose();

            searcher = NewSearcher(reader);

            termQuery = new TermQuery(new Term("contents", "woo"));
            phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term("contents", "map"));
            phraseQuery.Add(new Term("contents", "entry"));

            hits = searcher.Search(termQuery, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            hits = searcher.Search(phraseQuery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);

            booleanQuery = new BooleanQuery();
            booleanQuery.Add(termQuery, Occur.MUST);
            booleanQuery.Add(phraseQuery, Occur.MUST);
            hits = searcher.Search(booleanQuery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);

            booleanQuery = new BooleanQuery();
            booleanQuery.Add(phraseQuery, Occur.MUST);
            booleanQuery.Add(termQuery, Occur.MUST);
            hits = searcher.Search(booleanQuery, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            QueryUtils.Check(Random, booleanQuery, searcher);

            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestSlopScoring()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()).SetSimilarity(new DefaultSimilarity()));

            Documents.Document doc = new Documents.Document();
            doc.Add(NewTextField("field", "foo firstname lastname foo", Field.Store.YES));
            writer.AddDocument(doc);

            Documents.Document doc2 = new Documents.Document();
            doc2.Add(NewTextField("field", "foo firstname zzz lastname foo", Field.Store.YES));
            writer.AddDocument(doc2);

            Documents.Document doc3 = new Documents.Document();
            doc3.Add(NewTextField("field", "foo firstname zzz yyy lastname foo", Field.Store.YES));
            writer.AddDocument(doc3);

            IndexReader reader = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);
            searcher.Similarity = new DefaultSimilarity();
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("field", "firstname"));
            query.Add(new Term("field", "lastname"));
            query.Slop = int.MaxValue;
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            // Make sure that those matches where the terms appear closer to
            // each other get a higher score:
            Assert.AreEqual(0.71, hits[0].Score, 0.01);
            Assert.AreEqual(0, hits[0].Doc);
            Assert.AreEqual(0.44, hits[1].Score, 0.01);
            Assert.AreEqual(1, hits[1].Doc);
            Assert.AreEqual(0.31, hits[2].Score, 0.01);
            Assert.AreEqual(2, hits[2].Doc);
            QueryUtils.Check(Random, query, searcher);
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestToString()
        {
            PhraseQuery q = new PhraseQuery(); // Query "this hi this is a test is"
            q.Add(new Term("field", "hi"), 1);
            q.Add(new Term("field", "test"), 5);

            Assert.AreEqual(q.ToString(), "field:\"? hi ? ? ? test\"");
            q.Add(new Term("field", "hello"), 1);
            Assert.AreEqual(q.ToString(), "field:\"? hi|hello ? ? ? test\"");
        }

        [Test]
        public virtual void TestWrappedPhrase()
        {
            query.Add(new Term("repeated", "first"));
            query.Add(new Term("repeated", "part"));
            query.Add(new Term("repeated", "second"));
            query.Add(new Term("repeated", "part"));
            query.Slop = 100;

            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "slop of 100 just right");
            QueryUtils.Check(Random, query, searcher);

            query.Slop = 99;

            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "slop of 99 not enough");
            QueryUtils.Check(Random, query, searcher);
        }

        // work on two docs like this: "phrase exist notexist exist found"
        [Test]
        public virtual void TestNonExistingPhrase()
        {
            // phrase without repetitions that exists in 2 docs
            query.Add(new Term("nonexist", "phrase"));
            query.Add(new Term("nonexist", "notexist"));
            query.Add(new Term("nonexist", "found"));
            query.Slop = 2; // would be found this way

            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length, "phrase without repetitions exists in 2 docs");
            QueryUtils.Check(Random, query, searcher);

            // phrase with repetitions that exists in 2 docs
            query = new PhraseQuery();
            query.Add(new Term("nonexist", "phrase"));
            query.Add(new Term("nonexist", "exist"));
            query.Add(new Term("nonexist", "exist"));
            query.Slop = 1; // would be found

            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length, "phrase with repetitions exists in two docs");
            QueryUtils.Check(Random, query, searcher);

            // phrase I with repetitions that does not exist in any doc
            query = new PhraseQuery();
            query.Add(new Term("nonexist", "phrase"));
            query.Add(new Term("nonexist", "notexist"));
            query.Add(new Term("nonexist", "phrase"));
            query.Slop = 1000; // would not be found no matter how high the slop is

            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "nonexisting phrase with repetitions does not exist in any doc");
            QueryUtils.Check(Random, query, searcher);

            // phrase II with repetitions that does not exist in any doc
            query = new PhraseQuery();
            query.Add(new Term("nonexist", "phrase"));
            query.Add(new Term("nonexist", "exist"));
            query.Add(new Term("nonexist", "exist"));
            query.Add(new Term("nonexist", "exist"));
            query.Slop = 1000; // would not be found no matter how high the slop is

            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "nonexisting phrase with repetitions does not exist in any doc");
            QueryUtils.Check(Random, query, searcher);
        }

        /// <summary>
        /// Working on a 2 fields like this:
        ///    Field("field", "one two three four five")
        ///    Field("palindrome", "one two three two one")
        /// Phrase of size 2 occuriong twice, once in order and once in reverse,
        /// because doc is a palyndrome, is counted twice.
        /// Also, in this case order in query does not matter.
        /// Also, when an exact match is found, both sloppy scorer and exact scorer scores the same.
        /// </summary>
        [Test]
        public virtual void TestPalyndrome2()
        {
            // search on non palyndrome, find phrase with no slop, using exact phrase scorer
            query.Slop = 0; // to use exact phrase scorer
            query.Add(new Term("field", "two"));
            query.Add(new Term("field", "three"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "phrase found with exact phrase scorer");
            float score0 = hits[0].Score;
            //System.out.println("(exact) field: two three: "+score0);
            QueryUtils.Check(Random, query, searcher);

            // search on non palyndrome, find phrase with slop 2, though no slop required here.
            query.Slop = 2; // to use sloppy scorer
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "just sloppy enough");
            float score1 = hits[0].Score;
            //System.out.println("(sloppy) field: two three: "+score1);
            Assert.AreEqual(score0, score1, SCORE_COMP_THRESH, "exact scorer and sloppy scorer score the same when slop does not matter");
            QueryUtils.Check(Random, query, searcher);

            // search ordered in palyndrome, find it twice
            query = new PhraseQuery();
            query.Slop = 2; // must be at least two for both ordered and reversed to match
            query.Add(new Term("palindrome", "two"));
            query.Add(new Term("palindrome", "three"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "just sloppy enough");
            //float score2 = hits[0].Score;
            //System.out.println("palindrome: two three: "+score2);
            QueryUtils.Check(Random, query, searcher);

            //commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq().
            //Assert.IsTrue("ordered scores higher in palindrome",score1+SCORE_COMP_THRESH<score2);

            // search reveresed in palyndrome, find it twice
            query = new PhraseQuery();
            query.Slop = 2; // must be at least two for both ordered and reversed to match
            query.Add(new Term("palindrome", "three"));
            query.Add(new Term("palindrome", "two"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "just sloppy enough");
            //float score3 = hits[0].Score;
            //System.out.println("palindrome: three two: "+score3);
            QueryUtils.Check(Random, query, searcher);

            //commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq().
            //Assert.IsTrue("reversed scores higher in palindrome",score1+SCORE_COMP_THRESH<score3);
            //Assert.AreEqual("ordered or reversed does not matter",score2, score3, SCORE_COMP_THRESH);
        }

        /// <summary>
        /// Working on a 2 fields like this:
        ///    Field("field", "one two three four five")
        ///    Field("palindrome", "one two three two one")
        /// Phrase of size 3 occuriong twice, once in order and once in reverse,
        /// because doc is a palyndrome, is counted twice.
        /// Also, in this case order in query does not matter.
        /// Also, when an exact match is found, both sloppy scorer and exact scorer scores the same.
        /// </summary>
        [Test]
        public virtual void TestPalyndrome3()
        {
            // search on non palyndrome, find phrase with no slop, using exact phrase scorer
            query.Slop = 0; // to use exact phrase scorer
            query.Add(new Term("field", "one"));
            query.Add(new Term("field", "two"));
            query.Add(new Term("field", "three"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "phrase found with exact phrase scorer");
            float score0 = hits[0].Score;
            //System.out.println("(exact) field: one two three: "+score0);
            QueryUtils.Check(Random, query, searcher);

            // just make sure no exc:
            searcher.Explain(query, 0);

            // search on non palyndrome, find phrase with slop 3, though no slop required here.
            query.Slop = 4; // to use sloppy scorer
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "just sloppy enough");
            float score1 = hits[0].Score;
            //System.out.println("(sloppy) field: one two three: "+score1);
            Assert.AreEqual(score0, score1, SCORE_COMP_THRESH, "exact scorer and sloppy scorer score the same when slop does not matter");
            QueryUtils.Check(Random, query, searcher);

            // search ordered in palyndrome, find it twice
            query = new PhraseQuery();
            query.Slop = 4; // must be at least four for both ordered and reversed to match
            query.Add(new Term("palindrome", "one"));
            query.Add(new Term("palindrome", "two"));
            query.Add(new Term("palindrome", "three"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;

            // just make sure no exc:
            searcher.Explain(query, 0);

            Assert.AreEqual(1, hits.Length, "just sloppy enough");
            //float score2 = hits[0].Score;
            //System.out.println("palindrome: one two three: "+score2);
            QueryUtils.Check(Random, query, searcher);

            //commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq().
            //Assert.IsTrue("ordered scores higher in palindrome",score1+SCORE_COMP_THRESH<score2);

            // search reveresed in palyndrome, find it twice
            query = new PhraseQuery();
            query.Slop = 4; // must be at least four for both ordered and reversed to match
            query.Add(new Term("palindrome", "three"));
            query.Add(new Term("palindrome", "two"));
            query.Add(new Term("palindrome", "one"));
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "just sloppy enough");
            //float score3 = hits[0].Score;
            //System.out.println("palindrome: three two one: "+score3);
            QueryUtils.Check(Random, query, searcher);

            //commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq().
            //Assert.IsTrue("reversed scores higher in palindrome",score1+SCORE_COMP_THRESH<score3);
            //Assert.AreEqual("ordered or reversed does not matter",score2, score3, SCORE_COMP_THRESH);
        }

        // LUCENE-1280
        [Test]
        public virtual void TestEmptyPhraseQuery()
        {
            BooleanQuery q2 = new BooleanQuery();
            q2.Add(new PhraseQuery(), Occur.MUST);
            q2.ToString();
        }

        /* test that a single term is rewritten to a term query */

        [Test]
        public virtual void TestRewrite()
        {
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("foo", "bar"));
            Query rewritten = pq.Rewrite(searcher.IndexReader);
            Assert.IsTrue(rewritten is TermQuery);
        }

        [Test]
        public virtual void TestRandomPhrases()
        {
            Directory dir = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);

            RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMergePolicy(NewLogMergePolicy()));
            IList<IList<string>> docs = new JCG.List<IList<string>>();
            Documents.Document d = new Documents.Document();
            Field f = NewTextField("f", "", Field.Store.NO);
            d.Add(f);

            Random r = Random;

            int NUM_DOCS = AtLeast(10);
            for (int i = 0; i < NUM_DOCS; i++)
            {
                // must be > 4096 so it spans multiple chunks
                int termCount = TestUtil.NextInt32(Random, 4097, 8200);

                IList<string> doc = new JCG.List<string>();

                StringBuilder sb = new StringBuilder();
                while (doc.Count < termCount)
                {
                    if (r.Next(5) == 1 || docs.Count == 0)
                    {
                        // make new non-empty-string term
                        string term;
                        while (true)
                        {
                            term = TestUtil.RandomUnicodeString(r);
                            if (term.Length > 0)
                            {
                                break;
                            }
                        }
                        Exception priorException = null; // LUCENENET: No need to cast to IOExcpetion
                        TokenStream ts = analyzer.GetTokenStream("ignore", new StringReader(term));
                        try
                        {
                            ICharTermAttribute termAttr = ts.AddAttribute<ICharTermAttribute>();
                            ts.Reset();
                            while (ts.IncrementToken())
                            {
                                string text = termAttr.ToString();
                                doc.Add(text);
                                sb.Append(text).Append(' ');
                            }
                            ts.End();
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                            priorException = e;
                        }
                        finally
                        {
                            IOUtils.DisposeWhileHandlingException(priorException, ts);
                        }
                    }
                    else
                    {
                        // pick existing sub-phrase
                        IList<string> lastDoc = docs[r.Next(docs.Count)];
                        int len = TestUtil.NextInt32(r, 1, 10);
                        int start = r.Next(lastDoc.Count - len);
                        for (int k = start; k < start + len; k++)
                        {
                            string t = lastDoc[k];
                            doc.Add(t);
                            sb.Append(t).Append(' ');
                        }
                    }
                }
                docs.Add(doc);
                f.SetStringValue(sb.ToString());
                w.AddDocument(d);
            }

            IndexReader reader = w.GetReader();
            IndexSearcher s = NewSearcher(reader);
            w.Dispose();

            // now search
            int num = AtLeast(10);
            for (int i = 0; i < num; i++)
            {
                int docID = r.Next(docs.Count);
                IList<string> doc = docs[docID];

                int numTerm = TestUtil.NextInt32(r, 2, 20);
                int start = r.Next(doc.Count - numTerm);
                PhraseQuery pq = new PhraseQuery();
                StringBuilder sb = new StringBuilder();
                for (int t = start; t < start + numTerm; t++)
                {
                    pq.Add(new Term("f", doc[t]));
                    sb.Append(doc[t]).Append(' ');
                }

                TopDocs hits = s.Search(pq, NUM_DOCS);
                bool found = false;
                for (int j = 0; j < hits.ScoreDocs.Length; j++)
                {
                    if (hits.ScoreDocs[j].Doc == docID)
                    {
                        found = true;
                        break;
                    }
                }

                Assert.IsTrue(found, "phrase '" + sb + "' not found; start=" + start);
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNegativeSlop()
        {
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("field", "two"));
            query.Add(new Term("field", "one"));
            try
            {
                query.Slop = -2;
                Assert.Fail("didn't get expected exception");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // expected exception
            }
        }
    }
}