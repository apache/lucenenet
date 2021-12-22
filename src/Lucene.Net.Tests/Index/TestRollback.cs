using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestRollback : LuceneTestCase
    {
        // LUCENE-2536
        [Test]
        public virtual void TestRollbackIntegrityWithBufferFlush()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter rw = new RandomIndexWriter(Random, dir);
            for (int i = 0; i < 5; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("pk", Convert.ToString(i), Field.Store.YES));
                rw.AddDocument(doc);
            }
            rw.Dispose();

            // If buffer size is small enough to cause a flush, errors ensue...
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetOpenMode(OpenMode.APPEND));

            for (int i = 0; i < 3; i++)
            {
                Document doc = new Document();
                string value = Convert.ToString(i);
                doc.Add(NewStringField("pk", value, Field.Store.YES));
                doc.Add(NewStringField("text", "foo", Field.Store.YES));
                w.UpdateDocument(new Term("pk", value), doc);
            }
            w.Rollback();

            IndexReader r = DirectoryReader.Open(dir);
            Assert.AreEqual(5, r.NumDocs, "index should contain same number of docs post rollback");
            r.Dispose();
            dir.Dispose();
        }
    }
}