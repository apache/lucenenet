namespace Lucene.Net.Codecs.SimpleText
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

    using SegmentReadState = Index.SegmentReadState;
    using SegmentWriteState = Index.SegmentWriteState;

    /// <summary>
    /// Plain-text norms format.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextNormsFormat : NormsFormat
    {
        private const string NORMS_SEG_EXTENSION = "len";

        public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
        {
            return new SimpleTextNormsConsumer(state);
        }

        public override DocValuesProducer NormsProducer(SegmentReadState state)
        {
            return new SimpleTextNormsProducer(state);
        }

        /// <summary>
        /// Reads plain-text norms.
        /// <para>
        /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
        /// </para>
        /// @lucene.experimental
        /// </summary>
        public class SimpleTextNormsProducer : SimpleTextDocValuesReader
        {
            public SimpleTextNormsProducer(SegmentReadState state) 
                : base(state, NORMS_SEG_EXTENSION)
            {
                // All we do is change the extension from .dat -> .len;
                // otherwise this is a normal simple doc values file:
            }
        }

        /// <summary>
        /// Writes plain-text norms.
        /// <para>
        /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
        /// </para>
        /// @lucene.experimental
        /// </summary>
        public class SimpleTextNormsConsumer : SimpleTextDocValuesWriter
        {
            public SimpleTextNormsConsumer(SegmentWriteState state) 
                : base(state, NORMS_SEG_EXTENSION)
            {
                // All we do is change the extension from .dat -> .len;
                // otherwise this is a normal simple doc values file:
            }
        }
    }
}