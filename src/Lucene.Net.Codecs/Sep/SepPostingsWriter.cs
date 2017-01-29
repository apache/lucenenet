using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Sep
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

        internal IntIndexOutput freqOut;
        internal IntIndexOutput.AbstractIndex freqIndex;

        internal IntIndexOutput posOut;
        internal IntIndexOutput.AbstractIndex posIndex;

        internal IntIndexOutput docOut;
        internal IntIndexOutput.AbstractIndex docIndex;

        internal IndexOutput payloadOut;

        internal IndexOutput skipOut;

        internal readonly SepSkipListWriter skipListWriter;

        /// <summary>
        /// Expert: The fraction of TermDocs entries stored in skip tables,
        /// used to accelerate <seealso cref="DocsEnum#advance(int)"/>.  Larger values result in
        /// smaller indexes, greater acceleration, but fewer accelerable cases, while
        /// smaller values result in bigger indexes, less acceleration and more
        /// accelerable cases. More detailed experiments would be useful here. 
        /// </summary>
        internal readonly int skipInterval;

        internal const int DEFAULT_SKIP_INTERVAL = 16;

        /// <summary>
        /// Expert: minimum docFreq to write any skip data at all
        /// </summary>
        internal readonly int skipMinimum;

        /// <summary>
        /// Expert: The maximum number of skip levels. Smaller values result in 
        /// slightly smaller indexes, but slower skipping in big posting lists.
        /// </summary>
        internal readonly int maxSkipLevels = 10;

        internal readonly int totalNumDocs;

        internal bool storePayloads;
        internal IndexOptions indexOptions; 

        internal FieldInfo fieldInfo;

        internal int lastPayloadLength;
        internal int lastPosition;
        internal long payloadStart;
        internal int lastDocID;
        internal int df;

        private SepTermState lastState;
        internal long lastPayloadFP;
        internal long lastSkipFP; 

        public SepPostingsWriter(SegmentWriteState state, IntStreamFactory factory)
            : this(state, factory, DEFAULT_SKIP_INTERVAL)
        {
        }

        public SepPostingsWriter(SegmentWriteState state, IntStreamFactory factory, int skipInterval)
        {
            freqOut = null;
            freqIndex = null;
            posOut = null;
            posIndex = null;
            payloadOut = null;
            var success = false;
            try
            {
                this.skipInterval = skipInterval;
                skipMinimum = skipInterval; // set to the same for now
                var docFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, DOC_EXTENSION);

                docOut = factory.CreateOutput(state.Directory, docFileName, state.Context);
                docIndex = docOut.GetIndex();

                if (state.FieldInfos.HasFreq)
                {
                    var frqFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, FREQ_EXTENSION);
                    freqOut = factory.CreateOutput(state.Directory, frqFileName, state.Context);
                    freqIndex = freqOut.GetIndex();
                }

                if (state.FieldInfos.HasProx)
                {
                    var posFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, POS_EXTENSION);
                    posOut = factory.CreateOutput(state.Directory, posFileName, state.Context);
                    posIndex = posOut.GetIndex();

                    // TODO: -- only if at least one field stores payloads?
                    var payloadFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,PAYLOAD_EXTENSION);
                    payloadOut = state.Directory.CreateOutput(payloadFileName, state.Context);
                }

                var skipFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, SKIP_EXTENSION);
                skipOut = state.Directory.CreateOutput(skipFileName, state.Context);

                totalNumDocs = state.SegmentInfo.DocCount;

                skipListWriter = new SepSkipListWriter(skipInterval, maxSkipLevels, totalNumDocs, freqOut, docOut,
                    posOut, payloadOut);

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(docOut, skipOut, freqOut, posOut, payloadOut);
                }
            }
        }
        public override void Init(IndexOutput termsOut)
        {
            CodecUtil.WriteHeader(termsOut, CODEC, VERSION_CURRENT);
            // TODO: -- just ask skipper to "start" here
            termsOut.WriteInt(skipInterval);    // write skipInterval
            termsOut.WriteInt(maxSkipLevels);   // write maxSkipLevels
            termsOut.WriteInt(skipMinimum);     // write skipMinimum
        }

        public override BlockTermState NewTermState()
        {
            return new SepTermState();
        }

        public override void StartTerm()
        {
            docIndex.Mark();
            
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                freqIndex.Mark();
            }

            if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                posIndex.Mark();
                payloadStart = payloadOut.FilePointer;
                lastPayloadLength = -1;
            }

            skipListWriter.ResetSkip(docIndex, freqIndex, posIndex);
        }

        // Currently, this instance is re-used across fields, so
        // our parent calls setField whenever the field changes
        public override int SetField(FieldInfo fi)
        {
            fieldInfo = fi;
            
            if (fieldInfo.IndexOptions.HasValue)
                indexOptions = fieldInfo.IndexOptions.Value;

            if (indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
            {
                throw new System.NotSupportedException("this codec cannot index offsets");
            }
            skipListWriter.SetIndexOptions(indexOptions);
            storePayloads = indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS &&
                            fieldInfo.HasPayloads;
            lastPayloadFP = 0;
            lastSkipFP = 0;
            lastState = SetEmptyState();
            return 0;
        }

        private SepTermState SetEmptyState()
        {
            var emptyState = new SepTermState {DocIndex = docOut.GetIndex()};
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                emptyState.FreqIndex = freqOut.GetIndex();
                if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    emptyState.PosIndex = posOut.GetIndex();
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
            var delta = docId - lastDocID;
            
            if (docId < 0 || (df > 0 && delta <= 0))
            {
                throw new CorruptIndexException("docs out of order (" + docId + " <= " + lastDocID + " ) (docOut: " +
                                                docOut + ")");
            }

            if ((++df%skipInterval) == 0)
            {
                // TODO: -- awkward we have to make these two separate calls to skipper
                skipListWriter.SetSkipData(lastDocID, storePayloads, lastPayloadLength);
                skipListWriter.BufferSkip(df);
            }

            lastDocID = docId;
            docOut.Write(delta);
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                //System.out.println("    sepw startDoc: write freq=" + termDocFreq);
                freqOut.Write(termDocFreq);
            }
        }

        /// <summary>
        /// Add a new position & payload </summary>
        public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
        {
            Debug.Assert(indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
            int delta = position - lastPosition;
            Debug.Assert(delta >= 0, "position=" + position + " lastPosition=" + lastPosition);
            // not quite right (if pos=0 is repeated twice we don't catch it)
            lastPosition = position;

            if (storePayloads)
            {
                int payloadLength = payload == null ? 0 : payload.Length;
                if (payloadLength != lastPayloadLength)
                {
                    lastPayloadLength = payloadLength;
                    // TODO: explore whether we get better compression
                    // by not storing payloadLength into prox stream?
                    posOut.Write((delta << 1) | 1);
                    posOut.Write(payloadLength);
                }
                else
                {
                    posOut.Write(delta << 1);
                }

                if (payloadLength > 0 && payload != null)
                {
                    payloadOut.WriteBytes(payload.Bytes, payload.Offset, payloadLength);
                }
            }
            else
            {
                posOut.Write(delta);
            }

            lastPosition = position;
        }

        /// <summary>Called when we are done adding positions & payloads </summary>
        public override void FinishDoc()
        {
            lastPosition = 0;
        }

        private class SepTermState : BlockTermState
        {
            public IntIndexOutput.AbstractIndex DocIndex { get; set; }
            public IntIndexOutput.AbstractIndex FreqIndex { get; set; }
            public IntIndexOutput.AbstractIndex PosIndex { get; set; }
            public long PayloadFp { get; set; }
            public long SkipFp { get; set; }
        }

        /// <summary>Called when we are done adding docs to this term </summary>
        public override void FinishTerm(BlockTermState bstate)
        {
            var state = (SepTermState)bstate;
            // TODO: -- wasteful we are counting this in two places?
            Debug.Assert(state.DocFreq > 0);
            Debug.Assert(state.DocFreq == df);

            state.DocIndex = docOut.GetIndex();
            state.DocIndex.CopyFrom(docIndex, false);
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                state.FreqIndex = freqOut.GetIndex();
                state.FreqIndex.CopyFrom(freqIndex, false);
                if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    state.PosIndex = posOut.GetIndex();
                    state.PosIndex.CopyFrom(posIndex, false);
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

            if (df >= skipMinimum)
            {
                state.SkipFp = skipOut.FilePointer;
                skipListWriter.WriteSkip(skipOut);
            }
            else
            {
                state.SkipFp = -1;
            }
            state.PayloadFp = payloadStart;

            lastDocID = 0;
            df = 0;
        }

        public override void EncodeTerm(long[] longs, DataOutput output, FieldInfo fi, BlockTermState bstate, bool absolute)
        {
            var state = (SepTermState) bstate;
            if (absolute)
            {
                lastSkipFP = 0;
                lastPayloadFP = 0;
                lastState = state;
            }
            lastState.DocIndex.CopyFrom(state.DocIndex, false);
            lastState.DocIndex.Write(output, absolute);
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                lastState.FreqIndex.CopyFrom(state.FreqIndex, false);
                lastState.FreqIndex.Write(output, absolute);
                if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    lastState.PosIndex.CopyFrom(state.PosIndex, false);
                    lastState.PosIndex.Write(output, absolute);
                    if (storePayloads)
                    {
                        if (absolute)
                        {
                            output.WriteVLong(state.PayloadFp);
                        }
                        else
                        {
                            output.WriteVLong(state.PayloadFp - lastPayloadFP);
                        }
                        lastPayloadFP = state.PayloadFp;
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
                output.WriteVLong(state.SkipFp - lastSkipFP);
            }
            lastSkipFP = state.SkipFp;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            IOUtils.Close(docOut, skipOut, freqOut, posOut, payloadOut);
        }
    }
}