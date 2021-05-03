using J2N.Threading.Atomic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
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
            private readonly TestMultiLevelSkipList outerInstance;

            public CountingRAMDirectory(TestMultiLevelSkipList outerInstance, Directory @delegate)
                : base(Random, @delegate)
            {
                this.outerInstance = outerInstance;
            }

            public override IndexInput OpenInput(string fileName, IOContext context)
            {
                IndexInput @in = base.OpenInput(fileName, context);
                if (fileName.EndsWith(".frq", StringComparison.Ordinal))
                {
                    @in = new CountingStream(outerInstance, @in);
                }
                return @in;
            }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            counter = 0;
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
                d1.Add(NewTextField(term.Field, term.Text, Field.Store.NO));
                writer.AddDocument(d1);
            }
            writer.Commit();
            writer.ForceMerge(1);
            writer.Dispose();

            AtomicReader reader = GetOnlySegmentReader(DirectoryReader.Open(dir));

            for (int i = 0; i < 2; i++)
            {
                counter = 0;
                DocsAndPositionsEnum tp = reader.GetTermPositionsEnum(term);
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
            if (maxCounter < counter)
            {
                Assert.Fail("Too many bytes read: " + counter + " vs " + maxCounter);
            }

            Assert.AreEqual(target, tp.DocID, "Wrong document " + tp.DocID + " after skipTo target " + target);
            Assert.AreEqual(1, tp.Freq, "Frequency is not 1: " + tp.Freq);
            tp.NextPosition();
            BytesRef b = tp.GetPayload();
            Assert.AreEqual(1, b.Length);
            Assert.AreEqual((sbyte)target, (sbyte)b.Bytes[b.Offset], "Wrong payload for the target " + target + ": " + (sbyte)b.Bytes[b.Offset]);
        }

        private class PayloadAnalyzer : Analyzer
        {
            internal readonly AtomicInt32 payloadCount = new AtomicInt32(-1);

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(tokenizer, new PayloadFilter(payloadCount, tokenizer));
            }
        }

        private class PayloadFilter : TokenFilter
        {
            internal IPayloadAttribute payloadAtt;
            internal AtomicInt32 payloadCount;

            protected internal PayloadFilter(AtomicInt32 payloadCount, TokenStream input)
                : base(input)
            {
                this.payloadCount = payloadCount;
                payloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                bool hasNext = m_input.IncrementToken();
                if (hasNext)
                {
                    payloadAtt.Payload = new BytesRef(new[] { (byte)payloadCount.IncrementAndGet() });
                }
                return hasNext;
            }
        }

        private int counter = 0;

        // Simply extends IndexInput in a way that we are able to count the number
        // of bytes read
        internal class CountingStream : IndexInput
        {
            private readonly TestMultiLevelSkipList outerInstance;

            internal IndexInput input;

            internal CountingStream(TestMultiLevelSkipList outerInstance, IndexInput input)
                : base("CountingStream(" + input + ")")
            {
                this.outerInstance = outerInstance;
                this.input = input;
            }

            public override byte ReadByte()
            {
                outerInstance.counter++;
                return this.input.ReadByte();
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                outerInstance.counter += len;
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
                this.input.Seek(pos);
            }

            public override long Length => this.input.Length;

            public override object Clone()
            {
                return new CountingStream(outerInstance, (IndexInput)this.input.Clone());
            }
        }
    }
}