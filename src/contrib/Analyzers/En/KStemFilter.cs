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

using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.En
{
    public class KStemFilter : TokenFilter
    {
        private readonly KStemmer stemmer = new KStemmer();
        private readonly ICharTermAttribute termAttribute;
        private readonly IKeywordAttribute keywordAtt;

        public KStemFilter(TokenStream input)
            : base(input)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (!input.IncrementToken())
            {
                return false;
            }

            var term = termAttribute.Buffer;
            var len = termAttribute.Length;
            if ((!keywordAtt.IsKeyword) && stemmer.Stem(term, len))
            {
                termAttribute.SetEmpty().Append(stemmer.AsCharSequence);
            }

            return true;
        }
    }
}
