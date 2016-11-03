using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries
{
    public class TermFilterTest : LuceneTestCase
    {
        [Test]
        public void TestCachability()
        {
            TermFilter a = TermFilter(@"field1", @"a");
            var cachedFilters = new HashSet<Filter>();
            cachedFilters.Add(a);
            assertTrue(@"Must be cached", cachedFilters.Contains(TermFilter(@"field1", @"a")));
            assertFalse(@"Must not be cached", cachedFilters.Contains(TermFilter(@"field1", @"b")));
            assertFalse(@"Must not be cached", cachedFilters.Contains(TermFilter(@"field2", @"a")));
        }

        [Test]
        public void TestMissingTermAndField()
        {
            string fieldName = @"field1";
            Directory rd = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), rd, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField(fieldName, @"value1", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader reader = SlowCompositeReaderWrapper.Wrap(w.Reader);
            assertTrue(reader.Context is AtomicReaderContext);
            var context = (AtomicReaderContext)reader.Context;
            w.Dispose();
            DocIdSet idSet = TermFilter(fieldName, @"value1").GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertNotNull(@"must not be null", idSet);
            DocIdSetIterator iter = idSet.GetIterator();
            assertEquals(iter.NextDoc(), 0);
            assertEquals(iter.NextDoc(), DocIdSetIterator.NO_MORE_DOCS);
            idSet = TermFilter(fieldName, @"value2").GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertNull(@"must be null", idSet);
            idSet = TermFilter(@"field2", @"value1").GetDocIdSet(context, context.AtomicReader.LiveDocs);
            assertNull(@"must be null", idSet);
            reader.Dispose();
            rd.Dispose();
        }

        [Test]
        public void TestRandom()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            int num = AtLeast(100);
            var terms = new List<Term>();
            for (int i = 0; i < num; i++)
            {
                string field = @"field" + i;
                string str = TestUtil.RandomRealisticUnicodeString(Random());
                terms.Add(new Term(field, str));
                Document doc = new Document();
                doc.Add(NewStringField(field, str, Field.Store.NO));
                w.AddDocument(doc);
            }

            IndexReader reader = w.Reader;
            w.Dispose();
            IndexSearcher searcher = NewSearcher(reader);
            int numQueries = AtLeast(10);
            for (int i = 0; i < numQueries; i++)
            {
                Term term = terms[Random().nextInt(num)];
                TopDocs queryResult = searcher.Search(new TermQuery(term), reader.MaxDoc);
                MatchAllDocsQuery matchAll = new MatchAllDocsQuery();
                TermFilter filter = TermFilter(term);
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

        [Test]
        public void TestHashCodeAndEquals()
        {
            int num = AtLeast(100);
            for (int i = 0; i < num; i++)
            {
                string field1 = @"field" + i;
                string field2 = @"field" + i + num;
                string value1 = TestUtil.RandomRealisticUnicodeString(Random());
                string value2 = value1 + @"x";
                TermFilter filter1 = TermFilter(field1, value1);
                TermFilter filter2 = TermFilter(field1, value2);
                TermFilter filter3 = TermFilter(field2, value1);
                TermFilter filter4 = TermFilter(field2, value2);
                var filters = new TermFilter[]
                {
                    filter1, filter2, filter3, filter4
                };

                for (int j = 0; j < filters.Length; j++)
                {
                    TermFilter termFilter = filters[j];
                    for (int k = 0; k < filters.Length; k++)
                    {
                        TermFilter otherTermFilter = filters[k];
                        if (j == k)
                        {
                            assertEquals(termFilter, otherTermFilter);
                            assertEquals(termFilter.GetHashCode(), otherTermFilter.GetHashCode());
                            assertTrue(termFilter.Equals(otherTermFilter));
                        }
                        else
                        {
                            assertFalse(termFilter.Equals(otherTermFilter));
                        }
                    }
                }

                TermFilter filter5 = TermFilter(field2, value2);
                assertEquals(filter5, filter4);
                assertEquals(filter5.GetHashCode(), filter4.GetHashCode());
                assertTrue(filter5.Equals(filter4));
                assertEquals(filter5, filter4);
                assertTrue(filter5.Equals(filter4));
            }
        }

        [Test]
        public void TestNoTerms()
        {
            Assert.Throws<ArgumentException>(
                () => {  new TermFilter(null); },
                "must fail - no term!");

            Assert.Throws<ArgumentException>(
             () => { new TermFilter(new Term(null)); },
             "must fail - no field!");
        }

        [Test]
        public void TestToString()
        {
            var termsFilter = new TermFilter(new Term("field1", "a"));
            assertEquals(@"field1:a", termsFilter.ToString());
        }

        private TermFilter TermFilter(string field, string value)
        {
            return TermFilter(new Term(field, value));
        }

        private TermFilter TermFilter(Term term)
        {
            return new TermFilter(term);
        }
    }
}
