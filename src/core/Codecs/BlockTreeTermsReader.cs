using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Codecs
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
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using DocsEnum = Lucene.Net.Index.DocsEnum;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using FieldInfos = Lucene.Net.Index.FieldInfos;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using SegmentInfo = Lucene.Net.Index.SegmentInfo;
	using TermState = Lucene.Net.Index.TermState;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using ArrayUtil = Lucene.Net.Util.ArrayUtil;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
	using StringHelper = Lucene.Net.Util.StringHelper;
	using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
	using RunAutomaton = Lucene.Net.Util.Automaton.RunAutomaton;
	using Transition = Lucene.Net.Util.Automaton.Transition;
	using ByteSequenceOutputs = Lucene.Net.Util.Fst.ByteSequenceOutputs;
	using Lucene.Net.Util.Fst;
	using Lucene.Net.Util.Fst;
	using Util = Lucene.Net.Util.Fst.Util;

	/// <summary>
	/// A block-based terms index and dictionary that assigns
	///  terms to variable length blocks according to how they
	///  share prefixes.  The terms index is a prefix trie
	///  whose leaves are term blocks.  The advantage of this
	///  approach is that seekExact is often able to
	///  determine a term cannot exist without doing any IO, and
	///  intersection with Automata is very fast.  Note that this
	///  terms dictionary has it's own fixed terms index (ie, it
	///  does not support a pluggable terms index
	///  implementation).
	/// 
	///  <p><b>NOTE</b>: this terms dictionary does not support
	///  index divisor when opening an IndexReader.  Instead, you
	///  can change the min/maxItemsPerBlock during indexing.</p>
	/// 
	///  <p>The data structure used by this implementation is very
	///  similar to a burst trie
	///  (http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.18.3499),
	///  but with added logic to break up too-large blocks of all
	///  terms sharing a given prefix into smaller ones.</p>
	/// 
	///  <p>Use <seealso cref="Lucene.Net.Index.CheckIndex"/> with the <code>-verbose</code>
	///  option to see summary statistics on the blocks in the
	///  dictionary.
	/// 
	///  See <seealso cref="BlockTreeTermsWriter"/>.
	/// 
	/// @lucene.experimental
	/// </summary>

	public class BlockTreeTermsReader : FieldsProducer
	{
		private bool InstanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			NO_OUTPUT = FstOutputs.NoOutput;
		}


	  // Open input to the main terms dict file (_X.tib)
	  private readonly IndexInput @in;

	  //private static final boolean DEBUG = BlockTreeTermsWriter.DEBUG;

	  // Reads the terms dict entries, to gather state to
	  // produce DocsEnum on demand
	  private readonly PostingsReaderBase PostingsReader;

	  private readonly SortedDictionary<string, FieldReader> Fields = new SortedDictionary<string, FieldReader>();

	  /// <summary>
	  /// File offset where the directory starts in the terms file. </summary>
	  private long DirOffset;

	  /// <summary>
	  /// File offset where the directory starts in the index file. </summary>
	  private long IndexDirOffset;

	  private string Segment;

	  private readonly int Version;

	  /// <summary>
	  /// Sole constructor. </summary>
	  public BlockTreeTermsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo info, PostingsReaderBase postingsReader, IOContext ioContext, string segmentSuffix, int indexDivisor)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }

		this.PostingsReader = postingsReader;

		this.Segment = info.Name;
		@in = dir.OpenInput(IndexFileNames.SegmentFileName(Segment, segmentSuffix, BlockTreeTermsWriter.TERMS_EXTENSION), ioContext);

		bool success = false;
		IndexInput indexIn = null;

		try
		{
		  Version = ReadHeader(@in);
		  if (indexDivisor != -1)
		  {
			indexIn = dir.OpenInput(IndexFileNames.SegmentFileName(Segment, segmentSuffix, BlockTreeTermsWriter.TERMS_INDEX_EXTENSION), ioContext);
			int indexVersion = ReadIndexHeader(indexIn);
			if (indexVersion != Version)
			{
			  throw new CorruptIndexException("mixmatched version files: " + @in + "=" + Version + "," + indexIn + "=" + indexVersion);
			}
		  }

		  // verify
		  if (indexIn != null && Version >= BlockTreeTermsWriter.VERSION_CHECKSUM)
		  {
			CodecUtil.ChecksumEntireFile(indexIn);
		  }

		  // Have PostingsReader init itself
		  postingsReader.Init(@in);

		  // Read per-field details
		  SeekDir(@in, DirOffset);
		  if (indexDivisor != -1)
		  {
			SeekDir(indexIn, IndexDirOffset);
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numFields = in.readVInt();
		  int numFields = @in.ReadVInt();
		  if (numFields < 0)
		  {
			throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + @in + ")");
		  }

		  for (int i = 0;i < numFields;i++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int field = in.readVInt();
			int field = @in.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long numTerms = in.readVLong();
			long numTerms = @in.ReadVLong();
			Debug.Assert(numTerms >= 0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numBytes = in.readVInt();
			int numBytes = @in.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef rootCode = new Lucene.Net.Util.BytesRef(new byte[numBytes]);
			BytesRef rootCode = new BytesRef(new sbyte[numBytes]);
			@in.ReadBytes(rootCode.Bytes, 0, numBytes);
			rootCode.Length = numBytes;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.FieldInfo fieldInfo = fieldInfos.fieldInfo(field);
			FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
			Debug.Assert(fieldInfo != null, "field=" + field);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long sumTotalTermFreq = fieldInfo.getIndexOptions() == Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_ONLY ? -1 : in.readVLong();
			long sumTotalTermFreq = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY ? - 1 : @in.ReadVLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long sumDocFreq = in.readVLong();
			long sumDocFreq = @in.ReadVLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int docCount = in.readVInt();
			int docCount = @in.ReadVInt();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int longsSize = version >= BlockTreeTermsWriter.VERSION_META_ARRAY ? in.readVInt() : 0;
			int longsSize = Version >= BlockTreeTermsWriter.VERSION_META_ARRAY ? @in.ReadVInt() : 0;
			if (docCount < 0 || docCount > info.DocCount) // #docs with field must be <= #docs
			{
			  throw new CorruptIndexException("invalid docCount: " + docCount + " maxDoc: " + info.DocCount + " (resource=" + @in + ")");
			}
			if (sumDocFreq < docCount) // #postings must be >= #docs with field
			{
			  throw new CorruptIndexException("invalid sumDocFreq: " + sumDocFreq + " docCount: " + docCount + " (resource=" + @in + ")");
			}
			if (sumTotalTermFreq != -1 && sumTotalTermFreq < sumDocFreq) // #positions must be >= #postings
			{
			  throw new CorruptIndexException("invalid sumTotalTermFreq: " + sumTotalTermFreq + " sumDocFreq: " + sumDocFreq + " (resource=" + @in + ")");
			}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long indexStartFP = indexDivisor != -1 ? indexIn.readVLong() : 0;
			long indexStartFP = indexDivisor != -1 ? indexIn.ReadVLong() : 0;
			FieldReader previous = Fields[fieldInfo.Name] = new FieldReader(this, fieldInfo, numTerms, rootCode, sumTotalTermFreq, sumDocFreq, docCount, indexStartFP, longsSize, indexIn);
			if (previous != null)
			{
			  throw new CorruptIndexException("duplicate field: " + fieldInfo.Name + " (resource=" + @in + ")");
			}
		  }
		  if (indexDivisor != -1)
		  {
			indexIn.Close();
		  }

		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			// this.close() will close in:
			IOUtils.CloseWhileHandlingException(indexIn, this);
		  }
		}
	  }

	  /// <summary>
	  /// Reads terms file header. </summary>
	  protected internal virtual int ReadHeader(IndexInput input)
	  {
		int version = CodecUtil.CheckHeader(input, BlockTreeTermsWriter.TERMS_CODEC_NAME, BlockTreeTermsWriter.VERSION_START, BlockTreeTermsWriter.VERSION_CURRENT);
		if (version < BlockTreeTermsWriter.VERSION_APPEND_ONLY)
		{
		  DirOffset = input.ReadLong();
		}
		return version;
	  }

	  /// <summary>
	  /// Reads index file header. </summary>
	  protected internal virtual int ReadIndexHeader(IndexInput input)
	  {
		int version = CodecUtil.CheckHeader(input, BlockTreeTermsWriter.TERMS_INDEX_CODEC_NAME, BlockTreeTermsWriter.VERSION_START, BlockTreeTermsWriter.VERSION_CURRENT);
		if (version < BlockTreeTermsWriter.VERSION_APPEND_ONLY)
		{
		  IndexDirOffset = input.ReadLong();
		}
		return version;
	  }

	  /// <summary>
	  /// Seek {@code input} to the directory offset. </summary>
	  protected internal virtual void SeekDir(IndexInput input, long dirOffset)
	  {
		if (Version >= BlockTreeTermsWriter.VERSION_CHECKSUM)
		{
		  input.Seek(input.Length() - CodecUtil.FooterLength() - 8);
		  dirOffset = input.ReadLong();
		}
		else if (Version >= BlockTreeTermsWriter.VERSION_APPEND_ONLY)
		{
		  input.Seek(input.Length() - 8);
		  dirOffset = input.ReadLong();
		}
		input.Seek(dirOffset);
	  }

	  // for debugging
	  // private static String toHex(int v) {
	  //   return "0x" + Integer.toHexString(v);
	  // }

	  public override void Close()
	  {
		try
		{
		  IOUtils.close(@in, PostingsReader);
		}
		finally
		{
		  // Clear so refs to terms index is GCable even if
		  // app hangs onto us:
		  Fields.Clear();
		}
	  }

	  public override IEnumerator<string> Iterator()
	  {
		return Collections.unmodifiableSet(Fields.Keys).GetEnumerator();
	  }

	  public override Terms Terms(string field)
	  {
		Debug.Assert(field != null);
		return Fields[field];
	  }

	  public override int Size()
	  {
		return Fields.Count;
	  }

	  // for debugging
	  internal virtual string BrToString(BytesRef b)
	  {
		if (b == null)
		{
		  return "null";
		}
		else
		{
		  try
		  {
			return b.Utf8ToString() + " " + b;
		  }
		  catch (Exception t)
		  {
			// If BytesRef isn't actually UTF8, or it's eg a
			// prefix of UTF8 that ends mid-unicode-char, we
			// fallback to hex:
			return b.ToString();
		  }
		}
	  }

	  /// <summary>
	  /// BlockTree statistics for a single field 
	  /// returned by <seealso cref="FieldReader#computeStats()"/>.
	  /// </summary>
	  public class Stats
	  {
		/// <summary>
		/// How many nodes in the index FST. </summary>
		public long IndexNodeCount;

		/// <summary>
		/// How many arcs in the index FST. </summary>
		public long IndexArcCount;

		/// <summary>
		/// Byte size of the index. </summary>
		public long IndexNumBytes;

		/// <summary>
		/// Total number of terms in the field. </summary>
		public long TotalTermCount;

		/// <summary>
		/// Total number of bytes (sum of term lengths) across all terms in the field. </summary>
		public long TotalTermBytes;

		/// <summary>
		/// The number of normal (non-floor) blocks in the terms file. </summary>
		public int NonFloorBlockCount;

		/// <summary>
		/// The number of floor blocks (meta-blocks larger than the
		///  allowed {@code maxItemsPerBlock}) in the terms file. 
		/// </summary>
		public int FloorBlockCount;

		/// <summary>
		/// The number of sub-blocks within the floor blocks. </summary>
		public int FloorSubBlockCount;

		/// <summary>
		/// The number of "internal" blocks (that have both
		///  terms and sub-blocks). 
		/// </summary>
		public int MixedBlockCount;

		/// <summary>
		/// The number of "leaf" blocks (blocks that have only
		///  terms). 
		/// </summary>
		public int TermsOnlyBlockCount;

		/// <summary>
		/// The number of "internal" blocks that do not contain
		///  terms (have only sub-blocks). 
		/// </summary>
		public int SubBlocksOnlyBlockCount;

		/// <summary>
		/// Total number of blocks. </summary>
		public int TotalBlockCount;

		/// <summary>
		/// Number of blocks at each prefix depth. </summary>
		public int[] BlockCountByPrefixLen = new int[10];
		internal int StartBlockCount;
		internal int EndBlockCount;

		/// <summary>
		/// Total number of bytes used to store term suffixes. </summary>
		public long TotalBlockSuffixBytes;

		/// <summary>
		/// Total number of bytes used to store term stats (not
		///  including what the <seealso cref="PostingsBaseFormat"/>
		///  stores. 
		/// </summary>
		public long TotalBlockStatsBytes;

		/// <summary>
		/// Total bytes stored by the <seealso cref="PostingsBaseFormat"/>,
		///  plus the other few vInts stored in the frame. 
		/// </summary>
		public long TotalBlockOtherBytes;

		/// <summary>
		/// Segment name. </summary>
		public readonly string Segment;

		/// <summary>
		/// Field name. </summary>
		public readonly string Field;

		internal Stats(string segment, string field)
		{
		  this.Segment = segment;
		  this.Field = field;
		}

		internal virtual void StartBlock(FieldReader.SegmentTermsEnum.Frame frame, bool isFloor)
		{
		  TotalBlockCount++;
		  if (isFloor)
		  {
			if (frame.Fp == frame.FpOrig)
			{
			  FloorBlockCount++;
			}
			FloorSubBlockCount++;
		  }
		  else
		  {
			NonFloorBlockCount++;
		  }

		  if (BlockCountByPrefixLen.Length <= frame.Prefix)
		  {
			BlockCountByPrefixLen = ArrayUtil.Grow(BlockCountByPrefixLen, 1 + frame.Prefix);
		  }
		  BlockCountByPrefixLen[frame.Prefix]++;
		  StartBlockCount++;
		  TotalBlockSuffixBytes += frame.SuffixesReader.length();
		  TotalBlockStatsBytes += frame.StatsReader.length();
		}

		internal virtual void EndBlock(FieldReader.SegmentTermsEnum.Frame frame)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termCount = frame.isLeafBlock ? frame.entCount : frame.state.termBlockOrd;
		  int termCount = frame.IsLeafBlock ? frame.EntCount : frame.State.termBlockOrd;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int subBlockCount = frame.entCount - termCount;
		  int subBlockCount = frame.EntCount - termCount;
		  TotalTermCount += termCount;
		  if (termCount != 0 && subBlockCount != 0)
		  {
			MixedBlockCount++;
		  }
		  else if (termCount != 0)
		  {
			TermsOnlyBlockCount++;
		  }
		  else if (subBlockCount != 0)
		  {
			SubBlocksOnlyBlockCount++;
		  }
		  else
		  {
			throw new IllegalStateException();
		  }
		  EndBlockCount++;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long otherBytes = frame.fpEnd - frame.fp - frame.suffixesReader.length() - frame.statsReader.length();
		  long otherBytes = frame.FpEnd - frame.Fp - frame.SuffixesReader.length() - frame.StatsReader.length();
		  Debug.Assert(otherBytes > 0, "otherBytes=" + otherBytes + " frame.fp=" + frame.Fp + " frame.fpEnd=" + frame.FpEnd);
		  TotalBlockOtherBytes += otherBytes;
		}

		internal virtual void Term(BytesRef term)
		{
		  TotalTermBytes += term.Length;
		}

		internal virtual void Finish()
		{
		  Debug.Assert(StartBlockCount == EndBlockCount, "startBlockCount=" + StartBlockCount + " endBlockCount=" + EndBlockCount);
		  Debug.Assert(TotalBlockCount == FloorSubBlockCount + NonFloorBlockCount, "floorSubBlockCount=" + FloorSubBlockCount + " nonFloorBlockCount=" + NonFloorBlockCount + " totalBlockCount=" + TotalBlockCount);
		  Debug.Assert(TotalBlockCount == MixedBlockCount + TermsOnlyBlockCount + SubBlocksOnlyBlockCount, "totalBlockCount=" + TotalBlockCount + " mixedBlockCount=" + MixedBlockCount + " subBlocksOnlyBlockCount=" + SubBlocksOnlyBlockCount + " termsOnlyBlockCount=" + TermsOnlyBlockCount);
		}

		public override string ToString()
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream(1024);
		  ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
		  PrintStream @out;
		  try
		  {
			@out = new PrintStream(bos, false, IOUtils.UTF_8);
		  }
		  catch (UnsupportedEncodingException bogus)
		  {
			throw new Exception(bogus);
		  }

		  @out.println("  index FST:");
		  @out.println("    " + IndexNodeCount + " nodes");
		  @out.println("    " + IndexArcCount + " arcs");
		  @out.println("    " + IndexNumBytes + " bytes");
		  @out.println("  terms:");
		  @out.println("    " + TotalTermCount + " terms");
		  @out.println("    " + TotalTermBytes + " bytes" + (TotalTermCount != 0 ? " (" + string.format(Locale.ROOT, "%.1f", ((double) TotalTermBytes) / TotalTermCount) + " bytes/term)" : ""));
		  @out.println("  blocks:");
		  @out.println("    " + TotalBlockCount + " blocks");
		  @out.println("    " + TermsOnlyBlockCount + " terms-only blocks");
		  @out.println("    " + SubBlocksOnlyBlockCount + " sub-block-only blocks");
		  @out.println("    " + MixedBlockCount + " mixed blocks");
		  @out.println("    " + FloorBlockCount + " floor blocks");
		  @out.println("    " + (TotalBlockCount - FloorSubBlockCount) + " non-floor blocks");
		  @out.println("    " + FloorSubBlockCount + " floor sub-blocks");
		  @out.println("    " + TotalBlockSuffixBytes + " term suffix bytes" + (TotalBlockCount != 0 ? " (" + string.format(Locale.ROOT, "%.1f", ((double) TotalBlockSuffixBytes) / TotalBlockCount) + " suffix-bytes/block)" : ""));
		  @out.println("    " + TotalBlockStatsBytes + " term stats bytes" + (TotalBlockCount != 0 ? " (" + string.format(Locale.ROOT, "%.1f", ((double) TotalBlockStatsBytes) / TotalBlockCount) + " stats-bytes/block)" : ""));
		  @out.println("    " + TotalBlockOtherBytes + " other bytes" + (TotalBlockCount != 0 ? " (" + string.format(Locale.ROOT, "%.1f", ((double) TotalBlockOtherBytes) / TotalBlockCount) + " other-bytes/block)" : ""));
		  if (TotalBlockCount != 0)
		  {
			@out.println("    by prefix length:");
			int total = 0;
			for (int prefix = 0;prefix < BlockCountByPrefixLen.Length;prefix++)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int blockCount = blockCountByPrefixLen[prefix];
			  int blockCount = BlockCountByPrefixLen[prefix];
			  total += blockCount;
			  if (blockCount != 0)
			  {
				@out.println("      " + string.format(Locale.ROOT, "%2d", prefix) + ": " + blockCount);
			  }
			}
			Debug.Assert(TotalBlockCount == total);
		  }

		  try
		  {
			return bos.ToString(IOUtils.UTF_8);
		  }
		  catch (UnsupportedEncodingException bogus)
		  {
			throw new Exception(bogus);
		  }
		}
	  }

	  internal readonly Outputs<BytesRef> FstOutputs = ByteSequenceOutputs.Singleton;
	  internal BytesRef NO_OUTPUT;

	  /// <summary>
	  /// BlockTree's implementation of <seealso cref="Terms"/>. </summary>
	  public sealed class FieldReader : Terms
	  {
		  private readonly BlockTreeTermsReader OuterInstance;

		internal readonly long NumTerms;
		internal readonly FieldInfo FieldInfo;
		internal readonly long SumTotalTermFreq_Renamed;
		internal readonly long SumDocFreq_Renamed;
		internal readonly int DocCount_Renamed;
		internal readonly long IndexStartFP;
		internal readonly long RootBlockFP;
		internal readonly BytesRef RootCode;
		internal readonly int LongsSize;

		internal readonly FST<BytesRef> Index;
		//private boolean DEBUG;

		internal FieldReader(BlockTreeTermsReader outerInstance, FieldInfo fieldInfo, long numTerms, BytesRef rootCode, long sumTotalTermFreq, long sumDocFreq, int docCount, long indexStartFP, int longsSize, IndexInput indexIn)
		{
			this.OuterInstance = outerInstance;
		  Debug.Assert(numTerms > 0);
		  this.FieldInfo = fieldInfo;
		  //DEBUG = BlockTreeTermsReader.DEBUG && fieldInfo.name.equals("id");
		  this.NumTerms = numTerms;
		  this.SumTotalTermFreq_Renamed = sumTotalTermFreq;
		  this.SumDocFreq_Renamed = sumDocFreq;
		  this.DocCount_Renamed = docCount;
		  this.IndexStartFP = indexStartFP;
		  this.RootCode = rootCode;
		  this.LongsSize = longsSize;
		  // if (DEBUG) {
		  //   System.out.println("BTTR: seg=" + segment + " field=" + fieldInfo.name + " rootBlockCode=" + rootCode + " divisor=" + indexDivisor);
		  // }

		  RootBlockFP = (int)((uint)(new ByteArrayDataInput(rootCode.Bytes, rootCode.Offset, rootCode.Length)).ReadVLong() >> BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS);

		  if (indexIn != null)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Store.IndexInput clone = indexIn.clone();
			IndexInput clone = indexIn.Clone();
			//System.out.println("start=" + indexStartFP + " field=" + fieldInfo.name);
			clone.Seek(indexStartFP);
			Index = new FST<>(clone, ByteSequenceOutputs.Singleton);

			/*
			if (false) {
			  final String dotFileName = segment + "_" + fieldInfo.name + ".dot";
			  Writer w = new OutputStreamWriter(new FileOutputStream(dotFileName));
			  Util.toDot(index, w, false, false);
			  System.out.println("FST INDEX: SAVED to " + dotFileName);
			  w.close();
			}
			*/
		  }
		  else
		  {
			Index = null;
		  }
		}

		/// <summary>
		/// For debugging -- used by CheckIndex too </summary>
		// TODO: maybe push this into Terms?
		public Stats ComputeStats()
		{
		  return (new SegmentTermsEnum(this)).ComputeBlockStats();
		}

		public override IComparer<BytesRef> Comparator
		{
			get
			{
			  return BytesRef.UTF8SortedAsUnicodeComparator;
			}
		}

		public override bool HasFreqs()
		{
		  return FieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS) >= 0;
		}

		public override bool HasOffsets()
		{
		  return FieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		}

		public override bool HasPositions()
		{
		  return FieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
		}

		public override bool HasPayloads()
		{
		  return FieldInfo.HasPayloads();
		}

		public override TermsEnum Iterator(TermsEnum reuse)
		{
		  return new SegmentTermsEnum(this);
		}

		public override long Size()
		{
		  return NumTerms;
		}

		public override long SumTotalTermFreq
		{
			get
			{
			  return SumTotalTermFreq_Renamed;
			}
		}

		public override long SumDocFreq
		{
			get
			{
			  return SumDocFreq_Renamed;
			}
		}

		public override int DocCount
		{
			get
			{
			  return DocCount_Renamed;
			}
		}

		public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
		{
		  if (compiled.Type != CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
		  {
			throw new System.ArgumentException("please use CompiledAutomaton.getTermsEnum instead");
		  }
		  return new IntersectEnum(this, compiled, startTerm);
		}

		/// <summary>
		/// Returns approximate RAM bytes used </summary>
		public long RamBytesUsed()
		{
		  return ((Index != null)? Index.SizeInBytes() : 0);
		}

		// NOTE: cannot seek!
		private sealed class IntersectEnum : TermsEnum
		{
			private readonly BlockTreeTermsReader.FieldReader OuterInstance;

		  internal readonly IndexInput @in;

		  internal Frame[] Stack;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) private Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef>[] arcs = new Lucene.Net.Util.Fst.FST.Arc[5];
		  internal FST.Arc<BytesRef>[] Arcs = new FST.Arc[5];

		  internal readonly RunAutomaton RunAutomaton;
		  internal readonly CompiledAutomaton CompiledAutomaton;

		  internal Frame CurrentFrame;

		  internal readonly BytesRef Term_Renamed = new BytesRef();

		  internal readonly FST.BytesReader FstReader;

		  // TODO: can we share this with the frame in STE?
		  private sealed class Frame
		  {
			  private readonly BlockTreeTermsReader.FieldReader.IntersectEnum OuterInstance;

			internal readonly int Ord;
			internal long Fp;
			internal long FpOrig;
			internal long FpEnd;
			internal long LastSubFP;

			// State in automaton
			internal int State_Renamed;

			internal int MetaDataUpto;

			internal sbyte[] SuffixBytes = new sbyte[128];
			internal readonly ByteArrayDataInput SuffixesReader = new ByteArrayDataInput();

			internal sbyte[] StatBytes = new sbyte[64];
			internal readonly ByteArrayDataInput StatsReader = new ByteArrayDataInput();

			internal sbyte[] FloorData = new sbyte[32];
			internal readonly ByteArrayDataInput FloorDataReader = new ByteArrayDataInput();

			// Length of prefix shared by all terms in this block
			internal int Prefix;

			// Number of entries (term or sub-block) in this block
			internal int EntCount;

			// Which term we will next read
			internal int NextEnt;

			// True if this block is either not a floor block,
			// or, it's the last sub-block of a floor block
			internal bool IsLastInFloor;

			// True if all entries are terms
			internal bool IsLeafBlock;

			internal int NumFollowFloorBlocks;
			internal int NextFloorLabel;

			internal Transition[] Transitions;
			internal int CurTransitionMax;
			internal int TransitionIndex;

			internal FST.Arc<BytesRef> Arc;

			internal readonly BlockTermState TermState;

			// metadata buffer, holding monotonic values
			public long[] Longs;
			// metadata buffer, holding general values
			public sbyte[] Bytes;
			internal ByteArrayDataInput BytesReader;

			// Cumulative output so far
			internal BytesRef OutputPrefix;

			internal int StartBytePos;
			internal int Suffix;

			public Frame(BlockTreeTermsReader.FieldReader.IntersectEnum outerInstance, int ord)
			{
				this.OuterInstance = outerInstance;
			  this.Ord = ord;
			  this.TermState = outerInstance.OuterInstance.OuterInstance.PostingsReader.newTermState();
			  this.TermState.TotalTermFreq = -1;
			  this.Longs = new long[outerInstance.OuterInstance.LongsSize];
			}

			internal void LoadNextFloorBlock()
			{
			  Debug.Assert(NumFollowFloorBlocks > 0);
			  //if (DEBUG) System.out.println("    loadNextFoorBlock trans=" + transitions[transitionIndex]);

			  do
			  {
				Fp = FpOrig + ((int)((uint)FloorDataReader.ReadVLong() >> 1));
				NumFollowFloorBlocks--;
				// if (DEBUG) System.out.println("    skip floor block2!  nextFloorLabel=" + (char) nextFloorLabel + " vs target=" + (char) transitions[transitionIndex].getMin() + " newFP=" + fp + " numFollowFloorBlocks=" + numFollowFloorBlocks);
				if (NumFollowFloorBlocks != 0)
				{
				  NextFloorLabel = FloorDataReader.ReadByte() & 0xff;
				}
				else
				{
				  NextFloorLabel = 256;
				}
				// if (DEBUG) System.out.println("    nextFloorLabel=" + (char) nextFloorLabel);
			  } while (NumFollowFloorBlocks != 0 && NextFloorLabel <= Transitions[TransitionIndex].Min);

			  Load(Lucene.Net.Util.BytesRefIterator_Fields.Null);
			}

			public int State
			{
				set
				{
				  this.State_Renamed = value;
				  TransitionIndex = 0;
				  Transitions = outerInstance.CompiledAutomaton.SortedTransitions[value];
				  if (Transitions.Length != 0)
				  {
					CurTransitionMax = Transitions[0].Max;
				  }
				  else
				  {
					CurTransitionMax = -1;
				  }
				}
				get
				{
				int state = CurrentFrame.State_Renamed;
				for (int idx = 0;idx < CurrentFrame.Suffix;idx++)
				{
				  state = RunAutomaton.Step(state, CurrentFrame.SuffixBytes[CurrentFrame.StartBytePos + idx] & 0xff);
				  Debug.Assert(state != -1);
				}
				return state;
				}
			}

			internal void Load(BytesRef frameIndexData)
			{

			  // if (DEBUG) System.out.println("    load fp=" + fp + " fpOrig=" + fpOrig + " frameIndexData=" + frameIndexData + " trans=" + (transitions.length != 0 ? transitions[0] : "n/a" + " state=" + state));

			  if (frameIndexData != Lucene.Net.Util.BytesRefIterator_Fields.Null && Transitions.Length != 0)
			  {
				// Floor frame
				if (FloorData.Length < frameIndexData.Length)
				{
				  this.FloorData = new sbyte[ArrayUtil.Oversize(frameIndexData.Length, 1)];
				}
				Array.Copy(frameIndexData.Bytes, frameIndexData.Offset, FloorData, 0, frameIndexData.Length);
				FloorDataReader.Reset(FloorData, 0, frameIndexData.Length);
				// Skip first long -- has redundant fp, hasTerms
				// flag, isFloor flag
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long code = floorDataReader.readVLong();
				long code = FloorDataReader.ReadVLong();
				if ((code & BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR) != 0)
				{
				  NumFollowFloorBlocks = FloorDataReader.ReadVInt();
				  NextFloorLabel = FloorDataReader.ReadByte() & 0xff;
				  // if (DEBUG) System.out.println("    numFollowFloorBlocks=" + numFollowFloorBlocks + " nextFloorLabel=" + nextFloorLabel);

				  // If current state is accept, we must process
				  // first block in case it has empty suffix:
				  if (!outerInstance.RunAutomaton.IsAccept(State_Renamed))
				  {
					// Maybe skip floor blocks:
					while (NumFollowFloorBlocks != 0 && NextFloorLabel <= Transitions[0].Min)
					{
					  Fp = FpOrig + ((int)((uint)FloorDataReader.ReadVLong() >> 1));
					  NumFollowFloorBlocks--;
					  // if (DEBUG) System.out.println("    skip floor block!  nextFloorLabel=" + (char) nextFloorLabel + " vs target=" + (char) transitions[0].getMin() + " newFP=" + fp + " numFollowFloorBlocks=" + numFollowFloorBlocks);
					  if (NumFollowFloorBlocks != 0)
					  {
						NextFloorLabel = FloorDataReader.ReadByte() & 0xff;
					  }
					  else
					  {
						NextFloorLabel = 256;
					  }
					}
				  }
				}
			  }

			  outerInstance.@in.Seek(Fp);
			  int code = outerInstance.@in.ReadVInt();
			  EntCount = (int)((uint)code >> 1);
			  Debug.Assert(EntCount > 0);
			  IsLastInFloor = (code & 1) != 0;

			  // term suffixes:
			  code = outerInstance.@in.ReadVInt();
			  IsLeafBlock = (code & 1) != 0;
			  int numBytes = (int)((uint)code >> 1);
			  // if (DEBUG) System.out.println("      entCount=" + entCount + " lastInFloor?=" + isLastInFloor + " leafBlock?=" + isLeafBlock + " numSuffixBytes=" + numBytes);
			  if (SuffixBytes.Length < numBytes)
			  {
				SuffixBytes = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
			  }
			  outerInstance.@in.ReadBytes(SuffixBytes, 0, numBytes);
			  SuffixesReader.Reset(SuffixBytes, 0, numBytes);

			  // stats
			  numBytes = outerInstance.@in.ReadVInt();
			  if (StatBytes.Length < numBytes)
			  {
				StatBytes = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
			  }
			  outerInstance.@in.ReadBytes(StatBytes, 0, numBytes);
			  StatsReader.Reset(StatBytes, 0, numBytes);
			  MetaDataUpto = 0;

			  TermState.TermBlockOrd = 0;
			  NextEnt = 0;

			  // metadata
			  numBytes = outerInstance.@in.ReadVInt();
			  if (Bytes == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			  {
				Bytes = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
				BytesReader = new ByteArrayDataInput();
			  }
			  else if (Bytes.Length < numBytes)
			  {
				Bytes = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
			  }
			  outerInstance.@in.ReadBytes(Bytes, 0, numBytes);
			  BytesReader.Reset(Bytes, 0, numBytes);

			  if (!IsLastInFloor)
			  {
				// Sub-blocks of a single floor block are always
				// written one after another -- tail recurse:
				FpEnd = outerInstance.@in.FilePointer;
			  }
			}

			// TODO: maybe add scanToLabel; should give perf boost

			public bool Next()
			{
			  return IsLeafBlock ? NextLeaf() : NextNonLeaf();
			}

			// Decodes next entry; returns true if it's a sub-block
			public bool NextLeaf()
			{
			  //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
			  Debug.Assert(NextEnt != -1 && NextEnt < EntCount, "nextEnt=" + NextEnt + " entCount=" + EntCount + " fp=" + Fp);
			  NextEnt++;
			  Suffix = SuffixesReader.ReadVInt();
			  StartBytePos = SuffixesReader.Position;
			  SuffixesReader.SkipBytes(Suffix);
			  return false;
			}

			public bool NextNonLeaf()
			{
			  //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
			  Debug.Assert(NextEnt != -1 && NextEnt < EntCount, "nextEnt=" + NextEnt + " entCount=" + EntCount + " fp=" + Fp);
			  NextEnt++;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = suffixesReader.readVInt();
			  int code = SuffixesReader.ReadVInt();
			  Suffix = (int)((uint)code >> 1);
			  StartBytePos = SuffixesReader.Position;
			  SuffixesReader.SkipBytes(Suffix);
			  if ((code & 1) == 0)
			  {
				// A normal term
				TermState.TermBlockOrd++;
				return false;
			  }
			  else
			  {
				// A sub-block; make sub-FP absolute:
				LastSubFP = Fp - SuffixesReader.ReadVLong();
				return true;
			  }
			}

			public int TermBlockOrd
			{
				get
				{
				  return IsLeafBlock ? NextEnt : TermState.TermBlockOrd;
				}
			}

			public void DecodeMetaData()
			{

			  // lazily catch up on metadata decode:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int limit = getTermBlockOrd();
			  int limit = TermBlockOrd;
			  bool absolute = MetaDataUpto == 0;
			  Debug.Assert(limit > 0);

			  // TODO: better API would be "jump straight to term=N"???
			  while (MetaDataUpto < limit)
			  {

				// TODO: we could make "tiers" of metadata, ie,
				// decode docFreq/totalTF but don't decode postings
				// metadata; this way caller could get
				// docFreq/totalTF w/o paying decode cost for
				// postings

				// TODO: if docFreq were bulk decoded we could
				// just skipN here:

				// stats
				TermState.DocFreq = StatsReader.ReadVInt();
				//if (DEBUG) System.out.println("    dF=" + state.docFreq);
				if (outerInstance.OuterInstance.FieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
				{
				  TermState.TotalTermFreq = TermState.DocFreq + StatsReader.ReadVLong();
				  //if (DEBUG) System.out.println("    totTF=" + state.totalTermFreq);
				}
				// metadata 
				for (int i = 0; i < outerInstance.OuterInstance.LongsSize; i++)
				{
				  Longs[i] = BytesReader.ReadVLong();
				}
				outerInstance.OuterInstance.OuterInstance.PostingsReader.decodeTerm(Longs, BytesReader, outerInstance.OuterInstance.FieldInfo, TermState, absolute);

				MetaDataUpto++;
				absolute = false;
			  }
			  TermState.TermBlockOrd = MetaDataUpto;
			}
		  }

		  internal BytesRef SavedStartTerm_Renamed;

		  // TODO: in some cases we can filter by length?  eg
		  // regexp foo*bar must be at least length 6 bytes
		  public IntersectEnum(BlockTreeTermsReader.FieldReader outerInstance, CompiledAutomaton compiled, BytesRef startTerm)
		  {
			  this.OuterInstance = outerInstance;
			// if (DEBUG) {
			//   System.out.println("\nintEnum.init seg=" + segment + " commonSuffix=" + brToString(compiled.commonSuffixRef));
			// }
			RunAutomaton = compiled.RunAutomaton;
			CompiledAutomaton = compiled;
			@in = outerInstance.OuterInstance.@in.Clone();
			Stack = new Frame[5];
			for (int idx = 0;idx < Stack.Length;idx++)
			{
			  Stack[idx] = new Frame(this, idx);
			}
			for (int arcIdx = 0;arcIdx < Arcs.Length;arcIdx++)
			{
			  Arcs[arcIdx] = new FST.Arc<>();
			}

			if (outerInstance.Index == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  FstReader = Lucene.Net.Util.BytesRefIterator_Fields.Null;
			}
			else
			{
			  FstReader = outerInstance.Index.BytesReader;
			}

			// TODO: if the automaton is "smallish" we really
			// should use the terms index to seek at least to
			// the initial term and likely to subsequent terms
			// (or, maybe just fallback to ATE for such cases).
			// Else the seek cost of loading the frames will be
			// too costly.

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef> arc = index.getFirstArc(arcs[0]);
			FST.Arc<BytesRef> arc = outerInstance.Index.getFirstArc(Arcs[0]);
			// Empty string prefix must have an output in the index!
			Debug.Assert(arc.Final);

			// Special pushFrame since it's the first one:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Frame f = stack[0];
			Frame f = Stack[0];
			f.Fp = f.FpOrig = outerInstance.RootBlockFP;
			f.Prefix = 0;
			f.State = RunAutomaton.InitialState;
			f.Arc = arc;
			f.OutputPrefix = arc.Output;
			f.Load(outerInstance.RootCode);

			// for assert:
			Debug.Assert(setSavedStartTerm(startTerm));

			CurrentFrame = f;
			if (startTerm != Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  SeekToStartTerm(startTerm);
			}
		  }

		  // only for assert:
		  internal bool SetSavedStartTerm(BytesRef startTerm)
		  {
			SavedStartTerm_Renamed = startTerm == Lucene.Net.Util.BytesRefIterator_Fields.Null ? Lucene.Net.Util.BytesRefIterator_Fields.Null : BytesRef.DeepCopyOf(startTerm);
			return true;
		  }

		  public override TermState TermState()
		  {
			CurrentFrame.DecodeMetaData();
			return CurrentFrame.TermState.clone();
		  }

		  internal Frame GetFrame(int ord)
		  {
			if (ord >= Stack.Length)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Frame[] next = new Frame[Lucene.Net.Util.ArrayUtil.oversize(1+ord, Lucene.Net.Util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			  Frame[] next = new Frame[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			  Array.Copy(Stack, 0, next, 0, Stack.Length);
			  for (int stackOrd = Stack.Length;stackOrd < next.Length;stackOrd++)
			  {
				next[stackOrd] = new Frame(this, stackOrd);
			  }
			  Stack = next;
			}
			Debug.Assert(Stack[ord].Ord == ord);
			return Stack[ord];
		  }

		  internal FST.Arc<BytesRef> GetArc(int ord)
		  {
			if (ord >= Arcs.Length)
			{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) final Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef>[] next = new Lucene.Net.Util.Fst.FST.Arc[Lucene.Net.Util.ArrayUtil.oversize(1+ord, Lucene.Net.Util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
			  FST.Arc<BytesRef>[] next = new FST.Arc[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			  Array.Copy(Arcs, 0, next, 0, Arcs.Length);
			  for (int arcOrd = Arcs.Length;arcOrd < next.Length;arcOrd++)
			  {
				next[arcOrd] = new FST.Arc<>();
			  }
			  Arcs = next;
			}
			return Arcs[ord];
		  }

		  internal Frame PushFrame(int state)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Frame f = getFrame(currentFrame == Lucene.Net.Util.BytesRefIterator_Fields.null ? 0 : 1+currentFrame.ord);
			Frame f = GetFrame(CurrentFrame == Lucene.Net.Util.BytesRefIterator_Fields.Null ? 0 : 1 + CurrentFrame.Ord);

			f.Fp = f.FpOrig = CurrentFrame.LastSubFP;
			f.Prefix = CurrentFrame.Prefix + CurrentFrame.Suffix;
			// if (DEBUG) System.out.println("    pushFrame state=" + state + " prefix=" + f.prefix);
			f.State = state;

			// Walk the arc through the index -- we only
			// "bother" with this so we can get the floor data
			// from the index and skip floor blocks when
			// possible:
			FST.Arc<BytesRef> arc = CurrentFrame.Arc;
			int idx = CurrentFrame.Prefix;
			Debug.Assert(CurrentFrame.Suffix > 0);
			BytesRef output = CurrentFrame.OutputPrefix;
			while (idx < f.Prefix)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int target = term.bytes[idx] & 0xff;
			  int target = Term_Renamed.Bytes[idx] & 0xff;
			  // TODO: we could be more efficient for the next()
			  // case by using current arc as starting point,
			  // passed to findTargetArc
			  arc = outerInstance.Index.findTargetArc(target, arc, GetArc(1 + idx), FstReader);
			  Debug.Assert(arc != Lucene.Net.Util.BytesRefIterator_Fields.Null);
			  output = outerInstance.OuterInstance.FstOutputs.add(output, arc.Output);
			  idx++;
			}

			f.Arc = arc;
			f.OutputPrefix = output;
			Debug.Assert(arc.Final);
			f.Load(outerInstance.OuterInstance.FstOutputs.add(output, arc.NextFinalOutput));
			return f;
		  }

		  public override BytesRef Term()
		  {
			return Term_Renamed;
		  }

		  public override int DocFreq()
		  {
			//if (DEBUG) System.out.println("BTIR.docFreq");
			CurrentFrame.DecodeMetaData();
			//if (DEBUG) System.out.println("  return " + currentFrame.termState.docFreq);
			return CurrentFrame.TermState.docFreq;
		  }

		  public override long TotalTermFreq()
		  {
			CurrentFrame.DecodeMetaData();
			return CurrentFrame.TermState.totalTermFreq;
		  }

		  public override DocsEnum Docs(Bits skipDocs, DocsEnum reuse, int flags)
		  {
			CurrentFrame.DecodeMetaData();
			return outerInstance.OuterInstance.PostingsReader.docs(outerInstance.FieldInfo, CurrentFrame.TermState, skipDocs, reuse, flags);
		  }

		  public override DocsAndPositionsEnum DocsAndPositions(Bits skipDocs, DocsAndPositionsEnum reuse, int flags)
		  {
			if (outerInstance.FieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
			{
			  // Positions were not indexed:
			  return Lucene.Net.Util.BytesRefIterator_Fields.Null;
			}

			CurrentFrame.DecodeMetaData();
			return outerInstance.OuterInstance.PostingsReader.docsAndPositions(outerInstance.FieldInfo, CurrentFrame.TermState, skipDocs, reuse, flags);
		  }


		  // NOTE: specialized to only doing the first-time
		  // seek, but we could generalize it to allow
		  // arbitrary seekExact/Ceil.  Note that this is a
		  // seekFloor!
		  internal void SeekToStartTerm(BytesRef target)
		  {
			//if (DEBUG) System.out.println("seek to startTerm=" + target.utf8ToString());
			Debug.Assert(CurrentFrame.Ord == 0);
			if (Term_Renamed.Length < target.Length)
			{
			  Term_Renamed.Bytes = ArrayUtil.Grow(Term_Renamed.Bytes, target.Length);
			}
			FST.Arc<BytesRef> arc = Arcs[0];
			Debug.Assert(arc == CurrentFrame.Arc);

			for (int idx = 0;idx <= target.Length;idx++)
			{

			  while (true)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int savePos = currentFrame.suffixesReader.getPosition();
				int savePos = CurrentFrame.SuffixesReader.Position;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int saveStartBytePos = currentFrame.startBytePos;
				int saveStartBytePos = CurrentFrame.StartBytePos;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int saveSuffix = currentFrame.suffix;
				int saveSuffix = CurrentFrame.Suffix;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long saveLastSubFP = currentFrame.lastSubFP;
				long saveLastSubFP = CurrentFrame.LastSubFP;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int saveTermBlockOrd = currentFrame.termState.termBlockOrd;
				int saveTermBlockOrd = CurrentFrame.TermState.termBlockOrd;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean isSubBlock = currentFrame.next();
				bool isSubBlock = CurrentFrame.Next();

				//if (DEBUG) System.out.println("    cycle ent=" + currentFrame.nextEnt + " (of " + currentFrame.entCount + ") prefix=" + currentFrame.prefix + " suffix=" + currentFrame.suffix + " isBlock=" + isSubBlock + " firstLabel=" + (currentFrame.suffix == 0 ? "" : (currentFrame.suffixBytes[currentFrame.startBytePos])&0xff));
				Term_Renamed.Length = CurrentFrame.Prefix + CurrentFrame.Suffix;
				if (Term_Renamed.Bytes.Length < Term_Renamed.Length)
				{
				  Term_Renamed.Bytes = ArrayUtil.Grow(Term_Renamed.Bytes, Term_Renamed.Length);
				}
				Array.Copy(CurrentFrame.SuffixBytes, CurrentFrame.StartBytePos, Term_Renamed.Bytes, CurrentFrame.Prefix, CurrentFrame.Suffix);

				if (isSubBlock && StringHelper.StartsWith(target, Term_Renamed))
				{
				  // Recurse
				  //if (DEBUG) System.out.println("      recurse!");
				  CurrentFrame = PushFrame(State);
				  break;
				}
				else
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int cmp = term.compareTo(target);
				  int cmp = Term_Renamed.CompareTo(target);
				  if (cmp < 0)
				  {
					if (CurrentFrame.NextEnt == CurrentFrame.EntCount)
					{
					  if (!CurrentFrame.IsLastInFloor)
					  {
						//if (DEBUG) System.out.println("  load floorBlock");
						CurrentFrame.LoadNextFloorBlock();
						continue;
					  }
					  else
					  {
						//if (DEBUG) System.out.println("  return term=" + brToString(term));
						return;
					  }
					}
					continue;
				  }
				  else if (cmp == 0)
				  {
					//if (DEBUG) System.out.println("  return term=" + brToString(term));
					return;
				  }
				  else
				  {
					// Fallback to prior entry: the semantics of
					// this method is that the first call to
					// next() will return the term after the
					// requested term
					CurrentFrame.NextEnt--;
					CurrentFrame.LastSubFP = saveLastSubFP;
					CurrentFrame.StartBytePos = saveStartBytePos;
					CurrentFrame.Suffix = saveSuffix;
					CurrentFrame.SuffixesReader.Position = savePos;
					CurrentFrame.TermState.termBlockOrd = saveTermBlockOrd;
					Array.Copy(CurrentFrame.SuffixBytes, CurrentFrame.StartBytePos, Term_Renamed.Bytes, CurrentFrame.Prefix, CurrentFrame.Suffix);
					Term_Renamed.Length = CurrentFrame.Prefix + CurrentFrame.Suffix;
					// If the last entry was a block we don't
					// need to bother recursing and pushing to
					// the last term under it because the first
					// next() will simply skip the frame anyway
					return;
				  }
				}
			  }
			}

			Debug.Assert(false);
		  }

		  public override BytesRef Next()
		  {

			// if (DEBUG) {
			//   System.out.println("\nintEnum.next seg=" + segment);
			//   System.out.println("  frame ord=" + currentFrame.ord + " prefix=" + brToString(new BytesRef(term.bytes, term.offset, currentFrame.prefix)) + " state=" + currentFrame.state + " lastInFloor?=" + currentFrame.isLastInFloor + " fp=" + currentFrame.fp + " trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]) + " outputPrefix=" + currentFrame.outputPrefix);
			// }

			while (true)
			{
			  // Pop finished frames
			  while (CurrentFrame.NextEnt == CurrentFrame.EntCount)
			  {
				if (!CurrentFrame.IsLastInFloor)
				{
				  //if (DEBUG) System.out.println("    next-floor-block");
				  CurrentFrame.LoadNextFloorBlock();
				  //if (DEBUG) System.out.println("\n  frame ord=" + currentFrame.ord + " prefix=" + brToString(new BytesRef(term.bytes, term.offset, currentFrame.prefix)) + " state=" + currentFrame.state + " lastInFloor?=" + currentFrame.isLastInFloor + " fp=" + currentFrame.fp + " trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]) + " outputPrefix=" + currentFrame.outputPrefix);
				}
				else
				{
				  //if (DEBUG) System.out.println("  pop frame");
				  if (CurrentFrame.Ord == 0)
				  {
					return Lucene.Net.Util.BytesRefIterator_Fields.Null;
				  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long lastFP = currentFrame.fpOrig;
				  long lastFP = CurrentFrame.FpOrig;
				  CurrentFrame = Stack[CurrentFrame.Ord - 1];
				  Debug.Assert(CurrentFrame.LastSubFP == lastFP);
				  //if (DEBUG) System.out.println("\n  frame ord=" + currentFrame.ord + " prefix=" + brToString(new BytesRef(term.bytes, term.offset, currentFrame.prefix)) + " state=" + currentFrame.state + " lastInFloor?=" + currentFrame.isLastInFloor + " fp=" + currentFrame.fp + " trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]) + " outputPrefix=" + currentFrame.outputPrefix);
				}
			  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean isSubBlock = currentFrame.next();
			  bool isSubBlock = CurrentFrame.Next();
			  // if (DEBUG) {
			  //   final BytesRef suffixRef = new BytesRef();
			  //   suffixRef.bytes = currentFrame.suffixBytes;
			  //   suffixRef.offset = currentFrame.startBytePos;
			  //   suffixRef.length = currentFrame.suffix;
			  //   System.out.println("    " + (isSubBlock ? "sub-block" : "term") + " " + currentFrame.nextEnt + " (of " + currentFrame.entCount + ") suffix=" + brToString(suffixRef));
			  // }

			  if (CurrentFrame.Suffix != 0)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int label = currentFrame.suffixBytes[currentFrame.startBytePos] & 0xff;
				int label = CurrentFrame.SuffixBytes[CurrentFrame.StartBytePos] & 0xff;
				while (label > CurrentFrame.CurTransitionMax)
				{
				  if (CurrentFrame.TransitionIndex >= CurrentFrame.Transitions.Length - 1)
				  {
					// Stop processing this frame -- no further
					// matches are possible because we've moved
					// beyond what the max transition will allow
					//if (DEBUG) System.out.println("      break: trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]));

					// sneaky!  forces a pop above
					CurrentFrame.IsLastInFloor = true;
					CurrentFrame.NextEnt = CurrentFrame.EntCount;
					goto nextTermContinue;
				  }
				  CurrentFrame.TransitionIndex++;
				  CurrentFrame.CurTransitionMax = CurrentFrame.Transitions[CurrentFrame.TransitionIndex].Max;
				  //if (DEBUG) System.out.println("      next trans=" + currentFrame.transitions[currentFrame.transitionIndex]);
				}
			  }

			  // First test the common suffix, if set:
			  if (CompiledAutomaton.CommonSuffixRef != Lucene.Net.Util.BytesRefIterator_Fields.Null && !isSubBlock)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLen = currentFrame.prefix + currentFrame.suffix;
				int termLen = CurrentFrame.Prefix + CurrentFrame.Suffix;
				if (termLen < CompiledAutomaton.CommonSuffixRef.length)
				{
				  // No match
				  // if (DEBUG) {
				  //   System.out.println("      skip: common suffix length");
				  // }
				  goto nextTermContinue;
				}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] suffixBytes = currentFrame.suffixBytes;
				sbyte[] suffixBytes = CurrentFrame.SuffixBytes;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] commonSuffixBytes = compiledAutomaton.commonSuffixRef.bytes;
				sbyte[] commonSuffixBytes = CompiledAutomaton.CommonSuffixRef.bytes;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int lenInPrefix = compiledAutomaton.commonSuffixRef.length - currentFrame.suffix;
				int lenInPrefix = CompiledAutomaton.CommonSuffixRef.length - CurrentFrame.Suffix;
				Debug.Assert(CompiledAutomaton.CommonSuffixRef.offset == 0);
				int suffixBytesPos;
				int commonSuffixBytesPos = 0;

				if (lenInPrefix > 0)
				{
				  // A prefix of the common suffix overlaps with
				  // the suffix of the block prefix so we first
				  // test whether the prefix part matches:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] termBytes = term.bytes;
				  sbyte[] termBytes = Term_Renamed.Bytes;
				  int termBytesPos = CurrentFrame.Prefix - lenInPrefix;
				  Debug.Assert(termBytesPos >= 0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termBytesPosEnd = currentFrame.prefix;
				  int termBytesPosEnd = CurrentFrame.Prefix;
				  while (termBytesPos < termBytesPosEnd)
				  {
					if (termBytes[termBytesPos++] != commonSuffixBytes[commonSuffixBytesPos++])
					{
					  // if (DEBUG) {
					  //   System.out.println("      skip: common suffix mismatch (in prefix)");
					  // }
					  goto nextTermContinue;
					}
				  }
				  suffixBytesPos = CurrentFrame.StartBytePos;
				}
				else
				{
				  suffixBytesPos = CurrentFrame.StartBytePos + CurrentFrame.Suffix - CompiledAutomaton.CommonSuffixRef.length;
				}

				// Test overlapping suffix part:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int commonSuffixBytesPosEnd = compiledAutomaton.commonSuffixRef.length;
				int commonSuffixBytesPosEnd = CompiledAutomaton.CommonSuffixRef.length;
				while (commonSuffixBytesPos < commonSuffixBytesPosEnd)
				{
				  if (suffixBytes[suffixBytesPos++] != commonSuffixBytes[commonSuffixBytesPos++])
				  {
					// if (DEBUG) {
					//   System.out.println("      skip: common suffix mismatch");
					// }
					goto nextTermContinue;
				  }
				}
			  }

			  // TODO: maybe we should do the same linear test
			  // that AutomatonTermsEnum does, so that if we
			  // reach a part of the automaton where .* is
			  // "temporarily" accepted, we just blindly .next()
			  // until the limit

			  // See if the term prefix matches the automaton:
			  int state = CurrentFrame.State_Renamed;
			  for (int idx = 0;idx < CurrentFrame.Suffix;idx++)
			  {
				state = RunAutomaton.Step(state, CurrentFrame.SuffixBytes[CurrentFrame.StartBytePos + idx] & 0xff);
				if (state == -1)
				{
				  // No match
				  //System.out.println("    no s=" + state);
				  goto nextTermContinue;
				}
				else
				{
				  //System.out.println("    c s=" + state);
				}
			  }

			  if (isSubBlock)
			  {
				// Match!  Recurse:
				//if (DEBUG) System.out.println("      sub-block match to state=" + state + "; recurse fp=" + currentFrame.lastSubFP);
				CopyTerm();
				CurrentFrame = PushFrame(state);
				//if (DEBUG) System.out.println("\n  frame ord=" + currentFrame.ord + " prefix=" + brToString(new BytesRef(term.bytes, term.offset, currentFrame.prefix)) + " state=" + currentFrame.state + " lastInFloor?=" + currentFrame.isLastInFloor + " fp=" + currentFrame.fp + " trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]) + " outputPrefix=" + currentFrame.outputPrefix);
			  }
			  else if (RunAutomaton.IsAccept(state))
			  {
				CopyTerm();
				//if (DEBUG) System.out.println("      term match to state=" + state + "; return term=" + brToString(term));
				Debug.Assert(SavedStartTerm_Renamed == Lucene.Net.Util.BytesRefIterator_Fields.Null || Term_Renamed.CompareTo(SavedStartTerm_Renamed) > 0, "saveStartTerm=" + SavedStartTerm_Renamed.Utf8ToString() + " term=" + Term_Renamed.Utf8ToString());
				return Term_Renamed;
			  }
			  else
			  {
				//System.out.println("    no s=" + state);
			  }
				nextTermContinue:;
			}
			nextTermBreak:;
		  }

		  internal void CopyTerm()
		  {
			//System.out.println("      copyTerm cur.prefix=" + currentFrame.prefix + " cur.suffix=" + currentFrame.suffix + " first=" + (char) currentFrame.suffixBytes[currentFrame.startBytePos]);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int len = currentFrame.prefix + currentFrame.suffix;
			int len = CurrentFrame.Prefix + CurrentFrame.Suffix;
			if (Term_Renamed.Bytes.Length < len)
			{
			  Term_Renamed.Bytes = ArrayUtil.Grow(Term_Renamed.Bytes, len);
			}
			Array.Copy(CurrentFrame.SuffixBytes, CurrentFrame.StartBytePos, Term_Renamed.Bytes, CurrentFrame.Prefix, CurrentFrame.Suffix);
			Term_Renamed.Length = len;
		  }

		  public override IComparer<BytesRef> Comparator
		  {
			  get
			  {
				return BytesRef.UTF8SortedAsUnicodeComparator;
			  }
		  }

		  public override bool SeekExact(BytesRef text)
		  {
			throw new System.NotSupportedException();
		  }

		  public override void SeekExact(long ord)
		  {
			throw new System.NotSupportedException();
		  }

		  public override long Ord()
		  {
			throw new System.NotSupportedException();
		  }

		  public override SeekStatus SeekCeil(BytesRef text)
		  {
			throw new System.NotSupportedException();
		  }
		}

		// Iterates through terms in this field
		private sealed class SegmentTermsEnum : TermsEnum
		{
			private readonly BlockTreeTermsReader.FieldReader OuterInstance;

		  internal IndexInput @in;

		  internal Frame[] Stack;
		  internal readonly Frame StaticFrame;
		  internal Frame CurrentFrame;
		  internal bool TermExists;

		  internal int TargetBeforeCurrentLength;

		  internal readonly ByteArrayDataInput ScratchReader = new ByteArrayDataInput();

		  // What prefix of the current term was present in the index:
		  internal int ValidIndexPrefix;

		  // assert only:
		  internal bool Eof;

		  internal readonly BytesRef Term_Renamed = new BytesRef();
		  internal readonly FST.BytesReader FstReader;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) private Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef>[] arcs = new Lucene.Net.Util.Fst.FST.Arc[1];
		  internal FST.Arc<BytesRef>[] Arcs = new FST.Arc[1];

		  public SegmentTermsEnum(BlockTreeTermsReader.FieldReader outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			//if (DEBUG) System.out.println("BTTR.init seg=" + segment);
			Stack = new Frame[0];

			// Used to hold seek by TermState, or cached seek
			StaticFrame = new Frame(this, -1);

			if (outerInstance.Index == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  FstReader = Lucene.Net.Util.BytesRefIterator_Fields.Null;
			}
			else
			{
			  FstReader = outerInstance.Index.BytesReader;
			}

			// Init w/ root block; don't use index since it may
			// not (and need not) have been loaded
			for (int arcIdx = 0;arcIdx < Arcs.Length;arcIdx++)
			{
			  Arcs[arcIdx] = new FST.Arc<>();
			}

			CurrentFrame = StaticFrame;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef> arc;
			FST.Arc<BytesRef> arc;
			if (outerInstance.Index != Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  arc = outerInstance.Index.getFirstArc(Arcs[0]);
			  // Empty string prefix must have an output in the index!
			  Debug.Assert(arc.Final);
			}
			else
			{
			  arc = Lucene.Net.Util.BytesRefIterator_Fields.Null;
			}
			CurrentFrame = StaticFrame;
			//currentFrame = pushFrame(arc, rootCode, 0);
			//currentFrame.loadBlock();
			ValidIndexPrefix = 0;
			// if (DEBUG) {
			//   System.out.println("init frame state " + currentFrame.ord);
			//   printSeekState();
			// }

			//System.out.println();
			// computeBlockStats().print(System.out);
		  }

		  // Not private to avoid synthetic access$NNN methods
		  internal void InitIndexInput()
		  {
			if (this.@in == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  this.@in = OuterInstance.OuterInstance.@in.Clone();
			}
		  }

		  /// <summary>
		  /// Runs next() through the entire terms dict,
		  ///  computing aggregate statistics. 
		  /// </summary>
		  public Stats ComputeBlockStats()
		  {

			Stats stats = new Stats(outerInstance.OuterInstance.Segment, outerInstance.FieldInfo.Name);
			if (outerInstance.Index != Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  stats.IndexNodeCount = outerInstance.Index.NodeCount;
			  stats.IndexArcCount = outerInstance.Index.ArcCount;
			  stats.IndexNumBytes = outerInstance.Index.sizeInBytes();
			}

			CurrentFrame = StaticFrame;
			FST.Arc<BytesRef> arc;
			if (outerInstance.Index != Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  arc = outerInstance.Index.getFirstArc(Arcs[0]);
			  // Empty string prefix must have an output in the index!
			  Debug.Assert(arc.Final);
			}
			else
			{
			  arc = Lucene.Net.Util.BytesRefIterator_Fields.Null;
			}

			// Empty string prefix must have an output in the
			// index!
			CurrentFrame = PushFrame(arc, outerInstance.RootCode, 0);
			CurrentFrame.FpOrig = CurrentFrame.Fp;
			CurrentFrame.LoadBlock();
			ValidIndexPrefix = 0;

			stats.StartBlock(CurrentFrame, !CurrentFrame.IsLastInFloor);

			while (true)
			{

			  // Pop finished blocks
			  while (CurrentFrame.NextEnt == CurrentFrame.EntCount)
			  {
				stats.EndBlock(CurrentFrame);
				if (!CurrentFrame.IsLastInFloor)
				{
				  CurrentFrame.LoadNextFloorBlock();
				  stats.StartBlock(CurrentFrame, true);
				}
				else
				{
				  if (CurrentFrame.Ord == 0)
				  {
					goto allTermsBreak;
				  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long lastFP = currentFrame.fpOrig;
				  long lastFP = CurrentFrame.FpOrig;
				  CurrentFrame = Stack[CurrentFrame.Ord - 1];
				  Debug.Assert(lastFP == CurrentFrame.LastSubFP);
				  // if (DEBUG) {
				  //   System.out.println("  reset validIndexPrefix=" + validIndexPrefix);
				  // }
				}
			  }

			  while (true)
			  {
				if (CurrentFrame.Next())
				{
				  // Push to new block:
				  CurrentFrame = PushFrame(Lucene.Net.Util.BytesRefIterator_Fields.Null, CurrentFrame.LastSubFP, Term_Renamed.Length);
				  CurrentFrame.FpOrig = CurrentFrame.Fp;
				  // this is a "next" frame -- even if it's
				  // floor'd we must pretend it isn't so we don't
				  // try to scan to the right floor frame:
				  CurrentFrame.IsFloor = false;
				  //currentFrame.hasTerms = true;
				  CurrentFrame.LoadBlock();
				  stats.StartBlock(CurrentFrame, !CurrentFrame.IsLastInFloor);
				}
				else
				{
				  stats.Term(Term_Renamed);
				  break;
				}
			  }
				allTermsContinue:;
			}
			allTermsBreak:

			stats.Finish();

			// Put root frame back:
			CurrentFrame = StaticFrame;
			if (outerInstance.Index != Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  arc = outerInstance.Index.getFirstArc(Arcs[0]);
			  // Empty string prefix must have an output in the index!
			  Debug.Assert(arc.Final);
			}
			else
			{
			  arc = Lucene.Net.Util.BytesRefIterator_Fields.Null;
			}
			CurrentFrame = PushFrame(arc, outerInstance.RootCode, 0);
			CurrentFrame.Rewind();
			CurrentFrame.LoadBlock();
			ValidIndexPrefix = 0;
			Term_Renamed.Length = 0;

			return stats;
		  }

		  internal Frame GetFrame(int ord)
		  {
			if (ord >= Stack.Length)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Frame[] next = new Frame[Lucene.Net.Util.ArrayUtil.oversize(1+ord, Lucene.Net.Util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			  Frame[] next = new Frame[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			  Array.Copy(Stack, 0, next, 0, Stack.Length);
			  for (int stackOrd = Stack.Length;stackOrd < next.Length;stackOrd++)
			  {
				next[stackOrd] = new Frame(this, stackOrd);
			  }
			  Stack = next;
			}
			Debug.Assert(Stack[ord].Ord == ord);
			return Stack[ord];
		  }

		  internal FST.Arc<BytesRef> GetArc(int ord)
		  {
			if (ord >= Arcs.Length)
			{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) final Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef>[] next = new Lucene.Net.Util.Fst.FST.Arc[Lucene.Net.Util.ArrayUtil.oversize(1+ord, Lucene.Net.Util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
			  FST.Arc<BytesRef>[] next = new FST.Arc[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			  Array.Copy(Arcs, 0, next, 0, Arcs.Length);
			  for (int arcOrd = Arcs.Length;arcOrd < next.Length;arcOrd++)
			  {
				next[arcOrd] = new FST.Arc<>();
			  }
			  Arcs = next;
			}
			return Arcs[ord];
		  }

		  public override IComparer<BytesRef> Comparator
		  {
			  get
			  {
				return BytesRef.UTF8SortedAsUnicodeComparator;
			  }
		  }

		  // Pushes a frame we seek'd to
		  internal Frame PushFrame(FST.Arc<BytesRef> arc, BytesRef frameData, int length)
		  {
			ScratchReader.Reset(frameData.Bytes, frameData.Offset, frameData.Length);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long code = scratchReader.readVLong();
			long code = ScratchReader.ReadVLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long fpSeek = code >>> BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS;
			long fpSeek = (long)((ulong)code >> BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Frame f = getFrame(1+currentFrame.ord);
			Frame f = GetFrame(1 + CurrentFrame.Ord);
			f.HasTerms = (code & BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS) != 0;
			f.HasTermsOrig = f.HasTerms;
			f.IsFloor = (code & BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR) != 0;
			if (f.IsFloor)
			{
			  f.SetFloorData(ScratchReader, frameData);
			}
			PushFrame(arc, fpSeek, length);

			return f;
		  }

		  // Pushes next'd frame or seek'd frame; we later
		  // lazy-load the frame only when needed
		  internal Frame PushFrame(FST.Arc<BytesRef> arc, long fp, int length)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Frame f = getFrame(1+currentFrame.ord);
			Frame f = GetFrame(1 + CurrentFrame.Ord);
			f.Arc = arc;
			if (f.FpOrig == fp && f.NextEnt != -1)
			{
			  //if (DEBUG) System.out.println("      push reused frame ord=" + f.ord + " fp=" + f.fp + " isFloor?=" + f.isFloor + " hasTerms=" + f.hasTerms + " pref=" + term + " nextEnt=" + f.nextEnt + " targetBeforeCurrentLength=" + targetBeforeCurrentLength + " term.length=" + term.length + " vs prefix=" + f.prefix);
			  if (f.Prefix > TargetBeforeCurrentLength)
			  {
				f.Rewind();
			  }
			  else
			  {
				// if (DEBUG) {
				//   System.out.println("        skip rewind!");
				// }
			  }
			  Debug.Assert(length == f.Prefix);
			}
			else
			{
			  f.NextEnt = -1;
			  f.Prefix = length;
			  f.State.termBlockOrd = 0;
			  f.FpOrig = f.Fp = fp;
			  f.LastSubFP = -1;
			  // if (DEBUG) {
			  //   final int sav = term.length;
			  //   term.length = length;
			  //   System.out.println("      push new frame ord=" + f.ord + " fp=" + f.fp + " hasTerms=" + f.hasTerms + " isFloor=" + f.isFloor + " pref=" + brToString(term));
			  //   term.length = sav;
			  // }
			}

			return f;
		  }

		  // asserts only
		  internal bool ClearEOF()
		  {
			Eof = false;
			return true;
		  }

		  // asserts only
		  internal bool SetEOF()
		  {
			Eof = true;
			return true;
		  }

		  public override bool SeekExact(BytesRef target)
		  {

			if (outerInstance.Index == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  throw new IllegalStateException("terms index was not loaded");
			}

			if (Term_Renamed.Bytes.Length <= target.Length)
			{
			  Term_Renamed.Bytes = ArrayUtil.Grow(Term_Renamed.Bytes, 1 + target.Length);
			}

			Debug.Assert(ClearEOF());

			// if (DEBUG) {
			//   System.out.println("\nBTTR.seekExact seg=" + segment + " target=" + fieldInfo.name + ":" + brToString(target) + " current=" + brToString(term) + " (exists?=" + termExists + ") validIndexPrefix=" + validIndexPrefix);
			//   printSeekState();
			// }

			FST.Arc<BytesRef> arc;
			int targetUpto;
			BytesRef output;

			TargetBeforeCurrentLength = CurrentFrame.Ord;

			if (CurrentFrame != StaticFrame)
			{

			  // We are already seek'd; find the common
			  // prefix of new seek term vs current term and
			  // re-use the corresponding seek state.  For
			  // example, if app first seeks to foobar, then
			  // seeks to foobaz, we can re-use the seek state
			  // for the first 5 bytes.

			  // if (DEBUG) {
			  //   System.out.println("  re-use current seek state validIndexPrefix=" + validIndexPrefix);
			  // }

			  arc = Arcs[0];
			  Debug.Assert(arc.Final);
			  output = arc.Output;
			  targetUpto = 0;

			  Frame lastFrame = Stack[0];
			  Debug.Assert(ValidIndexPrefix <= Term_Renamed.Length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLimit = Math.min(target.length, validIndexPrefix);
			  int targetLimit = Math.Min(target.Length, ValidIndexPrefix);

			  int cmp = 0;

			  // TODO: reverse vLong byte order for better FST
			  // prefix output sharing

			  // First compare up to valid seek frames:
			  while (targetUpto < targetLimit)
			  {
				cmp = (Term_Renamed.Bytes[targetUpto] & 0xFF) - (target.Bytes[target.Offset + targetUpto] & 0xFF);
				// if (DEBUG) {
				//   System.out.println("    cycle targetUpto=" + targetUpto + " (vs limit=" + targetLimit + ") cmp=" + cmp + " (targetLabel=" + (char) (target.bytes[target.offset + targetUpto]) + " vs termLabel=" + (char) (term.bytes[targetUpto]) + ")"   + " arc.output=" + arc.output + " output=" + output);
				// }
				if (cmp != 0)
				{
				  break;
				}
				arc = Arcs[1 + targetUpto];
				//if (arc.label != (target.bytes[target.offset + targetUpto] & 0xFF)) {
				//System.out.println("FAIL: arc.label=" + (char) arc.label + " targetLabel=" + (char) (target.bytes[target.offset + targetUpto] & 0xFF));
				//}
				Debug.Assert(arc.Label == (target.Bytes[target.Offset + targetUpto] & 0xFF), "arc.label=" + (char) arc.Label + " targetLabel=" + (char)(target.Bytes[target.Offset + targetUpto] & 0xFF));
				if (arc.Output != outerInstance.OuterInstance.NO_OUTPUT)
				{
				  output = outerInstance.OuterInstance.FstOutputs.add(output, arc.Output);
				}
				if (arc.Final)
				{
				  lastFrame = Stack[1 + lastFrame.Ord];
				}
				targetUpto++;
			  }

			  if (cmp == 0)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetUptoMid = targetUpto;
				int targetUptoMid = targetUpto;

				// Second compare the rest of the term, but
				// don't save arc/output/frame; we only do this
				// to find out if the target term is before,
				// equal or after the current term
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLimit2 = Math.min(target.length, term.length);
				int targetLimit2 = Math.Min(target.Length, Term_Renamed.Length);
				while (targetUpto < targetLimit2)
				{
				  cmp = (Term_Renamed.Bytes[targetUpto] & 0xFF) - (target.Bytes[target.Offset + targetUpto] & 0xFF);
				  // if (DEBUG) {
				  //   System.out.println("    cycle2 targetUpto=" + targetUpto + " (vs limit=" + targetLimit + ") cmp=" + cmp + " (targetLabel=" + (char) (target.bytes[target.offset + targetUpto]) + " vs termLabel=" + (char) (term.bytes[targetUpto]) + ")");
				  // }
				  if (cmp != 0)
				  {
					break;
				  }
				  targetUpto++;
				}

				if (cmp == 0)
				{
				  cmp = Term_Renamed.Length - target.Length;
				}
				targetUpto = targetUptoMid;
			  }

			  if (cmp < 0)
			  {
				// Common case: target term is after current
				// term, ie, app is seeking multiple terms
				// in sorted order
				// if (DEBUG) {
				//   System.out.println("  target is after current (shares prefixLen=" + targetUpto + "); frame.ord=" + lastFrame.ord);
				// }
				CurrentFrame = lastFrame;

			  }
			  else if (cmp > 0)
			  {
				// Uncommon case: target term
				// is before current term; this means we can
				// keep the currentFrame but we must rewind it
				// (so we scan from the start)
				TargetBeforeCurrentLength = 0;
				// if (DEBUG) {
				//   System.out.println("  target is before current (shares prefixLen=" + targetUpto + "); rewind frame ord=" + lastFrame.ord);
				// }
				CurrentFrame = lastFrame;
				CurrentFrame.Rewind();
			  }
			  else
			  {
				// Target is exactly the same as current term
				Debug.Assert(Term_Renamed.Length == target.Length);
				if (TermExists)
				{
				  // if (DEBUG) {
				  //   System.out.println("  target is same as current; return true");
				  // }
				  return true;
				}
				else
				{
				  // if (DEBUG) {
				  //   System.out.println("  target is same as current but term doesn't exist");
				  // }
				}
				//validIndexPrefix = currentFrame.depth;
				//term.length = target.length;
				//return termExists;
			  }

			}
			else
			{

			  TargetBeforeCurrentLength = -1;
			  arc = outerInstance.Index.getFirstArc(Arcs[0]);

			  // Empty string prefix must have an output (block) in the index!
			  Debug.Assert(arc.Final);
			  Debug.Assert(arc.Output != Lucene.Net.Util.BytesRefIterator_Fields.Null);

			  // if (DEBUG) {
			  //   System.out.println("    no seek state; push root frame");
			  // }

			  output = arc.Output;

			  CurrentFrame = StaticFrame;

			  //term.length = 0;
			  targetUpto = 0;
			  CurrentFrame = PushFrame(arc, outerInstance.OuterInstance.FstOutputs.add(output, arc.NextFinalOutput), 0);
			}

			// if (DEBUG) {
			//   System.out.println("  start index loop targetUpto=" + targetUpto + " output=" + output + " currentFrame.ord=" + currentFrame.ord + " targetBeforeCurrentLength=" + targetBeforeCurrentLength);
			// }

			while (targetUpto < target.Length)
			{

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLabel = target.bytes[target.offset + targetUpto] & 0xFF;
			  int targetLabel = target.Bytes[target.Offset + targetUpto] & 0xFF;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef> nextArc = index.findTargetArc(targetLabel, arc, getArc(1+targetUpto), fstReader);
			  FST.Arc<BytesRef> nextArc = outerInstance.Index.findTargetArc(targetLabel, arc, GetArc(1 + targetUpto), FstReader);

			  if (nextArc == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			  {

				// Index is exhausted
				// if (DEBUG) {
				//   System.out.println("    index: index exhausted label=" + ((char) targetLabel) + " " + toHex(targetLabel));
				// }

				ValidIndexPrefix = CurrentFrame.Prefix;
				//validIndexPrefix = targetUpto;

				CurrentFrame.ScanToFloorFrame(target);

				if (!CurrentFrame.HasTerms)
				{
				  TermExists = false;
				  Term_Renamed.Bytes[targetUpto] = (sbyte) targetLabel;
				  Term_Renamed.Length = 1 + targetUpto;
				  // if (DEBUG) {
				  //   System.out.println("  FAST NOT_FOUND term=" + brToString(term));
				  // }
				  return false;
				}

				CurrentFrame.LoadBlock();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SeekStatus result = currentFrame.scanToTerm(target, true);
				SeekStatus result = CurrentFrame.ScanToTerm(target, true);
				if (result == SeekStatus.FOUND)
				{
				  // if (DEBUG) {
				  //   System.out.println("  return FOUND term=" + term.utf8ToString() + " " + term);
				  // }
				  return true;
				}
				else
				{
				  // if (DEBUG) {
				  //   System.out.println("  got " + result + "; return NOT_FOUND term=" + brToString(term));
				  // }
				  return false;
				}
			  }
			  else
			  {
				// Follow this arc
				arc = nextArc;
				Term_Renamed.Bytes[targetUpto] = (sbyte) targetLabel;
				// Aggregate output as we go:
				Debug.Assert(arc.Output != Lucene.Net.Util.BytesRefIterator_Fields.Null);
				if (arc.Output != outerInstance.OuterInstance.NO_OUTPUT)
				{
				  output = outerInstance.OuterInstance.FstOutputs.add(output, arc.Output);
				}

				// if (DEBUG) {
				//   System.out.println("    index: follow label=" + toHex(target.bytes[target.offset + targetUpto]&0xff) + " arc.output=" + arc.output + " arc.nfo=" + arc.nextFinalOutput);
				// }
				targetUpto++;

				if (arc.Final)
				{
				  //if (DEBUG) System.out.println("    arc is final!");
				  CurrentFrame = PushFrame(arc, outerInstance.OuterInstance.FstOutputs.add(output, arc.NextFinalOutput), targetUpto);
				  //if (DEBUG) System.out.println("    curFrame.ord=" + currentFrame.ord + " hasTerms=" + currentFrame.hasTerms);
				}
			  }
			}

			//validIndexPrefix = targetUpto;
			ValidIndexPrefix = CurrentFrame.Prefix;

			CurrentFrame.ScanToFloorFrame(target);

			// Target term is entirely contained in the index:
			if (!CurrentFrame.HasTerms)
			{
			  TermExists = false;
			  Term_Renamed.Length = targetUpto;
			  // if (DEBUG) {
			  //   System.out.println("  FAST NOT_FOUND term=" + brToString(term));
			  // }
			  return false;
			}

			CurrentFrame.LoadBlock();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SeekStatus result = currentFrame.scanToTerm(target, true);
			SeekStatus result = CurrentFrame.ScanToTerm(target, true);
			if (result == SeekStatus.FOUND)
			{
			  // if (DEBUG) {
			  //   System.out.println("  return FOUND term=" + term.utf8ToString() + " " + term);
			  // }
			  return true;
			}
			else
			{
			  // if (DEBUG) {
			  //   System.out.println("  got result " + result + "; return NOT_FOUND term=" + term.utf8ToString());
			  // }

			  return false;
			}
		  }

		  public override SeekStatus SeekCeil(BytesRef target)
		  {
			if (outerInstance.Index == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  throw new IllegalStateException("terms index was not loaded");
			}

			if (Term_Renamed.Bytes.Length <= target.Length)
			{
			  Term_Renamed.Bytes = ArrayUtil.Grow(Term_Renamed.Bytes, 1 + target.Length);
			}

			Debug.Assert(ClearEOF());

			//if (DEBUG) {
			//System.out.println("\nBTTR.seekCeil seg=" + segment + " target=" + fieldInfo.name + ":" + target.utf8ToString() + " " + target + " current=" + brToString(term) + " (exists?=" + termExists + ") validIndexPrefix=  " + validIndexPrefix);
			//printSeekState();
			//}

			FST.Arc<BytesRef> arc;
			int targetUpto;
			BytesRef output;

			TargetBeforeCurrentLength = CurrentFrame.Ord;

			if (CurrentFrame != StaticFrame)
			{

			  // We are already seek'd; find the common
			  // prefix of new seek term vs current term and
			  // re-use the corresponding seek state.  For
			  // example, if app first seeks to foobar, then
			  // seeks to foobaz, we can re-use the seek state
			  // for the first 5 bytes.

			  //if (DEBUG) {
			  //System.out.println("  re-use current seek state validIndexPrefix=" + validIndexPrefix);
			  //}

			  arc = Arcs[0];
			  Debug.Assert(arc.Final);
			  output = arc.Output;
			  targetUpto = 0;

			  Frame lastFrame = Stack[0];
			  Debug.Assert(ValidIndexPrefix <= Term_Renamed.Length);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLimit = Math.min(target.length, validIndexPrefix);
			  int targetLimit = Math.Min(target.Length, ValidIndexPrefix);

			  int cmp = 0;

			  // TOOD: we should write our vLong backwards (MSB
			  // first) to get better sharing from the FST

			  // First compare up to valid seek frames:
			  while (targetUpto < targetLimit)
			  {
				cmp = (Term_Renamed.Bytes[targetUpto] & 0xFF) - (target.Bytes[target.Offset + targetUpto] & 0xFF);
				//if (DEBUG) {
				//System.out.println("    cycle targetUpto=" + targetUpto + " (vs limit=" + targetLimit + ") cmp=" + cmp + " (targetLabel=" + (char) (target.bytes[target.offset + targetUpto]) + " vs termLabel=" + (char) (term.bytes[targetUpto]) + ")"   + " arc.output=" + arc.output + " output=" + output);
				//}
				if (cmp != 0)
				{
				  break;
				}
				arc = Arcs[1 + targetUpto];
				Debug.Assert(arc.Label == (target.Bytes[target.Offset + targetUpto] & 0xFF), "arc.label=" + (char) arc.Label + " targetLabel=" + (char)(target.Bytes[target.Offset + targetUpto] & 0xFF));
				// TOOD: we could save the outputs in local
				// byte[][] instead of making new objs ever
				// seek; but, often the FST doesn't have any
				// shared bytes (but this could change if we
				// reverse vLong byte order)
				if (arc.Output != outerInstance.OuterInstance.NO_OUTPUT)
				{
				  output = outerInstance.OuterInstance.FstOutputs.add(output, arc.Output);
				}
				if (arc.Final)
				{
				  lastFrame = Stack[1 + lastFrame.Ord];
				}
				targetUpto++;
			  }


			  if (cmp == 0)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetUptoMid = targetUpto;
				int targetUptoMid = targetUpto;
				// Second compare the rest of the term, but
				// don't save arc/output/frame:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLimit2 = Math.min(target.length, term.length);
				int targetLimit2 = Math.Min(target.Length, Term_Renamed.Length);
				while (targetUpto < targetLimit2)
				{
				  cmp = (Term_Renamed.Bytes[targetUpto] & 0xFF) - (target.Bytes[target.Offset + targetUpto] & 0xFF);
				  //if (DEBUG) {
				  //System.out.println("    cycle2 targetUpto=" + targetUpto + " (vs limit=" + targetLimit + ") cmp=" + cmp + " (targetLabel=" + (char) (target.bytes[target.offset + targetUpto]) + " vs termLabel=" + (char) (term.bytes[targetUpto]) + ")");
				  //}
				  if (cmp != 0)
				  {
					break;
				  }
				  targetUpto++;
				}

				if (cmp == 0)
				{
				  cmp = Term_Renamed.Length - target.Length;
				}
				targetUpto = targetUptoMid;
			  }

			  if (cmp < 0)
			  {
				// Common case: target term is after current
				// term, ie, app is seeking multiple terms
				// in sorted order
				//if (DEBUG) {
				//System.out.println("  target is after current (shares prefixLen=" + targetUpto + "); clear frame.scanned ord=" + lastFrame.ord);
				//}
				CurrentFrame = lastFrame;

			  }
			  else if (cmp > 0)
			  {
				// Uncommon case: target term
				// is before current term; this means we can
				// keep the currentFrame but we must rewind it
				// (so we scan from the start)
				TargetBeforeCurrentLength = 0;
				//if (DEBUG) {
				//System.out.println("  target is before current (shares prefixLen=" + targetUpto + "); rewind frame ord=" + lastFrame.ord);
				//}
				CurrentFrame = lastFrame;
				CurrentFrame.Rewind();
			  }
			  else
			  {
				// Target is exactly the same as current term
				Debug.Assert(Term_Renamed.Length == target.Length);
				if (TermExists)
				{
				  //if (DEBUG) {
				  //System.out.println("  target is same as current; return FOUND");
				  //}
				  return SeekStatus.FOUND;
				}
				else
				{
				  //if (DEBUG) {
				  //System.out.println("  target is same as current but term doesn't exist");
				  //}
				}
			  }

			}
			else
			{

			  TargetBeforeCurrentLength = -1;
			  arc = outerInstance.Index.getFirstArc(Arcs[0]);

			  // Empty string prefix must have an output (block) in the index!
			  Debug.Assert(arc.Final);
			  Debug.Assert(arc.Output != Lucene.Net.Util.BytesRefIterator_Fields.Null);

			  //if (DEBUG) {
			  //System.out.println("    no seek state; push root frame");
			  //}

			  output = arc.Output;

			  CurrentFrame = StaticFrame;

			  //term.length = 0;
			  targetUpto = 0;
			  CurrentFrame = PushFrame(arc, outerInstance.OuterInstance.FstOutputs.add(output, arc.NextFinalOutput), 0);
			}

			//if (DEBUG) {
			//System.out.println("  start index loop targetUpto=" + targetUpto + " output=" + output + " currentFrame.ord+1=" + currentFrame.ord + " targetBeforeCurrentLength=" + targetBeforeCurrentLength);
			//}

			while (targetUpto < target.Length)
			{

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLabel = target.bytes[target.offset + targetUpto] & 0xFF;
			  int targetLabel = target.Bytes[target.Offset + targetUpto] & 0xFF;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef> nextArc = index.findTargetArc(targetLabel, arc, getArc(1+targetUpto), fstReader);
			  FST.Arc<BytesRef> nextArc = outerInstance.Index.findTargetArc(targetLabel, arc, GetArc(1 + targetUpto), FstReader);

			  if (nextArc == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			  {

				// Index is exhausted
				// if (DEBUG) {
				//   System.out.println("    index: index exhausted label=" + ((char) targetLabel) + " " + toHex(targetLabel));
				// }

				ValidIndexPrefix = CurrentFrame.Prefix;
				//validIndexPrefix = targetUpto;

				CurrentFrame.ScanToFloorFrame(target);

				CurrentFrame.LoadBlock();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SeekStatus result = currentFrame.scanToTerm(target, false);
				SeekStatus result = CurrentFrame.ScanToTerm(target, false);
				if (result == SeekStatus.END)
				{
				  Term_Renamed.CopyBytes(target);
				  TermExists = false;

				  if (Next() != Lucene.Net.Util.BytesRefIterator_Fields.Null)
				  {
					//if (DEBUG) {
					//System.out.println("  return NOT_FOUND term=" + brToString(term) + " " + term);
					//}
					return SeekStatus.NOT_FOUND;
				  }
				  else
				  {
					//if (DEBUG) {
					//System.out.println("  return END");
					//}
					return SeekStatus.END;
				  }
				}
				else
				{
				  //if (DEBUG) {
				  //System.out.println("  return " + result + " term=" + brToString(term) + " " + term);
				  //}
				  return result;
				}
			  }
			  else
			  {
				// Follow this arc
				Term_Renamed.Bytes[targetUpto] = (sbyte) targetLabel;
				arc = nextArc;
				// Aggregate output as we go:
				Debug.Assert(arc.Output != Lucene.Net.Util.BytesRefIterator_Fields.Null);
				if (arc.Output != outerInstance.OuterInstance.NO_OUTPUT)
				{
				  output = outerInstance.OuterInstance.FstOutputs.add(output, arc.Output);
				}

				//if (DEBUG) {
				//System.out.println("    index: follow label=" + toHex(target.bytes[target.offset + targetUpto]&0xff) + " arc.output=" + arc.output + " arc.nfo=" + arc.nextFinalOutput);
				//}
				targetUpto++;

				if (arc.Final)
				{
				  //if (DEBUG) System.out.println("    arc is final!");
				  CurrentFrame = PushFrame(arc, outerInstance.OuterInstance.FstOutputs.add(output, arc.NextFinalOutput), targetUpto);
				  //if (DEBUG) System.out.println("    curFrame.ord=" + currentFrame.ord + " hasTerms=" + currentFrame.hasTerms);
				}
			  }
			}

			//validIndexPrefix = targetUpto;
			ValidIndexPrefix = CurrentFrame.Prefix;

			CurrentFrame.ScanToFloorFrame(target);

			CurrentFrame.LoadBlock();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SeekStatus result = currentFrame.scanToTerm(target, false);
			SeekStatus result = CurrentFrame.ScanToTerm(target, false);

			if (result == SeekStatus.END)
			{
			  Term_Renamed.CopyBytes(target);
			  TermExists = false;
			  if (Next() != Lucene.Net.Util.BytesRefIterator_Fields.Null)
			  {
				//if (DEBUG) {
				//System.out.println("  return NOT_FOUND term=" + term.utf8ToString() + " " + term);
				//}
				return SeekStatus.NOT_FOUND;
			  }
			  else
			  {
				//if (DEBUG) {
				//System.out.println("  return END");
				//}
				return SeekStatus.END;
			  }
			}
			else
			{
			  return result;
			}
		  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unused") private void printSeekState(java.io.PrintStream out) throws java.io.IOException
		  internal void PrintSeekState(PrintStream @out)
		  {
			if (CurrentFrame == StaticFrame)
			{
			  @out.println("  no prior seek");
			}
			else
			{
			  @out.println("  prior seek state:");
			  int ord = 0;
			  bool isSeekFrame = true;
			  while (true)
			  {
				Frame f = GetFrame(ord);
				Debug.Assert(f != Lucene.Net.Util.BytesRefIterator_Fields.Null);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.BytesRef prefix = new Lucene.Net.Util.BytesRef(term.bytes, 0, f.prefix);
				BytesRef prefix = new BytesRef(Term_Renamed.Bytes, 0, f.Prefix);
				if (f.NextEnt == -1)
				{
				  @out.println("    frame " + (isSeekFrame ? "(seek)" : "(next)") + " ord=" + ord + " fp=" + f.Fp + (f.IsFloor ? (" (fpOrig=" + f.FpOrig + ")") : "") + " prefixLen=" + f.Prefix + " prefix=" + prefix + (f.NextEnt == -1 ? "" : (" (of " + f.EntCount + ")")) + " hasTerms=" + f.HasTerms + " isFloor=" + f.IsFloor + " code=" + ((f.Fp << BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) + (f.HasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) + (f.IsFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0)) + " isLastInFloor=" + f.IsLastInFloor + " mdUpto=" + f.MetaDataUpto + " tbOrd=" + f.TermBlockOrd);
				}
				else
				{
				  @out.println("    frame " + (isSeekFrame ? "(seek, loaded)" : "(next, loaded)") + " ord=" + ord + " fp=" + f.Fp + (f.IsFloor ? (" (fpOrig=" + f.FpOrig + ")") : "") + " prefixLen=" + f.Prefix + " prefix=" + prefix + " nextEnt=" + f.NextEnt + (f.NextEnt == -1 ? "" : (" (of " + f.EntCount + ")")) + " hasTerms=" + f.HasTerms + " isFloor=" + f.IsFloor + " code=" + ((f.Fp << BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) + (f.HasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) + (f.IsFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0)) + " lastSubFP=" + f.LastSubFP + " isLastInFloor=" + f.IsLastInFloor + " mdUpto=" + f.MetaDataUpto + " tbOrd=" + f.TermBlockOrd);
				}
				if (outerInstance.Index != Lucene.Net.Util.BytesRefIterator_Fields.Null)
				{
				  Debug.Assert(!isSeekFrame || f.Arc != Lucene.Net.Util.BytesRefIterator_Fields.Null, "isSeekFrame=" + isSeekFrame + " f.arc=" + f.Arc);
				  if (f.Prefix > 0 && isSeekFrame && f.Arc.label != (Term_Renamed.Bytes[f.Prefix - 1] & 0xFF))
				  {
					@out.println("      broken seek state: arc.label=" + (char) f.Arc.label + " vs term byte=" + (char)(Term_Renamed.Bytes[f.Prefix - 1] & 0xFF));
					throw new Exception("seek state is broken");
				  }
				  BytesRef output = Util.Get(outerInstance.Index, prefix);
				  if (output == Lucene.Net.Util.BytesRefIterator_Fields.Null)
				  {
					@out.println("      broken seek state: prefix is not final in index");
					throw new Exception("seek state is broken");
				  }
				  else if (isSeekFrame && !f.IsFloor)
				  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Store.ByteArrayDataInput reader = new Lucene.Net.Store.ByteArrayDataInput(output.bytes, output.offset, output.length);
					ByteArrayDataInput reader = new ByteArrayDataInput(output.Bytes, output.Offset, output.Length);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long codeOrig = reader.readVLong();
					long codeOrig = reader.ReadVLong();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long code = (f.fp << BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) | (f.hasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) | (f.isFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0);
					long code = (f.Fp << BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) | (f.HasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) | (f.IsFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0);
					if (codeOrig != code)
					{
					  @out.println("      broken seek state: output code=" + codeOrig + " doesn't match frame code=" + code);
					  throw new Exception("seek state is broken");
					}
				  }
				}
				if (f == CurrentFrame)
				{
				  break;
				}
				if (f.Prefix == ValidIndexPrefix)
				{
				  isSeekFrame = false;
				}
				ord++;
			  }
			}
		  }

		  /* Decodes only the term bytes of the next term.  If caller then asks for
		     metadata, ie docFreq, totalTermFreq or pulls a D/&PEnum, we then (lazily)
		     decode all metadata up to the current term. */
		  public override BytesRef Next()
		  {

			if (@in == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			{
			  // Fresh TermsEnum; seek to first term:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Util.Fst.FST.Arc<Lucene.Net.Util.BytesRef> arc;
			  FST.Arc<BytesRef> arc;
			  if (outerInstance.Index != Lucene.Net.Util.BytesRefIterator_Fields.Null)
			  {
				arc = outerInstance.Index.getFirstArc(Arcs[0]);
				// Empty string prefix must have an output in the index!
				Debug.Assert(arc.Final);
			  }
			  else
			  {
				arc = Lucene.Net.Util.BytesRefIterator_Fields.Null;
			  }
			  CurrentFrame = PushFrame(arc, outerInstance.RootCode, 0);
			  CurrentFrame.LoadBlock();
			}

			TargetBeforeCurrentLength = CurrentFrame.Ord;

			Debug.Assert(!Eof);
			//if (DEBUG) {
			//System.out.println("\nBTTR.next seg=" + segment + " term=" + brToString(term) + " termExists?=" + termExists + " field=" + fieldInfo.name + " termBlockOrd=" + currentFrame.state.termBlockOrd + " validIndexPrefix=" + validIndexPrefix);
			//printSeekState();
			//}

			if (CurrentFrame == StaticFrame)
			{
			  // If seek was previously called and the term was
			  // cached, or seek(TermState) was called, usually
			  // caller is just going to pull a D/&PEnum or get
			  // docFreq, etc.  But, if they then call next(),
			  // this method catches up all internal state so next()
			  // works properly:
			  //if (DEBUG) System.out.println("  re-seek to pending term=" + term.utf8ToString() + " " + term);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean result = seekExact(term);
			  bool result = SeekExact(Term_Renamed);
			  Debug.Assert(result);
			}

			// Pop finished blocks
			while (CurrentFrame.NextEnt == CurrentFrame.EntCount)
			{
			  if (!CurrentFrame.IsLastInFloor)
			  {
				CurrentFrame.LoadNextFloorBlock();
			  }
			  else
			  {
				//if (DEBUG) System.out.println("  pop frame");
				if (CurrentFrame.Ord == 0)
				{
				  //if (DEBUG) System.out.println("  return null");
				  Debug.Assert(SetEOF());
				  Term_Renamed.Length = 0;
				  ValidIndexPrefix = 0;
				  CurrentFrame.Rewind();
				  TermExists = false;
				  return Lucene.Net.Util.BytesRefIterator_Fields.Null;
				}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long lastFP = currentFrame.fpOrig;
				long lastFP = CurrentFrame.FpOrig;
				CurrentFrame = Stack[CurrentFrame.Ord - 1];

				if (CurrentFrame.NextEnt == -1 || CurrentFrame.LastSubFP != lastFP)
				{
				  // We popped into a frame that's not loaded
				  // yet or not scan'd to the right entry
				  CurrentFrame.ScanToFloorFrame(Term_Renamed);
				  CurrentFrame.LoadBlock();
				  CurrentFrame.ScanToSubBlock(lastFP);
				}

				// Note that the seek state (last seek) has been
				// invalidated beyond this depth
				ValidIndexPrefix = Math.Min(ValidIndexPrefix, CurrentFrame.Prefix);
				//if (DEBUG) {
				//System.out.println("  reset validIndexPrefix=" + validIndexPrefix);
				//}
			  }
			}

			while (true)
			{
			  if (CurrentFrame.Next())
			  {
				// Push to new block:
				//if (DEBUG) System.out.println("  push frame");
				CurrentFrame = PushFrame(Lucene.Net.Util.BytesRefIterator_Fields.Null, CurrentFrame.LastSubFP, Term_Renamed.Length);
				// this is a "next" frame -- even if it's
				// floor'd we must pretend it isn't so we don't
				// try to scan to the right floor frame:
				CurrentFrame.IsFloor = false;
				//currentFrame.hasTerms = true;
				CurrentFrame.LoadBlock();
			  }
			  else
			  {
				//if (DEBUG) System.out.println("  return term=" + term.utf8ToString() + " " + term + " currentFrame.ord=" + currentFrame.ord);
				return Term_Renamed;
			  }
			}
		  }

		  public override BytesRef Term()
		  {
			Debug.Assert(!Eof);
			return Term_Renamed;
		  }

		  public override int DocFreq()
		  {
			Debug.Assert(!Eof);
			//if (DEBUG) System.out.println("BTR.docFreq");
			CurrentFrame.DecodeMetaData();
			//if (DEBUG) System.out.println("  return " + currentFrame.state.docFreq);
			return CurrentFrame.State.docFreq;
		  }

		  public override long TotalTermFreq()
		  {
			Debug.Assert(!Eof);
			CurrentFrame.DecodeMetaData();
			return CurrentFrame.State.totalTermFreq;
		  }

		  public override DocsEnum Docs(Bits skipDocs, DocsEnum reuse, int flags)
		  {
			Debug.Assert(!Eof);
			//if (DEBUG) {
			//System.out.println("BTTR.docs seg=" + segment);
			//}
			CurrentFrame.DecodeMetaData();
			//if (DEBUG) {
			//System.out.println("  state=" + currentFrame.state);
			//}
			return outerInstance.OuterInstance.PostingsReader.docs(outerInstance.FieldInfo, CurrentFrame.State, skipDocs, reuse, flags);
		  }

		  public override DocsAndPositionsEnum DocsAndPositions(Bits skipDocs, DocsAndPositionsEnum reuse, int flags)
		  {
			if (outerInstance.FieldInfo.IndexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
			{
			  // Positions were not indexed:
			  return Lucene.Net.Util.BytesRefIterator_Fields.Null;
			}

			Debug.Assert(!Eof);
			CurrentFrame.DecodeMetaData();
			return outerInstance.OuterInstance.PostingsReader.docsAndPositions(outerInstance.FieldInfo, CurrentFrame.State, skipDocs, reuse, flags);
		  }

		  public override void SeekExact(BytesRef target, TermState otherState)
		  {
			// if (DEBUG) {
			//   System.out.println("BTTR.seekExact termState seg=" + segment + " target=" + target.utf8ToString() + " " + target + " state=" + otherState);
			// }
			Debug.Assert(ClearEOF());
			if (target.CompareTo(Term_Renamed) != 0 || !TermExists)
			{
			  Debug.Assert(otherState != Lucene.Net.Util.BytesRefIterator_Fields.Null && otherState is BlockTermState);
			  CurrentFrame = StaticFrame;
			  CurrentFrame.State.copyFrom(otherState);
			  Term_Renamed.CopyBytes(target);
			  CurrentFrame.MetaDataUpto = CurrentFrame.TermBlockOrd;
			  Debug.Assert(CurrentFrame.MetaDataUpto > 0);
			  ValidIndexPrefix = 0;
			}
			else
			{
			  // if (DEBUG) {
			  //   System.out.println("  skip seek: already on target state=" + currentFrame.state);
			  // }
			}
		  }

		  public override TermState TermState()
		  {
			Debug.Assert(!Eof);
			CurrentFrame.DecodeMetaData();
			TermState ts = CurrentFrame.State.clone();
			//if (DEBUG) System.out.println("BTTR.termState seg=" + segment + " state=" + ts);
			return ts;
		  }

		  public override void SeekExact(long ord)
		  {
			throw new System.NotSupportedException();
		  }

		  public override long Ord()
		  {
			throw new System.NotSupportedException();
		  }

		  // Not static -- references term, postingsReader,
		  // fieldInfo, in
		  private sealed class Frame
		  {
			  private readonly BlockTreeTermsReader.FieldReader.SegmentTermsEnum OuterInstance;

			// Our index in stack[]:
			internal readonly int Ord;

			internal bool HasTerms;
			internal bool HasTermsOrig;
			internal bool IsFloor;

			internal FST.Arc<BytesRef> Arc;

			// File pointer where this block was loaded from
			internal long Fp;
			internal long FpOrig;
			internal long FpEnd;

			internal sbyte[] SuffixBytes = new sbyte[128];
			internal readonly ByteArrayDataInput SuffixesReader = new ByteArrayDataInput();

			internal sbyte[] StatBytes = new sbyte[64];
			internal readonly ByteArrayDataInput StatsReader = new ByteArrayDataInput();

			internal sbyte[] FloorData = new sbyte[32];
			internal readonly ByteArrayDataInput FloorDataReader = new ByteArrayDataInput();

			// Length of prefix shared by all terms in this block
			internal int Prefix;

			// Number of entries (term or sub-block) in this block
			internal int EntCount;

			// Which term we will next read, or -1 if the block
			// isn't loaded yet
			internal int NextEnt;

			// True if this block is either not a floor block,
			// or, it's the last sub-block of a floor block
			internal bool IsLastInFloor;

			// True if all entries are terms
			internal bool IsLeafBlock;

			internal long LastSubFP;

			internal int NextFloorLabel;
			internal int NumFollowFloorBlocks;

			// Next term to decode metaData; we decode metaData
			// lazily so that scanning to find the matching term is
			// fast and only if you find a match and app wants the
			// stats or docs/positions enums, will we decode the
			// metaData
			internal int MetaDataUpto;

			internal readonly BlockTermState State;

			// metadata buffer, holding monotonic values
			public long[] Longs;
			// metadata buffer, holding general values
			public sbyte[] Bytes;
			internal ByteArrayDataInput BytesReader;

			public Frame(BlockTreeTermsReader.FieldReader.SegmentTermsEnum outerInstance, int ord)
			{
				this.OuterInstance = outerInstance;
			  this.Ord = ord;
			  this.State = outerInstance.OuterInstance.OuterInstance.PostingsReader.newTermState();
			  this.State.TotalTermFreq = -1;
			  this.Longs = new long[outerInstance.OuterInstance.LongsSize];
			}

			public void SetFloorData(ByteArrayDataInput @in, BytesRef source)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numBytes = source.length - (in.getPosition() - source.offset);
			  int numBytes = source.Length - (@in.Position - source.Offset);
			  if (numBytes > FloorData.Length)
			  {
				FloorData = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
			  }
			  Array.Copy(source.Bytes, source.Offset + @in.Position, FloorData, 0, numBytes);
			  FloorDataReader.Reset(FloorData, 0, numBytes);
			  NumFollowFloorBlocks = FloorDataReader.ReadVInt();
			  NextFloorLabel = FloorDataReader.ReadByte() & 0xff;
			  //if (DEBUG) {
			  //System.out.println("    setFloorData fpOrig=" + fpOrig + " bytes=" + new BytesRef(source.bytes, source.offset + in.getPosition(), numBytes) + " numFollowFloorBlocks=" + numFollowFloorBlocks + " nextFloorLabel=" + toHex(nextFloorLabel));
			  //}
			}

			public int TermBlockOrd
			{
				get
				{
				  return IsLeafBlock ? NextEnt : State.TermBlockOrd;
				}
			}

			internal void LoadNextFloorBlock()
			{
			  //if (DEBUG) {
			  //System.out.println("    loadNextFloorBlock fp=" + fp + " fpEnd=" + fpEnd);
			  //}
			  Debug.Assert(Arc == Lucene.Net.Util.BytesRefIterator_Fields.Null || IsFloor, "arc=" + Arc + " isFloor=" + IsFloor);
			  Fp = FpEnd;
			  NextEnt = -1;
			  LoadBlock();
			}

			/* Does initial decode of next block of terms; this
			   doesn't actually decode the docFreq, totalTermFreq,
			   postings details (frq/prx offset, etc.) metadata;
			   it just loads them as byte[] blobs which are then      
			   decoded on-demand if the metadata is ever requested
			   for any term in this block.  this enables terms-only
			   intensive consumes (eg certain MTQs, respelling) to
			   not pay the price of decoding metadata they won't
			   use. */
			internal void LoadBlock()
			{

			  // Clone the IndexInput lazily, so that consumers
			  // that just pull a TermsEnum to
			  // seekExact(TermState) don't pay this cost:
			  outerInstance.InitIndexInput();

			  if (NextEnt != -1)
			  {
				// Already loaded
				return;
			  }
			  //System.out.println("blc=" + blockLoadCount);

			  outerInstance.@in.Seek(Fp);
			  int code = outerInstance.@in.ReadVInt();
			  EntCount = (int)((uint)code >> 1);
			  Debug.Assert(EntCount > 0);
			  IsLastInFloor = (code & 1) != 0;
			  Debug.Assert(Arc == Lucene.Net.Util.BytesRefIterator_Fields.Null || (IsLastInFloor || IsFloor));

			  // TODO: if suffixes were stored in random-access
			  // array structure, then we could do binary search
			  // instead of linear scan to find target term; eg
			  // we could have simple array of offsets

			  // term suffixes:
			  code = outerInstance.@in.ReadVInt();
			  IsLeafBlock = (code & 1) != 0;
			  int numBytes = (int)((uint)code >> 1);
			  if (SuffixBytes.Length < numBytes)
			  {
				SuffixBytes = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
			  }
			  outerInstance.@in.ReadBytes(SuffixBytes, 0, numBytes);
			  SuffixesReader.Reset(SuffixBytes, 0, numBytes);

			  /*if (DEBUG) {
			    if (arc == null) {
			      System.out.println("    loadBlock (next) fp=" + fp + " entCount=" + entCount + " prefixLen=" + prefix + " isLastInFloor=" + isLastInFloor + " leaf?=" + isLeafBlock);
			    } else {
			      System.out.println("    loadBlock (seek) fp=" + fp + " entCount=" + entCount + " prefixLen=" + prefix + " hasTerms?=" + hasTerms + " isFloor?=" + isFloor + " isLastInFloor=" + isLastInFloor + " leaf?=" + isLeafBlock);
			    }
			    }*/

			  // stats
			  numBytes = outerInstance.@in.ReadVInt();
			  if (StatBytes.Length < numBytes)
			  {
				StatBytes = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
			  }
			  outerInstance.@in.ReadBytes(StatBytes, 0, numBytes);
			  StatsReader.Reset(StatBytes, 0, numBytes);
			  MetaDataUpto = 0;

			  State.TermBlockOrd = 0;
			  NextEnt = 0;
			  LastSubFP = -1;

			  // TODO: we could skip this if !hasTerms; but
			  // that's rare so won't help much
			  // metadata
			  numBytes = outerInstance.@in.ReadVInt();
			  if (Bytes == Lucene.Net.Util.BytesRefIterator_Fields.Null)
			  {
				Bytes = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
				BytesReader = new ByteArrayDataInput();
			  }
			  else if (Bytes.Length < numBytes)
			  {
				Bytes = new sbyte[ArrayUtil.Oversize(numBytes, 1)];
			  }
			  outerInstance.@in.ReadBytes(Bytes, 0, numBytes);
			  BytesReader.Reset(Bytes, 0, numBytes);


			  // Sub-blocks of a single floor block are always
			  // written one after another -- tail recurse:
			  FpEnd = outerInstance.@in.FilePointer;
			  // if (DEBUG) {
			  //   System.out.println("      fpEnd=" + fpEnd);
			  // }
			}

			internal void Rewind()
			{

			  // Force reload:
			  Fp = FpOrig;
			  NextEnt = -1;
			  HasTerms = HasTermsOrig;
			  if (IsFloor)
			  {
				FloorDataReader.Rewind();
				NumFollowFloorBlocks = FloorDataReader.ReadVInt();
				NextFloorLabel = FloorDataReader.ReadByte() & 0xff;
			  }

			  /*
			  //System.out.println("rewind");
			  // Keeps the block loaded, but rewinds its state:
			  if (nextEnt > 0 || fp != fpOrig) {
			    if (DEBUG) {
			      System.out.println("      rewind frame ord=" + ord + " fpOrig=" + fpOrig + " fp=" + fp + " hasTerms?=" + hasTerms + " isFloor?=" + isFloor + " nextEnt=" + nextEnt + " prefixLen=" + prefix);
			    }
			    if (fp != fpOrig) {
			      fp = fpOrig;
			      nextEnt = -1;
			    } else {
			      nextEnt = 0;
			    }
			    hasTerms = hasTermsOrig;
			    if (isFloor) {
			      floorDataReader.rewind();
			      numFollowFloorBlocks = floorDataReader.readVInt();
			      nextFloorLabel = floorDataReader.readByte() & 0xff;
			    }
			    assert suffixBytes != null;
			    suffixesReader.rewind();
			    assert statBytes != null;
			    statsReader.rewind();
			    metaDataUpto = 0;
			    state.termBlockOrd = 0;
			    // TODO: skip this if !hasTerms?  Then postings
			    // impl wouldn't have to write useless 0 byte
			    postingsReader.resetTermsBlock(fieldInfo, state);
			    lastSubFP = -1;
			  } else if (DEBUG) {
			    System.out.println("      skip rewind fp=" + fp + " fpOrig=" + fpOrig + " nextEnt=" + nextEnt + " ord=" + ord);
			  }
			  */
			}

			public bool Next()
			{
			  return IsLeafBlock ? NextLeaf() : NextNonLeaf();
			}

			// Decodes next entry; returns true if it's a sub-block
			public bool NextLeaf()
			{
			  //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
			  Debug.Assert(NextEnt != -1 && NextEnt < EntCount, "nextEnt=" + NextEnt + " entCount=" + EntCount + " fp=" + Fp);
			  NextEnt++;
			  Suffix = SuffixesReader.ReadVInt();
			  StartBytePos = SuffixesReader.Position;
			  outerInstance.Term_Renamed.Length = Prefix + Suffix;
			  if (outerInstance.Term_Renamed.Bytes.Length < outerInstance.Term_Renamed.Length)
			  {
				outerInstance.Term_Renamed.Grow(outerInstance.Term_Renamed.Length);
			  }
			  SuffixesReader.ReadBytes(outerInstance.Term_Renamed.Bytes, Prefix, Suffix);
			  // A normal term
			  outerInstance.TermExists = true;
			  return false;
			}

			public bool NextNonLeaf()
			{
			  //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
			  Debug.Assert(NextEnt != -1 && NextEnt < EntCount, "nextEnt=" + NextEnt + " entCount=" + EntCount + " fp=" + Fp);
			  NextEnt++;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = suffixesReader.readVInt();
			  int code = SuffixesReader.ReadVInt();
			  Suffix = (int)((uint)code >> 1);
			  StartBytePos = SuffixesReader.Position;
			  outerInstance.Term_Renamed.Length = Prefix + Suffix;
			  if (outerInstance.Term_Renamed.Bytes.Length < outerInstance.Term_Renamed.Length)
			  {
				outerInstance.Term_Renamed.Grow(outerInstance.Term_Renamed.Length);
			  }
			  SuffixesReader.ReadBytes(outerInstance.Term_Renamed.Bytes, Prefix, Suffix);
			  if ((code & 1) == 0)
			  {
				// A normal term
				outerInstance.TermExists = true;
				SubCode = 0;
				State.TermBlockOrd++;
				return false;
			  }
			  else
			  {
				// A sub-block; make sub-FP absolute:
				outerInstance.TermExists = false;
				SubCode = SuffixesReader.ReadVLong();
				LastSubFP = Fp - SubCode;
				//if (DEBUG) {
				//System.out.println("    lastSubFP=" + lastSubFP);
				//}
				return true;
			  }
			}

			// TODO: make this array'd so we can do bin search?
			// likely not worth it?  need to measure how many
			// floor blocks we "typically" get
			public void ScanToFloorFrame(BytesRef target)
			{

			  if (!IsFloor || target.Length <= Prefix)
			  {
				// if (DEBUG) {
				//   System.out.println("    scanToFloorFrame skip: isFloor=" + isFloor + " target.length=" + target.length + " vs prefix=" + prefix);
				// }
				return;
			  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLabel = target.bytes[target.offset + prefix] & 0xFF;
			  int targetLabel = target.Bytes[target.Offset + Prefix] & 0xFF;

			  // if (DEBUG) {
			  //   System.out.println("    scanToFloorFrame fpOrig=" + fpOrig + " targetLabel=" + toHex(targetLabel) + " vs nextFloorLabel=" + toHex(nextFloorLabel) + " numFollowFloorBlocks=" + numFollowFloorBlocks);
			  // }

			  if (targetLabel < NextFloorLabel)
			  {
				// if (DEBUG) {
				//   System.out.println("      already on correct block");
				// }
				return;
			  }

			  Debug.Assert(NumFollowFloorBlocks != 0);

			  long newFP = FpOrig;
			  while (true)
			  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long code = floorDataReader.readVLong();
				long code = FloorDataReader.ReadVLong();
				newFP = FpOrig + ((long)((ulong)code >> 1));
				HasTerms = (code & 1) != 0;
				// if (DEBUG) {
				//   System.out.println("      label=" + toHex(nextFloorLabel) + " fp=" + newFP + " hasTerms?=" + hasTerms + " numFollowFloor=" + numFollowFloorBlocks);
				// }

				IsLastInFloor = NumFollowFloorBlocks == 1;
				NumFollowFloorBlocks--;

				if (IsLastInFloor)
				{
				  NextFloorLabel = 256;
				  // if (DEBUG) {
				  //   System.out.println("        stop!  last block nextFloorLabel=" + toHex(nextFloorLabel));
				  // }
				  break;
				}
				else
				{
				  NextFloorLabel = FloorDataReader.ReadByte() & 0xff;
				  if (targetLabel < NextFloorLabel)
				  {
					// if (DEBUG) {
					//   System.out.println("        stop!  nextFloorLabel=" + toHex(nextFloorLabel));
					// }
					break;
				  }
				}
			  }

			  if (newFP != Fp)
			  {
				// Force re-load of the block:
				// if (DEBUG) {
				//   System.out.println("      force switch to fp=" + newFP + " oldFP=" + fp);
				// }
				NextEnt = -1;
				Fp = newFP;
			  }
			  else
			  {
				// if (DEBUG) {
				//   System.out.println("      stay on same fp=" + newFP);
				// }
			  }
			}

			public void DecodeMetaData()
			{

			  //if (DEBUG) System.out.println("\nBTTR.decodeMetadata seg=" + segment + " mdUpto=" + metaDataUpto + " vs termBlockOrd=" + state.termBlockOrd);

			  // lazily catch up on metadata decode:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int limit = getTermBlockOrd();
			  int limit = TermBlockOrd;
			  bool absolute = MetaDataUpto == 0;
			  Debug.Assert(limit > 0);

			  // TODO: better API would be "jump straight to term=N"???
			  while (MetaDataUpto < limit)
			  {

				// TODO: we could make "tiers" of metadata, ie,
				// decode docFreq/totalTF but don't decode postings
				// metadata; this way caller could get
				// docFreq/totalTF w/o paying decode cost for
				// postings

				// TODO: if docFreq were bulk decoded we could
				// just skipN here:

				// stats
				State.DocFreq = StatsReader.ReadVInt();
				//if (DEBUG) System.out.println("    dF=" + state.docFreq);
				if (outerInstance.OuterInstance.FieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
				{
				  State.TotalTermFreq = State.DocFreq + StatsReader.ReadVLong();
				  //if (DEBUG) System.out.println("    totTF=" + state.totalTermFreq);
				}
				// metadata 
				for (int i = 0; i < outerInstance.OuterInstance.LongsSize; i++)
				{
				  Longs[i] = BytesReader.ReadVLong();
				}
				outerInstance.OuterInstance.OuterInstance.PostingsReader.decodeTerm(Longs, BytesReader, outerInstance.OuterInstance.FieldInfo, State, absolute);

				MetaDataUpto++;
				absolute = false;
			  }
			  State.TermBlockOrd = MetaDataUpto;
			}

			// Used only by assert
			internal bool PrefixMatches(BytesRef target)
			{
			  for (int bytePos = 0;bytePos < Prefix;bytePos++)
			  {
				if (target.Bytes[target.Offset + bytePos] != outerInstance.Term_Renamed.Bytes[bytePos])
				{
				  return false;
				}
			  }

			  return true;
			}

			// Scans to sub-block that has this target fp; only
			// called by next(); NOTE: does not set
			// startBytePos/suffix as a side effect
			public void ScanToSubBlock(long subFP)
			{
			  Debug.Assert(!IsLeafBlock);
			  //if (DEBUG) System.out.println("  scanToSubBlock fp=" + fp + " subFP=" + subFP + " entCount=" + entCount + " lastSubFP=" + lastSubFP);
			  //assert nextEnt == 0;
			  if (LastSubFP == subFP)
			  {
				//if (DEBUG) System.out.println("    already positioned");
				return;
			  }
			  Debug.Assert(subFP < Fp, "fp=" + Fp + " subFP=" + subFP);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long targetSubCode = fp - subFP;
			  long targetSubCode = Fp - subFP;
			  //if (DEBUG) System.out.println("    targetSubCode=" + targetSubCode);
			  while (true)
			  {
				Debug.Assert(NextEnt < EntCount);
				NextEnt++;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = suffixesReader.readVInt();
				int code = SuffixesReader.ReadVInt();
				SuffixesReader.SkipBytes(IsLeafBlock ? code : (int)((uint)code >> 1));
				//if (DEBUG) System.out.println("    " + nextEnt + " (of " + entCount + ") ent isSubBlock=" + ((code&1)==1));
				if ((code & 1) != 0)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long subCode = suffixesReader.readVLong();
				  long subCode = SuffixesReader.ReadVLong();
				  //if (DEBUG) System.out.println("      subCode=" + subCode);
				  if (targetSubCode == subCode)
				  {
					//if (DEBUG) System.out.println("        match!");
					LastSubFP = subFP;
					return;
				  }
				}
				else
				{
				  State.TermBlockOrd++;
				}
			  }
			}

			// NOTE: sets startBytePos/suffix as a side effect
			public SeekStatus ScanToTerm(BytesRef target, bool exactOnly)
			{
			  return IsLeafBlock ? ScanToTermLeaf(target, exactOnly) : ScanToTermNonLeaf(target, exactOnly);
			}

			internal int StartBytePos;
			internal int Suffix;
			internal long SubCode;

			// Target's prefix matches this block's prefix; we
			// scan the entries check if the suffix matches.
			public SeekStatus ScanToTermLeaf(BytesRef target, bool exactOnly)
			{

			  // if (DEBUG) System.out.println("    scanToTermLeaf: block fp=" + fp + " prefix=" + prefix + " nextEnt=" + nextEnt + " (of " + entCount + ") target=" + brToString(target) + " term=" + brToString(term));

			  Debug.Assert(NextEnt != -1);

			  outerInstance.TermExists = true;
			  SubCode = 0;

			  if (NextEnt == EntCount)
			  {
				if (exactOnly)
				{
				  FillTerm();
				}
				return SeekStatus.END;
			  }

			  Debug.Assert(PrefixMatches(target));

			  // Loop over each entry (term or sub-block) in this block:
			  //nextTerm: while(nextEnt < entCount) {
			  while (true)
			  {
				NextEnt++;

				Suffix = SuffixesReader.ReadVInt();

				// if (DEBUG) {
				//   BytesRef suffixBytesRef = new BytesRef();
				//   suffixBytesRef.bytes = suffixBytes;
				//   suffixBytesRef.offset = suffixesReader.getPosition();
				//   suffixBytesRef.length = suffix;
				//   System.out.println("      cycle: term " + (nextEnt-1) + " (of " + entCount + ") suffix=" + brToString(suffixBytesRef));
				// }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLen = prefix + suffix;
				int termLen = Prefix + Suffix;
				StartBytePos = SuffixesReader.Position;
				SuffixesReader.SkipBytes(Suffix);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLimit = target.offset + (target.length < termLen ? target.length : termLen);
				int targetLimit = target.Offset + (target.Length < termLen ? target.Length : termLen);
				int targetPos = target.Offset + Prefix;

				// Loop over bytes in the suffix, comparing to
				// the target
				int bytePos = StartBytePos;
				while (true)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int cmp;
				  int cmp;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean stop;
				  bool stop;
				  if (targetPos < targetLimit)
				  {
					cmp = (SuffixBytes[bytePos++] & 0xFF) - (target.Bytes[targetPos++] & 0xFF);
					stop = false;
				  }
				  else
				  {
					Debug.Assert(targetPos == targetLimit);
					cmp = termLen - target.Length;
					stop = true;
				  }

				  if (cmp < 0)
				  {
					// Current entry is still before the target;
					// keep scanning

					if (NextEnt == EntCount)
					{
					  if (exactOnly)
					  {
						FillTerm();
					  }
					  // We are done scanning this block
					  goto nextTermBreak;
					}
					else
					{
					  goto nextTermContinue;
					}
				  }
				  else if (cmp > 0)
				  {

					// Done!  Current entry is after target --
					// return NOT_FOUND:
					FillTerm();

					if (!exactOnly && !outerInstance.TermExists)
					{
					  // We are on a sub-block, and caller wants
					  // us to position to the next term after
					  // the target, so we must recurse into the
					  // sub-frame(s):
					  outerInstance.CurrentFrame = outerInstance.PushFrame(Lucene.Net.Util.BytesRefIterator_Fields.Null, outerInstance.CurrentFrame.LastSubFP, termLen);
					  outerInstance.CurrentFrame.LoadBlock();
					  while (outerInstance.CurrentFrame.Next())
					  {
						outerInstance.CurrentFrame = outerInstance.PushFrame(Lucene.Net.Util.BytesRefIterator_Fields.Null, outerInstance.CurrentFrame.LastSubFP, outerInstance.Term_Renamed.Length);
						outerInstance.CurrentFrame.LoadBlock();
					  }
					}

					//if (DEBUG) System.out.println("        not found");
					return SeekStatus.NOT_FOUND;
				  }
				  else if (stop)
				  {
					// Exact match!

					// this cannot be a sub-block because we
					// would have followed the index to this
					// sub-block from the start:

					Debug.Assert(outerInstance.TermExists);
					FillTerm();
					//if (DEBUG) System.out.println("        found!");
					return SeekStatus.FOUND;
				  }
				}
				  nextTermContinue:;
			  }
			  nextTermBreak:

			  // It is possible (and OK) that terms index pointed us
			  // at this block, but, we scanned the entire block and
			  // did not find the term to position to.  this happens
			  // when the target is after the last term in the block
			  // (but, before the next term in the index).  EG
			  // target could be foozzz, and terms index pointed us
			  // to the foo* block, but the last term in this block
			  // was fooz (and, eg, first term in the next block will
			  // bee fop).
			  //if (DEBUG) System.out.println("      block end");
			  if (exactOnly)
			  {
				FillTerm();
			  }

			  // TODO: not consistent that in the
			  // not-exact case we don't next() into the next
			  // frame here
			  return SeekStatus.END;
			}

			// Target's prefix matches this block's prefix; we
			// scan the entries check if the suffix matches.
			public SeekStatus ScanToTermNonLeaf(BytesRef target, bool exactOnly)
			{

			  //if (DEBUG) System.out.println("    scanToTermNonLeaf: block fp=" + fp + " prefix=" + prefix + " nextEnt=" + nextEnt + " (of " + entCount + ") target=" + brToString(target) + " term=" + brToString(term));

			  Debug.Assert(NextEnt != -1);

			  if (NextEnt == EntCount)
			  {
				if (exactOnly)
				{
				  FillTerm();
				  outerInstance.TermExists = SubCode == 0;
				}
				return SeekStatus.END;
			  }

			  Debug.Assert(PrefixMatches(target));

			  // Loop over each entry (term or sub-block) in this block:
			  //nextTerm: while(nextEnt < entCount) {
			  while (true)
			  {
				NextEnt++;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int code = suffixesReader.readVInt();
				int code = SuffixesReader.ReadVInt();
				Suffix = (int)((uint)code >> 1);
				// if (DEBUG) {
				//   BytesRef suffixBytesRef = new BytesRef();
				//   suffixBytesRef.bytes = suffixBytes;
				//   suffixBytesRef.offset = suffixesReader.getPosition();
				//   suffixBytesRef.length = suffix;
				//   System.out.println("      cycle: " + ((code&1)==1 ? "sub-block" : "term") + " " + (nextEnt-1) + " (of " + entCount + ") suffix=" + brToString(suffixBytesRef));
				// }

				outerInstance.TermExists = (code & 1) == 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLen = prefix + suffix;
				int termLen = Prefix + Suffix;
				StartBytePos = SuffixesReader.Position;
				SuffixesReader.SkipBytes(Suffix);
				if (outerInstance.TermExists)
				{
				  State.TermBlockOrd++;
				  SubCode = 0;
				}
				else
				{
				  SubCode = SuffixesReader.ReadVLong();
				  LastSubFP = Fp - SubCode;
				}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int targetLimit = target.offset + (target.length < termLen ? target.length : termLen);
				int targetLimit = target.Offset + (target.Length < termLen ? target.Length : termLen);
				int targetPos = target.Offset + Prefix;

				// Loop over bytes in the suffix, comparing to
				// the target
				int bytePos = StartBytePos;
				while (true)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int cmp;
				  int cmp;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean stop;
				  bool stop;
				  if (targetPos < targetLimit)
				  {
					cmp = (SuffixBytes[bytePos++] & 0xFF) - (target.Bytes[targetPos++] & 0xFF);
					stop = false;
				  }
				  else
				  {
					Debug.Assert(targetPos == targetLimit);
					cmp = termLen - target.Length;
					stop = true;
				  }

				  if (cmp < 0)
				  {
					// Current entry is still before the target;
					// keep scanning

					if (NextEnt == EntCount)
					{
					  if (exactOnly)
					  {
						FillTerm();
						//termExists = true;
					  }
					  // We are done scanning this block
					  goto nextTermBreak;
					}
					else
					{
					  goto nextTermContinue;
					}
				  }
				  else if (cmp > 0)
				  {

					// Done!  Current entry is after target --
					// return NOT_FOUND:
					FillTerm();

					if (!exactOnly && !outerInstance.TermExists)
					{
					  // We are on a sub-block, and caller wants
					  // us to position to the next term after
					  // the target, so we must recurse into the
					  // sub-frame(s):
					  outerInstance.CurrentFrame = outerInstance.PushFrame(Lucene.Net.Util.BytesRefIterator_Fields.Null, outerInstance.CurrentFrame.LastSubFP, termLen);
					  outerInstance.CurrentFrame.LoadBlock();
					  while (outerInstance.CurrentFrame.Next())
					  {
						outerInstance.CurrentFrame = outerInstance.PushFrame(Lucene.Net.Util.BytesRefIterator_Fields.Null, outerInstance.CurrentFrame.LastSubFP, outerInstance.Term_Renamed.Length);
						outerInstance.CurrentFrame.LoadBlock();
					  }
					}

					//if (DEBUG) System.out.println("        not found");
					return SeekStatus.NOT_FOUND;
				  }
				  else if (stop)
				  {
					// Exact match!

					// this cannot be a sub-block because we
					// would have followed the index to this
					// sub-block from the start:

					Debug.Assert(outerInstance.TermExists);
					FillTerm();
					//if (DEBUG) System.out.println("        found!");
					return SeekStatus.FOUND;
				  }
				}
				  nextTermContinue:;
			  }
			  nextTermBreak:

			  // It is possible (and OK) that terms index pointed us
			  // at this block, but, we scanned the entire block and
			  // did not find the term to position to.  this happens
			  // when the target is after the last term in the block
			  // (but, before the next term in the index).  EG
			  // target could be foozzz, and terms index pointed us
			  // to the foo* block, but the last term in this block
			  // was fooz (and, eg, first term in the next block will
			  // bee fop).
			  //if (DEBUG) System.out.println("      block end");
			  if (exactOnly)
			  {
				FillTerm();
			  }

			  // TODO: not consistent that in the
			  // not-exact case we don't next() into the next
			  // frame here
			  return SeekStatus.END;
			}

			internal void FillTerm()
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int termLength = prefix + suffix;
			  int termLength = Prefix + Suffix;
			  outerInstance.Term_Renamed.Length = Prefix + Suffix;
			  if (outerInstance.Term_Renamed.Bytes.Length < termLength)
			  {
				outerInstance.Term_Renamed.Grow(termLength);
			  }
			  Array.Copy(SuffixBytes, StartBytePos, outerInstance.Term_Renamed.Bytes, Prefix, Suffix);
			}
		  }
		}
	  }

	  public override long RamBytesUsed()
	  {
		long sizeInByes = ((PostingsReader != null) ? PostingsReader.RamBytesUsed() : 0);
		foreach (FieldReader reader in Fields.Values)
		{
		  sizeInByes += reader.RamBytesUsed();
		}
		return sizeInByes;
	  }

	  public override void CheckIntegrity()
	  {
		if (Version >= BlockTreeTermsWriter.VERSION_CHECKSUM)
		{
		  // term dictionary
		  CodecUtil.ChecksumEntireFile(@in);

		  // postings
		  PostingsReader.CheckIntegrity();
		}
	  }
	}

}