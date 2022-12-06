// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Removes words that are too long or too short from the stream.
    /// <para>
    /// Note: Length is calculated as the number of UTF-16 code units.
    /// </para>
    /// </summary>
    public sealed class LengthFilter : FilteringTokenFilter
    {
        private readonly int min;
        private readonly int max;

        private readonly ICharTermAttribute termAtt;

        [Obsolete("enablePositionIncrements=false is not supported anymore as of Lucene 4.4.")]
        public LengthFilter(LuceneVersion version, bool enablePositionIncrements, TokenStream @in, int min, int max)
            : base(version, enablePositionIncrements, @in)
        {
            if (min < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(min), "minimum length must be greater than or equal to zero"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (min > max)
            {
                throw new ArgumentOutOfRangeException(nameof(max), "maximum length must not be greater than minimum length"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.min = min;
            this.max = max;
            this.termAtt = AddAttribute<ICharTermAttribute>();
        }

        /// <summary>
        /// Create a new <see cref="LengthFilter"/>. This will filter out tokens whose
        /// <see cref="ICharTermAttribute"/> is either too short (<see cref="ICharTermAttribute.Length"/>
        /// &lt; min) or too long (<see cref="ICharTermAttribute.Length"/> &gt; max). </summary>
        /// <param name="version"> the Lucene match version </param>
        /// <param name="in">      the <see cref="TokenStream"/> to consume </param>
        /// <param name="min">     the minimum length </param>
        /// <param name="max">     the maximum length </param>
        public LengthFilter(LuceneVersion version, TokenStream @in, int min, int max)
            : base(version, @in)
        {
            if (min < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(min), "minimum length must be greater than or equal to zero"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (min > max)
            {
                throw new ArgumentOutOfRangeException(nameof(max), "maximum length must not be greater than minimum length"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.min = min;
            this.max = max;
            this.termAtt = AddAttribute<ICharTermAttribute>();
        }

        protected override bool Accept()
        {
            int len = termAtt.Length;
            return (len >= min && len <= max);
        }
    }
}