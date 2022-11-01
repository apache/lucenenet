using J2N.Collections.Generic.Extensions;
using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Index.Memory
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

            public override IBits LiveDocs => null;

            public override FieldInfos FieldInfos => new FieldInfos(outerInstance.fieldInfos.Values.ToArray(/*new FieldInfo[outerInstance.fieldInfos.Count]*/));

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
                    return new EnumeratorAnonymousClass(this);
                }

                private sealed class EnumeratorAnonymousClass : IEnumerator<string>
                {
                    private readonly MemoryFields outerInstance;

                    public EnumeratorAnonymousClass(MemoryFields outerInstance)
                    {
                        this.outerInstance = outerInstance;
                        upto = -1;
                    }

                    internal int upto;
                    private string current;

                    public string Current => this.current;

                    object IEnumerator.Current => Current;

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
                        throw UnsupportedOperationException.Create();
                    }
                }

                public override Terms GetTerms(string field)
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

                        return new TermsAnonymousClass(this, info);
                    }
                }

                private sealed class TermsAnonymousClass : Terms
                {
                    private readonly MemoryFields outerInstance;

                    private readonly MemoryIndex.Info info;

                    public TermsAnonymousClass(MemoryFields outerInstance, MemoryIndex.Info info)
                    {
                        this.outerInstance = outerInstance;
                        this.info = info;
                    }

                    public override TermsEnum GetEnumerator()
                    {
                        return new MemoryTermsEnum(outerInstance.outerInstance, info);
                    }

                    public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                    public override long Count => info.terms.Count;

                    public override long SumTotalTermFreq => info.SumTotalTermFreq;

                    public override long SumDocFreq =>
                        // each term has df=1
                        info.terms.Count;

                    public override int DocCount => info.terms.Count > 0 ? 1 : 0;

                    public override bool HasFreqs => true;

                    public override bool HasOffsets => outerInstance.outerInstance.outerInstance.storeOffsets;

                    public override bool HasPositions => true;

                    public override bool HasPayloads => false;
                }

                public override int Count => outerInstance.outerInstance.sortedFields.Length;
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

                internal static int BinarySearch(BytesRef b, BytesRef bytesRef, int low, int high, BytesRefHash hash, int[] ords, IComparer<BytesRef> comparer) // LUCENENET: CA1822: Mark members as static
                {
                    int mid; // LUCENENET: IDE0059: Remove unnecessary value assignment
                    while (low <= high)
                    {
                        mid = (low + high).TripleShift(1);
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(comparer.Compare(bytesRef, b) != 0);
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(ord < info.terms.Count);
                    termUpto = (int)ord;
                }

                public override bool MoveNext()
                {
                    termUpto++;
                    if (termUpto >= info.terms.Count)
                    {
                        return false;
                    }
                    else
                    {
                        info.terms.Get(info.sortedTerms[termUpto], br);
                        return br != null;
                    }
                }

                [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                public override BytesRef Next()
                {
                    if (MoveNext())
                        return br;
                    return null;
                }

                public override BytesRef Term => br;

                public override long Ord => termUpto;

                public override int DocFreq => 1;

                public override long TotalTermFreq => info.sliceArray.freq[info.sortedTerms[termUpto]];

                public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
                {
                    if (reuse is null || !(reuse is MemoryDocsEnum toReuse))
                        toReuse = new MemoryDocsEnum();

                    return toReuse.Reset(liveDocs, info.sliceArray.freq[info.sortedTerms[termUpto]]);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
                {
                    if (reuse is null || !(reuse is MemoryDocsAndPositionsEnum toReuse))
                        toReuse = new MemoryDocsAndPositionsEnum(outerInstance);

                    int ord = info.sortedTerms[termUpto];
                    return toReuse.Reset(liveDocs, info.sliceArray.start[ord], info.sliceArray.end[ord], info.sliceArray.freq[ord]);
                }

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                public override void SeekExact(BytesRef term, TermState state)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(state != null);
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
                public MemoryDocsEnum()
                {
                }

                internal bool hasNext;
                internal IBits liveDocs;
                internal int doc = -1;
                internal int freq;

                public virtual DocsEnum Reset(IBits liveDocs, int freq)
                {
                    this.liveDocs = liveDocs;
                    hasNext = true;
                    doc = -1;
                    this.freq = freq;
                    return this;
                }

                public override int DocID => doc;

                public override int NextDoc()
                {
                    if (hasNext && (liveDocs is null || liveDocs.Get(0)))
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

                public override int Freq => freq;

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
                internal Int32BlockPool.SliceReader sliceReader;
                internal int freq;
                internal int startOffset;
                internal int endOffset;

                public MemoryDocsAndPositionsEnum(MemoryIndex.MemoryIndexReader outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.sliceReader = new Int32BlockPool.SliceReader(outerInstance.outerInstance.intBlockPool);
                }

                public virtual DocsAndPositionsEnum Reset(IBits liveDocs, int start, int end, int freq)
                {
                    this.liveDocs = liveDocs;
                    this.sliceReader.Reset(start, end);
                    posUpto = 0; // for assert
                    hasNext = true;
                    doc = -1;
                    this.freq = freq;
                    return this;
                }


                public override int DocID => doc;

                public override int NextDoc()
                {
                    if (hasNext && (liveDocs is null || liveDocs.Get(0)))
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

                public override int Freq => freq;

                public override int NextPosition()
                {
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(posUpto++ < freq);
                        Debugging.Assert(!sliceReader.IsEndOfSlice, " stores offsets : {0}", startOffset);
                    }
                    if (outerInstance.outerInstance.storeOffsets)
                    {
                        int pos = sliceReader.ReadInt32();
                        startOffset = sliceReader.ReadInt32();
                        endOffset = sliceReader.ReadInt32();
                        return pos;
                    }
                    else
                    {
                        return sliceReader.ReadInt32();
                    }
                }

                public override int StartOffset => startOffset;

                public override int EndOffset => endOffset;

                public override BytesRef GetPayload()
                {
                    return null;
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
                get => this.searcher; // LUCENENET specific: added getter per MSDN guidelines
                set => this.searcher = value;
            }

            [SuppressMessage("Style", "IDE0025:Use expression body for properties", Justification = "Multiple lines")]
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

            [SuppressMessage("Style", "IDE0025:Use expression body for properties", Justification = "Multiple lines")]
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
            protected internal override void DoClose()
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
                if (!outerInstance.fieldInfos.TryGetValue(field, out FieldInfo fieldInfo) || fieldInfo.OmitsNorms)
                {
                    return null;
                }
                NumericDocValues norms = cachedNormValues;
                Similarity sim = Similarity;
                if (!field.Equals(cachedFieldName, StringComparison.Ordinal) || sim != cachedSimilarity) // not cached?
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
