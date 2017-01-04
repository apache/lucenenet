using System;

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

    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Lucene 4.2 DocValues format.
    /// <p>
    /// Encodes the four per-document value types (Numeric,Binary,Sorted,SortedSet) with seven basic strategies.
    /// <p>
    /// <ul>
    ///    <li>Delta-compressed Numerics: per-document integers written in blocks of 4096. For each block
    ///        the minimum value is encoded, and each entry is a delta from that minimum value.
    ///    <li>Table-compressed Numerics: when the number of unique values is very small, a lookup table
    ///        is written instead. Each per-document entry is instead the ordinal to this table.
    ///    <li>Uncompressed Numerics: when all values would fit into a single byte, and the
    ///        <code>acceptableOverheadRatio</code> would pack values into 8 bits per value anyway, they
    ///        are written as absolute values (with no indirection or packing) for performance.
    ///    <li>GCD-compressed Numerics: when all numbers share a common divisor, such as dates, the greatest
    ///        common denominator (GCD) is computed, and quotients are stored using Delta-compressed Numerics.
    ///    <li>Fixed-width Binary: one large concatenated byte[] is written, along with the fixed length.
    ///        Each document's value can be addressed by maxDoc*length.
    ///    <li>Variable-width Binary: one large concatenated byte[] is written, along with end addresses
    ///        for each document. The addresses are written in blocks of 4096, with the current absolute
    ///        start for the block, and the average (expected) delta per entry. For each document the
    ///        deviation from the delta (actual - expected) is written.
    ///    <li>Sorted: an FST mapping deduplicated terms to ordinals is written, along with the per-document
    ///        ordinals written using one of the numeric strategies above.
    ///    <li>SortedSet: an FST mapping deduplicated terms to ordinals is written, along with the per-document
    ///        ordinal list written using one of the binary strategies above.
    /// </ul>
    /// <p>
    /// Files:
    /// <ol>
    ///   <li><tt>.dvd</tt>: DocValues data</li>
    ///   <li><tt>.dvm</tt>: DocValues metadata</li>
    /// </ol>
    /// <ol>
    ///   <li><a name="dvm" id="dvm"></a>
    ///   <p>The DocValues metadata or .dvm file.</p>
    ///   <p>For DocValues field, this stores metadata, such as the offset into the
    ///      DocValues data (.dvd)</p>
    ///   <p>DocValues metadata (.dvm) --&gt; Header,&lt;FieldNumber,EntryType,Entry&gt;<sup>NumFields</sup>,Footer</p>
    ///   <ul>
    ///     <li>Entry --&gt; NumericEntry | BinaryEntry | SortedEntry</li>
    ///     <li>NumericEntry --&gt; DataOffset,CompressionType,PackedVersion</li>
    ///     <li>BinaryEntry --&gt; DataOffset,DataLength,MinLength,MaxLength,PackedVersion?,BlockSize?</li>
    ///     <li>SortedEntry --&gt; DataOffset,ValueCount</li>
    ///     <li>FieldNumber,PackedVersion,MinLength,MaxLength,BlockSize,ValueCount --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///     <li>DataOffset,DataLength --&gt; <seealso cref="DataOutput#writeLong Int64"/></li>
    ///     <li>EntryType,CompressionType --&gt; <seealso cref="DataOutput#writeByte Byte"/></li>
    ///     <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///     <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    ///   </ul>
    ///   <p>Sorted fields have two entries: a SortedEntry with the FST metadata,
    ///      and an ordinary NumericEntry for the document-to-ord metadata.</p>
    ///   <p>SortedSet fields have two entries: a SortedEntry with the FST metadata,
    ///      and an ordinary BinaryEntry for the document-to-ord-list metadata.</p>
    ///   <p>FieldNumber of -1 indicates the end of metadata.</p>
    ///   <p>EntryType is a 0 (NumericEntry), 1 (BinaryEntry, or 2 (SortedEntry)</p>
    ///   <p>DataOffset is the pointer to the start of the data in the DocValues data (.dvd)</p>
    ///   <p>CompressionType indicates how Numeric values will be compressed:
    ///      <ul>
    ///         <li>0 --&gt; delta-compressed. For each block of 4096 integers, every integer is delta-encoded
    ///             from the minimum value within the block.
    ///         <li>1 --&gt; table-compressed. When the number of unique numeric values is small and it would save space,
    ///             a lookup table of unique values is written, followed by the ordinal for each document.
    ///         <li>2 --&gt; uncompressed. When the <code>acceptableOverheadRatio</code> parameter would upgrade the number
    ///             of bits required to 8, and all values fit in a byte, these are written as absolute binary values
    ///             for performance.
    ///         <li>3 --&gt, gcd-compressed. When all integers share a common divisor, only quotients are stored
    ///             using blocks of delta-encoded ints.
    ///      </ul>
    ///   <p>MinLength and MaxLength represent the min and max byte[] value lengths for Binary values.
    ///      If they are equal, then all values are of a fixed size, and can be addressed as DataOffset + (docID * length).
    ///      Otherwise, the binary values are of variable size, and packed integer metadata (PackedVersion,BlockSize)
    ///      is written for the addresses.
    ///   <li><a name="dvd" id="dvd"></a>
    ///   <p>The DocValues data or .dvd file.</p>
    ///   <p>For DocValues field, this stores the actual per-document data (the heavy-lifting)</p>
    ///   <p>DocValues data (.dvd) --&gt; Header,&lt;NumericData | BinaryData | SortedData&gt;<sup>NumFields</sup>,Footer</p>
    ///   <ul>
    ///     <li>NumericData --&gt; DeltaCompressedNumerics | TableCompressedNumerics | UncompressedNumerics | GCDCompressedNumerics</li>
    ///     <li>BinaryData --&gt;  <seealso cref="DataOutput#writeByte Byte"/><sup>DataLength</sup>,Addresses</li>
    ///     <li>SortedData --&gt; <seealso cref="FST FST&lt;Int64&gt;"/></li>
    ///     <li>DeltaCompressedNumerics --&gt; <seealso cref="BlockPackedWriter BlockPackedInts(blockSize=4096)"/></li>
    ///     <li>TableCompressedNumerics --&gt; TableSize,<seealso cref="DataOutput#writeLong Int64"/><sup>TableSize</sup>,<seealso cref="PackedInts PackedInts"/></li>
    ///     <li>UncompressedNumerics --&gt; <seealso cref="DataOutput#writeByte Byte"/><sup>maxdoc</sup></li>
    ///     <li>Addresses --&gt; <seealso cref="MonotonicBlockPackedWriter MonotonicBlockPackedInts(blockSize=4096)"/></li>
    ///     <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    ///   </ul>
    ///   <p>SortedSet entries store the list of ordinals in their BinaryData as a
    ///      sequences of increasing <seealso cref="DataOutput#writeVLong vLong"/>s, delta-encoded.</p>
    /// </ol>
    /// <p>
    /// Limitations:
    /// <ul>
    ///   <li> Binary doc values can be at most <seealso cref="#MAX_BINARY_FIELD_LENGTH"/> in length.
    /// </ul> </summary>
    /// @deprecated Only for reading old 4.2 segments
    [Obsolete("Only for reading old 4.2 segments")]
    public class Lucene42DocValuesFormat : DocValuesFormat
    {
        /// <summary>
        /// Maximum length for each binary doc values field. </summary>
        public static readonly int MAX_BINARY_FIELD_LENGTH = (1 << 15) - 2;

        protected readonly float m_acceptableOverheadRatio;

        /// <summary>
        /// Calls {@link #Lucene42DocValuesFormat(float)
        /// Lucene42DocValuesFormat(PackedInts.DEFAULT)}
        /// </summary>
        public Lucene42DocValuesFormat()
            : this(PackedInts.DEFAULT)
        {
        }

        /// <summary>
        /// Creates a new Lucene42DocValuesFormat with the specified
        /// <code>acceptableOverheadRatio</code> for NumericDocValues. </summary>
        /// <param name="acceptableOverheadRatio"> compression parameter for numerics.
        ///        Currently this is only used when the number of unique values is small.
        ///
        /// @lucene.experimental </param>
        public Lucene42DocValuesFormat(float acceptableOverheadRatio)
            : base("Lucene42")
        {
            this.m_acceptableOverheadRatio = acceptableOverheadRatio;
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new System.NotSupportedException("this codec can only be used for reading");
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new Lucene42DocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION);
        }

        internal const string DATA_CODEC = "Lucene42DocValuesData";
        internal const string DATA_EXTENSION = "dvd";
        internal const string METADATA_CODEC = "Lucene42DocValuesMetadata";
        internal const string METADATA_EXTENSION = "dvm";
    }
}