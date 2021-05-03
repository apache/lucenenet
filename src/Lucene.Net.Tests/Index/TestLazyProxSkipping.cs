using Lucene.Net.Analysis;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.IO;
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
    using Document = Documents.Document;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Tests lazy skipping on the proximity file.
    ///
    /// </summary>
    [TestFixture]
    public class TestLazyProxSkipping : LuceneTestCase
    {
        private IndexSearcher searcher;
        private int seeksCounter = 0;

        private string field = "tokens";
        private string term1 = "xx";
        private string term2 = "yy";
        private string term3 = "zz";

        private class SeekCountingDirectory : MockDirectoryWrapper
        {
            private readonly TestLazyProxSkipping outerInstance;

            public SeekCountingDirectory(TestLazyProxSkipping outerInstance, Directory @delegate)
                : base(Random, @delegate)
            {
                this.outerInstance = outerInstance;
            }

            public override IndexInput OpenInput(string name, IOContext context)
            {
                IndexInput ii = base.OpenInput(name, context);
                if (name.EndsWith(".prx", StringComparison.Ordinal) || name.EndsWith(".pos", StringComparison.Ordinal))
                {
                    // we decorate the proxStream with a wrapper class that allows to count the number of calls of seek()
                    ii = new SeeksCountingStream(outerInstance, ii);
                }
                return ii;
            }
        }

        private void CreateIndex(int numHits)
        {
            int numDocs = 500;

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader2) =>
            {
                return new TokenStreamComponents(new MockTokenizer(reader2, MockTokenizer.WHITESPACE, true));
            });
            Directory directory = new SeekCountingDirectory(this, new RAMDirectory());
            // note: test explicitly disables payloads
            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy(false)));

            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                string content;
                if (i % (numDocs / numHits) == 0)
                {
                    // add a document that matches the query "term1 term2"
                    content = this.term1 + " " + this.term2;
                }
                else if (i % 15 == 0)
                {
                    // add a document that only contains term1
                    content = this.term1 + " " + this.term1;
                }
                else
                {
                    // add a document that contains term2 but not term 1
                    content = this.term3 + " " + this.term2;
                }

                doc.Add(NewTextField(this.field, content, Documents.Field.Store.YES));
                writer.AddDocument(doc);
            }

            // make sure the index has only a single segment
            writer.ForceMerge(1);
            writer.Dispose();

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(directory));

            this.searcher = NewSearcher(reader);
        }

        private ScoreDoc[] Search()
        {
            // create PhraseQuery "term1 term2" and search
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term(this.field, this.term1));
            pq.Add(new Term(this.field, this.term2));
            return this.searcher.Search(pq, null, 1000).ScoreDocs;
        }

        private void PerformTest(int numHits)
        {
            CreateIndex(numHits);
            this.seeksCounter = 0;
            ScoreDoc[] hits = Search();
            // verify that the right number of docs was found
            Assert.AreEqual(numHits, hits.Length);

            // check if the number of calls of seek() does not exceed the number of hits
            Assert.IsTrue(this.seeksCounter > 0);
            Assert.IsTrue(this.seeksCounter <= numHits + 1, "seeksCounter=" + this.seeksCounter + " numHits=" + numHits);
            searcher.IndexReader.Dispose();
        }

        [Test]
        public virtual void TestLazySkipping()
        {
            string fieldFormat = TestUtil.GetPostingsFormat(this.field);
            AssumeFalse("this test cannot run with Memory postings format", fieldFormat.Equals("Memory", StringComparison.Ordinal));
            AssumeFalse("this test cannot run with Direct postings format", fieldFormat.Equals("Direct", StringComparison.Ordinal));
            AssumeFalse("this test cannot run with SimpleText postings format", fieldFormat.Equals("SimpleText", StringComparison.Ordinal));

            // test whether only the minimum amount of seeks()
            // are performed
            PerformTest(5);
            PerformTest(10);
        }

        [Test]
        public virtual void TestSeek()
        {
            Directory directory = NewDirectory();
            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            for (int i = 0; i < 10; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField(this.field, "a b", Documents.Field.Store.YES));
                writer.AddDocument(doc);
            }

            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(directory);

            DocsAndPositionsEnum tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), this.field, new BytesRef("b"));

            for (int i = 0; i < 10; i++)
            {
                tp.NextDoc();
                Assert.AreEqual(tp.DocID, i);
                Assert.AreEqual(tp.NextPosition(), 1);
            }

            tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), this.field, new BytesRef("a"));

            for (int i = 0; i < 10; i++)
            {
                tp.NextDoc();
                Assert.AreEqual(tp.DocID, i);
                Assert.AreEqual(tp.NextPosition(), 0);
            }
            reader.Dispose();
            directory.Dispose();
        }

        // Simply extends IndexInput in a way that we are able to count the number
        // of invocations of seek()
        internal class SeeksCountingStream : IndexInput
        {
            private readonly TestLazyProxSkipping outerInstance;

            internal IndexInput input;

            internal SeeksCountingStream(TestLazyProxSkipping outerInstance, IndexInput input)
                : base("SeekCountingStream(" + input + ")")
            {
                this.outerInstance = outerInstance;
                this.input = input;
            }

            public override byte ReadByte()
            {
                return this.input.ReadByte();
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                this.input.ReadBytes(b, offset, len);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.input.Dispose();
                }
            }

            public override long Position => this.input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

            public override void Seek(long pos)
            {
                outerInstance.seeksCounter++;
                this.input.Seek(pos);
            }

            public override long Length => this.input.Length;

            public override object Clone()
            {
                return new SeeksCountingStream(outerInstance, (IndexInput)this.input.Clone());
            }
        }
    }
}