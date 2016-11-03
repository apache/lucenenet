using Lucene.Net.Support;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace Lucene.Net.Documents
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
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using Fields = Lucene.Net.Index.Fields;
    using IndexableField = Lucene.Net.Index.IndexableField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using Query = Lucene.Net.Search.Query;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Tests <seealso cref="Document"/> class.
    /// </summary>
    [TestFixture]
    public class TestDocument : LuceneTestCase
    {
        internal string BinaryVal = "this text will be stored as a byte array in the index";
        internal string BinaryVal2 = "this text will be also stored as a byte array in the index";

        [Test]
        public virtual void TestBinaryField()
        {
            Documents.Document doc = new Documents.Document();

            FieldType ft = new FieldType();
            ft.Stored = true;
            IndexableField stringFld = new Field("string", BinaryVal, ft);
            IndexableField binaryFld = new StoredField("binary", BinaryVal.GetBytes(Encoding.UTF8));
            IndexableField binaryFld2 = new StoredField("binary", BinaryVal2.GetBytes(Encoding.UTF8));

            doc.Add(stringFld);
            doc.Add(binaryFld);

            Assert.AreEqual(2, doc.Fields.Count);

            Assert.IsTrue(binaryFld.BinaryValue != null);
            Assert.IsTrue(binaryFld.FieldType.Stored);
            Assert.IsFalse(binaryFld.FieldType.Indexed);

            string binaryTest = doc.GetBinaryValue("binary").Utf8ToString();
            Assert.IsTrue(binaryTest.Equals(BinaryVal));

            string stringTest = doc.Get("string");
            Assert.IsTrue(binaryTest.Equals(stringTest));

            doc.Add(binaryFld2);

            Assert.AreEqual(3, doc.Fields.Count);

            BytesRef[] binaryTests = doc.GetBinaryValues("binary");

            Assert.AreEqual(2, binaryTests.Length);

            binaryTest = binaryTests[0].Utf8ToString();
            string binaryTest2 = binaryTests[1].Utf8ToString();

            Assert.IsFalse(binaryTest.Equals(binaryTest2));

            Assert.IsTrue(binaryTest.Equals(BinaryVal));
            Assert.IsTrue(binaryTest2.Equals(BinaryVal2));

            doc.RemoveField("string");
            Assert.AreEqual(2, doc.Fields.Count);

            doc.RemoveFields("binary");
            Assert.AreEqual(0, doc.Fields.Count);
        }

        /// <summary>
        /// Tests <seealso cref="Document#removeField(String)"/> method for a brand new Document
        /// that has not been indexed yet.
        /// </summary>
        /// <exception cref="Exception"> on error </exception>

        [Test]
        public virtual void TestRemoveForNewDocument()
        {
            Documents.Document doc = MakeDocumentWithFields();
            Assert.AreEqual(10, doc.Fields.Count);
            doc.RemoveFields("keyword");
            Assert.AreEqual(8, doc.Fields.Count);
            doc.RemoveFields("doesnotexists"); // removing non-existing fields is
            // siltenlty ignored
            doc.RemoveFields("keyword"); // removing a field more than once
            Assert.AreEqual(8, doc.Fields.Count);
            doc.RemoveFields("text");
            Assert.AreEqual(6, doc.Fields.Count);
            doc.RemoveFields("text");
            Assert.AreEqual(6, doc.Fields.Count);
            doc.RemoveFields("text");
            Assert.AreEqual(6, doc.Fields.Count);
            doc.RemoveFields("doesnotexists"); // removing non-existing fields is
            // siltenlty ignored
            Assert.AreEqual(6, doc.Fields.Count);
            doc.RemoveFields("unindexed");
            Assert.AreEqual(4, doc.Fields.Count);
            doc.RemoveFields("unstored");
            Assert.AreEqual(2, doc.Fields.Count);
            doc.RemoveFields("doesnotexists"); // removing non-existing fields is
            // siltenlty ignored
            Assert.AreEqual(2, doc.Fields.Count);

            doc.RemoveFields("indexed_not_tokenized");
            Assert.AreEqual(0, doc.Fields.Count);
        }

        [Test]
        public virtual void TestConstructorExceptions()
        {
            FieldType ft = new FieldType();
            ft.Stored = true;
            new Field("name", "value", ft); // okay
            new StringField("name", "value", Field.Store.NO); // okay
            try
            {
                new Field("name", "value", new FieldType());
                Assert.Fail();
            }
            catch (System.ArgumentException e)
            {
                // expected exception
            }
            new Field("name", "value", ft); // okay
            try
            {
                FieldType ft2 = new FieldType();
                ft2.Stored = true;
                ft2.StoreTermVectors = true;
                new Field("name", "value", ft2);
                Assert.Fail();
            }
            catch (System.ArgumentException e)
            {
                // expected exception
            }
        }

        /// <summary>
        /// Tests <seealso cref="Document#getValues(String)"/> method for a brand new Document
        /// that has not been indexed yet.
        /// </summary>
        /// <exception cref="Exception"> on error </exception>
        [Test]
        public virtual void TestGetValuesForNewDocument()
        {
            DoAssert(MakeDocumentWithFields(), false);
        }

        /// <summary>
        /// Tests <seealso cref="Document#getValues(String)"/> method for a Document retrieved
        /// from an index.
        /// </summary>
        /// <exception cref="Exception"> on error </exception>
        [Test]
        public virtual void TestGetValuesForIndexedDocument()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            writer.AddDocument(MakeDocumentWithFields());
            IndexReader reader = writer.Reader;

            IndexSearcher searcher = NewSearcher(reader);

            // search for something that does exists
            Query query = new TermQuery(new Term("keyword", "test1"));

            // ensure that queries return expected results without DateFilter first
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            DoAssert(searcher.Doc(hits[0].Doc), true);
            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestGetValues()
        {
            Documents.Document doc = MakeDocumentWithFields();
            Assert.AreEqual(new string[] { "test1", "test2" }, doc.GetValues("keyword"));
            Assert.AreEqual(new string[] { "test1", "test2" }, doc.GetValues("text"));
            Assert.AreEqual(new string[] { "test1", "test2" }, doc.GetValues("unindexed"));
            Assert.AreEqual(new string[0], doc.GetValues("nope"));
        }

        [Test]
        public virtual void TestPositionIncrementMultiFields()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            writer.AddDocument(MakeDocumentWithFields());
            IndexReader reader = writer.Reader;

            IndexSearcher searcher = NewSearcher(reader);
            PhraseQuery query = new PhraseQuery();
            query.Add(new Term("indexed_not_tokenized", "test1"));
            query.Add(new Term("indexed_not_tokenized", "test2"));

            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            DoAssert(searcher.Doc(hits[0].Doc), true);
            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        private Documents.Document MakeDocumentWithFields()
        {
            Documents.Document doc = new Documents.Document();
            FieldType stored = new FieldType();
            stored.Stored = true;
            FieldType indexedNotTokenized = new FieldType();
            indexedNotTokenized.Indexed = true;
            indexedNotTokenized.Tokenized = false;
            doc.Add(new StringField("keyword", "test1", Field.Store.YES));
            doc.Add(new StringField("keyword", "test2", Field.Store.YES));
            doc.Add(new TextField("text", "test1", Field.Store.YES));
            doc.Add(new TextField("text", "test2", Field.Store.YES));
            doc.Add(new Field("unindexed", "test1", stored));
            doc.Add(new Field("unindexed", "test2", stored));
            doc.Add(new TextField("unstored", "test1", Field.Store.NO));
            doc.Add(new TextField("unstored", "test2", Field.Store.NO));
            doc.Add(new Field("indexed_not_tokenized", "test1", indexedNotTokenized));
            doc.Add(new Field("indexed_not_tokenized", "test2", indexedNotTokenized));
            return doc;
        }

        private void DoAssert(Documents.Document doc, bool fromIndex)
        {
            IndexableField[] keywordFieldValues = doc.GetFields("keyword");
            IndexableField[] textFieldValues = doc.GetFields("text");
            IndexableField[] unindexedFieldValues = doc.GetFields("unindexed");
            IndexableField[] unstoredFieldValues = doc.GetFields("unstored");

            Assert.IsTrue(keywordFieldValues.Length == 2);
            Assert.IsTrue(textFieldValues.Length == 2);
            Assert.IsTrue(unindexedFieldValues.Length == 2);
            // this test cannot work for documents retrieved from the index
            // since unstored fields will obviously not be returned
            if (!fromIndex)
            {
                Assert.IsTrue(unstoredFieldValues.Length == 2);
            }

            Assert.IsTrue(keywordFieldValues[0].StringValue.Equals("test1"));
            Assert.IsTrue(keywordFieldValues[1].StringValue.Equals("test2"));
            Assert.IsTrue(textFieldValues[0].StringValue.Equals("test1"));
            Assert.IsTrue(textFieldValues[1].StringValue.Equals("test2"));
            Assert.IsTrue(unindexedFieldValues[0].StringValue.Equals("test1"));
            Assert.IsTrue(unindexedFieldValues[1].StringValue.Equals("test2"));
            // this test cannot work for documents retrieved from the index
            // since unstored fields will obviously not be returned
            if (!fromIndex)
            {
                Assert.IsTrue(unstoredFieldValues[0].StringValue.Equals("test1"));
                Assert.IsTrue(unstoredFieldValues[1].StringValue.Equals("test2"));
            }
        }

        [Test]
        public virtual void TestFieldSetValue()
        {
            Field field = new StringField("id", "id1", Field.Store.YES);
            Documents.Document doc = new Documents.Document();
            doc.Add(field);
            doc.Add(new StringField("keyword", "test", Field.Store.YES));

            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            writer.AddDocument(doc);
            field.StringValue = "id2";
            writer.AddDocument(doc);
            field.StringValue = "id3";
            writer.AddDocument(doc);

            IndexReader reader = writer.Reader;
            IndexSearcher searcher = NewSearcher(reader);

            Query query = new TermQuery(new Term("keyword", "test"));

            // ensure that queries return expected results without DateFilter first
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(3, hits.Length);
            int result = 0;
            for (int i = 0; i < 3; i++)
            {
                Documents.Document doc2 = searcher.Doc(hits[i].Doc);
                Field f = (Field)doc2.GetField("id");
                if (f.StringValue.Equals("id1"))
                {
                    result |= 1;
                }
                else if (f.StringValue.Equals("id2"))
                {
                    result |= 2;
                }
                else if (f.StringValue.Equals("id3"))
                {
                    result |= 4;
                }
                else
                {
                    Assert.Fail("unexpected id field");
                }
            }
            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
            Assert.AreEqual(7, result, "did not see all IDs");
        }

        // LUCENE-3616
        [Test]
        public virtual void TestInvalidFields()
        {
            Assert.Throws<System.ArgumentException>(() => { new Field("foo", new MockTokenizer(new StreamReader(File.Open("", FileMode.Open))), StringField.TYPE_STORED); });
        }

        // LUCENE-3682
        [Test]
        public virtual void TestTransitionAPI()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            Documents.Document doc = new Documents.Document();
            doc.Add(new Field("stored", "abc", Field.Store.YES, Field.Index.NO));
            doc.Add(new Field("stored_indexed", "abc xyz", Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("stored_tokenized", "abc xyz", Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("indexed", "abc xyz", Field.Store.NO, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("tokenized", "abc xyz", Field.Store.NO, Field.Index.ANALYZED));
            doc.Add(new Field("tokenized_reader", new StringReader("abc xyz")));
            doc.Add(new Field("tokenized_tokenstream", w.w.Analyzer.TokenStream("tokenized_tokenstream", new StringReader("abc xyz"))));
            doc.Add(new Field("binary", new byte[10]));
            doc.Add(new Field("tv", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.YES));
            doc.Add(new Field("tv_pos", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS));
            doc.Add(new Field("tv_off", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));
            doc.Add(new Field("tv_pos_off", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
            w.Dispose();

            doc = r.Document(0);
            // 4 stored fields
            Assert.AreEqual(4, doc.Fields.Count);
            Assert.AreEqual("abc", doc.Get("stored"));
            Assert.AreEqual("abc xyz", doc.Get("stored_indexed"));
            Assert.AreEqual("abc xyz", doc.Get("stored_tokenized"));
            BytesRef br = doc.GetBinaryValue("binary");
            Assert.IsNotNull(br);
            Assert.AreEqual(10, br.Length);

            IndexSearcher s = new IndexSearcher(r);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("stored_indexed", "abc xyz")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("stored_tokenized", "abc")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("stored_tokenized", "xyz")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("indexed", "abc xyz")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("tokenized", "abc")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("tokenized", "xyz")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("tokenized_reader", "abc")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("tokenized_reader", "xyz")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("tokenized_tokenstream", "abc")), 1).TotalHits);
            Assert.AreEqual(1, s.Search(new TermQuery(new Term("tokenized_tokenstream", "xyz")), 1).TotalHits);

            foreach (string field in new string[] { "tv", "tv_pos", "tv_off", "tv_pos_off" })
            {
                Fields tvFields = r.GetTermVectors(0);
                Terms tvs = tvFields.Terms(field);
                Assert.IsNotNull(tvs);
                Assert.AreEqual(2, tvs.Size());
                TermsEnum tvsEnum = tvs.Iterator(null);
                Assert.AreEqual(new BytesRef("abc"), tvsEnum.Next());
                DocsAndPositionsEnum dpEnum = tvsEnum.DocsAndPositions(null, null);
                if (field.Equals("tv"))
                {
                    Assert.IsNull(dpEnum);
                }
                else
                {
                    Assert.IsNotNull(dpEnum);
                }
                Assert.AreEqual(new BytesRef("xyz"), tvsEnum.Next());
                Assert.IsNull(tvsEnum.Next());
            }

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNumericFieldAsString()
        {
            Documents.Document doc = new Documents.Document();
            doc.Add(new IntField("int", 5, Field.Store.YES));
            Assert.AreEqual("5", doc.Get("int"));
            Assert.IsNull(doc.Get("somethingElse"));
            doc.Add(new IntField("int", 4, Field.Store.YES));
            Assert.AreEqual(new string[] { "5", "4" }, doc.GetValues("int"));

            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            iw.AddDocument(doc);
            DirectoryReader ir = iw.Reader;
            Documents.Document sdoc = ir.Document(0);
            Assert.AreEqual("5", sdoc.Get("int"));
            Assert.IsNull(sdoc.Get("somethingElse"));
            Assert.AreEqual(new string[] { "5", "4" }, sdoc.GetValues("int"));
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }
    }
}