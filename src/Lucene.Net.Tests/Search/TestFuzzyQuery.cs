using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using MultiReader = Lucene.Net.Index.MultiReader;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Tests <seealso cref="FuzzyQuery"/>.
    ///
    /// </summary>
    [TestFixture]
    public class TestFuzzyQuery : LuceneTestCase
    {
        [Test]
        public virtual void TestFuzziness()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            AddDoc("aaaaa", writer);
            AddDoc("aaaab", writer);
            AddDoc("aaabb", writer);
            AddDoc("aabbb", writer);
            AddDoc("abbbb", writer);
            AddDoc("bbbbb", writer);
            AddDoc("ddddd", writer);

            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            writer.Dispose();

            FuzzyQuery query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.DefaultMaxEdits, 0);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);

            // same with prefix
            query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.DefaultMaxEdits, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.DefaultMaxEdits, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.DefaultMaxEdits, 3);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.DefaultMaxEdits, 4);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.DefaultMaxEdits, 5);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.DefaultMaxEdits, 6);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            // test scoring
            query = new FuzzyQuery(new Term("field", "bbbbb"), FuzzyQuery.DefaultMaxEdits, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length, "3 documents should match");
            IList<string> order = new JCG.List<string> { "bbbbb", "abbbb", "aabbb" };
            for (int i = 0; i < hits.Length; i++)
            {
                string term = searcher.Doc(hits[i].Doc).Get("field");
                //System.out.println(hits[i].Score);
                Assert.AreEqual(order[i], term);
            }

            // test pq size by supplying maxExpansions=2
            // this query would normally return 3 documents, because 3 terms match (see above):
            query = new FuzzyQuery(new Term("field", "bbbbb"), FuzzyQuery.DefaultMaxEdits, 0, 2, false);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length, "only 2 documents should match");
            order = new JCG.List<string> { "bbbbb", "abbbb" };
            for (int i = 0; i < hits.Length; i++)
            {
                string term = searcher.Doc(hits[i].Doc).Get("field");
                //System.out.println(hits[i].Score);
                Assert.AreEqual(order[i], term);
            }

            // not similar enough:
            query = new FuzzyQuery(new Term("field", "xxxxx"), FuzzyQuery.DefaultMaxEdits, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);
            query = new FuzzyQuery(new Term("field", "aaccc"), FuzzyQuery.DefaultMaxEdits, 0); // edit distance to "aaaaa" = 3
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            // query identical to a word in the index:
            query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.DefaultMaxEdits, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            // default allows for up to two edits:
            Assert.AreEqual(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            Assert.AreEqual(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));

            // query similar to a word in the index:
            query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.DefaultMaxEdits, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            Assert.AreEqual(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            Assert.AreEqual(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));

            // now with prefix
            query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.DefaultMaxEdits, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            Assert.AreEqual(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            Assert.AreEqual(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));
            query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.DefaultMaxEdits, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            Assert.AreEqual(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            Assert.AreEqual(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));
            query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.DefaultMaxEdits, 3);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            Assert.AreEqual(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            Assert.AreEqual(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));
            query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.DefaultMaxEdits, 4);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(2, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            Assert.AreEqual(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.DefaultMaxEdits, 5);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.DefaultMaxEdits, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));

            // now with prefix
            query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.DefaultMaxEdits, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));
            query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.DefaultMaxEdits, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));
            query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.DefaultMaxEdits, 3);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));
            query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.DefaultMaxEdits, 4);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));
            query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.DefaultMaxEdits, 5);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            // different field = no match:
            query = new FuzzyQuery(new Term("anotherfield", "ddddX"), FuzzyQuery.DefaultMaxEdits, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length);

            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void Test2()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, new MockAnalyzer(Random, MockTokenizer.KEYWORD, false));
            AddDoc("LANGE", writer);
            AddDoc("LUETH", writer);
            AddDoc("PIRSING", writer);
            AddDoc("RIEGEL", writer);
            AddDoc("TRZECZIAK", writer);
            AddDoc("WALKER", writer);
            AddDoc("WBR", writer);
            AddDoc("WE", writer);
            AddDoc("WEB", writer);
            AddDoc("WEBE", writer);
            AddDoc("WEBER", writer);
            AddDoc("WEBERE", writer);
            AddDoc("WEBREE", writer);
            AddDoc("WEBEREI", writer);
            AddDoc("WBRE", writer);
            AddDoc("WITTKOPF", writer);
            AddDoc("WOJNAROWSKI", writer);
            AddDoc("WRICKE", writer);

            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            writer.Dispose();

            FuzzyQuery query = new FuzzyQuery(new Term("field", "WEBER"), 2, 1);
            //query.setRewriteMethod(FuzzyQuery.SCORING_BOOLEAN_QUERY_REWRITE);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(8, hits.Length);

            reader.Dispose();
            directory.Dispose();
        }

        /// <summary>
        /// MultiTermQuery provides (via attribute) information about which values
        /// must be competitive to enter the priority queue.
        ///
        /// FuzzyQuery optimizes itself around this information, if the attribute
        /// is not implemented correctly, there will be problems!
        /// </summary>
        [Test]
        public virtual void TestTieBreaker()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            AddDoc("a123456", writer);
            AddDoc("c123456", writer);
            AddDoc("d123456", writer);
            AddDoc("e123456", writer);

            Directory directory2 = NewDirectory();
            RandomIndexWriter writer2 = new RandomIndexWriter(Random, directory2);
            AddDoc("a123456", writer2);
            AddDoc("b123456", writer2);
            AddDoc("b123456", writer2);
            AddDoc("b123456", writer2);
            AddDoc("c123456", writer2);
            AddDoc("f123456", writer2);

            IndexReader ir1 = writer.GetReader();
            IndexReader ir2 = writer2.GetReader();

            MultiReader mr = new MultiReader(ir1, ir2);
            IndexSearcher searcher = NewSearcher(mr);
            FuzzyQuery fq = new FuzzyQuery(new Term("field", "z123456"), 1, 0, 2, false);
            TopDocs docs = searcher.Search(fq, 2);
            Assert.AreEqual(5, docs.TotalHits); // 5 docs, from the a and b's
            mr.Dispose();
            ir1.Dispose();
            ir2.Dispose();
            writer.Dispose();
            writer2.Dispose();
            directory.Dispose();
            directory2.Dispose();
        }

        /// <summary>
        /// Test the TopTermsBoostOnlyBooleanQueryRewrite rewrite method. </summary>
        [Test]
        public virtual void TestBoostOnlyRewrite()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            AddDoc("Lucene", writer);
            AddDoc("Lucene", writer);
            AddDoc("Lucenne", writer);

            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            writer.Dispose();

            FuzzyQuery query = new FuzzyQuery(new Term("field", "lucene"));
            query.MultiTermRewriteMethod = (new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(50));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            // normally, 'Lucenne' would be the first result as IDF will skew the score.
            Assert.AreEqual("Lucene", reader.Document(hits[0].Doc).Get("field"));
            Assert.AreEqual("Lucene", reader.Document(hits[1].Doc).Get("field"));
            Assert.AreEqual("Lucenne", reader.Document(hits[2].Doc).Get("field"));
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestGiga()
        {
            MockAnalyzer analyzer = new MockAnalyzer(Random);
            Directory index = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, index);

            AddDoc("Lucene in Action", w);
            AddDoc("Lucene for Dummies", w);

            //addDoc("Giga", w);
            AddDoc("Giga byte", w);

            AddDoc("ManagingGigabytesManagingGigabyte", w);
            AddDoc("ManagingGigabytesManagingGigabytes", w);

            AddDoc("The Art of Computer Science", w);
            AddDoc("J. K. Rowling", w);
            AddDoc("JK Rowling", w);
            AddDoc("Joanne K Roling", w);
            AddDoc("Bruce Willis", w);
            AddDoc("Willis bruce", w);
            AddDoc("Brute willis", w);
            AddDoc("B. willis", w);
            IndexReader r = w.GetReader();
            w.Dispose();

            Query q = new FuzzyQuery(new Term("field", "giga"), 0);

            // 3. search
            IndexSearcher searcher = NewSearcher(r);
            ScoreDoc[] hits = searcher.Search(q, 10).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual("Giga byte", searcher.Doc(hits[0].Doc).Get("field"));
            r.Dispose();
            index.Dispose();
        }

        [Test]
        public virtual void TestDistanceAsEditsSearching()
        {
            Directory index = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, index);
            AddDoc("foobar", w);
            AddDoc("test", w);
            AddDoc("working", w);
            IndexReader reader = w.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            w.Dispose();

            FuzzyQuery q = new FuzzyQuery(new Term("field", "fouba"), 2);
            ScoreDoc[] hits = searcher.Search(q, 10).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual("foobar", searcher.Doc(hits[0].Doc).Get("field"));

            q = new FuzzyQuery(new Term("field", "foubara"), 2);
            hits = searcher.Search(q, 10).ScoreDocs;
            Assert.AreEqual(1, hits.Length);
            Assert.AreEqual("foobar", searcher.Doc(hits[0].Doc).Get("field"));

            try
            {
                q = new FuzzyQuery(new Term("field", "t"), 3);
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // expected
            }

            reader.Dispose();
            index.Dispose();
        }

        private void AddDoc(string text, RandomIndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("field", text, Field.Store.YES));
            writer.AddDocument(doc);
        }
    }
}