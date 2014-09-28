using System;
using System.Diagnostics;
using System.Collections.Generic;
using Lucene.Net.Codecs.Memory;

namespace org.apache.lucene.codecs.memory
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


	using FieldInfo = org.apache.lucene.index.FieldInfo;
	using IndexFileNames = org.apache.lucene.index.IndexFileNames;
	using SegmentWriteState = org.apache.lucene.index.SegmentWriteState;
	using ByteArrayDataOutput = org.apache.lucene.store.ByteArrayDataOutput;
	using IndexOutput = org.apache.lucene.store.IndexOutput;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using IntsRef = org.apache.lucene.util.IntsRef;
	using MathUtil = org.apache.lucene.util.MathUtil;
	using Builder = org.apache.lucene.util.fst.Builder;
	using INPUT_TYPE = org.apache.lucene.util.fst.FST.INPUT_TYPE;
	using FST = org.apache.lucene.util.fst.FST;
	using PositiveIntOutputs = org.apache.lucene.util.fst.PositiveIntOutputs;
	using Util = org.apache.lucene.util.fst.Util;
	using BlockPackedWriter = org.apache.lucene.util.packed.BlockPackedWriter;
	using MonotonicBlockPackedWriter = org.apache.lucene.util.packed.MonotonicBlockPackedWriter;
	using FormatAndBits = org.apache.lucene.util.packed.PackedInts.FormatAndBits;
	using PackedInts = org.apache.lucene.util.packed.PackedInts;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.VERSION_CURRENT;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.BLOCK_SIZE;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.BYTES;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.NUMBER;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.FST;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.DELTA_COMPRESSED;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.GCD_COMPRESSED;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.TABLE_COMPRESSED;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
	import static org.apache.lucene.codecs.memory.MemoryDocValuesProducer.UNCOMPRESSED;

	/// <summary>
	/// Writer for <seealso cref="MemoryDocValuesFormat"/>
	/// </summary>
	internal class MemoryDocValuesConsumer : DocValuesConsumer
	{
	  internal IndexOutput data, meta;
	  internal readonly int maxDoc;
	  internal readonly float acceptableOverheadRatio;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: MemoryDocValuesConsumer(org.apache.lucene.index.SegmentWriteState state, String dataCodec, String dataExtension, String metaCodec, String metaExtension, float acceptableOverheadRatio) throws java.io.IOException
	  internal MemoryDocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio)
	  {
		this.acceptableOverheadRatio = acceptableOverheadRatio;
		maxDoc = state.segmentInfo.DocCount;
		bool success = false;
		try
		{
		  string dataName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, dataExtension);
		  data = state.directory.createOutput(dataName, state.context);
		  CodecUtil.writeHeader(data, dataCodec, VERSION_CURRENT);
		  string metaName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, metaExtension);
		  meta = state.directory.createOutput(metaName, state.context);
		  CodecUtil.writeHeader(meta, metaCodec, VERSION_CURRENT);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(this);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addNumericField(org.apache.lucene.index.FieldInfo field, Iterable<Number> values) throws java.io.IOException
	  public override void addNumericField(FieldInfo field, IEnumerable<Number> values)
	  {
		addNumericField(field, values, true);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void addNumericField(org.apache.lucene.index.FieldInfo field, Iterable<Number> values, boolean optimizeStorage) throws java.io.IOException
	  internal virtual void addNumericField(FieldInfo field, IEnumerable<Number> values, bool optimizeStorage)
	  {
		meta.writeVInt(field.number);
		meta.writeByte(NUMBER);
		meta.writeLong(data.FilePointer);
		long minValue = long.MaxValue;
		long maxValue = long.MinValue;
		long gcd = 0;
		bool missing = false;
		// TODO: more efficient?
		HashSet<long?> uniqueValues = null;
		if (optimizeStorage)
		{
		  uniqueValues = new HashSet<>();

		  long count = 0;
		  foreach (Number nv in values)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long v;
			long v;
			if (nv == null)
			{
			  v = 0;
			  missing = true;
			}
			else
			{
			  v = (long)nv;
			}

			if (gcd != 1)
			{
			  if (v < long.MinValue / 2 || v > long.MaxValue / 2)
			  {
				// in that case v - minValue might overflow and make the GCD computation return
				// wrong results. Since these extreme values are unlikely, we just discard
				// GCD computation for them
				gcd = 1;
			  } // minValue needs to be set first
			  else if (count != 0)
			  {
				gcd = MathUtil.gcd(gcd, v - minValue);
			  }
			}

			minValue = Math.Min(minValue, v);
			maxValue = Math.Max(maxValue, v);

			if (uniqueValues != null)
			{
			  if (uniqueValues.Add(v))
			  {
				if (uniqueValues.Count > 256)
				{
				  uniqueValues = null;
				}
			  }
			}

			++count;
		  }
		  Debug.Assert(count == maxDoc);
		}

		if (missing)
		{
		  long start = data.FilePointer;
		  writeMissingBitset(values);
		  meta.writeLong(start);
		  meta.writeLong(data.FilePointer - start);
		}
		else
		{
		  meta.writeLong(-1L);
		}

		if (uniqueValues != null)
		{
		  // small number of unique values
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bitsPerValue = org.apache.lucene.util.packed.PackedInts.bitsRequired(uniqueValues.size()-1);
		  int bitsPerValue = PackedInts.bitsRequired(uniqueValues.Count - 1);
		  FormatAndBits formatAndBits = PackedInts.fastestFormatAndBits(maxDoc, bitsPerValue, acceptableOverheadRatio);
		  if (formatAndBits.bitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
		  {
			meta.writeByte(UNCOMPRESSED); // uncompressed
			foreach (Number nv in values)
			{
			  data.writeByte(nv == null ? 0 : (long)(sbyte) nv);
			}
		  }
		  else
		  {
			meta.writeByte(TABLE_COMPRESSED); // table-compressed
			long?[] decode = uniqueValues.toArray(new long?[uniqueValues.Count]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.HashMap<Long,Integer> encode = new java.util.HashMap<>();
			Dictionary<long?, int?> encode = new Dictionary<long?, int?>();
			data.writeVInt(decode.Length);
			for (int i = 0; i < decode.Length; i++)
			{
			  data.writeLong(decode[i]);
			  encode[decode[i]] = i;
			}

			meta.writeVInt(PackedInts.VERSION_CURRENT);
			data.writeVInt(formatAndBits.format.Id);
			data.writeVInt(formatAndBits.bitsPerValue);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.PackedInts.Writer writer = org.apache.lucene.util.packed.PackedInts.getWriterNoHeader(data, formatAndBits.format, maxDoc, formatAndBits.bitsPerValue, org.apache.lucene.util.packed.PackedInts.DEFAULT_BUFFER_SIZE);
			PackedInts.Writer writer = PackedInts.getWriterNoHeader(data, formatAndBits.format, maxDoc, formatAndBits.bitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
			foreach (Number nv in values)
			{
			  writer.add(encode[nv == null ? 0 : (long)nv]);
			}
			writer.finish();
		  }
		}
		else if (gcd != 0 && gcd != 1)
		{
		  meta.writeByte(GCD_COMPRESSED);
		  meta.writeVInt(PackedInts.VERSION_CURRENT);
		  data.writeLong(minValue);
		  data.writeLong(gcd);
		  data.writeVInt(BLOCK_SIZE);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.BlockPackedWriter writer = new org.apache.lucene.util.packed.BlockPackedWriter(data, BLOCK_SIZE);
		  BlockPackedWriter writer = new BlockPackedWriter(data, BLOCK_SIZE);
		  foreach (Number nv in values)
		  {
			long value = nv == null ? 0 : (long)nv;
			writer.add((value - minValue) / gcd);
		  }
		  writer.finish();
		}
		else
		{
		  meta.writeByte(DELTA_COMPRESSED); // delta-compressed

		  meta.writeVInt(PackedInts.VERSION_CURRENT);
		  data.writeVInt(BLOCK_SIZE);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.BlockPackedWriter writer = new org.apache.lucene.util.packed.BlockPackedWriter(data, BLOCK_SIZE);
		  BlockPackedWriter writer = new BlockPackedWriter(data, BLOCK_SIZE);
		  foreach (Number nv in values)
		  {
			writer.add(nv == null ? 0 : (long)nv);
		  }
		  writer.finish();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		bool success = false;
		try
		{
		  if (meta != null)
		  {
			meta.writeVInt(-1); // write EOF marker
			CodecUtil.writeFooter(meta); // write checksum
		  }
		  if (data != null)
		  {
			CodecUtil.writeFooter(data);
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(data, meta);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(data, meta);
		  }
		  data = meta = null;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addBinaryField(org.apache.lucene.index.FieldInfo field, final Iterable<org.apache.lucene.util.BytesRef> values) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public override void addBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		// write the byte[] data
		meta.writeVInt(field.number);
		meta.writeByte(BYTES);
		int minLength = int.MaxValue;
		int maxLength = int.MinValue;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long startFP = data.getFilePointer();
		long startFP = data.FilePointer;
		bool missing = false;
		foreach (BytesRef v in values)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int length;
		  int length;
		  if (v == null)
		  {
			length = 0;
			missing = true;
		  }
		  else
		  {
			length = v.length;
		  }
		  if (length > MemoryDocValuesFormat.MAX_BINARY_FIELD_LENGTH)
		  {
			throw new System.ArgumentException("DocValuesField \"" + field.name + "\" is too large, must be <= " + MemoryDocValuesFormat.MAX_BINARY_FIELD_LENGTH);
		  }
		  minLength = Math.Min(minLength, length);
		  maxLength = Math.Max(maxLength, length);
		  if (v != null)
		  {
			data.writeBytes(v.bytes, v.offset, v.length);
		  }
		}
		meta.writeLong(startFP);
		meta.writeLong(data.FilePointer - startFP);
		if (missing)
		{
		  long start = data.FilePointer;
		  writeMissingBitset(values);
		  meta.writeLong(start);
		  meta.writeLong(data.FilePointer - start);
		}
		else
		{
		  meta.writeLong(-1L);
		}
		meta.writeVInt(minLength);
		meta.writeVInt(maxLength);

		// if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
		// otherwise, we need to record the length fields...
		if (minLength != maxLength)
		{
		  meta.writeVInt(PackedInts.VERSION_CURRENT);
		  meta.writeVInt(BLOCK_SIZE);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.MonotonicBlockPackedWriter writer = new org.apache.lucene.util.packed.MonotonicBlockPackedWriter(data, BLOCK_SIZE);
		  MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(data, BLOCK_SIZE);
		  long addr = 0;
		  foreach (BytesRef v in values)
		  {
			if (v != null)
			{
			  addr += v.length;
			}
			writer.add(addr);
		  }
		  writer.finish();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void writeFST(org.apache.lucene.index.FieldInfo field, Iterable<org.apache.lucene.util.BytesRef> values) throws java.io.IOException
	  private void writeFST(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		meta.writeVInt(field.number);
		meta.writeByte(FST);
		meta.writeLong(data.FilePointer);
		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		Builder<long?> builder = new Builder<long?>(INPUT_TYPE.BYTE1, outputs);
		IntsRef scratch = new IntsRef();
		long ord = 0;
		foreach (BytesRef v in values)
		{
		  builder.add(Util.toIntsRef(v, scratch), ord);
		  ord++;
		}
		FST<long?> fst = builder.finish();
		if (fst != null)
		{
		  fst.save(data);
		}
		meta.writeVLong(ord);
	  }

	  // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
	  // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void writeMissingBitset(Iterable<?> values) throws java.io.IOException
	  internal virtual void writeMissingBitset<T1>(IEnumerable<T1> values)
	  {
		long bits = 0;
		int count = 0;
		foreach (object v in values)
		{
		  if (count == 64)
		  {
			data.writeLong(bits);
			count = 0;
			bits = 0;
		  }
		  if (v != null)
		  {
			bits |= 1L << (count & 0x3f);
		  }
		  count++;
		}
		if (count > 0)
		{
		  data.writeLong(bits);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addSortedField(org.apache.lucene.index.FieldInfo field, Iterable<org.apache.lucene.util.BytesRef> values, Iterable<Number> docToOrd) throws java.io.IOException
	  public override void addSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		// write the ordinals as numerics
		addNumericField(field, docToOrd, false);

		// write the values as FST
		writeFST(field, values);
	  }

	  // note: this might not be the most efficient... but its fairly simple
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void addSortedSetField(org.apache.lucene.index.FieldInfo field, Iterable<org.apache.lucene.util.BytesRef> values, final Iterable<Number> docToOrdCount, final Iterable<Number> ords) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public override void addSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
	  {
		// write the ordinals as a binary field
		addBinaryField(field, new IterableAnonymousInnerClassHelper(this, docToOrdCount, ords));

		// write the values as FST
		writeFST(field, values);
	  }

	  private class IterableAnonymousInnerClassHelper : IEnumerable<BytesRef>
	  {
		  private readonly MemoryDocValuesConsumer outerInstance;

		  private IEnumerable<Number> docToOrdCount;
		  private IEnumerable<Number> ords;

		  public IterableAnonymousInnerClassHelper(MemoryDocValuesConsumer outerInstance, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
		  {
			  this.outerInstance = outerInstance;
			  this.docToOrdCount = docToOrdCount;
			  this.ords = ords;
		  }

		  public virtual IEnumerator<BytesRef> GetEnumerator()
		  {
			return new SortedSetIterator(docToOrdCount.GetEnumerator(), ords.GetEnumerator());
		  }
	  }

	  // per-document vint-encoded byte[]
	  internal class SortedSetIterator : IEnumerator<BytesRef>
	  {
		internal sbyte[] buffer = new sbyte[10];
		internal ByteArrayDataOutput @out = new ByteArrayDataOutput();
		internal BytesRef @ref = new BytesRef();

		internal readonly IEnumerator<Number> counts;
		internal readonly IEnumerator<Number> ords;

		internal SortedSetIterator(IEnumerator<Number> counts, IEnumerator<Number> ords)
		{
		  this.counts = counts;
		  this.ords = ords;
		}

		public override bool hasNext()
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  return counts.hasNext();
		}

		public override BytesRef next()
		{
		  if (!hasNext())
		  {
			throw new NoSuchElementException();
		  }

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  int count = (int)counts.next();
		  int maxSize = count * 9; // worst case
		  if (maxSize > buffer.Length)
		  {
			buffer = ArrayUtil.grow(buffer, maxSize);
		  }

		  try
		  {
			encodeValues(count);
		  }
		  catch (IOException bogus)
		  {
			throw new Exception(bogus);
		  }

		  @ref.bytes = buffer;
		  @ref.offset = 0;
		  @ref.length = @out.Position;

		  return @ref;
		}

		// encodes count values to buffer
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void encodeValues(int count) throws java.io.IOException
		internal virtual void encodeValues(int count)
		{
		  @out.reset(buffer);
		  long lastOrd = 0;
		  for (int i = 0; i < count; i++)
		  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			long ord = (long)ords.next();
			@out.writeVLong(ord - lastOrd);
			lastOrd = ord;
		  }
		}

		public override void remove()
		{
		  throw new System.NotSupportedException();
		}
	  }
	}

}