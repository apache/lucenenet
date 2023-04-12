using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    using Int32sRef = Lucene.Net.Util.Int32sRef;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using NoOutputs = Lucene.Net.Util.Fst.NoOutputs;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using Util = Lucene.Net.Util.Fst.Util;

    // LUCENENET specific - moved out constants from generic class
    public static class BlockTreeTermsWriter
    {
        /// <summary>
        /// Suggested default value for the 
        /// <c>minItemsInBlock</c> parameter to 
        /// <see cref="BlockTreeTermsWriter{TSubclassState}(SegmentWriteState, PostingsWriterBase, int, int, TSubclassState)"/>.
        /// </summary>
        public const int DEFAULT_MIN_BLOCK_SIZE = 25;

        /// <summary>
        /// Suggested default value for the 
        /// <c>maxItemsInBlock</c> parameter to 
        /// <see cref="BlockTreeTermsWriter{TSubclassState}(SegmentWriteState, PostingsWriterBase, int, int, TSubclassState)"/>.
        /// </summary>
        public const int DEFAULT_MAX_BLOCK_SIZE = 48;

        //public final static boolean DEBUG = false;
        //private final static boolean SAVE_DOT_FILES = false;

        internal const int OUTPUT_FLAGS_NUM_BITS = 2;
        internal const int OUTPUT_FLAGS_MASK = 0x3;
        internal const int OUTPUT_FLAG_IS_FLOOR = 0x1;
        internal const int OUTPUT_FLAG_HAS_TERMS = 0x2;

        /// <summary>
        /// Extension of terms file. </summary>
        internal const string TERMS_EXTENSION = "tim";

        internal const string TERMS_CODEC_NAME = "BLOCK_TREE_TERMS_DICT";

        /// <summary>
        /// Initial terms format. </summary>
        public const int VERSION_START = 0;

        /// <summary>
        /// Append-only </summary>
        public const int VERSION_APPEND_ONLY = 1;

        /// <summary>
        /// Meta data as array. </summary>
        public const int VERSION_META_ARRAY = 2;

        /// <summary>
        /// Checksums. </summary>
        public const int VERSION_CHECKSUM = 3;

        /// <summary>
        /// Current terms format. </summary>
        public const int VERSION_CURRENT = VERSION_CHECKSUM;

        /// <summary>
        /// Extension of terms index file. </summary>
        internal const string TERMS_INDEX_EXTENSION = "tip";

        internal const string TERMS_INDEX_CODEC_NAME = "BLOCK_TREE_TERMS_INDEX";
    }

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
    /// <para/>
    /// Writes terms dict and index, block-encoding (column
    /// stride) each term's metadata for each set of terms
    /// between two index terms.
    /// <para/>
    /// Files:
    /// <list type="bullet">
    ///     <item><term>.tim:</term> <description><a href="#Termdictionary">Term Dictionary</a></description></item>
    ///     <item><term>.tip:</term> <description><a href="#Termindex">Term Index</a></description></item>
    /// </list>
    /// <para/>
    /// <a name="Termdictionary" id="Termdictionary"></a>
    /// <h3>Term Dictionary</h3>
    ///
    /// <para>The .tim file contains the list of terms in each
    /// field along with per-term statistics (such as docfreq)
    /// and per-term metadata (typically pointers to the postings list
    /// for that term in the inverted index).
    /// </para>
    ///
    /// <para>The .tim is arranged in blocks: with blocks containing
    /// a variable number of entries (by default 25-48), where
    /// each entry is either a term or a reference to a
    /// sub-block.</para>
    ///
    /// <para>NOTE: The term dictionary can plug into different postings implementations:
    /// the postings writer/reader are actually responsible for encoding
    /// and decoding the Postings Metadata and Term Metadata sections.</para>
    ///
    /// <list type="bullet">
    ///    <item><description>TermsDict (.tim) --&gt; Header, <i>PostingsHeader</i>, NodeBlock<sup>NumBlocks</sup>,
    ///                               FieldSummary, DirOffset, Footer</description></item>
    ///    <item><description>NodeBlock --&gt; (OuterNode | InnerNode)</description></item>
    ///    <item><description>OuterNode --&gt; EntryCount, SuffixLength, Byte<sup>SuffixLength</sup>, StatsLength, &lt; TermStats &gt;<sup>EntryCount</sup>, MetaLength, &lt;<i>TermMetadata</i>&gt;<sup>EntryCount</sup></description></item>
    ///    <item><description>InnerNode --&gt; EntryCount, SuffixLength[,Sub?], Byte<sup>SuffixLength</sup>, StatsLength, &lt; TermStats ? &gt;<sup>EntryCount</sup>, MetaLength, &lt;<i>TermMetadata ? </i>&gt;<sup>EntryCount</sup></description></item>
    ///    <item><description>TermStats --&gt; DocFreq, TotalTermFreq </description></item>
    ///    <item><description>FieldSummary --&gt; NumFields, &lt;FieldNumber, NumTerms, RootCodeLength, Byte<sup>RootCodeLength</sup>,
    ///                            SumTotalTermFreq?, SumDocFreq, DocCount&gt;<sup>NumFields</sup></description></item>
    ///    <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/></description></item>
    ///    <item><description>DirOffset --&gt; Uint64 (<see cref="Store.DataOutput.WriteInt64(long)"/>)</description></item>
    ///    <item><description>EntryCount,SuffixLength,StatsLength,DocFreq,MetaLength,NumFields,
    ///        FieldNumber,RootCodeLength,DocCount --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>_</description></item>
    ///    <item><description>TotalTermFreq,NumTerms,SumTotalTermFreq,SumDocFreq --&gt;
    ///        VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>)</description></item>
    ///    <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(IndexOutput)"/>)</description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///    <item><description>Header is a CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) storing the version information
    ///        for the BlockTree implementation.</description></item>
    ///    <item><description>DirOffset is a pointer to the FieldSummary section.</description></item>
    ///    <item><description>DocFreq is the count of documents which contain the term.</description></item>
    ///    <item><description>TotalTermFreq is the total number of occurrences of the term. this is encoded
    ///        as the difference between the total number of occurrences and the DocFreq.</description></item>
    ///    <item><description>FieldNumber is the fields number from <see cref="fieldInfos"/>. (.fnm)</description></item>
    ///    <item><description>NumTerms is the number of unique terms for the field.</description></item>
    ///    <item><description>RootCode points to the root block for the field.</description></item>
    ///    <item><description>SumDocFreq is the total number of postings, the number of term-document pairs across
    ///        the entire field.</description></item>
    ///    <item><description>DocCount is the number of documents that have at least one posting for this field.</description></item>
    ///    <item><description>PostingsHeader and TermMetadata are plugged into by the specific postings implementation:
    ///        these contain arbitrary per-file data (such as parameters or versioning information)
    ///        and per-term data (such as pointers to inverted files).</description></item>
    ///    <item><description>For inner nodes of the tree, every entry will steal one bit to mark whether it points
    ///        to child nodes(sub-block). If so, the corresponding <see cref="TermStats"/> and TermMetadata are omitted </description></item>
    /// </list>
    /// <a name="Termindex" id="Termindex"></a>
    /// <h3>Term Index</h3>
    /// <para>The .tip file contains an index into the term dictionary, so that it can be
    /// accessed randomly.  The index is also used to determine
    /// when a given term cannot exist on disk (in the .tim file), saving a disk seek.</para>
    /// <list type="bullet">
    ///   <item><description>TermsIndex (.tip) --&gt; Header, FSTIndex<sup>NumFields</sup>
    ///                                &lt;IndexStartFP&gt;<sup>NumFields</sup>, DirOffset, Footer</description></item>
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>)</description></item>
    ///   <item><description>DirOffset --&gt; Uint64 (<see cref="Store.DataOutput.WriteInt64(long)"/></description>)</item>
    ///   <item><description>IndexStartFP --&gt; VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/></description>)</item>
    ///   <!-- TODO: better describe FST output here -->
    ///   <item><description>FSTIndex --&gt; <see cref="T:FST{byte[]}"/></description></item>
    ///   <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(IndexOutput)"/></description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///   <item><description>The .tip file contains a separate FST for each
    ///       field.  The FST maps a term prefix to the on-disk
    ///       block that holds all terms starting with that
    ///       prefix.  Each field's IndexStartFP points to its
    ///       FST.</description></item>
    ///   <item><description>DirOffset is a pointer to the start of the IndexStartFPs
    ///       for all fields</description></item>
    ///   <item><description>It's possible that an on-disk block would contain
    ///       too many terms (more than the allowed maximum
    ///       (default: 48)).  When this happens, the block is
    ///       sub-divided into new blocks (called "floor
    ///       blocks"), and then the output in the FST for the
    ///       block's prefix encodes the leading byte of each
    ///       sub-block, and its file pointer.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="BlockTreeTermsReader{TSubclassState}"/>
    public class BlockTreeTermsWriter<TSubclassState> : FieldsConsumer
    {
        // LUCENENET specific - moved constants from this generic class to static BlockTreeTermsWriter

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IndexOutput @out;
        private readonly IndexOutput indexOut;
#pragma warning restore CA2213 // Disposable fields should be disposed
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

            /// <summary>
            /// NOTE: This was longsSize (field) in Lucene
            /// </summary>
            internal int Int64sSize { get; private set; }

            public FieldMetaData(FieldInfo fieldInfo, BytesRef rootCode, long numTerms, long indexStartFP, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(numTerms > 0);
                this.FieldInfo = fieldInfo;
                if (Debugging.AssertsEnabled) Debugging.Assert(rootCode != null, "field={0} numTerms={1}", fieldInfo.Name, numTerms);
                this.RootCode = rootCode;
                this.IndexStartFP = indexStartFP;
                this.NumTerms = numTerms;
                this.SumTotalTermFreq = sumTotalTermFreq;
                this.SumDocFreq = sumDocFreq;
                this.DocCount = docCount;
                this.Int64sSize = longsSize;
            }
        }

        private readonly IList<FieldMetaData> fields = new JCG.List<FieldMetaData>();
        // private final String segment;

        protected object m_subclassState = null;

        /// <summary>
        /// Create a new writer.  The number of items (terms or
        /// sub-blocks) per block will aim to be between
        /// </summary>
        /// <param name="subclassState">LUCENENET specific parameter which allows a subclass
        /// to set state. It is *optional* and can be used when overriding the WriteHeader(),
        /// WriteIndexHeader(). It only matters in the case where the state
        /// is required inside of any of those methods that is passed in to the subclass constructor.
        /// 
        /// When passed to the constructor, it is set to the protected field m_subclassState before
        /// any of the above methods are called where it is available for reading when overriding the above methods.
        /// 
        /// If your subclass needs to pass more than one piece of data, you can create a class or struct to do so.
        /// All other virtual members of BlockTreeTermsWriter are not called in the constructor, 
        /// so the overrides of those methods won't specifically need to use this field (although they could for consistency).
        /// </param>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]
        public BlockTreeTermsWriter(SegmentWriteState state, PostingsWriterBase postingsWriter, int minItemsInBlock, int maxItemsInBlock, TSubclassState subclassState)
        {
            // LUCENENET specific - added state parameter that subclasses
            // can use to keep track of state and use it in their own virtual
            // methods that are called by this constructor
            this.m_subclassState = subclassState;

            if (minItemsInBlock <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minItemsInBlock), "minItemsInBlock must be >= 2; got " + minItemsInBlock); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (maxItemsInBlock <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItemsInBlock), "maxItemsInBlock must be >= 1; got " + maxItemsInBlock); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (minItemsInBlock > maxItemsInBlock)
            {
                throw new ArgumentException("maxItemsInBlock must be >= minItemsInBlock; got maxItemsInBlock=" + maxItemsInBlock + " minItemsInBlock=" + minItemsInBlock);
            }
            if (2 * (minItemsInBlock - 1) > maxItemsInBlock)
            {
                throw new ArgumentException("maxItemsInBlock must be at least 2*(minItemsInBlock-1); got maxItemsInBlock=" + maxItemsInBlock + " minItemsInBlock=" + minItemsInBlock);
            }

            string termsFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, BlockTreeTermsWriter.TERMS_EXTENSION);
            @out = state.Directory.CreateOutput(termsFileName, state.Context);
            bool success = false;
            IndexOutput indexOut = null;
            try
            {
                fieldInfos = state.FieldInfos;
                this.minItemsInBlock = minItemsInBlock;
                this.maxItemsInBlock = maxItemsInBlock;
                WriteHeader(@out);

                //DEBUG = state.segmentName.Equals("_4a", StringComparison.Ordinal);

                string termsIndexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, BlockTreeTermsWriter.TERMS_INDEX_EXTENSION);
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
                    IOUtils.DisposeWhileHandlingException(@out, indexOut);
                }
            }
            this.indexOut = indexOut;
        }

        /// <summary>
        /// Writes the terms file header. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal virtual void WriteHeader(IndexOutput @out)
        {
            CodecUtil.WriteHeader(@out, BlockTreeTermsWriter.TERMS_CODEC_NAME, BlockTreeTermsWriter.VERSION_CURRENT);
        }

        /// <summary>
        /// Writes the index file header. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal virtual void WriteIndexHeader(IndexOutput @out)
        {
            CodecUtil.WriteHeader(@out, BlockTreeTermsWriter.TERMS_INDEX_CODEC_NAME, BlockTreeTermsWriter.VERSION_CURRENT);
        }

        /// <summary>
        /// Writes the terms file trailer. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal virtual void WriteTrailer(IndexOutput @out, long dirStart)
        {
            @out.WriteInt64(dirStart);
        }

        /// <summary>
        /// Writes the index file trailer. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal virtual void WriteIndexTrailer(IndexOutput indexOut, long dirStart)
        {
            indexOut.WriteInt64(dirStart);
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            //DEBUG = field.name.Equals("id", StringComparison.Ordinal);
            //if (DEBUG) System.out.println("\nBTTW.addField seg=" + segment + " field=" + field.name);
            if (Debugging.AssertsEnabled) Debugging.Assert(currentField is null || currentField.Name.CompareToOrdinal(field.Name) < 0);
            currentField = field;
            return new TermsWriter(this, field);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long EncodeOutput(long fp, bool hasTerms, bool isFloor)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(fp < (1L << 62));
            return (fp << 2) | (uint)(hasTerms ? BlockTreeTermsWriter.OUTPUT_FLAG_HAS_TERMS : 0) | (uint)(isFloor ? BlockTreeTermsWriter.OUTPUT_FLAG_IS_FLOOR : 0);
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
            private readonly Int32sRef scratchIntsRef = new Int32sRef();

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

            // LUCENENET specific - to keep the Debug.Assert statement from throwing exceptions
            // because of invalid UTF8 code in Prefix, we have a wrapper class that falls back
            // to using PendingBlock.Prefix.ToString() if PendingBlock.ToString() errors.
            // This struct defers formatting the string until it is actually used as a parameter
            // in string.Format().
            private struct PendingBlocksFormatter // For assert
            {
#pragma warning disable IDE0044 // Add readonly modifier
                private IList<PendingBlock> blocks;
#pragma warning restore IDE0044 // Add readonly modifier
                public PendingBlocksFormatter(IList<PendingBlock> blocks)
                {
                    this.blocks = blocks; // May be null
                }

                public override string ToString() // For assert
                {
                    if (blocks is null)
                        return "null";

                    if (blocks.Count == 0)
                        return "[]";

                    using var it = blocks.GetEnumerator();
                    StringBuilder sb = new StringBuilder();
                    sb.Append('[');
                    it.MoveNext();
                    while (true)
                    {
                        var e = it.Current;
                        // There is a chance that the Prefix will contain invalid UTF8,
                        // so we catch that and use the alternative way of displaying it
                        try
                        {
                            sb.Append(e.ToString());
                        }
                        catch (IndexOutOfRangeException)
                        {
                            sb.Append("BLOCK: ");
                            sb.Append(e.Prefix.ToString());
                        }
                        if (!it.MoveNext())
                        {
                            return sb.Append(']').ToString();
                        }
                        sb.Append(',').Append(' ');
                    }
                }
            }

            public void CompileIndex(IList<PendingBlock> floorBlocks, RAMOutputStream scratchBytes)
            {
                if (Debugging.AssertsEnabled)
                {
                    // LUCENENET specific - we use a custom wrapper struct to display floorBlocks, since
                    // it might contain garbage that cannot be converted into text.
                    Debugging.Assert((IsFloor && floorBlocks != null && floorBlocks.Count != 0) || (!IsFloor && floorBlocks is null), "isFloor={0} floorBlocks={1}", IsFloor, new PendingBlocksFormatter(floorBlocks));

                    Debugging.Assert(scratchBytes.Position == 0); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }

                // TODO: try writing the leading vLong in MSB order
                // (opposite of what Lucene does today), for better
                // outputs sharing in the FST
                scratchBytes.WriteVInt64(EncodeOutput(Fp, HasTerms, IsFloor));
                if (IsFloor)
                {
                    scratchBytes.WriteVInt32(floorBlocks.Count);
                    foreach (PendingBlock sub in floorBlocks)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(sub.FloorLeadByte != -1);
                        //if (DEBUG) {
                        //  System.out.println("    write floorLeadByte=" + Integer.toHexString(sub.floorLeadByte&0xff));
                        //}
                        scratchBytes.WriteByte((byte)sub.FloorLeadByte);
                        if (Debugging.AssertsEnabled) Debugging.Assert(sub.Fp > Fp);
                        scratchBytes.WriteVInt64((sub.Fp - Fp) << 1 | (uint)(sub.HasTerms ? 1 : 0));
                    }
                }

                ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
                Builder<BytesRef> indexBuilder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, 0, 0, true, false, int.MaxValue, outputs, null, false, PackedInt32s.COMPACT, true, 15);
                var bytes = new byte[(int)scratchBytes.Position]; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                if (Debugging.AssertsEnabled) Debugging.Assert(bytes.Length > 0);
                scratchBytes.WriteTo(bytes, 0);
                indexBuilder.Add(Util.ToInt32sRef(Prefix, scratchIntsRef), new BytesRef(bytes, 0, bytes.Length));
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
                while (subIndexEnum.MoveNext())
                {
                    indexEnt = subIndexEnum.Current;
                    //if (DEBUG) {
                    //  System.out.println("      add sub=" + indexEnt.input + " " + indexEnt.input + " output=" + indexEnt.output);
                    //}
                    builder.Add(Util.ToInt32sRef(indexEnt.Input, scratchIntsRef), indexEnt.Output);
                }
            }
        }

