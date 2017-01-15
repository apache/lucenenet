using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Index.Memory
{
    public partial class MemoryIndex
    {
        ///////////////////////////////////////////////////////////////////////////////
        // Nested classes:
        ///////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Search support for Lucene framework integration; implements all methods
        /// required by the Lucene IndexReader contracts.
        /// </summary>
        private sealed class MemoryIndexReader : AtomicReader
        {
            private readonly MemoryIndex outerInstance;


            internal IndexSearcher searcher; // needed to find searcher.getSimilarity()

            internal MemoryIndexReader(MemoryIndex outerInstance)
                    : base() // avoid as much superclass baggage as possible
            {
                this.outerInstance = outerInstance;
            }

            internal Info GetInfo(string fieldName)
            {
                return outerInstance.fields[fieldName];
            }

            internal Info GetInfo(int pos)
            {
                return outerInstance.sortedFields[pos].Value;
            }

            public override IBits LiveDocs
            {
                get
                {
                    return null;
                }
            }

            public override FieldInfos FieldInfos
            {
                get
                {
                    return new FieldInfos(outerInstance.fieldInfos.Values.ToArray(/*new FieldInfo[outerInstance.fieldInfos.Count]*/));
                }
            }

            public override NumericDocValues GetNumericDocValues(string field)
            {
                return null;
            }

            public override BinaryDocValues GetBinaryDocValues(string field)
            {
                return null;
            }

            public override SortedDocValues GetSortedDocValues(string field)
            {
                return null;
            }

            public override SortedSetDocValues GetSortedSetDocValues(string field)
            {
                return null;
            }

            public override IBits GetDocsWithField(string field)
            {
                return null;
            }

            public override void CheckIntegrity()
            {
                // no-op
            }

            private class MemoryFields : Fields
            {
                private readonly MemoryIndex.MemoryIndexReader outerInstance;

                public MemoryFields(MemoryIndex.MemoryIndexReader outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override IEnumerator<string> GetEnumerator()
                {
                    return new IteratorAnonymousInnerClassHelper(this);
                }

                private class IteratorAnonymousInnerClassHelper : IEnumerator<string>
                {
                    private readonly MemoryFields outerInstance;

                    public IteratorAnonymousInnerClassHelper(MemoryFields outerInstance)
                    {
                        this.outerInstance = outerInstance;
                        upto = -1;
                    }

                    internal int upto;
                    private string current;

                    public string Current
                    {
                        get
                        {
                            return this.current;
                        }
                    }

                    object IEnumerator.Current
                    {
                        get
                        {
                            return Current;
                        }
                    }

                    public void Dispose()
                    {
                        // Nothing to do
                    }

                    public bool MoveNext()
                    {
                        if (upto + 1 >= outerInstance.outerInstance.outerInstance.sortedFields.Length)
                        {
                            return false;
                        }
                        upto++;
                        current = outerInstance.outerInstance.outerInstance.sortedFields[upto].Key;
                        return true;
                    }

                    public void Reset()
                    {
                        throw new NotSupportedException();
                    }
                }

                public override Terms Terms(string field)
                {
                    var searchField = new KeyValuePair<string, Info>(field, null);
                    int i = Array.BinarySearch(outerInstance.outerInstance.sortedFields, searchField, new TermComparer<string, Info>());
                    if (i < 0)
                    {
                        return null;
                    }
                    else
                    {
                        Info info = outerInstance.GetInfo(i);
                        info.SortTerms();

                        return new TermsAnonymousInnerClassHelper(this, info);
                    }
                }

                private class TermsAnonymousInnerClassHelper : Terms
                {
                    private readonly MemoryFields outerInstance;

                    private MemoryIndex.Info info;

                    public TermsAnonymousInnerClassHelper(MemoryFields outerInstance, MemoryIndex.Info info)
                    {
                        this.outerInstance = outerInstance;
                        this.info = info;
                    }

                    public override TermsEnum Iterator(TermsEnum reuse)
                    {
                        return new MemoryTermsEnum(outerInstance.outerInstance, info);
                    }

                    public override IComparer<BytesRef> Comparer
                    {
                        get
                        {
                            return BytesRef.UTF8SortedAsUnicodeComparer;
                        }
                    }

                    public override long Count
                    {
                        get { return info.terms.Count; }
                    }

                    public override long SumTotalTermFreq
                    {
                        get
                        {
                            return info.SumTotalTermFreq;
                        }
                    }

                    public override long SumDocFreq
                    {
                        get
                        {
                            // each term has df=1
                            return info.terms.Count;
                        }
                    }

                    public override int DocCount
                    {
                        get
                        {
                            return info.terms.Count > 0 ? 1 : 0;
                        }
                    }

                    public override bool HasFreqs
                    {
                        get { return true; }
                    }

                    public override bool HasOffsets
                    {
                        get { return outerInstance.outerInstance.outerInstance.storeOffsets; }
                    }

                    public override bool HasPositions
                    {
                        get { return true; }
                    }

                    public override bool HasPayloads
                    {
                        get { return false; }
                    }
                }

                public override int Count
                {
                    get { return outerInstance.outerInstance.sortedFields.Length; }
                }
            }

            public override Fields Fields
            {
                get
                {
                    outerInstance.SortFields();
                    return new MemoryFields(this);
                }
            }

            private class MemoryTermsEnum : TermsEnum
            {
                private readonly MemoryIndex.MemoryIndexReader outerInstance;

                internal readonly Info info;
                internal readonly BytesRef br = new BytesRef();
                internal int termUpto = -1;

                public MemoryTermsEnum(MemoryIndex.MemoryIndexReader outerInstance, Info info)
                {
                    this.outerInstance = outerInstance;
                    this.info = info;
                    info.SortTerms();
                }

                internal int BinarySearch(BytesRef b, BytesRef bytesRef, int low, int high, BytesRefHash hash, int[] ords, IComparer<BytesRef> comparer)
                {
                    int mid = 0;
                    while (low <= high)
                    {
                        mid = (int)((uint)(low + high) >> 1);
                        hash.Get(ords[mid], bytesRef);
                        int cmp = comparer.Compare(bytesRef, b);
                        if (cmp < 0)
                        {
                            low = mid + 1;
                        }
                        else if (cmp > 0)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            return mid;
                        }
                    }
                    Debug.Assert(comparer.Compare(bytesRef, b) != 0);
                    return -(low + 1);
                }


                public override bool SeekExact(BytesRef text)
                {
                    termUpto = BinarySearch(text, br, 0, info.terms.Count - 1, info.terms, info.sortedTerms, BytesRef.UTF8SortedAsUnicodeComparer);
                    return termUpto >= 0;
                }

                public override SeekStatus SeekCeil(BytesRef text)
                {
                    termUpto = BinarySearch(text, br, 0, info.terms.Count - 1, info.terms, info.sortedTerms, BytesRef.UTF8SortedAsUnicodeComparer);
                    if (termUpto < 0) // not found; choose successor
                    {
                        termUpto = -termUpto - 1;
                        if (termUpto >= info.terms.Count)
                        {
                            return SeekStatus.END;
                        }
                        else
                        {
                            info.terms.Get(info.sortedTerms[termUpto], br);
                            return SeekStatus.NOT_FOUND;
                        }
                    }
                    else
                    {
                        return SeekStatus.FOUND;
                    }
                }

                public override void SeekExact(long ord)
                {
                    Debug.Assert(ord < info.terms.Count);
                    termUpto = (int)ord;
                }

                public override BytesRef Next()
                {
                    termUpto++;
                    if (termUpto >= info.terms.Count)
                    {
                        return null;
                    }
                    else
                    {
                        info.terms.Get(info.sortedTerms[termUpto], br);
                        return br;
                    }
                }

                public override BytesRef Term
                {
                    get { return br; }
                }

                public override long Ord
                {
                    get { return termUpto; }
                }

                public override int DocFreq
                {
                    get { return 1; }
                }

                public override long TotalTermFreq
                {
                    get { return info.sliceArray.freq[info.sortedTerms[termUpto]]; }
                }

                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
                {
                    if (reuse == null || !(reuse is MemoryDocsEnum))
                    {
                        reuse = new MemoryDocsEnum(outerInstance);
                    }
                    return ((MemoryDocsEnum)reuse).Reset(liveDocs, info.sliceArray.freq[info.sortedTerms[termUpto]]);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
                {
                    if (reuse == null || !(reuse is MemoryDocsAndPositionsEnum))
                    {
                        reuse = new MemoryDocsAndPositionsEnum(outerInstance);
                    }
                    int ord = info.sortedTerms[termUpto];
                    return ((MemoryDocsAndPositionsEnum)reuse).Reset(liveDocs, info.sliceArray.start[ord], info.sliceArray.end[ord], info.sliceArray.freq[ord]);
                }

                public override IComparer<BytesRef> Comparer
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                }

                public override void SeekExact(BytesRef term, TermState state)
                {
                    Debug.Assert(state != null);
                    this.SeekExact(((OrdTermState)state).Ord);
                }

                public override TermState GetTermState()
                {
                    OrdTermState ts = new OrdTermState();
                    ts.Ord = termUpto;
                    return ts;
                }
            }

            private class MemoryDocsEnum : DocsEnum
            {
                private readonly MemoryIndex.MemoryIndexReader outerInstance;

                public MemoryDocsEnum(MemoryIndex.MemoryIndexReader outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                internal bool hasNext;
                internal IBits liveDocs;
                internal int doc = -1;
                internal int freq_Renamed;

                public virtual DocsEnum Reset(IBits liveDocs, int freq)
                {
                    this.liveDocs = liveDocs;
                    hasNext = true;
                    doc = -1;
                    this.freq_Renamed = freq;
                    return this;
                }

                public override int DocID
                {
                    get { return doc; }
                }

                public override int NextDoc()
                {
                    if (hasNext && (liveDocs == null || liveDocs.Get(0)))
                    {
                        hasNext = false;
                        return doc = 0;
                    }
                    else
                    {
                        return doc = NO_MORE_DOCS;
                    }
                }

                public override int Advance(int target)
                {
                    return SlowAdvance(target);
                }

                public override int Freq
                {
                    get { return freq_Renamed; }
                }

                public override long GetCost()
                {
                    return 1;
                }
            }

            private class MemoryDocsAndPositionsEnum : DocsAndPositionsEnum
            {
                private readonly MemoryIndex.MemoryIndexReader outerInstance;

                internal int posUpto; // for assert
                internal bool hasNext;
                internal IBits liveDocs;
                internal int doc = -1;
                internal IntBlockPool.SliceReader sliceReader;
                internal int freq_Renamed;
                internal int startOffset_Renamed;
                internal int endOffset_Renamed;

                public MemoryDocsAndPositionsEnum(MemoryIndex.MemoryIndexReader outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.sliceReader = new IntBlockPool.SliceReader(outerInstance.outerInstance.intBlockPool);
                }

                public virtual DocsAndPositionsEnum Reset(IBits liveDocs, int start, int end, int freq)
                {
                    this.liveDocs = liveDocs;
                    this.sliceReader.Reset(start, end);
                    posUpto = 0; // for assert
                    hasNext = true;
                    doc = -1;
                    this.freq_Renamed = freq;
                    return this;
                }


                public override int DocID
                {
                    get { return doc; }
                }

                public override int NextDoc()
                {
                    if (hasNext && (liveDocs == null || liveDocs.Get(0)))
                    {
                        hasNext = false;
                        return doc = 0;
                    }
                    else
                    {
                        return doc = NO_MORE_DOCS;
                    }
                }

                public override int Advance(int target)
                {
                    return SlowAdvance(target);
                }

                public override int Freq
                {
                    get { return freq_Renamed; }
                }

                public override int NextPosition()
                {
                    Debug.Assert(posUpto++ < freq_Renamed);
                    Debug.Assert(!sliceReader.EndOfSlice(), " stores offsets : " + startOffset_Renamed);
                    if (outerInstance.outerInstance.storeOffsets)
                    {
                        int pos = sliceReader.ReadInt();
                        startOffset_Renamed = sliceReader.ReadInt();
                        endOffset_Renamed = sliceReader.ReadInt();
                        return pos;
                    }
                    else
                    {
                        return sliceReader.ReadInt();
                    }
                }

                public override int StartOffset
                {
                    get { return startOffset_Renamed; }
                }

                public override int EndOffset
                {
                    get { return endOffset_Renamed; }
                }

                public override BytesRef Payload
                {
                    get
                    {
                        return null;
                    }
                }

                public override long GetCost()
                {
                    return 1;
                }
            }

            public override Fields GetTermVectors(int docID)
            {
                if (docID == 0)
                {
                    return Fields;
                }
                else
                {
                    return null;
                }
            }

            internal Similarity Similarity
            {
                get
                {
                    if (searcher != null)
                    {
                        return searcher.Similarity;
                    }
                    return IndexSearcher.DefaultSimilarity;
                }
            }

            internal IndexSearcher Searcher
            {
                set
                {
                    this.searcher = value;
                }
            }

            public override int NumDocs
            {
                get
                {
#if DEBUG
                    Debug.WriteLine("MemoryIndexReader.NumDocs");
#endif
                    return 1;
                }
            }

            public override int MaxDoc
            {
                get
                {
#if DEBUG
                    Debug.WriteLine("MemoryIndexReader.MaxDoc");
#endif
                    return 1;
                }
            }

            public override void Document(int docID, StoredFieldVisitor visitor)
            {
#if DEBUG
                Debug.WriteLine("MemoryIndexReader.Document");
#endif
                // no-op: there are no stored fields
            }
            protected override void DoClose()
            {
#if DEBUG
                Debug.WriteLine("MemoryIndexReader.DoClose");
#endif
            }

            /// <summary>
            /// performance hack: cache norms to avoid repeated expensive calculations </summary>
            internal NumericDocValues cachedNormValues;
            internal string cachedFieldName;
            internal Similarity cachedSimilarity;

            public override NumericDocValues GetNormValues(string field)
            {
                FieldInfo fieldInfo;
                if (!outerInstance.fieldInfos.TryGetValue(field, out fieldInfo) || fieldInfo.OmitsNorms)
                {
                    return null;
                }
                NumericDocValues norms = cachedNormValues;
                Similarity sim = Similarity;
                if (!field.Equals(cachedFieldName) || sim != cachedSimilarity) // not cached?
                {
                    Info info = GetInfo(field);
                    int numTokens = info != null ? info.numTokens : 0;
                    int numOverlapTokens = info != null ? info.numOverlapTokens : 0;
                    float boost = info != null ? info.Boost : 1.0f;
                    FieldInvertState invertState = new FieldInvertState(field, 0, numTokens, numOverlapTokens, 0, boost);
                    long value = sim.ComputeNorm(invertState);
                    norms = new MemoryIndexNormDocValues(value);
                    // cache it for future reuse
                    cachedNormValues = norms;
                    cachedFieldName = field;
                    cachedSimilarity = sim;
#if DEBUG
                    Debug.WriteLine("MemoryIndexReader.norms: " + field + ":" + value + ":" + numTokens);
#endif
                }
                return norms;
            }
        }
    }
}
