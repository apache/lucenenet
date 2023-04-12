using Lucene.Net.Diagnostics;

namespace Lucene.Net.Codecs.Lucene41
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

    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Lucene 4.1 postings format, which encodes postings in packed integer blocks
    /// for fast decode.
    ///
    /// <para><b>NOTE</b>: this format is still experimental and
    /// subject to change without backwards compatibility.
    ///
    /// <para>
    /// Basic idea:
    /// <list type="bullet">
    ///   <item><description>
    ///   <b>Packed Blocks and VInt Blocks</b>:
    ///   <para>In packed blocks, integers are encoded with the same bit width packed format (<see cref="Util.Packed.PackedInt32s"/>):
    ///      the block size (i.e. number of integers inside block) is fixed (currently 128). Additionally blocks
    ///      that are all the same value are encoded in an optimized way.</para>
    ///   <para>In VInt blocks, integers are encoded as VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>):
    ///      the block size is variable.</para>
    ///   </description></item>
    ///
    ///   <item><description>
    ///   <b>Block structure</b>:
    ///   <para>When the postings are long enough, Lucene41PostingsFormat will try to encode most integer data
    ///      as a packed block.</para>
    ///   <para>Take a term with 259 documents as an example, the first 256 document ids are encoded as two packed
    ///      blocks, while the remaining 3 are encoded as one VInt block. </para>
    ///   <para>Different kinds of data are always encoded separately into different packed blocks, but may
    ///      possibly be interleaved into the same VInt block. </para>
    ///   <para>This strategy is applied to pairs:
    ///      &lt;document number, frequency&gt;,
    ///      &lt;position, payload length&gt;,
    ///      &lt;position, offset start, offset length&gt;, and
    ///      &lt;position, payload length, offsetstart, offset length&gt;.</para>
    ///   </description></item>
    ///
    ///   <item><description>
    ///   <b>Skipdata settings</b>:
    ///   <para>The structure of skip table is quite similar to previous version of Lucene. Skip interval is the
    ///      same as block size, and each skip entry points to the beginning of each block. However, for
    ///      the first block, skip data is omitted.</para>
    ///   </description></item>
    ///
    ///   <item><description>
    ///   <b>Positions, Payloads, and Offsets</b>:
    ///   <para>A position is an integer indicating where the term occurs within one document.
    ///      A payload is a blob of metadata associated with current position.
    ///      An offset is a pair of integers indicating the tokenized start/end offsets for given term
    ///      in current position: it is essentially a specialized payload. </para>
    ///   <para>When payloads and offsets are not omitted, numPositions==numPayloads==numOffsets (assuming a
    ///      null payload contributes one count). As mentioned in block structure, it is possible to encode
    ///      these three either combined or separately.</para>
    ///   <para>In all cases, payloads and offsets are stored together. When encoded as a packed block,
    ///      position data is separated out as .pos, while payloads and offsets are encoded in .pay (payload
    ///      metadata will also be stored directly in .pay). When encoded as VInt blocks, all these three are
    ///      stored interleaved into the .pos (so is payload metadata).</para>
    ///   <para>With this strategy, the majority of payload and offset data will be outside .pos file.
    ///      So for queries that require only position data, running on a full index with payloads and offsets,
    ///      this reduces disk pre-fetches.</para>
    ///   </description></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Files and detailed format:
    /// <list type="bullet">
    ///   <item><description><c>.tim</c>: <a href="#Termdictionary">Term Dictionary</a></description></item>
    ///   <item><description><c>.tip</c>: <a href="#Termindex">Term Index</a></description></item>
    ///   <item><description><c>.doc</c>: <a href="#Frequencies">Frequencies and Skip Data</a></description></item>
    ///   <item><description><c>.pos</c>: <a href="#Positions">Positions</a></description></item>
    ///   <item><description><c>.pay</c>: <a href="#Payloads">Payloads and Offsets</a></description></item>
    /// </list>
    /// </para>
    ///
    /// <a name="Termdictionary" id="Termdictionary"></a>
    /// <dl>
    /// <dd>
    /// <b>Term Dictionary</b>
    ///
    /// <para>The .tim file contains the list of terms in each
    /// field along with per-term statistics (such as docfreq)
    /// and pointers to the frequencies, positions, payload and
    /// skip data in the .doc, .pos, and .pay files.
    /// See <see cref="BlockTreeTermsWriter{TSubclassState}"/> for more details on the format.
    /// </para>
    ///
    /// <para>NOTE: The term dictionary can plug into different postings implementations:
    /// the postings writer/reader are actually responsible for encoding
    /// and decoding the PostingsHeader and TermMetadata sections described here:</para>
    ///
    /// <list type="bullet">
    ///   <item><description>PostingsHeader --&gt; Header, PackedBlockSize</description></item>
    ///   <item><description>TermMetadata --&gt; (DocFPDelta|SingletonDocID), PosFPDelta?, PosVIntBlockFPDelta?, PayFPDelta?,
    ///                            SkipFPDelta?</description></item>
    ///   <item><description>Header, --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>PackedBlockSize, SingletonDocID --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>DocFPDelta, PosFPDelta, PayFPDelta, PosVIntBlockFPDelta, SkipFPDelta --&gt; VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </description></item>
    ///   <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///    <item><description>Header is a CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) storing the version information
    ///        for the postings.</description></item>
    ///    <item><description>PackedBlockSize is the fixed block size for packed blocks. In packed block, bit width is
    ///        determined by the largest integer. Smaller block size result in smaller variance among width
    ///        of integers hence smaller indexes. Larger block size result in more efficient bulk i/o hence
    ///        better acceleration. This value should always be a multiple of 64, currently fixed as 128 as
    ///        a tradeoff. It is also the skip interval used to accelerate <see cref="Search.DocIdSetIterator.Advance(int)"/>.</description></item>
    ///    <item><description>DocFPDelta determines the position of this term's TermFreqs within the .doc file.
    ///        In particular, it is the difference of file offset between this term's
    ///        data and previous term's data (or zero, for the first term in the block).On disk it is
    ///        stored as the difference from previous value in sequence. </description></item>
    ///    <item><description>PosFPDelta determines the position of this term's TermPositions within the .pos file.
    ///        While PayFPDelta determines the position of this term's &lt;TermPayloads, TermOffsets?&gt; within
    ///        the .pay file. Similar to DocFPDelta, it is the difference between two file positions (or
    ///        neglected, for fields that omit payloads and offsets).</description></item>
    ///    <item><description>PosVIntBlockFPDelta determines the position of this term's last TermPosition in last pos packed
    ///        block within the .pos file. It is synonym for PayVIntBlockFPDelta or OffsetVIntBlockFPDelta.
    ///        This is actually used to indicate whether it is necessary to load following
    ///        payloads and offsets from .pos instead of .pay. Every time a new block of positions are to be
    ///        loaded, the PostingsReader will use this value to check whether current block is packed format
    ///        or VInt. When packed format, payloads and offsets are fetched from .pay, otherwise from .pos.
    ///        (this value is neglected when total number of positions i.e. totalTermFreq is less or equal
    ///        to PackedBlockSize).</description></item>
    ///    <item><description>SkipFPDelta determines the position of this term's SkipData within the .doc
    ///        file. In particular, it is the length of the TermFreq data.
    ///        SkipDelta is only stored if DocFreq is not smaller than SkipMinimum
    ///        (i.e. 128 in Lucene41PostingsFormat).</description></item>
    ///    <item><description>SingletonDocID is an optimization when a term only appears in one document. In this case, instead
    ///        of writing a file pointer to the .doc file (DocFPDelta), and then a VIntBlock at that location, the
    ///        single document ID is written to the term dictionary.</description></item>
    /// </list>
    /// </dd>
    /// </dl>
    ///
    /// <a name="Termindex" id="Termindex"></a>
    /// <dl>
    /// <dd>
    /// <b>Term Index</b>
    /// <para>The .tip file contains an index into the term dictionary, so that it can be
    /// accessed randomly.  See <see cref="BlockTreeTermsWriter{TSubclassState}"/> for more details on the format.</para>
    /// </dd>
    /// </dl>
    ///
    ///
    /// <a name="Frequencies" id="Frequencies"></a>
    /// <dl>
    /// <dd>
    /// <b>Frequencies and Skip Data</b>
    ///
    /// <para>The .doc file contains the lists of documents which contain each term, along
    /// with the frequency of the term in that document (except when frequencies are
    /// omitted: <see cref="Index.IndexOptions.DOCS_ONLY"/>). It also saves skip data to the beginning of
    /// each packed or VInt block, when the length of document list is larger than packed block size.</para>
    ///
    /// <list type="bullet">
    ///   <item><description>docFile(.doc) --&gt; Header, &lt;TermFreqs, SkipData?&gt;<sup>TermCount</sup>, Footer</description></item>
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>)</description></item>
    ///   <item><description>TermFreqs --&gt; &lt;PackedBlock&gt; <sup>PackedDocBlockNum</sup>,
    ///                        VIntBlock? </description></item>
    ///   <item><description>PackedBlock --&gt; PackedDocDeltaBlock, PackedFreqBlock?</description></item>
    ///   <item><description>VIntBlock --&gt; &lt;DocDelta[, Freq?]&gt;<sup>DocFreq-PackedBlockSize*PackedDocBlockNum</sup></description></item>
    ///   <item><description>SkipData --&gt; &lt;&lt;SkipLevelLength, SkipLevel&gt;
    ///       <sup>NumSkipLevels-1</sup>, SkipLevel&gt;, SkipDatum?</description></item>
    ///   <item><description>SkipLevel --&gt; &lt;SkipDatum&gt; <sup>TrimmedDocFreq/(PackedBlockSize^(Level + 1))</sup></description></item>
    ///   <item><description>SkipDatum --&gt; DocSkip, DocFPSkip, &lt;PosFPSkip, PosBlockOffset, PayLength?,
    ///                        PayFPSkip?&gt;?, SkipChildLevelPointer?</description></item>
    ///   <item><description>PackedDocDeltaBlock, PackedFreqBlock --&gt; PackedInts (<see cref="Util.Packed.PackedInt32s"/>) </description></item>
    ///   <item><description>DocDelta, Freq, DocSkip, DocFPSkip, PosFPSkip, PosBlockOffset, PayByteUpto, PayFPSkip
    ///       --&gt;
    ///   VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>SkipChildLevelPointer --&gt; VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </description></item>
    ///   <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///   <item><description>PackedDocDeltaBlock is theoretically generated from two steps:
    ///     <list type="number">
    ///       <item><description>Calculate the difference between each document number and previous one,
    ///           and get a d-gaps list (for the first document, use absolute value); </description></item>
    ///       <item><description>For those d-gaps from first one to PackedDocBlockNum*PackedBlockSize<sup>th</sup>,
    ///           separately encode as packed blocks.</description></item>
    ///     </list>
    ///     If frequencies are not omitted, PackedFreqBlock will be generated without d-gap step.
    ///   </description></item>
    ///   <item><description>VIntBlock stores remaining d-gaps (along with frequencies when possible) with a format
    ///       that encodes DocDelta and Freq:
    ///       <para>DocDelta: if frequencies are indexed, this determines both the document
    ///       number and the frequency. In particular, DocDelta/2 is the difference between
    ///       this document number and the previous document number (or zero when this is the
    ///       first document in a TermFreqs). When DocDelta is odd, the frequency is one.
    ///       When DocDelta is even, the frequency is read as another VInt. If frequencies
    ///       are omitted, DocDelta contains the gap (not multiplied by 2) between document
    ///       numbers and no frequency information is stored.</para>
    ///       <para>For example, the TermFreqs for a term which occurs once in document seven
    ///          and three times in document eleven, with frequencies indexed, would be the
    ///          following sequence of VInts:</para>
    ///       <para>15, 8, 3</para>
    ///       <para>If frequencies were omitted (<see cref="Index.IndexOptions.DOCS_ONLY"/>) it would be this
    ///          sequence of VInts instead:</para>
    ///       <para>7,4</para>
    ///   </description></item>
    ///   <item><description>PackedDocBlockNum is the number of packed blocks for current term's docids or frequencies.
    ///       In particular, PackedDocBlockNum = floor(DocFreq/PackedBlockSize) </description></item>
    ///   <item><description>TrimmedDocFreq = DocFreq % PackedBlockSize == 0 ? DocFreq - 1 : DocFreq.
    ///       We use this trick since the definition of skip entry is a little different from base interface.
    ///       In <see cref="MultiLevelSkipListWriter"/>, skip data is assumed to be saved for
    ///       skipInterval<sup>th</sup>, 2*skipInterval<sup>th</sup> ... posting in the list. However,
    ///       in Lucene41PostingsFormat, the skip data is saved for skipInterval+1<sup>th</sup>,
    ///       2*skipInterval+1<sup>th</sup> ... posting (skipInterval==PackedBlockSize in this case).
    ///       When DocFreq is multiple of PackedBlockSize, MultiLevelSkipListWriter will expect one
    ///       more skip data than Lucene41SkipWriter. </description></item>
    ///   <item><description>SkipDatum is the metadata of one skip entry.
    ///      For the first block (no matter packed or VInt), it is omitted.</description></item>
    ///   <item><description>DocSkip records the document number of every PackedBlockSize<sup>th</sup> document number in
    ///       the postings (i.e. last document number in each packed block). On disk it is stored as the
    ///       difference from previous value in the sequence. </description></item>
    ///   <item><description>DocFPSkip records the file offsets of each block (excluding )posting at
    ///       PackedBlockSize+1<sup>th</sup>, 2*PackedBlockSize+1<sup>th</sup> ... , in DocFile.
    ///       The file offsets are relative to the start of current term's TermFreqs.
    ///       On disk it is also stored as the difference from previous SkipDatum in the sequence.</description></item>
    ///   <item><description>Since positions and payloads are also block encoded, the skip should skip to related block first,
    ///       then fetch the values according to in-block offset. PosFPSkip and PayFPSkip record the file
    ///       offsets of related block in .pos and .pay, respectively. While PosBlockOffset indicates
    ///       which value to fetch inside the related block (PayBlockOffset is unnecessary since it is always
    ///       equal to PosBlockOffset). Same as DocFPSkip, the file offsets are relative to the start of
    ///       current term's TermFreqs, and stored as a difference sequence.</description></item>
    ///   <item><description>PayByteUpto indicates the start offset of the current payload. It is equivalent to
    ///       the sum of the payload lengths in the current block up to PosBlockOffset</description></item>
    /// </list>
    /// </dd>
    /// </dl>
    ///
    /// <a name="Positions" id="Positions"></a>
    /// <dl>
    /// <dd>
    /// <b>Positions</b>
    /// <para>The .pos file contains the lists of positions that each term occurs at within documents. It also
    ///    sometimes stores part of payloads and offsets for speedup.</para>
    /// <list type="bullet">
    ///   <item><description>PosFile(.pos) --&gt; Header, &lt;TermPositions&gt; <sup>TermCount</sup>, Footer</description></item>
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>TermPositions --&gt; &lt;PackedPosDeltaBlock&gt; <sup>PackedPosBlockNum</sup>,
    ///                            VIntBlock? </description></item>
    ///   <item><description>VIntBlock --&gt; &lt;PositionDelta[, PayloadLength?], PayloadData?,
    ///                        OffsetDelta?, OffsetLength?&gt;<sup>PosVIntCount</sup></description></item>
    ///   <item><description>PackedPosDeltaBlock --&gt; PackedInts (<see cref="Util.Packed.PackedInt32s"/>)</description></item>
    ///   <item><description>PositionDelta, OffsetDelta, OffsetLength --&gt;
    ///       VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>PayloadData --&gt; byte (<see cref="Store.DataOutput.WriteByte(byte)"/>)<sup>PayLength</sup></description></item>
    ///   <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///   <item><description>TermPositions are order by term (terms are implicit, from the term dictionary), and position
    ///       values for each term document pair are incremental, and ordered by document number.</description></item>
    ///   <item><description>PackedPosBlockNum is the number of packed blocks for current term's positions, payloads or offsets.
    ///       In particular, PackedPosBlockNum = floor(totalTermFreq/PackedBlockSize) </description></item>
    ///   <item><description>PosVIntCount is the number of positions encoded as VInt format. In particular,
    ///       PosVIntCount = totalTermFreq - PackedPosBlockNum*PackedBlockSize</description></item>
    ///   <item><description>The procedure how PackedPosDeltaBlock is generated is the same as PackedDocDeltaBlock
    ///       in chapter <a href="#Frequencies">Frequencies and Skip Data</a>.</description></item>
    ///   <item><description>PositionDelta is, if payloads are disabled for the term's field, the
    ///       difference between the position of the current occurrence in the document and
    ///       the previous occurrence (or zero, if this is the first occurrence in this
    ///       document). If payloads are enabled for the term's field, then PositionDelta/2
    ///       is the difference between the current and the previous position. If payloads
    ///       are enabled and PositionDelta is odd, then PayloadLength is stored, indicating
    ///       the length of the payload at the current term position.</description></item>
    ///   <item><description>For example, the TermPositions for a term which occurs as the fourth term in
    ///       one document, and as the fifth and ninth term in a subsequent document, would
    ///       be the following sequence of VInts (payloads disabled):
    ///       <para>4, 5, 4</para></description></item>
    ///   <item><description>PayloadData is metadata associated with the current term position. If
    ///       PayloadLength is stored at the current position, then it indicates the length
    ///       of this payload. If PayloadLength is not stored, then this payload has the same
    ///       length as the payload at the previous position.</description></item>
    ///   <item><description>OffsetDelta/2 is the difference between this position's startOffset from the
    ///       previous occurrence (or zero, if this is the first occurrence in this document).
    ///       If OffsetDelta is odd, then the length (endOffset-startOffset) differs from the
    ///       previous occurrence and an OffsetLength follows. Offset data is only written for
    ///       <see cref="Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/>.</description></item>
    /// </list>
    /// </dd>
    /// </dl>
    ///
    /// <a name="Payloads" id="Payloads"></a>
    /// <dl>
    /// <dd>
    /// <b>Payloads and Offsets</b>
    /// <para>The .pay file will store payloads and offsets associated with certain term-document positions.
    ///    Some payloads and offsets will be separated out into .pos file, for performance reasons.</para>
    /// <list type="bullet">
    ///   <item><description>PayFile(.pay): --&gt; Header, &lt;TermPayloads, TermOffsets?&gt; <sup>TermCount</sup>, Footer</description></item>
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>TermPayloads --&gt; &lt;PackedPayLengthBlock, SumPayLength, PayData&gt; <sup>PackedPayBlockNum</sup></description></item>
    ///   <item><description>TermOffsets --&gt; &lt;PackedOffsetStartDeltaBlock, PackedOffsetLengthBlock&gt; <sup>PackedPayBlockNum</sup></description></item>
    ///   <item><description>PackedPayLengthBlock, PackedOffsetStartDeltaBlock, PackedOffsetLengthBlock --&gt; PackedInts (<see cref="Util.Packed.PackedInt32s"/>) </description></item>
    ///   <item><description>SumPayLength --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>PayData --&gt; byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) <sup>SumPayLength</sup></description></item>
    ///   <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///   <item><description>The order of TermPayloads/TermOffsets will be the same as TermPositions, note that part of
    ///       payload/offsets are stored in .pos.</description></item>
    ///   <item><description>The procedure how PackedPayLengthBlock and PackedOffsetLengthBlock are generated is the
    ///       same as PackedFreqBlock in chapter <a href="#Frequencies">Frequencies and Skip Data</a>.
    ///       While PackedStartDeltaBlock follows a same procedure as PackedDocDeltaBlock.</description></item>
    ///   <item><description>PackedPayBlockNum is always equal to PackedPosBlockNum, for the same term. It is also synonym
    ///       for PackedOffsetBlockNum.</description></item>
    ///   <item><description>SumPayLength is the total length of payloads written within one block, should be the sum
    ///       of PayLengths in one packed block.</description></item>
    ///   <item><description>PayLength in PackedPayLengthBlock is the length of each payload associated with the current
    ///       position.</description></item>
    /// </list>
    /// </dd>
    /// </dl>
    /// </para>
    ///
    /// @lucene.experimental
    /// </summary>
    [PostingsFormatName("Lucene41")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class Lucene41PostingsFormat : PostingsFormat
    {
        /// <summary>
        /// Filename extension for document number, frequencies, and skip data.
        /// See chapter: <a href="#Frequencies">Frequencies and Skip Data</a>
        /// </summary>
        public const string DOC_EXTENSION = "doc";

        /// <summary>
        /// Filename extension for positions.
        /// See chapter: <a href="#Positions">Positions</a>
        /// </summary>
        public const string POS_EXTENSION = "pos";

        /// <summary>
        /// Filename extension for payloads and offsets.
        /// See chapter: <a href="#Payloads">Payloads and Offsets</a>
        /// </summary>
        public const string PAY_EXTENSION = "pay";

        private readonly int minTermBlockSize;
        private readonly int maxTermBlockSize;

        /// <summary>
        /// Fixed packed block size, number of integers encoded in
        /// a single packed block.
        /// </summary>
        // NOTE: must be multiple of 64 because of PackedInts long-aligned encoding/decoding
        public static int BLOCK_SIZE = 128;

        /// <summary>
        /// Creates <see cref="Lucene41PostingsFormat"/> with default
        /// settings.
        /// </summary>
        public Lucene41PostingsFormat()
            : this(BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
        {
        }

        /// <summary>
        /// Creates <see cref="Lucene41PostingsFormat"/> with custom
        /// values for <paramref name="minTermBlockSize"/> and 
        /// <paramref name="maxTermBlockSize"/> passed to block terms dictionary. </summary>
        /// <seealso cref="BlockTreeTermsWriter{TSubclassState}.BlockTreeTermsWriter(SegmentWriteState,PostingsWriterBase,int,int,TSubclassState)"/>
        public Lucene41PostingsFormat(int minTermBlockSize, int maxTermBlockSize)
            : base()
        {
            this.minTermBlockSize = minTermBlockSize;
            if (Debugging.AssertsEnabled) Debugging.Assert(minTermBlockSize > 1);
            this.maxTermBlockSize = maxTermBlockSize;
            if (Debugging.AssertsEnabled) Debugging.Assert(minTermBlockSize <= maxTermBlockSize);
        }

        public override string ToString()
        {
            return Name + "(blocksize=" + BLOCK_SIZE + ")";
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            PostingsWriterBase postingsWriter = new Lucene41PostingsWriter(state);

            bool success = false;
            try
            {
                FieldsConsumer ret = new BlockTreeTermsWriter<object>(state, postingsWriter, minTermBlockSize, maxTermBlockSize, subclassState: null);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(postingsWriter);
                }
            }
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase postingsReader = new Lucene41PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
            bool success = false;
            try
            {
                FieldsProducer ret = new BlockTreeTermsReader<object>(state.Directory, state.FieldInfos, state.SegmentInfo, postingsReader, state.Context, state.SegmentSuffix, state.TermsIndexDivisor, subclassState: null);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(postingsReader);
                }
            }
        }
    }
}