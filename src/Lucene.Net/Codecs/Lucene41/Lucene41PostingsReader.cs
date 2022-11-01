using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
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

    /// <summary>
    /// Concrete class that reads docId(maybe frq,pos,offset,payloads) list
    /// with postings format.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Lucene41SkipReader"/>
    public sealed class Lucene41PostingsReader : PostingsReaderBase
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IndexInput docIn;
        private readonly IndexInput posIn;
        private readonly IndexInput payIn;
#pragma warning restore CA2213 // Disposable fields should be disposed

        private readonly ForUtil forUtil;
        private readonly int version; // LUCENENET: marked readonly

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
                version = CodecUtil.CheckHeader(docIn, Lucene41PostingsWriter.DOC_CODEC, Lucene41PostingsWriter.VERSION_START, Lucene41PostingsWriter.VERSION_CURRENT);
                forUtil = new ForUtil(docIn);

                if (fieldInfos.HasProx)
                {
                    posIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, Lucene41PostingsFormat.POS_EXTENSION), ioContext);
                    CodecUtil.CheckHeader(posIn, Lucene41PostingsWriter.POS_CODEC, version, version);

                    if (fieldInfos.HasPayloads || fieldInfos.HasOffsets)
                    {
                        payIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, Lucene41PostingsFormat.PAY_EXTENSION), ioContext);
                        CodecUtil.CheckHeader(payIn, Lucene41PostingsWriter.PAY_CODEC, version, version);
                    }
                }

                this.docIn = docIn;
                this.posIn = posIn;
                this.payIn = payIn;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(docIn, posIn, payIn);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Init(IndexInput termsIn)
        {
            // Make sure we are talking to the matching postings writer
            CodecUtil.CheckHeader(termsIn, Lucene41PostingsWriter.TERMS_CODEC, Lucene41PostingsWriter.VERSION_START, Lucene41PostingsWriter.VERSION_CURRENT);
            int indexBlockSize = termsIn.ReadVInt32();
            if (indexBlockSize != Lucene41PostingsFormat.BLOCK_SIZE)
            {
                throw IllegalStateException.Create("index-time BLOCK_SIZE (" + indexBlockSize + ") != read-time BLOCK_SIZE (" + Lucene41PostingsFormat.BLOCK_SIZE + ")");
            }
        }

        /// <summary>
        /// Read values that have been written using variable-length encoding instead of bit-packing.
        /// <para/>
        /// NOTE: This was readVIntBlock() in Lucene.
        /// </summary>
        internal static void ReadVInt32Block(IndexInput docIn, int[] docBuffer, int[] freqBuffer, int num, bool indexHasFreq)
        {
            if (indexHasFreq)
            {
                for (int i = 0; i < num; i++)
                {
                    int code = docIn.ReadVInt32();
                    docBuffer[i] = code.TripleShift(1);
                    if ((code & 1) != 0)
                    {
                        freqBuffer[i] = 1;
                    }
                    else
                    {
                        freqBuffer[i] = docIn.ReadVInt32();
                    }
                }
            }
            else
            {
                for (int i = 0; i < num; i++)
                {
                    docBuffer[i] = docIn.ReadVInt32();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override BlockTermState NewTermState()
        {
            return new Lucene41PostingsWriter.Int32BlockTermState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IOUtils.Dispose(docIn, posIn, payIn);
        }

        public override void DecodeTerm(long[] longs, DataInput @in, FieldInfo fieldInfo, BlockTermState termState, bool absolute)
        {
            Lucene41PostingsWriter.Int32BlockTermState termState2 = (Lucene41PostingsWriter.Int32BlockTermState)termState;
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            bool fieldHasPositions = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
            bool fieldHasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            bool fieldHasPayloads = fieldInfo.HasPayloads;

            if (absolute)
            {
                termState2.docStartFP = 0;
                termState2.posStartFP = 0;
                termState2.payStartFP = 0;
            }
            if (version < Lucene41PostingsWriter.VERSION_META_ARRAY) // backward compatibility
            {
                DecodeTerm(@in, fieldInfo, termState2);
                return;
            }
            termState2.docStartFP += longs[0];
            if (fieldHasPositions)
            {
                termState2.posStartFP += longs[1];
                if (fieldHasOffsets || fieldHasPayloads)
                {
                    termState2.payStartFP += longs[2];
                }
            }
            if (termState2.DocFreq == 1)
            {
                termState2.singletonDocID = @in.ReadVInt32();
            }
            else
            {
                termState2.singletonDocID = -1;
            }
            if (fieldHasPositions)
            {
                if (termState2.TotalTermFreq > Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    termState2.lastPosBlockOffset = @in.ReadVInt64();
                }
                else
                {
                    termState2.lastPosBlockOffset = -1;
                }
            }
            if (termState2.DocFreq > Lucene41PostingsFormat.BLOCK_SIZE)
            {
                termState2.skipOffset = @in.ReadVInt64();
            }
            else
            {
                termState2.skipOffset = -1;
            }
        }

        private static void DecodeTerm(DataInput @in, FieldInfo fieldInfo, Lucene41PostingsWriter.Int32BlockTermState termState) // LUCENENET: CA1822: Mark members as static
        {
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            bool fieldHasPositions = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
            bool fieldHasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            bool fieldHasPayloads = fieldInfo.HasPayloads;
            if (termState.DocFreq == 1)
            {
                termState.singletonDocID = @in.ReadVInt32();
            }
            else
            {
                termState.singletonDocID = -1;
                termState.docStartFP += @in.ReadVInt64();
            }
            if (fieldHasPositions)
            {
                termState.posStartFP += @in.ReadVInt64();
                if (termState.TotalTermFreq > Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    termState.lastPosBlockOffset = @in.ReadVInt64();
                }
                else
                {
                    termState.lastPosBlockOffset = -1;
                }
                if ((fieldHasPayloads || fieldHasOffsets) && termState.TotalTermFreq >= Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    termState.payStartFP += @in.ReadVInt64();
                }
            }
            if (termState.DocFreq > Lucene41PostingsFormat.BLOCK_SIZE)
            {
                termState.skipOffset = @in.ReadVInt64();
            }
            else
            {
                termState.skipOffset = -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState termState, IBits liveDocs, DocsEnum reuse, DocsFlags flags)
        {
            if (reuse is null || !(reuse is BlockDocsEnum docsEnum) || !docsEnum.CanReuse(docIn, fieldInfo))
                docsEnum = new BlockDocsEnum(this, fieldInfo);

            return docsEnum.Reset(liveDocs, (Lucene41PostingsWriter.Int32BlockTermState)termState, flags);
        }

        // TODO: specialize to liveDocs vs not

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState termState, IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
        {
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            bool indexHasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            bool indexHasPayloads = fieldInfo.HasPayloads;

            if ((!indexHasOffsets || (flags & DocsAndPositionsFlags.OFFSETS) == 0) && (!indexHasPayloads || (flags & DocsAndPositionsFlags.PAYLOADS) == 0))
            {
                if (reuse is null || !(reuse is BlockDocsAndPositionsEnum docsAndPositionsEnum) || !docsAndPositionsEnum.CanReuse(docIn, fieldInfo))
                    docsAndPositionsEnum = new BlockDocsAndPositionsEnum(this, fieldInfo);

                return docsAndPositionsEnum.Reset(liveDocs, (Lucene41PostingsWriter.Int32BlockTermState)termState);
            }
            else
            {
                if (reuse is null || !(reuse is EverythingEnum everythingEnum) || !everythingEnum.CanReuse(docIn, fieldInfo))
                    everythingEnum = new EverythingEnum(this, fieldInfo);

                return everythingEnum.Reset(liveDocs, (Lucene41PostingsWriter.Int32BlockTermState)termState, flags);
            }
        }

        internal sealed class BlockDocsEnum : DocsEnum
        {
            private readonly Lucene41PostingsReader outerInstance;

            private readonly byte[] encoded;

            private readonly int[] docDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] freqBuffer = new int[ForUtil.MAX_DATA_SIZE];

            private int docBufferUpto;

            private Lucene41SkipReader skipper;
            private bool skipped;

            internal readonly IndexInput startDocIn;

            internal IndexInput docIn;
            internal readonly bool indexHasFreq;
            internal readonly bool indexHasPos;
            internal readonly bool indexHasOffsets;
            internal readonly bool indexHasPayloads;

            private int docFreq; // number of docs in this posting list
            private long totalTermFreq; // sum of freqs in this posting list (or DocFreq when omitted)
            private int docUpto; // how many docs we've read
            private int doc; // doc we last read
            private int accum; // accumulator for doc deltas
            private int freq; // freq we last read

            // Where this term's postings start in the .doc file:
            private long docTermStartFP;

            // Where this term's skip data starts (after
            // docTermStartFP) in the .doc file (or -1 if there is
            // no skip data for this term):
            private long skipOffset;

            // docID for next skip point, we won't use skipper if
            // target docID is not larger than this
            private int nextSkipDoc;

            private IBits liveDocs;

            private bool needsFreq; // true if the caller actually needs frequencies
            private int singletonDocID; // docid when there is a single pulsed posting, otherwise -1

            public BlockDocsEnum(Lucene41PostingsReader outerInstance, FieldInfo fieldInfo)
            {
                this.outerInstance = outerInstance;
                this.startDocIn = outerInstance.docIn;
                this.docIn = null;
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                indexHasFreq = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;
                indexHasPos = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                indexHasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                indexHasPayloads = fieldInfo.HasPayloads;
                encoded = new byte[ForUtil.MAX_ENCODED_SIZE];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CanReuse(IndexInput docIn, FieldInfo fieldInfo)
            {
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                return docIn == startDocIn && 
                    indexHasFreq == (IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0) && 
                    indexHasPos == (IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0) && 
                    indexHasPayloads == fieldInfo.HasPayloads;
            }

            public DocsEnum Reset(IBits liveDocs, Lucene41PostingsWriter.Int32BlockTermState termState, DocsFlags flags)
            {
                this.liveDocs = liveDocs;
                // if (DEBUG) {
                //   System.out.println("  FPR.reset: termState=" + termState);
                // }
                docFreq = termState.DocFreq;
                totalTermFreq = indexHasFreq ? termState.TotalTermFreq : docFreq;
                docTermStartFP = termState.docStartFP;
                skipOffset = termState.skipOffset;
                singletonDocID = termState.singletonDocID;
                if (docFreq > 1)
                {
                    if (docIn is null)
                    {
                        // lazy init
                        docIn = (IndexInput)startDocIn.Clone();
                    }
                    docIn.Seek(docTermStartFP);
                }

                doc = -1;
                this.needsFreq = (flags & DocsFlags.FREQS) != 0;
                if (!indexHasFreq)
                {
                    Arrays.Fill(freqBuffer, 1);
                }
                accum = 0;
                docUpto = 0;
                nextSkipDoc = Lucene41PostingsFormat.BLOCK_SIZE - 1; // we won't skip if target is found in first block
                docBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                skipped = false;
                return this;
            }

            public override int Freq => freq;

            public override int DocID => doc;

            private void RefillDocs()
            {
                int left = docFreq - docUpto;
                if (Debugging.AssertsEnabled) Debugging.Assert(left > 0);

                if (left >= Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    // if (DEBUG) {
                    //   System.out.println("    fill doc block from fp=" + docIn.getFilePointer());
                    // }
                    outerInstance.forUtil.ReadBlock(docIn, encoded, docDeltaBuffer);

                    if (indexHasFreq)
                    {
                        // if (DEBUG) {
                        //   System.out.println("    fill freq block from fp=" + docIn.getFilePointer());
                        // }
                        if (needsFreq)
                        {
                            outerInstance.forUtil.ReadBlock(docIn, encoded, freqBuffer);
                        }
                        else
                        {
                            outerInstance.forUtil.SkipBlock(docIn); // skip over freqs
                        }
                    }
                }
                else if (docFreq == 1)
                {
                    docDeltaBuffer[0] = singletonDocID;
                    freqBuffer[0] = (int)totalTermFreq;
                }
                else
                {
                    // Read vInts:
                    // if (DEBUG) {
                    //   System.out.println("    fill last vInt block from fp=" + docIn.getFilePointer());
                    // }
                    ReadVInt32Block(docIn, docDeltaBuffer, freqBuffer, left, indexHasFreq);
                }
                docBufferUpto = 0;
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

                    if (docUpto == docFreq)
                    {
                        // if (DEBUG) {
                        //   System.out.println("  return doc=END");
                        // }
                        return doc = NO_MORE_DOCS;
                    }
                    if (docBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                    {
                        RefillDocs();
                    }

                    // if (DEBUG) {
                    //   System.out.println("    accum=" + accum + " docDeltaBuffer[" + docBufferUpto + "]=" + docDeltaBuffer[docBufferUpto]);
                    // }
                    accum += docDeltaBuffer[docBufferUpto];
                    docUpto++;

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        doc = accum;
                        freq = freqBuffer[docBufferUpto];
                        docBufferUpto++;
                        // if (DEBUG) {
                        //   System.out.println("  return doc=" + doc + " freq=" + freq);
                        // }
                        return doc;
                    }
                    // if (DEBUG) {
                    //   System.out.println("  doc=" + accum + " is deleted; try next doc");
                    // }
                    docBufferUpto++;
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
                if (docFreq > Lucene41PostingsFormat.BLOCK_SIZE && target > nextSkipDoc)
                {
                    // if (DEBUG) {
                    //   System.out.println("load skipper");
                    // }

                    if (skipper is null)
                    {
                        // Lazy init: first time this enum has ever been used for skipping
                        skipper = new Lucene41SkipReader((IndexInput)docIn.Clone(), Lucene41PostingsWriter.maxSkipLevels, Lucene41PostingsFormat.BLOCK_SIZE, indexHasPos, indexHasOffsets, indexHasPayloads);
                    }

                    if (!skipped)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(skipOffset != -1);
                        // this is the first time this enum has skipped
                        // since reset() was called; load the skip data:
                        skipper.Init(docTermStartFP + skipOffset, docTermStartFP, 0, 0, docFreq);
                        skipped = true;
                    }

                    // always plus one to fix the result, since skip position in Lucene41SkipReader
                    // is a little different from MultiLevelSkipListReader
                    int newDocUpto = skipper.SkipTo(target) + 1;

                    if (newDocUpto > docUpto)
                    {
                        // Skipper moved
                        // if (DEBUG) {
                        //   System.out.println("skipper moved to docUpto=" + newDocUpto + " vs current=" + docUpto + "; docID=" + skipper.getDoc() + " fp=" + skipper.getDocPointer());
                        // }
                        if (Debugging.AssertsEnabled) Debugging.Assert(newDocUpto % Lucene41PostingsFormat.BLOCK_SIZE == 0,"got {0}", newDocUpto);
                        docUpto = newDocUpto;

                        // Force to read next block
                        docBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                        accum = skipper.Doc; // actually, this is just lastSkipEntry
                        docIn.Seek(skipper.DocPointer); // now point to the block we want to search
                    }
                    // next time we call advance, this is used to
                    // foresee whether skipper is necessary.
                    nextSkipDoc = skipper.NextSkipDoc;
                }
                if (docUpto == docFreq)
                {
                    return doc = NO_MORE_DOCS;
                }
                if (docBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
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
                    accum += docDeltaBuffer[docBufferUpto];
                    docUpto++;

                    if (accum >= target)
                    {
                        break;
                    }
                    docBufferUpto++;
                    if (docUpto == docFreq)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                }

                if (liveDocs is null || liveDocs.Get(accum))
                {
                    // if (DEBUG) {
                    //   System.out.println("  return doc=" + accum);
                    // }
                    freq = freqBuffer[docBufferUpto];
                    docBufferUpto++;
                    return doc = accum;
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("  now do nextDoc()");
                    // }
                    docBufferUpto++;
                    return NextDoc();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return docFreq;
            }
        }

        internal sealed class BlockDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene41PostingsReader outerInstance;

            private readonly byte[] encoded;

            private readonly int[] docDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] freqBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] posDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];

            private int docBufferUpto;
            private int posBufferUpto;

            private Lucene41SkipReader skipper;
            private bool skipped;

            internal readonly IndexInput startDocIn;

            internal IndexInput docIn;
            internal readonly IndexInput posIn;

            internal readonly bool indexHasOffsets;
            internal readonly bool indexHasPayloads;

            private int docFreq; // number of docs in this posting list
            private long totalTermFreq; // number of positions in this posting list
            private int docUpto; // how many docs we've read
            private int doc; // doc we last read
            private int accum; // accumulator for doc deltas
            private int freq; // freq we last read
            private int position; // current position

            // how many positions "behind" we are; nextPosition must
            // skip these to "catch up":
            private int posPendingCount;

            // Lazy pos seek: if != -1 then we must seek to this FP
            // before reading positions:
            private long posPendingFP;

            // Where this term's postings start in the .doc file:
            private long docTermStartFP;

            // Where this term's postings start in the .pos file:
            private long posTermStartFP;

            // Where this term's payloads/offsets start in the .pay
            // file:
            private long payTermStartFP;

            // File pointer where the last (vInt encoded) pos delta
            // block is.  We need this to know whether to bulk
            // decode vs vInt decode the block:
            private long lastPosBlockFP;

            // Where this term's skip data starts (after
            // docTermStartFP) in the .doc file (or -1 if there is
            // no skip data for this term):
            private long skipOffset;

            private int nextSkipDoc;

            private IBits liveDocs;
            private int singletonDocID; // docid when there is a single pulsed posting, otherwise -1

            public BlockDocsAndPositionsEnum(Lucene41PostingsReader outerInstance, FieldInfo fieldInfo)
            {
                this.outerInstance = outerInstance;
                this.startDocIn = outerInstance.docIn;
                this.docIn = null;
                this.posIn = (IndexInput)outerInstance.posIn.Clone();
                encoded = new byte[ForUtil.MAX_ENCODED_SIZE];
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                indexHasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                indexHasPayloads = fieldInfo.HasPayloads;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CanReuse(IndexInput docIn, FieldInfo fieldInfo)
            {
                return docIn == startDocIn && 
                    indexHasOffsets == (IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0) 
                    && indexHasPayloads == fieldInfo.HasPayloads;
            }

            public DocsAndPositionsEnum Reset(IBits liveDocs, Lucene41PostingsWriter.Int32BlockTermState termState)
            {
                this.liveDocs = liveDocs;
                // if (DEBUG) {
                //   System.out.println("  FPR.reset: termState=" + termState);
                // }
                docFreq = termState.DocFreq;
                docTermStartFP = termState.docStartFP;
                posTermStartFP = termState.posStartFP;
                payTermStartFP = termState.payStartFP;
                skipOffset = termState.skipOffset;
                totalTermFreq = termState.TotalTermFreq;
                singletonDocID = termState.singletonDocID;
                if (docFreq > 1)
                {
                    if (docIn is null)
                    {
                        // lazy init
                        docIn = (IndexInput)startDocIn.Clone();
                    }
                    docIn.Seek(docTermStartFP);
                }
                posPendingFP = posTermStartFP;
                posPendingCount = 0;
                if (termState.TotalTermFreq < Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    lastPosBlockFP = posTermStartFP;
                }
                else if (termState.TotalTermFreq == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    lastPosBlockFP = -1;
                }
                else
                {
                    lastPosBlockFP = posTermStartFP + termState.lastPosBlockOffset;
                }

                doc = -1;
                accum = 0;
                docUpto = 0;
                nextSkipDoc = Lucene41PostingsFormat.BLOCK_SIZE - 1;
                docBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                skipped = false;
                return this;
            }

            public override int Freq => freq;

            public override int DocID => doc;

            private void RefillDocs()
            {
                int left = docFreq - docUpto;
                if (Debugging.AssertsEnabled) Debugging.Assert(left > 0);

                if (left >= Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    // if (DEBUG) {
                    //   System.out.println("    fill doc block from fp=" + docIn.getFilePointer());
                    // }
                    outerInstance.forUtil.ReadBlock(docIn, encoded, docDeltaBuffer);
                    // if (DEBUG) {
                    //   System.out.println("    fill freq block from fp=" + docIn.getFilePointer());
                    // }
                    outerInstance.forUtil.ReadBlock(docIn, encoded, freqBuffer);
                }
                else if (docFreq == 1)
                {
                    docDeltaBuffer[0] = singletonDocID;
                    freqBuffer[0] = (int)totalTermFreq;
                }
                else
                {
                    // Read vInts:
                    // if (DEBUG) {
                    //   System.out.println("    fill last vInt doc block from fp=" + docIn.getFilePointer());
                    // }
                    ReadVInt32Block(docIn, docDeltaBuffer, freqBuffer, left, true);
                }
                docBufferUpto = 0;
            }

            private void RefillPositions()
            {
                // if (DEBUG) {
                //   System.out.println("      refillPositions");
                // }
                if (posIn.Position == lastPosBlockFP) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                {
                    // if (DEBUG) {
                    //   System.out.println("        vInt pos block @ fp=" + posIn.getFilePointer() + " hasPayloads=" + indexHasPayloads + " hasOffsets=" + indexHasOffsets);
                    // }
                    int count = (int)(totalTermFreq % Lucene41PostingsFormat.BLOCK_SIZE);
                    int payloadLength = 0;
                    for (int i = 0; i < count; i++)
                    {
                        int code = posIn.ReadVInt32();
                        if (indexHasPayloads)
                        {
                            if ((code & 1) != 0)
                            {
                                payloadLength = posIn.ReadVInt32();
                            }
                            posDeltaBuffer[i] = code.TripleShift(1);
                            if (payloadLength != 0)
                            {
                                posIn.Seek(posIn.Position + payloadLength); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                            }
                        }
                        else
                        {
                            posDeltaBuffer[i] = code;
                        }
                        if (indexHasOffsets)
                        {
                            if ((posIn.ReadVInt32() & 1) != 0)
                            {
                                // offset length changed
                                posIn.ReadVInt32();
                            }
                        }
                    }
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("        bulk pos block @ fp=" + posIn.getFilePointer());
                    // }
                    outerInstance.forUtil.ReadBlock(posIn, encoded, posDeltaBuffer);
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
                    if (docUpto == docFreq)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                    if (docBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                    {
                        RefillDocs();
                    }
                    // if (DEBUG) {
                    //   System.out.println("    accum=" + accum + " docDeltaBuffer[" + docBufferUpto + "]=" + docDeltaBuffer[docBufferUpto]);
                    // }
                    accum += docDeltaBuffer[docBufferUpto];
                    freq = freqBuffer[docBufferUpto];
                    posPendingCount += freq;
                    docBufferUpto++;
                    docUpto++;

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        doc = accum;
                        position = 0;
                        // if (DEBUG) {
                        //   System.out.println("    return doc=" + doc + " freq=" + freq + " posPendingCount=" + posPendingCount);
                        // }
                        return doc;
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

                if (docFreq > Lucene41PostingsFormat.BLOCK_SIZE && target > nextSkipDoc)
                {
                    // if (DEBUG) {
                    //   System.out.println("    try skipper");
                    // }
                    if (skipper is null)
                    {
                        // Lazy init: first time this enum has ever been used for skipping
                        // if (DEBUG) {
                        //   System.out.println("    create skipper");
                        // }
                        skipper = new Lucene41SkipReader((IndexInput)docIn.Clone(), Lucene41PostingsWriter.maxSkipLevels, Lucene41PostingsFormat.BLOCK_SIZE, true, indexHasOffsets, indexHasPayloads);
                    }

                    if (!skipped)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(skipOffset != -1);
                        // this is the first time this enum has skipped
                        // since reset() was called; load the skip data:
                        // if (DEBUG) {
                        //   System.out.println("    init skipper");
                        // }
                        skipper.Init(docTermStartFP + skipOffset, docTermStartFP, posTermStartFP, payTermStartFP, docFreq);
                        skipped = true;
                    }

                    int newDocUpto = skipper.SkipTo(target) + 1;

                    if (newDocUpto > docUpto)
                    {
                        // Skipper moved
                        // if (DEBUG) {
                        //   System.out.println("    skipper moved to docUpto=" + newDocUpto + " vs current=" + docUpto + "; docID=" + skipper.getDoc() + " fp=" + skipper.getDocPointer() + " pos.fp=" + skipper.getPosPointer() + " pos.bufferUpto=" + skipper.getPosBufferUpto());
                        // }

                        if (Debugging.AssertsEnabled) Debugging.Assert(newDocUpto % Lucene41PostingsFormat.BLOCK_SIZE == 0,"got {0}", newDocUpto);
                        docUpto = newDocUpto;

                        // Force to read next block
                        docBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                        accum = skipper.Doc;
                        docIn.Seek(skipper.DocPointer);
                        posPendingFP = skipper.PosPointer;
                        posPendingCount = skipper.PosBufferUpto;
                    }
                    nextSkipDoc = skipper.NextSkipDoc;
                }
                if (docUpto == docFreq)
                {
                    return doc = NO_MORE_DOCS;
                }
                if (docBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
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
                    accum += docDeltaBuffer[docBufferUpto];
                    freq = freqBuffer[docBufferUpto];
                    posPendingCount += freq;
                    docBufferUpto++;
                    docUpto++;

                    if (accum >= target)
                    {
                        break;
                    }
                    if (docUpto == docFreq)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                }

                if (liveDocs is null || liveDocs.Get(accum))
                {
                    // if (DEBUG) {
                    //   System.out.println("  return doc=" + accum);
                    // }
                    position = 0;
                    return doc = accum;
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
                int toSkip = posPendingCount - freq;
                // if (DEBUG) {
                //   System.out.println("      FPR.skipPositions: toSkip=" + toSkip);
                // }

                int leftInBlock = Lucene41PostingsFormat.BLOCK_SIZE - posBufferUpto;
                if (toSkip < leftInBlock)
                {
                    posBufferUpto += toSkip;
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
                        if (Debugging.AssertsEnabled) Debugging.Assert(posIn.Position != lastPosBlockFP); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        outerInstance.forUtil.SkipBlock(posIn);
                        toSkip -= Lucene41PostingsFormat.BLOCK_SIZE;
                    }
                    RefillPositions();
                    posBufferUpto = toSkip;
                    // if (DEBUG) {
                    //   System.out.println("        skip w/in block to posBufferUpto=" + posBufferUpto);
                    // }
                }

                position = 0;
            }

            public override int NextPosition()
            {
                // if (DEBUG) {
                //   System.out.println("    FPR.nextPosition posPendingCount=" + posPendingCount + " posBufferUpto=" + posBufferUpto);
                // }
                if (posPendingFP != -1)
                {
                    // if (DEBUG) {
                    //   System.out.println("      seek to pendingFP=" + posPendingFP);
                    // }
                    posIn.Seek(posPendingFP);
                    posPendingFP = -1;

                    // Force buffer refill:
                    posBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                }

                if (posPendingCount > freq)
                {
                    SkipPositions();
                    posPendingCount = freq;
                }

                if (posBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    RefillPositions();
                    posBufferUpto = 0;
                }
                position += posDeltaBuffer[posBufferUpto++];
                posPendingCount--;
                // if (DEBUG) {
                //   System.out.println("      return pos=" + position);
                // }
                return position;
            }

            public override int StartOffset => -1;

            public override int EndOffset => -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override BytesRef GetPayload()
            {
                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return docFreq;
            }
        }

        // Also handles payloads + offsets
        internal sealed class EverythingEnum : DocsAndPositionsEnum
        {
            private readonly Lucene41PostingsReader outerInstance;

            private readonly byte[] encoded;

            private readonly int[] docDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] freqBuffer = new int[ForUtil.MAX_DATA_SIZE];
            private readonly int[] posDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];

            private readonly int[] payloadLengthBuffer;
            private readonly int[] offsetStartDeltaBuffer;
            private readonly int[] offsetLengthBuffer;

            private byte[] payloadBytes;
            private int payloadByteUpto;
            private int payloadLength;

            private int lastStartOffset;
            private int startOffset;
            private int endOffset;

            private int docBufferUpto;
            private int posBufferUpto;

            private Lucene41SkipReader skipper;
            private bool skipped;

            internal readonly IndexInput startDocIn;

            internal IndexInput docIn;
            internal readonly IndexInput posIn;
            internal readonly IndexInput payIn;
            internal readonly BytesRef payload;

            internal readonly bool indexHasOffsets;
            internal readonly bool indexHasPayloads;

            private int docFreq; // number of docs in this posting list
            private long totalTermFreq; // number of positions in this posting list
            private int docUpto; // how many docs we've read
            private int doc; // doc we last read
            private int accum; // accumulator for doc deltas
            private int freq; // freq we last read
            private int position; // current position

            // how many positions "behind" we are; nextPosition must
            // skip these to "catch up":
            private int posPendingCount;

            // Lazy pos seek: if != -1 then we must seek to this FP
            // before reading positions:
            private long posPendingFP;

            // Lazy pay seek: if != -1 then we must seek to this FP
            // before reading payloads/offsets:
            private long payPendingFP;

            // Where this term's postings start in the .doc file:
            private long docTermStartFP;

            // Where this term's postings start in the .pos file:
            private long posTermStartFP;

            // Where this term's payloads/offsets start in the .pay
            // file:
            private long payTermStartFP;

            // File pointer where the last (vInt encoded) pos delta
            // block is.  We need this to know whether to bulk
            // decode vs vInt decode the block:
            private long lastPosBlockFP;

            // Where this term's skip data starts (after
            // docTermStartFP) in the .doc file (or -1 if there is
            // no skip data for this term):
            private long skipOffset;

            private int nextSkipDoc;

            private IBits liveDocs;

            private bool needsOffsets; // true if we actually need offsets
            private bool needsPayloads; // true if we actually need payloads
            private int singletonDocID; // docid when there is a single pulsed posting, otherwise -1

            public EverythingEnum(Lucene41PostingsReader outerInstance, FieldInfo fieldInfo)
            {
                this.outerInstance = outerInstance;
                this.startDocIn = outerInstance.docIn;
                this.docIn = null;
                this.posIn = (IndexInput)outerInstance.posIn.Clone();
                this.payIn = (IndexInput)outerInstance.payIn.Clone();
                encoded = new byte[ForUtil.MAX_ENCODED_SIZE];
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                indexHasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                if (indexHasOffsets)
                {
                    offsetStartDeltaBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    offsetLengthBuffer = new int[ForUtil.MAX_DATA_SIZE];
                }
                else
                {
                    offsetStartDeltaBuffer = null;
                    offsetLengthBuffer = null;
                    startOffset = -1;
                    endOffset = -1;
                }

                indexHasPayloads = fieldInfo.HasPayloads;
                if (indexHasPayloads)
                {
                    payloadLengthBuffer = new int[ForUtil.MAX_DATA_SIZE];
                    payloadBytes = new byte[128];
                    payload = new BytesRef();
                }
                else
                {
                    payloadLengthBuffer = null;
                    payloadBytes = null;
                    payload = null;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CanReuse(IndexInput docIn, FieldInfo fieldInfo)
            {
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                return docIn == startDocIn && 
                    indexHasOffsets == (IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0) && 
                    indexHasPayloads == fieldInfo.HasPayloads;
            }

            public EverythingEnum Reset(IBits liveDocs, Lucene41PostingsWriter.Int32BlockTermState termState, DocsAndPositionsFlags flags)
            {
                this.liveDocs = liveDocs;
                // if (DEBUG) {
                //   System.out.println("  FPR.reset: termState=" + termState);
                // }
                docFreq = termState.DocFreq;
                docTermStartFP = termState.docStartFP;
                posTermStartFP = termState.posStartFP;
                payTermStartFP = termState.payStartFP;
                skipOffset = termState.skipOffset;
                totalTermFreq = termState.TotalTermFreq;
                singletonDocID = termState.singletonDocID;
                if (docFreq > 1)
                {
                    if (docIn is null)
                    {
                        // lazy init
                        docIn = (IndexInput)startDocIn.Clone();
                    }
                    docIn.Seek(docTermStartFP);
                }
                posPendingFP = posTermStartFP;
                payPendingFP = payTermStartFP;
                posPendingCount = 0;
                if (termState.TotalTermFreq < Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    lastPosBlockFP = posTermStartFP;
                }
                else if (termState.TotalTermFreq == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    lastPosBlockFP = -1;
                }
                else
                {
                    lastPosBlockFP = posTermStartFP + termState.lastPosBlockOffset;
                }

                this.needsOffsets = (flags & DocsAndPositionsFlags.OFFSETS) != 0;
                this.needsPayloads = (flags & DocsAndPositionsFlags.PAYLOADS) != 0;

                doc = -1;
                accum = 0;
                docUpto = 0;
                nextSkipDoc = Lucene41PostingsFormat.BLOCK_SIZE - 1;
                docBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                skipped = false;
                return this;
            }

            public override int Freq => freq;

            public override int DocID => doc;

            private void RefillDocs()
            {
                int left = docFreq - docUpto;
                if (Debugging.AssertsEnabled) Debugging.Assert(left > 0);

                if (left >= Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    // if (DEBUG) {
                    //   System.out.println("    fill doc block from fp=" + docIn.getFilePointer());
                    // }
                    outerInstance.forUtil.ReadBlock(docIn, encoded, docDeltaBuffer);
                    // if (DEBUG) {
                    //   System.out.println("    fill freq block from fp=" + docIn.getFilePointer());
                    // }
                    outerInstance.forUtil.ReadBlock(docIn, encoded, freqBuffer);
                }
                else if (docFreq == 1)
                {
                    docDeltaBuffer[0] = singletonDocID;
                    freqBuffer[0] = (int)totalTermFreq;
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("    fill last vInt doc block from fp=" + docIn.getFilePointer());
                    // }
                    ReadVInt32Block(docIn, docDeltaBuffer, freqBuffer, left, true);
                }
                docBufferUpto = 0;
            }

            private void RefillPositions()
            {
                // if (DEBUG) {
                //   System.out.println("      refillPositions");
                // }
                if (posIn.Position == lastPosBlockFP) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                {
                    // if (DEBUG) {
                    //   System.out.println("        vInt pos block @ fp=" + posIn.getFilePointer() + " hasPayloads=" + indexHasPayloads + " hasOffsets=" + indexHasOffsets);
                    // }
                    int count = (int)(totalTermFreq % Lucene41PostingsFormat.BLOCK_SIZE);
                    int payloadLength = 0;
                    int offsetLength = 0;
                    payloadByteUpto = 0;
                    for (int i = 0; i < count; i++)
                    {
                        int code = posIn.ReadVInt32();
                        if (indexHasPayloads)
                        {
                            if ((code & 1) != 0)
                            {
                                payloadLength = posIn.ReadVInt32();
                            }
                            // if (DEBUG) {
                            //   System.out.println("        i=" + i + " payloadLen=" + payloadLength);
                            // }
                            payloadLengthBuffer[i] = payloadLength;
                            posDeltaBuffer[i] = code.TripleShift(1);
                            if (payloadLength != 0)
                            {
                                if (payloadByteUpto + payloadLength > payloadBytes.Length)
                                {
                                    payloadBytes = ArrayUtil.Grow(payloadBytes, payloadByteUpto + payloadLength);
                                }
                                //System.out.println("          read payload @ pos.fp=" + posIn.getFilePointer());
                                posIn.ReadBytes(payloadBytes, payloadByteUpto, payloadLength);
                                payloadByteUpto += payloadLength;
                            }
                        }
                        else
                        {
                            posDeltaBuffer[i] = code;
                        }

                        if (indexHasOffsets)
                        {
                            // if (DEBUG) {
                            //   System.out.println("        i=" + i + " read offsets from posIn.fp=" + posIn.getFilePointer());
                            // }
                            int deltaCode = posIn.ReadVInt32();
                            if ((deltaCode & 1) != 0)
                            {
                                offsetLength = posIn.ReadVInt32();
                            }
                            offsetStartDeltaBuffer[i] = deltaCode.TripleShift(1);
                            offsetLengthBuffer[i] = offsetLength;
                            // if (DEBUG) {
                            //   System.out.println("          startOffDelta=" + offsetStartDeltaBuffer[i] + " offsetLen=" + offsetLengthBuffer[i]);
                            // }
                        }
                    }
                    payloadByteUpto = 0;
                }
                else
                {
                    // if (DEBUG) {
                    //   System.out.println("        bulk pos block @ fp=" + posIn.getFilePointer());
                    // }
                    outerInstance.forUtil.ReadBlock(posIn, encoded, posDeltaBuffer);

                    if (indexHasPayloads)
                    {
                        // if (DEBUG) {
                        //   System.out.println("        bulk payload block @ pay.fp=" + payIn.getFilePointer());
                        // }
                        if (needsPayloads)
                        {
                            outerInstance.forUtil.ReadBlock(payIn, encoded, payloadLengthBuffer);
                            int numBytes = payIn.ReadVInt32();
                            // if (DEBUG) {
                            //   System.out.println("        " + numBytes + " payload bytes @ pay.fp=" + payIn.getFilePointer());
                            // }
                            if (numBytes > payloadBytes.Length)
                            {
                                payloadBytes = ArrayUtil.Grow(payloadBytes, numBytes);
                            }
                            payIn.ReadBytes(payloadBytes, 0, numBytes);
                        }
                        else
                        {
                            // this works, because when writing a vint block we always force the first length to be written
                            outerInstance.forUtil.SkipBlock(payIn); // skip over lengths
                            int numBytes = payIn.ReadVInt32(); // read length of payloadBytes
                            payIn.Seek(payIn.Position + numBytes); // skip over payloadBytes // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        }
                        payloadByteUpto = 0;
                    }

                    if (indexHasOffsets)
                    {
                        // if (DEBUG) {
                        //   System.out.println("        bulk offset block @ pay.fp=" + payIn.getFilePointer());
                        // }
                        if (needsOffsets)
                        {
                            outerInstance.forUtil.ReadBlock(payIn, encoded, offsetStartDeltaBuffer);
                            outerInstance.forUtil.ReadBlock(payIn, encoded, offsetLengthBuffer);
                        }
                        else
                        {
                            // this works, because when writing a vint block we always force the first length to be written
                            outerInstance.forUtil.SkipBlock(payIn); // skip over starts
                            outerInstance.forUtil.SkipBlock(payIn); // skip over lengths
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
                    if (docUpto == docFreq)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                    if (docBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                    {
                        RefillDocs();
                    }
                    // if (DEBUG) {
                    //   System.out.println("    accum=" + accum + " docDeltaBuffer[" + docBufferUpto + "]=" + docDeltaBuffer[docBufferUpto]);
                    // }
                    accum += docDeltaBuffer[docBufferUpto];
                    freq = freqBuffer[docBufferUpto];
                    posPendingCount += freq;
                    docBufferUpto++;
                    docUpto++;

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        doc = accum;
                        // if (DEBUG) {
                        //   System.out.println("    return doc=" + doc + " freq=" + freq + " posPendingCount=" + posPendingCount);
                        // }
                        position = 0;
                        lastStartOffset = 0;
                        return doc;
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

                if (docFreq > Lucene41PostingsFormat.BLOCK_SIZE && target > nextSkipDoc)
                {
                    // if (DEBUG) {
                    //   System.out.println("    try skipper");
                    // }

                    if (skipper is null)
                    {
                        // Lazy init: first time this enum has ever been used for skipping
                        // if (DEBUG) {
                        //   System.out.println("    create skipper");
                        // }
                        skipper = new Lucene41SkipReader((IndexInput)docIn.Clone(), Lucene41PostingsWriter.maxSkipLevels, Lucene41PostingsFormat.BLOCK_SIZE, true, indexHasOffsets, indexHasPayloads);
                    }

                    if (!skipped)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(skipOffset != -1);
                        // this is the first time this enum has skipped
                        // since reset() was called; load the skip data:
                        // if (DEBUG) {
                        //   System.out.println("    init skipper");
                        // }
                        skipper.Init(docTermStartFP + skipOffset, docTermStartFP, posTermStartFP, payTermStartFP, docFreq);
                        skipped = true;
                    }

                    int newDocUpto = skipper.SkipTo(target) + 1;

                    if (newDocUpto > docUpto)
                    {
                        // Skipper moved
                        // if (DEBUG) {
                        //   System.out.println("    skipper moved to docUpto=" + newDocUpto + " vs current=" + docUpto + "; docID=" + skipper.getDoc() + " fp=" + skipper.getDocPointer() + " pos.fp=" + skipper.getPosPointer() + " pos.bufferUpto=" + skipper.getPosBufferUpto() + " pay.fp=" + skipper.getPayPointer() + " lastStartOffset=" + lastStartOffset);
                        // }
                        if (Debugging.AssertsEnabled) Debugging.Assert(newDocUpto % Lucene41PostingsFormat.BLOCK_SIZE == 0,"got {0}", newDocUpto);
                        docUpto = newDocUpto;

                        // Force to read next block
                        docBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                        accum = skipper.Doc;
                        docIn.Seek(skipper.DocPointer);
                        posPendingFP = skipper.PosPointer;
                        payPendingFP = skipper.PayPointer;
                        posPendingCount = skipper.PosBufferUpto;
                        lastStartOffset = 0; // new document
                        payloadByteUpto = skipper.PayloadByteUpto;
                    }
                    nextSkipDoc = skipper.NextSkipDoc;
                }
                if (docUpto == docFreq)
                {
                    return doc = NO_MORE_DOCS;
                }
                if (docBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    RefillDocs();
                }

                // Now scan:
                while (true)
                {
                    // if (DEBUG) {
                    //   System.out.println("  scan doc=" + accum + " docBufferUpto=" + docBufferUpto);
                    // }
                    accum += docDeltaBuffer[docBufferUpto];
                    freq = freqBuffer[docBufferUpto];
                    posPendingCount += freq;
                    docBufferUpto++;
                    docUpto++;

                    if (accum >= target)
                    {
                        break;
                    }
                    if (docUpto == docFreq)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                }

                if (liveDocs is null || liveDocs.Get(accum))
                {
                    // if (DEBUG) {
                    //   System.out.println("  return doc=" + accum);
                    // }
                    position = 0;
                    lastStartOffset = 0;
                    return doc = accum;
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
                int toSkip = posPendingCount - freq;
                // if (DEBUG) {
                //   System.out.println("      FPR.skipPositions: toSkip=" + toSkip);
                // }

                int leftInBlock = Lucene41PostingsFormat.BLOCK_SIZE - posBufferUpto;
                if (toSkip < leftInBlock)
                {
                    int end = posBufferUpto + toSkip;
                    while (posBufferUpto < end)
                    {
                        if (indexHasPayloads)
                        {
                            payloadByteUpto += payloadLengthBuffer[posBufferUpto];
                        }
                        posBufferUpto++;
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
                        if (Debugging.AssertsEnabled) Debugging.Assert(posIn.Position != lastPosBlockFP); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        outerInstance.forUtil.SkipBlock(posIn);

                        if (indexHasPayloads)
                        {
                            // Skip payloadLength block:
                            outerInstance.forUtil.SkipBlock(payIn);

                            // Skip payloadBytes block:
                            int numBytes = payIn.ReadVInt32();
                            payIn.Seek(payIn.Position + numBytes); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        }

                        if (indexHasOffsets)
                        {
                            outerInstance.forUtil.SkipBlock(payIn);
                            outerInstance.forUtil.SkipBlock(payIn);
                        }
                        toSkip -= Lucene41PostingsFormat.BLOCK_SIZE;
                    }
                    RefillPositions();
                    payloadByteUpto = 0;
                    posBufferUpto = 0;
                    while (posBufferUpto < toSkip)
                    {
                        if (indexHasPayloads)
                        {
                            payloadByteUpto += payloadLengthBuffer[posBufferUpto];
                        }
                        posBufferUpto++;
                    }
                    // if (DEBUG) {
                    //   System.out.println("        skip w/in block to posBufferUpto=" + posBufferUpto);
                    // }
                }

                position = 0;
                lastStartOffset = 0;
            }

            public override int NextPosition()
            {
                // if (DEBUG) {
                //   System.out.println("    FPR.nextPosition posPendingCount=" + posPendingCount + " posBufferUpto=" + posBufferUpto + " payloadByteUpto=" + payloadByteUpto)// ;
                // }
                if (posPendingFP != -1)
                {
                    // if (DEBUG) {
                    //   System.out.println("      seek pos to pendingFP=" + posPendingFP);
                    // }
                    posIn.Seek(posPendingFP);
                    posPendingFP = -1;

                    if (payPendingFP != -1)
                    {
                        // if (DEBUG) {
                        //   System.out.println("      seek pay to pendingFP=" + payPendingFP);
                        // }
                        payIn.Seek(payPendingFP);
                        payPendingFP = -1;
                    }

                    // Force buffer refill:
                    posBufferUpto = Lucene41PostingsFormat.BLOCK_SIZE;
                }

                if (posPendingCount > freq)
                {
                    SkipPositions();
                    posPendingCount = freq;
                }

                if (posBufferUpto == Lucene41PostingsFormat.BLOCK_SIZE)
                {
                    RefillPositions();
                    posBufferUpto = 0;
                }
                position += posDeltaBuffer[posBufferUpto];

                if (indexHasPayloads)
                {
                    payloadLength = payloadLengthBuffer[posBufferUpto];
                    payload.Bytes = payloadBytes;
                    payload.Offset = payloadByteUpto;
                    payload.Length = payloadLength;
                    payloadByteUpto += payloadLength;
                }

                if (indexHasOffsets)
                {
                    startOffset = lastStartOffset + offsetStartDeltaBuffer[posBufferUpto];
                    endOffset = startOffset + offsetLengthBuffer[posBufferUpto];
                    lastStartOffset = startOffset;
                }

                posBufferUpto++;
                posPendingCount--;
                // if (DEBUG) {
                //   System.out.println("      return pos=" + position);
                // }
                return position;
            }

            public override int StartOffset => startOffset;

            public override int EndOffset => endOffset;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override BytesRef GetPayload()
            {
                // if (DEBUG) {
                //   System.out.println("    FPR.getPayload payloadLength=" + payloadLength + " payloadByteUpto=" + payloadByteUpto);
                // }
                if (payloadLength == 0)
                {
                    return null;
                }
                else
                {
                    return payload;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return docFreq;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
            if (version >= Lucene41PostingsWriter.VERSION_CHECKSUM)
            {
                if (docIn != null)
                {
                    CodecUtil.ChecksumEntireFile(docIn);
                }
                if (posIn != null)
                {
                    CodecUtil.ChecksumEntireFile(posIn);
                }
                if (payIn != null)
                {
                    CodecUtil.ChecksumEntireFile(payIn);
                }
            }
        }
    }
}