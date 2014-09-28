using System;
using System.Diagnostics;
using System.Collections;
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


	using TermsReader = org.apache.lucene.codecs.memory.FSTTermsReader.TermsReader;
	using CorruptIndexException = org.apache.lucene.index.CorruptIndexException;
	using DocsAndPositionsEnum = org.apache.lucene.index.DocsAndPositionsEnum;
	using DocsEnum = org.apache.lucene.index.DocsEnum;
	using IndexOptions = org.apache.lucene.index.FieldInfo.IndexOptions;
	using FieldInfo = org.apache.lucene.index.FieldInfo;
	using FieldInfos = org.apache.lucene.index.FieldInfos;
	using IndexFileNames = org.apache.lucene.index.IndexFileNames;
	using SegmentInfo = org.apache.lucene.index.SegmentInfo;
	using SegmentReadState = org.apache.lucene.index.SegmentReadState;
	using TermState = org.apache.lucene.index.TermState;
	using Terms = org.apache.lucene.index.Terms;
	using TermsEnum = org.apache.lucene.index.TermsEnum;
	using ByteArrayDataInput = org.apache.lucene.store.ByteArrayDataInput;
	using ChecksumIndexInput = org.apache.lucene.store.ChecksumIndexInput;
	using IndexInput = org.apache.lucene.store.IndexInput;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using Bits = org.apache.lucene.util.Bits;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using RamUsageEstimator = org.apache.lucene.util.RamUsageEstimator;
	using ByteRunAutomaton = org.apache.lucene.util.automaton.ByteRunAutomaton;
	using CompiledAutomaton = org.apache.lucene.util.automaton.CompiledAutomaton;
	using InputOutput = org.apache.lucene.util.fst.BytesRefFSTEnum.InputOutput;
	using BytesRefFSTEnum = org.apache.lucene.util.fst.BytesRefFSTEnum;
	using FST = org.apache.lucene.util.fst.FST;
	using Outputs = org.apache.lucene.util.fst.Outputs;
	using PositiveIntOutputs = org.apache.lucene.util.fst.PositiveIntOutputs;
	using Util = org.apache.lucene.util.fst.Util;

	/// <summary>
	/// FST-based terms dictionary reader.
	/// 
	/// The FST index maps each term and its ord, and during seek 
	/// the ord is used fetch metadata from a single block.
	/// The term dictionary is fully memory resident.
	/// 
	/// @lucene.experimental
	/// </summary>
	public class FSTOrdTermsReader : FieldsProducer
	{
	  internal const int INTERVAL = FSTOrdTermsWriter.SKIP_INTERVAL;
	  internal readonly SortedDictionary<string, TermsReader> fields = new SortedDictionary<string, TermsReader>();
	  internal readonly PostingsReaderBase postingsReader;
	  internal int version;
	  //static final boolean TEST = false;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FSTOrdTermsReader(org.apache.lucene.index.SegmentReadState state, org.apache.lucene.codecs.PostingsReaderBase postingsReader) throws java.io.IOException
	  public FSTOrdTermsReader(SegmentReadState state, PostingsReaderBase postingsReader)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String termsIndexFileName = org.apache.lucene.index.IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, FSTOrdTermsWriter.TERMS_INDEX_EXTENSION);
		string termsIndexFileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, FSTOrdTermsWriter.TERMS_INDEX_EXTENSION);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String termsBlockFileName = org.apache.lucene.index.IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, FSTOrdTermsWriter.TERMS_BLOCK_EXTENSION);
		string termsBlockFileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, FSTOrdTermsWriter.TERMS_BLOCK_EXTENSION);

		this.postingsReader = postingsReader;
		ChecksumIndexInput indexIn = null;
		IndexInput blockIn = null;
		bool success = false;
		try
		{
		  indexIn = state.directory.openChecksumInput(termsIndexFileName, state.context);
		  blockIn = state.directory.openInput(termsBlockFileName, state.context);
		  version = readHeader(indexIn);
		  readHeader(blockIn);
		  if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
		  {
			CodecUtil.checksumEntireFile(blockIn);
		  }

		  this.postingsReader.init(blockIn);
		  seekDir(blockIn);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.FieldInfos fieldInfos = state.fieldInfos;
		  FieldInfos fieldInfos = state.fieldInfos;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numFields = blockIn.readVInt();
		  int numFields = blockIn.readVInt();
		  for (int i = 0; i < numFields; i++)
		  {
			FieldInfo fieldInfo = fieldInfos.fieldInfo(blockIn.readVInt());
			bool hasFreq = fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY;
			long numTerms = blockIn.readVLong();
			long sumTotalTermFreq = hasFreq ? blockIn.readVLong() : -1;
			long sumDocFreq = blockIn.readVLong();
			int docCount = blockIn.readVInt();
			int longsSize = blockIn.readVInt();
			FST<long?> index = new FST<long?>(indexIn, PositiveIntOutputs.Singleton);

			TermsReader current = new TermsReader(fieldInfo, blockIn, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize, index);
			TermsReader previous = fields[fieldInfo.name] = current;
			checkFieldSummary(state.segmentInfo, indexIn, blockIn, current, previous);
		  }
		  if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
		  {
			CodecUtil.checkFooter(indexIn);
		  }
		  else
		  {
			CodecUtil.checkEOF(indexIn);
		  }
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(indexIn, blockIn);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(indexIn, blockIn);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int readHeader(org.apache.lucene.store.IndexInput in) throws java.io.IOException
	  private int readHeader(IndexInput @in)
	  {
		return CodecUtil.checkHeader(@in, FSTOrdTermsWriter.TERMS_CODEC_NAME, FSTOrdTermsWriter.TERMS_VERSION_START, FSTOrdTermsWriter.TERMS_VERSION_CURRENT);
	  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void seekDir(org.apache.lucene.store.IndexInput in) throws java.io.IOException
	  private void seekDir(IndexInput @in)
	  {
		if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
		{
		  @in.seek(@in.length() - CodecUtil.footerLength() - 8);
		}
		else
		{
		  @in.seek(@in.length() - 8);
		}
		@in.seek(@in.readLong());
	  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void checkFieldSummary(org.apache.lucene.index.SegmentInfo info, org.apache.lucene.store.IndexInput indexIn, org.apache.lucene.store.IndexInput blockIn, org.apache.lucene.codecs.memory.FSTTermsReader.TermsReader field, org.apache.lucene.codecs.memory.FSTTermsReader.TermsReader previous) throws java.io.IOException
	  private void checkFieldSummary(SegmentInfo info, IndexInput indexIn, IndexInput blockIn, TermsReader field, TermsReader previous)
	  {
		// #docs with field must be <= #docs
		if (field.docCount < 0 || field.docCount > info.DocCount)
		{
		  throw new CorruptIndexException("invalid docCount: " + field.docCount + " maxDoc: " + info.DocCount + " (resource=" + indexIn + ", " + blockIn + ")");
		}
		// #postings must be >= #docs with field
		if (field.sumDocFreq < field.docCount)
		{
		  throw new CorruptIndexException("invalid sumDocFreq: " + field.sumDocFreq + " docCount: " + field.docCount + " (resource=" + indexIn + ", " + blockIn + ")");
		}
		// #positions must be >= #postings
		if (field.sumTotalTermFreq != -1 && field.sumTotalTermFreq < field.sumDocFreq)
		{
		  throw new CorruptIndexException("invalid sumTotalTermFreq: " + field.sumTotalTermFreq + " sumDocFreq: " + field.sumDocFreq + " (resource=" + indexIn + ", " + blockIn + ")");
		}
		if (previous != null)
		{
		  throw new CorruptIndexException("duplicate fields: " + field.fieldInfo.name + " (resource=" + indexIn + ", " + blockIn + ")");
		}
	  }

	  public override IEnumerator<string> iterator()
	  {
		return Collections.unmodifiableSet(fields.Keys).GetEnumerator();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.Terms terms(String field) throws java.io.IOException
	  public override Terms terms(string field)
	  {
		Debug.Assert(field != null);
		return fields[field];
	  }

	  public override int size()
	  {
		return fields.Count;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		try
		{
		  IOUtils.close(postingsReader);
		}
		finally
		{
		  fields.Clear();
		}
	  }

	  internal sealed class TermsReader : Terms
	  {
		  private readonly FSTOrdTermsReader outerInstance;

		internal readonly FieldInfo fieldInfo;
		internal readonly long numTerms;
		internal readonly long sumTotalTermFreq;
		internal readonly long sumDocFreq;
		internal readonly int docCount;
		internal readonly int longsSize;
		internal readonly FST<long?> index;

		internal readonly int numSkipInfo;
		internal readonly long[] skipInfo;
		internal readonly sbyte[] statsBlock;
		internal readonly sbyte[] metaLongsBlock;
		internal readonly sbyte[] metaBytesBlock;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: org.apache.lucene.codecs.memory.FSTTermsReader.TermsReader(org.apache.lucene.index.FieldInfo fieldInfo, org.apache.lucene.store.IndexInput blockIn, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize, org.apache.lucene.util.fst.FST<Long> index) throws java.io.IOException
		internal TermsReader(FSTOrdTermsReader outerInstance, FieldInfo fieldInfo, IndexInput blockIn, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize, FST<long?> index)
		{
			this.outerInstance = outerInstance;
		  this.fieldInfo = fieldInfo;
		  this.numTerms = numTerms;
		  this.sumTotalTermFreq = sumTotalTermFreq;
		  this.sumDocFreq = sumDocFreq;
		  this.docCount = docCount;
		  this.longsSize = longsSize;
		  this.index = index;

		  assert(numTerms & (~0xffffffffL)) == 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numBlocks = (int)(numTerms + INTERVAL - 1) / INTERVAL;
		  int numBlocks = (int)(numTerms + INTERVAL - 1) / INTERVAL;
		  this.numSkipInfo = longsSize + 3;
		  this.skipInfo = new long[numBlocks * numSkipInfo];
		  this.statsBlock = new sbyte[(int)blockIn.readVLong()];
		  this.metaLongsBlock = new sbyte[(int)blockIn.readVLong()];
		  this.metaBytesBlock = new sbyte[(int)blockIn.readVLong()];

		  int last = 0, next = 0;
		  for (int i = 1; i < numBlocks; i++)
		  {
			next = numSkipInfo * i;
			for (int j = 0; j < numSkipInfo; j++)
			{
			  skipInfo[next + j] = skipInfo[last + j] + blockIn.readVLong();
			}
			last = next;
		  }
		  blockIn.readBytes(statsBlock, 0, statsBlock.Length);
		  blockIn.readBytes(metaLongsBlock, 0, metaLongsBlock.Length);
		  blockIn.readBytes(metaBytesBlock, 0, metaBytesBlock.Length);
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

		public override bool hasFreqs()
		{
		  return fieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS) >= 0;
		}

		public override bool hasOffsets()
		{
		  return fieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		}

		public override bool hasPositions()
		{
		  return fieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
		}

		public override bool hasPayloads()
		{
		  return fieldInfo.hasPayloads();
		}

		public override long size()
		{
		  return numTerms;
		}

		public override long SumTotalTermFreq
		{
			get
			{
			  return sumTotalTermFreq;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public long getSumDocFreq() throws java.io.IOException
		public override long SumDocFreq
		{
			get
			{
			  return sumDocFreq;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int getDocCount() throws java.io.IOException
		public override int DocCount
		{
			get
			{
			  return docCount;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.TermsEnum iterator(org.apache.lucene.index.TermsEnum reuse) throws java.io.IOException
		public override TermsEnum iterator(TermsEnum reuse)
		{
		  return new SegmentTermsEnum(this);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.TermsEnum intersect(org.apache.lucene.util.automaton.CompiledAutomaton compiled, org.apache.lucene.util.BytesRef startTerm) throws java.io.IOException
		public override TermsEnum intersect(CompiledAutomaton compiled, BytesRef startTerm)
		{
		  return new IntersectTermsEnum(this, compiled, startTerm);
		}

		// Only wraps common operations for PBF interact
		internal abstract class BaseTermsEnum : TermsEnum
		{
			private readonly FSTOrdTermsReader.TermsReader outerInstance;

		  /* Current term, null when enum ends or unpositioned */
		  internal BytesRef term_Renamed;

		  /* Current term's ord, starts from 0 */
		  internal long ord_Renamed;

		  /* Current term stats + decoded metadata (customized by PBF) */
		  internal readonly BlockTermState state;

		  /* Datainput to load stats & metadata */
		  internal readonly ByteArrayDataInput statsReader = new ByteArrayDataInput();
		  internal readonly ByteArrayDataInput metaLongsReader = new ByteArrayDataInput();
		  internal readonly ByteArrayDataInput metaBytesReader = new ByteArrayDataInput();

		  /* To which block is buffered */ 
		  internal int statsBlockOrd;
		  internal int metaBlockOrd;

		  /* Current buffered metadata (long[] & byte[]) */
		  internal long[][] longs;
		  internal int[] bytesStart;
		  internal int[] bytesLength;

		  /* Current buffered stats (df & ttf) */
		  internal int[] docFreq_Renamed;
		  internal long[] totalTermFreq_Renamed;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: BaseTermsEnum() throws java.io.IOException
		  internal BaseTermsEnum(FSTOrdTermsReader.TermsReader outerInstance)
		  {
			  this.outerInstance = outerInstance;
			this.state = outerInstance.outerInstance.postingsReader.newTermState();
			this.term_Renamed = null;
			this.statsReader.reset(outerInstance.statsBlock);
			this.metaLongsReader.reset(outerInstance.metaLongsBlock);
			this.metaBytesReader.reset(outerInstance.metaBytesBlock);

//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: this.longs = new long[INTERVAL][outerInstance.longsSize];
			this.longs = RectangularArrays.ReturnRectangularLongArray(INTERVAL, outerInstance.longsSize);
			this.bytesStart = new int[INTERVAL];
			this.bytesLength = new int[INTERVAL];
			this.docFreq_Renamed = new int[INTERVAL];
			this.totalTermFreq_Renamed = new long[INTERVAL];
			this.statsBlockOrd = -1;
			this.metaBlockOrd = -1;
			if (!outerInstance.hasFreqs())
			{
			  Arrays.fill(totalTermFreq_Renamed, -1);
			}
		  }

		  public override IComparer<BytesRef> Comparator
		  {
			  get
			  {
				return BytesRef.UTF8SortedAsUnicodeComparator;
			  }
		  }

		  /// <summary>
		  /// Decodes stats data into term state </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void decodeStats() throws java.io.IOException
		  internal virtual void decodeStats()
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int upto = (int)ord % INTERVAL;
			int upto = (int)ord_Renamed % INTERVAL;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int oldBlockOrd = statsBlockOrd;
			int oldBlockOrd = statsBlockOrd;
			statsBlockOrd = (int)ord_Renamed / INTERVAL;
			if (oldBlockOrd != statsBlockOrd)
			{
			  refillStats();
			}
			state.docFreq = docFreq_Renamed[upto];
			state.totalTermFreq = totalTermFreq_Renamed[upto];
		  }

		  /// <summary>
		  /// Let PBF decode metadata </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void decodeMetaData() throws java.io.IOException
		  internal virtual void decodeMetaData()
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int upto = (int)ord % INTERVAL;
			int upto = (int)ord_Renamed % INTERVAL;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int oldBlockOrd = metaBlockOrd;
			int oldBlockOrd = metaBlockOrd;
			metaBlockOrd = (int)ord_Renamed / INTERVAL;
			if (metaBlockOrd != oldBlockOrd)
			{
			  refillMetadata();
			}
			metaBytesReader.Position = bytesStart[upto];
			outerInstance.outerInstance.postingsReader.decodeTerm(longs[upto], metaBytesReader, outerInstance.fieldInfo, state, true);
		  }

		  /// <summary>
		  /// Load current stats shard </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: final void refillStats() throws java.io.IOException
		  internal void refillStats()
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int offset = statsBlockOrd * numSkipInfo;
			int offset = statsBlockOrd * outerInstance.numSkipInfo;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int statsFP = (int)skipInfo[offset];
			int statsFP = (int)outerInstance.skipInfo[offset];
			statsReader.Position = statsFP;
			for (int i = 0; i < INTERVAL && !statsReader.eof(); i++)
			{
			  int code = statsReader.readVInt();
			  if (outerInstance.hasFreqs())
			  {
				docFreq_Renamed[i] = ((int)((uint)code >> 1));
				if ((code & 1) == 1)
				{
				  totalTermFreq_Renamed[i] = docFreq_Renamed[i];
				}
				else
				{
				  totalTermFreq_Renamed[i] = docFreq_Renamed[i] + statsReader.readVLong();
				}
			  }
			  else
			  {
				docFreq_Renamed[i] = code;
			  }
			}
		  }

		  /// <summary>
		  /// Load current metadata shard </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: final void refillMetadata() throws java.io.IOException
		  internal void refillMetadata()
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int offset = metaBlockOrd * numSkipInfo;
			int offset = metaBlockOrd * outerInstance.numSkipInfo;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int metaLongsFP = (int)skipInfo[offset + 1];
			int metaLongsFP = (int)outerInstance.skipInfo[offset + 1];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int metaBytesFP = (int)skipInfo[offset + 2];
			int metaBytesFP = (int)outerInstance.skipInfo[offset + 2];
			metaLongsReader.Position = metaLongsFP;
			for (int j = 0; j < outerInstance.longsSize; j++)
			{
			  longs[0][j] = outerInstance.skipInfo[offset + 3 + j] + metaLongsReader.readVLong();
			}
			bytesStart[0] = metaBytesFP;
			bytesLength[0] = (int)metaLongsReader.readVLong();
			for (int i = 1; i < INTERVAL && !metaLongsReader.eof(); i++)
			{
			  for (int j = 0; j < outerInstance.longsSize; j++)
			  {
				longs[i][j] = longs[i - 1][j] + metaLongsReader.readVLong();
			  }
			  bytesStart[i] = bytesStart[i - 1] + bytesLength[i - 1];
			  bytesLength[i] = (int)metaLongsReader.readVLong();
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.TermState termState() throws java.io.IOException
		  public override TermState termState()
		  {
			decodeMetaData();
			return state.clone();
		  }

		  public override BytesRef term()
		  {
			return term_Renamed;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int docFreq() throws java.io.IOException
		  public override int docFreq()
		  {
			return state.docFreq;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public long totalTermFreq() throws java.io.IOException
		  public override long totalTermFreq()
		  {
			return state.totalTermFreq;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.DocsEnum docs(org.apache.lucene.util.Bits liveDocs, org.apache.lucene.index.DocsEnum reuse, int flags) throws java.io.IOException
		  public override DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)
		  {
			decodeMetaData();
			return outerInstance.outerInstance.postingsReader.docs(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.DocsAndPositionsEnum docsAndPositions(org.apache.lucene.util.Bits liveDocs, org.apache.lucene.index.DocsAndPositionsEnum reuse, int flags) throws java.io.IOException
		  public override DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		  {
			if (!outerInstance.hasPositions())
			{
			  return null;
			}
			decodeMetaData();
			return outerInstance.outerInstance.postingsReader.docsAndPositions(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
		  }

		  // TODO: this can be achieved by making use of Util.getByOutput()
		  //           and should have related tests
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void seekExact(long ord) throws java.io.IOException
		  public override void seekExact(long ord)
		  {
			throw new System.NotSupportedException();
		  }

		  public override long ord()
		  {
			throw new System.NotSupportedException();
		  }
		}

		// Iterates through all terms in this field
		private sealed class SegmentTermsEnum : BaseTermsEnum
		{
			private readonly FSTOrdTermsReader.TermsReader outerInstance;

		  internal readonly BytesRefFSTEnum<long?> fstEnum;

		  /* True when current term's metadata is decoded */
		  internal bool decoded;

		  /* True when current enum is 'positioned' by seekExact(TermState) */
		  internal bool seekPending;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: SegmentTermsEnum() throws java.io.IOException
		  internal SegmentTermsEnum(FSTOrdTermsReader.TermsReader outerInstance) : base(outerInstance)
		  {
			  this.outerInstance = outerInstance;
			this.fstEnum = new BytesRefFSTEnum<>(outerInstance.index);
			this.decoded = false;
			this.seekPending = false;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override void decodeMetaData() throws java.io.IOException
		  internal override void decodeMetaData()
		  {
			if (!decoded && !seekPending)
			{
			  base.decodeMetaData();
			  decoded = true;
			}
		  }

		  // Update current enum according to FSTEnum
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void updateEnum(final org.apache.lucene.util.fst.BytesRefFSTEnum.InputOutput<Long> pair) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		  internal void updateEnum(InputOutput<long?> pair)
		  {
			if (pair == null)
			{
			  term_Renamed = null;
			}
			else
			{
			  term_Renamed = pair.input;
			  ord_Renamed = pair.output;
			  decodeStats();
			}
			decoded = false;
			seekPending = false;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.util.BytesRef next() throws java.io.IOException
		  public override BytesRef next()
		  {
			if (seekPending) // previously positioned, but termOutputs not fetched
			{
			  seekPending = false;
			  SeekStatus status = seekCeil(term_Renamed);
			  Debug.Assert(status == SeekStatus.FOUND); // must positioned on valid term
			}
			updateEnum(fstEnum.next());
			return term_Renamed;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean seekExact(org.apache.lucene.util.BytesRef target) throws java.io.IOException
		  public override bool seekExact(BytesRef target)
		  {
			updateEnum(fstEnum.seekExact(target));
			return term_Renamed != null;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public SeekStatus seekCeil(org.apache.lucene.util.BytesRef target) throws java.io.IOException
		  public override SeekStatus seekCeil(BytesRef target)
		  {
			updateEnum(fstEnum.seekCeil(target));
			if (term_Renamed == null)
			{
			  return SeekStatus.END;
			}
			else
			{
			  return term_Renamed.Equals(target) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
			}
		  }

		  public override void seekExact(BytesRef target, TermState otherState)
		  {
			if (!target.Equals(term_Renamed))
			{
			  state.copyFrom(otherState);
			  term_Renamed = BytesRef.deepCopyOf(target);
			  seekPending = true;
			}
		  }
		}

		// Iterates intersect result with automaton (cannot seek!)
		private sealed class IntersectTermsEnum : BaseTermsEnum
		{
			private readonly FSTOrdTermsReader.TermsReader outerInstance;

		  /* True when current term's metadata is decoded */
		  internal bool decoded;

		  /* True when there is pending term when calling next() */
		  internal bool pending;

		  /* stack to record how current term is constructed, 
		   * used to accumulate metadata or rewind term:
		   *   level == term.length + 1,
		   *         == 0 when term is null */
		  internal Frame[] stack;
		  internal int level;

		  /* term dict fst */
		  internal readonly FST<long?> fst;
		  internal readonly FST.BytesReader fstReader;
		  internal readonly Outputs<long?> fstOutputs;

		  /* query automaton to intersect with */
		  internal readonly ByteRunAutomaton fsa;

		  private sealed class Frame
		  {
			  private readonly FSTOrdTermsReader.TermsReader.IntersectTermsEnum outerInstance;

			/* fst stats */
			internal FST.Arc<long?> arc;

			/* automaton stats */
			internal int state;

			internal Frame(FSTOrdTermsReader.TermsReader.IntersectTermsEnum outerInstance)
			{
				this.outerInstance = outerInstance;
			  this.arc = new FST.Arc<>();
			  this.state = -1;
			}

			public override string ToString()
			{
			  return "arc=" + arc + " state=" + state;
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: IntersectTermsEnum(org.apache.lucene.util.automaton.CompiledAutomaton compiled, org.apache.lucene.util.BytesRef startTerm) throws java.io.IOException
		  internal IntersectTermsEnum(FSTOrdTermsReader.TermsReader outerInstance, CompiledAutomaton compiled, BytesRef startTerm) : base(outerInstance)
		  {
			//if (TEST) System.out.println("Enum init, startTerm=" + startTerm);
			  this.outerInstance = outerInstance;
			this.fst = outerInstance.index;
			this.fstReader = fst.BytesReader;
			this.fstOutputs = outerInstance.index.outputs;
			this.fsa = compiled.runAutomaton;
			this.level = -1;
			this.stack = new Frame[16];
			for (int i = 0 ; i < stack.Length; i++)
			{
			  this.stack[i] = new Frame(this);
			}

			Frame frame;
			frame = loadVirtualFrame(newFrame());
			this.level++;
			frame = loadFirstFrame(newFrame());
			pushFrame(frame);

			this.decoded = false;
			this.pending = false;

			if (startTerm == null)
			{
			  pending = isAccept(topFrame());
			}
			else
			{
			  doSeekCeil(startTerm);
			  pending = !startTerm.Equals(term_Renamed) && isValid(topFrame()) && isAccept(topFrame());
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override void decodeMetaData() throws java.io.IOException
		  internal override void decodeMetaData()
		  {
			if (!decoded)
			{
			  base.decodeMetaData();
			  decoded = true;
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override void decodeStats() throws java.io.IOException
		  internal override void decodeStats()
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Long> arc = topFrame().arc;
			FST.Arc<long?> arc = topFrame().arc;
			Debug.Assert(arc.nextFinalOutput == fstOutputs.NoOutput);
			ord_Renamed = arc.output;
			base.decodeStats();
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public SeekStatus seekCeil(org.apache.lucene.util.BytesRef target) throws java.io.IOException
		  public override SeekStatus seekCeil(BytesRef target)
		  {
			throw new System.NotSupportedException();
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.util.BytesRef next() throws java.io.IOException
		  public override BytesRef next()
		  {
			//if (TEST) System.out.println("Enum next()");
			if (pending)
			{
			  pending = false;
			  decodeStats();
			  return term_Renamed;
			}
			decoded = false;
			while (level > 0)
			{
			  Frame frame = newFrame();
			  if (loadExpandFrame(topFrame(), frame) != null) // has valid target
			  {
				pushFrame(frame);
				if (isAccept(frame)) // gotcha
				{
				  break;
				}
				continue; // check next target
			  }
			  frame = popFrame();
			  while (level > 0)
			  {
				if (loadNextFrame(topFrame(), frame) != null) // has valid sibling
				{
				  pushFrame(frame);
				  if (isAccept(frame)) // gotcha
				  {
					goto DFSBreak;
				  }
				  goto DFSContinue; // check next target
				}
				frame = popFrame();
			  }
			  return null;
			  DFSContinue:;
			}
		  DFSBreak:
			decodeStats();
			return term_Renamed;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: org.apache.lucene.util.BytesRef doSeekCeil(org.apache.lucene.util.BytesRef target) throws java.io.IOException
		  internal BytesRef doSeekCeil(BytesRef target)
		  {
			//if (TEST) System.out.println("Enum doSeekCeil()");
			Frame frame = null;
			int label , upto = 0, limit = target.length;
			while (upto < limit) // to target prefix, or ceil label (rewind prefix)
			{
			  frame = newFrame();
			  label = target.bytes[upto] & 0xff;
			  frame = loadCeilFrame(label, topFrame(), frame);
			  if (frame == null || frame.arc.label != label)
			  {
				break;
			  }
			  Debug.Assert(isValid(frame)); // target must be fetched from automaton
			  pushFrame(frame);
			  upto++;
			}
			if (upto == limit) // got target
			{
			  return term_Renamed;
			}
			if (frame != null) // got larger term('s prefix)
			{
			  pushFrame(frame);
			  return isAccept(frame) ? term_Renamed : next();
			}
			while (level > 0) // got target's prefix, advance to larger term
			{
			  frame = popFrame();
			  while (level > 0 && !canRewind(frame))
			  {
				frame = popFrame();
			  }
			  if (loadNextFrame(topFrame(), frame) != null)
			  {
				pushFrame(frame);
				return isAccept(frame) ? term_Renamed : next();
			  }
			}
			return null;
		  }

		  /// <summary>
		  /// Virtual frame, never pop </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Frame loadVirtualFrame(Frame frame) throws java.io.IOException
		  internal Frame loadVirtualFrame(Frame frame)
		  {
			frame.arc.output = fstOutputs.NoOutput;
			frame.arc.nextFinalOutput = fstOutputs.NoOutput;
			frame.state = -1;
			return frame;
		  }

		  /// <summary>
		  /// Load frame for start arc(node) on fst </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Frame loadFirstFrame(Frame frame) throws java.io.IOException
		  internal Frame loadFirstFrame(Frame frame)
		  {
			frame.arc = fst.getFirstArc(frame.arc);
			frame.state = fsa.InitialState;
			return frame;
		  }

		  /// <summary>
		  /// Load frame for target arc(node) on fst </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Frame loadExpandFrame(Frame top, Frame frame) throws java.io.IOException
		  internal Frame loadExpandFrame(Frame top, Frame frame)
		  {
			if (!canGrow(top))
			{
			  return null;
			}
			frame.arc = fst.readFirstRealTargetArc(top.arc.target, frame.arc, fstReader);
			frame.state = fsa.step(top.state, frame.arc.label);
			//if (TEST) System.out.println(" loadExpand frame="+frame);
			if (frame.state == -1)
			{
			  return loadNextFrame(top, frame);
			}
			return frame;
		  }

		  /// <summary>
		  /// Load frame for sibling arc(node) on fst </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Frame loadNextFrame(Frame top, Frame frame) throws java.io.IOException
		  internal Frame loadNextFrame(Frame top, Frame frame)
		  {
			if (!canRewind(frame))
			{
			  return null;
			}
			while (!frame.arc.Last)
			{
			  frame.arc = fst.readNextRealArc(frame.arc, fstReader);
			  frame.state = fsa.step(top.state, frame.arc.label);
			  if (frame.state != -1)
			  {
				break;
			  }
			}
			//if (TEST) System.out.println(" loadNext frame="+frame);
			if (frame.state == -1)
			{
			  return null;
			}
			return frame;
		  }

		  /// <summary>
		  /// Load frame for target arc(node) on fst, so that 
		  ///  arc.label >= label and !fsa.reject(arc.label) 
		  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Frame loadCeilFrame(int label, Frame top, Frame frame) throws java.io.IOException
		  internal Frame loadCeilFrame(int label, Frame top, Frame frame)
		  {
			FST.Arc<long?> arc = frame.arc;
			arc = Util.readCeilArc(label, fst, top.arc, arc, fstReader);
			if (arc == null)
			{
			  return null;
			}
			frame.state = fsa.step(top.state, arc.label);
			//if (TEST) System.out.println(" loadCeil frame="+frame);
			if (frame.state == -1)
			{
			  return loadNextFrame(top, frame);
			}
			return frame;
		  }

		  internal bool isAccept(Frame frame) // reach a term both fst&fsa accepts
		  {
			return fsa.isAccept(frame.state) && frame.arc.Final;
		  }
		  internal bool isValid(Frame frame) // reach a prefix both fst&fsa won't reject
		  {
			return frame.state != -1; //frame != null &&
		  }
		  internal bool canGrow(Frame frame) // can walk forward on both fst&fsa
		  {
			return frame.state != -1 && FST.targetHasArcs(frame.arc);
		  }
		  internal bool canRewind(Frame frame) // can jump to sibling
		  {
			return !frame.arc.Last;
		  }

		  internal void pushFrame(Frame frame)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<Long> arc = frame.arc;
			FST.Arc<long?> arc = frame.arc;
			arc.output = fstOutputs.add(topFrame().arc.output, arc.output);
			term_Renamed = grow(arc.label);
			level++;
			Debug.Assert(frame == stack[level]);
		  }

		  internal Frame popFrame()
		  {
			term_Renamed = shrink();
			return stack[level--];
		  }

		  internal Frame newFrame()
		  {
			if (level + 1 == stack.Length)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Frame[] temp = new Frame[org.apache.lucene.util.ArrayUtil.oversize(level+2, org.apache.lucene.util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			  Frame[] temp = new Frame[ArrayUtil.oversize(level + 2, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			  Array.Copy(stack, 0, temp, 0, stack.Length);
			  for (int i = stack.Length; i < temp.Length; i++)
			  {
				temp[i] = new Frame(this);
			  }
			  stack = temp;
			}
			return stack[level + 1];
		  }

		  internal Frame topFrame()
		  {
			return stack[level];
		  }

		  internal BytesRef grow(int label)
		  {
			if (term_Renamed == null)
			{
			  term_Renamed = new BytesRef(new sbyte[16], 0, 0);
			}
			else
			{
			  if (term_Renamed.length == term_Renamed.bytes.length)
			  {
				term_Renamed.grow(term_Renamed.length + 1);
			  }
			  term_Renamed.bytes[term_Renamed.length++] = (sbyte)label;
			}
			return term_Renamed;
		  }

		  internal BytesRef shrink()
		  {
			if (term_Renamed.length == 0)
			{
			  term_Renamed = null;
			}
			else
			{
			  term_Renamed.length--;
			}
			return term_Renamed;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static<T> void walk(org.apache.lucene.util.fst.FST<T> fst) throws java.io.IOException
	  internal static void walk<T>(FST<T> fst)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.ArrayList<org.apache.lucene.util.fst.FST.Arc<T>> queue = new java.util.ArrayList<>();
		List<FST.Arc<T>> queue = new List<FST.Arc<T>>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.BitSet seen = new java.util.BitSet();
		BitArray seen = new BitArray();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.BytesReader reader = fst.getBytesReader();
		FST.BytesReader reader = fst.BytesReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<T> startArc = fst.getFirstArc(new org.apache.lucene.util.fst.FST.Arc<T>());
		FST.Arc<T> startArc = fst.getFirstArc(new FST.Arc<T>());
		queue.Add(startArc);
		while (queue.Count > 0)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<T> arc = queue.remove(0);
		  FST.Arc<T> arc = queue.Remove(0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long node = arc.target;
		  long node = arc.target;
		  //System.out.println(arc);
		  if (FST.targetHasArcs(arc) && !seen.Get((int) node))
		  {
			seen.Set((int) node, true);
			fst.readFirstRealTargetArc(node, arc, reader);
			while (true)
			{
			  queue.Add((new FST.Arc<T>()).copyFrom(arc));
			  if (arc.Last)
			  {
				break;
			  }
			  else
			  {
				fst.readNextRealArc(arc, reader);
			  }
			}
		  }
		}
	  }

	  public override long ramBytesUsed()
	  {
		long ramBytesUsed = 0;
		foreach (TermsReader r in fields.Values)
		{
		  if (r.index != null)
		  {
			ramBytesUsed += r.index.sizeInBytes();
			ramBytesUsed += RamUsageEstimator.sizeOf(r.metaBytesBlock);
			ramBytesUsed += RamUsageEstimator.sizeOf(r.metaLongsBlock);
			ramBytesUsed += RamUsageEstimator.sizeOf(r.skipInfo);
			ramBytesUsed += RamUsageEstimator.sizeOf(r.statsBlock);
		  }
		}
		return ramBytesUsed;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void checkIntegrity() throws java.io.IOException
	  public override void checkIntegrity()
	  {
		postingsReader.checkIntegrity();
	  }
	}

}