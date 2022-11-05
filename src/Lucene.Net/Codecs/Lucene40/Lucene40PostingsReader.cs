using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using DataInput = Lucene.Net.Store.DataInput;
    using Directory = Lucene.Net.Store.Directory;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IBits = Lucene.Net.Util.IBits;
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
    /// <seealso cref="Lucene40PostingsFormat"/>
    [Obsolete("Only for reading old 4.0 segments")]
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

        internal int skipInterval;
        internal int maxSkipLevels;
        internal int skipMinimum;

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
                this.freqIn = freqIn;
                this.proxIn = proxIn;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(freqIn, proxIn);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Init(IndexInput termsIn)
        {
            // Make sure we are talking to the matching past writer
            CodecUtil.CheckHeader(termsIn, TERMS_CODEC, VERSION_START, VERSION_CURRENT);

            skipInterval = termsIn.ReadInt32();
            maxSkipLevels = termsIn.ReadInt32();
            skipMinimum = termsIn.ReadInt32();
        }

        // Must keep final because we do non-standard clone
        private sealed class StandardTermState : BlockTermState
        {
            internal long freqOffset;
            internal long proxOffset;
            internal long skipOffset;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override object Clone()
            {
                StandardTermState other = new StandardTermState();
                other.CopyFrom(this);
                return other;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void CopyFrom(TermState other)
            {
                base.CopyFrom(other);
                StandardTermState other2 = (StandardTermState)other;
                freqOffset = other2.freqOffset;
                proxOffset = other2.proxOffset;
                skipOffset = other2.skipOffset;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override string ToString()
            {
                return base.ToString() + " freqFP=" + freqOffset + " proxFP=" + proxOffset + " skipOffset=" + skipOffset;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        public override void DecodeTerm(long[] longs, DataInput @in, FieldInfo fieldInfo, BlockTermState termState, bool absolute)
        {
            StandardTermState termState2 = (StandardTermState)termState;
            // if (DEBUG) System.out.println("SPR: nextTerm seg=" + segment + " tbOrd=" + termState2.termBlockOrd + " bytesReader.fp=" + termState.bytesReader.getPosition());
            //bool isFirstTerm = termState2.TermBlockOrd == 0; // LUCENENET: IDE0059: Remove unnecessary value assignment
            if (absolute)
            {
                termState2.freqOffset = 0;
                termState2.proxOffset = 0;
            }

            termState2.freqOffset += @in.ReadVInt64();
            /*
            if (DEBUG) {
              System.out.println("  dF=" + termState2.docFreq);
              System.out.println("  freqFP=" + termState2.freqOffset);
            }
            */
            if (Debugging.AssertsEnabled) Debugging.Assert(termState2.freqOffset < freqIn.Length);

            if (termState2.DocFreq >= skipMinimum)
            {
                termState2.skipOffset = @in.ReadVInt64();
                // if (DEBUG) System.out.println("  skipOffset=" + termState2.skipOffset + " vs freqIn.length=" + freqIn.length());
                if (Debugging.AssertsEnabled) Debugging.Assert(termState2.freqOffset + termState2.skipOffset < freqIn.Length);
            }
            else
            {
                // undefined
            }

            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            if (IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
            {
                termState2.proxOffset += @in.ReadVInt64();
                // if (DEBUG) System.out.println("  proxFP=" + termState2.proxOffset);
            }
        }

        public override DocsEnum Docs(FieldInfo fieldInfo, BlockTermState termState, IBits liveDocs, DocsEnum reuse, DocsFlags flags)
        {
            if (CanReuse(reuse, liveDocs))
            {
                // if (DEBUG) System.out.println("SPR.docs ts=" + termState2);
                return ((SegmentDocsEnumBase)reuse).Reset(fieldInfo, (StandardTermState)termState);
            }
            return NewDocsEnum(liveDocs, fieldInfo, (StandardTermState)termState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanReuse(DocsEnum reuse, IBits liveDocs)
        {
            // If you are using ParellelReader, and pass in a
            // reused DocsEnum, it could have come from another
            // reader also using standard codec
            if (reuse != null && (reuse is SegmentDocsEnumBase docsEnum) && docsEnum.startFreqIn == freqIn)
                // we only reuse if the the actual the incoming enum has the same liveDocs as the given liveDocs
                return liveDocs == docsEnum.m_liveDocs;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DocsEnum NewDocsEnum(IBits liveDocs, FieldInfo fieldInfo, StandardTermState termState)
        {
            if (liveDocs is null)
            {
                return (new AllDocsSegmentDocsEnum(this, freqIn)).Reset(fieldInfo, termState);
            }
            else
            {
                return (new LiveDocsSegmentDocsEnum(this, freqIn, liveDocs)).Reset(fieldInfo, termState);
            }
        }

        public override DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState termState, IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
        {
            // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
            bool hasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

            // TODO: can we optimize if FLAG_PAYLOADS / FLAG_OFFSETS
            // isn't passed?

            // TODO: refactor
            if (fieldInfo.HasPayloads || hasOffsets)
            {
                // If you are using ParellelReader, and pass in a
                // reused DocsEnum, it could have come from another
                // reader also using standard codec
                if (reuse is null || !(reuse is SegmentFullPositionsEnum docsEnum) || docsEnum.startFreqIn != freqIn)
                    docsEnum = new SegmentFullPositionsEnum(this, freqIn, proxIn);

                return docsEnum.Reset(fieldInfo, (StandardTermState)termState, liveDocs);
            }
            else
            {
                // If you are using ParellelReader, and pass in a
                // reused DocsEnum, it could have come from another
                // reader also using standard codec
                if (reuse is null || !(reuse is SegmentDocsAndPositionsEnum docsEnum) || docsEnum.startFreqIn != freqIn)
                    docsEnum = new SegmentDocsAndPositionsEnum(this, freqIn, proxIn);

                return docsEnum.Reset(fieldInfo, (StandardTermState)termState, liveDocs);
            }
        }

        internal const int BUFFERSIZE = 64;

        private abstract class SegmentDocsEnumBase : DocsEnum
        {
            private readonly Lucene40PostingsReader outerInstance;

            protected readonly int[] m_docs = new int[BUFFERSIZE];
            protected readonly int[] m_freqs = new int[BUFFERSIZE];

            internal readonly IndexInput freqIn; // reuse
            internal readonly IndexInput startFreqIn; // reuse
            internal Lucene40SkipListReader skipper; // reuse - lazy loaded

            protected bool m_indexOmitsTF; // does current field omit term freq?
            protected bool m_storePayloads; // does current field store payloads?
            protected bool m_storeOffsets; // does current field store offsets?

            protected int m_limit; // number of docs in this posting
            protected int m_ord; // how many docs we've read
            protected int m_doc; // doc we last read
            protected int m_accum; // accumulator for doc deltas
            protected int m_freq; // freq we last read
            protected int m_maxBufferedDocId;

            protected int m_start;
            protected int m_count;

            protected long m_freqOffset;
            protected long m_skipOffset;

            protected bool m_skipped;
            protected internal readonly IBits m_liveDocs;

            private protected SegmentDocsEnumBase(Lucene40PostingsReader outerInstance, IndexInput startFreqIn, IBits liveDocs) // LUCENENET: Changed from internal to private protected
            {
                this.outerInstance = outerInstance;
                this.startFreqIn = startFreqIn;
                this.freqIn = (IndexInput)startFreqIn.Clone();
                this.m_liveDocs = liveDocs;
            }

            internal virtual DocsEnum Reset(FieldInfo fieldInfo, StandardTermState termState)
            {
                m_indexOmitsTF = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY;
                m_storePayloads = fieldInfo.HasPayloads;
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                m_storeOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                m_freqOffset = termState.freqOffset;
                m_skipOffset = termState.skipOffset;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                freqIn.Seek(termState.freqOffset);
                m_limit = termState.DocFreq;
                if (Debugging.AssertsEnabled) Debugging.Assert(m_limit > 0);
                m_ord = 0;
                m_doc = -1;
                m_accum = 0;
                // if (DEBUG) System.out.println("  sde limit=" + limit + " freqFP=" + freqOffset);
                m_skipped = false;

                m_start = -1;
                m_count = 0;
                m_freq = 1;
                if (m_indexOmitsTF)
                {
                    Arrays.Fill(m_freqs, 1);
                }
                m_maxBufferedDocId = -1;
                return this;
            }

            public override sealed int Freq => m_freq;

            public override sealed int DocID => m_doc;

            public override sealed int Advance(int target)
            {
                // last doc in our buffer is >= target, binary search + next()
                if (++m_start < m_count && m_maxBufferedDocId >= target)
                {
                    if ((m_count - m_start) > 32) // 32 seemed to be a sweetspot here so use binsearch if the pending results are a lot
                    {
                        m_start = BinarySearch(m_count - 1, m_start, target, m_docs);
                        return NextDoc();
                    }
                    else
                    {
                        return LinearScan(target);
                    }
                }

                m_start = m_count; // buffer is consumed

                return m_doc = SkipTo(target);
            }

            private int BinarySearch(int hi, int low, int target, int[] docs)
            {
                while (low <= hi)
                {
                    int mid = (hi + low).TripleShift(1);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static int ReadFreq(IndexInput freqIn, int code) // LUCENENET: CA1822: Mark members as static
            {
                if ((code & 1) != 0) // if low bit is set
                {
                    return 1; // freq is one
                }
                else
                {
                    return freqIn.ReadVInt32(); // else read freq
                }
            }

            protected internal abstract int LinearScan(int scanTo);

            protected internal abstract int ScanTo(int target);

            protected internal int Refill()
            {
                int doc = NextUnreadDoc();
                m_count = 0;
                m_start = -1;
                if (doc == NO_MORE_DOCS)
                {
                    return NO_MORE_DOCS;
                }
                int numDocs = Math.Min(m_docs.Length, m_limit - m_ord);
                m_ord += numDocs;
                if (m_indexOmitsTF)
                {
                    m_count = FillDocs(numDocs);
                }
                else
                {
                    m_count = FillDocsAndFreqs(numDocs);
                }
                m_maxBufferedDocId = m_count > 0 ? m_docs[m_count - 1] : NO_MORE_DOCS;
                return doc;
            }

            protected internal abstract int NextUnreadDoc();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int FillDocs(int size)
            {
                IndexInput freqIn = this.freqIn;
                int[] docs = this.m_docs;
                int docAc = m_accum;
                for (int i = 0; i < size; i++)
                {
                    docAc += freqIn.ReadVInt32();
                    docs[i] = docAc;
                }
                m_accum = docAc;
                return size;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int FillDocsAndFreqs(int size)
            {
                IndexInput freqIn = this.freqIn;
                int[] docs = this.m_docs;
                int[] freqs = this.m_freqs;
                int docAc = m_accum;
                for (int i = 0; i < size; i++)
                {
                    int code = freqIn.ReadVInt32();
                    docAc += code.TripleShift(1); // shift off low bit
                    freqs[i] = ReadFreq(freqIn, code);
                    docs[i] = docAc;
                }
                m_accum = docAc;
                return size;
            }

            private int SkipTo(int target)
            {
                if ((target - outerInstance.skipInterval) >= m_accum && m_limit >= outerInstance.skipMinimum)
                {
                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close.

                    if (skipper is null)
                    {
                        // this is the first time this enum has ever been used for skipping -- do lazy init
                        skipper = new Lucene40SkipListReader((IndexInput)freqIn.Clone(), outerInstance.maxSkipLevels, outerInstance.skipInterval);
                    }

                    if (!m_skipped)
                    {
                        // this is the first time this posting has
                        // skipped since reset() was called, so now we
                        // load the skip data for this posting

                        skipper.Init(m_freqOffset + m_skipOffset, m_freqOffset, 0, m_limit, m_storePayloads, m_storeOffsets);

                        m_skipped = true;
                    }

                    int newOrd = skipper.SkipTo(target);

                    if (newOrd > m_ord)
                    {
                        // Skipper moved

                        m_ord = newOrd;
                        m_accum = skipper.Doc;
                        freqIn.Seek(skipper.FreqPointer);
                    }
                }
                return ScanTo(target);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return m_limit;
            }
        }

        private sealed class AllDocsSegmentDocsEnum : SegmentDocsEnumBase
        {
            internal AllDocsSegmentDocsEnum(Lucene40PostingsReader outerInstance, IndexInput startFreqIn)
                : base(outerInstance, startFreqIn, null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(m_liveDocs is null);
            }

            public override int NextDoc()
            {
                if (++m_start < m_count)
                {
                    m_freq = m_freqs[m_start];
                    return m_doc = m_docs[m_start];
                }
                return m_doc = Refill();
            }

            protected internal override int LinearScan(int scanTo)
            {
                int[] docs = this.m_docs;
                int upTo = m_count;
                for (int i = m_start; i < upTo; i++)
                {
                    int d = docs[i];
                    if (scanTo <= d)
                    {
                        m_start = i;
                        m_freq = m_freqs[i];
                        return m_doc = docs[i];
                    }
                }
                return m_doc = Refill();
            }

            protected internal override int ScanTo(int target)
            {
                int docAcc = m_accum;
                int frq = 1;
                IndexInput freqIn = this.freqIn;
                bool omitTF = m_indexOmitsTF;
                int loopLimit = m_limit;
                for (int i = m_ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt32();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += code.TripleShift(1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (docAcc >= target)
                    {
                        m_freq = frq;
                        m_ord = i + 1;
                        return m_accum = docAcc;
                    }
                }
                m_ord = m_limit;
                m_freq = frq;
                m_accum = docAcc;
                return NO_MORE_DOCS;
            }

            protected internal override int NextUnreadDoc()
            {
                if (m_ord++ < m_limit)
                {
                    int code = freqIn.ReadVInt32();
                    if (m_indexOmitsTF)
                    {
                        m_accum += code;
                    }
                    else
                    {
                        m_accum += code.TripleShift(1); // shift off low bit
                        m_freq = ReadFreq(freqIn, code);
                    }
                    return m_accum;
                }
                else
                {
                    return NO_MORE_DOCS;
                }
            }
        }

        private sealed class LiveDocsSegmentDocsEnum : SegmentDocsEnumBase
        {
            internal LiveDocsSegmentDocsEnum(Lucene40PostingsReader outerInstance, IndexInput startFreqIn, IBits liveDocs)
                : base(outerInstance, startFreqIn, liveDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(liveDocs != null);
            }

            public override int NextDoc()
            {
                IBits liveDocs = this.m_liveDocs;
                for (int i = m_start + 1; i < m_count; i++)
                {
                    int d = m_docs[i];
                    if (liveDocs.Get(d))
                    {
                        m_start = i;
                        m_freq = m_freqs[i];
                        return m_doc = d;
                    }
                }
                m_start = m_count;
                return m_doc = Refill();
            }

            protected internal override int LinearScan(int scanTo)
            {
                int[] docs = this.m_docs;
                int upTo = m_count;
                IBits liveDocs = this.m_liveDocs;
                for (int i = m_start; i < upTo; i++)
                {
                    int d = docs[i];
                    if (scanTo <= d && liveDocs.Get(d))
                    {
                        m_start = i;
                        m_freq = m_freqs[i];
                        return m_doc = docs[i];
                    }
                }
                return m_doc = Refill();
            }

            protected internal override int ScanTo(int target)
            {
                int docAcc = m_accum;
                int frq = 1;
                IndexInput freqIn = this.freqIn;
                bool omitTF = m_indexOmitsTF;
                int loopLimit = m_limit;
                IBits liveDocs = this.m_liveDocs;
                for (int i = m_ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt32();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += code.TripleShift(1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (docAcc >= target && liveDocs.Get(docAcc))
                    {
                        m_freq = frq;
                        m_ord = i + 1;
                        return m_accum = docAcc;
                    }
                }
                m_ord = m_limit;
                m_freq = frq;
                m_accum = docAcc;
                return NO_MORE_DOCS;
            }

            protected internal override int NextUnreadDoc()
            {
                int docAcc = m_accum;
                int frq = 1;
                IndexInput freqIn = this.freqIn;
                bool omitTF = m_indexOmitsTF;
                int loopLimit = m_limit;
                IBits liveDocs = this.m_liveDocs;
                for (int i = m_ord; i < loopLimit; i++)
                {
                    int code = freqIn.ReadVInt32();
                    if (omitTF)
                    {
                        docAcc += code;
                    }
                    else
                    {
                        docAcc += code.TripleShift(1); // shift off low bit
                        frq = ReadFreq(freqIn, code);
                    }
                    if (liveDocs.Get(docAcc))
                    {
                        m_freq = frq;
                        m_ord = i + 1;
                        return m_accum = docAcc;
                    }
                }
                m_ord = m_limit;
                m_freq = frq;
                m_accum = docAcc;
                return NO_MORE_DOCS;
            }
        }

        // TODO specialize DocsAndPosEnum too

        // Decodes docs & positions. payloads nor offsets are present.
        private sealed class SegmentDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene40PostingsReader outerInstance;

            internal readonly IndexInput startFreqIn;
            internal readonly IndexInput freqIn;
            internal readonly IndexInput proxIn;
            internal int limit; // number of docs in this posting
            internal int ord; // how many docs we've read
            internal int doc = -1; // doc we last read
            internal int accum; // accumulator for doc deltas
            internal int freq; // freq we last read
            internal int position;

            internal IBits liveDocs;

            internal long freqOffset;
            internal long skipOffset;
            internal long proxOffset;

            internal int posPendingCount;

            internal bool skipped;
            internal Lucene40SkipListReader skipper;
            internal long lazyProxPointer;

            public SegmentDocsAndPositionsEnum(Lucene40PostingsReader outerInstance, IndexInput freqIn, IndexInput proxIn)
            {
                this.outerInstance = outerInstance;
                startFreqIn = freqIn;
                this.freqIn = (IndexInput)freqIn.Clone();
                this.proxIn = (IndexInput)proxIn.Clone();
            }

            public SegmentDocsAndPositionsEnum Reset(FieldInfo fieldInfo, StandardTermState termState, IBits liveDocs)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(fieldInfo.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
                    Debugging.Assert(!fieldInfo.HasPayloads);
                }

                this.liveDocs = liveDocs;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                freqIn.Seek(termState.freqOffset);
                lazyProxPointer = termState.proxOffset;

                limit = termState.DocFreq;
                if (Debugging.AssertsEnabled) Debugging.Assert(limit > 0);

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
                    int code = freqIn.ReadVInt32();

                    accum += code.TripleShift(1); // shift off low bit
                    if ((code & 1) != 0) // if low bit is set
                    {
                        freq = 1; // freq is one
                    }
                    else
                    {
                        freq = freqIn.ReadVInt32(); // else read freq
                    }
                    posPendingCount += freq;

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        break;
                    }
                }

                position = 0;

                // if (DEBUG) System.out.println("  return doc=" + doc);
                return (doc = accum);
            }

            public override int DocID => doc;

            public override int Freq => freq;

            public override int Advance(int target)
            {
                //System.out.println("StandardR.D&PE advance target=" + target);

                if ((target - outerInstance.skipInterval) >= doc && limit >= outerInstance.skipMinimum)
                {
                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close

                    if (skipper is null)
                    {
                        // this is the first time this enum has ever been used for skipping -- do lazy init
                        skipper = new Lucene40SkipListReader((IndexInput)freqIn.Clone(), outerInstance.maxSkipLevels, outerInstance.skipInterval);
                    }

                    if (!skipped)
                    {
                        // this is the first time this posting has
                        // skipped, since reset() was called, so now we
                        // load the skip data for this posting

                        skipper.Init(freqOffset + skipOffset, freqOffset, proxOffset, limit, false, false);

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

                position += proxIn.ReadVInt32();

                posPendingCount--;

                if (Debugging.AssertsEnabled) Debugging.Assert(posPendingCount >= 0,"NextPosition() was called too many times (more than Freq( times) posPendingCount={0}", posPendingCount);

                return position;
            }

            public override int StartOffset => -1;

            public override int EndOffset => -1;

            /// <summary>
            /// Returns the payload at this position, or <c>null</c> if no
            /// payload was indexed.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override BytesRef GetPayload()
            {
                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return limit;
            }
        }

        // Decodes docs & positions & (payloads and/or offsets)
        private class SegmentFullPositionsEnum : DocsAndPositionsEnum
        {
            private readonly Lucene40PostingsReader outerInstance;

            internal readonly IndexInput startFreqIn;
            private readonly IndexInput freqIn;
            private readonly IndexInput proxIn;

            internal int limit; // number of docs in this posting
            internal int ord; // how many docs we've read
            internal int doc = -1; // doc we last read
            internal int accum; // accumulator for doc deltas
            internal int freq; // freq we last read
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
            internal BytesRef payload;
            internal long lazyProxPointer;

            internal bool storePayloads;
            internal bool storeOffsets;

            internal int offsetLength;
            internal int startOffset;

            public SegmentFullPositionsEnum(Lucene40PostingsReader outerInstance, IndexInput freqIn, IndexInput proxIn)
            {
                this.outerInstance = outerInstance;
                startFreqIn = freqIn;
                this.freqIn = (IndexInput)freqIn.Clone();
                this.proxIn = (IndexInput)proxIn.Clone();
            }

            public virtual SegmentFullPositionsEnum Reset(FieldInfo fieldInfo, StandardTermState termState, IBits liveDocs)
            {
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                storeOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                storePayloads = fieldInfo.HasPayloads;
                if (Debugging.AssertsEnabled)
                {
                    // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                    Debugging.Assert(IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0);
                    Debugging.Assert(storePayloads || storeOffsets);
                }
                if (payload is null)
                {
                    payload = new BytesRef();
                    payload.Bytes = new byte[1];
                }

                this.liveDocs = liveDocs;

                // TODO: for full enum case (eg segment merging) this
                // seek is unnecessary; maybe we can avoid in such
                // cases
                freqIn.Seek(termState.freqOffset);
                lazyProxPointer = termState.proxOffset;

                limit = termState.DocFreq;
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
                    int code = freqIn.ReadVInt32();

                    accum += code.TripleShift(1); // shift off low bit
                    if ((code & 1) != 0) // if low bit is set
                    {
                        freq = 1; // freq is one
                    }
                    else
                    {
                        freq = freqIn.ReadVInt32(); // else read freq
                    }
                    posPendingCount += freq;

                    if (liveDocs is null || liveDocs.Get(accum))
                    {
                        break;
                    }
                }

                position = 0;
                startOffset = 0;

                //System.out.println("StandardR.D&PE nextDoc seg=" + segment + " return doc=" + doc);
                return (doc = accum);
            }

            public override int DocID => doc;

            public override int Freq => freq;

            public override int Advance(int target)
            {
                //System.out.println("StandardR.D&PE advance seg=" + segment + " target=" + target + " this=" + this);

                if ((target - outerInstance.skipInterval) >= doc && limit >= outerInstance.skipMinimum)
                {
                    // There are enough docs in the posting to have
                    // skip data, and it isn't too close

                    if (skipper is null)
                    {
                        // this is the first time this enum has ever been used for skipping -- do lazy init
                        skipper = new Lucene40SkipListReader((IndexInput)freqIn.Clone(), outerInstance.maxSkipLevels, outerInstance.skipInterval);
                    }

                    if (!skipped)
                    {
                        // this is the first time this posting has
                        // skipped, since reset() was called, so now we
                        // load the skip data for this posting
                        //System.out.println("  init skipper freqOffset=" + freqOffset + " skipOffset=" + skipOffset + " vs len=" + freqIn.length());
                        skipper.Init(freqOffset + skipOffset, freqOffset, proxOffset, limit, storePayloads, storeOffsets);

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
                    proxIn.Seek(proxIn.Position + payloadLength); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    payloadPending = false;
                }

                // scan over any docs that were iterated without their positions
                while (posPendingCount > freq)
                {
                    int code = proxIn.ReadVInt32();

                    if (storePayloads)
                    {
                        if ((code & 1) != 0)
                        {
                            // new payload length
                            payloadLength = proxIn.ReadVInt32();
                            if (Debugging.AssertsEnabled) Debugging.Assert(payloadLength >= 0);
                        }
                        if (Debugging.AssertsEnabled) Debugging.Assert(payloadLength != -1);
                    }

                    if (storeOffsets)
                    {
                        if ((proxIn.ReadVInt32() & 1) != 0)
                        {
                            // new offset length
                            offsetLength = proxIn.ReadVInt32();
                        }
                    }

                    if (storePayloads)
                    {
                        proxIn.Seek(proxIn.Position + payloadLength); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
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
                    proxIn.Seek(proxIn.Position + payloadLength); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }

                int code_ = proxIn.ReadVInt32();
                if (storePayloads)
                {
                    if ((code_ & 1) != 0)
                    {
                        // new payload length
                        payloadLength = proxIn.ReadVInt32();
                        if (Debugging.AssertsEnabled) Debugging.Assert(payloadLength >= 0);
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(payloadLength != -1);

                    payloadPending = true;
                    code_ = code_.TripleShift(1);
                }
                position += code_;

                if (storeOffsets)
                {
                    int offsetCode = proxIn.ReadVInt32();
                    if ((offsetCode & 1) != 0)
                    {
                        // new offset length
                        offsetLength = proxIn.ReadVInt32();
                    }
                    startOffset += offsetCode.TripleShift(1);
                }

                posPendingCount--;

                if (Debugging.AssertsEnabled) Debugging.Assert(posPendingCount >= 0,"NextPosition() was called too many times (more than Freq times) posPendingCount={0}", posPendingCount);

                //System.out.println("StandardR.D&PE nextPos   return pos=" + position);
                return position;
            }

            public override int StartOffset => storeOffsets ? startOffset : -1;

            public override int EndOffset => storeOffsets ? startOffset + offsetLength : -1;

            /// <summary>
            /// Returns the payload at this position, or <c>null</c> if no
            /// payload was indexed.
            /// </summary>
            public override BytesRef GetPayload()
            {
                if (storePayloads)
                {
                    if (payloadLength <= 0)
                    {
                        return null;
                    }
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(lazyProxPointer == -1);
                        Debugging.Assert(posPendingCount < freq);
                    }

                    if (payloadPending)
                    {
                        if (payloadLength > payload.Bytes.Length)
                        {
                            payload.Grow(payloadLength);
                        }

                        proxIn.ReadBytes(payload.Bytes, 0, payloadLength);
                        payload.Length = payloadLength;
                        payloadPending = false;
                    }

                    return payload;
                }
                else
                {
                    return null;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return limit;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CheckIntegrity()
        {
        }
    }
}