namespace Lucene.Net.Codecs.Lucene42
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

    using CompressingTermVectorsFormat = Lucene.Net.Codecs.Compressing.CompressingTermVectorsFormat;
    using CompressionMode = Lucene.Net.Codecs.Compressing.CompressionMode;

    /// <summary>
    /// Lucene 4.2 term vectors format (<see cref="TermVectorsFormat"/>).
    /// <para/>
    /// Very similarly to <see cref="Lucene41.Lucene41StoredFieldsFormat"/>, this format is based
    /// on compressed chunks of data, with document-level granularity so that a
    /// document can never span across distinct chunks. Moreover, data is made as
    /// compact as possible:
    /// <list type="bullet">
    ///     <item><description>textual data is compressed using the very light,
    ///         <a href="http://code.google.com/p/lz4/">LZ4</a> compression algorithm,</description></item>
    ///     <item><description>binary data is written using fixed-size blocks of
    ///         packed <see cref="int"/>s (<see cref="Util.Packed.PackedInt32s"/>).</description></item>
    /// </list>
    /// <para/>
    /// Term vectors are stored using two files
    /// <list type="bullet">
    ///     <item><description>a data file where terms, frequencies, positions, offsets and payloads
    ///         are stored,</description></item>
    ///     <item><description>an index file, loaded into memory, used to locate specific documents in
    ///         the data file.</description></item>
    /// </list>
    /// Looking up term vectors for any document requires at most 1 disk seek.
    /// <para/><b>File formats</b>
    /// <list type="number">
    ///     <item><description><a name="vector_data" id="vector_data"></a>
    ///         <para>A vector data file (extension <c>.tvd</c>). this file stores terms,
    ///         frequencies, positions, offsets and payloads for every document. Upon writing
    ///         a new segment, it accumulates data into memory until the buffer used to store
    ///         terms and payloads grows beyond 4KB. Then it flushes all metadata, terms
    ///         and positions to disk using <a href="http://code.google.com/p/lz4/">LZ4</a>
    ///         compression for terms and payloads and
    ///         blocks of packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) for positions.</para>
    ///         <para>Here is a more detailed description of the field data file format:</para>
    ///         <list type="bullet">
    ///             <item><description>VectorData (.tvd) --&gt; &lt;Header&gt;, PackedIntsVersion, ChunkSize, &lt;Chunk&gt;<sup>ChunkCount</sup>, Footer</description></item>
    ///             <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///             <item><description>PackedIntsVersion --&gt; <see cref="Util.Packed.PackedInt32s.VERSION_CURRENT"/> as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///             <item><description>ChunkSize is the number of bytes of terms to accumulate before flushing, as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///             <item><description>ChunkCount is not known in advance and is the number of chunks necessary to store all document of the segment</description></item>
    ///             <item><description>Chunk --&gt; DocBase, ChunkDocs, &lt; NumFields &gt;, &lt; FieldNums &gt;, &lt; FieldNumOffs &gt;, &lt; Flags &gt;,
    ///                 &lt; NumTerms &gt;, &lt; TermLengths &gt;, &lt; TermFreqs &gt;, &lt; Positions &gt;, &lt; StartOffsets &gt;, &lt; Lengths &gt;,
    ///                 &lt; PayloadLengths &gt;, &lt; TermAndPayloads &gt;</description></item>
    ///             <item><description>DocBase is the ID of the first doc of the chunk as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///             <item><description>ChunkDocs is the number of documents in the chunk</description></item>
    ///             <item><description>NumFields --&gt; DocNumFields<sup>ChunkDocs</sup></description></item>
    ///             <item><description>DocNumFields is the number of fields for each doc, written as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) if ChunkDocs==1 and as a <see cref="Util.Packed.PackedInt32s"/> array otherwise</description></item>
    ///             <item><description>FieldNums --&gt; FieldNumDelta<sup>TotalDistincFields</sup>, a delta-encoded list of the sorted unique field numbers present in the chunk</description></item>
    ///             <item><description>FieldNumOffs --&gt; FieldNumOff<sup>TotalFields</sup>, as a <see cref="Util.Packed.PackedInt32s"/> array</description></item>
    ///             <item><description>FieldNumOff is the offset of the field number in FieldNums</description></item>
    ///             <item><description>TotalFields is the total number of fields (sum of the values of NumFields)</description></item>
    ///             <item><description>Flags --&gt; Bit &lt; FieldFlags &gt;</description></item>
    ///             <item><description>Bit  is a single bit which when true means that fields have the same options for every document in the chunk</description></item>
    ///             <item><description>FieldFlags --&gt; if Bit==1: Flag<sup>TotalDistinctFields</sup> else Flag<sup>TotalFields</sup></description></item>
    ///             <item><description>Flag: a 3-bits int where:
    ///                 <list type="bullet">
    ///                     <item><description>the first bit means that the field has positions</description></item>
    ///                     <item><description>the second bit means that the field has offsets</description></item>
    ///                     <item><description>the third bit means that the field has payloads</description></item>
    ///                 </list>
    ///             </description></item>
    ///             <item><description>NumTerms --&gt; FieldNumTerms<sup>TotalFields</sup></description></item>
    ///             <item><description>FieldNumTerms: the number of terms for each field, using blocks of 64 packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///             <item><description>TermLengths --&gt; PrefixLength<sup>TotalTerms</sup> SuffixLength<sup>TotalTerms</sup></description></item>
    ///             <item><description>TotalTerms: total number of terms (sum of NumTerms)</description></item>
    ///             <item><description>PrefixLength: 0 for the first term of a field, the common prefix with the previous term otherwise using blocks of 64 packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///             <item><description>SuffixLength: length of the term minus PrefixLength for every term using blocks of 64 packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///             <item><description>TermFreqs --&gt; TermFreqMinus1<sup>TotalTerms</sup></description></item>
    ///             <item><description>TermFreqMinus1: (frequency - 1) for each term using blocks of 64 packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///             <item><description>Positions --&gt; PositionDelta<sup>TotalPositions</sup></description></item>
    ///             <item><description>TotalPositions is the sum of frequencies of terms of all fields that have positions</description></item>
    ///             <item><description>PositionDelta: the absolute position for the first position of a term, and the difference with the previous positions for following positions using blocks of 64 packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///             <item><description>StartOffsets --&gt; (AvgCharsPerTerm<sup>TotalDistinctFields</sup>) StartOffsetDelta<sup>TotalOffsets</sup></description></item>
    ///             <item><description>TotalOffsets is the sum of frequencies of terms of all fields that have offsets</description></item>
    ///             <item><description>AvgCharsPerTerm: average number of chars per term, encoded as a float on 4 bytes. They are not present if no field has both positions and offsets enabled.</description></item>
    ///             <item><description>StartOffsetDelta: (startOffset - previousStartOffset - AvgCharsPerTerm * PositionDelta). previousStartOffset is 0 for the first offset and AvgCharsPerTerm is 0 if the field has no positions using blocks of 64 packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///             <item><description>Lengths --&gt; LengthMinusTermLength<sup>TotalOffsets</sup></description></item>
    ///             <item><description>LengthMinusTermLength: (endOffset - startOffset - termLength) using blocks of 64 packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///             <item><description>PayloadLengths --&gt; PayloadLength<sup>TotalPayloads</sup></description></item>
    ///             <item><description>TotalPayloads is the sum of frequencies of terms of all fields that have payloads</description></item>
    ///             <item><description>PayloadLength is the payload length encoded using blocks of 64 packed <see cref="int"/>s (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///             <item><description>TermAndPayloads --&gt; LZ4-compressed representation of &lt; FieldTermsAndPayLoads &gt;<sup>TotalFields</sup></description></item>
    ///             <item><description>FieldTermsAndPayLoads --&gt; Terms (Payloads)</description></item>
    ///             <item><description>Terms: term bytes</description></item>
    ///             <item><description>Payloads: payload bytes (if the field has payloads)</description></item>
    ///             <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    ///         </list>
    ///     </description></item>
    ///     <item><description><a name="vector_index" id="vector_index"></a>
    ///         <para>An index file (extension <c>.tvx</c>).</para>
    ///         <list type="bullet">
    ///             <item><description>VectorIndex (.tvx) --&gt; &lt;Header&gt;, &lt;ChunkIndex&gt;, Footer</description></item>
    ///             <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///             <item><description>ChunkIndex: See <see cref="Compressing.CompressingStoredFieldsIndexWriter"/></description></item>
    ///             <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    ///         </list>
    ///     </description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class Lucene42TermVectorsFormat : CompressingTermVectorsFormat
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene42TermVectorsFormat()
            : base("Lucene41StoredFields", "", CompressionMode.FAST, 1 << 12)
        {
        }
    }
}