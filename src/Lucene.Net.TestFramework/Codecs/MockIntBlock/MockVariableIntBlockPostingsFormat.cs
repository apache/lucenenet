using Lucene.Net.Codecs.BlockTerms;
using Lucene.Net.Codecs.IntBlock;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.MockIntBlock
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
    /// A silly test codec to verify core support for variable
    /// sized int block encoders is working. The int encoder
    /// used here writes baseBlockSize ints at once, if the first
    /// int is &lt;= 3, else 2* baseBlockSize.
    /// </summary>
    [PostingsFormatName("MockVariableIntBlock")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public class MockVariableInt32BlockPostingsFormat : PostingsFormat
    {
        private readonly int baseBlockSize;
        
        public MockVariableInt32BlockPostingsFormat()
            : this(1)
        { }

        public MockVariableInt32BlockPostingsFormat(int baseBlockSize)
            : base()
        {
            this.baseBlockSize = baseBlockSize;
        }

        public override string ToString()
        {
            return Name + "(baseBlockSize=" + baseBlockSize + ")";
        }

        /// <summary>
        /// If the first value is &lt;= 3, writes baseBlockSize vInts at once,
        /// otherwise writes 2*baseBlockSize vInts.
        /// </summary>
        public class MockInt32Factory : Int32StreamFactory
        {

            private readonly int baseBlockSize;

            public MockInt32Factory(int baseBlockSize)
            {
                this.baseBlockSize = baseBlockSize;
            }

            public override Int32IndexInput OpenInput(Directory dir, string fileName, IOContext context)
            {
                IndexInput input = dir.OpenInput(fileName, context);
                int baseBlockSize = input.ReadInt32();
                return new VariableInt32BlockIndexInputAnonymousClass(input, baseBlockSize);
            }

            private sealed class VariableInt32BlockIndexInputAnonymousClass : VariableInt32BlockIndexInput
            {
                private readonly int baseBlockSize;

                public VariableInt32BlockIndexInputAnonymousClass(IndexInput input, int baseBlockSize)
                    : base(input)
                {
                    this.baseBlockSize = baseBlockSize;
                }
                protected override IBlockReader GetBlockReader(IndexInput @in, int[] buffer)
                {
                    return new BlockReaderAnonymousClass(@in, buffer, baseBlockSize);
                }

                private sealed class BlockReaderAnonymousClass : IBlockReader
                {
                    private readonly IndexInput input;
                    private readonly int[] buffer;
                    private readonly int baseBlockSize;

                    public BlockReaderAnonymousClass(IndexInput input, int[] buffer, int baseBlockSize)
                    {
                        this.input = input;
                        this.buffer = buffer;
                        this.baseBlockSize = baseBlockSize;
                    }

                    public void Seek(long pos)
                    {
                    }

                    public int ReadBlock()
                    {
                        buffer[0] = input.ReadVInt32();
                        int count = buffer[0] <= 3 ? baseBlockSize - 1 : 2 * baseBlockSize - 1;
                        if (Debugging.AssertsEnabled) Debugging.Assert(buffer.Length >= count,"buffer.length={0} count={1}", buffer.Length, count);
                        for (int i = 0; i < count; i++)
                        {
                            buffer[i + 1] = input.ReadVInt32();
                        }
                        return 1 + count;
                    }
                }
            }

            public override Int32IndexOutput CreateOutput(Directory dir, string fileName, IOContext context)
            {
                IndexOutput output = dir.CreateOutput(fileName, context);
                bool success = false;
                try
                {
                    output.WriteInt32(baseBlockSize);
                    VariableInt32BlockIndexOutput ret = new VariableInt32BlockIndexOutputAnonymousClass(output, baseBlockSize);
                    success = true;
                    return ret;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.DisposeWhileHandlingException(output);
                    }
                }
            }

            private sealed class VariableInt32BlockIndexOutputAnonymousClass : VariableInt32BlockIndexOutput
            {
                private readonly int baseBlockSize;
                private readonly IndexOutput output;
                public VariableInt32BlockIndexOutputAnonymousClass(IndexOutput output, int baseBlockSize)
                    : base(output, 2 * baseBlockSize)
                {
                    this.output = output;
                    this.baseBlockSize = baseBlockSize;
                    this.buffer = new int[2 + 2 * baseBlockSize];
                }

                private int pendingCount;
                private readonly int[] buffer;

                protected override int Add(int value)
                {
                    buffer[pendingCount++] = value;
                    // silly variable block length int encoder: if
                    // first value <= 3, we write N vints at once;
                    // else, 2*N
                    int flushAt = buffer[0] <= 3 ? baseBlockSize : 2 * baseBlockSize;

                    // intentionally be non-causal here:
                    if (pendingCount == flushAt + 1)
                    {
                        for (int i = 0; i < flushAt; i++)
                        {
                            this.output.WriteVInt32(buffer[i]);
                        }
                        buffer[0] = buffer[flushAt];
                        pendingCount = 1;
                        return flushAt;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockInt32Factory(baseBlockSize));

            bool success = false;
            TermsIndexWriterBase indexWriter;
            try
            {
                indexWriter = new FixedGapTermsIndexWriter(state);
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
                FieldsConsumer ret = new BlockTermsWriter(indexWriter, state, postingsWriter);
                success = true;
                return ret;
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

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase postingsReader = new SepPostingsReader(state.Directory,
                                                                      state.FieldInfos,
                                                                      state.SegmentInfo,
                                                                      state.Context,
                                                                      new MockInt32Factory(baseBlockSize), state.SegmentSuffix);

            TermsIndexReaderBase indexReader;
            bool success = false;
            try
            {
                indexReader = new FixedGapTermsIndexReader(state.Directory,
                                                                 state.FieldInfos,
                                                                 state.SegmentInfo.Name,
                                                                 state.TermsIndexDivisor,
                                                                 BytesRef.UTF8SortedAsUnicodeComparer,
                                                                 state.SegmentSuffix, state.Context);
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
                FieldsProducer ret = new BlockTermsReader(indexReader,
                                                          state.Directory,
                                                          state.FieldInfos,
                                                          state.SegmentInfo,
                                                          postingsReader,
                                                          state.Context,
                                                          state.SegmentSuffix);
                success = true;
                return ret;
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
    }
}
