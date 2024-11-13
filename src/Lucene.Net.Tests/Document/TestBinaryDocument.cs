using J2N.Text;
using NUnit.Framework;
using System;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

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
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    /// <summary>
    /// Tests <see cref="Document"/> class.
    /// </summary>
    [TestFixture]
    public class TestBinaryDocument : LuceneTestCase
    {
        internal const string binaryValStored = "this text will be stored as a byte array in the index";
        internal const string binaryValCompressed = "this text will be also stored and compressed as a byte array in the index";

        [Test]
        public virtual void TestBinaryFieldInIndex()
        {
            FieldType ft = new FieldType();
            ft.IsStored = true;
            IIndexableField binaryFldStored = new StoredField("binaryStored", Encoding.UTF8.GetBytes(binaryValStored));
            IIndexableField stringFldStored = new Field("stringStored", binaryValStored, ft);

            Document doc = new Document();

            doc.Add(binaryFldStored);

            doc.Add(stringFldStored);

            // test for field count
            Assert.AreEqual(2, doc.Fields.Count);

            // add the doc to a ram index
            Directory dir = NewDirectory();
            Random r = Random;
            RandomIndexWriter writer = new RandomIndexWriter(r, dir);
            writer.AddDocument(doc);

            // open a reader and fetch the document
            IndexReader reader = writer.GetReader();
            Document docFromReader = reader.Document(0);
            Assert.IsTrue(docFromReader != null);

            // fetch the binary stored field and compare it's content with the original one
            BytesRef bytes = docFromReader.GetBinaryValue("binaryStored");
            Assert.IsNotNull(bytes);
            // LUCENENET: was `= new string(bytes.Bytes, bytes.Offset, bytes.Length, IOUtils.CHARSET_UTF_8);`
            string binaryFldStoredTest = Encoding.UTF8.GetString(bytes.Bytes).Substring(bytes.Offset, bytes.Length);
            Assert.IsTrue(binaryFldStoredTest.Equals(binaryValStored, StringComparison.Ordinal));

            // fetch the string field and compare it's content with the original one
            string stringFldStoredTest = docFromReader.Get("stringStored");
            Assert.IsTrue(stringFldStoredTest.Equals(binaryValStored, StringComparison.Ordinal));

            writer.Dispose();
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestCompressionTools()
        {
            IIndexableField binaryFldCompressed = new StoredField("binaryCompressed", CompressionTools.Compress(binaryValCompressed.GetBytes(Encoding.UTF8)));
            IIndexableField stringFldCompressed = new StoredField("stringCompressed", CompressionTools.CompressString(binaryValCompressed));

            var doc = new Document();

            doc.Add(binaryFldCompressed);
            doc.Add(stringFldCompressed);

            // add the doc to a ram index
            using Directory dir = NewDirectory();
            using RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            writer.AddDocument(doc);

            // open a reader and fetch the document
            using IndexReader reader = writer.GetReader();
            Document docFromReader = reader.Document(0);
            Assert.IsTrue(docFromReader != null);

            // fetch the binary compressed field and compare it's content with the original one
            // LUCENENET: was `= new String(CompressionTools.decompress(docFromReader.getBinaryValue("binaryCompressed")), StandardCharsets.UTF_8);`
            string binaryFldCompressedTest =
                Encoding.UTF8.GetString(
                    CompressionTools.Decompress(docFromReader.GetBinaryValue("binaryCompressed")));
            Assert.IsTrue(binaryFldCompressedTest.Equals(binaryValCompressed, StringComparison.Ordinal));
            Assert.IsTrue(
                CompressionTools.DecompressString(docFromReader.GetBinaryValue("stringCompressed"))
                    .Equals(binaryValCompressed, StringComparison.Ordinal));

            // LUCENENET specific - variables disposed via `using` statements above
            // writer.Dispose();
            // reader.Dispose();
            // dir.Dispose();
        }
    }
}
