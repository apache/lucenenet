using J2N.Globalization;
using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Attributes;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using Directory = Lucene.Net.Store.Directory;

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
    using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using Token = Lucene.Net.Analysis.Token;

    // sanity check some basics of fields
    [TestFixture]
    public class TestField : LuceneTestCase
    {
        [Test]
        public virtual void TestDoubleField()
        {
            Field[] fields = new Field[] { new DoubleField("foo", 5d, Field.Store.NO), new DoubleField("foo", 5d, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                field.SetDoubleValue(6d); // ok
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(6d, field.GetDoubleValue().Value, 0.0d);
            }
        }

        [Test]
        public virtual void TestDoubleDocValuesField()
        {
            DoubleDocValuesField field = new DoubleDocValuesField("foo", 5d);

            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            field.SetDoubleValue(6d); // ok
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(6d, J2N.BitConversion.Int64BitsToDouble(field.GetInt64Value().Value), 0.0d);
        }

        [Test]
        public virtual void TestFloatDocValuesField()
        {
            SingleDocValuesField field = new SingleDocValuesField("foo", 5f);

            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            field.SetSingleValue(6f); // ok
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(6f, J2N.BitConversion.Int32BitsToSingle(field.GetInt32Value().Value), 0.0f);
        }

        [Test]
        public virtual void TestFloatField()
        {
            Field[] fields = new Field[] { new SingleField("foo", 5f, Field.Store.NO), new SingleField("foo", 5f, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                field.SetSingleValue(6f); // ok
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(6f, field.GetSingleValue().Value, 0.0f);
            }
        }

        [Test]
        public virtual void TestIntField()
        {
            Field[] fields = new Field[] { new Int32Field("foo", 5, Field.Store.NO), new Int32Field("foo", 5, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                field.SetInt32Value(6); // ok
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(6, field.GetInt32Value().Value);
            }
        }

        [Test]
        public virtual void TestNumericDocValuesField()
        {
            NumericDocValuesField field = new NumericDocValuesField("foo", 5L);

            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            field.SetInt64Value(6); // ok
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(6L, field.GetInt64Value().Value);
        }

        [Test]
        public virtual void TestLongField()
        {
            Field[] fields = new Field[] { new Int64Field("foo", 5L, Field.Store.NO), new Int64Field("foo", 5L, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                field.SetInt64Value(6); // ok
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(6L, field.GetInt64Value().Value);
            }
        }

        [Test]
        public virtual void TestSortedBytesDocValuesField()
        {
            SortedDocValuesField field = new SortedDocValuesField("foo", new BytesRef("bar"));

            TrySetBoost(field);
            TrySetByteValue(field);
            field.SetBytesValue("fubar".GetBytes(Encoding.UTF8));
            field.SetBytesValue(new BytesRef("baz"));
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(new BytesRef("baz"), field.GetBinaryValue());
        }

        [Test]
        public virtual void TestBinaryDocValuesField()
        {
            BinaryDocValuesField field = new BinaryDocValuesField("foo", new BytesRef("bar"));

            TrySetBoost(field);
            TrySetByteValue(field);
            field.SetBytesValue("fubar".GetBytes(Encoding.UTF8));
            field.SetBytesValue(new BytesRef("baz"));
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(new BytesRef("baz"), field.GetBinaryValue());
        }

        [Test]
        public virtual void TestStringField()
        {
            Field[] fields = new Field[] { new StringField("foo", "bar", Field.Store.NO), new StringField("foo", "bar", Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                field.SetStringValue("baz");
                TrySetTokenStreamValue(field);

                Assert.AreEqual("baz", field.GetStringValue());
            }
        }

        [Test]
        public virtual void TestTextFieldString()
        {
            Field[] fields = new Field[] { new TextField("foo", "bar", Field.Store.NO), new TextField("foo", "bar", Field.Store.YES) };

            foreach (Field field in fields)
            {
                field.Boost = 5f;
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                field.SetStringValue("baz");
                field.SetTokenStream(new CannedTokenStream(new Token("foo", 0, 3)));

                Assert.AreEqual("baz", field.GetStringValue());
                Assert.AreEqual(5f, field.Boost, 0f);
            }
        }

        [Test]
        public virtual void TestTextFieldReader()
        {
            Field field = new TextField("foo", new StringReader("bar"));

            field.Boost = 5f;
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            field.SetReaderValue(new StringReader("foobar"));
            TrySetShortValue(field);
            TrySetStringValue(field);
            field.SetTokenStream(new CannedTokenStream(new Token("foo", 0, 3)));

            Assert.IsNotNull(field.GetReaderValue());
            Assert.AreEqual(5f, field.Boost, 0f);
        }

        /* TODO: this is pretty expert and crazy
         * see if we can fix it up later
        public void testTextFieldTokenStream() throws Exception {
        }
        */

        [Test]
        public virtual void TestStoredFieldBytes()
        {
            Field[] fields = new Field[] { new StoredField("foo", "bar".GetBytes(Encoding.UTF8)), new StoredField("foo", "bar".GetBytes(Encoding.UTF8), 0, 3), new StoredField("foo", new BytesRef("bar")) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                field.SetBytesValue("baz".GetBytes(Encoding.UTF8));
                field.SetBytesValue(new BytesRef("baz"));
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(new BytesRef("baz"), field.GetBinaryValue());
            }
        }

        [Test]
        public virtual void TestStoredFieldString()
        {
            Field field = new StoredField("foo", "bar");
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            field.SetStringValue("baz");
            TrySetTokenStreamValue(field);

            Assert.AreEqual("baz", field.GetStringValue());
        }

        [Test]
        public virtual void TestStoredFieldInt()
        {
            Field field = new StoredField("foo", 1);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            field.SetInt32Value(5);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(5, field.GetInt32Value());
        }

        [Test]
        public virtual void TestStoredFieldDouble()
        {
            Field field = new StoredField("foo", 1D);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            field.SetDoubleValue(5D);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(5D, field.GetDoubleValue().Value, 0.0D);
        }

        [Test]
        public virtual void TestStoredFieldFloat()
        {
            Field field = new StoredField("foo", 1F);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            field.SetSingleValue(5f);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(5f, field.GetSingleValue().Value, 0.0f);
        }

        [Test]
        public virtual void TestStoredFieldLong()
        {
            Field field = new StoredField("foo", 1L);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            field.SetInt64Value(5);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(5L, field.GetInt64Value().Value);
        }

        private void TrySetByteValue(Field f)
        {
            try
            {
                f.SetByteValue((byte)10);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetBytesValue(Field f)
        {
            try
            {
                f.SetBytesValue(new byte[] { 5, 5 });
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetBytesRefValue(Field f)
        {
            try
            {
                f.SetBytesValue(new BytesRef("bogus"));
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetDoubleValue(Field f)
        {
            try
            {
                f.SetDoubleValue(double.MaxValue);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetIntValue(Field f)
        {
            try
            {
                f.SetInt32Value(int.MaxValue);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetLongValue(Field f)
        {
            try
            {
                f.SetInt64Value(long.MaxValue);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetFloatValue(Field f)
        {
            try
            {
                f.SetSingleValue(float.MaxValue);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetReaderValue(Field f)
        {
            try
            {
                f.SetReaderValue(new StringReader("BOO!"));
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetShortValue(Field f)
        {
            try
            {
                f.SetInt16Value(short.MaxValue);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetStringValue(Field f)
        {
            try
            {
                f.SetStringValue("BOO!");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetTokenStreamValue(Field f)
        {
            try
            {
                f.SetTokenStream(new CannedTokenStream(new Token("foo", 0, 3)));
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }

        private void TrySetBoost(Field f)
        {
            try
            {
                f.Boost = 5.0f;
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                // expected
            }
        }


        // Possible issue reported via dev maling list: http://apache.markmail.org/search/?q=lucenenet+issue+with+doublefield#query:lucenenet%20issue%20with%20doublefield+page:1+mid:4ewxqrsg2nl3en5d+state:results
        // As it turns out this is the correct behavior, as confirmed in Lucene using the following tests
        [Test, LuceneNetSpecific]
        public void TestStoreAndRetrieveFieldType()
        {
            Directory dir = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            IndexWriterConfig iwc = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

            double value = double.MaxValue;
            string fieldName = "DoubleField";

            FieldType type = new FieldType();
            type.IsIndexed = true;
            type.IsStored = true;
            type.IsTokenized = false;
            type.NumericType = NumericType.DOUBLE;


            using (IndexWriter writer = new IndexWriter(dir, iwc))
            {
                Document doc = new Document();
                Field field = new DoubleField(fieldName, value, type);
                FieldType fieldType = field.FieldType;

                assertEquals(true, fieldType.IsIndexed);
                assertEquals(true, fieldType.IsStored);
                assertEquals(false, fieldType.IsTokenized);
                assertEquals(NumericType.DOUBLE, fieldType.NumericType);

                doc.Add(field);
                writer.AddDocument(doc);
                writer.Commit();
            }

            using (IndexReader reader = DirectoryReader.Open(dir))
            {
                IndexSearcher searcher = new IndexSearcher(reader);
                var hits = searcher.Search(new MatchAllDocsQuery(), 10).ScoreDocs;

                Document doc = searcher.Doc(hits[0].Doc);
                Field field = doc.GetField<Field>(fieldName);
                FieldType fieldType = field.FieldType;

                assertEquals(false, fieldType.IsIndexed);
                assertEquals(true, fieldType.IsStored);
                assertEquals(true, fieldType.IsTokenized);
                assertEquals(NumericType.NONE, fieldType.NumericType);
            }

            dir.Dispose();
        }

        // In Java, the corresponding test is:
        //public void testStoreAndRetrieveFieldType() throws java.io.IOException
        //{
        //    org.apache.lucene.store.Directory dir = new org.apache.lucene.store.RAMDirectory();
        //    org.apache.lucene.analysis.Analyzer analyzer = new org.apache.lucene.analysis.standard.StandardAnalyzer(org.apache.lucene.util.Version.LUCENE_48);
        //    org.apache.lucene.index.IndexWriterConfig iwc = new org.apache.lucene.index.IndexWriterConfig(org.apache.lucene.util.Version.LUCENE_48, analyzer);

        //    double value = Double.MAX_VALUE;
        //    String fieldName = "DoubleField";

        //    FieldType type = new FieldType();
        //    type.setIndexed(true);
        //    type.setStored(true);
        //    type.setTokenized(false);
        //    type.setNumericType(FieldType.NumericType.DOUBLE);


        //    org.apache.lucene.index.IndexWriter writer = new org.apache.lucene.index.IndexWriter(dir, iwc);
        //    {
        //        Document doc = new Document();
        //        Field field = new DoubleField(fieldName, value, type);
        //        FieldType fieldType = field.fieldType();


        //        assertEquals(true, fieldType.indexed());

        //        assertEquals(true, fieldType.stored());

        //        assertEquals(false, fieldType.tokenized());

        //        assertEquals(FieldType.NumericType.DOUBLE, fieldType.numericType());

        //        doc.add(field);
        //        writer.addDocument(doc);
        //        writer.commit();
        //    }
        //    writer.close();

        //    org.apache.lucene.index.IndexReader reader = org.apache.lucene.index.DirectoryReader.open(dir);
        //    {
        //        org.apache.lucene.search.IndexSearcher searcher = new org.apache.lucene.search.IndexSearcher(reader);
        //        org.apache.lucene.search.ScoreDoc[] hits = searcher.search(new org.apache.lucene.search.MatchAllDocsQuery(), 10).scoreDocs;

        //        Document doc = searcher.doc(hits[0].doc);
        //        Field field = (Field)doc.getField(fieldName);
        //        FieldType fieldType = field.fieldType();

        //        assertEquals(false, fieldType.indexed());
        //        assertEquals(true, fieldType.stored());
        //        assertEquals(true, fieldType.tokenized());
        //        assertEquals(null, fieldType.numericType());
        //    }
        //    reader.close();

        //    dir.close();
        //}

        public enum ToStringCulture
        {
            Invariant,
            France
        }

        public static IEnumerable<TestCaseData> ToStringData(ToStringCulture cultureEnum)
        {
            CultureInfo culture = cultureEnum switch
            {
                ToStringCulture.France => new CultureInfo("fr-FR"),
                _ => CultureInfo.InvariantCulture
            };

            string sep = culture.NumberFormat.NumberDecimalSeparator;

            yield return new TestCaseData(new DoubleField("foo", 5d, Field.Store.NO), $"indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=DOUBLE,numericPrecisionStep=4<foo:5{sep}0>");
            yield return new TestCaseData(new DoubleField("foo", 5d, Field.Store.YES), $"stored,indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=DOUBLE,numericPrecisionStep=4<foo:5{sep}0>");
            yield return new TestCaseData(new DoubleDocValuesField("foo", 5d), "docValueType=NUMERIC<foo:4617315517961601024>");
            yield return new TestCaseData(new SingleDocValuesField("foo", 5f), "docValueType=NUMERIC<foo:1084227584>");
            yield return new TestCaseData(new SingleField("foo", 5f, Field.Store.NO), $"indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=SINGLE,numericPrecisionStep=4<foo:5{sep}0>");
            yield return new TestCaseData(new SingleField("foo", 5f, Field.Store.YES), $"stored,indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=SINGLE,numericPrecisionStep=4<foo:5{sep}0>");
            yield return new TestCaseData(new Int32Field("foo", 5, Field.Store.NO), "indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=INT32,numericPrecisionStep=4<foo:5>");
            yield return new TestCaseData(new Int32Field("foo", 5, Field.Store.YES), "stored,indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=INT32,numericPrecisionStep=4<foo:5>");
            yield return new TestCaseData(new NumericDocValuesField("foo", 5L), "docValueType=NUMERIC<foo:5>");
            yield return new TestCaseData(new Int64Field("foo", 5L, Field.Store.NO), "indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=INT64,numericPrecisionStep=4<foo:5>");
            yield return new TestCaseData(new Int64Field("foo", 5L, Field.Store.YES), "stored,indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=INT64,numericPrecisionStep=4<foo:5>");
            yield return new TestCaseData(new SortedDocValuesField("foo", new BytesRef("bar")), "docValueType=SORTED<foo:[62 61 72]>");
            yield return new TestCaseData(new BinaryDocValuesField("foo", new BytesRef("bar")), "docValueType=BINARY<foo:[62 61 72]>");
            yield return new TestCaseData(new StringField("foo", "bar", Field.Store.NO), "indexed,omitNorms,indexOptions=DOCS_ONLY<foo:bar>");
            yield return new TestCaseData(new StringField("foo", "bar", Field.Store.YES), "stored,indexed,omitNorms,indexOptions=DOCS_ONLY<foo:bar>");
            yield return new TestCaseData(new TextField("foo", "bar", Field.Store.NO), "indexed,tokenized<foo:bar>");
            yield return new TestCaseData(new TextField("foo", "bar", Field.Store.YES), "stored,indexed,tokenized<foo:bar>");
            yield return new TestCaseData(new TextField("foo", new StringReader("bar")), "indexed,tokenized<foo:System.IO.StringReader>");
            yield return new TestCaseData(new StoredField("foo", "bar".GetBytes(Encoding.UTF8)), "stored<foo:[62 61 72]>");
            yield return new TestCaseData(new StoredField("foo", "bar".GetBytes(Encoding.UTF8), 0, 3), "stored<foo:[62 61 72]>");
            yield return new TestCaseData(new StoredField("foo", new BytesRef("bar")), "stored<foo:[62 61 72]>");
            yield return new TestCaseData(new StoredField("foo", "bar"), "stored<foo:bar>");
            yield return new TestCaseData(new StoredField("foo", 1), "stored<foo:1>");
            yield return new TestCaseData(new StoredField("foo", 1D), $"stored<foo:1{sep}0>");
            yield return new TestCaseData(new StoredField("foo", 1F), $"stored<foo:1{sep}0>");
            yield return new TestCaseData(new StoredField("foo", 1L), "stored<foo:1>");

            // Negative Zero
            yield return new TestCaseData(new DoubleField("foo", -0.0d, Field.Store.NO), $"indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=DOUBLE,numericPrecisionStep=4<foo:-0{sep}0>");
            yield return new TestCaseData(new DoubleDocValuesField("foo", -0.0d), "docValueType=NUMERIC<foo:-9223372036854775808>");
            yield return new TestCaseData(new DoubleDocValuesField("foo", 0.0d), "docValueType=NUMERIC<foo:0>");
            yield return new TestCaseData(new SingleDocValuesField("foo", -0.0f), "docValueType=NUMERIC<foo:-2147483648>");
            yield return new TestCaseData(new SingleDocValuesField("foo", 0.0f), "docValueType=NUMERIC<foo:0>");
            yield return new TestCaseData(new SingleField("foo", -0.0f, Field.Store.NO), $"indexed,tokenized,omitNorms,indexOptions=DOCS_ONLY,numericType=SINGLE,numericPrecisionStep=4<foo:-0{sep}0>");
            yield return new TestCaseData(new StoredField("foo", -0D), $"stored<foo:-0{sep}0>");
            yield return new TestCaseData(new StoredField("foo", -0F), $"stored<foo:-0{sep}0>");
        }

        [Test]
        [LuceneNetSpecific]
        [TestCaseSource("ToStringData", new object[] { ToStringCulture.Invariant })]
        public void TestToStringInvariant(Field field, string expected)
        {
            using (var cultureContext = new CultureContext(CultureInfo.InvariantCulture))
            {
                string actual = field.ToString();
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        [LuceneNetSpecific]
        [TestCaseSource("ToStringData", new object[] { ToStringCulture.France })]
        public void TestToStringFrance(Field field, string expected)
        {
            using (var cultureContext = new CultureContext(new CultureInfo("fr-FR")))
            {
                string actual = field.ToString();
                Assert.AreEqual(expected, actual);
            }
        }
    }
}