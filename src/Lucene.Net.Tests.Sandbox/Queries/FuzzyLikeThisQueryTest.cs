using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Sandbox.Queries
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

    public class FuzzyLikeThisQueryTest : LuceneTestCase
    {
        private Directory directory;
        private IndexSearcher searcher;
        private IndexReader reader;
        private Analyzer analyzer;

        public override void SetUp()
        {
            base.SetUp();

            analyzer = new MockAnalyzer(Random);
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));

            //Add series of docs with misspelt names
            AddDoc(writer, "jonathon smythe", "1");
            AddDoc(writer, "jonathan smith", "2");
            AddDoc(writer, "johnathon smyth", "3");
            AddDoc(writer, "johnny smith", "4");
            AddDoc(writer, "jonny smith", "5");
            AddDoc(writer, "johnathon smythe", "6");
            reader = writer.GetReader();
            writer.Dispose();
            searcher = NewSearcher(reader);
        }

        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        private void AddDoc(RandomIndexWriter writer, string name, string id)
        {
            Document doc = new Document();
            doc.Add(NewTextField("name", name, Field.Store.YES));
            doc.Add(NewTextField("id", id, Field.Store.YES));
            writer.AddDocument(doc);
        }


        //Tests that idf ranking is not favouring rare mis-spellings over a strong edit-distance match
        [Test]
        public void TestClosestEditDistanceMatchComesFirst()
        {
            FuzzyLikeThisQuery flt = new FuzzyLikeThisQuery(10, analyzer);
            flt.AddTerms("smith", "name", 0.3f, 1);
            Query q = flt.Rewrite(searcher.IndexReader);
            ISet<Term> queryTerms = new JCG.HashSet<Term>();
            q.ExtractTerms(queryTerms);
            assertTrue("Should have variant smythe", queryTerms.contains(new Term("name", "smythe")));
            assertTrue("Should have variant smith", queryTerms.contains(new Term("name", "smith")));
            assertTrue("Should have variant smyth", queryTerms.contains(new Term("name", "smyth")));
            TopDocs topDocs = searcher.Search(flt, 1);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            assertTrue("score docs must match 1 doc", (sd != null) && (sd.Length > 0));
            Document doc = searcher.Doc(sd[0].Doc);
            assertEquals("Should match most similar not most rare variant", "2", doc.Get("id"));
        }

        //Test multiple input words are having variants produced
        [Test]
        public void TestMultiWord()
        {
            FuzzyLikeThisQuery flt = new FuzzyLikeThisQuery(10, analyzer);
            flt.AddTerms("jonathin smoth", "name", 0.3f, 1);
            Query q = flt.Rewrite(searcher.IndexReader);
            ISet<Term> queryTerms = new JCG.HashSet<Term>();
            q.ExtractTerms(queryTerms);
            assertTrue("Should have variant jonathan", queryTerms.contains(new Term("name", "jonathan")));
            assertTrue("Should have variant smith", queryTerms.contains(new Term("name", "smith")));
            TopDocs topDocs = searcher.Search(flt, 1);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            assertTrue("score docs must match 1 doc", (sd != null) && (sd.Length > 0));
            Document doc = searcher.Doc(sd[0].Doc);
            assertEquals("Should match most similar when using 2 words", "2", doc.Get("id"));
        }

        // LUCENE-4809
        [Test]
        public void TestNonExistingField()
        {
            FuzzyLikeThisQuery flt = new FuzzyLikeThisQuery(10, analyzer);
            flt.AddTerms("jonathin smoth", "name", 0.3f, 1);
            flt.AddTerms("jonathin smoth", "this field does not exist", 0.3f, 1);
            // don't fail here just because the field doesn't exits
            Query q = flt.Rewrite(searcher.IndexReader);
            ISet<Term> queryTerms = new JCG.HashSet<Term>();
            q.ExtractTerms(queryTerms);
            assertTrue("Should have variant jonathan", queryTerms.contains(new Term("name", "jonathan")));
            assertTrue("Should have variant smith", queryTerms.contains(new Term("name", "smith")));
            TopDocs topDocs = searcher.Search(flt, 1);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            assertTrue("score docs must match 1 doc", (sd != null) && (sd.Length > 0));
            Document doc = searcher.Doc(sd[0].Doc);
            assertEquals("Should match most similar when using 2 words", "2", doc.Get("id"));
        }


        //Test bug found when first query word does not match anything
        [Test]
        public void TestNoMatchFirstWordBug()
        {
            FuzzyLikeThisQuery flt = new FuzzyLikeThisQuery(10, analyzer);
            flt.AddTerms("fernando smith", "name", 0.3f, 1);
            Query q = flt.Rewrite(searcher.IndexReader);
            ISet<Term> queryTerms = new JCG.HashSet<Term>();
            q.ExtractTerms(queryTerms);
            assertTrue("Should have variant smith", queryTerms.contains(new Term("name", "smith")));
            TopDocs topDocs = searcher.Search(flt, 1);
            ScoreDoc[] sd = topDocs.ScoreDocs;
            assertTrue("score docs must match 1 doc", (sd != null) && (sd.Length > 0));
            Document doc = searcher.Doc(sd[0].Doc);
            assertEquals("Should match most similar when using 2 words", "2", doc.Get("id"));
        }

        [Test]
        public void TestFuzzyLikeThisQueryEquals()
        {
            Analyzer analyzer = new MockAnalyzer(Random);
            FuzzyLikeThisQuery fltq1 = new FuzzyLikeThisQuery(10, analyzer);
            fltq1.AddTerms("javi", "subject", 0.5f, 2);
            FuzzyLikeThisQuery fltq2 = new FuzzyLikeThisQuery(10, analyzer);
            fltq2.AddTerms("javi", "subject", 0.5f, 2);
            assertEquals("FuzzyLikeThisQuery with same attributes is not equal", fltq1,
                fltq2);
        }
    }
}
