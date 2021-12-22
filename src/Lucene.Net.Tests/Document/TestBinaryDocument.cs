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
    /// Tests <seealso cref="Document"/> class.
    /// </summary>
    [TestFixture]
    public class TestBinaryDocument : LuceneTestCase
    {
        internal string binaryValStored = "this text will be stored as a byte array in the index";
        internal string binaryValCompressed = "this text will be also stored and compressed as a byte array in the index";

        [Test]
        public virtual void TestBinaryFieldInIndex()
        {
            FieldType ft = new FieldType();
            ft.IsStored = true;
            IIndexableField binaryFldStored = new StoredField("binaryStored", Encoding.UTF8.GetBytes(binaryValStored));
            IIndexableField stringFldStored = new Field("stringStored", binaryValStored, ft);

            Documents.Document doc = new Documents.Document();

            doc.Add(binaryFldStored);

            doc.Add(stringFldStored);

            /// <summary>
            /// test for field count </summary>
            Assert.AreEqual(2, doc.Fields.Count);

            /// <summary>
            /// add the doc to a ram index </summary>
            Directory dir = NewDirectory();
            Random r = Random;
            RandomIndexWriter writer = new RandomIndexWriter(r, dir);
            writer.AddDocument(doc);

            /// <summary>
            /// open a reader and fetch the document </summary>
            IndexReader reader = writer.GetReader();
            Documents.Document docFromReader = reader.Document(0);
            Assert.IsTrue(docFromReader != null);

            /// <summary>
            /// fetch the binary stored field and compare it's content with the original one </summary>
            BytesRef bytes = docFromReader.GetBinaryValue("binaryStored");
            Assert.IsNotNull(bytes);

            string binaryFldStoredTest = Encoding.UTF8.GetString(bytes.Bytes).Substring(bytes.Offset, bytes.Length);
            //new string(bytes.Bytes, bytes.Offset, bytes.Length, IOUtils.CHARSET_UTF_8);
            Assert.IsTrue(binaryFldStoredTest.Equals(binaryValStored, StringComparison.Ordinal));

            /// <summary>
            /// fetch the string field and compare it's content with the original one </summary>
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

            var doc = new Documents.Document {binaryFldCompressed, stringFldCompressed};

            using Directory dir = NewDirectory();
            using RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            writer.AddDocument(doc);

            using IndexReader reader = writer.GetReader();
            Document docFromReader = reader.Document(0);
            Assert.IsTrue(docFromReader != null);

            string binaryFldCompressedTest =
                Encoding.UTF8.GetString(
                    CompressionTools.Decompress(docFromReader.GetBinaryValue("binaryCompressed")));
            //new string(CompressionTools.Decompress(docFromReader.GetBinaryValue("binaryCompressed")), IOUtils.CHARSET_UTF_8);
            Assert.IsTrue(binaryFldCompressedTest.Equals(binaryValCompressed, StringComparison.Ordinal));
            Assert.IsTrue(
                CompressionTools.DecompressString(docFromReader.GetBinaryValue("stringCompressed"))
                    .Equals(binaryValCompressed, StringComparison.Ordinal));
        }
    }
}