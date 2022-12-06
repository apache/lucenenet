using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;

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
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class ReaderSlice
    {
        /// <summary>
        /// Zero-length <see cref="ReaderSlice"/> array. </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly ReaderSlice[] EMPTY_ARRAY = Arrays.Empty<ReaderSlice>();

        /// <summary>
        /// Document ID this slice starts from. </summary>
        public int Start { get; private set; }

        /// <summary>
        /// Number of documents in this slice. </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Sub-reader index for this slice. </summary>
        public int ReaderIndex { get; private set; }

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