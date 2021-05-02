using Lucene.Net.Analysis;
using Lucene.Net.Attributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Documents.Extensions
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

    public class TestDocumentExtensions : LuceneTestCase
    {
        [Test]
        [LuceneNetSpecific]
        public void TestGetField()
        {
            var target = new BinaryDocValuesField("theName", new BytesRef("Foobar"));
            Document document = new Document
            {
                new BinaryDocValuesField("someOtherName", new BytesRef("Foobar2")),
                target
            };
            
            BinaryDocValuesField field = document.GetField<BinaryDocValuesField>("theName");
            Assert.AreSame(target, field);

            Assert.IsNull(document.GetField<BinaryDocValuesField>("nonExistantName"));

#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Throws<InvalidCastException>(() => document.GetField<Int32DocValuesField>("theName"));
#pragma warning restore CS0618 // Type or member is obsolete

            document = null;
            Assert.Throws<ArgumentNullException>(() => document.GetField<BinaryDocValuesField>("theName"));
        }

        [Test]
        [LuceneNetSpecific]
        public void TestGetFields()
        {
            Document document = new Document
            {
                new TextField("someOtherName", "Foobar2", Field.Store.YES),
                new TextField("theName", "Foobar", Field.Store.YES),
                new TextField("theName", "Crowbar", Field.Store.YES)
            };

            TextField[] fields = document.GetFields<TextField>("theName");
            Assert.AreEqual(2, fields.Length);
            Assert.AreEqual("Foobar", fields[0].GetStringValue());
            Assert.AreEqual("Crowbar", fields[1].GetStringValue());

            fields = document.GetFields<TextField>("nonExistantName");
            Assert.IsNotNull(fields);
            Assert.AreEqual(0, fields.Length);

            Assert.Throws<InvalidCastException>(() => document.GetFields<NumericDocValuesField>("theName"));

            document = null;
            Assert.Throws<ArgumentNullException>(() => document.GetFields<TextField>("theName"));
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddBinaryDocValuesField()
        {
            BinaryDocValuesField field = null;
            BytesRef value = new BytesRef("Foobar");
            AssertDocumentExtensionAddsToDocument(document => field = document.AddBinaryDocValuesField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreSame(value, field.FieldsData);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddDoubleDocValuesField()
        {
            DoubleDocValuesField field = null;
            double value = 123.456d;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddDoubleDocValuesField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(J2N.BitConversion.DoubleToRawInt64Bits(value), field.GetDoubleValueOrDefault());
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddDoubleField_Stored()
        {
            DoubleField field = null;
            double value = 123.456d;
            var stored = Field.Store.YES;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddDoubleField("theName", value, stored));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetDoubleValueOrDefault(), 0.0000001d); // We don't really care about precision, just checking to see if the value got passed through
            Assert.AreSame(DoubleField.TYPE_STORED, field.FieldType);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddDoubleField_FieldType()
        {
            DoubleField field = null;
            double value = 123.456d;
            var fieldType = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = false,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = NumericType.DOUBLE,
                IsStored = true
            }.Freeze();
            AssertDocumentExtensionAddsToDocument(document => field = document.AddDoubleField("theName", value, fieldType));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetDoubleValueOrDefault(), 0.0000001d); // We don't really care about precision, just checking to see if the value got passed through
            Assert.AreSame(fieldType, field.FieldType);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddSingleDocValuesField()
        {
            SingleDocValuesField field = null;
            float value = 123.456f;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddSingleDocValuesField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(J2N.BitConversion.SingleToRawInt32Bits(value), field.GetSingleValueOrDefault());
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddSingleField_Stored()
        {
            SingleField field = null;
            float value = 123.456f;
            var stored = Field.Store.YES;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddSingleField("theName", value, stored));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetSingleValueOrDefault(), 0.0000001f); // We don't really care about precision, just checking to see if the value got passed through
            Assert.AreSame(SingleField.TYPE_STORED, field.FieldType);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddSingleField_FieldType()
        {
            SingleField field = null;
            float value = 123.456f;
            var fieldType = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = false,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = NumericType.SINGLE,
                IsStored = true
            }.Freeze();
            AssertDocumentExtensionAddsToDocument(document => field = document.AddSingleField("theName", value, fieldType));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetSingleValueOrDefault(), 0.0000001f); // We don't really care about precision, just checking to see if the value got passed through
            Assert.AreSame(fieldType, field.FieldType);
        }

        // LUCENENET: Int32DocValuesField is obsolete, so we didn't build extension methods

        [Test]
        [LuceneNetSpecific]
        public void TestAddInt32Field_Stored()
        {
            Int32Field field = null;
            int value = 123;
            var stored = Field.Store.YES;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddInt32Field("theName", value, stored));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetInt32ValueOrDefault());
            Assert.AreSame(Int32Field.TYPE_STORED, field.FieldType);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddInt32Field_FieldType()
        {
            Int32Field field = null;
            int value = 123;
            var fieldType = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = false,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = NumericType.INT32,
                IsStored = true
            }.Freeze();
            AssertDocumentExtensionAddsToDocument(document => field = document.AddInt32Field("theName", value, fieldType));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetInt32ValueOrDefault());
            Assert.AreSame(fieldType, field.FieldType);
        }

        // LUCENENET: Int64DocValuesField is obsolete, so we didn't build extension methods

        [Test]
        [LuceneNetSpecific]
        public void TestAddInt64Field_Stored()
        {
            Int64Field field = null;
            long value = 123;
            var stored = Field.Store.YES;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddInt64Field("theName", value, stored));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetInt64ValueOrDefault());
            Assert.AreSame(Int64Field.TYPE_STORED, field.FieldType);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddInt64Field_FieldType()
        {
            Int64Field field = null;
            long value = 123;
            var fieldType = new FieldType
            {
                IsIndexed = true,
                IsTokenized = true,
                OmitNorms = false,
                IndexOptions = IndexOptions.DOCS_ONLY,
                NumericType = NumericType.INT64,
                IsStored = true
            }.Freeze();
            AssertDocumentExtensionAddsToDocument(document => field = document.AddInt64Field("theName", value, fieldType));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetInt64ValueOrDefault());
            Assert.AreSame(fieldType, field.FieldType);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddNumericDocValuesField()
        {
            NumericDocValuesField field = null;
            long value = 123;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddNumericDocValuesField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetInt64ValueOrDefault());
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddSortedDocValuesField()
        {
            SortedDocValuesField field = null;
            BytesRef bytes = new BytesRef("Foobar");
            AssertDocumentExtensionAddsToDocument(document => field = document.AddSortedDocValuesField("theName", bytes));
            Assert.AreEqual("theName", field.Name);
            Assert.AreSame(bytes, field.FieldsData);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStoredField_ByteArray()
        {
            StoredField field = null;
            byte[] bytes = Encoding.UTF8.GetBytes("Foobar");
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStoredField("theName", bytes));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(bytes, field.GetBinaryValue().Bytes);
            Assert.AreEqual(0, field.GetBinaryValue().Offset);
            Assert.AreEqual(bytes.Length, field.GetBinaryValue().Length);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStoredField_ByteArray_WithOffset()
        {
            StoredField field = null;
            byte[] bytes = Encoding.UTF8.GetBytes("FoobarAgain");
            int offset = 3;
            int length = 3;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStoredField("theName", bytes, offset, length));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(bytes, field.GetBinaryValue().Bytes);
            Assert.AreEqual(offset, field.GetBinaryValue().Offset);
            Assert.AreEqual(length, field.GetBinaryValue().Length);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStoredField_BytesRef()
        {
            StoredField field = null;
            BytesRef bytes = new BytesRef("Foobar");
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStoredField("theName", bytes));
            Assert.AreEqual("theName", field.Name);
            Assert.AreSame(bytes, field.GetBinaryValue());
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStoredField_String()
        {
            StoredField field = null;
            string value = "Foobar";
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStoredField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetStringValue());
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStoredField_Int32()
        {
            StoredField field = null;
            int value = 123;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStoredField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetInt32ValueOrDefault());
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStoredField_Single()
        {
            StoredField field = null;
            float value = 123.456f;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStoredField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetSingleValueOrDefault(), 0.0000001f); // We don't really care about precision, just checking to see if the value got passed through
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStoredField_Int64()
        {
            StoredField field = null;
            long value = 123;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStoredField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetInt64ValueOrDefault());
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStoredField_Double()
        {
            StoredField field = null;
            double value = 123.456d;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStoredField("theName", value));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetDoubleValueOrDefault(), 0.0000001d); // We don't really care about precision, just checking to see if the value got passed through
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddStringField()
        {
            StringField field = null;
            string value = "Foobar";
            AssertDocumentExtensionAddsToDocument(document => field = document.AddStringField("theName", value, Field.Store.YES));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetStringValue());
            Assert.AreSame(StringField.TYPE_STORED, field.FieldType);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddTextField_TextReader()
        {
            TextField field = null;
            TextReader reader = new StringReader("Foobar");
            AssertDocumentExtensionAddsToDocument(document => field = document.AddTextField("theName", reader));
            Assert.AreEqual("theName", field.Name);
            Assert.AreSame(reader, field.GetReaderValue());
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddTextField_Stored()
        {
            TextField field = null;
            string value = "Foobar";
            AssertDocumentExtensionAddsToDocument(document => field = document.AddTextField("theName", value, Field.Store.YES));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(value, field.GetStringValue());
            Assert.AreSame(TextField.TYPE_STORED, field.FieldType);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddTextField_TokenStream()
        {
            TextField field = null;
            TokenStream tokenStream = new CannedBinaryTokenStream(new BinaryToken(new BytesRef("Foobar")));
            AssertDocumentExtensionAddsToDocument(document => field = document.AddTextField("theName", tokenStream));
            Assert.AreEqual("theName", field.Name);
            Assert.AreSame(tokenStream, field.GetTokenStreamValue());
        }

        private void AssertDocumentExtensionAddsToDocument<T>(Func<Document, T> extension) where T : IIndexableField
        {
            var document = new Document();
            var field = extension(document);
            Assert.IsNotNull(field);
            Assert.AreEqual(1, document.Fields.Count);
            Assert.AreSame(field, document.Fields[0]);

            document = null;
            Assert.Throws<ArgumentNullException>(() => extension(document));
        }
    }
}