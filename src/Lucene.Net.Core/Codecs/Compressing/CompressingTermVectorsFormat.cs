using Directory = Lucene.Net.Store.Directory;

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

    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IOContext = Lucene.Net.Store.IOContext;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// A <seealso cref="TermVectorsFormat"/> that compresses chunks of documents together in
    /// order to improve the compression ratio.
    /// @lucene.experimental
    /// </summary>
    public class CompressingTermVectorsFormat : TermVectorsFormat
    {
        private readonly string FormatName;
        private readonly string SegmentSuffix;
        private readonly CompressionMode CompressionMode;
        private readonly int ChunkSize;

        /// <summary>
        /// Create a new <seealso cref="CompressingTermVectorsFormat"/>.
        /// <p>
        /// <code>formatName</code> is the name of the format. this name will be used
        /// in the file formats to perform
        /// <seealso cref="CodecUtil#checkHeader(Lucene.Net.Store.DataInput, String, int, int) codec header checks"/>.
        /// <p>
        /// The <code>compressionMode</code> parameter allows you to choose between
        /// compression algorithms that have various compression and decompression
        /// speeds so that you can pick the one that best fits your indexing and
        /// searching throughput. You should never instantiate two
        /// <seealso cref="CompressingTermVectorsFormat"/>s that have the same name but
        /// different <seealso cref="CompressionMode"/>s.
        /// <p>
        /// <code>chunkSize</code> is the minimum byte size of a chunk of documents.
        /// Higher values of <code>chunkSize</code> should improve the compression
        /// ratio but will require more memory at indexing time and might make document
        /// loading a little slower (depending on the size of your OS cache compared
        /// to the size of your index).
        /// </summary>
        /// <param name="formatName"> the name of the <seealso cref="StoredFieldsFormat"/> </param>
        /// <param name="segmentSuffix"> a suffix to append to files created by this format </param>
        /// <param name="compressionMode"> the <seealso cref="CompressionMode"/> to use </param>
        /// <param name="chunkSize"> the minimum number of bytes of a single chunk of stored documents </param>
        /// <seealso cref= CompressionMode </seealso>
        public CompressingTermVectorsFormat(string formatName, string segmentSuffix, CompressionMode compressionMode, int chunkSize)
        {
            this.FormatName = formatName;
            this.SegmentSuffix = segmentSuffix;
            this.CompressionMode = compressionMode;
            if (chunkSize < 1)
            {
                throw new System.ArgumentException("chunkSize must be >= 1");
            }
            this.ChunkSize = chunkSize;
        }

        public override sealed TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            return new CompressingTermVectorsReader(directory, segmentInfo, SegmentSuffix, fieldInfos, context, FormatName, CompressionMode);
        }

        public override sealed TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            return new CompressingTermVectorsWriter(directory, segmentInfo, SegmentSuffix, context, FormatName, CompressionMode, ChunkSize);
        }

        public override string ToString()
        {
            return this.GetType().Name + "(compressionMode=" + CompressionMode + ", chunkSize=" + ChunkSize + ")";
        }
    }
}