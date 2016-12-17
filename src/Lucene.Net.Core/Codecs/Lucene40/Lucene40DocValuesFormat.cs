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

    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Lucene 4.0 DocValues format.
    /// <p>
    /// Files:
    /// <ul>
    ///   <li><tt>.dv.cfs</tt>: <seealso cref="CompoundFileDirectory compound container"/></li>
    ///   <li><tt>.dv.cfe</tt>: <seealso cref="CompoundFileDirectory compound entries"/></li>
    /// </ul>
    /// Entries within the compound file:
    /// <ul>
    ///   <li><tt>&lt;segment&gt;_&lt;fieldNumber&gt;.dat</tt>: data values</li>
    ///   <li><tt>&lt;segment&gt;_&lt;fieldNumber&gt;.idx</tt>: index into the .dat for DEREF types</li>
    /// </ul>
    /// <p>
    /// There are several many types of {@code DocValues} with different encodings.
    /// From the perspective of filenames, all types store their values in <tt>.dat</tt>
    /// entries within the compound file. In the case of dereferenced/sorted types, the <tt>.dat</tt>
    /// actually contains only the unique values, and an additional <tt>.idx</tt> file contains
    /// pointers to these unique values.
    /// </p>
    /// Formats:
    /// <ul>
    ///    <li>{@code VAR_INTS} .dat --&gt; Header, PackedType, MinValue,
    ///        DefaultValue, PackedStream</li>
    ///    <li>{@code FIXED_INTS_8} .dat --&gt; Header, ValueSize,
    ///        <seealso cref="DataOutput#writeByte Byte"/><sup>maxdoc</sup></li>
    ///    <li>{@code FIXED_INTS_16} .dat --&gt; Header, ValueSize,
    ///        <seealso cref="DataOutput#writeShort Short"/><sup>maxdoc</sup></li>
    ///    <li>{@code FIXED_INTS_32} .dat --&gt; Header, ValueSize,
    ///        <seealso cref="DataOutput#writeInt Int32"/><sup>maxdoc</sup></li>
    ///    <li>{@code FIXED_INTS_64} .dat --&gt; Header, ValueSize,
    ///        <seealso cref="DataOutput#writeLong Int64"/><sup>maxdoc</sup></li>
    ///    <li>{@code FLOAT_32} .dat --&gt; Header, ValueSize, Float32<sup>maxdoc</sup></li>
    ///    <li>{@code FLOAT_64} .dat --&gt; Header, ValueSize, Float64<sup>maxdoc</sup></li>
    ///    <li>{@code BYTES_FIXED_STRAIGHT} .dat --&gt; Header, ValueSize,
    ///        (<seealso cref="DataOutput#writeByte Byte"/> * ValueSize)<sup>maxdoc</sup></li>
    ///    <li>{@code BYTES_VAR_STRAIGHT} .idx --&gt; Header, TotalBytes, Addresses</li>
    ///    <li>{@code BYTES_VAR_STRAIGHT} .dat --&gt; Header,
    ///          (<seealso cref="DataOutput#writeByte Byte"/> * <i>variable ValueSize</i>)<sup>maxdoc</sup></li>
    ///    <li>{@code BYTES_FIXED_DEREF} .idx --&gt; Header, NumValues, Addresses</li>
    ///    <li>{@code BYTES_FIXED_DEREF} .dat --&gt; Header, ValueSize,
    ///        (<seealso cref="DataOutput#writeByte Byte"/> * ValueSize)<sup>NumValues</sup></li>
    ///    <li>{@code BYTES_VAR_DEREF} .idx --&gt; Header, TotalVarBytes, Addresses</li>
    ///    <li>{@code BYTES_VAR_DEREF} .dat --&gt; Header,
    ///        (LengthPrefix + <seealso cref="DataOutput#writeByte Byte"/> * <i>variable ValueSize</i>)<sup>NumValues</sup></li>
    ///    <li>{@code BYTES_FIXED_SORTED} .idx --&gt; Header, NumValues, Ordinals</li>
    ///    <li>{@code BYTES_FIXED_SORTED} .dat --&gt; Header, ValueSize,
    ///        (<seealso cref="DataOutput#writeByte Byte"/> * ValueSize)<sup>NumValues</sup></li>
    ///    <li>{@code BYTES_VAR_SORTED} .idx --&gt; Header, TotalVarBytes, Addresses, Ordinals</li>
    ///    <li>{@code BYTES_VAR_SORTED} .dat --&gt; Header,
    ///        (<seealso cref="DataOutput#writeByte Byte"/> * <i>variable ValueSize</i>)<sup>NumValues</sup></li>
    /// </ul>
    /// Data Types:
    /// <ul>
    ///    <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///    <li>PackedType --&gt; <seealso cref="DataOutput#writeByte Byte"/></li>
    ///    <li>MaxAddress, MinValue, DefaultValue --&gt; <seealso cref="DataOutput#writeLong Int64"/></li>
    ///    <li>PackedStream, Addresses, Ordinals --&gt; <seealso cref="PackedInts"/></li>
    ///    <li>ValueSize, NumValues --&gt; <seealso cref="DataOutput#writeInt Int32"/></li>
    ///    <li>Float32 --&gt; 32-bit float encoded with <seealso cref="Float#floatToRawIntBits(float)"/>
    ///                       then written as <seealso cref="DataOutput#writeInt Int32"/></li>
    ///    <li>Float64 --&gt; 64-bit float encoded with <seealso cref="Double#doubleToRawLongBits(double)"/>
    ///                       then written as <seealso cref="DataOutput#writeLong Int64"/></li>
    ///    <li>TotalBytes --&gt; <seealso cref="DataOutput#writeVLong VLong"/></li>
    ///    <li>TotalVarBytes --&gt; <seealso cref="DataOutput#writeLong Int64"/></li>
    ///    <li>LengthPrefix --&gt; Length of the data value as <seealso cref="DataOutput#writeVInt VInt"/> (maximum
    ///                       of 2 bytes)</li>
    /// </ul>
    /// Notes:
    /// <ul>
    ///    <li>PackedType is a 0 when compressed, 1 when the stream is written as 64-bit integers.</li>
    ///    <li>Addresses stores pointers to the actual byte location (indexed by docid). In the VAR_STRAIGHT
    ///        case, each entry can have a different length, so to determine the length, docid+1 is
    ///        retrieved. A sentinel address is written at the end for the VAR_STRAIGHT case, so the Addresses
    ///        stream contains maxdoc+1 indices. For the deduplicated VAR_DEREF case, each length
    ///        is encoded as a prefix to the data itself as a <seealso cref="DataOutput#writeVInt VInt"/>
    ///        (maximum of 2 bytes).</li>
    ///    <li>Ordinals stores the term ID in sorted order (indexed by docid). In the FIXED_SORTED case,
    ///        the address into the .dat can be computed from the ordinal as
    ///        <code>Header+ValueSize+(ordinal*ValueSize)</code> because the byte length is fixed.
    ///        In the VAR_SORTED case, there is double indirection (docid -> ordinal -> address), but
    ///        an additional sentinel ordinal+address is always written (so there are NumValues+1 ordinals). To
    ///        determine the length, ord+1's address is looked up as well.</li>
    ///    <li>{@code BYTES_VAR_STRAIGHT BYTES_VAR_STRAIGHT} in contrast to other straight
    ///        variants uses a <tt>.idx</tt> file to improve lookup perfromance. In contrast to
    ///        {@code BYTES_VAR_DEREF BYTES_VAR_DEREF} it doesn't apply deduplication of the document values.
    ///    </li>
    /// </ul>
    /// <p>
    /// Limitations:
    /// <ul>
    ///   <li> Binary doc values can be at most <seealso cref="#MAX_BINARY_FIELD_LENGTH"/> in length.
    /// </ul> </summary>
    /// @deprecated Only for reading old 4.0 and 4.1 segments
    [Obsolete("Only for reading old 4.0 and 4.1 segments")]
    public class Lucene40DocValuesFormat : DocValuesFormat
    // NOTE: not registered in SPI, doesnt respect segment suffix, etc
    // for back compat only!
    {
        /// <summary>
        /// Maximum length for each binary doc values field. </summary>
        public static readonly int MAX_BINARY_FIELD_LENGTH = (1 << 15) - 2;

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40DocValuesFormat()
            : base("Lucene40")
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new System.NotSupportedException("this codec can only be used for reading");
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            string filename = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, "dv", IndexFileNames.COMPOUND_FILE_EXTENSION);
            return new Lucene40DocValuesReader(state, filename, Lucene40FieldInfosReader.LEGACY_DV_TYPE_KEY);
        }

        // constants for VAR_INTS
        internal const string VAR_INTS_CODEC_NAME = "PackedInts";

        internal const int VAR_INTS_VERSION_START = 0;
        internal const int VAR_INTS_VERSION_CURRENT = VAR_INTS_VERSION_START;
        internal const sbyte VAR_INTS_PACKED = 0x00;
        internal const sbyte VAR_INTS_FIXED_64 = 0x01;

        // constants for FIXED_INTS_8, FIXED_INTS_16, FIXED_INTS_32, FIXED_INTS_64
        internal const string INTS_CODEC_NAME = "Ints";

        internal const int INTS_VERSION_START = 0;
        internal const int INTS_VERSION_CURRENT = INTS_VERSION_START;

        // constants for FLOAT_32, FLOAT_64
        internal const string FLOATS_CODEC_NAME = "Floats";

        internal const int FLOATS_VERSION_START = 0;
        internal const int FLOATS_VERSION_CURRENT = FLOATS_VERSION_START;

        // constants for BYTES_FIXED_STRAIGHT
        internal const string BYTES_FIXED_STRAIGHT_CODEC_NAME = "FixedStraightBytes";

        internal const int BYTES_FIXED_STRAIGHT_VERSION_START = 0;
        internal const int BYTES_FIXED_STRAIGHT_VERSION_CURRENT = BYTES_FIXED_STRAIGHT_VERSION_START;

        // constants for BYTES_VAR_STRAIGHT
        internal const string BYTES_VAR_STRAIGHT_CODEC_NAME_IDX = "VarStraightBytesIdx";

        internal const string BYTES_VAR_STRAIGHT_CODEC_NAME_DAT = "VarStraightBytesDat";
        internal const int BYTES_VAR_STRAIGHT_VERSION_START = 0;
        internal const int BYTES_VAR_STRAIGHT_VERSION_CURRENT = BYTES_VAR_STRAIGHT_VERSION_START;

        // constants for BYTES_FIXED_DEREF
        internal const string BYTES_FIXED_DEREF_CODEC_NAME_IDX = "FixedDerefBytesIdx";

        internal const string BYTES_FIXED_DEREF_CODEC_NAME_DAT = "FixedDerefBytesDat";
        internal const int BYTES_FIXED_DEREF_VERSION_START = 0;
        internal const int BYTES_FIXED_DEREF_VERSION_CURRENT = BYTES_FIXED_DEREF_VERSION_START;

        // constants for BYTES_VAR_DEREF
        internal const string BYTES_VAR_DEREF_CODEC_NAME_IDX = "VarDerefBytesIdx";

        internal const string BYTES_VAR_DEREF_CODEC_NAME_DAT = "VarDerefBytesDat";
        internal const int BYTES_VAR_DEREF_VERSION_START = 0;
        internal const int BYTES_VAR_DEREF_VERSION_CURRENT = BYTES_VAR_DEREF_VERSION_START;

        // constants for BYTES_FIXED_SORTED
        internal const string BYTES_FIXED_SORTED_CODEC_NAME_IDX = "FixedSortedBytesIdx";

        internal const string BYTES_FIXED_SORTED_CODEC_NAME_DAT = "FixedSortedBytesDat";
        internal const int BYTES_FIXED_SORTED_VERSION_START = 0;
        internal const int BYTES_FIXED_SORTED_VERSION_CURRENT = BYTES_FIXED_SORTED_VERSION_START;

        // constants for BYTES_VAR_SORTED
        // NOTE this IS NOT A BUG! 4.0 actually screwed this up (VAR_SORTED and VAR_DEREF have same codec header)
        internal const string BYTES_VAR_SORTED_CODEC_NAME_IDX = "VarDerefBytesIdx";

        internal const string BYTES_VAR_SORTED_CODEC_NAME_DAT = "VarDerefBytesDat";
        internal const int BYTES_VAR_SORTED_VERSION_START = 0;
        internal const int BYTES_VAR_SORTED_VERSION_CURRENT = BYTES_VAR_SORTED_VERSION_START;
    }
}