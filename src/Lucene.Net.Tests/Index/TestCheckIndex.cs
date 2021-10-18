using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support.IO;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TextField = TextField;
    using Token = Lucene.Net.Analysis.Token;

    [TestFixture]
    public class TestCheckIndex : LuceneTestCase
    {
        [Test]
        public virtual void TestDeletedDocs()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2));
            for (int i = 0; i < 19; i++)
            {
                Document doc = new Document();
                FieldType customType = new FieldType(TextField.TYPE_STORED);
                customType.StoreTermVectors = true;
                customType.StoreTermVectorPositions = true;
                customType.StoreTermVectorOffsets = true;
                doc.Add(NewField("field", "aaa" + i, customType));
                writer.AddDocument(doc);
            }
            writer.ForceMerge(1);
            writer.Commit();
            writer.DeleteDocuments(new Term("field", "aaa5"));
            writer.Dispose();

            ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
            CheckIndex checker = new CheckIndex(dir);
            checker.InfoStream = new StreamWriter(bos, Encoding.UTF8);
            if (Verbose)
            {
                checker.InfoStream = Console.Out;
            }
            CheckIndex.Status indexStatus = checker.DoCheckIndex();
            if (indexStatus.Clean == false)
            {
                Console.WriteLine("CheckIndex failed");
                checker.FlushInfoStream();
                Console.WriteLine(bos.ToString());
                Assert.Fail();
            }

            CheckIndex.Status.SegmentInfoStatus seg = indexStatus.SegmentInfos[0];
            Assert.IsTrue(seg.OpenReaderPassed);

            Assert.IsNotNull(seg.Diagnostics);

            Assert.IsNotNull(seg.FieldNormStatus);
            Assert.IsNull(seg.FieldNormStatus.Error);
            Assert.AreEqual(1, seg.FieldNormStatus.TotFields);

            Assert.IsNotNull(seg.TermIndexStatus);
            Assert.IsNull(seg.TermIndexStatus.Error);
            Assert.AreEqual(18, seg.TermIndexStatus.TermCount);
            Assert.AreEqual(18, seg.TermIndexStatus.TotFreq);
            Assert.AreEqual(18, seg.TermIndexStatus.TotPos);

            Assert.IsNotNull(seg.StoredFieldStatus);
            Assert.IsNull(seg.StoredFieldStatus.Error);
            Assert.AreEqual(18, seg.StoredFieldStatus.DocCount);
            Assert.AreEqual(18, seg.StoredFieldStatus.TotFields);

            Assert.IsNotNull(seg.TermVectorStatus);
            Assert.IsNull(seg.TermVectorStatus.Error);
            Assert.AreEqual(18, seg.TermVectorStatus.DocCount);
            Assert.AreEqual(18, seg.TermVectorStatus.TotVectors);

            Assert.IsTrue(seg.Diagnostics.Count > 0);
            IList<string> onlySegments = new JCG.List<string>();
            onlySegments.Add("_0");

            Assert.IsTrue(checker.DoCheckIndex(onlySegments).Clean == true);
            dir.Dispose();
        }

        // LUCENE-4221: we have to let these thru, for now
        [Test]
        public virtual void TestBogusTermVectors()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.StoreTermVectors = true;
            ft.StoreTermVectorOffsets = true;
            Field field = new Field("foo", "", ft);
            field.SetTokenStream(new CannedTokenStream(new Token("bar", 5, 10), new Token("bar", 1, 4)));
            doc.Add(field);
            iw.AddDocument(doc);
            iw.Dispose();
            dir.Dispose(); // checkindex
        }
    }
}