using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using AppendingDeltaPackedLongBuffer = Lucene.Net.Util.Packed.AppendingDeltaPackedLongBuffer;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Counter = Lucene.Net.Util.Counter;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;

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

    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Buffers up pending byte[] per doc, then flushes when
    ///  segment flushes.
    /// </summary>
    internal class BinaryDocValuesWriter : DocValuesWriter
    {
        /// <summary>
        /// Maximum length for a binary field. </summary>
        private static readonly int MAX_LENGTH = ArrayUtil.MAX_ARRAY_LENGTH;

        // 32 KB block sizes for PagedBytes storage:
        private const int BLOCK_BITS = 15;

        private readonly PagedBytes Bytes;
        private readonly DataOutput BytesOut;
        private readonly DataInput BytesIn;

        private readonly Counter IwBytesUsed;
        private readonly AppendingDeltaPackedLongBuffer Lengths;
        private FixedBitSet DocsWithField;
        private readonly FieldInfo FieldInfo;
        private int AddedValues;
        private long BytesUsed;

        public BinaryDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.FieldInfo = fieldInfo;
            this.Bytes = new PagedBytes(BLOCK_BITS);
            this.BytesOut = Bytes.DataOutput;
            this.Lengths = new AppendingDeltaPackedLongBuffer(PackedInts.COMPACT);
            this.IwBytesUsed = iwBytesUsed;
            this.DocsWithField = new FixedBitSet(64);
            this.BytesUsed = DocsWithFieldBytesUsed();
            iwBytesUsed.AddAndGet(BytesUsed);
        }

        public virtual void AddValue(int docID, BytesRef value)
        {
            if (docID < AddedValues)
            {
                throw new System.ArgumentException("DocValuesField \"" + FieldInfo.Name + "\" appears more than once in this document (only one value is allowed per field)");
            }
            if (value == null)
            {
                throw new System.ArgumentException("field=\"" + FieldInfo.Name + "\": null value not allowed");
            }
            if (value.Length > MAX_LENGTH)
            {
                throw new System.ArgumentException("DocValuesField \"" + FieldInfo.Name + "\" is too large, must be <= " + MAX_LENGTH);
            }

            // Fill in any holes:
            while (AddedValues < docID)
            {
                AddedValues++;
                Lengths.Add(0);
            }
            AddedValues++;
            Lengths.Add(value.Length);
            try
            {
                BytesOut.WriteBytes(value.Bytes, value.Offset, value.Length);
            }
            catch (System.IO.IOException ioe)
            {
                // Should never happen!
                throw new Exception(ioe.Message, ioe);
            }
            DocsWithField = FixedBitSet.EnsureCapacity(DocsWithField, docID);
            DocsWithField.Set(docID);
            UpdateBytesUsed();
        }

        private long DocsWithFieldBytesUsed()
        {
            // size of the long[] + some overhead
            return RamUsageEstimator.SizeOf(DocsWithField.Bits) + 64;
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = Lengths.RamBytesUsed() + Bytes.RamBytesUsed() + DocsWithFieldBytesUsed();
            IwBytesUsed.AddAndGet(newBytesUsed - BytesUsed);
            BytesUsed = newBytesUsed;
        }

        internal override void Finish(int maxDoc)
        {
        }

        internal override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.SegmentInfo.DocCount;
            Bytes.Freeze(false);
            dvConsumer.AddBinaryField(FieldInfo, GetBytesIterator(maxDoc));
            //dvConsumer.AddBinaryField(FieldInfo, new IterableAnonymousInnerClassHelper(this, maxDoc));
        }

        /*
	  private class IterableAnonymousInnerClassHelper : IEnumerable<BytesRef>
	  {
		  private readonly BinaryDocValuesWriter OuterInstance;

		  private int MaxDoc;

		  public IterableAnonymousInnerClassHelper(BinaryDocValuesWriter outerInstance, int maxDoc)
		  {
			  this.OuterInstance = outerInstance;
			  this.MaxDoc = maxDoc;
		  }

		  public virtual IEnumerator<BytesRef> GetEnumerator()
		  {
			 return new BytesIterator(OuterInstance, MaxDoc);
		  }
	  }*/

        internal override void Abort()
        {
        }

        private IEnumerable<BytesRef> GetBytesIterator(int maxDocParam)
        {
            // Use yield return instead of ucsom IEnumerable

            BytesRef value = new BytesRef();
            AppendingDeltaPackedLongBuffer.Iterator lengthsIterator = (AppendingDeltaPackedLongBuffer.Iterator)Lengths.GetIterator();
            int size = (int)Lengths.Size();
            DataInput bytesIterator = Bytes.DataInput;
            int maxDoc = maxDocParam;
            int upto = 0;
            long byteOffset = 0L;

            while (upto < maxDoc)
            {
                if (upto < size)
                {
                    int length = (int)lengthsIterator.Next();
                    value.Grow(length);
                    value.Length = length;
                    //LUCENE TODO: This modification is slightly fishy, 4x port uses ByteBlockPool
                    bytesIterator.ReadBytes(/*byteOffset,*/ value.Bytes, value.Offset, value.Length);
                    byteOffset += length;
                }
                else
                {
                    // This is to handle last N documents not having
                    // this DV field in the end of the segment:
                    value.Length = 0;
                }

                upto++;
                yield return value;
            }
        }

        /*
	  // iterates over the values we have in ram
	  private class BytesIterator : IEnumerator<BytesRef>
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal virtual void InitializeInstanceFields()
		  {
			  LengthsIterator = OuterInstance.Lengths.Iterator();
              BytesIterator_Renamed = OuterInstance.Bytes.DataInput;
              Size = (int)OuterInstance.Lengths.Size();
		  }

		  private readonly BinaryDocValuesWriter OuterInstance;

		internal readonly BytesRef Value = new BytesRef();
		internal AppendingDeltaPackedLongBuffer.Iterator LengthsIterator;
		internal DataInput BytesIterator_Renamed;
		internal int Size;
		internal readonly int MaxDoc;
		internal int Upto;

		internal BytesIterator(BinaryDocValuesWriter outerInstance, int maxDoc)
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

		public override BytesRef Next()
		{
		  if (!HasNext())
		  {
			throw new NoSuchElementException();
		  }
		  BytesRef v;
		  if (Upto < Size)
		  {
			int length = (int) LengthsIterator.Next();
			Value.Grow(length);
			Value.Length = length;
			try
			{
			  BytesIterator_Renamed.ReadBytes(Value.Bytes, Value.Offset, Value.Length);
			}
			catch (System.IO.IOException ioe)
			{
			  // Should never happen!
			  throw new Exception(ioe.ToString(), ioe);
			}
            if (OuterInstance.DocsWithField.Get(Upto))
			{
			  v = Value;
			}
			else
			{
			  v = null;
			}
		  }
		  else
		  {
			v = null;
		  }
		  Upto++;
		  return v;
		}

		public override void Remove()
		{
		  throw new System.NotSupportedException();
		}
	  }*/
    }
}