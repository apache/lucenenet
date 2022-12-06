// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System.Text.RegularExpressions;

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
    /// Marks terms as keywords via the <see cref="IKeywordAttribute"/>. Each token
    /// that matches the provided pattern is marked as a keyword by setting
    /// <see cref="IKeywordAttribute.IsKeyword"/> to <c>true</c>.
    /// </summary>
    public sealed class PatternKeywordMarkerFilter : KeywordMarkerFilter
    {
        private readonly ICharTermAttribute termAtt;
        private Match matcher;
        private readonly Regex pattern;

        /// <summary>
        /// Create a new <see cref="PatternKeywordMarkerFilter"/>, that marks the current
        /// token as a keyword if the tokens term buffer matches the provided
        /// <see cref="Regex"/> via the <see cref="IKeywordAttribute"/>.
        /// </summary>
        /// <param name="in">
        ///          <see cref="TokenStream"/> to filter </param>
        /// <param name="pattern">
        ///          the pattern to apply to the incoming term buffer
        ///  </param>
        public PatternKeywordMarkerFilter(TokenStream @in, Regex pattern)
            : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();

            this.matcher = pattern.Match("");
            this.pattern = pattern;
        }

        protected override bool IsKeyword()
        {
            matcher = pattern.Match(termAtt.ToString()); 
            return matcher.Success;
        }
    }
}