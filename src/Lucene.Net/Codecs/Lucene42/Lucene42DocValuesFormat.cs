using System;
using System.Runtime.CompilerServices;

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

    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Lucene 4.2 DocValues format.
    /// <para/>
    /// Encodes the four per-document value types (Numeric,Binary,Sorted,SortedSet) with seven basic strategies.
    /// <para/>
    /// <list type="bullet">
    ///    <item><description>Delta-compressed Numerics: per-document integers written in blocks of 4096. For each block
    ///        the minimum value is encoded, and each entry is a delta from that minimum value.</description></item>
    ///    <item><description>Table-compressed Numerics: when the number of unique values is very small, a lookup table
    ///        is written instead. Each per-document entry is instead the ordinal to this table.</description></item>
    ///    <item><description>Uncompressed Numerics: when all values would fit into a single byte, and the
    ///        <c>acceptableOverheadRatio</c> would pack values into 8 bits per value anyway, they
    ///        are written as absolute values (with no indirection or packing) for performance.</description></item>
    ///    <item><description>GCD-compressed Numerics: when all numbers share a common divisor, such as dates, the greatest
    ///        common denominator (GCD) is computed, and quotients are stored using Delta-compressed Numerics.</description></item>
    ///    <item><description>Fixed-width Binary: one large concatenated byte[] is written, along with the fixed length.
    ///        Each document's value can be addressed by <c>maxDoc*length</c>.</description></item>
    ///    <item><description>Variable-width Binary: one large concatenated byte[] is written, along with end addresses
    ///        for each document. The addresses are written in blocks of 4096, with the current absolute
    ///        start for the block, and the average (expected) delta per entry. For each document the
    ///        deviation from the delta (actual - expected) is written.</description></item>
    ///    <item><description>Sorted: an FST mapping deduplicated terms to ordinals is written, along with the per-document
    ///        ordinals written using one of the numeric strategies above.</description></item>
    ///    <item><description>SortedSet: an FST mapping deduplicated terms to ordinals is written, along with the per-document
    ///        ordinal list written using one of the binary strategies above.</description></item>
    /// </list>
    /// <para/>
    /// Files:
    /// <list type="number">
    ///   <item><description><c>.dvd</c>: DocValues data</description></item>
    ///   <item><description><c>.dvm</c>: DocValues metadata</description></item>
    /// </list>
    /// <list type="number">
    ///   <item><description><a name="dvm" id="dvm"></a>
    ///   <para>The DocValues metadata or .dvm file.</para>
    ///   <para>For DocValues field, this stores metadata, such as the offset into the
    ///      DocValues data (.dvd)</para>
    ///   <para>DocValues metadata (.dvm) --&gt; Header,&lt;FieldNumber,EntryType,Entry&gt;<sup>NumFields</sup>,Footer</para>
    ///   <list type="bullet">
    ///     <item><description>Entry --&gt; NumericEntry | BinaryEntry | SortedEntry</description></item>
    ///     <item><description>NumericEntry --&gt; DataOffset,CompressionType,PackedVersion</description></item>
    ///     <item><description>BinaryEntry --&gt; DataOffset,DataLength,MinLength,MaxLength,PackedVersion?,BlockSize?</description></item>
    ///     <item><description>SortedEntry --&gt; DataOffset,ValueCount</description></item>
    ///     <item><description>FieldNumber,PackedVersion,MinLength,MaxLength,BlockSize,ValueCount --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///     <item><description>DataOffset,DataLength --&gt; Int64  (<see cref="Store.DataOutput.WriteInt64(long)"/>) </description></item>
    ///     <item><description>EntryType,CompressionType --&gt; Byte  (<see cref="Store.DataOutput.WriteByte(byte)"/>) </description></item>
    ///     <item><description>Header --&gt; CodecHeader  (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///     <item><description>Footer --&gt; CodecFooter  (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    ///   </list>
    ///   <para>Sorted fields have two entries: a SortedEntry with the FST metadata,
    ///      and an ordinary NumericEntry for the document-to-ord metadata.</para>
    ///   <para>SortedSet fields have two entries: a SortedEntry with the FST metadata,
    ///      and an ordinary BinaryEntry for the document-to-ord-list metadata.</para>
    ///   <para>FieldNumber of -1 indicates the end of metadata.</para>
    ///   <para>EntryType is a 0 (NumericEntry), 1 (BinaryEntry, or 2 (SortedEntry)</para>
    ///   <para>DataOffset is the pointer to the start of the data in the DocValues data (.dvd)</para>
    ///   <para/>CompressionType indicates how Numeric values will be compressed:
    ///      <list type="bullet">
    ///         <item><description>0 --&gt; delta-compressed. For each block of 4096 integers, every integer is delta-encoded
    ///             from the minimum value within the block.</description></item>
    ///         <item><description>1 --&gt; table-compressed. When the number of unique numeric values is small and it would save space,
    ///             a lookup table of unique values is written, followed by the ordinal for each document.</description></item>
    ///         <item><description>2 --&gt; uncompressed. When the <c>acceptableOverheadRatio</c> parameter would upgrade the number
    ///             of bits required to 8, and all values fit in a byte, these are written as absolute binary values
    ///             for performance.</description></item>
    ///         <item><description>3 --&gt; gcd-compressed. When all integers share a common divisor, only quotients are stored
    ///             using blocks of delta-encoded ints.</description></item>
    ///      </list>
    ///   <para/>MinLength and MaxLength represent the min and max byte[] value lengths for Binary values.
    ///      If they are equal, then all values are of a fixed size, and can be addressed as <c>DataOffset + (docID * length)</c>.
    ///      Otherwise, the binary values are of variable size, and packed integer metadata (PackedVersion,BlockSize)
    ///      is written for the addresses.</description></item>
    ///   <item><description><a name="dvd" id="dvd"></a>
    ///   <para>The DocValues data or .dvd file.</para>
    ///   <para>For DocValues field, this stores the actual per-document data (the heavy-lifting)</para>
    ///   <para>DocValues data (.dvd) --&gt; Header,&lt;NumericData | BinaryData | SortedData&gt;<sup>NumFields</sup>,Footer</para>
    ///   <list type="bullet">
    ///     <item><description>NumericData --&gt; DeltaCompressedNumerics | TableCompressedNumerics | UncompressedNumerics | GCDCompressedNumerics</description></item>
    ///     <item><description>BinaryData --&gt; Byte  (<see cref="Store.DataOutput.WriteByte(byte)"/>) <sup>DataLength</sup>,Addresses</description></item>
    ///     <item><description>SortedData --&gt; FST&lt;Int64&gt; (<see cref="Util.Fst.FST{T}"/>) </description></item>
    ///     <item><description>DeltaCompressedNumerics --&gt; BlockPackedInts(blockSize=4096) (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///     <item><description>TableCompressedNumerics --&gt; TableSize, Int64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) <sup>TableSize</sup>, PackedInts (<see cref="PackedInt32s"/>) </description></item>
    ///     <item><description>UncompressedNumerics --&gt; Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) <sup>maxdoc</sup></description></item>
    ///     <item><description>Addresses --&gt; MonotonicBlockPackedInts(blockSize=4096) (<see cref="Util.Packed.MonotonicBlockPackedWriter"/>) </description></item>
    ///     <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/></description></item>
    ///   </list>
    ///   <para>SortedSet entries store the list of ordinals in their BinaryData as a
    ///      sequences of increasing vLongs (<see cref="Store.DataOutput.WriteVInt64(long)"/>), delta-encoded.</para></description></item>
    /// </list>
    /// <para/>
    /// Limitations:
    /// <list type="bullet">
    ///   <item><description> Binary doc values can be at most <see cref="MAX_BINARY_FIELD_LENGTH"/> in length.</description></item>
    /// </list> 
    /// </summary>
    [Obsolete("Only for reading old 4.2 segments")]
    [DocValuesFormatName("Lucene42")] // LUCENENET specific - using DocValuesFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public class Lucene42DocValuesFormat : DocValuesFormat
    {
        /// <summary>
        /// Maximum length for each binary doc values field. </summary>
        public static readonly int MAX_BINARY_FIELD_LENGTH = (1 << 15) - 2;

        protected readonly float m_acceptableOverheadRatio;

        /// <summary>
        /// Calls <c>Lucene42DocValuesFormat(PackedInts.DEFAULT)</c> (<see cref="Lucene42DocValuesFormat(float)"/>.
        /// </summary>
        public Lucene42DocValuesFormat()
            : this(PackedInt32s.DEFAULT)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Lucene42DocValuesFormat"/> with the specified
        /// <paramref name="acceptableOverheadRatio"/> for <see cref="Index.NumericDocValues"/>. 
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <param name="acceptableOverheadRatio"> Compression parameter for numerics.
        ///        Currently this is only used when the number of unique values is small.</param>
        public Lucene42DocValuesFormat(float acceptableOverheadRatio)
            : base()
        {
            this.m_acceptableOverheadRatio = acceptableOverheadRatio;
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw UnsupportedOperationException.Create("this codec can only be used for reading");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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