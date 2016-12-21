using System;
using System.Diagnostics;

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
    using PackedInts = Util.Packed.PackedInts;
    using SegmentWriteState = Index.SegmentWriteState;
    using TermState = Index.TermState;

    /// <summary>
    /// Concrete class that writes docId(maybe frq,pos,offset,payloads) list
    /// with postings format.
    ///
    /// Postings list for each term will be stored separately.
    /// </summary>
    /// <seealso cref= Lucene41SkipWriter for details about skipping setting and postings layout.
    /// @lucene.experimental </seealso>
    public sealed class Lucene41PostingsWriter : PostingsWriterBase
    {
        /// <summary>
        /// Expert: The maximum number of skip levels. Smaller values result in
        /// slightly smaller indexes, but slower skipping in big posting lists.
        /// </summary>
        internal const int MaxSkipLevels = 10;

        internal const string TERMS_CODEC = "Lucene41PostingsWriterTerms";
        internal const string DOC_CODEC = "Lucene41PostingsWriterDoc";
        internal const string POS_CODEC = "Lucene41PostingsWriterPos";
        internal const string PAY_CODEC = "Lucene41PostingsWriterPay";

        // Increment version to change it
        internal const int VERSION_START = 0;

        internal const int VERSION_META_ARRAY = 1;
        internal const int VERSION_CHECKSUM = 2;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        internal IndexOutput DocOut;
        internal IndexOutput PosOut;
        internal IndexOutput PayOut;

        internal static readonly IntBlockTermState EmptyState = new IntBlockTermState();
        internal IntBlockTermState LastState;

        // How current field indexes postings:
        private bool FieldHasFreqs;

        private bool FieldHasPositions;
        private bool FieldHasOffsets;
        private bool FieldHasPayloads;

        // Holds starting file pointers for current term:
        private long DocStartFP;

        private long PosStartFP;
        private long PayStartFP;

        internal readonly int[] DocDeltaBuffer;
        internal readonly int[] FreqBuffer;
        private int DocBufferUpto;

        internal readonly int[] PosDeltaBuffer;
        internal readonly int[] PayloadLengthBuffer;
        internal readonly int[] OffsetStartDeltaBuffer;
        internal readonly int[] OffsetLengthBuffer;
        private int PosBufferUpto;

        private byte[] PayloadBytes;
        private int PayloadByteUpto;

        private int LastBlockDocID;
        private long LastBlockPosFP;
        private long LastBlockPayFP;
        private int LastBlockPosBufferUpto;
        private int LastBlockPayloadByteUpto;

        private int LastDocID;
        private int LastPosition;
        private int LastStartOffset;
        private int DocCount;

        internal readonly byte[] Encoded;

        private readonly ForUtil ForUtil;
        private readonly Lucene41SkipWriter SkipWriter;

        /// <summary>
        /// Creates a postings writer with the specified PackedInts overhead ratio </summary>
        // TODO: does this ctor even make sense?
        public Lucene41PostingsWriter(SegmentWriteState state, float acceptableOverheadRatio)
            : base()
        {
            DocOut = state.Directory.CreateOutput(IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, Lucene41PostingsFormat.DOC_EXTENSION), state.Context);
            IndexOutput posOut = null;
            IndexOutput payOut = null;
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(DocOut, DOC_CODEC, VERSION_CURRENT);
                ForUtil = new ForUtil(acceptableOverheadRatio, DocOut);
                if (state.FieldInfos.HasProx())
                {
                    PosDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    posOut = state.Directory.CreateOutput(IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, Lucene41PostingsFormat.POS_EXTENSION), state.Context);
                    CodecUtil.WriteHeader(posOut, POS_CODEC, VERSION_CURRENT);

                    if (state.FieldInfos.HasPayloads())
                    {
                        PayloadBytes = new byte[128];
                        PayloadLengthBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    }
                    else
                    {
                        PayloadBytes = null;
                        PayloadLengthBuffer = null;
                    }

                    if (state.FieldInfos.HasOffsets())
                    {
                        OffsetStartDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
                        OffsetLengthBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    }
                    else
                    {
                        OffsetStartDeltaBuffer = null;
                        OffsetLengthBuffer = null;
                    }

                    if (state.FieldInfos.HasPayloads() || state.FieldInfos.HasOffsets())
                    {
                        payOut = state.Directory.CreateOutput(IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, Lucene41PostingsFormat.PAY_EXTENSION), state.Context);
                        CodecUtil.WriteHeader(payOut, PAY_CODEC, VERSION_CURRENT);
                    }
                }
                else
                {
                    PosDeltaBuffer = null;
                    PayloadLengthBuffer = null;
                    OffsetStartDeltaBuffer = null;
                    OffsetLengthBuffer = null;
                    PayloadBytes = null;
                }
                this.PayOut = payOut;
                this.PosOut = posOut;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(DocOut, posOut, payOut);
                }
            }

            DocDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
            FreqBuffer = new int[ForUtil.MAX_DATA_SIZE];

            // TODO: should we try skipping every 2/4 blocks...?
            SkipWriter = new Lucene41SkipWriter(MaxSkipLevels, Lucene41PostingsFormat.BLOCK_SIZE, state.SegmentInfo.DocCount, DocOut, posOut, payOut);

            Encoded = new byte[ForUtil.MAX_ENCODED_SIZE];
        }

        /// <summary>
        /// Creates a postings writer with <code>PackedInts.COMPACT</code> </summary>
        public Lucene41PostingsWriter(SegmentWriteState state)
            : this(state, PackedInts.COMPACT)
        {
        }

        public sealed class IntBlockTermState : BlockTermState
        {
            internal long DocStartFP = 0;
            internal long PosStartFP = 0;
            internal long PayStartFP = 0;
            internal long SkipOffset = -1;
            internal long LastPosBlockOffset = -1;

            // docid when there is a single pulsed posting, otherwise -1
            // freq is always implicitly totalTermFreq in this case.
            internal int SingletonDocID = -1;

            public override object Clone()
            {
                IntBlockTermState other = new IntBlockTermState();
                other.CopyFrom(this);
                return other;
            }

            public override void CopyFrom(TermState _other)
            {
                base.CopyFrom(_other);
                IntBlockTermState other = (IntBlockTermState)_other;
                DocStartFP = other.DocStartFP;
                PosStartFP = other.PosStartFP;
                PayStartFP = other.PayStartFP;
                LastPosBlockOffset = other.LastPosBlockOffset;
                SkipOffset = other.SkipOffset;
                SingletonDocID = other.SingletonDocID;
            }

            public override string ToString()
            {
                return base.ToString() + " docStartFP=" + DocStartFP + " posStartFP=" + PosStartFP + " payStartFP=" + PayStartFP + " lastPosBlockOffset=" + LastPosBlockOffset + " singletonDocID=" + SingletonDocID;
            }
        }

        public override BlockTermState NewTermState()
        {
            return new IntBlockTermState();
        }

        public override void Init(IndexOutput termsOut)
        {
            CodecUtil.WriteHeader(termsOut, TERMS_CODEC, VERSION_CURRENT);
            termsOut.WriteVInt(Lucene41PostingsFormat.BLOCK_SIZE);
        }

        public override int SetField(FieldInfo fieldInfo)
        {
            IndexOptions? indexOptions = fieldInfo.IndexOptions;
            FieldHasFreqs = indexOptions >= IndexOptions.DOCS_AND_FREQS;
            FieldHasPositions = indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            FieldHasOffsets = indexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            FieldHasPayloads = fieldInfo.HasPayloads();
            SkipWriter.SetField(FieldHasPositions, FieldHasOffsets, FieldHasPayloads);
            LastState = EmptyState;
            if (FieldHasPositions)
            {
                if (FieldHasPayloads || FieldHasOffsets)
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
            DocStartFP = DocOut.FilePointer;
            if (FieldHasPositions)
            {
                PosStartFP = PosOut.FilePointer;
                if (FieldHasPayloads || FieldHasOffsets)
                {
                    PayStartFP = PayOut.FilePointer;
                }
            }
            LastDocID = 0;
            LastBlockDocID = -1;
            // if (DEBUG) {
            //   System.out.println("FPW.startTerm startFP=" + docStartFP);
            // }
            SkipWriter.ResetSkip();
        }

        public override void StartDoc(int docId, int termDocFreq)
        {
            // if (DEBUG) {
            //   System.out.println("FPW.startDoc docID["+docBufferUpto+"]=" + docID);
            // }
            // Have collected a block of docs, and get a new doc.
            // Should write skip data as well as postings list for
            // current block.
            if (LastBlockDocID != -1 && DocBufferUpto == 0)
            {
                // if (DEBUG) {
                //   System.out.println("  bufferSkip at writeBlock: lastDocID=" + lastBlockDocID + " docCount=" + (docCount-1));
                // }
                SkipWriter.BufferSkip(LastBlockDocID, DocCount, LastBlockPosFP, LastBlockPayFP, LastBlockPosBufferUpto, LastBlockPayloadByteUpto);
            }

            int docDelta = docId - LastDocID;

            if (docId < 0 || (DocCount > 0 && docDelta <= 0))
            {
                throw new Exception("docs out of order (" + docId + " <= " + LastDocID + " ) (docOut: " + DocOut + ")");
            }

            DocDeltaBuffer[DocBufferUpto] = docDelta;
            // if (DEBUG) {
            //   System.out.println("  docDeltaBuffer[" + docBufferUpto + "]=" + docDelta);
            // }
            if (FieldHasFreqs)
            {
                FreqBuffer[DocBufferUpto] = termDocFreq;
            }
            DocBufferUpto++;
            DocCount++;

            if (DocBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
            {
                // if (DEBUG) {
                //   System.out.println("  write docDelta block @ fp=" + docOut.getFilePointer());
                // }
                ForUtil.WriteBlock(DocDeltaBuffer, Encoded, DocOut);
                if (FieldHasFreqs)
                {
                    // if (DEBUG) {
                    //   System.out.println("  write freq block @ fp=" + docOut.getFilePointer());
                    // }
                    ForUtil.WriteBlock(FreqBuffer, Encoded, DocOut);
                }
                // NOTE: don't set docBufferUpto back to 0 here;
                // finishDoc will do so (because it needs to see that
                // the block was filled so it can save skip data)
            }

            LastDocID = docId;
            LastPosition = 0;
            LastStartOffset = 0;
        }

        /// <summary>
        /// Add a new position & payload </summary>
        public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
        {
            // if (DEBUG) {
            //   System.out.println("FPW.addPosition pos=" + position + " posBufferUpto=" + posBufferUpto + (fieldHasPayloads ? " payloadByteUpto=" + payloadByteUpto: ""));
            // }
            PosDeltaBuffer[PosBufferUpto] = position - LastPosition;
            if (FieldHasPayloads)
            {
                if (payload == null || payload.Length == 0)
                {
                    // no payload
                    PayloadLengthBuffer[PosBufferUpto] = 0;
                }
                else
                {
                    PayloadLengthBuffer[PosBufferUpto] = payload.Length;
                    if (PayloadByteUpto + payload.Length > PayloadBytes.Length)
                    {
                        PayloadBytes = ArrayUtil.Grow(PayloadBytes, PayloadByteUpto + payload.Length);
                    }
                    Array.Copy(payload.Bytes, payload.Offset, PayloadBytes, PayloadByteUpto, payload.Length);
                    PayloadByteUpto += payload.Length;
                }
            }

            if (FieldHasOffsets)
            {
                Debug.Assert(startOffset >= LastStartOffset);
                Debug.Assert(endOffset >= startOffset);
                OffsetStartDeltaBuffer[PosBufferUpto] = startOffset - LastStartOffset;
                OffsetLengthBuffer[PosBufferUpto] = endOffset - startOffset;
                LastStartOffset = startOffset;
            }

            PosBufferUpto++;
            LastPosition = position;
            if (PosBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
            {
                // if (DEBUG) {
                //   System.out.println("  write pos bulk block @ fp=" + posOut.getFilePointer());
                // }
                ForUtil.WriteBlock(PosDeltaBuffer, Encoded, PosOut);

                if (FieldHasPayloads)
                {
                    ForUtil.WriteBlock(PayloadLengthBuffer, Encoded, PayOut);
                    PayOut.WriteVInt(PayloadByteUpto);
                    PayOut.WriteBytes(PayloadBytes, 0, PayloadByteUpto);
                    PayloadByteUpto = 0;
                }
                if (FieldHasOffsets)
                {
                    ForUtil.WriteBlock(OffsetStartDeltaBuffer, Encoded, PayOut);
                    ForUtil.WriteBlock(OffsetLengthBuffer, Encoded, PayOut);
                }
                PosBufferUpto = 0;
            }
        }

        public override void FinishDoc()
        {
            // Since we don't know df for current term, we had to buffer
            // those skip data for each block, and when a new doc comes,
            // write them to skip file.
            if (DocBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
            {
                LastBlockDocID = LastDocID;
                if (PosOut != null)
                {
                    if (PayOut != null)
                    {
                        LastBlockPayFP = PayOut.FilePointer;
                    }
                    LastBlockPosFP = PosOut.FilePointer;
                    LastBlockPosBufferUpto = PosBufferUpto;
                    LastBlockPayloadByteUpto = PayloadByteUpto;
                }
                // if (DEBUG) {
                //   System.out.println("  docBufferUpto="+docBufferUpto+" now get lastBlockDocID="+lastBlockDocID+" lastBlockPosFP=" + lastBlockPosFP + " lastBlockPosBufferUpto=" +  lastBlockPosBufferUpto + " lastBlockPayloadByteUpto=" + lastBlockPayloadByteUpto);
                // }
                DocBufferUpto = 0;
            }
        }

        /// <summary>
        /// Called when we are done adding docs to this term </summary>
        public override void FinishTerm(BlockTermState _state)
        {
            IntBlockTermState state = (IntBlockTermState)_state;
            Debug.Assert(state.DocFreq > 0);

            // TODO: wasteful we are counting this (counting # docs
            // for this term) in two places?
            Debug.Assert(state.DocFreq == DocCount, state.DocFreq + " vs " + DocCount);

            // if (DEBUG) {
            //   System.out.println("FPW.finishTerm docFreq=" + state.docFreq);
            // }

            // if (DEBUG) {
            //   if (docBufferUpto > 0) {
            //     System.out.println("  write doc/freq vInt block (count=" + docBufferUpto + ") at fp=" + docOut.getFilePointer() + " docStartFP=" + docStartFP);
            //   }
            // }

            // docFreq == 1, don't write the single docid/freq to a separate file along with a pointer to it.
            int singletonDocID;
            if (state.DocFreq == 1)
            {
                // pulse the singleton docid into the term dictionary, freq is implicitly totalTermFreq
                singletonDocID = DocDeltaBuffer[0];
            }
            else
            {
                singletonDocID = -1;
                // vInt encode the remaining doc deltas and freqs:
                for (int i = 0; i < DocBufferUpto; i++)
                {
                    int docDelta = DocDeltaBuffer[i];
                    int freq = FreqBuffer[i];
                    if (!FieldHasFreqs)
                    {
                        DocOut.WriteVInt(docDelta);
                    }
                    else if (FreqBuffer[i] == 1)
                    {
                        DocOut.WriteVInt((docDelta << 1) | 1);
                    }
                    else
                    {
                        DocOut.WriteVInt(docDelta << 1);
                        DocOut.WriteVInt(freq);
                    }
                }
            }

            long lastPosBlockOffset;

            if (FieldHasPositions)
            {
                // if (DEBUG) {
                //   if (posBufferUpto > 0) {
                //     System.out.println("  write pos vInt block (count=" + posBufferUpto + ") at fp=" + posOut.getFilePointer() + " posStartFP=" + posStartFP + " hasPayloads=" + fieldHasPayloads + " hasOffsets=" + fieldHasOffsets);
                //   }
                // }

                // totalTermFreq is just total number of positions(or payloads, or offsets)
                // associated with current term.
                Debug.Assert(state.TotalTermFreq != -1);
                if (state.TotalTermFreq > Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    // record file offset for last pos in last block
                    lastPosBlockOffset = PosOut.FilePointer - PosStartFP;
                }
                else
                {
                    lastPosBlockOffset = -1;
                }
                if (PosBufferUpto > 0)
                {
                    // TODO: should we send offsets/payloads to
                    // .pay...?  seems wasteful (have to store extra
                    // vLong for low (< BLOCK_SIZE) DF terms = vast vast
                    // majority)

                    // vInt encode the remaining positions/payloads/offsets:
                    int lastPayloadLength = -1; // force first payload length to be written
                    int lastOffsetLength = -1; // force first offset length to be written
                    int payloadBytesReadUpto = 0;
                    for (int i = 0; i < PosBufferUpto; i++)
                    {
                        int posDelta = PosDeltaBuffer[i];
                        if (FieldHasPayloads)
                        {
                            int payloadLength = PayloadLengthBuffer[i];
                            if (payloadLength != lastPayloadLength)
                            {
                                lastPayloadLength = payloadLength;
                                PosOut.WriteVInt((posDelta << 1) | 1);
                                PosOut.WriteVInt(payloadLength);
                            }
                            else
                            {
                                PosOut.WriteVInt(posDelta << 1);
                            }

                            // if (DEBUG) {
                            //   System.out.println("        i=" + i + " payloadLen=" + payloadLength);
                            // }

                            if (payloadLength != 0)
                            {
                                // if (DEBUG) {
                                //   System.out.println("          write payload @ pos.fp=" + posOut.getFilePointer());
                                // }
                                PosOut.WriteBytes(PayloadBytes, payloadBytesReadUpto, payloadLength);
                                payloadBytesReadUpto += payloadLength;
                            }
                        }
                        else
                        {
                            PosOut.WriteVInt(posDelta);
                        }

                        if (FieldHasOffsets)
                        {
                            // if (DEBUG) {
                            //   System.out.println("          write offset @ pos.fp=" + posOut.getFilePointer());
                            // }
                            int delta = OffsetStartDeltaBuffer[i];
                            int length = OffsetLengthBuffer[i];
                            if (length == lastOffsetLength)
                            {
                                PosOut.WriteVInt(delta << 1);
                            }
                            else
                            {
                                PosOut.WriteVInt(delta << 1 | 1);
                                PosOut.WriteVInt(length);
                                lastOffsetLength = length;
                            }
                        }
                    }

                    if (FieldHasPayloads)
                    {
                        Debug.Assert(payloadBytesReadUpto == PayloadByteUpto);
                        PayloadByteUpto = 0;
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
            if (DocCount > Lucene41PostingsFormat.BLOCK_SIZE)
            {
                skipOffset = SkipWriter.WriteSkip(DocOut) - DocStartFP;

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
            state.DocStartFP = DocStartFP;
            state.PosStartFP = PosStartFP;
            state.PayStartFP = PayStartFP;
            state.SingletonDocID = singletonDocID;
            state.SkipOffset = skipOffset;
            state.LastPosBlockOffset = lastPosBlockOffset;
            DocBufferUpto = 0;
            PosBufferUpto = 0;
            LastDocID = 0;
            DocCount = 0;
        }

        public override void EncodeTerm(long[] longs, DataOutput @out, FieldInfo fieldInfo, BlockTermState _state, bool absolute)
        {
            IntBlockTermState state = (IntBlockTermState)_state;
            if (absolute)
            {
                LastState = EmptyState;
            }
            longs[0] = state.DocStartFP - LastState.DocStartFP;
            if (FieldHasPositions)
            {
                longs[1] = state.PosStartFP - LastState.PosStartFP;
                if (FieldHasPayloads || FieldHasOffsets)
                {
                    longs[2] = state.PayStartFP - LastState.PayStartFP;
                }
            }
            if (state.SingletonDocID != -1)
            {
                @out.WriteVInt(state.SingletonDocID);
            }
            if (FieldHasPositions)
            {
                if (state.LastPosBlockOffset != -1)
                {
                    @out.WriteVLong(state.LastPosBlockOffset);
                }
            }
            if (state.SkipOffset != -1)
            {
                @out.WriteVLong(state.SkipOffset);
            }
            LastState = state;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: add a finish() at least to PushBase? DV too...?
                bool success = false;
                try
                {
                    if (DocOut != null)
                    {
                        CodecUtil.WriteFooter(DocOut);
                    }
                    if (PosOut != null)
                    {
                        CodecUtil.WriteFooter(PosOut);
                    }
                    if (PayOut != null)
                    {
                        CodecUtil.WriteFooter(PayOut);
                    }
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Close(DocOut, PosOut, PayOut);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(DocOut, PosOut, PayOut);
                    }
                    DocOut = PosOut = PayOut = null;
                }
            }
        }
    }
}