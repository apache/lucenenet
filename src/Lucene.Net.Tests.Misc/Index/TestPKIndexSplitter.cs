using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;
using System.Text;

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

    public class TestPKIndexSplitter : LuceneTestCase
    {
        [Test]
        public void TestSplit()
        {
            string format = "{0:000000000}";
            IndexWriter w;
            using Directory dir = NewDirectory();
            using (w = new IndexWriter(dir, NewIndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false))
                .SetOpenMode(OpenMode.CREATE).SetMergePolicy(NoMergePolicy.COMPOUND_FILES)))
            {
                for (int x = 0; x < 11; x++)
                {
                    Document doc = CreateDocument(x, "1", 3, format);
                    w.AddDocument(doc);
                    if (x % 3 == 0) w.Commit();
                }
                for (int x = 11; x < 20; x++)
                {
                    Document doc = CreateDocument(x, "2", 3, format);
                    w.AddDocument(doc);
                    if (x % 3 == 0) w.Commit();
                }
            }

            Term midTerm = new Term("id", string.Format(CultureInfo.InvariantCulture, format, 11));


            CheckSplitting(dir, midTerm, 11, 9);

            // delete some documents
            using (w = new IndexWriter(dir, NewIndexWriterConfig(

                TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false))
                    .SetOpenMode(OpenMode.APPEND).SetMergePolicy(NoMergePolicy.COMPOUND_FILES)))
            {
                w.DeleteDocuments(midTerm);
                w.DeleteDocuments(new Term("id", string.Format(CultureInfo.InvariantCulture, format, 2)));
            }


            CheckSplitting(dir, midTerm, 10, 8);
        }

        private void CheckSplitting(Directory dir, Term splitTerm, int leftCount, int rightCount)
        {
            using Directory dir1 = NewDirectory();
            using Directory dir2 = NewDirectory();
            PKIndexSplitter splitter = new PKIndexSplitter(dir, dir1, dir2, splitTerm,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)),
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            splitter.Split();

            using IndexReader ir1 = DirectoryReader.Open(dir1);
            using IndexReader ir2 = DirectoryReader.Open(dir2);
            assertEquals(leftCount, ir1.NumDocs);
            assertEquals(rightCount, ir2.NumDocs);


            CheckContents(ir1, "1");
            CheckContents(ir2, "2");
        }

        private void CheckContents(IndexReader ir, string indexname)
        {
            IBits liveDocs = MultiFields.GetLiveDocs(ir);
            for (int i = 0; i < ir.MaxDoc; i++)
            {
                if (liveDocs is null || liveDocs.Get(i))
                {
                    assertEquals(indexname, ir.Document(i).Get("indexname"));
                }
            }
        }

        private Document CreateDocument(int n, string indexName,
            int numFields, string format)
        {
            StringBuilder sb = new StringBuilder();
            Document doc = new Document();
            string id = string.Format(CultureInfo.InvariantCulture, format, n);
            doc.Add(NewStringField("id", id, Field.Store.YES));
            doc.Add(NewStringField("indexname", indexName, Field.Store.YES));
            sb.append("a");
            sb.append(n);
            doc.Add(NewTextField("field1", sb.toString(), Field.Store.YES));
            sb.append(" b");
            sb.append(n);
            for (int i = 1; i < numFields; i++)
            {
                doc.Add(NewTextField("field" + (i + 1), sb.toString(), Field.Store.YES));
            }
            return doc;
        }
    }
}
