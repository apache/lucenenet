using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
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

    using AppendingDeltaPackedLongBuffer = Lucene.Net.Util.Packed.AppendingDeltaPackedLongBuffer;
    using AppendingPackedLongBuffer = Lucene.Net.Util.Packed.AppendingPackedLongBuffer;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using BytesRefHash = Lucene.Net.Util.BytesRefHash;
    using Counter = Lucene.Net.Util.Counter;
    using DirectBytesStartArray = Lucene.Net.Util.BytesRefHash.DirectBytesStartArray;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Buffers up pending byte[]s per doc, deref and sorting via
    ///  int ord, then flushes when segment flushes.
    /// </summary>
    internal class SortedSetDocValuesWriter : DocValuesWriter
    {
        internal readonly BytesRefHash Hash;
        private AppendingPackedLongBuffer Pending; // stream of all termIDs
        private AppendingDeltaPackedLongBuffer PendingCounts; // termIDs per doc
        private readonly Counter IwBytesUsed;
        private long BytesUsed; // this only tracks differences in 'pending' and 'pendingCounts'
        private readonly FieldInfo FieldInfo;
        private int CurrentDoc;
        private int[] CurrentValues = new int[8];
        private int CurrentUpto = 0;
        private int MaxCount = 0;

        public SortedSetDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.FieldInfo = fieldInfo;
            this.IwBytesUsed = iwBytesUsed;
            Hash = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(iwBytesUsed)), BytesRefHash.DEFAULT_CAPACITY, new DirectBytesStartArray(BytesRefHash.DEFAULT_CAPACITY, iwBytesUsed));
            Pending = new AppendingPackedLongBuffer(PackedInts.COMPACT);
            PendingCounts = new AppendingDeltaPackedLongBuffer(PackedInts.COMPACT);
            BytesUsed = Pending.RamBytesUsed() + PendingCounts.RamBytesUsed();
            iwBytesUsed.AddAndGet(BytesUsed);
        }

        public virtual void AddValue(int docID, BytesRef value)
        {
            if (value == null)
            {
                throw new System.ArgumentException("field \"" + FieldInfo.Name + "\": null value not allowed");
            }
            if (value.Length > (ByteBlockPool.BYTE_BLOCK_SIZE - 2))
            {
                throw new System.ArgumentException("DocValuesField \"" + FieldInfo.Name + "\" is too large, must be <= " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2));
            }

            if (docID != CurrentDoc)
            {
                FinishCurrentDoc();
            }

            // Fill in any holes:
            while (CurrentDoc < docID)
            {
                PendingCounts.Add(0); // no values
                CurrentDoc++;
            }

            AddOneValue(value);
            UpdateBytesUsed();
        }

        // finalize currentDoc: this deduplicates the current term ids
        private void FinishCurrentDoc()
        {
            Array.Sort(CurrentValues, 0, CurrentUpto);
            int lastValue = -1;
            int count = 0;
            for (int i = 0; i < CurrentUpto; i++)
            {
                int termID = CurrentValues[i];
                // if its not a duplicate
                if (termID != lastValue)
                {
                    Pending.Add(termID); // record the term id
                    count++;
                }
                lastValue = termID;
            }
            // record the number of unique term ids for this doc
            PendingCounts.Add(count);
            MaxCount = Math.Max(MaxCount, count);
            CurrentUpto = 0;
            CurrentDoc++;
        }

        public override void Finish(int maxDoc)
        {
            FinishCurrentDoc();

            // fill in any holes
            for (int i = CurrentDoc; i < maxDoc; i++)
            {
                PendingCounts.Add(0); // no values
            }
        }

        private void AddOneValue(BytesRef value)
        {
            int termID = Hash.Add(value);
            if (termID < 0)
            {
                termID = -termID - 1;
            }
            else
            {
                // reserve additional space for each unique value:
                // 1. when indexing, when hash is 50% full, rehash() suddenly needs 2*size ints.
                //    TODO: can this same OOM happen in THPF?
                // 2. when flushing, we need 1 int per value (slot in the ordMap).
                IwBytesUsed.AddAndGet(2 * RamUsageEstimator.NUM_BYTES_INT);
            }

            if (CurrentUpto == CurrentValues.Length)
            {
                CurrentValues = ArrayUtil.Grow(CurrentValues, CurrentValues.Length + 1);
                // reserve additional space for max # values per-doc
                // when flushing, we need an int[] to sort the mapped-ords within the doc
                IwBytesUsed.AddAndGet((CurrentValues.Length - CurrentUpto) * 2 * RamUsageEstimator.NUM_BYTES_INT);
            }

            CurrentValues[CurrentUpto] = termID;
            CurrentUpto++;
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = Pending.RamBytesUsed() + PendingCounts.RamBytesUsed();
            IwBytesUsed.AddAndGet(newBytesUsed - BytesUsed);
            BytesUsed = newBytesUsed;
        }

        public override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.SegmentInfo.DocCount;
            int maxCountPerDoc = MaxCount;
            Debug.Assert(PendingCounts.Size() == maxDoc);
            int valueCount = Hash.Size();

            int[] sortedValues = Hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            int[] ordMap = new int[valueCount];

            for (int ord = 0; ord < valueCount; ord++)
            {
                ordMap[sortedValues[ord]] = ord;
            }

            dvConsumer.AddSortedSetField(FieldInfo, GetBytesRefEnumberable(valueCount, sortedValues),

                                      // doc -> ordCount
                                      GetOrdsEnumberable(maxDoc),

                                      // ords
                                      GetOrdCountEnumberable(maxCountPerDoc, ordMap));
        }

        public override void Abort()
        {
        }

        private IEnumerable<BytesRef> GetBytesRefEnumberable(int valueCount, int[] sortedValues)
        {
            for (int i = 0; i < valueCount; ++i)
            {
                var scratch = new BytesRef();
                yield return Hash.Get(sortedValues[i], scratch);
            }
        }

        private IEnumerable<long?> GetOrdsEnumberable(int maxDoc)
        {
            AppendingDeltaPackedLongBuffer.Iterator iter = PendingCounts.GetIterator();

            Debug.Assert(maxDoc == PendingCounts.Size(), "MaxDoc: " + maxDoc + ", pending.Size(): " + Pending.Size());

            for (int i = 0; i < maxDoc; ++i)
            {
                yield return (int)iter.Next();
            }
        }

        private IEnumerable<long?> GetOrdCountEnumberable(int maxCountPerDoc, int[] ordMap)
        {
            int currentUpTo = 0, currentLength = 0;
            AppendingPackedLongBuffer.Iterator iter = Pending.GetIterator();
            AppendingDeltaPackedLongBuffer.Iterator counts = PendingCounts.GetIterator();
            int[] currentDoc = new int[maxCountPerDoc];

            for (long i = 0; i < Pending.Size(); ++i)
            {
                while (currentUpTo == currentLength)
                {
                    // refill next doc, and sort remapped ords within the doc.
                    currentUpTo = 0;
                    currentLength = (int)counts.Next();
                    for (int j = 0; j < currentLength; j++)
                    {
                        currentDoc[j] = ordMap[(int)iter.Next()];
                    }
                    Array.Sort(currentDoc, 0, currentLength);
                }
                int ord = currentDoc[currentUpTo];
                currentUpTo++;
                yield return ord;
            }
        }

        /*
	  private class IterableAnonymousInnerClassHelper : IEnumerable<BytesRef>
	  {
		  private readonly SortedSetDocValuesWriter OuterInstance;

		  private int ValueCount;
		  private int[] SortedValues;

		  public IterableAnonymousInnerClassHelper(SortedSetDocValuesWriter outerInstance, int valueCount, int[] sortedValues)
		  {
			  this.OuterInstance = outerInstance;
			  this.ValueCount = valueCount;
			  this.SortedValues = sortedValues;
		  }

									// ord -> value
		  public virtual IEnumerator<BytesRef> GetEnumerator()
		  {
			return new ValuesIterator(OuterInstance, SortedValues, ValueCount);
		  }
	  }

	  private class IterableAnonymousInnerClassHelper2 : IEnumerable<Number>
	  {
		  private readonly SortedSetDocValuesWriter OuterInstance;

		  private int MaxDoc;

		  public IterableAnonymousInnerClassHelper2(SortedSetDocValuesWriter outerInstance, int maxDoc)
		  {
			  this.OuterInstance = outerInstance;
			  this.MaxDoc = maxDoc;
		  }

		  public virtual IEnumerator<Number> GetEnumerator()
		  {
			return new OrdCountIterator(OuterInstance, MaxDoc);
		  }
	  }

	  private class IterableAnonymousInnerClassHelper3 : IEnumerable<Number>
	  {
		  private readonly SortedSetDocValuesWriter OuterInstance;

		  private int MaxCountPerDoc;
		  private int[] OrdMap;

		  public IterableAnonymousInnerClassHelper3(SortedSetDocValuesWriter outerInstance, int maxCountPerDoc, int[] ordMap)
		  {
			  this.OuterInstance = outerInstance;
			  this.MaxCountPerDoc = maxCountPerDoc;
			  this.OrdMap = ordMap;
		  }

		  public virtual IEnumerator<Number> GetEnumerator()
		  {
			return new OrdsIterator(OuterInstance, OrdMap, MaxCountPerDoc);
		  }
	  }

	  public override void Abort()
	  {
	  }

	  // iterates over the unique values we have in ram
	  private class ValuesIterator : IEnumerator<BytesRef>
	  {
		  private readonly SortedSetDocValuesWriter OuterInstance;

		internal readonly int[] SortedValues;
		internal readonly BytesRef Scratch = new BytesRef();
		internal readonly int ValueCount;
		internal int OrdUpto;

		internal ValuesIterator(SortedSetDocValuesWriter outerInstance, int[] sortedValues, int valueCount)
		{
			this.OuterInstance = outerInstance;
		  this.SortedValues = sortedValues;
		  this.ValueCount = valueCount;
		}

		public override bool HasNext()
		{
		  return OrdUpto < ValueCount;
		}

		public override BytesRef Next()
		{
		  if (!HasNext())
		  {
			throw new Exception();
		  }
		  OuterInstance.Hash.Get(SortedValues[OrdUpto], Scratch);
		  OrdUpto++;
		  return Scratch;
		}

		public override void Remove()
		{
		  throw new System.NotSupportedException();
		}
	  }

	  // iterates over the ords for each doc we have in ram
	  private class OrdsIterator : IEnumerator<Number>
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal virtual void InitializeInstanceFields()
		  {
			  Iter = OuterInstance.Pending.Iterator();
			  Counts = OuterInstance.PendingCounts.Iterator();
		  }

		  private readonly SortedSetDocValuesWriter OuterInstance;

		internal AppendingPackedLongBuffer.Iterator Iter;
		internal AppendingDeltaPackedLongBuffer.Iterator Counts;
		internal readonly int[] OrdMap;
		internal readonly long NumOrds;
		internal long OrdUpto;

		internal readonly int[] CurrentDoc;
		internal int CurrentUpto;
		internal int CurrentLength;

		internal OrdsIterator(SortedSetDocValuesWriter outerInstance, int[] ordMap, int maxCount)
		{
			this.OuterInstance = outerInstance;

			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  this.CurrentDoc = new int[maxCount];
		  this.OrdMap = ordMap;
		  this.NumOrds = outerInstance.Pending.Size();
		}

		public override bool HasNext()
		{
		  return OrdUpto < NumOrds;
		}

		public override Number Next()
		{
		  if (!HasNext())
		  {
			throw new Exception();
		  }
		  while (CurrentUpto == CurrentLength)
		  {
			// refill next doc, and sort remapped ords within the doc.
			CurrentUpto = 0;
			CurrentLength = (int) Counts.Next();
			for (int i = 0; i < CurrentLength; i++)
			{
			  CurrentDoc[i] = OrdMap[(int) Iter.Next()];
			}
			Array.Sort(CurrentDoc, 0, CurrentLength);
		  }
		  int ord = CurrentDoc[CurrentUpto];
		  CurrentUpto++;
		  OrdUpto++;
		  // TODO: make reusable Number
		  return ord;
		}

		public override void Remove()
		{
		  throw new System.NotSupportedException();
		}
	  }

	  private class OrdCountIterator : IEnumerator<Number>
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal virtual void InitializeInstanceFields()
		  {
			  Iter = OuterInstance.PendingCounts.Iterator();
		  }

		  private readonly SortedSetDocValuesWriter OuterInstance;

		internal AppendingDeltaPackedLongBuffer.Iterator Iter;
		internal readonly int MaxDoc;
		internal int DocUpto;

		internal OrdCountIterator(SortedSetDocValuesWriter outerInstance, int maxDoc)
		{
			this.OuterInstance = outerInstance;

			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  this.MaxDoc = maxDoc;
		  Debug.Assert(outerInstance.PendingCounts.Size() == maxDoc);
		}

		public override bool HasNext()
		{
		  return DocUpto < MaxDoc;
		}

		public override Number Next()
		{
		  if (!HasNext())
		  {
			throw new Exception();
		  }
		  DocUpto++;
		  // TODO: make reusable Number
		  return Iter.Next();
		}

		public override void Remove()
		{
		  throw new System.NotSupportedException();
		}
	  }*/
    }
}