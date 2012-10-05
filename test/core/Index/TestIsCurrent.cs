/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Util;

using NUnit.Framework;

namespace Lucene.Net.Index
{
    [TestFixture]
    public class TestIsCurrent : LuceneTestCase
    {

        private IndexWriter writer;

        private Directory directory;

        private Random rand;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // initialize directory
            directory = new MockRAMDirectory();
            writer = new IndexWriter(directory, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.LIMITED);

            // write document
            Document doc = new Document();
            doc.Add(new Field("UUID", "1", Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);
            writer.Commit();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            writer.Close();
            directory.Close();
        }

        /*
         * Failing testcase showing the trouble
         * 
         * @throws IOException
         */
        [Test]
        public void TestDeleteByTermIsCurrent()
        {

            // get reader
            IndexReader reader = writer.GetReader();

            // assert index has a document and reader is up2date 
            Assert.AreEqual(1, writer.NumDocs(), "One document should be in the index");
            Assert.IsTrue(reader.IsCurrent(), "Document added, reader should be stale ");

            // remove document
            Term idTerm = new Term("UUID", "1");
            writer.DeleteDocuments(idTerm);
            writer.Commit();

            // assert document has been deleted (index changed), reader is stale
            Assert.AreEqual(0, writer.NumDocs(), "Document should be removed");
            Assert.IsFalse(reader.IsCurrent(), "Reader should be stale");

            reader.Close();
        }

        /*
         * Testcase for example to show that writer.deleteAll() is working as expected
         * 
         * @throws IOException
         */
        [Test]
        public void TestDeleteAllIsCurrent()
        {

            // get reader
            IndexReader reader = writer.GetReader();

            // assert index has a document and reader is up2date 
            Assert.AreEqual(1, writer.NumDocs(), "One document should be in the index");
            Assert.IsTrue(reader.IsCurrent(), "Document added, reader should be stale ");

            // remove all documents
            writer.DeleteAll();
            writer.Commit();

            // assert document has been deleted (index changed), reader is stale
            Assert.AreEqual(0, writer.NumDocs(), "Document should be removed");
            Assert.IsFalse(reader.IsCurrent(), "Reader should be stale");

            reader.Close();
        }
    }

}
