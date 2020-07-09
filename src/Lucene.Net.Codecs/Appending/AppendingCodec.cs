using System;
using Lucene.Net.Codecs.Lucene40;

namespace Lucene.Net.Codecs.Appending
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
    /// This codec uses an index format that is very similar to <see cref="Lucene40Codec"/> 
    /// but works on append-only outputs, such as plain output streams and 
    /// append-only filesystems.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [Obsolete(
        "This codec is read-only: as the functionality has been folded into the default codec. Its only for convenience to read old segments."
        )]
    [CodecName("Appending")] // LUCENENET specific - using CodecName attribute to ensure the default name passed from subclasses is the same as this class name
    public class AppendingCodec : FilterCodec
    {
        private readonly PostingsFormat _postings = new AppendingPostingsFormat();

        public AppendingCodec() 
            : base(new Lucene40Codec())
        {
        }

        public override PostingsFormat PostingsFormat => _postings;
    }
}

