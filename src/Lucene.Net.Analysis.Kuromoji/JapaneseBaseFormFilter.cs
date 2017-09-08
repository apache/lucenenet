using Lucene.Net.Analysis.Ja.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Ja
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
    /// Replaces term text with the <see cref="IBaseFormAttribute"/>.
    /// <para/>
    /// This acts as a lemmatizer for verbs and adjectives.
    /// To prevent terms from being stemmed use an instance of
    /// <see cref="Miscellaneous.SetKeywordMarkerFilter"/> or a custom <see cref="TokenFilter"/> that sets
    /// the <see cref="IKeywordAttribute"/> before this <see cref="TokenStream"/>.
    /// </summary>
    public sealed class JapaneseBaseFormFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly IBaseFormAttribute basicFormAtt;
        private readonly IKeywordAttribute keywordAtt;

        public JapaneseBaseFormFilter(TokenStream input)
            : base(input)
        {
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.basicFormAtt = AddAttribute<IBaseFormAttribute>();
            this.keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (!keywordAtt.IsKeyword)
                {
                    string baseForm = basicFormAtt.GetBaseForm();
                    if (baseForm != null)
                    {
                        termAtt.SetEmpty().Append(baseForm);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
