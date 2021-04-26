using System;
using System.Runtime.CompilerServices;

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
    /// <para/>
    /// Files:
    /// <list type="bullet">
    ///   <item><description><c>.dv.cfs</c>: compound container (<see cref="Store.CompoundFileDirectory"/>)</description></item>
    ///   <item><description><c>.dv.cfe</c>: compound entries (<see cref="Store.CompoundFileDirectory"/>)</description></item>
    /// </list>
    /// Entries within the compound file:
    /// <list type="bullet">
    ///   <item><description><c>&lt;segment&gt;_&lt;fieldNumber&gt;.dat</c>: data values</description></item>
    ///   <item><description><c>&lt;segment&gt;_&lt;fieldNumber&gt;.idx</c>: index into the .dat for DEREF types</description></item>
    /// </list>
    /// <para>
    /// There are several many types of <see cref="Index.DocValues"/> with different encodings.
    /// From the perspective of filenames, all types store their values in <c>.dat</c>
    /// entries within the compound file. In the case of dereferenced/sorted types, the <c>.dat</c>
    /// actually contains only the unique values, and an additional <c>.idx</c> file contains
    /// pointers to these unique values.
    /// </para>
    /// Formats:
    /// <list type="bullet">
    ///    <item><description><see cref="LegacyDocValuesType.VAR_INTS"/> .dat --&gt; Header, PackedType, MinValue,
    ///        DefaultValue, PackedStream</description></item>
    ///    <item><description><see cref="LegacyDocValuesType.FIXED_INTS_8"/> .dat --&gt; Header, ValueSize,
    ///        Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) <sup>maxdoc</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.FIXED_INTS_16"/> .dat --&gt; Header, ValueSize,
    ///        Short (<see cref="Store.DataOutput.WriteInt16(short)"/>) <sup>maxdoc</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.FIXED_INTS_32"/> .dat --&gt; Header, ValueSize,
    ///        Int32 (<see cref="Store.DataOutput.WriteInt32(int)"/>) <sup>maxdoc</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.FIXED_INTS_64"/> .dat --&gt; Header, ValueSize,
    ///        Int64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) <sup>maxdoc</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.FLOAT_32"/> .dat --&gt; Header, ValueSize, Float32<sup>maxdoc</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.FLOAT_64"/> .dat --&gt; Header, ValueSize, Float64<sup>maxdoc</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_FIXED_STRAIGHT"/> .dat --&gt; Header, ValueSize,
    ///        (Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) * ValueSize)<sup>maxdoc</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_VAR_STRAIGHT"/> .idx --&gt; Header, TotalBytes, Addresses</description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_VAR_STRAIGHT"/> .dat --&gt; Header,
    ///          (Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) * <i>variable ValueSize</i>)<sup>maxdoc</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_FIXED_DEREF"/> .idx --&gt; Header, NumValues, Addresses</description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_FIXED_DEREF"/> .dat --&gt; Header, ValueSize,
    ///        (Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) * ValueSize)<sup>NumValues</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_VAR_DEREF"/> .idx --&gt; Header, TotalVarBytes, Addresses</description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_VAR_DEREF"/> .dat --&gt; Header,
    ///        (LengthPrefix + Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) * <i>variable ValueSize</i>)<sup>NumValues</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_FIXED_SORTED"/> .idx --&gt; Header, NumValues, Ordinals</description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_FIXED_SORTED"/> .dat --&gt; Header, ValueSize,
    ///        (Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) * ValueSize)<sup>NumValues</sup></description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_VAR_SORTED"/> .idx --&gt; Header, TotalVarBytes, Addresses, Ordinals</description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_VAR_SORTED"/> .dat --&gt; Header,
    ///        (Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) * <i>variable ValueSize</i>)<sup>NumValues</sup></description></item>
    /// </list>
    /// Data Types:
    /// <list type="bullet">
    ///    <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///    <item><description>PackedType --&gt; Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>)</description></item>
    ///    <item><description>MaxAddress, MinValue, DefaultValue --&gt; Int64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) </description></item>
    ///    <item><description>PackedStream, Addresses, Ordinals --&gt; <see cref="Util.Packed.PackedInt32s"/></description></item>
    ///    <item><description>ValueSize, NumValues --&gt; Int32 (<see cref="Store.DataOutput.WriteInt32(int)"/>) </description></item>
    ///    <item><description>Float32 --&gt; 32-bit float encoded with <see cref="J2N.BitConversion.SingleToRawInt32Bits(float)"/>
    ///                       then written as Int32 (<see cref="Store.DataOutput.WriteInt32(int)"/>) </description></item>
    ///    <item><description>Float64 --&gt; 64-bit float encoded with <see cref="J2N.BitConversion.DoubleToRawInt64Bits(double)"/>
    ///                       then written as Int64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) </description></item>
    ///    <item><description>TotalBytes --&gt; VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </description></item>
    ///    <item><description>TotalVarBytes --&gt; Int64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) </description></item>
    ///    <item><description>LengthPrefix --&gt; Length of the data value as VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) (maximum
    ///                       of 2 bytes)</description></item>
    /// </list>
    /// Notes:
    /// <list type="bullet">
    ///    <item><description>PackedType is a 0 when compressed, 1 when the stream is written as 64-bit integers.</description></item>
    ///    <item><description>Addresses stores pointers to the actual byte location (indexed by docid). In the VAR_STRAIGHT
    ///        case, each entry can have a different length, so to determine the length, docid+1 is
    ///        retrieved. A sentinel address is written at the end for the VAR_STRAIGHT case, so the Addresses
    ///        stream contains maxdoc+1 indices. For the deduplicated VAR_DEREF case, each length
    ///        is encoded as a prefix to the data itself as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>)
    ///        (maximum of 2 bytes).</description></item>
    ///    <item><description>Ordinals stores the term ID in sorted order (indexed by docid). In the FIXED_SORTED case,
    ///        the address into the .dat can be computed from the ordinal as
    ///        <c>Header+ValueSize+(ordinal*ValueSize)</c> because the byte length is fixed.
    ///        In the VAR_SORTED case, there is double indirection (docid -> ordinal -> address), but
    ///        an additional sentinel ordinal+address is always written (so there are NumValues+1 ordinals). To
    ///        determine the length, ord+1's address is looked up as well.</description></item>
    ///    <item><description><see cref="LegacyDocValuesType.BYTES_VAR_STRAIGHT"/> in contrast to other straight
    ///        variants uses a <c>.idx</c> file to improve lookup perfromance. In contrast to
    ///        <see cref="LegacyDocValuesType.BYTES_VAR_DEREF"/> it doesn't apply deduplication of the document values.
    ///    </description></item>
    /// </list>
    /// <para/>
    /// Limitations:
    /// <list type="bullet">
    ///   <item><description> Binary doc values can be at most <see cref="MAX_BINARY_FIELD_LENGTH"/> in length.</description></item>
    /// </list> 
    /// </summary>
    [Obsolete("Only for reading old 4.0 and 4.1 segments")]
    [DocValuesFormatName("Lucene40")] // LUCENENET specific - using DocValuesFormatName attribute to ensure the default name passed from subclasses is the same as this class name
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
            : base()
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw UnsupportedOperationException.Create("this codec can only be used for reading");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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