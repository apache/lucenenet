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
    /// <p>
    /// Encodes the four per-document value types (Numeric,Binary,Sorted,SortedSet) with these strategies:
    /// <p>
    /// <seealso cref="DocValuesType#NUMERIC NUMERIC"/>:
    /// <ul>
    ///    <li>Delta-compressed: per-document integers written in blocks of 16k. For each block
    ///        the minimum value in that block is encoded, and each entry is a delta from that
    ///        minimum value. Each block of deltas is compressed with bitpacking. For more
    ///        information, see <seealso cref="BlockPackedWriter"/>.
    ///    <li>Table-compressed: when the number of unique values is very small (&lt; 256), and
    ///        when there are unused "gaps" in the range of values used (such as <seealso cref="SmallFloat"/>),
    ///        a lookup table is written instead. Each per-document entry is instead the ordinal
    ///        to this table, and those ordinals are compressed with bitpacking (<seealso cref="PackedInts"/>).
    ///    <li>GCD-compressed: when all numbers share a common divisor, such as dates, the greatest
    ///        common denominator (GCD) is computed, and quotients are stored using Delta-compressed Numerics.
    /// </ul>
    /// <p>
    /// <seealso cref="DocValuesType#BINARY BINARY"/>:
    /// <ul>
    ///    <li>Fixed-width Binary: one large concatenated byte[] is written, along with the fixed length.
    ///        Each document's value can be addressed directly with multiplication ({@code docID * length}).
    ///    <li>Variable-width Binary: one large concatenated byte[] is written, along with end addresses
    ///        for each document. The addresses are written in blocks of 16k, with the current absolute
    ///        start for the block, and the average (expected) delta per entry. For each document the
    ///        deviation from the delta (actual - expected) is written.
    ///    <li>Prefix-compressed Binary: values are written in chunks of 16, with the first value written
    ///        completely and other values sharing prefixes. chunk addresses are written in blocks of 16k,
    ///        with the current absolute start for the block, and the average (expected) delta per entry.
    ///        For each chunk the deviation from the delta (actual - expected) is written.
    /// </ul>
    /// <p>
    /// <seealso cref="DocValuesType#SORTED SORTED"/>:
    /// <ul>
    ///    <li>Sorted: a mapping of ordinals to deduplicated terms is written as Prefix-Compressed Binary,
    ///        along with the per-document ordinals written using one of the numeric strategies above.
    /// </ul>
    /// <p>
    /// <seealso cref="DocValuesType#SORTED_SET SORTED_SET"/>:
    /// <ul>
    ///    <li>SortedSet: a mapping of ordinals to deduplicated terms is written as Prefix-Compressed Binary,
    ///        an ordinal list and per-document index into this list are written using the numeric strategies
    ///        above.
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
    ///   <p>DocValues metadata (.dvm) --&gt; Header,&lt;Entry&gt;<sup>NumFields</sup>,Footer</p>
    ///   <ul>
    ///     <li>Entry --&gt; NumericEntry | BinaryEntry | SortedEntry | SortedSetEntry</li>
    ///     <li>NumericEntry --&gt; GCDNumericEntry | TableNumericEntry | DeltaNumericEntry</li>
    ///     <li>GCDNumericEntry --&gt; NumericHeader,MinValue,GCD</li>
    ///     <li>TableNumericEntry --&gt; NumericHeader,TableSize,<seealso cref="DataOutput#writeLong Int64"/><sup>TableSize</sup></li>
    ///     <li>DeltaNumericEntry --&gt; NumericHeader</li>
    ///     <li>NumericHeader --&gt; FieldNumber,EntryType,NumericType,MissingOffset,PackedVersion,DataOffset,Count,BlockSize</li>
    ///     <li>BinaryEntry --&gt; FixedBinaryEntry | VariableBinaryEntry | PrefixBinaryEntry</li>
    ///     <li>FixedBinaryEntry --&gt; BinaryHeader</li>
    ///     <li>VariableBinaryEntry --&gt; BinaryHeader,AddressOffset,PackedVersion,BlockSize</li>
    ///     <li>PrefixBinaryEntry --&gt; BinaryHeader,AddressInterval,AddressOffset,PackedVersion,BlockSize</li>
    ///     <li>BinaryHeader --&gt; FieldNumber,EntryType,BinaryType,MissingOffset,MinLength,MaxLength,DataOffset</li>
    ///     <li>SortedEntry --&gt; FieldNumber,EntryType,BinaryEntry,NumericEntry</li>
    ///     <li>SortedSetEntry --&gt; EntryType,BinaryEntry,NumericEntry,NumericEntry</li>
    ///     <li>FieldNumber,PackedVersion,MinLength,MaxLength,BlockSize,ValueCount --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///     <li>EntryType,CompressionType --&gt; <seealso cref="DataOutput#writeByte Byte"/></li>
    ///     <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///     <li>MinValue,GCD,MissingOffset,AddressOffset,DataOffset --&gt; <seealso cref="DataOutput#writeLong Int64"/></li>
    ///     <li>TableSize --&gt; <seealso cref="DataOutput#writeVInt vInt"/></li>
    ///     <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    ///   </ul>
    ///   <p>Sorted fields have two entries: a BinaryEntry with the value metadata,
    ///      and an ordinary NumericEntry for the document-to-ord metadata.</p>
    ///   <p>SortedSet fields have three entries: a BinaryEntry with the value metadata,
    ///      and two NumericEntries for the document-to-ord-index and ordinal list metadata.</p>
    ///   <p>FieldNumber of -1 indicates the end of metadata.</p>
    ///   <p>EntryType is a 0 (NumericEntry) or 1 (BinaryEntry)</p>
    ///   <p>DataOffset is the pointer to the start of the data in the DocValues data (.dvd)</p>
    ///   <p>NumericType indicates how Numeric values will be compressed:
    ///      <ul>
    ///         <li>0 --&gt; delta-compressed. For each block of 16k integers, every integer is delta-encoded
    ///             from the minimum value within the block.
    ///         <li>1 --&gt, gcd-compressed. When all integers share a common divisor, only quotients are stored
    ///             using blocks of delta-encoded ints.
    ///         <li>2 --&gt; table-compressed. When the number of unique numeric values is small and it would save space,
    ///             a lookup table of unique values is written, followed by the ordinal for each document.
    ///      </ul>
    ///   <p>BinaryType indicates how Binary values will be stored:
    ///      <ul>
    ///         <li>0 --&gt; fixed-width. All values have the same length, addressing by multiplication.
    ///         <li>1 --&gt, variable-width. An address for each value is stored.
    ///         <li>2 --&gt; prefix-compressed. An address to the start of every interval'th value is stored.
    ///      </ul>
    ///   <p>MinLength and MaxLength represent the min and max byte[] value lengths for Binary values.
    ///      If they are equal, then all values are of a fixed size, and can be addressed as DataOffset + (docID * length).
    ///      Otherwise, the binary values are of variable size, and packed integer metadata (PackedVersion,BlockSize)
    ///      is written for the addresses.
    ///   <p>MissingOffset points to a byte[] containing a bitset of all documents that had a value for the field.
    ///      If its -1, then there are no missing values.
    ///   <p>Checksum contains the CRC32 checksum of all bytes in the .dvm file up
    ///      until the checksum. this is used to verify integrity of the file on opening the
    ///      index.
    ///   <li><a name="dvd" id="dvd"></a>
    ///   <p>The DocValues data or .dvd file.</p>
    ///   <p>For DocValues field, this stores the actual per-document data (the heavy-lifting)</p>
    ///   <p>DocValues data (.dvd) --&gt; Header,&lt;NumericData | BinaryData | SortedData&gt;<sup>NumFields</sup>,Footer</p>
    ///   <ul>
    ///     <li>NumericData --&gt; DeltaCompressedNumerics | TableCompressedNumerics | GCDCompressedNumerics</li>
    ///     <li>BinaryData --&gt;  <seealso cref="DataOutput#writeByte Byte"/><sup>DataLength</sup>,Addresses</li>
    ///     <li>SortedData --&gt; <seealso cref="FST FST&lt;Int64&gt;"/></li>
    ///     <li>DeltaCompressedNumerics --&gt; <seealso cref="BlockPackedWriter BlockPackedInts(blockSize=16k)"/></li>
    ///     <li>TableCompressedNumerics --&gt; <seealso cref="PackedInts PackedInts"/></li>
    ///     <li>GCDCompressedNumerics --&gt; <seealso cref="BlockPackedWriter BlockPackedInts(blockSize=16k)"/></li>
    ///     <li>Addresses --&gt; <seealso cref="MonotonicBlockPackedWriter MonotonicBlockPackedInts(blockSize=16k)"/></li>
    ///     <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    ///   </ul>
    ///   <p>SortedSet entries store the list of ordinals in their BinaryData as a
    ///      sequences of increasing <seealso cref="DataOutput#writeVLong vLong"/>s, delta-encoded.</p>
    /// </ol>
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

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new Lucene45DocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION);
        }

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