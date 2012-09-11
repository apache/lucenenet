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
    public class TestRollback : LuceneTestCase
    {

        // LUCENE-2536
        [Test]
        public void TestRollbackIntegrityWithBufferFlush()
        {
            Directory dir = new MockRAMDirectory();
            IndexWriter w = new IndexWriter(dir, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
            for (int i = 0; i < 5; i++)
            {
                Document doc = new Document();
                doc.Add(new Field("pk", i.ToString(), Field.Store.YES, Field.Index.ANALYZED_NO_NORMS));
                w.AddDocument(doc);
            }
            w.Close();

            // If buffer size is small enough to cause a flush, errors ensue...
            w = new IndexWriter(dir, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
            w.SetMaxBufferedDocs(2);

            Term pkTerm = new Term("pk", "");
            for (int i = 0; i < 3; i++)
            {
                Document doc = new Document();
                String value = i.ToString();
                doc.Add(new Field("pk", value, Field.Store.YES, Field.Index.ANALYZED_NO_NORMS));
                doc.Add(new Field("text", "foo", Field.Store.YES, Field.Index.ANALYZED_NO_NORMS));
                w.UpdateDocument(pkTerm.CreateTerm(value), doc);
            }
            w.Rollback();

            IndexReader r = IndexReader.Open(dir, true);
            Assert.AreEqual(5, r.NumDocs(), "index should contain same number of docs post rollback");
            r.Close();
            dir.Close();
        }
    }
}
