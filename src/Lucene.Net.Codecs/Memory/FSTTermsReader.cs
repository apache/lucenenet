using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Memory
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


	using CorruptIndexException = index.CorruptIndexException;
	using DocsAndPositionsEnum = index.DocsAndPositionsEnum;
	using DocsEnum = index.DocsEnum;
	using IndexOptions = index.FieldInfo.IndexOptions;
	using FieldInfo = index.FieldInfo;
	using FieldInfos = index.FieldInfos;
	using IndexFileNames = index.IndexFileNames;
	using SegmentInfo = index.SegmentInfo;
	using SegmentReadState = index.SegmentReadState;
	using TermState = index.TermState;
	using Terms = index.Terms;
	using TermsEnum = index.TermsEnum;
	using ByteArrayDataInput = store.ByteArrayDataInput;
	using IndexInput = store.IndexInput;
	using ArrayUtil = util.ArrayUtil;
	using Bits = util.Bits;
	using BytesRef = util.BytesRef;
	using IOUtils = util.IOUtils;
	using RamUsageEstimator = util.RamUsageEstimator;
	using ByteRunAutomaton = util.automaton.ByteRunAutomaton;
	using CompiledAutomaton = util.automaton.CompiledAutomaton;
	using InputOutput = util.fst.BytesRefFSTEnum.InputOutput;
	using BytesRefFSTEnum = util.fst.BytesRefFSTEnum;
	using FST = util.fst.FST;
	using Outputs = util.fst.Outputs;
	using Util = util.fst.Util;

	/// <summary>
	/// FST-based terms dictionary reader.
	/// 
	/// The FST directly maps each term and its metadata, 
	/// it is memory resident.
	/// 
	/// @lucene.experimental
	/// </summary>

	public class FSTTermsReader : FieldsProducer
	{
	  internal readonly SortedDictionary<string, TermsReader> fields = new SortedDictionary<string, TermsReader>();
	  internal readonly PostingsReaderBase postingsReader;
	  //static boolean TEST = false;
	  internal readonly int version;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public FSTTermsReader(index.SegmentReadState state, codecs.PostingsReaderBase postingsReader) throws java.io.IOException
	  public FSTTermsReader(SegmentReadState state, PostingsReaderBase postingsReader)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String termsFileName = index.IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, FSTTermsWriter.TERMS_EXTENSION);
		string termsFileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, FSTTermsWriter.TERMS_EXTENSION);

		this.postingsReader = postingsReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final store.IndexInput in = state.directory.openInput(termsFileName, state.context);
		IndexInput @in = state.directory.openInput(termsFileName, state.context);

		bool success = false;
		try
		{
		  version = readHeader(@in);
		  if (version >= FSTTermsWriter.TERMS_VERSION_CHECKSUM)
		  {
			CodecUtil.checksumEntireFile(@in);
		  }
		  this.postingsReader.init(@in);
		  seekDir(@in);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final index.FieldInfos fieldInfos = state.fieldInfos;
		  FieldInfos fieldInfos = state.fieldInfos;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numFields = in.readVInt();
		  int numFields = @in.readVInt();
		  for (int i = 0; i < numFields; i++)
		  {
			int fieldNumber = @in.readVInt();
			FieldInfo fieldInfo = fieldInfos.fieldInfo(fieldNumber);
			long numTerms = @in.readVLong();
			long sumTotalTermFreq = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY ? - 1 : @in.readVLong();
			long sumDocFreq = @in.readVLong();
			int docCount = @in.readVInt();
			int longsSize = @in.readVInt();
			TermsReader current = new TermsReader(this, fieldInfo, @in, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize);
			TermsReader previous = fields[fieldInfo.name] = current;
			checkFieldSummary(state.segmentInfo, @in, current, previous);
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
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int readHeader(store.IndexInput in) throws java.io.IOException
	  private int readHeader(IndexInput @in)
	  {
		return CodecUtil.checkHeader(@in, FSTTermsWriter.TERMS_CODEC_NAME, FSTTermsWriter.TERMS_VERSION_START, FSTTermsWriter.TERMS_VERSION_CURRENT);
	  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void seekDir(store.IndexInput in) throws java.io.IOException
	  private void seekDir(IndexInput @in)
	  {
		if (version >= FSTTermsWriter.TERMS_VERSION_CHECKSUM)
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
//ORIGINAL LINE: private void checkFieldSummary(index.SegmentInfo info, store.IndexInput in, TermsReader field, TermsReader previous) throws java.io.IOException
	  private void checkFieldSummary(SegmentInfo info, IndexInput @in, TermsReader field, TermsReader previous)
	  {
		// #docs with field must be <= #docs
		if (field.docCount < 0 || field.docCount > info.DocCount)
		{
		  throw new CorruptIndexException("invalid docCount: " + field.docCount + " maxDoc: " + info.DocCount + " (resource=" + @in + ")");
		}
		// #postings must be >= #docs with field
		if (field.sumDocFreq < field.docCount)
		{
		  throw new CorruptIndexException("invalid sumDocFreq: " + field.sumDocFreq + " docCount: " + field.docCount + " (resource=" + @in + ")");
		}
		// #positions must be >= #postings
		if (field.sumTotalTermFreq != -1 && field.sumTotalTermFreq < field.sumDocFreq)
		{
		  throw new CorruptIndexException("invalid sumTotalTermFreq: " + field.sumTotalTermFreq + " sumDocFreq: " + field.sumDocFreq + " (resource=" + @in + ")");
		}
		if (previous != null)
		{
		  throw new CorruptIndexException("duplicate fields: " + field.fieldInfo.name + " (resource=" + @in + ")");
		}
	  }

	  public override IEnumerator<string> iterator()
	  {
		return Collections.unmodifiableSet(fields.Keys).GetEnumerator();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.Terms terms(String field) throws java.io.IOException
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
		  private readonly FSTTermsReader outerInstance;

		internal readonly FieldInfo fieldInfo;
		internal readonly long numTerms;
		internal readonly long sumTotalTermFreq;
		internal readonly long sumDocFreq;
		internal readonly int docCount;
		internal readonly int longsSize;
		internal readonly FST<FSTTermOutputs.TermData> dict;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: TermsReader(index.FieldInfo fieldInfo, store.IndexInput in, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize) throws java.io.IOException
		internal TermsReader(FSTTermsReader outerInstance, FieldInfo fieldInfo, IndexInput @in, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize)
		{
			this.outerInstance = outerInstance;
		  this.fieldInfo = fieldInfo;
		  this.numTerms = numTerms;
		  this.sumTotalTermFreq = sumTotalTermFreq;
		  this.sumDocFreq = sumDocFreq;
		  this.docCount = docCount;
		  this.longsSize = longsSize;
		  this.dict = new FST<>(@in, new FSTTermOutputs(fieldInfo, longsSize));
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
//ORIGINAL LINE: @Override public index.TermsEnum iterator(index.TermsEnum reuse) throws java.io.IOException
		public override TermsEnum iterator(TermsEnum reuse)
		{
		  return new SegmentTermsEnum(this);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.TermsEnum intersect(util.automaton.CompiledAutomaton compiled, util.BytesRef startTerm) throws java.io.IOException
		public override TermsEnum intersect(CompiledAutomaton compiled, BytesRef startTerm)
		{
		  return new IntersectTermsEnum(this, compiled, startTerm);
		}

		// Only wraps common operations for PBF interact
		internal abstract class BaseTermsEnum : TermsEnum
		{
			private readonly FSTTermsReader.TermsReader outerInstance;

		  /* Current term, null when enum ends or unpositioned */
		  internal BytesRef term_Renamed;

		  /* Current term stats + decoded metadata (customized by PBF) */
		  internal readonly BlockTermState state;

		  /* Current term stats + undecoded metadata (long[] & byte[]) */
		  internal FSTTermOutputs.TermData meta;
		  internal ByteArrayDataInput bytesReader;

		  /// <summary>
		  /// Decodes metadata into customized term state </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: abstract void decodeMetaData() throws java.io.IOException;
		  internal abstract void decodeMetaData();

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: BaseTermsEnum() throws java.io.IOException
		  internal BaseTermsEnum(FSTTermsReader.TermsReader outerInstance)
		  {
			  this.outerInstance = outerInstance;
			this.state = outerInstance.outerInstance.postingsReader.newTermState();
			this.bytesReader = new ByteArrayDataInput();
			this.term_Renamed = null;
			// NOTE: metadata will only be initialized in child class
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.TermState termState() throws java.io.IOException
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
//ORIGINAL LINE: @Override public index.DocsEnum docs(util.Bits liveDocs, index.DocsEnum reuse, int flags) throws java.io.IOException
		  public override DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)
		  {
			decodeMetaData();
			return outerInstance.outerInstance.postingsReader.docs(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public index.DocsAndPositionsEnum docsAndPositions(util.Bits liveDocs, index.DocsAndPositionsEnum reuse, int flags) throws java.io.IOException
		  public override DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		  {
			if (!outerInstance.hasPositions())
			{
			  return null;
			}
			decodeMetaData();
			return outerInstance.outerInstance.postingsReader.docsAndPositions(outerInstance.fieldInfo, state, liveDocs, reuse, flags);
		  }

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
			private readonly FSTTermsReader.TermsReader outerInstance;

		  internal readonly BytesRefFSTEnum<FSTTermOutputs.TermData> fstEnum;

		  /* True when current term's metadata is decoded */
		  internal bool decoded;

		  /* True when current enum is 'positioned' by seekExact(TermState) */
		  internal bool seekPending;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: SegmentTermsEnum() throws java.io.IOException
		  internal SegmentTermsEnum(FSTTermsReader.TermsReader outerInstance) : base(outerInstance)
		  {
			  this.outerInstance = outerInstance;
			this.fstEnum = new BytesRefFSTEnum<>(outerInstance.dict);
			this.decoded = false;
			this.seekPending = false;
			this.meta = null;
		  }

		  public override IComparer<BytesRef> Comparator
		  {
			  get
			  {
				return BytesRef.UTF8SortedAsUnicodeComparator;
			  }
		  }

		  // Let PBF decode metadata from long[] and byte[]
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override void decodeMetaData() throws java.io.IOException
		  internal override void decodeMetaData()
		  {
			if (!decoded && !seekPending)
			{
			  if (meta.BYTES != null)
			  {
				bytesReader.reset(meta.BYTES, 0, meta.BYTES.Length);
			  }
			  outerInstance.outerInstance.postingsReader.decodeTerm(meta.LONGS, bytesReader, outerInstance.fieldInfo, state, true);
			  decoded = true;
			}
		  }

		  // Update current enum according to FSTEnum
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: void updateEnum(final util.fst.BytesRefFSTEnum.InputOutput<FSTTermOutputs.TermData> pair)
		  internal void updateEnum(InputOutput<FSTTermOutputs.TermData> pair)
		  {
			if (pair == null)
			{
			  term_Renamed = null;
			}
			else
			{
			  term_Renamed = pair.input;
			  meta = pair.output;
			  state.docFreq = meta.DOC_FREQ;
			  state.totalTermFreq = meta.TOTAL_TERM_FREQ;
			}
			decoded = false;
			seekPending = false;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public util.BytesRef next() throws java.io.IOException
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
//ORIGINAL LINE: @Override public boolean seekExact(util.BytesRef target) throws java.io.IOException
		  public override bool seekExact(BytesRef target)
		  {
			updateEnum(fstEnum.seekExact(target));
			return term_Renamed != null;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public SeekStatus seekCeil(util.BytesRef target) throws java.io.IOException
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
			private readonly FSTTermsReader.TermsReader outerInstance;

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

		  /* to which level the metadata is accumulated 
		   * so that we can accumulate metadata lazily */
		  internal int metaUpto;

		  /* term dict fst */
		  internal readonly FST<FSTTermOutputs.TermData> fst;
		  internal readonly FST.BytesReader fstReader;
		  internal readonly Outputs<FSTTermOutputs.TermData> fstOutputs;

		  /* query automaton to intersect with */
		  internal readonly ByteRunAutomaton fsa;

		  private sealed class Frame
		  {
			  private readonly FSTTermsReader.TermsReader.IntersectTermsEnum outerInstance;

			/* fst stats */
			internal FST.Arc<FSTTermOutputs.TermData> fstArc;

			/* automaton stats */
			internal int fsaState;

			internal Frame(FSTTermsReader.TermsReader.IntersectTermsEnum outerInstance)
			{
				this.outerInstance = outerInstance;
			  this.fstArc = new FST.Arc<>();
			  this.fsaState = -1;
			}

			public override string ToString()
			{
			  return "arc=" + fstArc + " state=" + fsaState;
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: IntersectTermsEnum(util.automaton.CompiledAutomaton compiled, util.BytesRef startTerm) throws java.io.IOException
		  internal IntersectTermsEnum(FSTTermsReader.TermsReader outerInstance, CompiledAutomaton compiled, BytesRef startTerm) : base(outerInstance)
		  {
			  this.outerInstance = outerInstance;
			//if (TEST) System.out.println("Enum init, startTerm=" + startTerm);
			this.fst = outerInstance.dict;
			this.fstReader = fst.BytesReader;
			this.fstOutputs = outerInstance.dict.outputs;
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

			this.meta = null;
			this.metaUpto = 1;
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

		  public override IComparer<BytesRef> Comparator
		  {
			  get
			  {
				return BytesRef.UTF8SortedAsUnicodeComparator;
			  }
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override void decodeMetaData() throws java.io.IOException
		  internal override void decodeMetaData()
		  {
			Debug.Assert(term_Renamed != null);
			if (!decoded)
			{
			  if (meta.BYTES != null)
			  {
				bytesReader.reset(meta.BYTES, 0, meta.BYTES.Length);
			  }
			  outerInstance.outerInstance.postingsReader.decodeTerm(meta.LONGS, bytesReader, outerInstance.fieldInfo, state, true);
			  decoded = true;
			}
		  }

		  /// <summary>
		  /// Lazily accumulate meta data, when we got a accepted term </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void loadMetaData() throws java.io.IOException
		  internal void loadMetaData()
		  {
			FST.Arc<FSTTermOutputs.TermData> last, next;
			last = stack[metaUpto].fstArc;
			while (metaUpto != level)
			{
			  metaUpto++;
			  next = stack[metaUpto].fstArc;
			  next.output = fstOutputs.add(next.output, last.output);
			  last = next;
			}
			if (last.Final)
			{
			  meta = fstOutputs.add(last.output, last.nextFinalOutput);
			}
			else
			{
			  meta = last.output;
			}
			state.docFreq = meta.DOC_FREQ;
			state.totalTermFreq = meta.TOTAL_TERM_FREQ;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public SeekStatus seekCeil(util.BytesRef target) throws java.io.IOException
		  public override SeekStatus seekCeil(BytesRef target)
		  {
			decoded = false;
			term_Renamed = doSeekCeil(target);
			loadMetaData();
			if (term_Renamed == null)
			{
			  return SeekStatus.END;
			}
			else
			{
			  return term_Renamed.Equals(target) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public util.BytesRef next() throws java.io.IOException
		  public override BytesRef next()
		  {
			//if (TEST) System.out.println("Enum next()");
			if (pending)
			{
			  pending = false;
			  loadMetaData();
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
			loadMetaData();
			return term_Renamed;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private util.BytesRef doSeekCeil(util.BytesRef target) throws java.io.IOException
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
			  if (frame == null || frame.fstArc.label != label)
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
			frame.fstArc.output = fstOutputs.NoOutput;
			frame.fstArc.nextFinalOutput = fstOutputs.NoOutput;
			frame.fsaState = -1;
			return frame;
		  }

		  /// <summary>
		  /// Load frame for start arc(node) on fst </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: Frame loadFirstFrame(Frame frame) throws java.io.IOException
		  internal Frame loadFirstFrame(Frame frame)
		  {
			frame.fstArc = fst.getFirstArc(frame.fstArc);
			frame.fsaState = fsa.InitialState;
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
			frame.fstArc = fst.readFirstRealTargetArc(top.fstArc.target, frame.fstArc, fstReader);
			frame.fsaState = fsa.step(top.fsaState, frame.fstArc.label);
			//if (TEST) System.out.println(" loadExpand frame="+frame);
			if (frame.fsaState == -1)
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
			while (!frame.fstArc.Last)
			{
			  frame.fstArc = fst.readNextRealArc(frame.fstArc, fstReader);
			  frame.fsaState = fsa.step(top.fsaState, frame.fstArc.label);
			  if (frame.fsaState != -1)
			  {
				break;
			  }
			}
			//if (TEST) System.out.println(" loadNext frame="+frame);
			if (frame.fsaState == -1)
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
			FST.Arc<FSTTermOutputs.TermData> arc = frame.fstArc;
			arc = Util.readCeilArc(label, fst, top.fstArc, arc, fstReader);
			if (arc == null)
			{
			  return null;
			}
			frame.fsaState = fsa.step(top.fsaState, arc.label);
			//if (TEST) System.out.println(" loadCeil frame="+frame);
			if (frame.fsaState == -1)
			{
			  return loadNextFrame(top, frame);
			}
			return frame;
		  }

		  internal bool isAccept(Frame frame) // reach a term both fst&fsa accepts
		  {
			return fsa.isAccept(frame.fsaState) && frame.fstArc.Final;
		  }
		  internal bool isValid(Frame frame) // reach a prefix both fst&fsa won't reject
		  {
			return frame.fsaState != -1; //frame != null &&
		  }
		  internal bool canGrow(Frame frame) // can walk forward on both fst&fsa
		  {
			return frame.fsaState != -1 && FST.targetHasArcs(frame.fstArc);
		  }
		  internal bool canRewind(Frame frame) // can jump to sibling
		  {
			return !frame.fstArc.Last;
		  }

		  internal void pushFrame(Frame frame)
		  {
			term_Renamed = grow(frame.fstArc.label);
			level++;
			//if (TEST) System.out.println("  term=" + term + " level=" + level);
		  }

		  internal Frame popFrame()
		  {
			term_Renamed = shrink();
			level--;
			metaUpto = metaUpto > level ? level : metaUpto;
			//if (TEST) System.out.println("  term=" + term + " level=" + level);
			return stack[level + 1];
		  }

		  internal Frame newFrame()
		  {
			if (level + 1 == stack.Length)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Frame[] temp = new Frame[util.ArrayUtil.oversize(level+2, util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
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
//ORIGINAL LINE: static<T> void walk(util.fst.FST<T> fst) throws java.io.IOException
	  internal static void walk<T>(FST<T> fst)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.ArrayList<util.fst.FST.Arc<T>> queue = new java.util.ArrayList<>();
		List<FST.Arc<T>> queue = new List<FST.Arc<T>>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.BitSet seen = new java.util.BitSet();
		BitArray seen = new BitArray();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.fst.FST.BytesReader reader = fst.getBytesReader();
		FST.BytesReader reader = fst.BytesReader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.fst.FST.Arc<T> startArc = fst.getFirstArc(new util.fst.FST.Arc<T>());
		FST.Arc<T> startArc = fst.getFirstArc(new FST.Arc<T>());
		queue.Add(startArc);
		while (queue.Count > 0)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final util.fst.FST.Arc<T> arc = queue.remove(0);
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
		  ramBytesUsed += r.dict == null ? 0 : r.dict.sizeInBytes();
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