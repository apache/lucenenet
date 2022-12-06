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

    using SegmentInfo = Lucene.Net.Index.SegmentInfo;

    /// <summary>
    /// Expert: Controls the format of the
    /// <see cref="SegmentInfo"/> (segment metadata file).
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="SegmentInfo"/>
    public abstract class SegmentInfoFormat
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected SegmentInfoFormat()
        {
        }

        /// <summary>
        /// Returns the <see cref="Codecs.SegmentInfoReader"/> for reading
        /// <see cref="SegmentInfo"/> instances.
        /// </summary>
        public abstract SegmentInfoReader SegmentInfoReader { get; }

        /// <summary>
        /// Returns the <see cref="Codecs.SegmentInfoWriter"/> for writing
        /// <see cref="SegmentInfo"/> instances.
        /// </summary>
        public abstract SegmentInfoWriter SegmentInfoWriter { get; }
    }
}