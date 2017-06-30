namespace Lucene.Net.Codecs.Lucene41
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

    using CompressingStoredFieldsFormat = Lucene.Net.Codecs.Compressing.CompressingStoredFieldsFormat;
    using CompressingStoredFieldsIndexWriter = Lucene.Net.Codecs.Compressing.CompressingStoredFieldsIndexWriter;
    using CompressionMode = Lucene.Net.Codecs.Compressing.CompressionMode;
    using Lucene40StoredFieldsFormat = Lucene.Net.Codecs.Lucene40.Lucene40StoredFieldsFormat;
    using StoredFieldVisitor = Lucene.Net.Index.StoredFieldVisitor;

    /// <summary>
    /// Lucene 4.1 stored fields format.
    ///
    /// <para><b>Principle</b></para>
    /// <para>This <seealso cref="StoredFieldsFormat"/> compresses blocks of 16KB of documents in
    /// order to improve the compression ratio compared to document-level
    /// compression. It uses the <a href="http://code.google.com/p/lz4/">LZ4</a>
    /// compression algorithm, which is fast to compress and very fast to decompress
    /// data. Although the compression method that is used focuses more on speed
    /// than on compression ratio, it should provide interesting compression ratios
    /// for redundant inputs (such as log files, HTML or plain text).</para>
    /// <para><b>File formats</b></para>
    /// <para>Stored fields are represented by two files:</para>
    /// <list type="number">
    /// <item><description><a name="field_data" id="field_data"></a>
    /// <para>A fields data file (extension <c>.fdt</c>). this file stores a compact
    /// representation of documents in compressed blocks of 16KB or more. When
    /// writing a segment, documents are appended to an in-memory <c>byte[]</c>
    /// buffer. When its size reaches 16KB or more, some metadata about the documents
    /// is flushed to disk, immediately followed by a compressed representation of
    /// the buffer using the
    /// <a href="http://code.google.com/p/lz4/">LZ4</a>
    /// <a href="http://fastcompression.blogspot.fr/2011/05/lz4-explained.html">compression format</a>.</para>
    /// <para>Here is a more detailed description of the field data file format:</para>
    /// <list type="bullet">
    /// <item><description>FieldData (.fdt) --&gt; &lt;Header&gt;, PackedIntsVersion, &lt;Chunk&gt;<sup>ChunkCount</sup></description></item>
    /// <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    /// <item><description>PackedIntsVersion --&gt; <see cref="Util.Packed.PackedInt32s.VERSION_CURRENT"/> as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    /// <item><description>ChunkCount is not known in advance and is the number of chunks necessary to store all document of the segment</description></item>
    /// <item><description>Chunk --&gt; DocBase, ChunkDocs, DocFieldCounts, DocLengths, &lt;CompressedDocs&gt;</description></item>
    /// <item><description>DocBase --&gt; the ID of the first document of the chunk as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    /// <item><description>ChunkDocs --&gt; the number of documents in the chunk as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    /// <item><description>DocFieldCounts --&gt; the number of stored fields of every document in the chunk, encoded as followed:
    /// <list type="bullet">
    ///   <item><description>if chunkDocs=1, the unique value is encoded as a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>else read a VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) (let's call it <c>bitsRequired</c>)
    ///   <list type="bullet">
    ///     <item><description>if <c>bitsRequired</c> is <c>0</c> then all values are equal, and the common value is the following VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///     <item><description>else <c>bitsRequired</c> is the number of bits required to store any value, and values are stored in a packed (<see cref="Util.Packed.PackedInt32s"/>) array where every value is stored on exactly <c>bitsRequired</c> bits</description></item>
    ///   </list>
    ///   </description></item>
    /// </list>
    /// </description></item>
    /// <item><description>DocLengths --&gt; the lengths of all documents in the chunk, encoded with the same method as DocFieldCounts</description></item>
    /// <item><description>CompressedDocs --&gt; a compressed representation of &lt;Docs&gt; using the LZ4 compression format</description></item>
    /// <item><description>Docs --&gt; &lt;Doc&gt;<sup>ChunkDocs</sup></description></item>
    /// <item><description>Doc --&gt; &lt;FieldNumAndType, Value&gt;<sup>DocFieldCount</sup></description></item>
    /// <item><description>FieldNumAndType --&gt; a VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>), whose 3 last bits are Type and other bits are FieldNum</description></item>
    /// <item><description>Type --&gt;
    /// <list type="bullet">
    ///   <item><description>0: Value is String</description></item>
    ///   <item><description>1: Value is BinaryValue</description></item>
    ///   <item><description>2: Value is Int</description></item>
    ///   <item><description>3: Value is Float</description></item>
    ///   <item><description>4: Value is Long</description></item>
    ///   <item><description>5: Value is Double</description></item>
    ///   <item><description>6, 7: unused</description></item>
    /// </list>
    /// </description></item>
    /// <item><description>FieldNum --&gt; an ID of the field</description></item>
    /// <item><description>Value --&gt; String (<see cref="Store.DataOutput.WriteString(string)"/>) | BinaryValue | Int | Float | Long | Double depending on Type</description></item>
    /// <item><description>BinaryValue --&gt; ValueLength &lt;Byte&gt;<sup>ValueLength</sup></description></item>
    /// </list>
    /// <para>Notes</para>
    /// <list type="bullet">
    /// <item><description>If documents are larger than 16KB then chunks will likely contain only
    /// one document. However, documents can never spread across several chunks (all
    /// fields of a single document are in the same chunk).</description></item>
    /// <item><description>When at least one document in a chunk is large enough so that the chunk
    /// is larger than 32KB, the chunk will actually be compressed in several LZ4
    /// blocks of 16KB. this allows <see cref="StoredFieldVisitor"/>s which are only
    /// interested in the first fields of a document to not have to decompress 10MB
    /// of data if the document is 10MB, but only 16KB.</description></item>
    /// <item><description>Given that the original lengths are written in the metadata of the chunk,
    /// the decompressor can leverage this information to stop decoding as soon as
    /// enough data has been decompressed.</description></item>
    /// <item><description>In case documents are incompressible, CompressedDocs will be less than
    /// 0.5% larger than Docs.</description></item>
    /// </list>
    /// </description></item>
    /// <item><description><a name="field_index" id="field_index"></a>
    /// <para>A fields index file (extension <c>.fdx</c>).</para>
    /// <list type="bullet">
    /// <item><description>FieldsIndex (.fdx) --&gt; &lt;Header&gt;, &lt;ChunkIndex&gt;</description></item>
    /// <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    /// <item><description>ChunkIndex: See <see cref="CompressingStoredFieldsIndexWriter"/></description></item>
    /// </list>
    /// </description></item>
    /// </list>
    /// <para><b>Known limitations</b></para>
    /// <para>This <see cref="StoredFieldsFormat"/> does not support individual documents
    /// larger than (<c>2<sup>31</sup> - 2<sup>14</sup></c>) bytes. In case this
    /// is a problem, you should use another format, such as
    /// <see cref="Lucene40StoredFieldsFormat"/>.</para>
    /// @lucene.experimental
    /// </summary>
    public sealed class Lucene41StoredFieldsFormat : CompressingStoredFieldsFormat
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene41StoredFieldsFormat()
            : base("Lucene41StoredFields", CompressionMode.FAST, 1 << 14)
        {
        }
    }
}