using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Diagnostics;

namespace Lucene.Net.Index.Sorter
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
    /// An <see cref="AtomicReader"/> which supports sorting documents by a given
    /// <see cref="Sort"/>. You can use this class to sort an index as follows:
    /// 
    /// <code>
    /// IndexWriter writer; // writer to which the sorted index will be added
    /// DirectoryReader reader; // reader on the input index
    /// Sort sort; // determines how the documents are sorted
    /// AtomicReader sortingReader = SortingAtomicReader.Wrap(SlowCompositeReaderWrapper.Wrap(reader), sort);
    /// writer.AddIndexes(reader);
    /// reader.Dispose(); // alternatively, you can use a using block
    /// writer.Dispose(); // alternatively, you can use a using block
    /// </code>
    /// 
    /// @lucene.experimental
    /// </summary>
    public class SortingAtomicReader : FilterAtomicReader
    {
        private class SortingFields : FilterFields
        {
            private readonly Sorter.DocMap docMap;
            private readonly FieldInfos infos;

            public SortingFields(Fields input, FieldInfos infos, Sorter.DocMap docMap)
                : base(input)
            {
                this.docMap = docMap;
                this.infos = infos;
            }

            public override Terms GetTerms(string field)
            {
                Terms terms = this.m_input.GetTerms(field);
                if (terms is null)
                {
                    return null;
                }
                else
                {
                    return new SortingTerms(terms, infos.FieldInfo(field).IndexOptions, docMap);
                }
            }
        }

        private class SortingTerms : FilterTerms
        {
            private readonly Sorter.DocMap docMap;
            private readonly IndexOptions indexOptions;

            public SortingTerms(Terms input, IndexOptions indexOptions, Sorter.DocMap docMap) 
                : base(input)
            {
                this.docMap = docMap;
                this.indexOptions = indexOptions;
            }

            public override TermsEnum GetEnumerator()
            {
                return new SortingTermsEnum(m_input.GetEnumerator(), docMap, indexOptions);
            }

            public override TermsEnum GetEnumerator(TermsEnum reuse)
            {
                return new SortingTermsEnum(m_input.GetEnumerator(reuse), docMap, indexOptions);
            }

            public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
            {
                return new SortingTermsEnum(m_input.Intersect(compiled, startTerm), docMap, indexOptions);
            }
        }

        private class SortingTermsEnum : FilterTermsEnum
        {
            private readonly Sorter.DocMap docMap; // pkg-protected to avoid synthetic accessor methods
            private readonly IndexOptions indexOptions;

            public SortingTermsEnum(TermsEnum input, Sorter.DocMap docMap, IndexOptions indexOptions)
                : base(input)
            {
                this.docMap = docMap;
                this.indexOptions = indexOptions;
            }

            internal virtual IBits NewToOld(IBits liveDocs)
            {
                if (liveDocs is null)
                {
                    return null;
                }
                return new BitsAnonymousClass(this, liveDocs);
            }

            private sealed class BitsAnonymousClass : IBits
            {
                private readonly SortingTermsEnum outerInstance;

                private readonly IBits liveDocs;

                public BitsAnonymousClass(SortingTermsEnum outerInstance, IBits liveDocs)
                {
                    this.outerInstance = outerInstance;
                    this.liveDocs = liveDocs;
                }


                public bool Get(int index)
                {
                    return liveDocs.Get(outerInstance.docMap.OldToNew(index));
                }

                public int Length => liveDocs.Length;
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                DocsEnum inReuse;
                if (reuse != null && reuse is SortingDocsEnum wrapReuse)
                {
                    // if we're asked to reuse the given DocsEnum and it is Sorting, return
                    // the wrapped one, since some Codecs expect it.
                    inReuse = wrapReuse.Wrapped;
                }
                else
                {
                    wrapReuse = null;
                    inReuse = reuse;
                }

                DocsEnum inDocs = m_input.Docs(NewToOld(liveDocs), inReuse, flags);
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                bool withFreqs = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS) >= 0 && (flags & DocsFlags.FREQS) != 0;
                return new SortingDocsEnum(docMap.Count, wrapReuse, inDocs, withFreqs, docMap);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                DocsAndPositionsEnum inReuse;
                if (reuse != null && reuse is SortingDocsAndPositionsEnum wrapReuse)
                {
                    // if we're asked to reuse the given DocsEnum and it is Sorting, return
                    // the wrapped one, since some Codecs expect it.
                    inReuse = wrapReuse.Wrapped;
                }
                else
                {
                    wrapReuse = null;
                    inReuse = reuse;
                }

                DocsAndPositionsEnum inDocsAndPositions = m_input.DocsAndPositions(NewToOld(liveDocs), inReuse, flags);
                if (inDocsAndPositions is null)
                {
                    return null;
                }

                // we ignore the fact that offsets may be stored but not asked for,
                // since this code is expected to be used during addIndexes which will
                // ask for everything. if that assumption changes in the future, we can
                // factor in whether 'flags' says offsets are not required.
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                bool storeOffsets = IndexOptionsComparer.Default.Compare(indexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                return new SortingDocsAndPositionsEnum(docMap.Count, wrapReuse, inDocsAndPositions, docMap, storeOffsets);
            }
        }

        private class SortingBinaryDocValues : BinaryDocValues
        {
            private readonly BinaryDocValues @in;
            private readonly Sorter.DocMap docMap;

            internal SortingBinaryDocValues(BinaryDocValues input, Sorter.DocMap docMap)
            {
                this.@in = input;
                this.docMap = docMap;
            }

            public override void Get(int docID, BytesRef result)
            {
                @in.Get(docMap.NewToOld(docID), result);
            }
        }

        private class SortingNumericDocValues : NumericDocValues
        {
            private readonly NumericDocValues @in;
            private readonly Sorter.DocMap docMap;

            public SortingNumericDocValues(NumericDocValues input, Sorter.DocMap docMap)
            {
                this.@in = input;
                this.docMap = docMap;
            }

            public override long Get(int docID)
            {
                return @in.Get(docMap.NewToOld(docID));
            }
        }

        private class SortingBits : IBits
        {
            private readonly IBits @in;
            private readonly Sorter.DocMap docMap;

            public SortingBits(IBits input, Sorter.DocMap docMap)
            {
                this.@in = input;
                this.docMap = docMap;
            }

            public bool Get(int index)
            {
                return @in.Get(docMap.NewToOld(index));
            }

            public int Length => @in.Length;
        }

        private class SortingSortedDocValues : SortedDocValues
        {
            private readonly SortedDocValues @in;
            private readonly Sorter.DocMap docMap;

            internal SortingSortedDocValues(SortedDocValues input, Sorter.DocMap docMap)
            {
                this.@in = input;
                this.docMap = docMap;
            }

            public override int GetOrd(int docID)
            {
                return @in.GetOrd(docMap.NewToOld(docID));
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                @in.LookupOrd(ord, result);
            }

            public override int ValueCount => @in.ValueCount;

            public override void Get(int docID, BytesRef result)
            {
                @in.Get(docMap.NewToOld(docID), result);
            }

            public override int LookupTerm(BytesRef key)
            {
                return @in.LookupTerm(key);
            }
        }

        private class SortingSortedSetDocValues : SortedSetDocValues
        {
            private readonly SortedSetDocValues @in;
            private readonly Sorter.DocMap docMap;

            internal SortingSortedSetDocValues(SortedSetDocValues input, Sorter.DocMap docMap)
            {
                this.@in = input;
                this.docMap = docMap;
            }

            public override long NextOrd()
            {
                return @in.NextOrd();
            }

            public override void SetDocument(int docID)
            {
                @in.SetDocument(docMap.NewToOld(docID));
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                @in.LookupOrd(ord, result);
            }

            public override long ValueCount => @in.ValueCount;

            public override long LookupTerm(BytesRef key)
            {
                return @in.LookupTerm(key);
            }
        }

        internal class SortingDocsEnum : FilterDocsEnum
        {
            private sealed class DocFreqSorter : TimSorter
            {
                private int[] docs;
                private int[] freqs;
                private readonly int[] tmpDocs;
                private int[] tmpFreqs;

                public DocFreqSorter(int maxDoc) 
                    : base(maxDoc / 64)
                {
                    this.tmpDocs = new int[maxDoc / 64];
                }

                public void Reset(int[] docs, int[] freqs)
                {
                    this.docs = docs;
                    this.freqs = freqs;
                    if (freqs != null && tmpFreqs is null)
                    {
                        tmpFreqs = new int[tmpDocs.Length];
                    }
                }

                protected override int Compare(int i, int j)
                {
                    return docs[i] - docs[j];
                }

                protected override void Swap(int i, int j)
                {
                    int tmpDoc = docs[i];
                    docs[i] = docs[j];
                    docs[j] = tmpDoc;

                    if (freqs != null)
                    {
                        int tmpFreq = freqs[i];
                        freqs[i] = freqs[j];
                        freqs[j] = tmpFreq;
                    }
                }

                protected override void Copy(int src, int dest)
                {
                    docs[dest] = docs[src];
                    if (freqs != null)
                    {
                        freqs[dest] = freqs[src];
                    }
                }

                protected override void Save(int i, int len)
                {
                    Arrays.Copy(docs, i, tmpDocs, 0, len);
                    if (freqs != null)
                    {
                        Arrays.Copy(freqs, i, tmpFreqs, 0, len);
                    }
                }

                protected override void Restore(int i, int j)
                {
                    docs[j] = tmpDocs[i];
                    if (freqs != null)
                    {
                        freqs[j] = tmpFreqs[i];
                    }
                }

                protected override int CompareSaved(int i, int j)
                {
                    return tmpDocs[i] - docs[j];
                }
            }

            private readonly int maxDoc;
            private readonly DocFreqSorter sorter;
            private readonly int[] docs; // LUCENENET: marked readonly
            private readonly int[] freqs; // LUCENENET: marked readonly
            private int docIt = -1;
            private readonly int upto;
            private readonly bool withFreqs;

            internal SortingDocsEnum(int maxDoc, SortingDocsEnum reuse, DocsEnum input, bool withFreqs, Sorter.DocMap docMap)
                : base(input)
            {
                this.maxDoc = maxDoc;
                this.withFreqs = withFreqs;
                if (reuse != null)
                {
                    if (reuse.maxDoc == maxDoc)
                    {
                        sorter = reuse.sorter;
                    }
                    else
                    {
                        sorter = new DocFreqSorter(maxDoc);
                    }
                    docs = reuse.docs;
                    freqs = reuse.freqs; // maybe null
                }
                else
                {
                    docs = new int[64];
                    sorter = new DocFreqSorter(maxDoc);
                }
                docIt = -1;
                int i = 0;
                int doc;
                if (withFreqs)
                {
                    if (freqs is null || freqs.Length < docs.Length)
                    {
                        freqs = new int[docs.Length];
                    }
                    while ((doc = input.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (i >= docs.Length)
                        {
                            docs = ArrayUtil.Grow(docs, docs.Length + 1);
                            freqs = ArrayUtil.Grow(freqs, freqs.Length + 1);
                        }
                        docs[i] = docMap.OldToNew(doc);
                        freqs[i] = input.Freq;
                        ++i;
                    }
                }
                else
                {
                    freqs = null;
                    while ((doc = input.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (i >= docs.Length)
                        {
                            docs = ArrayUtil.Grow(docs, docs.Length + 1);
                        }
                        docs[i++] = docMap.OldToNew(doc);
                    }
                }
                // TimSort can save much time compared to other sorts in case of
                // reverse sorting, or when sorting a concatenation of sorted readers
                sorter.Reset(docs, freqs);
                sorter.Sort(0, i);
                upto = i;
            }

            // for testing
            internal virtual bool Reused(DocsEnum other)
            {
                if (other is null || !(other is SortingDocsEnum))
                {
                    return false;
                }
                return docs == ((SortingDocsEnum)other).docs;
            }

            public override int Advance(int target)
            {
                // need to support it for checkIndex, but in practice it won't be called, so
                // don't bother to implement efficiently for now.
                return SlowAdvance(target);
            }

            public override int DocID => docIt < 0 ? -1 : docIt >= upto ? NO_MORE_DOCS : docs[docIt];

            public override int Freq => withFreqs && docIt < upto ? freqs[docIt] : 1;

            public override int NextDoc()
            {
                if (++docIt >= upto)
                {
                    return NO_MORE_DOCS;
                }
                return docs[docIt];
            }

            /// <summary>
            /// Returns the wrapped <see cref="DocsEnum"/>. </summary>
            internal virtual DocsEnum Wrapped => base.m_input;
        }

        internal class SortingDocsAndPositionsEnum : FilterDocsAndPositionsEnum
        {
            /// <summary>
            /// A <see cref="TimSorter"/> which sorts two parallel arrays of doc IDs and
            /// offsets in one go. Everytime a doc ID is 'swapped', its correponding offset
            /// is swapped too.
            /// </summary>
            internal sealed class DocOffsetSorter : TimSorter
            {
                private int[] docs;
                private long[] offsets;
                private readonly int[] tmpDocs;
                private readonly long[] tmpOffsets;

                public DocOffsetSorter(int maxDoc) 
                    : base(maxDoc / 64)
                {
                    this.tmpDocs = new int[maxDoc / 64];
                    this.tmpOffsets = new long[maxDoc / 64];
                }

                public void Reset(int[] docs, long[] offsets)
                {
                    this.docs = docs;
                    this.offsets = offsets;
                }

                protected override int Compare(int i, int j)
                {
                    return docs[i] - docs[j];
                }

                protected override void Swap(int i, int j)
                {
                    int tmpDoc = docs[i];
                    docs[i] = docs[j];
                    docs[j] = tmpDoc;

                    long tmpOffset = offsets[i];
                    offsets[i] = offsets[j];
                    offsets[j] = tmpOffset;
                }

                protected override void Copy(int src, int dest)
                {
                    docs[dest] = docs[src];
                    offsets[dest] = offsets[src];
                }

                protected override void Save(int i, int len)
                {
                    Arrays.Copy(docs, i, tmpDocs, 0, len);
                    Arrays.Copy(offsets, i, tmpOffsets, 0, len);
                }

                protected override void Restore(int i, int j)
                {
                    docs[j] = tmpDocs[i];
                    offsets[j] = tmpOffsets[i];
                }

                protected override int CompareSaved(int i, int j)
                {
                    return tmpDocs[i] - docs[j];
                }
            }

            private readonly int maxDoc;
            private readonly DocOffsetSorter sorter;
            private readonly int[] docs; // LUCENENET: marked readonly
            private readonly long[] offsets; // LUCENENET: marked readonly
            private readonly int upto;

            private readonly IndexInput postingInput;
            private readonly bool storeOffsets;

            private int docIt = -1;
            private int pos;
            private int startOffset = -1;
            private int endOffset = -1;
            private readonly BytesRef payload;
            private int currFreq;

            private readonly RAMFile file;

            internal SortingDocsAndPositionsEnum(int maxDoc, SortingDocsAndPositionsEnum reuse, DocsAndPositionsEnum @in, Sorter.DocMap docMap, bool storeOffsets)
                : base(@in)
            {
                this.maxDoc = maxDoc;
                this.storeOffsets = storeOffsets;
                if (reuse != null)
                {
                    docs = reuse.docs;
                    offsets = reuse.offsets;
                    payload = reuse.payload;
                    file = reuse.file;
                    if (reuse.maxDoc == maxDoc)
                    {
                        sorter = reuse.sorter;
                    }
                    else
                    {
                        sorter = new DocOffsetSorter(maxDoc);
                    }
                }
                else
                {
                    docs = new int[32];
                    offsets = new long[32];
                    payload = new BytesRef(32);
                    file = new RAMFile();
                    sorter = new DocOffsetSorter(maxDoc);
                }
                using (IndexOutput @out = new RAMOutputStream(file))
                {
                    int doc;
                    int i = 0;
                    while ((doc = @in.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (i == docs.Length)
                        {
                            int newLength = ArrayUtil.Oversize(i + 1, 4);
                            docs = Arrays.CopyOf(docs, newLength);
                            offsets = Arrays.CopyOf(offsets, newLength);
                        }
                        docs[i] = docMap.OldToNew(doc);
                        offsets[i] = @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        AddPositions(@in, @out);
                        i++;
                    }
                    upto = i;
                    sorter.Reset(docs, offsets);
                    sorter.Sort(0, upto);
                }
                this.postingInput = new RAMInputStream("", file);
            }

            // for testing
            internal virtual bool Reused(DocsAndPositionsEnum other)
            {
                if (other is null || !(other is SortingDocsAndPositionsEnum))
                {
                    return false;
                }
                return docs == ((SortingDocsAndPositionsEnum)other).docs;
            }

            private void AddPositions(DocsAndPositionsEnum @in, IndexOutput @out)
            {
                int freq = @in.Freq;
                @out.WriteVInt32(freq);
                int previousPosition = 0;
                int previousEndOffset = 0;
                for (int i = 0; i < freq; i++)
                {
                    int pos = @in.NextPosition();
                    BytesRef payload = @in.GetPayload();
                    // The low-order bit of token is set only if there is a payload, the
                    // previous bits are the delta-encoded position. 
                    int token = (pos - previousPosition) << 1 | (payload is null ? 0 : 1);
                    @out.WriteVInt32(token);
                    previousPosition = pos;
                    if (storeOffsets) // don't encode offsets if they are not stored
                    {
                        int startOffset = @in.StartOffset;
                        int endOffset = @in.EndOffset;
                        @out.WriteVInt32(startOffset - previousEndOffset);
                        @out.WriteVInt32(endOffset - startOffset);
                        previousEndOffset = endOffset;
                    }
                    if (payload != null)
                    {
                        @out.WriteVInt32(payload.Length);
                        @out.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
                    }
                }
            }

            public override int Advance(int target)
            {
                // need to support it for checkIndex, but in practice it won't be called, so
                // don't bother to implement efficiently for now.
                return SlowAdvance(target);
            }

            public override int DocID => docIt < 0 ? -1 : docIt >= upto ? NO_MORE_DOCS : docs[docIt];

            public override int EndOffset => endOffset;

            public override int Freq => currFreq;

            public override BytesRef GetPayload()
            {
                return payload.Length == 0 ? null : payload;
            }

            public override int NextDoc()
            {
                if (++docIt >= upto)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                postingInput.Seek(offsets[docIt]);
                currFreq = postingInput.ReadVInt32();
                // reset variables used in nextPosition
                pos = 0;
                endOffset = 0;
                return docs[docIt];
            }

            public override int NextPosition()
            {
                int token = postingInput.ReadVInt32();
                pos += token.TripleShift(1);
                if (storeOffsets)
                {
                    startOffset = endOffset + postingInput.ReadVInt32();
                    endOffset = startOffset + postingInput.ReadVInt32();
                }
                if ((token & 1) != 0)
                {
                    payload.Offset = 0;
                    payload.Length = postingInput.ReadVInt32();
                    if (payload.Length > payload.Bytes.Length)
                    {
                        payload.Bytes = new byte[ArrayUtil.Oversize(payload.Length, 1)];
                    }
                    postingInput.ReadBytes(payload.Bytes, 0, payload.Length);
                }
                else
                {
                    payload.Length = 0;
                }
                return pos;
            }

            public override int StartOffset => startOffset;

            /// <summary>
            /// Returns the wrapped <see cref="DocsAndPositionsEnum"/>.
            /// </summary>
            internal virtual DocsAndPositionsEnum Wrapped => m_input;
        }

        /// <summary>
        /// Return a sorted view of <paramref name="reader"/> according to the order
        /// defined by <paramref name="sort"/>. If the reader is already sorted, this
        /// method might return the reader as-is. 
        /// </summary>
        public static AtomicReader Wrap(AtomicReader reader, Sort sort)
        {
            return Wrap(reader, new Sorter(sort).Sort(reader));
        }

        /// <summary>
        /// Expert: same as <see cref="Wrap(AtomicReader, Sort)"/> but operates directly on a <see cref="Sorter.DocMap"/>.
        /// </summary>
        internal static AtomicReader Wrap(AtomicReader reader, Sorter.DocMap docMap)
        {
            if (docMap is null)
            {
                // the reader is already sorter
                return reader;
            }
            if (reader.MaxDoc != docMap.Count)
            {
                throw new ArgumentException("reader.MaxDoc should be equal to docMap.Count, got" + reader.MaxDoc + " != " + docMap.Count);
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(Sorter.IsConsistent(docMap));
            return new SortingAtomicReader(reader, docMap);
        }

        private readonly Sorter.DocMap docMap; // pkg-protected to avoid synthetic accessor methods

        private SortingAtomicReader(AtomicReader @in, Sorter.DocMap docMap) : base(@in)
        {
            this.docMap = docMap;
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            m_input.Document(docMap.NewToOld(docID), visitor);
        }

        public override Fields Fields
        {
            get
            {
                Fields fields = m_input.Fields;
                if (fields is null)
                {
                    return null;
                }
                else
                {
                    return new SortingFields(fields, m_input.FieldInfos, docMap);
                }
            }
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            BinaryDocValues oldDocValues = m_input.GetBinaryDocValues(field);
            if (oldDocValues is null)
            {
                return null;
            }
            else
            {
                return new SortingBinaryDocValues(oldDocValues, docMap);
            }
        }

        public override IBits LiveDocs
        {
            get
            {
                IBits inLiveDocs = m_input.LiveDocs;
                if (inLiveDocs is null)
                {
                    return null;
                }
                else
                {
                    return new SortingBits(inLiveDocs, docMap);
                }
            }
        }

        public override NumericDocValues GetNormValues(string field)
        {
            NumericDocValues norm = m_input.GetNormValues(field);
            if (norm is null)
            {
                return null;
            }
            else
            {
                return new SortingNumericDocValues(norm, docMap);
            }
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            NumericDocValues oldDocValues = m_input.GetNumericDocValues(field);
            if (oldDocValues is null)
            {
                return null;
            }
            return new SortingNumericDocValues(oldDocValues, docMap);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            SortedDocValues sortedDV = m_input.GetSortedDocValues(field);
            if (sortedDV is null)
            {
                return null;
            }
            else
            {
                return new SortingSortedDocValues(sortedDV, docMap);
            }
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            SortedSetDocValues sortedSetDV = m_input.GetSortedSetDocValues(field);
            if (sortedSetDV is null)
            {
                return null;
            }
            else
            {
                return new SortingSortedSetDocValues(sortedSetDV, docMap);
            }
        }

        public override IBits GetDocsWithField(string field)
        {
            IBits bits = m_input.GetDocsWithField(field);
            if (bits is null || bits is Bits.MatchAllBits || bits is Bits.MatchNoBits)
            {
                return bits;
            }
            else
            {
                return new SortingBits(bits, docMap);
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            return m_input.GetTermVectors(docMap.NewToOld(docID));
        }
    }
}