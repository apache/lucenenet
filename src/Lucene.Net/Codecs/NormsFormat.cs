using System.IO;

namespace Lucene.Net.Codecs
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

    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Encodes/decodes per-document score normalization values.
    /// </summary>
    public abstract class NormsFormat
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected NormsFormat()
        {
        }

        /// <summary>
        /// Returns a <see cref="DocValuesConsumer"/> to write norms to the
        /// index.
        /// </summary>
        public abstract DocValuesConsumer NormsConsumer(SegmentWriteState state);

        /// <summary>
        /// Returns a <see cref="DocValuesProducer"/> to read norms from the index.
        /// <para/>
        /// NOTE: by the time this call returns, it must hold open any files it will
        /// need to use; else, those files may be deleted. Additionally, required files
        /// may be deleted during the execution of this call before there is a chance
        /// to open them. Under these circumstances an <see cref="IOException"/> should be thrown by
        /// the implementation. <see cref="IOException"/> are expected and will automatically cause
        /// a retry of the segment opening logic with the newly revised segments.
        /// </summary>
        public abstract DocValuesProducer NormsProducer(SegmentReadState state);
    }
}