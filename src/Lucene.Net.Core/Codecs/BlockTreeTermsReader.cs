using Lucene.Net.Index;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using IBits = Lucene.Net.Util.IBits;
    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using ByteSequenceOutputs = Lucene.Net.Util.Fst.ByteSequenceOutputs;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using Directory = Lucene.Net.Store.Directory;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using RunAutomaton = Lucene.Net.Util.Automaton.RunAutomaton;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using StringHelper = Lucene.Net.Util.StringHelper;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;
    using Transition = Lucene.Net.Util.Automaton.Transition;

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
        private void InitializeInstanceFields()
        {
            NO_OUTPUT = fstOutputs.NoOutput;
        }

        // Open input to the main terms dict file (_X.tib)
        private readonly IndexInput @in;

        //private static final boolean DEBUG = BlockTreeTermsWriter.DEBUG;

        // Reads the terms dict entries, to gather state to
        // produce DocsEnum on demand
        private readonly PostingsReaderBase postingsReader;

        private readonly SortedDictionary<string, FieldReader> fields = new SortedDictionary<string, FieldReader>();

        /// <summary>
        /// File offset where the directory starts in the terms file. </summary>
        private long dirOffset;

        /// <summary>
        /// File offset where the directory starts in the index file. </summary>
        private long indexDirOffset;

        private string segment;

        private readonly int version;

        /// <summary>
        /// Sole constructor. </summary>
        public BlockTreeTermsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo info, PostingsReaderBase postingsReader, IOContext ioContext, string segmentSuffix, int indexDivisor)
        {
            InitializeInstanceFields();

            this.postingsReader = postingsReader;

            this.segment = info.Name;
            @in = dir.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, BlockTreeTermsWriter.TERMS_EXTENSION), ioContext);

            bool success = false;
            IndexInput indexIn = null;

            try
            {
                version = ReadHeader(@in);
                if (indexDivisor != -1)
                {
                    indexIn = dir.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, BlockTreeTermsWriter.TERMS_INDEX_EXTENSION), ioContext);
                    int indexVersion = ReadIndexHeader(indexIn);
                    if (indexVersion != version)
                    {
                        throw new CorruptIndexException("mixmatched version files: " + @in + "=" + version + "," + indexIn + "=" + indexVersion);
                    }
                }

                // verify
                if (indexIn != null && version >= BlockTreeTermsWriter.VERSION_CHECKSUM)
                {
                    CodecUtil.ChecksumEntireFile(indexIn);
                }

                // Have PostingsReader init itself
                postingsReader.Init(@in);

                // Read per-field details
                SeekDir(@in, dirOffset);
                if (indexDivisor != -1)
                {
                    SeekDir(indexIn, indexDirOffset);
                }

                int numFields = @in.ReadVInt();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + @in + ")");
                }

                for (int i = 0; i < numFields; i++)
                {
                    int field = @in.ReadVInt();
                    long numTerms = @in.ReadVLong();
                    Debug.Assert(numTerms >= 0);
                    int numBytes = @in.ReadVInt();
                    BytesRef rootCode = new BytesRef(new byte[numBytes]);
                    @in.ReadBytes(rootCode.Bytes, 0, numBytes);
                    rootCode.Length = numBytes;
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                    Debug.Assert(fieldInfo != null, "field=" + field);
                    long sumTotalTermFreq = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY ? -1 : @in.ReadVLong();
                    long sumDocFreq = @in.ReadVLong();
                    int docCount = @in.ReadVInt();
                    int longsSize = version >= BlockTreeTermsWriter.VERSION_META_ARRAY ? @in.ReadVInt() : 0;
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
                    long indexStartFP = indexDivisor != -1 ? indexIn.ReadVLong() : 0;

                    if (fields.ContainsKey(fieldInfo.Name))
                    {
                        throw new CorruptIndexException("duplicate field: " + fieldInfo.Name + " (resource=" + @in + ")");
                    }
                    else
                    {
                        fields[fieldInfo.Name] = new FieldReader(this, fieldInfo, numTerms, rootCode, sumTotalTermFreq, sumDocFreq, docCount, indexStartFP, longsSize, indexIn);
                    }
                }
                if (indexDivisor != -1)
                {
                    indexIn.Dispose();
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    // this.Dispose() will close in:
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
                dirOffset = input.ReadLong();
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
                indexDirOffset = input.ReadLong();
            }
            return version;
        }

        /// <summary>
        /// Seek {@code input} to the directory offset. </summary>
        protected internal virtual void SeekDir(IndexInput input, long dirOffset)
        {
            if (version >= BlockTreeTermsWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadLong();
            }
            else if (version >= BlockTreeTermsWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length - 8);
                dirOffset = input.ReadLong();
            }
            input.Seek(dirOffset);
        }

        // for debugging
        // private static String toHex(int v) {
        //   return "0x" + Integer.toHexString(v);
        // }

        public override void Dispose()
        {
            try
            {
                IOUtils.Close(@in, postingsReader);
            }
            finally
            {
                // Clear so refs to terms index is GCable even if
                // app hangs onto us:
                fields.Clear();
            }
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return fields.Keys.GetEnumerator();
        }

        public override Terms Terms(string field)
        {
            Debug.Assert(field != null);
            FieldReader ret;
            fields.TryGetValue(field, out ret);
            return ret;
        }

        public override int Count
        {
            get { return fields.Count; }
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
                catch (Exception)
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
            public long IndexNodeCount { get; set; }

            /// <summary>
            /// How many arcs in the index FST. </summary>
            public long IndexArcCount { get; set; }

            /// <summary>
            /// Byte size of the index. </summary>
            public long IndexNumBytes { get; set; }

            /// <summary>
            /// Total number of terms in the field. </summary>
            public long TotalTermCount { get; set; }

            /// <summary>
            /// Total number of bytes (sum of term lengths) across all terms in the field. </summary>
            public long TotalTermBytes { get; set; }

            /// <summary>
            /// The number of normal (non-floor) blocks in the terms file. </summary>
            public int NonFloorBlockCount { get; set; }

            /// <summary>
            /// The number of floor blocks (meta-blocks larger than the
            ///  allowed {@code maxItemsPerBlock}) in the terms file.
            /// </summary>
            public int FloorBlockCount { get; set; }

            /// <summary>
            /// The number of sub-blocks within the floor blocks. </summary>
            public int FloorSubBlockCount { get; set; }

            /// <summary>
            /// The number of "internal" blocks (that have both
            ///  terms and sub-blocks).
            /// </summary>
            public int MixedBlockCount { get; set; }

            /// <summary>
            /// The number of "leaf" blocks (blocks that have only
            ///  terms).
            /// </summary>
            public int TermsOnlyBlockCount { get; set; }

            /// <summary>
            /// The number of "internal" blocks that do not contain
            ///  terms (have only sub-blocks).
            /// </summary>
            public int SubBlocksOnlyBlockCount { get; set; }

            /// <summary>
            /// Total number of blocks. </summary>
            public int TotalBlockCount { get; set; }

            /// <summary>
            /// Number of blocks at each prefix depth. </summary>
            public int[] BlockCountByPrefixLen = new int[10]; // LUCENENET TODO: Make property ?

            internal int startBlockCount;
            internal int endBlockCount;

            /// <summary>
            /// Total number of bytes used to store term suffixes. </summary>
            public long TotalBlockSuffixBytes { get; set; }

            /// <summary>
            /// Total number of bytes used to store term stats (not
            ///  including what the <seealso cref="PostingsBaseFormat"/>
            ///  stores.
            /// </summary>
            public long TotalBlockStatsBytes { get; set; }

            /// <summary>
            /// Total bytes stored by the <seealso cref="PostingsBaseFormat"/>,
            ///  plus the other few vInts stored in the frame.
            /// </summary>
            public long TotalBlockOtherBytes { get; set; }

            /// <summary>
            /// Segment name. </summary>
            public string Segment { get; private set; }

            /// <summary>
            /// Field name. </summary>
            public string Field { get; private set; }

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
                    if (frame.fp == frame.fpOrig)
                    {
                        FloorBlockCount++;
                    }
                    FloorSubBlockCount++;
                }
                else
                {
                    NonFloorBlockCount++;
                }

                if (BlockCountByPrefixLen.Length <= frame.prefix)
                {
                    BlockCountByPrefixLen = ArrayUtil.Grow(BlockCountByPrefixLen, 1 + frame.prefix);
                }
                BlockCountByPrefixLen[frame.prefix]++;
                startBlockCount++;
                TotalBlockSuffixBytes += frame.suffixesReader.Length;
                TotalBlockStatsBytes += frame.statsReader.Length;
            }

            internal virtual void EndBlock(FieldReader.SegmentTermsEnum.Frame frame)
            {
                int termCount = frame.isLeafBlock ? frame.entCount : frame.state.TermBlockOrd;
                int subBlockCount = frame.entCount - termCount;
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
                    throw new InvalidOperationException();
                }
                endBlockCount++;
                long otherBytes = frame.fpEnd - frame.fp - frame.suffixesReader.Length - frame.statsReader.Length;
                Debug.Assert(otherBytes > 0, "otherBytes=" + otherBytes + " frame.fp=" + frame.fp + " frame.fpEnd=" + frame.fpEnd);
                TotalBlockOtherBytes += otherBytes;
            }

            internal virtual void Term(BytesRef term)
            {
                TotalTermBytes += term.Length;
            }

            internal virtual void Finish()
            {
                Debug.Assert(startBlockCount == endBlockCount, "startBlockCount=" + startBlockCount + " endBlockCount=" + endBlockCount);
                Debug.Assert(TotalBlockCount == FloorSubBlockCount + NonFloorBlockCount, "floorSubBlockCount=" + FloorSubBlockCount + " nonFloorBlockCount=" + NonFloorBlockCount + " totalBlockCount=" + TotalBlockCount);
                Debug.Assert(TotalBlockCount == MixedBlockCount + TermsOnlyBlockCount + SubBlocksOnlyBlockCount, "totalBlockCount=" + TotalBlockCount + " mixedBlockCount=" + MixedBlockCount + " subBlocksOnlyBlockCount=" + SubBlocksOnlyBlockCount + " termsOnlyBlockCount=" + TermsOnlyBlockCount);
            }

            public override string ToString()
            {
                StringBuilder @out = new StringBuilder();

                /* LUCENE TO-DO I don't think this is neccesary
                try
                {
                  @out = new PrintStream(bos, false, IOUtils.UTF_8);
                }
                catch (UnsupportedEncodingException bogus)
                {
                  throw new Exception(bogus);
                }*/

                @out.AppendLine("  index FST:");
                @out.AppendLine("    " + IndexNodeCount + " nodes");
                @out.AppendLine("    " + IndexArcCount + " arcs");
                @out.AppendLine("    " + IndexNumBytes + " bytes");
                @out.AppendLine("  terms:");
                @out.AppendLine("    " + TotalTermCount + " terms");
                @out.AppendLine("    " + TotalTermBytes + " bytes" + (TotalTermCount != 0 ? " (" + ((double)TotalTermBytes / TotalTermCount).ToString("0.0") + " bytes/term)" : ""));
                @out.AppendLine("  blocks:");
                @out.AppendLine("    " + TotalBlockCount + " blocks");
                @out.AppendLine("    " + TermsOnlyBlockCount + " terms-only blocks");
                @out.AppendLine("    " + SubBlocksOnlyBlockCount + " sub-block-only blocks");
                @out.AppendLine("    " + MixedBlockCount + " mixed blocks");
                @out.AppendLine("    " + FloorBlockCount + " floor blocks");
                @out.AppendLine("    " + (TotalBlockCount - FloorSubBlockCount) + " non-floor blocks");
                @out.AppendLine("    " + FloorSubBlockCount + " floor sub-blocks");
                @out.AppendLine("    " + TotalBlockSuffixBytes + " term suffix bytes" + (TotalBlockCount != 0 ? " (" + ((double)TotalBlockSuffixBytes / TotalBlockCount).ToString("0.0") + " suffix-bytes/block)" : ""));
                @out.AppendLine("    " + TotalBlockStatsBytes + " term stats bytes" + (TotalBlockCount != 0 ? " (" + ((double)TotalBlockStatsBytes / TotalBlockCount).ToString("0.0") + " stats-bytes/block)" : ""));
                @out.AppendLine("    " + TotalBlockOtherBytes + " other bytes" + (TotalBlockCount != 0 ? " (" + ((double)TotalBlockOtherBytes / TotalBlockCount).ToString("0.0") + " other-bytes/block)" : ""));
                if (TotalBlockCount != 0)
                {
                    @out.AppendLine("    by prefix length:");
                    int total = 0;
                    for (int prefix = 0; prefix < BlockCountByPrefixLen.Length; prefix++)
                    {
                        int blockCount = BlockCountByPrefixLen[prefix];
                        total += blockCount;
                        if (blockCount != 0)
                        {
                            @out.AppendLine("      " + prefix.ToString().PadLeft(2, ' ') + ": " + blockCount);
                        }
                    }
                    Debug.Assert(TotalBlockCount == total);
                }
                return @out.ToString();
                /* LUCENE TO-DO I dont think this is neccesary
                try
                {
                  return bos.ToString(IOUtils.UTF_8);
                }
                catch (UnsupportedEncodingException bogus)
                {
                  throw new Exception(bogus);
                }*/
            }
        }

        internal readonly Outputs<BytesRef> fstOutputs = ByteSequenceOutputs.Singleton;
        internal BytesRef NO_OUTPUT;

        /// <summary>
        /// BlockTree's implementation of <seealso cref="Terms"/>. </summary>
        public sealed class FieldReader : Terms
        {
            private readonly BlockTreeTermsReader outerInstance;

            internal readonly long numTerms;
            internal readonly FieldInfo fieldInfo;
            internal readonly long sumTotalTermFreq;
            internal readonly long sumDocFreq;
            internal readonly int docCount;
            internal readonly long indexStartFP;
            internal readonly long rootBlockFP;
            internal readonly BytesRef rootCode;
            internal readonly int longsSize;

            internal readonly FST<BytesRef> index;
            //private boolean DEBUG;

            internal FieldReader(BlockTreeTermsReader outerInstance, FieldInfo fieldInfo, long numTerms, BytesRef rootCode, long sumTotalTermFreq, long sumDocFreq, int docCount, long indexStartFP, int longsSize, IndexInput indexIn)
            {
                this.outerInstance = outerInstance;
                Debug.Assert(numTerms > 0);
                this.fieldInfo = fieldInfo;
                //DEBUG = BlockTreeTermsReader.DEBUG && fieldInfo.name.equals("id");
                this.numTerms = numTerms;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
                this.indexStartFP = indexStartFP;
                this.rootCode = rootCode;
                this.longsSize = longsSize;
                // if (DEBUG) {
                //   System.out.println("BTTR: seg=" + segment + " field=" + fieldInfo.name + " rootBlockCode=" + rootCode + " divisor=" + indexDivisor);
                // }

                rootBlockFP = (int)((uint)(new ByteArrayDataInput((byte[])(Array)rootCode.Bytes, rootCode.Offset, rootCode.Length)).ReadVLong() >> BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS);

                if (indexIn != null)
                {
                    IndexInput clone = (IndexInput)indexIn.Clone();
                    //System.out.println("start=" + indexStartFP + " field=" + fieldInfo.name);
                    clone.Seek(indexStartFP);
                    index = new FST<BytesRef>(clone, ByteSequenceOutputs.Singleton);

                    /*
                    if (false) {
                      final String dotFileName = segment + "_" + fieldInfo.name + ".dot";
                      Writer w = new OutputStreamWriter(new FileOutputStream(dotFileName));
                      Util.toDot(index, w, false, false);
                      System.out.println("FST INDEX: SAVED to " + dotFileName);
                      w.Dispose();
                    }
                    */
                }
                else
                {
                    index = null;
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
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override bool HasFreqs
            {
                get { return fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS; }
            }

            public override bool HasOffsets
            {
                get { return fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS; }
            }

            public override bool HasPositions
            {
                get { return fieldInfo.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS; }
            }

            public override bool HasPayloads
            {
                get { return fieldInfo.HasPayloads; }
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return new SegmentTermsEnum(this);
            }

            public override long Count
            {
                get { return numTerms; }
            }

            public override long SumTotalTermFreq
            {
                get
                {
                    return sumTotalTermFreq;
                }
            }

            public override long SumDocFreq
            {
                get
                {
                    return sumDocFreq;
                }
            }

            public override int DocCount
            {
                get
                {
                    return docCount;
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
                return ((index != null) ? index.SizeInBytes() : 0);
            }

            // NOTE: cannot seek!
            private sealed class IntersectEnum : TermsEnum
            {
                private readonly BlockTreeTermsReader.FieldReader outerInstance;

                private readonly IndexInput @in;

                private Frame[] stack;

                private FST.Arc<BytesRef>[] arcs = new FST.Arc<BytesRef>[5];

                private readonly RunAutomaton runAutomaton;
                private readonly CompiledAutomaton compiledAutomaton;

                private Frame currentFrame;

                private readonly BytesRef term = new BytesRef();

                private readonly FST.BytesReader fstReader;

                // TODO: can we share this with the frame in STE?
                private sealed class Frame
                {
                    private readonly BlockTreeTermsReader.FieldReader.IntersectEnum outerInstance;

                    internal readonly int ord;
                    internal long fp;
                    internal long fpOrig;
                    internal long fpEnd;
                    internal long lastSubFP;

                    // State in automaton
                    internal int state;

                    internal int metaDataUpto;

                    internal byte[] suffixBytes = new byte[128];
                    internal readonly ByteArrayDataInput suffixesReader = new ByteArrayDataInput();

                    internal byte[] statBytes = new byte[64];
                    internal readonly ByteArrayDataInput statsReader = new ByteArrayDataInput();

                    internal byte[] floorData = new byte[32];
                    internal readonly ByteArrayDataInput floorDataReader = new ByteArrayDataInput();

                    // Length of prefix shared by all terms in this block
                    internal int prefix;

                    // Number of entries (term or sub-block) in this block
                    internal int entCount;

                    // Which term we will next read
                    internal int nextEnt;

                    // True if this block is either not a floor block,
                    // or, it's the last sub-block of a floor block
                    internal bool isLastInFloor;

                    // True if all entries are terms
                    internal bool isLeafBlock;

                    internal int numFollowFloorBlocks;
                    internal int nextFloorLabel;

                    internal Transition[] transitions;
                    internal int curTransitionMax;
                    internal int transitionIndex;

                    internal FST.Arc<BytesRef> arc;

                    internal readonly BlockTermState termState;

                    // metadata buffer, holding monotonic values
                    public long[] longs; 

                    // metadata buffer, holding general values
                    public byte[] bytes;

                    internal ByteArrayDataInput bytesReader;

                    // Cumulative output so far
                    internal BytesRef outputPrefix;

                    internal int startBytePos;
                    internal int suffix;

                    public Frame(BlockTreeTermsReader.FieldReader.IntersectEnum outerInstance, int ord)
                    {
                        this.outerInstance = outerInstance;
                        this.ord = ord;
                        this.termState = outerInstance.outerInstance.outerInstance.postingsReader.NewTermState();
                        this.termState.TotalTermFreq = -1;
                        this.longs = new long[outerInstance.outerInstance.longsSize];
                    }

                    internal void LoadNextFloorBlock()
                    {
                        Debug.Assert(numFollowFloorBlocks > 0);
                        //if (DEBUG) System.out.println("    loadNextFoorBlock trans=" + transitions[transitionIndex]);

                        do
                        {
                            fp = fpOrig + ((int)((uint)floorDataReader.ReadVLong() >> 1));
                            numFollowFloorBlocks--;
                            // if (DEBUG) System.out.println("    skip floor block2!  nextFloorLabel=" + (char) nextFloorLabel + " vs target=" + (char) transitions[transitionIndex].getMin() + " newFP=" + fp + " numFollowFloorBlocks=" + numFollowFloorBlocks);
                            if (numFollowFloorBlocks != 0)
                            {
                                nextFloorLabel = floorDataReader.ReadByte() & 0xff;
                            }
                            else
                            {
                                nextFloorLabel = 256;
                            }
                            // if (DEBUG) System.out.println("    nextFloorLabel=" + (char) nextFloorLabel);
                        } while (numFollowFloorBlocks != 0 && nextFloorLabel <= transitions[transitionIndex].Min);

                        Load(null);
                    }

                    public void SetState(int state)
                    {
                        this.state = state;
                        transitionIndex = 0;
                        transitions = outerInstance.compiledAutomaton.SortedTransitions[state];
                        if (transitions.Length != 0)
                        {
                            curTransitionMax = transitions[0].Max;
                        }
                        else
                        {
                            curTransitionMax = -1;
                        }
                    }

                    internal void Load(BytesRef frameIndexData)
                    {
                        // if (DEBUG) System.out.println("    load fp=" + fp + " fpOrig=" + fpOrig + " frameIndexData=" + frameIndexData + " trans=" + (transitions.length != 0 ? transitions[0] : "n/a" + " state=" + state));

                        if (frameIndexData != null && transitions.Length != 0)
                        {
                            // Floor frame
                            if (floorData.Length < frameIndexData.Length)
                            {
                                this.floorData = new byte[ArrayUtil.Oversize(frameIndexData.Length, 1)];
                            }
                            System.Buffer.BlockCopy(frameIndexData.Bytes, frameIndexData.Offset, floorData, 0, frameIndexData.Length);
                            floorDataReader.Reset(floorData, 0, frameIndexData.Length);
                            // Skip first long -- has redundant fp, hasTerms
                            // flag, isFloor flag
                            long code = floorDataReader.ReadVLong();
                            if ((code & BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR) != 0)
                            {
                                numFollowFloorBlocks = floorDataReader.ReadVInt();
                                nextFloorLabel = floorDataReader.ReadByte() & 0xff;
                                // if (DEBUG) System.out.println("    numFollowFloorBlocks=" + numFollowFloorBlocks + " nextFloorLabel=" + nextFloorLabel);

                                // If current state is accept, we must process
                                // first block in case it has empty suffix:
                                if (outerInstance.runAutomaton.IsAccept(state))
                                {
                                    // Maybe skip floor blocks:
                                    while (numFollowFloorBlocks != 0 && nextFloorLabel <= transitions[0].Min)
                                    {
                                        fp = fpOrig + ((int)((uint)floorDataReader.ReadVLong() >> 1));
                                        numFollowFloorBlocks--;
                                        // if (DEBUG) System.out.println("    skip floor block!  nextFloorLabel=" + (char) nextFloorLabel + " vs target=" + (char) transitions[0].getMin() + " newFP=" + fp + " numFollowFloorBlocks=" + numFollowFloorBlocks);
                                        if (numFollowFloorBlocks != 0)
                                        {
                                            nextFloorLabel = floorDataReader.ReadByte() & 0xff;
                                        }
                                        else
                                        {
                                            nextFloorLabel = 256;
                                        }
                                    }
                                }
                            }
                        }

                        outerInstance.@in.Seek(fp);
                        int code_ = outerInstance.@in.ReadVInt();
                        entCount = (int)((uint)code_ >> 1);
                        Debug.Assert(entCount > 0);
                        isLastInFloor = (code_ & 1) != 0;

                        // term suffixes:
                        code_ = outerInstance.@in.ReadVInt();
                        isLeafBlock = (code_ & 1) != 0;
                        int numBytes = (int)((uint)code_ >> 1);
                        // if (DEBUG) System.out.println("      entCount=" + entCount + " lastInFloor?=" + isLastInFloor + " leafBlock?=" + isLeafBlock + " numSuffixBytes=" + numBytes);
                        if (suffixBytes.Length < numBytes)
                        {
                            suffixBytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        outerInstance.@in.ReadBytes(suffixBytes, 0, numBytes);
                        suffixesReader.Reset(suffixBytes, 0, numBytes);

                        // stats
                        numBytes = outerInstance.@in.ReadVInt();
                        if (statBytes.Length < numBytes)
                        {
                            statBytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        outerInstance.@in.ReadBytes(statBytes, 0, numBytes);
                        statsReader.Reset(statBytes, 0, numBytes);
                        metaDataUpto = 0;

                        termState.TermBlockOrd = 0;
                        nextEnt = 0;

                        // metadata
                        numBytes = outerInstance.@in.ReadVInt();
                        if (bytes == null)
                        {
                            bytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                            bytesReader = new ByteArrayDataInput();
                        }
                        else if (bytes.Length < numBytes)
                        {
                            bytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        outerInstance.@in.ReadBytes(bytes, 0, numBytes);
                        bytesReader.Reset(bytes, 0, numBytes);

                        if (!isLastInFloor)
                        {
                            // Sub-blocks of a single floor block are always
                            // written one after another -- tail recurse:
                            fpEnd = outerInstance.@in.FilePointer;
                        }
                    }

                    // TODO: maybe add scanToLabel; should give perf boost

                    public bool Next()
                    {
                        return isLeafBlock ? NextLeaf() : NextNonLeaf();
                    }

                    // Decodes next entry; returns true if it's a sub-block
                    public bool NextLeaf()
                    {
                        //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
                        Debug.Assert(nextEnt != -1 && nextEnt < entCount, "nextEnt=" + nextEnt + " entCount=" + entCount + " fp=" + fp);
                        nextEnt++;
                        suffix = suffixesReader.ReadVInt();
                        startBytePos = suffixesReader.Position;
                        suffixesReader.SkipBytes(suffix);
                        return false;
                    }

                    public bool NextNonLeaf()
                    {
                        //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
                        Debug.Assert(nextEnt != -1 && nextEnt < entCount, "nextEnt=" + nextEnt + " entCount=" + entCount + " fp=" + fp);
                        nextEnt++;
                        int code = suffixesReader.ReadVInt();
                        suffix = (int)((uint)code >> 1);
                        startBytePos = suffixesReader.Position;
                        suffixesReader.SkipBytes(suffix);
                        if ((code & 1) == 0)
                        {
                            // A normal term
                            termState.TermBlockOrd++;
                            return false;
                        }
                        else
                        {
                            // A sub-block; make sub-FP absolute:
                            lastSubFP = fp - suffixesReader.ReadVLong();
                            return true;
                        }
                    }

                    public int TermBlockOrd
                    {
                        get
                        {
                            return isLeafBlock ? nextEnt : termState.TermBlockOrd;
                        }
                    }

                    public void DecodeMetaData()
                    {
                        // lazily catch up on metadata decode:
                        int limit = TermBlockOrd;
                        bool absolute = metaDataUpto == 0;
                        Debug.Assert(limit > 0);

                        // TODO: better API would be "jump straight to term=N"???
                        while (metaDataUpto < limit)
                        {
                            // TODO: we could make "tiers" of metadata, ie,
                            // decode docFreq/totalTF but don't decode postings
                            // metadata; this way caller could get
                            // docFreq/totalTF w/o paying decode cost for
                            // postings

                            // TODO: if docFreq were bulk decoded we could
                            // just skipN here:

                            // stats
                            termState.DocFreq = statsReader.ReadVInt();
                            //if (DEBUG) System.out.println("    dF=" + state.docFreq);
                            if (outerInstance.outerInstance.fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                            {
                                termState.TotalTermFreq = termState.DocFreq + statsReader.ReadVLong();
                                //if (DEBUG) System.out.println("    totTF=" + state.totalTermFreq);
                            }
                            // metadata
                            for (int i = 0; i < outerInstance.outerInstance.longsSize; i++)
                            {
                                longs[i] = bytesReader.ReadVLong();
                            }
                            outerInstance.outerInstance.outerInstance.postingsReader.DecodeTerm(longs, bytesReader, outerInstance.outerInstance.fieldInfo, termState, absolute);

                            metaDataUpto++;
                            absolute = false;
                        }
                        termState.TermBlockOrd = metaDataUpto;
                    }
                }

                private BytesRef savedStartTerm;

                // TODO: in some cases we can filter by length?  eg
                // regexp foo*bar must be at least length 6 bytes
                public IntersectEnum(BlockTreeTermsReader.FieldReader outerInstance, CompiledAutomaton compiled, BytesRef startTerm)
                {
                    this.outerInstance = outerInstance;
                    // if (DEBUG) {
                    //   System.out.println("\nintEnum.init seg=" + segment + " commonSuffix=" + brToString(compiled.commonSuffixRef));
                    // }
                    runAutomaton = compiled.RunAutomaton;
                    compiledAutomaton = compiled;
                    @in = (IndexInput)outerInstance.outerInstance.@in.Clone();
                    stack = new Frame[5];
                    for (int idx = 0; idx < stack.Length; idx++)
                    {
                        stack[idx] = new Frame(this, idx);
                    }
                    for (int arcIdx = 0; arcIdx < arcs.Length; arcIdx++)
                    {
                        arcs[arcIdx] = new FST.Arc<BytesRef>();
                    }

                    if (outerInstance.index == null)
                    {
                        fstReader = null;
                    }
                    else
                    {
                        fstReader = outerInstance.index.GetBytesReader();
                    }

                    // TODO: if the automaton is "smallish" we really
                    // should use the terms index to seek at least to
                    // the initial term and likely to subsequent terms
                    // (or, maybe just fallback to ATE for such cases).
                    // Else the seek cost of loading the frames will be
                    // too costly.

                    FST.Arc<BytesRef> arc = outerInstance.index.GetFirstArc(arcs[0]);
                    // Empty string prefix must have an output in the index!
                    Debug.Assert(arc.IsFinal);

                    // Special pushFrame since it's the first one:
                    Frame f = stack[0];
                    f.fp = f.fpOrig = outerInstance.rootBlockFP;
                    f.prefix = 0;
                    f.SetState(runAutomaton.InitialState);
                    f.arc = arc;
                    f.outputPrefix = arc.Output;
                    f.Load(outerInstance.rootCode);

                    // for assert:
                    Debug.Assert(SetSavedStartTerm(startTerm));

                    currentFrame = f;
                    if (startTerm != null)
                    {
                        SeekToStartTerm(startTerm);
                    }
                }

                // only for assert:
                internal bool SetSavedStartTerm(BytesRef startTerm)
                {
                    savedStartTerm = startTerm == null ? null : BytesRef.DeepCopyOf(startTerm);
                    return true;
                }

                public override TermState GetTermState()
                {
                    currentFrame.DecodeMetaData();
                    return (TermState)currentFrame.termState.Clone();
                }

                private Frame GetFrame(int ord)
                {
                    if (ord >= stack.Length)
                    {
                        Frame[] next = new Frame[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        Array.Copy(stack, 0, next, 0, stack.Length);
                        for (int stackOrd = stack.Length; stackOrd < next.Length; stackOrd++)
                        {
                            next[stackOrd] = new Frame(this, stackOrd);
                        }
                        stack = next;
                    }
                    Debug.Assert(stack[ord].ord == ord);
                    return stack[ord];
                }

                private FST.Arc<BytesRef> GetArc(int ord)
                {
                    if (ord >= arcs.Length)
                    {
                        FST.Arc<BytesRef>[] next = new FST.Arc<BytesRef>[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        Array.Copy(arcs, 0, next, 0, arcs.Length);
                        for (int arcOrd = arcs.Length; arcOrd < next.Length; arcOrd++)
                        {
                            next[arcOrd] = new FST.Arc<BytesRef>();
                        }
                        arcs = next;
                    }
                    return arcs[ord];
                }

                private Frame PushFrame(int state)
                {
                    Frame f = GetFrame(currentFrame == null ? 0 : 1 + currentFrame.ord);

                    f.fp = f.fpOrig = currentFrame.lastSubFP;
                    f.prefix = currentFrame.prefix + currentFrame.suffix;
                    // if (DEBUG) System.out.println("    pushFrame state=" + state + " prefix=" + f.prefix);
                    f.SetState(state);

                    // Walk the arc through the index -- we only
                    // "bother" with this so we can get the floor data
                    // from the index and skip floor blocks when
                    // possible:
                    FST.Arc<BytesRef> arc = currentFrame.arc;
                    int idx = currentFrame.prefix;
                    Debug.Assert(currentFrame.suffix > 0);
                    BytesRef output = currentFrame.outputPrefix;
                    while (idx < f.prefix)
                    {
                        int target = term.Bytes[idx] & 0xff;
                        // TODO: we could be more efficient for the next()
                        // case by using current arc as starting point,
                        // passed to findTargetArc
                        arc = outerInstance.index.FindTargetArc(target, arc, GetArc(1 + idx), fstReader);
                        Debug.Assert(arc != null);
                        output = outerInstance.outerInstance.fstOutputs.Add(output, arc.Output);
                        idx++;
                    }

                    f.arc = arc;
                    f.outputPrefix = output;
                    Debug.Assert(arc.IsFinal);
                    f.Load(outerInstance.outerInstance.fstOutputs.Add(output, arc.NextFinalOutput));
                    return f;
                }

                public override BytesRef Term
                {
                    get { return term; }
                }

                public override int DocFreq
                {
                    get
                    {
                        //if (DEBUG) System.out.println("BTIR.docFreq");
                        currentFrame.DecodeMetaData();
                        //if (DEBUG) System.out.println("  return " + currentFrame.termState.docFreq);
                        return currentFrame.termState.DocFreq;
                    }
                }

                public override long TotalTermFreq
                {
                    get
                    {
                        currentFrame.DecodeMetaData();
                        return currentFrame.termState.TotalTermFreq;
                    }
                }

                public override DocsEnum Docs(IBits skipDocs, DocsEnum reuse, int flags)
                {
                    currentFrame.DecodeMetaData();
                    return outerInstance.outerInstance.postingsReader.Docs(outerInstance.fieldInfo, currentFrame.termState, skipDocs, reuse, flags);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits skipDocs, DocsAndPositionsEnum reuse, int flags)
                {
                    if (outerInstance.fieldInfo.IndexOptions < IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        // Positions were not indexed:
                        return null;
                    }

                    currentFrame.DecodeMetaData();
                    return outerInstance.outerInstance.postingsReader.DocsAndPositions(outerInstance.fieldInfo, currentFrame.termState, skipDocs, reuse, flags);
                }

                private int GetState()
                {
                    int state = currentFrame.state;
                    for (int idx = 0; idx < currentFrame.suffix; idx++)
                    {
                        state = runAutomaton.Step(state, currentFrame.suffixBytes[currentFrame.startBytePos + idx] & 0xff);
                        Debug.Assert(state != -1);
                    }
                    return state;
                }

                // NOTE: specialized to only doing the first-time
                // seek, but we could generalize it to allow
                // arbitrary seekExact/Ceil.  Note that this is a
                // seekFloor!
                private void SeekToStartTerm(BytesRef target)
                {
                    //if (DEBUG) System.out.println("seek to startTerm=" + target.utf8ToString());
                    Debug.Assert(currentFrame.ord == 0);
                    if (term.Length < target.Length)
                    {
                        term.Bytes = ArrayUtil.Grow(term.Bytes, target.Length);
                    }
                    FST.Arc<BytesRef> arc = arcs[0];
                    Debug.Assert(arc == currentFrame.arc);

                    for (int idx = 0; idx <= target.Length; idx++)
                    {
                        while (true)
                        {
                            int savePos = currentFrame.suffixesReader.Position;
                            int saveStartBytePos = currentFrame.startBytePos;
                            int saveSuffix = currentFrame.suffix;
                            long saveLastSubFP = currentFrame.lastSubFP;
                            int saveTermBlockOrd = currentFrame.termState.TermBlockOrd;

                            bool isSubBlock = currentFrame.Next();

                            //if (DEBUG) System.out.println("    cycle ent=" + currentFrame.nextEnt + " (of " + currentFrame.entCount + ") prefix=" + currentFrame.prefix + " suffix=" + currentFrame.suffix + " isBlock=" + isSubBlock + " firstLabel=" + (currentFrame.suffix == 0 ? "" : (currentFrame.suffixBytes[currentFrame.startBytePos])&0xff));
                            term.Length = currentFrame.prefix + currentFrame.suffix;
                            if (term.Bytes.Length < term.Length)
                            {
                                term.Bytes = ArrayUtil.Grow(term.Bytes, term.Length);
                            }
                            System.Buffer.BlockCopy(currentFrame.suffixBytes, currentFrame.startBytePos, term.Bytes, currentFrame.prefix, currentFrame.suffix);

                            if (isSubBlock && StringHelper.StartsWith(target, term))
                            {
                                // Recurse
                                currentFrame = PushFrame(GetState());
                                break;
                            }
                            else
                            {
                                int cmp = term.CompareTo(target);
                                if (cmp < 0)
                                {
                                    if (currentFrame.nextEnt == currentFrame.entCount)
                                    {
                                        if (!currentFrame.isLastInFloor)
                                        {
                                            //if (DEBUG) System.out.println("  load floorBlock");
                                            currentFrame.LoadNextFloorBlock();
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
                                    currentFrame.nextEnt--;
                                    currentFrame.lastSubFP = saveLastSubFP;
                                    currentFrame.startBytePos = saveStartBytePos;
                                    currentFrame.suffix = saveSuffix;
                                    currentFrame.suffixesReader.Position = savePos;
                                    currentFrame.termState.TermBlockOrd = saveTermBlockOrd;
                                    System.Buffer.BlockCopy(currentFrame.suffixBytes, currentFrame.startBytePos, term.Bytes, currentFrame.prefix, currentFrame.suffix);
                                    term.Length = currentFrame.prefix + currentFrame.suffix;
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
                        while (currentFrame.nextEnt == currentFrame.entCount)
                        {
                            if (!currentFrame.isLastInFloor)
                            {
                                //if (DEBUG) System.out.println("    next-floor-block");
                                currentFrame.LoadNextFloorBlock();
                                //if (DEBUG) System.out.println("\n  frame ord=" + currentFrame.ord + " prefix=" + brToString(new BytesRef(term.bytes, term.offset, currentFrame.prefix)) + " state=" + currentFrame.state + " lastInFloor?=" + currentFrame.isLastInFloor + " fp=" + currentFrame.fp + " trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]) + " outputPrefix=" + currentFrame.outputPrefix);
                            }
                            else
                            {
                                //if (DEBUG) System.out.println("  pop frame");
                                if (currentFrame.ord == 0)
                                {
                                    return null;
                                }
                                long lastFP = currentFrame.fpOrig;
                                currentFrame = stack[currentFrame.ord - 1];
                                Debug.Assert(currentFrame.lastSubFP == lastFP);
                                //if (DEBUG) System.out.println("\n  frame ord=" + currentFrame.ord + " prefix=" + brToString(new BytesRef(term.bytes, term.offset, currentFrame.prefix)) + " state=" + currentFrame.state + " lastInFloor?=" + currentFrame.isLastInFloor + " fp=" + currentFrame.fp + " trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]) + " outputPrefix=" + currentFrame.outputPrefix);
                            }
                        }

                        bool isSubBlock = currentFrame.Next();
                        // if (DEBUG) {
                        //   final BytesRef suffixRef = new BytesRef();
                        //   suffixRef.bytes = currentFrame.suffixBytes;
                        //   suffixRef.offset = currentFrame.startBytePos;
                        //   suffixRef.length = currentFrame.suffix;
                        //   System.out.println("    " + (isSubBlock ? "sub-block" : "term") + " " + currentFrame.nextEnt + " (of " + currentFrame.entCount + ") suffix=" + brToString(suffixRef));
                        // }

                        if (currentFrame.suffix != 0)
                        {
                            int label = currentFrame.suffixBytes[currentFrame.startBytePos] & 0xff;
                            while (label > currentFrame.curTransitionMax)
                            {
                                if (currentFrame.transitionIndex >= currentFrame.transitions.Length - 1)
                                {
                                    // Stop processing this frame -- no further
                                    // matches are possible because we've moved
                                    // beyond what the max transition will allow
                                    //if (DEBUG) System.out.println("      break: trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]));

                                    // sneaky!  forces a pop above
                                    currentFrame.isLastInFloor = true;
                                    currentFrame.nextEnt = currentFrame.entCount;
                                    goto nextTermContinue;
                                }
                                currentFrame.transitionIndex++;
                                currentFrame.curTransitionMax = currentFrame.transitions[currentFrame.transitionIndex].Max;
                                //if (DEBUG) System.out.println("      next trans=" + currentFrame.transitions[currentFrame.transitionIndex]);
                            }
                        }

                        // First test the common suffix, if set:
                        if (compiledAutomaton.CommonSuffixRef != null && !isSubBlock)
                        {
                            int termLen = currentFrame.prefix + currentFrame.suffix;
                            if (termLen < compiledAutomaton.CommonSuffixRef.Length)
                            {
                                // No match
                                // if (DEBUG) {
                                //   System.out.println("      skip: common suffix length");
                                // }
                                goto nextTermContinue;
                            }

                            byte[] suffixBytes = currentFrame.suffixBytes;
                            byte[] commonSuffixBytes = (byte[])(Array)compiledAutomaton.CommonSuffixRef.Bytes;

                            int lenInPrefix = compiledAutomaton.CommonSuffixRef.Length - currentFrame.suffix;
                            Debug.Assert(compiledAutomaton.CommonSuffixRef.Offset == 0);
                            int suffixBytesPos;
                            int commonSuffixBytesPos = 0;

                            if (lenInPrefix > 0)
                            {
                                // A prefix of the common suffix overlaps with
                                // the suffix of the block prefix so we first
                                // test whether the prefix part matches:
                                byte[] termBytes = term.Bytes;
                                int termBytesPos = currentFrame.prefix - lenInPrefix;
                                Debug.Assert(termBytesPos >= 0);
                                int termBytesPosEnd = currentFrame.prefix;
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
                                suffixBytesPos = currentFrame.startBytePos;
                            }
                            else
                            {
                                suffixBytesPos = currentFrame.startBytePos + currentFrame.suffix - compiledAutomaton.CommonSuffixRef.Length;
                            }

                            // Test overlapping suffix part:
                            int commonSuffixBytesPosEnd = compiledAutomaton.CommonSuffixRef.Length;
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
                        int state = currentFrame.state;
                        for (int idx = 0; idx < currentFrame.suffix; idx++)
                        {
                            state = runAutomaton.Step(state, currentFrame.suffixBytes[currentFrame.startBytePos + idx] & 0xff);
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
                            currentFrame = PushFrame(state);
                            //if (DEBUG) System.out.println("\n  frame ord=" + currentFrame.ord + " prefix=" + brToString(new BytesRef(term.bytes, term.offset, currentFrame.prefix)) + " state=" + currentFrame.state + " lastInFloor?=" + currentFrame.isLastInFloor + " fp=" + currentFrame.fp + " trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]) + " outputPrefix=" + currentFrame.outputPrefix);
                        }
                        else if (runAutomaton.IsAccept(state))
                        {
                            CopyTerm();
                            //if (DEBUG) System.out.println("      term match to state=" + state + "; return term=" + brToString(term));
                            if (!(savedStartTerm == null || term.CompareTo(savedStartTerm) > 0))
                            {
                                Debug.Assert(false, "saveStartTerm=" + savedStartTerm.Utf8ToString() + " term=" + term.Utf8ToString());
                            }
                            return term;
                        }
                        else
                        {
                            //System.out.println("    no s=" + state);
                        }
                    nextTermContinue: ;
                    }
                    //nextTermBreak:;
                }

                internal void CopyTerm()
                {
                    int len = currentFrame.prefix + currentFrame.suffix;
                    if (term.Bytes.Length < len)
                    {
                        term.Bytes = ArrayUtil.Grow(term.Bytes, len);
                    }
                    System.Buffer.BlockCopy(currentFrame.suffixBytes, currentFrame.startBytePos, term.Bytes, currentFrame.prefix, currentFrame.suffix);
                    term.Length = len;
                }

                public override IComparer<BytesRef> Comparator
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
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

                public override long Ord
                {
                    get { throw new System.NotSupportedException(); }
                }

                public override SeekStatus SeekCeil(BytesRef text)
                {
                    throw new System.NotSupportedException();
                }
            }

            // Iterates through terms in this field
            internal sealed class SegmentTermsEnum : TermsEnum
            {
                private readonly BlockTreeTermsReader.FieldReader outerInstance;

                private IndexInput @in;

                private Frame[] stack;
                private readonly Frame staticFrame;
                private Frame currentFrame;
                private bool termExists;

                private int targetBeforeCurrentLength;

                private readonly ByteArrayDataInput scratchReader = new ByteArrayDataInput();

                // What prefix of the current term was present in the index:
                private int validIndexPrefix;

                // assert only:
                private bool eof;

                internal readonly BytesRef term = new BytesRef();
                private readonly FST.BytesReader fstReader;

                private FST.Arc<BytesRef>[] arcs = new FST.Arc<BytesRef>[1];

                public SegmentTermsEnum(BlockTreeTermsReader.FieldReader outerInstance)
                {
                    this.outerInstance = outerInstance;
                    //if (DEBUG) System.out.println("BTTR.init seg=" + segment);
                    stack = new Frame[0];

                    // Used to hold seek by TermState, or cached seek
                    staticFrame = new Frame(this, -1);

                    if (outerInstance.index == null)
                    {
                        fstReader = null;
                    }
                    else
                    {
                        fstReader = this.outerInstance.index.GetBytesReader();
                    }

                    // Init w/ root block; don't use index since it may
                    // not (and need not) have been loaded
                    for (int arcIdx = 0; arcIdx < arcs.Length; arcIdx++)
                    {
                        arcs[arcIdx] = new FST.Arc<BytesRef>();
                    }

                    currentFrame = staticFrame;
                    FST.Arc<BytesRef> arc;
                    if (outerInstance.index != null)
                    {
                        arc = outerInstance.index.GetFirstArc(arcs[0]);
                        // Empty string prefix must have an output in the index!
                        Debug.Assert(arc.IsFinal);
                    }
                    else
                    {
                        arc = null;
                    }
                    currentFrame = staticFrame;
                    //currentFrame = pushFrame(arc, rootCode, 0);
                    //currentFrame.loadBlock();
                    validIndexPrefix = 0;
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
                    if (this.@in == null)
                    {
                        this.@in = (IndexInput)outerInstance.outerInstance.@in.Clone();
                    }
                }

                /// <summary>
                /// Runs next() through the entire terms dict,
                ///  computing aggregate statistics.
                /// </summary>
                public Stats ComputeBlockStats()
                {
                    Stats stats = new Stats(outerInstance.outerInstance.segment, outerInstance.fieldInfo.Name);
                    if (outerInstance.index != null)
                    {
                        stats.IndexNodeCount = outerInstance.index.NodeCount;
                        stats.IndexArcCount = outerInstance.index.ArcCount;
                        stats.IndexNumBytes = outerInstance.index.SizeInBytes();
                    }

                    currentFrame = staticFrame;
                    FST.Arc<BytesRef> arc;
                    if (outerInstance.index != null)
                    {
                        arc = outerInstance.index.GetFirstArc(arcs[0]);
                        // Empty string prefix must have an output in the index!
                        Debug.Assert(arc.IsFinal);
                    }
                    else
                    {
                        arc = null;
                    }

                    // Empty string prefix must have an output in the
                    // index!
                    currentFrame = PushFrame(arc, outerInstance.rootCode, 0);
                    currentFrame.fpOrig = currentFrame.fp;
                    currentFrame.LoadBlock();
                    validIndexPrefix = 0;

                    stats.StartBlock(currentFrame, !(currentFrame.isLastInFloor));

                    while (true)
                    {
                        // Pop finished blocks
                        while (currentFrame.nextEnt == currentFrame.entCount)
                        {
                            stats.EndBlock(currentFrame);
                            if (!currentFrame.isLastInFloor)
                            {
                                currentFrame.LoadNextFloorBlock();
                                stats.StartBlock(currentFrame, true);
                            }
                            else
                            {
                                if (currentFrame.ord == 0)
                                {
                                    goto allTermsBreak;
                                }
                                long lastFP = currentFrame.fpOrig;
                                currentFrame = stack[currentFrame.ord - 1];
                                Debug.Assert(lastFP == currentFrame.lastSubFP);
                                // if (DEBUG) {
                                //   System.out.println("  reset validIndexPrefix=" + validIndexPrefix);
                                // }
                            }
                        }

                        while (true)
                        {
                            if (currentFrame.Next())
                            {
                                // Push to new block:
                                currentFrame = PushFrame(null, currentFrame.lastSubFP, term.Length);
                                currentFrame.fpOrig = currentFrame.fp;
                                // this is a "next" frame -- even if it's
                                // floor'd we must pretend it isn't so we don't
                                // try to scan to the right floor frame:
                                currentFrame.isFloor = false;
                                //currentFrame.hasTerms = true;
                                currentFrame.LoadBlock();
                                stats.StartBlock(currentFrame, !currentFrame.isLastInFloor);
                            }
                            else
                            {
                                stats.Term(term);
                                break;
                            }
                        }
                        //allTermsContinue:;
                    }
                allTermsBreak:

                    stats.Finish();

                    // Put root frame back:
                    currentFrame = staticFrame;
                    if (outerInstance.index != null)
                    {
                        arc = outerInstance.index.GetFirstArc(arcs[0]);
                        // Empty string prefix must have an output in the index!
                        Debug.Assert(arc.IsFinal);
                    }
                    else
                    {
                        arc = null;
                    }
                    currentFrame = PushFrame(arc, outerInstance.rootCode, 0);
                    currentFrame.Rewind();
                    currentFrame.LoadBlock();
                    validIndexPrefix = 0;
                    term.Length = 0;

                    return stats;
                }

                private Frame GetFrame(int ord)
                {
                    if (ord >= stack.Length)
                    {
                        Frame[] next = new Frame[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        Array.Copy(stack, 0, next, 0, stack.Length);
                        for (int stackOrd = stack.Length; stackOrd < next.Length; stackOrd++)
                        {
                            next[stackOrd] = new Frame(this, stackOrd);
                        }
                        stack = next;
                    }
                    Debug.Assert(stack[ord].ord == ord);
                    return stack[ord];
                }

                private FST.Arc<BytesRef> GetArc(int ord)
                {
                    if (ord >= arcs.Length)
                    {
                        FST.Arc<BytesRef>[] next = new FST.Arc<BytesRef>[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        Array.Copy(arcs, 0, next, 0, arcs.Length);
                        for (int arcOrd = arcs.Length; arcOrd < next.Length; arcOrd++)
                        {
                            next[arcOrd] = new FST.Arc<BytesRef>();
                        }
                        arcs = next;
                    }
                    return arcs[ord];
                }

                public override IComparer<BytesRef> Comparator
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                }

                // Pushes a frame we seek'd to
                internal Frame PushFrame(FST.Arc<BytesRef> arc, BytesRef frameData, int length)
                {
                    scratchReader.Reset((byte[])(Array)frameData.Bytes, frameData.Offset, frameData.Length);
                    long code = scratchReader.ReadVLong();
                    long fpSeek = (long)((ulong)code >> BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS);
                    Frame f = GetFrame(1 + currentFrame.ord);
                    f.hasTerms = (code & BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS) != 0;
                    f.hasTermsOrig = f.hasTerms;
                    f.isFloor = (code & BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR) != 0;
                    if (f.isFloor)
                    {
                        f.SetFloorData(scratchReader, frameData);
                    }
                    PushFrame(arc, fpSeek, length);

                    return f;
                }

                // Pushes next'd frame or seek'd frame; we later
                // lazy-load the frame only when needed
                internal Frame PushFrame(FST.Arc<BytesRef> arc, long fp, int length)
                {
                    Frame f = GetFrame(1 + currentFrame.ord);
                    f.arc = arc;
                    if (f.fpOrig == fp && f.nextEnt != -1)
                    {
                        //if (DEBUG) System.out.println("      push reused frame ord=" + f.ord + " fp=" + f.fp + " isFloor?=" + f.isFloor + " hasTerms=" + f.hasTerms + " pref=" + term + " nextEnt=" + f.nextEnt + " targetBeforeCurrentLength=" + targetBeforeCurrentLength + " term.length=" + term.length + " vs prefix=" + f.prefix);
                        if (f.prefix > targetBeforeCurrentLength)
                        {
                            f.Rewind();
                        }
                        else
                        {
                            // if (DEBUG) {
                            //   System.out.println("        skip rewind!");
                            // }
                        }
                        Debug.Assert(length == f.prefix);
                    }
                    else
                    {
                        f.nextEnt = -1;
                        f.prefix = length;
                        f.state.TermBlockOrd = 0;
                        f.fpOrig = f.fp = fp;
                        f.lastSubFP = -1;
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
                private bool ClearEOF()
                {
                    eof = false;
                    return true;
                }

                // asserts only
                private bool SetEOF()
                {
                    eof = true;
                    return true;
                }

                public override bool SeekExact(BytesRef target)
                {
                    if (outerInstance.index == null)
                    {
                        throw new InvalidOperationException("terms index was not loaded");
                    }

                    if (term.Bytes.Length <= target.Length)
                    {
                        term.Bytes = ArrayUtil.Grow(term.Bytes, 1 + target.Length);
                    }

                    Debug.Assert(ClearEOF());

                    FST.Arc<BytesRef> arc;
                    int targetUpto;
                    BytesRef output;

                    targetBeforeCurrentLength = currentFrame.ord;

                    if (currentFrame != staticFrame)
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

                        arc = arcs[0];
                        Debug.Assert(arc.IsFinal);
                        output = arc.Output;
                        targetUpto = 0;

                        Frame lastFrame = stack[0];
                        Debug.Assert(validIndexPrefix <= term.Length);

                        int targetLimit = Math.Min(target.Length, validIndexPrefix);

                        int cmp = 0;

                        // TODO: reverse vLong byte order for better FST
                        // prefix output sharing

                        // First compare up to valid seek frames:
                        while (targetUpto < targetLimit)
                        {
                            cmp = (term.Bytes[targetUpto] & 0xFF) - (target.Bytes[target.Offset + targetUpto] & 0xFF);
                            // if (DEBUG) {
                            //   System.out.println("    cycle targetUpto=" + targetUpto + " (vs limit=" + targetLimit + ") cmp=" + cmp + " (targetLabel=" + (char) (target.bytes[target.offset + targetUpto]) + " vs termLabel=" + (char) (term.bytes[targetUpto]) + ")"   + " arc.output=" + arc.output + " output=" + output);
                            // }
                            if (cmp != 0)
                            {
                                break;
                            }
                            arc = arcs[1 + targetUpto];
                            //if (arc.label != (target.bytes[target.offset + targetUpto] & 0xFF)) {
                            //System.out.println("FAIL: arc.label=" + (char) arc.label + " targetLabel=" + (char) (target.bytes[target.offset + targetUpto] & 0xFF));
                            //}
                            Debug.Assert(arc.Label == (target.Bytes[target.Offset + targetUpto] & 0xFF), "arc.label=" + (char)arc.Label + " targetLabel=" + (char)(target.Bytes[target.Offset + targetUpto] & 0xFF));
                            if (arc.Output != outerInstance.outerInstance.NO_OUTPUT)
                            {
                                output = outerInstance.outerInstance.fstOutputs.Add(output, arc.Output);
                            }
                            if (arc.IsFinal)
                            {
                                lastFrame = stack[1 + lastFrame.ord];
                            }
                            targetUpto++;
                        }

                        if (cmp == 0)
                        {
                            int targetUptoMid = targetUpto;

                            // Second compare the rest of the term, but
                            // don't save arc/output/frame; we only do this
                            // to find out if the target term is before,
                            // equal or after the current term
                            int targetLimit2 = Math.Min(target.Length, term.Length);
                            while (targetUpto < targetLimit2)
                            {
                                cmp = (term.Bytes[targetUpto] & 0xFF) - (target.Bytes[target.Offset + targetUpto] & 0xFF);
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
                                cmp = term.Length - target.Length;
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
                            currentFrame = lastFrame;
                        }
                        else if (cmp > 0)
                        {
                            // Uncommon case: target term
                            // is before current term; this means we can
                            // keep the currentFrame but we must rewind it
                            // (so we scan from the start)
                            targetBeforeCurrentLength = 0;
                            // if (DEBUG) {
                            //   System.out.println("  target is before current (shares prefixLen=" + targetUpto + "); rewind frame ord=" + lastFrame.ord);
                            // }
                            currentFrame = lastFrame;
                            currentFrame.Rewind();
                        }
                        else
                        {
                            // Target is exactly the same as current term
                            Debug.Assert(term.Length == target.Length);
                            if (termExists)
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
                        targetBeforeCurrentLength = -1;
                        arc = outerInstance.index.GetFirstArc(arcs[0]);

                        // Empty string prefix must have an output (block) in the index!
                        Debug.Assert(arc.IsFinal);
                        Debug.Assert(arc.Output != null);

                        // if (DEBUG) {
                        //   System.out.println("    no seek state; push root frame");
                        // }

                        output = arc.Output;

                        currentFrame = staticFrame;

                        //term.length = 0;
                        targetUpto = 0;
                        currentFrame = PushFrame(arc, outerInstance.outerInstance.fstOutputs.Add(output, arc.NextFinalOutput), 0);
                    }

                    // if (DEBUG) {
                    //   System.out.println("  start index loop targetUpto=" + targetUpto + " output=" + output + " currentFrame.ord=" + currentFrame.ord + " targetBeforeCurrentLength=" + targetBeforeCurrentLength);
                    // }

                    while (targetUpto < target.Length)
                    {
                        int targetLabel = target.Bytes[target.Offset + targetUpto] & 0xFF;

                        FST.Arc<BytesRef> nextArc = outerInstance.index.FindTargetArc(targetLabel, arc, GetArc(1 + targetUpto), fstReader);

                        if (nextArc == null)
                        {
                            // Index is exhausted
                            // if (DEBUG) {
                            //   System.out.println("    index: index exhausted label=" + ((char) targetLabel) + " " + toHex(targetLabel));
                            // }

                            validIndexPrefix = currentFrame.prefix;
                            //validIndexPrefix = targetUpto;

                            currentFrame.ScanToFloorFrame(target);

                            if (!currentFrame.hasTerms)
                            {
                                termExists = false;
                                term.Bytes[targetUpto] = (byte)targetLabel;
                                term.Length = 1 + targetUpto;
                                // if (DEBUG) {
                                //   System.out.println("  FAST NOT_FOUND term=" + brToString(term));
                                // }
                                return false;
                            }

                            currentFrame.LoadBlock();

                            SeekStatus result = currentFrame.ScanToTerm(target, true);
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
                            term.Bytes[targetUpto] = (byte)targetLabel;
                            // Aggregate output as we go:
                            Debug.Assert(arc.Output != null);
                            if (arc.Output != outerInstance.outerInstance.NO_OUTPUT)
                            {
                                output = outerInstance.outerInstance.fstOutputs.Add(output, arc.Output);
                            }

                            // if (DEBUG) {
                            //   System.out.println("    index: follow label=" + toHex(target.bytes[target.offset + targetUpto]&0xff) + " arc.output=" + arc.output + " arc.nfo=" + arc.nextFinalOutput);
                            // }
                            targetUpto++;

                            if (arc.IsFinal)
                            {
                                //if (DEBUG) System.out.println("    arc is final!");
                                currentFrame = PushFrame(arc, outerInstance.outerInstance.fstOutputs.Add(output, arc.NextFinalOutput), targetUpto);
                                //if (DEBUG) System.out.println("    curFrame.ord=" + currentFrame.ord + " hasTerms=" + currentFrame.hasTerms);
                            }
                        }
                    }

                    //validIndexPrefix = targetUpto;
                    validIndexPrefix = currentFrame.prefix;

                    currentFrame.ScanToFloorFrame(target);

                    // Target term is entirely contained in the index:
                    if (!currentFrame.hasTerms)
                    {
                        termExists = false;
                        term.Length = targetUpto;
                        // if (DEBUG) {
                        //   System.out.println("  FAST NOT_FOUND term=" + brToString(term));
                        // }
                        return false;
                    }

                    currentFrame.LoadBlock();

                    SeekStatus result_ = currentFrame.ScanToTerm(target, true);
                    if (result_ == SeekStatus.FOUND)
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
                    if (outerInstance.index == null)
                    {
                        throw new InvalidOperationException("terms index was not loaded");
                    }

                    if (term.Bytes.Length <= target.Length)
                    {
                        term.Bytes = ArrayUtil.Grow(term.Bytes, 1 + target.Length);
                    }

                    Debug.Assert(ClearEOF());

                    //if (DEBUG) {
                    //System.out.println("\nBTTR.seekCeil seg=" + segment + " target=" + fieldInfo.name + ":" + target.utf8ToString() + " " + target + " current=" + brToString(term) + " (exists?=" + termExists + ") validIndexPrefix=  " + validIndexPrefix);
                    //printSeekState();
                    //}

                    FST.Arc<BytesRef> arc;
                    int targetUpto;
                    BytesRef output;

                    targetBeforeCurrentLength = currentFrame.ord;

                    if (currentFrame != staticFrame)
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

                        arc = arcs[0];
                        Debug.Assert(arc.IsFinal);
                        output = arc.Output;
                        targetUpto = 0;

                        Frame lastFrame = stack[0];
                        Debug.Assert(validIndexPrefix <= term.Length);

                        int targetLimit = Math.Min(target.Length, validIndexPrefix);

                        int cmp = 0;

                        // TOOD: we should write our vLong backwards (MSB
                        // first) to get better sharing from the FST

                        // First compare up to valid seek frames:
                        while (targetUpto < targetLimit)
                        {
                            cmp = (term.Bytes[targetUpto] & 0xFF) - (target.Bytes[target.Offset + targetUpto] & 0xFF);
                            //if (DEBUG) {
                            //System.out.println("    cycle targetUpto=" + targetUpto + " (vs limit=" + targetLimit + ") cmp=" + cmp + " (targetLabel=" + (char) (target.bytes[target.offset + targetUpto]) + " vs termLabel=" + (char) (term.bytes[targetUpto]) + ")"   + " arc.output=" + arc.output + " output=" + output);
                            //}
                            if (cmp != 0)
                            {
                                break;
                            }
                            arc = arcs[1 + targetUpto];
                            Debug.Assert(arc.Label == (target.Bytes[target.Offset + targetUpto] & 0xFF), "arc.label=" + (char)arc.Label + " targetLabel=" + (char)(target.Bytes[target.Offset + targetUpto] & 0xFF));
                            // TOOD: we could save the outputs in local
                            // byte[][] instead of making new objs ever
                            // seek; but, often the FST doesn't have any
                            // shared bytes (but this could change if we
                            // reverse vLong byte order)
                            if (arc.Output != outerInstance.outerInstance.NO_OUTPUT)
                            {
                                output = outerInstance.outerInstance.fstOutputs.Add(output, arc.Output);
                            }
                            if (arc.IsFinal)
                            {
                                lastFrame = stack[1 + lastFrame.ord];
                            }
                            targetUpto++;
                        }

                        if (cmp == 0)
                        {
                            int targetUptoMid = targetUpto;
                            // Second compare the rest of the term, but
                            // don't save arc/output/frame:
                            int targetLimit2 = Math.Min(target.Length, term.Length);
                            while (targetUpto < targetLimit2)
                            {
                                cmp = (term.Bytes[targetUpto] & 0xFF) - (target.Bytes[target.Offset + targetUpto] & 0xFF);
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
                                cmp = term.Length - target.Length;
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
                            currentFrame = lastFrame;
                        }
                        else if (cmp > 0)
                        {
                            // Uncommon case: target term
                            // is before current term; this means we can
                            // keep the currentFrame but we must rewind it
                            // (so we scan from the start)
                            targetBeforeCurrentLength = 0;
                            //if (DEBUG) {
                            //System.out.println("  target is before current (shares prefixLen=" + targetUpto + "); rewind frame ord=" + lastFrame.ord);
                            //}
                            currentFrame = lastFrame;
                            currentFrame.Rewind();
                        }
                        else
                        {
                            // Target is exactly the same as current term
                            Debug.Assert(term.Length == target.Length);
                            if (termExists)
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
                        targetBeforeCurrentLength = -1;
                        arc = outerInstance.index.GetFirstArc(arcs[0]);

                        // Empty string prefix must have an output (block) in the index!
                        Debug.Assert(arc.IsFinal);
                        Debug.Assert(arc.Output != null);

                        //if (DEBUG) {
                        //System.out.println("    no seek state; push root frame");
                        //}

                        output = arc.Output;

                        currentFrame = staticFrame;

                        //term.length = 0;
                        targetUpto = 0;
                        currentFrame = PushFrame(arc, outerInstance.outerInstance.fstOutputs.Add(output, arc.NextFinalOutput), 0);
                    }

                    //if (DEBUG) {
                    //System.out.println("  start index loop targetUpto=" + targetUpto + " output=" + output + " currentFrame.ord+1=" + currentFrame.ord + " targetBeforeCurrentLength=" + targetBeforeCurrentLength);
                    //}

                    while (targetUpto < target.Length)
                    {
                        int targetLabel = target.Bytes[target.Offset + targetUpto] & 0xFF;

                        FST.Arc<BytesRef> nextArc = outerInstance.index.FindTargetArc(targetLabel, arc, GetArc(1 + targetUpto), fstReader);

                        if (nextArc == null)
                        {
                            // Index is exhausted
                            // if (DEBUG) {
                            //   System.out.println("    index: index exhausted label=" + ((char) targetLabel) + " " + toHex(targetLabel));
                            // }

                            validIndexPrefix = currentFrame.prefix;
                            //validIndexPrefix = targetUpto;

                            currentFrame.ScanToFloorFrame(target);

                            currentFrame.LoadBlock();

                            SeekStatus result = currentFrame.ScanToTerm(target, false);
                            if (result == SeekStatus.END)
                            {
                                term.CopyBytes(target);
                                termExists = false;

                                if (Next() != null)
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
                            term.Bytes[targetUpto] = (byte)targetLabel;
                            arc = nextArc;
                            // Aggregate output as we go:
                            Debug.Assert(arc.Output != null);
                            if (arc.Output != outerInstance.outerInstance.NO_OUTPUT)
                            {
                                output = outerInstance.outerInstance.fstOutputs.Add(output, arc.Output);
                            }

                            //if (DEBUG) {
                            //System.out.println("    index: follow label=" + toHex(target.bytes[target.offset + targetUpto]&0xff) + " arc.output=" + arc.output + " arc.nfo=" + arc.nextFinalOutput);
                            //}
                            targetUpto++;

                            if (arc.IsFinal)
                            {
                                //if (DEBUG) System.out.println("    arc is final!");
                                currentFrame = PushFrame(arc, outerInstance.outerInstance.fstOutputs.Add(output, arc.NextFinalOutput), targetUpto);
                                //if (DEBUG) System.out.println("    curFrame.ord=" + currentFrame.ord + " hasTerms=" + currentFrame.hasTerms);
                            }
                        }
                    }

                    //validIndexPrefix = targetUpto;
                    validIndexPrefix = currentFrame.prefix;

                    currentFrame.ScanToFloorFrame(target);

                    currentFrame.LoadBlock();

                    SeekStatus result_ = currentFrame.ScanToTerm(target, false);

                    if (result_ == SeekStatus.END)
                    {
                        term.CopyBytes(target);
                        termExists = false;
                        if (Next() != null)
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
                        return result_;
                    }
                }

                /*LUCENE TO-DO Not in use
                private void PrintSeekState(PrintStream @out)
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
                      Debug.Assert(f != null);
                      BytesRef prefix = new BytesRef(Term_Renamed.Bytes, 0, f.Prefix);
                      if (f.NextEnt == -1)
                      {
                        @out.println("    frame " + (isSeekFrame ? "(seek)" : "(next)") + " ord=" + ord + " fp=" + f.Fp + (f.IsFloor ? (" (fpOrig=" + f.FpOrig + ")") : "") + " prefixLen=" + f.Prefix + " prefix=" + prefix + (f.NextEnt == -1 ? "" : (" (of " + f.EntCount + ")")) + " hasTerms=" + f.HasTerms + " isFloor=" + f.IsFloor + " code=" + ((f.Fp << BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) + (f.HasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) + (f.IsFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0)) + " isLastInFloor=" + f.IsLastInFloor + " mdUpto=" + f.MetaDataUpto + " tbOrd=" + f.TermBlockOrd);
                      }
                      else
                      {
                        @out.println("    frame " + (isSeekFrame ? "(seek, loaded)" : "(next, loaded)") + " ord=" + ord + " fp=" + f.Fp + (f.IsFloor ? (" (fpOrig=" + f.FpOrig + ")") : "") + " prefixLen=" + f.Prefix + " prefix=" + prefix + " nextEnt=" + f.NextEnt + (f.NextEnt == -1 ? "" : (" (of " + f.EntCount + ")")) + " hasTerms=" + f.HasTerms + " isFloor=" + f.IsFloor + " code=" + ((f.Fp << BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) + (f.HasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) + (f.IsFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0)) + " lastSubFP=" + f.LastSubFP + " isLastInFloor=" + f.IsLastInFloor + " mdUpto=" + f.MetaDataUpto + " tbOrd=" + f.TermBlockOrd);
                      }
                      if (OuterInstance.Index != null)
                      {
                        Debug.Assert(!isSeekFrame || f.Arc != null, "isSeekFrame=" + isSeekFrame + " f.arc=" + f.Arc);
                        if (f.Prefix > 0 && isSeekFrame && f.Arc.Label != (Term_Renamed.Bytes[f.Prefix - 1] & 0xFF))
                        {
                          @out.println("      broken seek state: arc.label=" + (char) f.Arc.Label + " vs term byte=" + (char)(Term_Renamed.Bytes[f.Prefix - 1] & 0xFF));
                          throw new Exception("seek state is broken");
                        }
                        BytesRef output = Util.Get(OuterInstance.Index, prefix);
                        if (output == null)
                        {
                          @out.println("      broken seek state: prefix is not final in index");
                          throw new Exception("seek state is broken");
                        }
                        else if (isSeekFrame && !f.IsFloor)
                        {
                          ByteArrayDataInput reader = new ByteArrayDataInput(output.Bytes, output.Offset, output.Length);
                          long codeOrig = reader.ReadVLong();
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
                }*/

                /* Decodes only the term bytes of the next term.  If caller then asks for
                   metadata, ie docFreq, totalTermFreq or pulls a D/&PEnum, we then (lazily)
                   decode all metadata up to the current term. */

                public override BytesRef Next()
                {
                    if (@in == null)
                    {
                        // Fresh TermsEnum; seek to first term:
                        FST.Arc<BytesRef> arc;
                        if (outerInstance.index != null)
                        {
                            arc = outerInstance.index.GetFirstArc(arcs[0]);
                            // Empty string prefix must have an output in the index!
                            Debug.Assert(arc.IsFinal);
                        }
                        else
                        {
                            arc = null;
                        }
                        currentFrame = PushFrame(arc, outerInstance.rootCode, 0);
                        currentFrame.LoadBlock();
                    }

                    targetBeforeCurrentLength = currentFrame.ord;

                    Debug.Assert(!eof);
                    //if (DEBUG) {
                    //System.out.println("\nBTTR.next seg=" + segment + " term=" + brToString(term) + " termExists?=" + termExists + " field=" + fieldInfo.name + " termBlockOrd=" + currentFrame.state.termBlockOrd + " validIndexPrefix=" + validIndexPrefix);
                    //printSeekState();
                    //}

                    if (currentFrame == staticFrame)
                    {
                        // If seek was previously called and the term was
                        // cached, or seek(TermState) was called, usually
                        // caller is just going to pull a D/&PEnum or get
                        // docFreq, etc.  But, if they then call next(),
                        // this method catches up all internal state so next()
                        // works properly:
                        //if (DEBUG) System.out.println("  re-seek to pending term=" + term.utf8ToString() + " " + term);
                        bool result = SeekExact(term);
                        Debug.Assert(result);
                    }

                    // Pop finished blocks
                    while (currentFrame.nextEnt == currentFrame.entCount)
                    {
                        if (!currentFrame.isLastInFloor)
                        {
                            currentFrame.LoadNextFloorBlock();
                        }
                        else
                        {
                            //if (DEBUG) System.out.println("  pop frame");
                            if (currentFrame.ord == 0)
                            {
                                //if (DEBUG) System.out.println("  return null");
                                Debug.Assert(SetEOF());
                                term.Length = 0;
                                validIndexPrefix = 0;
                                currentFrame.Rewind();
                                termExists = false;
                                return null;
                            }
                            long lastFP = currentFrame.fpOrig;
                            currentFrame = stack[currentFrame.ord - 1];

                            if (currentFrame.nextEnt == -1 || currentFrame.lastSubFP != lastFP)
                            {
                                // We popped into a frame that's not loaded
                                // yet or not scan'd to the right entry
                                currentFrame.ScanToFloorFrame(term);
                                currentFrame.LoadBlock();
                                currentFrame.ScanToSubBlock(lastFP);
                            }

                            // Note that the seek state (last seek) has been
                            // invalidated beyond this depth
                            validIndexPrefix = Math.Min(validIndexPrefix, currentFrame.prefix);
                            //if (DEBUG) {
                            //System.out.println("  reset validIndexPrefix=" + validIndexPrefix);
                            //}
                        }
                    }

                    while (true)
                    {
                        if (currentFrame.Next())
                        {
                            // Push to new block:
                            //if (DEBUG) System.out.println("  push frame");
                            currentFrame = PushFrame(null, currentFrame.lastSubFP, term.Length);
                            // this is a "next" frame -- even if it's
                            // floor'd we must pretend it isn't so we don't
                            // try to scan to the right floor frame:
                            currentFrame.isFloor = false;
                            //currentFrame.hasTerms = true;
                            currentFrame.LoadBlock();
                        }
                        else
                        {
                            //if (DEBUG) System.out.println("  return term=" + term.utf8ToString() + " " + term + " currentFrame.ord=" + currentFrame.ord);
                            return term;
                        }
                    }
                }

                public override BytesRef Term
                {
                    get
                    {
                        Debug.Assert(!eof);
                        return term;
                    }
                }

                public override int DocFreq
                {
                    get
                    {
                        Debug.Assert(!eof);
                        //if (DEBUG) System.out.println("BTR.docFreq");
                        currentFrame.DecodeMetaData();
                        //if (DEBUG) System.out.println("  return " + currentFrame.state.docFreq);
                        return currentFrame.state.DocFreq;
                    }
                }

                public override long TotalTermFreq
                {
                    get
                    {
                        Debug.Assert(!eof);
                        currentFrame.DecodeMetaData();
                        return currentFrame.state.TotalTermFreq;
                    }
                }

                public override DocsEnum Docs(IBits skipDocs, DocsEnum reuse, int flags)
                {
                    Debug.Assert(!eof);
                    //if (DEBUG) {
                    //System.out.println("BTTR.docs seg=" + segment);
                    //}
                    currentFrame.DecodeMetaData();
                    //if (DEBUG) {
                    //System.out.println("  state=" + currentFrame.state);
                    //}
                    return outerInstance.outerInstance.postingsReader.Docs(outerInstance.fieldInfo, currentFrame.state, skipDocs, reuse, flags);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits skipDocs, DocsAndPositionsEnum reuse, int flags)
                {
                    if (outerInstance.fieldInfo.IndexOptions < IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        // Positions were not indexed:
                        return null;
                    }

                    Debug.Assert(!eof);
                    currentFrame.DecodeMetaData();
                    return outerInstance.outerInstance.postingsReader.DocsAndPositions(outerInstance.fieldInfo, currentFrame.state, skipDocs, reuse, flags);
                }

                public override void SeekExact(BytesRef target, TermState otherState)
                {
                    // if (DEBUG) {
                    //   System.out.println("BTTR.seekExact termState seg=" + segment + " target=" + target.utf8ToString() + " " + target + " state=" + otherState);
                    // }
                    Debug.Assert(ClearEOF());
                    if (target.CompareTo(term) != 0 || !termExists)
                    {
                        Debug.Assert(otherState != null && otherState is BlockTermState);
                        currentFrame = staticFrame;
                        currentFrame.state.CopyFrom(otherState);
                        term.CopyBytes(target);
                        currentFrame.metaDataUpto = currentFrame.TermBlockOrd;
                        Debug.Assert(currentFrame.metaDataUpto > 0);
                        validIndexPrefix = 0;
                    }
                    else
                    {
                        // if (DEBUG) {
                        //   System.out.println("  skip seek: already on target state=" + currentFrame.state);
                        // }
                    }
                }

                public override TermState GetTermState()
                {
                    Debug.Assert(!eof);
                    currentFrame.DecodeMetaData();
                    TermState ts = (TermState)currentFrame.state.Clone();
                    //if (DEBUG) System.out.println("BTTR.termState seg=" + segment + " state=" + ts);
                    return ts;
                }

                public override void SeekExact(long ord)
                {
                    throw new NotSupportedException();
                }

                public override long Ord
                {
                    get { throw new NotSupportedException(); }
                }

                // Not static -- references term, postingsReader,
                // fieldInfo, in
                internal sealed class Frame
                {
                    private readonly BlockTreeTermsReader.FieldReader.SegmentTermsEnum outerInstance;

                    // Our index in stack[]:
                    internal readonly int ord;

                    internal bool hasTerms;
                    internal bool hasTermsOrig;
                    internal bool isFloor;

                    internal FST.Arc<BytesRef> arc;

                    // File pointer where this block was loaded from
                    internal long fp;

                    internal long fpOrig;
                    internal long fpEnd;

                    internal byte[] suffixBytes = new byte[128];
                    internal readonly ByteArrayDataInput suffixesReader = new ByteArrayDataInput();

                    internal byte[] statBytes = new byte[64];
                    internal readonly ByteArrayDataInput statsReader = new ByteArrayDataInput();

                    internal byte[] floorData = new byte[32];
                    internal readonly ByteArrayDataInput floorDataReader = new ByteArrayDataInput();

                    // Length of prefix shared by all terms in this block
                    internal int prefix;

                    // Number of entries (term or sub-block) in this block
                    internal int entCount;

                    // Which term we will next read, or -1 if the block
                    // isn't loaded yet
                    internal int nextEnt;

                    // True if this block is either not a floor block,
                    // or, it's the last sub-block of a floor block
                    internal bool isLastInFloor;

                    // True if all entries are terms
                    internal bool isLeafBlock;

                    internal long lastSubFP;

                    internal int nextFloorLabel;
                    internal int numFollowFloorBlocks;

                    // Next term to decode metaData; we decode metaData
                    // lazily so that scanning to find the matching term is
                    // fast and only if you find a match and app wants the
                    // stats or docs/positions enums, will we decode the
                    // metaData
                    internal int metaDataUpto;

                    internal readonly BlockTermState state;

                    // metadata buffer, holding monotonic values
                    public long[] longs;

                    // metadata buffer, holding general values
                    public byte[] bytes;

                    internal ByteArrayDataInput bytesReader;

                    public Frame(BlockTreeTermsReader.FieldReader.SegmentTermsEnum outerInstance, int ord)
                    {
                        this.outerInstance = outerInstance;
                        this.ord = ord;
                        this.state = outerInstance.outerInstance.outerInstance.postingsReader.NewTermState();
                        this.state.TotalTermFreq = -1;
                        this.longs = new long[outerInstance.outerInstance.longsSize];
                    }

                    public void SetFloorData(ByteArrayDataInput @in, BytesRef source)
                    {
                        int numBytes = source.Length - (@in.Position - source.Offset);
                        if (numBytes > floorData.Length)
                        {
                            floorData = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        System.Buffer.BlockCopy(source.Bytes, source.Offset + @in.Position, floorData, 0, numBytes);
                        floorDataReader.Reset(floorData, 0, numBytes);
                        numFollowFloorBlocks = floorDataReader.ReadVInt();
                        nextFloorLabel = floorDataReader.ReadByte() & 0xff;
                        //if (DEBUG) {
                        //System.out.println("    setFloorData fpOrig=" + fpOrig + " bytes=" + new BytesRef(source.bytes, source.offset + in.getPosition(), numBytes) + " numFollowFloorBlocks=" + numFollowFloorBlocks + " nextFloorLabel=" + toHex(nextFloorLabel));
                        //}
                    }

                    public int TermBlockOrd
                    {
                        get
                        {
                            return isLeafBlock ? nextEnt : state.TermBlockOrd;
                        }
                    }

                    internal void LoadNextFloorBlock()
                    {
                        //if (DEBUG) {
                        //System.out.println("    loadNextFloorBlock fp=" + fp + " fpEnd=" + fpEnd);
                        //}
                        Debug.Assert(arc == null || isFloor, "arc=" + arc + " isFloor=" + isFloor);
                        fp = fpEnd;
                        nextEnt = -1;
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

                        if (nextEnt != -1)
                        {
                            // Already loaded
                            return;
                        }
                        //System.out.println("blc=" + blockLoadCount);

                        outerInstance.@in.Seek(fp);
                        int code = outerInstance.@in.ReadVInt();
                        entCount = (int)((uint)code >> 1);
                        Debug.Assert(entCount > 0);
                        isLastInFloor = (code & 1) != 0;
                        Debug.Assert(arc == null || (isLastInFloor || isFloor));

                        // TODO: if suffixes were stored in random-access
                        // array structure, then we could do binary search
                        // instead of linear scan to find target term; eg
                        // we could have simple array of offsets

                        // term suffixes:
                        code = outerInstance.@in.ReadVInt();
                        isLeafBlock = (code & 1) != 0;
                        int numBytes = (int)((uint)code >> 1);
                        if (suffixBytes.Length < numBytes)
                        {
                            suffixBytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        outerInstance.@in.ReadBytes(suffixBytes, 0, numBytes);
                        suffixesReader.Reset(suffixBytes, 0, numBytes);

                        /*if (DEBUG) {
                          if (arc == null) {
                            System.out.println("    loadBlock (next) fp=" + fp + " entCount=" + entCount + " prefixLen=" + prefix + " isLastInFloor=" + isLastInFloor + " leaf?=" + isLeafBlock);
                          } else {
                            System.out.println("    loadBlock (seek) fp=" + fp + " entCount=" + entCount + " prefixLen=" + prefix + " hasTerms?=" + hasTerms + " isFloor?=" + isFloor + " isLastInFloor=" + isLastInFloor + " leaf?=" + isLeafBlock);
                          }
                          }*/

                        // stats
                        numBytes = outerInstance.@in.ReadVInt();
                        if (statBytes.Length < numBytes)
                        {
                            statBytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        outerInstance.@in.ReadBytes(statBytes, 0, numBytes);
                        statsReader.Reset(statBytes, 0, numBytes);
                        metaDataUpto = 0;

                        state.TermBlockOrd = 0;
                        nextEnt = 0;
                        lastSubFP = -1;

                        // TODO: we could skip this if !hasTerms; but
                        // that's rare so won't help much
                        // metadata
                        numBytes = outerInstance.@in.ReadVInt();
                        if (bytes == null)
                        {
                            bytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                            bytesReader = new ByteArrayDataInput();
                        }
                        else if (bytes.Length < numBytes)
                        {
                            bytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        outerInstance.@in.ReadBytes(bytes, 0, numBytes);
                        bytesReader.Reset(bytes, 0, numBytes);

                        // Sub-blocks of a single floor block are always
                        // written one after another -- tail recurse:
                        fpEnd = outerInstance.@in.FilePointer;
                        // if (DEBUG) {
                        //   System.out.println("      fpEnd=" + fpEnd);
                        // }
                    }

                    internal void Rewind()
                    {
                        // Force reload:
                        fp = fpOrig;
                        nextEnt = -1;
                        hasTerms = hasTermsOrig;
                        if (isFloor)
                        {
                            floorDataReader.Rewind();
                            numFollowFloorBlocks = floorDataReader.ReadVInt();
                            nextFloorLabel = floorDataReader.ReadByte() & 0xff;
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
                        return isLeafBlock ? NextLeaf() : NextNonLeaf();
                    }

                    // Decodes next entry; returns true if it's a sub-block
                    public bool NextLeaf()
                    {
                        //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
                        Debug.Assert(nextEnt != -1 && nextEnt < entCount, "nextEnt=" + nextEnt + " entCount=" + entCount + " fp=" + fp);
                        nextEnt++;
                        suffix = suffixesReader.ReadVInt();
                        startBytePos = suffixesReader.Position;
                        outerInstance.term.Length = prefix + suffix;
                        if (outerInstance.term.Bytes.Length < outerInstance.term.Length)
                        {
                            outerInstance.term.Grow(outerInstance.term.Length);
                        }
                        suffixesReader.ReadBytes(outerInstance.term.Bytes, prefix, suffix);
                        // A normal term
                        outerInstance.termExists = true;
                        return false;
                    }

                    public bool NextNonLeaf()
                    {
                        //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
                        Debug.Assert(nextEnt != -1 && nextEnt < entCount, "nextEnt=" + nextEnt + " entCount=" + entCount + " fp=" + fp);
                        nextEnt++;
                        int code = suffixesReader.ReadVInt();
                        suffix = (int)((uint)code >> 1);
                        startBytePos = suffixesReader.Position;
                        outerInstance.term.Length = prefix + suffix;
                        if (outerInstance.term.Bytes.Length < outerInstance.term.Length)
                        {
                            outerInstance.term.Grow(outerInstance.term.Length);
                        }
                        suffixesReader.ReadBytes(outerInstance.term.Bytes, prefix, suffix);
                        if ((code & 1) == 0)
                        {
                            // A normal term
                            outerInstance.termExists = true;
                            subCode = 0;
                            state.TermBlockOrd++;
                            return false;
                        }
                        else
                        {
                            // A sub-block; make sub-FP absolute:
                            outerInstance.termExists = false;
                            subCode = suffixesReader.ReadVLong();
                            lastSubFP = fp - subCode;
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
                        if (!isFloor || target.Length <= prefix)
                        {
                            // if (DEBUG) {
                            //   System.out.println("    scanToFloorFrame skip: isFloor=" + isFloor + " target.length=" + target.length + " vs prefix=" + prefix);
                            // }
                            return;
                        }

                        int targetLabel = target.Bytes[target.Offset + prefix] & 0xFF;

                        // if (DEBUG) {
                        //   System.out.println("    scanToFloorFrame fpOrig=" + fpOrig + " targetLabel=" + toHex(targetLabel) + " vs nextFloorLabel=" + toHex(nextFloorLabel) + " numFollowFloorBlocks=" + numFollowFloorBlocks);
                        // }

                        if (targetLabel < nextFloorLabel)
                        {
                            // if (DEBUG) {
                            //   System.out.println("      already on correct block");
                            // }
                            return;
                        }

                        Debug.Assert(numFollowFloorBlocks != 0);

                        long newFP = fpOrig;
                        while (true)
                        {
                            long code = floorDataReader.ReadVLong();
                            newFP = fpOrig + ((long)((ulong)code >> 1));
                            hasTerms = (code & 1) != 0;
                            // if (DEBUG) {
                            //   System.out.println("      label=" + toHex(nextFloorLabel) + " fp=" + newFP + " hasTerms?=" + hasTerms + " numFollowFloor=" + numFollowFloorBlocks);
                            // }

                            isLastInFloor = numFollowFloorBlocks == 1;
                            numFollowFloorBlocks--;

                            if (isLastInFloor)
                            {
                                nextFloorLabel = 256;
                                // if (DEBUG) {
                                //   System.out.println("        stop!  last block nextFloorLabel=" + toHex(nextFloorLabel));
                                // }
                                break;
                            }
                            else
                            {
                                nextFloorLabel = floorDataReader.ReadByte() & 0xff;
                                if (targetLabel < nextFloorLabel)
                                {
                                    // if (DEBUG) {
                                    //   System.out.println("        stop!  nextFloorLabel=" + toHex(nextFloorLabel));
                                    // }
                                    break;
                                }
                            }
                        }

                        if (newFP != fp)
                        {
                            // Force re-load of the block:
                            // if (DEBUG) {
                            //   System.out.println("      force switch to fp=" + newFP + " oldFP=" + fp);
                            // }
                            nextEnt = -1;
                            fp = newFP;
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
                        int limit = TermBlockOrd;
                        bool absolute = metaDataUpto == 0;
                        Debug.Assert(limit > 0);

                        // TODO: better API would be "jump straight to term=N"???
                        while (metaDataUpto < limit)
                        {
                            // TODO: we could make "tiers" of metadata, ie,
                            // decode docFreq/totalTF but don't decode postings
                            // metadata; this way caller could get
                            // docFreq/totalTF w/o paying decode cost for
                            // postings

                            // TODO: if docFreq were bulk decoded we could
                            // just skipN here:

                            // stats
                            state.DocFreq = statsReader.ReadVInt();
                            //if (DEBUG) System.out.println("    dF=" + state.docFreq);
                            if (outerInstance.outerInstance.fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                            {
                                state.TotalTermFreq = state.DocFreq + statsReader.ReadVLong();
                                //if (DEBUG) System.out.println("    totTF=" + state.totalTermFreq);
                            }
                            // metadata
                            for (int i = 0; i < outerInstance.outerInstance.longsSize; i++)
                            {
                                longs[i] = bytesReader.ReadVLong();
                            }
                            outerInstance.outerInstance.outerInstance.postingsReader.DecodeTerm(longs, bytesReader, outerInstance.outerInstance.fieldInfo, state, absolute);

                            metaDataUpto++;
                            absolute = false;
                        }
                        state.TermBlockOrd = metaDataUpto;
                    }

                    // Used only by assert
                    private bool PrefixMatches(BytesRef target)
                    {
                        for (int bytePos = 0; bytePos < prefix; bytePos++)
                        {
                            if (target.Bytes[target.Offset + bytePos] != outerInstance.term.Bytes[bytePos])
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
                        Debug.Assert(!isLeafBlock);
                        //if (DEBUG) System.out.println("  scanToSubBlock fp=" + fp + " subFP=" + subFP + " entCount=" + entCount + " lastSubFP=" + lastSubFP);
                        //assert nextEnt == 0;
                        if (lastSubFP == subFP)
                        {
                            //if (DEBUG) System.out.println("    already positioned");
                            return;
                        }
                        Debug.Assert(subFP < fp, "fp=" + fp + " subFP=" + subFP);
                        long targetSubCode = fp - subFP;
                        //if (DEBUG) System.out.println("    targetSubCode=" + targetSubCode);
                        while (true)
                        {
                            Debug.Assert(nextEnt < entCount);
                            nextEnt++;
                            int code = suffixesReader.ReadVInt();
                            suffixesReader.SkipBytes(isLeafBlock ? code : (int)((uint)code >> 1));
                            //if (DEBUG) System.out.println("    " + nextEnt + " (of " + entCount + ") ent isSubBlock=" + ((code&1)==1));
                            if ((code & 1) != 0)
                            {
                                long subCode = suffixesReader.ReadVLong();
                                //if (DEBUG) System.out.println("      subCode=" + subCode);
                                if (targetSubCode == subCode)
                                {
                                    //if (DEBUG) System.out.println("        match!");
                                    lastSubFP = subFP;
                                    return;
                                }
                            }
                            else
                            {
                                state.TermBlockOrd++;
                            }
                        }
                    }

                    // NOTE: sets startBytePos/suffix as a side effect
                    public SeekStatus ScanToTerm(BytesRef target, bool exactOnly)
                    {
                        return isLeafBlock ? ScanToTermLeaf(target, exactOnly) : ScanToTermNonLeaf(target, exactOnly);
                    }

                    private int startBytePos;
                    private int suffix;
                    private long subCode;

                    // Target's prefix matches this block's prefix; we
                    // scan the entries check if the suffix matches.
                    public SeekStatus ScanToTermLeaf(BytesRef target, bool exactOnly)
                    {
                        // if (DEBUG) System.out.println("    scanToTermLeaf: block fp=" + fp + " prefix=" + prefix + " nextEnt=" + nextEnt + " (of " + entCount + ") target=" + brToString(target) + " term=" + brToString(term));

                        Debug.Assert(nextEnt != -1);

                        outerInstance.termExists = true;
                        subCode = 0;

                        if (nextEnt == entCount)
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
                            nextEnt++;

                            suffix = suffixesReader.ReadVInt();

                            // if (DEBUG) {
                            //   BytesRef suffixBytesRef = new BytesRef();
                            //   suffixBytesRef.bytes = suffixBytes;
                            //   suffixBytesRef.offset = suffixesReader.getPosition();
                            //   suffixBytesRef.length = suffix;
                            //   System.out.println("      cycle: term " + (nextEnt-1) + " (of " + entCount + ") suffix=" + brToString(suffixBytesRef));
                            // }

                            int termLen = prefix + suffix;
                            startBytePos = suffixesReader.Position;
                            suffixesReader.SkipBytes(suffix);

                            int targetLimit = target.Offset + (target.Length < termLen ? target.Length : termLen);
                            int targetPos = target.Offset + prefix;

                            // Loop over bytes in the suffix, comparing to
                            // the target
                            int bytePos = startBytePos;
                            while (true)
                            {
                                int cmp;
                                bool stop;
                                if (targetPos < targetLimit)
                                {
                                    cmp = (suffixBytes[bytePos++] & 0xFF) - (target.Bytes[targetPos++] & 0xFF);
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

                                    if (nextEnt == entCount)
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

                                    if (!exactOnly && !outerInstance.termExists)
                                    {
                                        // We are on a sub-block, and caller wants
                                        // us to position to the next term after
                                        // the target, so we must recurse into the
                                        // sub-frame(s):
                                        outerInstance.currentFrame = outerInstance.PushFrame(null, outerInstance.currentFrame.lastSubFP, termLen);
                                        outerInstance.currentFrame.LoadBlock();
                                        while (outerInstance.currentFrame.Next())
                                        {
                                            outerInstance.currentFrame = outerInstance.PushFrame(null, outerInstance.currentFrame.lastSubFP, outerInstance.term.Length);
                                            outerInstance.currentFrame.LoadBlock();
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

                                    Debug.Assert(outerInstance.termExists);
                                    FillTerm();
                                    //if (DEBUG) System.out.println("        found!");
                                    return SeekStatus.FOUND;
                                }
                            }
                        nextTermContinue: ;
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

                        Debug.Assert(nextEnt != -1);

                        if (nextEnt == entCount)
                        {
                            if (exactOnly)
                            {
                                FillTerm();
                                outerInstance.termExists = subCode == 0;
                            }
                            return SeekStatus.END;
                        }

                        Debug.Assert(PrefixMatches(target));

                        // Loop over each entry (term or sub-block) in this block:
                        //nextTerm: while(nextEnt < entCount) {
                        while (true)
                        {
                            nextEnt++;

                            int code = suffixesReader.ReadVInt();
                            suffix = (int)((uint)code >> 1);
                            // if (DEBUG) {
                            //   BytesRef suffixBytesRef = new BytesRef();
                            //   suffixBytesRef.bytes = suffixBytes;
                            //   suffixBytesRef.offset = suffixesReader.getPosition();
                            //   suffixBytesRef.length = suffix;
                            //   System.out.println("      cycle: " + ((code&1)==1 ? "sub-block" : "term") + " " + (nextEnt-1) + " (of " + entCount + ") suffix=" + brToString(suffixBytesRef));
                            // }

                            outerInstance.termExists = (code & 1) == 0;
                            int termLen = prefix + suffix;
                            startBytePos = suffixesReader.Position;
                            suffixesReader.SkipBytes(suffix);
                            if (outerInstance.termExists)
                            {
                                state.TermBlockOrd++;
                                subCode = 0;
                            }
                            else
                            {
                                subCode = suffixesReader.ReadVLong();
                                lastSubFP = fp - subCode;
                            }

                            int targetLimit = target.Offset + (target.Length < termLen ? target.Length : termLen);
                            int targetPos = target.Offset + prefix;

                            // Loop over bytes in the suffix, comparing to
                            // the target
                            int bytePos = startBytePos;
                            while (true)
                            {
                                int cmp;
                                bool stop;
                                if (targetPos < targetLimit)
                                {
                                    cmp = (suffixBytes[bytePos++] & 0xFF) - (target.Bytes[targetPos++] & 0xFF);
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

                                    if (nextEnt == entCount)
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

                                    if (!exactOnly && !outerInstance.termExists)
                                    {
                                        // We are on a sub-block, and caller wants
                                        // us to position to the next term after
                                        // the target, so we must recurse into the
                                        // sub-frame(s):
                                        outerInstance.currentFrame = outerInstance.PushFrame(null, outerInstance.currentFrame.lastSubFP, termLen);
                                        outerInstance.currentFrame.LoadBlock();
                                        while (outerInstance.currentFrame.Next())
                                        {
                                            outerInstance.currentFrame = outerInstance.PushFrame(null, outerInstance.currentFrame.lastSubFP, outerInstance.term.Length);
                                            outerInstance.currentFrame.LoadBlock();
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

                                    Debug.Assert(outerInstance.termExists);
                                    FillTerm();
                                    //if (DEBUG) System.out.println("        found!");
                                    return SeekStatus.FOUND;
                                }
                            }
                        nextTermContinue: ;
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

                    private void FillTerm()
                    {
                        int termLength = prefix + suffix;
                        outerInstance.term.Length = prefix + suffix;
                        if (outerInstance.term.Bytes.Length < termLength)
                        {
                            outerInstance.term.Grow(termLength);
                        }
                        System.Buffer.BlockCopy(suffixBytes, startBytePos, outerInstance.term.Bytes, prefix, suffix);
                    }
                }
            }
        }

        public override long RamBytesUsed()
        {
            long sizeInByes = ((postingsReader != null) ? postingsReader.RamBytesUsed() : 0);
            foreach (FieldReader reader in fields.Values)
            {
                sizeInByes += reader.RamBytesUsed();
            }
            return sizeInByes;
        }

        public override void CheckIntegrity()
        {
            if (version >= BlockTreeTermsWriter.VERSION_CHECKSUM)
            {
                // term dictionary
                CodecUtil.ChecksumEntireFile(@in);

                // postings
                postingsReader.CheckIntegrity();
            }
        }
    }
}