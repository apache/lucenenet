using System;
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


	using IndexOptions = org.apache.lucene.index.FieldInfo.IndexOptions;
	using FieldInfo = org.apache.lucene.index.FieldInfo;
	using FieldInfos = org.apache.lucene.index.FieldInfos;
	using IndexFileNames = org.apache.lucene.index.IndexFileNames;
	using SegmentWriteState = org.apache.lucene.index.SegmentWriteState;
	using DataOutput = org.apache.lucene.store.DataOutput;
	using IndexOutput = org.apache.lucene.store.IndexOutput;
	using RAMOutputStream = org.apache.lucene.store.RAMOutputStream;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using IntsRef = org.apache.lucene.util.IntsRef;
	using Builder = org.apache.lucene.util.fst.Builder;
	using FST = org.apache.lucene.util.fst.FST;
	using PositiveIntOutputs = org.apache.lucene.util.fst.PositiveIntOutputs;
	using Util = org.apache.lucene.util.fst.Util;

	/// <summary>
	/// FST-based term dict, using ord as FST output.
	/// 
	/// The FST holds the mapping between &lt;term, ord&gt;, and 
	/// term's metadata is delta encoded into a single byte block.
	/// 
	/// Typically the byte block consists of four parts:
	/// 1. term statistics: docFreq, totalTermFreq;
	/// 2. monotonic long[], e.g. the pointer to the postings list for that term;
	/// 3. generic byte[], e.g. other information customized by postings base.
	/// 4. single-level skip list to speed up metadata decoding by ord.
	/// 
	/// <para>
	/// Files:
	/// <ul>
	///  <li><tt>.tix</tt>: <a href="#Termindex">Term Index</a></li>
	///  <li><tt>.tbk</tt>: <a href="#Termblock">Term Block</a></li>
	/// </ul>
	/// </para>
	/// 
	/// <a name="Termindex" id="Termindex"></a>
	/// <h3>Term Index</h3>
	/// <para>
	///  The .tix contains a list of FSTs, one for each field.
	///  The FST maps a term to its corresponding order in current field.
	/// </para>
	/// 
	/// <ul>
	///  <li>TermIndex(.tix) --&gt; Header, TermFST<sup>NumFields</sup>, Footer</li>
	///  <li>TermFST --&gt; <seealso cref="FST FST&lt;long&gt;"/></li>
	///  <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
	///  <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
	/// </ul>
	/// 
	/// <para>Notes:</para>
	/// <ul>
	///  <li>
	///  Since terms are already sorted before writing to <a href="#Termblock">Term Block</a>, 
	///  their ords can directly used to seek term metadata from term block.
	///  </li>
	/// </ul>
	/// 
	/// <a name="Termblock" id="Termblock"></a>
	/// <h3>Term Block</h3>
	/// <para>
	///  The .tbk contains all the statistics and metadata for terms, along with field summary (e.g. 
	///  per-field data like number of documents in current field). For each field, there are four blocks:
	///  <ul>
	///   <li>statistics bytes block: contains term statistics; </li>
	///   <li>metadata longs block: delta-encodes monotonic part of metadata; </li>
	///   <li>metadata bytes block: encodes other parts of metadata; </li>
	///   <li>skip block: contains skip data, to speed up metadata seeking and decoding</li>
	///  </ul>
	/// </para>
	/// 
	/// <para>File Format:</para>
	/// <ul>
	///  <li>TermBlock(.tbk) --&gt; Header, <i>PostingsHeader</i>, FieldSummary, DirOffset</li>
	///  <li>FieldSummary --&gt; NumFields, &lt;FieldNumber, NumTerms, SumTotalTermFreq?, SumDocFreq,
	///                                         DocCount, LongsSize, DataBlock &gt; <sup>NumFields</sup>, Footer</li>
	/// 
	///  <li>DataBlock --&gt; StatsBlockLength, MetaLongsBlockLength, MetaBytesBlockLength, 
	///                       SkipBlock, StatsBlock, MetaLongsBlock, MetaBytesBlock </li>
	///  <li>SkipBlock --&gt; &lt; StatsFPDelta, MetaLongsSkipFPDelta, MetaBytesSkipFPDelta, 
	///                            MetaLongsSkipDelta<sup>LongsSize</sup> &gt;<sup>NumTerms</sup>
	///  <li>StatsBlock --&gt; &lt; DocFreq[Same?], (TotalTermFreq-DocFreq) ? &gt; <sup>NumTerms</sup>
	///  <li>MetaLongsBlock --&gt; &lt; LongDelta<sup>LongsSize</sup>, BytesSize &gt; <sup>NumTerms</sup>
	///  <li>MetaBytesBlock --&gt; Byte <sup>MetaBytesBlockLength</sup>
	///  <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
	///  <li>DirOffset --&gt; <seealso cref="DataOutput#writeLong Uint64"/></li>
	///  <li>NumFields, FieldNumber, DocCount, DocFreq, LongsSize, 
	///        FieldNumber, DocCount --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
	///  <li>NumTerms, SumTotalTermFreq, SumDocFreq, StatsBlockLength, MetaLongsBlockLength, MetaBytesBlockLength,
	///        StatsFPDelta, MetaLongsSkipFPDelta, MetaBytesSkipFPDelta, MetaLongsSkipStart, TotalTermFreq, 
	///        LongDelta,--&gt; <seealso cref="DataOutput#writeVLong VLong"/></li>
	///  <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
	/// </ul>
	/// <para>Notes: </para>
	/// <ul>
	///  <li>
	///   The format of PostingsHeader and MetaBytes are customized by the specific postings implementation:
	///   they contain arbitrary per-file data (such as parameters or versioning information), and per-term data 
	///   (non-monotonic ones like pulsed postings data).
	///  </li>
	///  <li>
	///   During initialization the reader will load all the blocks into memory. SkipBlock will be decoded, so that during seek
	///   term dict can lookup file pointers directly. StatsFPDelta, MetaLongsSkipFPDelta, etc. are file offset
	///   for every SkipInterval's term. MetaLongsSkipDelta is the difference from previous one, which indicates
	///   the value of preceding metadata longs for every SkipInterval's term.
	///  </li>
	///  <li>
	///   DocFreq is the count of documents which contain the term. TotalTermFreq is the total number of occurrences of the term. 
	///   Usually these two values are the same for long tail terms, therefore one bit is stole from DocFreq to check this case,
	///   so that encoding of TotalTermFreq may be omitted.
	///  </li>
	/// </ul>
	/// 
	/// @lucene.experimental 
	/// </summary>

	public class FSTOrdTermsWriter : FieldsConsumer
	{
	  internal const string TERMS_INDEX_EXTENSION = "tix";
	  internal const string TERMS_BLOCK_EXTENSION = "tbk";
	  internal const string TERMS_CODEC_NAME = "FST_ORD_TERMS_DICT";
	  public const int TERMS_VERSION_START = 0;
	  public const int TERMS_VERSION_CHECKSUM = 1;
	  public const int TERMS_VERSION_CURRENT = TERMS_VERSION_CHECKSUM;
	  public const int SKIP_INTERVAL = 8;

	  internal readonly PostingsWriterBase postingsWriter;
	  internal readonly FieldInfos fieldInfos;
	  internal readonly IList<FieldMetaData> fields = new List<FieldMetaData>();
	  internal IndexOutput blockOut = null;
	  internal IndexOutput indexOut = null;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FSTOrdTermsWriter(org.apache.lucene.index.SegmentWriteState state, org.apache.lucene.codecs.PostingsWriterBase postingsWriter) throws java.io.IOException
	  public FSTOrdTermsWriter(SegmentWriteState state, PostingsWriterBase postingsWriter)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String termsIndexFileName = org.apache.lucene.index.IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, TERMS_INDEX_EXTENSION);
		string termsIndexFileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, TERMS_INDEX_EXTENSION);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String termsBlockFileName = org.apache.lucene.index.IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, TERMS_BLOCK_EXTENSION);
		string termsBlockFileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, TERMS_BLOCK_EXTENSION);

		this.postingsWriter = postingsWriter;
		this.fieldInfos = state.fieldInfos;

		bool success = false;
		try
		{
		  this.indexOut = state.directory.createOutput(termsIndexFileName, state.context);
		  this.blockOut = state.directory.createOutput(termsBlockFileName, state.context);
		  writeHeader(indexOut);
		  writeHeader(blockOut);
		  this.postingsWriter.init(blockOut);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(indexOut, blockOut);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.codecs.TermsConsumer addField(org.apache.lucene.index.FieldInfo field) throws java.io.IOException
	  public override TermsConsumer addField(FieldInfo field)
	  {
		return new TermsWriter(this, field);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		if (blockOut != null)
		{
		  IOException ioe = null;
		  try
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long blockDirStart = blockOut.getFilePointer();
			long blockDirStart = blockOut.FilePointer;

			// write field summary
			blockOut.writeVInt(fields.Count);
			foreach (FieldMetaData field in fields)
			{
			  blockOut.writeVInt(field.fieldInfo.number);
			  blockOut.writeVLong(field.numTerms);
			  if (field.fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
			  {
				blockOut.writeVLong(field.sumTotalTermFreq);
			  }
			  blockOut.writeVLong(field.sumDocFreq);
			  blockOut.writeVInt(field.docCount);
			  blockOut.writeVInt(field.longsSize);
			  blockOut.writeVLong(field.statsOut.FilePointer);
			  blockOut.writeVLong(field.metaLongsOut.FilePointer);
			  blockOut.writeVLong(field.metaBytesOut.FilePointer);

			  field.skipOut.writeTo(blockOut);
			  field.statsOut.writeTo(blockOut);
			  field.metaLongsOut.writeTo(blockOut);
			  field.metaBytesOut.writeTo(blockOut);
			  field.dict.save(indexOut);
			}
			writeTrailer(blockOut, blockDirStart);
			CodecUtil.writeFooter(indexOut);
			CodecUtil.writeFooter(blockOut);
		  }
		  catch (IOException ioe2)
		  {
			ioe = ioe2;
		  }
		  finally
		  {
			IOUtils.closeWhileHandlingException(ioe, blockOut, indexOut, postingsWriter);
			blockOut = null;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void writeHeader(org.apache.lucene.store.IndexOutput out) throws java.io.IOException
	  private void writeHeader(IndexOutput @out)
	  {
		CodecUtil.writeHeader(@out, TERMS_CODEC_NAME, TERMS_VERSION_CURRENT);
	  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void writeTrailer(org.apache.lucene.store.IndexOutput out, long dirStart) throws java.io.IOException
	  private void writeTrailer(IndexOutput @out, long dirStart)
	  {
		@out.writeLong(dirStart);
	  }

	  private class FieldMetaData
	  {
		public FieldInfo fieldInfo;
		public long numTerms;
		public long sumTotalTermFreq;
		public long sumDocFreq;
		public int docCount;
		public int longsSize;
		public FST<long?> dict;

		// TODO: block encode each part 

		// vint encode next skip point (fully decoded when reading)
		public RAMOutputStream skipOut;
		// vint encode df, (ttf-df)
		public RAMOutputStream statsOut;
		// vint encode monotonic long[] and length for corresponding byte[]
		public RAMOutputStream metaLongsOut;
		// generic byte[]
		public RAMOutputStream metaBytesOut;
	  }

	  internal sealed class TermsWriter : TermsConsumer
	  {
		  private readonly FSTOrdTermsWriter outerInstance;

		internal readonly Builder<long?> builder;
		internal readonly PositiveIntOutputs outputs;
		internal readonly FieldInfo fieldInfo;
		internal readonly int longsSize;
		internal long numTerms;

		internal readonly IntsRef scratchTerm = new IntsRef();
		internal readonly RAMOutputStream statsOut = new RAMOutputStream();
		internal readonly RAMOutputStream metaLongsOut = new RAMOutputStream();
		internal readonly RAMOutputStream metaBytesOut = new RAMOutputStream();

		internal readonly RAMOutputStream skipOut = new RAMOutputStream();
		internal long lastBlockStatsFP;
		internal long lastBlockMetaLongsFP;
		internal long lastBlockMetaBytesFP;
		internal long[] lastBlockLongs;

		internal long[] lastLongs;
		internal long lastMetaBytesFP;

		internal TermsWriter(FSTOrdTermsWriter outerInstance, FieldInfo fieldInfo)
		{
			this.outerInstance = outerInstance;
		  this.numTerms = 0;
		  this.fieldInfo = fieldInfo;
		  this.longsSize = outerInstance.postingsWriter.setField(fieldInfo);
		  this.outputs = PositiveIntOutputs.Singleton;
		  this.builder = new Builder<>(FST.INPUT_TYPE.BYTE1, outputs);

		  this.lastBlockStatsFP = 0;
		  this.lastBlockMetaLongsFP = 0;
		  this.lastBlockMetaBytesFP = 0;
		  this.lastBlockLongs = new long[longsSize];

		  this.lastLongs = new long[longsSize];
		  this.lastMetaBytesFP = 0;
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.codecs.PostingsConsumer startTerm(org.apache.lucene.util.BytesRef text) throws java.io.IOException
		public override PostingsConsumer startTerm(BytesRef text)
		{
		  outerInstance.postingsWriter.startTerm();
		  return outerInstance.postingsWriter;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void finishTerm(org.apache.lucene.util.BytesRef text, org.apache.lucene.codecs.TermStats stats) throws java.io.IOException
		public override void finishTerm(BytesRef text, TermStats stats)
		{
		  if (numTerms > 0 && numTerms % SKIP_INTERVAL == 0)
		  {
			bufferSkip();
		  }
		  // write term meta data into fst
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long longs[] = new long[longsSize];
		  long[] longs = new long[longsSize];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long delta = stats.totalTermFreq - stats.docFreq;
		  long delta = stats.totalTermFreq - stats.docFreq;
		  if (stats.totalTermFreq > 0)
		  {
			if (delta == 0)
			{
			  statsOut.writeVInt(stats.docFreq << 1 | 1);
			}
			else
			{
			  statsOut.writeVInt(stats.docFreq << 1 | 0);
			  statsOut.writeVLong(stats.totalTermFreq - stats.docFreq);
			}
		  }
		  else
		  {
			statsOut.writeVInt(stats.docFreq);
		  }
		  BlockTermState state = outerInstance.postingsWriter.newTermState();
		  state.docFreq = stats.docFreq;
		  state.totalTermFreq = stats.totalTermFreq;
		  outerInstance.postingsWriter.finishTerm(state);
		  outerInstance.postingsWriter.encodeTerm(longs, metaBytesOut, fieldInfo, state, true);
		  for (int i = 0; i < longsSize; i++)
		  {
			metaLongsOut.writeVLong(longs[i] - lastLongs[i]);
			lastLongs[i] = longs[i];
		  }
		  metaLongsOut.writeVLong(metaBytesOut.FilePointer - lastMetaBytesFP);

		  builder.add(Util.toIntsRef(text, scratchTerm), numTerms);
		  numTerms++;

		  lastMetaBytesFP = metaBytesOut.FilePointer;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void finish(long sumTotalTermFreq, long sumDocFreq, int docCount) throws java.io.IOException
		public override void finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
		{
		  if (numTerms > 0)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FieldMetaData metadata = new FieldMetaData();
			FieldMetaData metadata = new FieldMetaData();
			metadata.fieldInfo = fieldInfo;
			metadata.numTerms = numTerms;
			metadata.sumTotalTermFreq = sumTotalTermFreq;
			metadata.sumDocFreq = sumDocFreq;
			metadata.docCount = docCount;
			metadata.longsSize = longsSize;
			metadata.skipOut = skipOut;
			metadata.statsOut = statsOut;
			metadata.metaLongsOut = metaLongsOut;
			metadata.metaBytesOut = metaBytesOut;
			metadata.dict = builder.finish();
			outerInstance.fields.Add(metadata);
		  }
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void bufferSkip() throws java.io.IOException
		internal void bufferSkip()
		{
		  skipOut.writeVLong(statsOut.FilePointer - lastBlockStatsFP);
		  skipOut.writeVLong(metaLongsOut.FilePointer - lastBlockMetaLongsFP);
		  skipOut.writeVLong(metaBytesOut.FilePointer - lastBlockMetaBytesFP);
		  for (int i = 0; i < longsSize; i++)
		  {
			skipOut.writeVLong(lastLongs[i] - lastBlockLongs[i]);
		  }
		  lastBlockStatsFP = statsOut.FilePointer;
		  lastBlockMetaLongsFP = metaLongsOut.FilePointer;
		  lastBlockMetaBytesFP = metaBytesOut.FilePointer;
		  Array.Copy(lastLongs, 0, lastBlockLongs, 0, longsSize);
		}
	  }
	}

}