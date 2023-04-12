using Lucene.Net.Diagnostics;
using System;

namespace Lucene.Net.Codecs.Lucene40
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

    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Lucene 4.0 Postings format.
    /// <para>
    /// Files:
    /// <list type="bullet">
    ///   <item><description><tt>.tim</tt>: <a href="#Termdictionary">Term Dictionary</a></description></item>
    ///   <item><description><tt>.tip</tt>: <a href="#Termindex">Term Index</a></description></item>
    ///   <item><description><tt>.frq</tt>: <a href="#Frequencies">Frequencies</a></description></item>
    ///   <item><description><tt>.prx</tt>: <a href="#Positions">Positions</a></description></item>
    /// </list>
    /// </para>
    /// <para/>
    /// <a name="Termdictionary" id="Termdictionary"></a>
    /// <h3>Term Dictionary</h3>
    ///
    /// <para>The .tim file contains the list of terms in each
    /// field along with per-term statistics (such as docfreq)
    /// and pointers to the frequencies, positions and
    /// skip data in the .frq and .prx files.
    /// See <see cref="BlockTreeTermsWriter{TSubclassState}"/> for more details on the format.
    /// </para>
    ///
    /// <para>NOTE: The term dictionary can plug into different postings implementations:
    /// the postings writer/reader are actually responsible for encoding
    /// and decoding the Postings Metadata and Term Metadata sections described here:</para>
    /// <list type="bullet">
    ///    <item><description>Postings Metadata --&gt; Header, SkipInterval, MaxSkipLevels, SkipMinimum</description></item>
    ///    <item><description>Term Metadata --&gt; FreqDelta, SkipDelta?, ProxDelta?</description></item>
    ///    <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///    <item><description>SkipInterval,MaxSkipLevels,SkipMinimum --&gt; Uint32 (<see cref="Store.DataOutput.WriteInt32(int)"/>) </description></item>
    ///    <item><description>SkipDelta,FreqDelta,ProxDelta --&gt; VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///    <item><description>Header is a CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>)  storing the version information
    ///        for the postings.</description></item>
    ///    <item><description>SkipInterval is the fraction of TermDocs stored in skip tables. It is used to accelerate
    ///        <see cref="Search.DocIdSetIterator.Advance(int)"/>. Larger values result in smaller indexes, greater
    ///        acceleration, but fewer accelerable cases, while smaller values result in bigger indexes,
    ///        less acceleration (in case of a small value for MaxSkipLevels) and more accelerable cases.
    ///        </description></item>
    ///    <item><description>MaxSkipLevels is the max. number of skip levels stored for each term in the .frq file. A
    ///        low value results in smaller indexes but less acceleration, a larger value results in
    ///        slightly larger indexes but greater acceleration. See format of .frq file for more
    ///        information about skip levels.</description></item>
    ///    <item><description>SkipMinimum is the minimum document frequency a term must have in order to write any
    ///        skip data at all.</description></item>
    ///    <item><description>FreqDelta determines the position of this term's TermFreqs within the .frq
    ///        file. In particular, it is the difference between the position of this term's
    ///        data in that file and the position of the previous term's data (or zero, for
    ///        the first term in the block).</description></item>
    ///    <item><description>ProxDelta determines the position of this term's TermPositions within the
    ///        .prx file. In particular, it is the difference between the position of this
    ///        term's data in that file and the position of the previous term's data (or zero,
    ///        for the first term in the block. For fields that omit position data, this will
    ///        be 0 since prox information is not stored.</description></item>
    ///    <item><description>SkipDelta determines the position of this term's SkipData within the .frq
    ///        file. In particular, it is the number of bytes after TermFreqs that the
    ///        SkipData starts. In other words, it is the length of the TermFreq data.
    ///        SkipDelta is only stored if DocFreq is not smaller than SkipMinimum.</description></item>
    /// </list>
    /// <a name="Termindex" id="Termindex"></a>
    /// <h3>Term Index</h3>
    /// <para>The .tip file contains an index into the term dictionary, so that it can be
    /// accessed randomly.  See <see cref="BlockTreeTermsWriter{TSubclassState}"/> for more details on the format.</para>
    /// <a name="Frequencies" id="Frequencies"></a>
    /// <h3>Frequencies</h3>
    /// <para>The .frq file contains the lists of documents which contain each term, along
    /// with the frequency of the term in that document (except when frequencies are
    /// omitted: <see cref="Index.IndexOptions.DOCS_ONLY"/>).</para>
    /// <list type="bullet">
    ///   <item><description>FreqFile (.frq) --&gt; Header, &lt;TermFreqs, SkipData?&gt; <sup>TermCount</sup></description></item>
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>TermFreqs --&gt; &lt;TermFreq&gt; <sup>DocFreq</sup></description></item>
    ///   <item><description>TermFreq --&gt; DocDelta[, Freq?]</description></item>
    ///   <item><description>SkipData --&gt; &lt;&lt;SkipLevelLength, SkipLevel&gt;
    ///       <sup>NumSkipLevels-1</sup>, SkipLevel&gt; &lt;SkipDatum&gt;</description></item>
    ///   <item><description>SkipLevel --&gt; &lt;SkipDatum&gt; <sup>DocFreq/(SkipInterval^(Level +
    ///       1))</sup></description></item>
    ///   <item><description>SkipDatum --&gt;
    ///       DocSkip,PayloadLength?,OffsetLength?,FreqSkip,ProxSkip,SkipChildLevelPointer?</description></item>
    ///   <item><description>DocDelta,Freq,DocSkip,PayloadLength,OffsetLength,FreqSkip,ProxSkip --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>SkipChildLevelPointer --&gt; VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </description></item>
    /// </list>
    /// <para>TermFreqs are ordered by term (the term is implicit, from the term dictionary).</para>
    /// <para>TermFreq entries are ordered by increasing document number.</para>
    /// <para>DocDelta: if frequencies are indexed, this determines both the document
    /// number and the frequency. In particular, DocDelta/2 is the difference between
    /// this document number and the previous document number (or zero when this is the
    /// first document in a TermFreqs). When DocDelta is odd, the frequency is one.
    /// When DocDelta is even, the frequency is read as another VInt. If frequencies
    /// are omitted, DocDelta contains the gap (not multiplied by 2) between document
    /// numbers and no frequency information is stored.</para>
    /// <para>For example, the TermFreqs for a term which occurs once in document seven
    /// and three times in document eleven, with frequencies indexed, would be the
    /// following sequence of VInts:</para>
    /// <para>15, 8, 3</para>
    /// <para>If frequencies were omitted (<see cref="Index.IndexOptions.DOCS_ONLY"/>) it would be this
    /// sequence of VInts instead:</para>
    /// <para>7,4</para>
    /// <para>DocSkip records the document number before every SkipInterval <sup>th</sup>
    /// document in TermFreqs. If payloads and offsets are disabled for the term's field, then
    /// DocSkip represents the difference from the previous value in the sequence. If
    /// payloads and/or offsets are enabled for the term's field, then DocSkip/2 represents the
    /// difference from the previous value in the sequence. In this case when
    /// DocSkip is odd, then PayloadLength and/or OffsetLength are stored indicating the length of
    /// the last payload/offset before the SkipInterval<sup>th</sup> document in TermPositions.</para>
    /// <para>PayloadLength indicates the length of the last payload.</para>
    /// <para>OffsetLength indicates the length of the last offset (endOffset-startOffset).</para>
    /// <para>
    /// FreqSkip and ProxSkip record the position of every SkipInterval <sup>th</sup>
    /// entry in FreqFile and ProxFile, respectively. File positions are relative to
    /// the start of TermFreqs and Positions, to the previous SkipDatum in the
    /// sequence.</para>
    /// <para>For example, if DocFreq=35 and SkipInterval=16, then there are two SkipData
    /// entries, containing the 15 <sup>th</sup> and 31 <sup>st</sup> document numbers
    /// in TermFreqs. The first FreqSkip names the number of bytes after the beginning
    /// of TermFreqs that the 16 <sup>th</sup> SkipDatum starts, and the second the
    /// number of bytes after that that the 32 <sup>nd</sup> starts. The first ProxSkip
    /// names the number of bytes after the beginning of Positions that the 16
    /// <sup>th</sup> SkipDatum starts, and the second the number of bytes after that
    /// that the 32 <sup>nd</sup> starts.</para>
    /// <para>Each term can have multiple skip levels. The amount of skip levels for a
    /// term is NumSkipLevels = Min(MaxSkipLevels,
    /// floor(log(DocFreq/log(SkipInterval)))). The number of SkipData entries for a
    /// skip level is DocFreq/(SkipInterval^(Level + 1)), whereas the lowest skip level
    /// is Level=0.
    /// <para/>
    /// Example: SkipInterval = 4, MaxSkipLevels = 2, DocFreq = 35. Then skip level 0
    /// has 8 SkipData entries, containing the 3<sup>rd</sup>, 7<sup>th</sup>,
    /// 11<sup>th</sup>, 15<sup>th</sup>, 19<sup>th</sup>, 23<sup>rd</sup>,
    /// 27<sup>th</sup>, and 31<sup>st</sup> document numbers in TermFreqs. Skip level
    /// 1 has 2 SkipData entries, containing the 15<sup>th</sup> and 31<sup>st</sup>
    /// document numbers in TermFreqs.
    /// <para/>
    /// The SkipData entries on all upper levels &gt; 0 contain a SkipChildLevelPointer
    /// referencing the corresponding SkipData entry in level-1. In the example has
    /// entry 15 on level 1 a pointer to entry 15 on level 0 and entry 31 on level 1 a
    /// pointer to entry 31 on level 0.
    /// </para>
    /// <a name="Positions" id="Positions"></a>
    /// <h3>Positions</h3>
    /// <para>The .prx file contains the lists of positions that each term occurs at
    /// within documents. Note that fields omitting positional data do not store
    /// anything into this file, and if all fields in the index omit positional data
    /// then the .prx file will not exist.</para>
    /// <list type="bullet">
    ///   <item><description>ProxFile (.prx) --&gt; Header, &lt;TermPositions&gt; <sup>TermCount</sup></description></item>
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///   <item><description>TermPositions --&gt; &lt;Positions&gt; <sup>DocFreq</sup></description></item>
    ///   <item><description>Positions --&gt; &lt;PositionDelta,PayloadLength?,OffsetDelta?,OffsetLength?,PayloadData?&gt; <sup>Freq</sup></description></item>
    ///   <item><description>PositionDelta,OffsetDelta,OffsetLength,PayloadLength --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>PayloadData --&gt; byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) <sup>PayloadLength</sup></description></item>
    /// </list>
    /// <para>TermPositions are ordered by term (the term is implicit, from the term dictionary).</para>
    /// <para>Positions entries are ordered by increasing document number (the document
    /// number is implicit from the .frq file).</para>
    /// <para>PositionDelta is, if payloads are disabled for the term's field, the
    /// difference between the position of the current occurrence in the document and
    /// the previous occurrence (or zero, if this is the first occurrence in this
    /// document). If payloads are enabled for the term's field, then PositionDelta/2
    /// is the difference between the current and the previous position. If payloads
    /// are enabled and PositionDelta is odd, then PayloadLength is stored, indicating
    /// the length of the payload at the current term position.</para>
    /// <para>For example, the TermPositions for a term which occurs as the fourth term in
    /// one document, and as the fifth and ninth term in a subsequent document, would
    /// be the following sequence of VInts (payloads disabled):</para>
    /// <para>4, 5, 4</para>
    /// <para>PayloadData is metadata associated with the current term position. If
    /// PayloadLength is stored at the current position, then it indicates the length
    /// of this payload. If PayloadLength is not stored, then this payload has the same
    /// length as the payload at the previous position.</para>
    /// <para>OffsetDelta/2 is the difference between this position's startOffset from the
    /// previous occurrence (or zero, if this is the first occurrence in this document).
    /// If OffsetDelta is odd, then the length (endOffset-startOffset) differs from the
    /// previous occurrence and an OffsetLength follows. Offset data is only written for
    /// <see cref="Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS"/>.</para>
    /// </summary>
    // TODO: this class could be created by wrapping
    // BlockTreeTermsDict around Lucene40PostingsBaseFormat; ie
    // we should not duplicate the code from that class here:
    [Obsolete("Only for reading old 4.0 segments")]
    [PostingsFormatName("Lucene40")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public class Lucene40PostingsFormat : PostingsFormat
    {
        /// <summary>
        /// Minimum items (terms or sub-blocks) per block for BlockTree. </summary>
        protected readonly int m_minBlockSize;

        /// <summary>
        /// Maximum items (terms or sub-blocks) per block for BlockTree. </summary>
        protected readonly int m_maxBlockSize;

        /// <summary>
        /// Creates <see cref="Lucene40PostingsFormat"/> with default
        /// settings.
        /// </summary>
        public Lucene40PostingsFormat()
            : this(BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
        {
        }

        /// <summary>
        /// Creates <see cref="Lucene40PostingsFormat"/> with custom
        /// values for <paramref name="minBlockSize"/> and 
        /// <paramref name="maxBlockSize"/> passed to block terms dictionary. </summary>
        ///  <seealso cref="BlockTreeTermsWriter{TSubclassState}.BlockTreeTermsWriter(SegmentWriteState,PostingsWriterBase,int,int,TSubclassState)"/>
        private Lucene40PostingsFormat(int minBlockSize, int maxBlockSize)
            : base()
        {
            this.m_minBlockSize = minBlockSize;
            if (Debugging.AssertsEnabled) Debugging.Assert(minBlockSize > 1);
            this.m_maxBlockSize = maxBlockSize;
        }

        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw UnsupportedOperationException.Create("this codec can only be used for reading");
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            PostingsReaderBase postings = new Lucene40PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);

            bool success = false;
            try
            {
                FieldsProducer ret = new BlockTreeTermsReader<object>(state.Directory, state.FieldInfos, state.SegmentInfo, postings, state.Context, state.SegmentSuffix, state.TermsIndexDivisor, subclassState: null);
                success = true;
                return ret;
            }
            finally
            {
                if (!success)
                {
                    postings.Dispose();
                }
            }
        }

        /// <summary>
        /// Extension of freq postings file. </summary>
        internal const string FREQ_EXTENSION = "frq";

        /// <summary>
        /// Extension of prox postings file. </summary>
        internal const string PROX_EXTENSION = "prx";

        public override string ToString()
        {
            return Name + "(minBlockSize=" + m_minBlockSize + " maxBlockSize=" + m_maxBlockSize + ")";
        }
    }
}