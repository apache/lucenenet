using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Pulsing
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
    /// This postings format "inlines" the postings for terms that have
    /// low docFreq.  It wraps another postings format, which is used for
    /// writing the non-inlined terms.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    public abstract class PulsingPostingsFormat : PostingsFormat
    {
        private readonly int _freqCutoff;
        private readonly int _minBlockSize;
        private readonly int _maxBlockSize;
        private readonly PostingsBaseFormat _wrappedPostingsBaseFormat;

        protected PulsingPostingsFormat(PostingsBaseFormat wrappedPostingsBaseFormat, int freqCutoff) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(wrappedPostingsBaseFormat, freqCutoff, BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE,
            BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
        {
        }

        /// <summary>Terms with freq less than or equal <paramref name="freqCutoff"/> are inlined into terms dict.</summary>
        protected PulsingPostingsFormat(PostingsBaseFormat wrappedPostingsBaseFormat, int freqCutoff,
            int minBlockSize, int maxBlockSize) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(minBlockSize > 1);

            _freqCutoff = freqCutoff;
            _minBlockSize = minBlockSize;
            _maxBlockSize = maxBlockSize;
            _wrappedPostingsBaseFormat = wrappedPostingsBaseFormat;
        }

        public override string ToString()
        {
            return string.Format("{0} (freqCutoff={1}, minBlockSize={2}, maxBlockSize={3})", Name, _freqCutoff, _minBlockSize, _maxBlockSize);
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase docsWriter = null;

            // Terms that have <= freqCutoff number of docs are
            // "pulsed" (inlined):
            PostingsWriterBase pulsingWriter = null;

            // Terms dict
            bool success = false;
            try
            {
                docsWriter = _wrappedPostingsBaseFormat.PostingsWriterBase(state);

                // Terms that have <= freqCutoff number of docs are
                // "pulsed" (inlined):
                pulsingWriter = new PulsingPostingsWriter(state, _freqCutoff, docsWriter);
                FieldsConsumer ret = new BlockTreeTermsWriter<object>(state, pulsingWriter, _minBlockSize, _maxBlockSize, subclassState: null);
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
                    IOUtils.DisposeWhileHandlingException(docsReader, pulsingReader);
                }
            }
        }

        public virtual int FreqCutoff => _freqCutoff;
    }
}