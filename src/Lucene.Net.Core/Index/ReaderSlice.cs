namespace Lucene.Net.Index
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
    /// Subreader slice from a parent composite reader.
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class ReaderSlice
    {
        /// <summary>
        /// Zero-length {@code ReaderSlice} array. </summary>
        public static readonly ReaderSlice[] EMPTY_ARRAY = new ReaderSlice[0];

        /// <summary>
        /// Document ID this slice starts from. </summary>
        public readonly int Start; // LUCENENET TODO: Make property

        /// <summary>
        /// Number of documents in this slice. </summary>
        public readonly int Length; // LUCENENET TODO: Make property

        /// <summary>
        /// Sub-reader index for this slice. </summary>
        public readonly int ReaderIndex; // LUCENENET TODO: Make property

        /// <summary>
        /// Sole constructor. </summary>
        public ReaderSlice(int start, int length, int readerIndex)
        {
            this.Start = start;
            this.Length = length;
            this.ReaderIndex = readerIndex;
        }

        public override string ToString()
        {
            return "slice start=" + Start + " length=" + Length + " readerIndex=" + ReaderIndex;
        }
    }
}