using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
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

    /// <summary>
    /// Concrete class that reads docId(maybe frq,pos,offset,payloads) list
    /// with postings format.
    /// </summary>
    /// <seealso cref= Lucene41SkipReader for details
    /// @lucene.experimental </seealso>
    public sealed class Lucene41PostingsReader : PostingsReaderBase
    {
        private readonly IndexInput DocIn;
        private readonly IndexInput PosIn;
        private readonly IndexInput PayIn;

        private readonly ForUtil forUtil;
        private int Version;

        // public static boolean DEBUG = false;

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene41PostingsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo segmentInfo, IOContext ioContext, string segmentSuffix)
        {
            bool success = false;
            IndexInput docIn = null;
            IndexInput posIn = null;
            IndexInput payIn = null;
            try
            {
                docIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, Lucene41PostingsFormat.DOC_EXTENSION), ioContext);
                Version = CodecUtil.CheckHeader(docIn, Lucene41PostingsWriter.DOC_CODEC, Lucene41PostingsWriter.VERSION_START, Lucene41PostingsWriter.VERSION_CURRENT);
                forUtil = new ForUtil(docIn);

                if (fieldInfos.HasProx())
                {
                    posIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, Lucene41PostingsFormat.POS_EXTENSION), ioContext);
                    CodecUtil.CheckHeader(posIn, Lucene41PostingsWriter.POS_CODEC, Version, Version);

                    if (fieldInfos.HasPayloads() || fieldInfos.HasOffsets())
                    {
                        payIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, Lucene41PostingsFormat.PAY_EXTENSION), ioContext);
                        CodecUtil.CheckHeader(payIn, Lucene41PostingsWriter.PAY_CODEC, Version, Version);
                    }
                }

                this.DocIn = docIn;
                this.PosIn = posIn;
                this.PayIn = payIn;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(docIn, posIn, payIn);
                }
            }
        }

        public override void Init(IndexInput termsIn)
        {
            // Make sure we are talking to the matching postings writer
            CodecUtil.CheckHeader(termsIn, Lucene41PostingsWriter.TERMS_CODEC, Lucene41PostingsWriter.VERSION_START, Lucene41PostingsWriter.VERSION_CURRENT);
            int indexBlockSize = termsIn.ReadVInt();
            if (indexBlockSize != Lucene41PostingsFormat.BLOCK_SIZE)
            {
                throw new InvalidOperationException("index-time BLOCK_SIZE (" + indexBlockSize + ") != read-time BLOCK_SIZE (" + Lucene41PostingsFormat.BLOCK_SIZE + ")");
            }
        }

        /// <summary>
        /// Read values that have been written using variable-length encoding instead of bit-packing.
        /// </summary>
        internal static void ReadVIntBlock(IndexInput docIn, int[] docBuffer, int[] freqBuffer, int num, bool indexHasFreq)
        {
            if (indexHasFreq)
            {
                for (int i = 0; i < num; i++)
                {
                    int code = docIn.ReadVInt();
                    docBuffer[i] = (int)((uint)code >> 1);
                    if ((code & 1) != 0)
                    {
                        freqBuffer[i] = 1;
                    }
                    else
                    {
                        freqBuffer[i] = docIn.ReadVInt();
                    }
                }
            }
            else
            {
                for (int i = 0; i < num; i++)
                {
                    docBuffer[i] = docIn.ReadVInt();
                }
            }
        }

        public override BlockTermState NewTermState()
        {
            return new Lucene41PostingsWriter.IntBlockTermState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IOUtils.Close(DocIn, PosIn, PayIn);
        }

        public override void DecodeTerm(long[] longs, DataInput @in, FieldInfo fieldInfo, BlockTermState _termState, bool absolute)
        {
            Lucene41PostingsWriter.IntBlockTermState termState = (Lucene41PostingsWriter.IntBlockTermState)_termState;
            bool fieldHasPositions = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            bool fieldHasOffsets = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            bool fieldHasPayloads = fieldInfo.HasPayloads();

            if (absolute)
            {
                termState.DocStartFP = 0;
                termState.PosStartFP = 0;
                termState.PayStartFP = 0;
            }
            if (Version < Lucene41PostingsWriter.VERSION_META_ARRAY) // backward compatibility
            {
                DecodeTerm(@in, fieldInfo, termState);
                return;
            }
            termState.DocStartFP += longs[0];
            if (fieldHasPositions)
            {
                termState.PosStartFP += longs[1];
                if (fieldHasOffsets || fieldHasPayloads)
                {
                    termState.PayStartFP += longs[2];
                }
            }
            if (termState.DocFreq == 1)
            {
                termState.SingletonDocID = @in.ReadVInt();
            }
            else
            {
                termState.SingletonDocID = -1;
            }
            if (fieldHasPositions)
            {
                if (termState.TotalTermFreq > Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    termState.LastPosBlockOffset = @in.ReadVLong();
                }
                else
                {
                    termState.LastPosBlockOffset = -1;
                }
            }
            if (termState.DocFreq > Lucene41PostingsFormat.BLOCK_SIZE)
            {
                termState.SkipOffset = @in.ReadVLong();
            }
            else
            {
                termState.SkipOffset = -1;
            }
        }

        private void DecodeTerm(DataInput @in, FieldInfo fieldInfo, Lucene41PostingsWriter.IntBlockTermState termState)
        {
            bool fieldHasPositions = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            bool fieldHasOffsets = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            bool fieldHasPayloads = fieldInfo.HasPayloads();
            if (termState.DocFreq == 1)
            {
                termState.SingletonDocID = @in.ReadVInt();
            }
            else
            {
                termState.SingletonDocID = -1;
                termState.DocStartFP += @in.ReadVLong();
            }
            if (fieldHasPositions)
            {
                termState.PosStartFP += @in.ReadVLong();
                if (termState.TotalTermFreq > Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    termState.LastPosBlockOffset = @in.ReadVLong();
                }
                else
                {
                    termState.LastPosBlockOffset = -1;
                }
                if ((fieldHasPayloads || fieldHasOffsets) && termState.TotalTermFreq >= Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    termState.PayStartFP += @in.ReadVLong();
                }
            }
            if (termState.DocFreq > Lucene41PostingsFormat.BLOCK_SIZE)
            {
                termState.SkipOffset = @in.ReadVLong();
            }
            else
            {
                termState.SkipOffset = -1;
            }
        }

        public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState termState, Bits liveDocs, DocsEnum reuse, int flags)
        {
            BlockDocsEnum docsEnum;
            if (reuse is BlockDocsEnum)
            {
                docsEnum = (BlockDocsEnum)reuse;
                if (!docsEnum.CanReuse(DocIn, fieldInfo))
                {
                    docsEnum = new BlockDocsEnum(this, fieldInfo);
                }
            }
            else
            {
                docsEnum = new BlockDocsEnum(this, fieldInfo);
            }
            return docsEnum.Reset(liveDocs, (Lucene41PostingsWriter.IntBlockTermState)termState, flags);
        }

        // TODO: specialize to liveDocs vs not

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState termState, Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            bool indexHasOffsets = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            bool indexHasPayloads = fieldInfo.HasPayloads();

            if ((!indexHasOffsets || (flags & DocsAndPositionsEnum.FLAG_OFFSETS) == 0) && (!indexHasPayloads || (flags & DocsAndPositionsEnum.FLAG_PAYLOADS) == 0))
            {
                BlockDocsAndPositionsEnum docsAndPositionsEnum;
                if (reuse is BlockDocsAndPositionsEnum)
                {
                    docsAndPositionsEnum = (BlockDocsAndPositionsEnum)reuse;
                    if (!docsAndPositionsEnum.CanReuse(DocIn, fieldInfo))
                    {
                        docsAndPositionsEnum = new BlockDocsAndPositionsEnum(this, fieldInfo);
                    }
                }
                else
                {
                    docsAndPositionsEnum = new BlockDocsAndPositionsEnum(this, fieldInfo);
                }
                return docsAndPositionsEnum.Reset(liveDocs, (Lucene41PostingsWriter.IntBlockTermState)termState);
            }
            else
            {
                EverythingEnum everythingEnum;
                if (reuse is EverythingEnum)
                {
                    everythingEnum = (EverythingEnum)reuse;
                    if (!everythingEnum.CanReuse(DocIn, fieldInfo))
                    {
                        everythingEnum = new EverythingEnum(this, fieldInfo);
                    }
                }
                else
                {
                    everythingEnum = new EverythingEnum(this, fieldInfo);
                }
                return everythingEnum.Reset(liveDocs, (Lucene41PostingsWriter.IntBlockTermState)termState, flags);
            }
        }

        internal sealed class BlockDocsEnum : DocsEnum
        {
            private readonly Lucene41PostingsReader OuterInstance;

            private readonly byte[] Encoded;

            private readonly int[] DocDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] FreqBuffer = new int[ForUtil.MAX_DATA_SIZE];

            private int DocBufferUpto;

            private Lucene41SkipReader Skipper;
            private bool Skipped;

            internal readonly IndexInput StartDocIn;

            internal IndexInput DocIn;
            internal readonly bool IndexHasFreq;
            internal readonly bool IndexHasPos;
            internal readonly bool IndexHasOffsets;
            internal readonly bool IndexHasPayloads;

            private int DocFreq; // number of docs in this posting list
            private long TotalTermFreq; // sum of freqs in this posting list (or DocFreq when omitted)
            private int DocUpto; // how many docs we've read
            private int Doc; // doc we last read
            private int Accum; // accumulator for doc deltas
            private int Freq_Renamed; // freq we last read

            // Where this term's postings start in the .doc file:
            private long DocTermStartFP;

            // Where this term's skip data starts (after
            // docTermStartFP) in the .doc file (or -1 if there is
            // no skip data for this term):
            private long SkipOffset;

            // docID for next skip point, we won't use skipper if
            // target docID is not larger than this
            private int NextSkipDoc;

            private Bits LiveDocs;

            private bool NeedsFreq; // true if the caller actually needs frequencies
            private int SingletonDocID; // docid when there is a single pulsed posting, otherwise -1

            public BlockDocsEnum(Lucene41PostingsReader outerInstance, FieldInfo fieldInfo)
            {
                this.OuterInstance = outerInstance;
                this.StartDocIn = outerInstance.DocIn;
                this.DocIn = null;
                IndexHasFreq = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
                IndexHasPos = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                IndexHasOffsets = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                IndexHasPayloads = fieldInfo.HasPayloads();
                Encoded = new byte[ForUtil.MAX_ENCODED_SIZE];
            }

            public bool CanReuse(IndexInput docIn, FieldInfo fieldInfo)
            {
                return docIn == StartDocIn && IndexHasFreq == (fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS) && IndexHasPos == (fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) && IndexHasPayloads == fieldInfo.HasPayloads();
            }

            public DocsEnum Reset(Bits liveDocs, Lucene41PostingsWriter.IntBlockTermState termState, int flags)
            {
                this.LiveDocs = liveDocs;
                // if (DEBUG) {
                //   System.out.println("  FPR.reset: termState=" + termState);
                // }
                DocFreq = termState.DocFreq;
                TotalTermFreq = IndexHasFreq ? termState.TotalTermFreq : DocFreq;
                DocTermStartFP = termState.DocStartFP;
                SkipOffset = termState.SkipOffset;
                SingletonDocID = termState.SingletonDocID;
                if (DocFreq > 1)
                {
                    if (DocIn == null)
                    {
                        // lazy init
                        DocIn = (IndexInput)StartDocIn.Clone();
                    }
                    DocIn.Seek(DocTermStartFP);
                }

                Doc = -1;
                this.NeedsFreq = (flags & DocsEnum.FLAG_FREQS) != 0;
                if (!IndexHasFreq)
                {
                    CollectionsHelper.Fill(FreqBuffer, 1);
                }
                Accum = 0;
                DocUpto = 0;
                NextSkipDoc = Lucene41PostingsFormat.BLOCK_SIZE - 1; // we won't skip if target is found in first block
                DocBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                Skipped = false;
                return this;
            }

            public override int Freq
            {
                get { return Freq_Renamed; }
            }

            public override int DocID()
            {
                return Doc;
            }

            private void RefillDocs()
            {
                int left = DocFreq - DocUpto;
                Debug.Assert(left > 0);

                if (left >= Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    // if (DEBUG) {
                    //   System.out.println("    fill doc block from fp=" + docIn.getFilePointer());
                    // }
                    OuterInstance.forUtil.ReadBlock(DocIn, Encoded, DocDeltaBuffer);

                    if (IndexHasFreq)
                    {
                        // if (DEBUG) {
                        //   System.out.println("    fill freq block from fp=" + docIn.getFilePointer());
                        // }
                        if (NeedsFreq)
                        {
                            OuterInstance.forUtil.ReadBlock(DocIn, Encoded, FreqBuffer);
                        }
                        else
                        {
                            OuterInstance.forUtil.SkipBlock(DocIn); // skip over freqs
                        }
                    }
                }
                else if (DocFreq == 1)
                {
                    DocDeltaBuffer[0] = SingletonDocID;
                    FreqBuffer[0] = (int)TotalTermFreq;
                }
                else
                {
                    // Read vInts:
                    // if (DEBUG) {
                    //   System.out.println("    fill last vInt block from fp=" + docIn.getFilePointer());
                    // }
                    ReadVIntBlock(DocIn, DocDeltaBuffer, FreqBuffer, left, IndexHasFreq);
                }
                DocBufferUpto = 0;
            }

            public override int NextDoc()
            {
                // if (DEBUG) {
                //   System.out.println("\nFPR.nextDoc");
                // }
                while (true)
                {
                    // if (DEBUG) {
                    //   System.out.println("  docUpto=" + docUpto + " (of df=" + DocFreq + ") docBufferUpto=" + docBufferUpto);
                    // }

                    if (DocUpto == DocFreq)
                    {
                        // if (DEBUG) {
                        //   System.out.println("  return doc=END");
                        // }
                        return Doc = NO_MORE_DOCS;
                    }
                    if (DocBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                    {
                        RefillDocs();
                    }

                    // if (DEBUG) {
                    //   System.out.println("    accum=" + accum + " docDeltaBuffer[" + docBufferUpto + "]=" + docDeltaBuffer[docBufferUpto]);
                    // }
                    Accum += DocDeltaBuffer[DocBufferUpto];
                    DocUpto++;

                    if (LiveDocs == null || LiveDocs.Get(Accum))
                    {
                        Doc = Accum;
                        Freq_Renamed = FreqBuffer[DocBufferUpto];
                        DocBufferUpto++;
                        // if (DEBUG) {
                        //   System.out.println("  return doc=" + doc + " freq=" + freq);
                        // }
                        return Doc;
                    }
                    // if (DEBUG) {
                    //   System.out.println("  doc=" + accum + " is deleted; try next doc");
                    // }
                    DocBufferUpto++;
                }
            }

            public override int Advance(int target)
            {
                // TODO: make frq block load lazy/skippable
                // if (DEBUG) {
                //   System.out.println("  FPR.advance target=" + target);
                // }

                // current skip docID < docIDs generated from current buffer <= next skip docID
                // we don't need to skip if target is buffered already
                if (DocFreq > Lucene41PostingsFormat.BLOCK_SIZE && target > NextSkipDoc)
                {
                    // if (DEBUG) {
                    //   System.out.println("load skipper");
                    // }

                    if (Skipper == null)
                    {
                        // Lazy init: first time this enum has ever been used for skipping
                        Skipper = new Lucene41SkipReader((IndexInput)DocIn.Clone(), Lucene41PostingsWriter.MaxSkipLevels, Lucene41PostingsFormat.BLOCK_SIZE, IndexHasPos, IndexHasOffsets, IndexHasPayloads);
                    }

                    if (!Skipped)
                    {
                        Debug.Assert(SkipOffset != -1);
                        // this is the first time this enum has skipped
                        // since reset() was called; load the skip data:
                        Skipper.Init(DocTermStartFP + SkipOffset, DocTermStartFP, 0, 0, DocFreq);
                        Skipped = true;
                    }

                    // always plus one to fix the result, since skip position in Lucene41SkipReader
                    // is a little different from MultiLevelSkipListReader
                    int newDocUpto = Skipper.SkipTo(target) + 1;

                    if (newDocUpto > DocUpto)
                    {
                        // Skipper moved
                        // if (DEBUG) {
                        //   System.out.println("skipper moved to docUpto=" + newDocUpto + " vs current=" + docUpto + "; docID=" + skipper.getDoc() + " fp=" + skipper.getDocPointer());
                        // }
                        Debug.Assert(newDocUpto % Lucene41PostingsFormat.BLOCK_SIZE == 0, "got " + newDocUpto);
                        DocUpto = newDocUpto;

                        // Force to read next block
                        DocBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                        Accum = Skipper.Doc; // actually, this is just lastSkipEntry
                        DocIn.Seek(Skipper.DocPointer); // now point to the block we want to search
                    }
                    // next time we call advance, this is used to
                    // foresee whether skipper is necessary.
                    NextSkipDoc = Skipper.NextSkipDoc;
                }
                if (DocUpto == DocFreq)
                {
                    return Doc = NO_MORE_DOCS;
                }
                if (DocBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    RefillDocs();
                }

                // Now scan... this is an inlined/pared down version
                // of nextDoc():
                while (true)
                {
                    // if (DEBUG) {
                    //   System.out.println("  scan doc=" + accum + " docBufferUpto=" + docBufferUpto);
                    // }
                    Accum += DocDeltaBuffer[DocBufferUpto];
                    DocUpto++;

                    if (Accum >= target)
                    {
                        break;
                    }
                    DocBufferUpto++;
                    if (DocUpto == DocFreq)
                    {
                        return Doc = NO_MORE_DOCS;
                    }
                }

                if (LiveDocs == null || LiveDocs.Get(Accum))
                {
                    // if (DEBUG) {
                    //   System.out.println("  return doc=" + accum);
                    // }
                    Freq_Renamed = FreqBuffer[DocBufferUpto];
                    DocBufferUpto++;
                    return Doc = Accum;
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("  now do nextDoc()");
                    // }
                    DocBufferUpto++;
                    return NextDoc();
                }
            }

            public override long Cost()
            {
                return DocFreq;
            }
        }

        internal sealed class BlockDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene41PostingsReader OuterInstance;

            private readonly byte[] Encoded;

            private readonly int[] DocDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] FreqBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] PosDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];

            private int DocBufferUpto;
            private int PosBufferUpto;

            private Lucene41SkipReader Skipper;
            private bool Skipped;

            internal readonly IndexInput StartDocIn;

            internal IndexInput DocIn;
            internal readonly IndexInput PosIn;

            internal readonly bool IndexHasOffsets;
            internal readonly bool IndexHasPayloads;

            private int DocFreq; // number of docs in this posting list
            private long TotalTermFreq; // number of positions in this posting list
            private int DocUpto; // how many docs we've read
            private int Doc; // doc we last read
            private int Accum; // accumulator for doc deltas
            private int Freq_Renamed; // freq we last read
            private int Position; // current position

            // how many positions "behind" we are; nextPosition must
            // skip these to "catch up":
            private int PosPendingCount;

            // Lazy pos seek: if != -1 then we must seek to this FP
            // before reading positions:
            private long PosPendingFP;

            // Where this term's postings start in the .doc file:
            private long DocTermStartFP;

            // Where this term's postings start in the .pos file:
            private long PosTermStartFP;

            // Where this term's payloads/offsets start in the .pay
            // file:
            private long PayTermStartFP;

            // File pointer where the last (vInt encoded) pos delta
            // block is.  We need this to know whether to bulk
            // decode vs vInt decode the block:
            private long LastPosBlockFP;

            // Where this term's skip data starts (after
            // docTermStartFP) in the .doc file (or -1 if there is
            // no skip data for this term):
            private long SkipOffset;

            private int NextSkipDoc;

            private Bits LiveDocs;
            private int SingletonDocID; // docid when there is a single pulsed posting, otherwise -1

            public BlockDocsAndPositionsEnum(Lucene41PostingsReader outerInstance, FieldInfo fieldInfo)
            {
                this.OuterInstance = outerInstance;
                this.StartDocIn = outerInstance.DocIn;
                this.DocIn = null;
                this.PosIn = (IndexInput)outerInstance.PosIn.Clone();
                Encoded = new byte[ForUtil.MAX_ENCODED_SIZE];
                IndexHasOffsets = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                IndexHasPayloads = fieldInfo.HasPayloads();
            }

            public bool CanReuse(IndexInput docIn, FieldInfo fieldInfo)
            {
                return docIn == StartDocIn && IndexHasOffsets == (fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) && IndexHasPayloads == fieldInfo.HasPayloads();
            }

            public DocsAndPositionsEnum Reset(Bits liveDocs, Lucene41PostingsWriter.IntBlockTermState termState)
            {
                this.LiveDocs = liveDocs;
                // if (DEBUG) {
                //   System.out.println("  FPR.reset: termState=" + termState);
                // }
                DocFreq = termState.DocFreq;
                DocTermStartFP = termState.DocStartFP;
                PosTermStartFP = termState.PosStartFP;
                PayTermStartFP = termState.PayStartFP;
                SkipOffset = termState.SkipOffset;
                TotalTermFreq = termState.TotalTermFreq;
                SingletonDocID = termState.SingletonDocID;
                if (DocFreq > 1)
                {
                    if (DocIn == null)
                    {
                        // lazy init
                        DocIn = (IndexInput)StartDocIn.Clone();
                    }
                    DocIn.Seek(DocTermStartFP);
                }
                PosPendingFP = PosTermStartFP;
                PosPendingCount = 0;
                if (termState.TotalTermFreq < Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    LastPosBlockFP = PosTermStartFP;
                }
                else if (termState.TotalTermFreq == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    LastPosBlockFP = -1;
                }
                else
                {
                    LastPosBlockFP = PosTermStartFP + termState.LastPosBlockOffset;
                }

                Doc = -1;
                Accum = 0;
                DocUpto = 0;
                NextSkipDoc = Lucene41PostingsFormat.BLOCK_SIZE - 1;
                DocBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                Skipped = false;
                return this;
            }

            public override int Freq
            {
                get { return Freq_Renamed; }
            }

            public override int DocID()
            {
                return Doc;
            }

            private void RefillDocs()
            {
                int left = DocFreq - DocUpto;
                Debug.Assert(left > 0);

                if (left >= Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    // if (DEBUG) {
                    //   System.out.println("    fill doc block from fp=" + docIn.getFilePointer());
                    // }
                    OuterInstance.forUtil.ReadBlock(DocIn, Encoded, DocDeltaBuffer);
                    // if (DEBUG) {
                    //   System.out.println("    fill freq block from fp=" + docIn.getFilePointer());
                    // }
                    OuterInstance.forUtil.ReadBlock(DocIn, Encoded, FreqBuffer);
                }
                else if (DocFreq == 1)
                {
                    DocDeltaBuffer[0] = SingletonDocID;
                    FreqBuffer[0] = (int)TotalTermFreq;
                }
                else
                {
                    // Read vInts:
                    // if (DEBUG) {
                    //   System.out.println("    fill last vInt doc block from fp=" + docIn.getFilePointer());
                    // }
                    ReadVIntBlock(DocIn, DocDeltaBuffer, FreqBuffer, left, true);
                }
                DocBufferUpto = 0;
            }

            private void RefillPositions()
            {
                // if (DEBUG) {
                //   System.out.println("      refillPositions");
                // }
                if (PosIn.FilePointer == LastPosBlockFP)
                {
                    // if (DEBUG) {
                    //   System.out.println("        vInt pos block @ fp=" + posIn.getFilePointer() + " hasPayloads=" + indexHasPayloads + " hasOffsets=" + indexHasOffsets);
                    // }
                    int count = (int)(TotalTermFreq % Lucene41PostingsFormat.BLOCK_SIZE);
                    int payloadLength = 0;
                    for (int i = 0; i < count; i++)
                    {
                        int code = PosIn.ReadVInt();
                        if (IndexHasPayloads)
                        {
                            if ((code & 1) != 0)
                            {
                                payloadLength = PosIn.ReadVInt();
                            }
                            PosDeltaBuffer[i] = (int)((uint)code >> 1);
                            if (payloadLength != 0)
                            {
                                PosIn.Seek(PosIn.FilePointer + payloadLength);
                            }
                        }
                        else
                        {
                            PosDeltaBuffer[i] = code;
                        }
                        if (IndexHasOffsets)
                        {
                            if ((PosIn.ReadVInt() & 1) != 0)
                            {
                                // offset length changed
                                PosIn.ReadVInt();
                            }
                        }
                    }
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("        bulk pos block @ fp=" + posIn.getFilePointer());
                    // }
                    OuterInstance.forUtil.ReadBlock(PosIn, Encoded, PosDeltaBuffer);
                }
            }

            public override int NextDoc()
            {
                // if (DEBUG) {
                //   System.out.println("  FPR.nextDoc");
                // }
                while (true)
                {
                    // if (DEBUG) {
                    //   System.out.println("    docUpto=" + docUpto + " (of df=" + DocFreq + ") docBufferUpto=" + docBufferUpto);
                    // }
                    if (DocUpto == DocFreq)
                    {
                        return Doc = NO_MORE_DOCS;
                    }
                    if (DocBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                    {
                        RefillDocs();
                    }
                    // if (DEBUG) {
                    //   System.out.println("    accum=" + accum + " docDeltaBuffer[" + docBufferUpto + "]=" + docDeltaBuffer[docBufferUpto]);
                    // }
                    Accum += DocDeltaBuffer[DocBufferUpto];
                    Freq_Renamed = FreqBuffer[DocBufferUpto];
                    PosPendingCount += Freq_Renamed;
                    DocBufferUpto++;
                    DocUpto++;

                    if (LiveDocs == null || LiveDocs.Get(Accum))
                    {
                        Doc = Accum;
                        Position = 0;
                        // if (DEBUG) {
                        //   System.out.println("    return doc=" + doc + " freq=" + freq + " posPendingCount=" + posPendingCount);
                        // }
                        return Doc;
                    }
                    // if (DEBUG) {
                    //   System.out.println("    doc=" + accum + " is deleted; try next doc");
                    // }
                }
            }

            public override int Advance(int target)
            {
                // TODO: make frq block load lazy/skippable
                // if (DEBUG) {
                //   System.out.println("  FPR.advance target=" + target);
                // }

                if (DocFreq > Lucene41PostingsFormat.BLOCK_SIZE && target > NextSkipDoc)
                {
                    // if (DEBUG) {
                    //   System.out.println("    try skipper");
                    // }
                    if (Skipper == null)
                    {
                        // Lazy init: first time this enum has ever been used for skipping
                        // if (DEBUG) {
                        //   System.out.println("    create skipper");
                        // }
                        Skipper = new Lucene41SkipReader((IndexInput)DocIn.Clone(), Lucene41PostingsWriter.MaxSkipLevels, Lucene41PostingsFormat.BLOCK_SIZE, true, IndexHasOffsets, IndexHasPayloads);
                    }

                    if (!Skipped)
                    {
                        Debug.Assert(SkipOffset != -1);
                        // this is the first time this enum has skipped
                        // since reset() was called; load the skip data:
                        // if (DEBUG) {
                        //   System.out.println("    init skipper");
                        // }
                        Skipper.Init(DocTermStartFP + SkipOffset, DocTermStartFP, PosTermStartFP, PayTermStartFP, DocFreq);
                        Skipped = true;
                    }

                    int newDocUpto = Skipper.SkipTo(target) + 1;

                    if (newDocUpto > DocUpto)
                    {
                        // Skipper moved
                        // if (DEBUG) {
                        //   System.out.println("    skipper moved to docUpto=" + newDocUpto + " vs current=" + docUpto + "; docID=" + skipper.getDoc() + " fp=" + skipper.getDocPointer() + " pos.fp=" + skipper.getPosPointer() + " pos.bufferUpto=" + skipper.getPosBufferUpto());
                        // }

                        Debug.Assert(newDocUpto % Lucene41PostingsFormat.BLOCK_SIZE == 0, "got " + newDocUpto);
                        DocUpto = newDocUpto;

                        // Force to read next block
                        DocBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                        Accum = Skipper.Doc;
                        DocIn.Seek(Skipper.DocPointer);
                        PosPendingFP = Skipper.PosPointer;
                        PosPendingCount = Skipper.PosBufferUpto;
                    }
                    NextSkipDoc = Skipper.NextSkipDoc;
                }
                if (DocUpto == DocFreq)
                {
                    return Doc = NO_MORE_DOCS;
                }
                if (DocBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    RefillDocs();
                }

                // Now scan... this is an inlined/pared down version
                // of nextDoc():
                while (true)
                {
                    // if (DEBUG) {
                    //   System.out.println("  scan doc=" + accum + " docBufferUpto=" + docBufferUpto);
                    // }
                    Accum += DocDeltaBuffer[DocBufferUpto];
                    Freq_Renamed = FreqBuffer[DocBufferUpto];
                    PosPendingCount += Freq_Renamed;
                    DocBufferUpto++;
                    DocUpto++;

                    if (Accum >= target)
                    {
                        break;
                    }
                    if (DocUpto == DocFreq)
                    {
                        return Doc = NO_MORE_DOCS;
                    }
                }

                if (LiveDocs == null || LiveDocs.Get(Accum))
                {
                    // if (DEBUG) {
                    //   System.out.println("  return doc=" + accum);
                    // }
                    Position = 0;
                    return Doc = Accum;
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("  now do nextDoc()");
                    // }
                    return NextDoc();
                }
            }

            // TODO: in theory we could avoid loading frq block
            // when not needed, ie, use skip data to load how far to
            // seek the pos pointer ... instead of having to load frq
            // blocks only to sum up how many positions to skip
            private void SkipPositions()
            {
                // Skip positions now:
                int toSkip = PosPendingCount - Freq_Renamed;
                // if (DEBUG) {
                //   System.out.println("      FPR.skipPositions: toSkip=" + toSkip);
                // }

                int leftInBlock = Lucene41PostingsFormat.BLOCK_SIZE - PosBufferUpto;
                if (toSkip < leftInBlock)
                {
                    PosBufferUpto += toSkip;
                    // if (DEBUG) {
                    //   System.out.println("        skip w/in block to posBufferUpto=" + posBufferUpto);
                    // }
                }
                else
                {
                    toSkip -= leftInBlock;
                    while (toSkip >= Lucene41PostingsFormat.BLOCK_SIZE)
                    {
                        // if (DEBUG) {
                        //   System.out.println("        skip whole block @ fp=" + posIn.getFilePointer());
                        // }
                        Debug.Assert(PosIn.FilePointer != LastPosBlockFP);
                        OuterInstance.forUtil.SkipBlock(PosIn);
                        toSkip -= Lucene41PostingsFormat.BLOCK_SIZE;
                    }
                    RefillPositions();
                    PosBufferUpto = toSkip;
                    // if (DEBUG) {
                    //   System.out.println("        skip w/in block to posBufferUpto=" + posBufferUpto);
                    // }
                }

                Position = 0;
            }

            public override int NextPosition()
            {
                // if (DEBUG) {
                //   System.out.println("    FPR.nextPosition posPendingCount=" + posPendingCount + " posBufferUpto=" + posBufferUpto);
                // }
                if (PosPendingFP != -1)
                {
                    // if (DEBUG) {
                    //   System.out.println("      seek to pendingFP=" + posPendingFP);
                    // }
                    PosIn.Seek(PosPendingFP);
                    PosPendingFP = -1;

                    // Force buffer refill:
                    PosBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                }

                if (PosPendingCount > Freq_Renamed)
                {
                    SkipPositions();
                    PosPendingCount = Freq_Renamed;
                }

                if (PosBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    RefillPositions();
                    PosBufferUpto = 0;
                }
                Position += PosDeltaBuffer[PosBufferUpto++];
                PosPendingCount--;
                // if (DEBUG) {
                //   System.out.println("      return pos=" + position);
                // }
                return Position;
            }

            public override int StartOffset
            {
                get { return -1; }
            }

            public override int EndOffset
            {
                get { return -1; }
            }

            public override BytesRef Payload
            {
                get
                {
                    return null;
                }
            }

            public override long Cost()
            {
                return DocFreq;
            }
        }

        // Also handles payloads + offsets
        internal sealed class EverythingEnum : DocsAndPositionsEnum
        {
            private readonly Lucene41PostingsReader OuterInstance;

            private readonly byte[] Encoded;

            private readonly int[] DocDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] FreqBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] PosDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];

            private readonly int[] PayloadLengthBuffer;
            private readonly int[] OffsetStartDeltaBuffer;
            private readonly int[] OffsetLengthBuffer;

            private byte[] PayloadBytes;
            private int PayloadByteUpto;
            private int PayloadLength;

            private int LastStartOffset;
            private int StartOffset_Renamed;
            private int EndOffset_Renamed;

            private int DocBufferUpto;
            private int PosBufferUpto;

            private Lucene41SkipReader Skipper;
            private bool Skipped;

            internal readonly IndexInput StartDocIn;

            internal IndexInput DocIn;
            internal readonly IndexInput PosIn;
            internal readonly IndexInput PayIn;
            internal readonly BytesRef Payload_Renamed;

            internal readonly bool IndexHasOffsets;
            internal readonly bool IndexHasPayloads;

            private int DocFreq; // number of docs in this posting list
            private long TotalTermFreq; // number of positions in this posting list
            private int DocUpto; // how many docs we've read
            private int Doc; // doc we last read
            private int Accum; // accumulator for doc deltas
            private int Freq_Renamed; // freq we last read
            private int Position; // current position

            // how many positions "behind" we are; nextPosition must
            // skip these to "catch up":
            private int PosPendingCount;

            // Lazy pos seek: if != -1 then we must seek to this FP
            // before reading positions:
            private long PosPendingFP;

            // Lazy pay seek: if != -1 then we must seek to this FP
            // before reading payloads/offsets:
            private long PayPendingFP;

            // Where this term's postings start in the .doc file:
            private long DocTermStartFP;

            // Where this term's postings start in the .pos file:
            private long PosTermStartFP;

            // Where this term's payloads/offsets start in the .pay
            // file:
            private long PayTermStartFP;

            // File pointer where the last (vInt encoded) pos delta
            // block is.  We need this to know whether to bulk
            // decode vs vInt decode the block:
            private long LastPosBlockFP;

            // Where this term's skip data starts (after
            // docTermStartFP) in the .doc file (or -1 if there is
            // no skip data for this term):
            private long SkipOffset;

            private int NextSkipDoc;

            private Bits LiveDocs;

            private bool NeedsOffsets; // true if we actually need offsets
            private bool NeedsPayloads; // true if we actually need payloads
            private int SingletonDocID; // docid when there is a single pulsed posting, otherwise -1

            public EverythingEnum(Lucene41PostingsReader outerInstance, FieldInfo fieldInfo)
            {
                this.OuterInstance = outerInstance;
                this.StartDocIn = outerInstance.DocIn;
                this.DocIn = null;
                this.PosIn = (IndexInput)outerInstance.PosIn.Clone();
                this.PayIn = (IndexInput)outerInstance.PayIn.Clone();
                Encoded = new byte[ForUtil.MAX_ENCODED_SIZE];
                IndexHasOffsets = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                if (IndexHasOffsets)
                {
                    OffsetStartDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    OffsetLengthBuffer = new int[ForUtil.MAX_DATA_SIZE];
                }
                else
                {
                    OffsetStartDeltaBuffer = null;
                    OffsetLengthBuffer = null;
                    StartOffset_Renamed = -1;
                    EndOffset_Renamed = -1;
                }

                IndexHasPayloads = fieldInfo.HasPayloads();
                if (IndexHasPayloads)
                {
                    PayloadLengthBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    PayloadBytes = new byte[128];
                    Payload_Renamed = new BytesRef();
                }
                else
                {
                    PayloadLengthBuffer = null;
                    PayloadBytes = null;
                    Payload_Renamed = null;
                }
            }

            public bool CanReuse(IndexInput docIn, FieldInfo fieldInfo)
            {
                return docIn == StartDocIn && IndexHasOffsets == (fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) && IndexHasPayloads == fieldInfo.HasPayloads();
            }

            public EverythingEnum Reset(Bits liveDocs, Lucene41PostingsWriter.IntBlockTermState termState, int flags)
            {
                this.LiveDocs = liveDocs;
                // if (DEBUG) {
                //   System.out.println("  FPR.reset: termState=" + termState);
                // }
                DocFreq = termState.DocFreq;
                DocTermStartFP = termState.DocStartFP;
                PosTermStartFP = termState.PosStartFP;
                PayTermStartFP = termState.PayStartFP;
                SkipOffset = termState.SkipOffset;
                TotalTermFreq = termState.TotalTermFreq;
                SingletonDocID = termState.SingletonDocID;
                if (DocFreq > 1)
                {
                    if (DocIn == null)
                    {
                        // lazy init
                        DocIn = (IndexInput)StartDocIn.Clone();
                    }
                    DocIn.Seek(DocTermStartFP);
                }
                PosPendingFP = PosTermStartFP;
                PayPendingFP = PayTermStartFP;
                PosPendingCount = 0;
                if (termState.TotalTermFreq < Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    LastPosBlockFP = PosTermStartFP;
                }
                else if (termState.TotalTermFreq == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    LastPosBlockFP = -1;
                }
                else
                {
                    LastPosBlockFP = PosTermStartFP + termState.LastPosBlockOffset;
                }

                this.NeedsOffsets = (flags & DocsAndPositionsEnum.FLAG_OFFSETS) != 0;
                this.NeedsPayloads = (flags & DocsAndPositionsEnum.FLAG_PAYLOADS) != 0;

                Doc = -1;
                Accum = 0;
                DocUpto = 0;
                NextSkipDoc = Lucene41PostingsFormat.BLOCK_SIZE - 1;
                DocBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                Skipped = false;
                return this;
            }

            public override int Freq
            {
                get { return Freq_Renamed; }
            }

            public override int DocID()
            {
                return Doc;
            }

            private void RefillDocs()
            {
                int left = DocFreq - DocUpto;
                Debug.Assert(left > 0);

                if (left >= Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    // if (DEBUG) {
                    //   System.out.println("    fill doc block from fp=" + docIn.getFilePointer());
                    // }
                    OuterInstance.forUtil.ReadBlock(DocIn, Encoded, DocDeltaBuffer);
                    // if (DEBUG) {
                    //   System.out.println("    fill freq block from fp=" + docIn.getFilePointer());
                    // }
                    OuterInstance.forUtil.ReadBlock(DocIn, Encoded, FreqBuffer);
                }
                else if (DocFreq == 1)
                {
                    DocDeltaBuffer[0] = SingletonDocID;
                    FreqBuffer[0] = (int)TotalTermFreq;
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("    fill last vInt doc block from fp=" + docIn.getFilePointer());
                    // }
                    ReadVIntBlock(DocIn, DocDeltaBuffer, FreqBuffer, left, true);
                }
                DocBufferUpto = 0;
            }

            private void RefillPositions()
            {
                // if (DEBUG) {
                //   System.out.println("      refillPositions");
                // }
                if (PosIn.FilePointer == LastPosBlockFP)
                {
                    // if (DEBUG) {
                    //   System.out.println("        vInt pos block @ fp=" + posIn.getFilePointer() + " hasPayloads=" + indexHasPayloads + " hasOffsets=" + indexHasOffsets);
                    // }
                    int count = (int)(TotalTermFreq % Lucene41PostingsFormat.BLOCK_SIZE);
                    int payloadLength = 0;
                    int offsetLength = 0;
                    PayloadByteUpto = 0;
                    for (int i = 0; i < count; i++)
                    {
                        int code = PosIn.ReadVInt();
                        if (IndexHasPayloads)
                        {
                            if ((code & 1) != 0)
                            {
                                payloadLength = PosIn.ReadVInt();
                            }
                            // if (DEBUG) {
                            //   System.out.println("        i=" + i + " payloadLen=" + payloadLength);
                            // }
                            PayloadLengthBuffer[i] = payloadLength;
                            PosDeltaBuffer[i] = (int)((uint)code >> 1);
                            if (payloadLength != 0)
                            {
                                if (PayloadByteUpto + payloadLength > PayloadBytes.Length)
                                {
                                    PayloadBytes = ArrayUtil.Grow(PayloadBytes, PayloadByteUpto + payloadLength);
                                }
                                //System.out.println("          read payload @ pos.fp=" + posIn.getFilePointer());
                                PosIn.ReadBytes(PayloadBytes, PayloadByteUpto, payloadLength);
                                PayloadByteUpto += payloadLength;
                            }
                        }
                        else
                        {
                            PosDeltaBuffer[i] = code;
                        }

                        if (IndexHasOffsets)
                        {
                            // if (DEBUG) {
                            //   System.out.println("        i=" + i + " read offsets from posIn.fp=" + posIn.getFilePointer());
                            // }
                            int deltaCode = PosIn.ReadVInt();
                            if ((deltaCode & 1) != 0)
                            {
                                offsetLength = PosIn.ReadVInt();
                            }
                            OffsetStartDeltaBuffer[i] = (int)((uint)deltaCode >> 1);
                            OffsetLengthBuffer[i] = offsetLength;
                            // if (DEBUG) {
                            //   System.out.println("          startOffDelta=" + offsetStartDeltaBuffer[i] + " offsetLen=" + offsetLengthBuffer[i]);
                            // }
                        }
                    }
                    PayloadByteUpto = 0;
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("        bulk pos block @ fp=" + posIn.getFilePointer());
                    // }
                    OuterInstance.forUtil.ReadBlock(PosIn, Encoded, PosDeltaBuffer);

                    if (IndexHasPayloads)
                    {
                        // if (DEBUG) {
                        //   System.out.println("        bulk payload block @ pay.fp=" + payIn.getFilePointer());
                        // }
                        if (NeedsPayloads)
                        {
                            OuterInstance.forUtil.ReadBlock(PayIn, Encoded, PayloadLengthBuffer);
                            int numBytes = PayIn.ReadVInt();
                            // if (DEBUG) {
                            //   System.out.println("        " + numBytes + " payload bytes @ pay.fp=" + payIn.getFilePointer());
                            // }
                            if (numBytes > PayloadBytes.Length)
                            {
                                PayloadBytes = ArrayUtil.Grow(PayloadBytes, numBytes);
                            }
                            PayIn.ReadBytes(PayloadBytes, 0, numBytes);
                        }
                        else
                        {
                            // this works, because when writing a vint block we always force the first length to be written
                            OuterInstance.forUtil.SkipBlock(PayIn); // skip over lengths
                            int numBytes = PayIn.ReadVInt(); // read length of payloadBytes
                            PayIn.Seek(PayIn.FilePointer + numBytes); // skip over payloadBytes
                        }
                        PayloadByteUpto = 0;
                    }

                    if (IndexHasOffsets)
                    {
                        // if (DEBUG) {
                        //   System.out.println("        bulk offset block @ pay.fp=" + payIn.getFilePointer());
                        // }
                        if (NeedsOffsets)
                        {
                            OuterInstance.forUtil.ReadBlock(PayIn, Encoded, OffsetStartDeltaBuffer);
                            OuterInstance.forUtil.ReadBlock(PayIn, Encoded, OffsetLengthBuffer);
                        }
                        else
                        {
                            // this works, because when writing a vint block we always force the first length to be written
                            OuterInstance.forUtil.SkipBlock(PayIn); // skip over starts
                            OuterInstance.forUtil.SkipBlock(PayIn); // skip over lengths
                        }
                    }
                }
            }

            public override int NextDoc()
            {
                // if (DEBUG) {
                //   System.out.println("  FPR.nextDoc");
                // }
                while (true)
                {
                    // if (DEBUG) {
                    //   System.out.println("    docUpto=" + docUpto + " (of df=" + DocFreq + ") docBufferUpto=" + docBufferUpto);
                    // }
                    if (DocUpto == DocFreq)
                    {
                        return Doc = NO_MORE_DOCS;
                    }
                    if (DocBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                    {
                        RefillDocs();
                    }
                    // if (DEBUG) {
                    //   System.out.println("    accum=" + accum + " docDeltaBuffer[" + docBufferUpto + "]=" + docDeltaBuffer[docBufferUpto]);
                    // }
                    Accum += DocDeltaBuffer[DocBufferUpto];
                    Freq_Renamed = FreqBuffer[DocBufferUpto];
                    PosPendingCount += Freq_Renamed;
                    DocBufferUpto++;
                    DocUpto++;

                    if (LiveDocs == null || LiveDocs.Get(Accum))
                    {
                        Doc = Accum;
                        // if (DEBUG) {
                        //   System.out.println("    return doc=" + doc + " freq=" + freq + " posPendingCount=" + posPendingCount);
                        // }
                        Position = 0;
                        LastStartOffset = 0;
                        return Doc;
                    }

                    // if (DEBUG) {
                    //   System.out.println("    doc=" + accum + " is deleted; try next doc");
                    // }
                }
            }

            public override int Advance(int target)
            {
                // TODO: make frq block load lazy/skippable
                // if (DEBUG) {
                //   System.out.println("  FPR.advance target=" + target);
                // }

                if (DocFreq > Lucene41PostingsFormat.BLOCK_SIZE && target > NextSkipDoc)
                {
                    // if (DEBUG) {
                    //   System.out.println("    try skipper");
                    // }

                    if (Skipper == null)
                    {
                        // Lazy init: first time this enum has ever been used for skipping
                        // if (DEBUG) {
                        //   System.out.println("    create skipper");
                        // }
                        Skipper = new Lucene41SkipReader((IndexInput)DocIn.Clone(), Lucene41PostingsWriter.MaxSkipLevels, Lucene41PostingsFormat.BLOCK_SIZE, true, IndexHasOffsets, IndexHasPayloads);
                    }

                    if (!Skipped)
                    {
                        Debug.Assert(SkipOffset != -1);
                        // this is the first time this enum has skipped
                        // since reset() was called; load the skip data:
                        // if (DEBUG) {
                        //   System.out.println("    init skipper");
                        // }
                        Skipper.Init(DocTermStartFP + SkipOffset, DocTermStartFP, PosTermStartFP, PayTermStartFP, DocFreq);
                        Skipped = true;
                    }

                    int newDocUpto = Skipper.SkipTo(target) + 1;

                    if (newDocUpto > DocUpto)
                    {
                        // Skipper moved
                        // if (DEBUG) {
                        //   System.out.println("    skipper moved to docUpto=" + newDocUpto + " vs current=" + docUpto + "; docID=" + skipper.getDoc() + " fp=" + skipper.getDocPointer() + " pos.fp=" + skipper.getPosPointer() + " pos.bufferUpto=" + skipper.getPosBufferUpto() + " pay.fp=" + skipper.getPayPointer() + " lastStartOffset=" + lastStartOffset);
                        // }
                        Debug.Assert(newDocUpto % Lucene41PostingsFormat.BLOCK_SIZE == 0, "got " + newDocUpto);
                        DocUpto = newDocUpto;

                        // Force to read next block
                        DocBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                        Accum = Skipper.Doc;
                        DocIn.Seek(Skipper.DocPointer);
                        PosPendingFP = Skipper.PosPointer;
                        PayPendingFP = Skipper.PayPointer;
                        PosPendingCount = Skipper.PosBufferUpto;
                        LastStartOffset = 0; // new document
                        PayloadByteUpto = Skipper.PayloadByteUpto;
                    }
                    NextSkipDoc = Skipper.NextSkipDoc;
                }
                if (DocUpto == DocFreq)
                {
                    return Doc = NO_MORE_DOCS;
                }
                if (DocBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    RefillDocs();
                }

                // Now scan:
                while (true)
                {
                    // if (DEBUG) {
                    //   System.out.println("  scan doc=" + accum + " docBufferUpto=" + docBufferUpto);
                    // }
                    Accum += DocDeltaBuffer[DocBufferUpto];
                    Freq_Renamed = FreqBuffer[DocBufferUpto];
                    PosPendingCount += Freq_Renamed;
                    DocBufferUpto++;
                    DocUpto++;

                    if (Accum >= target)
                    {
                        break;
                    }
                    if (DocUpto == DocFreq)
                    {
                        return Doc = NO_MORE_DOCS;
                    }
                }

                if (LiveDocs == null || LiveDocs.Get(Accum))
                {
                    // if (DEBUG) {
                    //   System.out.println("  return doc=" + accum);
                    // }
                    Position = 0;
                    LastStartOffset = 0;
                    return Doc = Accum;
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("  now do nextDoc()");
                    // }
                    return NextDoc();
                }
            }

            // TODO: in theory we could avoid loading frq block
            // when not needed, ie, use skip data to load how far to
            // seek the pos pointer ... instead of having to load frq
            // blocks only to sum up how many positions to skip
            private void SkipPositions()
            {
                // Skip positions now:
                int toSkip = PosPendingCount - Freq_Renamed;
                // if (DEBUG) {
                //   System.out.println("      FPR.skipPositions: toSkip=" + toSkip);
                // }

                int leftInBlock = Lucene41PostingsFormat.BLOCK_SIZE - PosBufferUpto;
                if (toSkip < leftInBlock)
                {
                    int end = PosBufferUpto + toSkip;
                    while (PosBufferUpto < end)
                    {
                        if (IndexHasPayloads)
                        {
                            PayloadByteUpto += PayloadLengthBuffer[PosBufferUpto];
                        }
                        PosBufferUpto++;
                    }
                    // if (DEBUG) {
                    //   System.out.println("        skip w/in block to posBufferUpto=" + posBufferUpto);
                    // }
                }
                else
                {
                    toSkip -= leftInBlock;
                    while (toSkip >= Lucene41PostingsFormat.BLOCK_SIZE)
                    {
                        // if (DEBUG) {
                        //   System.out.println("        skip whole block @ fp=" + posIn.getFilePointer());
                        // }
                        Debug.Assert(PosIn.FilePointer != LastPosBlockFP);
                        OuterInstance.forUtil.SkipBlock(PosIn);

                        if (IndexHasPayloads)
                        {
                            // Skip payloadLength block:
                            OuterInstance.forUtil.SkipBlock(PayIn);

                            // Skip payloadBytes block:
                            int numBytes = PayIn.ReadVInt();
                            PayIn.Seek(PayIn.FilePointer + numBytes);
                        }

                        if (IndexHasOffsets)
                        {
                            OuterInstance.forUtil.SkipBlock(PayIn);
                            OuterInstance.forUtil.SkipBlock(PayIn);
                        }
                        toSkip -= Lucene41PostingsFormat.BLOCK_SIZE;
                    }
                    RefillPositions();
                    PayloadByteUpto = 0;
                    PosBufferUpto = 0;
                    while (PosBufferUpto < toSkip)
                    {
                        if (IndexHasPayloads)
                        {
                            PayloadByteUpto += PayloadLengthBuffer[PosBufferUpto];
                        }
                        PosBufferUpto++;
                    }
                    // if (DEBUG) {
                    //   System.out.println("        skip w/in block to posBufferUpto=" + posBufferUpto);
                    // }
                }

                Position = 0;
                LastStartOffset = 0;
            }

            public override int NextPosition()
            {
                // if (DEBUG) {
                //   System.out.println("    FPR.nextPosition posPendingCount=" + posPendingCount + " posBufferUpto=" + posBufferUpto + " payloadByteUpto=" + payloadByteUpto)// ;
                // }
                if (PosPendingFP != -1)
                {
                    // if (DEBUG) {
                    //   System.out.println("      seek pos to pendingFP=" + posPendingFP);
                    // }
                    PosIn.Seek(PosPendingFP);
                    PosPendingFP = -1;

                    if (PayPendingFP != -1)
                    {
                        // if (DEBUG) {
                        //   System.out.println("      seek pay to pendingFP=" + payPendingFP);
                        // }
                        PayIn.Seek(PayPendingFP);
                        PayPendingFP = -1;
                    }

                    // Force buffer refill:
                    PosBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                }

                if (PosPendingCount > Freq_Renamed)
                {
                    SkipPositions();
                    PosPendingCount = Freq_Renamed;
                }

                if (PosBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    RefillPositions();
                    PosBufferUpto = 0;
                }
                Position += PosDeltaBuffer[PosBufferUpto];

                if (IndexHasPayloads)
                {
                    PayloadLength = PayloadLengthBuffer[PosBufferUpto];
                    Payload_Renamed.Bytes = PayloadBytes;
                    Payload_Renamed.Offset = PayloadByteUpto;
                    Payload_Renamed.Length = PayloadLength;
                    PayloadByteUpto += PayloadLength;
                }

                if (IndexHasOffsets)
                {
                    StartOffset_Renamed = LastStartOffset + OffsetStartDeltaBuffer[PosBufferUpto];
                    EndOffset_Renamed = StartOffset_Renamed + OffsetLengthBuffer[PosBufferUpto];
                    LastStartOffset = StartOffset_Renamed;
                }

                PosBufferUpto++;
                PosPendingCount--;
                // if (DEBUG) {
                //   System.out.println("      return pos=" + position);
                // }
                return Position;
            }

            public override int StartOffset
            {
                get { return StartOffset_Renamed; }
            }

            public override int EndOffset
            {
                get { return EndOffset_Renamed; }
            }

            public override BytesRef Payload
            {
                get
                {
                    // if (DEBUG) {
                    //   System.out.println("    FPR.getPayload payloadLength=" + payloadLength + " payloadByteUpto=" + payloadByteUpto);
                    // }
                    if (PayloadLength == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return Payload_Renamed;
                    }
                }
            }

            public override long Cost()
            {
                return DocFreq;
            }
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
            if (Version >= Lucene41PostingsWriter.VERSION_CHECKSUM)
            {
                if (DocIn != null)
                {
                    CodecUtil.ChecksumEntireFile(DocIn);
                }
                if (PosIn != null)
                {
                    CodecUtil.ChecksumEntireFile(PosIn);
                }
                if (PayIn != null)
                {
                    CodecUtil.ChecksumEntireFile(PayIn);
                }
            }
        }
    }
}