#pragma warning disable CA2213 // Disposable fields should be disposed
        internal readonly RAMOutputStream scratchBytes = new RAMOutputStream();
#pragma warning restore CA2213 // Disposable fields should be disposed

        internal class TermsWriter : TermsConsumer
        {
            private readonly BlockTreeTermsWriter<TSubclassState> outerInstance;

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
            private readonly IList<PendingEntry> pending = new JCG.List<PendingEntry>();

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
                private readonly BlockTreeTermsWriter<TSubclassState>.TermsWriter outerInstance;

                public FindBlocks(BlockTreeTermsWriter<TSubclassState>.TermsWriter outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override void Freeze(Builder.UnCompiledNode<object>[] frontier, int prefixLenPlus1, Int32sRef lastInput)
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

            internal virtual void WriteBlocks(Int32sRef prevTerm, int prefixLength, int count)
            {
                if (prefixLength == 0 || count <= outerInstance.maxItemsInBlock)
                {
                    // Easy case: not floor block.  Eg, prefix is "foo",
                    // and we found 30 terms/sub-blocks starting w/ that
                    // prefix, and minItemsInBlock <= 30 <=
                    // maxItemsInBlock.
                    PendingBlock nonFloorBlock = WriteBlock(prevTerm, prefixLength, prefixLength, count, count, /*0, LUCENENET: Never read */ false, -1, true);
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

                    int savLabel = prevTerm.Int32s[prevTerm.Offset + prefixLength];

                    // Count up how many items fall under
                    // each unique label after the prefix.

                    // TODO: this is wasteful since the builder had
                    // already done this (partitioned these sub-terms
                    // according to their leading prefix byte)

                    IList<PendingEntry> slice = pending.GetView(pending.Count - count, count); // LUCENENET: Converted end index to length
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
                                if (Debugging.AssertsEnabled)
                                {
                                    Debugging.Assert(lastSuffixLeadLabel == -1);
                                    Debugging.Assert(numSubs == 0);
                                }
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
                            if (Debugging.AssertsEnabled) Debugging.Assert(block.Prefix.Length > prefixLength);
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

                    IList<PendingBlock> floorBlocks = new JCG.List<PendingBlock>();
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
                                prevTerm.Int32s[prevTerm.Offset + prefixLength] = startLabel;
                            }
                            //System.out.println("  " + subCount + " subs");
                            PendingBlock floorBlock = WriteBlock(prevTerm, prefixLength, curPrefixLength, curStart, pendingCount, /*subTermCountSums[1 + sub], LUCENENET: Never read */ true, startLabel, curStart == pendingCount);
                            if (firstBlock is null)
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

                            if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.minItemsInBlock == 1 || subCount > 1, "minItemsInBlock={0} subCount={1} sub={2} of {3} subTermCount={4} subSubCount={5} depth={6}", outerInstance.minItemsInBlock, subCount, sub, numSubs, subTermCountSums[sub], subSubCounts[sub], prefixLength);
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
                                if (Debugging.AssertsEnabled)
                                {
                                    Debugging.Assert(startLabel != -1);
                                    Debugging.Assert(firstBlock != null);
                                }
                                prevTerm.Int32s[prevTerm.Offset + prefixLength] = startLabel;
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
                                floorBlocks.Add(WriteBlock(prevTerm, prefixLength, prefixLength + 1, curStart,curStart, /* 0, LUCENENET: Never read */ true, startLabel, true));
                                break;
                            }
                        }
                    }

                    prevTerm.Int32s[prevTerm.Offset + prefixLength] = savLabel;

                    if (Debugging.AssertsEnabled) Debugging.Assert(firstBlock != null);
                    firstBlock.CompileIndex(floorBlocks, outerInstance.scratchBytes);

                    pending.Add(firstBlock);
                    //if (DEBUG) System.out.println("  done pending.size()=" + pending.size());
                }
                lastBlockIndex = pending.Count - 1;
            }

            // for debugging
