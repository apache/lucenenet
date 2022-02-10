using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene40
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
    /// Concrete class that writes the 4.0 frq/prx postings format.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Lucene40PostingsFormat"/>
#pragma warning disable 612, 618
    public sealed class Lucene40PostingsWriter : PostingsWriterBase
    {
        internal readonly IndexOutput freqOut;
        internal readonly IndexOutput proxOut;
        internal readonly Lucene40SkipListWriter skipListWriter;

        /// <summary>
        /// Expert: The fraction of TermDocs entries stored in skip tables,
        /// used to accelerate <see cref="Search.DocIdSetIterator.Advance(int)"/>.  Larger values result in
        /// smaller indexes, greater acceleration, but fewer accelerable cases, while
        /// smaller values result in bigger indexes, less acceleration and more
        /// accelerable cases. More detailed experiments would be useful here.
        /// </summary>
        internal const int DEFAULT_SKIP_INTERVAL = 16;

        internal readonly int skipInterval;

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

        internal IndexOptions indexOptions;
        internal bool storePayloads;
        internal bool storeOffsets;

        // Starts a new term
        internal long freqStart;

        internal long proxStart;
        internal FieldInfo fieldInfo;
        internal int lastPayloadLength;
        internal int lastOffsetLength;
        internal int lastPosition;
        internal int lastOffset;

        internal static readonly StandardTermState emptyState = new StandardTermState();
        internal StandardTermState lastState;

        // private String segment;

        /// <summary>
        /// Creates a <see cref="Lucene40PostingsWriter"/>, with the
        /// <see cref="DEFAULT_SKIP_INTERVAL"/>.
        /// </summary>
        public Lucene40PostingsWriter(SegmentWriteState state)
            : this(state, DEFAULT_SKIP_INTERVAL)
        {
        }

        /// <summary>
        /// Creates a <see cref="Lucene40PostingsWriter"/>, with the
        /// specified <paramref name="skipInterval"/>.
        /// </summary>
        public Lucene40PostingsWriter(SegmentWriteState state, int skipInterval)
            : base()
        {
            this.skipInterval = skipInterval;
            this.skipMinimum = skipInterval; // set to the same for now
            // this.segment = state.segmentName;
            string fileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, Lucene40PostingsFormat.FREQ_EXTENSION);
            freqOut = state.Directory.CreateOutput(fileName, state.Context);
            bool success = false;
            IndexOutput proxOut = null;
            try
            {
                CodecUtil.WriteHeader(freqOut, Lucene40PostingsReader.FRQ_CODEC, Lucene40PostingsReader.VERSION_CURRENT);
                // TODO: this is a best effort, if one of these fields has no postings
                // then we make an empty prx file, same as if we are wrapped in
                // per-field postingsformat. maybe... we shouldn't
                // bother w/ this opto?  just create empty prx file...?
                if (state.FieldInfos.HasProx)
                {
                    // At least one field does not omit TF, so create the
                    // prox file
                    fileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, Lucene40PostingsFormat.PROX_EXTENSION);
                    proxOut = state.Directory.CreateOutput(fileName, state.Context);
                    CodecUtil.WriteHeader(proxOut, Lucene40PostingsReader.PRX_CODEC, Lucene40PostingsReader.VERSION_CURRENT);
                }
                else
                {
                    // Every field omits TF so we will write no prox file
                    proxOut = null;
                }
                this.proxOut = proxOut;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(freqOut, proxOut);
                }
            }

            totalNumDocs = state.SegmentInfo.DocCount;

            skipListWriter = new Lucene40SkipListWriter(skipInterval, maxSkipLevels, totalNumDocs, freqOut, proxOut);
        }

        public override void Init(IndexOutput termsOut)
        {
            CodecUtil.WriteHeader(termsOut, Lucene40PostingsReader.TERMS_CODEC, Lucene40PostingsReader.VERSION_CURRENT);
            termsOut.WriteInt32(skipInterval); // write skipInterval
            termsOut.WriteInt32(maxSkipLevels); // write maxSkipLevels
            termsOut.WriteInt32(skipMinimum); // write skipMinimum
        }

        public override BlockTermState NewTermState()
        {
            return new StandardTermState();
        }

        public override void StartTerm()
        {
            freqStart = freqOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            //if (DEBUG) System.out.println("SPW: startTerm freqOut.fp=" + freqStart);
            if (proxOut != null)
            {
                proxStart = proxOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            // force first payload to write its length
            lastPayloadLength = -1;
            // force first offset to write its length
            lastOffsetLength = -1;
            skipListWriter.ResetSkip();
        }

        // Currently, this instance is re-used across fields, so
        // our parent calls setField whenever the field changes
        public override int SetField(FieldInfo fieldInfo)
        {
            //System.out.println("SPW: setField");
            /*
            if (BlockTreeTermsWriter.DEBUG && fieldInfo.Name.Equals("id", StringComparison.Ordinal)) {
              DEBUG = true;
            } else {
              DEBUG = false;
            }
            */
            this.fieldInfo = fieldInfo;
            indexOptions = fieldInfo.IndexOptions;

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            storeOffsets = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            storePayloads = fieldInfo.HasPayloads;
            lastState = emptyState;
            //System.out.println("  set init blockFreqStart=" + freqStart);
            //System.out.println("  set init blockProxStart=" + proxStart);
            return 0;
        }

        internal int lastDocID;
        internal int df;

        public override void StartDoc(int docID, int termDocFreq)
        {
            // if (DEBUG) System.out.println("SPW:   startDoc seg=" + segment + " docID=" + docID + " tf=" + termDocFreq + " freqOut.fp=" + freqOut.getFilePointer());

            int delta = docID - lastDocID;

            if (docID < 0 || (df > 0 && delta <= 0))
            {
                throw new CorruptIndexException("docs out of order (" + docID + " <= " + lastDocID + " ) (freqOut: " + freqOut + ")");
            }

            if ((++df % skipInterval) == 0)
            {
                skipListWriter.SetSkipData(lastDocID, storePayloads, lastPayloadLength, storeOffsets, lastOffsetLength);
                skipListWriter.BufferSkip(df);
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(docID < totalNumDocs,"docID={0} totalNumDocs={1}", docID, totalNumDocs);

            lastDocID = docID;
            if (indexOptions == IndexOptions.DOCS_ONLY)
            {
                freqOut.WriteVInt32(delta);
            }
            else if (1 == termDocFreq)
            {
                freqOut.WriteVInt32((delta << 1) | 1);
            }
            else
            {
                freqOut.WriteVInt32(delta << 1);
                freqOut.WriteVInt32(termDocFreq);
            }

            lastPosition = 0;
            lastOffset = 0;
        }

        /// <summary>
        /// Add a new <paramref name="position"/> &amp; <paramref name="payload"/>. </summary>
        public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
        {
            //if (DEBUG) System.out.println("SPW:     addPos pos=" + position + " payload=" + (payload is null ? "null" : (payload.Length + " bytes")) + " proxFP=" + proxOut.getFilePointer());
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            if (Debugging.AssertsEnabled) Debugging.Assert(IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0, "invalid indexOptions: {0}", indexOptions);
            if (Debugging.AssertsEnabled) Debugging.Assert(proxOut != null);

            int delta = position - lastPosition;

            if (Debugging.AssertsEnabled) Debugging.Assert(delta >= 0,"position={0} lastPosition={1}", position, lastPosition); // not quite right (if pos=0 is repeated twice we don't catch it)

            lastPosition = position;

            int payloadLength = 0;

            if (storePayloads)
            {
                payloadLength = payload is null ? 0 : payload.Length;

                if (payloadLength != lastPayloadLength)
                {
                    lastPayloadLength = payloadLength;
                    proxOut.WriteVInt32((delta << 1) | 1);
                    proxOut.WriteVInt32(payloadLength);
                }
                else
                {
                    proxOut.WriteVInt32(delta << 1);
                }
            }
            else
            {
                proxOut.WriteVInt32(delta);
            }

            if (storeOffsets)
            {
                // don't use startOffset - lastEndOffset, because this creates lots of negative vints for synonyms,
                // and the numbers aren't that much smaller anyways.
                int offsetDelta = startOffset - lastOffset;
                int offsetLength = endOffset - startOffset;
                if (Debugging.AssertsEnabled) Debugging.Assert(offsetDelta >= 0 && offsetLength >= 0, "startOffset={0},lastOffset={1},endOffset={2}", startOffset, lastOffset, endOffset);
                if (offsetLength != lastOffsetLength)
                {
                    proxOut.WriteVInt32(offsetDelta << 1 | 1);
                    proxOut.WriteVInt32(offsetLength);
                }
                else
                {
                    proxOut.WriteVInt32(offsetDelta << 1);
                }
                lastOffset = startOffset;
                lastOffsetLength = offsetLength;
            }

            if (payloadLength > 0)
            {
                proxOut.WriteBytes(payload.Bytes, payload.Offset, payloadLength);
            }
        }

        public override void FinishDoc()
        {
        }

        internal class StandardTermState : BlockTermState
        {
            public long FreqStart { get; set; }
            public long ProxStart { get; set; }
            public long SkipOffset { get; set; }
        }

        /// <summary>
        /// Called when we are done adding docs to this term. </summary>
        public override void FinishTerm(BlockTermState state)
        {
            StandardTermState state_ = (StandardTermState)state;
            // if (DEBUG) System.out.println("SPW: finishTerm seg=" + segment + " freqStart=" + freqStart);
            if (Debugging.AssertsEnabled) Debugging.Assert(state_.DocFreq > 0);

            // TODO: wasteful we are counting this (counting # docs
            // for this term) in two places?
            if (Debugging.AssertsEnabled) Debugging.Assert(state_.DocFreq == df);
            state_.FreqStart = freqStart;
            state_.ProxStart = proxStart;
            if (df >= skipMinimum)
            {
                state_.SkipOffset = skipListWriter.WriteSkip(freqOut) - freqStart;
            }
            else
            {
                state_.SkipOffset = -1;
            }
            lastDocID = 0;
            df = 0;
        }

        public override void EncodeTerm(long[] empty, DataOutput @out, FieldInfo fieldInfo, BlockTermState state, bool absolute)
        {
            StandardTermState state_ = (StandardTermState)state;
            if (absolute)
            {
                lastState = emptyState;
            }
            @out.WriteVInt64(state_.FreqStart - lastState.FreqStart);
            if (state_.SkipOffset != -1)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state_.SkipOffset > 0);
                @out.WriteVInt64(state_.SkipOffset);
            }
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            if (IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
            {
                @out.WriteVInt64(state_.ProxStart - lastState.ProxStart);
            }
            lastState = state_;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    freqOut.Dispose();
                }
                finally
                {
                    if (proxOut != null)
                    {
                        proxOut.Dispose();
                    }
                }
            }
        }
    }
#pragma warning restore 612, 618
}