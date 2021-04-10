using System;
using System.Runtime.CompilerServices;
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
    /// A <see cref="TermVectorsFormat"/> that compresses chunks of documents together in
    /// order to improve the compression ratio.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class CompressingTermVectorsFormat : TermVectorsFormat
    {
        private readonly string formatName;
        private readonly string segmentSuffix;
        private readonly CompressionMode compressionMode;
        private readonly int chunkSize;

        /// <summary>
        /// Create a new <see cref="CompressingTermVectorsFormat"/>.
        /// <para/>
        /// <paramref name="formatName"/> is the name of the format. this name will be used
        /// in the file formats to perform
        /// codec header checks (<see cref="CodecUtil.CheckHeader(Lucene.Net.Store.DataInput, string, int, int)"/>).
        /// <para/>
        /// The <paramref name="compressionMode"/> parameter allows you to choose between
        /// compression algorithms that have various compression and decompression
        /// speeds so that you can pick the one that best fits your indexing and
        /// searching throughput. You should never instantiate two
        /// <see cref="CompressingTermVectorsFormat"/>s that have the same name but
        /// different <see cref="CompressionMode"/>s.
        /// <para/>
        /// <paramref name="chunkSize"/> is the minimum byte size of a chunk of documents.
        /// Higher values of <paramref name="chunkSize"/> should improve the compression
        /// ratio but will require more memory at indexing time and might make document
        /// loading a little slower (depending on the size of your OS cache compared
        /// to the size of your index).
        /// </summary>
        /// <param name="formatName"> The name of the <see cref="StoredFieldsFormat"/>. </param>
        /// <param name="segmentSuffix"> A suffix to append to files created by this format. </param>
        /// <param name="compressionMode"> The <see cref="CompressionMode"/> to use. </param>
        /// <param name="chunkSize"> The minimum number of bytes of a single chunk of stored documents. </param>
        /// <seealso cref="CompressionMode"/>
        public CompressingTermVectorsFormat(string formatName, string segmentSuffix, CompressionMode compressionMode, int chunkSize)
        {
            this.formatName = formatName;
            this.segmentSuffix = segmentSuffix;
            this.compressionMode = compressionMode;
            if (chunkSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "chunkSize must be >= 1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.chunkSize = chunkSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed TermVectorsReader VectorsReader(Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
        {
            return new CompressingTermVectorsReader(directory, segmentInfo, segmentSuffix, fieldInfos, context, formatName, compressionMode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo segmentInfo, IOContext context)
        {
            return new CompressingTermVectorsWriter(directory, segmentInfo, segmentSuffix, context, formatName, compressionMode, chunkSize);
        }

        public override string ToString()
        {
            return this.GetType().Name + "(compressionMode=" + compressionMode + ", chunkSize=" + chunkSize + ")";
        }
    }
}