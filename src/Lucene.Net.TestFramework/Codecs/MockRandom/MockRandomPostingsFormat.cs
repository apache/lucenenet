using Lucene.Net.Codecs.BlockTerms;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Codecs.MockIntBlock;
using Lucene.Net.Codecs.MockSep;
using Lucene.Net.Codecs.Pulsing;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.MockRandom
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

    /// <summary>
    /// Randomly combines terms index impl w/ postings impls.
    /// </summary>
    [PostingsFormatName("MockRandom")]
    public sealed class MockRandomPostingsFormat : PostingsFormat
    {
        private readonly Random seedRandom;
        private const string SEED_EXT = "sd";

        private sealed class RandomAnonymousClassHelper : J2N.Randomizer
        {
            public RandomAnonymousClassHelper()
                : base(0)
            { }

            public override int Next(int maxValue)
            {
                throw IllegalStateException.Create("Please use MockRandomPostingsFormat(Random)");
            }
        }

        public MockRandomPostingsFormat()
            : this(null)
        {
            // This ctor should *only* be used at read-time: get NPE if you use it!
        }

        public MockRandomPostingsFormat(Random random)
            : base()
        {
            if (random is null)
                this.seedRandom = new RandomAnonymousClassHelper();
            else
                this.seedRandom = new J2N.Randomizer(random.NextInt64());
        }

        // Chooses random IntStreamFactory depending on file's extension
        private class MockInt32StreamFactory : Int32StreamFactory
        {
            private readonly int salt;
            private readonly IList<Int32StreamFactory> delegates = new JCG.List<Int32StreamFactory>();

            public MockInt32StreamFactory(Random random)
            {
                salt = random.Next();
                delegates.Add(new MockSingleInt32Factory());
                int blockSize = TestUtil.NextInt32(random, 1, 2000);
                delegates.Add(new MockFixedInt32BlockPostingsFormat.MockInt32Factory(blockSize));
                int baseBlockSize = TestUtil.NextInt32(random, 1, 127);
                delegates.Add(new MockVariableInt32BlockPostingsFormat.MockInt32Factory(baseBlockSize));
                // TODO: others
            }

            private static string GetExtension(string fileName)
            {
                int idx = fileName.IndexOf('.');
                if (Debugging.AssertsEnabled) Debugging.Assert(idx != -1);
                return fileName.Substring(idx);
            }

            public override Int32IndexInput OpenInput(Directory dir, string fileName, IOContext context)
            {
                // Must only use extension, because IW.addIndexes can
                // rename segment!
                Int32StreamFactory f = delegates[(Math.Abs(salt ^ GetExtension(fileName).GetHashCode())) % delegates.Count];
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: read using int factory " + f + " from fileName=" + fileName);
                }
                return f.OpenInput(dir, fileName, context);
            }

            public override Int32IndexOutput CreateOutput(Directory dir, string fileName, IOContext context)
            {
                Int32StreamFactory f = delegates[(Math.Abs(salt ^ GetExtension(fileName).GetHashCode())) % delegates.Count];
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: write using int factory " + f + " to fileName=" + fileName);
                }
                return f.CreateOutput(dir, fileName, context);
            }
        }

        private sealed class IndexTermSelectorAnonymousClass : VariableGapTermsIndexWriter.IndexTermSelector
        {
            private readonly Random rand;
            private readonly int gap;
            public IndexTermSelectorAnonymousClass(long seed, int gap)
            {
                rand = new J2N.Randomizer(seed);
                this.gap = gap;
            }
            public override bool IsIndexTerm(BytesRef term, TermStats stats)
            {
                return rand.Next(gap) == gap / 2;
            }

            public override void NewField(FieldInfo fieldInfo)
            {
            }
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            int minSkipInterval;
            if (state.SegmentInfo.DocCount > 1000000)
            {
                // Test2BPostings can OOME otherwise:
                minSkipInterval = 3;
            }
            else
            {
                minSkipInterval = 2;
            }

            // we pull this before the seed intentionally: because its not consumed at runtime
            // (the skipInterval is written into postings header)
            int skipInterval = TestUtil.NextInt32(seedRandom, minSkipInterval, 10);

            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("MockRandomCodec: skipInterval=" + skipInterval);
            }

            long seed = seedRandom.NextInt64();

            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("MockRandomCodec: writing to seg=" + state.SegmentInfo.Name + " formatID=" + state.SegmentSuffix + " seed=" + seed);
            }

            string seedFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, SEED_EXT);
            IndexOutput @out = state.Directory.CreateOutput(seedFileName, state.Context);
            try
            {
                @out.WriteInt64(seed);
            }
            finally
            {
                @out.Dispose();
            }

            Random random = new J2N.Randomizer(seed);

            random.Next(); // consume a random for buffersize

            PostingsWriterBase postingsWriter;
            if (random.nextBoolean())
            {
                postingsWriter = new SepPostingsWriter(state, new MockInt32StreamFactory(random), skipInterval);
            }
            else
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: writing Standard postings");
                }
                // TODO: randomize variables like acceptibleOverHead?!
                postingsWriter = new Lucene41PostingsWriter(state, skipInterval);
            }

            if (random.NextBoolean())
            {
                int totTFCutoff = TestUtil.NextInt32(random, 1, 20);
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: writing pulsing postings with totTFCutoff=" + totTFCutoff);
                }
                postingsWriter = new PulsingPostingsWriter(state, totTFCutoff, postingsWriter);
            }

            FieldsConsumer fields;
            int t1 = random.Next(4);

            if (t1 == 0)
            {
                bool success = false;
                try
                {
                    fields = new FSTTermsWriter(state, postingsWriter);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        postingsWriter.Dispose();
                    }
                }
            }
            else if (t1 == 1)
            {
                bool success = false;
                try
                {
                    fields = new FSTOrdTermsWriter(state, postingsWriter);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        postingsWriter.Dispose();
                    }
                }
            }
            else if (t1 == 2)
            {
                // Use BlockTree terms dict

                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: writing BlockTree terms dict");
                }

                // TODO: would be nice to allow 1 but this is very
                // slow to write
                int minTermsInBlock = TestUtil.NextInt32(random, 2, 100);
                int maxTermsInBlock = Math.Max(2, (minTermsInBlock - 1) * 2 + random.Next(100));

                bool success = false;
                try
                {
                    fields = new BlockTreeTermsWriter<object>(state, postingsWriter, minTermsInBlock, maxTermsInBlock, subclassState: null);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        postingsWriter.Dispose();
                    }
                }
            }
            else
            {

                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: writing Block terms dict");
                }

                bool success = false;

                TermsIndexWriterBase indexWriter;
                try
                {
                    if (random.NextBoolean())
                    {
                        state.TermIndexInterval = TestUtil.NextInt32(random, 1, 100);
                        if (LuceneTestCase.Verbose)
                        {
                            Console.WriteLine("MockRandomCodec: fixed-gap terms index (tii=" + state.TermIndexInterval + ")");
                        }
                        indexWriter = new FixedGapTermsIndexWriter(state);
                    }
                    else
                    {
                        VariableGapTermsIndexWriter.IndexTermSelector selector;
                        int n2 = random.Next(3);
                        if (n2 == 0)
                        {
                            int tii = TestUtil.NextInt32(random, 1, 100);
                            selector = new VariableGapTermsIndexWriter.EveryNTermSelector(tii);
                            if (LuceneTestCase.Verbose)
                            {
                                Console.WriteLine("MockRandomCodec: variable-gap terms index (tii=" + tii + ")");
                            }
                        }
                        else if (n2 == 1)
                        {
                            int docFreqThresh = TestUtil.NextInt32(random, 2, 100);
                            int tii = TestUtil.NextInt32(random, 1, 100);
                            selector = new VariableGapTermsIndexWriter.EveryNOrDocFreqTermSelector(docFreqThresh, tii);
                        }
                        else
                        {
                            long seed2 = random.NextInt64();
                            int gap = TestUtil.NextInt32(random, 2, 40);
                            if (LuceneTestCase.Verbose)
                            {
                                Console.WriteLine("MockRandomCodec: random-gap terms index (max gap=" + gap + ")");
                            }
                            selector = new IndexTermSelectorAnonymousClass(seed2, gap);
                        }
                        indexWriter = new VariableGapTermsIndexWriter(state, selector);
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        postingsWriter.Dispose();
                    }
                }

                success = false;
                try
                {
                    fields = new BlockTermsWriter(indexWriter, state, postingsWriter);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        try
                        {
                            postingsWriter.Dispose();
                        }
                        finally
                        {
                            indexWriter.Dispose();
                        }
                    }
                }
            }

            return fields;
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {

            string seedFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, SEED_EXT);
            IndexInput @in = state.Directory.OpenInput(seedFileName, state.Context);
            long seed = @in.ReadInt64();
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("MockRandomCodec: reading from seg=" + state.SegmentInfo.Name + " formatID=" + state.SegmentSuffix + " seed=" + seed);
            }
            @in.Dispose();

            Random random = new J2N.Randomizer(seed);

            int readBufferSize = TestUtil.NextInt32(random, 1, 4096);
            if (LuceneTestCase.Verbose)
            {
                Console.WriteLine("MockRandomCodec: readBufferSize=" + readBufferSize);
            }

            PostingsReaderBase postingsReader;

            if (random.NextBoolean())
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: reading Sep postings");
                }
                postingsReader = new SepPostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo,
                                                       state.Context, new MockInt32StreamFactory(random), state.SegmentSuffix);
            }
            else
            {
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: reading Standard postings");
                }
                postingsReader = new Lucene41PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
            }

            if (random.NextBoolean())
            {
                int totTFCutoff = TestUtil.NextInt32(random, 1, 20);
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: reading pulsing postings with totTFCutoff=" + totTFCutoff);
                }
                postingsReader = new PulsingPostingsReader(state, postingsReader);
            }

            FieldsProducer fields;
            int t1 = random.Next(4);
            if (t1 == 0)
            {
                bool success = false;
                try
                {
                    fields = new FSTTermsReader(state, postingsReader);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        postingsReader.Dispose();
                    }
                }
            }
            else if (t1 == 1)
            {
                bool success = false;
                try
                {
                    fields = new FSTOrdTermsReader(state, postingsReader);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        postingsReader.Dispose();
                    }
                }
            }
            else if (t1 == 2)
            {
                // Use BlockTree terms dict
                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: reading BlockTree terms dict");
                }

                bool success = false;
                try
                {
                    fields = new BlockTreeTermsReader<object>(state.Directory,
                                                      state.FieldInfos,
                                                      state.SegmentInfo,
                                                      postingsReader,
                                                      state.Context,
                                                      state.SegmentSuffix,
                                                      state.TermsIndexDivisor,
                                                      subclassState: null);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        postingsReader.Dispose();
                    }
                }
            }
            else
            {

                if (LuceneTestCase.Verbose)
                {
                    Console.WriteLine("MockRandomCodec: reading Block terms dict");
                }
                TermsIndexReaderBase indexReader;
                bool success = false;
                try
                {
                    bool doFixedGap = random.NextBoolean();

                    // randomness diverges from writer, here:
                    if (state.TermsIndexDivisor != -1)
                    {
                        state.TermsIndexDivisor = TestUtil.NextInt32(random, 1, 10);
                    }

                    if (doFixedGap)
                    {
                        // if termsIndexDivisor is set to -1, we should not touch it. It means a
                        // test explicitly instructed not to load the terms index.
                        if (LuceneTestCase.Verbose)
                        {
                            Console.WriteLine("MockRandomCodec: fixed-gap terms index (divisor=" + state.TermsIndexDivisor + ")");
                        }
                        indexReader = new FixedGapTermsIndexReader(state.Directory,
                                                                   state.FieldInfos,
                                                                   state.SegmentInfo.Name,
                                                                   state.TermsIndexDivisor,
                                                                   BytesRef.UTF8SortedAsUnicodeComparer,
                                                                   state.SegmentSuffix, state.Context);
                    }
                    else
                    {
                        int n2 = random.Next(3);
                        if (n2 == 1)
                        {
                            random.Next();
                        }
                        else if (n2 == 2)
                        {
                            random.NextInt64();
                        }
                        if (LuceneTestCase.Verbose)
                        {
                            Console.WriteLine("MockRandomCodec: variable-gap terms index (divisor=" + state.TermsIndexDivisor + ")");
                        }
                        indexReader = new VariableGapTermsIndexReader(state.Directory,
                                                                      state.FieldInfos,
                                                                      state.SegmentInfo.Name,
                                                                      state.TermsIndexDivisor,
                                                                      state.SegmentSuffix, state.Context);

                    }

                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        postingsReader.Dispose();
                    }
                }

                success = false;
                try
                {
                    fields = new BlockTermsReader(indexReader,
                                                  state.Directory,
                                                  state.FieldInfos,
                                                  state.SegmentInfo,
                                                  postingsReader,
                                                  state.Context,
                                                  state.SegmentSuffix);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        try
                        {
                            postingsReader.Dispose();
                        }
                        finally
                        {
                            indexReader.Dispose();
                        }
                    }
                }
            }

            return fields;
        }
    }
}
