using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene41
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
    /// Provides a <see cref="Codecs.PostingsReaderBase"/> and 
    /// <see cref="Codecs.PostingsWriterBase"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    // TODO: should these also be named / looked up via SPI?
    public sealed class Lucene41PostingsBaseFormat : PostingsBaseFormat
    {
        /// <summary>
        /// Sole constructor. </summary>
        public Lucene41PostingsBaseFormat()
            : base("Lucene41")
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override PostingsReaderBase PostingsReaderBase(SegmentReadState state)
        {
            return new Lucene41PostingsReader(state.Directory, state.FieldInfos, state.SegmentInfo, state.Context, state.SegmentSuffix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override PostingsWriterBase PostingsWriterBase(SegmentWriteState state)
        {
            return new Lucene41PostingsWriter(state);
        }
    }
}