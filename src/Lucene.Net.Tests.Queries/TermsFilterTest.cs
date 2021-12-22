// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Tests.Queries
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

    public class TermsFilterTest : LuceneTestCase
    {
        [Test]
        public void TestCachability()
        {
            TermsFilter a = TermsFilter(Random.NextBoolean(), new Term("field1", "a"), new Term("field1", "b"));
            ISet<Filter> cachedFilters = new JCG.HashSet<Filter>();
            cachedFilters.Add(a);
            TermsFilter b = TermsFilter(Random.NextBoolean(), new Term("field1", "b"), new Term("field1", "a"));
            assertTrue("Must be cached", cachedFilters.Contains(b));
            //duplicate term
            assertTrue("Must be cached", cachedFilters.Contains(TermsFilter(true, new Term("field1", "a"), new Term("field1", "a"), new Term("field1", "b"))));
            assertFalse("Must not be cached", cachedFilters.Contains(TermsFilter(Random.NextBoolean(), new Term("field1", "a"), new Term("field1", "a"), new Term("field1", "b"), new Term("field1", "v"))));
        }

        [Test]
        public void TestMissingTerms()
        {
            string fieldName = "field1";
            Directory rd = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, rd);
            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                int term = i * 10; //terms are units of 10;
                doc.Add(NewStringField(fieldName, "" + term, Field.Store.YES));
                w.AddDocument(doc);
            }
            IndexReader reader = SlowCompositeReaderWrapper.Wrap(w.GetReader());
            assertTrue(reader.Context is AtomicReaderContext);
            AtomicReaderContext context = (AtomicReaderContext)reader.Context;
            w.Dispose();

            IList<Term> terms = new JCG.List<Term>();
            terms.Add(new Term(fieldName, "19"));
            FixedBitSet bits = (FixedBitSet)TermsFilter(Random.NextBoolean(), terms).GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertNull("Must match nothing", bits);

            terms.Add(new Term(fieldName, "20"));
            bits = (FixedBitSet)TermsFilter(Random.NextBoolean(), terms).GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals("Must match 1", 1, bits.Cardinality);

            terms.Add(new Term(fieldName, "10"));
            bits = (FixedBitSet)TermsFilter(Random.NextBoolean(), terms).GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals("Must match 2", 2, bits.Cardinality);

            terms.Add(new Term(fieldName, "00"));
            bits = (FixedBitSet)TermsFilter(Random.NextBoolean(), terms).GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals("Must match 2", 2, bits.Cardinality);

            reader.Dispose();
            rd.Dispose();
        }

        [Test]
        public void TestMissingField()
        {
            string fieldName = "field1";
            Directory rd1 = NewDirectory();
            RandomIndexWriter w1 = new RandomIndexWriter(Random, rd1);
            Document doc = new Document();
            doc.Add(NewStringField(fieldName, "content1", Field.Store.YES));
            w1.AddDocument(doc);
            IndexReader reader1 = w1.GetReader();
            w1.Dispose();

            fieldName = "field2";
            Directory rd2 = NewDirectory();
            RandomIndexWriter w2 = new RandomIndexWriter(Random, rd2);
            doc = new Document();
            doc.Add(NewStringField(fieldName, "content2", Field.Store.YES));
            w2.AddDocument(doc);
            IndexReader reader2 = w2.GetReader();
            w2.Dispose();

            TermsFilter tf = new TermsFilter(new Term(fieldName, "content1"));
            MultiReader multi = new MultiReader(reader1, reader2);
            foreach (AtomicReaderContext context in multi.Leaves)
            {
                DocIdSet docIdSet = tf.GetDocIdSet(context, context.AtomicReader.LiveDocs);
                if (context.Reader.DocFreq(new Term(fieldName, "content1")) == 0)
                {
                    assertNull(docIdSet);
                }
                else
                {
                    FixedBitSet bits = (FixedBitSet)docIdSet;
                    assertTrue("Must be >= 0", bits.Cardinality >= 0);
                }
            }
            multi.Dispose();
            reader1.Dispose();
            reader2.Dispose();
            rd1.Dispose();
            rd2.Dispose();
        }

        [Test]
        public void TestFieldNotPresent()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            int num = AtLeast(3);
            int skip = Random.Next(num);
            var terms = new JCG.List<Term>();
            for (int i = 0; i < num; i++)
            {
                terms.Add(new Term("field" + i, "content1"));
                Document doc = new Document();
                if (skip == i)
                {
                    continue;
                }
                doc.Add(NewStringField("field" + i, "content1", Field.Store.YES));
                w.AddDocument(doc);
            }

            w.ForceMerge(1);
            IndexReader reader = w.GetReader();
            w.Dispose();
            assertEquals(1, reader.Leaves.size());



            AtomicReaderContext context = reader.Leaves[0];
            TermsFilter tf = new TermsFilter(terms);

            FixedBitSet bits = (FixedBitSet)tf.GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals("Must be num fields - 1 since we skip only one field", num - 1, bits.Cardinality);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestSkipField()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            int num = AtLeast(10);
            var terms = new JCG.HashSet<Term>();
            for (int i = 0; i < num; i++)
            {
                string field = "field" + Random.Next(100);
                terms.Add(new Term(field, "content1"));
                Document doc = new Document();
                doc.Add(NewStringField(field, "content1", Field.Store.YES));
                w.AddDocument(doc);
            }
            int randomFields = Random.Next(10);
            for (int i = 0; i < randomFields; i++)
            {
                while (true)
                {
                    string field = "field" + Random.Next(100);
                    Term t = new Term(field, "content1");
                    if (!terms.Contains(t))
                    {
                        terms.Add(t);
                        break;
                    }
                }
            }
            w.ForceMerge(1);
            IndexReader reader = w.GetReader();
            w.Dispose();
            assertEquals(1, reader.Leaves.size());
            AtomicReaderContext context = reader.Leaves[0];
            TermsFilter tf = new TermsFilter(terms.ToList());

            FixedBitSet bits = (FixedBitSet)tf.GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals(context.Reader.NumDocs, bits.Cardinality);
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestRandom()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            int num = AtLeast(100);
            bool singleField = Random.NextBoolean();
            JCG.List<Term> terms = new JCG.List<Term>();
            for (int i = 0; i < num; i++)
            {
                string field = "field" + (singleField ? "1" : Random.Next(100).ToString(CultureInfo.InvariantCulture));
                string @string = TestUtil.RandomRealisticUnicodeString(Random);
                terms.Add(new Term(field, @string));
                Document doc = new Document();
                doc.Add(NewStringField(field, @string, Field.Store.YES));
                w.AddDocument(doc);
            }
            IndexReader reader = w.GetReader();
            w.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            int numQueries = AtLeast(10);
            for (int i = 0; i < numQueries; i++)
            {
                terms.Shuffle(Random);
                int numTerms = 1 + Random.Next(Math.Min(BooleanQuery.MaxClauseCount, terms.Count));
                BooleanQuery bq = new BooleanQuery();
                for (int j = 0; j < numTerms; j++)
                {
                    bq.Add(new BooleanClause(new TermQuery(terms[j]), Occur.SHOULD));
                }
                TopDocs queryResult = searcher.Search(new ConstantScoreQuery(bq), reader.MaxDoc);

                MatchAllDocsQuery matchAll = new MatchAllDocsQuery();
                TermsFilter filter = TermsFilter(singleField, terms.GetView(0, numTerms)); // LUCENENET: Checked length for correctness
                TopDocs filterResult = searcher.Search(matchAll, filter, reader.MaxDoc);
                assertEquals(filterResult.TotalHits, queryResult.TotalHits);
                ScoreDoc[] scoreDocs = filterResult.ScoreDocs;
                for (int j = 0; j < scoreDocs.Length; j++)
                {
                    assertEquals(scoreDocs[j].Doc, queryResult.ScoreDocs[j].Doc);
                }
            }

            reader.Dispose();
            dir.Dispose();
        }

        private TermsFilter TermsFilter(bool singleField, params Term[] terms)
        {
            return TermsFilter(singleField, terms.ToList());
        }

        private TermsFilter TermsFilter(bool singleField, IEnumerable<Term> termList)
        {
            if (!singleField)
            {
                return new TermsFilter(termList.ToList());
            }
            TermsFilter filter;
            var bytes = new JCG.List<BytesRef>();
            string field = null;
            foreach (Term term in termList)
            {
                bytes.Add(term.Bytes);
                if (field != null)
                {
                    assertEquals(term.Field, field);
                }
                field = term.Field;
            }
            assertNotNull(field);
            filter = new TermsFilter(field, bytes);
            return filter;
        }

        [Test]
        public void TestHashCodeAndEquals()
        {
            int num = AtLeast(100);
            bool singleField = Random.NextBoolean();
            IList<Term> terms = new JCG.List<Term>();
            var uniqueTerms = new JCG.HashSet<Term>();
            for (int i = 0; i < num; i++)
            {
                string field = "field" + (singleField ? "1" : Random.Next(100).ToString());
                string @string = TestUtil.RandomRealisticUnicodeString(Random);
                terms.Add(new Term(field, @string));
                uniqueTerms.Add(new Term(field, @string));
                TermsFilter left = TermsFilter(singleField && Random.NextBoolean(), uniqueTerms);
                terms.Shuffle(Random);
                TermsFilter right = TermsFilter(singleField && Random.NextBoolean(), terms);
                assertEquals(right, left);
                assertEquals(right.GetHashCode(), left.GetHashCode());
                if (uniqueTerms.Count > 1)
                {
                    IList<Term> asList = new JCG.List<Term>(uniqueTerms);
                    asList.RemoveAt(0);
                    TermsFilter notEqual = TermsFilter(singleField && Random.NextBoolean(), asList);
                    assertFalse(left.Equals(notEqual));
                    assertFalse(right.Equals(notEqual));
                }
            }
        }

        [Test]
        public void TestSingleFieldEquals()
        {
            //// Two terms with the same hash code
            //assertEquals("AaAaBB".GetHashCode(), "BBBBBB".GetHashCode());
            //TermsFilter left = TermsFilter(true, new Term("id", "AaAaAa"), new Term("id", "AaAaBB"));
            //TermsFilter right = TermsFilter(true, new Term("id", "AaAaAa"), new Term("id", "BBBBBB"));
            //assertFalse(left.Equals(right));

            // LUCENENET specific - since in .NET the hash code is dependent on the underlying
            // target framework, we need to generate a collision at runtime.
            GenerateHashCollision(out string theString, out string stringWithCollision);
            assertEquals(theString.GetHashCode(), stringWithCollision.GetHashCode());
            TermsFilter left = TermsFilter(true, new Term("id", "AaAaAa"), new Term("id", theString));
            TermsFilter right = TermsFilter(true, new Term("id", "AaAaAa"), new Term("id", stringWithCollision));
            assertFalse(left.Equals(right));
        }

        // LUCENENET specific - since in .NET the hash code is dependent on the underlying
        // target framework, we need to generate a collision at runtime.
        // Source: https://stackoverflow.com/a/32027473
        private static void GenerateHashCollision(out string theString, out string stringWithCollision)
        {
            var words = new Dictionary<int, string>();

            int i = 0;
            string teststring;
            while (true)
            {
                i++;
                teststring = i.ToString();
                try
                {
                    words.Add(teststring.GetHashCode(), teststring);
                }
                catch (Exception)
                {
                    break;
                }
            }

            var collisionHash = teststring.GetHashCode();

            theString = teststring;
            stringWithCollision = words[collisionHash];
        }

        [Test]
        public void TestNoTerms()
        {
            var emptyTerms = Collections.EmptyList<Term>();
            var emptyBytesRef = Collections.EmptyList<BytesRef>();

            Assert.Throws<ArgumentException>(() => new TermsFilter(emptyTerms));
            Assert.Throws<ArgumentException>(() => new TermsFilter(emptyTerms.ToArray()));
            Assert.Throws<ArgumentException>(() => new TermsFilter(null, emptyBytesRef.ToArray()));
            Assert.Throws<ArgumentException>(() => new TermsFilter(null, emptyBytesRef));
        }

        [Test]
        public void TestToString()
        {
            TermsFilter termsFilter = new TermsFilter(
                new Term("field1", "a"),
                new Term("field1", "b"),
                new Term("field1", "c"));
            assertEquals("field1:a field1:b field1:c", termsFilter.ToString());
        }
    }
}
