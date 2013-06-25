using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;

namespace Lucene.Net.Codecs
{
    public class BlockTreeTermsReader : FieldsProducer
    {
        private readonly IndexInput input;

        private readonly PostingsReaderBase postingsReader;

        private readonly IDictionary<String, FieldReader> fields = new HashMap<String, FieldReader>(); // TODO: do we need treemap?

        private long dirOffset;

        private long indexDirOffset;

        private String segment;

        private readonly int version;

        public BlockTreeTermsReader(Directory dir, FieldInfos fieldInfos, SegmentInfo info,
                              PostingsReaderBase postingsReader, IOContext ioContext,
                              String segmentSuffix, int indexDivisor)
        {

            this.postingsReader = postingsReader;

            this.segment = info.name;
            input = dir.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, BlockTreeTermsWriter.TERMS_EXTENSION),
                               ioContext);

            bool success = false;
            IndexInput indexIn = null;

            try
            {
                version = ReadHeader(input);
                if (indexDivisor != -1)
                {
                    indexIn = dir.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, BlockTreeTermsWriter.TERMS_INDEX_EXTENSION),
                                            ioContext);
                    int indexVersion = ReadIndexHeader(indexIn);
                    if (indexVersion != version)
                    {
                        throw new CorruptIndexException("mixmatched version files: " + input + "=" + version + "," + indexIn + "=" + indexVersion);
                    }
                }

                // Have PostingsReader init itself
                postingsReader.Init(input);

                // Read per-field details
                SeekDir(input, dirOffset);
                if (indexDivisor != -1)
                {
                    SeekDir(indexIn, indexDirOffset);
                }

                int numFields = input.ReadVInt();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + input + ")");
                }

                for (int i = 0; i < numFields; i++)
                {
                    int field = input.ReadVInt();
                    long numTerms = input.ReadVLong();
                    //assert numTerms >= 0;
                    int numBytes = input.ReadVInt();
                    BytesRef rootCode = new BytesRef(new sbyte[numBytes]);
                    input.ReadBytes(rootCode.bytes, 0, numBytes);
                    rootCode.length = numBytes;
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                    //assert fieldInfo != null: "field=" + field;
                    long sumTotalTermFreq = fieldInfo.IndexOptionsValue == FieldInfo.IndexOptions.DOCS_ONLY ? -1 : input.ReadVLong();
                    long sumDocFreq = input.ReadVLong();
                    int docCount = input.ReadVInt();
                    if (docCount < 0 || docCount > info.DocCount)
                    { // #docs with field must be <= #docs
                        throw new CorruptIndexException("invalid docCount: " + docCount + " maxDoc: " + info.DocCount + " (resource=" + input + ")");
                    }
                    if (sumDocFreq < docCount)
                    {  // #postings must be >= #docs with field
                        throw new CorruptIndexException("invalid sumDocFreq: " + sumDocFreq + " docCount: " + docCount + " (resource=" + input + ")");
                    }
                    if (sumTotalTermFreq != -1 && sumTotalTermFreq < sumDocFreq)
                    { // #positions must be >= #postings
                        throw new CorruptIndexException("invalid sumTotalTermFreq: " + sumTotalTermFreq + " sumDocFreq: " + sumDocFreq + " (resource=" + input + ")");
                    }
                    long indexStartFP = indexDivisor != -1 ? indexIn.ReadVLong() : 0;
                    FieldReader previous = fields[fieldInfo.name] = new FieldReader(this, fieldInfo, numTerms, rootCode, sumTotalTermFreq, sumDocFreq, docCount, indexStartFP, indexIn);
                    if (previous != null)
                    {
                        throw new CorruptIndexException("duplicate field: " + fieldInfo.name + " (resource=" + input + ")");
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
                    // this.close() will close in:
                    IOUtils.CloseWhileHandlingException((IDisposable)indexIn, this);
                }
            }
        }

        protected int ReadHeader(IndexInput input)
        {
            int version = CodecUtil.CheckHeader(input, BlockTreeTermsWriter.TERMS_CODEC_NAME,
                                  BlockTreeTermsWriter.TERMS_VERSION_START,
                                  BlockTreeTermsWriter.TERMS_VERSION_CURRENT);
            if (version < BlockTreeTermsWriter.TERMS_VERSION_APPEND_ONLY)
            {
                dirOffset = input.ReadLong();
            }
            return version;
        }

        protected int ReadIndexHeader(IndexInput input)
        {
            int version = CodecUtil.CheckHeader(input, BlockTreeTermsWriter.TERMS_INDEX_CODEC_NAME,
                                  BlockTreeTermsWriter.TERMS_INDEX_VERSION_START,
                                  BlockTreeTermsWriter.TERMS_INDEX_VERSION_CURRENT);
            if (version < BlockTreeTermsWriter.TERMS_INDEX_VERSION_APPEND_ONLY)
            {
                indexDirOffset = input.ReadLong();
            }
            return version;
        }

        protected void SeekDir(IndexInput input, long dirOffset)
        {
            if (version >= BlockTreeTermsWriter.TERMS_INDEX_VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length - 8);
                dirOffset = input.ReadLong();
            }
            input.Seek(dirOffset);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Close(input, postingsReader);
                }
                finally
                {
                    // Clear so refs to terms index is GCable even if
                    // app hangs onto us:
                    fields.Clear();
                }
            }
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return fields.Keys.GetEnumerator();
        }

        public override Terms Terms(string field)
        {
            //assert field != null;
            return fields[field];
        }

        public override int Size
        {
            get { return fields.Count; }
        }

        // for debugging
        internal String BrToString(BytesRef b)
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
                catch
                {
                    // If BytesRef isn't actually UTF8, or it's eg a
                    // prefix of UTF8 that ends mid-unicode-char, we
                    // fallback to hex:
                    return b.ToString();
                }
            }
        }

        public class Stats
        {
            /** How many nodes in the index FST. */
            public long indexNodeCount;

            /** How many arcs in the index FST. */
            public long indexArcCount;

            /** Byte size of the index. */
            public long indexNumBytes;

            /** Total number of terms in the field. */
            public long totalTermCount;

            /** Total number of bytes (sum of term lengths) across all terms in the field. */
            public long totalTermBytes;

            /** The number of normal (non-floor) blocks in the terms file. */
            public int nonFloorBlockCount;

            /** The number of floor blocks (meta-blocks larger than the
             *  allowed {@code maxItemsPerBlock}) in the terms file. */
            public int floorBlockCount;

            /** The number of sub-blocks within the floor blocks. */
            public int floorSubBlockCount;

            /** The number of "internal" blocks (that have both
             *  terms and sub-blocks). */
            public int mixedBlockCount;

            /** The number of "leaf" blocks (blocks that have only
             *  terms). */
            public int termsOnlyBlockCount;

            /** The number of "internal" blocks that do not contain
             *  terms (have only sub-blocks). */
            public int subBlocksOnlyBlockCount;

            /** Total number of blocks. */
            public int totalBlockCount;

            /** Number of blocks at each prefix depth. */
            public int[] blockCountByPrefixLen = new int[10];
            private int startBlockCount;
            private int endBlockCount;

            /** Total number of bytes used to store term suffixes. */
            public long totalBlockSuffixBytes;

            /** Total number of bytes used to store term stats (not
             *  including what the {@link PostingsBaseFormat}
             *  stores. */
            public long totalBlockStatsBytes;

            /** Total bytes stored by the {@link PostingsBaseFormat},
             *  plus the other few vInts stored in the frame. */
            public long totalBlockOtherBytes;

            /** Segment name. */
            public readonly String segment;

            /** Field name. */
            public readonly String field;

            public Stats(String segment, String field)
            {
                this.segment = segment;
                this.field = field;
            }

            internal void StartBlock(FieldReader.SegmentTermsEnum.Frame frame, bool isFloor)
            {
                totalBlockCount++;
                if (isFloor)
                {
                    if (frame.fp == frame.fpOrig)
                    {
                        floorBlockCount++;
                    }
                    floorSubBlockCount++;
                }
                else
                {
                    nonFloorBlockCount++;
                }

                if (blockCountByPrefixLen.Length <= frame.prefix)
                {
                    blockCountByPrefixLen = ArrayUtil.Grow(blockCountByPrefixLen, 1 + frame.prefix);
                }
                blockCountByPrefixLen[frame.prefix]++;
                startBlockCount++;
                totalBlockSuffixBytes += frame.suffixesReader.Length;
                totalBlockStatsBytes += frame.statsReader.Length;
            }

            internal void EndBlock(FieldReader.SegmentTermsEnum.Frame frame)
            {
                int termCount = frame.isLeafBlock ? frame.entCount : frame.state.termBlockOrd;
                int subBlockCount = frame.entCount - termCount;
                totalTermCount += termCount;
                if (termCount != 0 && subBlockCount != 0)
                {
                    mixedBlockCount++;
                }
                else if (termCount != 0)
                {
                    termsOnlyBlockCount++;
                }
                else if (subBlockCount != 0)
                {
                    subBlocksOnlyBlockCount++;
                }
                else
                {
                    throw new InvalidOperationException();
                }
                endBlockCount++;
                long otherBytes = frame.fpEnd - frame.fp - frame.suffixesReader.Length - frame.statsReader.Length;
                //assert otherBytes > 0 : "otherBytes=" + otherBytes + " frame.fp=" + frame.fp + " frame.fpEnd=" + frame.fpEnd;
                totalBlockOtherBytes += otherBytes;
            }

            internal void Term(BytesRef term)
            {
                totalTermBytes += term.length;
            }

            internal void Finish()
            {
                //assert startBlockCount == endBlockCount: "startBlockCount=" + startBlockCount + " endBlockCount=" + endBlockCount;
                //assert totalBlockCount == floorSubBlockCount + nonFloorBlockCount: "floorSubBlockCount=" + floorSubBlockCount + " nonFloorBlockCount=" + nonFloorBlockCount + " totalBlockCount=" + totalBlockCount;
                //assert totalBlockCount == mixedBlockCount + termsOnlyBlockCount + subBlocksOnlyBlockCount: "totalBlockCount=" + totalBlockCount + " mixedBlockCount=" + mixedBlockCount + " subBlocksOnlyBlockCount=" + subBlocksOnlyBlockCount + " termsOnlyBlockCount=" + termsOnlyBlockCount;
            }

            public override string ToString()
            {
                StringBuilder output = new StringBuilder();

                output.AppendLine("  index FST:");
                output.AppendLine("    " + indexNodeCount + " nodes");
                output.AppendLine("    " + indexArcCount + " arcs");
                output.AppendLine("    " + indexNumBytes + " bytes");
                output.AppendLine("  terms:");
                output.AppendLine("    " + totalTermCount + " terms");
                output.AppendLine("    " + totalTermBytes + " bytes" + (totalTermCount != 0 ? " (" + ((double)totalTermBytes / totalTermCount).ToString("0.0") + " bytes/term)" : ""));
                output.AppendLine("  blocks:");
                output.AppendLine("    " + totalBlockCount + " blocks");
                output.AppendLine("    " + termsOnlyBlockCount + " terms-only blocks");
                output.AppendLine("    " + subBlocksOnlyBlockCount + " sub-block-only blocks");
                output.AppendLine("    " + mixedBlockCount + " mixed blocks");
                output.AppendLine("    " + floorBlockCount + " floor blocks");
                output.AppendLine("    " + (totalBlockCount - floorSubBlockCount) + " non-floor blocks");
                output.AppendLine("    " + floorSubBlockCount + " floor sub-blocks");
                output.AppendLine("    " + totalBlockSuffixBytes + " term suffix bytes" + (totalBlockCount != 0 ? " (" + ((double)totalBlockSuffixBytes / totalBlockCount).ToString("0.0") + " suffix-bytes/block)" : ""));
                output.AppendLine("    " + totalBlockStatsBytes + " term stats bytes" + (totalBlockCount != 0 ? " (" + ((double)totalBlockStatsBytes / totalBlockCount).ToString("0.0") + " stats-bytes/block)" : ""));
                output.AppendLine("    " + totalBlockOtherBytes + " other bytes" + (totalBlockCount != 0 ? " (" + ((double)totalBlockOtherBytes / totalBlockCount).ToString("0.0") + " other-bytes/block)" : ""));
                if (totalBlockCount != 0)
                {
                    output.AppendLine("    by prefix length:");
                    int total = 0;
                    for (int prefix = 0; prefix < blockCountByPrefixLen.Length; prefix++)
                    {
                        int blockCount = blockCountByPrefixLen[prefix];
                        total += blockCount;
                        if (blockCount != 0)
                        {
                            output.AppendLine("      " + prefix.ToString().PadLeft(2, ' ') + ": " + blockCount);
                        }
                    }
                    //assert totalBlockCount == total;
                }

                return output.ToString();
            }
        }

        internal readonly Outputs<BytesRef> fstOutputs = ByteSequenceOutputs.GetSingleton();
        internal readonly BytesRef NO_OUTPUT = ByteSequenceOutputs.GetSingleton().GetNoOutput();

        public sealed class FieldReader : Terms
        {
            internal readonly long numTerms;
            internal readonly FieldInfo fieldInfo;
            internal readonly long sumTotalTermFreq;
            internal readonly long sumDocFreq;
            internal readonly int docCount;
            internal readonly long indexStartFP;
            internal readonly long rootBlockFP;
            internal readonly BytesRef rootCode;
            private readonly FST<BytesRef> index;

            internal readonly BlockTreeTermsReader parent;

            internal FieldReader(BlockTreeTermsReader parent, FieldInfo fieldInfo, long numTerms, BytesRef rootCode, long sumTotalTermFreq, long sumDocFreq, int docCount, long indexStartFP, IndexInput indexIn)
            {
                this.parent = parent;

                //assert numTerms > 0;
                this.fieldInfo = fieldInfo;
                //DEBUG = BlockTreeTermsReader.DEBUG && fieldInfo.name.equals("id");
                this.numTerms = numTerms;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
                this.indexStartFP = indexStartFP;
                this.rootCode = rootCode;
                // if (DEBUG) {
                //   System.out.println("BTTR: seg=" + segment + " field=" + fieldInfo.name + " rootBlockCode=" + rootCode + " divisor=" + indexDivisor);
                // }

                rootBlockFP = Number.URShift((new ByteArrayDataInput((byte[])(Array)rootCode.bytes, rootCode.offset, rootCode.length)).ReadVLong(), BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS);

                if (indexIn != null)
                {
                    IndexInput clone = (IndexInput)indexIn.Clone();
                    //System.out.println("start=" + indexStartFP + " field=" + fieldInfo.name);
                    clone.Seek(indexStartFP);
                    index = new FST<BytesRef>(clone, ByteSequenceOutputs.GetSingleton());

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
                    index = null;
                }
            }

            public Stats ComputeStats()
            {
                return new SegmentTermsEnum(this).ComputeBlockStats();
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            public override bool HasOffsets
            {
                get { return fieldInfo.IndexOptionsValue.GetValueOrDefault() >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS; }
            }

            public override bool HasPositions
            {
                get { return fieldInfo.IndexOptionsValue.GetValueOrDefault() >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS; }
            }

            public override bool HasPayloads
            {
                get { return fieldInfo.HasPayloads; }
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return new SegmentTermsEnum(this);
            }

            public override long Size
            {
                get { return numTerms; }
            }

            public override long SumTotalTermFreq
            {
                get { return sumTotalTermFreq; }
            }

            public override long SumDocFreq
            {
                get { return sumDocFreq; }
            }

            public override int DocCount
            {
                get { return docCount; }
            }

            public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm)
            {
                if (compiled.type != CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
                {
                    throw new ArgumentException("please use CompiledAutomaton.getTermsEnum instead");
                }
                return new IntersectEnum(this, compiled, startTerm);
            }

            // NOTE: cannot seek!
            private sealed class IntersectEnum : TermsEnum
            {
                private readonly IndexInput input;

                private Frame[] stack;

                private FST<BytesRef>.Arc<BytesRef>[] arcs = new FST<BytesRef>.Arc<BytesRef>[5];

                private readonly RunAutomaton runAutomaton;
                private readonly CompiledAutomaton compiledAutomaton;

                private Frame currentFrame;

                private readonly BytesRef term = new BytesRef();

                private readonly FST.BytesReader fstReader;

                internal readonly FieldReader parent;

                private sealed class Frame
                {
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

                    internal FST<BytesRef>.Arc<BytesRef> arc;

                    internal readonly BlockTermState termState;

                    // Cumulative output so far
                    internal BytesRef outputPrefix;

                    internal int startBytePos;
                    internal int suffix;

                    private readonly IntersectEnum parent;

                    public Frame(IntersectEnum parent, int ord)
                    {
                        this.parent = parent;
                        this.ord = ord;
                        termState = parent.parent.parent.postingsReader.NewTermState();
                        termState.totalTermFreq = -1;
                    }

                    internal void LoadNextFloorBlock()
                    {
                        //assert numFollowFloorBlocks > 0;
                        //if (DEBUG) System.out.println("    loadNextFoorBlock trans=" + transitions[transitionIndex]);

                        do
                        {
                            fp = fpOrig + Number.URShift(floorDataReader.ReadVLong(), 1);
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
                        transitions = parent.compiledAutomaton.sortedTransitions[state];
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
                            if (floorData.Length < frameIndexData.length)
                            {
                                this.floorData = new byte[ArrayUtil.Oversize(frameIndexData.length, 1)];
                            }
                            Array.Copy(frameIndexData.bytes, frameIndexData.offset, floorData, 0, frameIndexData.length);
                            floorDataReader.Reset(floorData, 0, frameIndexData.length);
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
                                if (!parent.runAutomaton.IsAccept(state))
                                {
                                    // Maybe skip floor blocks:
                                    while (numFollowFloorBlocks != 0 && nextFloorLabel <= transitions[0].Min)
                                    {
                                        fp = fpOrig + Number.URShift(floorDataReader.ReadVLong(), 1);
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

                        parent.input.Seek(fp);
                        int code2 = parent.input.ReadVInt();
                        entCount = Number.URShift(code2, 1);
                        //assert entCount > 0;
                        isLastInFloor = (code2 & 1) != 0;

                        // term suffixes:
                        code2 = parent.input.ReadVInt();
                        isLeafBlock = (code2 & 1) != 0;
                        int numBytes = Number.URShift(code2, 1);
                        // if (DEBUG) System.out.println("      entCount=" + entCount + " lastInFloor?=" + isLastInFloor + " leafBlock?=" + isLeafBlock + " numSuffixBytes=" + numBytes);
                        if (suffixBytes.Length < numBytes)
                        {
                            suffixBytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        parent.input.ReadBytes(suffixBytes, 0, numBytes);
                        suffixesReader.Reset(suffixBytes, 0, numBytes);

                        // stats
                        numBytes = parent.input.ReadVInt();
                        if (statBytes.Length < numBytes)
                        {
                            statBytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        parent.input.ReadBytes(statBytes, 0, numBytes);
                        statsReader.Reset(statBytes, 0, numBytes);
                        metaDataUpto = 0;

                        termState.termBlockOrd = 0;
                        nextEnt = 0;

                        parent.parent.parent.postingsReader.ReadTermsBlock(parent.input, parent.parent.fieldInfo, termState);

                        if (!isLastInFloor)
                        {
                            // Sub-blocks of a single floor block are always
                            // written one after another -- tail recurse:
                            fpEnd = parent.input.FilePointer;
                        }
                    }

                    public bool Next()
                    {
                        return isLeafBlock ? NextLeaf() : NextNonLeaf();
                    }

                    // Decodes next entry; returns true if it's a sub-block
                    public bool NextLeaf()
                    {
                        //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
                        //assert nextEnt != -1 && nextEnt < entCount: "nextEnt=" + nextEnt + " entCount=" + entCount + " fp=" + fp;
                        nextEnt++;
                        suffix = suffixesReader.ReadVInt();
                        startBytePos = suffixesReader.Position;
                        suffixesReader.SkipBytes(suffix);
                        return false;
                    }

                    public bool NextNonLeaf()
                    {
                        //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
                        //assert nextEnt != -1 && nextEnt < entCount: "nextEnt=" + nextEnt + " entCount=" + entCount + " fp=" + fp;
                        nextEnt++;
                        int code = suffixesReader.ReadVInt();
                        suffix = Number.URShift(code, 1);
                        startBytePos = suffixesReader.Position;
                        suffixesReader.SkipBytes(suffix);
                        if ((code & 1) == 0)
                        {
                            // A normal term
                            termState.termBlockOrd++;
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
                        get { return isLeafBlock ? nextEnt : termState.termBlockOrd; }
                    }

                    public void DecodeMetaData()
                    {

                        // lazily catch up on metadata decode:
                        int limit = TermBlockOrd;
                        //assert limit > 0;

                        // We must set/incr state.termCount because
                        // postings impl can look at this
                        termState.termBlockOrd = metaDataUpto;

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
                            termState.docFreq = statsReader.ReadVInt();
                            //if (DEBUG) System.out.println("    dF=" + state.docFreq);
                            if (parent.parent.fieldInfo.IndexOptionsValue != IndexOptions.DOCS_ONLY)
                            {
                                termState.totalTermFreq = termState.docFreq + statsReader.ReadVLong();
                                //if (DEBUG) System.out.println("    totTF=" + state.totalTermFreq);
                            }

                            parent.parent.parent.postingsReader.NextTerm(parent.parent.fieldInfo, termState);
                            metaDataUpto++;
                            termState.termBlockOrd++;
                        }
                    }
                }

                private BytesRef savedStartTerm;

                // TODO: in some cases we can filter by length?  eg
                // regexp foo*bar must be at least length 6 bytes
                public IntersectEnum(FieldReader parent, CompiledAutomaton compiled, BytesRef startTerm)
                {
                    this.parent = parent;

                    // if (DEBUG) {
                    //   System.out.println("\nintEnum.init seg=" + segment + " commonSuffix=" + brToString(compiled.commonSuffixRef));
                    // }
                    runAutomaton = compiled.runAutomaton;
                    compiledAutomaton = compiled;
                    input = (IndexInput)parent.parent.input.Clone();
                    stack = new Frame[5];
                    for (int idx = 0; idx < stack.Length; idx++)
                    {
                        stack[idx] = new Frame(this, idx);
                    }
                    for (int arcIdx = 0; arcIdx < arcs.Length; arcIdx++)
                    {
                        arcs[arcIdx] = new FST<BytesRef>.Arc<BytesRef>();
                    }

                    if (parent.index == null)
                    {
                        fstReader = null;
                    }
                    else
                    {
                        fstReader = parent.index.GetBytesReader();
                    }

                    // TODO: if the automaton is "smallish" we really
                    // should use the terms index to seek at least to
                    // the initial term and likely to subsequent terms
                    // (or, maybe just fallback to ATE for such cases).
                    // Else the seek cost of loading the frames will be
                    // too costly.

                    FST<BytesRef>.Arc<BytesRef> arc = parent.index.GetFirstArc(arcs[0]);
                    // Empty string prefix must have an output in the index!
                    //assert arc.isFinal();

                    // Special pushFrame since it's the first one:
                    Frame f = stack[0];
                    f.fp = f.fpOrig = parent.rootBlockFP;
                    f.prefix = 0;
                    f.SetState(runAutomaton.InitialState);
                    f.arc = arc;
                    f.outputPrefix = arc.Output;
                    f.Load(parent.rootCode);

                    // for assert:
                    //assert setSavedStartTerm(startTerm);

                    currentFrame = f;
                    if (startTerm != null)
                    {
                        SeekToStartTerm(startTerm);
                    }
                }

                private bool SetSavedStartTerm(BytesRef startTerm)
                {
                    savedStartTerm = startTerm == null ? null : BytesRef.DeepCopyOf(startTerm);
                    return true;
                }

                public override TermState TermState
                {
                    get
                    {
                        currentFrame.DecodeMetaData();
                        return (TermState)currentFrame.termState.Clone();
                    }
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
                    //assert stack[ord].ord == ord;
                    return stack[ord];
                }

                private FST<BytesRef>.Arc<BytesRef> GetArc(int ord)
                {
                    if (ord >= arcs.Length)
                    {
                        FST<BytesRef>.Arc<BytesRef>[] next =
                          new FST<BytesRef>.Arc<BytesRef>[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        Array.Copy(arcs, 0, next, 0, arcs.Length);
                        for (int arcOrd = arcs.Length; arcOrd < next.Length; arcOrd++)
                        {
                            next[arcOrd] = new FST<BytesRef>.Arc<BytesRef>();
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
                    FST<BytesRef>.Arc<BytesRef> arc = currentFrame.arc;
                    int idx = currentFrame.prefix;
                    //assert currentFrame.suffix > 0;
                    BytesRef output = currentFrame.outputPrefix;
                    while (idx < f.prefix)
                    {
                        int target = term.bytes[idx] & 0xff;
                        // TODO: we could be more efficient for the next()
                        // case by using current arc as starting point,
                        // passed to findTargetArc
                        arc = parent.index.FindTargetArc(target, arc, GetArc(1 + idx), fstReader);
                        //assert arc != null;
                        output = parent.parent.fstOutputs.Add(output, arc.Output);
                        idx++;
                    }

                    f.arc = arc;
                    f.outputPrefix = output;
                    //assert arc.isFinal();
                    f.Load(parent.parent.fstOutputs.Add(output, arc.NextFinalOutput));
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
                        return currentFrame.termState.docFreq;
                    }
                }

                public override long TotalTermFreq
                {
                    get
                    {
                        currentFrame.DecodeMetaData();
                        return currentFrame.termState.totalTermFreq;
                    }
                }

                public override DocsEnum Docs(IBits skipDocs, DocsEnum reuse, int flags)
                {
                    currentFrame.DecodeMetaData();
                    return parent.parent.postingsReader.Docs(parent.fieldInfo, currentFrame.termState, skipDocs, reuse, flags);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits skipDocs, DocsAndPositionsEnum reuse, int flags)
                {
                    if (parent.fieldInfo.IndexOptionsValue.GetValueOrDefault() < IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        // Positions were not indexed:
                        return null;
                    }

                    currentFrame.DecodeMetaData();
                    return parent.parent.postingsReader.DocsAndPositions(parent.fieldInfo, currentFrame.termState, skipDocs, reuse, flags);
                }

                private int GetState()
                {
                    int state = currentFrame.state;
                    for (int idx = 0; idx < currentFrame.suffix; idx++)
                    {
                        state = runAutomaton.Step(state, currentFrame.suffixBytes[currentFrame.startBytePos + idx] & 0xff);
                        //assert state != -1;
                    }
                    return state;
                }

                private void SeekToStartTerm(BytesRef target)
                {
                    //if (DEBUG) System.out.println("seek to startTerm=" + target.utf8ToString());
                    //assert currentFrame.ord == 0;
                    if (term.length < target.length)
                    {
                        term.bytes = ArrayUtil.Grow(term.bytes, target.length);
                    }
                    FST<BytesRef>.Arc<BytesRef> arc = arcs[0];
                    //assert arc == currentFrame.arc;

                    for (int idx = 0; idx <= target.length; idx++)
                    {

                        while (true)
                        {
                            int savePos = currentFrame.suffixesReader.Position;
                            int saveStartBytePos = currentFrame.startBytePos;
                            int saveSuffix = currentFrame.suffix;
                            long saveLastSubFP = currentFrame.lastSubFP;
                            int saveTermBlockOrd = currentFrame.termState.termBlockOrd;

                            bool isSubBlock = currentFrame.Next();

                            //if (DEBUG) System.out.println("    cycle ent=" + currentFrame.nextEnt + " (of " + currentFrame.entCount + ") prefix=" + currentFrame.prefix + " suffix=" + currentFrame.suffix + " isBlock=" + isSubBlock + " firstLabel=" + (currentFrame.suffix == 0 ? "" : (currentFrame.suffixBytes[currentFrame.startBytePos])&0xff));
                            term.length = currentFrame.prefix + currentFrame.suffix;
                            if (term.bytes.Length < term.length)
                            {
                                term.bytes = ArrayUtil.Grow(term.bytes, term.length);
                            }
                            Array.Copy(currentFrame.suffixBytes, currentFrame.startBytePos, term.bytes, currentFrame.prefix, currentFrame.suffix);

                            if (isSubBlock && StringHelper.StartsWith(target, term))
                            {
                                // Recurse
                                //if (DEBUG) System.out.println("      recurse!");
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
                                    currentFrame.termState.termBlockOrd = saveTermBlockOrd;
                                    Array.Copy(currentFrame.suffixBytes, currentFrame.startBytePos, term.bytes, currentFrame.prefix, currentFrame.suffix);
                                    term.length = currentFrame.prefix + currentFrame.suffix;
                                    // If the last entry was a block we don't
                                    // need to bother recursing and pushing to
                                    // the last term under it because the first
                                    // next() will simply skip the frame anyway
                                    return;
                                }
                            }
                        }
                    }

                    //assert false;
                }

                public override BytesRef Next()
                {
                    // if (DEBUG) {
                    //   System.out.println("\nintEnum.next seg=" + segment);
                    //   System.out.println("  frame ord=" + currentFrame.ord + " prefix=" + brToString(new BytesRef(term.bytes, term.offset, currentFrame.prefix)) + " state=" + currentFrame.state + " lastInFloor?=" + currentFrame.isLastInFloor + " fp=" + currentFrame.fp + " trans=" + (currentFrame.transitions.length == 0 ? "n/a" : currentFrame.transitions[currentFrame.transitionIndex]) + " outputPrefix=" + currentFrame.outputPrefix);
                    // }

                    bool continueNextTerm = false;
                    while (true)
                    {
                        continueNextTerm = false;
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
                                //assert currentFrame.lastSubFP == lastFP;
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
                                    continueNextTerm = true;
                                    break;
                                }
                                currentFrame.transitionIndex++;
                                currentFrame.curTransitionMax = currentFrame.transitions[currentFrame.transitionIndex].Max;
                                //if (DEBUG) System.out.println("      next trans=" + currentFrame.transitions[currentFrame.transitionIndex]);
                            }

                            if (continueNextTerm)
                                continue;
                        }

                        // First test the common suffix, if set:
                        if (compiledAutomaton.commonSuffixRef != null && !isSubBlock)
                        {
                            int termLen = currentFrame.prefix + currentFrame.suffix;
                            if (termLen < compiledAutomaton.commonSuffixRef.length)
                            {
                                // No match
                                // if (DEBUG) {
                                //   System.out.println("      skip: common suffix length");
                                // }
                                continue; // no need to do outer continue here
                            }

                            byte[] suffixBytes = currentFrame.suffixBytes;
                            byte[] commonSuffixBytes = (byte[])(Array)compiledAutomaton.commonSuffixRef.bytes;

                            int lenInPrefix = compiledAutomaton.commonSuffixRef.length - currentFrame.suffix;
                            //assert compiledAutomaton.commonSuffixRef.offset == 0;
                            int suffixBytesPos;
                            int commonSuffixBytesPos = 0;

                            if (lenInPrefix > 0)
                            {
                                // A prefix of the common suffix overlaps with
                                // the suffix of the block prefix so we first
                                // test whether the prefix part matches:
                                byte[] termBytes = (byte[])(Array)term.bytes;
                                int termBytesPos = currentFrame.prefix - lenInPrefix;
                                //assert termBytesPos >= 0;
                                int termBytesPosEnd = currentFrame.prefix;
                                while (termBytesPos < termBytesPosEnd)
                                {
                                    if (termBytes[termBytesPos++] != commonSuffixBytes[commonSuffixBytesPos++])
                                    {
                                        // if (DEBUG) {
                                        //   System.out.println("      skip: common suffix mismatch (in prefix)");
                                        // }
                                        continueNextTerm = true;
                                        break;
                                    }
                                }
                                if (continueNextTerm)
                                    continue;

                                suffixBytesPos = currentFrame.startBytePos;
                            }
                            else
                            {
                                suffixBytesPos = currentFrame.startBytePos + currentFrame.suffix - compiledAutomaton.commonSuffixRef.length;
                            }

                            // Test overlapping suffix part:
                            int commonSuffixBytesPosEnd = compiledAutomaton.commonSuffixRef.length;
                            while (commonSuffixBytesPos < commonSuffixBytesPosEnd)
                            {
                                if (suffixBytes[suffixBytesPos++] != commonSuffixBytes[commonSuffixBytesPos++])
                                {
                                    // if (DEBUG) {
                                    //   System.out.println("      skip: common suffix mismatch");
                                    // }
                                    continueNextTerm = true;
                                    break;
                                }
                            }

                            if (continueNextTerm)
                                continue;
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
                                continueNextTerm = true;
                                break;
                            }
                            else
                            {
                                //System.out.println("    c s=" + state);
                            }
                        }

                        if (continueNextTerm)
                            continue;

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
                            //assert savedStartTerm == null || term.compareTo(savedStartTerm) > 0: "saveStartTerm=" + savedStartTerm.utf8ToString() + " term=" + term.utf8ToString();
                            return term;
                        }
                        else
                        {
                            //System.out.println("    no s=" + state);
                        }
                    }
                }

                private void CopyTerm()
                {
                    //System.out.println("      copyTerm cur.prefix=" + currentFrame.prefix + " cur.suffix=" + currentFrame.suffix + " first=" + (char) currentFrame.suffixBytes[currentFrame.startBytePos]);
                    int len = currentFrame.prefix + currentFrame.suffix;
                    if (term.bytes.Length < len)
                    {
                        term.bytes = ArrayUtil.Grow(term.bytes, len);
                    }
                    Array.Copy(currentFrame.suffixBytes, currentFrame.startBytePos, term.bytes, currentFrame.prefix, currentFrame.suffix);
                    term.length = len;
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
                }

                public override bool SeekExact(BytesRef text, bool useCache)
                {
                    throw new NotSupportedException();
                }

                public override void SeekExact(long ord)
                {
                    throw new NotSupportedException();
                }

                public override long Ord
                {
                    get { throw new NotSupportedException(); }
                }

                public override SeekStatus SeekCeil(BytesRef text, bool useCache)
                {
                    throw new NotSupportedException();
                }
            }

            internal sealed class SegmentTermsEnum : TermsEnum
            {
                private IndexInput input;

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

                private FST<BytesRef>.Arc<BytesRef>[] arcs = new FST<BytesRef>.Arc<BytesRef>[1];

                private readonly FieldReader parent;

                public SegmentTermsEnum(FieldReader parent)
                {
                    this.parent = parent;

                    //if (DEBUG) System.out.println("BTTR.init seg=" + segment);
                    stack = new Frame[0];

                    // Used to hold seek by TermState, or cached seek
                    staticFrame = new Frame(this, -1);

                    if (parent.index == null)
                    {
                        fstReader = null;
                    }
                    else
                    {
                        fstReader = parent.index.GetBytesReader();
                    }

                    // Init w/ root block; don't use index since it may
                    // not (and need not) have been loaded
                    for (int arcIdx = 0; arcIdx < arcs.Length; arcIdx++)
                    {
                        arcs[arcIdx] = new FST<BytesRef>.Arc<BytesRef>();
                    }

                    currentFrame = staticFrame;
                    FST<BytesRef>.Arc<BytesRef> arc;
                    if (parent.index != null)
                    {
                        arc = parent.index.GetFirstArc(arcs[0]);
                        // Empty string prefix must have an output in the index!
                        //assert arc.isFinal();
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

                internal void InitIndexInput()
                {
                    if (this.input == null)
                    {
                        this.input = (IndexInput)parent.parent.input.Clone();
                    }
                }

                public Stats ComputeBlockStats()
                {
                    Stats stats = new Stats(parent.parent.segment, parent.fieldInfo.name);
                    if (parent.index != null)
                    {
                        stats.indexNodeCount = parent.index.GetNodeCount();
                        stats.indexArcCount = parent.index.ArcCount;
                        stats.indexNumBytes = parent.index.SizeInBytes();
                    }

                    currentFrame = staticFrame;
                    FST<BytesRef>.Arc<BytesRef> arc;
                    if (parent.index != null)
                    {
                        arc = parent.index.GetFirstArc(arcs[0]);
                        // Empty string prefix must have an output in the index!
                        //assert arc.isFinal();
                    }
                    else
                    {
                        arc = null;
                    }

                    // Empty string prefix must have an output in the
                    // index!
                    currentFrame = PushFrame(arc, parent.rootCode, 0);
                    currentFrame.fpOrig = currentFrame.fp;
                    currentFrame.LoadBlock();
                    validIndexPrefix = 0;

                    stats.StartBlock(currentFrame, !currentFrame.isLastInFloor);

                    bool breakAllTerms = false;
                    while (true)
                    {

                        breakAllTerms = false;

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
                                    breakAllTerms = true;
                                    break;
                                }
                                long lastFP = currentFrame.fpOrig;
                                currentFrame = stack[currentFrame.ord - 1];
                                //assert lastFP == currentFrame.lastSubFP;
                                // if (DEBUG) {
                                //   System.out.println("  reset validIndexPrefix=" + validIndexPrefix);
                                // }
                            }
                        }

                        if (breakAllTerms)
                            break;

                        while (true)
                        {
                            if (currentFrame.Next())
                            {
                                // Push to new block:
                                currentFrame = PushFrame(null, currentFrame.lastSubFP, term.length);
                                currentFrame.fpOrig = currentFrame.fp;
                                // This is a "next" frame -- even if it's
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
                    }

                    stats.Finish();

                    // Put root frame back:
                    currentFrame = staticFrame;
                    if (parent.index != null)
                    {
                        arc = parent.index.GetFirstArc(arcs[0]);
                        // Empty string prefix must have an output in the index!
                        //assert arc.isFinal();
                    }
                    else
                    {
                        arc = null;
                    }
                    currentFrame = PushFrame(arc, parent.rootCode, 0);
                    currentFrame.Rewind();
                    currentFrame.LoadBlock();
                    validIndexPrefix = 0;
                    term.length = 0;

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
                    //assert stack[ord].ord == ord;
                    return stack[ord];
                }

                private FST<BytesRef>.Arc<BytesRef> GetArc(int ord)
                {
                    if (ord >= arcs.Length)
                    {
                        FST<BytesRef>.Arc<BytesRef>[] next =
                            new FST<BytesRef>.Arc<BytesRef>[ArrayUtil.Oversize(1 + ord, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        Array.Copy(arcs, 0, next, 0, arcs.Length);
                        for (int arcOrd = arcs.Length; arcOrd < next.Length; arcOrd++)
                        {
                            next[arcOrd] = new FST<BytesRef>.Arc<BytesRef>();
                        }
                        arcs = next;
                    }
                    return arcs[ord];
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
                }

                // Pushes a frame we seek'd to
                internal Frame PushFrame(FST<BytesRef>.Arc<BytesRef> arc, BytesRef frameData, int length)
                {
                    scratchReader.Reset((byte[])(Array)frameData.bytes, frameData.offset, frameData.length);
                    long code = scratchReader.ReadVLong();
                    long fpSeek = Number.URShift(code, BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS);
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
                internal Frame PushFrame(FST<BytesRef>.Arc<BytesRef> arc, long fp, int length)
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
                        //assert length == f.prefix;
                    }
                    else
                    {
                        f.nextEnt = -1;
                        f.prefix = length;
                        f.state.termBlockOrd = 0;
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

                private bool ClearEOF()
                {
                    eof = false;
                    return true;
                }

                private bool SetEOF()
                {
                    eof = true;
                    return true;
                }

                public override bool SeekExact(BytesRef target, bool useCache)
                {
                    if (parent.index == null)
                    {
                        throw new InvalidOperationException("terms index was not loaded");
                    }

                    if (term.bytes.Length <= target.length)
                    {
                        term.bytes = ArrayUtil.Grow(term.bytes, 1 + target.length);
                    }

                    //assert clearEOF();

                    // if (DEBUG) {
                    //   System.out.println("\nBTTR.seekExact seg=" + segment + " target=" + fieldInfo.name + ":" + brToString(target) + " current=" + brToString(term) + " (exists?=" + termExists + ") validIndexPrefix=" + validIndexPrefix);
                    //   printSeekState();
                    // }

                    FST<BytesRef>.Arc<BytesRef> arc;
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
                        //assert arc.isFinal();
                        output = arc.Output;
                        targetUpto = 0;

                        Frame lastFrame = stack[0];
                        //assert validIndexPrefix <= term.length;

                        int targetLimit = Math.Min(target.length, validIndexPrefix);

                        int cmp = 0;

                        // TODO: reverse vLong byte order for better FST
                        // prefix output sharing

                        // First compare up to valid seek frames:
                        while (targetUpto < targetLimit)
                        {
                            cmp = (term.bytes[targetUpto] & 0xFF) - (target.bytes[target.offset + targetUpto] & 0xFF);
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
                            //assert arc.label == (target.bytes[target.offset + targetUpto] & 0xFF): "arc.label=" + (char) arc.label + " targetLabel=" + (char) (target.bytes[target.offset + targetUpto] & 0xFF);
                            if (arc.Output != parent.parent.NO_OUTPUT)
                            {
                                output = parent.parent.fstOutputs.Add(output, arc.Output);
                            }
                            if (arc.IsFinal())
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
                            int targetLimit2 = Math.Min(target.length, term.length);
                            while (targetUpto < targetLimit2)
                            {
                                cmp = (term.bytes[targetUpto] & 0xFF) - (target.bytes[target.offset + targetUpto] & 0xFF);
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
                                cmp = term.length - target.length;
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
                            //assert term.length == target.length;
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
                        arc = parent.index.GetFirstArc(arcs[0]);

                        // Empty string prefix must have an output (block) in the index!
                        //assert arc.isFinal();
                        //assert arc.output != null;

                        // if (DEBUG) {
                        //   System.out.println("    no seek state; push root frame");
                        // }

                        output = arc.Output;

                        currentFrame = staticFrame;

                        //term.length = 0;
                        targetUpto = 0;
                        currentFrame = PushFrame(arc, parent.parent.fstOutputs.Add(output, arc.NextFinalOutput), 0);
                    }

                    // if (DEBUG) {
                    //   System.out.println("  start index loop targetUpto=" + targetUpto + " output=" + output + " currentFrame.ord=" + currentFrame.ord + " targetBeforeCurrentLength=" + targetBeforeCurrentLength);
                    // }

                    while (targetUpto < target.length)
                    {

                        int targetLabel = target.bytes[target.offset + targetUpto] & 0xFF;

                        FST<BytesRef>.Arc<BytesRef> nextArc = parent.index.FindTargetArc(targetLabel, arc, GetArc(1 + targetUpto), fstReader);

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
                                term.bytes[targetUpto] = (sbyte)targetLabel;
                                term.length = 1 + targetUpto;
                                // if (DEBUG) {
                                //   System.out.println("  FAST NOT_FOUND term=" + brToString(term));
                                // }
                                return false;
                            }

                            currentFrame.LoadBlock();

                            SeekStatus result = currentFrame.ScanToTerm(target, true);
                            if (result == TermsEnum.SeekStatus.FOUND)
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
                            term.bytes[targetUpto] = (sbyte)targetLabel;
                            // Aggregate output as we go:
                            //assert arc.output != null;
                            if (arc.Output != parent.parent.NO_OUTPUT)
                            {
                                output = parent.parent.fstOutputs.Add(output, arc.Output);
                            }

                            // if (DEBUG) {
                            //   System.out.println("    index: follow label=" + toHex(target.bytes[target.offset + targetUpto]&0xff) + " arc.output=" + arc.output + " arc.nfo=" + arc.nextFinalOutput);
                            // }
                            targetUpto++;

                            if (arc.IsFinal())
                            {
                                //if (DEBUG) System.out.println("    arc is final!");
                                currentFrame = PushFrame(arc, parent.parent.fstOutputs.Add(output, arc.NextFinalOutput), targetUpto);
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
                        term.length = targetUpto;
                        // if (DEBUG) {
                        //   System.out.println("  FAST NOT_FOUND term=" + brToString(term));
                        // }
                        return false;
                    }

                    currentFrame.LoadBlock();

                    SeekStatus result2 = currentFrame.ScanToTerm(target, true);
                    if (result2 == SeekStatus.FOUND)
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

                public override SeekStatus SeekCeil(BytesRef target, bool useCache)
                {
                    if (parent.index == null)
                    {
                        throw new InvalidOperationException("terms index was not loaded");
                    }

                    if (term.bytes.Length <= target.length)
                    {
                        term.bytes = ArrayUtil.Grow(term.bytes, 1 + target.length);
                    }

                    //assert clearEOF();

                    //if (DEBUG) {
                    //System.out.println("\nBTTR.seekCeil seg=" + segment + " target=" + fieldInfo.name + ":" + target.utf8ToString() + " " + target + " current=" + brToString(term) + " (exists?=" + termExists + ") validIndexPrefix=  " + validIndexPrefix);
                    //printSeekState();
                    //}

                    FST<BytesRef>.Arc<BytesRef> arc;
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
                        //assert arc.isFinal();
                        output = arc.Output;
                        targetUpto = 0;

                        Frame lastFrame = stack[0];
                        //assert validIndexPrefix <= term.length;

                        int targetLimit = Math.Min(target.length, validIndexPrefix);

                        int cmp = 0;

                        // TOOD: we should write our vLong backwards (MSB
                        // first) to get better sharing from the FST

                        // First compare up to valid seek frames:
                        while (targetUpto < targetLimit)
                        {
                            cmp = (term.bytes[targetUpto] & 0xFF) - (target.bytes[target.offset + targetUpto] & 0xFF);
                            //if (DEBUG) {
                            //System.out.println("    cycle targetUpto=" + targetUpto + " (vs limit=" + targetLimit + ") cmp=" + cmp + " (targetLabel=" + (char) (target.bytes[target.offset + targetUpto]) + " vs termLabel=" + (char) (term.bytes[targetUpto]) + ")"   + " arc.output=" + arc.output + " output=" + output);
                            //}
                            if (cmp != 0)
                            {
                                break;
                            }
                            arc = arcs[1 + targetUpto];
                            //assert arc.label == (target.bytes[target.offset + targetUpto] & 0xFF): "arc.label=" + (char) arc.label + " targetLabel=" + (char) (target.bytes[target.offset + targetUpto] & 0xFF);
                            // TOOD: we could save the outputs in local
                            // byte[][] instead of making new objs ever
                            // seek; but, often the FST doesn't have any
                            // shared bytes (but this could change if we
                            // reverse vLong byte order)
                            if (arc.Output != parent.parent.NO_OUTPUT)
                            {
                                output = parent.parent.fstOutputs.Add(output, arc.Output);
                            }
                            if (arc.IsFinal())
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
                            int targetLimit2 = Math.Min(target.length, term.length);
                            while (targetUpto < targetLimit2)
                            {
                                cmp = (term.bytes[targetUpto] & 0xFF) - (target.bytes[target.offset + targetUpto] & 0xFF);
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
                                cmp = term.length - target.length;
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
                            //assert term.length == target.length;
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
                        arc = parent.index.GetFirstArc(arcs[0]);

                        // Empty string prefix must have an output (block) in the index!
                        //assert arc.isFinal();
                        //assert arc.output != null;

                        //if (DEBUG) {
                        //System.out.println("    no seek state; push root frame");
                        //}

                        output = arc.Output;

                        currentFrame = staticFrame;

                        //term.length = 0;
                        targetUpto = 0;
                        currentFrame = PushFrame(arc, parent.parent.fstOutputs.Add(output, arc.NextFinalOutput), 0);
                    }

                    //if (DEBUG) {
                    //System.out.println("  start index loop targetUpto=" + targetUpto + " output=" + output + " currentFrame.ord+1=" + currentFrame.ord + " targetBeforeCurrentLength=" + targetBeforeCurrentLength);
                    //}

                    while (targetUpto < target.length)
                    {

                        int targetLabel = target.bytes[target.offset + targetUpto] & 0xFF;

                        FST<BytesRef>.Arc<BytesRef> nextArc = parent.index.FindTargetArc(targetLabel, arc, GetArc(1 + targetUpto), fstReader);

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
                            term.bytes[targetUpto] = (sbyte)targetLabel;
                            arc = nextArc;
                            // Aggregate output as we go:
                            //assert arc.output != null;
                            if (arc.Output != parent.parent.NO_OUTPUT)
                            {
                                output = parent.parent.fstOutputs.Add(output, arc.Output);
                            }

                            //if (DEBUG) {
                            //System.out.println("    index: follow label=" + toHex(target.bytes[target.offset + targetUpto]&0xff) + " arc.output=" + arc.output + " arc.nfo=" + arc.nextFinalOutput);
                            //}
                            targetUpto++;

                            if (arc.IsFinal())
                            {
                                //if (DEBUG) System.out.println("    arc is final!");
                                currentFrame = PushFrame(arc, parent.parent.fstOutputs.Add(output, arc.NextFinalOutput), targetUpto);
                                //if (DEBUG) System.out.println("    curFrame.ord=" + currentFrame.ord + " hasTerms=" + currentFrame.hasTerms);
                            }
                        }
                    }

                    //validIndexPrefix = targetUpto;
                    validIndexPrefix = currentFrame.prefix;

                    currentFrame.ScanToFloorFrame(target);

                    currentFrame.LoadBlock();

                    SeekStatus result2 = currentFrame.ScanToTerm(target, false);

                    if (result2 == SeekStatus.END)
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
                        return result2;
                    }
                }

                //          private void PrintSeekState(PrintStream out) throws IOException {
                //  if (currentFrame == staticFrame) {
                //    out.println("  no prior seek");
                //  } else {
                //    out.println("  prior seek state:");
                //    int ord = 0;
                //    boolean isSeekFrame = true;
                //    while(true) {
                //      Frame f = getFrame(ord);
                //      assert f != null;
                //      final BytesRef prefix = new BytesRef(term.bytes, 0, f.prefix);
                //      if (f.nextEnt == -1) {
                //        out.println("    frame " + (isSeekFrame ? "(seek)" : "(next)") + " ord=" + ord + " fp=" + f.fp + (f.isFloor ? (" (fpOrig=" + f.fpOrig + ")") : "") + " prefixLen=" + f.prefix + " prefix=" + prefix + (f.nextEnt == -1 ? "" : (" (of " + f.entCount + ")")) + " hasTerms=" + f.hasTerms + " isFloor=" + f.isFloor + " code=" + ((f.fp<<BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) + (f.hasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) + (f.isFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0)) + " isLastInFloor=" + f.isLastInFloor + " mdUpto=" + f.metaDataUpto + " tbOrd=" + f.getTermBlockOrd());
                //      } else {
                //        out.println("    frame " + (isSeekFrame ? "(seek, loaded)" : "(next, loaded)") + " ord=" + ord + " fp=" + f.fp + (f.isFloor ? (" (fpOrig=" + f.fpOrig + ")") : "") + " prefixLen=" + f.prefix + " prefix=" + prefix + " nextEnt=" + f.nextEnt + (f.nextEnt == -1 ? "" : (" (of " + f.entCount + ")")) + " hasTerms=" + f.hasTerms + " isFloor=" + f.isFloor + " code=" + ((f.fp<<BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) + (f.hasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) + (f.isFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0)) + " lastSubFP=" + f.lastSubFP + " isLastInFloor=" + f.isLastInFloor + " mdUpto=" + f.metaDataUpto + " tbOrd=" + f.getTermBlockOrd());
                //      }
                //      if (index != null) {
                //        assert !isSeekFrame || f.arc != null: "isSeekFrame=" + isSeekFrame + " f.arc=" + f.arc;
                //        if (f.prefix > 0 && isSeekFrame && f.arc.label != (term.bytes[f.prefix-1]&0xFF)) {
                //          out.println("      broken seek state: arc.label=" + (char) f.arc.label + " vs term byte=" + (char) (term.bytes[f.prefix-1]&0xFF));
                //          throw new RuntimeException("seek state is broken");
                //        }
                //        BytesRef output = Util.get(index, prefix);
                //        if (output == null) {
                //          out.println("      broken seek state: prefix is not final in index");
                //          throw new RuntimeException("seek state is broken");
                //        } else if (isSeekFrame && !f.isFloor) {
                //          final ByteArrayDataInput reader = new ByteArrayDataInput(output.bytes, output.offset, output.length);
                //          final long codeOrig = reader.readVLong();
                //          final long code = (f.fp << BlockTreeTermsWriter.OUTPUT_FLAGS_NUM_BITS) | (f.hasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS:0) | (f.isFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR:0);
                //          if (codeOrig != code) {
                //            out.println("      broken seek state: output code=" + codeOrig + " doesn't match frame code=" + code);
                //            throw new RuntimeException("seek state is broken");
                //          }
                //        }
                //      }
                //      if (f == currentFrame) {
                //        break;
                //      }
                //      if (f.prefix == validIndexPrefix) {
                //        isSeekFrame = false;
                //      }
                //      ord++;
                //    }
                //  }
                //}

                public override BytesRef Next()
                {
                    if (input == null)
                    {
                        // Fresh TermsEnum; seek to first term:
                        FST<BytesRef>.Arc<BytesRef> arc;
                        if (parent.index != null)
                        {
                            arc = parent.index.GetFirstArc(arcs[0]);
                            // Empty string prefix must have an output in the index!
                            //assert arc.isFinal();
                        }
                        else
                        {
                            arc = null;
                        }
                        currentFrame = PushFrame(arc, parent.rootCode, 0);
                        currentFrame.LoadBlock();
                    }

                    targetBeforeCurrentLength = currentFrame.ord;

                    //assert !eof;
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
                        bool result = SeekExact(term, false);
                        //assert result;
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
                                //assert setEOF();
                                term.length = 0;
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
                            currentFrame = PushFrame(null, currentFrame.lastSubFP, term.length);
                            // This is a "next" frame -- even if it's
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
                        //assert !eof;
                        return term;
                    }
                }

                public override int DocFreq
                {
                    get
                    {
                        //assert !eof;
                        //if (DEBUG) System.out.println("BTR.docFreq");
                        currentFrame.DecodeMetaData();
                        //if (DEBUG) System.out.println("  return " + currentFrame.state.docFreq);
                        return currentFrame.state.docFreq;
                    }
                }

                public override long TotalTermFreq
                {
                    get
                    {
                        //assert !eof;
                        currentFrame.DecodeMetaData();
                        return currentFrame.state.totalTermFreq;
                    }
                }

                public override DocsEnum Docs(IBits skipDocs, DocsEnum reuse, int flags)
                {
                    //assert !eof;
                    //if (DEBUG) {
                    //System.out.println("BTTR.docs seg=" + segment);
                    //}
                    currentFrame.DecodeMetaData();
                    //if (DEBUG) {
                    //System.out.println("  state=" + currentFrame.state);
                    //}
                    return parent.parent.postingsReader.Docs(parent.fieldInfo, currentFrame.state, skipDocs, reuse, flags);
                }

                public override DocsAndPositionsEnum DocsAndPositions(IBits skipDocs, DocsAndPositionsEnum reuse, int flags)
                {
                    if (parent.fieldInfo.IndexOptionsValue.GetValueOrDefault() < IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
                    {
                        // Positions were not indexed:
                        return null;
                    }

                    //assert !eof;
                    currentFrame.DecodeMetaData();
                    return parent.parent.postingsReader.DocsAndPositions(parent.fieldInfo, currentFrame.state, skipDocs, reuse, flags);
                }

                public override void SeekExact(BytesRef target, TermState otherState)
                {
                    // if (DEBUG) {
                    //   System.out.println("BTTR.seekExact termState seg=" + segment + " target=" + target.utf8ToString() + " " + target + " state=" + otherState);
                    // }
                    //assert clearEOF();
                    if (target.CompareTo(term) != 0 || !termExists)
                    {
                        //assert otherState != null && otherState instanceof BlockTermState;
                        currentFrame = staticFrame;
                        currentFrame.state.CopyFrom(otherState);
                        term.CopyBytes(target);
                        currentFrame.metaDataUpto = currentFrame.TermBlockOrd;
                        //assert currentFrame.metaDataUpto > 0;
                        validIndexPrefix = 0;
                    }
                    else
                    {
                        // if (DEBUG) {
                        //   System.out.println("  skip seek: already on target state=" + currentFrame.state);
                        // }
                    }
                }

                public override TermState TermState
                {
                    get
                    {
                        //assert !eof;
                        currentFrame.DecodeMetaData();
                        TermState ts = (TermState)currentFrame.state.Clone();
                        //if (DEBUG) System.out.println("BTTR.termState seg=" + segment + " state=" + ts);
                        return ts;
                    }
                }

                public override void SeekExact(long ord)
                {
                    throw new NotSupportedException();
                }

                public override long Ord
                {
                    get { throw new NotSupportedException(); }
                }

                internal sealed class Frame
                {
                    // Our index in stack[]:
                    internal readonly int ord;

                    internal bool hasTerms;
                    internal bool hasTermsOrig;
                    internal bool isFloor;

                    internal FST<BytesRef>.Arc<BytesRef> arc;

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

                    private readonly SegmentTermsEnum parent;

                    public Frame(SegmentTermsEnum parent, int ord)
                    {
                        this.parent = parent;
                        this.ord = ord;
                        state = parent.parent.parent.postingsReader.NewTermState();
                        state.totalTermFreq = -1;
                    }

                    public void SetFloorData(ByteArrayDataInput input, BytesRef source)
                    {
                        int numBytes = source.length - (input.Position - source.offset);
                        if (numBytes > floorData.Length)
                        {
                            floorData = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        Array.Copy(source.bytes, source.offset + input.Position, floorData, 0, numBytes);
                        floorDataReader.Reset(floorData, 0, numBytes);
                        numFollowFloorBlocks = floorDataReader.ReadVInt();
                        nextFloorLabel = floorDataReader.ReadByte() & 0xff;
                        //if (DEBUG) {
                        //System.out.println("    setFloorData fpOrig=" + fpOrig + " bytes=" + new BytesRef(source.bytes, source.offset + in.getPosition(), numBytes) + " numFollowFloorBlocks=" + numFollowFloorBlocks + " nextFloorLabel=" + toHex(nextFloorLabel));
                        //}
                    }

                    public int TermBlockOrd
                    {
                        get { return isLeafBlock ? nextEnt : state.termBlockOrd; }
                    }

                    internal void LoadNextFloorBlock()
                    {
                        //if (DEBUG) {
                        //System.out.println("    loadNextFloorBlock fp=" + fp + " fpEnd=" + fpEnd);
                        //}
                        //assert arc == null || isFloor: "arc=" + arc + " isFloor=" + isFloor;
                        fp = fpEnd;
                        nextEnt = -1;
                        LoadBlock();
                    }

                    internal void LoadBlock()
                    {
                        // Clone the IndexInput lazily, so that consumers
                        // that just pull a TermsEnum to
                        // seekExact(TermState) don't pay this cost:
                        parent.InitIndexInput();

                        if (nextEnt != -1)
                        {
                            // Already loaded
                            return;
                        }
                        //System.out.println("blc=" + blockLoadCount);

                        parent.input.Seek(fp);
                        int code = parent.input.ReadVInt();
                        entCount = Number.URShift(code, 1);
                        //assert entCount > 0;
                        isLastInFloor = (code & 1) != 0;
                        //assert arc == null || (isLastInFloor || isFloor);

                        // TODO: if suffixes were stored in random-access
                        // array structure, then we could do binary search
                        // instead of linear scan to find target term; eg
                        // we could have simple array of offsets

                        // term suffixes:
                        code = parent.input.ReadVInt();
                        isLeafBlock = (code & 1) != 0;
                        int numBytes = Number.URShift(code, 1);
                        if (suffixBytes.Length < numBytes)
                        {
                            suffixBytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        parent.input.ReadBytes(suffixBytes, 0, numBytes);
                        suffixesReader.Reset(suffixBytes, 0, numBytes);

                        /*if (DEBUG) {
                          if (arc == null) {
                            System.out.println("    loadBlock (next) fp=" + fp + " entCount=" + entCount + " prefixLen=" + prefix + " isLastInFloor=" + isLastInFloor + " leaf?=" + isLeafBlock);
                          } else {
                            System.out.println("    loadBlock (seek) fp=" + fp + " entCount=" + entCount + " prefixLen=" + prefix + " hasTerms?=" + hasTerms + " isFloor?=" + isFloor + " isLastInFloor=" + isLastInFloor + " leaf?=" + isLeafBlock);
                          }
                          }*/

                        // stats
                        numBytes = parent.input.ReadVInt();
                        if (statBytes.Length < numBytes)
                        {
                            statBytes = new byte[ArrayUtil.Oversize(numBytes, 1)];
                        }
                        parent.input.ReadBytes(statBytes, 0, numBytes);
                        statsReader.Reset(statBytes, 0, numBytes);
                        metaDataUpto = 0;

                        state.termBlockOrd = 0;
                        nextEnt = 0;
                        lastSubFP = -1;

                        // TODO: we could skip this if !hasTerms; but
                        // that's rare so won't help much
                        parent.parent.parent.postingsReader.ReadTermsBlock(parent.input, parent.parent.fieldInfo, state);

                        // Sub-blocks of a single floor block are always
                        // written one after another -- tail recurse:
                        fpEnd = parent.input.FilePointer;
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

                    public bool NextLeaf()
                    {
                        //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
                        //assert nextEnt != -1 && nextEnt < entCount: "nextEnt=" + nextEnt + " entCount=" + entCount + " fp=" + fp;
                        nextEnt++;
                        suffix = suffixesReader.ReadVInt();
                        startBytePos = suffixesReader.Position;
                        parent.term.length = prefix + suffix;
                        if (parent.term.bytes.Length < parent.term.length)
                        {
                            parent.term.Grow(parent.term.length);
                        }
                        suffixesReader.ReadBytes(parent.term.bytes, prefix, suffix);
                        // A normal term
                        parent.termExists = true;
                        return false;
                    }

                    public bool NextNonLeaf()
                    {
                        //if (DEBUG) System.out.println("  frame.next ord=" + ord + " nextEnt=" + nextEnt + " entCount=" + entCount);
                        //assert nextEnt != -1 && nextEnt < entCount: "nextEnt=" + nextEnt + " entCount=" + entCount + " fp=" + fp;
                        nextEnt++;
                        int code = suffixesReader.ReadVInt();
                        suffix = Number.URShift(code, 1);
                        startBytePos = suffixesReader.Position;
                        parent.term.length = prefix + suffix;
                        if (parent.term.bytes.Length < parent.term.length)
                        {
                            parent.term.Grow(parent.term.length);
                        }
                        suffixesReader.ReadBytes(parent.term.bytes, prefix, suffix);
                        if ((code & 1) == 0)
                        {
                            // A normal term
                            parent.termExists = true;
                            subCode = 0;
                            state.termBlockOrd++;
                            return false;
                        }
                        else
                        {
                            // A sub-block; make sub-FP absolute:
                            parent.termExists = false;
                            subCode = suffixesReader.ReadVLong();
                            lastSubFP = fp - subCode;
                            //if (DEBUG) {
                            //System.out.println("    lastSubFP=" + lastSubFP);
                            //}
                            return true;
                        }
                    }

                    public void ScanToFloorFrame(BytesRef target)
                    {

                        if (!isFloor || target.length <= prefix)
                        {
                            // if (DEBUG) {
                            //   System.out.println("    scanToFloorFrame skip: isFloor=" + isFloor + " target.length=" + target.length + " vs prefix=" + prefix);
                            // }
                            return;
                        }

                        int targetLabel = target.bytes[target.offset + prefix] & 0xFF;

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

                        //assert numFollowFloorBlocks != 0;

                        long newFP = fpOrig;
                        while (true)
                        {
                            long code = floorDataReader.ReadVLong();
                            newFP = fpOrig + Number.URShift(code, 1);
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
                        //assert limit > 0;

                        // We must set/incr state.termCount because
                        // postings impl can look at this
                        state.termBlockOrd = metaDataUpto;

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
                            state.docFreq = statsReader.ReadVInt();
                            //if (DEBUG) System.out.println("    dF=" + state.docFreq);
                            if (parent.parent.fieldInfo.IndexOptionsValue.GetValueOrDefault() != IndexOptions.DOCS_ONLY)
                            {
                                state.totalTermFreq = state.docFreq + statsReader.ReadVLong();
                                //if (DEBUG) System.out.println("    totTF=" + state.totalTermFreq);
                            }

                            parent.parent.parent.postingsReader.NextTerm(parent.parent.fieldInfo, state);
                            metaDataUpto++;
                            state.termBlockOrd++;
                        }
                    }

                    private bool PrefixMatches(BytesRef target)
                    {
                        for (int bytePos = 0; bytePos < prefix; bytePos++)
                        {
                            if (target.bytes[target.offset + bytePos] != parent.term.bytes[bytePos])
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    public void ScanToSubBlock(long subFP)
                    {
                        //assert !isLeafBlock;
                        //if (DEBUG) System.out.println("  scanToSubBlock fp=" + fp + " subFP=" + subFP + " entCount=" + entCount + " lastSubFP=" + lastSubFP);
                        //assert nextEnt == 0;
                        if (lastSubFP == subFP)
                        {
                            //if (DEBUG) System.out.println("    already positioned");
                            return;
                        }
                        //assert subFP < fp : "fp=" + fp + " subFP=" + subFP;
                        long targetSubCode = fp - subFP;
                        //if (DEBUG) System.out.println("    targetSubCode=" + targetSubCode);
                        while (true)
                        {
                            //assert nextEnt < entCount;
                            nextEnt++;
                            int code = suffixesReader.ReadVInt();
                            suffixesReader.SkipBytes(isLeafBlock ? code : Number.URShift(code, 1));
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
                                state.termBlockOrd++;
                            }
                        }
                    }

                    public SeekStatus ScanToTerm(BytesRef target, bool exactOnly)
                    {
                        return isLeafBlock ? ScanToTermLeaf(target, exactOnly) : ScanToTermNonLeaf(target, exactOnly);
                    }

                    private int startBytePos;
                    private int suffix;
                    private long subCode;

                    public SeekStatus ScanToTermLeaf(BytesRef target, bool exactOnly)
                    {

                        // if (DEBUG) System.out.println("    scanToTermLeaf: block fp=" + fp + " prefix=" + prefix + " nextEnt=" + nextEnt + " (of " + entCount + ") target=" + brToString(target) + " term=" + brToString(term));

                        //assert nextEnt != -1;

                        parent.termExists = true;
                        subCode = 0;

                        if (nextEnt == entCount)
                        {
                            if (exactOnly)
                            {
                                FillTerm();
                            }
                            return SeekStatus.END;
                        }

                        //assert prefixMatches(target);

                        // Loop over each entry (term or sub-block) in this block:
                        //nextTerm: while(nextEnt < entCount) {
                        bool breakNextTerm = false;
                        bool continueNextTerm = false;
                        while (true)
                        {
                            breakNextTerm = false;
                            continueNextTerm = false;
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

                            int targetLimit = target.offset + (target.length < termLen ? target.length : termLen);
                            int targetPos = target.offset + prefix;

                            // Loop over bytes in the suffix, comparing to
                            // the target
                            int bytePos = startBytePos;
                            while (true)
                            {
                                int cmp;
                                bool stop;
                                if (targetPos < targetLimit)
                                {
                                    cmp = (suffixBytes[bytePos++] & 0xFF) - (target.bytes[targetPos++] & 0xFF);
                                    stop = false;
                                }
                                else
                                {
                                    //assert targetPos == targetLimit;
                                    cmp = termLen - target.length;
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
                                        breakNextTerm = true;
                                        break;
                                    }
                                    else
                                    {
                                        continueNextTerm = true;
                                        break;
                                    }
                                }
                                else if (cmp > 0)
                                {

                                    // Done!  Current entry is after target --
                                    // return NOT_FOUND:
                                    FillTerm();

                                    if (!exactOnly && !parent.termExists)
                                    {
                                        // We are on a sub-block, and caller wants
                                        // us to position to the next term after
                                        // the target, so we must recurse into the
                                        // sub-frame(s):
                                        parent.currentFrame = parent.PushFrame(null, parent.currentFrame.lastSubFP, termLen);
                                        parent.currentFrame.LoadBlock();
                                        while (parent.currentFrame.Next())
                                        {
                                            parent.currentFrame = parent.PushFrame(null, parent.currentFrame.lastSubFP, parent.term.length);
                                            parent.currentFrame.LoadBlock();
                                        }
                                    }

                                    //if (DEBUG) System.out.println("        not found");
                                    return SeekStatus.NOT_FOUND;
                                }
                                else if (stop)
                                {
                                    // Exact match!

                                    // This cannot be a sub-block because we
                                    // would have followed the index to this
                                    // sub-block from the start:

                                    //assert termExists;
                                    FillTerm();
                                    //if (DEBUG) System.out.println("        found!");
                                    return SeekStatus.FOUND;
                                }
                            }

                            if (breakNextTerm)
                                break;
                            if (continueNextTerm)
                                continue;
                        }

                        // It is possible (and OK) that terms index pointed us
                        // at this block, but, we scanned the entire block and
                        // did not find the term to position to.  This happens
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

                    public SeekStatus ScanToTermNonLeaf(BytesRef target, bool exactOnly)
                    {

                        //if (DEBUG) System.out.println("    scanToTermNonLeaf: block fp=" + fp + " prefix=" + prefix + " nextEnt=" + nextEnt + " (of " + entCount + ") target=" + brToString(target) + " term=" + brToString(term));

                        //assert nextEnt != -1;

                        if (nextEnt == entCount)
                        {
                            if (exactOnly)
                            {
                                FillTerm();
                                parent.termExists = subCode == 0;
                            }
                            return SeekStatus.END;
                        }

                        //assert prefixMatches(target);

                        // Loop over each entry (term or sub-block) in this block:
                        //nextTerm: while(nextEnt < entCount) {
                        bool breakNextTerm = false;
                        bool continueNextTerm = false;
                        while (true)
                        {
                            breakNextTerm = false;
                            continueNextTerm = false;
                            nextEnt++;

                            int code = suffixesReader.ReadVInt();
                            suffix = Number.URShift(code, 1);
                            // if (DEBUG) {
                            //   BytesRef suffixBytesRef = new BytesRef();
                            //   suffixBytesRef.bytes = suffixBytes;
                            //   suffixBytesRef.offset = suffixesReader.getPosition();
                            //   suffixBytesRef.length = suffix;
                            //   System.out.println("      cycle: " + ((code&1)==1 ? "sub-block" : "term") + " " + (nextEnt-1) + " (of " + entCount + ") suffix=" + brToString(suffixBytesRef));
                            // }

                            parent.termExists = (code & 1) == 0;
                            int termLen = prefix + suffix;
                            startBytePos = suffixesReader.Position;
                            suffixesReader.SkipBytes(suffix);
                            if (parent.termExists)
                            {
                                state.termBlockOrd++;
                                subCode = 0;
                            }
                            else
                            {
                                subCode = suffixesReader.ReadVLong();
                                lastSubFP = fp - subCode;
                            }

                            int targetLimit = target.offset + (target.length < termLen ? target.length : termLen);
                            int targetPos = target.offset + prefix;

                            // Loop over bytes in the suffix, comparing to
                            // the target
                            int bytePos = startBytePos;
                            while (true)
                            {
                                int cmp;
                                bool stop;
                                if (targetPos < targetLimit)
                                {
                                    cmp = (suffixBytes[bytePos++] & 0xFF) - (target.bytes[targetPos++] & 0xFF);
                                    stop = false;
                                }
                                else
                                {
                                    //assert targetPos == targetLimit;
                                    cmp = termLen - target.length;
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
                                        breakNextTerm = true;
                                        break;
                                    }
                                    else
                                    {
                                        continueNextTerm = true;
                                        break;
                                    }
                                }
                                else if (cmp > 0)
                                {

                                    // Done!  Current entry is after target --
                                    // return NOT_FOUND:
                                    FillTerm();

                                    if (!exactOnly && !parent.termExists)
                                    {
                                        // We are on a sub-block, and caller wants
                                        // us to position to the next term after
                                        // the target, so we must recurse into the
                                        // sub-frame(s):
                                        parent.currentFrame = parent.PushFrame(null, parent.currentFrame.lastSubFP, termLen);
                                        parent.currentFrame.LoadBlock();
                                        while (parent.currentFrame.Next())
                                        {
                                            parent.currentFrame = parent.PushFrame(null, parent.currentFrame.lastSubFP, parent.term.length);
                                            parent.currentFrame.LoadBlock();
                                        }
                                    }

                                    //if (DEBUG) System.out.println("        not found");
                                    return SeekStatus.NOT_FOUND;
                                }
                                else if (stop)
                                {
                                    // Exact match!

                                    // This cannot be a sub-block because we
                                    // would have followed the index to this
                                    // sub-block from the start:

                                    //assert termExists;
                                    FillTerm();
                                    //if (DEBUG) System.out.println("        found!");
                                    return SeekStatus.FOUND;
                                }
                            }

                            if (breakNextTerm)
                                break;
                            if (continueNextTerm)
                                continue;
                        }

                        // It is possible (and OK) that terms index pointed us
                        // at this block, but, we scanned the entire block and
                        // did not find the term to position to.  This happens
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
                        parent.term.length = prefix + suffix;
                        if (parent.term.bytes.Length < termLength)
                        {
                            parent.term.Grow(termLength);
                        }
                        Array.Copy(suffixBytes, startBytePos, parent.term.bytes, prefix, suffix);
                    }
                }

            }
        }
    }
}
