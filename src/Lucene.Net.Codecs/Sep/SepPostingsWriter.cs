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

namespace Lucene.Net.Codecs.Sep
{
    using System.Diagnostics;
    using Index;
    using Store;
    using Util;

    /// <summary>
    /// Writes frq to .frq, docs to .doc, pos to .pos, payloads
    /// to .pyl, skip data to .skp
    /// 
    /// @lucene.experimental 
    /// </summary>
    public sealed class SepPostingsWriter : PostingsWriterBase
    {
        internal const string CODEC = "SepPostingsWriter";

        internal const string DOC_EXTENSION = "doc";
        internal const string SKIP_EXTENSION = "skp";
        internal const string FREQ_EXTENSION = "frq";
        internal const string POS_EXTENSION = "pos";
        internal const string PAYLOAD_EXTENSION = "pyl";

        // Increment version to change it:
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;

        internal IntIndexOutput FREQ_OUT;
        internal IntIndexOutputIndex FREQ_INDEX;

        internal IntIndexOutput POS_OUT;
        internal IntIndexOutputIndex POS_INDEX;

        internal IntIndexOutput DOC_OUT;
        internal IntIndexOutputIndex DOC_INDEX;

        internal IndexOutput PAYLOAD_OUT;

        internal IndexOutput SKIP_OUT;

        internal readonly SepSkipListWriter SKIP_LIST_WRITER;

        /// <summary>
        /// Expert: The fraction of TermDocs entries stored in skip tables,
        /// used to accelerate <seealso cref="DocsEnum#advance(int)"/>.  Larger values result in
        /// smaller indexes, greater acceleration, but fewer accelerable cases, while
        /// smaller values result in bigger indexes, less acceleration and more
        /// accelerable cases. More detailed experiments would be useful here. 
        /// </summary>
        internal readonly int SKIP_INTERVAL;

        internal const int DEFAULT_SKIP_INTERVAL = 16;

        /// <summary>
        /// Expert: minimum docFreq to write any skip data at all
        /// </summary>
        internal readonly int SKIP_MINIMUM;

        /// <summary>
        /// Expert: The maximum number of skip levels. Smaller values result in 
        /// slightly smaller indexes, but slower skipping in big posting lists.
        /// </summary>
        internal readonly int MAX_SKIP_LEVELS = 10;

        internal readonly int TOTAL_NUM_DOCS;

        internal bool STORE_PAYLOADS;
        internal IndexOptions INDEX_OPTIONS;

        internal FieldInfo FIELD_INFO;

        internal int LAST_PAYLOAD_LENGTH;
        internal int LAST_POSITION;
        internal long PAYLOAD_START;
        internal int LAST_DOC_ID;
        internal int DF;

        private SepTermState _lastState;
        internal long LAST_PAYLOAD_FP;
        internal long LAST_SKIP_FP;

        public SepPostingsWriter(SegmentWriteState state, IntStreamFactory factory)
            : this(state, factory, DEFAULT_SKIP_INTERVAL)
        {
        }

        public SepPostingsWriter(SegmentWriteState state, IntStreamFactory factory, int skipInterval)
        {
            FREQ_OUT = null;
            FREQ_INDEX = null;
            POS_OUT = null;
            POS_INDEX = null;
            PAYLOAD_OUT = null;
            var success = false;
            try
            {
                SKIP_INTERVAL = skipInterval;
                SKIP_MINIMUM = skipInterval; // set to the same for now
                var docFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, DOC_EXTENSION);

                DOC_OUT = factory.CreateOutput(state.Directory, docFileName, state.Context);
                DOC_INDEX = DOC_OUT.Index();

                if (state.FieldInfos.HasFreq())
                {
                    var frqFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, FREQ_EXTENSION);
                    FREQ_OUT = factory.CreateOutput(state.Directory, frqFileName, state.Context);
                    FREQ_INDEX = FREQ_OUT.Index();
                }

                if (state.FieldInfos.HasProx())
                {
                    var posFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, POS_EXTENSION);
                    POS_OUT = factory.CreateOutput(state.Directory, posFileName, state.Context);
                    POS_INDEX = POS_OUT.Index();

