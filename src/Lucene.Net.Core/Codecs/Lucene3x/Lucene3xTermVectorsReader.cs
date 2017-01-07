using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using IBits = Lucene.Net.Util.IBits;
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

        private readonly FieldInfos fieldInfos;

        private IndexInput tvx;
        private IndexInput tvd;
        private IndexInput tvf;
        private int size;
        private int numTotalDocs;

        // The docID offset where our docs begin in the index
        // file.  this will be 0 if we have our own private file.
        private int docStoreOffset;

        // when we are inside a compound share doc store (CFX),
        // (lucene 3.0 indexes only), we privately open our own fd.
        // TODO: if we are worried, maybe we could eliminate the
        // extra fd somehow when you also have vectors...
        private readonly CompoundFileDirectory storeCFSReader;

        private readonly int format;

        // used by clone
        internal Lucene3xTermVectorsReader(FieldInfos fieldInfos, IndexInput tvx, IndexInput tvd, IndexInput tvf, int size, int numTotalDocs, int docStoreOffset, int format)
        {
            this.fieldInfos = fieldInfos;
            this.tvx = tvx;
            this.tvd = tvd;
            this.tvf = tvf;
            this.size = size;
            this.numTotalDocs = numTotalDocs;
            this.docStoreOffset = docStoreOffset;
            this.format = format;
            this.storeCFSReader = null;
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
                    d = storeCFSReader = new CompoundFileDirectory(si.Dir, IndexFileNames.SegmentFileName(segment, "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION), context, false);
                }
                else
                {
                    storeCFSReader = null;
                }
                string idxName = IndexFileNames.SegmentFileName(segment, "", VECTORS_INDEX_EXTENSION);
                tvx = d.OpenInput(idxName, context);
                format = CheckValidFormat(tvx);
                string fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_DOCUMENTS_EXTENSION);
                tvd = d.OpenInput(fn, context);
                int tvdFormat = CheckValidFormat(tvd);
                fn = IndexFileNames.SegmentFileName(segment, "", VECTORS_FIELDS_EXTENSION);
                tvf = d.OpenInput(fn, context);
                int tvfFormat = CheckValidFormat(tvf);

                Debug.Assert(format == tvdFormat);
                Debug.Assert(format == tvfFormat);

                numTotalDocs = (int)(tvx.Length >> 4);

                if (-1 == docStoreOffset)
                {
                    this.docStoreOffset = 0;
                    this.size = numTotalDocs;
                    Debug.Assert(size == 0 || numTotalDocs == size);
                }
                else
                {
                    this.docStoreOffset = docStoreOffset;
                    this.size = size;
                    // Verify the file is long enough to hold all of our
                    // docs
                    Debug.Assert(numTotalDocs >= size + docStoreOffset, "numTotalDocs=" + numTotalDocs + " size=" + size + " docStoreOffset=" + docStoreOffset);
                }

                this.fieldInfos = fieldInfos;
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
            tvx.Seek((docNum + docStoreOffset) * 16L + FORMAT_SIZE);
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
                IOUtils.Close(tvx, tvd, tvf, storeCFSReader);
            }
        }

        /// <summary>The number of documents in the reader. NOTE: This was size() in Lucene.</summary>
        internal virtual int Count
        {
            get { return size; }
        }

        private class TVFields : Fields
        {
            private readonly Lucene3xTermVectorsReader outerInstance;

            private readonly int[] fieldNumbers;
            private readonly long[] fieldFPs;
            private readonly IDictionary<int, int> fieldNumberToIndex = new Dictionary<int, int>();

            public TVFields(Lucene3xTermVectorsReader outerInstance, int docID)
            {
                this.outerInstance = outerInstance;
                outerInstance.SeekTvx(docID);
                outerInstance.tvd.Seek(outerInstance.tvx.ReadLong());

                int fieldCount = outerInstance.tvd.ReadVInt();
                Debug.Assert(fieldCount >= 0);
                if (fieldCount != 0)
                {
                    fieldNumbers = new int[fieldCount];
                    fieldFPs = new long[fieldCount];
                    for (int fieldUpto = 0; fieldUpto < fieldCount; fieldUpto++)
                    {
                        int fieldNumber = outerInstance.tvd.ReadVInt();
                        fieldNumbers[fieldUpto] = fieldNumber;
                        fieldNumberToIndex[fieldNumber] = fieldUpto;
                    }

                    long position = outerInstance.tvx.ReadLong();
                    fieldFPs[0] = position;
                    for (int fieldUpto = 1; fieldUpto < fieldCount; fieldUpto++)
                    {
                        position += outerInstance.tvd.ReadVLong();
                        fieldFPs[fieldUpto] = position;
                    }
                }
                else
                {
                    // TODO: we can improve writer here, eg write 0 into
                    // tvx file, so we know on first read from tvx that
                    // this doc has no TVs
                    fieldNumbers = null;
                    fieldFPs = null;
                }
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this);
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<string>
            {
                private readonly TVFields outerInstance;
                private string current;
                private int i, upTo;

                public IteratorAnonymousInnerClassHelper(TVFields outerInstance)
                {
                    this.outerInstance = outerInstance;
                    upTo = this.outerInstance.fieldNumbers.Length;
                    i = 0;
                }

                public bool MoveNext()
                {
                    if (outerInstance.fieldNumbers != null && i < upTo)
                    {
                        current = outerInstance.outerInstance.fieldInfos.FieldInfo(outerInstance.fieldNumbers[i++]).Name;
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
                FieldInfo fieldInfo = outerInstance.fieldInfos.FieldInfo(field);
                if (fieldInfo == null)
                {
                    // No such field
                    return null;
                }

                int fieldIndex;
                if (!fieldNumberToIndex.TryGetValue(fieldInfo.Number, out fieldIndex))
                {
                    // Term vectors were not indexed for this field
                    return null;
                }

                return new TVTerms(outerInstance, fieldFPs[fieldIndex]);
            }

            public override int Count
            {
                get
                {
                    if (fieldNumbers == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return fieldNumbers.Length;
                    }
                }
            }
        }

        private class TVTerms : Terms
        {
            private readonly Lucene3xTermVectorsReader outerInstance;

            private readonly int numTerms;
            private readonly long tvfFPStart;
            private readonly bool storePositions;
            private readonly bool storeOffsets;
            private readonly bool unicodeSortOrder;

            public TVTerms(Lucene3xTermVectorsReader outerInstance, long tvfFP)
            {
                this.outerInstance = outerInstance;
                outerInstance.tvf.Seek(tvfFP);
                numTerms = outerInstance.tvf.ReadVInt();
                byte bits = outerInstance.tvf.ReadByte();
                storePositions = (bits & STORE_POSITIONS_WITH_TERMVECTOR) != 0;
                storeOffsets = (bits & STORE_OFFSET_WITH_TERMVECTOR) != 0;
                tvfFPStart = outerInstance.tvf.FilePointer;
                unicodeSortOrder = outerInstance.SortTermsByUnicode();
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                TVTermsEnum termsEnum;
                if (reuse is TVTermsEnum)
                {
                    termsEnum = (TVTermsEnum)reuse;
                    if (!termsEnum.CanReuse(outerInstance.tvf))
                    {
                        termsEnum = new TVTermsEnum(outerInstance);
                    }
                }
                else
                {
                    termsEnum = new TVTermsEnum(outerInstance);
                }
                termsEnum.Reset(numTerms, tvfFPStart, storePositions, storeOffsets, unicodeSortOrder);
                return termsEnum;
            }

            public override long Count
            {
                get { return numTerms; }
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
                    return numTerms;
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
                    if (unicodeSortOrder)
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                    else
                    {
                        return BytesRef.UTF8SortedAsUTF16Comparer;
                    }
                }
            }

            public override bool HasFreqs
            {
                get { return true; }
            }

            public override bool HasOffsets
            {
                get { return storeOffsets; }
            }

            public override bool HasPositions
            {
                get { return storePositions; }
            }

            public override bool HasPayloads
            {
                get { return false; }
            }
        }

        internal class TermAndPostings
        {
            internal BytesRef Term { get; set; }
            internal int Freq { get; set; }
            internal int[] Positions { get; set; }
            internal int[] StartOffsets { get; set; }
            internal int[] EndOffsets { get; set; }
        }

        private class TVTermsEnum : TermsEnum
        {
            private readonly Lucene3xTermVectorsReader outerInstance;

            internal bool unicodeSortOrder;
            internal readonly IndexInput origTVF;
            internal readonly IndexInput tvf;
            internal int numTerms;
            internal int currentTerm;
            internal bool storePositions;
            internal bool storeOffsets;

            internal TermAndPostings[] termAndPostings;

            // NOTE: tvf is pre-positioned by caller
            public TVTermsEnum(Lucene3xTermVectorsReader outerInstance)
            {
                this.outerInstance = outerInstance;
                this.origTVF = outerInstance.tvf;
                tvf = (IndexInput)origTVF.Clone();
            }

            public virtual bool CanReuse(IndexInput tvf)
            {
                return tvf == origTVF;
            }

            public virtual void Reset(int numTerms, long tvfFPStart, bool storePositions, bool storeOffsets, bool unicodeSortOrder)
            {
                this.numTerms = numTerms;
                this.storePositions = storePositions;
                this.storeOffsets = storeOffsets;
                currentTerm = -1;
                tvf.Seek(tvfFPStart);
                this.unicodeSortOrder = unicodeSortOrder;
                ReadVectors();
                if (unicodeSortOrder)
                {
                    Array.Sort(termAndPostings, new ComparatorAnonymousInnerClassHelper(this));
                }
            }

            private class ComparatorAnonymousInnerClassHelper : IComparer<TermAndPostings>
            {
                private readonly TVTermsEnum outerInstance;

                public ComparatorAnonymousInnerClassHelper(TVTermsEnum outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public virtual int Compare(TermAndPostings left, TermAndPostings right)
                {
                    return left.Term.CompareTo(right.Term);
                }
            }

            private void ReadVectors()
            {
                termAndPostings = new TermAndPostings[numTerms];
                BytesRef lastTerm = new BytesRef();
                for (int i = 0; i < numTerms; i++)
                {
                    TermAndPostings t = new TermAndPostings();
                    BytesRef term = new BytesRef();
                    term.CopyBytes(lastTerm);
                    int start = tvf.ReadVInt();
                    int deltaLen = tvf.ReadVInt();
                    term.Length = start + deltaLen;
                    term.Grow(term.Length);
                    tvf.ReadBytes(term.Bytes, start, deltaLen);
                    t.Term = term;
                    int freq = tvf.ReadVInt();
                    t.Freq = freq;

                    if (storePositions)
                    {
                        int[] positions = new int[freq];
                        int pos = 0;
                        for (int posUpto = 0; posUpto < freq; posUpto++)
                        {
                            int delta = tvf.ReadVInt();
                            if (delta == -1)
                            {
                                delta = 0; // LUCENE-1542 correction
                            }
                            pos += delta;
                            positions[posUpto] = pos;
                        }
                        t.Positions = positions;
                    }

                    if (storeOffsets)
                    {
                        int[] startOffsets = new int[freq];
                        int[] endOffsets = new int[freq];
                        int offset = 0;
                        for (int posUpto = 0; posUpto < freq; posUpto++)
                        {
                            startOffsets[posUpto] = offset + tvf.ReadVInt();
                            offset = endOffsets[posUpto] = startOffsets[posUpto] + tvf.ReadVInt();
                        }
                        t.StartOffsets = startOffsets;
                        t.EndOffsets = endOffsets;
                    }
                    lastTerm.CopyBytes(term);
                    termAndPostings[i] = t;
                }
            }

            // NOTE: slow!  (linear scan)
            public override SeekStatus SeekCeil(BytesRef text)
            {
                IComparer<BytesRef> comparator = Comparator;
                for (int i = 0; i < numTerms; i++)
                {
                    int cmp = comparator.Compare(text, termAndPostings[i].Term);
                    if (cmp < 0)
                    {
                        currentTerm = i;
                        return SeekStatus.NOT_FOUND;
                    }
                    else if (cmp == 0)
                    {
                        currentTerm = i;
                        return SeekStatus.FOUND;
                    }
                }
                currentTerm = termAndPostings.Length;
                return SeekStatus.END;
            }

            public override void SeekExact(long ord)
            {
                throw new System.NotSupportedException();
            }

            public override BytesRef Next()
            {
                if (++currentTerm >= numTerms)
                {
                    return null;
                }
                return Term;
            }

            public override BytesRef Term
            {
                get { return termAndPostings[currentTerm].Term; }
            }

            public override long Ord
            {
                get { throw new System.NotSupportedException(); }
            }

            public override int DocFreq
            {
                get { return 1; }
            }

            public override long TotalTermFreq
            {
                get { return termAndPostings[currentTerm].Freq; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags) // ignored
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
                docsEnum.Reset(liveDocs, termAndPostings[currentTerm]);
                return docsEnum;
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                if (!storePositions && !storeOffsets)
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
                docsAndPositionsEnum.Reset(liveDocs, termAndPostings[currentTerm]);
                return docsAndPositionsEnum;
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    if (unicodeSortOrder)
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
            internal bool didNext;
            internal int doc = -1;
            internal int freq;
            internal IBits liveDocs;

            public override int Freq
            {
                get { return freq; }
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                if (!didNext && (liveDocs == null || liveDocs.Get(0)))
                {
                    didNext = true;
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                if (!didNext && target == 0)
                {
                    return NextDoc();
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            public virtual void Reset(IBits liveDocs, TermAndPostings termAndPostings)
            {
                this.liveDocs = liveDocs;
                this.freq = termAndPostings.Freq;
                this.doc = -1;
                didNext = false;
            }

            public override long Cost()
            {
                return 1;
            }
        }

        private class TVDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private bool didNext;
            private int doc = -1;
            private int nextPos;
            private IBits liveDocs;
            private int[] positions;
            private int[] startOffsets;
            private int[] endOffsets;

            public override int Freq
            {
                get
                {
                    if (positions != null)
                    {
                        return positions.Length;
                    }
                    else
                    {
                        Debug.Assert(startOffsets != null);
                        return startOffsets.Length;
                    }
                }
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                if (!didNext && (liveDocs == null || liveDocs.Get(0)))
                {
                    didNext = true;
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                if (!didNext && target == 0)
                {
                    return NextDoc();
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            public virtual void Reset(IBits liveDocs, TermAndPostings termAndPostings)
            {
                this.liveDocs = liveDocs;
                this.positions = termAndPostings.Positions;
                this.startOffsets = termAndPostings.StartOffsets;
                this.endOffsets = termAndPostings.EndOffsets;
                this.doc = -1;
                didNext = false;
                nextPos = 0;
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
                Debug.Assert((positions != null && nextPos < positions.Length) || startOffsets != null && nextPos < startOffsets.Length);

                if (positions != null)
                {
                    return positions[nextPos++];
                }
                else
                {
                    nextPos++;
                    return -1;
                }
            }

            public override int StartOffset
            {
                get
                {
                    if (startOffsets != null)
                    {
                        return startOffsets[nextPos - 1];
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
                    if (endOffsets != null)
                    {
                        return endOffsets[nextPos - 1];
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
            if (tvx != null)
            {
                Fields fields = new TVFields(this, docID);
                if (fields.Count == 0)
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
            if (tvx != null && tvd != null && tvf != null)
            {
                cloneTvx = (IndexInput)tvx.Clone();
                cloneTvd = (IndexInput)tvd.Clone();
                cloneTvf = (IndexInput)tvf.Clone();
            }

            return new Lucene3xTermVectorsReader(fieldInfos, cloneTvx, cloneTvd, cloneTvf, size, numTotalDocs, docStoreOffset, format);
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