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
    /// Run OpenNLP chunker. Prerequisite: the <see cref="OpenNLPTokenizer"/> and <see cref="OpenNLPPOSFilter"/> must precede this filter.
    /// Tags terms in the <see cref="ITypeAttribute"/>, replacing the POS tags previously put there by <see cref="OpenNLPPOSFilter"/>.
    /// </summary>
    public sealed class OpenNLPChunkerFilter : TokenFilter
    {
        private readonly IList<AttributeSource> sentenceTokenAttrs = new JCG.List<AttributeSource>();
        private int tokenNum = 0;
        private bool moreTokensAvailable = true;
        private string[] sentenceTerms = null;
        private string[] sentenceTermPOSTags = null;

        private readonly NLPChunkerOp chunkerOp;
        private readonly ITypeAttribute typeAtt;
        private readonly IFlagsAttribute flagsAtt;
        private readonly ICharTermAttribute termAtt;

        public OpenNLPChunkerFilter(TokenStream input, NLPChunkerOp chunkerOp)
            : base(input)
        {
            this.chunkerOp = chunkerOp;
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
            {
                NextSentence();
                if (sentenceTerms is null)
                {
                    Clear();
                    return false;
                }
                AssignTokenTypes(chunkerOp.GetChunks(sentenceTerms, sentenceTermPOSTags, null));
                tokenNum = 0;
            }
            ClearAttributes();
            sentenceTokenAttrs[tokenNum++].CopyTo(this);
            return true;
        }

        private void NextSentence()
        {
            var termList = new JCG.List<string>();
            var posTagList = new JCG.List<string>();
            sentenceTokenAttrs.Clear();
            bool endOfSentence = false;
            while (!endOfSentence && (moreTokensAvailable = m_input.IncrementToken()))
            {
                termList.Add(termAtt.ToString());
                posTagList.Add(typeAtt.Type);
                endOfSentence = 0 != (flagsAtt.Flags & OpenNLPTokenizer.EOS_FLAG_BIT);
                sentenceTokenAttrs.Add(m_input.CloneAttributes());
            }
            sentenceTerms = termList.Count > 0 ? termList.ToArray() : null;
            sentenceTermPOSTags = posTagList.Count > 0 ? posTagList.ToArray() : null;
        }

        private void AssignTokenTypes(string[] tags)
        {
            for (int i = 0; i < tags.Length; ++i)
            {
                sentenceTokenAttrs[i].GetAttribute<ITypeAttribute>().Type = tags[i];
            }
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
            sentenceTerms = null;
            sentenceTermPOSTags = null;
            tokenNum = 0;
        }
    }
}
