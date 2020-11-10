using System.Runtime.CompilerServices;

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

    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Lucene 4.2 score normalization format.
    /// <para/>
    /// NOTE: this uses the same format as <see cref="Lucene42DocValuesFormat"/>
    /// Numeric DocValues, but with different file extensions, and passing
    /// <see cref="PackedInt32s.FASTEST"/> for uncompressed encoding: trading off
    /// space for performance.
    /// <para/>
    /// Files:
    /// <list type="bullet">
    ///   <item><description><c>.nvd</c>: DocValues data</description></item>
    ///   <item><description><c>.nvm</c>: DocValues metadata</description></item>
    /// </list>
    /// </summary>
    /// <seealso cref="Lucene42DocValuesFormat"/>
    public class Lucene42NormsFormat : NormsFormat
    {
        internal readonly float acceptableOverheadRatio;

        /// <summary>
        /// Calls <c>Lucene42DocValuesFormat(PackedInt32s.FASTEST)</c> (<see cref="Lucene42NormsFormat(float)"/>).
        /// </summary>
        public Lucene42NormsFormat()
            : this(PackedInt32s.FASTEST)
        {
            // note: we choose FASTEST here (otherwise our norms are half as big but 15% slower than previous lucene)
        }

        /// <summary>
        /// Creates a new <see cref="Lucene42DocValuesFormat"/> with the specified
        /// <paramref name="acceptableOverheadRatio"/> for <see cref="Index.NumericDocValues"/>. 
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <param name="acceptableOverheadRatio"> Compression parameter for numerics.
        ///        Currently this is only used when the number of unique values is small.</param>
        public Lucene42NormsFormat(float acceptableOverheadRatio)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            return new Lucene42NormsConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION, acceptableOverheadRatio);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override DocValuesProducer NormsProducer(SegmentReadState state)
        {
            return new Lucene42DocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION);
        }

        private const string DATA_CODEC = "Lucene41NormsData";
        private const string DATA_EXTENSION = "nvd";
        private const string METADATA_CODEC = "Lucene41NormsMetadata";
        private const string METADATA_EXTENSION = "nvm";
    }
}