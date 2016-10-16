using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

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
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests the Terms.DocCount statistic
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestDocCount : LuceneTestCase
    {
        [Test]
        public virtual void TestSimple()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            int numDocs = AtLeast(100);
            for (int i = 0; i < numDocs; i++)
            {
                iw.AddDocument(Doc());
            }
            IndexReader ir = iw.Reader;
            VerifyCount(ir);
            ir.Dispose();
            iw.ForceMerge(1);
            ir = iw.Reader;
            VerifyCount(ir);
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }

        private IEnumerable<IndexableField> Doc()
        {
            Document doc = new Document();
            int numFields = TestUtil.NextInt(Random(), 1, 10);
            for (int i = 0; i < numFields; i++)
            {
                doc.Add(NewStringField("" + TestUtil.NextInt(Random(), 'a', 'z'), "" + TestUtil.NextInt(Random(), 'a', 'z'), Field.Store.NO));
            }
            return doc;
        }

        private void VerifyCount(IndexReader ir)
        {
            Fields fields = MultiFields.GetFields(ir);
            if (fields == null)
            {
                return;
            }
            foreach (string field in fields)
            {
                Terms terms = fields.Terms(field);
                if (terms == null)
                {
                    continue;
                }
                int docCount = terms.DocCount;
                FixedBitSet visited = new FixedBitSet(ir.MaxDoc);
                TermsEnum te = terms.Iterator(null);
                while (te.Next() != null)
                {
                    DocsEnum de = TestUtil.Docs(Random(), te, null, null, DocsEnum.FLAG_NONE);
                    while (de.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        visited.Set(de.DocID());
                    }
                }
                Assert.AreEqual(visited.Cardinality(), docCount);
            }
        }
    }
}