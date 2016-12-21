using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Bits = Lucene.Net.Util.Bits;
using BytesRef = Lucene.Net.Util.BytesRef;
using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Codecs.Lucene3x
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

    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using Fields = Lucene.Net.Index.Fields;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexFormatTooNewException = Lucene.Net.Index.IndexFormatTooNewException;
    using IndexFormatTooOldException = Lucene.Net.Index.IndexFormatTooOldException;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// @deprecated Only for reading existing 3.x indexes
    [Obsolete("Only for reading existing 3.x indexes")]
    internal class Lucene3xTermVectorsReader : TermVectorsReader
    {
        // NOTE: if you make a new format, it must be larger than
        // the current format

        // Changed strings to UTF8 with length-in-bytes not length-in-chars
        internal const int FORMAT_UTF8_LENGTH_IN_BYTES = 4;

        // NOTE: always change this if you switch to a new format!
        // whenever you add a new format, make it 1 larger (positive version logic)!
        public const int FORMAT_CURRENT = FORMAT_UTF8_LENGTH_IN_BYTES;

        // when removing support for old versions, leave the last supported version here
        public const int FORMAT_MINIMUM = FORMAT_UTF8_LENGTH_IN_BYTES;

        //The size in bytes that the FORMAT_VERSION will take up at the beginning of each file
        internal const int FORMAT_SIZE = 4;

        public const sbyte STORE_POSITIONS_WITH_TERMVECTOR = 0x1;

        public const sbyte STORE_OFFSET_WITH_TERMVECTOR = 0x2;

        /// <summary>
        /// Extension of vectors fields file </summary>
        public const string VECTORS_FIELDS_EXTENSION = "tvf";

        /// <summary>
        /// Extension of vectors documents file </summary>
        public const string VECTORS_DOCUMENTS_EXTENSION = "tvd";

        /// <summary>
        /// Extension of vectors index file </summary>
        public const string VECTORS_INDEX_EXTENSION = "tvx";

        private readonly FieldInfos FieldInfos;

        private IndexInput Tvx;
        private IndexInput Tvd;
        private IndexInput Tvf;
        private int Size_Renamed;
        private int NumTotalDocs;

        // The docID offset where our docs begin in the index
        // file.  this will be 0 if we have our own private file.
        private int DocStoreOffset;

        // when we are inside a compound share doc store (CFX),
        // (lucene 3.0 indexes only), we privately open our own fd.
        // TODO: if we are worried, maybe we could eliminate the
        // extra fd somehow when you also have vectors...
        private readonly CompoundFileDirectory StoreCFSReader;

        private readonly int Format;

        // used by clone
        internal Lucene3xTermVectorsReader(FieldInfos fieldInfos, IndexInput tvx, IndexInput tvd, IndexInput tvf, int size, int numTotalDocs, int docStoreOffset, int format)
        {
            this.FieldInfos = fieldInfos;
            this.Tvx = tvx;
            this.Tvd = tvd;
            this.Tvf = tvf;
            this.Size_Renamed = size;
            this.NumTotalDocs = numTotalDocs;
            this.DocStoreOffset = docStoreOffset;
            this.Format = format;
            this.StoreCFSReader = null;
        }

        public Lucene3xTermVectorsReader(Directory d, SegmentInfo si, FieldInfos fieldInfos, IOContext context)
        {
            string segment = Lucene3xSegmentInfoFormat.GetDocStoreSegment(si);
            int docStoreOffset = Lucene3xSegmentInfoFormat.GetDocStoreOffset(si);
            int size = si.DocCount;

            bool success = false;

            try
            {
                if (docStoreOffset != -1 && Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(si))
                {
                    d = StoreCFSReader = new CompoundFileDirectory(si.Dir, IndexFileNames.SegmentFileName(segment, "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION), context, false);
                }
                else
                {
                    StoreCFSReader = null;
                }
                string idxName = IndexFileNames.SegmentFileName(segment, "", VECTORS_INDEX_EXTENSION);
                Tvx = d.OpenInput(idxName, context);
                Format = CheckValidFormat(Tvx);
                string fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_DOCUMENTS_EXTENSION);
                Tvd = d.OpenInput(fn, context);
                int tvdFormat = CheckValidFormat(Tvd);
                fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_FIELDS_EXTENSION);
                Tvf = d.OpenInput(fn, context);
                int tvfFormat = CheckValidFormat(Tvf);

                Debug.Assert(Format == tvdFormat);
                Debug.Assert(Format == tvfFormat);

                NumTotalDocs = (int)(Tvx.Length() >> 4);

                if (-1 == docStoreOffset)
                {
                    this.DocStoreOffset = 0;
                    this.Size_Renamed = NumTotalDocs;
                    Debug.Assert(size == 0 || NumTotalDocs == size);
                }
                else
                {
                    this.DocStoreOffset = docStoreOffset;
                    this.Size_Renamed = size;
                    // Verify the file is long enough to hold all of our
                    // docs
                    Debug.Assert(NumTotalDocs >= size + docStoreOffset, "numTotalDocs=" + NumTotalDocs + " size=" + size + " docStoreOffset=" + docStoreOffset);
                }

                this.FieldInfos = fieldInfos;
                success = true;
            }
            finally
            {
                // With lock-less commits, it's entirely possible (and
                // fine) to hit a FileNotFound exception above. In
                // this case, we want to explicitly close any subset
                // of things that were opened so that we don't have to
                // wait for a GC to do so.
                if (!success)
                {
                    try
                    {
                        Dispose();
                    } // keep our original exception
                    catch (Exception)
                    {
                    }
                }
            }
        }

        // Not private to avoid synthetic access$NNN methods
        internal virtual void SeekTvx(int docNum)
        {
            Tvx.Seek((docNum + DocStoreOffset) * 16L + FORMAT_SIZE);
        }

        private int CheckValidFormat(IndexInput @in)
        {
            int format = @in.ReadInt();
            if (format < FORMAT_MINIMUM)
            {
                throw new IndexFormatTooOldException(@in, format, FORMAT_MINIMUM, FORMAT_CURRENT);
            }
            if (format > FORMAT_CURRENT)
            {
                throw new IndexFormatTooNewException(@in, format, FORMAT_MINIMUM, FORMAT_CURRENT);
            }
            return format;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IOUtils.Close(Tvx, Tvd, Tvf, StoreCFSReader);
            }
        }

        ///
        /// <returns> The number of documents in the reader </returns>
        internal virtual int Size() // LUCENENET TODO: Rename to Count
        {
            return Size_Renamed;
        }

        private class TVFields : Fields
        {
            private readonly Lucene3xTermVectorsReader OuterInstance;

            private readonly int[] FieldNumbers;
            private readonly long[] FieldFPs;
            private readonly IDictionary<int, int> FieldNumberToIndex = new Dictionary<int, int>();

            public TVFields(Lucene3xTermVectorsReader outerInstance, int docID)
            {
                this.OuterInstance = outerInstance;
                outerInstance.SeekTvx(docID);
                outerInstance.Tvd.Seek(outerInstance.Tvx.ReadLong());

                int fieldCount = outerInstance.Tvd.ReadVInt();
                Debug.Assert(fieldCount >= 0);
                if (fieldCount != 0)
                {
                    FieldNumbers = new int[fieldCount];
                    FieldFPs = new long[fieldCount];
                    for (int fieldUpto = 0; fieldUpto < fieldCount; fieldUpto++)
                    {
                        int fieldNumber = outerInstance.Tvd.ReadVInt();
                        FieldNumbers[fieldUpto] = fieldNumber;
                        FieldNumberToIndex[fieldNumber] = fieldUpto;
                    }

                    long position = outerInstance.Tvx.ReadLong();
                    FieldFPs[0] = position;
                    for (int fieldUpto = 1; fieldUpto < fieldCount; fieldUpto++)
                    {
                        position += outerInstance.Tvd.ReadVLong();
                        FieldFPs[fieldUpto] = position;
                    }
                }
                else
                {
                    // TODO: we can improve writer here, eg write 0 into
                    // tvx file, so we know on first read from tvx that
                    // this doc has no TVs
                    FieldNumbers = null;
                    FieldFPs = null;
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this);
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<string>
            {
                private readonly TVFields OuterInstance;
                private string current;
                private int i, upTo;

                public IteratorAnonymousInnerClassHelper(TVFields outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    upTo = OuterInstance.FieldNumbers.Length;
                    i = 0;
                }

                public bool MoveNext()
                {
                    if (OuterInstance.FieldNumbers != null && i < upTo)
                    {
                        current = OuterInstance.OuterInstance.FieldInfos.FieldInfo(OuterInstance.FieldNumbers[i++]).Name;
                        return true;
                    }
                    return false;
                }

                public string Current
                {
                    get
                    {
                        return current;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {
                }
            }

            public override Terms Terms(string field)
            {
                FieldInfo fieldInfo = OuterInstance.FieldInfos.FieldInfo(field);
                if (fieldInfo == null)
                {
                    // No such field
                    return null;
                }

                int fieldIndex;
                if (!FieldNumberToIndex.TryGetValue(fieldInfo.Number, out fieldIndex))
                {
                    // Term vectors were not indexed for this field
                    return null;
                }

                return new TVTerms(OuterInstance, FieldFPs[fieldIndex]);
            }

            public override int Size
            {
                get
                {
                    if (FieldNumbers == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return FieldNumbers.Length;
                    }
                }
            }
        }

        private class TVTerms : Terms
        {
            private readonly Lucene3xTermVectorsReader OuterInstance;

            private readonly int NumTerms;
            private readonly long TvfFPStart;
            private readonly bool StorePositions;
            private readonly bool StoreOffsets;
            private readonly bool UnicodeSortOrder;

            public TVTerms(Lucene3xTermVectorsReader outerInstance, long tvfFP)
            {
                this.OuterInstance = outerInstance;
                outerInstance.Tvf.Seek(tvfFP);
                NumTerms = outerInstance.Tvf.ReadVInt();
                byte bits = outerInstance.Tvf.ReadByte();
                StorePositions = (bits & STORE_POSITIONS_WITH_TERMVECTOR) != 0;
                StoreOffsets = (bits & STORE_OFFSET_WITH_TERMVECTOR) != 0;
                TvfFPStart = outerInstance.Tvf.FilePointer;
                UnicodeSortOrder = outerInstance.SortTermsByUnicode();
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                TVTermsEnum termsEnum;
                if (reuse is TVTermsEnum)
                {
                    termsEnum = (TVTermsEnum)reuse;
                    if (!termsEnum.CanReuse(OuterInstance.Tvf))
                    {
                        termsEnum = new TVTermsEnum(OuterInstance);
                    }
                }
                else
                {
                    termsEnum = new TVTermsEnum(OuterInstance);
                }
                termsEnum.Reset(NumTerms, TvfFPStart, StorePositions, StoreOffsets, UnicodeSortOrder);
                return termsEnum;
            }

            public override long Size()
            {
                return NumTerms;
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return -1;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    // Every term occurs in just one doc:
                    return NumTerms;
                }
            }

            public override int DocCount
            {
                get
                {
                    return 1;
                }
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    if (UnicodeSortOrder)
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                    else
                    {
                        return BytesRef.UTF8SortedAsUTF16Comparer;
                    }
                }
            }

            public override bool HasFreqs()
            {
                return true;
            }

            public override bool HasOffsets()
            {
                return StoreOffsets;
            }

            public override bool HasPositions()
            {
                return StorePositions;
            }

            public override bool HasPayloads()
            {
                return false;
            }
        }

        internal class TermAndPostings
        {
            internal BytesRef Term;
            internal int Freq;
            internal int[] Positions;
            internal int[] StartOffsets;
            internal int[] EndOffsets;
        }

        private class TVTermsEnum : TermsEnum
        {
            private readonly Lucene3xTermVectorsReader OuterInstance;

            internal bool UnicodeSortOrder;
            internal readonly IndexInput OrigTVF;
            internal readonly IndexInput Tvf;
            internal int NumTerms;
            internal int CurrentTerm;
            internal bool StorePositions;
            internal bool StoreOffsets;

            internal TermAndPostings[] TermAndPostings;

            // NOTE: tvf is pre-positioned by caller
            public TVTermsEnum(Lucene3xTermVectorsReader outerInstance)
            {
                this.OuterInstance = outerInstance;
                this.OrigTVF = outerInstance.Tvf;
                Tvf = (IndexInput)OrigTVF.Clone();
            }

            public virtual bool CanReuse(IndexInput tvf)
            {
                return tvf == OrigTVF;
            }

            public virtual void Reset(int numTerms, long tvfFPStart, bool storePositions, bool storeOffsets, bool unicodeSortOrder)
            {
                this.NumTerms = numTerms;
                this.StorePositions = storePositions;
                this.StoreOffsets = storeOffsets;
                CurrentTerm = -1;
                Tvf.Seek(tvfFPStart);
                this.UnicodeSortOrder = unicodeSortOrder;
                ReadVectors();
                if (unicodeSortOrder)
                {
                    Array.Sort(TermAndPostings, new ComparatorAnonymousInnerClassHelper(this));
                }
            }

            private class ComparatorAnonymousInnerClassHelper : IComparer<TermAndPostings>
            {
                private readonly TVTermsEnum OuterInstance;

                public ComparatorAnonymousInnerClassHelper(TVTermsEnum outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public virtual int Compare(TermAndPostings left, TermAndPostings right)
                {
                    return left.Term.CompareTo(right.Term);
                }
            }

            private void ReadVectors()
            {
                TermAndPostings = new TermAndPostings[NumTerms];
                BytesRef lastTerm = new BytesRef();
                for (int i = 0; i < NumTerms; i++)
                {
                    TermAndPostings t = new TermAndPostings();
                    BytesRef term = new BytesRef();
                    term.CopyBytes(lastTerm);
                    int start = Tvf.ReadVInt();
                    int deltaLen = Tvf.ReadVInt();
                    term.Length = start + deltaLen;
                    term.Grow(term.Length);
                    Tvf.ReadBytes(term.Bytes, start, deltaLen);
                    t.Term = term;
                    int freq = Tvf.ReadVInt();
                    t.Freq = freq;

                    if (StorePositions)
                    {
                        int[] positions = new int[freq];
                        int pos = 0;
                        for (int posUpto = 0; posUpto < freq; posUpto++)
                        {
                            int delta = Tvf.ReadVInt();
                            if (delta == -1)
                            {
                                delta = 0; // LUCENE-1542 correction
                            }
                            pos += delta;
                            positions[posUpto] = pos;
                        }
                        t.Positions = positions;
                    }

                    if (StoreOffsets)
                    {
                        int[] startOffsets = new int[freq];
                        int[] endOffsets = new int[freq];
                        int offset = 0;
                        for (int posUpto = 0; posUpto < freq; posUpto++)
                        {
                            startOffsets[posUpto] = offset + Tvf.ReadVInt();
                            offset = endOffsets[posUpto] = startOffsets[posUpto] + Tvf.ReadVInt();
                        }
                        t.StartOffsets = startOffsets;
                        t.EndOffsets = endOffsets;
                    }
                    lastTerm.CopyBytes(term);
                    TermAndPostings[i] = t;
                }
            }

            // NOTE: slow!  (linear scan)
            public override SeekStatus SeekCeil(BytesRef text)
            {
                IComparer<BytesRef> comparator = Comparator;
                for (int i = 0; i < NumTerms; i++)
                {
                    int cmp = comparator.Compare(text, TermAndPostings[i].Term);
                    if (cmp < 0)
                    {
                        CurrentTerm = i;
                        return SeekStatus.NOT_FOUND;
                    }
                    else if (cmp == 0)
                    {
                        CurrentTerm = i;
                        return SeekStatus.FOUND;
                    }
                }
                CurrentTerm = TermAndPostings.Length;
                return SeekStatus.END;
            }

            public override void SeekExact(long ord)
            {
                throw new System.NotSupportedException();
            }

            public override BytesRef Next()
            {
                if (++CurrentTerm >= NumTerms)
                {
                    return null;
                }
                return Term();
            }

            public override BytesRef Term()
            {
                return TermAndPostings[CurrentTerm].Term;
            }

            public override long Ord()
            {
                throw new System.NotSupportedException();
            }

            public override int DocFreq()
            {
                return 1;
            }

            public override long TotalTermFreq()
            {
                return TermAndPostings[CurrentTerm].Freq;
            }

            public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags) // ignored
            {
                TVDocsEnum docsEnum;
                if (reuse != null && reuse is TVDocsEnum)
                {
                    docsEnum = (TVDocsEnum)reuse;
                }
                else
                {
                    docsEnum = new TVDocsEnum();
                }
                docsEnum.Reset(liveDocs, TermAndPostings[CurrentTerm]);
                return docsEnum;
            }

            public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                if (!StorePositions && !StoreOffsets)
                {
                    return null;
                }

                TVDocsAndPositionsEnum docsAndPositionsEnum;
                if (reuse != null && reuse is TVDocsAndPositionsEnum)
                {
                    docsAndPositionsEnum = (TVDocsAndPositionsEnum)reuse;
                }
                else
                {
                    docsAndPositionsEnum = new TVDocsAndPositionsEnum();
                }
                docsAndPositionsEnum.Reset(liveDocs, TermAndPostings[CurrentTerm]);
                return docsAndPositionsEnum;
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    if (UnicodeSortOrder)
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                    else
                    {
                        return BytesRef.UTF8SortedAsUTF16Comparer;
                    }
                }
            }
        }

        // NOTE: sort of a silly class, since you can get the
        // freq() already by TermsEnum.totalTermFreq
        private class TVDocsEnum : DocsEnum
        {
            internal bool DidNext;
            internal int Doc = -1;
            internal int Freq_Renamed;
            internal Bits LiveDocs;

            public override int Freq()
            {
                return Freq_Renamed;
            }

            public override int DocID()
            {
                return Doc;
            }

            public override int NextDoc()
            {
                if (!DidNext && (LiveDocs == null || LiveDocs.Get(0)))
                {
                    DidNext = true;
                    return (Doc = 0);
                }
                else
                {
                    return (Doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                if (!DidNext && target == 0)
                {
                    return NextDoc();
                }
                else
                {
                    return (Doc = NO_MORE_DOCS);
                }
            }

            public virtual void Reset(Bits liveDocs, TermAndPostings termAndPostings)
            {
                this.LiveDocs = liveDocs;
                this.Freq_Renamed = termAndPostings.Freq;
                this.Doc = -1;
                DidNext = false;
            }

            public override long Cost()
            {
                return 1;
            }
        }

        private class TVDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private bool DidNext;
            private int Doc = -1;
            private int NextPos;
            private Bits LiveDocs;
            private int[] Positions;
            private int[] StartOffsets;
            private int[] EndOffsets;

            public override int Freq()
            {
                if (Positions != null)
                {
                    return Positions.Length;
                }
                else
                {
                    Debug.Assert(StartOffsets != null);
                    return StartOffsets.Length;
                }
            }

            public override int DocID()
            {
                return Doc;
            }

            public override int NextDoc()
            {
                if (!DidNext && (LiveDocs == null || LiveDocs.Get(0)))
                {
                    DidNext = true;
                    return (Doc = 0);
                }
                else
                {
                    return (Doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                if (!DidNext && target == 0)
                {
                    return NextDoc();
                }
                else
                {
                    return (Doc = NO_MORE_DOCS);
                }
            }

            public virtual void Reset(Bits liveDocs, TermAndPostings termAndPostings)
            {
                this.LiveDocs = liveDocs;
                this.Positions = termAndPostings.Positions;
                this.StartOffsets = termAndPostings.StartOffsets;
                this.EndOffsets = termAndPostings.EndOffsets;
                this.Doc = -1;
                DidNext = false;
                NextPos = 0;
            }

            public override BytesRef Payload
            {
                get
                {
                    return null;
                }
            }

            public override int NextPosition()
            {
                Debug.Assert((Positions != null && NextPos < Positions.Length) || StartOffsets != null && NextPos < StartOffsets.Length);

                if (Positions != null)
                {
                    return Positions[NextPos++];
                }
                else
                {
                    NextPos++;
                    return -1;
                }
            }

            public override int StartOffset
            {
                get
                {
                    if (StartOffsets != null)
                    {
                        return StartOffsets[NextPos - 1];
                    }
                    else
                    {
                        return -1;
                    }
                }
            }

            public override int EndOffset
            {
                get
                {
                    if (EndOffsets != null)
                    {
                        return EndOffsets[NextPos - 1];
                    }
                    else
                    {
                        return -1;
                    }
                }
            }

            public override long Cost()
            {
                return 1;
            }
        }

        public override Fields Get(int docID)
        {
            if (Tvx != null)
            {
                Fields fields = new TVFields(this, docID);
                if (fields.Size == 0)
                {
                    // TODO: we can improve writer here, eg write 0 into
                    // tvx file, so we know on first read from tvx that
                    // this doc has no TVs
                    return null;
                }
                else
                {
                    return fields;
                }
            }
            else
            {
                return null;
            }
        }

        public override object Clone()
        {
            IndexInput cloneTvx = null;
            IndexInput cloneTvd = null;
            IndexInput cloneTvf = null;

            // These are null when a TermVectorsReader was created
            // on a segment that did not have term vectors saved
            if (Tvx != null && Tvd != null && Tvf != null)
            {
                cloneTvx = (IndexInput)Tvx.Clone();
                cloneTvd = (IndexInput)Tvd.Clone();
                cloneTvf = (IndexInput)Tvf.Clone();
            }

            return new Lucene3xTermVectorsReader(FieldInfos, cloneTvx, cloneTvd, cloneTvf, Size_Renamed, NumTotalDocs, DocStoreOffset, Format);
        }

        // If this returns, we do the surrogates shuffle so that the
        // terms are sorted by unicode sort order.  this should be
        // true when segments are used for "normal" searching;
        // it's only false during testing, to create a pre-flex
        // index, using the test-only PreFlexRW.
        protected internal virtual bool SortTermsByUnicode()
        {
            return true;
        }

        public override long RamBytesUsed()
        {
            // everything is disk-based
            return 0;
        }

        public override void CheckIntegrity()
        {
        }
    }
}