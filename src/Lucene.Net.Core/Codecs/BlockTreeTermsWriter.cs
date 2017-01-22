using Lucene.Net.Support;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    using ByteSequenceOutputs = Lucene.Net.Util.Fst.ByteSequenceOutputs;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IntsRef = Lucene.Net.Util.IntsRef;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using NoOutputs = Lucene.Net.Util.Fst.NoOutputs;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using Util = Lucene.Net.Util.Fst.Util;

    /*
      TODO:

        - Currently there is a one-to-one mapping of indexed
          term to term block, but we could decouple the two, ie,
          put more terms into the index than there are blocks.
          The index would take up more RAM but then it'd be able
          to avoid seeking more often and could make PK/FuzzyQ
          faster if the additional indexed terms could store
          the offset into the terms block.

        - The blocks are not written in true depth-first
          order, meaning if you just next() the file pointer will
          sometimes jump backwards.  For example, block foo* will
          be written before block f* because it finished before.
          this could possibly hurt performance if the terms dict is
          not hot, since OSs anticipate sequential file access.  We
          could fix the writer to re-order the blocks as a 2nd
          pass.

        - Each block encodes the term suffixes packed
          sequentially using a separate vInt per term, which is
          1) wasteful and 2) slow (must linear scan to find a
          particular suffix).  We should instead 1) make
          random-access array so we can directly access the Nth
          suffix, and 2) bulk-encode this array using bulk int[]
          codecs; then at search time we can binary search when
          we seek a particular term.
    */

    /// <summary>
    /// Block-based terms index and dictionary writer.
    /// <p>
    /// Writes terms dict and index, block-encoding (column
    /// stride) each term's metadata for each set of terms
    /// between two index terms.
    /// <p>
    /// Files:
    /// <ul>
    ///   <li><tt>.tim</tt>: <a href="#Termdictionary">Term Dictionary</a></li>
    ///   <li><tt>.tip</tt>: <a href="#Termindex">Term Index</a></li>
    /// </ul>
    /// <p>
    /// <a name="Termdictionary" id="Termdictionary"></a>
    /// <h3>Term Dictionary</h3>
    ///
    /// <p>The .tim file contains the list of terms in each
    /// field along with per-term statistics (such as docfreq)
    /// and per-term metadata (typically pointers to the postings list
    /// for that term in the inverted index).
    /// </p>
    ///
    /// <p>The .tim is arranged in blocks: with blocks containing
    /// a variable number of entries (by default 25-48), where
    /// each entry is either a term or a reference to a
    /// sub-block.</p>
    ///
    /// <p>NOTE: The term dictionary can plug into different postings implementations:
    /// the postings writer/reader are actually responsible for encoding
    /// and decoding the Postings Metadata and Term Metadata sections.</p>
    ///
    /// <ul>
    ///    <li>TermsDict (.tim) --&gt; Header, <i>PostingsHeader</i>, NodeBlock<sup>NumBlocks</sup>,
    ///                               FieldSummary, DirOffset, Footer</li>
    ///    <li>NodeBlock --&gt; (OuterNode | InnerNode)</li>
    ///    <li>OuterNode --&gt; EntryCount, SuffixLength, Byte<sup>SuffixLength</sup>, StatsLength, &lt; TermStats &gt;<sup>EntryCount</sup>, MetaLength, &lt;<i>TermMetadata</i>&gt;<sup>EntryCount</sup></li>
    ///    <li>InnerNode --&gt; EntryCount, SuffixLength[,Sub?], Byte<sup>SuffixLength</sup>, StatsLength, &lt; TermStats ? &gt;<sup>EntryCount</sup>, MetaLength, &lt;<i>TermMetadata ? </i>&gt;<sup>EntryCount</sup></li>
    ///    <li>TermStats --&gt; DocFreq, TotalTermFreq </li>
    ///    <li>FieldSummary --&gt; NumFields, &lt;FieldNumber, NumTerms, RootCodeLength, Byte<sup>RootCodeLength</sup>,
    ///                            SumTotalTermFreq?, SumDocFreq, DocCount&gt;<sup>NumFields</sup></li>
    ///    <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///    <li>DirOffset --&gt; <seealso cref="DataOutput#writeLong Uint64"/></li>
    ///    <li>EntryCount,SuffixLength,StatsLength,DocFreq,MetaLength,NumFields,
    ///        FieldNumber,RootCodeLength,DocCount --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///    <li>TotalTermFreq,NumTerms,SumTotalTermFreq,SumDocFreq --&gt;
    ///        <seealso cref="DataOutput#writeVLong VLong"/></li>
    ///    <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    /// </ul>
    /// <p>Notes:</p>
    /// <ul>
    ///    <li>Header is a <seealso cref="CodecUtil#writeHeader CodecHeader"/> storing the version information
    ///        for the BlockTree implementation.</li>
    ///    <li>DirOffset is a pointer to the FieldSummary section.</li>
    ///    <li>DocFreq is the count of documents which contain the term.</li>
    ///    <li>TotalTermFreq is the total number of occurrences of the term. this is encoded
    ///        as the difference between the total number of occurrences and the DocFreq.</li>
    ///    <li>FieldNumber is the fields number from <seealso cref="fieldInfos"/>. (.fnm)</li>
    ///    <li>NumTerms is the number of unique terms for the field.</li>
    ///    <li>RootCode points to the root block for the field.</li>
    ///    <li>SumDocFreq is the total number of postings, the number of term-document pairs across
    ///        the entire field.</li>
    ///    <li>DocCount is the number of documents that have at least one posting for this field.</li>
    ///    <li>PostingsHeader and TermMetadata are plugged into by the specific postings implementation:
    ///        these contain arbitrary per-file data (such as parameters or versioning information)
    ///        and per-term data (such as pointers to inverted files).</li>
    ///    <li>For inner nodes of the tree, every entry will steal one bit to mark whether it points
    ///        to child nodes(sub-block). If so, the corresponding TermStats and TermMetaData are omitted </li>
    /// </ul>
    /// <a name="Termindex" id="Termindex"></a>
    /// <h3>Term Index</h3>
    /// <p>The .tip file contains an index into the term dictionary, so that it can be
    /// accessed randomly.  The index is also used to determine
    /// when a given term cannot exist on disk (in the .tim file), saving a disk seek.</p>
    /// <ul>
    ///   <li>TermsIndex (.tip) --&gt; Header, FSTIndex<sup>NumFields</sup>
    ///                                &lt;IndexStartFP&gt;<sup>NumFields</sup>, DirOffset, Footer</li>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///   <li>DirOffset --&gt; <seealso cref="DataOutput#writeLong Uint64"/></li>
    ///   <li>IndexStartFP --&gt; <seealso cref="DataOutput#writeVLong VLong"/></li>
    ///   <!-- TODO: better describe FST output here -->
    ///   <li>FSTIndex --&gt; <seealso cref="FST FST&lt;byte[]&gt;"/></li>
    ///   <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    /// </ul>
    /// <p>Notes:</p>
    /// <ul>
    ///   <li>The .tip file contains a separate FST for each
    ///       field.  The FST maps a term prefix to the on-disk
    ///       block that holds all terms starting with that
    ///       prefix.  Each field's IndexStartFP points to its
    ///       FST.</li>
    ///   <li>DirOffset is a pointer to the start of the IndexStartFPs
    ///       for all fields</li>
    ///   <li>It's possible that an on-disk block would contain
    ///       too many terms (more than the allowed maximum
    ///       (default: 48)).  When this happens, the block is
    ///       sub-divided into new blocks (called "floor
    ///       blocks"), and then the output in the FST for the
    ///       block's prefix encodes the leading byte of each
    ///       sub-block, and its file pointer.
    /// </ul>
    /// </summary>
    /// <seealso cref= BlockTreeTermsReader
    /// @lucene.experimental </seealso>
    public class BlockTreeTermsWriter : FieldsConsumer
    {
        /// <summary>
        /// Suggested default value for the {@code
        ///  minItemsInBlock} parameter to {@link
        ///  #BlockTreeTermsWriter(SegmentWriteState,PostingsWriterBase,int,int)}.
        /// </summary>
        public const int DEFAULT_MIN_BLOCK_SIZE = 25;

        /// <summary>
        /// Suggested default value for the {@code
        ///  maxItemsInBlock} parameter to {@link
        ///  #BlockTreeTermsWriter(SegmentWriteState,PostingsWriterBase,int,int)}.
        /// </summary>
        public const int DEFAULT_MAX_BLOCK_SIZE = 48;

        //public final static boolean DEBUG = false;
        //private final static boolean SAVE_DOT_FILES = false;

        internal const int OUTPUT_FLAGS_NUM_BITS = 2;
        internal const int OUTPUT_FLAGS_MASK = 0x3;
        internal const int OUTPUT_FLAG_IS_FLOOR = 0x1;
        internal const int OUTPUT_FLAG_HAS_TERMS = 0x2;

        /// <summary>
        /// Extension of terms file </summary>
        internal const string TERMS_EXTENSION = "tim";

        internal const string TERMS_CODEC_NAME = "BLOCK_TREE_TERMS_DICT";

        /// <summary>
        /// Initial terms format. </summary>
        public const int VERSION_START = 0;

        /// <summary>
        /// Append-only </summary>
        public const int VERSION_APPEND_ONLY = 1;

        /// <summary>
        /// Meta data as array </summary>
        public const int VERSION_META_ARRAY = 2;

        /// <summary>
        /// checksums </summary>
        public const int VERSION_CHECKSUM = 3;

        /// <summary>
        /// Current terms format. </summary>
        public const int VERSION_CURRENT = VERSION_CHECKSUM;

        /// <summary>
        /// Extension of terms index file </summary>
        internal const string TERMS_INDEX_EXTENSION = "tip";

        internal const string TERMS_INDEX_CODEC_NAME = "BLOCK_TREE_TERMS_INDEX";

        private readonly IndexOutput @out;
        private readonly IndexOutput indexOut;
        internal readonly int minItemsInBlock;
        internal readonly int maxItemsInBlock;

        internal readonly PostingsWriterBase postingsWriter;
        internal readonly FieldInfos fieldInfos;
        internal FieldInfo currentField;

        private class FieldMetaData
        {
            public FieldInfo FieldInfo { get; private set; }
            public BytesRef RootCode { get; private set; }
            public long NumTerms { get; private set; }
            public long IndexStartFP { get; private set; }
            public long SumTotalTermFreq { get; private set; }
            public long SumDocFreq { get; private set; }
            public int DocCount { get; private set; }
            internal int LongsSize { get; private set; }

            public FieldMetaData(FieldInfo fieldInfo, BytesRef rootCode, long numTerms, long indexStartFP, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize)
            {
                Debug.Assert(numTerms > 0);
                this.FieldInfo = fieldInfo;
                Debug.Assert(rootCode != null, "field=" + fieldInfo.Name + " numTerms=" + numTerms);
                this.RootCode = rootCode;
                this.IndexStartFP = indexStartFP;
                this.NumTerms = numTerms;
                this.SumTotalTermFreq = sumTotalTermFreq;
                this.SumDocFreq = sumDocFreq;
                this.DocCount = docCount;
                this.LongsSize = longsSize;
            }
        }

        private readonly IList<FieldMetaData> fields = new List<FieldMetaData>();
        // private final String segment;

        /// <summary>
        /// Create a new writer.  The number of items (terms or
        ///  sub-blocks) per block will aim to be between
        ///  minItemsPerBlock and maxItemsPerBlock, though in some
        ///  cases the blocks may be smaller than the min.
        /// </summary>
        public BlockTreeTermsWriter(SegmentWriteState state, PostingsWriterBase postingsWriter, int minItemsInBlock, int maxItemsInBlock)
        {
            if (minItemsInBlock <= 1)
            {
                throw new System.ArgumentException("minItemsInBlock must be >= 2; got " + minItemsInBlock);
            }
            if (maxItemsInBlock <= 0)
            {
                throw new System.ArgumentException("maxItemsInBlock must be >= 1; got " + maxItemsInBlock);
            }
            if (minItemsInBlock > maxItemsInBlock)
            {
                throw new System.ArgumentException("maxItemsInBlock must be >= minItemsInBlock; got maxItemsInBlock=" + maxItemsInBlock + " minItemsInBlock=" + minItemsInBlock);
            }
            if (2 * (minItemsInBlock - 1) > maxItemsInBlock)
            {
                throw new System.ArgumentException("maxItemsInBlock must be at least 2*(minItemsInBlock-1); got maxItemsInBlock=" + maxItemsInBlock + " minItemsInBlock=" + minItemsInBlock);
            }

            string termsFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, TERMS_EXTENSION);
            @out = state.Directory.CreateOutput(termsFileName, state.Context);
            bool success = false;
            IndexOutput indexOut = null;
            try
            {
                fieldInfos = state.FieldInfos;
                this.minItemsInBlock = minItemsInBlock;
                this.maxItemsInBlock = maxItemsInBlock;
                WriteHeader(@out);

                //DEBUG = state.segmentName.equals("_4a");

                string termsIndexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, TERMS_INDEX_EXTENSION);
                indexOut = state.Directory.CreateOutput(termsIndexFileName, state.Context);
                WriteIndexHeader(indexOut);

                currentField = null;
                this.postingsWriter = postingsWriter;
                // segment = state.segmentName;

                // System.out.println("BTW.init seg=" + state.segmentName);

                postingsWriter.Init(@out); // have consumer write its format/header
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(@out, indexOut);
                }
            }
            this.indexOut = indexOut;
        }

        /// <summary>
        /// Writes the terms file header. </summary>
        protected internal virtual void WriteHeader(IndexOutput @out)
        {
            CodecUtil.WriteHeader(@out, TERMS_CODEC_NAME, VERSION_CURRENT);
        }

        /// <summary>
        /// Writes the index file header. </summary>
        protected internal virtual void WriteIndexHeader(IndexOutput @out)
        {
            CodecUtil.WriteHeader(@out, TERMS_INDEX_CODEC_NAME, VERSION_CURRENT);
        }

        /// <summary>
        /// Writes the terms file trailer. </summary>
        protected internal virtual void WriteTrailer(IndexOutput @out, long dirStart)
        {
            @out.WriteLong(dirStart);
        }

        /// <summary>
        /// Writes the index file trailer. </summary>
        protected internal virtual void WriteIndexTrailer(IndexOutput indexOut, long dirStart)
        {
            indexOut.WriteLong(dirStart);
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            //DEBUG = field.name.equals("id");
            //if (DEBUG) System.out.println("\nBTTW.addField seg=" + segment + " field=" + field.name);
            Debug.Assert(currentField == null || currentField.Name.CompareTo(field.Name) < 0);
            currentField = field;
            return new TermsWriter(this, field);
        }

        internal static long EncodeOutput(long fp, bool hasTerms, bool isFloor)
        {
            Debug.Assert(fp < (1L << 62));
            return (fp << 2) | (hasTerms ? OUTPUT_FLAG_HAS_TERMS : 0) | (isFloor ? OUTPUT_FLAG_IS_FLOOR : 0);
        }

        private class PendingEntry
        {
            public bool IsTerm { get; private set; }

            protected internal PendingEntry(bool isTerm)
            {
                this.IsTerm = isTerm;
            }
        }

        private sealed class PendingTerm : PendingEntry
        {
            public BytesRef Term { get; private set; }

            // stats + metadata
            public BlockTermState State { get; private set; }

            public PendingTerm(BytesRef term, BlockTermState state)
                : base(true)
            {
                this.Term = term;
                this.State = state;
            }

            public override string ToString()
            {
                return Term.Utf8ToString();
            }
        }

        private sealed class PendingBlock : PendingEntry
        {
            public BytesRef Prefix { get; private set; }
            public long Fp { get; private set; }
            public FST<BytesRef> Index { get; set; }
            public IList<FST<BytesRef>> SubIndices { get; set; }
            public bool HasTerms { get; private set; }
            public bool IsFloor { get; private set; }
            public int FloorLeadByte { get; private set; }
            private readonly IntsRef scratchIntsRef = new IntsRef();

            public PendingBlock(BytesRef prefix, long fp, bool hasTerms, bool isFloor, int floorLeadByte, IList<FST<BytesRef>> subIndices)
                : base(false)
            {
                this.Prefix = prefix;
                this.Fp = fp;
                this.HasTerms = hasTerms;
                this.IsFloor = isFloor;
                this.FloorLeadByte = floorLeadByte;
                this.SubIndices = subIndices;
            }

            public override string ToString()
            {
                return "BLOCK: " + Prefix.Utf8ToString();
            }

            public void CompileIndex(IList<PendingBlock> floorBlocks, RAMOutputStream scratchBytes)
            {
                Debug.Assert((IsFloor && floorBlocks != null && floorBlocks.Count != 0) || (!IsFloor && floorBlocks == null), "isFloor=" + IsFloor + " floorBlocks=" + floorBlocks);

                Debug.Assert(scratchBytes.FilePointer == 0);

                // TODO: try writing the leading vLong in MSB order
                // (opposite of what Lucene does today), for better
                // outputs sharing in the FST
                scratchBytes.WriteVLong(EncodeOutput(Fp, HasTerms, IsFloor));
                if (IsFloor)
                {
                    scratchBytes.WriteVInt(floorBlocks.Count);
                    foreach (PendingBlock sub in floorBlocks)
                    {
                        Debug.Assert(sub.FloorLeadByte != -1);
                        //if (DEBUG) {
                        //  System.out.println("    write floorLeadByte=" + Integer.toHexString(sub.floorLeadByte&0xff));
                        //}
                        scratchBytes.WriteByte((byte)(sbyte)sub.FloorLeadByte);
                        Debug.Assert(sub.Fp > Fp);
                        scratchBytes.WriteVLong((sub.Fp - Fp) << 1 | (sub.HasTerms ? 1 : 0));
                    }
                }

                ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
                Builder<BytesRef> indexBuilder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, 0, 0, true, false, int.MaxValue, outputs, null, false, PackedInts.COMPACT, true, 15);
                var bytes = new byte[(int)scratchBytes.FilePointer];
                Debug.Assert(bytes.Length > 0);
                scratchBytes.WriteTo(bytes, 0);
                indexBuilder.Add(Util.ToIntsRef(Prefix, scratchIntsRef), new BytesRef(bytes, 0, bytes.Length));
                scratchBytes.Reset();

                // Copy over index for all sub-blocks

                if (SubIndices != null)
                {
                    foreach (FST<BytesRef> subIndex in SubIndices)
                    {
                        Append(indexBuilder, subIndex);
                    }
                }

                if (floorBlocks != null)
                {
                    foreach (PendingBlock sub in floorBlocks)
                    {
                        if (sub.SubIndices != null)
                        {
                            foreach (FST<BytesRef> subIndex in sub.SubIndices)
                            {
                                Append(indexBuilder, subIndex);
                            }
                        }
                        sub.SubIndices = null;
                    }
                }

                Index = indexBuilder.Finish();
                SubIndices = null;

                /*
                Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
                Util.toDot(index, w, false, false);
                System.out.println("SAVED to out.dot");
                w.Dispose();
                */
            }

            // TODO: maybe we could add bulk-add method to
            // Builder?  Takes FST and unions it w/ current
            // FST.
            private void Append(Builder<BytesRef> builder, FST<BytesRef> subIndex)
            {
                BytesRefFSTEnum<BytesRef> subIndexEnum = new BytesRefFSTEnum<BytesRef>(subIndex);
                BytesRefFSTEnum.InputOutput<BytesRef> indexEnt;
                while ((indexEnt = subIndexEnum.Next()) != null)
                {
                    //if (DEBUG) {
                    //  System.out.println("      add sub=" + indexEnt.input + " " + indexEnt.input + " output=" + indexEnt.output);
                    //}
                    builder.Add(Util.ToIntsRef(indexEnt.Input, scratchIntsRef), indexEnt.Output);
                }
            }
        }

        internal readonly RAMOutputStream scratchBytes = new RAMOutputStream();

        internal class TermsWriter : TermsConsumer
        {
            private readonly BlockTreeTermsWriter outerInstance;

            private readonly FieldInfo fieldInfo;
            private readonly int longsSize;
            private long numTerms;
            internal long sumTotalTermFreq;
            internal long sumDocFreq;
            internal int docCount;
            internal long indexStartFP;

            // Used only to partition terms into the block tree; we
            // don't pull an FST from this builder:
            private readonly NoOutputs noOutputs;

            private readonly Builder<object> blockBuilder;

            // PendingTerm or PendingBlock:
            private readonly IList<PendingEntry> pending = new List<PendingEntry>();

            // Index into pending of most recently written block
            private int lastBlockIndex = -1;

            // Re-used when segmenting a too-large block into floor
            // blocks:
            private int[] subBytes = new int[10];
            private int[] subTermCounts = new int[10];
            private int[] subTermCountSums = new int[10];
            private int[] subSubCounts = new int[10];

            // this class assigns terms to blocks "naturally", ie,
            // according to the number of terms under a given prefix
            // that we encounter:
            private class FindBlocks : Builder.FreezeTail<object>
            {
                private readonly BlockTreeTermsWriter.TermsWriter outerInstance;

                public FindBlocks(BlockTreeTermsWriter.TermsWriter outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override void Freeze(Builder.UnCompiledNode<object>[] frontier, int prefixLenPlus1, IntsRef lastInput)
                {
                    //if (DEBUG) System.out.println("  freeze prefixLenPlus1=" + prefixLenPlus1);

                    for (int idx = lastInput.Length; idx >= prefixLenPlus1; idx--)
                    {
                        Builder.UnCompiledNode<object> node = frontier[idx];

                        long totCount = 0;

                        if (node.IsFinal)
                        {
                            totCount++;
                        }

                        for (int arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
                        {
                            Builder.UnCompiledNode<object> target = (Builder.UnCompiledNode<object>)node.Arcs[arcIdx].Target;
                            totCount += target.InputCount;
                            target.Clear();
                            node.Arcs[arcIdx].Target = null;
                        }
                        node.NumArcs = 0;

                        if (totCount >= outerInstance.outerInstance.minItemsInBlock || idx == 0)
                        {
                            // We are on a prefix node that has enough
                            // entries (terms or sub-blocks) under it to let
                            // us write a new block or multiple blocks (main
                            // block + follow on floor blocks):
                            //if (DEBUG) {
                            //  if (totCount < minItemsInBlock && idx != 0) {
                            //    System.out.println("  force block has terms");
                            //  }
                            //}
                            outerInstance.WriteBlocks(lastInput, idx, (int)totCount);
                            node.InputCount = 1;
                        }
                        else
                        {
                            // stragglers!  carry count upwards
                            node.InputCount = totCount;
                        }
                        frontier[idx] = new Builder.UnCompiledNode<object>(outerInstance.blockBuilder, idx);
                    }
                }
            }

            // Write the top count entries on the pending stack as
            // one or more blocks.  Returns how many blocks were
            // written.  If the entry count is <= maxItemsPerBlock
            // we just write a single block; else we break into
            // primary (initial) block and then one or more
            // following floor blocks:

            internal virtual void WriteBlocks(IntsRef prevTerm, int prefixLength, int count)
            {
                if (prefixLength == 0 || count <= outerInstance.maxItemsInBlock)
                {
                    // Easy case: not floor block.  Eg, prefix is "foo",
                    // and we found 30 terms/sub-blocks starting w/ that
                    // prefix, and minItemsInBlock <= 30 <=
                    // maxItemsInBlock.
                    PendingBlock nonFloorBlock = WriteBlock(prevTerm, prefixLength, prefixLength, count, count, 0, false, -1, true);
                    nonFloorBlock.CompileIndex(null, outerInstance.scratchBytes);
                    pending.Add(nonFloorBlock);
                }
                else
                {
                    // Floor block case.  Eg, prefix is "foo" but we
                    // have 100 terms/sub-blocks starting w/ that
                    // prefix.  We segment the entries into a primary
                    // block and following floor blocks using the first
                    // label in the suffix to assign to floor blocks.

                    // TODO: we could store min & max suffix start byte
                    // in each block, to make floor blocks authoritative

                    //if (DEBUG) {
                    //  final BytesRef prefix = new BytesRef(prefixLength);
                    //  for(int m=0;m<prefixLength;m++) {
                    //    prefix.bytes[m] = (byte) prevTerm.ints[m];
                    //  }
                    //  prefix.length = prefixLength;
                    //  //System.out.println("\nWBS count=" + count + " prefix=" + prefix.utf8ToString() + " " + prefix);
                    //  System.out.println("writeBlocks: prefix=" + prefix + " " + prefix + " count=" + count + " pending.size()=" + pending.size());
                    //}
                    //System.out.println("\nwbs count=" + count);

                    int savLabel = prevTerm.Ints[prevTerm.Offset + prefixLength];

                    // Count up how many items fall under
                    // each unique label after the prefix.

                    // TODO: this is wasteful since the builder had
                    // already done this (partitioned these sub-terms
                    // according to their leading prefix byte)

                    IList<PendingEntry> slice = ListExtensions.SubList<PendingEntry>(pending, pending.Count - count, pending.Count);
                    int lastSuffixLeadLabel = -1;
                    int termCount = 0;
                    int subCount = 0;
                    int numSubs = 0;

                    foreach (PendingEntry ent in slice)
                    {
                        // First byte in the suffix of this term
                        int suffixLeadLabel;
                        if (ent.IsTerm)
                        {
                            PendingTerm term = (PendingTerm)ent;
                            if (term.Term.Length == prefixLength)
                            {
                                // Suffix is 0, ie prefix 'foo' and term is
                                // 'foo' so the term has empty string suffix
                                // in this block
                                Debug.Assert(lastSuffixLeadLabel == -1);
                                Debug.Assert(numSubs == 0);
                                suffixLeadLabel = -1;
                            }
                            else
                            {
                                suffixLeadLabel = term.Term.Bytes[term.Term.Offset + prefixLength] & 0xff;
                            }
                        }
                        else
                        {
                            PendingBlock block = (PendingBlock)ent;
                            Debug.Assert(block.Prefix.Length > prefixLength);
                            suffixLeadLabel = block.Prefix.Bytes[block.Prefix.Offset + prefixLength] & 0xff;
                        }

                        if (suffixLeadLabel != lastSuffixLeadLabel && (termCount + subCount) != 0)
                        {
                            if (subBytes.Length == numSubs)
                            {
                                subBytes = ArrayUtil.Grow(subBytes);
                                subTermCounts = ArrayUtil.Grow(subTermCounts);
                                subSubCounts = ArrayUtil.Grow(subSubCounts);
                            }
                            subBytes[numSubs] = lastSuffixLeadLabel;
                            lastSuffixLeadLabel = suffixLeadLabel;
                            subTermCounts[numSubs] = termCount;
                            subSubCounts[numSubs] = subCount;
                            /*
                            if (suffixLeadLabel == -1) {
                              System.out.println("  sub " + -1 + " termCount=" + termCount + " subCount=" + subCount);
                            } else {
                              System.out.println("  sub " + Integer.toHexString(suffixLeadLabel) + " termCount=" + termCount + " subCount=" + subCount);
                            }
                            */
                            termCount = subCount = 0;
                            numSubs++;
                        }

                        if (ent.IsTerm)
                        {
                            termCount++;
                        }
                        else
                        {
                            subCount++;
                        }
                    }

                    if (subBytes.Length == numSubs)
                    {
                        subBytes = ArrayUtil.Grow(subBytes);
                        subTermCounts = ArrayUtil.Grow(subTermCounts);
                        subSubCounts = ArrayUtil.Grow(subSubCounts);
                    }

                    subBytes[numSubs] = lastSuffixLeadLabel;
                    subTermCounts[numSubs] = termCount;
                    subSubCounts[numSubs] = subCount;
                    numSubs++;
                    /*
                    if (lastSuffixLeadLabel == -1) {
                      System.out.println("  sub " + -1 + " termCount=" + termCount + " subCount=" + subCount);
                    } else {
                      System.out.println("  sub " + Integer.toHexString(lastSuffixLeadLabel) + " termCount=" + termCount + " subCount=" + subCount);
                    }
                    */

                    if (subTermCountSums.Length < numSubs)
                    {
                        subTermCountSums = ArrayUtil.Grow(subTermCountSums, numSubs);
                    }

                    // Roll up (backwards) the termCounts; postings impl
                    // needs this to know where to pull the term slice
                    // from its pending terms stack:
                    int sum = 0;
                    for (int idx = numSubs - 1; idx >= 0; idx--)
                    {
                        sum += subTermCounts[idx];
                        subTermCountSums[idx] = sum;
                    }

                    // TODO: make a better segmenter?  It'd have to
                    // absorb the too-small end blocks backwards into
                    // the previous blocks

                    // Naive greedy segmentation; this is not always
                    // best (it can produce a too-small block as the
                    // last block):
                    int pendingCount = 0;
                    int startLabel = subBytes[0];
                    int curStart = count;
                    subCount = 0;

                    IList<PendingBlock> floorBlocks = new List<PendingBlock>();
                    PendingBlock firstBlock = null;

                    for (int sub = 0; sub < numSubs; sub++)
                    {
                        pendingCount += subTermCounts[sub] + subSubCounts[sub];
                        //System.out.println("  " + (subTermCounts[sub] + subSubCounts[sub]));
                        subCount++;

                        // Greedily make a floor block as soon as we've
                        // crossed the min count
                        if (pendingCount >= outerInstance.minItemsInBlock)
                        {
                            int curPrefixLength;
                            if (startLabel == -1)
                            {
                                curPrefixLength = prefixLength;
                            }
                            else
                            {
                                curPrefixLength = 1 + prefixLength;
                                // floor term:
                                prevTerm.Ints[prevTerm.Offset + prefixLength] = startLabel;
                            }
                            //System.out.println("  " + subCount + " subs");
                            PendingBlock floorBlock = WriteBlock(prevTerm, prefixLength, curPrefixLength, curStart, pendingCount, subTermCountSums[1 + sub], true, startLabel, curStart == pendingCount);
                            if (firstBlock == null)
                            {
                                firstBlock = floorBlock;
                            }
                            else
                            {
                                floorBlocks.Add(floorBlock);
                            }
                            curStart -= pendingCount;
                            //System.out.println("    = " + pendingCount);
                            pendingCount = 0;

                            Debug.Assert(outerInstance.minItemsInBlock == 1 || subCount > 1, "minItemsInBlock=" + outerInstance.minItemsInBlock + " subCount=" + subCount + " sub=" + sub + " of " + numSubs + " subTermCount=" + subTermCountSums[sub] + " subSubCount=" + subSubCounts[sub] + " depth=" + prefixLength);
                            subCount = 0;
                            startLabel = subBytes[sub + 1];

                            if (curStart == 0)
                            {
                                break;
                            }

                            if (curStart <= outerInstance.maxItemsInBlock)
                            {
                                // remainder is small enough to fit into a
                                // block.  NOTE that this may be too small (<
                                // minItemsInBlock); need a true segmenter
                                // here
                                Debug.Assert(startLabel != -1);
                                Debug.Assert(firstBlock != null);
                                prevTerm.Ints[prevTerm.Offset + prefixLength] = startLabel;
                                //System.out.println("  final " + (numSubs-sub-1) + " subs");
                                /*
                                for(sub++;sub < numSubs;sub++) {
                                  System.out.println("  " + (subTermCounts[sub] + subSubCounts[sub]));
                                }
                                System.out.println("    = " + curStart);
                                if (curStart < minItemsInBlock) {
                                  System.out.println("      **");
                                }
                                */
                                floorBlocks.Add(WriteBlock(prevTerm, prefixLength, prefixLength + 1, curStart, curStart, 0, true, startLabel, true));
                                break;
                            }
                        }
                    }

                    prevTerm.Ints[prevTerm.Offset + prefixLength] = savLabel;

                    Debug.Assert(firstBlock != null);
                    firstBlock.CompileIndex(floorBlocks, outerInstance.scratchBytes);

                    pending.Add(firstBlock);
                    //if (DEBUG) System.out.println("  done pending.size()=" + pending.size());
                }
                lastBlockIndex = pending.Count - 1;
            }

            // for debugging
            private string ToString(BytesRef b)
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

            // Writes all entries in the pending slice as a single
            // block:
            private PendingBlock WriteBlock(IntsRef prevTerm, int prefixLength, int indexPrefixLength, int startBackwards, int length, int futureTermCount, bool isFloor, int floorLeadByte, bool isLastInFloor)
            {
                Debug.Assert(length > 0);

                int start = pending.Count - startBackwards;

                Debug.Assert(start >= 0, "pending.size()=" + pending.Count + " startBackwards=" + startBackwards + " length=" + length);

                IList<PendingEntry> slice = pending.SubList(start, start + length);

                long startFP = outerInstance.@out.FilePointer;

                BytesRef prefix = new BytesRef(indexPrefixLength);
                for (int m = 0; m < indexPrefixLength; m++)
                {
                    prefix.Bytes[m] = (byte)prevTerm.Ints[m];
                }
                prefix.Length = indexPrefixLength;

                // Write block header:
                outerInstance.@out.WriteVInt((length << 1) | (isLastInFloor ? 1 : 0));

                // 1st pass: pack term suffix bytes into byte[] blob
                // TODO: cutover to bulk int codec... simple64?
                bool isLeafBlock;
                if (lastBlockIndex < start)
                {
                    // this block definitely does not contain sub-blocks:
                    isLeafBlock = true;
                    //System.out.println("no scan true isFloor=" + isFloor);
                }
                else if (!isFloor)
                {
                    // this block definitely does contain at least one sub-block:
                    isLeafBlock = false;
                    //System.out.println("no scan false " + lastBlockIndex + " vs start=" + start + " len=" + length);
                }
                else
                {
                    // Must scan up-front to see if there is a sub-block
                    bool v = true;
                    //System.out.println("scan " + lastBlockIndex + " vs start=" + start + " len=" + length);
                    foreach (PendingEntry ent in slice)
                    {
                        if (!ent.IsTerm)
                        {
                            v = false;
                            break;
                        }
                    }
                    isLeafBlock = v;
                }

                IList<FST<BytesRef>> subIndices;

                int termCount;

                long[] longs = new long[longsSize];
                bool absolute = true;

                if (isLeafBlock)
                {
                    subIndices = null;
                    foreach (PendingEntry ent in slice)
                    {
                        Debug.Assert(ent.IsTerm);
                        PendingTerm term = (PendingTerm)ent;
                        BlockTermState state = term.State;
                        int suffix = term.Term.Length - prefixLength;
                        // if (DEBUG) {
                        //   BytesRef suffixBytes = new BytesRef(suffix);
                        //   System.arraycopy(term.term.bytes, prefixLength, suffixBytes.bytes, 0, suffix);
                        //   suffixBytes.length = suffix;
                        //   System.out.println("    write term suffix=" + suffixBytes);
                        // }
                        // For leaf block we write suffix straight
                        suffixWriter.WriteVInt(suffix);
                        suffixWriter.WriteBytes(term.Term.Bytes, prefixLength, suffix);

                        // Write term stats, to separate byte[] blob:
                        statsWriter.WriteVInt(state.DocFreq);
                        if (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                        {
                            Debug.Assert(state.TotalTermFreq >= state.DocFreq, state.TotalTermFreq + " vs " + state.DocFreq);
                            statsWriter.WriteVLong(state.TotalTermFreq - state.DocFreq);
                        }

                        // Write term meta data
                        outerInstance.postingsWriter.EncodeTerm(longs, bytesWriter, fieldInfo, state, absolute);
                        for (int pos = 0; pos < longsSize; pos++)
                        {
                            Debug.Assert(longs[pos] >= 0);
                            metaWriter.WriteVLong(longs[pos]);
                        }
                        bytesWriter.WriteTo(metaWriter);
                        bytesWriter.Reset();
                        absolute = false;
                    }
                    termCount = length;
                }
                else
                {
                    subIndices = new List<FST<BytesRef>>();
                    termCount = 0;
                    foreach (PendingEntry ent in slice)
                    {
                        if (ent.IsTerm)
                        {
                            PendingTerm term = (PendingTerm)ent;
                            BlockTermState state = term.State;
                            int suffix = term.Term.Length - prefixLength;
                            // if (DEBUG) {
                            //   BytesRef suffixBytes = new BytesRef(suffix);
                            //   System.arraycopy(term.term.bytes, prefixLength, suffixBytes.bytes, 0, suffix);
                            //   suffixBytes.length = suffix;
                            //   System.out.println("    write term suffix=" + suffixBytes);
                            // }
                            // For non-leaf block we borrow 1 bit to record
                            // if entry is term or sub-block
                            suffixWriter.WriteVInt(suffix << 1);
                            suffixWriter.WriteBytes(term.Term.Bytes, prefixLength, suffix);

                            // Write term stats, to separate byte[] blob:
                            statsWriter.WriteVInt(state.DocFreq);
                            if (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                            {
                                Debug.Assert(state.TotalTermFreq >= state.DocFreq);
                                statsWriter.WriteVLong(state.TotalTermFreq - state.DocFreq);
                            }

                            // TODO: now that terms dict "sees" these longs,
                            // we can explore better column-stride encodings
                            // to encode all long[0]s for this block at
                            // once, all long[1]s, etc., e.g. using
                            // Simple64.  Alternatively, we could interleave
                            // stats + meta ... no reason to have them
                            // separate anymore:

                            // Write term meta data
                            outerInstance.postingsWriter.EncodeTerm(longs, bytesWriter, fieldInfo, state, absolute);
                            for (int pos = 0; pos < longsSize; pos++)
                            {
                                Debug.Assert(longs[pos] >= 0);
                                metaWriter.WriteVLong(longs[pos]);
                            }
                            bytesWriter.WriteTo(metaWriter);
                            bytesWriter.Reset();
                            absolute = false;

                            termCount++;
                        }
                        else
                        {
                            PendingBlock block = (PendingBlock)ent;
                            int suffix = block.Prefix.Length - prefixLength;

                            Debug.Assert(suffix > 0);

                            // For non-leaf block we borrow 1 bit to record
                            // if entry is term or sub-block
                            suffixWriter.WriteVInt((suffix << 1) | 1);
                            suffixWriter.WriteBytes(block.Prefix.Bytes, prefixLength, suffix);
                            Debug.Assert(block.Fp < startFP);

                            // if (DEBUG) {
                            //   BytesRef suffixBytes = new BytesRef(suffix);
                            //   System.arraycopy(block.prefix.bytes, prefixLength, suffixBytes.bytes, 0, suffix);
                            //   suffixBytes.length = suffix;
                            //   System.out.println("    write sub-block suffix=" + toString(suffixBytes) + " subFP=" + block.fp + " subCode=" + (startFP-block.fp) + " floor=" + block.isFloor);
                            // }

                            suffixWriter.WriteVLong(startFP - block.Fp);
                            subIndices.Add(block.Index);
                        }
                    }

                    Debug.Assert(subIndices.Count != 0);
                }

                // TODO: we could block-write the term suffix pointers;
                // this would take more space but would enable binary
                // search on lookup

                // Write suffixes byte[] blob to terms dict output:
                outerInstance.@out.WriteVInt((int)(suffixWriter.FilePointer << 1) | (isLeafBlock ? 1 : 0));
                suffixWriter.WriteTo(outerInstance.@out);
                suffixWriter.Reset();

                // Write term stats byte[] blob
                outerInstance.@out.WriteVInt((int)statsWriter.FilePointer);
                statsWriter.WriteTo(outerInstance.@out);
                statsWriter.Reset();

                // Write term meta data byte[] blob
                outerInstance.@out.WriteVInt((int)metaWriter.FilePointer);
                metaWriter.WriteTo(outerInstance.@out);
                metaWriter.Reset();

                // Remove slice replaced by block:
                slice.Clear();

                if (lastBlockIndex >= start)
                {
                    if (lastBlockIndex < start + length)
                    {
                        lastBlockIndex = start;
                    }
                    else
                    {
                        lastBlockIndex -= length;
                    }
                }

                // if (DEBUG) {
                //   System.out.println("      fpEnd=" + out.getFilePointer());
                // }

                return new PendingBlock(prefix, startFP, termCount != 0, isFloor, floorLeadByte, subIndices);
            }

            internal TermsWriter(BlockTreeTermsWriter outerInstance, FieldInfo fieldInfo)
            {
                this.outerInstance = outerInstance;
                this.fieldInfo = fieldInfo;

                noOutputs = NoOutputs.Singleton;

                // this Builder is just used transiently to fragment
                // terms into "good" blocks; we don't save the
                // resulting FST:
                blockBuilder = new Builder<object>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, noOutputs, new FindBlocks(this), false, PackedInts.COMPACT, true, 15);

                this.longsSize = outerInstance.postingsWriter.SetField(fieldInfo);
            }

            public override IComparer<BytesRef> Comparer
            {
                get
                {
                    return BytesRef.UTF8SortedAsUnicodeComparer;
                }
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                //if (DEBUG) System.out.println("\nBTTW.startTerm term=" + fieldInfo.name + ":" + toString(text) + " seg=" + segment);
                outerInstance.postingsWriter.StartTerm();
                /*
                if (fieldInfo.name.equals("id")) {
                  postingsWriter.termID = Integer.parseInt(text.utf8ToString());
                } else {
                  postingsWriter.termID = -1;
                }
                */
                return outerInstance.postingsWriter;
            }

            private readonly IntsRef scratchIntsRef = new IntsRef();

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                Debug.Assert(stats.DocFreq > 0);
                //if (DEBUG) System.out.println("BTTW.finishTerm term=" + fieldInfo.name + ":" + toString(text) + " seg=" + segment + " df=" + stats.docFreq);

                blockBuilder.Add(Util.ToIntsRef(text, scratchIntsRef), noOutputs.NoOutput);
                BlockTermState state = outerInstance.postingsWriter.NewTermState();
                state.DocFreq = stats.DocFreq;
                state.TotalTermFreq = stats.TotalTermFreq;
                outerInstance.postingsWriter.FinishTerm(state);

                PendingTerm term = new PendingTerm(BytesRef.DeepCopyOf(text), state);
                pending.Add(term);
                numTerms++;
            }

            // Finishes all terms in this field
            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                if (numTerms > 0)
                {
                    blockBuilder.Finish();

                    // We better have one final "root" block:
                    Debug.Assert(pending.Count == 1 && !pending[0].IsTerm, "pending.size()=" + pending.Count + " pending=" + pending);
                    PendingBlock root = (PendingBlock)pending[0];
                    Debug.Assert(root.Prefix.Length == 0);
                    Debug.Assert(root.Index.EmptyOutput != null);

                    this.sumTotalTermFreq = sumTotalTermFreq;
                    this.sumDocFreq = sumDocFreq;
                    this.docCount = docCount;

                    // Write FST to index
                    indexStartFP = outerInstance.indexOut.FilePointer;
                    root.Index.Save(outerInstance.indexOut);
                    //System.out.println("  write FST " + indexStartFP + " field=" + fieldInfo.name);

                    // if (SAVE_DOT_FILES || DEBUG) {
                    //   final String dotFileName = segment + "_" + fieldInfo.name + ".dot";
                    //   Writer w = new OutputStreamWriter(new FileOutputStream(dotFileName));
                    //   Util.toDot(root.index, w, false, false);
                    //   System.out.println("SAVED to " + dotFileName);
                    //   w.Dispose();
                    // }

                    outerInstance.fields.Add(new FieldMetaData(fieldInfo, ((PendingBlock)pending[0]).Index.EmptyOutput, numTerms, indexStartFP, sumTotalTermFreq, sumDocFreq, docCount, longsSize));
                }
                else
                {
                    Debug.Assert(sumTotalTermFreq == 0 || fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY && sumTotalTermFreq == -1);
                    Debug.Assert(sumDocFreq == 0);
                    Debug.Assert(docCount == 0);
                }
            }

            internal readonly RAMOutputStream suffixWriter = new RAMOutputStream();
            internal readonly RAMOutputStream statsWriter = new RAMOutputStream();
            internal readonly RAMOutputStream metaWriter = new RAMOutputStream();
            internal readonly RAMOutputStream bytesWriter = new RAMOutputStream();
        }

        public override void Dispose()
        {
            System.IO.IOException ioe = null;
            try
            {
                long dirStart = @out.FilePointer;
                long indexDirStart = indexOut.FilePointer;

                @out.WriteVInt(fields.Count);

                foreach (FieldMetaData field in fields)
                {
                    //System.out.println("  field " + field.fieldInfo.name + " " + field.numTerms + " terms");
                    @out.WriteVInt(field.FieldInfo.Number);
                    @out.WriteVLong(field.NumTerms);
                    @out.WriteVInt(field.RootCode.Length);
                    @out.WriteBytes(field.RootCode.Bytes, field.RootCode.Offset, field.RootCode.Length);
                    if (field.FieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                    {
                        @out.WriteVLong(field.SumTotalTermFreq);
                    }
                    @out.WriteVLong(field.SumDocFreq);
                    @out.WriteVInt(field.DocCount);
                    @out.WriteVInt(field.LongsSize);
                    indexOut.WriteVLong(field.IndexStartFP);
                }
                WriteTrailer(@out, dirStart);
                CodecUtil.WriteFooter(@out);
                WriteIndexTrailer(indexOut, indexDirStart);
                CodecUtil.WriteFooter(indexOut);
            }
            catch (System.IO.IOException ioe2)
            {
                ioe = ioe2;
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(ioe, @out, indexOut, postingsWriter);
            }
        }
    }
}