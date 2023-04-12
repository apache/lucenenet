using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Pulsing;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.NestedPulsing
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
    /// Pulsing(1, Pulsing(2, Lucene41))
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    // TODO: if we create PulsingPostingsBaseFormat then we
    // can simplify this? note: I don't like the *BaseFormat
    // hierarchy, maybe we can clean that up...
    [PostingsFormatName("NestedPulsing")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class NestedPulsingPostingsFormat : PostingsFormat
    {
        public NestedPulsingPostingsFormat()
            : base()
        { }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase docsWriter = null;
            PostingsWriterBase pulsingWriterInner = null;
            PostingsWriterBase pulsingWriter = null;

            // Terms dict
            bool success = false;
            try
            {
                docsWriter = new Lucene41PostingsWriter(state);

                pulsingWriterInner = new PulsingPostingsWriter(state, 2, docsWriter);
                pulsingWriter = new PulsingPostingsWriter(state, 1, pulsingWriterInner);
                FieldsConsumer ret = new BlockTreeTermsWriter<object>(state, pulsingWriter,
                    BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE, subclassState:null);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(docsWriter, pulsingWriterInner, pulsingWriter);
                }
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase docsReader = null;
            PostingsReaderBase pulsingReaderInner = null;
            PostingsReaderBase pulsingReader = null;
            bool success = false;
            try
            {
                docsReader = new Lucene41PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
                pulsingReaderInner = new PulsingPostingsReader(state, docsReader);
                pulsingReader = new PulsingPostingsReader(state, pulsingReaderInner);
                FieldsProducer ret = new BlockTreeTermsReader<object>(
                                                              state.Directory, state.FieldInfos, state.SegmentInfo,
                                                              pulsingReader,
                                                              state.Context,
                                                              state.SegmentSuffix,
                                                              state.TermsIndexDivisor,
                                                              subclassState: null);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(docsReader, pulsingReaderInner, pulsingReader);
                }
            }
        }
    }
}
