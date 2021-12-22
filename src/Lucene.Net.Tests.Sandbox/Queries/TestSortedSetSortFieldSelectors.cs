using Lucene.Net.Codecs;
using Lucene.Net.Codecs.DiskDV;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Codecs.Memory;
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

    /// <summary>
    /// Tests for SortedSetSortField selectors other than MIN,
    /// these require optional codec support (random access to ordinals)
    /// </summary>
    public class TestSortedSetSortFieldSelectors : LuceneTestCase
    {
        static Codec savedCodec;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            savedCodec = Codec.Default;
            // currently only these codecs that support random access ordinals
            int victim = Random.nextInt(3);
            switch (victim)
            {
                case 0: Codec.Default = (TestUtil.AlwaysDocValuesFormat(new DirectDocValuesFormat())); break;
                case 1: Codec.Default = (TestUtil.AlwaysDocValuesFormat(new DiskDocValuesFormat())); break;
                default: Codec.Default = (TestUtil.AlwaysDocValuesFormat(new Lucene45DocValuesFormat())); break;
            }
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            Codec.Default = (savedCodec);

            base.AfterClass();
        }

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
        public void TestMax()
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

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);

            Sort sort = new Sort(new SortedSetSortField("value", false, Selector.MAX));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'baz' comes before 'foo'
            assertEquals("2", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMaxReverse()
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

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);

            Sort sort = new Sort(new SortedSetSortField("value", true, Selector.MAX));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'baz' comes before 'foo'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMaxMissingFirst()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("foo")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("baz")));
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);

            SortField sortField = new SortedSetSortField("value", false, Selector.MAX);
            sortField.SetMissingValue(SortField.STRING_FIRST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // null comes first
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            // 'baz' comes before 'foo'
            assertEquals("3", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMaxMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("foo")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("baz")));
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);

            SortField sortField = new SortedSetSortField("value", false, Selector.MAX);
            sortField.SetMissingValue(SortField.STRING_LAST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // 'baz' comes before 'foo'
            assertEquals("3", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            // null comes last
            assertEquals("1", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMaxSingleton()
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

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            Sort sort = new Sort(new SortedSetSortField("value", false, Selector.MAX));

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
        public void TestMiddleMin()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("a")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("d")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            Sort sort = new Sort(new SortedSetSortField("value", false, Selector.MIDDLE_MIN));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'b' comes before 'c'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMiddleMinReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("a")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("d")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            Sort sort = new Sort(new SortedSetSortField("value", true, Selector.MIDDLE_MIN));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'b' comes before 'c'
            assertEquals("2", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMiddleMinMissingFirst()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("a")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("d")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            SortField sortField = new SortedSetSortField("value", false, Selector.MIDDLE_MIN);
            sortField.SetMissingValue(SortField.STRING_FIRST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // null comes first
            assertEquals("3", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            // 'b' comes before 'c'
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMiddleMinMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("a")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("d")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            SortField sortField = new SortedSetSortField("value", false, Selector.MIDDLE_MIN);
            sortField.SetMissingValue(SortField.STRING_LAST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // 'b' comes before 'c'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            // null comes last
            assertEquals("3", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMiddleMinSingleton()
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

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            Sort sort = new Sort(new SortedSetSortField("value", false, Selector.MIDDLE_MIN));

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
        public void TestMiddleMax()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("a")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("d")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            Sort sort = new Sort(new SortedSetSortField("value", false, Selector.MIDDLE_MAX));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'b' comes before 'c'
            assertEquals("2", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMiddleMaxReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("a")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("d")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            Sort sort = new Sort(new SortedSetSortField("value", true, Selector.MIDDLE_MAX));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(2, td.TotalHits);
            // 'b' comes before 'c'
            assertEquals("1", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMiddleMaxMissingFirst()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("a")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("d")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            SortField sortField = new SortedSetSortField("value", false, Selector.MIDDLE_MAX);
            sortField.SetMissingValue(SortField.STRING_FIRST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // null comes first
            assertEquals("3", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            // 'b' comes before 'c'
            assertEquals("2", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMiddleMaxMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("id", "3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("a")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("c")));
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("d")));
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedSetDocValuesField("value", new BytesRef("b")));
            doc.Add(NewStringField("id", "2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            SortField sortField = new SortedSetSortField("value", false, Selector.MIDDLE_MAX);
            sortField.SetMissingValue(SortField.STRING_LAST);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            assertEquals(3, td.TotalHits);
            // 'b' comes before 'c'
            assertEquals("2", searcher.Doc(td.ScoreDocs[0].Doc).Get("id"));
            assertEquals("1", searcher.Doc(td.ScoreDocs[1].Doc).Get("id"));
            // null comes last
            assertEquals("3", searcher.Doc(td.ScoreDocs[2].Doc).Get("id"));
            assertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestMiddleMaxSingleton()
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

            // slow wrapper does not support random access ordinals (there is no need for that!)
            IndexSearcher searcher = NewSearcher(ir, false);
            Sort sort = new Sort(new SortedSetSortField("value", false, Selector.MIDDLE_MAX));

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
