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

    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Lucene 4.2 score normalization format.
    /// <p>
    /// NOTE: this uses the same format as <seealso cref="Lucene42DocValuesFormat"/>
    /// Numeric DocValues, but with different file extensions, and passing
    /// <seealso cref="PackedInts#FASTEST"/> for uncompressed encoding: trading off
    /// space for performance.
    /// <p>
    /// Files:
    /// <ul>
    ///   <li><tt>.nvd</tt>: DocValues data</li>
    ///   <li><tt>.nvm</tt>: DocValues metadata</li>
    /// </ul> </summary>
    /// <seealso cref= Lucene42DocValuesFormat </seealso>
    public class Lucene42NormsFormat : NormsFormat
    {
        internal readonly float AcceptableOverheadRatio;

        /// <summary>
        /// Calls {@link #Lucene42NormsFormat(float)
        /// Lucene42DocValuesFormat(PackedInts.FASTEST)}
        /// </summary>
        public Lucene42NormsFormat()
            : this(PackedInts.FASTEST)
        {
            // note: we choose FASTEST here (otherwise our norms are half as big but 15% slower than previous lucene)
        }

        /// <summary>
        /// Creates a new Lucene42DocValuesFormat with the specified
        /// <code>acceptableOverheadRatio</code> for NumericDocValues. </summary>
        /// <param name="acceptableOverheadRatio"> compression parameter for numerics.
        ///        Currently this is only used when the number of unique values is small.
        ///
        /// @lucene.experimental </param>
        public Lucene42NormsFormat(float acceptableOverheadRatio)
        {
            this.AcceptableOverheadRatio = acceptableOverheadRatio;
        }

        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            return new Lucene42NormsConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION, AcceptableOverheadRatio);
        }

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