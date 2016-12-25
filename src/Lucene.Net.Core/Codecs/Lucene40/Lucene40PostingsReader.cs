using Lucene.Net.Support;
using System;
using System.Diagnostics;

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

    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DataInput = Lucene.Net.Store.DataInput;
    using Directory = Lucene.Net.Store.Directory;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using TermState = Lucene.Net.Index.TermState;

    /// <summary>
    /// Concrete class that reads the 4.0 frq/prox
    /// postings format.
    /// </summary>
    ///  <seealso cref= Lucene40PostingsFormat </seealso>
    ///  @deprecated Only for reading old 4.0 segments
    [Obsolete("Only for reading old 4.0 segments")]
    public class Lucene40PostingsReader : PostingsReaderBase
    {
        internal static readonly string TERMS_CODEC = "Lucene40PostingsWriterTerms";
        internal static readonly string FRQ_CODEC = "Lucene40PostingsWriterFrq";
        internal static readonly string PRX_CODEC = "Lucene40PostingsWriterPrx";

        //private static boolean DEBUG = BlockTreeTermsWriter.DEBUG;

        // Increment version to change it:
        internal static readonly int VERSION_START = 0;

        internal static readonly int VERSION_LONG_SKIP = 1;
        internal static readonly int VERSION_CURRENT = VERSION_LONG_SKIP;

        private readonly IndexInput FreqIn;
        private readonly IndexInput ProxIn;
        // public static boolean DEBUG = BlockTreeTermsWriter.DEBUG;

        internal int SkipInterval;
        internal int MaxSkipLevels;
        internal int SkipMinimum;

        // private String segment;

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40PostingsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo segmentInfo, IOContext ioContext, string segmentSuffix)
        {
            bool success = false;
            IndexInput freqIn = null;
            IndexInput proxIn = null;
            try
            {
                freqIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, Lucene40PostingsFormat.FREQ_EXTENSION), ioContext);
                CodecUtil.CheckHeader(freqIn, FRQ_CODEC, VERSION_START, VERSION_CURRENT);
                // TODO: hasProx should (somehow!) become codec private,
                // but it's tricky because 1) FIS.hasProx is global (it
                // could be all fields that have prox are written by a
                // different codec), 2) the field may have had prox in
                // the past but all docs w/ that field were deleted.
                // Really we'd need to init prxOut lazily on write, and
                // then somewhere record that we actually wrote it so we
                // know whether to open on read:
                if (fieldInfos.HasProx)
                {
                    proxIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.Name, segmentSuffix, Lucene40PostingsFormat.PROX_EXTENSION), ioContext);
                    CodecUtil.CheckHeader(proxIn, PRX_CODEC, VERSION_START, VERSION_CURRENT);
                }
                else
                {
                    proxIn = null;
                }
                this.FreqIn = freqIn;
                this.ProxIn = proxIn;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(freqIn, proxIn);
                }
            }
        }

        public override void Init(IndexInput termsIn)
        {
            // Make sure we are talking to the matching past writer
            CodecUtil.CheckHeader(termsIn, TERMS_CODEC, VERSION_START, VERSION_CURRENT);

            SkipInterval = termsIn.ReadInt();
            MaxSkipLevels = termsIn.ReadInt();
            SkipMinimum = termsIn.ReadInt();
        }

        // Must keep final because we do non-standard clone
        private sealed class StandardTermState : BlockTermState
        {
            internal long FreqOffset;
            internal long ProxOffset;
            internal long SkipOffset;

            public override object Clone()
            {
                StandardTermState other = new StandardTermState();
                other.CopyFrom(this);
                return other;
            }

            public override void CopyFrom(TermState _other)
            {
                base.CopyFrom(_other);
                StandardTermState other = (StandardTermState)_other;
                FreqOffset = other.FreqOffset;
                ProxOffset = other.ProxOffset;
                SkipOffset = other.SkipOffset;
            }

            public override string ToString()
            {
                return base.ToString() + " freqFP=" + FreqOffset + " proxFP=" + ProxOffset + " skipOffset=" + SkipOffset;
            }
        }

        public override BlockTermState NewTermState()
        {
            return new StandardTermState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (FreqIn != null)
                    {
                        FreqIn.Dispose();
                    }
                }
                finally
                {
                    if (ProxIn != null)
                    {
                        ProxIn.Dispose();
                    }
                }
            }
        }

        public override void DecodeTerm(long[] longs, DataInput @in, FieldInfo fieldInfo, BlockTermState _termState, bool absolute)
        {
            StandardTermState termState = (StandardTermState)_termState;
            // if (DEBUG) System.out.println("SPR: nextTerm seg=" + segment + " tbOrd=" + termState.termBlockOrd + " bytesReader.fp=" + termState.bytesReader.getPosition());
            bool isFirstTerm = termState.TermBlockOrd == 0;
            if (absolute)
            {
                termState.FreqOffset = 0;
                termState.ProxOffset = 0;
            }

            termState.FreqOffset += @in.ReadVLong();
            /*
            if (DEBUG) {
              System.out.println("  dF=" + termState.docFreq);
              System.out.println("  freqFP=" + termState.freqOffset);
            }
            */
            Debug.Assert(termState.FreqOffset < FreqIn.Length());

            if (termState.DocFreq >= SkipMinimum)
            {
                termState.SkipOffset = @in.ReadVLong();
                // if (DEBUG) System.out.println("  skipOffset=" + termState.skipOffset + " vs freqIn.length=" + freqIn.length());
                Debug.Assert(termState.FreqOffset + termState.SkipOffset < FreqIn.Length());
            }
            else
            {
                // undefined
            }

            if (fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                termState.ProxOffset += @in.ReadVLong();
                // if (DEBUG) System.out.println("  proxFP=" + termState.proxOffset);
            }
        }

        public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState termState, Bits liveDocs, DocsEnum reuse, int flags)
        {
            if (CanReuse(reuse, liveDocs))
            {
                // if (DEBUG) System.out.println("SPR.docs ts=" + termState);
                return ((SegmentDocsEnumBase)reuse).Reset(fieldInfo, (StandardTermState)termState);
            }
            return NewDocsEnum(liveDocs, fieldInfo, (StandardTermState)termState);
        }

        private bool CanReuse(DocsEnum reuse, Bits liveDocs)
        {
            if (reuse != null && (reuse is SegmentDocsEnumBase))
            {
                SegmentDocsEnumBase docsEnum = (SegmentDocsEnumBase)reuse;
                // If you are using ParellelReader, and pass in a
                // reused DocsEnum, it could have come from another
                // reader also using standard codec
                if (docsEnum.StartFreqIn == FreqIn)
                {
                    // we only reuse if the the actual the incoming enum has the same liveDocs as the given liveDocs
                    return liveDocs == docsEnum.LiveDocs;
                }
            }
            return false;
        }

        private DocsEnum NewDocsEnum(Bits liveDocs, FieldInfo fieldInfo, StandardTermState termState)
        {
            if (liveDocs == null)
            {
                return (new AllDocsSegmentDocsEnum(this, FreqIn)).Reset(fieldInfo, termState);
            }
            else
            {
                return (new LiveDocsSegmentDocsEnum(this, FreqIn, liveDocs)).Reset(fieldInfo, termState);
            }
        }

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState termState, Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            bool hasOffsets = fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;

            // TODO: can we optimize if FLAG_PAYLOADS / FLAG_OFFSETS
            // isn't passed?

            // TODO: refactor
            if (fieldInfo.HasPayloads || hasOffsets)
            {
                SegmentFullPositionsEnum docsEnum;
                if (reuse == null || !(reuse is SegmentFullPositionsEnum))
                {
                    docsEnum = new SegmentFullPositionsEnum(this, FreqIn, ProxIn);
                }
                else
                {
                    docsEnum = (SegmentFullPositionsEnum)reuse;
                    if (docsEnum.StartFreqIn != FreqIn)
                    {
                        // If you are using ParellelReader, and pass in a
                        // reused DocsEnum, it could have come from another
                        // reader also using standard codec
                        docsEnum = new SegmentFullPositionsEnum(this, FreqIn, ProxIn);
                    }
                }
                return docsEnum.Reset(fieldInfo, (StandardTermState)termState, liveDocs);
            }
            else
            {
                SegmentDocsAndPositionsEnum docsEnum;
                if (reuse == null || !(reuse is SegmentDocsAndPositionsEnum))
                {
                    docsEnum = new SegmentDocsAndPositionsEnum(this, FreqIn, ProxIn);
                }
                else
                {
                    docsEnum = (SegmentDocsAndPositionsEnum)reuse;
                    if (docsEnum.StartFreqIn != FreqIn)
                    {
                        // If you are using ParellelReader, and pass in a
                        // reused DocsEnum, it could have come from another
                        // reader also using standard codec
                        docsEnum = new SegmentDocsAndPositionsEnum(this, FreqIn, ProxIn);
                    }
                }
                return docsEnum.Reset(fieldInfo, (StandardTermState)termState, liveDocs);
            }
        }

        internal static readonly int BUFFERSIZE = 64;

        private abstract class SegmentDocsEnumBase : DocsEnum
        {
            private readonly Lucene40PostingsReader OuterInstance;

            protected internal readonly int[] Docs = new int[BUFFERSIZE];
            protected internal readonly int[] Freqs = new int[BUFFERSIZE];

            internal readonly IndexInput FreqIn; // reuse
            internal readonly IndexInput StartFreqIn; // reuse
            internal Lucene40SkipListReader Skipper; // reuse - lazy loaded

            protected internal bool IndexOmitsTF; // does current field omit term freq?
            protected internal bool StorePayloads; // does current field store payloads?
            protected internal bool StoreOffsets; // does current field store offsets?

            protected internal int Limit; // number of docs in this posting
            protected internal int Ord; // how many docs we've read
            protected internal int Doc; // doc we last read
            protected internal int Accum; // accumulator for doc deltas
            protected internal int Freq_Renamed; // freq we last read
            protected internal int MaxBufferedDocId;

            protected internal int Start;
            protected internal int Count;

            protected internal long FreqOffset;
            protected internal long SkipOffset;

            protected internal bool Skipped;
            protected internal readonly Bits LiveDocs;

            internal SegmentDocsEnumBase(Lucene40PostingsReader outerInstance, IndexInput startFreqIn, Bits liveDocs)
            {
                this.OuterInstance = outerInstance;
                this.StartFreqIn = startFreqIn;
                this.FreqIn = (IndexInput)startFreqIn.Clone();
                this.LiveDocs = liveDocs;
            }

            internal virtual DocsEnum Reset(FieldInfo fieldInfo, StandardTermState termState)
            {
                IndexOmitsTF = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY;
                StorePayloads = fieldInfo.HasPayloads;
                StoreOffsets = fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                FreqOffset = termState.FreqOffset;
                SkipOffset = termState.SkipOffset;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                FreqIn.Seek(termState.FreqOffset);
                Limit = termState.DocFreq;
                Debug.Assert(Limit > 0);
                Ord = 0;
                Doc = -1;
                Accum = 0;
                // if (DEBUG) System.out.println("  sde limit=" + limit + " freqFP=" + freqOffset);
                Skipped = false;

                Start = -1;
                Count = 0;
                Freq_Renamed = 1;
                if (IndexOmitsTF)
                {
                    CollectionsHelper.Fill(Freqs, 1);
                }
                MaxBufferedDocId = -1;
                return this;
            }

            public override sealed int Freq
            {
                get { return Freq_Renamed; }
            }

            public override sealed int DocID
            {
                get { return Doc; }
            }

            public override sealed int Advance(int target)
            {
                // last doc in our buffer is >= target, binary search + next()
                if (++Start < Count && MaxBufferedDocId >= target)
                {
                    if ((Count - Start) > 32) // 32 seemed to be a sweetspot here so use binsearch if the pending results are a lot
                    {
                        Start = BinarySearch(Count - 1, Start, target, Docs);
                        return NextDoc();
                    }
                    else
                    {
                        return LinearScan(target);
                    }
                }

                Start = Count; // buffer is consumed

                return Doc = SkipTo(target);
            }

            private int BinarySearch(int hi, int low, int target, int[] docs)
            {
                while (low <= hi)
                {
                    int mid = (int)((uint)(hi + low) >> 1);
                    int doc = docs[mid];
                    if (doc < target)
                    {
                        low = mid + 1;
                    }
                    else if (doc > target)
                    {
                        hi = mid - 1;
                    }
                    else
                    {
                        low = mid;
                        break;
                    }
                }
                return low - 1;
            }

            internal int ReadFreq(IndexInput freqIn, int code)
            {
                if ((code & 1) != 0) // if low bit is set
                {
                    return 1; // freq is one
                }
                else
                {
                    return freqIn.ReadVInt(); // else read freq
                }
            }

            protected internal abstract int LinearScan(int scanTo);

            protected internal abstract int ScanTo(int target);

            protected internal int Refill()
            {
                int doc = NextUnreadDoc();
                Count = 0;
                Start = -1;
                if (doc == NO_MORE_DOCS)
                {
                    return NO_MORE_DOCS;
                }
                int numDocs = Math.Min(Docs.Length, Limit - Ord);
                Ord += numDocs;
                if (IndexOmitsTF)
                {
                    Count = FillDocs(numDocs);
                }
                else
                {
                    Count = FillDocsAndFreqs(numDocs);
                }
                MaxBufferedDocId = Count > 0 ? Docs[Count - 1] : NO_MORE_DOCS;
                return doc;
            }

            protected internal abstract int NextUnreadDoc();

            private int FillDocs(int size)
            {
                IndexInput freqIn = this.FreqIn;
                int[] docs = this.Docs;
                int docAc = Accum;
                for (int i = 0; i < size; i++)
                {
                    docAc += freqIn.ReadVInt();
                    docs[i] = docAc;
                }
                Accum = docAc;
                return size;
            }

            private int FillDocsAndFreqs(int size)
            {
                IndexInput freqIn = this.FreqIn;
                int[] docs = this.Docs;
                int[] freqs = this.Freqs;
                int docAc = Accum;
                for (int i = 0; i < size; i++)
                {
                    int code = freqIn.ReadVInt();
                    docAc += (int)((uint)code >> 1); // shift off low bit
                    freqs[i] = ReadFreq(freqIn, code);
                    docs[i] = docAc;
                }
                Accum = docAc;
                return size;
            }

            private int SkipTo(int target)
            {
                if ((target - OuterInstance.SkipInterval) >= Accum && Limit >= OuterInstance.SkipMinimum)
                {
                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close.

                    if (Skipper == null)
                    {
                        // this is the first time this enum has ever been used for skipping -- do lazy init
                        Skipper = new Lucene40SkipListReader((IndexInput)FreqIn.Clone(), OuterInstance.MaxSkipLevels, OuterInstance.SkipInterval);
                    }

                    if (!Skipped)
                    {
                        // this is the first time this posting has
                        // skipped since reset() was called, so now we
                        // load the skip data for this posting

                        Skipper.Init(FreqOffset + SkipOffset, FreqOffset, 0, Limit, StorePayloads, StoreOffsets);

                        Skipped = true;
                    }

                    int newOrd = Skipper.SkipTo(target);

                    if (newOrd > Ord)
                    {
                        // Skipper moved

                        Ord = newOrd;
                        Accum = Skipper.Doc;
                        FreqIn.Seek(Skipper.FreqPointer);
                    }
                }
                return ScanTo(target);
            }

            public override long Cost()
            {
                return Limit;
            }
        }

        private sealed class AllDocsSegmentDocsEnum : SegmentDocsEnumBase
        {
            private readonly Lucene40PostingsReader OuterInstance;

            internal AllDocsSegmentDocsEnum(Lucene40PostingsReader outerInstance, IndexInput startFreqIn)
                : base(outerInstance, startFreqIn, null)
            {
                this.OuterInstance = outerInstance;
                Debug.Assert(LiveDocs == null);
            }

            public override int NextDoc()
            {
                if (++Start < Count)
                {
                    Freq_Renamed = Freqs[Start];
                    return Doc = Docs[Start];
                }
                return Doc = Refill();
            }

            protected internal override int LinearScan(int scanTo)
            {
                int[] docs = this.Docs;
                int upTo = Count;
                for (int i = Start; i < upTo; i++)
                {
                    int d = docs[i];
                    if (scanTo <= d)
                    {
                        Start = i;
                        Freq_Renamed = Freqs[i];
                        return Doc = docs[i];
                    }
                }
                return Doc = Refill();
            }

            protected internal override int ScanTo(int target)
            {
                int docAcc = Accum;
                int frq = 1;
                IndexInput freqIn = this.FreqIn;
                bool omitTF = IndexOmitsTF;
                int loopLimit = Limit;
                for (int i = Ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += (int)((uint)code >> 1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (docAcc >= target)
                    {
                        Freq_Renamed = frq;
                        Ord = i + 1;
                        return Accum = docAcc;
                    }
                }
                Ord = Limit;
                Freq_Renamed = frq;
                Accum = docAcc;
                return NO_MORE_DOCS;
            }

            protected internal override int NextUnreadDoc()
            {
                if (Ord++ < Limit)
                {
                    int code = FreqIn.ReadVInt();
                    if (IndexOmitsTF)
                    {
                        Accum += code;
                    }
                    else
                    {
                        Accum += (int)((uint)code >> 1); // shift off low bit
                        Freq_Renamed = ReadFreq(FreqIn, code);
                    }
                    return Accum;
                }
                else
                {
                    return NO_MORE_DOCS;
                }
            }
        }

        private sealed class LiveDocsSegmentDocsEnum : SegmentDocsEnumBase
        {
            private readonly Lucene40PostingsReader OuterInstance;

            internal LiveDocsSegmentDocsEnum(Lucene40PostingsReader outerInstance, IndexInput startFreqIn, Bits liveDocs)
                : base(outerInstance, startFreqIn, liveDocs)
            {
                this.OuterInstance = outerInstance;
                Debug.Assert(liveDocs != null);
            }

            public override int NextDoc()
            {
                Bits liveDocs = this.LiveDocs;
                for (int i = Start + 1; i < Count; i++)
                {
                    int d = Docs[i];
                    if (liveDocs.Get(d))
                    {
                        Start = i;
                        Freq_Renamed = Freqs[i];
                        return Doc = d;
                    }
                }
                Start = Count;
                return Doc = Refill();
            }

            protected internal override int LinearScan(int scanTo)
            {
                int[] docs = this.Docs;
                int upTo = Count;
                Bits liveDocs = this.LiveDocs;
                for (int i = Start; i < upTo; i++)
                {
                    int d = docs[i];
                    if (scanTo <= d && liveDocs.Get(d))
                    {
                        Start = i;
                        Freq_Renamed = Freqs[i];
                        return Doc = docs[i];
                    }
                }
                return Doc = Refill();
            }

            protected internal override int ScanTo(int target)
            {
                int docAcc = Accum;
                int frq = 1;
                IndexInput freqIn = this.FreqIn;
                bool omitTF = IndexOmitsTF;
                int loopLimit = Limit;
                Bits liveDocs = this.LiveDocs;
                for (int i = Ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += (int)((uint)code >> 1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (docAcc >= target && liveDocs.Get(docAcc))
                    {
                        Freq_Renamed = frq;
                        Ord = i + 1;
                        return Accum = docAcc;
                    }
                }
                Ord = Limit;
                Freq_Renamed = frq;
                Accum = docAcc;
                return NO_MORE_DOCS;
            }

            protected internal override int NextUnreadDoc()
            {
                int docAcc = Accum;
                int frq = 1;
                IndexInput freqIn = this.FreqIn;
                bool omitTF = IndexOmitsTF;
                int loopLimit = Limit;
                Bits liveDocs = this.LiveDocs;
                for (int i = Ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += (int)((uint)code >> 1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (liveDocs.Get(docAcc))
                    {
                        Freq_Renamed = frq;
                        Ord = i + 1;
                        return Accum = docAcc;
                    }
                }
                Ord = Limit;
                Freq_Renamed = frq;
                Accum = docAcc;
                return NO_MORE_DOCS;
            }
        }

        // TODO specialize DocsAndPosEnum too

        // Decodes docs & positions. payloads nor offsets are present.
        private sealed class SegmentDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene40PostingsReader OuterInstance;

            internal readonly IndexInput StartFreqIn;
            internal readonly IndexInput FreqIn;
            internal readonly IndexInput ProxIn;
            internal int Limit; // number of docs in this posting
            internal int Ord; // how many docs we've read
            internal int Doc = -1; // doc we last read
            internal int Accum; // accumulator for doc deltas
            internal int Freq_Renamed; // freq we last read
            internal int Position;

            internal Bits LiveDocs;

            internal long FreqOffset;
            internal long SkipOffset;
            internal long ProxOffset;

            internal int PosPendingCount;

            internal bool Skipped;
            internal Lucene40SkipListReader Skipper;
            internal long LazyProxPointer;

            public SegmentDocsAndPositionsEnum(Lucene40PostingsReader outerInstance, IndexInput freqIn, IndexInput proxIn)
            {
                this.OuterInstance = outerInstance;
                StartFreqIn = freqIn;
                this.FreqIn = (IndexInput)freqIn.Clone();
                this.ProxIn = (IndexInput)proxIn.Clone();
            }

            public SegmentDocsAndPositionsEnum Reset(FieldInfo fieldInfo, StandardTermState termState, Bits liveDocs)
            {
                Debug.Assert(fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
                Debug.Assert(!fieldInfo.HasPayloads);

                this.LiveDocs = liveDocs;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                FreqIn.Seek(termState.FreqOffset);
                LazyProxPointer = termState.ProxOffset;

                Limit = termState.DocFreq;
                Debug.Assert(Limit > 0);

                Ord = 0;
                Doc = -1;
                Accum = 0;
                Position = 0;

                Skipped = false;
                PosPendingCount = 0;

                FreqOffset = termState.FreqOffset;
                ProxOffset = termState.ProxOffset;
                SkipOffset = termState.SkipOffset;
                // if (DEBUG) System.out.println("StandardR.D&PE reset seg=" + segment + " limit=" + limit + " freqFP=" + freqOffset + " proxFP=" + proxOffset);

                return this;
            }

            public override int NextDoc()
            {
                // if (DEBUG) System.out.println("SPR.nextDoc seg=" + segment + " freqIn.fp=" + freqIn.getFilePointer());
                while (true)
                {
                    if (Ord == Limit)
                    {
                        // if (DEBUG) System.out.println("  return END");
                        return Doc = NO_MORE_DOCS;
                    }

                    Ord++;

                    // Decode next doc/freq pair
                    int code = FreqIn.ReadVInt();

                    Accum += (int)((uint)code >> 1); // shift off low bit
                    if ((code & 1) != 0) // if low bit is set
                    {
                        Freq_Renamed = 1; // freq is one
                    }
                    else
                    {
                        Freq_Renamed = FreqIn.ReadVInt(); // else read freq
                    }
                    PosPendingCount += Freq_Renamed;

                    if (LiveDocs == null || LiveDocs.Get(Accum))
                    {
                        break;
                    }
                }

                Position = 0;

                // if (DEBUG) System.out.println("  return doc=" + doc);
                return (Doc = Accum);
            }

            public override int DocID
            {
                get { return Doc; }
            }

            public override int Freq
            {
                get { return Freq_Renamed; }
            }

            public override int Advance(int target)
            {
                //System.out.println("StandardR.D&PE advance target=" + target);

                if ((target - OuterInstance.SkipInterval) >= Doc && Limit >= OuterInstance.SkipMinimum)
                {
                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close

                    if (Skipper == null)
                    {
                        // this is the first time this enum has ever been used for skipping -- do lazy init
                        Skipper = new Lucene40SkipListReader((IndexInput)FreqIn.Clone(), OuterInstance.MaxSkipLevels, OuterInstance.SkipInterval);
                    }

                    if (!Skipped)
                    {
                        // this is the first time this posting has
                        // skipped, since reset() was called, so now we
                        // load the skip data for this posting

                        Skipper.Init(FreqOffset + SkipOffset, FreqOffset, ProxOffset, Limit, false, false);

                        Skipped = true;
                    }

                    int newOrd = Skipper.SkipTo(target);

                    if (newOrd > Ord)
                    {
                        // Skipper moved
                        Ord = newOrd;
                        Doc = Accum = Skipper.Doc;
                        FreqIn.Seek(Skipper.FreqPointer);
                        LazyProxPointer = Skipper.ProxPointer;
                        PosPendingCount = 0;
                        Position = 0;
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    NextDoc();
                } while (target > Doc);

                return Doc;
            }

            public override int NextPosition()
            {
                if (LazyProxPointer != -1)
                {
                    ProxIn.Seek(LazyProxPointer);
                    LazyProxPointer = -1;
                }

                // scan over any docs that were iterated without their positions
                if (PosPendingCount > Freq_Renamed)
                {
                    Position = 0;
                    while (PosPendingCount != Freq_Renamed)
                    {
                        if ((ProxIn.ReadByte() & 0x80) == 0)
                        {
                            PosPendingCount--;
                        }
                    }
                }

                Position += ProxIn.ReadVInt();

                PosPendingCount--;

                Debug.Assert(PosPendingCount >= 0, "nextPosition() was called too many times (more than freq() times) posPendingCount=" + PosPendingCount);

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

            /// <summary>
            /// Returns the payload at this position, or null if no
            ///  payload was indexed.
            /// </summary>
            public override BytesRef Payload
            {
                get
                {
                    return null;
                }
            }

            public override long Cost()
            {
                return Limit;
            }
        }

        // Decodes docs & positions & (payloads and/or offsets)
        private class SegmentFullPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene40PostingsReader OuterInstance;

            internal readonly IndexInput StartFreqIn;
            private readonly IndexInput FreqIn;
            private readonly IndexInput ProxIn;

            internal int Limit; // number of docs in this posting
            internal int Ord; // how many docs we've read
            internal int Doc = -1; // doc we last read
            internal int Accum; // accumulator for doc deltas
            internal int Freq_Renamed; // freq we last read
            internal int Position;

            internal Bits LiveDocs;

            internal long FreqOffset;
            internal long SkipOffset;
            internal long ProxOffset;

            internal int PosPendingCount;
            internal int PayloadLength;
            internal bool PayloadPending;

            internal bool Skipped;
            internal Lucene40SkipListReader Skipper;
            internal BytesRef Payload_Renamed;
            internal long LazyProxPointer;

            internal bool StorePayloads;
            internal bool StoreOffsets;

            internal int OffsetLength;
            internal int StartOffset_Renamed;

            public SegmentFullPositionsEnum(Lucene40PostingsReader outerInstance, IndexInput freqIn, IndexInput proxIn)
            {
                this.OuterInstance = outerInstance;
                StartFreqIn = freqIn;
                this.FreqIn = (IndexInput)freqIn.Clone();
                this.ProxIn = (IndexInput)proxIn.Clone();
            }

            public virtual SegmentFullPositionsEnum Reset(FieldInfo fieldInfo, StandardTermState termState, Bits liveDocs)
            {
                StoreOffsets = fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                StorePayloads = fieldInfo.HasPayloads;
                Debug.Assert(fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
                Debug.Assert(StorePayloads || StoreOffsets);
                if (Payload_Renamed == null)
                {
                    Payload_Renamed = new BytesRef();
                    Payload_Renamed.Bytes = new byte[1];
                }

                this.LiveDocs = liveDocs;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                FreqIn.Seek(termState.FreqOffset);
                LazyProxPointer = termState.ProxOffset;

                Limit = termState.DocFreq;
                Ord = 0;
                Doc = -1;
                Accum = 0;
                Position = 0;
                StartOffset_Renamed = 0;

                Skipped = false;
                PosPendingCount = 0;
                PayloadPending = false;

                FreqOffset = termState.FreqOffset;
                ProxOffset = termState.ProxOffset;
                SkipOffset = termState.SkipOffset;
                //System.out.println("StandardR.D&PE reset seg=" + segment + " limit=" + limit + " freqFP=" + freqOffset + " proxFP=" + proxOffset + " this=" + this);

                return this;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    if (Ord == Limit)
                    {
                        //System.out.println("StandardR.D&PE seg=" + segment + " nextDoc return doc=END");
                        return Doc = NO_MORE_DOCS;
                    }

                    Ord++;

                    // Decode next doc/freq pair
                    int code = FreqIn.ReadVInt();

                    Accum += (int)((uint)code >> 1); // shift off low bit
                    if ((code & 1) != 0) // if low bit is set
                    {
                        Freq_Renamed = 1; // freq is one
                    }
                    else
                    {
                        Freq_Renamed = FreqIn.ReadVInt(); // else read freq
                    }
                    PosPendingCount += Freq_Renamed;

                    if (LiveDocs == null || LiveDocs.Get(Accum))
                    {
                        break;
                    }
                }

                Position = 0;
                StartOffset_Renamed = 0;

                //System.out.println("StandardR.D&PE nextDoc seg=" + segment + " return doc=" + doc);
                return (Doc = Accum);
            }

            public override int DocID
            {
                get { return Doc; }
            }

            public override int Freq
            {
                get { return Freq_Renamed; }
            }

            public override int Advance(int target)
            {
                //System.out.println("StandardR.D&PE advance seg=" + segment + " target=" + target + " this=" + this);

                if ((target - OuterInstance.SkipInterval) >= Doc && Limit >= OuterInstance.SkipMinimum)
                {
                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close

                    if (Skipper == null)
                    {
                        // this is the first time this enum has ever been used for skipping -- do lazy init
                        Skipper = new Lucene40SkipListReader((IndexInput)FreqIn.Clone(), OuterInstance.MaxSkipLevels, OuterInstance.SkipInterval);
                    }

                    if (!Skipped)
                    {
                        // this is the first time this posting has
                        // skipped, since reset() was called, so now we
                        // load the skip data for this posting
                        //System.out.println("  init skipper freqOffset=" + freqOffset + " skipOffset=" + skipOffset + " vs len=" + freqIn.length());
                        Skipper.Init(FreqOffset + SkipOffset, FreqOffset, ProxOffset, Limit, StorePayloads, StoreOffsets);

                        Skipped = true;
                    }

                    int newOrd = Skipper.SkipTo(target);

                    if (newOrd > Ord)
                    {
                        // Skipper moved
                        Ord = newOrd;
                        Doc = Accum = Skipper.Doc;
                        FreqIn.Seek(Skipper.FreqPointer);
                        LazyProxPointer = Skipper.ProxPointer;
                        PosPendingCount = 0;
                        Position = 0;
                        StartOffset_Renamed = 0;
                        PayloadPending = false;
                        PayloadLength = Skipper.PayloadLength;
                        OffsetLength = Skipper.OffsetLength;
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    NextDoc();
                } while (target > Doc);

                return Doc;
            }

            public override int NextPosition()
            {
                if (LazyProxPointer != -1)
                {
                    ProxIn.Seek(LazyProxPointer);
                    LazyProxPointer = -1;
                }

                if (PayloadPending && PayloadLength > 0)
                {
                    // payload of last position was never retrieved -- skip it
                    ProxIn.Seek(ProxIn.FilePointer + PayloadLength);
                    PayloadPending = false;
                }

                // scan over any docs that were iterated without their positions
                while (PosPendingCount > Freq_Renamed)
                {
                    int code = ProxIn.ReadVInt();

                    if (StorePayloads)
                    {
                        if ((code & 1) != 0)
                        {
                            // new payload length
                            PayloadLength = ProxIn.ReadVInt();
                            Debug.Assert(PayloadLength >= 0);
                        }
                        Debug.Assert(PayloadLength != -1);
                    }

                    if (StoreOffsets)
                    {
                        if ((ProxIn.ReadVInt() & 1) != 0)
                        {
                            // new offset length
                            OffsetLength = ProxIn.ReadVInt();
                        }
                    }

                    if (StorePayloads)
                    {
                        ProxIn.Seek(ProxIn.FilePointer + PayloadLength);
                    }

                    PosPendingCount--;
                    Position = 0;
                    StartOffset_Renamed = 0;
                    PayloadPending = false;
                    //System.out.println("StandardR.D&PE skipPos");
                }

                // read next position
                if (PayloadPending && PayloadLength > 0)
                {
                    // payload wasn't retrieved for last position
                    ProxIn.Seek(ProxIn.FilePointer + PayloadLength);
                }

                int code_ = ProxIn.ReadVInt();
                if (StorePayloads)
                {
                    if ((code_ & 1) != 0)
                    {
                        // new payload length
                        PayloadLength = ProxIn.ReadVInt();
                        Debug.Assert(PayloadLength >= 0);
                    }
                    Debug.Assert(PayloadLength != -1);

                    PayloadPending = true;
                    code_ = (int)((uint)code_ >> 1);
                }
                Position += code_;

                if (StoreOffsets)
                {
                    int offsetCode = ProxIn.ReadVInt();
                    if ((offsetCode & 1) != 0)
                    {
                        // new offset length
                        OffsetLength = ProxIn.ReadVInt();
                    }
                    StartOffset_Renamed += (int)((uint)offsetCode >> 1);
                }

                PosPendingCount--;

                Debug.Assert(PosPendingCount >= 0, "nextPosition() was called too many times (more than freq() times) posPendingCount=" + PosPendingCount);

                //System.out.println("StandardR.D&PE nextPos   return pos=" + position);
                return Position;
            }

            public override int StartOffset
            {
                get { return StoreOffsets ? StartOffset_Renamed : -1; }
            }

            public override int EndOffset
            {
                get { return StoreOffsets ? StartOffset_Renamed + OffsetLength : -1; }
            }

            /// <summary>
            /// Returns the payload at this position, or null if no
            ///  payload was indexed.
            /// </summary>
            public override BytesRef Payload
            {
                get
                {
                    if (StorePayloads)
                    {
                        if (PayloadLength <= 0)
                        {
                            return null;
                        }
                        Debug.Assert(LazyProxPointer == -1);
                        Debug.Assert(PosPendingCount < Freq_Renamed);

                        if (PayloadPending)
                        {
                            if (PayloadLength > Payload_Renamed.Bytes.Length)
                            {
                                Payload_Renamed.Grow(PayloadLength);
                            }

                            ProxIn.ReadBytes(Payload_Renamed.Bytes, 0, PayloadLength);
                            Payload_Renamed.Length = PayloadLength;
                            PayloadPending = false;
                        }

                        return Payload_Renamed;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public override long Cost()
            {
                return Limit;
            }
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
        }
    }
}