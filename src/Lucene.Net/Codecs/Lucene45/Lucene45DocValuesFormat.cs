using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene45
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
    /// Lucene 4.5 DocValues format.
    /// <para/>
    /// Encodes the four per-document value types (Numeric,Binary,Sorted,SortedSet) with these strategies:
    /// <para/>
    /// <see cref="Index.DocValuesType.NUMERIC"/>:
    /// <list type="bullet">
    ///    <item><description>Delta-compressed: per-document integers written in blocks of 16k. For each block
    ///        the minimum value in that block is encoded, and each entry is a delta from that
    ///        minimum value. Each block of deltas is compressed with bitpacking. For more
    ///        information, see <see cref="Util.Packed.BlockPackedWriter"/>.</description></item>
    ///    <item><description>Table-compressed: when the number of unique values is very small (&lt; 256), and
    ///        when there are unused "gaps" in the range of values used (such as <see cref="Util.SmallSingle"/>),
    ///        a lookup table is written instead. Each per-document entry is instead the ordinal
    ///        to this table, and those ordinals are compressed with bitpacking (<see cref="Util.Packed.PackedInt32s"/>).</description></item>
    ///    <item><description>GCD-compressed: when all numbers share a common divisor, such as dates, the greatest
    ///        common denominator (GCD) is computed, and quotients are stored using Delta-compressed Numerics.</description></item>
    /// </list>
    /// <para/>
    /// <see cref="Index.DocValuesType.BINARY"/>:
    /// <list type="bullet">
    ///    <item><description>Fixed-width Binary: one large concatenated <see cref="T:byte[]"/> is written, along with the fixed length.
    ///        Each document's value can be addressed directly with multiplication (<c>docID * length</c>).</description></item>
    ///    <item><description>Variable-width Binary: one large concatenated <see cref="T:byte[]"/> is written, along with end addresses
    ///        for each document. The addresses are written in blocks of 16k, with the current absolute
    ///        start for the block, and the average (expected) delta per entry. For each document the
    ///        deviation from the delta (actual - expected) is written.</description></item>
    ///    <item><description>Prefix-compressed Binary: values are written in chunks of 16, with the first value written
    ///        completely and other values sharing prefixes. Chunk addresses are written in blocks of 16k,
    ///        with the current absolute start for the block, and the average (expected) delta per entry.
    ///        For each chunk the deviation from the delta (actual - expected) is written.</description></item>
    /// </list>
    /// <para/>
    /// <see cref="Index.DocValuesType.SORTED"/>:
    /// <list type="bullet">
    ///    <item><description>Sorted: a mapping of ordinals to deduplicated terms is written as Prefix-Compressed Binary,
    ///        along with the per-document ordinals written using one of the numeric strategies above.</description></item>
    /// </list>
    /// <para/>
    /// <see cref="Index.DocValuesType.SORTED_SET"/>:
    /// <list type="bullet">
    ///    <item><description>SortedSet: a mapping of ordinals to deduplicated terms is written as Prefix-Compressed Binary,
    ///        an ordinal list and per-document index into this list are written using the numeric strategies
    ///        above.</description></item>
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
    ///   <para>DocValues metadata (.dvm) --&gt; Header,&lt;Entry&gt;<sup>NumFields</sup>,Footer</para>
    ///   <list type="bullet">
    ///     <item><description>Entry --&gt; NumericEntry | BinaryEntry | SortedEntry | SortedSetEntry</description></item>
    ///     <item><description>NumericEntry --&gt; GCDNumericEntry | TableNumericEntry | DeltaNumericEntry</description></item>
    ///     <item><description>GCDNumericEntry --&gt; NumericHeader,MinValue,GCD</description></item>
    ///     <item><description>TableNumericEntry --&gt; NumericHeader,TableSize,Int64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) <sup>TableSize</sup></description></item>
    ///     <item><description>DeltaNumericEntry --&gt; NumericHeader</description></item>
    ///     <item><description>NumericHeader --&gt; FieldNumber,EntryType,NumericType,MissingOffset,PackedVersion,DataOffset,Count,BlockSize</description></item>
    ///     <item><description>BinaryEntry --&gt; FixedBinaryEntry | VariableBinaryEntry | PrefixBinaryEntry</description></item>
    ///     <item><description>FixedBinaryEntry --&gt; BinaryHeader</description></item>
    ///     <item><description>VariableBinaryEntry --&gt; BinaryHeader,AddressOffset,PackedVersion,BlockSize</description></item>
    ///     <item><description>PrefixBinaryEntry --&gt; BinaryHeader,AddressInterval,AddressOffset,PackedVersion,BlockSize</description></item>
    ///     <item><description>BinaryHeader --&gt; FieldNumber,EntryType,BinaryType,MissingOffset,MinLength,MaxLength,DataOffset</description></item>
    ///     <item><description>SortedEntry --&gt; FieldNumber,EntryType,BinaryEntry,NumericEntry</description></item>
    ///     <item><description>SortedSetEntry --&gt; EntryType,BinaryEntry,NumericEntry,NumericEntry</description></item>
    ///     <item><description>FieldNumber,PackedVersion,MinLength,MaxLength,BlockSize,ValueCount --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/></description></item>
    ///     <item><description>EntryType,CompressionType --&gt; Byte (<see cref="Store.DataOutput.WriteByte(byte)"/></description></item>
    ///     <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///     <item><description>MinValue,GCD,MissingOffset,AddressOffset,DataOffset --&gt; Int64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) </description></item>
    ///     <item><description>TableSize --&gt; vInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///     <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    ///   </list>
    ///   <para>Sorted fields have two entries: a <see cref="Lucene45DocValuesProducer.BinaryEntry"/> with the value metadata,
    ///      and an ordinary <see cref="Lucene45DocValuesProducer.NumericEntry"/> for the document-to-ord metadata.</para>
    ///   <para>SortedSet fields have three entries: a <see cref="Lucene45DocValuesProducer.BinaryEntry"/> with the value metadata,
    ///      and two <see cref="Lucene45DocValuesProducer.NumericEntry"/>s for the document-to-ord-index and ordinal list metadata.</para>
    ///   <para>FieldNumber of -1 indicates the end of metadata.</para>
    ///   <para>EntryType is a 0 (<see cref="Lucene45DocValuesProducer.NumericEntry"/>) or 1 (<see cref="Lucene45DocValuesProducer.BinaryEntry"/>)</para>
    ///   <para>DataOffset is the pointer to the start of the data in the DocValues data (.dvd)</para>
    ///   <para/>NumericType indicates how Numeric values will be compressed:
    ///      <list type="bullet">
    ///         <item><description>0 --&gt; delta-compressed. For each block of 16k integers, every integer is delta-encoded
    ///             from the minimum value within the block.</description></item>
    ///         <item><description>1 --&gt; gcd-compressed. When all integers share a common divisor, only quotients are stored
    ///             using blocks of delta-encoded ints.</description></item>
    ///         <item><description>2 --&gt; table-compressed. When the number of unique numeric values is small and it would save space,
    ///             a lookup table of unique values is written, followed by the ordinal for each document.</description></item>
    ///      </list>
    ///   <para/>BinaryType indicates how Binary values will be stored:
    ///      <list type="bullet">
    ///         <item><description>0 --&gt; fixed-width. All values have the same length, addressing by multiplication.</description></item>
    ///         <item><description>1 --&gt; variable-width. An address for each value is stored.</description></item>
    ///         <item><description>2 --&gt; prefix-compressed. An address to the start of every interval'th value is stored.</description></item>
    ///      </list>
    ///   <para/>MinLength and MaxLength represent the min and max byte[] value lengths for Binary values.
    ///      If they are equal, then all values are of a fixed size, and can be addressed as DataOffset + (docID * length).
    ///      Otherwise, the binary values are of variable size, and packed integer metadata (PackedVersion,BlockSize)
    ///      is written for the addresses.
    ///   <para/>MissingOffset points to a <see cref="T:byte[]"/> containing a bitset of all documents that had a value for the field.
    ///      If its -1, then there are no missing values.
    ///   <para/>Checksum contains the CRC32 checksum of all bytes in the .dvm file up
    ///      until the checksum. this is used to verify integrity of the file on opening the
    ///      index.</description></item>
    ///   <item><description><a name="dvd" id="dvd"></a>
    ///   <para>The DocValues data or .dvd file.</para>
    ///   <para>For DocValues field, this stores the actual per-document data (the heavy-lifting)</para>
    ///   <para>DocValues data (.dvd) --&gt; Header,&lt;NumericData | BinaryData | SortedData&gt;<sup>NumFields</sup>,Footer</para>
    ///   <list type="bullet">
    ///     <item><description>NumericData --&gt; DeltaCompressedNumerics | TableCompressedNumerics | GCDCompressedNumerics</description></item>
    ///     <item><description>BinaryData --&gt;  Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) <sup>DataLength</sup>,Addresses</description></item>
    ///     <item><description>SortedData --&gt; FST&lt;Int64&gt; (<see cref="Util.Fst.FST{T}"/>) </description></item>
    ///     <item><description>DeltaCompressedNumerics --&gt; BlockPackedInts(blockSize=16k) (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///     <item><description>TableCompressedNumerics --&gt; PackedInts (<see cref="Util.Packed.PackedInt32s"/>) </description></item>
    ///     <item><description>GCDCompressedNumerics --&gt; BlockPackedInts(blockSize=16k) (<see cref="Util.Packed.BlockPackedWriter"/>) </description></item>
    ///     <item><description>Addresses --&gt; MonotonicBlockPackedInts(blockSize=16k) (<see cref="Util.Packed.MonotonicBlockPackedWriter"/>) </description></item>
    ///     <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(Store.IndexOutput)"/>) </description></item>
    ///   </list>
    ///   <para>SortedSet entries store the list of ordinals in their BinaryData as a
    ///      sequences of increasing vLongs (<see cref="Store.DataOutput.WriteVInt64(long)"/>), delta-encoded.</para></description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [DocValuesFormatName("Lucene45")] // LUCENENET specific - using DocValuesFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public sealed class Lucene45DocValuesFormat : DocValuesFormat
    {
        /// <summary>
        /// Sole Constructor </summary>
        public Lucene45DocValuesFormat()
            : base()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new Lucene45DocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new Lucene45DocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION);
        }

        internal const string DATA_CODEC = "Lucene45DocValuesData";
        internal const string DATA_EXTENSION = "dvd";
        internal const string META_CODEC = "Lucene45ValuesMetadata";
        internal const string META_EXTENSION = "dvm";
        internal const int VERSION_START = 0;
        internal const int VERSION_SORTED_SET_SINGLE_VALUE_OPTIMIZED = 1;
        internal const int VERSION_CHECKSUM = 2;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;
        internal const sbyte NUMERIC = 0;
        internal const sbyte BINARY = 1;
        internal const sbyte SORTED = 2;
        internal const sbyte SORTED_SET = 3;
    }
}