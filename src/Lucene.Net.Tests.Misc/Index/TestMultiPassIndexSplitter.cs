using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

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

    public class TestMultiPassIndexSplitter : LuceneTestCase
    {
        IndexReader input;
        int NUM_DOCS = 11;
        Directory dir;

        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            using (IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES)))
            {
                Document doc;
                for (int i = 0; i < NUM_DOCS; i++)
                {
                    doc = new Document();
                    doc.Add(NewStringField("id", i + "", Field.Store.YES));
                    doc.Add(NewTextField("f", i + " " + i, Field.Store.YES));
                    w.AddDocument(doc);
                    if (i % 3 == 0) w.Commit();
                }
                w.Commit();
                w.DeleteDocuments(new Term("id", "" + (NUM_DOCS - 1)));
            }
            input = DirectoryReader.Open(dir);
        }


        public override void TearDown()
        {
            input.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        /**
         * Test round-robin splitting.
         */
         [Test]
        public void TestSplitRR()
        {
            MultiPassIndexSplitter splitter = new MultiPassIndexSplitter();
            Directory[] dirs = new Directory[]{
                NewDirectory(),
                NewDirectory(),
                NewDirectory()
            };
            try
            {
                splitter.Split(TEST_VERSION_CURRENT, input, dirs, false);
                Document doc;
                TermsEnum te;
                IndexReader ir;
                using (ir = DirectoryReader.Open(dirs[0]))
                {
                    assertTrue(ir.NumDocs - NUM_DOCS / 3 <= 1); // rounding error
                    doc = ir.Document(0);
                    assertEquals("0", doc.Get("id"));
                    te = MultiFields.GetTerms(ir, "id").GetEnumerator();
                    assertEquals(TermsEnum.SeekStatus.NOT_FOUND, te.SeekCeil(new BytesRef("1")));
                    assertNotSame("1", te.Term.Utf8ToString());
                }
                using (ir = DirectoryReader.Open(dirs[1]))
                {
                    assertTrue(ir.NumDocs - NUM_DOCS / 3 <= 1);
                    doc = ir.Document(0);
                    assertEquals("1", doc.Get("id"));
                    te = MultiFields.GetTerms(ir, "id").GetEnumerator();
                    assertEquals(TermsEnum.SeekStatus.NOT_FOUND, te.SeekCeil(new BytesRef("0")));

                    assertNotSame("0", te.Term.Utf8ToString());
                }
                using (ir = DirectoryReader.Open(dirs[2]))
                {
                    assertTrue(ir.NumDocs - NUM_DOCS / 3 <= 1);
                    doc = ir.Document(0);
                    assertEquals("2", doc.Get("id"));

                    te = MultiFields.GetTerms(ir, "id").GetEnumerator();
                    assertEquals(TermsEnum.SeekStatus.NOT_FOUND, te.SeekCeil(new BytesRef("1")));
                    assertNotSame("1", te.Term);

                    assertEquals(TermsEnum.SeekStatus.NOT_FOUND, te.SeekCeil(new BytesRef("0")));
                    assertNotSame("0", te.Term.Utf8ToString());
                }
            }
            finally
            {
                foreach (Directory d in dirs)
                {
                    d.Dispose();
                }
            }
        }

        /**
         * Test sequential splitting.
         */
        [Test]
        public void TestSplitSeq()
        {
            MultiPassIndexSplitter splitter = new MultiPassIndexSplitter();
            Directory[] dirs = new Directory[]{
                NewDirectory(),
                NewDirectory(),
                NewDirectory()
            };
            try
            {
                splitter.Split(TEST_VERSION_CURRENT, input, dirs, true);
                Document doc;
                int start;
                IndexReader ir;
                using (ir = DirectoryReader.Open(dirs[0]))
                {
                    assertTrue(ir.NumDocs - NUM_DOCS / 3 <= 1);
                    doc = ir.Document(0);
                    assertEquals("0", doc.Get("id"));
                    start = ir.NumDocs;
                }
                using (ir = DirectoryReader.Open(dirs[1]))
                {
                    assertTrue(ir.NumDocs - NUM_DOCS / 3 <= 1);
                    doc = ir.Document(0);
                    assertEquals(start + "", doc.Get("id"));
                    start += ir.NumDocs;
                }
                using (ir = DirectoryReader.Open(dirs[2]))
                {
                    assertTrue(ir.NumDocs - NUM_DOCS / 3 <= 1);
                    doc = ir.Document(0);
                    assertEquals(start + "", doc.Get("id"));
                    // make sure the deleted doc is not here
                    TermsEnum te = MultiFields.GetTerms(ir, "id").GetEnumerator();
                    Term t = new Term("id", (NUM_DOCS - 1) + "");
                    assertEquals(TermsEnum.SeekStatus.NOT_FOUND, te.SeekCeil(new BytesRef(t.Text)));
                    assertNotSame(t.Text, te.Term.Utf8ToString());
                }
            }
            finally
            {
                foreach (Directory d in dirs)
                {
                    d.Dispose();
                }
            }
        }
    }
}
