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

    // javadoc
    // javadoc

    /// <summary>
    /// Lucene 4.2 Field Infos format.
    /// <p>
    /// <p>Field names are stored in the field info file, with suffix <tt>.fnm</tt>.</p>
    /// <p>FieldInfos (.fnm) --&gt; Header,FieldsCount, &lt;FieldName,FieldNumber,
    /// FieldBits,DocValuesBits,Attributes&gt; <sup>FieldsCount</sup></p>
    /// <p>Data types:
    /// <ul>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#checkHeader CodecHeader"/></li>
    ///   <li>FieldsCount --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>FieldName --&gt; <seealso cref="DataOutput#writeString String"/></li>
    ///   <li>FieldBits, DocValuesBits --&gt; <seealso cref="DataOutput#writeByte Byte"/></li>
    ///   <li>FieldNumber --&gt; <seealso cref="DataOutput#writeInt VInt"/></li>
    ///   <li>Attributes --&gt; <seealso cref="DataOutput#writeStringStringMap Map&lt;String,String&gt;"/></li>
    /// </ul>
    /// </p>
    /// Field Descriptions:
    /// <ul>
    ///   <li>FieldsCount: the number of fields in this file.</li>
    ///   <li>FieldName: name of the field as a UTF-8 String.</li>
    ///   <li>FieldNumber: the field's number. Note that unlike previous versions of
    ///       Lucene, the fields are not numbered implicitly by their order in the
    ///       file, instead explicitly.</li>
    ///   <li>FieldBits: a byte containing field options.
    ///       <ul>
    ///         <li>The low-order bit is one for indexed fields, and zero for non-indexed
    ///             fields.</li>
    ///         <li>The second lowest-order bit is one for fields that have term vectors
    ///             stored, and zero for fields without term vectors.</li>
    ///         <li>If the third lowest order-bit is set (0x4), offsets are stored into
    ///             the postings list in addition to positions.</li>
    ///         <li>Fourth bit is unused.</li>
    ///         <li>If the fifth lowest-order bit is set (0x10), norms are omitted for the
    ///             indexed field.</li>
    ///         <li>If the sixth lowest-order bit is set (0x20), payloads are stored for the
    ///             indexed field.</li>
    ///         <li>If the seventh lowest-order bit is set (0x40), term frequencies and
    ///             positions omitted for the indexed field.</li>
    ///         <li>If the eighth lowest-order bit is set (0x80), positions are omitted for the
    ///             indexed field.</li>
    ///       </ul>
    ///    </li>
    ///    <li>DocValuesBits: a byte containing per-document value types. The type
    ///        recorded as two four-bit integers, with the high-order bits representing
    ///        <code>norms</code> options, and the low-order bits representing
    ///        {@code DocValues} options. Each four-bit integer can be decoded as such:
    ///        <ul>
    ///          <li>0: no DocValues for this field.</li>
    ///          <li>1: NumericDocValues. (<seealso cref="DocValuesType#NUMERIC"/>)</li>
    ///          <li>2: BinaryDocValues. ({@code DocValuesType#BINARY})</li>
    ///          <li>3: SortedDocValues. ({@code DocValuesType#SORTED})</li>
    ///        </ul>
    ///    </li>
    ///    <li>Attributes: a key-value map of codec-private attributes.</li>
    /// </ul>
    ///
    /// @lucene.experimental </summary>
    /// @deprecated Only for reading old 4.2-4.5 segments
    [Obsolete("Only for reading old 4.2-4.5 segments")]
    public class Lucene42FieldInfosFormat : FieldInfosFormat
    {
        private readonly FieldInfosReader reader = new Lucene42FieldInfosReader();

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene42FieldInfosFormat()
        {
        }

        public override FieldInfosReader FieldInfosReader
        {
            get
            {
                return reader;
            }
        }

        public override FieldInfosWriter FieldInfosWriter
        {
            get
            {
                throw new System.NotSupportedException("this codec can only be used for reading");
            }
        }

        /// <summary>
        /// Extension of field infos </summary>
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