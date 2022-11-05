using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Codecs
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using IBits = Lucene.Net.Util.IBits;
    using Int64BitSet = Lucene.Net.Util.Int64BitSet;
    using MergeState = Lucene.Net.Index.MergeState;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using OrdinalMap = Lucene.Net.Index.MultiDocValues.OrdinalMap;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Abstract API that consumes numeric, binary and
    /// sorted docvalues.  Concrete implementations of this
    /// actually do "something" with the docvalues (write it into
    /// the index in a specific format).
    /// <para/>
    /// The lifecycle is:
    /// <list type="number">
    ///   <item><description>DocValuesConsumer is created by
    ///       <see cref="DocValuesFormat.FieldsConsumer(Index.SegmentWriteState)"/> or
    ///       <see cref="NormsFormat.NormsConsumer(Index.SegmentWriteState)"/>.</description></item>
    ///   <item><description><see cref="AddNumericField(FieldInfo, IEnumerable{long?})"/>, 
    ///       <see cref="AddBinaryField(FieldInfo, IEnumerable{BytesRef})"/>,
    ///       or <see cref="AddSortedField(FieldInfo, IEnumerable{BytesRef}, IEnumerable{long?})"/> are called for each Numeric,
    ///       Binary, or Sorted docvalues field. The API is a "pull" rather
    ///       than "push", and the implementation is free to iterate over the
    ///       values multiple times (<see cref="IEnumerable{T}.GetEnumerator()"/>).</description></item>
    ///   <item><description>After all fields are added, the consumer is <see cref="Dispose()"/>d.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class DocValuesConsumer : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected DocValuesConsumer()
        {
        }

        /// <summary>
        /// Writes numeric docvalues for a field. </summary>
        /// <param name="field"> Field information. </param>
        /// <param name="values"> <see cref="IEnumerable{T}"/> of numeric values (one for each document). <c>null</c> indicates
        ///               a missing value. </param>
        /// <exception cref="IOException"> If an I/O error occurred. </exception>
        public abstract void AddNumericField(FieldInfo field, IEnumerable<long?> values);

        /// <summary>
        /// Writes binary docvalues for a field. </summary>
        /// <param name="field"> Field information. </param>
        /// <param name="values"> <see cref="IEnumerable{T}"/> of binary values (one for each document). <c>null</c> indicates
        ///               a missing value. </param>
        /// <exception cref="IOException"> If an I/O error occurred. </exception>
        public abstract void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values);

        /// <summary>
        /// Writes pre-sorted binary docvalues for a field. </summary>
        /// <param name="field"> Field information. </param>
        /// <param name="values"> <see cref="IEnumerable{T}"/> of binary values in sorted order (deduplicated). </param>
        /// <param name="docToOrd"> <see cref="IEnumerable{T}"/> of ordinals (one for each document). <c>-1</c> indicates
        ///                 a missing value. </param>
        /// <exception cref="IOException"> If an I/O error occurred. </exception>
        public abstract void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd);

        /// <summary>
        /// Writes pre-sorted set docvalues for a field </summary>
        /// <param name="field"> Field information. </param>
        /// <param name="values"> <see cref="IEnumerable{T}"/> of binary values in sorted order (deduplicated). </param>
        /// <param name="docToOrdCount"> <see cref="IEnumerable{T}"/> of the number of values for each document. A zero ordinal
        ///                      count indicates a missing value. </param>
        /// <param name="ords"> <see cref="IEnumerable{T}"/> of ordinal occurrences (<paramref name="docToOrdCount"/>*maxDoc total). </param>
        /// <exception cref="IOException"> If an I/O error occurred. </exception>
        public abstract void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords);

        /// <summary>
        /// Merges the numeric docvalues from <paramref name="toMerge"/>.
        /// <para>
        /// The default implementation calls <see cref="AddNumericField(FieldInfo, IEnumerable{long?})"/>, passing
        /// an <see cref="IEnumerable{T}"/> that merges and filters deleted documents on the fly.</para>
        /// </summary>
        public virtual void MergeNumericField(FieldInfo fieldInfo, MergeState mergeState, IList<NumericDocValues> toMerge, IList<IBits> docsWithField)
        {
            AddNumericField(fieldInfo, GetMergeNumericFieldEnumerable(/* fieldInfo, // LUCENENET: Never read */ mergeState, toMerge, docsWithField));
        }

        private static IEnumerable<long?> GetMergeNumericFieldEnumerable(/*FieldInfo fieldinfo, // LUCENENET: Never read */ MergeState mergeState, IList<NumericDocValues> toMerge, IList<IBits> docsWithField) // LUCENENET: CA1822: Mark members as static
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            NumericDocValues currentValues = null;
            IBits currentLiveDocs = null;
            IBits currentDocsWithField = null;

            while (true)
            {
                if (readerUpto == toMerge.Count)
                {
                    yield break;
                }

                if (currentReader is null || docIDUpto == currentReader.MaxDoc)
                {
                    readerUpto++;
                    if (readerUpto < toMerge.Count)
                    {
                        currentReader = mergeState.Readers[readerUpto];
                        currentValues = toMerge[readerUpto];
                        currentDocsWithField = docsWithField[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                    }
                    docIDUpto = 0;
                    continue;
                }

                if (currentLiveDocs is null || currentLiveDocs.Get(docIDUpto))
                {
                    long? nextValue;
                    if (currentDocsWithField.Get(docIDUpto))
                    {
                        nextValue = currentValues.Get(docIDUpto);
                    }
                    else
                    {
                        nextValue = null;
                    }

                    docIDUpto++;
                    yield return nextValue;
                    continue;
                }

                docIDUpto++;
            }
        }

        /// <summary>
        /// Merges the binary docvalues from <paramref name="toMerge"/>.
        /// <para>
        /// The default implementation calls <see cref="AddBinaryField(FieldInfo, IEnumerable{BytesRef})"/>, passing
        /// an <see cref="IEnumerable{T}"/> that merges and filters deleted documents on the fly.</para>
        /// </summary>
        public virtual void MergeBinaryField(FieldInfo fieldInfo, MergeState mergeState, IList<BinaryDocValues> toMerge, IList<IBits> docsWithField)
        {
            AddBinaryField(fieldInfo, GetMergeBinaryFieldEnumerable(/*fieldInfo, // LUCENENET: Never read */ mergeState, toMerge, docsWithField));
        }

        private static IEnumerable<BytesRef> GetMergeBinaryFieldEnumerable(/*FieldInfo fieldInfo, // LUCENENET: Never read */ MergeState mergeState, IList<BinaryDocValues> toMerge, IList<IBits> docsWithField) // LUCENENET: CA1822: Mark members as static
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            var nextValue = new BytesRef();
            BytesRef nextPointer; // points to null if missing, or nextValue
            AtomicReader currentReader = null;
            BinaryDocValues currentValues = null;
            IBits currentLiveDocs = null;
            IBits currentDocsWithField = null;

            while (true)
            {
                if (readerUpto == toMerge.Count)
                {
                    yield break;
                }

                if (currentReader is null || docIDUpto == currentReader.MaxDoc)
                {
                    readerUpto++;
                    if (readerUpto < toMerge.Count)
                    {
                        currentReader = mergeState.Readers[readerUpto];
                        currentValues = toMerge[readerUpto];
                        currentDocsWithField = docsWithField[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                    }
                    docIDUpto = 0;
                    continue;
                }

                if (currentLiveDocs is null || currentLiveDocs.Get(docIDUpto))
                {
                    if (currentDocsWithField.Get(docIDUpto))
                    {
                        currentValues.Get(docIDUpto, nextValue);
                        nextPointer = nextValue;
                    }
                    else
                    {
                        nextPointer = null;
                    }

                    docIDUpto++;
                    yield return nextPointer;
                    continue;
                }

                docIDUpto++;
            }
        }

        /// <summary>
        /// Merges the sorted docvalues from <paramref name="toMerge"/>.
        /// <para>
        /// The default implementation calls <see cref="AddSortedField(FieldInfo, IEnumerable{BytesRef}, IEnumerable{long?})"/>, passing
        /// an <see cref="IEnumerable{T}"/> that merges ordinals and values and filters deleted documents.</para>
        /// </summary>
        public virtual void MergeSortedField(FieldInfo fieldInfo, MergeState mergeState, IList<SortedDocValues> toMerge)
        {
            AtomicReader[] readers = mergeState.Readers.ToArray();
            SortedDocValues[] dvs = toMerge.ToArray();

            // step 1: iterate thru each sub and mark terms still in use
            var liveTerms = new TermsEnum[dvs.Length];
            for (int sub = 0; sub < liveTerms.Length; sub++)
            {
                AtomicReader reader = readers[sub];
                SortedDocValues dv = dvs[sub];
                IBits liveDocs = reader.LiveDocs;
                if (liveDocs is null)
                {
                    liveTerms[sub] = dv.GetTermsEnum();
                }
                else
                {
                    var bitset = new Int64BitSet(dv.ValueCount);
                    for (int i = 0; i < reader.MaxDoc; i++)
                    {
                        if (liveDocs.Get(i))
                        {
                            int ord = dv.GetOrd(i);
                            if (ord >= 0)
                            {
                                bitset.Set(ord);
                            }
                        }
                    }
                    liveTerms[sub] = new BitsFilteredTermsEnum(dv.GetTermsEnum(), bitset);
                }
            }

            // step 2: create ordinal map (this conceptually does the "merging")
            var map = new OrdinalMap(this, liveTerms);

            // step 3: add field
            AddSortedField(fieldInfo, GetMergeSortValuesEnumerable(map, dvs),
                // doc -> ord
                GetMergeSortedFieldDocToOrdEnumerable(readers, dvs, map)
           );
        }

        private static IEnumerable<BytesRef> GetMergeSortValuesEnumerable(OrdinalMap map, SortedDocValues[] dvs) // LUCENENET: CA1822: Mark members as static
        {
            var scratch = new BytesRef();
            int currentOrd = 0;

            while (currentOrd < map.ValueCount)
            {
                int segmentNumber = map.GetFirstSegmentNumber(currentOrd);
                var segmentOrd = (int)map.GetFirstSegmentOrd(currentOrd);
                dvs[segmentNumber].LookupOrd(segmentOrd, scratch);
                currentOrd++;
                yield return scratch;
            }
        }

        private static IEnumerable<long?> GetMergeSortedFieldDocToOrdEnumerable(AtomicReader[] readers, SortedDocValues[] dvs, OrdinalMap map) // LUCENENET: CA1822: Mark members as static
        {
            int readerUpTo = -1;
            int docIDUpTo = 0;
            AtomicReader currentReader = null;
            IBits currentLiveDocs = null;

            while (true)
            {
                if (readerUpTo == readers.Length)
                {
                    yield break;
                }

                if (currentReader is null || docIDUpTo == currentReader.MaxDoc)
                {
                    readerUpTo++;
                    if (readerUpTo < readers.Length)
                    {
                        currentReader = readers[readerUpTo];
                        currentLiveDocs = currentReader.LiveDocs;
                    }
                    docIDUpTo = 0;
                    continue;
                }

                if (currentLiveDocs is null || currentLiveDocs.Get(docIDUpTo))
                {
                    int segOrd = dvs[readerUpTo].GetOrd(docIDUpTo);
                    docIDUpTo++;
                    yield return segOrd == -1 ? -1 : map.GetGlobalOrd(readerUpTo, segOrd);
                    continue;
                }

                docIDUpTo++;
            }
        }

        /// <summary>
        /// Merges the sortedset docvalues from <paramref name="toMerge"/>.
        /// <para>
        /// The default implementation calls <see cref="AddSortedSetField(FieldInfo, IEnumerable{BytesRef}, IEnumerable{long?}, IEnumerable{long?})"/>, passing
        /// an <see cref="IEnumerable{T}"/> that merges ordinals and values and filters deleted documents.</para>
        /// </summary>
        public virtual void MergeSortedSetField(FieldInfo fieldInfo, MergeState mergeState, IList<SortedSetDocValues> toMerge)
        {
            var readers = mergeState.Readers.ToArray();
            var dvs = toMerge.ToArray();

            // step 1: iterate thru each sub and mark terms still in use
            var liveTerms = new TermsEnum[dvs.Length];
            for (int sub = 0; sub < liveTerms.Length; sub++)
            {
                var reader = readers[sub];
                var dv = dvs[sub];
                var liveDocs = reader.LiveDocs;
                if (liveDocs is null)
                {
                    liveTerms[sub] = dv.GetTermsEnum();
                }
                else
                {
                    var bitset = new Int64BitSet(dv.ValueCount);
                    for (int i = 0; i < reader.MaxDoc; i++)
                    {
                        if (liveDocs.Get(i))
                        {
                            dv.SetDocument(i);
                            long ord;
                            while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                            {
                                bitset.Set(ord);
                            }
                        }
                    }
                    liveTerms[sub] = new BitsFilteredTermsEnum(dv.GetTermsEnum(), bitset);
                }
            }

            // step 2: create ordinal map (this conceptually does the "merging")
            var map = new OrdinalMap(this, liveTerms);

            // step 3: add field
            AddSortedSetField(fieldInfo, GetMergeSortedSetValuesEnumerable(map, dvs),
                // doc -> ord count
                GetMergeSortedSetDocToOrdCountEnumerable(readers, dvs),
                // ords
                GetMergeSortedSetOrdsEnumerable(readers, dvs, map)
            );
        }

        private static IEnumerable<BytesRef> GetMergeSortedSetValuesEnumerable(OrdinalMap map, SortedSetDocValues[] dvs) // LUCENENET: CA1822: Mark members as static
        {
            var scratch = new BytesRef();
            long currentOrd = 0;

            while (currentOrd < map.ValueCount)
            {
                int segmentNumber = map.GetFirstSegmentNumber(currentOrd);
                long segmentOrd = map.GetFirstSegmentOrd(currentOrd);
                dvs[segmentNumber].LookupOrd(segmentOrd, scratch);
                currentOrd++;
                yield return scratch;
            }
        }

        private static IEnumerable<long?> GetMergeSortedSetDocToOrdCountEnumerable(AtomicReader[] readers, SortedSetDocValues[] dvs) // LUCENENET: CA1822: Mark members as static
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            IBits currentLiveDocs = null;

            while (true)
            {
                if (readerUpto == readers.Length)
                {
                    yield break;
                }

                if (currentReader is null || docIDUpto == currentReader.MaxDoc)
                {
                    readerUpto++;
                    if (readerUpto < readers.Length)
                    {
                        currentReader = readers[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                    }
                    docIDUpto = 0;
                    continue;
                }

                if (currentLiveDocs is null || currentLiveDocs.Get(docIDUpto))
                {
                    SortedSetDocValues dv = dvs[readerUpto];
                    dv.SetDocument(docIDUpto);
                    long value = 0;
                    while (dv.NextOrd() != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        value++;
                    }
                    docIDUpto++;
                    yield return value;
                    continue;
                }

                docIDUpto++;
            }
        }

        private static IEnumerable<long?> GetMergeSortedSetOrdsEnumerable(AtomicReader[] readers, SortedSetDocValues[] dvs, OrdinalMap map) // LUCENENET: CA1822: Mark members as static
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            IBits currentLiveDocs = null;
            var ords = new long[8];
            int ordUpto = 0;
            int ordLength = 0;

            while (true)
            {
                if (readerUpto == readers.Length)
                {
                    yield break;
                }

                if (ordUpto < ordLength)
                {
                    var value = ords[ordUpto];
                    ordUpto++;
                    yield return value;
                    continue;
                }

                if (currentReader is null || docIDUpto == currentReader.MaxDoc)
                {
                    readerUpto++;
                    if (readerUpto < readers.Length)
                    {
                        currentReader = readers[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                    }
                    docIDUpto = 0;
                    continue;
                }

                if (currentLiveDocs is null || currentLiveDocs.Get(docIDUpto))
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(docIDUpto < currentReader.MaxDoc);
                    SortedSetDocValues dv = dvs[readerUpto];
                    dv.SetDocument(docIDUpto);
                    ordUpto = ordLength = 0;
                    long ord;
                    while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        if (ordLength == ords.Length)
                        {
                            ords = ArrayUtil.Grow(ords, ordLength + 1);
                        }
                        ords[ordLength] = map.GetGlobalOrd(readerUpto, ord);
                        ordLength++;
                    }
                    docIDUpto++;
                    continue;
                }

                docIDUpto++;
            }
        }

        // TODO: seek-by-ord to nextSetBit
        internal class BitsFilteredTermsEnum : FilteredTermsEnum
        {
            internal readonly Int64BitSet liveTerms;

            internal BitsFilteredTermsEnum(TermsEnum @in, Int64BitSet liveTerms)
                : base(@in, false)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(liveTerms != null);
                this.liveTerms = liveTerms;
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                return liveTerms.Get(Ord) ? AcceptStatus.YES : AcceptStatus.NO;
            }
        }

        /// <summary>
        /// Disposes all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementations must override and should dispose all resources used by this instance.
        /// </summary>
        protected abstract void Dispose(bool disposing);
    }
}