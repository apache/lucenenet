using Lucene.Net.Index;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Memory
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
    /// In-memory docvalues format </summary>
    public class MemoryDocValuesFormat : DocValuesFormat
    {
        /// <summary>Maximum length for each binary doc values field. </summary>
        public static readonly int MAX_BINARY_FIELD_LENGTH = (1 << 15) - 2;

        internal readonly float acceptableOverheadRatio;

        /// <summary>
        /// Calls {@link #MemoryDocValuesFormat(float) 
        /// MemoryDocValuesFormat(PackedInts.DEFAULT)} 
        /// </summary>
        public MemoryDocValuesFormat() 
            : this(PackedInt32s.DEFAULT)
        {
        }

        /// <summary>
        /// Creates a new MemoryDocValuesFormat with the specified
        /// <code>acceptableOverheadRatio</code> for NumericDocValues. </summary>
        /// <param name="acceptableOverheadRatio"> compression parameter for numerics. 
        ///        Currently this is only used when the number of unique values is small.
        ///        
        /// @lucene.experimental </param>
        public MemoryDocValuesFormat(float acceptableOverheadRatio) 
            : base()
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new MemoryDocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION,
                acceptableOverheadRatio);
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new MemoryDocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION);
        }

        internal const string DATA_CODEC = "MemoryDocValuesData";
        internal const string DATA_EXTENSION = "mdvd";
        internal const string METADATA_CODEC = "MemoryDocValuesMetadata";
        internal const string METADATA_EXTENSION = "mdvm";
    }
}