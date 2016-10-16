using Lucene.Net.Analysis.Tokenattributes;
using System.Diagnostics;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using System.IO;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using TextField = TextField;
    using Token = Lucene.Net.Analysis.Token;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestPayloadsOnVectors : LuceneTestCase
    {
        /// <summary>
        /// some docs have payload att, some not </summary>
        [Test]
        public virtual void TestMixupDocs()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorPayloads = true;
            customType.StoreTermVectorOffsets = Random().NextBoolean();
            Field field = new Field("field", "", customType);
            TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.WHITESPACE, true);
            Assert.IsFalse(ts.HasAttribute<IPayloadAttribute>());
            field.TokenStream = ts;
            doc.Add(field);
            writer.AddDocument(doc);

            Token withPayload = new Token("withPayload", 0, 11);
            withPayload.Payload = new BytesRef("test");
            ts = new CannedTokenStream(withPayload);
            Assert.IsTrue(ts.HasAttribute<IPayloadAttribute>());
            field.TokenStream = ts;
            writer.AddDocument(doc);

            ts = new MockTokenizer(new StringReader("another"), MockTokenizer.WHITESPACE, true);
            Assert.IsFalse(ts.HasAttribute<IPayloadAttribute>());
            field.TokenStream = ts;
            writer.AddDocument(doc);

            DirectoryReader reader = writer.Reader;
            Terms terms = reader.GetTermVector(1, "field");
            Debug.Assert(terms != null);
            TermsEnum termsEnum = terms.Iterator(null);
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("withPayload")));
            DocsAndPositionsEnum de = termsEnum.DocsAndPositions(null, null);
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(0, de.NextPosition());
            Assert.AreEqual(new BytesRef("test"), de.Payload);
            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// some field instances have payload att, some not </summary>
        [Test]
        public virtual void TestMixupMultiValued()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = true;
            customType.StoreTermVectorPayloads = true;
            customType.StoreTermVectorOffsets = Random().NextBoolean();
            Field field = new Field("field", "", customType);
            TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.WHITESPACE, true);
            Assert.IsFalse(ts.HasAttribute<IPayloadAttribute>());
            field.TokenStream = ts;
            doc.Add(field);
            Field field2 = new Field("field", "", customType);
            Token withPayload = new Token("withPayload", 0, 11);
            withPayload.Payload = new BytesRef("test");
            ts = new CannedTokenStream(withPayload);
            Assert.IsTrue(ts.HasAttribute<IPayloadAttribute>());
            field2.TokenStream = ts;
            doc.Add(field2);
            Field field3 = new Field("field", "", customType);
            ts = new MockTokenizer(new StringReader("nopayload"), MockTokenizer.WHITESPACE, true);
            Assert.IsFalse(ts.HasAttribute<IPayloadAttribute>());
            field3.TokenStream = ts;
            doc.Add(field3);
            writer.AddDocument(doc);
            DirectoryReader reader = writer.Reader;
            Terms terms = reader.GetTermVector(0, "field");
            Debug.Assert(terms != null);
            TermsEnum termsEnum = terms.Iterator(null);
            Assert.IsTrue(termsEnum.SeekExact(new BytesRef("withPayload")));
            DocsAndPositionsEnum de = termsEnum.DocsAndPositions(null, null);
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(3, de.NextPosition());
            Assert.AreEqual(new BytesRef("test"), de.Payload);
            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestPayloadsWithoutPositions()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.StoreTermVectors = true;
            customType.StoreTermVectorPositions = false;
            customType.StoreTermVectorPayloads = true;
            customType.StoreTermVectorOffsets = Random().NextBoolean();
            doc.Add(new Field("field", "foo", customType));
            try
            {
                writer.AddDocument(doc);
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
            writer.Dispose();
            dir.Dispose();
        }
    }
}