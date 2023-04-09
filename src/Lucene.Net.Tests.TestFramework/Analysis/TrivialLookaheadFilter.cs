// Lucene version compatibility level 8.2.0

using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis
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
    /// Simple example of a filter that seems to show some problems with LookaheadTokenFilter.
    /// </summary>
    public sealed class TrivialLookaheadFilter : LookaheadTokenFilter<TestPosition>
    {
        private ICharTermAttribute termAtt;
        private IPositionIncrementAttribute posIncAtt;
        private IOffsetAttribute offsetAtt;

        private int insertUpto;

        // LUCENENET specific - removed NewPosition override and using factory instead
        public TrivialLookaheadFilter(TokenStream input)
            : base(input, RollingBufferItemFactory<TestPosition>.Default)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        public override bool IncrementToken()
        {
            // At the outset, getMaxPos is -1. So we'll peek. When we reach the end of the sentence and go to the
            // first token of the next sentence, maxPos will be the prev sentence's end token, and we'll go again.
            if (m_positions.MaxPos < m_outputPos)
            {
                PeekSentence();
            }

            return NextToken();
        }

        public override void Reset()
        {
            base.Reset();
            insertUpto = -1;
        }

        protected override void AfterPosition()
        {
            if (insertUpto < m_outputPos)
            {
                InsertToken();
                // replace term with 'improved' term.
                ClearAttributes();
                termAtt.SetEmpty();
                posIncAtt.PositionIncrement = (0);
                termAtt.Append(m_positions.Get(m_outputPos).Fact);
                offsetAtt.SetOffset(m_positions.Get(m_outputPos).StartOffset,
                                    m_positions.Get(m_outputPos + 1).EndOffset);
                insertUpto = m_outputPos;
            }
        }

        private void PeekSentence()
        {
            var facts = new JCG.List<string>();
            bool haveSentence = false;
            do
            {
                if (PeekToken())
                {

                    String term = new String(termAtt.Buffer, 0, termAtt.Length);
                    facts.Add(term + "-huh?");
                    if (".".equals(term))
                    {
                        haveSentence = true;
                    }

                }
                else
                {
                    haveSentence = true;
                }

            } while (!haveSentence);

            // attach the (now disambiguated) analyzed tokens to the positions.
            for (int x = 0; x < facts.size(); x++)
            {
                // sentenceTokens is just relative to sentence, positions is absolute.
                m_positions.Get(m_outputPos + x).Fact = (facts[x]);
            }
        }
    }
}
