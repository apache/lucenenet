using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;

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

    using Lucene.Net.Analysis;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// this testcase tests whether multi-level skipping is being used
    /// to reduce I/O while skipping through posting lists.
    ///
    /// Skipping in general is already covered by several other
    /// testcases.
    ///
    /// </summary>
    [TestFixture]
    public class TestMultiLevelSkipList : LuceneTestCase
    {
        internal class CountingRAMDirectory : MockDirectoryWrapper
        {
            private readonly TestMultiLevelSkipList OuterInstance;

            public CountingRAMDirectory(TestMultiLevelSkipList outerInstance, Directory @delegate)
                : base(Random(), @delegate)
            {
                this.OuterInstance = outerInstance;
            }

            public override IndexInput OpenInput(string fileName, IOContext context)
            {
                IndexInput @in = base.OpenInput(fileName, context);
                if (fileName.EndsWith(".frq"))
                {
                    @in = new CountingStream(OuterInstance, @in);
                }
                return @in;
            }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Counter = 0;
        }

        [Test]
        public virtual void TestSimpleSkip()
        {
            Directory dir = new CountingRAMDirectory(this, new RAMDirectory());
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new PayloadAnalyzer()).SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat())).SetMergePolicy(NewLogMergePolicy()));
            Term term = new Term("test", "a");
            for (int i = 0; i < 5000; i++)
            {
                Document d1 = new Document();
                d1.Add(NewTextField(term.Field, term.Text(), Field.Store.NO));
                writer.AddDocument(d1);
            }
            writer.Commit();
            writer.ForceMerge(1);
            writer.Dispose();

            AtomicReader reader = GetOnlySegmentReader(DirectoryReader.Open(dir));

            for (int i = 0; i < 2; i++)
            {
                Counter = 0;
                DocsAndPositionsEnum tp = reader.TermPositionsEnum(term);
                CheckSkipTo(tp, 14, 185); // no skips
                CheckSkipTo(tp, 17, 190); // one skip on level 0
                CheckSkipTo(tp, 287, 200); // one skip on level 1, two on level 0

                // this test would fail if we had only one skip level,
                // because than more bytes would be read from the freqStream
                CheckSkipTo(tp, 4800, 250); // one skip on level 2
            }
        }

        public virtual void CheckSkipTo(DocsAndPositionsEnum tp, int target, int maxCounter)
        {
            tp.Advance(target);
            if (maxCounter < Counter)
            {
                Assert.Fail("Too many bytes read: " + Counter + " vs " + maxCounter);
            }

            Assert.AreEqual(target, tp.DocID(), "Wrong document " + tp.DocID() + " after skipTo target " + target);
            Assert.AreEqual(1, tp.Freq, "Frequency is not 1: " + tp.Freq);
            tp.NextPosition();
            BytesRef b = tp.Payload;
            Assert.AreEqual(1, b.Length);
            Assert.AreEqual((sbyte)target, (sbyte)b.Bytes[b.Offset], "Wrong payload for the target " + target + ": " + (sbyte)b.Bytes[b.Offset]);
        }

        private class PayloadAnalyzer : Analyzer
        {
            internal readonly AtomicInteger PayloadCount = new AtomicInteger(-1);

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(tokenizer, new PayloadFilter(PayloadCount, tokenizer));
            }
        }

        private class PayloadFilter : TokenFilter
        {
            internal IPayloadAttribute PayloadAtt;
            internal AtomicInteger PayloadCount;

            protected internal PayloadFilter(AtomicInteger payloadCount, TokenStream input)
                : base(input)
            {
                this.PayloadCount = payloadCount;
                PayloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                bool hasNext = input.IncrementToken();
                if (hasNext)
                {
                    PayloadAtt.Payload = new BytesRef(new[] { (byte)PayloadCount.IncrementAndGet() });
                }
                return hasNext;
            }
        }

        private int Counter = 0;

        // Simply extends IndexInput in a way that we are able to count the number
        // of bytes read
        internal class CountingStream : IndexInput
        {
            private readonly TestMultiLevelSkipList OuterInstance;

            internal IndexInput Input;

            internal CountingStream(TestMultiLevelSkipList outerInstance, IndexInput input)
                : base("CountingStream(" + input + ")")
            {
                this.OuterInstance = outerInstance;
                this.Input = input;
            }

            public override byte ReadByte()
            {
                OuterInstance.Counter++;
                return this.Input.ReadByte();
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                OuterInstance.Counter += len;
                this.Input.ReadBytes(b, offset, len);
            }

            public override void Dispose()
            {
                this.Input.Dispose();
            }

            public override long FilePointer
            {
                get
                {
                    return this.Input.FilePointer;
                }
            }

            public override void Seek(long pos)
            {
                this.Input.Seek(pos);
            }

            public override long Length()
            {
                return this.Input.Length();
            }

            public override object Clone()
            {
                return new CountingStream(OuterInstance, (IndexInput)this.Input.Clone());
            }
        }
    }
}