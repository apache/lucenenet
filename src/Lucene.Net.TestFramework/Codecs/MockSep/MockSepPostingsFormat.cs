using Lucene.Net.Codecs.BlockTerms;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.MockSep
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
    /// A silly codec that simply writes each file separately as
    /// single vInts. Don't use this (performance will be poor)!
    /// This is here just to test the core sep codec
    /// classes.
    /// </summary>
    [PostingsFormatName("MockSep")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class MockSepPostingsFormat : PostingsFormat
    {
        public MockSepPostingsFormat()
            : base()
        { }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {

            PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockSingleInt32Factory());

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

            PostingsReaderBase postingsReader = new SepPostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo,
                state.Context, new MockSingleInt32Factory(), state.SegmentSuffix);

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
