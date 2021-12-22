using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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

    using Document = Documents.Document;
    using Field = Field;

    [TestFixture]
    public class TestIsCurrent : LuceneTestCase
    {
        private RandomIndexWriter writer;

        private Directory directory;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // initialize directory
            directory = NewDirectory();
            writer = new RandomIndexWriter(Random, directory);

            // write document
            Document doc = new Document();
            doc.Add(NewTextField("UUID", "1", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Commit();
        }

        [TearDown]
        public override void TearDown()
        {
            writer.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// Failing testcase showing the trouble
        /// </summary>
        [Test]
        public virtual void TestDeleteByTermIsCurrent()
        {
            // get reader
            DirectoryReader reader = writer.GetReader();

            // assert index has a document and reader is up2date
            Assert.AreEqual(1, writer.NumDocs, "One document should be in the index");
            Assert.IsTrue(reader.IsCurrent(), "One document added, reader should be current");

            // remove document
            Term idTerm = new Term("UUID", "1");
            writer.DeleteDocuments(idTerm);
            writer.Commit();

            // assert document has been deleted (index changed), reader is stale
            Assert.AreEqual(0, writer.NumDocs, "Document should be removed");
            Assert.IsFalse(reader.IsCurrent(), "Reader should be stale");

            reader.Dispose();
        }

        /// <summary>
        /// Testcase for example to show that writer.deleteAll() is working as expected
        /// </summary>
        [Test]
        public virtual void TestDeleteAllIsCurrent()
        {
            // get reader
            DirectoryReader reader = writer.GetReader();

            // assert index has a document and reader is up2date
            Assert.AreEqual(1, writer.NumDocs, "One document should be in the index");
            Assert.IsTrue(reader.IsCurrent(), "Document added, reader should be stale ");

            // remove all documents
            writer.DeleteAll();
            writer.Commit();

            // assert document has been deleted (index changed), reader is stale
            Assert.AreEqual(0, writer.NumDocs, "Document should be removed");
            Assert.IsFalse(reader.IsCurrent(), "Reader should be stale");

            reader.Dispose();
        }
    }
}