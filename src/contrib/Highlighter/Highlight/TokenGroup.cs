/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Search.Highlight
{
    /// <summary> One, or several overlapping tokens, along with the score(s) and the
    /// scope of the original text
    /// </summary>
    public class TokenGroup
    {
        private static readonly int MAX_NUM_TOKENS_PER_GROUP = 50;

        private Token[] tokens = new Token[MAX_NUM_TOKENS_PER_GROUP];
        private float[] scores = new float[MAX_NUM_TOKENS_PER_GROUP];
        internal int numTokens = 0;
        internal int startOffset = 0;
        internal int endOffset = 0;
        private float tot;
        internal int matchStartOffset, matchEndOffset;

        private IOffsetAttribute offsetAtt;
        private ICharTermAttribute termAtt;

        public TokenGroup(TokenStream tokenStream)
        {
            offsetAtt = tokenStream.AddAttribute<IOffsetAttribute>();
            termAtt = tokenStream.AddAttribute<ICharTermAttribute>();
        }

        protected internal void AddToken(float score)
        {
            if (numTokens < MAX_NUM_TOKENS_PER_GROUP)
            {
                int termStartOffset = offsetAtt.StartOffset;
                int termEndOffset = offsetAtt.EndOffset;
                if (numTokens == 0)
                {
                    startOffset = matchStartOffset = termStartOffset;
                    endOffset = matchEndOffset = termEndOffset;
                    tot += score;
                }
                else
                {
                    startOffset = Math.Min(startOffset, termStartOffset);
                    endOffset = Math.Max(endOffset, termEndOffset);
                    if (score > 0)
                    {
                        if (tot == 0)
                        {
                            matchStartOffset = offsetAtt.StartOffset;
                            matchEndOffset = offsetAtt.EndOffset;
                        }
                        else
                        {
                            matchStartOffset = Math.Min(matchStartOffset, termStartOffset);
                            matchEndOffset = Math.Max(matchEndOffset, termEndOffset);
                        }
                        tot += score;
                    }
                }
                Token token = new Token(termStartOffset, termEndOffset);
                token.SetEmpty().Append(termAtt);
                tokens[numTokens] = token;
                scores[numTokens] = score;
                numTokens++;
            }
        }

        protected internal bool IsDistinct()
        {
            return offsetAtt.StartOffset >= endOffset;
        }

        protected internal void Clear()
        {
            numTokens = 0;
            tot = 0;
        }


        /// <summary>
        /// the "n"th token
        /// </summary>
        /// <param name="index">a value between 0 and numTokens -1</param>
        public Token GetToken(int index)
        {
            return tokens[index];
        }

        /// <summary>
        /// the "n"th score
        /// </summary>
        /// <param name="index">a value between 0 and numTokens -1</param>
        public float GetScore(int index)
        {
            return scores[index];
        }

        /// <summary>
        /// the end position in the original text
        /// </summary>
        public int EndOffset
        {
            get { return endOffset; }
        }

        /// <summary>
        /// The start position in the original text
        /// </summary>
        public int StartOffset
        {
            get { return startOffset; }
        }

        /// <summary>
        /// All tokens' scores summed up
        /// </summary>
        public float TotalScore
        {
            get { return tot; }
        }
    }
}
