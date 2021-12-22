using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

#pragma warning disable 612, 618
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

    /// <summary>
    /// Tests <see cref="SlowFuzzyQuery"/>
    /// </summary>
    public class TestSlowFuzzyQuery : LuceneTestCase
    {
        [Test]
        public void TestFuzziness()
        {
            //every test with SlowFuzzyQuery.defaultMinSimilarity
            //is exercising the Automaton, not the brute force linear method

            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            addDoc("aaaaa", writer);
            addDoc("aaaab", writer);
            addDoc("aaabb", writer);
            addDoc("aabbb", writer);
            addDoc("abbbb", writer);
            addDoc("bbbbb", writer);
            addDoc("ddddd", writer);

            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            writer.Dispose();

            SlowFuzzyQuery query = new SlowFuzzyQuery(new Term("field", "aaaaa"), SlowFuzzyQuery.defaultMinSimilarity, 0);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);

            // same with prefix
            query = new SlowFuzzyQuery(new Term("field", "aaaaa"), SlowFuzzyQuery.defaultMinSimilarity, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "aaaaa"), SlowFuzzyQuery.defaultMinSimilarity, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "aaaaa"), SlowFuzzyQuery.defaultMinSimilarity, 3);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "aaaaa"), SlowFuzzyQuery.defaultMinSimilarity, 4);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(2, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "aaaaa"), SlowFuzzyQuery.defaultMinSimilarity, 5);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "aaaaa"), SlowFuzzyQuery.defaultMinSimilarity, 6);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);

            // test scoring
            query = new SlowFuzzyQuery(new Term("field", "bbbbb"), SlowFuzzyQuery.defaultMinSimilarity, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals("3 documents should match", 3, hits.Length);
            IList<String> order = new string[] { "bbbbb", "abbbb", "aabbb" };
            for (int i = 0; i < hits.Length; i++)
            {
                string term = searcher.Doc(hits[i].Doc).Get("field");
                //System.out.println(hits[i].score);
                assertEquals(order[i], term);
            }

            // test pq size by supplying maxExpansions=2
            // This query would normally return 3 documents, because 3 terms match (see above):
            query = new SlowFuzzyQuery(new Term("field", "bbbbb"), SlowFuzzyQuery.defaultMinSimilarity, 0, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals("only 2 documents should match", 2, hits.Length);
            order = new string[] { "bbbbb", "abbbb" };
            for (int i = 0; i < hits.Length; i++)
            {
                string term = searcher.Doc(hits[i].Doc).Get("field");
                //System.out.println(hits[i].score);
                assertEquals(order[i], term);
            }

            // not similar enough:
            query = new SlowFuzzyQuery(new Term("field", "xxxxx"), SlowFuzzyQuery.defaultMinSimilarity, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "aaccc"), SlowFuzzyQuery.defaultMinSimilarity, 0);   // edit distance to "aaaaa" = 3
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            // query identical to a word in the index:
            query = new SlowFuzzyQuery(new Term("field", "aaaaa"), SlowFuzzyQuery.defaultMinSimilarity, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            // default allows for up to two edits:
            assertEquals(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            assertEquals(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));

            // query similar to a word in the index:
            query = new SlowFuzzyQuery(new Term("field", "aaaac"), SlowFuzzyQuery.defaultMinSimilarity, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            assertEquals(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            assertEquals(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));

            // now with prefix
            query = new SlowFuzzyQuery(new Term("field", "aaaac"), SlowFuzzyQuery.defaultMinSimilarity, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            assertEquals(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            assertEquals(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));
            query = new SlowFuzzyQuery(new Term("field", "aaaac"), SlowFuzzyQuery.defaultMinSimilarity, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            assertEquals(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            assertEquals(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));
            query = new SlowFuzzyQuery(new Term("field", "aaaac"), SlowFuzzyQuery.defaultMinSimilarity, 3);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            assertEquals(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            assertEquals(searcher.Doc(hits[2].Doc).Get("field"), ("aaabb"));
            query = new SlowFuzzyQuery(new Term("field", "aaaac"), SlowFuzzyQuery.defaultMinSimilarity, 4);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(2, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaa"));
            assertEquals(searcher.Doc(hits[1].Doc).Get("field"), ("aaaab"));
            query = new SlowFuzzyQuery(new Term("field", "aaaac"), SlowFuzzyQuery.defaultMinSimilarity, 5);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);


            query = new SlowFuzzyQuery(new Term("field", "ddddX"), SlowFuzzyQuery.defaultMinSimilarity, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));

            // now with prefix
            query = new SlowFuzzyQuery(new Term("field", "ddddX"), SlowFuzzyQuery.defaultMinSimilarity, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));
            query = new SlowFuzzyQuery(new Term("field", "ddddX"), SlowFuzzyQuery.defaultMinSimilarity, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));
            query = new SlowFuzzyQuery(new Term("field", "ddddX"), SlowFuzzyQuery.defaultMinSimilarity, 3);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));
            query = new SlowFuzzyQuery(new Term("field", "ddddX"), SlowFuzzyQuery.defaultMinSimilarity, 4);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("ddddd"));
            query = new SlowFuzzyQuery(new Term("field", "ddddX"), SlowFuzzyQuery.defaultMinSimilarity, 5);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);


            // different field = no match:
            query = new SlowFuzzyQuery(new Term("anotherfield", "ddddX"), SlowFuzzyQuery.defaultMinSimilarity, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public void TestFuzzinessLong2()
        {
            //Lucene-5033
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            addDoc("abcdef", writer);
            addDoc("segment", writer);

            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            writer.Dispose();

            SlowFuzzyQuery query;

            query = new SlowFuzzyQuery(new Term("field", "abcxxxx"), 3f, 0);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            query = new SlowFuzzyQuery(new Term("field", "abcxxxx"), 4f, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public void TestFuzzinessLong()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            addDoc("aaaaaaa", writer);
            addDoc("segment", writer);

            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            writer.Dispose();

            SlowFuzzyQuery query;
            // not similar enough:
            query = new SlowFuzzyQuery(new Term("field", "xxxxx"), 0.5f, 0);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);
            // edit distance to "aaaaaaa" = 3, this matches because the string is longer than
            // in testDefaultFuzziness so a bigger difference is allowed:
            query = new SlowFuzzyQuery(new Term("field", "aaaaccc"), 0.5f, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaaaa"));

            // now with prefix
            query = new SlowFuzzyQuery(new Term("field", "aaaaccc"), 0.5f, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaaaa"));
            query = new SlowFuzzyQuery(new Term("field", "aaaaccc"), 0.5f, 4);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals(searcher.Doc(hits[0].Doc).Get("field"), ("aaaaaaa"));
            query = new SlowFuzzyQuery(new Term("field", "aaaaccc"), 0.5f, 5);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            // no match, more than half of the characters is wrong:
            query = new SlowFuzzyQuery(new Term("field", "aaacccc"), 0.5f, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            // now with prefix
            query = new SlowFuzzyQuery(new Term("field", "aaacccc"), 0.5f, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            // "student" and "stellent" are indeed similar to "segment" by default:
            query = new SlowFuzzyQuery(new Term("field", "student"), 0.5f, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "stellent"), 0.5f, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);

            // now with prefix
            query = new SlowFuzzyQuery(new Term("field", "student"), 0.5f, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "stellent"), 0.5f, 1);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "student"), 0.5f, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);
            query = new SlowFuzzyQuery(new Term("field", "stellent"), 0.5f, 2);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            // "student" doesn't match anymore thanks to increased minimum similarity:
            query = new SlowFuzzyQuery(new Term("field", "student"), 0.6f, 0);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            try
            {
                query = new SlowFuzzyQuery(new Term("field", "student"), 1.1f);
                fail("Expected IllegalArgumentException");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expecting exception
            }
            try
            {
                query = new SlowFuzzyQuery(new Term("field", "student"), -0.1f);
                fail("Expected IllegalArgumentException");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // expecting exception
            }

            reader.Dispose();
            directory.Dispose();
        }

        /** 
         * MultiTermQuery provides (via attribute) information about which values
         * must be competitive to enter the priority queue. 
         * 
         * SlowFuzzyQuery optimizes itself around this information, if the attribute
         * is not implemented correctly, there will be problems!
         */
        [Test]
        public void TestTieBreaker()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            addDoc("a123456", writer);
            addDoc("c123456", writer);
            addDoc("d123456", writer);
            addDoc("e123456", writer);

            Directory directory2 = NewDirectory();
            RandomIndexWriter writer2 = new RandomIndexWriter(Random, directory2);
            addDoc("a123456", writer2);
            addDoc("b123456", writer2);
            addDoc("b123456", writer2);
            addDoc("b123456", writer2);
            addDoc("c123456", writer2);
            addDoc("f123456", writer2);

            IndexReader ir1 = writer.GetReader();
            IndexReader ir2 = writer2.GetReader();

            MultiReader mr = new MultiReader(ir1, ir2);
            IndexSearcher searcher = NewSearcher(mr);
            SlowFuzzyQuery fq = new SlowFuzzyQuery(new Term("field", "z123456"), 1f, 0, 2);
            TopDocs docs = searcher.Search(fq, 2);
            assertEquals(5, docs.TotalHits); // 5 docs, from the a and b's
            mr.Dispose();
            ir1.Dispose();
            ir2.Dispose();
            writer.Dispose();
            writer2.Dispose();
            directory.Dispose();
            directory2.Dispose();
        }

        [Test]
        public void TestTokenLengthOpt()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            addDoc("12345678911", writer);
            addDoc("segment", writer);

            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            writer.Dispose();

            Query query;
            // term not over 10 chars, so optimization shortcuts
            query = new SlowFuzzyQuery(new Term("field", "1234569"), 0.9f);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            // 10 chars, so no optimization
            query = new SlowFuzzyQuery(new Term("field", "1234567891"), 0.9f);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            // over 10 chars, so no optimization
            query = new SlowFuzzyQuery(new Term("field", "12345678911"), 0.9f);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(1, hits.Length);

            // over 10 chars, no match
            query = new SlowFuzzyQuery(new Term("field", "sdfsdfsdfsdf"), 0.9f);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(0, hits.Length);

            reader.Dispose();
            directory.Dispose();
        }

        /** Test the TopTermsBoostOnlyBooleanQueryRewrite rewrite method. */
        [Test]
        public void TestBoostOnlyRewrite()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            addDoc("Lucene", writer);
            addDoc("Lucene", writer);
            addDoc("Lucenne", writer);

            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            writer.Dispose();

            SlowFuzzyQuery query = new SlowFuzzyQuery(new Term("field", "lucene"));
            query.MultiTermRewriteMethod = new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(50);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            assertEquals(3, hits.Length);
            // normally, 'Lucenne' would be the first result as IDF will skew the score.
            assertEquals("Lucene", reader.Document(hits[0].Doc).Get("field"));
            assertEquals("Lucene", reader.Document(hits[1].Doc).Get("field"));
            assertEquals("Lucenne", reader.Document(hits[2].Doc).Get("field"));
            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public void TestGiga()
        {

            Directory index = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, index);

            addDoc("Lucene in Action", w);
            addDoc("Lucene for Dummies", w);

            //addDoc("Giga", w);
            addDoc("Giga byte", w);

            addDoc("ManagingGigabytesManagingGigabyte", w);
            addDoc("ManagingGigabytesManagingGigabytes", w);

            addDoc("The Art of Computer Science", w);
            addDoc("J. K. Rowling", w);
            addDoc("JK Rowling", w);
            addDoc("Joanne K Roling", w);
            addDoc("Bruce Willis", w);
            addDoc("Willis bruce", w);
            addDoc("Brute willis", w);
            addDoc("B. willis", w);
            IndexReader r = w.GetReader();
            w.Dispose();

            Query q = new SlowFuzzyQuery(new Term("field", "giga"), 0.9f);

            // 3. search
            IndexSearcher searcher = NewSearcher(r);
            ScoreDoc[] hits = searcher.Search(q, 10).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals("Giga byte", searcher.Doc(hits[0].Doc).Get("field"));
            r.Dispose();
            index.Dispose();
        }

        [Test]
        public void TestDistanceAsEditsSearching()
        {
            Directory index = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, index);
            addDoc("foobar", w);
            addDoc("test", w);
            addDoc("working", w);
            IndexReader reader = w.GetReader();
            IndexSearcher searcher = NewSearcher(reader);
            w.Dispose();

            SlowFuzzyQuery q = new SlowFuzzyQuery(new Term("field", "fouba"), 2);
            ScoreDoc[] hits = searcher.Search(q, 10).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals("foobar", searcher.Doc(hits[0].Doc).Get("field"));

            q = new SlowFuzzyQuery(new Term("field", "foubara"), 2);
            hits = searcher.Search(q, 10).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals("foobar", searcher.Doc(hits[0].Doc).Get("field"));

            q = new SlowFuzzyQuery(new Term("field", "t"), 3);
            hits = searcher.Search(q, 10).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals("test", searcher.Doc(hits[0].Doc).Get("field"));

            q = new SlowFuzzyQuery(new Term("field", "a"), 4f, 0, 50);
            hits = searcher.Search(q, 10).ScoreDocs;
            assertEquals(1, hits.Length);
            assertEquals("test", searcher.Doc(hits[0].Doc).Get("field"));

            q = new SlowFuzzyQuery(new Term("field", "a"), 6f, 0, 50);
            hits = searcher.Search(q, 10).ScoreDocs;
            assertEquals(2, hits.Length);
            assertEquals("test", searcher.Doc(hits[0].Doc).Get("field"));
            assertEquals("foobar", searcher.Doc(hits[1].Doc).Get("field"));

            reader.Dispose();
            index.Dispose();
        }

        private void addDoc(string text, RandomIndexWriter writer)
        {
            Document doc = new Document();
            doc.Add(NewTextField("field", text, Field.Store.YES));
            writer.AddDocument(doc);
        }
    }
}
#pragma warning restore 612, 618