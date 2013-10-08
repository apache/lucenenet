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

using System.Collections.Generic;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.BR
{
    public sealed class BrazilianStemFilter : TokenFilter
    {
        private BrazilianStemmer _stemmer = new BrazilianStemmer();
        private ISet<string> _exclusions = null;
        private readonly CharTermAttribute _termAtt;
        private readonly KeywordAttribute _keywordAtt;

        public BrazilianStemFilter(TokenStream input) : base(input)
        {
            _termAtt = AddAttribute<CharTermAttribute>();
            _keywordAtt = AddAttribute<KeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                var term = _termAtt.ToString();
                // Check the exclusion table.
                if (!_keywordAtt.IsKeyword() && (_exclusions == null || !_exclusions.Contains(term)))
                {
                    var s = _stemmer.Stem(term);
                    // If not stemmed, don't waste the time adjusting the token.
                    if ((s != null) && !s.Equals(term))
                        _termAtt.SetEmpty().Append(s);
                }
                return true;
            }
            return false;
        }
    }
}
