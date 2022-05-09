using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

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

    /// <summary>Simple tests for SortedSetSortField, indexing the sortedset up front</summary>
    [SuppressCodecs("Lucene40", "Lucene41", "Appending", "Lucene3x")]// avoid codecs that don't support sortedset
    public class TestSortedSetSortFieldDocValues : LuceneTestCase
    {
        public override void SetUp()
        {
            base.SetUp();
            // ensure there is nothing in fieldcache before test starts
            FieldCache.DEFAULT.PurgeAllCaches();
        }

        private void assertNoFieldCaches()
        {
            // docvalues sorting should NOT create any fieldcache entries!
            assertEquals(0, FieldCache.DEFAULT.GetCacheEntries().Length);
        }

        [Test]
        public void TestForward()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("baz")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("foo")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortedSetSortField("value", false));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'bar' comes before 'baz'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("foo")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("baz")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);

            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortedSetSortField("value", true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'bar' comes before 'baz'
            assertEquals("2", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMissingFirst()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("baz")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("foo")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortedSetSortField("value", false);
            sortField.SetMissingValue(SortField.STRING_FIRST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // 'bar' comes before 'baz'
            // null comes first
            assertEquals("3", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("baz")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("foo")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortedSetSortField("value", false);
            sortField.SetMissingValue(SortField.STRING_LAST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // 'bar' comes before 'baz'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            // null comes last
            assertEquals("3", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestSingleton()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("baz")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortedSetSortField("value", false));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'bar' comes before 'baz'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }
    }
}
