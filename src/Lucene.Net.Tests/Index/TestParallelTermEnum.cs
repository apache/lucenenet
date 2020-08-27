using System.Collections.Generic;
using Lucene.Net.Documents;
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

    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestParallelTermEnum : LuceneTestCase
    {
        private AtomicReader ir1;
        private AtomicReader ir2;
        private Directory rd1;
        private Directory rd2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Document doc;
            rd1 = NewDirectory();
            IndexWriter iw1 = new IndexWriter(rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            doc = new Document();
            doc.Add(NewTextField("field1", "the quick brown fox jumps", Field.Store.YES));
            doc.Add(NewTextField("field2", "the quick brown fox jumps", Field.Store.YES));
            iw1.AddDocument(doc);

            iw1.Dispose();
            rd2 = NewDirectory();
            IndexWriter iw2 = new IndexWriter(rd2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            doc = new Document();
            doc.Add(NewTextField("field1", "the fox jumps over the lazy dog", Field.Store.YES));
            doc.Add(NewTextField("field3", "the fox jumps over the lazy dog", Field.Store.YES));
            iw2.AddDocument(doc);

            iw2.Dispose();

            this.ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(rd1));
            this.ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(rd2));
        }

        [TearDown]
        public override void TearDown()
        {
            ir1.Dispose();
            ir2.Dispose();
            rd1.Dispose();
            rd2.Dispose();
            base.TearDown();
        }

        private void CheckTerms(Terms terms, IBits liveDocs, params string[] termsList)
        {
            Assert.IsNotNull(terms);
            TermsEnum te = terms.GetEnumerator();

            foreach (string t in termsList)
            {
                Assert.IsTrue(te.MoveNext());
                BytesRef b = te.Term;
                Assert.AreEqual(t, b.Utf8ToString());
                DocsEnum td = TestUtil.Docs(Random, te, liveDocs, null, DocsFlags.NONE);
                Assert.IsTrue(td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.AreEqual(0, td.DocID);
                Assert.AreEqual(td.NextDoc(), DocIdSetIterator.NO_MORE_DOCS);
            }
            Assert.IsFalse(te.MoveNext());
        }

        [Test]
        public virtual void Test1()
        {
            ParallelAtomicReader pr = new ParallelAtomicReader(ir1, ir2);

            IBits liveDocs = pr.LiveDocs;

            Fields fields = pr.Fields;
            IEnumerator<string> fe = fields.GetEnumerator();

            fe.MoveNext();
            string f = fe.Current;
            Assert.AreEqual("field1", f);
            CheckTerms(fields.GetTerms(f), liveDocs, "brown", "fox", "jumps", "quick", "the");

            fe.MoveNext();
            f = fe.Current;
            Assert.AreEqual("field2", f);
            CheckTerms(fields.GetTerms(f), liveDocs, "brown", "fox", "jumps", "quick", "the");

            fe.MoveNext();
            f = fe.Current;
            Assert.AreEqual("field3", f);
            CheckTerms(fields.GetTerms(f), liveDocs, "dog", "fox", "jumps", "lazy", "over", "the");

            Assert.IsFalse(fe.MoveNext());
        }
    }
}