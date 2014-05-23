using System;
using System.Diagnostics;
using System.Collections.Generic;
using Lucene.Net.Util;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Util.Packed;
using Lucene.Net.Support;

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




	/// <summary>
	/// <seealso cref="TermVectorsReader"/> for <seealso cref="CompressingTermVectorsFormat"/>.
	/// @lucene.experimental
	/// </summary>
	public sealed class CompressingTermVectorsReader : TermVectorsReader, IDisposable
	{

	  private readonly FieldInfos FieldInfos;
	  internal readonly CompressingStoredFieldsIndexReader IndexReader;
	  internal readonly IndexInput VectorsStream_Renamed;
	  private readonly int Version_Renamed;
	  private readonly int PackedIntsVersion_Renamed;
	  private readonly CompressionMode CompressionMode_Renamed;
	  private readonly Decompressor Decompressor;
	  private readonly int ChunkSize_Renamed;
	  private readonly int NumDocs;
	  private bool Closed;
	  private readonly BlockPackedReaderIterator Reader;

	  // used by clone
	  private CompressingTermVectorsReader(CompressingTermVectorsReader reader)
	  {
		this.FieldInfos = reader.FieldInfos;
		this.VectorsStream_Renamed = reader.VectorsStream_Renamed.Clone();
		this.IndexReader = reader.IndexReader.Clone();
		this.PackedIntsVersion_Renamed = reader.PackedIntsVersion_Renamed;
		this.CompressionMode_Renamed = reader.CompressionMode_Renamed;
		this.Decompressor = reader.Decompressor.Clone();
		this.ChunkSize_Renamed = reader.ChunkSize_Renamed;
		this.NumDocs = reader.NumDocs;
        this.Reader = new BlockPackedReaderIterator(VectorsStream_Renamed, PackedIntsVersion_Renamed, CompressingTermVectorsWriter.BLOCK_SIZE, 0);
		this.Version_Renamed = reader.Version_Renamed;
		this.Closed = false;
	  }

	  /// <summary>
	  /// Sole constructor. </summary>
	  public CompressingTermVectorsReader(Directory d, SegmentInfo si, string segmentSuffix, FieldInfos fn, IOContext context, string formatName, CompressionMode compressionMode)
	  {
		this.CompressionMode_Renamed = compressionMode;
		string segment = si.Name;
		bool success = false;
		FieldInfos = fn;
		NumDocs = si.DocCount;
		ChecksumIndexInput indexStream = null;
		try
		{
		  // Load the index into memory
          string indexStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, CompressingTermVectorsWriter.VECTORS_INDEX_EXTENSION);
		  indexStream = d.OpenChecksumInput(indexStreamFN, context);
          string codecNameIdx = formatName + CompressingTermVectorsWriter.CODEC_SFX_IDX;
          Version_Renamed = CodecUtil.CheckHeader(indexStream, codecNameIdx, CompressingTermVectorsWriter.VERSION_START, CompressingTermVectorsWriter.VERSION_CURRENT);
		  Debug.Assert(CodecUtil.HeaderLength(codecNameIdx) == indexStream.FilePointer);
		  IndexReader = new CompressingStoredFieldsIndexReader(indexStream, si);

          if (Version_Renamed >= CompressingTermVectorsWriter.VERSION_CHECKSUM)
		  {
			indexStream.ReadVLong(); // the end of the data file
			CodecUtil.CheckFooter(indexStream);
		  }
		  else
		  {
			CodecUtil.CheckEOF(indexStream);
		  }
		  indexStream.Close();
		  indexStream = null;

		  // Open the data file and read metadata
          string vectorsStreamFN = IndexFileNames.SegmentFileName(segment, segmentSuffix, CompressingTermVectorsWriter.VECTORS_EXTENSION);
		  VectorsStream_Renamed = d.OpenInput(vectorsStreamFN, context);
          string codecNameDat = formatName + CompressingTermVectorsWriter.CODEC_SFX_DAT;
          int version2 = CodecUtil.CheckHeader(VectorsStream_Renamed, codecNameDat, CompressingTermVectorsWriter.VERSION_START, CompressingTermVectorsWriter.VERSION_CURRENT);
		  if (Version_Renamed != version2)
		  {
			throw new Exception("Version mismatch between stored fields index and data: " + Version_Renamed + " != " + version2);
		  }
		  Debug.Assert(CodecUtil.HeaderLength(codecNameDat) == VectorsStream_Renamed.FilePointer);

		  PackedIntsVersion_Renamed = VectorsStream_Renamed.ReadVInt();
		  ChunkSize_Renamed = VectorsStream_Renamed.ReadVInt();
		  Decompressor = compressionMode.NewDecompressor();
          this.Reader = new BlockPackedReaderIterator(VectorsStream_Renamed, PackedIntsVersion_Renamed, CompressingTermVectorsWriter.BLOCK_SIZE, 0);

		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(this, indexStream);
		  }
		}
	  }

	  internal CompressionMode CompressionMode
	  {
		  get
		  {
			return CompressionMode_Renamed;
		  }
	  }

	  internal int ChunkSize
	  {
		  get
		  {
			return ChunkSize_Renamed;
		  }
	  }

	  internal int PackedIntsVersion
	  {
		  get
		  {
			return PackedIntsVersion_Renamed;
		  }
	  }

	  internal int Version
	  {
		  get
		  {
			return Version_Renamed;
		  }
	  }

	  internal CompressingStoredFieldsIndexReader Index
	  {
		  get
		  {
			return IndexReader;
		  }
	  }

	  internal IndexInput VectorsStream
	  {
		  get
		  {
			return VectorsStream_Renamed;
		  }
	  }

	  /// <exception cref="AlreadyClosedException"> if this TermVectorsReader is closed </exception>
	  private void EnsureOpen()
	  {
		if (Closed)
		{
		  throw new Exception("this FieldsReader is closed");
		}
	  }

	  public override void Close()
	  {
		if (!Closed)
		{
		  IOUtils.Close(VectorsStream_Renamed);
		  Closed = true;
		}
	  }

	  public override TermVectorsReader Clone()
	  {
		return new CompressingTermVectorsReader(this);
	  }

	  public override Fields Get(int doc)
	  {
		EnsureOpen();

		// seek to the right place
		{
		  long startPointer = IndexReader.GetStartPointer(doc);
		  VectorsStream_Renamed.Seek(startPointer);
		}

		// decode
		// - docBase: first doc ID of the chunk
		// - chunkDocs: number of docs of the chunk
		int docBase = VectorsStream_Renamed.ReadVInt();
		int chunkDocs = VectorsStream_Renamed.ReadVInt();
		if (doc < docBase || doc >= docBase + chunkDocs || docBase + chunkDocs > NumDocs)
		{
		  throw new CorruptIndexException("docBase=" + docBase + ",chunkDocs=" + chunkDocs + ",doc=" + doc + " (resource=" + VectorsStream_Renamed + ")");
		}

		int skip; // number of fields to skip
		int numFields; // number of fields of the document we're looking for
		int totalFields; // total number of fields of the chunk (sum for all docs)
		if (chunkDocs == 1)
		{
		  skip = 0;
		  numFields = totalFields = VectorsStream_Renamed.ReadVInt();
		}
		else
		{
		  Reader.Reset(VectorsStream_Renamed, chunkDocs);
		  int sum = 0;
		  for (int i = docBase; i < doc; ++i)
		  {
			sum += (int)Reader.Next();
		  }
		  skip = sum;
		  numFields = (int) Reader.Next();
		  sum += numFields;
		  for (int i = doc + 1; i < docBase + chunkDocs; ++i)
		  {
			sum += (int)Reader.Next();
		  }
		  totalFields = sum;
		}

		if (numFields == 0)
		{
		  // no vectors
		  return null;
		}

		// read field numbers that have term vectors
		int[] fieldNums;
		{
		  int token = VectorsStream_Renamed.ReadByte() & 0xFF;
		  Debug.Assert(token != 0); // means no term vectors, cannot happen since we checked for numFields == 0
		  int bitsPerFieldNum = token & 0x1F;
		  int totalDistinctFields = (int)((uint)token >> 5);
		  if (totalDistinctFields == 0x07)
		  {
			totalDistinctFields += VectorsStream_Renamed.ReadVInt();
		  }
		  ++totalDistinctFields;
		  PackedInts.ReaderIterator it = PackedInts.GetReaderIteratorNoHeader(VectorsStream_Renamed, PackedInts.Format.PACKED, PackedIntsVersion_Renamed, totalDistinctFields, bitsPerFieldNum, 1);
		  fieldNums = new int[totalDistinctFields];
		  for (int i = 0; i < totalDistinctFields; ++i)
		  {
			fieldNums[i] = (int) it.Next();
		  }
		}

		// read field numbers and flags
		int[] fieldNumOffs = new int[numFields];
		PackedInts.Reader flags;
		{
		  int bitsPerOff = PackedInts.BitsRequired(fieldNums.Length - 1);
		  PackedInts.Reader allFieldNumOffs = PackedInts.GetReaderNoHeader(VectorsStream_Renamed, PackedInts.Format.PACKED, PackedIntsVersion_Renamed, totalFields, bitsPerOff);
		  switch (VectorsStream_Renamed.ReadVInt())
		  {
			case 0:
                  PackedInts.Reader fieldFlags = PackedInts.GetReaderNoHeader(VectorsStream_Renamed, PackedInts.Format.PACKED, PackedIntsVersion_Renamed, fieldNums.Length, CompressingTermVectorsWriter.FLAGS_BITS);
              PackedInts.Mutable f = PackedInts.GetMutable(totalFields, CompressingTermVectorsWriter.FLAGS_BITS, PackedInts.COMPACT);
			  for (int i = 0; i < totalFields; ++i)
			  {
				int fieldNumOff = (int) allFieldNumOffs.Get(i);
				Debug.Assert(fieldNumOff >= 0 && fieldNumOff < fieldNums.Length);
				int fgs = (int) fieldFlags.Get(fieldNumOff);
				f.Set(i, fgs);
			  }
			  flags = f;
			  break;
			case 1:
              flags = PackedInts.GetReaderNoHeader(VectorsStream_Renamed, PackedInts.Format.PACKED, PackedIntsVersion_Renamed, totalFields, CompressingTermVectorsWriter.FLAGS_BITS);
			  break;
			default:
			  throw new Exception();
		  }
		  for (int i = 0; i < numFields; ++i)
		  {
			fieldNumOffs[i] = (int) allFieldNumOffs.Get(skip + i);
		  }
		}

		// number of terms per field for all fields
		PackedInts.Reader numTerms;
		int totalTerms;
		{
		  int bitsRequired = VectorsStream_Renamed.ReadVInt();
		  numTerms = PackedInts.GetReaderNoHeader(VectorsStream_Renamed, PackedInts.Format.PACKED, PackedIntsVersion_Renamed, totalFields, bitsRequired);
		  int sum = 0;
		  for (int i = 0; i < totalFields; ++i)
		  {
			sum += (int)numTerms.Get(i);
		  }
		  totalTerms = sum;
		}

		// term lengths
		int docOff = 0, docLen = 0, totalLen ;
		int[] fieldLengths = new int[numFields];
		int[][] prefixLengths = new int[numFields][];
		int[][] suffixLengths = new int[numFields][];
		{
		  Reader.Reset(VectorsStream_Renamed, totalTerms);
		  // skip
		  int toSkip = 0;
		  for (int i = 0; i < skip; ++i)
		  {
              toSkip += (int)numTerms.Get(i);
		  }
		  Reader.Skip(toSkip);
		  // read prefix lengths
		  for (int i = 0; i < numFields; ++i)
		  {
			int termCount = (int) numTerms.Get(skip + i);
			int[] fieldPrefixLengths = new int[termCount];
			prefixLengths[i] = fieldPrefixLengths;
			for (int j = 0; j < termCount;)
			{
			  LongsRef next = Reader.Next(termCount - j);
			  for (int k = 0; k < next.Length; ++k)
			  {
				fieldPrefixLengths[j++] = (int) next.Longs[next.Offset + k];
			  }
			}
		  }
		  Reader.Skip(totalTerms - Reader.Ord());

		  Reader.Reset(VectorsStream_Renamed, totalTerms);
		  // skip
		  toSkip = 0;
		  for (int i = 0; i < skip; ++i)
		  {
			for (int j = 0; j < numTerms.Get(i); ++j)
			{
			  docOff += (int)Reader.Next();
			}
		  }
		  for (int i = 0; i < numFields; ++i)
		  {
			int termCount = (int) numTerms.Get(skip + i);
			int[] fieldSuffixLengths = new int[termCount];
			suffixLengths[i] = fieldSuffixLengths;
			for (int j = 0; j < termCount;)
			{
			  LongsRef next = Reader.Next(termCount - j);
			  for (int k = 0; k < next.Length; ++k)
			  {
				fieldSuffixLengths[j++] = (int) next.Longs[next.Offset + k];
			  }
			}
			fieldLengths[i] = Sum(suffixLengths[i]);
			docLen += fieldLengths[i];
		  }
		  totalLen = docOff + docLen;
		  for (int i = skip + numFields; i < totalFields; ++i)
		  {
			for (int j = 0; j < numTerms.Get(i); ++j)
			{
			  totalLen += (int)Reader.Next();
			}
		  }
		}

		// term freqs
		int[] termFreqs = new int[totalTerms];
		{
		  Reader.Reset(VectorsStream_Renamed, totalTerms);
		  for (int i = 0; i < totalTerms;)
		  {
			LongsRef next = Reader.Next(totalTerms - i);
			for (int k = 0; k < next.Length; ++k)
			{
			  termFreqs[i++] = 1 + (int) next.Longs[next.Offset + k];
			}
		  }
		}

		// total number of positions, offsets and payloads
		int totalPositions = 0, totalOffsets = 0, totalPayloads = 0;
		for (int i = 0, termIndex = 0; i < totalFields; ++i)
		{
		  int f = (int) flags.Get(i);
		  int termCount = (int) numTerms.Get(i);
		  for (int j = 0; j < termCount; ++j)
		  {
			int freq = termFreqs[termIndex++];
            if ((f & CompressingTermVectorsWriter.POSITIONS) != 0)
			{
			  totalPositions += freq;
			}
            if ((f & CompressingTermVectorsWriter.OFFSETS) != 0)
			{
			  totalOffsets += freq;
			}
            if ((f & CompressingTermVectorsWriter.PAYLOADS) != 0)
			{
			  totalPayloads += freq;
			}
		  }
		  Debug.Assert(i != totalFields - 1 || termIndex == totalTerms, termIndex + " " + totalTerms);
		}

		int[][] positionIndex = PositionIndex(skip, numFields, numTerms, termFreqs);
		int[][] positions, startOffsets, lengths;
		if (totalPositions > 0)
		{
            positions = ReadPositions(skip, numFields, flags, numTerms, termFreqs, CompressingTermVectorsWriter.POSITIONS, totalPositions, positionIndex);
		}
		else
		{
		  positions = new int[numFields][];
		}

		if (totalOffsets > 0)
		{
		  // average number of chars per term
		  float[] charsPerTerm = new float[fieldNums.Length];
		  for (int i = 0; i < charsPerTerm.Length; ++i)
		  {
			charsPerTerm[i] = Number.IntBitsToFloat(VectorsStream_Renamed.ReadInt());
		  }
          startOffsets = ReadPositions(skip, numFields, flags, numTerms, termFreqs, CompressingTermVectorsWriter.OFFSETS, totalOffsets, positionIndex);
          lengths = ReadPositions(skip, numFields, flags, numTerms, termFreqs, CompressingTermVectorsWriter.OFFSETS, totalOffsets, positionIndex);

		  for (int i = 0; i < numFields; ++i)
		  {
			int[] fStartOffsets = startOffsets[i];
			int[] fPositions = positions[i];
			// patch offsets from positions
			if (fStartOffsets != null && fPositions != null)
			{
			  float fieldCharsPerTerm = charsPerTerm[fieldNumOffs[i]];
			  for (int j = 0; j < startOffsets[i].Length; ++j)
			  {
				fStartOffsets[j] += (int)(fieldCharsPerTerm * fPositions[j]);
			  }
			}
			if (fStartOffsets != null)
			{
			  int[] fPrefixLengths = prefixLengths[i];
			  int[] fSuffixLengths = suffixLengths[i];
			  int[] fLengths = lengths[i];
			  for (int j = 0, end = (int) numTerms.Get(skip + i); j < end; ++j)
			  {
				// delta-decode start offsets and  patch lengths using term lengths
				int termLength = fPrefixLengths[j] + fSuffixLengths[j];
				lengths[i][positionIndex[i][j]] += termLength;
				for (int k = positionIndex[i][j] + 1; k < positionIndex[i][j + 1]; ++k)
				{
				  fStartOffsets[k] += fStartOffsets[k - 1];
				  fLengths[k] += termLength;
				}
			  }
			}
		  }
		}
		else
		{
		  startOffsets = lengths = new int[numFields][];
		}
		if (totalPositions > 0)
		{
		  // delta-decode positions
		  for (int i = 0; i < numFields; ++i)
		  {
			int[] fPositions = positions[i];
			int[] fpositionIndex = positionIndex[i];
			if (fPositions != null)
			{
			  for (int j = 0, end = (int) numTerms.Get(skip + i); j < end; ++j)
			  {
				// delta-decode start offsets
				for (int k = fpositionIndex[j] + 1; k < fpositionIndex[j + 1]; ++k)
				{
				  fPositions[k] += fPositions[k - 1];
				}
			  }
			}
		  }
		}

		// payload lengths
		int[][] payloadIndex = new int[numFields][];
		int totalPayloadLength = 0;
		int payloadOff = 0;
		int payloadLen = 0;
		if (totalPayloads > 0)
		{
		  Reader.Reset(VectorsStream_Renamed, totalPayloads);
		  // skip
		  int termIndex = 0;
		  for (int i = 0; i < skip; ++i)
		  {
			int f = (int) flags.Get(i);
			int termCount = (int) numTerms.Get(i);
            if ((f & CompressingTermVectorsWriter.PAYLOADS) != 0)
			{
			  for (int j = 0; j < termCount; ++j)
			  {
				int freq = termFreqs[termIndex + j];
				for (int k = 0; k < freq; ++k)
				{
				  int l = (int) Reader.Next();
				  payloadOff += l;
				}
			  }
			}
			termIndex += termCount;
		  }
		  totalPayloadLength = payloadOff;
		  // read doc payload lengths
		  for (int i = 0; i < numFields; ++i)
		  {
			int f = (int) flags.Get(skip + i);
			int termCount = (int) numTerms.Get(skip + i);
            if ((f & CompressingTermVectorsWriter.PAYLOADS) != 0)
			{
			  int totalFreq = positionIndex[i][termCount];
			  payloadIndex[i] = new int[totalFreq + 1];
			  int posIdx = 0;
			  payloadIndex[i][posIdx] = payloadLen;
			  for (int j = 0; j < termCount; ++j)
			  {
				int freq = termFreqs[termIndex + j];
				for (int k = 0; k < freq; ++k)
				{
				  int payloadLength = (int) Reader.Next();
				  payloadLen += payloadLength;
				  payloadIndex[i][posIdx + 1] = payloadLen;
				  ++posIdx;
				}
			  }
			  Debug.Assert(posIdx == totalFreq);
			}
			termIndex += termCount;
		  }
		  totalPayloadLength += payloadLen;
		  for (int i = skip + numFields; i < totalFields; ++i)
		  {
			int f = (int) flags.Get(i);
			int termCount = (int) numTerms.Get(i);
            if ((f & CompressingTermVectorsWriter.PAYLOADS) != 0)
			{
			  for (int j = 0; j < termCount; ++j)
			  {
				int freq = termFreqs[termIndex + j];
				for (int k = 0; k < freq; ++k)
				{
				  totalPayloadLength += (int)Reader.Next();
				}
			  }
			}
			termIndex += termCount;
		  }
		  Debug.Assert(termIndex == totalTerms, termIndex + " " + totalTerms);
		}

		// decompress data
		BytesRef suffixBytes = new BytesRef();
		Decompressor.Decompress(VectorsStream_Renamed, totalLen + totalPayloadLength, docOff + payloadOff, docLen + payloadLen, suffixBytes);
		suffixBytes.Length = docLen;
		BytesRef payloadBytes = new BytesRef(suffixBytes.Bytes, suffixBytes.Offset + docLen, payloadLen);

		int[] FieldFlags = new int[numFields];
		for (int i = 0; i < numFields; ++i)
		{
		  FieldFlags[i] = (int) flags.Get(skip + i);
		}

		int[] fieldNumTerms = new int[numFields];
		for (int i = 0; i < numFields; ++i)
		{
		  fieldNumTerms[i] = (int) numTerms.Get(skip + i);
		}

		int[][] fieldTermFreqs = new int[numFields][];
		{
		  int termIdx = 0;
		  for (int i = 0; i < skip; ++i)
		  {
			termIdx += (int)numTerms.Get(i);
		  }
		  for (int i = 0; i < numFields; ++i)
		  {
			int termCount = (int) numTerms.Get(skip + i);
			fieldTermFreqs[i] = new int[termCount];
			for (int j = 0; j < termCount; ++j)
			{
			  fieldTermFreqs[i][j] = termFreqs[termIdx++];
			}
		  }
		}

		Debug.Assert(Sum(fieldLengths) == docLen, Sum(fieldLengths) + " != " + docLen);

		return new TVFields(this, fieldNums, FieldFlags, fieldNumOffs, fieldNumTerms, fieldLengths, prefixLengths, suffixLengths, fieldTermFreqs, positionIndex, positions, startOffsets, lengths, payloadBytes, payloadIndex, suffixBytes);
	  }

	  // field -> term index -> position index
	  private int[][] PositionIndex(int skip, int numFields, PackedInts.Reader numTerms, int[] termFreqs)
	  {
		int[][] positionIndex = new int[numFields][];
		int termIndex = 0;
		for (int i = 0; i < skip; ++i)
		{
		  int termCount = (int) numTerms.Get(i);
		  termIndex += termCount;
		}
		for (int i = 0; i < numFields; ++i)
		{
		  int termCount = (int) numTerms.Get(skip + i);
		  positionIndex[i] = new int[termCount + 1];
		  for (int j = 0; j < termCount; ++j)
		  {
			int freq = termFreqs[termIndex + j];
			positionIndex[i][j + 1] = positionIndex[i][j] + freq;
		  }
		  termIndex += termCount;
		}
		return positionIndex;
	  }

	  private int[][] ReadPositions(int skip, int numFields, PackedInts.Reader flags, PackedInts.Reader numTerms, int[] termFreqs, int flag, int totalPositions, int[][] positionIndex)
	  {
		int[][] positions = new int[numFields][];
		Reader.Reset(VectorsStream_Renamed, totalPositions);
		// skip
		int toSkip = 0;
		int termIndex = 0;
		for (int i = 0; i < skip; ++i)
		{
		  int f = (int) flags.Get(i);
		  int termCount = (int) numTerms.Get(i);
		  if ((f & flag) != 0)
		  {
			for (int j = 0; j < termCount; ++j)
			{
			  int freq = termFreqs[termIndex + j];
			  toSkip += freq;
			}
		  }
		  termIndex += termCount;
		}
		Reader.Skip(toSkip);
		// read doc positions
		for (int i = 0; i < numFields; ++i)
		{
		  int f = (int) flags.Get(skip + i);
		  int termCount = (int) numTerms.Get(skip + i);
		  if ((f & flag) != 0)
		  {
			int totalFreq = positionIndex[i][termCount];
			int[] fieldPositions = new int[totalFreq];
			positions[i] = fieldPositions;
			for (int j = 0; j < totalFreq;)
			{
			  LongsRef nextPositions = Reader.Next(totalFreq - j);
			  for (int k = 0; k < nextPositions.Length; ++k)
			  {
				fieldPositions[j++] = (int) nextPositions.Longs[nextPositions.Offset + k];
			  }
			}
		  }
		  termIndex += termCount;
		}
		Reader.Skip(totalPositions - Reader.Ord());
		return positions;
	  }

	  private class TVFields : Fields
	  {
		  private readonly CompressingTermVectorsReader OuterInstance;


		internal readonly int[] FieldNums, FieldFlags, FieldNumOffs, NumTerms, FieldLengths;
		internal readonly int[][] PrefixLengths, SuffixLengths, TermFreqs, PositionIndex, Positions, StartOffsets, Lengths, PayloadIndex;
		internal readonly BytesRef SuffixBytes, PayloadBytes;

		public TVFields(CompressingTermVectorsReader outerInstance, int[] fieldNums, int[] fieldFlags, int[] fieldNumOffs, int[] numTerms, int[] fieldLengths, int[][] prefixLengths, int[][] suffixLengths, int[][] termFreqs, int[][] positionIndex, int[][] positions, int[][] startOffsets, int[][] lengths, BytesRef payloadBytes, int[][] payloadIndex, BytesRef suffixBytes)
		{
			this.OuterInstance = outerInstance;
		  this.FieldNums = fieldNums;
		  this.FieldFlags = fieldFlags;
		  this.FieldNumOffs = fieldNumOffs;
		  this.NumTerms = numTerms;
		  this.FieldLengths = fieldLengths;
		  this.PrefixLengths = prefixLengths;
		  this.SuffixLengths = suffixLengths;
		  this.TermFreqs = termFreqs;
		  this.PositionIndex = positionIndex;
		  this.Positions = positions;
		  this.StartOffsets = startOffsets;
		  this.Lengths = lengths;
		  this.PayloadBytes = payloadBytes;
		  this.PayloadIndex = payloadIndex;
		  this.SuffixBytes = suffixBytes;
		}

		public override IEnumerator<string> Iterator()
		{
		  return new IteratorAnonymousInnerClassHelper(this);
		}

		private class IteratorAnonymousInnerClassHelper : IEnumerator<string>
		{
			private readonly TVFields OuterInstance;

			public IteratorAnonymousInnerClassHelper(TVFields outerInstance)
			{
				this.OuterInstance = outerInstance;
				i = 0;
			}

			internal int i;
			public virtual bool HasNext()
			{
			  return i < OuterInstance.FieldNumOffs.Length;
			}
			public virtual string Next()
			{
			  if (!HasNext())
			  {
				throw new Exception();
			  }
			  int fieldNum = OuterInstance.FieldNums[OuterInstance.FieldNumOffs[i++]];
			  return OuterInstance.OuterInstance.FieldInfos.FieldInfo(fieldNum).Name;
			}
			public virtual void Remove()
			{
			  throw new System.NotSupportedException();
			}
		}

		public override Terms Terms(string field)
		{
		  FieldInfo fieldInfo = OuterInstance.FieldInfos.FieldInfo(field);
		  if (fieldInfo == null)
		  {
			return null;
		  }
		  int idx = -1;
		  for (int i = 0; i < FieldNumOffs.Length; ++i)
		  {
			if (FieldNums[FieldNumOffs[i]] == fieldInfo.Number)
			{
			  idx = i;
			  break;
			}
		  }

		  if (idx == -1 || NumTerms[idx] == 0)
		  {
			// no term
			return null;
		  }
		  int fieldOff = 0, fieldLen = -1;
		  for (int i = 0; i < FieldNumOffs.Length; ++i)
		  {
			if (i < idx)
			{
			  fieldOff += FieldLengths[i];
			}
			else
			{
			  fieldLen = FieldLengths[i];
			  break;
			}
		  }
		  Debug.Assert(fieldLen >= 0);
		  return new TVTerms(OuterInstance, NumTerms[idx], FieldFlags[idx], PrefixLengths[idx], SuffixLengths[idx], TermFreqs[idx], PositionIndex[idx], Positions[idx], StartOffsets[idx], Lengths[idx], PayloadIndex[idx], PayloadBytes, new BytesRef(SuffixBytes.Bytes, SuffixBytes.Offset + fieldOff, fieldLen));
		}

		public override int Size()
		{
		  return FieldNumOffs.Length;
		}

	  }

	  private class TVTerms : Terms
	  {
		  private readonly CompressingTermVectorsReader OuterInstance;


		internal readonly int NumTerms, Flags;
		internal readonly int[] PrefixLengths, SuffixLengths, TermFreqs, PositionIndex, Positions, StartOffsets, Lengths, PayloadIndex;
		internal readonly BytesRef TermBytes, PayloadBytes;

		internal TVTerms(CompressingTermVectorsReader outerInstance, int numTerms, int flags, int[] prefixLengths, int[] suffixLengths, int[] termFreqs, int[] positionIndex, int[] positions, int[] startOffsets, int[] lengths, int[] payloadIndex, BytesRef payloadBytes, BytesRef termBytes)
		{
			this.OuterInstance = outerInstance;
		  this.NumTerms = numTerms;
		  this.Flags = flags;
		  this.PrefixLengths = prefixLengths;
		  this.SuffixLengths = suffixLengths;
		  this.TermFreqs = termFreqs;
		  this.PositionIndex = positionIndex;
		  this.Positions = positions;
		  this.StartOffsets = startOffsets;
		  this.Lengths = lengths;
		  this.PayloadIndex = payloadIndex;
		  this.PayloadBytes = payloadBytes;
		  this.TermBytes = termBytes;
		}

		public override TermsEnum Iterator(TermsEnum reuse)
		{
		  TVTermsEnum termsEnum;
		  if (reuse != null && reuse is TVTermsEnum)
		  {
			termsEnum = (TVTermsEnum) reuse;
		  }
		  else
		  {
			termsEnum = new TVTermsEnum();
		  }
		  termsEnum.Reset(NumTerms, Flags, PrefixLengths, SuffixLengths, TermFreqs, PositionIndex, Positions, StartOffsets, Lengths, PayloadIndex, PayloadBytes, new ByteArrayDataInput(TermBytes.Bytes, TermBytes.Offset, TermBytes.Length));
		  return termsEnum;
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

		public override long Size()
		{
		  return NumTerms;
		}

		public override long SumTotalTermFreq
		{
			get
			{
			  return -1L;
			}
		}

		public override long SumDocFreq
		{
			get
			{
			  return NumTerms;
			}
		}

		public override int DocCount
		{
			get
			{
			  return 1;
			}
		}

		public override bool HasFreqs()
		{
		  return true;
		}

		public override bool HasOffsets()
		{
            return (Flags & CompressingTermVectorsWriter.OFFSETS) != 0;
		}

		public override bool HasPositions()
		{
            return (Flags & CompressingTermVectorsWriter.POSITIONS) != 0;
		}

		public override bool HasPayloads()
		{
            return (Flags & CompressingTermVectorsWriter.PAYLOADS) != 0;
		}

	  }

	  private class TVTermsEnum : TermsEnum
	  {

		internal int NumTerms, StartPos, Ord_Renamed;
		internal int[] PrefixLengths, SuffixLengths, TermFreqs, PositionIndex, Positions, StartOffsets, Lengths, PayloadIndex;
		internal ByteArrayDataInput @in;
		internal BytesRef Payloads;
		internal readonly BytesRef Term_Renamed;

		internal TVTermsEnum()
		{
		  Term_Renamed = new BytesRef(16);
		}

		internal virtual void Reset(int numTerms, int flags, int[] prefixLengths, int[] suffixLengths, int[] termFreqs, int[] positionIndex, int[] positions, int[] startOffsets, int[] lengths, int[] payloadIndex, BytesRef payloads, ByteArrayDataInput @in)
		{
		  this.NumTerms = numTerms;
		  this.PrefixLengths = prefixLengths;
		  this.SuffixLengths = suffixLengths;
		  this.TermFreqs = termFreqs;
		  this.PositionIndex = positionIndex;
		  this.Positions = positions;
		  this.StartOffsets = startOffsets;
		  this.Lengths = lengths;
		  this.PayloadIndex = payloadIndex;
		  this.Payloads = payloads;
		  this.@in = @in;
		  StartPos = @in.Position;
		  Reset();
		}

		internal virtual void Reset()
		{
		  Term_Renamed.Length = 0;
		  @in.Position = StartPos;
		  Ord_Renamed = -1;
		}

		public override BytesRef Next()
		{
		  if (Ord_Renamed == NumTerms - 1)
		  {
			return null;
		  }
		  else
		  {
			Debug.Assert(Ord_Renamed < NumTerms);
			++Ord_Renamed;
		  }

		  // read term
		  Term_Renamed.Offset = 0;
		  Term_Renamed.Length = PrefixLengths[Ord_Renamed] + SuffixLengths[Ord_Renamed];
		  if (Term_Renamed.Length > Term_Renamed.Bytes.Length)
		  {
			Term_Renamed.Bytes = ArrayUtil.Grow(Term_Renamed.Bytes, Term_Renamed.Length);
		  }
		  @in.ReadBytes(Term_Renamed.Bytes, PrefixLengths[Ord_Renamed], SuffixLengths[Ord_Renamed]);

		  return Term_Renamed;
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

		public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
		{
		  if (Ord_Renamed < NumTerms && Ord_Renamed >= 0)
		  {
			int cmp = Term().CompareTo(text);
			if (cmp == 0)
			{
			  return TermsEnum.SeekStatus.FOUND;
			}
			else if (cmp > 0)
			{
			  Reset();
			}
		  }
		  // linear scan
		  while (true)
		  {
			BytesRef term = Next();
			if (term == null)
			{
			  return TermsEnum.SeekStatus.END;
			}
			int cmp = term.CompareTo(text);
			if (cmp > 0)
			{
			  return TermsEnum.SeekStatus.NOT_FOUND;
			}
			else if (cmp == 0)
			{
			  return TermsEnum.SeekStatus.FOUND;
			}
		  }
		}

		public override void SeekExact(long ord)
		{
		  throw new System.NotSupportedException();
		}

		public override BytesRef Term()
		{
		  return Term_Renamed;
		}

		public override long Ord()
		{
		  throw new System.NotSupportedException();
		}

		public override int DocFreq()
		{
		  return 1;
		}

		public override long TotalTermFreq()
		{
		  return TermFreqs[Ord_Renamed];
		}

		public override sealed DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
		{
		  TVDocsEnum docsEnum;
		  if (reuse != null && reuse is TVDocsEnum)
		  {
			docsEnum = (TVDocsEnum) reuse;
		  }
		  else
		  {
			docsEnum = new TVDocsEnum();
		  }

		  docsEnum.Reset(liveDocs, TermFreqs[Ord_Renamed], PositionIndex[Ord_Renamed], Positions, StartOffsets, Lengths, Payloads, PayloadIndex);
		  return docsEnum;
		}

		public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		{
		  if (Positions == null && StartOffsets == null)
		  {
			return null;
		  }
		  // TODO: slightly sheisty
		  return (DocsAndPositionsEnum) Docs(liveDocs, reuse, flags);
		}

	  }

	  private class TVDocsEnum : DocsAndPositionsEnum
	  {

		internal Bits LiveDocs;
		internal int Doc = -1;
		internal int TermFreq;
		internal int PositionIndex;
		internal int[] Positions;
		internal int[] StartOffsets;
		internal int[] Lengths;
		internal readonly BytesRef Payload_Renamed;
		internal int[] PayloadIndex;
		internal int BasePayloadOffset;
		internal int i;

		internal TVDocsEnum()
		{
		  Payload_Renamed = new BytesRef();
		}

		public virtual void Reset(Bits liveDocs, int freq, int positionIndex, int[] positions, int[] startOffsets, int[] lengths, BytesRef payloads, int[] payloadIndex)
		{
		  this.LiveDocs = liveDocs;
		  this.TermFreq = freq;
		  this.PositionIndex = positionIndex;
		  this.Positions = positions;
		  this.StartOffsets = startOffsets;
		  this.Lengths = lengths;
		  this.BasePayloadOffset = payloads.Offset;
		  this.Payload_Renamed.Bytes = payloads.Bytes;
		  Payload_Renamed.Offset = Payload_Renamed.Length = 0;
		  this.PayloadIndex = payloadIndex;

		  Doc = i = -1;
		}

		internal virtual void CheckDoc()
		{
          if (Doc == NO_MORE_DOCS)
		  {
			throw new Exception("DocsEnum exhausted");
		  }
		  else if (Doc == -1)
		  {
			throw new Exception("DocsEnum not started");
		  }
		}

		internal virtual void CheckPosition()
		{
		  CheckDoc();
		  if (i < 0)
		  {
			throw new Exception("Position enum not started");
		  }
		  else if (i >= TermFreq)
		  {
			throw new Exception("Read past last position");
		  }
		}

		public override int NextPosition()
		{
		  if (Doc != 0)
		  {
			throw new Exception();
		  }
		  else if (i >= TermFreq - 1)
		  {
			throw new Exception("Read past last position");
		  }

		  ++i;

		  if (PayloadIndex != null)
		  {
			Payload_Renamed.Offset = BasePayloadOffset + PayloadIndex[PositionIndex + i];
			Payload_Renamed.Length = PayloadIndex[PositionIndex + i + 1] - PayloadIndex[PositionIndex + i];
		  }

		  if (Positions == null)
		  {
			return -1;
		  }
		  else
		  {
			return Positions[PositionIndex + i];
		  }
		}

		public override int StartOffset()
		{
		  CheckPosition();
		  if (StartOffsets == null)
		  {
			return -1;
		  }
		  else
		  {
			return StartOffsets[PositionIndex + i];
		  }
		}

		public override int EndOffset()
		{
		  CheckPosition();
		  if (StartOffsets == null)
		  {
			return -1;
		  }
		  else
		  {
			return StartOffsets[PositionIndex + i] + Lengths[PositionIndex + i];
		  }
		}

		public override BytesRef Payload
		{
			get
			{
			  CheckPosition();
			  if (PayloadIndex == null || Payload_Renamed.Length == 0)
			  {
				return null;
			  }
			  else
			  {
				return Payload_Renamed;
			  }
			}
		}

		public override int Freq()
		{
		  CheckDoc();
		  return TermFreq;
		}

		public override int DocID()
		{
		  return Doc;
		}

		public override int NextDoc()
		{
		  if (Doc == -1 && (LiveDocs == null || LiveDocs.Get(0)))
		  {
			return (Doc = 0);
		  }
		  else
		  {
              return (Doc = NO_MORE_DOCS);
		  }
		}

		public override int Advance(int target)
		{
		  return SlowAdvance(target);
		}

		public override long Cost()
		{
		  return 1;
		}
	  }

	  private static int Sum(int[] arr)
	  {
		int sum = 0;
		foreach (int el in arr)
		{
		  sum += el;
		}
		return sum;
	  }

	  public override long RamBytesUsed()
	  {
		return IndexReader.RamBytesUsed();
	  }

	  public override void CheckIntegrity()
	  {
          if (Version_Renamed >= CompressingTermVectorsWriter.VERSION_CHECKSUM)
		{
		  CodecUtil.ChecksumEntireFile(VectorsStream_Renamed);
		}
	  }

	}

}