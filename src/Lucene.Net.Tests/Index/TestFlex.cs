using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
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

    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;

    [TestFixture]
    public class TestFlex : LuceneTestCase
    {
        // Test non-flex API emulated on flex index
        [Test]
        public virtual void TestNonFlex()
        {
            Directory d = NewDirectory();

            const int DOC_COUNT = 177;

            IndexWriter w = new IndexWriter(d, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMaxBufferedDocs(7).SetMergePolicy(NewLogMergePolicy()));

            for (int iter = 0; iter < 2; iter++)
            {
                if (iter == 0)
                {
                    Documents.Document doc = new Documents.Document();
                    doc.Add(NewTextField("field1", "this is field1", Field.Store.NO));
                    doc.Add(NewTextField("field2", "this is field2", Field.Store.NO));
                    doc.Add(NewTextField("field3", "aaa", Field.Store.NO));
                    doc.Add(NewTextField("field4", "bbb", Field.Store.NO));
                    for (int i = 0; i < DOC_COUNT; i++)
                    {
                        w.AddDocument(doc);
                    }
                }
                else
                {
                    w.ForceMerge(1);
                }

                IndexReader r = w.GetReader();

                TermsEnum terms = MultiFields.GetTerms(r, "field3").GetEnumerator();
                Assert.AreEqual(TermsEnum.SeekStatus.END, terms.SeekCeil(new BytesRef("abc")));
                r.Dispose();
            }

            w.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestTermOrd()
        {
            Directory d = NewDirectory();
            IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())));
            Documents.Document doc = new Documents.Document();
            doc.Add(NewTextField("f", "a b c", Field.Store.NO));
            w.AddDocument(doc);
            w.ForceMerge(1);
            DirectoryReader r = w.GetReader();
            TermsEnum terms = GetOnlySegmentReader(r).Fields.GetTerms("f").GetEnumerator();
            Assert.IsTrue(terms.MoveNext());
            try
            {
                Assert.AreEqual(0, terms.Ord);
            }
            catch (Exception uoe) when (uoe.IsUnsupportedOperationException())
            {
                // ok -- codec is not required to support this op
            }
            r.Dispose();
            w.Dispose();
            d.Dispose();
        }
    }
}