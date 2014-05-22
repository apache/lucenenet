using System;
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


	using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
	using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
	using DocValues = Lucene.Net.Index.DocValues;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using DocsEnum = Lucene.Net.Index.DocsEnum;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using NumericDocValues = Lucene.Net.Index.NumericDocValues;
	using SegmentReadState = Lucene.Net.Index.SegmentReadState;
	using SortedDocValues = Lucene.Net.Index.SortedDocValues;
	using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
	using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using IntsRef = Lucene.Net.Util.IntsRef;
	using PagedBytes = Lucene.Net.Util.PagedBytes;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
	using Lucene.Net.Util.Fst;
	using Lucene.Net.Util.Fst.BytesRefFSTEnum;
	using Lucene.Net.Util.Fst;
	using Lucene.Net.Util.Fst.FST;
	using BytesReader = Lucene.Net.Util.Fst.FST.BytesReader;
	using PositiveIntOutputs = Lucene.Net.Util.Fst.PositiveIntOutputs;
	using Util = Lucene.Net.Util.Fst.Util;
	using BlockPackedReader = Lucene.Net.Util.Packed.BlockPackedReader;
	using MonotonicBlockPackedReader = Lucene.Net.Util.Packed.MonotonicBlockPackedReader;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

	/// <summary>
	/// Reader for <seealso cref="Lucene42DocValuesFormat"/>
	/// </summary>
	internal class Lucene42DocValuesProducer : DocValuesProducer
	{
	  // metadata maps (just file pointers and minimal stuff)
	  private readonly IDictionary<int?, NumericEntry> Numerics;
	  private readonly IDictionary<int?, BinaryEntry> Binaries;
	  private readonly IDictionary<int?, FSTEntry> Fsts;
	  private readonly IndexInput Data;
	  private readonly int Version;

	  // ram instances we have already loaded
	  private readonly IDictionary<int?, Org.apache.lucene.index.NumericDocValues> NumericInstances = new Dictionary<int?, Org.apache.lucene.index.NumericDocValues>();
	  private readonly IDictionary<int?, Org.apache.lucene.index.BinaryDocValues> BinaryInstances = new Dictionary<int?, Org.apache.lucene.index.BinaryDocValues>();
	  private readonly IDictionary<int?, Org.apache.lucene.util.fst.FST<long?>> FstInstances = new Dictionary<int?, Org.apache.lucene.util.fst.FST<long?>>();

	  private readonly int MaxDoc;
	  private readonly AtomicLong RamBytesUsed_Renamed;

	  internal const sbyte NUMBER = 0;
	  internal const sbyte BYTES = 1;
	  internal const sbyte Org;

	  internal const int BLOCK_SIZE = 4096;

	  internal const sbyte DELTA_COMPRESSED = 0;
	  internal const sbyte TABLE_COMPRESSED = 1;
	  internal const sbyte UNCOMPRESSED = 2;
	  internal const sbyte GCD_COMPRESSED = 3;

	  internal const int VERSION_START = 0;
	  internal const int VERSION_GCD_COMPRESSION = 1;
	  internal const int VERSION_CHECKSUM = 2;
	  internal const int VERSION_CURRENT = VERSION_CHECKSUM;

	  internal Lucene42DocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
	  {
		MaxDoc = state.SegmentInfo.DocCount;
		string metaName = Org.apache.lucene.index.IndexFileNames.segmentFileName(state.SegmentInfo.name, state.SegmentSuffix, metaExtension);
		// read in the entries from the metadata file.
		Org.apache.lucene.store.ChecksumIndexInput @in = state.Directory.openChecksumInput(metaName, state.Context);
		bool success = false;
		RamBytesUsed_Renamed = new AtomicLong(Org.apache.lucene.util.RamUsageEstimator.shallowSizeOfInstance(this.GetType()));
		try
		{
		  Version = Org.apache.lucene.codecs.CodecUtil.checkHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
		  Numerics = new Dictionary<>();
		  Binaries = new Dictionary<>();
		  Fsts = new Dictionary<>();
		  ReadFields(@in, state.FieldInfos);

		  if (Version >= VERSION_CHECKSUM)
		  {
			Org.apache.lucene.codecs.CodecUtil.checkFooter(@in);
		  }
		  else
		  {
			Org.apache.lucene.codecs.CodecUtil.checkEOF(@in);
		  }

		  success = true;
		}
		finally
		{
		  if (success)
		  {
			Org.apache.lucene.util.IOUtils.close(@in);
		  }
		  else
		  {
			Org.apache.lucene.util.IOUtils.closeWhileHandlingException(@in);
		  }
		}

		success = false;
		try
		{
		  string dataName = Org.apache.lucene.index.IndexFileNames.segmentFileName(state.SegmentInfo.name, state.SegmentSuffix, dataExtension);
		  Data = state.Directory.openInput(dataName, state.Context);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int version2 = Lucene.Net.Codecs.CodecUtil.checkHeader(data, dataCodec, VERSION_START, VERSION_CURRENT);
		  int version2 = Org.apache.lucene.codecs.CodecUtil.checkHeader(Data, dataCodec, VERSION_START, VERSION_CURRENT);
		  if (Version != version2)
		  {
			throw new CorruptIndexException("Format versions mismatch");
		  }

		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			Org.apache.lucene.util.IOUtils.closeWhileHandlingException(this.Data);
		  }
		}
	  }

	  private void ReadFields(IndexInput meta, FieldInfos infos)
	  {
		int fieldNumber = meta.ReadVInt();
		while (fieldNumber != -1)
		{
		  // check should be: infos.fieldInfo(fieldNumber) != null, which incorporates negative check
		  // but docvalues updates are currently buggy here (loading extra stuff, etc): LUCENE-5616
		  if (fieldNumber < 0)
		  {
			// trickier to validate more: because we re-use for norms, because we use multiple entries
			// for "composite" types like sortedset, etc.
			throw new CorruptIndexException("Invalid field number: " + fieldNumber + ", input=" + meta);
		  }
		  int fieldType = meta.ReadByte();
		  if (fieldType == NUMBER)
		  {
			NumericEntry entry = new NumericEntry();
			entry.Offset = meta.ReadLong();
			entry.Format = meta.ReadByte();
			switch (entry.Format)
			{
			  case DELTA_COMPRESSED:
			  case TABLE_COMPRESSED:
			  case GCD_COMPRESSED:
			  case UNCOMPRESSED:
				   break;
			  default:
				   throw new CorruptIndexException("Unknown format: " + entry.Format + ", input=" + meta);
			}
			if (entry.Format != UNCOMPRESSED)
			{
			  entry.PackedIntsVersion = meta.ReadVInt();
			}
			Numerics[fieldNumber] = entry;
		  }
		  else if (fieldType == BYTES)
		  {
			BinaryEntry entry = new BinaryEntry();
			entry.Offset = meta.ReadLong();
			entry.NumBytes = meta.ReadLong();
			entry.MinLength = meta.ReadVInt();
			entry.MaxLength = meta.ReadVInt();
			if (entry.MinLength != entry.MaxLength)
			{
			  entry.PackedIntsVersion = meta.ReadVInt();
			  entry.BlockSize = meta.ReadVInt();
			}
			Binaries[fieldNumber] = entry;
		  }
		  else if (fieldType == Org.apache.lucene.util.fst.FST)
		  {
			FSTEntry entry = new FSTEntry();
			entry.Offset = meta.ReadLong();
			entry.NumOrds = meta.ReadVLong();
			Fsts[fieldNumber] = entry;
		  }
		  else
		  {
			throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" + meta);
		  }
		  fieldNumber = meta.ReadVInt();
		}
	  }

	  public override NumericDocValues GetNumeric(FieldInfo field)
	  {
		  lock (this)
		  {
			Org.apache.lucene.index.NumericDocValues instance = NumericInstances[field.Number];
			if (instance == null)
			{
			  instance = LoadNumeric(field);
			  NumericInstances[field.Number] = instance;
			}
			return instance;
		  }
	  }

	  public override long RamBytesUsed()
	  {
		return RamBytesUsed_Renamed.get();
	  }

	  public override void CheckIntegrity()
	  {
		if (Version >= VERSION_CHECKSUM)
		{
		  Org.apache.lucene.codecs.CodecUtil.checksumEntireFile(Data);
		}
	  }

	  private NumericDocValues LoadNumeric(FieldInfo field)
	  {
		NumericEntry entry = Numerics[field.Number];
		Data.Seek(entry.Offset);
		switch (entry.Format)
		{
		  case TABLE_COMPRESSED:
			int size = Data.ReadVInt();
			if (size > 256)
			{
			  throw new CorruptIndexException("TABLE_COMPRESSED cannot have more than 256 distinct values, input=" + Data);
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long decode[] = new long[size];
			long[] decode = new long[size];
			for (int i = 0; i < decode.Length; i++)
			{
			  decode[i] = Data.ReadLong();
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int formatID = data.readVInt();
			int formatID = Data.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bitsPerValue = data.readVInt();
			int bitsPerValue = Data.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.PackedInts.Reader ordsReader = Lucene.Net.Util.Packed.PackedInts.getReaderNoHeader(data, Lucene.Net.Util.Packed.PackedInts.Format.byId(formatID), entry.packedIntsVersion, maxDoc, bitsPerValue);
			Org.apache.lucene.util.packed.PackedInts.Reader ordsReader = Org.apache.lucene.util.packed.PackedInts.getReaderNoHeader(Data, Org.apache.lucene.util.packed.PackedInts.Format.byId(formatID), entry.PackedIntsVersion, MaxDoc, bitsPerValue);
			RamBytesUsed_Renamed.addAndGet(Org.apache.lucene.util.RamUsageEstimator.sizeOf(decode) + ordsReader.RamBytesUsed());
			return new NumericDocValuesAnonymousInnerClassHelper(this, decode, ordsReader);
		  case DELTA_COMPRESSED:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int blockSize = data.readVInt();
			int blockSize = Data.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.BlockPackedReader reader = new Lucene.Net.Util.Packed.BlockPackedReader(data, entry.packedIntsVersion, blockSize, maxDoc, false);
			Org.apache.lucene.util.packed.BlockPackedReader reader = new BlockPackedReader(Data, entry.PackedIntsVersion, blockSize, MaxDoc, false);
			RamBytesUsed_Renamed.addAndGet(reader.RamBytesUsed());
			return reader;
		  case UNCOMPRESSED:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte bytes[] = new byte[maxDoc];
			sbyte[] bytes = new sbyte[MaxDoc];
			Data.ReadBytes(bytes, 0, bytes.Length);
			RamBytesUsed_Renamed.addAndGet(Org.apache.lucene.util.RamUsageEstimator.sizeOf(bytes));
			return new NumericDocValuesAnonymousInnerClassHelper2(this, bytes);
		  case GCD_COMPRESSED:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long min = data.readLong();
			long min = Data.ReadLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long mult = data.readLong();
			long mult = Data.ReadLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int quotientBlockSize = data.readVInt();
			int quotientBlockSize = Data.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.BlockPackedReader quotientReader = new Lucene.Net.Util.Packed.BlockPackedReader(data, entry.packedIntsVersion, quotientBlockSize, maxDoc, false);
			Org.apache.lucene.util.packed.BlockPackedReader quotientReader = new BlockPackedReader(Data, entry.PackedIntsVersion, quotientBlockSize, MaxDoc, false);
			RamBytesUsed_Renamed.addAndGet(quotientReader.RamBytesUsed());
			return new NumericDocValuesAnonymousInnerClassHelper3(this, min, mult, quotientReader);
		  default:
			throw new AssertionError();
		}
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
	  {
		  private readonly Lucene42DocValuesProducer OuterInstance;

		  private long[] Decode;
		  private PackedInts.Reader OrdsReader;

		  public NumericDocValuesAnonymousInnerClassHelper(Lucene42DocValuesProducer outerInstance, long[] decode, PackedInts.Reader ordsReader)
		  {
			  this.OuterInstance = outerInstance;
			  this.Decode = decode;
			  this.OrdsReader = ordsReader;
		  }

		  public override long Get(int docID)
		  {
			return Decode[(int)OrdsReader.Get(docID)];
		  }
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
	  {
		  private readonly Lucene42DocValuesProducer OuterInstance;

		  private sbyte[] Bytes;

		  public NumericDocValuesAnonymousInnerClassHelper2(Lucene42DocValuesProducer outerInstance, sbyte[] bytes)
		  {
			  this.OuterInstance = outerInstance;
			  this.Bytes = bytes;
		  }

		  public override long Get(int docID)
		  {
			return Bytes[docID];
		  }
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
	  {
		  private readonly Lucene42DocValuesProducer OuterInstance;

		  private long Min;
		  private long Mult;
		  private BlockPackedReader QuotientReader;

		  public NumericDocValuesAnonymousInnerClassHelper3(Lucene42DocValuesProducer outerInstance, long min, long mult, BlockPackedReader quotientReader)
		  {
			  this.OuterInstance = outerInstance;
			  this.Min = min;
			  this.Mult = mult;
			  this.QuotientReader = quotientReader;
		  }

		  public override long Get(int docID)
		  {
			return Min + Mult * QuotientReader.Get(docID);
		  }
	  }

	  public override BinaryDocValues GetBinary(FieldInfo field)
	  {
		  lock (this)
		  {
			Org.apache.lucene.index.BinaryDocValues instance = BinaryInstances[field.Number];
			if (instance == null)
			{
			  instance = LoadBinary(field);
			  BinaryInstances[field.Number] = instance;
			}
			return instance;
		  }
	  }

	  private BinaryDocValues LoadBinary(FieldInfo field)
	  {
		BinaryEntry entry = Binaries[field.Number];
		Data.Seek(entry.Offset);
		Org.apache.lucene.util.PagedBytes bytes = new PagedBytes(16);
		bytes.Copy(Data, entry.NumBytes);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.PagedBytes.Reader bytesReader = bytes.freeze(true);
		Org.apache.lucene.util.PagedBytes.Reader bytesReader = bytes.Freeze(true);
		if (entry.MinLength == entry.MaxLength)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int fixedLength = entry.minLength;
		  int fixedLength = entry.MinLength;
		  RamBytesUsed_Renamed.addAndGet(bytes.RamBytesUsed());
		  return new BinaryDocValuesAnonymousInnerClassHelper(this, bytesReader, fixedLength);
		}
		else
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Packed.MonotonicBlockPackedReader addresses = new Lucene.Net.Util.Packed.MonotonicBlockPackedReader(data, entry.packedIntsVersion, entry.blockSize, maxDoc, false);
		  Org.apache.lucene.util.packed.MonotonicBlockPackedReader addresses = new MonotonicBlockPackedReader(Data, entry.PackedIntsVersion, entry.BlockSize, MaxDoc, false);
		  RamBytesUsed_Renamed.addAndGet(bytes.RamBytesUsed() + addresses.RamBytesUsed());
		  return new BinaryDocValuesAnonymousInnerClassHelper2(this, bytesReader, addresses);
		}
	  }

	  private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
	  {
		  private readonly Lucene42DocValuesProducer OuterInstance;

		  private PagedBytes.Reader BytesReader;
		  private int FixedLength;

		  public BinaryDocValuesAnonymousInnerClassHelper(Lucene42DocValuesProducer outerInstance, PagedBytes.Reader bytesReader, int fixedLength)
		  {
			  this.OuterInstance = outerInstance;
			  this.BytesReader = bytesReader;
			  this.FixedLength = fixedLength;
		  }

		  public override void Get(int docID, BytesRef result)
		  {
			BytesReader.FillSlice(result, FixedLength * (long)docID, FixedLength);
		  }
	  }

	  private class BinaryDocValuesAnonymousInnerClassHelper2 : BinaryDocValues
	  {
		  private readonly Lucene42DocValuesProducer OuterInstance;

		  private PagedBytes.Reader BytesReader;
		  private MonotonicBlockPackedReader Addresses;

		  public BinaryDocValuesAnonymousInnerClassHelper2(Lucene42DocValuesProducer outerInstance, PagedBytes.Reader bytesReader, MonotonicBlockPackedReader addresses)
		  {
			  this.OuterInstance = outerInstance;
			  this.BytesReader = bytesReader;
			  this.Addresses = addresses;
		  }

		  public override void Get(int docID, BytesRef result)
		  {
			long startAddress = docID == 0 ? 0 : Addresses.Get(docID - 1);
			long endAddress = Addresses.Get(docID);
			BytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
		  }
	  }

	  public override SortedDocValues GetSorted(FieldInfo field)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FSTEntry entry = fsts.get(field.number);
		FSTEntry entry = Fsts[field.Number];
		Org.apache.lucene.util.fst.FST<long?> instance;
		lock (this)
		{
		  instance = FstInstances[field.Number];
		  if (instance == null)
		  {
			Data.Seek(entry.Offset);
			instance = new FST<>(Data, Org.apache.lucene.util.fst.PositiveIntOutputs.Singleton);
			RamBytesUsed_Renamed.addAndGet(instance.SizeInBytes());
			FstInstances[field.Number] = instance;
		  }
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.NumericDocValues docToOrd = getNumeric(field);
		Org.apache.lucene.index.NumericDocValues docToOrd = GetNumeric(field);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST<Long> fst = instance;
		Org.apache.lucene.util.fst.FST<long?> fst = instance;

		// per-thread resources
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.BytesReader in = fst.getBytesReader();
		Org.apache.lucene.util.fst.FST.BytesReader @in = fst.BytesReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Long> firstArc = new Lucene.Net.Util.Fst.FST.Arc<>();
		Org.apache.lucene.util.fst.FST.Arc<long?> firstArc = new FST.Arc<long?>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Long> scratchArc = new Lucene.Net.Util.Fst.FST.Arc<>();
		Org.apache.lucene.util.fst.FST.Arc<long?> scratchArc = new FST.Arc<long?>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.IntsRef scratchInts = new Lucene.Net.Util.IntsRef();
		Org.apache.lucene.util.IntsRef scratchInts = new IntsRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.BytesRefFSTEnum<Long> fstEnum = new Lucene.Net.Util.Fst.BytesRefFSTEnum<>(fst);
		Org.apache.lucene.util.fst.BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);

		return new SortedDocValuesAnonymousInnerClassHelper(this, entry, docToOrd, fst, @in, firstArc, scratchArc, scratchInts, fstEnum);
	  }

	  private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
	  {
		  private readonly Lucene42DocValuesProducer OuterInstance;

		  private Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.FSTEntry Entry;
		  private NumericDocValues DocToOrd;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private Lucene.Net.Util.Fst.FST<long?> fst;
		  private FST<long?> Fst;
		  private FST.BytesReader @in;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private Lucene.Net.Util.Fst.FST.Arc<long?> firstArc;
		  private FST.Arc<long?> FirstArc;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private Lucene.Net.Util.Fst.FST.Arc<long?> scratchArc;
		  private FST.Arc<long?> ScratchArc;
		  private IntsRef ScratchInts;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private Lucene.Net.Util.Fst.BytesRefFSTEnum<long?> fstEnum;
		  private BytesRefFSTEnum<long?> FstEnum;

		  public SortedDocValuesAnonymousInnerClassHelper<T1, T2, T3, T4>(Lucene42DocValuesProducer outerInstance, Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.FSTEntry entry, NumericDocValues docToOrd, FST<T1> fst, FST.BytesReader @in, FST.Arc<T2> firstArc, FST.Arc<T3> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<T4> fstEnum)
		  {
			  this.OuterInstance = outerInstance;
			  this.Entry = entry;
			  this.DocToOrd = docToOrd;
			  this.Fst = fst;
			  this.@in = @in;
			  this.FirstArc = firstArc;
			  this.ScratchArc = scratchArc;
			  this.ScratchInts = scratchInts;
			  this.FstEnum = fstEnum;
		  }

		  public override int GetOrd(int docID)
		  {
			return (int) DocToOrd.Get(docID);
		  }

		  public override void LookupOrd(int ord, BytesRef result)
		  {
			try
			{
			  @in.Position = 0;
			  Fst.GetFirstArc(FirstArc);
			  Org.apache.lucene.util.IntsRef output = Org.apache.lucene.util.fst.Util.getByOutput(Fst, ord, @in, FirstArc, ScratchArc, ScratchInts);
			  result.Bytes = new sbyte[output.Length];
			  result.Offset = 0;
			  result.Length = 0;
			  Org.apache.lucene.util.fst.Util.toBytesRef(output, result);
			}
			catch (IOException bogus)
			{
			  throw new Exception(bogus);
			}
		  }

		  public override int LookupTerm(BytesRef key)
		  {
			try
			{
			  Org.apache.lucene.util.fst.BytesRefFSTEnum.InputOutput<long?> o = FstEnum.SeekCeil(key);
			  if (o == null)
			  {
				return -ValueCount - 1;
			  }
			  else if (o.Input.Equals(key))
			  {
				return (int)o.Output;
			  }
			  else
			  {
				return (int) - o.Output - 1;
			  }
			}
			catch (IOException bogus)
			{
			  throw new Exception(bogus);
			}
		  }

		  public override int ValueCount
		  {
			  get
			  {
				return (int)Entry.NumOrds;
			  }
		  }

		  public override TermsEnum TermsEnum()
		  {
			return new FSTTermsEnum(Fst);
		  }
	  }

	  public override SortedSetDocValues GetSortedSet(FieldInfo field)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FSTEntry entry = fsts.get(field.number);
		FSTEntry entry = Fsts[field.Number];
		if (entry.NumOrds == 0)
		{
		  return Org.apache.lucene.index.DocValues.EMPTY_SORTED_SET; // empty FST!
		}
		Org.apache.lucene.util.fst.FST<long?> instance;
		lock (this)
		{
		  instance = FstInstances[field.Number];
		  if (instance == null)
		  {
			Data.Seek(entry.Offset);
			instance = new FST<>(Data, Org.apache.lucene.util.fst.PositiveIntOutputs.Singleton);
			RamBytesUsed_Renamed.addAndGet(instance.SizeInBytes());
			FstInstances[field.Number] = instance;
		  }
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.BinaryDocValues docToOrds = getBinary(field);
		Org.apache.lucene.index.BinaryDocValues docToOrds = GetBinary(field);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST<Long> fst = instance;
		Org.apache.lucene.util.fst.FST<long?> fst = instance;

		// per-thread resources
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.BytesReader in = fst.getBytesReader();
		Org.apache.lucene.util.fst.FST.BytesReader @in = fst.BytesReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Long> firstArc = new Lucene.Net.Util.Fst.FST.Arc<>();
		Org.apache.lucene.util.fst.FST.Arc<long?> firstArc = new FST.Arc<long?>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Long> scratchArc = new Lucene.Net.Util.Fst.FST.Arc<>();
		Org.apache.lucene.util.fst.FST.Arc<long?> scratchArc = new FST.Arc<long?>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.IntsRef scratchInts = new Lucene.Net.Util.IntsRef();
		Org.apache.lucene.util.IntsRef scratchInts = new IntsRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.BytesRefFSTEnum<Long> fstEnum = new Lucene.Net.Util.Fst.BytesRefFSTEnum<>(fst);
		Org.apache.lucene.util.fst.BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef ref = new Lucene.Net.Util.BytesRef();
		Org.apache.lucene.util.BytesRef @ref = new BytesRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Store.ByteArrayDataInput input = new Lucene.Net.Store.ByteArrayDataInput();
		Org.apache.lucene.store.ByteArrayDataInput input = new ByteArrayDataInput();
		return new SortedSetDocValuesAnonymousInnerClassHelper(this, entry, docToOrds, fst, @in, firstArc, scratchArc, scratchInts, fstEnum, @ref, input);
	  }

	  private class SortedSetDocValuesAnonymousInnerClassHelper : SortedSetDocValues
	  {
		  private readonly Lucene42DocValuesProducer OuterInstance;

		  private Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.FSTEntry Entry;
		  private BinaryDocValues DocToOrds;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private Lucene.Net.Util.Fst.FST<long?> fst;
		  private FST<long?> Fst;
		  private FST.BytesReader @in;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private Lucene.Net.Util.Fst.FST.Arc<long?> firstArc;
		  private FST.Arc<long?> FirstArc;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private Lucene.Net.Util.Fst.FST.Arc<long?> scratchArc;
		  private FST.Arc<long?> ScratchArc;
		  private IntsRef ScratchInts;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private Lucene.Net.Util.Fst.BytesRefFSTEnum<long?> fstEnum;
		  private BytesRefFSTEnum<long?> FstEnum;
		  private BytesRef @ref;
		  private ByteArrayDataInput Input;

		  public SortedSetDocValuesAnonymousInnerClassHelper<T1, T2, T3, T4>(Lucene42DocValuesProducer outerInstance, Lucene.Net.Codecs.Lucene42.Lucene42DocValuesProducer.FSTEntry entry, BinaryDocValues docToOrds, FST<T1> fst, FST.BytesReader @in, FST.Arc<T2> firstArc, FST.Arc<T3> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<T4> fstEnum, BytesRef @ref, ByteArrayDataInput input)
		  {
			  this.OuterInstance = outerInstance;
			  this.Entry = entry;
			  this.DocToOrds = docToOrds;
			  this.Fst = fst;
			  this.@in = @in;
			  this.FirstArc = firstArc;
			  this.ScratchArc = scratchArc;
			  this.ScratchInts = scratchInts;
			  this.FstEnum = fstEnum;
			  this.@ref = @ref;
			  this.Input = input;
		  }

		  internal long currentOrd;

		  public override long NextOrd()
		  {
			if (Input.Eof())
			{
			  return NO_MORE_ORDS;
			}
			else
			{
			  currentOrd += Input.ReadVLong();
			  return currentOrd;
			}
		  }

		  public override int Document
		  {
			  set
			  {
				DocToOrds.Get(value, @ref);
				Input.Reset(@ref.Bytes, @ref.Offset, @ref.Length);
				currentOrd = 0;
			  }
		  }

		  public override void LookupOrd(long ord, BytesRef result)
		  {
			try
			{
			  @in.Position = 0;
			  Fst.GetFirstArc(FirstArc);
			  Org.apache.lucene.util.IntsRef output = Org.apache.lucene.util.fst.Util.getByOutput(Fst, ord, @in, FirstArc, ScratchArc, ScratchInts);
			  result.Bytes = new sbyte[output.Length];
			  result.Offset = 0;
			  result.Length = 0;
			  Org.apache.lucene.util.fst.Util.toBytesRef(output, result);
			}
			catch (IOException bogus)
			{
			  throw new Exception(bogus);
			}
		  }

		  public override long LookupTerm(BytesRef key)
		  {
			try
			{
			  Org.apache.lucene.util.fst.BytesRefFSTEnum.InputOutput<long?> o = FstEnum.SeekCeil(key);
			  if (o == null)
			  {
				return -ValueCount - 1;
			  }
			  else if (o.Input.Equals(key))
			  {
				return (int)o.Output;
			  }
			  else
			  {
				return -o.Output - 1;
			  }
			}
			catch (IOException bogus)
			{
			  throw new Exception(bogus);
			}
		  }

		  public override long ValueCount
		  {
			  get
			  {
				return Entry.NumOrds;
			  }
		  }

		  public override TermsEnum TermsEnum()
		  {
			return new FSTTermsEnum(Fst);
		  }
	  }

	  public override Bits GetDocsWithField(FieldInfo field)
	  {
		if (field.DocValuesType == Org.apache.lucene.index.FieldInfo.DocValuesType.SORTED_SET)
		{
		  return Org.apache.lucene.index.DocValues.docsWithValue(GetSortedSet(field), MaxDoc);
		}
		else
		{
		  return new Lucene.Net.Util.Bits_MatchAllBits(MaxDoc);
		}
	  }

	  public override void Close()
	  {
		Data.Close();
	  }

	  internal class NumericEntry
	  {
		internal long Offset;
		internal sbyte Format;
		internal int PackedIntsVersion;
	  }

	  internal class BinaryEntry
	  {
		internal long Offset;
		internal long NumBytes;
		internal int MinLength;
		internal int MaxLength;
		internal int PackedIntsVersion;
		internal int BlockSize;
	  }

	  internal class FSTEntry
	  {
		internal long Offset;
		internal long NumOrds;
	  }

	  // exposes FSTEnum directly as a TermsEnum: avoids binary-search next()
	  internal class FSTTermsEnum : TermsEnum
	  {
		internal readonly BytesRefFSTEnum<long?> @in;

		// this is all for the complicated seek(ord)...
		// maybe we should add a FSTEnum that supports this operation?
		internal readonly FST<long?> Fst;
		internal readonly FST.BytesReader BytesReader;
		internal readonly FST.Arc<long?> FirstArc = new FST.Arc<long?>();
		internal readonly FST.Arc<long?> ScratchArc = new FST.Arc<long?>();
		internal readonly IntsRef ScratchInts = new IntsRef();
		internal readonly BytesRef ScratchBytes = new BytesRef();

		internal FSTTermsEnum(FST<long?> fst)
		{
		  this.Fst = fst;
		  @in = new BytesRefFSTEnum<>(fst);
		  BytesReader = fst.BytesReader;
		}

		public override BytesRef Next()
		{
		  Org.apache.lucene.util.fst.BytesRefFSTEnum.InputOutput<long?> io = @in.Next();
		  if (io == Org.apache.lucene.util.BytesRefIterator_Fields.null)
		  {
			return Org.apache.lucene.util.BytesRefIterator_Fields.null;
		  }
		  else
		  {
			return io.Input;
		  }
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return Org.apache.lucene.util.BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

		public override SeekStatus SeekCeil(BytesRef text)
		{
		  if (@in.SeekCeil(text) == Org.apache.lucene.util.BytesRefIterator_Fields.null)
		  {
			return SeekStatus.END;
		  }
		  else if (Term().Equals(text))
		  {
			// TODO: add SeekStatus to FSTEnum like in https://issues.apache.org/jira/browse/LUCENE-3729
			// to remove this comparision?
			return SeekStatus.FOUND;
		  }
		  else
		  {
			return SeekStatus.NOT_FOUND;
		  }
		}

		public override bool SeekExact(BytesRef text)
		{
		  if (@in.SeekExact(text) == Org.apache.lucene.util.BytesRefIterator_Fields.null)
		  {
			return false;
		  }
		  else
		  {
			return true;
		  }
		}

		public override void SeekExact(long ord)
		{
		  // TODO: would be better to make this simpler and faster.
		  // but we dont want to introduce a bug that corrupts our enum state!
		  BytesReader.Position = 0;
		  Fst.GetFirstArc(FirstArc);
		  Org.apache.lucene.util.IntsRef output = Org.apache.lucene.util.fst.Util.getByOutput(Fst, ord, BytesReader, FirstArc, ScratchArc, ScratchInts);
		  ScratchBytes.Bytes = new sbyte[output.Length];
		  ScratchBytes.Offset = 0;
		  ScratchBytes.Length = 0;
		  Org.apache.lucene.util.fst.Util.toBytesRef(output, ScratchBytes);
		  // TODO: we could do this lazily, better to try to push into FSTEnum though?
		  @in.SeekExact(ScratchBytes);
		}

		public override BytesRef Term()
		{
		  return @in.Current().Input;
		}

		public override long Ord()
		{
		  return @in.Current().Output;
		}

		public override int DocFreq()
		{
		  throw new System.NotSupportedException();
		}

		public override long TotalTermFreq()
		{
		  throw new System.NotSupportedException();
		}

		public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
		{
		  throw new System.NotSupportedException();
		}

		public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		{
		  throw new System.NotSupportedException();
		}
	  }
	}

}