using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;
using OrdinalMap = Lucene.Net.Index.MultiDocValues.OrdinalMap;

namespace Lucene.Net.Codecs
{
    public abstract class DocValuesConsumer : IDisposable
    {
        protected DocValuesConsumer()
        {
        }

        public abstract void AddNumericField(FieldInfo field, IEnumerable<long> values);

        public abstract void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values);

        public abstract void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrd);

        public abstract void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrdCount, IEnumerable<long> ords);

        public void MergeNumericField(FieldInfo fieldInfo, MergeState mergeState, IList<NumericDocValues> toMerge)
        {
            AddNumericField(fieldInfo, GetMergeNumericFieldEnumerable(fieldInfo, mergeState, toMerge));
        }

        private IEnumerable<long> GetMergeNumericFieldEnumerable(FieldInfo fieldInfo, MergeState mergeState, IList<NumericDocValues> toMerge)
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            NumericDocValues currentValues = null;
            IBits currentLiveDocs = null;

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
                        currentReader = mergeState.readers[readerUpto];
                        currentValues = toMerge[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                    }
                    docIDUpto = 0;
                    continue;
                }

                if (currentLiveDocs == null || currentLiveDocs[docIDUpto])
                {
                    docIDUpto++;
                    yield return currentValues.Get(docIDUpto);
                    continue;
                }

                docIDUpto++;
            }
        }

        public void MergeBinaryField(FieldInfo fieldInfo, MergeState mergeState, IList<BinaryDocValues> toMerge)
        {
            AddBinaryField(fieldInfo, GetMergeBinaryFieldEnumerable(fieldInfo, mergeState, toMerge));
        }

        private IEnumerable<BytesRef> GetMergeBinaryFieldEnumerable(FieldInfo fieldInfo, MergeState mergeState, IList<BinaryDocValues> toMerge)
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            BinaryDocValues currentValues = null;
            IBits currentLiveDocs = null;
            BytesRef nextValue = new BytesRef();

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
                        currentReader = mergeState.readers[readerUpto];
                        currentValues = toMerge[readerUpto];
                        currentLiveDocs = currentReader.LiveDocs;
                    }
                    docIDUpto = 0;
                    continue;
                }

                if (currentLiveDocs == null || currentLiveDocs[docIDUpto])
                {
                    currentValues.Get(docIDUpto, nextValue);
                    docIDUpto++;
                    yield return nextValue;
                    continue;
                }

                docIDUpto++;
            }
        }

        public void MergeSortedField(FieldInfo fieldInfo, MergeState mergeState, List<SortedDocValues> toMerge)
        {
            AtomicReader[] readers = mergeState.readers.ToArray();
            SortedDocValues[] dvs = toMerge.ToArray();

            // step 1: iterate thru each sub and mark terms still in use
            TermsEnum[] liveTerms = new TermsEnum[dvs.Length];
            for (int sub = 0; sub < liveTerms.Length; sub++)
            {
                AtomicReader reader = readers[sub];
                SortedDocValues dv = dvs[sub];
                IBits liveDocs = reader.LiveDocs;
                if (liveDocs == null)
                {
                    liveTerms[sub] = dv.TermsEnum;
                }
                else
                {
                    OpenBitSet bitset = new OpenBitSet(dv.ValueCount);
                    for (int i = 0; i < reader.MaxDoc; i++)
                    {
                        if (liveDocs[i])
                        {
                            bitset.Set(dv.GetOrd(i));
                        }
                    }
                    liveTerms[sub] = new BitsFilteredTermsEnum(dv.TermsEnum, bitset);
                }
            }

            // step 2: create ordinal map (this conceptually does the "merging")
            OrdinalMap map = new OrdinalMap(this, liveTerms);

            // step 3: add field
            AddSortedField(fieldInfo, GetMergeSortedFieldValuesEnumerable(map, dvs), GetMergeSortedFieldDocToOrdEnumerable(readers, dvs, map));
        }

        private IEnumerable<BytesRef> GetMergeSortedFieldValuesEnumerable(OrdinalMap map, SortedDocValues[] dvs)
        {
            BytesRef scratch = new BytesRef();
            int currentOrd = 0;

            while (currentOrd < map.ValueCount)
            {
                int segmentNumber = map.GetSegmentNumber(currentOrd);
                int segmentOrd = (int)map.GetSegmentOrd(segmentNumber, currentOrd);
                dvs[segmentNumber].LookupOrd(segmentOrd, scratch);
                currentOrd++;
                yield return scratch;
            }
        }

        private IEnumerable<int> GetMergeSortedFieldDocToOrdEnumerable(AtomicReader[] readers, SortedDocValues[] dvs, OrdinalMap map)
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

                if (currentLiveDocs == null || currentLiveDocs[docIDUpto])
                {
                    int segOrd = dvs[readerUpto].GetOrd(docIDUpto);
                    docIDUpto++;
                    yield return (int)map.GetGlobalOrd(readerUpto, segOrd);
                    continue;
                }

                docIDUpto++;
            }
        }

        public void MergeSortedSetField(FieldInfo fieldInfo, MergeState mergeState, IList<SortedSetDocValues> toMerge)
        {
            AtomicReader[] readers = mergeState.readers.ToArray();
            SortedSetDocValues[] dvs = toMerge.ToArray();

            // step 1: iterate thru each sub and mark terms still in use
            TermsEnum[] liveTerms = new TermsEnum[dvs.Length];
            for (int sub = 0; sub < liveTerms.Length; sub++)
            {
                AtomicReader reader = readers[sub];
                SortedSetDocValues dv = dvs[sub];
                IBits liveDocs = reader.LiveDocs;
                if (liveDocs == null)
                {
                    liveTerms[sub] = dv.TermsEnum;
                }
                else
                {
                    OpenBitSet bitset = new OpenBitSet(dv.ValueCount);
                    for (int i = 0; i < reader.MaxDoc; i++)
                    {
                        if (liveDocs[i])
                        {
                            dv.SetDocument(i);
                            long ord;
                            while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                            {
                                bitset.Set(ord);
                            }
                        }
                    }
                    liveTerms[sub] = new BitsFilteredTermsEnum(dv.TermsEnum, bitset);
                }
            }

            // step 2: create ordinal map (this conceptually does the "merging")
            OrdinalMap map = new OrdinalMap(this, liveTerms);

            AddSortedSetField(fieldInfo,
                GetMergeSortedSetValuesEnumerable(map, dvs),
                GetMergeSortedSetDocToOrdCountEnumerable(readers, dvs),
                GetMergeSortedSetOrdsEnumerable(readers, dvs, map));
        }

        private IEnumerable<BytesRef> GetMergeSortedSetValuesEnumerable(OrdinalMap map, SortedSetDocValues[] dvs)
        {
            BytesRef scratch = new BytesRef();
            long currentOrd = 0;

            while (currentOrd < map.ValueCount)
            {
                int segmentNumber = map.GetSegmentNumber(currentOrd);
                long segmentOrd = map.GetSegmentOrd(segmentNumber, currentOrd);
                dvs[segmentNumber].LookupOrd(segmentOrd, scratch);
                currentOrd++;
                yield return scratch;
            }
        }

        private IEnumerable<int> GetMergeSortedSetDocToOrdCountEnumerable(AtomicReader[] readers, SortedSetDocValues[] dvs)
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

                if (currentLiveDocs == null || currentLiveDocs[docIDUpto])
                {
                    SortedSetDocValues dv = dvs[readerUpto];
                    dv.SetDocument(docIDUpto);
                    int value = 0;
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

        private IEnumerable<long> GetMergeSortedSetOrdsEnumerable(AtomicReader[] readers, SortedSetDocValues[] dvs, OrdinalMap map)
        {
            int readerUpto = -1;
            int docIDUpto = 0;
            AtomicReader currentReader = null;
            IBits currentLiveDocs = null;
            long[] ords = new long[8];
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

                    ordUpto++;
                    yield return ords[ordUpto];
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

                if (currentLiveDocs == null || currentLiveDocs[docIDUpto])
                {
                    //assert docIDUpto < currentReader.maxDoc();
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

        internal class BitsFilteredTermsEnum : FilteredTermsEnum
        {
            internal readonly OpenBitSet liveTerms;

            internal BitsFilteredTermsEnum(TermsEnum input, OpenBitSet liveTerms)
                : base(input, false)
            {
                //assert liveTerms != null;
                this.liveTerms = liveTerms;
            }

            protected override AcceptStatus Accept(BytesRef term)
            {
                if (liveTerms.Get(Ord))
                {
                    return AcceptStatus.YES;
                }
                else
                {
                    return AcceptStatus.NO;
                }
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
