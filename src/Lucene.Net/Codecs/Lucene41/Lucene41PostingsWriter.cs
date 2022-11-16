using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene41
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

    using ArrayUtil = Util.ArrayUtil;
    using BytesRef = Util.BytesRef;
    using DataOutput = Store.DataOutput;
    using FieldInfo = Index.FieldInfo;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IndexOutput = Store.IndexOutput;
    using IOUtils = Util.IOUtils;
    using PackedInt32s = Util.Packed.PackedInt32s;
    using SegmentWriteState = Index.SegmentWriteState;
    using TermState = Index.TermState;

    /// <summary>
    /// Concrete class that writes docId(maybe frq,pos,offset,payloads) list
    /// with postings format.
    /// <para/>
    /// Postings list for each term will be stored separately.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Lucene41SkipWriter"/> for details about skipping setting and postings layout.
    public sealed class Lucene41PostingsWriter : PostingsWriterBase
    {
        /// <summary>
        /// Expert: The maximum number of skip levels. Smaller values result in
        /// slightly smaller indexes, but slower skipping in big posting lists.
        /// </summary>
        internal const int maxSkipLevels = 10;

        internal const string TERMS_CODEC = "Lucene41PostingsWriterTerms";
        internal const string DOC_CODEC = "Lucene41PostingsWriterDoc";
        internal const string POS_CODEC = "Lucene41PostingsWriterPos";
        internal const string PAY_CODEC = "Lucene41PostingsWriterPay";

        // Increment version to change it
        internal const int VERSION_START = 0;

        internal const int VERSION_META_ARRAY = 1;
        internal const int VERSION_CHECKSUM = 2;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

#pragma warning disable CA2213 // Disposable fields should be disposed
        internal IndexOutput docOut;
        internal IndexOutput posOut;
        internal IndexOutput payOut;
#pragma warning restore CA2213 // Disposable fields should be disposed

        internal static readonly Int32BlockTermState emptyState = new Int32BlockTermState();
        internal Int32BlockTermState lastState;

        // How current field indexes postings:
        private bool fieldHasFreqs;

        private bool fieldHasPositions;
        private bool fieldHasOffsets;
        private bool fieldHasPayloads;

        // Holds starting file pointers for current term:
        private long docStartFP;

        private long posStartFP;
        private long payStartFP;

        internal readonly int[] docDeltaBuffer;
        internal readonly int[] freqBuffer;
        private int docBufferUpto;

        internal readonly int[] posDeltaBuffer;
        internal readonly int[] payloadLengthBuffer;
        internal readonly int[] offsetStartDeltaBuffer;
        internal readonly int[] offsetLengthBuffer;
        private int posBufferUpto;

        private byte[] payloadBytes;
        private int payloadByteUpto;

        private int lastBlockDocID;
        private long lastBlockPosFP;
        private long lastBlockPayFP;
        private int lastBlockPosBufferUpto;
        private int lastBlockPayloadByteUpto;

        private int lastDocID;
        private int lastPosition;
        private int lastStartOffset;
        private int docCount;

        internal readonly byte[] encoded;

        private readonly ForUtil forUtil;
        private readonly Lucene41SkipWriter skipWriter;

        /// <summary>
        /// Creates a postings writer with the specified PackedInts overhead ratio </summary>
        // TODO: does this ctor even make sense?
        public Lucene41PostingsWriter(SegmentWriteState state, float acceptableOverheadRatio)
            : base()
        {
            docOut = state.Directory.CreateOutput(IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, Lucene41PostingsFormat.DOC_EXTENSION), state.Context);
            IndexOutput posOut = null;
            IndexOutput payOut = null;
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(docOut, DOC_CODEC, VERSION_CURRENT);
                forUtil = new ForUtil(acceptableOverheadRatio, docOut);
                if (state.FieldInfos.HasProx)
                {
                    posDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    posOut = state.Directory.CreateOutput(IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, Lucene41PostingsFormat.POS_EXTENSION), state.Context);
                    CodecUtil.WriteHeader(posOut, POS_CODEC, VERSION_CURRENT);

                    if (state.FieldInfos.HasPayloads)
                    {
                        payloadBytes = new byte[128];
                        payloadLengthBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    }
                    else
                    {
                        payloadBytes = null;
                        payloadLengthBuffer = null;
                    }

                    if (state.FieldInfos.HasOffsets)
                    {
                        offsetStartDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
                        offsetLengthBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    }
                    else
                    {
                        offsetStartDeltaBuffer = null;
                        offsetLengthBuffer = null;
                    }

                    if (state.FieldInfos.HasPayloads || state.FieldInfos.HasOffsets)
                    {
                        payOut = state.Directory.CreateOutput(IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, Lucene41PostingsFormat.PAY_EXTENSION), state.Context);
                        CodecUtil.WriteHeader(payOut, PAY_CODEC, VERSION_CURRENT);
                    }
                }
                else
                {
                    posDeltaBuffer = null;
                    payloadLengthBuffer = null;
                    offsetStartDeltaBuffer = null;
                    offsetLengthBuffer = null;
                    payloadBytes = null;
                }
                this.payOut = payOut;
                this.posOut = posOut;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(docOut, posOut, payOut);
                }
            }

            docDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
            freqBuffer = new int[ForUtil.MAX_DATA_SIZE];

            // TODO: should we try skipping every 2/4 blocks...?
            skipWriter = new Lucene41SkipWriter(maxSkipLevels, Lucene41PostingsFormat.BLOCK_SIZE, state.SegmentInfo.DocCount, docOut, posOut, payOut);

            encoded = new byte[ForUtil.MAX_ENCODED_SIZE];
        }

        /// <summary>
        /// Creates a postings writer with <code>PackedInts.COMPACT</code> </summary>
        public Lucene41PostingsWriter(SegmentWriteState state)
            : this(state, PackedInt32s.COMPACT)
        {
        }

        /// <summary>
        /// NOTE: This was IntBlockTermState in Lucene
        /// </summary>
        public sealed class Int32BlockTermState : BlockTermState
        {
            internal long docStartFP = 0;
            internal long posStartFP = 0;
            internal long payStartFP = 0;
            internal long skipOffset = -1;
            internal long lastPosBlockOffset = -1;

            // docid when there is a single pulsed posting, otherwise -1
            // freq is always implicitly totalTermFreq in this case.
            internal int singletonDocID = -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override object Clone()
            {
                Int32BlockTermState other = new Int32BlockTermState();
                other.CopyFrom(this);
                return other;
            }

            public override void CopyFrom(TermState other)
            {
                base.CopyFrom(other);
                Int32BlockTermState other2 = (Int32BlockTermState)other;
                docStartFP = other2.docStartFP;
                posStartFP = other2.posStartFP;
                payStartFP = other2.payStartFP;
                lastPosBlockOffset = other2.lastPosBlockOffset;
                skipOffset = other2.skipOffset;
                singletonDocID = other2.singletonDocID;
            }

            public override string ToString()
            {
                return base.ToString() + " docStartFP=" + docStartFP + " posStartFP=" + posStartFP + " payStartFP=" + payStartFP + " lastPosBlockOffset=" + lastPosBlockOffset + " singletonDocID=" + singletonDocID;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override BlockTermState NewTermState()
        {
            return new Int32BlockTermState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Init(IndexOutput termsOut)
        {
            CodecUtil.WriteHeader(termsOut, TERMS_CODEC, VERSION_CURRENT);
            termsOut.WriteVInt32(Lucene41PostingsFormat.BLOCK_SIZE);
        }

        public override int SetField(FieldInfo fieldInfo)
        {
            IndexOptions indexOptions = fieldInfo.IndexOptions;
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            fieldHasFreqs = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;
            fieldHasPositions = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
            fieldHasOffsets = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            fieldHasPayloads = fieldInfo.HasPayloads;
            skipWriter.SetField(fieldHasPositions, fieldHasOffsets, fieldHasPayloads);
            lastState = emptyState;
            if (fieldHasPositions)
            {
                if (fieldHasPayloads || fieldHasOffsets)
                {
                    return 3; // doc + pos + pay FP
                }
                else
                {
                    return 2; // doc + pos FP
                }
            }
            else
            {
                return 1; // doc FP
            }
        }

        public override void StartTerm()
        {
            docStartFP = docOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            if (fieldHasPositions)
            {
                posStartFP = posOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                if (fieldHasPayloads || fieldHasOffsets)
                {
                    payStartFP = payOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
            }
            lastDocID = 0;
            lastBlockDocID = -1;
            // if (DEBUG) {
            //   System.out.println("FPW.startTerm startFP=" + docStartFP);
            // }
            skipWriter.ResetSkip();
        }

        public override void StartDoc(int docId, int termDocFreq)
        {
            // if (DEBUG) {
            //   System.out.println("FPW.startDoc docID["+docBufferUpto+"]=" + docID);
            // }
            // Have collected a block of docs, and get a new doc.
            // Should write skip data as well as postings list for
            // current block.
            if (lastBlockDocID != -1 && docBufferUpto == 0)
            {
                // if (DEBUG) {
                //   System.out.println("  bufferSkip at writeBlock: lastDocID=" + lastBlockDocID + " docCount=" + (docCount-1));
                // }
                skipWriter.BufferSkip(lastBlockDocID, docCount, lastBlockPosFP, lastBlockPayFP, lastBlockPosBufferUpto, lastBlockPayloadByteUpto);
            }

            int docDelta = docId - lastDocID;

            if (docId < 0 || (docCount > 0 && docDelta <= 0))
            {
                throw new CorruptIndexException("docs out of order (" + docId + " <= " + lastDocID + " ) (docOut: " + docOut + ")");
            }

            docDeltaBuffer[docBufferUpto] = docDelta;
            // if (DEBUG) {
            //   System.out.println("  docDeltaBuffer[" + docBufferUpto + "]=" + docDelta);
            // }
            if (fieldHasFreqs)
            {
                freqBuffer[docBufferUpto] = termDocFreq;
            }
            docBufferUpto++;
            docCount++;

            if (docBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
            {
                // if (DEBUG) {
                //   System.out.println("  write docDelta block @ fp=" + docOut.getFilePointer());
                // }
                forUtil.WriteBlock(docDeltaBuffer, encoded, docOut);
                if (fieldHasFreqs)
                {
                    // if (DEBUG) {
                    //   System.out.println("  write freq block @ fp=" + docOut.getFilePointer());
                    // }
                    forUtil.WriteBlock(freqBuffer, encoded, docOut);
                }
                // NOTE: don't set docBufferUpto back to 0 here;
                // finishDoc will do so (because it needs to see that
                // the block was filled so it can save skip data)
            }

            lastDocID = docId;
            lastPosition = 0;
            lastStartOffset = 0;
        }

        /// <summary>
        /// Add a new position &amp; payload </summary>
        public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
        {
            // if (DEBUG) {
            //   System.out.println("FPW.addPosition pos=" + position + " posBufferUpto=" + posBufferUpto + (fieldHasPayloads ? " payloadByteUpto=" + payloadByteUpto: ""));
            // }
            posDeltaBuffer[posBufferUpto] = position - lastPosition;
            if (fieldHasPayloads)
            {
                if (payload is null || payload.Length == 0)
                {
                    // no payload
                    payloadLengthBuffer[posBufferUpto] = 0;
                }
                else
                {
                    payloadLengthBuffer[posBufferUpto] = payload.Length;
                    if (payloadByteUpto + payload.Length > payloadBytes.Length)
                    {
                        payloadBytes = ArrayUtil.Grow(payloadBytes, payloadByteUpto + payload.Length);
                    }
                    Arrays.Copy(payload.Bytes, payload.Offset, payloadBytes, payloadByteUpto, payload.Length);
                    payloadByteUpto += payload.Length;
                }
            }

            if (fieldHasOffsets)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(startOffset >= lastStartOffset);
                    Debugging.Assert(endOffset >= startOffset);
                }
                offsetStartDeltaBuffer[posBufferUpto] = startOffset - lastStartOffset;
                offsetLengthBuffer[posBufferUpto] = endOffset - startOffset;
                lastStartOffset = startOffset;
            }

            posBufferUpto++;
            lastPosition = position;
            if (posBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
            {
                // if (DEBUG) {
                //   System.out.println("  write pos bulk block @ fp=" + posOut.getFilePointer());
                // }
                forUtil.WriteBlock(posDeltaBuffer, encoded, posOut);

                if (fieldHasPayloads)
                {
                    forUtil.WriteBlock(payloadLengthBuffer, encoded, payOut);
                    payOut.WriteVInt32(payloadByteUpto);
                    payOut.WriteBytes(payloadBytes, 0, payloadByteUpto);
                    payloadByteUpto = 0;
                }
                if (fieldHasOffsets)
                {
                    forUtil.WriteBlock(offsetStartDeltaBuffer, encoded, payOut);
                    forUtil.WriteBlock(offsetLengthBuffer, encoded, payOut);
                }
                posBufferUpto = 0;
            }
        }

        public override void FinishDoc()
        {
            // Since we don't know df for current term, we had to buffer
            // those skip data for each block, and when a new doc comes,
            // write them to skip file.
            if (docBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
            {
                lastBlockDocID = lastDocID;
                if (posOut != null)
                {
                    if (payOut != null)
                    {
                        lastBlockPayFP = payOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    }
                    lastBlockPosFP = posOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    lastBlockPosBufferUpto = posBufferUpto;
                    lastBlockPayloadByteUpto = payloadByteUpto;
                }
                // if (DEBUG) {
                //   System.out.println("  docBufferUpto="+docBufferUpto+" now get lastBlockDocID="+lastBlockDocID+" lastBlockPosFP=" + lastBlockPosFP + " lastBlockPosBufferUpto=" +  lastBlockPosBufferUpto + " lastBlockPayloadByteUpto=" + lastBlockPayloadByteUpto);
                // }
                docBufferUpto = 0;
            }
        }

        /// <summary>
        /// Called when we are done adding docs to this term. </summary>
        public override void FinishTerm(BlockTermState state)
        {
            Int32BlockTermState state2 = (Int32BlockTermState)state;
            if (Debugging.AssertsEnabled) Debugging.Assert(state2.DocFreq > 0);

            // TODO: wasteful we are counting this (counting # docs
            // for this term) in two places?
            if (Debugging.AssertsEnabled) Debugging.Assert(state2.DocFreq == docCount, "{0} vs {1}", state2.DocFreq, docCount);

            // if (DEBUG) {
            //   System.out.println("FPW.finishTerm docFreq=" + state2.docFreq);
            // }

            // if (DEBUG) {
            //   if (docBufferUpto > 0) {
            //     System.out.println("  write doc/freq vInt block (count=" + docBufferUpto + ") at fp=" + docOut.getFilePointer() + " docStartFP=" + docStartFP);
            //   }
            // }

            // docFreq == 1, don't write the single docid/freq to a separate file along with a pointer to it.
            int singletonDocID;
            if (state2.DocFreq == 1)
            {
                // pulse the singleton docid into the term dictionary, freq is implicitly totalTermFreq
                singletonDocID = docDeltaBuffer[0];
            }
            else
            {
                singletonDocID = -1;
                // vInt encode the remaining doc deltas and freqs:
                for (int i = 0; i < docBufferUpto; i++)
                {
                    int docDelta = docDeltaBuffer[i];
                    int freq = freqBuffer[i];
                    if (!fieldHasFreqs)
                    {
                        docOut.WriteVInt32(docDelta);
                    }
                    else if (freqBuffer[i] == 1)
                    {
                        docOut.WriteVInt32((docDelta << 1) | 1);
                    }
                    else
                    {
                        docOut.WriteVInt32(docDelta << 1);
                        docOut.WriteVInt32(freq);
                    }
                }
            }

            long lastPosBlockOffset;

            if (fieldHasPositions)
            {
                // if (DEBUG) {
                //   if (posBufferUpto > 0) {
                //     System.out.println("  write pos vInt block (count=" + posBufferUpto + ") at fp=" + posOut.getFilePointer() + " posStartFP=" + posStartFP + " hasPayloads=" + fieldHasPayloads + " hasOffsets=" + fieldHasOffsets);
                //   }
                // }

                // totalTermFreq is just total number of positions(or payloads, or offsets)
                // associated with current term.
                if (Debugging.AssertsEnabled) Debugging.Assert(state2.TotalTermFreq != -1);
                if (state2.TotalTermFreq > Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    // record file offset for last pos in last block
                    lastPosBlockOffset = posOut.Position - posStartFP; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
                else
                {
                    lastPosBlockOffset = -1;
                }
                if (posBufferUpto > 0)
                {
                    // TODO: should we send offsets/payloads to
                    // .pay...?  seems wasteful (have to store extra
                    // vLong for low (< BLOCK_SIZE) DF terms = vast vast
                    // majority)

                    // vInt encode the remaining positions/payloads/offsets:
                    int lastPayloadLength = -1; // force first payload length to be written
                    int lastOffsetLength = -1; // force first offset length to be written
                    int payloadBytesReadUpto = 0;
                    for (int i = 0; i < posBufferUpto; i++)
                    {
                        int posDelta = posDeltaBuffer[i];
                        if (fieldHasPayloads)
                        {
                            int payloadLength = payloadLengthBuffer[i];
                            if (payloadLength != lastPayloadLength)
                            {
                                lastPayloadLength = payloadLength;
                                posOut.WriteVInt32((posDelta << 1) | 1);
                                posOut.WriteVInt32(payloadLength);
                            }
                            else
                            {
                                posOut.WriteVInt32(posDelta << 1);
                            }

                            // if (DEBUG) {
                            //   System.out.println("        i=" + i + " payloadLen=" + payloadLength);
                            // }

                            if (payloadLength != 0)
                            {
                                // if (DEBUG) {
                                //   System.out.println("          write payload @ pos.fp=" + posOut.getFilePointer());
                                // }
                                posOut.WriteBytes(payloadBytes, payloadBytesReadUpto, payloadLength);
                                payloadBytesReadUpto += payloadLength;
                            }
                        }
                        else
                        {
                            posOut.WriteVInt32(posDelta);
                        }

                        if (fieldHasOffsets)
                        {
                            // if (DEBUG) {
                            //   System.out.println("          write offset @ pos.fp=" + posOut.getFilePointer());
                            // }
                            int delta = offsetStartDeltaBuffer[i];
                            int length = offsetLengthBuffer[i];
                            if (length == lastOffsetLength)
                            {
                                posOut.WriteVInt32(delta << 1);
                            }
                            else
                            {
                                posOut.WriteVInt32(delta << 1 | 1);
                                posOut.WriteVInt32(length);
                                lastOffsetLength = length;
                            }
                        }
                    }

                    if (fieldHasPayloads)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(payloadBytesReadUpto == payloadByteUpto);
                        payloadByteUpto = 0;
                    }
                }
                // if (DEBUG) {
                //   System.out.println("  totalTermFreq=" + state.totalTermFreq + " lastPosBlockOffset=" + lastPosBlockOffset);
                // }
            }
            else
            {
                lastPosBlockOffset = -1;
            }

            long skipOffset;
            if (docCount > Lucene41PostingsFormat.BLOCK_SIZE)
            {
                skipOffset = skipWriter.WriteSkip(docOut) - docStartFP;

                // if (DEBUG) {
                //   System.out.println("skip packet " + (docOut.getFilePointer() - (docStartFP + skipOffset)) + " bytes");
                // }
            }
            else
            {
                skipOffset = -1;
                // if (DEBUG) {
                //   System.out.println("  no skip: docCount=" + docCount);
                // }
            }
            // if (DEBUG) {
            //   System.out.println("  payStartFP=" + payStartFP);
            // }
            state2.docStartFP = docStartFP;
            state2.posStartFP = posStartFP;
            state2.payStartFP = payStartFP;
            state2.singletonDocID = singletonDocID;
            state2.skipOffset = skipOffset;
            state2.lastPosBlockOffset = lastPosBlockOffset;
            docBufferUpto = 0;
            posBufferUpto = 0;
            lastDocID = 0;
            docCount = 0;
        }

        public override void EncodeTerm(long[] longs, DataOutput @out, FieldInfo fieldInfo, BlockTermState state, bool absolute)
        {
            Int32BlockTermState state2 = (Int32BlockTermState)state;
            if (absolute)
            {
                lastState = emptyState;
            }
            longs[0] = state2.docStartFP - lastState.docStartFP;
            if (fieldHasPositions)
            {
                longs[1] = state2.posStartFP - lastState.posStartFP;
                if (fieldHasPayloads || fieldHasOffsets)
                {
                    longs[2] = state2.payStartFP - lastState.payStartFP;
                }
            }
            if (state2.singletonDocID != -1)
            {
                @out.WriteVInt32(state2.singletonDocID);
            }
            if (fieldHasPositions)
            {
                if (state2.lastPosBlockOffset != -1)
                {
                    @out.WriteVInt64(state2.lastPosBlockOffset);
                }
            }
            if (state2.skipOffset != -1)
            {
                @out.WriteVInt64(state2.skipOffset);
            }
            lastState = state2;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: add a finish() at least to PushBase? DV too...?
                bool success = false;
                try
                {
                    if (docOut != null)
                    {
                        CodecUtil.WriteFooter(docOut);
                    }
                    if (posOut != null)
                    {
                        CodecUtil.WriteFooter(posOut);
                    }
                    if (payOut != null)
                    {
                        CodecUtil.WriteFooter(payOut);
                    }
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Dispose(docOut, posOut, payOut);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(docOut, posOut, payOut);
                    }
                    docOut = posOut = payOut = null;
                }
            }
        }
    }
}