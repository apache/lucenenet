namespace Lucene.Net.Codecs.Compressing
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
    /// A <seealso cref="StoredFieldsFormat"/> that is very similar to
    /// <seealso cref="Lucene40StoredFieldsFormat"/> but compresses documents in chunks in
    /// order to improve the compression ratio.
    /// <p>
    /// For a chunk size of <tt>chunkSize</tt> bytes, this <seealso cref="StoredFieldsFormat"/>
    /// does not support documents larger than (<tt>2<sup>31</sup> - chunkSize</tt>)
    /// bytes. In case this is a problem, you should use another format, such as
    /// <seealso cref="Lucene40StoredFieldsFormat"/>.
    /// <p>
    /// For optimal performance, you should use a <seealso cref="MergePolicy"/> that returns
    /// segments that have the biggest byte size first.
    /// @lucene.experimental
    /// </summary>
    public class CompressingStoredFieldsFormat : StoredFieldsFormat
    {
        private readonly string formatName;
        private readonly string segmentSuffix;
        private readonly CompressionMode compressionMode;
        private readonly int chunkSize;

        /// <summary>
        /// Create a new <seealso cref="CompressingStoredFieldsFormat"/> with an empty segment
        /// suffix.
        /// </summary>
        /// <seealso cref= CompressingStoredFieldsFormat#CompressingStoredFieldsFormat(String, String, CompressionMode, int) </seealso>
        public CompressingStoredFieldsFormat(string formatName, CompressionMode compressionMode, int chunkSize)
            : this(formatName, "", compressionMode, chunkSize)
        {
        }

        /// <summary>
        /// Create a new <seealso cref="CompressingStoredFieldsFormat"/>.
        /// <p>
        /// <code>formatName</code> is the name of the format. this name will be used
        /// in the file formats to perform
        /// <seealso cref="CodecUtil#checkHeader(Lucene.Net.Store.DataInput, String, int, int) codec header checks"/>.
        /// <p>
        /// <code>segmentSuffix</code> is the segment suffix. this suffix is added to
        /// the result file name only if it's not the empty string.
        /// <p>
        /// The <code>compressionMode</code> parameter allows you to choose between
        /// compression algorithms that have various compression and decompression
        /// speeds so that you can pick the one that best fits your indexing and
        /// searching throughput. You should never instantiate two
        /// <seealso cref="CompressingStoredFieldsFormat"/>s that have the same name but
        /// different <seealso cref="compressionMode"/>s.
        /// <p>
        /// <code>chunkSize</code> is the minimum byte size of a chunk of documents.
        /// A value of <code>1</code> can make sense if there is redundancy across
        /// fields. In that case, both performance and compression ratio should be
        /// better than with <seealso cref="Lucene40StoredFieldsFormat"/> with compressed
        /// fields.
        /// <p>
        /// Higher values of <code>chunkSize</code> should improve the compression
        /// ratio but will require more memory at indexing time and might make document
        /// loading a little slower (depending on the size of your OS cache compared
        /// to the size of your index).
        /// </summary>
        /// <param name="formatName"> the name of the <seealso cref="StoredFieldsFormat"/> </param>
        /// <param name="compressionMode"> the <seealso cref="compressionMode"/> to use </param>
        /// <param name="chunkSize"> the minimum number of bytes of a single chunk of stored documents </param>
        /// <seealso cref= compressionMode </seealso>
        public CompressingStoredFieldsFormat(string formatName, string segmentSuffix, CompressionMode compressionMode, int chunkSize)
        {
            this.formatName = formatName;
            this.segmentSuffix = segmentSuffix;
            this.compressionMode = compressionMode;
            if (chunkSize < 1)
            {
                throw new System.ArgumentException("chunkSize must be >= 1");
            }
            this.chunkSize = chunkSize;
        }

        public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            return new CompressingStoredFieldsReader(directory, si, segmentSuffix, fn, context, formatName, compressionMode);
        }

        public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
        {
            return new CompressingStoredFieldsWriter(directory, si, segmentSuffix, context, formatName, compressionMode, chunkSize);
        }

        public override string ToString()
        {
            return this.GetType().Name + "(compressionMode=" + compressionMode + ", chunkSize=" + chunkSize + ")";
        }
    }
}