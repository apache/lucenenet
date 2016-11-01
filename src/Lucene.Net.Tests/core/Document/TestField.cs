using Lucene.Net.Support;
using NUnit.Framework;
using System;
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
                field.DoubleValue = 6d; // ok
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(6d, (double)field.NumericValue, 0.0d);
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
            field.DoubleValue = 6d; // ok
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(6d, BitConverter.Int64BitsToDouble((long)field.NumericValue), 0.0d);
        }

        [Test]
        public virtual void TestFloatDocValuesField()
        {
            FloatDocValuesField field = new FloatDocValuesField("foo", 5f);

            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            field.FloatValue = 6f; // ok
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(6f, Number.IntBitsToFloat(Convert.ToInt32(field.NumericValue)), 0.0f);
        }

        [Test]
        public virtual void TestFloatField()
        {
            Field[] fields = new Field[] { new FloatField("foo", 5f, Field.Store.NO), new FloatField("foo", 5f, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                field.FloatValue = 6f; // ok
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(6f, (float)field.NumericValue, 0.0f);
            }
        }

        [Test]
        public virtual void TestIntField()
        {
            Field[] fields = new Field[] { new IntField("foo", 5, Field.Store.NO), new IntField("foo", 5, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                field.IntValue = 6; // ok
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(6, (int)field.NumericValue);
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
            field.LongValue = 6; // ok
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(6L, (long)field.NumericValue);
        }

        [Test]
        public virtual void TestLongField()
        {
            Field[] fields = new Field[] { new LongField("foo", 5L, Field.Store.NO), new LongField("foo", 5L, Field.Store.YES) };

            foreach (Field field in fields)
            {
                TrySetBoost(field);
                TrySetByteValue(field);
                TrySetBytesValue(field);
                TrySetBytesRefValue(field);
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                field.LongValue = 6; // ok
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(6L, (long)field.NumericValue);
            }
        }

        [Test]
        public virtual void TestSortedBytesDocValuesField()
        {
            SortedDocValuesField field = new SortedDocValuesField("foo", new BytesRef("bar"));

            TrySetBoost(field);
            TrySetByteValue(field);
            field.BytesValue = "fubar".ToBytesRefArray(Encoding.UTF8);
            field.BytesValue = new BytesRef("baz");
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(new BytesRef("baz"), field.BinaryValue);
        }

        [Test]
        public virtual void TestBinaryDocValuesField()
        {
            BinaryDocValuesField field = new BinaryDocValuesField("foo", new BytesRef("bar"));

            TrySetBoost(field);
            TrySetByteValue(field);
            field.BytesValue = "fubar".ToBytesRefArray(Encoding.UTF8);
            field.BytesValue = new BytesRef("baz");
            TrySetDoubleValue(field);
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(new BytesRef("baz"), field.BinaryValue);
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
                field.StringValue = "baz";
                TrySetTokenStreamValue(field);

                Assert.AreEqual("baz", field.StringValue);
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
                field.StringValue = "baz";
                field.TokenStream = new CannedTokenStream(new Token("foo", 0, 3));

                Assert.AreEqual("baz", field.StringValue);
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
            field.ReaderValue = new StringReader("foobar");
            TrySetShortValue(field);
            TrySetStringValue(field);
            field.TokenStream = new CannedTokenStream(new Token("foo", 0, 3));

            Assert.IsNotNull(field.ReaderValue);
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
                field.BytesValue = "baz".ToBytesRefArray(Encoding.UTF8);
                field.BytesValue = new BytesRef("baz");
                TrySetDoubleValue(field);
                TrySetIntValue(field);
                TrySetFloatValue(field);
                TrySetLongValue(field);
                TrySetReaderValue(field);
                TrySetShortValue(field);
                TrySetStringValue(field);
                TrySetTokenStreamValue(field);

                Assert.AreEqual(new BytesRef("baz"), field.BinaryValue);
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
            field.StringValue = "baz";
            TrySetTokenStreamValue(field);

            Assert.AreEqual("baz", field.StringValue);
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
            field.IntValue = 5;
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(5, (int)field.NumericValue);
        }

        [Test]
        public virtual void TestStoredFieldDouble()
        {
            Field field = new StoredField("foo", 1D);
            TrySetBoost(field);
            TrySetByteValue(field);
            TrySetBytesValue(field);
            TrySetBytesRefValue(field);
            field.DoubleValue = 5D;
            TrySetIntValue(field);
            TrySetFloatValue(field);
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(5D, (double)field.NumericValue, 0.0D);
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
            field.FloatValue = 5f;
            TrySetLongValue(field);
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(5f, (float)field.NumericValue, 0.0f);
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
            field.LongValue = 5;
            TrySetReaderValue(field);
            TrySetShortValue(field);
            TrySetStringValue(field);
            TrySetTokenStreamValue(field);

            Assert.AreEqual(5L, (long)field.NumericValue);
        }

        private void TrySetByteValue(Field f)
        {
            try
            {
                f.ByteValue = (sbyte)10;
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetBytesValue(Field f)
        {
            try
            {
                f.BytesValue = new BytesRef(new byte[] { 5, 5 });
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetBytesRefValue(Field f)
        {
            try
            {
                f.BytesValue = new BytesRef("bogus");
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetDoubleValue(Field f)
        {
            try
            {
                f.DoubleValue = double.MaxValue;
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetIntValue(Field f)
        {
            try
            {
                f.IntValue = int.MaxValue;
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetLongValue(Field f)
        {
            try
            {
                f.LongValue = long.MaxValue;
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetFloatValue(Field f)
        {
            try
            {
                f.FloatValue = float.MaxValue;
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetReaderValue(Field f)
        {
            try
            {
                f.ReaderValue = new StringReader("BOO!");
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetShortValue(Field f)
        {
            try
            {
                f.ShortValue = short.MaxValue;
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetStringValue(Field f)
        {
            try
            {
                f.StringValue = "BOO!";
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        private void TrySetTokenStreamValue(Field f)
        {
            try
            {
                f.TokenStream = new CannedTokenStream(new Token("foo", 0, 3));
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
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
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }
    }
}