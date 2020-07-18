using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System.Text;
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Document = Documents.Document;
    using Field = Field;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using TermRangeQuery = Lucene.Net.Search.TermRangeQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestForTooMuchCloning : LuceneTestCase
    {
        // Make sure we don't clone IndexInputs too frequently
        // during merging:
        [Test]
        public virtual void Test()
        {
            // NOTE: if we see a fail on this test with "NestedPulsing" its because its
            // reuse isnt perfect (but reasonable). see TestPulsingReuse.testNestedPulsing
            // for more details
            MockDirectoryWrapper dir = NewMockDirectory();
            TieredMergePolicy tmp = new TieredMergePolicy();
            tmp.MaxMergeAtOnce = 2;
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(2).SetMergePolicy(tmp));
            const int numDocs = 20;
            for (int docs = 0; docs < numDocs; docs++)
            {
                StringBuilder sb = new StringBuilder();
                for (int terms = 0; terms < 100; terms++)
                {
                    sb.Append(TestUtil.RandomRealisticUnicodeString(Random));
                    sb.Append(' ');
                }
                Document doc = new Document();
                doc.Add(new TextField("field", sb.ToString(), Field.Store.NO));
                w.AddDocument(doc);
            }
            IndexReader r = w.GetReader();
            w.Dispose();

            int cloneCount = dir.InputCloneCount;
            //System.out.println("merge clone count=" + cloneCount);
            Assert.IsTrue(cloneCount < 500, "too many calls to IndexInput.clone during merging: " + dir.InputCloneCount);

            IndexSearcher s = NewSearcher(r);

            // MTQ that matches all terms so the AUTO_REWRITE should
            // cutover to filter rewrite and reuse a single DocsEnum
            // across all terms;
            TopDocs hits = s.Search(new TermRangeQuery("field", new BytesRef(), new BytesRef("\uFFFF"), true, true), 10);
            Assert.IsTrue(hits.TotalHits > 0);
            int queryCloneCount = dir.InputCloneCount - cloneCount;
            //System.out.println("query clone count=" + queryCloneCount);
            Assert.IsTrue(queryCloneCount < 50, "too many calls to IndexInput.clone during TermRangeQuery: " + queryCloneCount);
            r.Dispose();
            dir.Dispose();
        }
    }
}