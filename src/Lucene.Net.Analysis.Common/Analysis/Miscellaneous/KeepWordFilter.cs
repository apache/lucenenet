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
    /// A <see cref="TokenFilter"/> that only keeps tokens with text contained in the
    /// required words.  This filter behaves like the inverse of <see cref="Core.StopFilter"/>.
    /// 
    /// @since solr 1.3
    /// </summary>
    public sealed class KeepWordFilter : FilteringTokenFilter
    {
        private readonly CharArraySet words;
        private readonly ICharTermAttribute termAtt;

        /// @deprecated enablePositionIncrements=false is not supported anymore as of Lucene 4.4. 
        [Obsolete("enablePositionIncrements=false is not supported anymore as of Lucene 4.4.")]
        public KeepWordFilter(LuceneVersion version, bool enablePositionIncrements, TokenStream @in, CharArraySet words)
            : base(version, enablePositionIncrements, @in)
        {
            this.words = words;
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        /// <summary>
        /// Create a new <see cref="KeepWordFilter"/>.
        /// <para><c>NOTE</c>: The words set passed to this constructor will be directly
        /// used by this filter and should not be modified.
        /// </para>
        /// </summary>
        /// <param name="version"> the Lucene match version </param>
        /// <param name="in">      the <see cref="TokenStream"/> to consume </param>
        /// <param name="words">   the words to keep </param>
        public KeepWordFilter(LuceneVersion version, TokenStream @in, CharArraySet words)
            : base(version, @in)
        {
            this.words = words;
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        protected override bool Accept()
        {
            return words.Contains(termAtt.Buffer, 0, termAtt.Length);
        }
    }
}