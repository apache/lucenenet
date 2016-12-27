using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestParallelTermEnum : LuceneTestCase
    {
        private AtomicReader Ir1;
        private AtomicReader Ir2;
        private Directory Rd1;
        private Directory Rd2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Document doc;
            Rd1 = NewDirectory();
            IndexWriter iw1 = new IndexWriter(Rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            doc = new Document();
            doc.Add(NewTextField("field1", "the quick brown fox jumps", Field.Store.YES));
            doc.Add(NewTextField("field2", "the quick brown fox jumps", Field.Store.YES));
            iw1.AddDocument(doc);

            iw1.Dispose();
            Rd2 = NewDirectory();
            IndexWriter iw2 = new IndexWriter(Rd2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            doc = new Document();
            doc.Add(NewTextField("field1", "the fox jumps over the lazy dog", Field.Store.YES));
            doc.Add(NewTextField("field3", "the fox jumps over the lazy dog", Field.Store.YES));
            iw2.AddDocument(doc);

            iw2.Dispose();

            this.Ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(Rd1));
            this.Ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(Rd2));
        }

        [TearDown]
        public override void TearDown()
        {
            Ir1.Dispose();
            Ir2.Dispose();
            Rd1.Dispose();
            Rd2.Dispose();
            base.TearDown();
        }

        private void CheckTerms(Terms terms, IBits liveDocs, params string[] termsList)
        {
            Assert.IsNotNull(terms);
            TermsEnum te = terms.Iterator(null);

            foreach (string t in termsList)
            {
                BytesRef b = te.Next();
                Assert.IsNotNull(b);
                Assert.AreEqual(t, b.Utf8ToString());
                DocsEnum td = TestUtil.Docs(Random(), te, liveDocs, null, DocsEnum.FLAG_NONE);
                Assert.IsTrue(td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                Assert.AreEqual(0, td.DocID);
                Assert.AreEqual(td.NextDoc(), DocIdSetIterator.NO_MORE_DOCS);
            }
            Assert.IsNull(te.Next());
        }

        [Test]
        public virtual void Test1()
        {
            ParallelAtomicReader pr = new ParallelAtomicReader(Ir1, Ir2);

            IBits liveDocs = pr.LiveDocs;

            Fields fields = pr.Fields;
            IEnumerator<string> fe = fields.GetEnumerator();

            fe.MoveNext();
            string f = fe.Current;
            Assert.AreEqual("field1", f);
            CheckTerms(fields.Terms(f), liveDocs, "brown", "fox", "jumps", "quick", "the");

            fe.MoveNext();
            f = fe.Current;
            Assert.AreEqual("field2", f);
            CheckTerms(fields.Terms(f), liveDocs, "brown", "fox", "jumps", "quick", "the");

            fe.MoveNext();
            f = fe.Current;
            Assert.AreEqual("field3", f);
            CheckTerms(fields.Terms(f), liveDocs, "dog", "fox", "jumps", "lazy", "over", "the");

            Assert.IsFalse(fe.MoveNext());
        }
    }
}