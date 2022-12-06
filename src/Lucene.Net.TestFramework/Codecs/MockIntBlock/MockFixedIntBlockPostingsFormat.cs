using Lucene.Net.Codecs.BlockTerms;
using Lucene.Net.Codecs.IntBlock;
using Lucene.Net.Codecs.Sep;
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
    /// A silly test codec to verify core support for fixed
    /// sized int block encoders is working. The int encoder
    /// used here just writes each block as a series of vInt.
    /// </summary>
    [PostingsFormatName("MockFixedIntBlock")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class MockFixedInt32BlockPostingsFormat : PostingsFormat
    {
        private readonly int blockSize;

        public MockFixedInt32BlockPostingsFormat()
            : this(1)
        { }

        public MockFixedInt32BlockPostingsFormat(int blockSize)
            : base()
        {
            this.blockSize = blockSize;
        }

        public override string ToString()
        {
            return Name + "(blockSize=" + blockSize + ")";
        }

        // only for testing
        public Int32StreamFactory GetInt32Factory()
        {
            return new MockInt32Factory(blockSize);
        }

        /// <summary>
        /// Encodes blocks as vInts of a fixed block size.
        /// </summary>
        public class MockInt32Factory : Int32StreamFactory
        {
            private readonly int blockSize;

            public MockInt32Factory(int blockSize)
            {
                this.blockSize = blockSize;
            }

            public override Int32IndexInput OpenInput(Directory dir, string fileName, IOContext context)
            {
                return new FixedInt32BlockIndexInputAnonymousClass(dir.OpenInput(fileName, context));
            }

            private sealed class FixedInt32BlockIndexInputAnonymousClass : FixedInt32BlockIndexInput
            {
                public FixedInt32BlockIndexInputAnonymousClass(IndexInput input)
                    : base(input)
                {
                }

                protected override IBlockReader GetBlockReader(IndexInput @in, int[] buffer)
                {
                    return new BlockReaderAnonymousClass(@in, buffer);
                }

                private sealed class BlockReaderAnonymousClass : FixedInt32BlockIndexInput.IBlockReader
                {
                    private readonly IndexInput @in;
                    private readonly int[] buffer;

                    public BlockReaderAnonymousClass(IndexInput @in, int[] buffer)
                    {
                        this.@in = @in;
                        this.buffer = buffer;
                    }
                    //public void Seek(long pos) // LUCENENET: Not referenced;
                    //{
                    //}

                    public void ReadBlock()
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buffer[i] = @in.ReadVInt32();
                        }
                    }
                }
            }


            public override Int32IndexOutput CreateOutput(Directory dir, string fileName, IOContext context)
            {
                IndexOutput output = dir.CreateOutput(fileName, context);
                bool success = false;
                try
                {
                    FixedInt32BlockIndexOutputAnonymousClass ret = new FixedInt32BlockIndexOutputAnonymousClass(output, blockSize);

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

            private sealed class FixedInt32BlockIndexOutputAnonymousClass : FixedInt32BlockIndexOutput
            {
                public FixedInt32BlockIndexOutputAnonymousClass(IndexOutput output, int blockSize)
                    : base(output, blockSize)
                {
                }
                protected override void FlushBlock()
                {
                    for (int i = 0; i < m_buffer.Length; i++)
                    {
                        m_output.WriteVInt32(m_buffer[i]);
                    }
                }
            }
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockInt32Factory(blockSize));

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
                                                                      new MockInt32Factory(blockSize), state.SegmentSuffix);

            TermsIndexReaderBase indexReader;
            bool success = false;
            try
            {
                indexReader = new FixedGapTermsIndexReader(state.Directory,
                                                                 state.FieldInfos,
                                                                 state.SegmentInfo.Name,
                                                                 state.TermsIndexDivisor,
                                                                 BytesRef.UTF8SortedAsUnicodeComparer, state.SegmentSuffix,
                                                                 IOContext.DEFAULT);
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
