using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
    [Obsolete]
    public class Lucene40PostingsReader : PostingsReaderBase
    {
        internal const string TERMS_CODEC = "Lucene40PostingsWriterTerms";
        internal const string FRQ_CODEC = "Lucene40PostingsWriterFrq";
        internal const string PRX_CODEC = "Lucene40PostingsWriterPrx";

        //private static boolean DEBUG = BlockTreeTermsWriter.DEBUG;

        // Increment version to change it:
        internal const int VERSION_START = 0;
        internal const int VERSION_LONG_SKIP = 1;
        internal const int VERSION_CURRENT = VERSION_LONG_SKIP;

        private readonly IndexInput freqIn;
        private readonly IndexInput proxIn;
        // public static boolean DEBUG = BlockTreeTermsWriter.DEBUG;

        int skipInterval;
        int maxSkipLevels;
        int skipMinimum;

        public Lucene40PostingsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo segmentInfo, IOContext ioContext, String segmentSuffix)
        {
            bool success = false;
            IndexInput freqIn = null;
            IndexInput proxIn = null;
            try
            {
                freqIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.name, segmentSuffix, Lucene40PostingsFormat.FREQ_EXTENSION),
                                     ioContext);
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
                    proxIn = dir.OpenInput(IndexFileNames.SegmentFileName(segmentInfo.name, segmentSuffix, Lucene40PostingsFormat.PROX_EXTENSION),
                                         ioContext);
                    CodecUtil.CheckHeader(proxIn, PRX_CODEC, VERSION_START, VERSION_CURRENT);
                }
                else
                {
                    proxIn = null;
                }
                this.freqIn = freqIn;
                this.proxIn = proxIn;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)freqIn, proxIn);
                }
            }
        }

        public override void Init(IndexInput termsIn)
        {
            // Make sure we are talking to the matching past writer
            CodecUtil.CheckHeader(termsIn, TERMS_CODEC, VERSION_START, VERSION_CURRENT);

            skipInterval = termsIn.ReadInt();
            maxSkipLevels = termsIn.ReadInt();
            skipMinimum = termsIn.ReadInt();
        }

        private sealed class StandardTermState : BlockTermState
        {
            internal long freqOffset;
            internal long proxOffset;
            internal long skipOffset;

            // Only used by the "primary" TermState -- clones don't
            // copy this (basically they are "transient"):
            internal ByteArrayDataInput bytesReader;  // TODO: should this NOT be in the TermState...?
            internal byte[] bytes;

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
                freqOffset = other.freqOffset;
                proxOffset = other.proxOffset;
                skipOffset = other.skipOffset;

                // Do not copy bytes, bytesReader (else TermState is
                // very heavy, ie drags around the entire block's
                // byte[]).  On seek back, if next() is in fact used
                // (rare!), they will be re-read from disk.
            }

            public override string ToString()
            {
                return base.ToString() + " freqFP=" + freqOffset + " proxFP=" + proxOffset + " skipOffset=" + skipOffset;
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
                    if (freqIn != null)
                    {
                        freqIn.Dispose();
                    }
                }
                finally
                {
                    if (proxIn != null)
                    {
                        proxIn.Dispose();
                    }
                }
            }
        }

        public override void ReadTermsBlock(IndexInput termsIn, FieldInfo fieldInfo, BlockTermState _termState)
        {
            StandardTermState termState = (StandardTermState)_termState;

            int len = termsIn.ReadVInt();

            // if (DEBUG) System.out.println("  SPR.readTermsBlock bytes=" + len + " ts=" + _termState);
            if (termState.bytes == null)
            {
                termState.bytes = new byte[ArrayUtil.Oversize(len, 1)];
                termState.bytesReader = new ByteArrayDataInput();
            }
            else if (termState.bytes.Length < len)
            {
                termState.bytes = new byte[ArrayUtil.Oversize(len, 1)];
            }

            termsIn.ReadBytes(termState.bytes, 0, len);
            termState.bytesReader.Reset(termState.bytes, 0, len);
        }

        public override void NextTerm(FieldInfo fieldInfo, BlockTermState _termState)
        {
            StandardTermState termState = (StandardTermState)_termState;
            // if (DEBUG) System.out.println("SPR: nextTerm seg=" + segment + " tbOrd=" + termState.termBlockOrd + " bytesReader.fp=" + termState.bytesReader.getPosition());
            bool isFirstTerm = termState.termBlockOrd == 0;

            if (isFirstTerm)
            {
                termState.freqOffset = termState.bytesReader.ReadVLong();
            }
            else
            {
                termState.freqOffset += termState.bytesReader.ReadVLong();
            }
            /*
            if (DEBUG) {
              System.out.println("  dF=" + termState.docFreq);
              System.out.println("  freqFP=" + termState.freqOffset);
            }
            */
            //assert termState.freqOffset < freqIn.length();

            if (termState.docFreq >= skipMinimum)
            {
                termState.skipOffset = termState.bytesReader.ReadVLong();
                // if (DEBUG) System.out.println("  skipOffset=" + termState.skipOffset + " vs freqIn.length=" + freqIn.length());
                //assert termState.freqOffset + termState.skipOffset < freqIn.length();
            }
            else
            {
                // undefined
            }

            if (fieldInfo.IndexOptionsValue >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
            {
                if (isFirstTerm)
                {
                    termState.proxOffset = termState.bytesReader.ReadVLong();
                }
                else
                {
                    termState.proxOffset += termState.bytesReader.ReadVLong();
                }
                // if (DEBUG) System.out.println("  proxFP=" + termState.proxOffset);
            }
        }

        public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState termState, IBits liveDocs, DocsEnum reuse, int flags)
        {
            if (CanReuse(reuse, liveDocs))
            {
                // if (DEBUG) System.out.println("SPR.docs ts=" + termState);
                return ((SegmentDocsEnumBase)reuse).reset(fieldInfo, (StandardTermState)termState);
            }
            return NewDocsEnum(liveDocs, fieldInfo, (StandardTermState)termState);
        }

        private bool CanReuse(DocsEnum reuse, IBits liveDocs)
        {
            if (reuse != null && (reuse is SegmentDocsEnumBase))
            {
                SegmentDocsEnumBase docsEnum = (SegmentDocsEnumBase)reuse;
                // If you are using ParellelReader, and pass in a
                // reused DocsEnum, it could have come from another
                // reader also using standard codec
                if (docsEnum.startFreqIn == freqIn)
                {
                    // we only reuse if the the actual the incoming enum has the same liveDocs as the given liveDocs
                    return liveDocs == docsEnum.liveDocs;
                }
            }
            return false;
        }

        private DocsEnum NewDocsEnum(IBits liveDocs, FieldInfo fieldInfo, StandardTermState termState)
        {
            if (liveDocs == null)
            {
                return new AllDocsSegmentDocsEnum(this, freqIn).Reset(fieldInfo, termState);
            }
            else
            {
                return new LiveDocsSegmentDocsEnum(this, freqIn, liveDocs).Reset(fieldInfo, termState);
            }
        }

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState termState, IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            bool hasOffsets = fieldInfo.IndexOptionsValue >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;

            // TODO: can we optimize if FLAG_PAYLOADS / FLAG_OFFSETS
            // isn't passed?

            // TODO: refactor
            if (fieldInfo.HasPayloads || hasOffsets)
            {
                SegmentFullPositionsEnum docsEnum;
                if (reuse == null || !(reuse is SegmentFullPositionsEnum))
                {
                    docsEnum = new SegmentFullPositionsEnum(this, freqIn, proxIn);
                }
                else
                {
                    docsEnum = (SegmentFullPositionsEnum)reuse;
                    if (docsEnum.startFreqIn != freqIn)
                    {
                        // If you are using ParellelReader, and pass in a
                        // reused DocsEnum, it could have come from another
                        // reader also using standard codec
                        docsEnum = new SegmentFullPositionsEnum(this, freqIn, proxIn);
                    }
                }
                return docsEnum.Reset(fieldInfo, (StandardTermState)termState, liveDocs);
            }
            else
            {
                SegmentDocsAndPositionsEnum docsEnum;
                if (reuse == null || !(reuse is SegmentDocsAndPositionsEnum))
                {
                    docsEnum = new SegmentDocsAndPositionsEnum(this, freqIn, proxIn);
                }
                else
                {
                    docsEnum = (SegmentDocsAndPositionsEnum)reuse;
                    if (docsEnum.startFreqIn != freqIn)
                    {
                        // If you are using ParellelReader, and pass in a
                        // reused DocsEnum, it could have come from another
                        // reader also using standard codec
                        docsEnum = new SegmentDocsAndPositionsEnum(this, freqIn, proxIn);
                    }
                }
                return docsEnum.Reset(fieldInfo, (StandardTermState)termState, liveDocs);
            }
        }

        internal const int BUFFERSIZE = 64;

        private abstract class SegmentDocsEnumBase : DocsEnum
        {
            protected readonly Lucene40PostingsReader parent;

            protected readonly int[] docs = new int[BUFFERSIZE];
            protected readonly int[] freqs = new int[BUFFERSIZE];

            internal readonly IndexInput freqIn; // reuse
            internal readonly IndexInput startFreqIn; // reuse
            internal Lucene40SkipListReader skipper; // reuse - lazy loaded

            protected bool indexOmitsTF;                               // does current field omit term freq?
            protected bool storePayloads;                        // does current field store payloads?
            protected bool storeOffsets;                         // does current field store offsets?

            protected int limit;                                    // number of docs in this posting
            protected int ord;                                      // how many docs we've read
            protected int doc;                                 // doc we last read
            protected int accum;                                    // accumulator for doc deltas
            protected int freq;                                     // freq we last read
            protected int maxBufferedDocId;

            protected int start;
            protected int count;


            protected long freqOffset;
            protected long skipOffset;

            protected bool skipped;
            protected internal readonly IBits liveDocs;

            internal SegmentDocsEnumBase(Lucene40PostingsReader parent, IndexInput startFreqIn, IBits liveDocs)
            {
                this.parent = parent;
                this.startFreqIn = startFreqIn;
                this.freqIn = (IndexInput)startFreqIn.Clone();
                this.liveDocs = liveDocs;
            }

            internal virtual DocsEnum Reset(FieldInfo fieldInfo, StandardTermState termState)
            {
                indexOmitsTF = fieldInfo.IndexOptionsValue == FieldInfo.IndexOptions.DOCS_ONLY;
                storePayloads = fieldInfo.HasPayloads;
                storeOffsets = fieldInfo.IndexOptionsValue >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                freqOffset = termState.freqOffset;
                skipOffset = termState.skipOffset;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                freqIn.Seek(termState.freqOffset);
                limit = termState.docFreq;
                //assert limit > 0;
                ord = 0;
                doc = -1;
                accum = 0;
                // if (DEBUG) System.out.println("  sde limit=" + limit + " freqFP=" + freqOffset);
                skipped = false;

                start = -1;
                count = 0;
                freq = 1;
                if (indexOmitsTF)
                {
                    Arrays.Fill(freqs, 1);
                }
                maxBufferedDocId = -1;
                return this;
            }

            public override int Freq
            {
                get { return freq; }
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int Advance(int target)
            {
                // last doc in our buffer is >= target, binary search + next()
                if (++start < count && maxBufferedDocId >= target)
                {
                    if ((count - start) > 32)
                    {
                        // 32 seemed to be a sweetspot here so use binsearch if the pending results are a lot
                        start = BinarySearch(count - 1, start, target, docs);
                        return NextDoc();
                    }
                    else
                    {
                        return LinearScan(target);
                    }
                }

                start = count; // buffer is consumed

                return doc = SkipTo(target);
            }

            private int BinarySearch(int hi, int low, int target, int[] docs)
            {
                while (low <= hi)
                {
                    int mid = Number.URShift((hi + low), 1);
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
                if ((code & 1) != 0)
                {
                    // if low bit is set
                    return 1; // freq is one
                }
                else
                {
                    return freqIn.ReadVInt(); // else read freq
                }
            }

            protected abstract int LinearScan(int scanTo);

            protected abstract int ScanTo(int target);

            protected int Refill()
            {
                int doc = NextUnreadDoc();
                count = 0;
                start = -1;
                if (doc == NO_MORE_DOCS)
                {
                    return NO_MORE_DOCS;
                }
                int numDocs = Math.Min(docs.Length, limit - ord);
                ord += numDocs;
                if (indexOmitsTF)
                {
                    count = FillDocs(numDocs);
                }
                else
                {
                    count = FillDocsAndFreqs(numDocs);
                }
                maxBufferedDocId = count > 0 ? docs[count - 1] : NO_MORE_DOCS;
                return doc;
            }

            protected abstract int NextUnreadDoc();

            private int FillDocs(int size)
            {
                IndexInput freqIn = this.freqIn;
                int[] docs = this.docs;
                int docAc = accum;
                for (int i = 0; i < size; i++)
                {
                    docAc += freqIn.ReadVInt();
                    docs[i] = docAc;
                }
                accum = docAc;
                return size;
            }

            private int FillDocsAndFreqs(int size)
            {
                IndexInput freqIn = this.freqIn;
                int[] docs = this.docs;
                int[] freqs = this.freqs;
                int docAc = accum;
                for (int i = 0; i < size; i++)
                {
                    int code = freqIn.ReadVInt();
                    docAc += Number.URShift(code, 1); // shift off low bit
                    freqs[i] = ReadFreq(freqIn, code);
                    docs[i] = docAc;
                }
                accum = docAc;
                return size;
            }

            private int SkipTo(int target)
            {
                if ((target - parent.skipInterval) >= accum && limit >= parent.skipMinimum)
                {

                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close.

                    if (skipper == null)
                    {
                        // This is the first time this enum has ever been used for skipping -- do lazy init
                        skipper = new Lucene40SkipListReader((IndexInput)freqIn.Clone(), parent.maxSkipLevels, parent.skipInterval);
                    }

                    if (!skipped)
                    {

                        // This is the first time this posting has
                        // skipped since reset() was called, so now we
                        // load the skip data for this posting

                        skipper.Init(freqOffset + skipOffset,
                                     freqOffset, 0,
                                     limit, storePayloads, storeOffsets);

                        skipped = true;
                    }

                    int newOrd = skipper.SkipTo(target);

                    if (newOrd > ord)
                    {
                        // Skipper moved

                        ord = newOrd;
                        accum = skipper.Doc;
                        freqIn.Seek(skipper.FreqPointer);
                    }
                }
                return ScanTo(target);
            }

            public override long Cost
            {
                get { return limit; }
            }
        }

        private sealed class AllDocsSegmentDocsEnum : SegmentDocsEnumBase
        {
            internal AllDocsSegmentDocsEnum(Lucene40PostingsReader parent, IndexInput startFreqIn)
                : base(parent, startFreqIn, null)
            {
                //assert liveDocs == null;
            }

            public override int NextDoc()
            {
                if (++start < count)
                {
                    freq = freqs[start];
                    return doc = docs[start];
                }
                return doc = Refill();
            }

            protected override int LinearScan(int scanTo)
            {
                int[] docs = this.docs;
                int upTo = count;
                for (int i = start; i < upTo; i++)
                {
                    int d = docs[i];
                    if (scanTo <= d)
                    {
                        start = i;
                        freq = freqs[i];
                        return doc = docs[i];
                    }
                }
                return doc = Refill();
            }

            protected override int ScanTo(int target)
            {
                int docAcc = accum;
                int frq = 1;
                IndexInput freqIn = this.freqIn;
                bool omitTF = indexOmitsTF;
                int loopLimit = limit;
                for (int i = ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += Number.URShift(code, 1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (docAcc >= target)
                    {
                        freq = frq;
                        ord = i + 1;
                        return accum = docAcc;
                    }
                }
                ord = limit;
                freq = frq;
                accum = docAcc;
                return NO_MORE_DOCS;
            }

            protected override int NextUnreadDoc()
            {
                if (ord++ < limit)
                {
                    int code = freqIn.ReadVInt();
                    if (indexOmitsTF)
                    {
                        accum += code;
                    }
                    else
                    {
                        accum += Number.URShift(code, 1); // shift off low bit
                        freq = ReadFreq(freqIn, code);
                    }
                    return accum;
                }
                else
                {
                    return NO_MORE_DOCS;
                }
            }
        }

        private sealed class LiveDocsSegmentDocsEnum : SegmentDocsEnumBase
        {
            internal LiveDocsSegmentDocsEnum(Lucene40PostingsReader parent, IndexInput startFreqIn, IBits liveDocs)
                : base(parent, startFreqIn, liveDocs)
            {
                //assert liveDocs != null;
            }

            public override int NextDoc()
            {
                IBits liveDocs = this.liveDocs;
                for (int i = start + 1; i < count; i++)
                {
                    int d = docs[i];
                    if (liveDocs[d])
                    {
                        start = i;
                        freq = freqs[i];
                        return doc = d;
                    }
                }
                start = count;
                return doc = Refill();
            }

            protected override int LinearScan(int scanTo)
            {
                int[] docs = this.docs;
                int upTo = count;
                IBits liveDocs = this.liveDocs;
                for (int i = start; i < upTo; i++)
                {
                    int d = docs[i];
                    if (scanTo <= d && liveDocs[d])
                    {
                        start = i;
                        freq = freqs[i];
                        return doc = docs[i];
                    }
                }
                return doc = Refill();
            }

            protected override int ScanTo(int target)
            {
                int docAcc = accum;
                int frq = 1;
                IndexInput freqIn = this.freqIn;
                bool omitTF = indexOmitsTF;
                int loopLimit = limit;
                IBits liveDocs = this.liveDocs;
                for (int i = ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += Number.URShift(code, 1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (docAcc >= target && liveDocs[docAcc])
                    {
                        freq = frq;
                        ord = i + 1;
                        return accum = docAcc;
                    }
                }
                ord = limit;
                freq = frq;
                accum = docAcc;
                return NO_MORE_DOCS;
            }

            protected override int NextUnreadDoc()
            {
                int docAcc = accum;
                int frq = 1;
                IndexInput freqIn = this.freqIn;
                bool omitTF = indexOmitsTF;
                int loopLimit = limit;
                IBits liveDocs = this.liveDocs;
                for (int i = ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += Number.URShift(code, 1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (liveDocs[docAcc])
                    {
                        freq = frq;
                        ord = i + 1;
                        return accum = docAcc;
                    }
                }
                ord = limit;
                freq = frq;
                accum = docAcc;
                return NO_MORE_DOCS;
            }
        }

        private sealed class SegmentDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene40PostingsReader parent;

            internal readonly IndexInput startFreqIn;
            private readonly IndexInput freqIn;
            private readonly IndexInput proxIn;
            internal int limit;                                    // number of docs in this posting
            internal int ord;                                      // how many docs we've read
            internal int doc = -1;                                 // doc we last read
            internal int accum;                                    // accumulator for doc deltas
            internal int freq;                                     // freq we last read
            internal int position;

            internal IBits liveDocs;

            internal long freqOffset;
            internal long skipOffset;
            internal long proxOffset;

            internal int posPendingCount;

            internal bool skipped;
            internal Lucene40SkipListReader skipper;
            private long lazyProxPointer;

            public SegmentDocsAndPositionsEnum(Lucene40PostingsReader parent, IndexInput freqIn, IndexInput proxIn)
            {
                this.parent = parent;
                startFreqIn = freqIn;
                this.freqIn = (IndexInput)freqIn.Clone();
                this.proxIn = (IndexInput)proxIn.Clone();
            }

            public SegmentDocsAndPositionsEnum Reset(FieldInfo fieldInfo, StandardTermState termState, IBits liveDocs)
            {
                //assert fieldInfo.getIndexOptions() == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                //assert !fieldInfo.hasPayloads();

                this.liveDocs = liveDocs;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                freqIn.Seek(termState.freqOffset);
                lazyProxPointer = termState.proxOffset;

                limit = termState.docFreq;
                //assert limit > 0;

                ord = 0;
                doc = -1;
                accum = 0;
                position = 0;

                skipped = false;
                posPendingCount = 0;

                freqOffset = termState.freqOffset;
                proxOffset = termState.proxOffset;
                skipOffset = termState.skipOffset;
                // if (DEBUG) System.out.println("StandardR.D&PE reset seg=" + segment + " limit=" + limit + " freqFP=" + freqOffset + " proxFP=" + proxOffset);

                return this;
            }

            public override int NextDoc()
            {
                // if (DEBUG) System.out.println("SPR.nextDoc seg=" + segment + " freqIn.fp=" + freqIn.getFilePointer());
                while (true)
                {
                    if (ord == limit)
                    {
                        // if (DEBUG) System.out.println("  return END");
                        return doc = NO_MORE_DOCS;
                    }

                    ord++;

                    // Decode next doc/freq pair
                    int code = freqIn.ReadVInt();

                    accum += Number.URShift(code, 1);              // shift off low bit
                    if ((code & 1) != 0)
                    {          // if low bit is set
                        freq = 1;                     // freq is one
                    }
                    else
                    {
                        freq = freqIn.ReadVInt();     // else read freq
                    }
                    posPendingCount += freq;

                    if (liveDocs == null || liveDocs[accum])
                    {
                        break;
                    }
                }

                position = 0;

                // if (DEBUG) System.out.println("  return doc=" + doc);
                return (doc = accum);
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int Freq
            {
                get { return freq; }
            }

            public override int Advance(int target)
            {
                //System.out.println("StandardR.D&PE advance target=" + target);

                if ((target - parent.skipInterval) >= doc && limit >= parent.skipMinimum)
                {

                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close

                    if (skipper == null)
                    {
                        // This is the first time this enum has ever been used for skipping -- do lazy init
                        skipper = new Lucene40SkipListReader((IndexInput)freqIn.Clone(), parent.maxSkipLevels, parent.skipInterval);
                    }

                    if (!skipped)
                    {

                        // This is the first time this posting has
                        // skipped, since reset() was called, so now we
                        // load the skip data for this posting

                        skipper.Init(freqOffset + skipOffset,
                                     freqOffset, proxOffset,
                                     limit, false, false);

                        skipped = true;
                    }

                    int newOrd = skipper.SkipTo(target);

                    if (newOrd > ord)
                    {
                        // Skipper moved
                        ord = newOrd;
                        doc = accum = skipper.Doc;
                        freqIn.Seek(skipper.FreqPointer);
                        lazyProxPointer = skipper.ProxPointer;
                        posPendingCount = 0;
                        position = 0;
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    NextDoc();
                } while (target > doc);

                return doc;
            }

            public override int NextPosition()
            {
                if (lazyProxPointer != -1)
                {
                    proxIn.Seek(lazyProxPointer);
                    lazyProxPointer = -1;
                }

                // scan over any docs that were iterated without their positions
                if (posPendingCount > freq)
                {
                    position = 0;
                    while (posPendingCount != freq)
                    {
                        if ((proxIn.ReadByte() & 0x80) == 0)
                        {
                            posPendingCount--;
                        }
                    }
                }

                position += proxIn.ReadVInt();

                posPendingCount--;

                //assert posPendingCount >= 0: "nextPosition() was called too many times (more than freq() times) posPendingCount=" + posPendingCount;

                return position;
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
                get { return null; }
            }

            public override long Cost
            {
                get { return limit; }
            }
        }

        private class SegmentFullPositionsEnum : DocsAndPositionsEnum
        {
            protected readonly Lucene40PostingsReader parent;

            internal readonly IndexInput startFreqIn;
            private readonly IndexInput freqIn;
            private readonly IndexInput proxIn;

            internal int limit;                                    // number of docs in this posting
            internal int ord;                                      // how many docs we've read
            internal int doc = -1;                                 // doc we last read
            internal int accum;                                    // accumulator for doc deltas
            internal int freq;                                     // freq we last read
            internal int position;

            internal IBits liveDocs;

            internal long freqOffset;
            internal long skipOffset;
            internal long proxOffset;

            internal int posPendingCount;
            internal int payloadLength;
            internal bool payloadPending;

            internal bool skipped;
            internal Lucene40SkipListReader skipper;
            private BytesRef payload;
            private long lazyProxPointer;

            internal bool storePayloads;
            internal bool storeOffsets;

            internal int offsetLength;
            internal int startOffset;

            public SegmentFullPositionsEnum(Lucene40PostingsReader parent, IndexInput freqIn, IndexInput proxIn)
            {
                this.parent = parent;
                startFreqIn = freqIn;
                this.freqIn = (IndexInput)freqIn.Clone();
                this.proxIn = (IndexInput)proxIn.Clone();
            }

            public SegmentFullPositionsEnum Reset(FieldInfo fieldInfo, StandardTermState termState, IBits liveDocs)
            {
                storeOffsets = fieldInfo.IndexOptionsValue >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                storePayloads = fieldInfo.HasPayloads;
                //assert fieldInfo.getIndexOptions().compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                //assert storePayloads || storeOffsets;
                if (payload == null)
                {
                    payload = new BytesRef();
                    payload.bytes = new sbyte[1];
                }

                this.liveDocs = liveDocs;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                freqIn.Seek(termState.freqOffset);
                lazyProxPointer = termState.proxOffset;

                limit = termState.docFreq;
                ord = 0;
                doc = -1;
                accum = 0;
                position = 0;
                startOffset = 0;

                skipped = false;
                posPendingCount = 0;
                payloadPending = false;

                freqOffset = termState.freqOffset;
                proxOffset = termState.proxOffset;
                skipOffset = termState.skipOffset;
                //System.out.println("StandardR.D&PE reset seg=" + segment + " limit=" + limit + " freqFP=" + freqOffset + " proxFP=" + proxOffset + " this=" + this);

                return this;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    if (ord == limit)
                    {
                        //System.out.println("StandardR.D&PE seg=" + segment + " nextDoc return doc=END");
                        return doc = NO_MORE_DOCS;
                    }

                    ord++;

                    // Decode next doc/freq pair
                    int code = freqIn.ReadVInt();

                    accum += Number.URShift(code, 1); // shift off low bit
                    if ((code & 1) != 0)
                    { // if low bit is set
                        freq = 1; // freq is one
                    }
                    else
                    {
                        freq = freqIn.ReadVInt(); // else read freq
                    }
                    posPendingCount += freq;

                    if (liveDocs == null || liveDocs[accum])
                    {
                        break;
                    }
                }

                position = 0;
                startOffset = 0;

                //System.out.println("StandardR.D&PE nextDoc seg=" + segment + " return doc=" + doc);
                return (doc = accum);
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int Freq
            {
                get { return freq; }
            }

            public override int Advance(int target)
            {
                //System.out.println("StandardR.D&PE advance seg=" + segment + " target=" + target + " this=" + this);

                if ((target - parent.skipInterval) >= doc && limit >= parent.skipMinimum)
                {

                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close

                    if (skipper == null)
                    {
                        // This is the first time this enum has ever been used for skipping -- do lazy init
                        skipper = new Lucene40SkipListReader((IndexInput)freqIn.Clone(), parent.maxSkipLevels, parent.skipInterval);
                    }

                    if (!skipped)
                    {

                        // This is the first time this posting has
                        // skipped, since reset() was called, so now we
                        // load the skip data for this posting
                        //System.out.println("  init skipper freqOffset=" + freqOffset + " skipOffset=" + skipOffset + " vs len=" + freqIn.length());
                        skipper.Init(freqOffset + skipOffset,
                                     freqOffset, proxOffset,
                                     limit, storePayloads, storeOffsets);

                        skipped = true;
                    }

                    int newOrd = skipper.SkipTo(target);

                    if (newOrd > ord)
                    {
                        // Skipper moved
                        ord = newOrd;
                        doc = accum = skipper.Doc;
                        freqIn.Seek(skipper.FreqPointer);
                        lazyProxPointer = skipper.ProxPointer;
                        posPendingCount = 0;
                        position = 0;
                        startOffset = 0;
                        payloadPending = false;
                        payloadLength = skipper.PayloadLength;
                        offsetLength = skipper.OffsetLength;
                    }
                }

                // Now, linear scan for the rest:
                do
                {
                    NextDoc();
                } while (target > doc);

                return doc;
            }

            public override int NextPosition()
            {
                if (lazyProxPointer != -1)
                {
                    proxIn.Seek(lazyProxPointer);
                    lazyProxPointer = -1;
                }

                if (payloadPending && payloadLength > 0)
                {
                    // payload of last position was never retrieved -- skip it
                    proxIn.Seek(proxIn.FilePointer + payloadLength);
                    payloadPending = false;
                }

                // scan over any docs that were iterated without their positions
                while (posPendingCount > freq)
                {
                    int code = proxIn.ReadVInt();

                    if (storePayloads)
                    {
                        if ((code & 1) != 0)
                        {
                            // new payload length
                            payloadLength = proxIn.ReadVInt();
                            //assert payloadLength >= 0;
                        }
                        //assert payloadLength != -1;
                    }

                    if (storeOffsets)
                    {
                        if ((proxIn.ReadVInt() & 1) != 0)
                        {
                            // new offset length
                            offsetLength = proxIn.ReadVInt();
                        }
                    }

                    if (storePayloads)
                    {
                        proxIn.Seek(proxIn.FilePointer + payloadLength);
                    }

                    posPendingCount--;
                    position = 0;
                    startOffset = 0;
                    payloadPending = false;
                    //System.out.println("StandardR.D&PE skipPos");
                }

                // read next position
                if (payloadPending && payloadLength > 0)
                {
                    // payload wasn't retrieved for last position
                    proxIn.Seek(proxIn.FilePointer + payloadLength);
                }

                int code2 = proxIn.ReadVInt();
                if (storePayloads)
                {
                    if ((code2 & 1) != 0)
                    {
                        // new payload length
                        payloadLength = proxIn.ReadVInt();
                        //assert payloadLength >= 0;
                    }
                    //assert payloadLength != -1;

                    payloadPending = true;
                    code2 = Number.URShift(code2, 1);
                }
                position += code2;

                if (storeOffsets)
                {
                    int offsetCode = proxIn.ReadVInt();
                    if ((offsetCode & 1) != 0)
                    {
                        // new offset length
                        offsetLength = proxIn.ReadVInt();
                    }
                    startOffset += Number.URShift(offsetCode, 1);
                }

                posPendingCount--;

                //assert posPendingCount >= 0: "nextPosition() was called too many times (more than freq() times) posPendingCount=" + posPendingCount;

                //System.out.println("StandardR.D&PE nextPos   return pos=" + position);
                return position;
            }

            public override int StartOffset
            {
                get { return storeOffsets ? startOffset : -1; }
            }

            public override int EndOffset
            {
                get { return storeOffsets ? startOffset + offsetLength : -1; }
            }

            public override BytesRef Payload
            {
                get
                {
                    if (storePayloads)
                    {
                        if (payloadLength <= 0)
                        {
                            return null;
                        }
                        //assert lazyProxPointer == -1;
                        //assert posPendingCount < freq;

                        if (payloadPending)
                        {
                            if (payloadLength > payload.bytes.Length)
                            {
                                payload.Grow(payloadLength);
                            }

                            proxIn.ReadBytes(payload.bytes, 0, payloadLength);
                            payload.length = payloadLength;
                            payloadPending = false;
                        }

                        return payload;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public override long Cost
            {
                get { return limit; }
            }
        }
    }
}
