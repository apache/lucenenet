using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestSegmentTermDocs : LuceneTestCase
    {
        private Document testDoc;
        private Directory dir;
        private SegmentCommitInfo info;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            testDoc = new Document();
            dir = NewDirectory();
            DocHelper.SetupDoc(testDoc);
            info = DocHelper.WriteDoc(Random, dir, testDoc);
        }

        [TearDown]
        public override void TearDown()
        {
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void Test()
        {
            Assert.IsTrue(dir != null);
        }

        [Test]
        public virtual void TestTermDocs()
        {
            TestTermDocs(1);
        }

        public virtual void TestTermDocs(int indexDivisor)
        {
            //After adding the document, we should be able to read it back in
            SegmentReader reader = new SegmentReader(info, indexDivisor, NewIOContext(Random));
            Assert.IsTrue(reader != null);
            Assert.AreEqual(indexDivisor, reader.TermInfosIndexDivisor);

            TermsEnum terms = reader.Fields.GetTerms(DocHelper.TEXT_FIELD_2_KEY).GetEnumerator();
            terms.SeekCeil(new BytesRef("field"));
            DocsEnum termDocs = TestUtil.Docs(Random, terms, reader.LiveDocs, null, DocsFlags.FREQS);
            if (termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                int docId = termDocs.DocID;
                Assert.IsTrue(docId == 0);
                int freq = termDocs.Freq;
                Assert.IsTrue(freq == 3);
            }
            reader.Dispose();
        }

        [Test]
        public virtual void TestBadSeek()
        {
            TestBadSeek(1);
        }

        public virtual void TestBadSeek(int indexDivisor)
        {
            {
                //After adding the document, we should be able to read it back in
                SegmentReader reader = new SegmentReader(info, indexDivisor, NewIOContext(Random));
                Assert.IsTrue(reader != null);
                DocsEnum termDocs = TestUtil.Docs(Random, reader, "textField2", new BytesRef("bad"), reader.LiveDocs, null, 0);

                Assert.IsNull(termDocs);
                reader.Dispose();
            }
            {
                //After adding the document, we should be able to read it back in
                SegmentReader reader = new SegmentReader(info, indexDivisor, NewIOContext(Random));
                Assert.IsTrue(reader != null);
                DocsEnum termDocs = TestUtil.Docs(Random, reader, "junk", new BytesRef("bad"), reader.LiveDocs, null, 0);
                Assert.IsNull(termDocs);
                reader.Dispose();
            }
        }

        [Test]
        public virtual void TestSkipTo()
        {
            TestSkipTo(1);
        }

        public virtual void TestSkipTo(int indexDivisor)
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));

            Term ta = new Term("content", "aaa");
            for (int i = 0; i < 10; i++)
            {
                AddDoc(writer, "aaa aaa aaa aaa");
            }

            Term tb = new Term("content", "bbb");
            for (int i = 0; i < 16; i++)
            {
                AddDoc(writer, "bbb bbb bbb bbb");
            }

            Term tc = new Term("content", "ccc");
            for (int i = 0; i < 50; i++)
            {
                AddDoc(writer, "ccc ccc ccc ccc");
            }

            // assure that we deal with a single segment
            writer.ForceMerge(1);
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir, indexDivisor);

            DocsEnum tdocs = TestUtil.Docs(Random, reader, ta.Field, new BytesRef(ta.Text), MultiFields.GetLiveDocs(reader), null, DocsFlags.FREQS);

            // without optimization (assumption skipInterval == 16)

            // with next
            Assert.IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(0, tdocs.DocID);
            Assert.AreEqual(4, tdocs.Freq);
            Assert.IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(1, tdocs.DocID);
            Assert.AreEqual(4, tdocs.Freq);
            Assert.IsTrue(tdocs.Advance(2) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(2, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(4) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(4, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(9) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(9, tdocs.DocID);
            Assert.IsFalse(tdocs.Advance(10) != DocIdSetIterator.NO_MORE_DOCS);

            // without next
            tdocs = TestUtil.Docs(Random, reader, ta.Field, new BytesRef(ta.Text), MultiFields.GetLiveDocs(reader), null, 0);

            Assert.IsTrue(tdocs.Advance(0) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(0, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(4) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(4, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(9) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(9, tdocs.DocID);
            Assert.IsFalse(tdocs.Advance(10) != DocIdSetIterator.NO_MORE_DOCS);

            // exactly skipInterval documents and therefore with optimization

            // with next
            tdocs = TestUtil.Docs(Random, reader, tb.Field, new BytesRef(tb.Text), MultiFields.GetLiveDocs(reader), null, DocsFlags.FREQS);

            Assert.IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(10, tdocs.DocID);
            Assert.AreEqual(4, tdocs.Freq);
            Assert.IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(11, tdocs.DocID);
            Assert.AreEqual(4, tdocs.Freq);
            Assert.IsTrue(tdocs.Advance(12) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(12, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(15) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(15, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(24) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(24, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(25) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(25, tdocs.DocID);
            Assert.IsFalse(tdocs.Advance(26) != DocIdSetIterator.NO_MORE_DOCS);

            // without next
            tdocs = TestUtil.Docs(Random, reader, tb.Field, new BytesRef(tb.Text), MultiFields.GetLiveDocs(reader), null, DocsFlags.FREQS);

            Assert.IsTrue(tdocs.Advance(5) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(10, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(15) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(15, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(24) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(24, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(25) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(25, tdocs.DocID);
            Assert.IsFalse(tdocs.Advance(26) != DocIdSetIterator.NO_MORE_DOCS);

            // much more than skipInterval documents and therefore with optimization

            // with next
            tdocs = TestUtil.Docs(Random, reader, tc.Field, new BytesRef(tc.Text), MultiFields.GetLiveDocs(reader), null, DocsFlags.FREQS);

            Assert.IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(26, tdocs.DocID);
            Assert.AreEqual(4, tdocs.Freq);
            Assert.IsTrue(tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(27, tdocs.DocID);
            Assert.AreEqual(4, tdocs.Freq);
            Assert.IsTrue(tdocs.Advance(28) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(28, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(40) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(40, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(57) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(57, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(74) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(74, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(75) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(75, tdocs.DocID);
            Assert.IsFalse(tdocs.Advance(76) != DocIdSetIterator.NO_MORE_DOCS);

            //without next
            tdocs = TestUtil.Docs(Random, reader, tc.Field, new BytesRef(tc.Text), MultiFields.GetLiveDocs(reader), null, 0);
            Assert.IsTrue(tdocs.Advance(5) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(26, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(40) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(40, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(57) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(57, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(74) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(74, tdocs.DocID);
            Assert.IsTrue(tdocs.Advance(75) != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(75, tdocs.DocID);
            Assert.IsFalse(tdocs.Advance(76) != DocIdSetIterator.NO_MORE_DOCS);

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIndexDivisor()
        {
            testDoc = new Document();
            DocHelper.SetupDoc(testDoc);
            DocHelper.WriteDoc(Random, dir, testDoc);
            TestTermDocs(2);
            TestBadSeek(2);
            TestSkipTo(2);
        }

        private void AddDoc(IndexWriter writer, string value)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", value, Field.Store.NO));
            writer.AddDocument(doc);
        }
    }
}