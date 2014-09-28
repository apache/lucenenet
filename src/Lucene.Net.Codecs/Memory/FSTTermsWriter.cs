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
	using Util = org.apache.lucene.util.fst.Util;

	/// <summary>
	/// FST-based term dict, using metadata as FST output.
	/// 
	/// The FST directly holds the mapping between &lt;term, metadata&gt;.
	/// 
	/// Term metadata consists of three parts:
	/// 1. term statistics: docFreq, totalTermFreq;
	/// 2. monotonic long[], e.g. the pointer to the postings list for that term;
	/// 3. generic byte[], e.g. other information need by postings reader.
	/// 
	/// <para>
	/// File:
	/// <ul>
	///   <li><tt>.tst</tt>: <a href="#Termdictionary">Term Dictionary</a></li>
	/// </ul>
	/// </para>
	/// <para>
	/// 
	/// <a name="Termdictionary" id="Termdictionary"></a>
	/// <h3>Term Dictionary</h3>
	/// </para>
	/// <para>
	///  The .tst contains a list of FSTs, one for each field.
	///  The FST maps a term to its corresponding statistics (e.g. docfreq) 
	///  and metadata (e.g. information for postings list reader like file pointer
	///  to postings list).
	/// </para>
	/// <para>
	///  Typically the metadata is separated into two parts:
	///  <ul>
	///   <li>
	///    Monotonical long array: Some metadata will always be ascending in order
	///    with the corresponding term. This part is used by FST to share outputs between arcs.
	///   </li>
	///   <li>
	///    Generic byte array: Used to store non-monotonic metadata.
	///   </li>
	///  </ul>
	/// </para>
	/// 
	/// File format:
	/// <ul>
	///  <li>TermsDict(.tst) --&gt; Header, <i>PostingsHeader</i>, FieldSummary, DirOffset</li>
	///  <li>FieldSummary --&gt; NumFields, &lt;FieldNumber, NumTerms, SumTotalTermFreq?, 
	///                                      SumDocFreq, DocCount, LongsSize, TermFST &gt;<sup>NumFields</sup></li>
	///  <li>TermFST --&gt; <seealso cref="FST FST&lt;TermData&gt;"/></li>
	///  <li>TermData --&gt; Flag, BytesSize?, LongDelta<sup>LongsSize</sup>?, Byte<sup>BytesSize</sup>?, 
	///                      &lt; DocFreq[Same?], (TotalTermFreq-DocFreq) &gt; ? </li>
	///  <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
	///  <li>DirOffset --&gt; <seealso cref="DataOutput#writeLong Uint64"/></li>
	///  <li>DocFreq, LongsSize, BytesSize, NumFields,
	///        FieldNumber, DocCount --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
	///  <li>TotalTermFreq, NumTerms, SumTotalTermFreq, SumDocFreq, LongDelta --&gt; 
	///        <seealso cref="DataOutput#writeVLong VLong"/></li>
	/// </ul>
	/// <para>Notes:</para>
	/// <ul>
	///  <li>
	///   The format of PostingsHeader and generic meta bytes are customized by the specific postings implementation:
	///   they contain arbitrary per-file data (such as parameters or versioning information), and per-term data
	///   (non-monotonic ones like pulsed postings data).
	///  </li>
	///  <li>
	///   The format of TermData is determined by FST, typically monotonic metadata will be dense around shallow arcs,
	///   while in deeper arcs only generic bytes and term statistics exist.
	///  </li>
	///  <li>
	///   The byte Flag is used to indicate which part of metadata exists on current arc. Specially the monotonic part
	///   is omitted when it is an array of 0s.
	///  </li>
	///  <li>
	///   Since LongsSize is per-field fixed, it is only written once in field summary.
	///  </li>
	/// </ul>
	/// 
	/// @lucene.experimental
	/// </summary>

	public class FSTTermsWriter : FieldsConsumer
	{
	  internal const string TERMS_EXTENSION = "tmp";
	  internal const string TERMS_CODEC_NAME = "FST_TERMS_DICT";
	  public const int TERMS_VERSION_START = 0;
	  public const int TERMS_VERSION_CHECKSUM = 1;
	  public const int TERMS_VERSION_CURRENT = TERMS_VERSION_CHECKSUM;

	  internal readonly PostingsWriterBase postingsWriter;
	  internal readonly FieldInfos fieldInfos;
	  internal IndexOutput @out;
	  internal readonly IList<FieldMetaData> fields = new List<FieldMetaData>();

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FSTTermsWriter(org.apache.lucene.index.SegmentWriteState state, org.apache.lucene.codecs.PostingsWriterBase postingsWriter) throws java.io.IOException
	  public FSTTermsWriter(SegmentWriteState state, PostingsWriterBase postingsWriter)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String termsFileName = org.apache.lucene.index.IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, TERMS_EXTENSION);
		string termsFileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, TERMS_EXTENSION);

		this.postingsWriter = postingsWriter;
		this.fieldInfos = state.fieldInfos;
		this.@out = state.directory.createOutput(termsFileName, state.context);

		bool success = false;
		try
		{
		  writeHeader(@out);
		  this.postingsWriter.init(@out);
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(@out);
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
		if (@out != null)
		{
		  IOException ioe = null;
		  try
		  {
			// write field summary
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long dirStart = out.getFilePointer();
			long dirStart = @out.FilePointer;

			@out.writeVInt(fields.Count);
			foreach (FieldMetaData field in fields)
			{
			  @out.writeVInt(field.fieldInfo.number);
			  @out.writeVLong(field.numTerms);
			  if (field.fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
			  {
				@out.writeVLong(field.sumTotalTermFreq);
			  }
			  @out.writeVLong(field.sumDocFreq);
			  @out.writeVInt(field.docCount);
			  @out.writeVInt(field.longsSize);
			  field.dict.save(@out);
			}
			writeTrailer(@out, dirStart);
			CodecUtil.writeFooter(@out);
		  }
		  catch (IOException ioe2)
		  {
			ioe = ioe2;
		  }
		  finally
		  {
			IOUtils.closeWhileHandlingException(ioe, @out, postingsWriter);
			@out = null;
		  }
		}
	  }

	  private class FieldMetaData
	  {
		public readonly FieldInfo fieldInfo;
		public readonly long numTerms;
		public readonly long sumTotalTermFreq;
		public readonly long sumDocFreq;
		public readonly int docCount;
		public readonly int longsSize;
		public readonly FST<FSTTermOutputs.TermData> dict;

		public FieldMetaData(FieldInfo fieldInfo, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize, FST<FSTTermOutputs.TermData> fst)
		{
		  this.fieldInfo = fieldInfo;
		  this.numTerms = numTerms;
		  this.sumTotalTermFreq = sumTotalTermFreq;
		  this.sumDocFreq = sumDocFreq;
		  this.docCount = docCount;
		  this.longsSize = longsSize;
		  this.dict = fst;
		}
	  }

	  internal sealed class TermsWriter : TermsConsumer
	  {
		  private readonly FSTTermsWriter outerInstance;

		internal readonly Builder<FSTTermOutputs.TermData> builder;
		internal readonly FSTTermOutputs outputs;
		internal readonly FieldInfo fieldInfo;
		internal readonly int longsSize;
		internal long numTerms;

		internal readonly IntsRef scratchTerm = new IntsRef();
		internal readonly RAMOutputStream statsWriter = new RAMOutputStream();
		internal readonly RAMOutputStream metaWriter = new RAMOutputStream();

		internal TermsWriter(FSTTermsWriter outerInstance, FieldInfo fieldInfo)
		{
			this.outerInstance = outerInstance;
		  this.numTerms = 0;
		  this.fieldInfo = fieldInfo;
		  this.longsSize = outerInstance.postingsWriter.setField(fieldInfo);
		  this.outputs = new FSTTermOutputs(fieldInfo, longsSize);
		  this.builder = new Builder<>(FST.INPUT_TYPE.BYTE1, outputs);
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
		  // write term meta data into fst
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.codecs.BlockTermState state = postingsWriter.newTermState();
		  BlockTermState state = outerInstance.postingsWriter.newTermState();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FSTTermOutputs.TermData meta = new FSTTermOutputs.TermData();
		  FSTTermOutputs.TermData meta = new FSTTermOutputs.TermData();
		  meta.longs = new long[longsSize];
		  meta.bytes = null;
		  meta.docFreq = state.docFreq = stats.docFreq;
		  meta.totalTermFreq = state.totalTermFreq = stats.totalTermFreq;
		  outerInstance.postingsWriter.finishTerm(state);
		  outerInstance.postingsWriter.encodeTerm(meta.longs, metaWriter, fieldInfo, state, true);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int bytesSize = (int)metaWriter.getFilePointer();
		  int bytesSize = (int)metaWriter.FilePointer;
		  if (bytesSize > 0)
		  {
			meta.bytes = new sbyte[bytesSize];
			metaWriter.writeTo(meta.bytes, 0);
			metaWriter.reset();
		  }
		  builder.add(Util.toIntsRef(text, scratchTerm), meta);
		  numTerms++;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void finish(long sumTotalTermFreq, long sumDocFreq, int docCount) throws java.io.IOException
		public override void finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
		{
		  // save FST dict
		  if (numTerms > 0)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST<FSTTermOutputs.TermData> fst = builder.finish();
			FST<FSTTermOutputs.TermData> fst = builder.finish();
			outerInstance.fields.Add(new FieldMetaData(fieldInfo, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize, fst));
		  }
		}
	  }
	}

}