                    // TODO: -- only if at least one field stores payloads?
                    var payloadFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,PAYLOAD_EXTENSION);
                    PAYLOAD_OUT = state.Directory.CreateOutput(payloadFileName, state.Context);
                }

                var skipFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, SKIP_EXTENSION);
                SKIP_OUT = state.Directory.CreateOutput(skipFileName, state.Context);

                TOTAL_NUM_DOCS = state.SegmentInfo.DocCount;

                SKIP_LIST_WRITER = new SepSkipListWriter(skipInterval, MAX_SKIP_LEVELS, TOTAL_NUM_DOCS, FREQ_OUT, DOC_OUT,
                    POS_OUT, PAYLOAD_OUT);

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(DOC_OUT, SKIP_OUT, FREQ_OUT, POS_OUT, PAYLOAD_OUT);
                }
            }
        }
        public override void Init(IndexOutput termsOut)
        {
            CodecUtil.WriteHeader(termsOut, CODEC, VERSION_CURRENT);
            // TODO: -- just ask skipper to "start" here
            termsOut.WriteInt(SKIP_INTERVAL);    // write skipInterval
            termsOut.WriteInt(MAX_SKIP_LEVELS);   // write maxSkipLevels
            termsOut.WriteInt(SKIP_MINIMUM);     // write skipMinimum
        }

        public override BlockTermState NewTermState()
        {
            return new SepTermState();
        }

        public override void StartTerm()
        {
            DOC_INDEX.Mark();
            
            if (INDEX_OPTIONS != IndexOptions.DOCS_ONLY)
            {
                FREQ_INDEX.Mark();
            }

            if (INDEX_OPTIONS == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                POS_INDEX.Mark();
                PAYLOAD_START = PAYLOAD_OUT.FilePointer;
                LAST_PAYLOAD_LENGTH = -1;
            }

            SKIP_LIST_WRITER.ResetSkip(DOC_INDEX, FREQ_INDEX, POS_INDEX);
        }

        // Currently, this instance is re-used across fields, so
        // our parent calls setField whenever the field changes
        public override int SetField(FieldInfo fi)
        {
            FIELD_INFO = fi;
            
            if (FIELD_INFO.IndexOptions.HasValue)
                INDEX_OPTIONS = FIELD_INFO.IndexOptions.Value;

            if (INDEX_OPTIONS >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
            {
                throw new System.NotSupportedException("this codec cannot index offsets");
            }
            SKIP_LIST_WRITER.IndexOptions = INDEX_OPTIONS;
            STORE_PAYLOADS = INDEX_OPTIONS == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS &&
                            FIELD_INFO.HasPayloads;
            LAST_PAYLOAD_FP = 0;
            LAST_SKIP_FP = 0;
            _lastState = SetEmptyState();
            return 0;
        }

        private SepTermState SetEmptyState()
        {
            var emptyState = new SepTermState {DocIndex = DOC_OUT.Index()};
            if (INDEX_OPTIONS != IndexOptions.DOCS_ONLY)
            {
                emptyState.FreqIndex = FREQ_OUT.Index();
                if (INDEX_OPTIONS == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    emptyState.PosIndex = POS_OUT.Index();
                }
            }
            emptyState.PayloadFp = 0;
            emptyState.SkipFp = 0;
            return emptyState;
        }

        /// <summary>
        /// Adds a new doc in this term.  If this returns null
        ///  then we just skip consuming positions/payloads. 
        /// </summary>
        public override void StartDoc(int docId, int termDocFreq)
        {
            var delta = docId - LAST_DOC_ID;
            
            if (docId < 0 || (DF > 0 && delta <= 0))
            {
                throw new CorruptIndexException("docs out of order (" + docId + " <= " + LAST_DOC_ID + " ) (docOut: " +
                                                DOC_OUT + ")");
            }

            if ((++DF%SKIP_INTERVAL) == 0)
            {
                // TODO: -- awkward we have to make these two separate calls to skipper
                SKIP_LIST_WRITER.SetSkipData(LAST_DOC_ID, STORE_PAYLOADS, LAST_PAYLOAD_LENGTH);
                SKIP_LIST_WRITER.BufferSkip(DF);
            }

            LAST_DOC_ID = docId;
            DOC_OUT.Write(delta);
            if (INDEX_OPTIONS != IndexOptions.DOCS_ONLY)
            {
                //System.out.println("    sepw startDoc: write freq=" + termDocFreq);
                FREQ_OUT.Write(termDocFreq);
            }
        }

        /// <summary>
        /// Add a new position & payload </summary>
        public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
        {
            Debug.Assert(INDEX_OPTIONS == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
            int delta = position - LAST_POSITION;
            Debug.Assert(delta >= 0, "position=" + position + " lastPosition=" + LAST_POSITION);
            // not quite right (if pos=0 is repeated twice we don't catch it)
            LAST_POSITION = position;

            if (STORE_PAYLOADS)
            {
                int payloadLength = payload == null ? 0 : payload.Length;
                if (payloadLength != LAST_PAYLOAD_LENGTH)
                {
                    LAST_PAYLOAD_LENGTH = payloadLength;
                    // TODO: explore whether we get better compression
                    // by not storing payloadLength into prox stream?
                    POS_OUT.Write((delta << 1) | 1);
                    POS_OUT.Write(payloadLength);
                }
                else
                {
                    POS_OUT.Write(delta << 1);
                }

                if (payloadLength > 0 && payload != null)
                {
                    PAYLOAD_OUT.WriteBytes(payload.Bytes, payload.Offset, payloadLength);
                }
            }
            else
            {
                POS_OUT.Write(delta);
            }

            LAST_POSITION = position;
        }

        /// <summary>Called when we are done adding positions & payloads </summary>
        public override void FinishDoc()
        {
            LAST_POSITION = 0;
        }

        private class SepTermState : BlockTermState
        {
            public IntIndexOutputIndex DocIndex { get; set; }
            public IntIndexOutputIndex FreqIndex { get; set; }
            public IntIndexOutputIndex PosIndex { get; set; }
            public long PayloadFp { get; set; }
            public long SkipFp { get; set; }
        }

        /// <summary>Called when we are done adding docs to this term </summary>
        public override void FinishTerm(BlockTermState bstate)
        {
            var state = (SepTermState)bstate;
            // TODO: -- wasteful we are counting this in two places?
            Debug.Assert(state.DocFreq > 0);
            Debug.Assert(state.DocFreq == DF);

            state.DocIndex = DOC_OUT.Index();
            state.DocIndex.CopyFrom(DOC_INDEX, false);
            if (INDEX_OPTIONS != IndexOptions.DOCS_ONLY)
            {
                state.FreqIndex = FREQ_OUT.Index();
                state.FreqIndex.CopyFrom(FREQ_INDEX, false);
                if (INDEX_OPTIONS == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    state.PosIndex = POS_OUT.Index();
                    state.PosIndex.CopyFrom(POS_INDEX, false);
                }
                else
                {
                    state.PosIndex = null;
                }
            }
            else
            {
                state.FreqIndex = null;
                state.PosIndex = null;
            }

            if (DF >= SKIP_MINIMUM)
            {
                state.SkipFp = SKIP_OUT.FilePointer;
                SKIP_LIST_WRITER.WriteSkip(SKIP_OUT);
            }
            else
            {
                state.SkipFp = -1;
            }
            state.PayloadFp = PAYLOAD_START;

            LAST_DOC_ID = 0;
            DF = 0;
        }

        public override void EncodeTerm(long[] longs, DataOutput output, FieldInfo fi, BlockTermState bstate, bool absolute)
        {
            var state = (SepTermState) bstate;
            if (absolute)
            {
                LAST_SKIP_FP = 0;
                LAST_PAYLOAD_FP = 0;
                _lastState = state;
            }
            _lastState.DocIndex.CopyFrom(state.DocIndex, false);
            _lastState.DocIndex.Write(output, absolute);
            if (INDEX_OPTIONS != IndexOptions.DOCS_ONLY)
            {
                _lastState.FreqIndex.CopyFrom(state.FreqIndex, false);
                _lastState.FreqIndex.Write(output, absolute);
                if (INDEX_OPTIONS == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    _lastState.PosIndex.CopyFrom(state.PosIndex, false);
                    _lastState.PosIndex.Write(output, absolute);
                    if (STORE_PAYLOADS)
                    {
                        if (absolute)
                        {
                            output.WriteVLong(state.PayloadFp);
                        }
                        else
                        {
                            output.WriteVLong(state.PayloadFp - LAST_PAYLOAD_FP);
                        }
                        LAST_PAYLOAD_FP = state.PayloadFp;
                    }
                }
            }
            if (state.SkipFp == -1) return;

            if (absolute)
            {
                output.WriteVLong(state.SkipFp);
            }
            else
            {
                output.WriteVLong(state.SkipFp - LAST_SKIP_FP);
            }
            LAST_SKIP_FP = state.SkipFp;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            IOUtils.Close(DOC_OUT, SKIP_OUT, FREQ_OUT, POS_OUT, PAYLOAD_OUT);
        }
    }

}