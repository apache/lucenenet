using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries
{
    public class TermsFilterTest : LuceneTestCase
    {
        [Test]
        public void TestCachability()
        {
            TermsFilter a = TermsFilter(Random().NextBoolean(), new Term("field1", "a"), new Term("field1", "b"));
            HashSet<Filter> cachedFilters = new HashSet<Filter>();
            cachedFilters.Add(a);
            TermsFilter b = TermsFilter(Random().NextBoolean(), new Term("field1", "b"), new Term("field1", "a"));
            assertTrue("Must be cached", cachedFilters.Contains(b));
            //duplicate term
            assertTrue("Must be cached", cachedFilters.Contains(TermsFilter(true, new Term("field1", "a"), new Term("field1", "a"), new Term("field1", "b"))));
            assertFalse("Must not be cached", cachedFilters.Contains(TermsFilter(Random().NextBoolean(), new Term("field1", "a"), new Term("field1", "a"), new Term("field1", "b"), new Term("field1", "v"))));
        }

        [Test]
        public void TestMissingTerms()
        {
            string fieldName = "field1";
            Directory rd = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), rd, Similarity, TimeZone);
            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                int term = i * 10; //terms are units of 10;
                doc.Add(NewStringField(fieldName, "" + term, Field.Store.YES));
                w.AddDocument(doc);
            }
            IndexReader reader = SlowCompositeReaderWrapper.Wrap(w.Reader);
            assertTrue(reader.Context is AtomicReaderContext);
            AtomicReaderContext context = (AtomicReaderContext)reader.Context;
            w.Dispose();

            IList<Term> terms = new List<Term>();
            terms.Add(new Term(fieldName, "19"));
            FixedBitSet bits = (FixedBitSet)TermsFilter(Random().NextBoolean(), terms).GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertNull("Must match nothing", bits);

            terms.Add(new Term(fieldName, "20"));
            bits = (FixedBitSet)TermsFilter(Random().NextBoolean(), terms).GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals("Must match 1", 1, bits.Cardinality());

            terms.Add(new Term(fieldName, "10"));
            bits = (FixedBitSet)TermsFilter(Random().NextBoolean(), terms).GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals("Must match 2", 2, bits.Cardinality());

            terms.Add(new Term(fieldName, "00"));
            bits = (FixedBitSet)TermsFilter(Random().NextBoolean(), terms).GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals("Must match 2", 2, bits.Cardinality());

            reader.Dispose();
            rd.Dispose();
        }
        
        [Test]
        public void TestMissingField()
        {
            string fieldName = "field1";
            Directory rd1 = NewDirectory();
            RandomIndexWriter w1 = new RandomIndexWriter(Random(), rd1, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField(fieldName, "content1", Field.Store.YES));
            w1.AddDocument(doc);
            IndexReader reader1 = w1.Reader;
            w1.Dispose();

            fieldName = "field2";
            Directory rd2 = NewDirectory();
            RandomIndexWriter w2 = new RandomIndexWriter(Random(), rd2, Similarity, TimeZone);
            doc = new Document();
            doc.Add(NewStringField(fieldName, "content2", Field.Store.YES));
            w2.AddDocument(doc);
            IndexReader reader2 = w2.Reader;
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
                    assertTrue("Must be >= 0", bits.Cardinality() >= 0);
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
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            int num = AtLeast(3);
            int skip = Random().Next(num);
            var terms = new List<Term>();
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
            IndexReader reader = w.Reader;
            w.Dispose();
            assertEquals(1, reader.Leaves.size());



            AtomicReaderContext context = reader.Leaves.First();
            TermsFilter tf = new TermsFilter(terms);

            FixedBitSet bits = (FixedBitSet)tf.GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals("Must be num fields - 1 since we skip only one field", num - 1, bits.Cardinality());
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestSkipField()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            int num = AtLeast(10);
            var terms = new HashSet<Term>();
            for (int i = 0; i < num; i++)
            {
                string field = "field" + Random().Next(100);
                terms.Add(new Term(field, "content1"));
                Document doc = new Document();
                doc.Add(NewStringField(field, "content1", Field.Store.YES));
                w.AddDocument(doc);
            }
            int randomFields = Random().Next(10);
            for (int i = 0; i < randomFields; i++)
            {
                while (true)
                {
                    string field = "field" + Random().Next(100);
                    Term t = new Term(field, "content1");
                    if (!terms.Contains(t))
                    {
                        terms.Add(t);
                        break;
                    }
                }
            }
            w.ForceMerge(1);
            IndexReader reader = w.Reader;
            w.Dispose();
            assertEquals(1, reader.Leaves.size());
            AtomicReaderContext context = reader.Leaves.First();
            TermsFilter tf = new TermsFilter(terms.ToList());

            FixedBitSet bits = (FixedBitSet)tf.GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertEquals(context.Reader.NumDocs, bits.Cardinality());
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestRandom()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            int num = AtLeast(100);
            bool singleField = Random().NextBoolean();
            IList<Term> terms = new List<Term>();
            for (int i = 0; i < num; i++)
            {
                string field = "field" + (singleField ? "1" : Random().Next(100).ToString());
                string @string = TestUtil.RandomRealisticUnicodeString(Random());
                terms.Add(new Term(field, @string));
                Document doc = new Document();
                doc.Add(NewStringField(field, @string, Field.Store.YES));
                w.AddDocument(doc);
            }
            IndexReader reader = w.Reader;
            w.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            int numQueries = AtLeast(10);
            for (int i = 0; i < numQueries; i++)
            {
                CollectionsHelper.Shuffle(terms);
                int numTerms = 1 + Random().Next(Math.Min(BooleanQuery.MaxClauseCount, terms.Count));
                BooleanQuery bq = new BooleanQuery();
                for (int j = 0; j < numTerms; j++)
                {
                    bq.Add(new BooleanClause(new TermQuery(terms[j]), Occur.SHOULD));
                }
                TopDocs queryResult = searcher.Search(new ConstantScoreQuery(bq), reader.MaxDoc);

                MatchAllDocsQuery matchAll = new MatchAllDocsQuery();
                TermsFilter filter = TermsFilter(singleField, terms.SubList(0, numTerms));
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
            var bytes = new List<BytesRef>();
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
            bool singleField = Random().NextBoolean();
            IList<Term> terms = new List<Term>();
            var uniqueTerms = new HashSet<Term>();
            for (int i = 0; i < num; i++)
            {
                string field = "field" + (singleField ? "1" : Random().Next(100).ToString());
                string @string = TestUtil.RandomRealisticUnicodeString(Random());
                terms.Add(new Term(field, @string));
                uniqueTerms.Add(new Term(field, @string));
                TermsFilter left = TermsFilter(singleField && Random().NextBoolean(), uniqueTerms);
                CollectionsHelper.Shuffle(terms);
                TermsFilter right = TermsFilter(singleField && Random().NextBoolean(), terms);
                assertEquals(right, left);
                assertEquals(right.GetHashCode(), left.GetHashCode());
                if (uniqueTerms.Count > 1)
                {
                    IList<Term> asList = new List<Term>(uniqueTerms);
                    asList.RemoveAt(0);
                    TermsFilter notEqual = TermsFilter(singleField && Random().NextBoolean(), asList);
                    assertFalse(left.Equals(notEqual));
                    assertFalse(right.Equals(notEqual));
                }
            }
        }

        [Test]
        public void TestSingleFieldEquals()
        {
            // Two terms with the same hash code
            //assertEquals("AaAaBB".GetHashCode(), "BBBBBB".GetHashCode());
            TermsFilter left = TermsFilter(true, new Term("id", "AaAaAa"), new Term("id", "AaAaBB"));
            TermsFilter right = TermsFilter(true, new Term("id", "AaAaAa"), new Term("id", "BBBBBB"));
            assertFalse(left.Equals(right));
        }

        [Test]
        public void TestNoTerms()
        {
            var emptyTerms = new List<Term>();
            var emptyBytesRef = new List<BytesRef>();

            Assert.Throws<ArgumentException>(() => new TermsFilter(emptyTerms));
            Assert.Throws<ArgumentException>(() => new TermsFilter(emptyTerms.ToArray()));
            Assert.Throws<ArgumentException>(() => new TermsFilter(null, emptyBytesRef.ToArray()));
            Assert.Throws<ArgumentException>(() => new TermsFilter(null, emptyBytesRef));
        }

        [Test]
        public void TestToString()
        {
            TermsFilter termsFilter = new TermsFilter(new Term("field1", "a"), new Term("field1", "b"), new Term("field1", "c"));
            assertEquals("field1:a field1:b field1:c", termsFilter.ToString());
        }
    }
}