using Lucene.Net.Analysis.TokenAttributes;
using System.Collections.Generic;

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
        private readonly ICharTermAttribute TermAtt;
        private readonly IPositionIncrementAttribute PosIncAtt;
        private readonly IOffsetAttribute OffsetAtt;

        private int InsertUpto;

        internal TrivialLookaheadFilter(TokenStream input)
            : base(input)
        {
            TermAtt = AddAttribute<ICharTermAttribute>();
            PosIncAtt = AddAttribute<IPositionIncrementAttribute>();
            OffsetAtt = AddAttribute<IOffsetAttribute>();
        }

        protected internal override TestPosition NewPosition()
        {
            return new TestPosition();
        }

        public override bool IncrementToken()
        {
            // At the outset, getMaxPos is -1. So we'll peek. When we reach the end of the sentence and go to the
            // first token of the next sentence, maxPos will be the prev sentence's end token, and we'll go again.
            if (positions.MaxPos < OutputPos)
            {
                PeekSentence();
            }

            return NextToken();
        }

        public override void Reset()
        {
            base.Reset();
            InsertUpto = -1;
        }

        protected internal override void AfterPosition()
        {
            if (InsertUpto < OutputPos)
            {
                InsertToken();
                // replace term with 'improved' term.
                ClearAttributes();
                TermAtt.SetEmpty();
                PosIncAtt.PositionIncrement = 0;
                TermAtt.Append(((TestPosition)positions.Get(OutputPos)).Fact);
                OffsetAtt.SetOffset(positions.Get(OutputPos).StartOffset, positions.Get(OutputPos + 1).EndOffset);
                InsertUpto = OutputPos;
            }
        }

        private void PeekSentence()
        {
            IList<string> facts = new List<string>();
            bool haveSentence = false;
            do
            {
                if (PeekToken())
                {
                    string term = new string(TermAtt.Buffer, 0, TermAtt.Length);
                    facts.Add(term + "-huh?");
                    if (".".Equals(term))
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
            for (int x = 0; x < facts.Count; x++)
            {
                // sentenceTokens is just relative to sentence, positions is absolute.
                ((TestPosition)positions.Get(OutputPos + x)).Fact = facts[x];
            }
        }
    }
}