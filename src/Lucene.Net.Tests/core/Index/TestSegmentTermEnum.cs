using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;

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

    using Field = Field;
    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestSegmentTermEnum : LuceneTestCase
    {
        internal Directory Dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
        }

        [TearDown]
        public override void TearDown()
        {
            Dir.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestTermEnum()
        {
            IndexWriter writer = null;

            writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            // ADD 100 documents with term : aaa
            // add 100 documents with terms: aaa bbb
            // Therefore, term 'aaa' has document frequency of 200 and term 'bbb' 100
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer, "aaa");
                AddDoc(writer, "aaa bbb");
            }

            writer.Dispose();

            // verify document frequency of terms in an multi segment index
            VerifyDocFreq();

            // merge segments
            writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(OpenMode.APPEND));
            writer.ForceMerge(1);
            writer.Dispose();

            // verify document frequency of terms in a single segment index
            VerifyDocFreq();
        }

        [Test]
        public virtual void TestPrevTermAtEnd()
        {
            IndexWriter writer = new IndexWriter(Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())));
            AddDoc(writer, "aaa bbb");
            writer.Dispose();
            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(Dir));
            TermsEnum terms = reader.Fields.Terms("content").Iterator(null);
            Assert.IsNotNull(terms.Next());
            Assert.AreEqual("aaa", terms.Term.Utf8ToString());
            Assert.IsNotNull(terms.Next());
            long ordB;
            try
            {
                ordB = terms.Ord();
            }
            catch (System.NotSupportedException uoe)
            {
                // ok -- codec is not required to support ord
                reader.Dispose();
                return;
            }
            Assert.AreEqual("bbb", terms.Term.Utf8ToString());
            Assert.IsNull(terms.Next());

            terms.SeekExact(ordB);
            Assert.AreEqual("bbb", terms.Term.Utf8ToString());
            reader.Dispose();
        }

        private void VerifyDocFreq()
        {
            IndexReader reader = DirectoryReader.Open(Dir);
            TermsEnum termEnum = MultiFields.GetTerms(reader, "content").Iterator(null);

            // create enumeration of all terms
            // go to the first term (aaa)
            termEnum.Next();
            // assert that term is 'aaa'
            Assert.AreEqual("aaa", termEnum.Term.Utf8ToString());
            Assert.AreEqual(200, termEnum.DocFreq());
            // go to the second term (bbb)
            termEnum.Next();
            // assert that term is 'bbb'
            Assert.AreEqual("bbb", termEnum.Term.Utf8ToString());
            Assert.AreEqual(100, termEnum.DocFreq());

            // create enumeration of terms after term 'aaa',
            // including 'aaa'
            termEnum.SeekCeil(new BytesRef("aaa"));
            // assert that term is 'aaa'
            Assert.AreEqual("aaa", termEnum.Term.Utf8ToString());
            Assert.AreEqual(200, termEnum.DocFreq());
            // go to term 'bbb'
            termEnum.Next();
            // assert that term is 'bbb'
            Assert.AreEqual("bbb", termEnum.Term.Utf8ToString());
            Assert.AreEqual(100, termEnum.DocFreq());
            reader.Dispose();
        }

        private void AddDoc(IndexWriter writer, string value)
        {
            Document doc = new Document();
            doc.Add(NewTextField("content", value, Field.Store.NO));
            writer.AddDocument(doc);
        }
    }
}