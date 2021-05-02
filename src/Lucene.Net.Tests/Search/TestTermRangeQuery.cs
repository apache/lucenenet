using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System.IO;
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
    
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using Terms = Lucene.Net.Index.Terms;

    [TestFixture]
    public class TestTermRangeQuery : LuceneTestCase
    {
        private int docCount = 0;
        private Directory dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
        }

        [TearDown]
        public override void TearDown()
        {
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestExclusive()
        {
            Query query = TermRangeQuery.NewStringRange("content", "A", "C", false, false);
            InitializeIndex(new string[] { "A", "B", "C", "D" });
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "A,B,C,D, only B in range");
            reader.Dispose();

            InitializeIndex(new string[] { "A", "B", "D" });
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "A,B,D, only B in range");
            reader.Dispose();

            AddDoc("C");
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "C added, still only B in range");
            reader.Dispose();
        }

        [Test]
        public virtual void TestInclusive()
        {
            Query query = TermRangeQuery.NewStringRange("content", "A", "C", true, true);

            InitializeIndex(new string[] { "A", "B", "C", "D" });
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length, "A,B,C,D - A,B,C in range");
            reader.Dispose();

            InitializeIndex(new string[] { "A", "B", "D" });
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length, "A,B,D - A and B in range");
            reader.Dispose();

            AddDoc("C");
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length, "C added - A, B, C in range");
            reader.Dispose();
        }

        [Test]
        public virtual void TestAllDocs()
        {
            InitializeIndex(new string[] { "A", "B", "C", "D" });
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            TermRangeQuery query = new TermRangeQuery("content", null, null, true, true);
            Terms terms = MultiFields.GetTerms(searcher.IndexReader, "content");
            Assert.IsFalse(query.GetTermsEnum(terms) is TermRangeTermsEnum);
            Assert.AreEqual(4, searcher.Search(query, null, 1000).ScoreDocs.Length);
            query = new TermRangeQuery("content", null, null, false, false);
            Assert.IsFalse(query.GetTermsEnum(terms) is TermRangeTermsEnum);
            Assert.AreEqual(4, searcher.Search(query, null, 1000).ScoreDocs.Length);
            query = TermRangeQuery.NewStringRange("content", "", null, true, false);
            Assert.IsFalse(query.GetTermsEnum(terms) is TermRangeTermsEnum);
            Assert.AreEqual(4, searcher.Search(query, null, 1000).ScoreDocs.Length);
            // and now anothe one
            query = TermRangeQuery.NewStringRange("content", "B", null, true, false);
            Assert.IsTrue(query.GetTermsEnum(terms) is TermRangeTermsEnum);
            Assert.AreEqual(3, searcher.Search(query, null, 1000).ScoreDocs.Length);
            reader.Dispose();
        }

        /// <summary>
        /// this test should not be here, but it tests the fuzzy query rewrite mode (TOP_TERMS_SCORING_BOOLEAN_REWRITE)
        /// with constant score and checks, that only the lower end of terms is put into the range
        /// </summary>
        [Test]
        public virtual void TestTopTermsRewrite()
        {
            InitializeIndex(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" });

            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            TermRangeQuery query = TermRangeQuery.NewStringRange("content", "B", "J", true, true);
            CheckBooleanTerms(searcher, query, "B", "C", "D", "E", "F", "G", "H", "I", "J");

            int savedClauseCount = BooleanQuery.MaxClauseCount;
            try
            {
                BooleanQuery.MaxClauseCount = 3;
                CheckBooleanTerms(searcher, query, "B", "C", "D");
            }
            finally
            {
                BooleanQuery.MaxClauseCount = savedClauseCount;
            }
            reader.Dispose();
        }

        private void CheckBooleanTerms(IndexSearcher searcher, TermRangeQuery query, params string[] terms)
        {
            query.MultiTermRewriteMethod = new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(50);
            BooleanQuery bq = (BooleanQuery)searcher.Rewrite(query);
            var allowedTerms = AsSet(terms);
            Assert.AreEqual(allowedTerms.Count, bq.Clauses.Count);
            foreach (BooleanClause c in bq.Clauses)
            {
                Assert.IsTrue(c.Query is TermQuery);
                TermQuery tq = (TermQuery)c.Query;
                string term = tq.Term.Text;
                Assert.IsTrue(allowedTerms.Contains(term), "invalid term: " + term);
                allowedTerms.Remove(term); // remove to fail on double terms
            }
            Assert.AreEqual(0, allowedTerms.Count);
        }

        [Test]
        public virtual void TestEqualsHashcode()
        {
            Query query = TermRangeQuery.NewStringRange("content", "A", "C", true, true);

            query.Boost = 1.0f;
            Query other = TermRangeQuery.NewStringRange("content", "A", "C", true, true);
            other.Boost = 1.0f;

            Assert.AreEqual(query, query, "query equals itself is true");
            Assert.AreEqual(query, other, "equivalent queries are equal");
            Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode must return same value when equals is true");

            other.Boost = 2.0f;
            Assert.IsFalse(query.Equals(other), "Different boost queries are not equal");

            other = TermRangeQuery.NewStringRange("notcontent", "A", "C", true, true);
            Assert.IsFalse(query.Equals(other), "Different fields are not equal");

            other = TermRangeQuery.NewStringRange("content", "X", "C", true, true);
            Assert.IsFalse(query.Equals(other), "Different lower terms are not equal");

            other = TermRangeQuery.NewStringRange("content", "A", "Z", true, true);
            Assert.IsFalse(query.Equals(other), "Different upper terms are not equal");

            query = TermRangeQuery.NewStringRange("content", null, "C", true, true);
            other = TermRangeQuery.NewStringRange("content", null, "C", true, true);
            Assert.AreEqual(query, other, "equivalent queries with null lowerterms are equal()");
            Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode must return same value when equals is true");

            query = TermRangeQuery.NewStringRange("content", "C", null, true, true);
            other = TermRangeQuery.NewStringRange("content", "C", null, true, true);
            Assert.AreEqual(query, other, "equivalent queries with null upperterms are equal()");
            Assert.AreEqual(query.GetHashCode(), other.GetHashCode(), "hashcode returns same value");

            query = TermRangeQuery.NewStringRange("content", null, "C", true, true);
            other = TermRangeQuery.NewStringRange("content", "C", null, true, true);
            Assert.IsFalse(query.Equals(other), "queries with different upper and lower terms are not equal");

            query = TermRangeQuery.NewStringRange("content", "A", "C", false, false);
            other = TermRangeQuery.NewStringRange("content", "A", "C", true, true);
            Assert.IsFalse(query.Equals(other), "queries with different inclusive are not equal");
        }

        private class SingleCharAnalyzer : Analyzer
        {
            private class SingleCharTokenizer : Tokenizer
            {
                private readonly char[] buffer = new char[1];
                private bool done = false;
                private readonly ICharTermAttribute termAtt;

                public SingleCharTokenizer(TextReader r)
                    : base(r)
                {
                    termAtt = AddAttribute<ICharTermAttribute>();
                }

                public override sealed bool IncrementToken()
                {
                    if (done)
                    {
                        return false;
                    }
                    else
                    {
                        int count = m_input.Read(buffer, 0, buffer.Length);
                        ClearAttributes();
                        done = true;
                        if (count == 1)
                        {
                            termAtt.CopyBuffer(buffer, 0, 1);
                        }
                        return true;
                    }
                }

                public override void Reset()
                {
                    base.Reset();
                    done = false;
                }
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new SingleCharTokenizer(reader));
            }
        }

        private void InitializeIndex(string[] values)
        {
            InitializeIndex(values, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));
        }

        private void InitializeIndex(string[] values, Analyzer analyzer)
        {
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetOpenMode(OpenMode.CREATE));
            for (int i = 0; i < values.Length; i++)
            {
                InsertDoc(writer, values[i]);
            }
            writer.Dispose();
        }

        // shouldnt create an analyzer for every doc?
        private void AddDoc(string content)
        {
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).SetOpenMode(OpenMode.APPEND));
            InsertDoc(writer, content);
            writer.Dispose();
        }

        private void InsertDoc(IndexWriter writer, string content)
        {
            Document doc = new Document();

            doc.Add(NewStringField("id", "id" + docCount, Field.Store.YES));
            doc.Add(NewTextField("content", content, Field.Store.NO));

            writer.AddDocument(doc);
            docCount++;
        }

        // LUCENE-38
        [Test]
        public virtual void TestExclusiveLowerNull()
        {
            Analyzer analyzer = new SingleCharAnalyzer();
            //http://issues.apache.org/jira/browse/LUCENE-38
            Query query = TermRangeQuery.NewStringRange("content", null, "C", false, false);
            InitializeIndex(new string[] { "A", "B", "", "C", "D" }, analyzer);
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            int numHits = searcher.Search(query, null, 1000).TotalHits;
            // When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(3, numHits, "A,B,<empty string>,C,D => A, B & <empty string> are in range");
            // until Lucene-38 is fixed, use this assert:
            //Assert.AreEqual(2, hits.Length(), "A,B,<empty string>,C,D => A, B & <empty string> are in range");

            reader.Dispose();
            InitializeIndex(new string[] { "A", "B", "", "D" }, analyzer);
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            numHits = searcher.Search(query, null, 1000).TotalHits;
            // When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(3, numHits, "A,B,<empty string>,D => A, B & <empty string> are in range");
            // until Lucene-38 is fixed, use this assert:
            //Assert.AreEqual(2, hits.Length(), "A,B,<empty string>,D => A, B & <empty string> are in range");
            reader.Dispose();
            AddDoc("C");
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            numHits = searcher.Search(query, null, 1000).TotalHits;
            // When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(3, numHits, "C added, still A, B & <empty string> are in range");
            // until Lucene-38 is fixed, use this assert
            //Assert.AreEqual(2, hits.Length(), "C added, still A, B & <empty string> are in range");
            reader.Dispose();
        }

        // LUCENE-38
        [Test]
        public virtual void TestInclusiveLowerNull()
        {
            //http://issues.apache.org/jira/browse/LUCENE-38
            Analyzer analyzer = new SingleCharAnalyzer();
            Query query = TermRangeQuery.NewStringRange("content", null, "C", true, true);
            InitializeIndex(new string[] { "A", "B", "", "C", "D" }, analyzer);
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            int numHits = searcher.Search(query, null, 1000).TotalHits;
            // When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(4, numHits, "A,B,<empty string>,C,D => A,B,<empty string>,C in range");
            // until Lucene-38 is fixed, use this assert
            //Assert.AreEqual(3, hits.Length(), "A,B,<empty string>,C,D => A,B,<empty string>,C in range");
            reader.Dispose();
            InitializeIndex(new string[] { "A", "B", "", "D" }, analyzer);
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            numHits = searcher.Search(query, null, 1000).TotalHits;
            // When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(3, numHits, "A,B,<empty string>,D - A, B and <empty string> in range");
            // until Lucene-38 is fixed, use this assert
            //Assert.AreEqual(2, hits.Length(), "A,B,<empty string>,D => A, B and <empty string> in range");
            reader.Dispose();
            AddDoc("C");
            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            numHits = searcher.Search(query, null, 1000).TotalHits;
            // When Lucene-38 is fixed, use the assert on the next line:
            Assert.AreEqual(4, numHits, "C added => A,B,<empty string>,C in range");
            // until Lucene-38 is fixed, use this assert
            //Assert.AreEqual(3, hits.Length(), "C added => A,B,<empty string>,C in range");
            reader.Dispose();
        }
    }
}