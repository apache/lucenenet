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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Codecs.Sep;
using org.apache.lucene.codecs.memory;

namespace Lucene.Net.Codecs.Memory
{
    /// <summary>
	/// Reader for <seealso cref="MemoryDocValuesFormat"/>
	/// </summary>
	internal class MemoryDocValuesProducer : DocValuesProducer
	{
	  // metadata maps (just file pointers and minimal stuff)
	  private readonly IDictionary<int?, NumericEntry> numerics;
	  private readonly IDictionary<int?, BinaryEntry> binaries;
	  private readonly IDictionary<int?, FSTEntry> fsts;
	  private readonly IndexInput data;

	  // ram instances we have already loaded
	  private readonly IDictionary<int?, NumericDocValues> numericInstances = new Dictionary<int?, NumericDocValues>();
	  private readonly IDictionary<int?, BinaryDocValues> binaryInstances = new Dictionary<int?, BinaryDocValues>();
	  private readonly IDictionary<int?, FST<long?>> fstInstances = new Dictionary<int?, FST<long?>>();
	  private readonly IDictionary<int?, Bits> docsWithFieldInstances = new Dictionary<int?, Bits>();

	  private readonly int maxDoc;
	  private readonly AtomicLong ramBytesUsed_Renamed;
	  private readonly int version;

	  internal const sbyte NUMBER = 0;
	  internal const sbyte BYTES = 1;
	  internal const sbyte org;

	  internal const int BLOCK_SIZE = 4096;

	  internal const sbyte DELTA_COMPRESSED = 0;
	  internal const sbyte TABLE_COMPRESSED = 1;
	  internal const sbyte UNCOMPRESSED = 2;
	  internal const sbyte GCD_COMPRESSED = 3;

	  internal const int VERSION_START = 0;
	  internal const int VERSION_GCD_COMPRESSION = 1;
	  internal const int VERSION_CHECKSUM = 2;
	  internal const int VERSION_CURRENT = VERSION_CHECKSUM;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: MemoryDocValuesProducer(org.apache.lucene.index.SegmentReadState state, String dataCodec, String dataExtension, String metaCodec, String metaExtension) throws java.io.IOException
	  internal MemoryDocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
	  {
		maxDoc = state.segmentInfo.DocCount;
		string metaName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, metaExtension);
		// read in the entries from the metadata file.
		ChecksumIndexInput @in = state.directory.openChecksumInput(metaName, state.context);
		bool success = false;
		try
		{
		  version = CodecUtil.checkHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
		  numerics = new Dictionary<>();
		  binaries = new Dictionary<>();
		  fsts = new Dictionary<>();
		  readFields(@in, state.fieldInfos);
		  if (version >= VERSION_CHECKSUM)
		  {
			CodecUtil.checkFooter(@in);
		  }
		  else
		  {
			CodecUtil.checkEOF(@in);
		  }
		  ramBytesUsed_Renamed = new AtomicLong(RamUsageEstimator.shallowSizeOfInstance(this.GetType()));
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(@in);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(@in);
		  }
		}

