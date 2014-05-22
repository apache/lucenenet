using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene42
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


	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using ByteArrayDataOutput = Lucene.Net.Store.ByteArrayDataOutput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using ArrayUtil = Lucene.Net.Util.ArrayUtil;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using IntsRef = Lucene.Net.Util.IntsRef;
	using MathUtil = Lucene.Net.Util.MathUtil;
	using Builder = Lucene.Net.Util.Fst.Builder;
	using INPUT_TYPE = Lucene.Net.Util.Fst.FST.INPUT_TYPE;
	using FST = Lucene.Net.Util.Fst.FST;
	using PositiveIntOutputs = Lucene.Net.Util.Fst.PositiveIntOutputs;
	using Util = Lucene.Net.Util.Fst.Util;
	using BlockPackedWriter = Lucene.Net.Util.Packed.BlockPackedWriter;
	using MonotonicBlockPackedWriter = Lucene.Net.Util.Packed.MonotonicBlockPackedWriter;
	using FormatAndBits = Lucene.Net.Util.Packed.PackedInts.FormatAndBits;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.VERSION_GCD_COMPRESSION;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.BLOCK_SIZE;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.BYTES;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.NUMBER;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.FST;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.DELTA_COMPRESSED;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.GCD_COMPRESSED;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.TABLE_COMPRESSED;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.UNCOMPRESSED;

	/// <summary>
	/// Writer for <seealso cref="Lucene42DocValuesFormat"/>
	/// </summary>
	internal class Lucene42DocValuesConsumer : DocValuesConsumer
	{
	  internal readonly IndexOutput Data, Meta;
	  internal readonly int MaxDoc;
	  internal readonly float AcceptableOverheadRatio;

	  internal Lucene42DocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio)
	  {
		this.AcceptableOverheadRatio = acceptableOverheadRatio;
		MaxDoc = state.segmentInfo.DocCount;
		bool success = false;
		try
		{
		  string dataName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, dataExtension);
		  Data = state.directory.createOutput(dataName, state.context);
		  // this writer writes the format 4.2 did!
		  CodecUtil.writeHeader(Data, dataCodec, VERSION_GCD_COMPRESSION);
		  string metaName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, metaExtension);
		  Meta = state.directory.createOutput(metaName, state.context);
		  CodecUtil.writeHeader(Meta, metaCodec, VERSION_GCD_COMPRESSION);
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

	  public override void AddNumericField(FieldInfo field, IEnumerable<Number> values)
	  {
		AddNumericField(field, values, true);
	  }

	  internal virtual void AddNumericField(FieldInfo field, IEnumerable<Number> values, bool optimizeStorage)
	  {
		Meta.writeVInt(field.number);
		Meta.writeByte(NUMBER);
		Meta.writeLong(Data.FilePointer);
		long minValue = long.MaxValue;
		long maxValue = long.MinValue;
		long gcd = 0;
		// TODO: more efficient?
		HashSet<long?> uniqueValues = null;
		if (optimizeStorage)
		{
		  uniqueValues = new HashSet<>();

		  long count = 0;
		  foreach (Number nv in values)
		  {
			// TODO: support this as MemoryDVFormat (and be smart about missing maybe)
			long v = nv == null ? 0 : (long)nv;

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
		  Debug.Assert(count == MaxDoc);
		}

		if (uniqueValues != null)
		{
		  // small number of unique values
		  int bitsPerValue = PackedInts.bitsRequired(uniqueValues.Count - 1);
		  FormatAndBits formatAndBits = PackedInts.fastestFormatAndBits(MaxDoc, bitsPerValue, AcceptableOverheadRatio);
		  if (formatAndBits.bitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
		  {
			Meta.writeByte(UNCOMPRESSED); // uncompressed
			foreach (Number nv in values)
			{
			  Data.writeByte(nv == null ? 0 : (long)(sbyte) nv);
			}
		  }
		  else
		  {
			Meta.writeByte(TABLE_COMPRESSED); // table-compressed
			long?[] decode = uniqueValues.toArray(new long?[uniqueValues.Count]);
			Dictionary<long?, int?> encode = new Dictionary<long?, int?>();
			Data.writeVInt(decode.Length);
			for (int i = 0; i < decode.Length; i++)
			{
			  Data.writeLong(decode[i]);
			  encode[decode[i]] = i;
			}

			Meta.writeVInt(PackedInts.VERSION_CURRENT);
			Data.writeVInt(formatAndBits.format.Id);
			Data.writeVInt(formatAndBits.bitsPerValue);

			PackedInts.Writer writer = PackedInts.getWriterNoHeader(Data, formatAndBits.format, MaxDoc, formatAndBits.bitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
			foreach (Number nv in values)
			{
			  writer.add(encode[nv == null ? 0 : (long)nv]);
			}
			writer.finish();
		  }
		}
		else if (gcd != 0 && gcd != 1)
		{
		  Meta.writeByte(GCD_COMPRESSED);
		  Meta.writeVInt(PackedInts.VERSION_CURRENT);
		  Data.writeLong(minValue);
		  Data.writeLong(gcd);
		  Data.writeVInt(BLOCK_SIZE);

		  BlockPackedWriter writer = new BlockPackedWriter(Data, BLOCK_SIZE);
		  foreach (Number nv in values)
		  {
			long value = nv == null ? 0 : (long)nv;
			writer.add((value - minValue) / gcd);
		  }
		  writer.finish();
		}
		else
		{
		  Meta.writeByte(DELTA_COMPRESSED); // delta-compressed

		  Meta.writeVInt(PackedInts.VERSION_CURRENT);
		  Data.writeVInt(BLOCK_SIZE);

		  BlockPackedWriter writer = new BlockPackedWriter(Data, BLOCK_SIZE);
		  foreach (Number nv in values)
		  {
			writer.add(nv == null ? 0 : (long)nv);
		  }
		  writer.finish();
		}
	  }

	  public override void Close()
	  {
		bool success = false;
		try
		{
		  if (Meta != null)
		  {
			Meta.writeVInt(-1); // write EOF marker
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(Data, Meta);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(Data, Meta);
		  }
		}
	  }

	  public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		// write the byte[] data
		Meta.writeVInt(field.number);
		Meta.writeByte(BYTES);
		int minLength = int.MaxValue;
		int maxLength = int.MinValue;
		long startFP = Data.FilePointer;
		foreach (BytesRef v in values)
		{
		  int length = v == null ? 0 : v.length;
		  if (length > Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH)
		  {
			throw new System.ArgumentException("DocValuesField \"" + field.name + "\" is too large, must be <= " + Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
		  }
		  minLength = Math.Min(minLength, length);
		  maxLength = Math.Max(maxLength, length);
		  if (v != null)
		  {
			Data.writeBytes(v.bytes, v.offset, v.length);
		  }
		}
		Meta.writeLong(startFP);
		Meta.writeLong(Data.FilePointer - startFP);
		Meta.writeVInt(minLength);
		Meta.writeVInt(maxLength);

		// if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
		// otherwise, we need to record the length fields...
		if (minLength != maxLength)
		{
		  Meta.writeVInt(PackedInts.VERSION_CURRENT);
		  Meta.writeVInt(BLOCK_SIZE);

		  MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(Data, BLOCK_SIZE);
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

	  private void WriteFST(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		Meta.writeVInt(field.number);
		Meta.writeByte(FST);
		Meta.writeLong(Data.FilePointer);
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
		  fst.save(Data);
		}
		Meta.writeVLong(ord);
	  }

	  public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		// three cases for simulating the old writer:
		// 1. no missing
		// 2. missing (and empty string in use): remap ord=-1 -> ord=0
		// 3. missing (and empty string not in use): remap all ords +1, insert empty string into values
		bool anyMissing = false;
		foreach (Number n in docToOrd)
		{
		  if ((long)n == -1)
		  {
			anyMissing = true;
			break;
		  }
		}

		bool hasEmptyString = false;
		foreach (BytesRef b in values)
		{
		  hasEmptyString = b.length == 0;
		  break;
		}

		if (!anyMissing)
		{
		  // nothing to do
		}
		else if (hasEmptyString)
		{
		  docToOrd = MissingOrdRemapper.MapMissingToOrd0(docToOrd);
		}
		else
		{
		  docToOrd = MissingOrdRemapper.MapAllOrds(docToOrd);
		  values = MissingOrdRemapper.InsertEmptyValue(values);
		}

		// write the ordinals as numerics
		AddNumericField(field, docToOrd, false);

		// write the values as FST
		WriteFST(field, values);
	  }

	  // note: this might not be the most efficient... but its fairly simple
	  public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
	  {
		// write the ordinals as a binary field
		AddBinaryField(field, new IterableAnonymousInnerClassHelper(this, docToOrdCount, ords));

		// write the values as FST
		WriteFST(field, values);
	  }

	  private class IterableAnonymousInnerClassHelper : IEnumerable<BytesRef>
	  {
		  private readonly Lucene42DocValuesConsumer OuterInstance;

		  private IEnumerable<Number> DocToOrdCount;
		  private IEnumerable<Number> Ords;

		  public IterableAnonymousInnerClassHelper(Lucene42DocValuesConsumer outerInstance, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
		  {
			  this.OuterInstance = outerInstance;
			  this.DocToOrdCount = docToOrdCount;
			  this.Ords = ords;
		  }

		  public virtual IEnumerator<BytesRef> GetEnumerator()
		  {
			return new SortedSetIterator(DocToOrdCount.GetEnumerator(), Ords.GetEnumerator());
		  }
	  }

	  // per-document vint-encoded byte[]
	  internal class SortedSetIterator : IEnumerator<BytesRef>
	  {
		internal sbyte[] Buffer = new sbyte[10];
		internal ByteArrayDataOutput @out = new ByteArrayDataOutput();
		internal BytesRef @ref = new BytesRef();

		internal readonly IEnumerator<Number> Counts;
		internal readonly IEnumerator<Number> Ords;

		internal SortedSetIterator(IEnumerator<Number> counts, IEnumerator<Number> ords)
		{
		  this.Counts = counts;
		  this.Ords = ords;
		}

		public override bool HasNext()
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  return Counts.hasNext();
		}

		public override BytesRef Next()
		{
		  if (!HasNext())
		  {
			throw new NoSuchElementException();
		  }

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  int count = (int)Counts.next();
		  int maxSize = count * 9; // worst case
		  if (maxSize > Buffer.Length)
		  {
			Buffer = ArrayUtil.grow(Buffer, maxSize);
		  }

		  try
		  {
			EncodeValues(count);
		  }
		  catch (IOException bogus)
		  {
			throw new Exception(bogus);
		  }

		  @ref.bytes = Buffer;
		  @ref.offset = 0;
		  @ref.length = @out.Position;

		  return @ref;
		}

		// encodes count values to buffer
		internal virtual void EncodeValues(int count)
		{
		  @out.reset(Buffer);
		  long lastOrd = 0;
		  for (int i = 0; i < count; i++)
		  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			long ord = (long)Ords.next();
			@out.writeVLong(ord - lastOrd);
			lastOrd = ord;
		  }
		}

		public override void Remove()
		{
		  throw new System.NotSupportedException();
		}
	  }
	}

}