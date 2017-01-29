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

namespace Lucene.Net.Codecs.Pulsing
{
    using System;
    using System.Diagnostics;
    using Index;
    using Util;

    /// <summary>
    /// This postings format "inlines" the postings for terms that have
    /// low docFreq.  It wraps another postings format, which is used for
    /// writing the non-inlined terms.
    /// @lucene.experimental 
    /// </summary>
    public abstract class PulsingPostingsFormat : PostingsFormat
    {
        private readonly int _freqCutoff;
        private readonly int _minBlockSize;
        private readonly int _maxBlockSize;
        private readonly PostingsBaseFormat _wrappedPostingsBaseFormat;

        public PulsingPostingsFormat(string name, PostingsBaseFormat wrappedPostingsBaseFormat, int freqCutoff) 
            : this(name, wrappedPostingsBaseFormat, freqCutoff, BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE,
            BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
        {
        }

        /// <summary>Terms with freq less than or equal freqCutoff are inlined into terms dict.</summary>
        public PulsingPostingsFormat(string name, PostingsBaseFormat wrappedPostingsBaseFormat, int freqCutoff,
            int minBlockSize, int maxBlockSize) 
            : base(name)
        {
            Debug.Assert(minBlockSize > 1);

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
                FieldsConsumer ret = new BlockTreeTermsWriter(state, pulsingWriter, _minBlockSize, _maxBlockSize);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(docsWriter, pulsingWriter);
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
                FieldsProducer ret = new BlockTreeTermsReader(
                    state.Directory, state.FieldInfos, state.SegmentInfo,
                    pulsingReader,
                    state.Context,
                    state.SegmentSuffix,
                    state.TermsIndexDivisor);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(docsReader, pulsingReader);
                }
            }
        }

        public virtual int FreqCutoff
        {
            get { return _freqCutoff; }
        }
    }
}