using System;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries.Function
{
    /// <summary>
    /// Test that functionquery's getSortField() actually works.
    /// </summary>
    public class TestFunctionQuerySort : LuceneTestCase
    {
        [Test]
        public void TestSearchAfterWhenSortingByFunctionValues()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            iwc.SetMergePolicy(NewLogMergePolicy()); // depends on docid order
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);

            Document doc = new Document();
            Field field = new StringField("value", "", Field.Store.YES);
            doc.Add(field);

            // Save docs unsorted (decreasing value n, n-1, ...)
            const int NUM_VALS = 5;
            for (int val = NUM_VALS; val > 0; val--)
            {
                field.SetStringValue(Convert.ToString(val));
                writer.AddDocument(doc);
            }

            // Open index
            IndexReader reader = writer.Reader;
            writer.Dispose();
            IndexSearcher searcher = NewSearcher(reader);

            // Get ValueSource from FieldCache
            IntFieldSource src = new IntFieldSource("value");
            // ...and make it a sort criterion
            SortField sf = src.GetSortField(false).Rewrite(searcher);
            Sort orderBy = new Sort(sf);

            // Get hits sorted by our FunctionValues (ascending values)
            Query q = new MatchAllDocsQuery();
            TopDocs hits = searcher.Search(q, reader.MaxDoc, orderBy);
            assertEquals(NUM_VALS, hits.ScoreDocs.Length);
            // Verify that sorting works in general
            int i = 0;
            foreach (ScoreDoc hit in hits.ScoreDocs)
            {
                int valueFromDoc = Convert.ToInt32(reader.Document(hit.Doc).Get("value"));
                assertEquals(++i, valueFromDoc);
            }

            // Now get hits after hit #2 using IS.searchAfter()
            int afterIdx = 1;
            FieldDoc afterHit = (FieldDoc)hits.ScoreDocs[afterIdx];
            hits = searcher.SearchAfter(afterHit, q, reader.MaxDoc, orderBy);

            // Expected # of hits: NUM_VALS - 2
            assertEquals(NUM_VALS - (afterIdx + 1), hits.ScoreDocs.Length);

            // Verify that hits are actually "after"
            int afterValue = (int)((double?)afterHit.Fields[0]);
            foreach (ScoreDoc hit in hits.ScoreDocs)
            {
                int val = Convert.ToInt32(reader.Document(hit.Doc).Get("value"));
                assertTrue(afterValue <= val);
                assertFalse(hit.Doc == afterHit.Doc);
            }
            reader.Dispose();
            dir.Dispose();
        }
    }
}