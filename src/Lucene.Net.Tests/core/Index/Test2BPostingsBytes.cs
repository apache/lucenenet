using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
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
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using TextField = TextField;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    /*using Ignore = org.junit.Ignore;

    using TimeoutSuite = com.carrotsearch.randomizedtesting.annotations.TimeoutSuite;*/

    /// <summary>
    /// Test indexes 2B docs with 65k freqs each,
    /// so you get > Integer.MAX_VALUE postings data for the term
    /// @lucene.experimental
    /// </summary>
    [SuppressCodecs("SimpleText", "Memory", "Direct", "Lucene3x")]
    [TestFixture]
    public class Test2BPostingsBytes : LuceneTestCase
    // disable Lucene3x: older lucene formats always had this issue.
    // @Absurd @Ignore takes ~20GB-30GB of space and 10 minutes.
    // with some codecs needs more heap space as well.
    {
        [Ignore("Very slow. Enable manually by removing Ignore.")]
        [Test]
        public virtual void Test([ValueSource(typeof(ConcurrentMergeSchedulers), "Values")]IConcurrentMergeScheduler scheduler)
        {
            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BPostingsBytes1"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
            }

            var config = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                            .SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH)
                            .SetRAMBufferSizeMB(256.0)
                            .SetMergeScheduler(scheduler)
                            .SetMergePolicy(NewLogMergePolicy(false, 10))
                            .SetOpenMode(IndexWriterConfig.OpenMode_e.CREATE);
            IndexWriter w = new IndexWriter(dir, config);

            MergePolicy mp = w.Config.MergePolicy;
            if (mp is LogByteSizeMergePolicy)
            {
                // 1 petabyte:
                ((LogByteSizeMergePolicy)mp).MaxMergeMB = 1024 * 1024 * 1024;
            }

            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;
            ft.OmitNorms = true;
            MyTokenStream tokenStream = new MyTokenStream();
            Field field = new Field("field", tokenStream, ft);
            doc.Add(field);

            const int numDocs = 1000;
            for (int i = 0; i < numDocs; i++)
            {
                if (i % 2 == 1) // trick blockPF's little optimization
                {
                    tokenStream.n = 65536;
                }
                else
                {
                    tokenStream.n = 65537;
                }
                w.AddDocument(doc);
            }
            w.ForceMerge(1);
            w.Dispose();

            DirectoryReader oneThousand = DirectoryReader.Open(dir);
            IndexReader[] subReaders = new IndexReader[1000];
            Arrays.Fill(subReaders, oneThousand);
            MultiReader mr = new MultiReader(subReaders);
            BaseDirectoryWrapper dir2 = NewFSDirectory(CreateTempDir("2BPostingsBytes2"));
            if (dir2 is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir2).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
            }
            IndexWriter w2 = new IndexWriter(dir2, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            w2.AddIndexes(mr);
            w2.ForceMerge(1);
            w2.Dispose();
            oneThousand.Dispose();

            DirectoryReader oneMillion = DirectoryReader.Open(dir2);
            subReaders = new IndexReader[2000];
            Arrays.Fill(subReaders, oneMillion);
            mr = new MultiReader(subReaders);
            BaseDirectoryWrapper dir3 = NewFSDirectory(CreateTempDir("2BPostingsBytes3"));
            if (dir3 is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir3).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
            }
            IndexWriter w3 = new IndexWriter(dir3, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
            w3.AddIndexes(mr);
            w3.ForceMerge(1);
            w3.Dispose();
            oneMillion.Dispose();

            dir.Dispose();
            dir2.Dispose();
            dir3.Dispose();
        }

        public sealed class MyTokenStream : TokenStream
        {
            internal readonly ICharTermAttribute TermAtt;
            internal int Index;
            internal int n;

            public MyTokenStream()
            {
                TermAtt = AddAttribute<ICharTermAttribute>();
            }

            public override bool IncrementToken()
            {
                if (Index < n)
                {
                    ClearAttributes();
                    TermAtt.Buffer()[0] = 'a';
                    TermAtt.Length = 1;
                    Index++;
                    return true;
                }
                return false;
            }

            public override void Reset()
            {
                Index = 0;
            }
        }
    }
}