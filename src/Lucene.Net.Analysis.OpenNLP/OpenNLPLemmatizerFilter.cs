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
    /// Runs OpenNLP dictionary-based and/or MaxEnt lemmatizers.
    /// <para/>
    /// Both a dictionary-based lemmatizer and a MaxEnt lemmatizer are supported,
    /// via the "dictionary" and "lemmatizerModel" params, respectively.
    /// If both are configured, the dictionary-based lemmatizer is tried first,
    /// and then the MaxEnt lemmatizer is consulted for out-of-vocabulary tokens.
    /// <para/>
    /// The dictionary file must be encoded as UTF-8, with one entry per line,
    /// in the form <c>word[tab]lemma[tab]part-of-speech</c>
    /// </summary>
    public class OpenNLPLemmatizerFilter : TokenFilter
    {
        private readonly NLPLemmatizerOp lemmatizerOp;
        private readonly ICharTermAttribute termAtt;
        private readonly ITypeAttribute typeAtt;
        private readonly IKeywordAttribute keywordAtt;
        private readonly IFlagsAttribute flagsAtt;
        private readonly IList<AttributeSource> sentenceTokenAttrs = new JCG.List<AttributeSource>(); // LUCENENET: marked readonly
        private IEnumerator<AttributeSource> sentenceTokenAttrsIter = null;
        private bool moreTokensAvailable = true;
        private string[] sentenceTokens = null;     // non-keyword tokens
        private string[] sentenceTokenTypes = null; // types for non-keyword tokens
        private string[] lemmas = null;             // lemmas for non-keyword tokens
        private int lemmaNum = 0;                   // lemma counter

        public OpenNLPLemmatizerFilter(TokenStream input, NLPLemmatizerOp lemmatizerOp)
            : base(input)
        {
            this.lemmatizerOp = lemmatizerOp;
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.typeAtt = AddAttribute<ITypeAttribute>();
            this.keywordAtt = AddAttribute<IKeywordAttribute>();
            this.flagsAtt = AddAttribute<IFlagsAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            if (!moreTokensAvailable)
            {
                Clear();
                return false;
            }
            if (sentenceTokenAttrsIter is null || !sentenceTokenAttrsIter.MoveNext())
            {
                NextSentence();
                if (sentenceTokens is null)
                { // zero non-keyword tokens
                    Clear();
                    return false;
                }
                lemmas = lemmatizerOp.Lemmatize(sentenceTokens, sentenceTokenTypes);
                lemmaNum = 0;
                sentenceTokenAttrsIter = sentenceTokenAttrs.GetEnumerator();
                sentenceTokenAttrsIter.MoveNext();
            }
            ClearAttributes();
            sentenceTokenAttrsIter.Current.CopyTo(this);
            if (!keywordAtt.IsKeyword)
            {
                termAtt.SetEmpty().Append(lemmas[lemmaNum++]);
            }
            return true;

        }

        private void NextSentence()
        {
            var tokenList = new JCG.List<string>();
            var typeList = new JCG.List<string>();
            sentenceTokenAttrs.Clear();
            bool endOfSentence = false;
            while (!endOfSentence && (moreTokensAvailable = m_input.IncrementToken()))
            {
                if (!keywordAtt.IsKeyword)
                {
                    tokenList.Add(termAtt.ToString());
                    typeList.Add(typeAtt.Type);
                }
                endOfSentence = 0 != (flagsAtt.Flags & OpenNLPTokenizer.EOS_FLAG_BIT);
                sentenceTokenAttrs.Add(m_input.CloneAttributes());
            }
            sentenceTokens = tokenList.Count > 0 ? tokenList.ToArray() : null;
            sentenceTokenTypes = typeList.Count > 0 ? typeList.ToArray() : null;
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
            sentenceTokenAttrsIter?.Dispose();
            sentenceTokenAttrsIter = null;
            sentenceTokens = null;
            sentenceTokenTypes = null;
            lemmas = null;
            lemmaNum = 0;
        }

        /// <summary>
        /// Releases resources used by the <see cref="OpenNLPLemmatizerFilter"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>

        // LUCENENET specific
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    sentenceTokenAttrsIter?.Dispose();
                    sentenceTokenAttrsIter = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
