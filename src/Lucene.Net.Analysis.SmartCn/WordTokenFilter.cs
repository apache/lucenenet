// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.Cn.Smart.Hhmm;
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Cn.Smart
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
    /// A <see cref="TokenFilter"/> that breaks sentences into words.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [Obsolete("Use HMMChineseTokenizer instead.")]
    public sealed class WordTokenFilter : TokenFilter
    {
        private readonly WordSegmenter wordSegmenter; // LUCENENET: marked readonly

        private IEnumerator<SegToken> tokenIter;

        private IList<SegToken> tokenBuffer;

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly ITypeAttribute typeAtt;

        private int tokStart; // only used if the length changed before this filter
        private int tokEnd; // only used if the length changed before this filter
        private bool hasIllegalOffsets; // only if the length changed before this filter

        /// <summary>
        /// Construct a new <see cref="WordTokenFilter"/>.
        /// </summary>
        /// <param name="input"><see cref="TokenStream"/> of sentences.</param>
        public WordTokenFilter(TokenStream input)
            : base(input)
        {
            this.wordSegmenter = new WordSegmenter();
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.typeAtt = AddAttribute<ITypeAttribute>();
        }

        public override bool IncrementToken()
        {
            if (tokenIter is null || !tokenIter.MoveNext())
            {
                // there are no remaining tokens from the current sentence... are there more sentences?
                if (m_input.IncrementToken())
                {
                    tokStart = offsetAtt.StartOffset;
                    tokEnd = offsetAtt.EndOffset;
                    // if length by start + end offsets doesn't match the term text then assume
                    // this is a synonym and don't adjust the offsets.
                    hasIllegalOffsets = (tokStart + termAtt.Length) != tokEnd;
                    // a new sentence is available: process it.
                    tokenBuffer = wordSegmenter.SegmentSentence(termAtt.ToString(), offsetAtt.StartOffset);
                    tokenIter = tokenBuffer.GetEnumerator();
                    /* 
                     * it should not be possible to have a sentence with 0 words, check just in case.
                     * returning EOS isn't the best either, but its the behavior of the original code.
                     */
                    if (!tokenIter.MoveNext())
                    {
                        return false;
                    }
                }
                else
                {
                    return false; // no more sentences, end of stream!
                }
            }

            // WordTokenFilter must clear attributes, as it is creating new tokens.
            ClearAttributes();
            // There are remaining tokens from the current sentence, return the next one. 
            SegToken nextWord = tokenIter.Current;

            termAtt.CopyBuffer(nextWord.CharArray, 0, nextWord.CharArray.Length);
            if (hasIllegalOffsets)
            {
                offsetAtt.SetOffset(tokStart, tokEnd);
            }
            else
            {
                offsetAtt.SetOffset(nextWord.StartOffset, nextWord.EndOffset);
            }
            typeAtt.Type = "word";
            return true;
        }

        public override void Reset()
        {
            base.Reset();
            tokenIter?.Dispose(); // LUCENENET specific
            tokenIter = null;
        }

        /// <summary>
        /// Releases resources used by the <see cref="WordTokenFilter"/> and
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
                    tokenIter?.Dispose(); // LUCENENET specific - dispose tokenIter and set to null
                    tokenIter = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
