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

    /// <summary>
    /// Lucene 4.2 Field Infos format.
    /// <para/>
    /// <para>Field names are stored in the field info file, with suffix <c>.fnm</c>.</para>
    /// <para>FieldInfos (.fnm) --&gt; Header,FieldsCount, &lt;FieldName,FieldNumber,
    /// FieldBits,DocValuesBits,Attributes&gt; <sup>FieldsCount</sup></para>
    /// <para>Data types:
    /// <list type="bullet">
    ///   <item><description>Header --&gt; CodecHeader <see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/></description></item>
    ///   <item><description>FieldsCount --&gt; VInt <see cref="Store.DataOutput.WriteVInt32(int)"/></description></item>
    ///   <item><description>FieldName --&gt; String <see cref="Store.DataOutput.WriteString(string)"/></description></item>
    ///   <item><description>FieldBits, DocValuesBits --&gt; Byte <see cref="Store.DataOutput.WriteByte(byte)"/></description></item>
    ///   <item><description>FieldNumber --&gt; VInt <see cref="Store.DataOutput.WriteInt32(int)"/></description></item>
    ///   <item><description>Attributes --&gt; IDictionary&lt;String,String&gt; <see cref="Store.DataOutput.WriteStringStringMap(System.Collections.Generic.IDictionary{string, string})"/></description></item>
    /// </list>
    /// </para>
    /// Field Descriptions:
    /// <list type="bullet">
    ///   <item><description>FieldsCount: the number of fields in this file.</description></item>
    ///   <item><description>FieldName: name of the field as a UTF-8 String.</description></item>
    ///   <item><description>FieldNumber: the field's number. Note that unlike previous versions of
    ///       Lucene, the fields are not numbered implicitly by their order in the
    ///       file, instead explicitly.</description></item>
    ///   <item><description>FieldBits: a byte containing field options.
    ///       <list type="bullet">
    ///         <item><description>The low-order bit is one for indexed fields, and zero for non-indexed
    ///             fields.</description></item>
    ///         <item><description>The second lowest-order bit is one for fields that have term vectors
    ///             stored, and zero for fields without term vectors.</description></item>
    ///         <item><description>If the third lowest order-bit is set (0x4), offsets are stored into
    ///             the postings list in addition to positions.</description></item>
    ///         <item><description>Fourth bit is unused.</description></item>
    ///         <item><description>If the fifth lowest-order bit is set (0x10), norms are omitted for the
    ///             indexed field.</description></item>
    ///         <item><description>If the sixth lowest-order bit is set (0x20), payloads are stored for the
    ///             indexed field.</description></item>
    ///         <item><description>If the seventh lowest-order bit is set (0x40), term frequencies and
    ///             positions omitted for the indexed field.</description></item>
    ///         <item><description>If the eighth lowest-order bit is set (0x80), positions are omitted for the
    ///             indexed field.</description></item>
    ///       </list>
    ///    </description></item>
    ///    <item><description>DocValuesBits: a byte containing per-document value types. The type
    ///        recorded as two four-bit integers, with the high-order bits representing
    ///        <c>norms</c> options, and the low-order bits representing
    ///        <see cref="Index.DocValues"/> options. Each four-bit integer can be decoded as such:
    ///        <list type="bullet">
    ///          <item><description>0: no DocValues for this field.</description></item>
    ///          <item><description>1: NumericDocValues. (<see cref="Index.DocValuesType.NUMERIC"/>)</description></item>
    ///          <item><description>2: BinaryDocValues. (<see cref="Index.DocValuesType.BINARY"/>)</description></item>
    ///          <item><description>3: SortedDocValues. (<see cref="Index.DocValuesType.SORTED"/>)</description></item>
    ///        </list>
    ///    </description></item>
    ///    <item><description>Attributes: a key-value map of codec-private attributes.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [Obsolete("Only for reading old 4.2-4.5 segments")]
    public class Lucene42FieldInfosFormat : FieldInfosFormat
    {
        private readonly FieldInfosReader reader = new Lucene42FieldInfosReader();

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene42FieldInfosFormat()
        {
        }

        public override FieldInfosReader FieldInfosReader => reader;

        public override FieldInfosWriter FieldInfosWriter => throw UnsupportedOperationException.Create("this codec can only be used for reading");

        /// <summary>
        /// Extension of field infos. </summary>
        internal const string EXTENSION = "fnm";

        // Codec header
        internal const string CODEC_NAME = "Lucene42FieldInfos";

        internal const int FORMAT_START = 0;
        internal const int FORMAT_CURRENT = FORMAT_START;

        // Field flags
        internal const sbyte IS_INDEXED = 0x1;

        internal const sbyte STORE_TERMVECTOR = 0x2;
        internal const sbyte STORE_OFFSETS_IN_POSTINGS = 0x4;
        internal const sbyte OMIT_NORMS = 0x10;
        internal const sbyte STORE_PAYLOADS = 0x20;
        internal const sbyte OMIT_TERM_FREQ_AND_POSITIONS = 0x40;
        internal const sbyte OMIT_POSITIONS = -128;
    }
}