#pragma warning disable IDE0051 // Remove unused private members
            private static string ToString(BytesRef b) // LUCENENET: CA1822: Mark members as static
#pragma warning restore IDE0051 // Remove unused private members
            {
                try
                {
                    return b.Utf8ToString() + " " + b;
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    // If BytesRef isn't actually UTF8, or it's eg a
                    // prefix of UTF8 that ends mid-unicode-char, we
                    // fallback to hex:
                    return b.ToString();
                }
            }

            // Writes all entries in the pending slice as a single
            // block:
            private PendingBlock WriteBlock(Int32sRef prevTerm, int prefixLength, int indexPrefixLength,
                int startBackwards, int length, /*int futureTermCount, // LUCENENET: Not used*/
                bool isFloor, int floorLeadByte, bool isLastInFloor)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(length > 0);

                int start = pending.Count - startBackwards;

                if (Debugging.AssertsEnabled) Debugging.Assert(start >= 0, "pending.Count={0} startBackwards={1} length={2}", pending.Count, startBackwards, length);

                IList<PendingEntry> slice = pending.GetView(start, length); // LUCENENET: Converted end index to length

                long startFP = outerInstance.@out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                BytesRef prefix = new BytesRef(indexPrefixLength);
                for (int m = 0; m < indexPrefixLength; m++)
                {
                    prefix.Bytes[m] = (byte)prevTerm.Int32s[m];
                }
                prefix.Length = indexPrefixLength;

                // Write block header:
                outerInstance.@out.WriteVInt32((length << 1) | (isLastInFloor ? 1 : 0));

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
                        if (Debugging.AssertsEnabled) Debugging.Assert(ent.IsTerm);
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
                        suffixWriter.WriteVInt32(suffix);
                        suffixWriter.WriteBytes(term.Term.Bytes, prefixLength, suffix);

                        // Write term stats, to separate byte[] blob:
                        statsWriter.WriteVInt32(state.DocFreq);
                        if (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                        {
                            if (Debugging.AssertsEnabled) Debugging.Assert(state.TotalTermFreq >= state.DocFreq, "{0} vs {1}", state.TotalTermFreq, state.DocFreq);
                            statsWriter.WriteVInt64(state.TotalTermFreq - state.DocFreq);
                        }

                        // Write term meta data
                        outerInstance.postingsWriter.EncodeTerm(longs, bytesWriter, fieldInfo, state, absolute);
                        for (int pos = 0; pos < longsSize; pos++)
                        {
                            if (Debugging.AssertsEnabled) Debugging.Assert(longs[pos] >= 0);
                            metaWriter.WriteVInt64(longs[pos]);
                        }
                        bytesWriter.WriteTo(metaWriter);
                        bytesWriter.Reset();
                        absolute = false;
                    }
                    termCount = length;
                }
                else
                {
                    subIndices = new JCG.List<FST<BytesRef>>();
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
                            suffixWriter.WriteVInt32(suffix << 1);
                            suffixWriter.WriteBytes(term.Term.Bytes, prefixLength, suffix);

                            // Write term stats, to separate byte[] blob:
                            statsWriter.WriteVInt32(state.DocFreq);
                            if (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                            {
                                if (Debugging.AssertsEnabled) Debugging.Assert(state.TotalTermFreq >= state.DocFreq);
                                statsWriter.WriteVInt64(state.TotalTermFreq - state.DocFreq);
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
                                if (Debugging.AssertsEnabled) Debugging.Assert(longs[pos] >= 0);
                                metaWriter.WriteVInt64(longs[pos]);
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

                            if (Debugging.AssertsEnabled) Debugging.Assert(suffix > 0);

                            // For non-leaf block we borrow 1 bit to record
                            // if entry is term or sub-block
                            suffixWriter.WriteVInt32((suffix << 1) | 1);
                            suffixWriter.WriteBytes(block.Prefix.Bytes, prefixLength, suffix);
                            if (Debugging.AssertsEnabled) Debugging.Assert(block.Fp < startFP);

                            // if (DEBUG) {
                            //   BytesRef suffixBytes = new BytesRef(suffix);
                            //   System.arraycopy(block.prefix.bytes, prefixLength, suffixBytes.bytes, 0, suffix);
                            //   suffixBytes.length = suffix;
                            //   System.out.println("    write sub-block suffix=" + toString(suffixBytes) + " subFP=" + block.fp + " subCode=" + (startFP-block.fp) + " floor=" + block.isFloor);
                            // }

                            suffixWriter.WriteVInt64(startFP - block.Fp);
                            subIndices.Add(block.Index);
                        }
                    }

                    if (Debugging.AssertsEnabled) Debugging.Assert(subIndices.Count != 0);
                }

                // TODO: we could block-write the term suffix pointers;
                // this would take more space but would enable binary
                // search on lookup

                // Write suffixes byte[] blob to terms dict output:
                outerInstance.@out.WriteVInt32((int)(suffixWriter.Position << 1) | (isLeafBlock ? 1 : 0)); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                suffixWriter.WriteTo(outerInstance.@out);
                suffixWriter.Reset();

                // Write term stats byte[] blob
                outerInstance.@out.WriteVInt32((int)statsWriter.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                statsWriter.WriteTo(outerInstance.@out);
                statsWriter.Reset();

                // Write term meta data byte[] blob
                outerInstance.@out.WriteVInt32((int)metaWriter.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
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

            internal TermsWriter(BlockTreeTermsWriter<TSubclassState> outerInstance, FieldInfo fieldInfo)
            {
                this.outerInstance = outerInstance;
                this.fieldInfo = fieldInfo;

                noOutputs = NoOutputs.Singleton;

                // this Builder is just used transiently to fragment
                // terms into "good" blocks; we don't save the
                // resulting FST:
                blockBuilder = new Builder<object>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, noOutputs, new FindBlocks(this), false, PackedInt32s.COMPACT, true, 15);

                this.longsSize = outerInstance.postingsWriter.SetField(fieldInfo);
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override PostingsConsumer StartTerm(BytesRef text)
            {
                //if (DEBUG) System.out.println("\nBTTW.startTerm term=" + fieldInfo.name + ":" + toString(text) + " seg=" + segment);
                outerInstance.postingsWriter.StartTerm();
                /*
                if (fieldInfo.name.Equals("id", StringComparison.Ordinal)) {
                  postingsWriter.termID = Integer.parseInt(text.utf8ToString());
                } else {
                  postingsWriter.termID = -1;
                }
                */
                return outerInstance.postingsWriter;
            }

            private readonly Int32sRef scratchIntsRef = new Int32sRef();

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(stats.DocFreq > 0);
                //if (DEBUG) System.out.println("BTTW.finishTerm term=" + fieldInfo.name + ":" + toString(text) + " seg=" + segment + " df=" + stats.docFreq);

                blockBuilder.Add(Util.ToInt32sRef(text, scratchIntsRef), noOutputs.NoOutput);
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(pending.Count == 1 && !pending[0].IsTerm, "pending.Count={0} pending={1}", pending.Count, pending);
                    PendingBlock root = (PendingBlock)pending[0];
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(root.Prefix.Length == 0);
                        Debugging.Assert(root.Index.EmptyOutput != null);
                    }

                    this.sumTotalTermFreq = sumTotalTermFreq;
                    this.sumDocFreq = sumDocFreq;
                    this.docCount = docCount;

                    // Write FST to index
                    indexStartFP = outerInstance.indexOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
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
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(sumTotalTermFreq == 0 || fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY && sumTotalTermFreq == -1);
                        Debugging.Assert(sumDocFreq == 0);
                        Debugging.Assert(docCount == 0);
                    }
                }
            }

            internal readonly RAMOutputStream suffixWriter = new RAMOutputStream();
            internal readonly RAMOutputStream statsWriter = new RAMOutputStream();
            internal readonly RAMOutputStream metaWriter = new RAMOutputStream();
            internal readonly RAMOutputStream bytesWriter = new RAMOutputStream();
        }

        /// <summary>
        /// Disposes all resources used by this object.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Exception ioe = null; // LUCENENET: No need to cast to IOExcpetion
                try
                {
                    long dirStart = @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    long indexDirStart = indexOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                    @out.WriteVInt32(fields.Count);

                    foreach (FieldMetaData field in fields)
                    {
                        //System.out.println("  field " + field.fieldInfo.name + " " + field.numTerms + " terms");
                        @out.WriteVInt32(field.FieldInfo.Number);
                        @out.WriteVInt64(field.NumTerms);
                        @out.WriteVInt32(field.RootCode.Length);
                        @out.WriteBytes(field.RootCode.Bytes, field.RootCode.Offset, field.RootCode.Length);
                        if (field.FieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                        {
                            @out.WriteVInt64(field.SumTotalTermFreq);
                        }
                        @out.WriteVInt64(field.SumDocFreq);
                        @out.WriteVInt32(field.DocCount);
                        @out.WriteVInt32(field.Int64sSize);
                        indexOut.WriteVInt64(field.IndexStartFP);
                    }
                    WriteTrailer(@out, dirStart);
                    CodecUtil.WriteFooter(@out);
                    WriteIndexTrailer(indexOut, indexDirStart);
                    CodecUtil.WriteFooter(indexOut);
                }
                catch (Exception ioe2) when (ioe2.IsIOException())
                {
                    ioe = ioe2;
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(ioe, @out, indexOut, postingsWriter, scratchBytes); // LUCENENET: Added scratchBytes
                }
            }
        }
    }
}