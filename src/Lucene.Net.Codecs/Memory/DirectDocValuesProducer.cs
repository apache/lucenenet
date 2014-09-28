using System.Diagnostics;
using System.Collections.Generic;

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


	using BinaryDocValues = org.apache.lucene.index.BinaryDocValues;
	using CorruptIndexException = org.apache.lucene.index.CorruptIndexException;
	using DocValues = org.apache.lucene.index.DocValues;
	using FieldInfo = org.apache.lucene.index.FieldInfo;
	using IndexFileNames = org.apache.lucene.index.IndexFileNames;
	using NumericDocValues = org.apache.lucene.index.NumericDocValues;
	using RandomAccessOrds = org.apache.lucene.index.RandomAccessOrds;
	using SegmentReadState = org.apache.lucene.index.SegmentReadState;
	using SortedDocValues = org.apache.lucene.index.SortedDocValues;
	using SortedSetDocValues = org.apache.lucene.index.SortedSetDocValues;
	using ChecksumIndexInput = org.apache.lucene.store.ChecksumIndexInput;
	using IndexInput = org.apache.lucene.store.IndexInput;
	using Bits = org.apache.lucene.util.Bits;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using FixedBitSet = org.apache.lucene.util.FixedBitSet;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using RamUsageEstimator = org.apache.lucene.util.RamUsageEstimator;

	/// <summary>
	/// Reader for <seealso cref="DirectDocValuesFormat"/>
	/// </summary>

	internal class DirectDocValuesProducer : DocValuesProducer
	{
	  // metadata maps (just file pointers and minimal stuff)
	  private readonly IDictionary<int?, NumericEntry> numerics = new Dictionary<int?, NumericEntry>();
	  private readonly IDictionary<int?, BinaryEntry> binaries = new Dictionary<int?, BinaryEntry>();
	  private readonly IDictionary<int?, SortedEntry> sorteds = new Dictionary<int?, SortedEntry>();
	  private readonly IDictionary<int?, SortedSetEntry> sortedSets = new Dictionary<int?, SortedSetEntry>();
	  private readonly IndexInput data;

	  // ram instances we have already loaded
	  private readonly IDictionary<int?, NumericDocValues> numericInstances = new Dictionary<int?, NumericDocValues>();
	  private readonly IDictionary<int?, BinaryDocValues> binaryInstances = new Dictionary<int?, BinaryDocValues>();
	  private readonly IDictionary<int?, SortedDocValues> sortedInstances = new Dictionary<int?, SortedDocValues>();
	  private readonly IDictionary<int?, SortedSetRawValues> sortedSetInstances = new Dictionary<int?, SortedSetRawValues>();
	  private readonly IDictionary<int?, Bits> docsWithFieldInstances = new Dictionary<int?, Bits>();

	  private readonly int maxDoc;
	  private readonly AtomicLong ramBytesUsed_Renamed;
	  private readonly int version;

	  internal const sbyte NUMBER = 0;
	  internal const sbyte BYTES = 1;
	  internal const sbyte SORTED = 2;
	  internal const sbyte SORTED_SET = 3;

	  internal const int VERSION_START = 0;
	  internal const int VERSION_CHECKSUM = 1;
	  internal const int VERSION_CURRENT = VERSION_CHECKSUM;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: DirectDocValuesProducer(org.apache.lucene.index.SegmentReadState state, String dataCodec, String dataExtension, String metaCodec, String metaExtension) throws java.io.IOException
	  internal DirectDocValuesProducer(SegmentReadState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
	  {
		maxDoc = state.segmentInfo.DocCount;
		string metaName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, metaExtension);
		// read in the entries from the metadata file.
		ChecksumIndexInput @in = state.directory.openChecksumInput(metaName, state.context);
		ramBytesUsed_Renamed = new AtomicLong(RamUsageEstimator.shallowSizeOfInstance(this.GetType()));
		bool success = false;
		try
		{
		  version = CodecUtil.checkHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
		  readFields(@in);

		  if (version >= VERSION_CHECKSUM)
		  {
			CodecUtil.checkFooter(@in);
		  }
		  else
		  {
			CodecUtil.checkEOF(@in);
		  }
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
//ORIGINAL LINE: private NumericEntry readNumericEntry(org.apache.lucene.store.IndexInput meta) throws java.io.IOException
	  private NumericEntry readNumericEntry(IndexInput meta)
	  {
		NumericEntry entry = new NumericEntry();
		entry.offset = meta.readLong();
		entry.count = meta.readInt();
		entry.missingOffset = meta.readLong();
		if (entry.missingOffset != -1)
		{
		  entry.missingBytes = meta.readLong();
		}
		else
		{
		  entry.missingBytes = 0;
		}
		entry.byteWidth = meta.readByte();

		return entry;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private BinaryEntry readBinaryEntry(org.apache.lucene.store.IndexInput meta) throws java.io.IOException
	  private BinaryEntry readBinaryEntry(IndexInput meta)
	  {
		BinaryEntry entry = new BinaryEntry();
		entry.offset = meta.readLong();
		entry.numBytes = meta.readInt();
		entry.count = meta.readInt();
		entry.missingOffset = meta.readLong();
		if (entry.missingOffset != -1)
		{
		  entry.missingBytes = meta.readLong();
		}
		else
		{
		  entry.missingBytes = 0;
		}

		return entry;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private SortedEntry readSortedEntry(org.apache.lucene.store.IndexInput meta) throws java.io.IOException
	  private SortedEntry readSortedEntry(IndexInput meta)
	  {
		SortedEntry entry = new SortedEntry();
		entry.docToOrd = readNumericEntry(meta);
		entry.values = readBinaryEntry(meta);
		return entry;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private SortedSetEntry readSortedSetEntry(org.apache.lucene.store.IndexInput meta) throws java.io.IOException
	  private SortedSetEntry readSortedSetEntry(IndexInput meta)
	  {
		SortedSetEntry entry = new SortedSetEntry();
		entry.docToOrdAddress = readNumericEntry(meta);
		entry.ords = readNumericEntry(meta);
		entry.values = readBinaryEntry(meta);
		return entry;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void readFields(org.apache.lucene.store.IndexInput meta) throws java.io.IOException
	  private void readFields(IndexInput meta)
	  {
		int fieldNumber = meta.readVInt();
		while (fieldNumber != -1)
		{
		  int fieldType = meta.readByte();
		  if (fieldType == NUMBER)
		  {
			numerics[fieldNumber] = readNumericEntry(meta);
		  }
		  else if (fieldType == BYTES)
		  {
			binaries[fieldNumber] = readBinaryEntry(meta);
		  }
		  else if (fieldType == SORTED)
		  {
			sorteds[fieldNumber] = readSortedEntry(meta);
		  }
		  else if (fieldType == SORTED_SET)
		  {
			sortedSets[fieldNumber] = readSortedSetEntry(meta);
		  }
		  else
		  {
			throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" + meta);
		  }
		  fieldNumber = meta.readVInt();
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
//ORIGINAL LINE: @Override public synchronized org.apache.lucene.index.NumericDocValues getNumeric(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override NumericDocValues getNumeric(FieldInfo field)
	  {
		  lock (this)
		  {
			NumericDocValues instance = numericInstances[field.number];
			if (instance == null)
			{
			  // Lazy load
			  instance = loadNumeric(numerics[field.number]);
			  numericInstances[field.number] = instance;
			}
			return instance;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.index.NumericDocValues loadNumeric(NumericEntry entry) throws java.io.IOException
	  private NumericDocValues loadNumeric(NumericEntry entry)
	  {
		data.seek(entry.offset + entry.missingBytes);
		switch (entry.byteWidth)
		{
		case 1:
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] values = new byte[entry.count];
			sbyte[] values = new sbyte[entry.count];
			data.readBytes(values, 0, entry.count);
			ramBytesUsed_Renamed.addAndGet(RamUsageEstimator.sizeOf(values));
			return new NumericDocValuesAnonymousInnerClassHelper(this, values);
		}

		case 2:
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final short[] values = new short[entry.count];
			short[] values = new short[entry.count];
			for (int i = 0;i < entry.count;i++)
			{
			  values[i] = data.readShort();
			}
			ramBytesUsed_Renamed.addAndGet(RamUsageEstimator.sizeOf(values));
			return new NumericDocValuesAnonymousInnerClassHelper2(this, values);
		}

		case 4:
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] values = new int[entry.count];
			int[] values = new int[entry.count];
			for (int i = 0;i < entry.count;i++)
			{
			  values[i] = data.readInt();
			}
			ramBytesUsed_Renamed.addAndGet(RamUsageEstimator.sizeOf(values));
			return new NumericDocValuesAnonymousInnerClassHelper3(this, values);
		}

		case 8:
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long[] values = new long[entry.count];
			long[] values = new long[entry.count];
			for (int i = 0;i < entry.count;i++)
			{
			  values[i] = data.readLong();
			}
			ramBytesUsed_Renamed.addAndGet(RamUsageEstimator.sizeOf(values));
			return new NumericDocValuesAnonymousInnerClassHelper4(this, values);
		}

		default:
		  throw new AssertionError();
		}
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
	  {
		  private readonly DirectDocValuesProducer outerInstance;

		  private sbyte[] values;

		  public NumericDocValuesAnonymousInnerClassHelper(DirectDocValuesProducer outerInstance, sbyte[] values)
		  {
			  this.outerInstance = outerInstance;
			  this.values = values;
		  }

		  public override long get(int idx)
		  {
			return values[idx];
		  }
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper2 : NumericDocValues
	  {
		  private readonly DirectDocValuesProducer outerInstance;

		  private short[] values;

		  public NumericDocValuesAnonymousInnerClassHelper2(DirectDocValuesProducer outerInstance, short[] values)
		  {
			  this.outerInstance = outerInstance;
			  this.values = values;
		  }

		  public override long get(int idx)
		  {
			return values[idx];
		  }
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper3 : NumericDocValues
	  {
		  private readonly DirectDocValuesProducer outerInstance;

		  private int[] values;

		  public NumericDocValuesAnonymousInnerClassHelper3(DirectDocValuesProducer outerInstance, int[] values)
		  {
			  this.outerInstance = outerInstance;
			  this.values = values;
		  }

		  public override long get(int idx)
		  {
			return values[idx];
		  }
	  }

	  private class NumericDocValuesAnonymousInnerClassHelper4 : NumericDocValues
	  {
		  private readonly DirectDocValuesProducer outerInstance;

		  private long[] values;

		  public NumericDocValuesAnonymousInnerClassHelper4(DirectDocValuesProducer outerInstance, long[] values)
		  {
			  this.outerInstance = outerInstance;
			  this.values = values;
		  }

		  public override long get(int idx)
		  {
			return values[idx];
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
			  // Lazy load
			  instance = loadBinary(binaries[field.number]);
			  binaryInstances[field.number] = instance;
			}
			return instance;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.index.BinaryDocValues loadBinary(BinaryEntry entry) throws java.io.IOException
	  private BinaryDocValues loadBinary(BinaryEntry entry)
	  {
		data.seek(entry.offset);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] bytes = new byte[entry.numBytes];
		sbyte[] bytes = new sbyte[entry.numBytes];
		data.readBytes(bytes, 0, entry.numBytes);
		data.seek(entry.offset + entry.numBytes + entry.missingBytes);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] address = new int[entry.count+1];
		int[] address = new int[entry.count + 1];
		for (int i = 0;i < entry.count;i++)
		{
		  address[i] = data.readInt();
		}
		address[entry.count] = data.readInt();

		ramBytesUsed_Renamed.addAndGet(RamUsageEstimator.sizeOf(bytes) + RamUsageEstimator.sizeOf(address));

		return new BinaryDocValuesAnonymousInnerClassHelper(this, bytes, address);
	  }

	  private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
	  {
		  private readonly DirectDocValuesProducer outerInstance;

		  private sbyte[] bytes;
		  private int[] address;

		  public BinaryDocValuesAnonymousInnerClassHelper(DirectDocValuesProducer outerInstance, sbyte[] bytes, int[] address)
		  {
			  this.outerInstance = outerInstance;
			  this.bytes = bytes;
			  this.address = address;
		  }

		  public override void get(int docID, BytesRef result)
		  {
			result.bytes = bytes;
			result.offset = address[docID];
			result.length = address[docID + 1] - result.offset;
		  };
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public synchronized org.apache.lucene.index.SortedDocValues getSorted(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override SortedDocValues getSorted(FieldInfo field)
	  {
		  lock (this)
		  {
			SortedDocValues instance = sortedInstances[field.number];
			if (instance == null)
			{
			  // Lazy load
			  instance = loadSorted(field);
			  sortedInstances[field.number] = instance;
			}
			return instance;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.index.SortedDocValues loadSorted(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  private SortedDocValues loadSorted(FieldInfo field)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SortedEntry entry = sorteds.get(field.number);
		SortedEntry entry = sorteds[field.number];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.NumericDocValues docToOrd = loadNumeric(entry.docToOrd);
		NumericDocValues docToOrd = loadNumeric(entry.docToOrd);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.BinaryDocValues values = loadBinary(entry.values);
		BinaryDocValues values = loadBinary(entry.values);

		return new SortedDocValuesAnonymousInnerClassHelper(this, entry, docToOrd, values);
	  }

	  private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
	  {
		  private readonly DirectDocValuesProducer outerInstance;

		  private org.apache.lucene.codecs.memory.DirectDocValuesProducer.SortedEntry entry;
		  private NumericDocValues docToOrd;
		  private BinaryDocValues values;

		  public SortedDocValuesAnonymousInnerClassHelper(DirectDocValuesProducer outerInstance, org.apache.lucene.codecs.memory.DirectDocValuesProducer.SortedEntry entry, NumericDocValues docToOrd, BinaryDocValues values)
		  {
			  this.outerInstance = outerInstance;
			  this.entry = entry;
			  this.docToOrd = docToOrd;
			  this.values = values;
		  }


		  public override int getOrd(int docID)
		  {
			return (int) docToOrd.get(docID);
		  }

		  public override void lookupOrd(int ord, BytesRef result)
		  {
			values.get(ord, result);
		  }

		  public override int ValueCount
		  {
			  get
			  {
				return entry.values.count;
			  }
		  }

		  // Leave lookupTerm to super's binary search

		  // Leave termsEnum to super
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public synchronized org.apache.lucene.index.SortedSetDocValues getSortedSet(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override SortedSetDocValues getSortedSet(FieldInfo field)
	  {
		  lock (this)
		  {
			SortedSetRawValues instance = sortedSetInstances[field.number];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SortedSetEntry entry = sortedSets.get(field.number);
			SortedSetEntry entry = sortedSets[field.number];
			if (instance == null)
			{
			  // Lazy load
			  instance = loadSortedSet(entry);
			  sortedSetInstances[field.number] = instance;
			}
        
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.NumericDocValues docToOrdAddress = instance.docToOrdAddress;
			NumericDocValues docToOrdAddress = instance.docToOrdAddress;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.NumericDocValues ords = instance.ords;
			NumericDocValues ords = instance.ords;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.BinaryDocValues values = instance.values;
			BinaryDocValues values = instance.values;
        
			// Must make a new instance since the iterator has state:
			return new RandomAccessOrdsAnonymousInnerClassHelper(this, entry, docToOrdAddress, ords, values);
		  }
	  }

	  private class RandomAccessOrdsAnonymousInnerClassHelper : RandomAccessOrds
	  {
		  private readonly DirectDocValuesProducer outerInstance;

		  private org.apache.lucene.codecs.memory.DirectDocValuesProducer.SortedSetEntry entry;
		  private NumericDocValues docToOrdAddress;
		  private NumericDocValues ords;
		  private BinaryDocValues values;

		  public RandomAccessOrdsAnonymousInnerClassHelper(DirectDocValuesProducer outerInstance, org.apache.lucene.codecs.memory.DirectDocValuesProducer.SortedSetEntry entry, NumericDocValues docToOrdAddress, NumericDocValues ords, BinaryDocValues values)
		  {
			  this.outerInstance = outerInstance;
			  this.entry = entry;
			  this.docToOrdAddress = docToOrdAddress;
			  this.ords = ords;
			  this.values = values;
		  }

		  internal int ordStart;
		  internal int ordUpto;
		  internal int ordLimit;

		  public override long nextOrd()
		  {
			if (ordUpto == ordLimit)
			{
			  return NO_MORE_ORDS;
			}
			else
			{
			  return ords.get(ordUpto++);
			}
		  }

		  public override int Document
		  {
			  set
			  {
				ordStart = ordUpto = (int) docToOrdAddress.get(value);
				ordLimit = (int) docToOrdAddress.get(value+1);
			  }
		  }

		  public override void lookupOrd(long ord, BytesRef result)
		  {
			values.get((int) ord, result);
		  }

		  public override long ValueCount
		  {
			  get
			  {
				return entry.values.count;
			  }
		  }

		  public override long ordAt(int index)
		  {
			return ords.get(ordStart + index);
		  }

		  public override int cardinality()
		  {
			return ordLimit - ordStart;
		  }

		  // Leave lookupTerm to super's binary search

		  // Leave termsEnum to super
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private SortedSetRawValues loadSortedSet(SortedSetEntry entry) throws java.io.IOException
	  private SortedSetRawValues loadSortedSet(SortedSetEntry entry)
	  {
		SortedSetRawValues instance = new SortedSetRawValues();
		instance.docToOrdAddress = loadNumeric(entry.docToOrdAddress);
		instance.ords = loadNumeric(entry.ords);
		instance.values = loadBinary(entry.values);
		return instance;
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

	  internal class SortedSetRawValues
	  {
		internal NumericDocValues docToOrdAddress;
		internal NumericDocValues ords;
		internal BinaryDocValues values;
	  }

	  internal class NumericEntry
	  {
		internal long offset;
		internal int count;
		internal long missingOffset;
		internal long missingBytes;
		internal sbyte byteWidth;
		internal int packedIntsVersion;
	  }

	  internal class BinaryEntry
	  {
		internal long offset;
		internal long missingOffset;
		internal long missingBytes;
		internal int count;
		internal int numBytes;
		internal int minLength;
		internal int maxLength;
		internal int packedIntsVersion;
		internal int blockSize;
	  }

	  internal class SortedEntry
	  {
		internal NumericEntry docToOrd;
		internal BinaryEntry values;
	  }

	  internal class SortedSetEntry
	  {
		internal NumericEntry docToOrdAddress;
		internal NumericEntry ords;
		internal BinaryEntry values;
	  }

	  internal class FSTEntry
	  {
		internal long offset;
		internal long numOrds;
	  }
	}

}