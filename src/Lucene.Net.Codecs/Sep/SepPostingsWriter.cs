using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
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
    /// <para/>
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

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly Int32IndexOutput freqOut; // LUCENENET: marked readonly
        private readonly Int32IndexOutput.Index freqIndex; // LUCENENET: marked readonly

        private readonly Int32IndexOutput posOut; // LUCENENET: marked readonly
        private readonly Int32IndexOutput.Index posIndex; // LUCENENET: marked readonly

        private readonly Int32IndexOutput docOut; // LUCENENET: marked readonly
        private readonly Int32IndexOutput.Index docIndex; // LUCENENET: marked readonly

        private readonly IndexOutput payloadOut; // LUCENENET: marked readonly

        private readonly IndexOutput skipOut; // LUCENENET: marked readonly
#pragma warning restore CA2213 // Disposable fields should be disposed

        private readonly SepSkipListWriter skipListWriter;

        /// <summary>
        /// Expert: The fraction of TermDocs entries stored in skip tables,
        /// used to accelerate <see cref="Lucene.Net.Search.DocIdSetIterator.Advance(int)"/>.  Larger values result in
        /// smaller indexes, greater acceleration, but fewer accelerable cases, while
        /// smaller values result in bigger indexes, less acceleration and more
        /// accelerable cases. More detailed experiments would be useful here. 
        /// </summary>
        private readonly int skipInterval;
        private const int DEFAULT_SKIP_INTERVAL = 16;

        /// <summary>
        /// Expert: minimum docFreq to write any skip data at all.
        /// </summary>
        private readonly int skipMinimum;

        /// <summary>
        /// Expert: The maximum number of skip levels. Smaller values result in 
        /// slightly smaller indexes, but slower skipping in big posting lists.
        /// </summary>
        private readonly int maxSkipLevels = 10;

        private readonly int totalNumDocs;

        private bool storePayloads;
        private IndexOptions indexOptions;

        //private FieldInfo fieldInfo; // LUCENENET: Never read

        private int lastPayloadLength;
        private int lastPosition;
        private long payloadStart;
        private int lastDocID;
        private int df;

        private SepTermState lastState;
        private long lastPayloadFP;
        private long lastSkipFP;

        public SepPostingsWriter(SegmentWriteState state, Int32StreamFactory factory)
            : this(state, factory, DEFAULT_SKIP_INTERVAL)
        {
        }

        public SepPostingsWriter(SegmentWriteState state, Int32StreamFactory factory, int skipInterval)
        {
            freqOut = null;
            freqIndex = null;
            posOut = null;
            posIndex = null;
            payloadOut = null;
            bool success = false;
            try
            {
                this.skipInterval = skipInterval;
                this.skipMinimum = skipInterval; /* set to the same for now */
                string docFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, DOC_EXTENSION);

                docOut = factory.CreateOutput(state.Directory, docFileName, state.Context);
                docIndex = docOut.GetIndex();

                if (state.FieldInfos.HasFreq)
                {
                    string frqFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, FREQ_EXTENSION);
                    freqOut = factory.CreateOutput(state.Directory, frqFileName, state.Context);
                    freqIndex = freqOut.GetIndex();
                }

                if (state.FieldInfos.HasProx)
                {
                    string posFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, POS_EXTENSION);
                    posOut = factory.CreateOutput(state.Directory, posFileName, state.Context);
                    posIndex = posOut.GetIndex();

                    // TODO: -- only if at least one field stores payloads?
                    string payloadFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, PAYLOAD_EXTENSION);
                    payloadOut = state.Directory.CreateOutput(payloadFileName, state.Context);
                }

                string skipFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, SKIP_EXTENSION);
                skipOut = state.Directory.CreateOutput(skipFileName, state.Context);

                totalNumDocs = state.SegmentInfo.DocCount;

                skipListWriter = new SepSkipListWriter(skipInterval,
                    maxSkipLevels,
                    totalNumDocs,
                    freqOut, docOut,
                    posOut, payloadOut);

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(docOut, skipOut, freqOut, posOut, payloadOut);
                }
            }
        }
        public override void Init(IndexOutput termsOut)
        {
            CodecUtil.WriteHeader(termsOut, CODEC, VERSION_CURRENT);
            // TODO: -- just ask skipper to "start" here
            termsOut.WriteInt32(skipInterval);    // write skipInterval
            termsOut.WriteInt32(maxSkipLevels);   // write maxSkipLevels
            termsOut.WriteInt32(skipMinimum);     // write skipMinimum
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
                payloadStart = payloadOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                lastPayloadLength = -1;
            }

            skipListWriter.ResetSkip(docIndex, freqIndex, posIndex);
        }

        // Currently, this instance is re-used across fields, so
        // our parent calls setField whenever the field changes
        public override int SetField(FieldInfo fieldInfo)
        {
            //this.fieldInfo = fieldInfo; // LUCENENET: Never read
            this.indexOptions = fieldInfo.IndexOptions;
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            if (IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0)
            {
                throw UnsupportedOperationException.Create("this codec cannot index offsets");
            }
            skipListWriter.SetIndexOptions(indexOptions);
            storePayloads = indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS && fieldInfo.HasPayloads;
            lastPayloadFP = 0;
            lastSkipFP = 0;
            lastState = SetEmptyState();
            return 0;
        }

        private SepTermState SetEmptyState()
        {
            SepTermState emptyState = new SepTermState();
            emptyState.DocIndex = docOut.GetIndex();
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                emptyState.FreqIndex = freqOut.GetIndex();
                if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    emptyState.PosIndex = posOut.GetIndex();
                }
            }
            emptyState.PayloadFP = 0;
            emptyState.SkipFP = 0;
            return emptyState;
        }

        /// <summary>
        /// Adds a new doc in this term.  If this returns <c>null</c>
        /// then we just skip consuming positions/payloads. 
        /// </summary>
        public override void StartDoc(int docID, int termDocFreq)
        {
            int delta = docID - lastDocID;
            //System.out.println("SEPW: startDoc: write doc=" + docID + " delta=" + delta + " out.fp=" + docOut);

            if (docID < 0 || (df > 0 && delta <= 0))
            {
                throw new CorruptIndexException("docs out of order (" + docID + " <= " + lastDocID + " ) (docOut: " + docOut + ")");
            }

            if ((++df % skipInterval) == 0)
            {
                // TODO: -- awkward we have to make these two
                // separate calls to skipper
                //System.out.println("    buffer skip lastDocID=" + lastDocID);
                skipListWriter.SetSkipData(lastDocID, storePayloads, lastPayloadLength);
                skipListWriter.BufferSkip(df);
            }

            lastDocID = docID;
            docOut.Write(delta);
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                //System.out.println("    sepw startDoc: write freq=" + termDocFreq);
                freqOut.Write(termDocFreq);
            }
        }

        /// <summary>
        /// Add a new position &amp; payload. </summary>
        public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

            int delta = position - lastPosition;
            if (Debugging.AssertsEnabled) Debugging.Assert(delta >= 0, "position={0} lastPosition={1}", position, lastPosition);            // not quite right (if pos=0 is repeated twice we don't catch it)
            lastPosition = position;

            if (storePayloads)
            {
                int payloadLength = payload is null ? 0 : payload.Length;
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

                if (payloadLength > 0)
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

        /// <summary>Called when we are done adding positions &amp; payloads. </summary>
        public override void FinishDoc()
        {
            lastPosition = 0;
        }

        private class SepTermState : BlockTermState
        {
            public Int32IndexOutput.Index DocIndex { get; set; }
            public Int32IndexOutput.Index FreqIndex { get; set; }
            public Int32IndexOutput.Index PosIndex { get; set; }
            public long PayloadFP { get; set; }
            public long SkipFP { get; set; }
        }

        /// <summary>Called when we are done adding docs to this term. </summary>
        public override void FinishTerm(BlockTermState state)
        {
            SepTermState state_ = (SepTermState)state;
            // TODO: -- wasteful we are counting this in two places?
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(state_.DocFreq > 0);
                Debugging.Assert(state_.DocFreq == df);
            }

            state_.DocIndex = docOut.GetIndex();
            state_.DocIndex.CopyFrom(docIndex, false);
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                state_.FreqIndex = freqOut.GetIndex();
                state_.FreqIndex.CopyFrom(freqIndex, false);
                if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    state_.PosIndex = posOut.GetIndex();
                    state_.PosIndex.CopyFrom(posIndex, false);
                }
                else
                {
                    state_.PosIndex = null;
                }
            }
            else
            {
                state_.FreqIndex = null;
                state_.PosIndex = null;
            }

            if (df >= skipMinimum)
            {
                state_.SkipFP = skipOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                //System.out.println("  skipFP=" + skipFP);
                skipListWriter.WriteSkip(skipOut);
                //System.out.println("    numBytes=" + (skipOut.getFilePointer()-skipFP));
            }
            else
            {
                state_.SkipFP = -1;
            }
            state_.PayloadFP = payloadStart;

            lastDocID = 0;
            df = 0;
        }

        public override void EncodeTerm(long[] longs, DataOutput output, FieldInfo fi, BlockTermState state, bool absolute)
        {
            SepTermState state_ = (SepTermState)state;
            if (absolute)
            {
                lastSkipFP = 0;
                lastPayloadFP = 0;
                lastState = state_;
            }
            lastState.DocIndex.CopyFrom(state_.DocIndex, false);
            lastState.DocIndex.Write(output, absolute);
            if (indexOptions != IndexOptions.DOCS_ONLY)
            {
                lastState.FreqIndex.CopyFrom(state_.FreqIndex, false);
                lastState.FreqIndex.Write(output, absolute);
                if (indexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                {
                    lastState.PosIndex.CopyFrom(state_.PosIndex, false);
                    lastState.PosIndex.Write(output, absolute);
                    if (storePayloads)
                    {
                        if (absolute)
                        {
                            output.WriteVInt64(state_.PayloadFP);
                        }
                        else
                        {
                            output.WriteVInt64(state_.PayloadFP - lastPayloadFP);
                        }
                        lastPayloadFP = state_.PayloadFP;
                    }
                }
            }
            if (state_.SkipFP != -1)
            {
                if (absolute)
                {
                    output.WriteVInt64(state_.SkipFP);
                }
                else
                {
                    output.WriteVInt64(state_.SkipFP - lastSkipFP);
                }
                lastSkipFP = state_.SkipFP;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IOUtils.Dispose(docOut, skipOut, freqOut, posOut, payloadOut);
            }
        }
    }
}