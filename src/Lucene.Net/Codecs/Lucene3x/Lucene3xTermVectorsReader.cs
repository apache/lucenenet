using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using IBits = Lucene.Net.Util.IBits;
using BytesRef = Lucene.Net.Util.BytesRef;
using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
using Directory = Lucene.Net.Store.Directory;
using System.Runtime.CompilerServices;

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
        /// Extension of vectors fields file. </summary>
        public const string VECTORS_FIELDS_EXTENSION = "tvf";

        /// <summary>
        /// Extension of vectors documents file. </summary>
        public const string VECTORS_DOCUMENTS_EXTENSION = "tvd";

        /// <summary>
        /// Extension of vectors index file. </summary>
        public const string VECTORS_INDEX_EXTENSION = "tvx";

        private readonly FieldInfos fieldInfos;

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IndexInput tvx; // LUCENENET: marked readonly
        private readonly IndexInput tvd; // LUCENENET: marked readonly
        private readonly IndexInput tvf; // LUCENENET: marked readonly
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly int size; // LUCENENET: marked readonly
        private readonly int numTotalDocs; // LUCENENET: marked readonly

        // The docID offset where our docs begin in the index
        // file.  this will be 0 if we have our own private file.
        private readonly int docStoreOffset; // LUCENENET: marked readonly

        // when we are inside a compound share doc store (CFX),
        // (lucene 3.0 indexes only), we privately open our own fd.
        // TODO: if we are worried, maybe we could eliminate the
        // extra fd somehow when you also have vectors...
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly CompoundFileDirectory storeCFSReader;
#pragma warning restore CA2213 // Disposable fields should be disposed

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

                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(format == tvdFormat);
                    Debugging.Assert(format == tvfFormat);
                }

                numTotalDocs = (int)(tvx.Length >> 4);

                if (-1 == docStoreOffset)
                {
                    this.docStoreOffset = 0;
                    this.size = numTotalDocs;
                    if (Debugging.AssertsEnabled) Debugging.Assert(size == 0 || numTotalDocs == size);
                }
                else
                {
                    this.docStoreOffset = docStoreOffset;
                    this.size = size;
                    // Verify the file is long enough to hold all of our
                    // docs
                    if (Debugging.AssertsEnabled) Debugging.Assert(numTotalDocs >= size + docStoreOffset, "numTotalDocs={0} size={1} docStoreOffset={2}", numTotalDocs, size, docStoreOffset);
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
                    catch (Exception t) when (t.IsThrowable())
                    {
                    }
                }
            }
        }

        // Not private to avoid synthetic access$NNN methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void SeekTvx(int docNum)
        {
            tvx.Seek((docNum + docStoreOffset) * 16L + FORMAT_SIZE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CheckValidFormat(IndexInput @in)
        {
            int format = @in.ReadInt32();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IOUtils.Dispose(tvx, tvd, tvf, storeCFSReader);
            }
        }

        /// <summary>
        /// The number of documents in the reader. 
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        internal virtual int Count => size;

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
                outerInstance.tvd.Seek(outerInstance.tvx.ReadInt64());

                int fieldCount = outerInstance.tvd.ReadVInt32();
                if (Debugging.AssertsEnabled) Debugging.Assert(fieldCount >= 0);
                if (fieldCount != 0)
                {
                    fieldNumbers = new int[fieldCount];
                    fieldFPs = new long[fieldCount];
                    for (int fieldUpto = 0; fieldUpto < fieldCount; fieldUpto++)
                    {
                        int fieldNumber = outerInstance.tvd.ReadVInt32();
                        fieldNumbers[fieldUpto] = fieldNumber;
                        fieldNumberToIndex[fieldNumber] = fieldUpto;
                    }

                    long position = outerInstance.tvx.ReadInt64();
                    fieldFPs[0] = position;
                    for (int fieldUpto = 1; fieldUpto < fieldCount; fieldUpto++)
                    {
                        position += outerInstance.tvd.ReadVInt64();
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override IEnumerator<string> GetEnumerator()
            {
                return new EnumeratorAnonymousClass(this);
            }

            private sealed class EnumeratorAnonymousClass : IEnumerator<string>
            {
                private readonly TVFields outerInstance;
                private string current;
                private int i;
                private readonly int upTo;

                public EnumeratorAnonymousClass(TVFields outerInstance)
                {
                    this.outerInstance = outerInstance;
                    upTo = this.outerInstance.fieldNumbers.Length;
                    i = 0;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    if (outerInstance.fieldNumbers != null && i < upTo)
                    {
                        current = outerInstance.outerInstance.fieldInfos.FieldInfo(outerInstance.fieldNumbers[i++]).Name;
                        return true;
                    }
                    return false;
                }

                public string Current => current;

                object IEnumerator.Current => Current;

                public void Reset()
                {
                    throw UnsupportedOperationException.Create();
                }

                public void Dispose()
                {
                    // LUCENENET: Intentionally blank
                }
            }

            public override Terms GetTerms(string field)
            {
                FieldInfo fieldInfo = outerInstance.fieldInfos.FieldInfo(field);
                if (fieldInfo is null)
                {
                    // No such field
                    return null;
                }

                if (!fieldNumberToIndex.TryGetValue(fieldInfo.Number, out int fieldIndex))
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
                    if (fieldNumbers is null)
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
                numTerms = outerInstance.tvf.ReadVInt32();
                byte bits = outerInstance.tvf.ReadByte();
                storePositions = (bits & STORE_POSITIONS_WITH_TERMVECTOR) != 0;
                storeOffsets = (bits & STORE_OFFSET_WITH_TERMVECTOR) != 0;
                tvfFPStart = outerInstance.tvf.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                unicodeSortOrder = outerInstance.SortTermsByUnicode();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override TermsEnum GetEnumerator()
            {
                var termsEnum = new TVTermsEnum(outerInstance);
                termsEnum.Reset(numTerms, tvfFPStart, storePositions, storeOffsets, unicodeSortOrder);
                return termsEnum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override TermsEnum GetEnumerator(TermsEnum reuse)
            {
                if (reuse is null || !(reuse is TVTermsEnum termsEnum) || !termsEnum.CanReuse(outerInstance.tvf))
                    termsEnum = new TVTermsEnum(outerInstance);

                termsEnum.Reset(numTerms, tvfFPStart, storePositions, storeOffsets, unicodeSortOrder);
                return termsEnum;
            }

            public override long Count => numTerms;

            public override long SumTotalTermFreq => -1;

            public override long SumDocFreq =>
                // Every term occurs in just one doc:
                numTerms;

            public override int DocCount => 1;

            public override IComparer<BytesRef> Comparer
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            public override bool HasFreqs => true;

            public override bool HasOffsets => storeOffsets;

            public override bool HasPositions => storePositions;

            public override bool HasPayloads => false;
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
                this.origTVF = outerInstance.tvf;
                tvf = (IndexInput)origTVF.Clone();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    Array.Sort(termAndPostings, Comparer<TermAndPostings>.Create((left, right) => left.Term.CompareTo(right.Term)));
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
                    int start = tvf.ReadVInt32();
                    int deltaLen = tvf.ReadVInt32();
                    term.Length = start + deltaLen;
                    term.Grow(term.Length);
                    tvf.ReadBytes(term.Bytes, start, deltaLen);
                    t.Term = term;
                    int freq = tvf.ReadVInt32();
                    t.Freq = freq;

                    if (storePositions)
                    {
                        int[] positions = new int[freq];
                        int pos = 0;
                        for (int posUpto = 0; posUpto < freq; posUpto++)
                        {
                            int delta = tvf.ReadVInt32();
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
                            startOffsets[posUpto] = offset + tvf.ReadVInt32();
                            offset = endOffsets[posUpto] = startOffsets[posUpto] + tvf.ReadVInt32();
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
                IComparer<BytesRef> comparer = Comparer;
                for (int i = 0; i < numTerms; i++)
                {
                    int cmp = comparer.Compare(text, termAndPostings[i].Term);
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
                throw UnsupportedOperationException.Create();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool MoveNext()
            {
                if (++currentTerm >= numTerms)
                {
                    return false;
                }
                return termAndPostings[currentTerm].Term != null;
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return Term;
                return null;
            }

            public override BytesRef Term => termAndPostings[currentTerm].Term;

            public override long Ord => throw UnsupportedOperationException.Create();

            public override int DocFreq => 1;

            public override long TotalTermFreq => termAndPostings[currentTerm].Freq;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags) // ignored
            {
                if (reuse is null || !(reuse is TVDocsEnum docsEnum))
                    docsEnum = new TVDocsEnum();

                docsEnum.Reset(liveDocs, termAndPostings[currentTerm]);
                return docsEnum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                if (!storePositions && !storeOffsets)
                {
                    return null;
                }

                if (reuse is null || !(reuse is TVDocsAndPositionsEnum docsAndPositionsEnum))
                    docsAndPositionsEnum = new TVDocsAndPositionsEnum();

                docsAndPositionsEnum.Reset(liveDocs, termAndPostings[currentTerm]);
                return docsAndPositionsEnum;
            }

            public override IComparer<BytesRef> Comparer
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            public override int Freq => freq;

            public override int DocID => doc;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int NextDoc()
            {
                if (!didNext && (liveDocs is null || liveDocs.Get(0)))
                {
                    didNext = true;
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Reset(IBits liveDocs, TermAndPostings termAndPostings)
            {
                this.liveDocs = liveDocs;
                this.freq = termAndPostings.Freq;
                this.doc = -1;
                didNext = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
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
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (positions != null)
                    {
                        return positions.Length;
                    }
                    else
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(startOffsets != null);
                        return startOffsets.Length;
                    }
                }
            }

            public override int DocID => doc;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int NextDoc()
            {
                if (!didNext && (liveDocs is null || liveDocs.Get(0)))
                {
                    didNext = true;
                    return (doc = 0);
                }
                else
                {
                    return (doc = NO_MORE_DOCS);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override BytesRef GetPayload()
            {
                return null;
            }

            public override int NextPosition()
            {
                //if (Debugging.AssertsEnabled) Debugging.Assert((positions != null && nextPos < positions.Length) || startOffsets != null && nextPos < startOffsets.Length);

                // LUCENENET: The above assertion was for control flow when testing. In Java, it would throw an AssertionError, which is
                // caught by the BaseTermVectorsFormatTestCase.assertEquals(RandomTokenStream tk, FieldType ft, Terms terms) method in the
                // part that is checking for an error after reading to the end of the enumerator.

                // In .NET it is more natural to throw an InvalidOperationException in this case, since we would potentially get an
                // IndexOutOfRangeException if we didn't, which doesn't really provide good feedback as to what the cause is.
                // This matches the behavior of Lucene 8.x. See #267.
                if (((positions != null && nextPos < positions.Length) || startOffsets != null && nextPos < startOffsets.Length) == false)
                    throw IllegalStateException.Create("Read past last position");

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
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal virtual bool SortTermsByUnicode()
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            // everything is disk-based
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CheckIntegrity()
        {
        }
    }
}