using Lucene.Net.Util.Packed;
using System.Collections.Generic;

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
    using Counter = Lucene.Net.Util.Counter;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Buffers up pending long per doc, then flushes when
    ///  segment flushes.
    /// </summary>
    internal class NumericDocValuesWriter : DocValuesWriter
    {
        private const long MISSING = 0L;

        private AppendingDeltaPackedLongBuffer pending;
        private readonly Counter iwBytesUsed;
        private long bytesUsed;
        private FixedBitSet docsWithField;
        private readonly FieldInfo fieldInfo;

        public NumericDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed, bool trackDocsWithField)
        {
            pending = new AppendingDeltaPackedLongBuffer(PackedInts.COMPACT);
            docsWithField = trackDocsWithField ? new FixedBitSet(64) : null;
            bytesUsed = pending.RamBytesUsed() + DocsWithFieldBytesUsed();
            this.fieldInfo = fieldInfo;
            this.iwBytesUsed = iwBytesUsed;
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public virtual void AddValue(int docID, long value)
        {
            if (docID < pending.Count)
            {
                throw new System.ArgumentException("DocValuesField \"" + fieldInfo.Name + "\" appears more than once in this document (only one value is allowed per field)");
            }

            // Fill in any holes:
            for (int i = (int)pending.Count; i < docID; ++i)
            {
                pending.Add(MISSING);
            }

            pending.Add(value);
            if (docsWithField != null)
            {
                docsWithField = FixedBitSet.EnsureCapacity(docsWithField, docID);
                docsWithField.Set(docID);
            }

            UpdateBytesUsed();
        }

        private long DocsWithFieldBytesUsed()
        {
            // size of the long[] + some overhead
            return docsWithField == null ? 0 : RamUsageEstimator.SizeOf(docsWithField.Bits) + 64;
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = pending.RamBytesUsed() + DocsWithFieldBytesUsed();
            iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
            bytesUsed = newBytesUsed;
        }

        public override void Finish(int maxDoc)
        {
        }

        public override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.SegmentInfo.DocCount;

            dvConsumer.AddNumericField(fieldInfo, GetNumericIterator(maxDoc));
        }

        private IEnumerable<long?> GetNumericIterator(int maxDoc)
        {
            // LUCENENET specific: using yield return instead of custom iterator type. Much less code.
            AbstractAppendingLongBuffer.Iterator iter = pending.GetIterator();
            int size = (int)pending.Count;
            int upto = 0;

            while (upto < maxDoc)
            {
                long? value;
                if (upto < size)
                {
                    var v = iter.Next();
                    if (docsWithField == null || docsWithField.Get(upto))
                    {
                        value = v;
                    }
                    else
                    {
                        value = null;
                    }
                }
                else
                {
                    value = docsWithField != null ? (long?) null : MISSING;
                }
                upto++;
                // TODO: make reusable Number
                yield return value;
            }
        }

        /*
	  private class IterableAnonymousInnerClassHelper : IEnumerable<Number>
	  {
		  private readonly NumericDocValuesWriter OuterInstance;

		  private int MaxDoc;

		  public IterableAnonymousInnerClassHelper(NumericDocValuesWriter outerInstance, int maxDoc)
		  {
			  this.OuterInstance = outerInstance;
			  this.MaxDoc = maxDoc;
		  }

		  public virtual IEnumerator<Number> GetEnumerator()
		  {
			return new NumericIterator(OuterInstance, MaxDoc);
		  }
	  }*/

        public override void Abort()
        {
        }

        /*
	  // iterates over the values we have in ram
	  private class NumericIterator : IEnumerator<Number>
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal virtual void InitializeInstanceFields()
		  {
			  Iter = OuterInstance.Pending.Iterator();
			  Size = (int)OuterInstance.Pending.Size();
		  }

		  private readonly NumericDocValuesWriter OuterInstance;

		internal AppendingDeltaPackedLongBuffer.Iterator Iter;
		internal int Size;
		internal readonly int MaxDoc;
		internal int Upto;

		internal NumericIterator(NumericDocValuesWriter outerInstance, int maxDoc)
		{
			this.OuterInstance = outerInstance;

			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  this.MaxDoc = maxDoc;
		}

		public override bool HasNext()
		{
		  return Upto < MaxDoc;
		}

		public override Number Next()
		{
		  if (!HasNext())
		  {
			throw new NoSuchElementException();
		  }
		  long? value;
		  if (Upto < Size)
		  {
			long v = Iter.next();
            if (OuterInstance.DocsWithField == null || OuterInstance.DocsWithField.Get(Upto))
			{
			  value = v;
			}
			else
			{
			  value = null;
			}
		  }
		  else
		  {
              value = OuterInstance.DocsWithField != null ? null : MISSING;
		  }
		  Upto++;
		  return value;
		}

		public override void Remove()
		{
		  throw new System.NotSupportedException();
		}
	  }*/
    }
}