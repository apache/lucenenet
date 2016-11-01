using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MultiReader = Lucene.Net.Index.MultiReader;
    using Occur = Lucene.Net.Search.BooleanClause.Occur;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /*
     * Very simple tests of sorting.
     *
     * THE RULES:
     * 1. keywords like 'abstract' and 'static' should not appear in this file.
     * 2. each test method should be self-contained and understandable.
     * 3. no test methods should share code with other test methods.
     * 4. no testing of things unrelated to sorting.
     * 5. no tracers.
     * 6. keyword 'class' should appear only once in this file, here ----
     *                                                                  |
     *        -----------------------------------------------------------
     *        |
     *       \./
     */

    [TestFixture]
    public class TestSort : LuceneTestCase
    {
        /// <summary>
        /// LUCENENET specific. Ensure we have an infostream attached to the default FieldCache
        /// when running the tests. In Java, this was done in the Core.Search.TestFieldCache.TestInfoStream() 
        /// method (which polluted the state of these tests), but we need to make the tests self-contained 
        /// so they can be run correctly regardless of order. Not setting the InfoStream skips an execution
        /// path within these tests, so we should do it to make sure we test all of the code.
        /// </summary>
        public override void SetUp()
        {
            base.SetUp();
            FieldCache.DEFAULT.InfoStream = new StringWriter();
        }

        /// <summary>
        /// LUCENENET specific. See <see cref="SetUp()"/>. Dispose our InfoStream and set it to null
        /// to avoid polluting the state of other tests.
        /// </summary>
        public override void TearDown()
        {
            FieldCache.DEFAULT.InfoStream.Dispose();
            FieldCache.DEFAULT.InfoStream = null;
            base.TearDown();
        }

        /// <summary>
        /// Tests sorting on type string </summary>
        [Test]
        public virtual void TestString()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.STRING));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'bar' comes before 'foo'
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type string with a missing value </summary>
        [Test]
        public virtual void TestStringMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.STRING));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null comes first
            Assert.IsNull(searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests reverse sorting on type string </summary>
        [Test]
        public virtual void TestStringReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.STRING, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'foo' comes after 'bar' in reverse order
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type string_val </summary>
        [Test]
        public virtual void TestStringVal()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.STRING_VAL));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'bar' comes before 'foo'
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type string_val with a missing value </summary>
        [Test]
        public virtual void TestStringValMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.STRING_VAL));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null comes first
            Assert.IsNull(searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type string with a missing
        ///  value sorted first
        /// </summary>
        [Test]
        public virtual void TestStringMissingSortedFirst()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sf = new SortField("value", SortField.Type_e.STRING);
            Sort sort = new Sort(sf);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null comes first
            Assert.IsNull(searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests reverse sorting on type string with a missing
        ///  value sorted first
        /// </summary>
        [Test]
        public virtual void TestStringMissingSortedFirstReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sf = new SortField("value", SortField.Type_e.STRING, true);
            Sort sort = new Sort(sf);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            // null comes last
            Assert.IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type string with a missing
        ///  value sorted last
        /// </summary>
        [Test]
        public virtual void TestStringValMissingSortedLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sf = new SortField("value", SortField.Type_e.STRING);
            sf.MissingValue = SortField.STRING_LAST;
            Sort sort = new Sort(sf);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            // null comes last
            Assert.IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests reverse sorting on type string with a missing
        ///  value sorted last
        /// </summary>
        [Test]
        public virtual void TestStringValMissingSortedLastReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sf = new SortField("value", SortField.Type_e.STRING, true);
            sf.MissingValue = SortField.STRING_LAST;
            Sort sort = new Sort(sf);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null comes first
            Assert.IsNull(searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests reverse sorting on type string_val </summary>
        [Test]
        public virtual void TestStringValReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.STRING_VAL, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'foo' comes after 'bar' in reverse order
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on internal docid order </summary>
        [Test]
        public virtual void TestFieldDoc()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.NO));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(SortField.FIELD_DOC);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // docid 0, then docid 1
            Assert.AreEqual(0, td.ScoreDocs[0].Doc);
            Assert.AreEqual(1, td.ScoreDocs[1].Doc);

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on reverse internal docid order </summary>
        [Test]
        public virtual void TestFieldDocReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.NO));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField(null, SortField.Type_e.DOC, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // docid 1, then docid 0
            Assert.AreEqual(1, td.ScoreDocs[0].Doc);
            Assert.AreEqual(0, td.ScoreDocs[1].Doc);

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests default sort (by score) </summary>
        [Test]
        public virtual void TestFieldScore()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewTextField("value", "foo bar bar bar bar", Field.Store.NO));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewTextField("value", "foo foo foo foo foo", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort();

            TopDocs actual = searcher.Search(new TermQuery(new Term("value", "foo")), 10, sort);
            Assert.AreEqual(2, actual.TotalHits);

            TopDocs expected = searcher.Search(new TermQuery(new Term("value", "foo")), 10);
            // the two topdocs should be the same
            Assert.AreEqual(expected.TotalHits, actual.TotalHits);
            for (int i = 0; i < actual.ScoreDocs.Length; i++)
            {
                Assert.AreEqual(actual.ScoreDocs[i].Doc, expected.ScoreDocs[i].Doc);
            }

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests default sort (by score) in reverse </summary>
        [Test]
        public virtual void TestFieldScoreReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewTextField("value", "foo bar bar bar bar", Field.Store.NO));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewTextField("value", "foo foo foo foo foo", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField(null, SortField.Type_e.SCORE, true));

            TopDocs actual = searcher.Search(new TermQuery(new Term("value", "foo")), 10, sort);
            Assert.AreEqual(2, actual.TotalHits);

            TopDocs expected = searcher.Search(new TermQuery(new Term("value", "foo")), 10);
            // the two topdocs should be the reverse of each other
            Assert.AreEqual(expected.TotalHits, actual.TotalHits);
            Assert.AreEqual(actual.ScoreDocs[0].Doc, expected.ScoreDocs[1].Doc);
            Assert.AreEqual(actual.ScoreDocs[1].Doc, expected.ScoreDocs[0].Doc);

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type byte </summary>
        [Test]
        public virtual void TestByte()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "23", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.BYTE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("23", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type byte with a missing value </summary>
        [Test]
        public virtual void TestByteMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.BYTE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null value is treated as a 0
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type byte, specifying the missing value should be treated as Byte.MAX_VALUE </summary>
        [Test]
        public virtual void TestByteMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortField.Type_e.BYTE);
            sortField.MissingValue = sbyte.MaxValue;
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null value is treated Byte.MAX_VALUE
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type byte in reverse </summary>
        [Test]
        public virtual void TestByteReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "23", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.BYTE, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("23", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type short </summary>
        [Test]
        public virtual void TestShort()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "300", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.SHORT));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("300", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type short with a missing value </summary>
        [Test]
        public virtual void TestShortMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.SHORT));

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
        /// Tests sorting on type short, specifying the missing value should be treated as Short.MAX_VALUE </summary>
        [Test]
        public virtual void TestShortMissingLast()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortField.Type_e.SHORT);
            sortField.MissingValue = short.MaxValue;
            Sort sort = new Sort(sortField);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // null is treated as Short.MAX_VALUE
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.IsNull(searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type short in reverse </summary>
        [Test]
        public virtual void TestShortReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "300", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.SHORT, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("300", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type int </summary>
        [Test]
        public virtual void TestInt()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "300000", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.INT));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("300000", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type int with a missing value </summary>
        [Test]
        public virtual void TestIntMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.INT));

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
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortField.Type_e.INT);
            sortField.MissingValue = int.MaxValue;
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
        /// Tests sorting on type int in reverse </summary>
        [Test]
        public virtual void TestIntReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "300000", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.INT, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("300000", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type long </summary>
        [Test]
        public virtual void TestLong()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "3000000000", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.LONG));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("3000000000", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type long with a missing value </summary>
        [Test]
        public virtual void TestLongMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.LONG));

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
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortField.Type_e.LONG);
            sortField.MissingValue = long.MaxValue;
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
        /// Tests sorting on type long in reverse </summary>
        [Test]
        public virtual void TestLongReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "3000000000", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.LONG, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("3000000000", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type float </summary>
        [Test]
        public virtual void TestFloat()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "30.1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.FLOAT));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("30.1", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type float with a missing value </summary>
        [Test]
        public virtual void TestFloatMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.FLOAT));

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
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortField.Type_e.FLOAT);
            sortField.MissingValue = float.MaxValue;
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
        /// Tests sorting on type float in reverse </summary>
        [Test]
        public virtual void TestFloatReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "30.1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.FLOAT, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(3, td.TotalHits);
            // reverse numeric order
            Assert.AreEqual("30.1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double </summary>
        [Test]
        public virtual void TestDouble()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "30.1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.DOUBLE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(4, td.TotalHits);
            // numeric order
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            Assert.AreEqual("30.1", searcher.Doc(td.ScoreDocs[3].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double with +/- zero </summary>
        [Test]
        public virtual void TestDoubleSignedZero()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "+0", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-0", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.DOUBLE));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // numeric order
            Assert.AreEqual("0", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("0", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting on type double with a missing value </summary>
        [Test]
        public virtual void TestDoubleMissing()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.DOUBLE));

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
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            SortField sortField = new SortField("value", SortField.Type_e.DOUBLE);
            sortField.MissingValue = double.MaxValue;
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

        /// <summary>
        /// Tests sorting on type double in reverse </summary>
        [Test]
        public virtual void TestDoubleReverse()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "30.1", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "-1.3", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2333333333333", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "4.2333333333332", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.DOUBLE, true));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(4, td.TotalHits);
            // numeric order
            Assert.AreEqual("30.1", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("4.2333333333333", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));
            Assert.AreEqual("4.2333333333332", searcher.Doc(td.ScoreDocs[2].Doc).Get("value"));
            Assert.AreEqual("-1.3", searcher.Doc(td.ScoreDocs[3].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestEmptyStringVsNullStringSort()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            Document doc = new Document();
            doc.Add(NewStringField("f", "", Field.Store.NO));
            doc.Add(NewStringField("t", "1", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();
            doc = new Document();
            doc.Add(NewStringField("t", "1", Field.Store.NO));
            w.AddDocument(doc);

            IndexReader r = DirectoryReader.Open(w, true);
            w.Dispose();
            IndexSearcher s = NewSearcher(r);
            TopDocs hits = s.Search(new TermQuery(new Term("t", "1")), null, 10, new Sort(new SortField("f", SortField.Type_e.STRING)));
            Assert.AreEqual(2, hits.TotalHits);
            // null sorts first
            Assert.AreEqual(1, hits.ScoreDocs[0].Doc);
            Assert.AreEqual(0, hits.ScoreDocs[1].Doc);
            r.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// test that we don't throw exception on multi-valued field (LUCENE-2142) </summary>
        [Test]
        public virtual void TestMultiValuedField()
        {
            Directory indexStore = NewDirectory();
            IndexWriter writer = new IndexWriter(indexStore, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            for (int i = 0; i < 5; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("string", "a" + i, Field.Store.NO));
                doc.Add(new StringField("string", "b" + i, Field.Store.NO));
                writer.AddDocument(doc);
            }
            writer.ForceMerge(1); // enforce one segment to have a higher unique term count in all cases
            writer.Dispose();
            Sort sort = new Sort(new SortField("string", SortField.Type_e.STRING), SortField.FIELD_DOC);
            // this should not throw AIOOBE or RuntimeEx
            IndexReader reader = DirectoryReader.Open(indexStore);
            IndexSearcher searcher = NewSearcher(reader);
            searcher.Search(new MatchAllDocsQuery(), null, 500, sort);
            reader.Dispose();
            indexStore.Dispose();
        }

        [Test]
        public virtual void TestMaxScore()
        {
            Directory d = NewDirectory();
            // Not RIW because we need exactly 2 segs:
            IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            int id = 0;
            for (int seg = 0; seg < 2; seg++)
            {
                for (int docIDX = 0; docIDX < 10; docIDX++)
                {
                    Document doc = new Document();
                    doc.Add(NewStringField("id", "" + docIDX, Field.Store.YES));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < id; i++)
                    {
                        sb.Append(' ');
                        sb.Append("text");
                    }
                    doc.Add(NewTextField("body", sb.ToString(), Field.Store.NO));
                    w.AddDocument(doc);
                    id++;
                }
                w.Commit();
            }

            IndexReader r = DirectoryReader.Open(w, true);
            w.Dispose();
            Query q = new TermQuery(new Term("body", "text"));
            IndexSearcher s = NewSearcher(r);
            float maxScore = s.Search(q, 10).MaxScore;
            Assert.AreEqual(maxScore, s.Search(q, null, 3, Sort.INDEXORDER, Random().NextBoolean(), true).MaxScore, 0.0);
            Assert.AreEqual(maxScore, s.Search(q, null, 3, Sort.RELEVANCE, Random().NextBoolean(), true).MaxScore, 0.0);
            Assert.AreEqual(maxScore, s.Search(q, null, 3, new Sort(new SortField[] { new SortField("id", SortField.Type_e.INT, false) }), Random().NextBoolean(), true).MaxScore, 0.0);
            Assert.AreEqual(maxScore, s.Search(q, null, 3, new Sort(new SortField[] { new SortField("id", SortField.Type_e.INT, true) }), Random().NextBoolean(), true).MaxScore, 0.0);
            r.Dispose();
            d.Dispose();
        }

        /// <summary>
        /// test sorts when there's nothing in the index </summary>
        [Test]
        public virtual void TestEmptyIndex()
        {
            IndexSearcher empty = NewSearcher(new MultiReader());
            Query query = new TermQuery(new Term("contents", "foo"));

            Sort sort = new Sort();
            TopDocs td = empty.Search(query, null, 10, sort, true, true);
            Assert.AreEqual(0, td.TotalHits);

            sort.SetSort(SortField.FIELD_DOC);
            td = empty.Search(query, null, 10, sort, true, true);
            Assert.AreEqual(0, td.TotalHits);

            sort.SetSort(new SortField("int", SortField.Type_e.INT), SortField.FIELD_DOC);
            td = empty.Search(query, null, 10, sort, true, true);
            Assert.AreEqual(0, td.TotalHits);

            sort.SetSort(new SortField("string", SortField.Type_e.STRING, true), SortField.FIELD_DOC);
            td = empty.Search(query, null, 10, sort, true, true);
            Assert.AreEqual(0, td.TotalHits);

            sort.SetSort(new SortField("string_val", SortField.Type_e.STRING_VAL, true), SortField.FIELD_DOC);
            td = empty.Search(query, null, 10, sort, true, true);
            Assert.AreEqual(0, td.TotalHits);

            sort.SetSort(new SortField("float", SortField.Type_e.FLOAT), new SortField("string", SortField.Type_e.STRING));
            td = empty.Search(query, null, 10, sort, true, true);
            Assert.AreEqual(0, td.TotalHits);
        }

        /// <summary>
        /// test sorts for a custom int parser that uses a simple char encoding
        /// </summary>
        [Test]
        public virtual void TestCustomIntParser()
        {
            List<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" });
            letters = (List<string>)CollectionsHelper.Shuffle(letters);

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            foreach (string letter in letters)
            {
                Document doc = new Document();
                doc.Add(NewStringField("parser", letter, Field.Store.YES));
                iw.AddDocument(doc);
            }

            IndexReader ir = iw.Reader;
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("parser", new IntParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);

            // results should be in alphabetical order
            Assert.AreEqual(10, td.TotalHits);
            letters.Sort();
            for (int i = 0; i < letters.Count; i++)
            {
                Assert.AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get("parser"));
            }

            ir.Dispose();
            dir.Dispose();
        }

        private class IntParserAnonymousInnerClassHelper : FieldCache.IIntParser
        {
            private readonly TestSort OuterInstance;

            public IntParserAnonymousInnerClassHelper(TestSort outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public int ParseInt(BytesRef term)
            {
                return (term.Bytes[term.Offset] - 'A') * 123456;
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }
        }

        /// <summary>
        /// test sorts for a custom byte parser that uses a simple char encoding
        /// </summary>
        [Test]
        public virtual void TestCustomByteParser()
        {
            List<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" });
            letters = (List<string>)CollectionsHelper.Shuffle(letters);

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            foreach (string letter in letters)
            {
                Document doc = new Document();
                doc.Add(NewStringField("parser", letter, Field.Store.YES));
                iw.AddDocument(doc);
            }

            IndexReader ir = iw.Reader;
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("parser", new ByteParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);

            // results should be in alphabetical order
            Assert.AreEqual(10, td.TotalHits);
            letters.Sort();
            for (int i = 0; i < letters.Count; i++)
            {
                Assert.AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get("parser"));
            }

            ir.Dispose();
            dir.Dispose();
        }

        private class ByteParserAnonymousInnerClassHelper : FieldCache.IByteParser
        {
            private readonly TestSort OuterInstance;

            public ByteParserAnonymousInnerClassHelper(TestSort outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public sbyte ParseByte(BytesRef term)
            {
                return (sbyte)(term.Bytes[term.Offset] - 'A');
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }
        }

        /// <summary>
        /// test sorts for a custom short parser that uses a simple char encoding
        /// </summary>
        [Test]
        public virtual void TestCustomShortParser()
        {
            List<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" });
            letters = (List<string>)CollectionsHelper.Shuffle(letters);

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            foreach (string letter in letters)
            {
                Document doc = new Document();
                doc.Add(NewStringField("parser", letter, Field.Store.YES));
                iw.AddDocument(doc);
            }

            IndexReader ir = iw.Reader;
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("parser", new ShortParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);

            // results should be in alphabetical order
            Assert.AreEqual(10, td.TotalHits);
            letters.Sort();
            for (int i = 0; i < letters.Count; i++)
            {
                Assert.AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get("parser"));
            }

            ir.Dispose();
            dir.Dispose();
        }

        private class ShortParserAnonymousInnerClassHelper : FieldCache.IShortParser
        {
            private readonly TestSort OuterInstance;

            public ShortParserAnonymousInnerClassHelper(TestSort outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public short ParseShort(BytesRef term)
            {
                return (short)(term.Bytes[term.Offset] - 'A');
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }
        }

        /// <summary>
        /// test sorts for a custom long parser that uses a simple char encoding
        /// </summary>
        [Test]
        public virtual void TestCustomLongParser()
        {
            List<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" });
            letters = (List<string>)CollectionsHelper.Shuffle(letters);

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            foreach (string letter in letters)
            {
                Document doc = new Document();
                doc.Add(NewStringField("parser", letter, Field.Store.YES));
                iw.AddDocument(doc);
            }

            IndexReader ir = iw.Reader;
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("parser", new LongParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);

            // results should be in alphabetical order
            Assert.AreEqual(10, td.TotalHits);
            letters.Sort();
            for (int i = 0; i < letters.Count; i++)
            {
                Assert.AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get("parser"));
            }

            ir.Dispose();
            dir.Dispose();
        }

        private class LongParserAnonymousInnerClassHelper : FieldCache.ILongParser
        {
            private readonly TestSort OuterInstance;

            public LongParserAnonymousInnerClassHelper(TestSort outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public long ParseLong(BytesRef term)
            {
                return (term.Bytes[term.Offset] - 'A') * 1234567890L;
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }
        }

        /// <summary>
        /// test sorts for a custom float parser that uses a simple char encoding
        /// </summary>
        [Test]
        public virtual void TestCustomFloatParser()
        {
            List<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" });
            letters = (List<string>)CollectionsHelper.Shuffle(letters);

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            foreach (string letter in letters)
            {
                Document doc = new Document();
                doc.Add(NewStringField("parser", letter, Field.Store.YES));
                iw.AddDocument(doc);
            }

            IndexReader ir = iw.Reader;
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("parser", new FloatParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);

            // results should be in alphabetical order
            Assert.AreEqual(10, td.TotalHits);
            letters.Sort();
            for (int i = 0; i < letters.Count; i++)
            {
                Assert.AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get("parser"));
            }

            ir.Dispose();
            dir.Dispose();
        }

        private class FloatParserAnonymousInnerClassHelper : FieldCache.IFloatParser
        {
            private readonly TestSort OuterInstance;

            public FloatParserAnonymousInnerClassHelper(TestSort outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public float ParseFloat(BytesRef term)
            {
                return (float)Math.Sqrt(term.Bytes[term.Offset]);
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }
        }

        /// <summary>
        /// test sorts for a custom double parser that uses a simple char encoding
        /// </summary>
        [Test]
        public virtual void TestCustomDoubleParser()
        {
            List<string> letters = Arrays.AsList(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" });
            letters = (List<string>)CollectionsHelper.Shuffle(letters);

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            foreach (string letter in letters)
            {
                Document doc = new Document();
                doc.Add(NewStringField("parser", letter, Field.Store.YES));
                iw.AddDocument(doc);
            }

            IndexReader ir = iw.Reader;
            iw.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("parser", new DoubleParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);

            // results should be in alphabetical order
            Assert.AreEqual(10, td.TotalHits);
            letters.Sort();
            for (int i = 0; i < letters.Count; i++)
            {
                Assert.AreEqual(letters[i], searcher.Doc(td.ScoreDocs[i].Doc).Get("parser"));
            }

            ir.Dispose();
            dir.Dispose();
        }

        private class DoubleParserAnonymousInnerClassHelper : FieldCache.IDoubleParser
        {
            private readonly TestSort OuterInstance;

            public DoubleParserAnonymousInnerClassHelper(TestSort outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public double ParseDouble(BytesRef term)
            {
                return Math.Pow(term.Bytes[term.Offset], (term.Bytes[term.Offset] - 'A'));
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }
        }

        /// <summary>
        /// Tests sorting a single document </summary>
        [Test]
        public virtual void TestSortOneDocument()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.STRING));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(1, td.TotalHits);
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting a single document with scores </summary>
        [Test]
        public virtual void TestSortOneDocumentWithScores()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(new SortField("value", SortField.Type_e.STRING));

            TopDocs expected = searcher.Search(new TermQuery(new Term("value", "foo")), 10);
            Assert.AreEqual(1, expected.TotalHits);
            TopDocs actual = searcher.Search(new TermQuery(new Term("value", "foo")), null, 10, sort, true, true);

            Assert.AreEqual(expected.TotalHits, actual.TotalHits);
            Assert.AreEqual(expected.ScoreDocs[0].Score, actual.ScoreDocs[0].Score, 0F);

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Tests sorting with two fields </summary>
        [Test]
        public virtual void TestSortTwoFields()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("tievalue", "tied", Field.Store.NO));
            doc.Add(NewStringField("value", "foo", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("tievalue", "tied", Field.Store.NO));
            doc.Add(NewStringField("value", "bar", Field.Store.YES));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            // tievalue, then value
            Sort sort = new Sort(new SortField("tievalue", SortField.Type_e.STRING), new SortField("value", SortField.Type_e.STRING));

            TopDocs td = searcher.Search(new MatchAllDocsQuery(), 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            // 'bar' comes before 'foo'
            Assert.AreEqual("bar", searcher.Doc(td.ScoreDocs[0].Doc).Get("value"));
            Assert.AreEqual("foo", searcher.Doc(td.ScoreDocs[1].Doc).Get("value"));

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestScore()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewStringField("value", "bar", Field.Store.NO));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("value", "foo", Field.Store.NO));
            writer.AddDocument(doc);
            IndexReader ir = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(ir);
            Sort sort = new Sort(SortField.FIELD_SCORE);

            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("value", "foo")), Occur.SHOULD);
            bq.Add(new MatchAllDocsQuery(), Occur.SHOULD);
            TopDocs td = searcher.Search(bq, 10, sort);
            Assert.AreEqual(2, td.TotalHits);
            Assert.AreEqual(1, td.ScoreDocs[0].Doc);
            Assert.AreEqual(0, td.ScoreDocs[1].Doc);

            ir.Dispose();
            dir.Dispose();
        }
    }
}