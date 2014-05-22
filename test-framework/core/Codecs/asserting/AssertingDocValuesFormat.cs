using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.asserting
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


	using Lucene45DocValuesFormat = Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat;
	using AssertingAtomicReader = Lucene.Net.Index.AssertingAtomicReader;
	using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using NumericDocValues = Lucene.Net.Index.NumericDocValues;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using SortedDocValues = Lucene.Net.Index.SortedDocValues;
	using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using LongBitSet = Lucene.Net.Util.LongBitSet;

	/// <summary>
	/// Just like <seealso cref="Lucene45DocValuesFormat"/> but with additional asserts.
	/// </summary>
	public class AssertingDocValuesFormat : DocValuesFormat
	{
	  private readonly DocValuesFormat @in = new Lucene45DocValuesFormat();

	  public AssertingDocValuesFormat() : base("Asserting")
	  {
	  }

	  public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
	  {
		DocValuesConsumer consumer = @in.fieldsConsumer(state);
		Debug.Assert(consumer != null);
		return new AssertingDocValuesConsumer(consumer, state.segmentInfo.DocCount);
	  }

	  public override DocValuesProducer FieldsProducer(SegmentReadState state)
	  {
		Debug.Assert(state.fieldInfos.hasDocValues());
		DocValuesProducer producer = @in.fieldsProducer(state);
		Debug.Assert(producer != null);
		return new AssertingDocValuesProducer(producer, state.segmentInfo.DocCount);
	  }

	  internal class AssertingDocValuesConsumer : DocValuesConsumer
	  {
		internal readonly DocValuesConsumer @in;
		internal readonly int MaxDoc;

		internal AssertingDocValuesConsumer(DocValuesConsumer @in, int maxDoc)
		{
		  this.@in = @in;
		  this.MaxDoc = maxDoc;
		}

		public override void AddNumericField(FieldInfo field, IEnumerable<Number> values)
		{
		  int count = 0;
		  foreach (Number v in values)
		  {
			count++;
		  }
		  Debug.Assert(count == MaxDoc);
		  CheckIterator(values.GetEnumerator(), MaxDoc, true);
		  @in.addNumericField(field, values);
		}

		public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
		{
		  int count = 0;
		  foreach (BytesRef b in values)
		  {
			Debug.Assert(b == null || b.Valid);
			count++;
		  }
		  Debug.Assert(count == MaxDoc);
		  CheckIterator(values.GetEnumerator(), MaxDoc, true);
		  @in.addBinaryField(field, values);
		}

		public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
		{
		  int valueCount = 0;
		  BytesRef lastValue = null;
		  foreach (BytesRef b in values)
		  {
			Debug.Assert(b != null);
			Debug.Assert(b.Valid);
			if (valueCount > 0)
			{
			  Debug.Assert(b.compareTo(lastValue) > 0);
			}
			lastValue = BytesRef.deepCopyOf(b);
			valueCount++;
		  }
		  Debug.Assert(valueCount <= MaxDoc);

		  FixedBitSet seenOrds = new FixedBitSet(valueCount);

		  int count = 0;
		  foreach (Number v in docToOrd)
		  {
			Debug.Assert(v != null);
			int ord = (int)v;
			Debug.Assert(ord >= -1 && ord < valueCount);
			if (ord >= 0)
			{
			  seenOrds.set(ord);
			}
			count++;
		  }

		  Debug.Assert(count == MaxDoc);
		  Debug.Assert(seenOrds.cardinality() == valueCount);
		  CheckIterator(values.GetEnumerator(), valueCount, false);
		  CheckIterator(docToOrd.GetEnumerator(), MaxDoc, false);
		  @in.addSortedField(field, values, docToOrd);
		}

		public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
		{
		  long valueCount = 0;
		  BytesRef lastValue = null;
		  foreach (BytesRef b in values)
		  {
			Debug.Assert(b != null);
			Debug.Assert(b.Valid);
			if (valueCount > 0)
			{
			  Debug.Assert(b.compareTo(lastValue) > 0);
			}
			lastValue = BytesRef.deepCopyOf(b);
			valueCount++;
		  }

		  int docCount = 0;
		  long ordCount = 0;
		  LongBitSet seenOrds = new LongBitSet(valueCount);
		  IEnumerator<Number> ordIterator = ords.GetEnumerator();
		  foreach (Number v in docToOrdCount)
		  {
			Debug.Assert(v != null);
			int count = (int)v;
			Debug.Assert(count >= 0);
			docCount++;
			ordCount += count;

			long lastOrd = -1;
			for (int i = 0; i < count; i++)
			{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  Number o = ordIterator.next();
			  Debug.Assert(o != null);
			  long ord = (long)o;
			  Debug.Assert(ord >= 0 && ord < valueCount);
			  Debug.Assert(ord > lastOrd, "ord=" + ord + ",lastOrd=" + lastOrd);
			  seenOrds.set(ord);
			  lastOrd = ord;
			}
		  }
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Debug.Assert(ordIterator.hasNext() == false);

		  Debug.Assert(docCount == MaxDoc);
		  Debug.Assert(seenOrds.cardinality() == valueCount);
		  CheckIterator(values.GetEnumerator(), valueCount, false);
		  CheckIterator(docToOrdCount.GetEnumerator(), MaxDoc, false);
		  CheckIterator(ords.GetEnumerator(), ordCount, false);
		  @in.addSortedSetField(field, values, docToOrdCount, ords);
		}

		public override void Close()
		{
		  @in.close();
		}
	  }

	  internal class AssertingNormsConsumer : DocValuesConsumer
	  {
		internal readonly DocValuesConsumer @in;
		internal readonly int MaxDoc;

		internal AssertingNormsConsumer(DocValuesConsumer @in, int maxDoc)
		{
		  this.@in = @in;
		  this.MaxDoc = maxDoc;
		}

		public override void AddNumericField(FieldInfo field, IEnumerable<Number> values)
		{
		  int count = 0;
		  foreach (Number v in values)
		  {
			Debug.Assert(v != null);
			count++;
		  }
		  Debug.Assert(count == MaxDoc);
		  CheckIterator(values.GetEnumerator(), MaxDoc, false);
		  @in.addNumericField(field, values);
		}

		public override void Close()
		{
		  @in.close();
		}

		public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
		{
		  throw new IllegalStateException();
		}

		public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
		{
		  throw new IllegalStateException();
		}

		public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
		{
		  throw new IllegalStateException();
		}
	  }

	  private static void checkIterator<T>(IEnumerator<T> iterator, long expectedSize, bool allowNull)
	  {
		for (long i = 0; i < expectedSize; i++)
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  bool hasNext = iterator.hasNext();
		  Debug.Assert(hasNext);
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  T v = iterator.next();
		  Debug.Assert(allowNull || v != null);
		  try
		  {
			iterator.remove();
			throw new AssertionError("broken iterator (supports remove): " + iterator);
		  }
		  catch (System.NotSupportedException expected)
		  {
			// ok
		  }
		}
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Debug.Assert(!iterator.hasNext());
		try
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  iterator.next();
		  throw new AssertionError("broken iterator (allows next() when hasNext==false) " + iterator);
		}
		catch (NoSuchElementException expected)
		{
		  // ok
		}
	  }

	  internal class AssertingDocValuesProducer : DocValuesProducer
	  {
		internal readonly DocValuesProducer @in;
		internal readonly int MaxDoc;

		internal AssertingDocValuesProducer(DocValuesProducer @in, int maxDoc)
		{
		  this.@in = @in;
		  this.MaxDoc = maxDoc;
		}

		public override NumericDocValues GetNumeric(FieldInfo field)
		{
		  Debug.Assert(field.DocValuesType == FieldInfo.DocValuesType.NUMERIC || field.NormType == FieldInfo.DocValuesType.NUMERIC);
		  NumericDocValues values = @in.getNumeric(field);
		  Debug.Assert(values != null);
		  return new AssertingAtomicReader.AssertingNumericDocValues(values, MaxDoc);
		}

		public override BinaryDocValues GetBinary(FieldInfo field)
		{
		  Debug.Assert(field.DocValuesType == FieldInfo.DocValuesType.BINARY);
		  BinaryDocValues values = @in.getBinary(field);
		  Debug.Assert(values != null);
		  return new AssertingAtomicReader.AssertingBinaryDocValues(values, MaxDoc);
		}

		public override SortedDocValues GetSorted(FieldInfo field)
		{
		  Debug.Assert(field.DocValuesType == FieldInfo.DocValuesType.SORTED);
		  SortedDocValues values = @in.getSorted(field);
		  Debug.Assert(values != null);
		  return new AssertingAtomicReader.AssertingSortedDocValues(values, MaxDoc);
		}

		public override SortedSetDocValues GetSortedSet(FieldInfo field)
		{
		  Debug.Assert(field.DocValuesType == FieldInfo.DocValuesType.SORTED_SET);
		  SortedSetDocValues values = @in.getSortedSet(field);
		  Debug.Assert(values != null);
		  return new AssertingAtomicReader.AssertingSortedSetDocValues(values, MaxDoc);
		}

		public override Bits GetDocsWithField(FieldInfo field)
		{
		  Debug.Assert(field.DocValuesType != null);
		  Bits bits = @in.getDocsWithField(field);
		  Debug.Assert(bits != null);
		  Debug.Assert(bits.length() == MaxDoc);
		  return new AssertingAtomicReader.AssertingBits(bits);
		}

		public override void Close()
		{
		  @in.close();
		}

		public override long RamBytesUsed()
		{
		  return @in.ramBytesUsed();
		}

		public override void CheckIntegrity()
		{
		  @in.checkIntegrity();
		}
	  }
	}

}