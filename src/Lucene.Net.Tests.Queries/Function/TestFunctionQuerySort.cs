// Lucene version compatibility level 4.8.1
using System;
using System.Globalization;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries.Function
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
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);

            Document doc = new Document();
            Field field = new StringField("value", "", Field.Store.YES);
            doc.Add(field);

            // Save docs unsorted (decreasing value n, n-1, ...)
            const int NUM_VALS = 5;
            for (int val = NUM_VALS; val > 0; val--)
            {
                field.SetStringValue(Convert.ToString(val, CultureInfo.InvariantCulture));
                writer.AddDocument(doc);
            }

            // Open index
            IndexReader reader = writer.GetReader();
            writer.Dispose();
            IndexSearcher searcher = NewSearcher(reader);

            // Get ValueSource from FieldCache
            Int32FieldSource src = new Int32FieldSource("value");
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
            int afterValue = ((J2N.Numerics.Double)afterHit.Fields[0]).ToInt32();
            foreach (ScoreDoc hit in hits.ScoreDocs)
            {
                int val = Convert.ToInt32(reader.Document(hit.Doc).Get("value"), CultureInfo.InvariantCulture);
                assertTrue(afterValue <= val);
                assertFalse(hit.Doc == afterHit.Doc);
            }
            reader.Dispose();
            dir.Dispose();
        }
    }
}