		success = false;
		try
		{
		  string dataName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, dataExtension);
		  data = state.directory.openInput(dataName, state.context);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int version2 = org.apache.lucene.codecs.CodecUtil.checkHeader(data, dataCodec, VERSION_START, VERSION_CURRENT);
		  int version2 = CodecUtil.checkHeader(data, dataCodec, VERSION_START, VERSION_CURRENT);
		  if (version != version2)
		  {
			throw new CorruptIndexException("Format versions mismatch");
		  }

		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(this.data);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readFields(org.apache.lucene.store.IndexInput meta, org.apache.lucene.index.FieldInfos infos) throws java.io.IOException
	  private void readFields(IndexInput meta, FieldInfos infos)
	  {
		int fieldNumber = meta.readVInt();
		while (fieldNumber != -1)
		{
		  int fieldType = meta.readByte();
		  if (fieldType == NUMBER)
		  {
			NumericEntry entry = new NumericEntry();
			entry.offset = meta.readLong();
			entry.missingOffset = meta.readLong();
			if (entry.missingOffset != -1)
			{
			  entry.missingBytes = meta.readLong();
			}
			else
			{
			  entry.missingBytes = 0;
			}
			entry.format = meta.readByte();
			switch (entry.format)
			{
			  case DELTA_COMPRESSED:
			  case TABLE_COMPRESSED:
			  case GCD_COMPRESSED:
			  case UNCOMPRESSED:
				   break;
			  default:
				   throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" + meta);
			}
			if (entry.format != UNCOMPRESSED)
			{
			  entry.packedIntsVersion = meta.readVInt();
			}
			numerics[fieldNumber] = entry;
		  }
		  else if (fieldType == BYTES)
		  {
			BinaryEntry entry = new BinaryEntry();
			entry.offset = meta.readLong();
			entry.numBytes = meta.readLong();
			entry.missingOffset = meta.readLong();
			if (entry.missingOffset != -1)
			{
			  entry.missingBytes = meta.readLong();
			}
			else
			{
			  entry.missingBytes = 0;
			}
			entry.minLength = meta.readVInt();
			entry.maxLength = meta.readVInt();
			if (entry.minLength != entry.maxLength)
			{
			  entry.packedIntsVersion = meta.readVInt();
			  entry.blockSize = meta.readVInt();
			}
			binaries[fieldNumber] = entry;
		  }
		  else if (fieldType == FST)
		  {
			FSTEntry entry = new FSTEntry();
			entry.offset = meta.readLong();
			entry.numOrds = meta.readVLong();
			fsts[fieldNumber] = entry;
		  }
		  else
		  {
			throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" + meta);
		  }
		  fieldNumber = meta.readVInt();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public synchronized org.apache.lucene.index.NumericDocValues getNumeric(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override NumericDocValues getNumeric(FieldInfo field)
	  {
		  lock (this)
		  {
			NumericDocValues instance = numericInstances[field.number];
			if (instance == null)
			{
			  instance = loadNumeric(field);
			  numericInstances[field.number] = instance;
			}
			return instance;
		  }
	  }

	  public override long ramBytesUsed()
	  {
		return ramBytesUsed_Renamed.get();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void checkIntegrity() throws java.io.IOException
	  public override void checkIntegrity()
	  {
		if (version >= VERSION_CHECKSUM)
		{
		  CodecUtil.checksumEntireFile(data);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.index.NumericDocValues loadNumeric(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  private NumericDocValues loadNumeric(FieldInfo field)
	  {
		NumericEntry entry = numerics[field.number];
		data.seek(entry.offset + entry.missingBytes);
		switch (entry.format)
		{
		  case TABLE_COMPRESSED:
			int size = data.readVInt();
			if (size > 256)
			{
			  throw new CorruptIndexException("TABLE_COMPRESSED cannot have more than 256 distinct values, input=" + data);
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long decode[] = new long[size];
			long[] decode = new long[size];
			for (int i = 0; i < decode.Length; i++)
			{
			  decode[i] = data.readLong();
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int formatID = data.readVInt();
			int formatID = data.readVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bitsPerValue = data.readVInt();
			int bitsPerValue = data.readVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.PackedInts.Reader ordsReader = org.apache.lucene.util.packed.PackedInts.getReaderNoHeader(data, org.apache.lucene.util.packed.PackedInts.Format.byId(formatID), entry.packedIntsVersion, maxDoc, bitsPerValue);
			PackedInts.Reader ordsReader = PackedInts.getReaderNoHeader(data, PackedInts.Format.byId(formatID), entry.packedIntsVersion, maxDoc, bitsPerValue);
			ramBytesUsed_Renamed.addAndGet(RamUsageEstimator.sizeOf(decode) + ordsReader.ramBytesUsed());
			return new NumericDocValuesAnonymousInnerClassHelper(this, decode, ordsReader);
		  case DELTA_COMPRESSED:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int blockSize = data.readVInt();
			int blockSize = data.readVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.BlockPackedReader reader = new org.apache.lucene.util.packed.BlockPackedReader(data, entry.packedIntsVersion, blockSize, maxDoc, false);
			BlockPackedReader reader = new BlockPackedReader(data, entry.packedIntsVersion, blockSize, maxDoc, false);
			ramBytesUsed_Renamed.addAndGet(reader.ramBytesUsed());
			return reader;
		  case UNCOMPRESSED:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte bytes[] = new byte[maxDoc];
			sbyte[] bytes = new sbyte[maxDoc];
			data.readBytes(bytes, 0, bytes.Length);
			ramBytesUsed_Renamed.addAndGet(RamUsageEstimator.sizeOf(bytes));
			return new NumericDocValuesAnonymousInnerClassHelper2(this, bytes);
		  case GCD_COMPRESSED:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long min = data.readLong();
			long min = data.readLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long mult = data.readLong();
			long mult = data.readLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int quotientBlockSize = data.readVInt();
			int quotientBlockSize = data.readVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.BlockPackedReader quotientReader = new org.apache.lucene.util.packed.BlockPackedReader(data, entry.packedIntsVersion, quotientBlockSize, maxDoc, false);
			BlockPackedReader quotientReader = new BlockPackedReader(data, entry.packedIntsVersion, quotientBlockSize, maxDoc, false);
			ramBytesUsed_Renamed.addAndGet(quotientReader.ramBytesUsed());
			return new NumericDocValuesAnonymousInnerClassHelper3(this, min, mult, quotientReader);
		  default:
			throw new AssertionError();
		}
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
	  {
		  private readonly MemoryDocValuesProducer outerInstance;

		  private long[] decode;
		  private IntIndexInput.Reader ordsReader;

		  public NumericDocValuesAnonymousInnerClassHelper(MemoryDocValuesProducer outerInstance, long[] decode, IntIndexInput.Reader ordsReader)
		  {
			  this.outerInstance = outerInstance;
			  this.decode = decode;
			  this.ordsReader = ordsReader;
		  }

		  public override long get(int docID)
		  {
			return decode[(int)ordsReader.get(docID)];
		  }
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
	  {
		  private readonly MemoryDocValuesProducer outerInstance;

		  private sbyte[] bytes;

		  public NumericDocValuesAnonymousInnerClassHelper2(MemoryDocValuesProducer outerInstance, sbyte[] bytes)
		  {
			  this.outerInstance = outerInstance;
			  this.bytes = bytes;
		  }

		  public override long get(int docID)
		  {
			return bytes[docID];
		  }
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
	  {
		  private readonly MemoryDocValuesProducer outerInstance;

		  private long min;
		  private long mult;
		  private BlockPackedReader quotientReader;

		  public NumericDocValuesAnonymousInnerClassHelper3(MemoryDocValuesProducer outerInstance, long min, long mult, BlockPackedReader quotientReader)
		  {
			  this.outerInstance = outerInstance;
			  this.min = min;
			  this.mult = mult;
			  this.quotientReader = quotientReader;
		  }

		  public override long get(int docID)
		  {
			return min + mult * quotientReader.get(docID);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public synchronized org.apache.lucene.index.BinaryDocValues getBinary(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override BinaryDocValues getBinary(FieldInfo field)
	  {
		  lock (this)
		  {
			BinaryDocValues instance = binaryInstances[field.number];
			if (instance == null)
			{
			  instance = loadBinary(field);
			  binaryInstances[field.number] = instance;
			}
			return instance;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.index.BinaryDocValues loadBinary(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  private BinaryDocValues loadBinary(FieldInfo field)
	  {
		BinaryEntry entry = binaries[field.number];
		data.seek(entry.offset);
		PagedBytes bytes = new PagedBytes(16);
		bytes.copy(data, entry.numBytes);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.PagedBytes.Reader bytesReader = bytes.freeze(true);
		PagedBytes.Reader bytesReader = bytes.freeze(true);
		if (entry.minLength == entry.maxLength)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int fixedLength = entry.minLength;
		  int fixedLength = entry.minLength;
		  ramBytesUsed_Renamed.addAndGet(bytes.ramBytesUsed());
		  return new BinaryDocValuesAnonymousInnerClassHelper(this, bytesReader, fixedLength);
		}
		else
		{
		  data.seek(data.FilePointer + entry.missingBytes);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.MonotonicBlockPackedReader addresses = new org.apache.lucene.util.packed.MonotonicBlockPackedReader(data, entry.packedIntsVersion, entry.blockSize, maxDoc, false);
		  MonotonicBlockPackedReader addresses = new MonotonicBlockPackedReader(data, entry.packedIntsVersion, entry.blockSize, maxDoc, false);
		  ramBytesUsed_Renamed.addAndGet(bytes.ramBytesUsed() + addresses.ramBytesUsed());
		  return new BinaryDocValuesAnonymousInnerClassHelper2(this, bytesReader, addresses);
		}
	  }

	  private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
	  {
		  private readonly MemoryDocValuesProducer outerInstance;

		  private IntIndexInput.Reader bytesReader;
		  private int fixedLength;

		  public BinaryDocValuesAnonymousInnerClassHelper(MemoryDocValuesProducer outerInstance, IntIndexInput.Reader bytesReader, int fixedLength)
		  {
			  this.outerInstance = outerInstance;
			  this.bytesReader = bytesReader;
			  this.fixedLength = fixedLength;
		  }

		  public override void get(int docID, BytesRef result)
		  {
			bytesReader.fillSlice(result, fixedLength * (long)docID, fixedLength);
		  }
	  }

	  private class BinaryDocValuesAnonymousInnerClassHelper2 : BinaryDocValues
	  {
		  private readonly MemoryDocValuesProducer outerInstance;

		  private IntIndexInput.Reader bytesReader;
		  private MonotonicBlockPackedReader addresses;

		  public BinaryDocValuesAnonymousInnerClassHelper2(MemoryDocValuesProducer outerInstance, IntIndexInput.Reader bytesReader, MonotonicBlockPackedReader addresses)
		  {
			  this.outerInstance = outerInstance;
			  this.bytesReader = bytesReader;
			  this.addresses = addresses;
		  }

		  public override void get(int docID, BytesRef result)
		  {
			long startAddress = docID == 0 ? 0 : addresses.get(docID - 1);
			long endAddress = addresses.get(docID);
			bytesReader.fillSlice(result, startAddress, (int)(endAddress - startAddress));
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.SortedDocValues getSorted(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override SortedDocValues getSorted(FieldInfo field)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FSTEntry entry = fsts.get(field.number);
		FSTEntry entry = fsts[field.number];
		if (entry.numOrds == 0)
		{
		  return DocValues.EMPTY_SORTED;
		}
		FST<long?> instance;
		lock (this)
		{
		  instance = fstInstances[field.number];
		  if (instance == null)
		  {
			data.seek(entry.offset);
			instance = new FST<>(data, PositiveIntOutputs.Singleton);
			ramBytesUsed_Renamed.addAndGet(instance.sizeInBytes());
			fstInstances[field.number] = instance;
		  }
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.NumericDocValues docToOrd = getNumeric(field);
		NumericDocValues docToOrd = getNumeric(field);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST<Long> fst = instance;
		FST<long?> fst = instance;

		// per-thread resources
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.BytesReader in = fst.getBytesReader();
		FST.BytesReader @in = fst.BytesReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Long> firstArc = new org.apache.lucene.util.fst.FST.Arc<>();
		FST.Arc<long?> firstArc = new FST.Arc<long?>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Long> scratchArc = new org.apache.lucene.util.fst.FST.Arc<>();
		FST.Arc<long?> scratchArc = new FST.Arc<long?>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.IntsRef scratchInts = new org.apache.lucene.util.IntsRef();
		IntsRef scratchInts = new IntsRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.BytesRefFSTEnum<Long> fstEnum = new org.apache.lucene.util.fst.BytesRefFSTEnum<>(fst);
		BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);

		return new SortedDocValuesAnonymousInnerClassHelper(this, entry, docToOrd, fst, @in, firstArc, scratchArc, scratchInts, fstEnum);
	  }

	  private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
	  {
		  private readonly MemoryDocValuesProducer outerInstance;

		  private MemoryDocValuesProducer.FSTEntry entry;
		  private NumericDocValues docToOrd;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.FST<long?> fst;
		  private FST<long?> fst;
		  private FST.BytesReader @in;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.FST.Arc<long?> firstArc;
		  private FST.Arc<long?> firstArc;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.FST.Arc<long?> scratchArc;
		  private FST.Arc<long?> scratchArc;
		  private IntsRef scratchInts;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.BytesRefFSTEnum<long?> fstEnum;
		  private BytesRefFSTEnum<long?> fstEnum;

		  public SortedDocValuesAnonymousInnerClassHelper<T1, T2, T3, T4>(MemoryDocValuesProducer outerInstance, org.MemoryDocValuesProducer.FSTEntry entry, NumericDocValues docToOrd, FST<T1> fst, FST.BytesReader @in, FST.Arc<T2> firstArc, FST.Arc<T3> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<T4> fstEnum)
		  {
			  this.outerInstance = outerInstance;
			  this.entry = entry;
			  this.docToOrd = docToOrd;
			  this.fst = fst;
			  this.@in = @in;
			  this.firstArc = firstArc;
			  this.scratchArc = scratchArc;
			  this.scratchInts = scratchInts;
			  this.fstEnum = fstEnum;
		  }

		  public override int getOrd(int docID)
		  {
			return (int) docToOrd.get(docID);
		  }

		  public override void lookupOrd(int ord, BytesRef result)
		  {
			try
			{
			  @in.Position = 0;
			  fst.getFirstArc(firstArc);
			  IntsRef output = Util.getByOutput(fst, ord, @in, firstArc, scratchArc, scratchInts);
			  result.bytes = new sbyte[output.length];
			  result.offset = 0;
			  result.length = 0;
			  Util.toBytesRef(output, result);
			}
			catch (IOException bogus)
			{
			  throw new Exception(bogus);
			}
		  }

		  public override int lookupTerm(BytesRef key)
		  {
			try
			{
			  BytesRefFSTEnum.InputOutput<long?> o = fstEnum.seekCeil(key);
			  if (o == null)
			  {
				return -ValueCount - 1;
			  }
			  else if (o.input.Equals(key))
			  {
				return (int)o.output;
			  }
			  else
			  {
				return (int) -o.output - 1;
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
				return (int)entry.numOrds;
			  }
		  }

		  public override TermsEnum termsEnum()
		  {
			return new FSTTermsEnum(fst);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.SortedSetDocValues getSortedSet(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override SortedSetDocValues getSortedSet(FieldInfo field)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FSTEntry entry = fsts.get(field.number);
		FSTEntry entry = fsts[field.number];
		if (entry.numOrds == 0)
		{
		  return DocValues.EMPTY_SORTED_SET; // empty FST!
		}
		FST<long?> instance;
		lock (this)
		{
		  instance = fstInstances[field.number];
		  if (instance == null)
		  {
			data.seek(entry.offset);
			instance = new FST<>(data, PositiveIntOutputs.Singleton);
			ramBytesUsed_Renamed.addAndGet(instance.sizeInBytes());
			fstInstances[field.number] = instance;
		  }
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.BinaryDocValues docToOrds = getBinary(field);
		BinaryDocValues docToOrds = getBinary(field);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST<Long> fst = instance;
		FST<long?> fst = instance;

		// per-thread resources
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.BytesReader in = fst.getBytesReader();
		FST.BytesReader @in = fst.BytesReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Long> firstArc = new org.apache.lucene.util.fst.FST.Arc<>();
		FST.Arc<long?> firstArc = new FST.Arc<long?>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Long> scratchArc = new org.apache.lucene.util.fst.FST.Arc<>();
		FST.Arc<long?> scratchArc = new FST.Arc<long?>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.IntsRef scratchInts = new org.apache.lucene.util.IntsRef();
		IntsRef scratchInts = new IntsRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.BytesRefFSTEnum<Long> fstEnum = new org.apache.lucene.util.fst.BytesRefFSTEnum<>(fst);
		BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.BytesRef ref = new org.apache.lucene.util.BytesRef();
		BytesRef @ref = new BytesRef();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.store.ByteArrayDataInput input = new org.apache.lucene.store.ByteArrayDataInput();
		ByteArrayDataInput input = new ByteArrayDataInput();
		return new SortedSetDocValuesAnonymousInnerClassHelper(this, entry, docToOrds, fst, @in, firstArc, scratchArc, scratchInts, fstEnum, @ref, input);
	  }

	  private class SortedSetDocValuesAnonymousInnerClassHelper : SortedSetDocValues
	  {
		  private readonly MemoryDocValuesProducer outerInstance;

		  private MemoryDocValuesProducer.FSTEntry entry;
		  private BinaryDocValues docToOrds;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.FST<long?> fst;
		  private FST<long?> fst;
		  private FST.BytesReader @in;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.FST.Arc<long?> firstArc;
		  private FST.Arc<long?> firstArc;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.FST.Arc<long?> scratchArc;
		  private FST.Arc<long?> scratchArc;
		  private IntsRef scratchInts;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.util.fst.BytesRefFSTEnum<long?> fstEnum;
		  private BytesRefFSTEnum<long?> fstEnum;
		  private BytesRef @ref;
		  private ByteArrayDataInput input;

		  public SortedSetDocValuesAnonymousInnerClassHelper<T1, T2, T3, T4>(MemoryDocValuesProducer outerInstance, org.MemoryDocValuesProducer.FSTEntry entry, BinaryDocValues docToOrds, FST<T1> fst, FST.BytesReader @in, FST.Arc<T2> firstArc, FST.Arc<T3> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<T4> fstEnum, BytesRef @ref, ByteArrayDataInput input)
		  {
			  this.outerInstance = outerInstance;
			  this.entry = entry;
			  this.docToOrds = docToOrds;
			  this.fst = fst;
			  this.@in = @in;
			  this.firstArc = firstArc;
			  this.scratchArc = scratchArc;
			  this.scratchInts = scratchInts;
			  this.fstEnum = fstEnum;
			  this.@ref = @ref;
			  this.input = input;
		  }

		  internal long currentOrd;

		  public override long nextOrd()
		  {
			if (input.eof())
			{
			  return NO_MORE_ORDS;
			}
			else
			{
			  currentOrd += input.readVLong();
			  return currentOrd;
			}
		  }

		  public override int Document
		  {
			  set
			  {
				docToOrds.get(value, @ref);
				input.reset(@ref.bytes, @ref.offset, @ref.length);
				currentOrd = 0;
			  }
		  }

		  public override void lookupOrd(long ord, BytesRef result)
		  {
			try
			{
			  @in.Position = 0;
			  fst.getFirstArc(firstArc);
			  IntsRef output = Util.getByOutput(fst, ord, @in, firstArc, scratchArc, scratchInts);
			  result.bytes = new sbyte[output.length];
			  result.offset = 0;
			  result.length = 0;
			  Util.toBytesRef(output, result);
			}
			catch (IOException bogus)
			{
			  throw new Exception(bogus);
			}
		  }

		  public override long lookupTerm(BytesRef key)
		  {
			try
			{
			  BytesRefFSTEnum.InputOutput<long?> o = fstEnum.seekCeil(key);
			  if (o == null)
			  {
				return -ValueCount - 1;
			  }
			  else if (o.input.Equals(key))
			  {
				return (int)o.output;
			  }
			  else
			  {
				return -o.output - 1;
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
				return entry.numOrds;
			  }
		  }

		  public override TermsEnum termsEnum()
		  {
			return new FSTTermsEnum(fst);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.util.Bits getMissingBits(int fieldNumber, final long offset, final long length) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  private Bits getMissingBits(int fieldNumber, long offset, long length)
	  {
		if (offset == -1)
		{
		  return new Bits.MatchAllBits(maxDoc);
		}
		else
		{
		  Bits instance;
		  lock (this)
		  {
			instance = docsWithFieldInstances[fieldNumber];
			if (instance == null)
			{
			  IndexInput data = this.data.clone();
			  data.seek(offset);
			  Debug.Assert(length % 8 == 0);
			  long[] bits = new long[(int) length >> 3];
			  for (int i = 0; i < bits.Length; i++)
			  {
				bits[i] = data.readLong();
			  }
			  instance = new FixedBitSet(bits, maxDoc);
			  docsWithFieldInstances[fieldNumber] = instance;
			}
		  }
		  return instance;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.util.Bits getDocsWithField(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override Bits getDocsWithField(FieldInfo field)
	  {
		switch (field.DocValuesType)
		{
		  case SORTED_SET:
			return DocValues.docsWithValue(getSortedSet(field), maxDoc);
		  case SORTED:
			return DocValues.docsWithValue(getSorted(field), maxDoc);
		  case BINARY:
			BinaryEntry be = binaries[field.number];
			return getMissingBits(field.number, be.missingOffset, be.missingBytes);
		  case NUMERIC:
			NumericEntry ne = numerics[field.number];
			return getMissingBits(field.number, ne.missingOffset, ne.missingBytes);
		  default:
			throw new AssertionError();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		data.close();
	  }

	  internal class NumericEntry
	  {
		internal long offset;
		internal long missingOffset;
		internal long missingBytes;
		internal sbyte format;
		internal int packedIntsVersion;
	  }

	  internal class BinaryEntry
	  {
		internal long offset;
		internal long missingOffset;
		internal long missingBytes;
		internal long numBytes;
		internal int minLength;
		internal int maxLength;
		internal int packedIntsVersion;
		internal int blockSize;
	  }

	  internal class FSTEntry
	  {
		internal long offset;
		internal long numOrds;
	  }

	  // exposes FSTEnum directly as a TermsEnum: avoids binary-search next()
	  internal class FSTTermsEnum : TermsEnum
	  {
		internal readonly BytesRefFSTEnum<long?> @in;

		// this is all for the complicated seek(ord)...
		// maybe we should add a FSTEnum that supports this operation?
		internal readonly FST<long?> fst;
		internal readonly FST.BytesReader bytesReader;
		internal readonly FST.Arc<long?> firstArc = new FST.Arc<long?>();
		internal readonly FST.Arc<long?> scratchArc = new FST.Arc<long?>();
		internal readonly IntsRef scratchInts = new IntsRef();
		internal readonly BytesRef scratchBytes = new BytesRef();

		internal FSTTermsEnum(FST<long?> fst)
		{
		  this.fst = fst;
		  @in = new BytesRefFSTEnum<>(fst);
		  bytesReader = fst.BytesReader;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.util.BytesRef next() throws java.io.IOException
		public override BytesRef next()
		{
		  BytesRefFSTEnum.InputOutput<long?> io = @in.next();
		  if (io == null)
		  {
			return null;
		  }
		  else
		  {
			return io.input;
		  }
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public SeekStatus seekCeil(org.apache.lucene.util.BytesRef text) throws java.io.IOException
		public override SeekStatus seekCeil(BytesRef text)
		{
		  if (@in.seekCeil(text) == null)
		  {
			return SeekStatus.END;
		  }
		  else if (term().Equals(text))
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

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean seekExact(org.apache.lucene.util.BytesRef text) throws java.io.IOException
		public override bool seekExact(BytesRef text)
		{
		  if (@in.seekExact(text) == null)
		  {
			return false;
		  }
		  else
		  {
			return true;
		  }
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void seekExact(long ord) throws java.io.IOException
		public override void seekExact(long ord)
		{
		  // TODO: would be better to make this simpler and faster.
		  // but we dont want to introduce a bug that corrupts our enum state!
		  bytesReader.Position = 0;
		  fst.getFirstArc(firstArc);
		  IntsRef output = Util.getByOutput(fst, ord, bytesReader, firstArc, scratchArc, scratchInts);
		  scratchBytes.bytes = new sbyte[output.length];
		  scratchBytes.offset = 0;
		  scratchBytes.length = 0;
		  Util.toBytesRef(output, scratchBytes);
		  // TODO: we could do this lazily, better to try to push into FSTEnum though?
		  @in.seekExact(scratchBytes);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.util.BytesRef term() throws java.io.IOException
		public override BytesRef term()
		{
		  return @in.current().input;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public long ord() throws java.io.IOException
		public override long ord()
		{
		  return @in.current().output;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int docFreq() throws java.io.IOException
		public override int docFreq()
		{
		  throw new System.NotSupportedException();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public long totalTermFreq() throws java.io.IOException
		public override long totalTermFreq()
		{
		  throw new System.NotSupportedException();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.DocsEnum docs(org.apache.lucene.util.Bits liveDocs, org.apache.lucene.index.DocsEnum reuse, int flags) throws java.io.IOException
		public override DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)
		{
		  throw new System.NotSupportedException();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.DocsAndPositionsEnum docsAndPositions(org.apache.lucene.util.Bits liveDocs, org.apache.lucene.index.DocsAndPositionsEnum reuse, int flags) throws java.io.IOException
		public override DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		{
		  throw new System.NotSupportedException();
		}
	  }
	}

}