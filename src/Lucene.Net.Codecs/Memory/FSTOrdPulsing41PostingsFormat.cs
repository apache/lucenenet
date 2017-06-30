namespace Lucene.Net.Codecs.Memory
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

    using Lucene41PostingsBaseFormat = Lucene41.Lucene41PostingsBaseFormat;
    using PulsingPostingsWriter = Pulsing.PulsingPostingsWriter;
    using PulsingPostingsReader = Pulsing.PulsingPostingsReader;
    using SegmentReadState = Index.SegmentReadState;
    using SegmentWriteState = Index.SegmentWriteState;
    using IOUtils = Util.IOUtils;

    /// <summary>
    /// FSTOrd + Pulsing41
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    [PostingsFormatName("FSTOrdPulsing41")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public class FSTOrdPulsing41PostingsFormat : PostingsFormat
    {
        private readonly PostingsBaseFormat _wrappedPostingsBaseFormat;
        private readonly int _freqCutoff;

        public FSTOrdPulsing41PostingsFormat() 
            : this(1)
        {
        }

        public FSTOrdPulsing41PostingsFormat(int freqCutoff) 
            : base()
        {
            _wrappedPostingsBaseFormat = new Lucene41PostingsBaseFormat();
            _freqCutoff = freqCutoff;
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase docsWriter = null;
            PostingsWriterBase pulsingWriter = null;

            bool success = false;
            try
            {
                docsWriter = _wrappedPostingsBaseFormat.PostingsWriterBase(state);
                pulsingWriter = new PulsingPostingsWriter(state, _freqCutoff, docsWriter);
                FieldsConsumer ret = new FSTOrdTermsWriter(state, pulsingWriter);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(docsWriter, pulsingWriter);
                }
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase docsReader = null;
            PostingsReaderBase pulsingReader = null;
            bool success = false;
            try
            {
                docsReader = _wrappedPostingsBaseFormat.PostingsReaderBase(state);
                pulsingReader = new PulsingPostingsReader(state, docsReader);
                FieldsProducer ret = new FSTOrdTermsReader(state, pulsingReader);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(docsReader, pulsingReader);
                }
            }
        }
    }
}