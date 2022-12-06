// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Hi
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
    /// A <see cref="TokenFilter"/> that applies <see cref="HindiNormalizer"/> to normalize the
    /// orthography.
    /// <para>
    /// In some cases the normalization may cause unrelated terms to conflate, so
    /// to prevent terms from being normalized use an instance of
    /// <see cref="Miscellaneous.SetKeywordMarkerFilter"/> or a custom <see cref="TokenFilter"/> that sets
    /// the <see cref="IKeywordAttribute"/> before this <see cref="TokenStream"/>.
    /// </para> 
    /// </summary>
    /// <seealso cref="HindiNormalizer"/>
    public sealed class HindiNormalizationFilter : TokenFilter
    {
        private readonly HindiNormalizer normalizer = new HindiNormalizer();
        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAtt;

        public HindiNormalizationFilter(TokenStream input)
              : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (!keywordAtt.IsKeyword)
                {
                    termAtt.Length = normalizer.Normalize(termAtt.Buffer, termAtt.Length);
                }
                return true;
            }
            return false;
        }
    }
}