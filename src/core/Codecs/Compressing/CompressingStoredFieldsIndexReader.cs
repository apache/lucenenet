using System;

namespace Lucene.Net.Codecs.Compressing
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


	using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
	using SegmentInfo = Lucene.Net.Index.SegmentInfo;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using ArrayUtil = Lucene.Net.Util.ArrayUtil;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;

	/// <summary>
	/// Random-access reader for <seealso cref="CompressingStoredFieldsIndexWriter"/>.
	/// @lucene.internal
	/// </summary>
	public sealed class CompressingStoredFieldsIndexReader : ICloneable
	{

	  internal static long MoveLowOrderBitToSign(long n)
	  {
		return (((long)((ulong)n >> 1)) ^ -(n & 1));
	  }

	  internal readonly int MaxDoc;
	  internal readonly int[] DocBases;
	  internal readonly long[] StartPointers;
	  internal readonly int[] AvgChunkDocs;
	  internal readonly long[] AvgChunkSizes;
	  internal readonly PackedInts.Reader[] DocBasesDeltas; // delta from the avg
	  internal readonly PackedInts.Reader[] StartPointersDeltas; // delta from the avg

	  // It is the responsibility of the caller to close fieldsIndexIn after this constructor
	  // has been called
	  internal CompressingStoredFieldsIndexReader(IndexInput fieldsIndexIn, SegmentInfo si)
	  {
		MaxDoc = si.DocCount;
		int[] docBases = new int[16];
		long[] startPointers = new long[16];
		int[] avgChunkDocs = new int[16];
		long[] avgChunkSizes = new long[16];
		PackedInts.Reader[] docBasesDeltas = new PackedInts.Reader[16];
		PackedInts.Reader[] startPointersDeltas = new PackedInts.Reader[16];

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int packedIntsVersion = fieldsIndexIn.readVInt();
		int packedIntsVersion = fieldsIndexIn.ReadVInt();

		int blockCount = 0;

		for (;;)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numChunks = fieldsIndexIn.readVInt();
		  int numChunks = fieldsIndexIn.ReadVInt();
		  if (numChunks == 0)
		  {
			break;
		  }
		  if (blockCount == docBases.Length)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newSize = Lucene.Net.Util.ArrayUtil.oversize(blockCount + 1, 8);
			int newSize = ArrayUtil.Oversize(blockCount + 1, 8);
			docBases = Arrays.copyOf(docBases, newSize);
			startPointers = Arrays.copyOf(startPointers, newSize);
			avgChunkDocs = Arrays.copyOf(avgChunkDocs, newSize);
			avgChunkSizes = Arrays.copyOf(avgChunkSizes, newSize);
			docBasesDeltas = Arrays.copyOf(docBasesDeltas, newSize);
			startPointersDeltas = Arrays.copyOf(startPointersDeltas, newSize);
		  }

		  // doc bases
		  docBases[blockCount] = fieldsIndexIn.ReadVInt();
		  avgChunkDocs[blockCount] = fieldsIndexIn.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bitsPerDocBase = fieldsIndexIn.readVInt();
		  int bitsPerDocBase = fieldsIndexIn.ReadVInt();
		  if (bitsPerDocBase > 32)
		  {
			throw new CorruptIndexException("Corrupted bitsPerDocBase (resource=" + fieldsIndexIn + ")");
		  }
		  docBasesDeltas[blockCount] = PackedInts.GetReaderNoHeader(fieldsIndexIn, PackedInts.Format.PACKED, packedIntsVersion, numChunks, bitsPerDocBase);

		  // start pointers
		  startPointers[blockCount] = fieldsIndexIn.ReadVLong();
		  avgChunkSizes[blockCount] = fieldsIndexIn.ReadVLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bitsPerStartPointer = fieldsIndexIn.readVInt();
		  int bitsPerStartPointer = fieldsIndexIn.ReadVInt();
		  if (bitsPerStartPointer > 64)
		  {
			throw new CorruptIndexException("Corrupted bitsPerStartPointer (resource=" + fieldsIndexIn + ")");
		  }
		  startPointersDeltas[blockCount] = PackedInts.GetReaderNoHeader(fieldsIndexIn, PackedInts.Format.PACKED, packedIntsVersion, numChunks, bitsPerStartPointer);

		  ++blockCount;
		}

		this.DocBases = Arrays.copyOf(docBases, blockCount);
		this.StartPointers = Arrays.copyOf(startPointers, blockCount);
		this.AvgChunkDocs = Arrays.copyOf(avgChunkDocs, blockCount);
		this.AvgChunkSizes = Arrays.copyOf(avgChunkSizes, blockCount);
		this.DocBasesDeltas = Arrays.copyOf(docBasesDeltas, blockCount);
		this.StartPointersDeltas = Arrays.copyOf(startPointersDeltas, blockCount);
	  }

	  private int Block(int docID)
	  {
		int lo = 0, hi = DocBases.Length - 1;
		while (lo <= hi)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int mid = (lo + hi) >>> 1;
		  int mid = (int)((uint)(lo + hi) >> 1);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int midValue = docBases[mid];
		  int midValue = DocBases[mid];
		  if (midValue == docID)
		  {
			return mid;
		  }
		  else if (midValue < docID)
		  {
			lo = mid + 1;
		  }
		  else
		  {
			hi = mid - 1;
		  }
		}
		return hi;
	  }

	  private int RelativeDocBase(int block, int relativeChunk)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int expected = avgChunkDocs[block] * relativeChunk;
		int expected = AvgChunkDocs[block] * relativeChunk;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long delta = moveLowOrderBitToSign(docBasesDeltas[block].get(relativeChunk));
		long delta = MoveLowOrderBitToSign(DocBasesDeltas[block].Get(relativeChunk));
		return expected + (int) delta;
	  }

	  private long RelativeStartPointer(int block, int relativeChunk)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long expected = avgChunkSizes[block] * relativeChunk;
		long expected = AvgChunkSizes[block] * relativeChunk;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long delta = moveLowOrderBitToSign(startPointersDeltas[block].get(relativeChunk));
		long delta = MoveLowOrderBitToSign(StartPointersDeltas[block].Get(relativeChunk));
		return expected + delta;
	  }

	  private int RelativeChunk(int block, int relativeDoc)
	  {
		int lo = 0, hi = DocBasesDeltas[block].Size() - 1;
		while (lo <= hi)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int mid = (lo + hi) >>> 1;
		  int mid = (int)((uint)(lo + hi) >> 1);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int midValue = relativeDocBase(block, mid);
		  int midValue = RelativeDocBase(block, mid);
		  if (midValue == relativeDoc)
		  {
			return mid;
		  }
		  else if (midValue < relativeDoc)
		  {
			lo = mid + 1;
		  }
		  else
		  {
			hi = mid - 1;
		  }
		}
		return hi;
	  }

	  internal long GetStartPointer(int docID)
	  {
		if (docID < 0 || docID >= MaxDoc)
		{
		  throw new System.ArgumentException("docID out of range [0-" + MaxDoc + "]: " + docID);
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int block = block(docID);
		int block = Block(docID);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int relativeChunk = relativeChunk(block, docID - docBases[block]);
		int relativeChunk = RelativeChunk(block, docID - DocBases[block]);
		return StartPointers[block] + RelativeStartPointer(block, relativeChunk);
	  }

	  public override CompressingStoredFieldsIndexReader Clone()
	  {
		return this;
	  }

	  internal long RamBytesUsed()
	  {
		long res = 0;

		foreach (PackedInts.Reader r in DocBasesDeltas)
		{
		  res += r.RamBytesUsed();
		}
		foreach (PackedInts.Reader r in StartPointersDeltas)
		{
		  res += r.RamBytesUsed();
		}

		res += RamUsageEstimator.SizeOf(DocBases);
		res += RamUsageEstimator.SizeOf(StartPointers);
		res += RamUsageEstimator.SizeOf(AvgChunkDocs);
		res += RamUsageEstimator.SizeOf(AvgChunkSizes);

		return res;
	  }

	}

}