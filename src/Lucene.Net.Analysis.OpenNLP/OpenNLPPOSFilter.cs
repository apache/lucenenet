// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.OpenNlp.Tools;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.OpenNlp
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
    /// Run OpenNLP POS tagger.  Tags all terms in the <see cref="ITypeAttribute"/>.
    /// </summary>
    public sealed class OpenNLPPOSFilter : TokenFilter
    {
        private readonly IList<AttributeSource> sentenceTokenAttrs = new JCG.List<AttributeSource>();
        private string[] tags = null;
        private int tokenNum = 0;
        private bool moreTokensAvailable = true;

        private readonly NLPPOSTaggerOp posTaggerOp;
        private readonly ITypeAttribute typeAtt;
        private readonly IFlagsAttribute flagsAtt;
        private readonly ICharTermAttribute termAtt;

        public OpenNLPPOSFilter(TokenStream input, NLPPOSTaggerOp posTaggerOp)
            : base(input)
        {
            this.posTaggerOp = posTaggerOp;
            this.typeAtt = AddAttribute<ITypeAttribute>();
            this.flagsAtt = AddAttribute<IFlagsAttribute>();
            this.termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (!moreTokensAvailable)
            {
                Clear();
                return false;
            }
            if (tokenNum == sentenceTokenAttrs.Count)
            { // beginning of stream, or previous sentence exhausted
                string[] sentenceTokens = NextSentence();
                if (sentenceTokens is null)
                {
                    Clear();
                    return false;
                }
                tags = posTaggerOp.GetPOSTags(sentenceTokens);
                tokenNum = 0;
            }
            ClearAttributes();
            sentenceTokenAttrs[tokenNum].CopyTo(this);
            typeAtt.Type = tags[tokenNum++];
            return true;
        }

        private string[] NextSentence()
        {
            var termList = new JCG.List<string>();
            sentenceTokenAttrs.Clear();
            bool endOfSentence = false;
            while (!endOfSentence && (moreTokensAvailable = m_input.IncrementToken()))
            {
                termList.Add(termAtt.ToString());
                endOfSentence = 0 != (flagsAtt.Flags & OpenNLPTokenizer.EOS_FLAG_BIT);
                sentenceTokenAttrs.Add(m_input.CloneAttributes());
            }
            return termList.Count > 0 ? termList.ToArray() : null;
        }

        public override void Reset()
        {
            base.Reset();
            moreTokensAvailable = true;
            Clear();
        }

        private void Clear()
        {
            sentenceTokenAttrs.Clear();
            tags = null;
            tokenNum = 0;
        }
    }
}
