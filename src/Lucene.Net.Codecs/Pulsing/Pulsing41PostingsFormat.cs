using Lucene.Net.Codecs.Lucene41;

namespace Lucene.Net.Codecs.Pulsing
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
    /// Concrete pulsing implementation over <see cref="Lucene41PostingsFormat"/>.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    [PostingsFormatName("Pulsing41")] // LUCENENET specific - using PostingsFormatName attribute to ensure the default name passed from subclasses is the same as this class name
    public class Pulsing41PostingsFormat : PulsingPostingsFormat
    {
        /// <summary>Inlines docFreq=1 terms, otherwise uses the normal "Lucene41" format.</summary>
        public Pulsing41PostingsFormat() 
            : this(1)
        {
        }

        /// <summary>Inlines docFreq=<paramref name="freqCutoff"/> terms, otherwise uses the normal "Lucene41" format.</summary>
        public Pulsing41PostingsFormat(int freqCutoff) 
            : this(freqCutoff, BlockTreeTermsWriter.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
        {
        }

        /// <summary>Inlines docFreq=<paramref name="freqCutoff"/> terms, otherwise uses the normal "Lucene41" format.</summary>
        public Pulsing41PostingsFormat(int freqCutoff, int minBlockSize, int maxBlockSize) 
            : base(new Lucene41PostingsBaseFormat(), freqCutoff, minBlockSize, maxBlockSize)
        {
        }
    }
}
