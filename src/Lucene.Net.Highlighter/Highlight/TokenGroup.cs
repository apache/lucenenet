using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Search.Highlight
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
    /// One, or several overlapping tokens, along with the score(s) and the scope of
    /// the original text
    /// </summary>
    public class TokenGroup
    {
        private const int MAX_NUM_TOKENS_PER_GROUP = 50;

        internal Token[] tokens = new Token[MAX_NUM_TOKENS_PER_GROUP];
        internal float[] scores = new float[MAX_NUM_TOKENS_PER_GROUP];

        internal int MatchStartOffset { get; set; }
        internal int MatchEndOffset { get; set; }

        /// <summary>
        /// the number of tokens in this group
        /// </summary>
        public virtual int NumTokens { get; internal set; } = 0;

        /// <summary>
        /// the start position in the original text
        /// </summary>
        public virtual int StartOffset { get; internal set; } = 0;

        /// <summary>
        /// the end position in the original text
        /// </summary>
        public virtual int EndOffset { get; private set; } = 0;

        /// <summary>
        /// all tokens' scores summed up
        /// </summary>
        public virtual float TotalScore { get; private set; }

        private readonly IOffsetAttribute offsetAtt; // LUCENENET: marked readonly
        private readonly ICharTermAttribute termAtt; // LUCENENET: marked readonly

        public TokenGroup(TokenStream tokenStream)
        {
            offsetAtt = tokenStream.AddAttribute<IOffsetAttribute>();
            termAtt = tokenStream.AddAttribute<ICharTermAttribute>();
        }

        internal void AddToken(float score)
        {
            if (NumTokens < MAX_NUM_TOKENS_PER_GROUP)
            {
                int termStartOffset = offsetAtt.StartOffset;
                int termEndOffset = offsetAtt.EndOffset;
                if (NumTokens == 0)
                {
                    StartOffset = MatchStartOffset = termStartOffset;
                    EndOffset = MatchEndOffset = termEndOffset;
                    TotalScore += score;
                }
                else
                {
                    StartOffset = Math.Min(StartOffset, termStartOffset);
                    EndOffset = Math.Max(EndOffset, termEndOffset);
                    if (score > 0)
                    {
                        if (TotalScore == 0)
                        {
                            MatchStartOffset = termStartOffset;
                            MatchEndOffset = termEndOffset;
                        }
                        else
                        {
                            MatchStartOffset = Math.Min(MatchStartOffset, termStartOffset);
                            MatchEndOffset = Math.Max(MatchEndOffset, termEndOffset);
                        }
                        TotalScore += score;
                    }
                }
                Token token = new Token(termStartOffset, termEndOffset);
                token.SetEmpty().Append(termAtt);
                tokens[NumTokens] = token;
                scores[NumTokens] = score;
                NumTokens++;
            }
        }

        internal bool IsDistinct()
        {
            return offsetAtt.StartOffset >= EndOffset;
        }

        internal void Clear()
        {
            NumTokens = 0;
            TotalScore = 0;
        }

        /// <summary>
        /// the "n"th token
        /// </summary>
        /// <param name="index">a value between 0 and numTokens -1</param>
        public virtual Token GetToken(int index)
        {
            return tokens[index];
        }

        /// <summary>
        /// the "n"th score
        /// </summary>
        /// <param name="index">a value between 0 and numTokens -1</param>
        public virtual float GetScore(int index)
        {
            return scores[index];
        }
    }
}
