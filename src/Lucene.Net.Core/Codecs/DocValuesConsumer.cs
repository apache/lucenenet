using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using LongBitSet = Lucene.Net.Util.LongBitSet;
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
    /// <p>
    /// The lifecycle is:
    /// <ol>
    ///   <li>DocValuesConsumer is created by
    ///       <seealso cref="DocValuesFormat#fieldsConsumer(SegmentWriteState)"/> or
    ///       <seealso cref="NormsFormat#normsConsumer(SegmentWriteState)"/>.
    ///   <li><seealso cref="#addNumericField"/>, <seealso cref="#addBinaryField"/>,
    ///       or <seealso cref="#addSortedField"/> are called for each Numeric,
    ///       Binary, or Sorted docvalues field. The API is a "pull" rather
    ///       than "push", and the implementation is free to iterate over the
    ///       values multiple times (<seealso cref="Iterable#iterator()"/>).
    ///   <li>After all fields are added, the consumer is <seealso cref="#close"/>d.
    /// </ol>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class DocValuesConsumer : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal DocValuesConsumer()
        {
        }

        /// <summary>
        /// Writes numeric docvalues for a field. </summary>
        /// <param name="field"> field information </param>
        /// <param name="values"> Iterable of numeric values (one for each document). {@code null} indicates
        ///               a missing value. </param>
        /// <exception cref="IOException"> if an I/O error occurred. </exception>
        public abstract void AddNumericField(FieldInfo field, IEnumerable<long?> values);

        /// <summary>
        /// Writes binary docvalues for a field. </summary>
        /// <param name="field"> field information </param>
        /// <param name="values"> Iterable of binary values (one for each document). {@code null} indicates
        ///               a missing value. </param>
        /// <exception cref="IOException"> if an I/O error occurred. </exception>
        public abstract void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values);

        /// <summary>
        /// Writes pre-sorted binary docvalues for a field. </summary>
        /// <param name="field"> field information </param>
        /// <param name="values"> Iterable of binary values in sorted order (deduplicated). </param>
        /// <param name="docToOrd"> Iterable of ordinals (one for each document). {@code -1} indicates
        ///                 a missing value. </param>
        /// <exception cref="IOException"> if an I/O error occurred. </exception>
        public abstract void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd);

        /// <summary>
        /// Writes pre-sorted set docvalues for a field </summary>
        /// <param name="field"> field information </param>
        /// <param name="values"> Iterable of binary values in sorted order (deduplicated). </param>
        /// <param name="docToOrdCount"> Iterable of the number of values for each document. A zero ordinal
        ///                      count indicates a missing value. </param>
        /// <param name="ords"> Iterable of ordinal occurrences (docToOrdCount*maxDoc total). </param>
        /// <exception cref="IOException"> if an I/O error occurred. </exception>
        public abstract void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords);

        /// <summary>
        /// Merges the numeric docvalues from <code>toMerge</code>.
        /// <p>
        /// The default implementation calls <seealso cref="#addNumericField"/>, passing
        /// an Iterable that merges and filters deleted documents on the fly.</p>
        /// </summary>
        public virtual void MergeNumericField(FieldInfo fieldInfo, MergeState mergeState, IList<NumericDocValues> toMerge, IList<Bits> docsWithField)
        {
            AddNumericField(fieldInfo, GetMergeNumericFieldEnumerable(fieldInfo, mergeState, toMerge, docsWithField));
        }

        private IEnumerable<long?> GetMergeNumericFieldEnumerable(FieldInfo fieldinfo, MergeState mergeState, IList<NumericDocValues> toMerge, IList<Bits> docsWithField)
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            NumericDocValues currentValues = null;
            Bits currentLiveDocs = null;
            Bits currentDocsWithField = null;

            while (true)
            {
                if (readerUpto == toMerge.Count)
                {
                    yield break;
                }

                if (currentReader == null || docIDUpto == currentReader.MaxDoc)
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

                if (currentLiveDocs == null || currentLiveDocs.Get(docIDUpto))
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
        /// Merges the binary docvalues from <code>toMerge</code>.
        /// <p>
        /// The default implementation calls <seealso cref="#addBinaryField"/>, passing
        /// an Iterable that merges and filters deleted documents on the fly.
        /// </summary>
        public virtual void MergeBinaryField(FieldInfo fieldInfo, MergeState mergeState, IList<BinaryDocValues> toMerge, IList<Bits> docsWithField)
        {
            AddBinaryField(fieldInfo, GetMergeBinaryFieldEnumerable(fieldInfo, mergeState, toMerge, docsWithField));
        }

        private IEnumerable<BytesRef> GetMergeBinaryFieldEnumerable(FieldInfo fieldInfo, MergeState mergeState, IList<BinaryDocValues> toMerge, IList<Bits> docsWithField)
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            BinaryDocValues currentValues = null;
            Bits currentLiveDocs = null;
            Bits currentDocsWithField = null;

            while (true)
            {
                if (readerUpto == toMerge.Count)
                {
                    yield break;
                }

                if (currentReader == null || docIDUpto == currentReader.MaxDoc)
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

                if (currentLiveDocs == null || currentLiveDocs.Get(docIDUpto))
                {
                    var nextValue = new BytesRef();

                    if (currentDocsWithField.Get(docIDUpto))
                    {
                        currentValues.Get(docIDUpto, nextValue);
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
        /// Merges the sorted docvalues from <code>toMerge</code>.
        /// <p>
        /// The default implementation calls <seealso cref="#addSortedField"/>, passing
        /// an Iterable that merges ordinals and values and filters deleted documents.</p>
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
                Bits liveDocs = reader.LiveDocs;
                if (liveDocs == null)
                {
                    liveTerms[sub] = dv.TermsEnum();
                }
                else
                {
                    var bitset = new LongBitSet(dv.ValueCount);
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
                    liveTerms[sub] = new BitsFilteredTermsEnum(dv.TermsEnum(), bitset);
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

        private IEnumerable<BytesRef> GetMergeSortValuesEnumerable(OrdinalMap map, SortedDocValues[] dvs)
        {
            int currentOrd = 0;

            while (currentOrd < map.ValueCount)
            {
                var scratch = new BytesRef();
                int segmentNumber = map.GetFirstSegmentNumber(currentOrd);
                var segmentOrd = (int)map.GetFirstSegmentOrd(currentOrd);
                dvs[segmentNumber].LookupOrd(segmentOrd, scratch);
                currentOrd++;
                yield return scratch;
            }
        }

        private IEnumerable<long?> GetMergeSortedFieldDocToOrdEnumerable(AtomicReader[] readers, SortedDocValues[] dvs, OrdinalMap map)
        {
            int readerUpTo = -1;
            int docIDUpTo = 0;
            AtomicReader currentReader = null;
            Bits currentLiveDocs = null;

            while (true)
            {
                if (readerUpTo == readers.Length)
                {
                    yield break;
                }

                if (currentReader == null || docIDUpTo == currentReader.MaxDoc)
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

                if (currentLiveDocs == null || currentLiveDocs.Get(docIDUpTo))
                {
                    int segOrd = dvs[readerUpTo].GetOrd(docIDUpTo);
                    docIDUpTo++;
                    yield return segOrd == -1 ? -1 : map.GetGlobalOrd(readerUpTo, segOrd);
                    continue;
                }

                docIDUpTo++;
            }
        }

        /*
        private class IterableAnonymousInnerClassHelper3 : IEnumerable<BytesRef>
        {
            private readonly DocValuesConsumer OuterInstance;

            private SortedDocValues[] Dvs;
            private OrdinalMap Map;

            public IterableAnonymousInnerClassHelper3(DocValuesConsumer outerInstance, SortedDocValues[] dvs, OrdinalMap map)
            {
                this.OuterInstance = outerInstance;
                this.Dvs = dvs;
                this.Map = map;
            }

                // ord -> value
            public virtual IEnumerator<BytesRef> GetEnumerator()
            {
              return new IteratorAnonymousInnerClassHelper3(this);
            }

            private class IteratorAnonymousInnerClassHelper3 : IEnumerator<BytesRef>
            {
                private readonly IterableAnonymousInnerClassHelper3 OuterInstance;

                public IteratorAnonymousInnerClassHelper3(IterableAnonymousInnerClassHelper3 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    scratch = new BytesRef();
                }

                internal readonly BytesRef scratch;
                internal int currentOrd;

                public virtual bool HasNext()
                {
                  return currentOrd < OuterInstance.Map.ValueCount;
                }

                public virtual BytesRef Next()
                {
                  if (!HasNext())
                  {
                    throw new Exception();
                  }
                  int segmentNumber = OuterInstance.Map.GetFirstSegmentNumber(currentOrd);
                  int segmentOrd = (int)OuterInstance.Map.GetFirstSegmentOrd(currentOrd);
                  OuterInstance.Dvs[segmentNumber].LookupOrd(segmentOrd, scratch);
                  currentOrd++;
                  return scratch;
                }

                public virtual void Remove()
                {
                  throw new System.NotSupportedException();
                }
            }
        }

        private class IterableAnonymousInnerClassHelper4 : IEnumerable<Number>
        {
            private readonly DocValuesConsumer OuterInstance;

            private AtomicReader[] Readers;
            private SortedDocValues[] Dvs;
            private OrdinalMap Map;

            public IterableAnonymousInnerClassHelper4(DocValuesConsumer outerInstance, AtomicReader[] readers, SortedDocValues[] dvs, OrdinalMap map)
            {
                this.OuterInstance = outerInstance;
                this.Readers = readers;
                this.Dvs = dvs;
                this.Map = map;
            }

            public virtual IEnumerator<Number> GetEnumerator()
            {
              return new IteratorAnonymousInnerClassHelper4(this);
            }

            private class IteratorAnonymousInnerClassHelper4 : IEnumerator<Number>
            {
                private readonly IterableAnonymousInnerClassHelper4 OuterInstance;

                public IteratorAnonymousInnerClassHelper4(IterableAnonymousInnerClassHelper4 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    readerUpto = -1;
                }

                internal int readerUpto;
                internal int docIDUpto;
                internal int nextValue;
                internal AtomicReader currentReader;
                internal Bits currentLiveDocs;
                internal bool nextIsSet;

                public virtual bool HasNext()
                {
                  return nextIsSet || SetNext();
                }

                public virtual void Remove()
                {
                  throw new System.NotSupportedException();
                }

                public virtual Number Next()
                {
                  if (!HasNext())
                  {
                    throw new NoSuchElementException();
                  }
                  Debug.Assert(nextIsSet);
                  nextIsSet = false;
                  // TODO make a mutable number
                  return nextValue;
                }

                private bool SetNext()
                {
                  while (true)
                  {
                    if (readerUpto == OuterInstance.Readers.Length)
                    {
                      return false;
                    }

                    if (currentReader == null || docIDUpto == currentReader.MaxDoc)
                    {
                      readerUpto++;
                      if (readerUpto < OuterInstance.Readers.Length)
                      {
                        currentReader = OuterInstance.Readers[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                      }
                      docIDUpto = 0;
                      continue;
                    }

                    if (currentLiveDocs == null || currentLiveDocs.get(docIDUpto))
                    {
                      nextIsSet = true;
                      int segOrd = OuterInstance.Dvs[readerUpto].GetOrd(docIDUpto);
                      nextValue = segOrd == -1 ? - 1 : (int) OuterInstance.Map.GetGlobalOrd(readerUpto, segOrd);
                      docIDUpto++;
                      return true;
                    }

                    docIDUpto++;
                  }
                }
            }
        }*/

        /// <summary>
        /// Merges the sortedset docvalues from <code>toMerge</code>.
        /// <p>
        /// The default implementation calls <seealso cref="#addSortedSetField"/>, passing
        /// an Iterable that merges ordinals and values and filters deleted documents .
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
                if (liveDocs == null)
                {
                    liveTerms[sub] = dv.TermsEnum();
                }
                else
                {
                    var bitset = new LongBitSet(dv.ValueCount);
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
                    liveTerms[sub] = new BitsFilteredTermsEnum(dv.TermsEnum(), bitset);
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

        private IEnumerable<BytesRef> GetMergeSortedSetValuesEnumerable(OrdinalMap map, SortedSetDocValues[] dvs)
        {
            long currentOrd = 0;

            while (currentOrd < map.ValueCount)
            {
                int segmentNumber = map.GetFirstSegmentNumber(currentOrd);
                long segmentOrd = map.GetFirstSegmentOrd(currentOrd);
                var scratch = new BytesRef();
                dvs[segmentNumber].LookupOrd(segmentOrd, scratch);
                currentOrd++;
                yield return scratch;
            }
        }

        private IEnumerable<long?> GetMergeSortedSetDocToOrdCountEnumerable(AtomicReader[] readers, SortedSetDocValues[] dvs)
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            Bits currentLiveDocs = null;

            while (true)
            {
                if (readerUpto == readers.Length)
                {
                    yield break;
                }

                if (currentReader == null || docIDUpto == currentReader.MaxDoc)
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

                if (currentLiveDocs == null || currentLiveDocs.Get(docIDUpto))
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

        private IEnumerable<long?> GetMergeSortedSetOrdsEnumerable(AtomicReader[] readers, SortedSetDocValues[] dvs, OrdinalMap map)
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            Bits currentLiveDocs = null;
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

                if (currentReader == null || docIDUpto == currentReader.MaxDoc)
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

                if (currentLiveDocs == null || currentLiveDocs.Get(docIDUpto))
                {
                    Debug.Assert(docIDUpto < currentReader.MaxDoc);
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

        /*
        private class IterableAnonymousInnerClassHelper5 : IEnumerable<BytesRef>
        {
            private readonly DocValuesConsumer OuterInstance;

            private SortedSetDocValues[] Dvs;
            private OrdinalMap Map;

            public IterableAnonymousInnerClassHelper5(DocValuesConsumer outerInstance, SortedSetDocValues[] dvs, OrdinalMap map)
            {
                this.OuterInstance = outerInstance;
                this.Dvs = dvs;
                this.Map = map;
            }

                // ord -> value
            public virtual IEnumerator<BytesRef> GetEnumerator()
            {
              return new IteratorAnonymousInnerClassHelper5(this);
            }

            private class IteratorAnonymousInnerClassHelper5 : IEnumerator<BytesRef>
            {
                private readonly IterableAnonymousInnerClassHelper5 OuterInstance;

                public IteratorAnonymousInnerClassHelper5(IterableAnonymousInnerClassHelper5 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    scratch = new BytesRef();
                }

                internal readonly BytesRef scratch;
                internal long currentOrd;

                public virtual bool HasNext()
                {
                  return currentOrd < OuterInstance.Map.ValueCount;
                }

                public virtual BytesRef Next()
                {
                  if (!HasNext())
                  {
                    throw new Exception();
                  }
                  int segmentNumber = OuterInstance.Map.GetFirstSegmentNumber(currentOrd);
                  long segmentOrd = OuterInstance.Map.GetFirstSegmentOrd(currentOrd);
                  OuterInstance.Dvs[segmentNumber].LookupOrd(segmentOrd, scratch);
                  currentOrd++;
                  return scratch;
                }

                public virtual void Remove()
                {
                  throw new System.NotSupportedException();
                }
            }
        }

        private class IterableAnonymousInnerClassHelper6 : IEnumerable<Number>
        {
            private readonly DocValuesConsumer OuterInstance;

            private AtomicReader[] Readers;
            private SortedSetDocValues[] Dvs;

            public IterableAnonymousInnerClassHelper6(DocValuesConsumer outerInstance, AtomicReader[] readers, SortedSetDocValues[] dvs)
            {
                this.OuterInstance = outerInstance;
                this.Readers = readers;
                this.Dvs = dvs;
            }

            public virtual IEnumerator<Number> GetEnumerator()
            {
              return new IteratorAnonymousInnerClassHelper6(this);
            }

            private class IteratorAnonymousInnerClassHelper6 : IEnumerator<Number>
            {
                private readonly IterableAnonymousInnerClassHelper6 OuterInstance;

                public IteratorAnonymousInnerClassHelper6(IterableAnonymousInnerClassHelper6 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    readerUpto = -1;
                }

                internal int readerUpto;
                internal int docIDUpto;
                internal int nextValue;
                internal AtomicReader currentReader;
                internal Bits currentLiveDocs;
                internal bool nextIsSet;

                public virtual bool HasNext()
                {
                  return nextIsSet || SetNext();
                }

                public virtual void Remove()
                {
                  throw new System.NotSupportedException();
                }

                public virtual Number Next()
                {
                  if (!HasNext())
                  {
                    throw new Exception();
                  }
                  Debug.Assert(nextIsSet);
                  nextIsSet = false;
                  // TODO make a mutable number
                  return nextValue;
                }

                private bool SetNext()
                {
                  while (true)
                  {
                    if (readerUpto == OuterInstance.Readers.Length)
                    {
                      return false;
                    }

                    if (currentReader == null || docIDUpto == currentReader.MaxDoc)
                    {
                      readerUpto++;
                      if (readerUpto < OuterInstance.Readers.Length)
                      {
                        currentReader = OuterInstance.Readers[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                      }
                      docIDUpto = 0;
                      continue;
                    }

                    if (currentLiveDocs == null || currentLiveDocs.Get(docIDUpto))
                    {
                      nextIsSet = true;
                      SortedSetDocValues dv = OuterInstance.Dvs[readerUpto];
                      dv.Document = docIDUpto;
                      nextValue = 0;
                      while (dv.NextOrd() != SortedSetDocValues.NO_MORE_ORDS)
                      {
                        nextValue++;
                      }
                      docIDUpto++;
                      return true;
                    }

                    docIDUpto++;
                  }
                }
            }
        }

        private class IterableAnonymousInnerClassHelper7 : IEnumerable<Number>
        {
            private readonly DocValuesConsumer OuterInstance;

            private AtomicReader[] Readers;
            private SortedSetDocValues[] Dvs;
            private OrdinalMap Map;

            public IterableAnonymousInnerClassHelper7(DocValuesConsumer outerInstance, AtomicReader[] readers, SortedSetDocValues[] dvs, OrdinalMap map)
            {
                this.OuterInstance = outerInstance;
                this.Readers = readers;
                this.Dvs = dvs;
                this.Map = map;
            }

            public virtual IEnumerator<Number> GetEnumerator()
            {
              return new IteratorAnonymousInnerClassHelper7(this);
            }

            private class IteratorAnonymousInnerClassHelper7 : IEnumerator<Number>
            {
                private readonly IterableAnonymousInnerClassHelper7 OuterInstance;

                public IteratorAnonymousInnerClassHelper7(IterableAnonymousInnerClassHelper7 outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    readerUpto = -1;
                    ords = new long[8];
                }

                internal int readerUpto;
                internal int docIDUpto;
                internal long nextValue;
                internal AtomicReader currentReader;
                internal Bits currentLiveDocs;
                internal bool nextIsSet;
                internal long[] ords;
                internal int ordUpto;
                internal int ordLength;

                public virtual bool HasNext()
                {
                  return nextIsSet || SetNext();
                }

                public virtual void Remove()
                {
                  throw new System.NotSupportedException();
                }

                public virtual Number Next()
                {
                  if (!HasNext())
                  {
                    throw new Exception();
                  }
                  Debug.Assert(nextIsSet);
                  nextIsSet = false;
                  // TODO make a mutable number
                  return nextValue;
                }

                private bool SetNext()
                {
                  while (true)
                  {
                    if (readerUpto == OuterInstance.Readers.Length)
                    {
                      return false;
                    }

                    if (ordUpto < ordLength)
                    {
                      nextValue = ords[ordUpto];
                      ordUpto++;
                      nextIsSet = true;
                      return true;
                    }

                    if (currentReader == null || docIDUpto == currentReader.MaxDoc)
                    {
                      readerUpto++;
                      if (readerUpto < OuterInstance.Readers.Length)
                      {
                        currentReader = OuterInstance.Readers[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                      }
                      docIDUpto = 0;
                      continue;
                    }

                    if (currentLiveDocs == null || currentLiveDocs.Get(docIDUpto))
                    {
                      Debug.Assert(docIDUpto < currentReader.MaxDoc);
                      SortedSetDocValues dv = OuterInstance.Dvs[readerUpto];
                      dv.Document = docIDUpto;
                      ordUpto = ordLength = 0;
                      long ord;
                      while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                      {
                        if (ordLength == ords.Length)
                        {
                          ords = ArrayUtil.Grow(ords, ordLength + 1);
                        }
                        ords[ordLength] = OuterInstance.Map.GetGlobalOrd(readerUpto, ord);
                        ordLength++;
                      }
                      docIDUpto++;
                      continue;
                    }

                    docIDUpto++;
                  }
                }
            }
        }*/

        // TODO: seek-by-ord to nextSetBit
        internal class BitsFilteredTermsEnum : FilteredTermsEnum
        {
            internal readonly LongBitSet LiveTerms;

            internal BitsFilteredTermsEnum(TermsEnum @in, LongBitSet liveTerms)
                : base(@in, false)
            {
                Debug.Assert(liveTerms != null);
                LiveTerms = liveTerms;
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                return LiveTerms.Get(Ord()) ? AcceptStatus.YES : AcceptStatus.NO;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}