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
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using MathUtil = Lucene.Net.Util.MathUtil;
	using BlockPackedWriter = Lucene.Net.Util.Packed.BlockPackedWriter;
	using FormatAndBits = Lucene.Net.Util.Packed.PackedInts.FormatAndBits;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.VERSION_CURRENT;

	/// <summary>
	/// Writer for <seealso cref="Lucene42NormsFormat"/>
	/// </summary>
	internal class Lucene42NormsConsumer : DocValuesConsumer
	{
	  internal const sbyte NUMBER = 0;

	  internal const int BLOCK_SIZE = 4096;

	  internal const sbyte DELTA_COMPRESSED = 0;
	  internal const sbyte TABLE_COMPRESSED = 1;
	  internal const sbyte UNCOMPRESSED = 2;
	  internal const sbyte GCD_COMPRESSED = 3;

	  internal IndexOutput Data, Meta;
	  internal readonly int MaxDoc;
	  internal readonly float AcceptableOverheadRatio;

	  internal Lucene42NormsConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio)
	  {
		this.AcceptableOverheadRatio = acceptableOverheadRatio;
		MaxDoc = state.SegmentInfo.DocCount;
		bool success = false;
		try
		{
		  string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.name, state.SegmentSuffix, dataExtension);
		  Data = state.Directory.createOutput(dataName, state.Context);
		  CodecUtil.WriteHeader(Data, dataCodec, VERSION_CURRENT);
		  string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.name, state.SegmentSuffix, metaExtension);
		  Meta = state.Directory.createOutput(metaName, state.Context);
		  CodecUtil.WriteHeader(Meta, metaCodec, VERSION_CURRENT);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(this);
		  }
		}
	  }

	  public override void AddNumericField(FieldInfo field, IEnumerable<Number> values)
	  {
		Meta.WriteVInt(field.Number);
		Meta.WriteByte(NUMBER);
		Meta.WriteLong(Data.FilePointer);
		long minValue = long.MaxValue;
		long maxValue = long.MinValue;
		long gcd = 0;
		// TODO: more efficient?
		HashSet<long?> uniqueValues = null;
		if (true)
		{
		  uniqueValues = new HashSet<>();

		  long count = 0;
		  foreach (Number nv in values)
		  {
			Debug.Assert(nv != null);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long v = nv.longValue();
			long v = (long)nv;

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
				gcd = MathUtil.Gcd(gcd, v - minValue);
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
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bitsPerValue = Lucene.Net.Util.Packed.PackedInts.bitsRequired(uniqueValues.size()-1);
		  int bitsPerValue = PackedInts.BitsRequired(uniqueValues.Count - 1);
		  FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(MaxDoc, bitsPerValue, AcceptableOverheadRatio);
		  if (formatAndBits.BitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
		  {
			Meta.WriteByte(UNCOMPRESSED); // uncompressed
			foreach (Number nv in values)
			{
			  Data.WriteByte(nv == null ? 0 : (long)(sbyte) nv);
			}
		  }
		  else
		  {
			Meta.WriteByte(TABLE_COMPRESSED); // table-compressed
			long?[] decode = uniqueValues.toArray(new long?[uniqueValues.Count]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.HashMap<Long,Integer> encode = new java.util.HashMap<>();
			Dictionary<long?, int?> encode = new Dictionary<long?, int?>();
			Data.WriteVInt(decode.Length);
			for (int i = 0; i < decode.Length; i++)
			{
			  Data.WriteLong(decode[i]);
			  encode[decode[i]] = i;
			}

			Meta.WriteVInt(PackedInts.VERSION_CURRENT);
			Data.WriteVInt(formatAndBits.Format.Id);
			Data.WriteVInt(formatAndBits.BitsPerValue);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.PackedInts.Writer writer = Lucene.Net.Util.Packed.PackedInts.getWriterNoHeader(data, formatAndBits.format, maxDoc, formatAndBits.bitsPerValue, Lucene.Net.Util.Packed.PackedInts.DEFAULT_BUFFER_SIZE);
			PackedInts.Writer writer = PackedInts.GetWriterNoHeader(Data, formatAndBits.Format, MaxDoc, formatAndBits.BitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
			foreach (Number nv in values)
			{
			  writer.Add(encode[nv == null ? 0 : (long)nv]);
			}
			writer.Finish();
		  }
		}
		else if (gcd != 0 && gcd != 1)
		{
		  Meta.WriteByte(GCD_COMPRESSED);
		  Meta.WriteVInt(PackedInts.VERSION_CURRENT);
		  Data.WriteLong(minValue);
		  Data.WriteLong(gcd);
		  Data.WriteVInt(BLOCK_SIZE);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.BlockPackedWriter writer = new Lucene.Net.Util.Packed.BlockPackedWriter(data, BLOCK_SIZE);
		  BlockPackedWriter writer = new BlockPackedWriter(Data, BLOCK_SIZE);
		  foreach (Number nv in values)
		  {
			long value = nv == null ? 0 : (long)nv;
			writer.Add((value - minValue) / gcd);
		  }
		  writer.Finish();
		}
		else
		{
		  Meta.WriteByte(DELTA_COMPRESSED); // delta-compressed

		  Meta.WriteVInt(PackedInts.VERSION_CURRENT);
		  Data.WriteVInt(BLOCK_SIZE);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.BlockPackedWriter writer = new Lucene.Net.Util.Packed.BlockPackedWriter(data, BLOCK_SIZE);
		  BlockPackedWriter writer = new BlockPackedWriter(Data, BLOCK_SIZE);
		  foreach (Number nv in values)
		  {
			writer.Add(nv == null ? 0 : (long)nv);
		  }
		  writer.Finish();
		}
	  }

	  public override void Close()
	  {
		bool success = false;
		try
		{
		  if (Meta != null)
		  {
			Meta.WriteVInt(-1); // write EOF marker
			CodecUtil.WriteFooter(Meta); // write checksum
		  }
		  if (Data != null)
		  {
			CodecUtil.WriteFooter(Data); // write checksum
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
			IOUtils.CloseWhileHandlingException(Data, Meta);
		  }
		  Meta = Data = null;
		}
	  }

	  public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		throw new System.NotSupportedException();
	  }

	  public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		throw new System.NotSupportedException();
	  }

	  public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
	  {
		throw new System.NotSupportedException();
	  }
	}

}