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

    using Directory = Lucene.Net.Store.Directory;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IOContext = Lucene.Net.Store.IOContext;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Lucene 4.0 Stored Fields Format.
    /// <para>Stored fields are represented by two files:</para>
    /// <list type="number">
    ///     <item><description><a name="field_index" id="field_index"></a>
    ///         <para>The field index, or <c>.fdx</c> file.</para>
    ///         <para>This is used to find the location within the field data file of the fields
    ///         of a particular document. Because it contains fixed-length data, this file may
    ///         be easily randomly accessed. The position of document <i>n</i> 's field data is
    ///         the Uint64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) at <i>n*8</i> in this file.</para>
    ///         <para>This contains, for each document, a pointer to its field data, as
    ///         follows:</para>
    ///         <list type="bullet">
    ///             <item><description>FieldIndex (.fdx) --&gt; &lt;Header&gt;, &lt;FieldValuesPosition&gt; <sup>SegSize</sup></description></item>
    ///             <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///             <item><description>FieldValuesPosition --&gt; Uint64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) </description></item>
    ///         </list>
    ///     </description></item>
    ///     <item><description>
    ///         <para><a name="field_data" id="field_data"></a>The field data, or <c>.fdt</c> file.</para>
    ///         <para>This contains the stored fields of each document, as follows:</para>
    ///         <list type="bullet">
    ///             <item><description>FieldData (.fdt) --&gt; &lt;Header&gt;, &lt;DocFieldData&gt; <sup>SegSize</sup></description></item>
    ///             <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///             <item><description>DocFieldData --&gt; FieldCount, &lt;FieldNum, Bits, Value&gt;
    ///                 <sup>FieldCount</sup></description></item>
    ///             <item><description>FieldCount --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///             <item><description>FieldNum --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///             <item><description>Bits --&gt; Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>)
    ///                 <list type="bullet">
    ///                     <item><description>low order bit reserved.</description></item>
    ///                     <item><description>second bit is one for fields containing binary data</description></item>
    ///                     <item><description>third bit reserved.</description></item>
    ///                     <item><description>4th to 6th bit (mask: 0x7&lt;&lt;3) define the type of a numeric field:
    ///                         <list type="bullet">
    ///                             <item><description>all bits in mask are cleared if no numeric field at all</description></item>
    ///                             <item><description>1&lt;&lt;3: Value is Int</description></item>
    ///                             <item><description>2&lt;&lt;3: Value is Long</description></item>
    ///                             <item><description>3&lt;&lt;3: Value is Int as Float (as of <see cref="J2N.BitConversion.Int32BitsToSingle(int)"/></description></item>
    ///                             <item><description>4&lt;&lt;3: Value is Long as Double (as of <see cref="J2N.BitConversion.Int64BitsToDouble(long)"/></description></item>
    ///                         </list>
    ///                     </description></item>
    ///                 </list>
    ///             </description></item>
    ///             <item><description>Value --&gt; String | BinaryValue | Int | Long (depending on Bits)</description></item>
    ///             <item><description>BinaryValue --&gt; ValueSize, &lt; Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) &gt;^ValueSize</description></item>
    ///             <item><description>ValueSize --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///         </list>
    ///     </description></item>
    /// </list>
    /// @lucene.experimental
    /// </summary>
    public class Lucene40StoredFieldsFormat : StoredFieldsFormat
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40StoredFieldsFormat()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            return new Lucene40StoredFieldsReader(directory, si, fn, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
        {
            return new Lucene40StoredFieldsWriter(directory, si.Name, context);
        }
    }
}