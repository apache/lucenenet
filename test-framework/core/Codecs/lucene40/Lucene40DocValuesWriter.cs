using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
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


	using LegacyDocValuesType = Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosReader.LegacyDocValuesType;
	using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
	using Directory = Lucene.Net.Store.Directory;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

	internal class Lucene40DocValuesWriter : DocValuesConsumer
	{
	  private readonly Directory Dir;
	  private readonly SegmentWriteState State;
	  private readonly string LegacyKey;
	  private const string SegmentSuffix = "dv";

	  // note: intentionally ignores seg suffix
	  internal Lucene40DocValuesWriter(SegmentWriteState state, string filename, string legacyKey)
	  {
		this.State = state;
		this.LegacyKey = legacyKey;
		this.Dir = new CompoundFileDirectory(state.directory, filename, state.context, true);
	  }

	  public override void AddNumericField(FieldInfo field, IEnumerable<Number> values)
	  {
		// examine the values to determine best type to use
		long minValue = long.MaxValue;
		long maxValue = long.MinValue;
		foreach (Number n in values)
		{
		  long v = n == null ? 0 : (long)n;
		  minValue = Math.Min(minValue, v);
		  maxValue = Math.Max(maxValue, v);
		}

		string fileName = IndexFileNames.segmentFileName(State.segmentInfo.name + "_" + Convert.ToString(field.number), SegmentSuffix, "dat");
		IndexOutput data = Dir.createOutput(fileName, State.context);
		bool success = false;
		try
		{
		  if (minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue && PackedInts.bitsRequired(maxValue - minValue) > 4)
		  {
			// fits in a byte[], would be more than 4bpv, just write byte[]
			AddBytesField(field, data, values);
		  }
		  else if (minValue >= short.MinValue && maxValue <= short.MaxValue && PackedInts.bitsRequired(maxValue - minValue) > 8)
		  {
			// fits in a short[], would be more than 8bpv, just write short[]
			AddShortsField(field, data, values);
		  }
		  else if (minValue >= int.MinValue && maxValue <= int.MaxValue && PackedInts.bitsRequired(maxValue - minValue) > 16)
		  {
			// fits in a int[], would be more than 16bpv, just write int[]
			AddIntsField(field, data, values);
		  }
		  else
		  {
			AddVarIntsField(field, data, values, minValue, maxValue);
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(data);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(data);
		  }
		}
	  }

	  private void AddBytesField(FieldInfo field, IndexOutput output, IEnumerable<Number> values)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.FIXED_INTS_8.name());
		CodecUtil.writeHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
		output.writeInt(1); // size
		foreach (Number n in values)
		{
		  output.writeByte(n == null ? 0 : (sbyte)n);
		}
	  }

	  private void AddShortsField(FieldInfo field, IndexOutput output, IEnumerable<Number> values)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.FIXED_INTS_16.name());
		CodecUtil.writeHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
		output.writeInt(2); // size
		foreach (Number n in values)
		{
		  output.writeShort(n == null ? 0 : (short)n);
		}
	  }

	  private void AddIntsField(FieldInfo field, IndexOutput output, IEnumerable<Number> values)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.FIXED_INTS_32.name());
		CodecUtil.writeHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat.INTS_VERSION_CURRENT);
		output.writeInt(4); // size
		foreach (Number n in values)
		{
		  output.writeInt(n == null ? 0 : (int)n);
		}
	  }

	  private void AddVarIntsField(FieldInfo field, IndexOutput output, IEnumerable<Number> values, long minValue, long maxValue)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.VAR_INTS.name());

		CodecUtil.writeHeader(output, Lucene40DocValuesFormat.VAR_INTS_CODEC_NAME, Lucene40DocValuesFormat.VAR_INTS_VERSION_CURRENT);

		long delta = maxValue - minValue;

		if (delta < 0)
		{
		  // writes longs
		  output.writeByte(Lucene40DocValuesFormat.VAR_INTS_FIXED_64);
		  foreach (Number n in values)
		  {
			output.writeLong(n == null ? 0 : (long)n);
		  }
		}
		else
		{
		  // writes packed ints
		  output.writeByte(Lucene40DocValuesFormat.VAR_INTS_PACKED);
		  output.writeLong(minValue);
		  output.writeLong(0 - minValue); // default value (representation of 0)
		  PackedInts.Writer writer = PackedInts.getWriter(output, State.segmentInfo.DocCount, PackedInts.bitsRequired(delta), PackedInts.DEFAULT);
		  foreach (Number n in values)
		  {
			long v = n == null ? 0 : (long)n;
			writer.add(v - minValue);
		  }
		  writer.finish();
		}
	  }

	  public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		// examine the values to determine best type to use
		HashSet<BytesRef> uniqueValues = new HashSet<BytesRef>();
		int minLength = int.MaxValue;
		int maxLength = int.MinValue;
		foreach (BytesRef b in values)
		{
		  if (b == null)
		  {
			b = new BytesRef(); // 4.0 doesnt distinguish
		  }
		  if (b.length > Lucene40DocValuesFormat.MAX_BINARY_FIELD_LENGTH)
		  {
			throw new System.ArgumentException("DocValuesField \"" + field.name + "\" is too large, must be <= " + Lucene40DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
		  }
		  minLength = Math.Min(minLength, b.length);
		  maxLength = Math.Max(maxLength, b.length);
		  if (uniqueValues != null)
		  {
			if (uniqueValues.Add(BytesRef.deepCopyOf(b)))
			{
			  if (uniqueValues.Count > 256)
			  {
				uniqueValues = null;
			  }
			}
		  }
		}

		int maxDoc = State.segmentInfo.DocCount;
		bool @fixed = minLength == maxLength;
		bool dedup = uniqueValues != null && uniqueValues.Count * 2 < maxDoc;

		if (dedup)
		{
		  // we will deduplicate and deref values
		  bool success = false;
		  IndexOutput data = null;
		  IndexOutput index = null;
		  string dataName = IndexFileNames.segmentFileName(State.segmentInfo.name + "_" + Convert.ToString(field.number), SegmentSuffix, "dat");
		  string indexName = IndexFileNames.segmentFileName(State.segmentInfo.name + "_" + Convert.ToString(field.number), SegmentSuffix, "idx");
		  try
		  {
			data = Dir.createOutput(dataName, State.context);
			index = Dir.createOutput(indexName, State.context);
			if (@fixed)
			{
			  AddFixedDerefBytesField(field, data, index, values, minLength);
			}
			else
			{
			  AddVarDerefBytesField(field, data, index, values);
			}
			success = true;
		  }
		  finally
		  {
			if (success)
			{
			  IOUtils.close(data, index);
			}
			else
			{
			  IOUtils.closeWhileHandlingException(data, index);
			}
		  }
		}
		else
		{
		  // we dont deduplicate, just write values straight
		  if (@fixed)
		  {
			// fixed byte[]
			string fileName = IndexFileNames.segmentFileName(State.segmentInfo.name + "_" + Convert.ToString(field.number), SegmentSuffix, "dat");
			IndexOutput data = Dir.createOutput(fileName, State.context);
			bool success = false;
			try
			{
			  AddFixedStraightBytesField(field, data, values, minLength);
			  success = true;
			}
			finally
			{
			  if (success)
			  {
				IOUtils.close(data);
			  }
			  else
			  {
				IOUtils.closeWhileHandlingException(data);
			  }
			}
		  }
		  else
		  {
			// variable byte[]
			bool success = false;
			IndexOutput data = null;
			IndexOutput index = null;
			string dataName = IndexFileNames.segmentFileName(State.segmentInfo.name + "_" + Convert.ToString(field.number), SegmentSuffix, "dat");
			string indexName = IndexFileNames.segmentFileName(State.segmentInfo.name + "_" + Convert.ToString(field.number), SegmentSuffix, "idx");
			try
			{
			  data = Dir.createOutput(dataName, State.context);
			  index = Dir.createOutput(indexName, State.context);
			  AddVarStraightBytesField(field, data, index, values);
			  success = true;
			}
			finally
			{
			  if (success)
			  {
				IOUtils.close(data, index);
			  }
			  else
			  {
				IOUtils.closeWhileHandlingException(data, index);
			  }
			}
		  }
		}
	  }

	  private void AddFixedStraightBytesField(FieldInfo field, IndexOutput output, IEnumerable<BytesRef> values, int length)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.BYTES_FIXED_STRAIGHT.name());

		CodecUtil.writeHeader(output, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_CODEC_NAME, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_CURRENT);

		output.writeInt(length);
		foreach (BytesRef v in values)
		{
		  if (v != null)
		  {
			output.writeBytes(v.bytes, v.offset, v.length);
		  }
		}
	  }

	  // NOTE: 4.0 file format docs are crazy/wrong here...
	  private void AddVarStraightBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.BYTES_VAR_STRAIGHT.name());

		CodecUtil.writeHeader(data, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);

		CodecUtil.writeHeader(index, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);

		/* values */

		long startPos = data.FilePointer;

		foreach (BytesRef v in values)
		{
		  if (v != null)
		  {
			data.writeBytes(v.bytes, v.offset, v.length);
		  }
		}

		/* addresses */

		long maxAddress = data.FilePointer - startPos;
		index.writeVLong(maxAddress);

		int maxDoc = State.segmentInfo.DocCount;
		Debug.Assert(maxDoc != int.MaxValue); // unsupported by the 4.0 impl

		PackedInts.Writer w = PackedInts.getWriter(index, maxDoc + 1, PackedInts.bitsRequired(maxAddress), PackedInts.DEFAULT);
		long currentPosition = 0;
		foreach (BytesRef v in values)
		{
		  w.add(currentPosition);
		  if (v != null)
		  {
			currentPosition += v.length;
		  }
		}
		// write sentinel
		Debug.Assert(currentPosition == maxAddress);
		w.add(currentPosition);
		w.finish();
	  }

	  private void AddFixedDerefBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values, int length)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.BYTES_FIXED_DEREF.name());

		CodecUtil.writeHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);

		CodecUtil.writeHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);

		// deduplicate
		SortedSet<BytesRef> dictionary = new SortedSet<BytesRef>();
		foreach (BytesRef v in values)
		{
		  dictionary.Add(v == null ? new BytesRef() : BytesRef.deepCopyOf(v));
		}

		/* values */
		data.writeInt(length);
		foreach (BytesRef v in dictionary)
		{
		  data.writeBytes(v.bytes, v.offset, v.length);
		}

		/* ordinals */
		int valueCount = dictionary.Count;
		Debug.Assert(valueCount > 0);
		index.writeInt(valueCount);
		int maxDoc = State.segmentInfo.DocCount;
		PackedInts.Writer w = PackedInts.getWriter(index, maxDoc, PackedInts.bitsRequired(valueCount - 1), PackedInts.DEFAULT);

		foreach (BytesRef v in values)
		{
		  if (v == null)
		  {
			v = new BytesRef();
		  }
		  int ord = dictionary.headSet(v).size();
		  w.add(ord);
		}
		w.finish();
	  }

	  private void AddVarDerefBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.BYTES_VAR_DEREF.name());

		CodecUtil.writeHeader(data, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);

		CodecUtil.writeHeader(index, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);

		// deduplicate
		SortedSet<BytesRef> dictionary = new SortedSet<BytesRef>();
		foreach (BytesRef v in values)
		{
		  dictionary.Add(v == null ? new BytesRef() : BytesRef.deepCopyOf(v));
		}

		/* values */
		long startPosition = data.FilePointer;
		long currentAddress = 0;
		Dictionary<BytesRef, long?> valueToAddress = new Dictionary<BytesRef, long?>();
		foreach (BytesRef v in dictionary)
		{
		  currentAddress = data.FilePointer - startPosition;
		  valueToAddress[v] = currentAddress;
		  WriteVShort(data, v.length);
		  data.writeBytes(v.bytes, v.offset, v.length);
		}

		/* ordinals */
		long totalBytes = data.FilePointer - startPosition;
		index.writeLong(totalBytes);
		int maxDoc = State.segmentInfo.DocCount;
		PackedInts.Writer w = PackedInts.getWriter(index, maxDoc, PackedInts.bitsRequired(currentAddress), PackedInts.DEFAULT);

		foreach (BytesRef v in values)
		{
		  w.add(valueToAddress[v == null ? new BytesRef() : v]);
		}
		w.finish();
	  }

	  // the little vint encoding used for var-deref
	  private static void WriteVShort(IndexOutput o, int i)
	  {
		Debug.Assert(i >= 0 && i <= short.MaxValue);
		if (i < 128)
		{
		  o.writeByte((sbyte)i);
		}
		else
		{
		  o.writeByte(unchecked((sbyte)(0x80 | (i >> 8))));
		  o.writeByte(unchecked((sbyte)(i & 0xff)));
		}
	  }

	  public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		// examine the values to determine best type to use
		int minLength = int.MaxValue;
		int maxLength = int.MinValue;
		foreach (BytesRef b in values)
		{
		  minLength = Math.Min(minLength, b.length);
		  maxLength = Math.Max(maxLength, b.length);
		}

		// but dont use fixed if there are missing values (we are simulating how lucene40 wrote dv...)
		bool anyMissing = false;
		foreach (Number n in docToOrd)
		{
		  if ((long)n == -1)
		  {
			anyMissing = true;
			break;
		  }
		}

		bool success = false;
		IndexOutput data = null;
		IndexOutput index = null;
		string dataName = IndexFileNames.segmentFileName(State.segmentInfo.name + "_" + Convert.ToString(field.number), SegmentSuffix, "dat");
		string indexName = IndexFileNames.segmentFileName(State.segmentInfo.name + "_" + Convert.ToString(field.number), SegmentSuffix, "idx");

		try
		{
		  data = Dir.createOutput(dataName, State.context);
		  index = Dir.createOutput(indexName, State.context);
		  if (minLength == maxLength && !anyMissing)
		  {
			// fixed byte[]
			AddFixedSortedBytesField(field, data, index, values, docToOrd, minLength);
		  }
		  else
		  {
			// var byte[]
			// three cases for simulating the old writer:
			// 1. no missing
			// 2. missing (and empty string in use): remap ord=-1 -> ord=0
			// 3. missing (and empty string not in use): remap all ords +1, insert empty string into values
			if (!anyMissing)
			{
			  AddVarSortedBytesField(field, data, index, values, docToOrd);
			}
			else if (minLength == 0)
			{
			  AddVarSortedBytesField(field, data, index, values, MissingOrdRemapper.MapMissingToOrd0(docToOrd));
			}
			else
			{
			  AddVarSortedBytesField(field, data, index, MissingOrdRemapper.InsertEmptyValue(values), MissingOrdRemapper.MapAllOrds(docToOrd));
			}
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(data, index);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(data, index);
		  }
		}
	  }

	  private void AddFixedSortedBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd, int length)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.BYTES_FIXED_SORTED.name());

		CodecUtil.writeHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);

		CodecUtil.writeHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);

		/* values */

		data.writeInt(length);
		int valueCount = 0;
		foreach (BytesRef v in values)
		{
		  data.writeBytes(v.bytes, v.offset, v.length);
		  valueCount++;
		}

		/* ordinals */

		index.writeInt(valueCount);
		int maxDoc = State.segmentInfo.DocCount;
		Debug.Assert(valueCount > 0);
		PackedInts.Writer w = PackedInts.getWriter(index, maxDoc, PackedInts.bitsRequired(valueCount - 1), PackedInts.DEFAULT);
		foreach (Number n in docToOrd)
		{
		  w.add((long)n);
		}
		w.finish();
	  }

	  private void AddVarSortedBytesField(FieldInfo field, IndexOutput data, IndexOutput index, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		field.putAttribute(LegacyKey, LegacyDocValuesType.BYTES_VAR_SORTED.name());

		CodecUtil.writeHeader(data, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_DAT, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);

		CodecUtil.writeHeader(index, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_IDX, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);

		/* values */

		long startPos = data.FilePointer;

		int valueCount = 0;
		foreach (BytesRef v in values)
		{
		  data.writeBytes(v.bytes, v.offset, v.length);
		  valueCount++;
		}

		/* addresses */

		long maxAddress = data.FilePointer - startPos;
		index.writeLong(maxAddress);

		Debug.Assert(valueCount != int.MaxValue); // unsupported by the 4.0 impl

		PackedInts.Writer w = PackedInts.getWriter(index, valueCount + 1, PackedInts.bitsRequired(maxAddress), PackedInts.DEFAULT);
		long currentPosition = 0;
		foreach (BytesRef v in values)
		{
		  w.add(currentPosition);
		  currentPosition += v.length;
		}
		// write sentinel
		Debug.Assert(currentPosition == maxAddress);
		w.add(currentPosition);
		w.finish();

		/* ordinals */

		int maxDoc = State.segmentInfo.DocCount;
		Debug.Assert(valueCount > 0);
		PackedInts.Writer ords = PackedInts.getWriter(index, maxDoc, PackedInts.bitsRequired(valueCount - 1), PackedInts.DEFAULT);
		foreach (Number n in docToOrd)
		{
		  ords.add((long)n);
		}
		ords.finish();
	  }

	  public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
	  {
		throw new System.NotSupportedException("Lucene 4.0 does not support SortedSet docvalues");
	  }

	  public override void Close()
	  {
		Dir.close();
	  }
	}

}