using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using NUnit.Framework;
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

    using BinaryDocValuesField = BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using DoubleDocValuesField = DoubleDocValuesField;
    using Field = Field;
    using SingleDocValuesField = SingleDocValuesField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SortedDocValuesField = SortedDocValuesField;

    /// <summary>
    /// Tests basic sorting on docvalues fields.
    /// These are mostly like TestSort's tests, except each test
    /// indexes the field up-front as docvalues, and checks no fieldcaches were made
    /// </summary>
    [SuppressCodecs("Lucene3x", "Appending", "Lucene40", "Lucene41", "Lucene42")] // avoid codecs that don't support "missing"
    [TestFixture]
    public class TestSortDocValues : LuceneTestCase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // ensure there is nothing in fieldcache before test starts
            FieldCache.DEFAULT.PurgeAllCaches();
        }

        private void AssertNoFieldCaches()
        {
            // docvalues sorting should NOT create any fieldcache entries!
            Assert.AreEqual(0, FieldCache.DEFAULT.GetCacheEntries().Length);
        }

        /// <summary>
        /// Tests sorting on type string </summary>
        [Test]
        public virtual void TestString()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("value", new BytesRef("foo")));
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.STRING));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'bar' comes before 'foo'
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests reverse sorting on type string </summary>
        [Test]
        public virtual void TestStringReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("value", new BytesRef("foo")));
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.STRING, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'foo' comes after 'bar' in reverse order
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type string_val </summary>
        [Test]
        public virtual void TestStringVal()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new BinaryDocValuesField("value", new BytesRef("foo")));
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new BinaryDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.STRING_VAL));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'bar' comes before 'foo'
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests reverse sorting on type string_val </summary>
        [Test]
        public virtual void TestStringValReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new BinaryDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new BinaryDocValuesField("value", new BytesRef("foo")));
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.STRING_VAL, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'foo' comes after 'bar' in reverse order
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type string_val, but with a SortedDocValuesField </summary>
        [Test]
        public virtual void TestStringValSorted()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("value", new BytesRef("foo")));
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.STRING_VAL));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'bar' comes before 'foo'
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests reverse sorting on type string_val, but with a SortedDocValuesField </summary>
        [Test]
        public virtual void TestStringValReverseSorted()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SortedDocValuesField("value", new BytesRef("bar")));
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SortedDocValuesField("value", new BytesRef("foo")));
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.STRING_VAL, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'foo' comes after 'bar' in reverse order
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type byte </summary>
        [Test]
        public virtual void TestByte()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("value", 23));
            doc.Add(NewStringField("value", "23", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
#pragma warning disable 612, 618
            Sort sort = new Sort(new SortField("value", SortFieldType.BYTE));
#pragma warning restore 612, 618

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("23", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type byte in reverse </summary>
        [Test]
        public virtual void TestByteReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("value", 23));
            doc.Add(NewStringField("value", "23", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
#pragma warning disable 612, 618
            Sort sort = new Sort(new SortField("value", SortFieldType.BYTE, true));
#pragma warning restore 612, 618

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("23", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type short </summary>
        [Test]
        public virtual void TestShort()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("value", 300));
            doc.Add(NewStringField("value", "300", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
#pragma warning disable 612, 618
            Sort sort = new Sort(new SortField("value", SortFieldType.INT16));
#pragma warning restore 612, 618

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("300", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type short in reverse </summary>
        [Test]
        public virtual void TestShortReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("value", 300));
            doc.Add(NewStringField("value", "300", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
#pragma warning disable 612, 618
            Sort sort = new Sort(new SortField("value", SortFieldType.INT16, true));
#pragma warning restore 612, 618

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("300", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type int </summary>
        [Test]
        public virtual void TestInt()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("value", 300000));
            doc.Add(NewStringField("value", "300000", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.INT32));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("300000", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type int in reverse </summary>
        [Test]
        public virtual void TestIntReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("value", 300000));
            doc.Add(NewStringField("value", "300000", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.INT32, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("300000", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type int with a missing value </summary>
        [Test]
        public virtual void TestIntMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.INT32));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null is treated as a 0
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type int, specifying the missing value should be treated as Integer.MAX_VALUE </summary>
        [Test]
        public virtual void TestIntMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortFieldType.INT32);
            sortField.SetMissingValue(int.MaxValue);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null is treated as a Integer.MAX_VALUE
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type long </summary>
        [Test]
        public virtual void TestLong()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("value", 3000000000L));
            doc.Add(NewStringField("value", "3000000000", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.INT64));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("3000000000", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type long in reverse </summary>
        [Test]
        public virtual void TestLongReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new NumericDocValuesField("value", 3000000000L));
            doc.Add(NewStringField("value", "3000000000", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.INT64, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("3000000000", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type long with a missing value </summary>
        [Test]
        public virtual void TestLongMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.INT64));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null is treated as 0
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type long, specifying the missing value should be treated as Long.MAX_VALUE </summary>
        [Test]
        public virtual void TestLongMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", -1));
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new NumericDocValuesField("value", 4));
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortFieldType.INT64);
            sortField.SetMissingValue(long.MaxValue);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null is treated as Long.MAX_VALUE
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type float </summary>
        [Test]
        public virtual void TestFloat()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SingleDocValuesField("value", 30.1F));
            doc.Add(NewStringField("value", "30.1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", -1.3F));
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", 4.2F));
            doc.Add(NewStringField("value", "4.2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.SINGLE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("30.1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double with +/- zero </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestFloatSignedZero()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SingleDocValuesField("value", +0f));
            doc.Add(NewStringField("value", "+0", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", -0f));
            doc.Add(NewStringField("value", "-0", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.SINGLE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // numeric order
            Assert.AreEqual("-0", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("+0", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type float in reverse </summary>
        [Test]
        public virtual void TestFloatReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new SingleDocValuesField("value", 30.1F));
            doc.Add(NewStringField("value", "30.1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", -1.3F));
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", 4.2F));
            doc.Add(NewStringField("value", "4.2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.SINGLE, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("30.1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type float with a missing value </summary>
        [Test]
        public virtual void TestFloatMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", -1.3F));
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", 4.2F));
            doc.Add(NewStringField("value", "4.2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.SINGLE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null is treated as 0
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4.2", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type float, specifying the missing value should be treated as Float.MAX_VALUE </summary>
        [Test]
        public virtual void TestFloatMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", -1.3F));
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new SingleDocValuesField("value", 4.2F));
            doc.Add(NewStringField("value", "4.2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortFieldType.SINGLE);
            sortField.SetMissingValue(float.MaxValue);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null is treated as Float.MAX_VALUE
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double </summary>
        [Test]
        public virtual void TestDouble()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 30.1));
            doc.Add(NewStringField("value", "30.1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", -1.3));
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 4.2333333333333));
            doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 4.2333333333332));
            doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.DOUBLE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(4, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            Assert.AreEqual("30.1", searcher.Doc(td.ScoreDocs[3].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double with +/- zero </summary>
        [Test]
        public virtual void TestDoubleSignedZero()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new DoubleDocValuesField("value", +0D));
            doc.Add(NewStringField("value", "+0", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", -0D));
            doc.Add(NewStringField("value", "-0", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.DOUBLE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // numeric order
            Assert.AreEqual("-0", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("+0", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double in reverse </summary>
        [Test]
        public virtual void TestDoubleReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 30.1));
            doc.Add(NewStringField("value", "30.1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", -1.3));
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 4.2333333333333));
            doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 4.2333333333332));
            doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.DOUBLE, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(4, td.TotalHits);
            // numeric order
            Assert.AreEqual("30.1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[3].Doc).Get("value"));
            AssertNoFieldCaches();

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double with a missing value </summary>
        [Test]
        public virtual void TestDoubleMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", -1.3));
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 4.2333333333333));
            doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 4.2333333333332));
            doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortFieldType.DOUBLE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(4, td.TotalHits);
            // null treated as a 0
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            Assert.AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[3].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double, specifying the missing value should be treated as Double.MAX_VALUE </summary>
        [Test]
        public virtual void TestDoubleMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", -1.3));
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 4.2333333333333));
            doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new DoubleDocValuesField("value", 4.2333333333332));
            doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.GetReader();
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortFieldType.DOUBLE);
            sortField.SetMissingValue(double.MaxValue);
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(4, td.TotalHits);
            // null treated as Double.MAX_VALUE
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[3].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }
    }
}