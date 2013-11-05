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


namespace Lucene.Net.Analysis.AR
{


    /*
     * A <see cref="TokenFilter"/> that applies <see cref="ArabicStemmer"/> to stem Arabic words..
     * 
     */

    public class ArabicStemFilter : TokenFilter
    {
        private readonly ArabicStemmer _stemmer;
        private readonly ICharTermAttribute _termAtt; // AddAttribute<>() must be called in constructor 
        private readonly IKeywordAttribute _keywordAtt; // because it can't be called in the member initializer

        public ArabicStemFilter(TokenStream input) : base(input)
        {
            _stemmer = new ArabicStemmer();
            _termAtt = AddAttribute<ICharTermAttribute>();
            _keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                if (!_keywordAtt.IsKeyword)
                {
                    var newLen = _stemmer.Stem(_termAtt.Buffer, _termAtt.Length);
                    _termAtt.SetLength(newLen);
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