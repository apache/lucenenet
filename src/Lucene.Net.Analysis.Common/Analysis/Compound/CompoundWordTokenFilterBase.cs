// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Compound
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
    /// Base class for decomposition token filters.
    /// <para/>
    /// You must specify the required <see cref="LuceneVersion"/> compatibility when creating
    /// <see cref="CompoundWordTokenFilterBase"/>:
    /// <list type="bullet">
    ///     <item><description>As of 3.1, CompoundWordTokenFilterBase correctly handles Unicode 4.0
    ///     supplementary characters in strings and char arrays provided as compound word
    ///     dictionaries.</description></item>
    ///     <item><description>As of 4.4, <see cref="CompoundWordTokenFilterBase"/> doesn't update offsets.</description></item>
    /// </list>
    /// </summary>
    public abstract class CompoundWordTokenFilterBase : TokenFilter
    {
        /// <summary>
        /// The default for minimal word length that gets decomposed
        /// </summary>
        public const int DEFAULT_MIN_WORD_SIZE = 5;

        /// <summary>
        /// The default for minimal length of subwords that get propagated to the output of this filter
        /// </summary>
        public const int DEFAULT_MIN_SUBWORD_SIZE = 2;

        /// <summary>
        /// The default for maximal length of subwords that get propagated to the output of this filter
        /// </summary>
        public const int DEFAULT_MAX_SUBWORD_SIZE = 15;

        protected readonly LuceneVersion m_matchVersion;
        protected readonly CharArraySet m_dictionary;
        protected readonly Queue<CompoundToken> m_tokens;
        protected readonly int m_minWordSize;
        protected readonly int m_minSubwordSize;
        protected readonly int m_maxSubwordSize;
        protected readonly bool m_onlyLongestMatch;

        protected readonly ICharTermAttribute m_termAtt;
        protected readonly IOffsetAttribute m_offsetAtt;
        private readonly IPositionIncrementAttribute posIncAtt;

        private AttributeSource.State current;

        protected CompoundWordTokenFilterBase(LuceneVersion matchVersion, TokenStream input, CharArraySet dictionary, bool onlyLongestMatch)
            : this(matchVersion, input, dictionary, DEFAULT_MIN_WORD_SIZE, DEFAULT_MIN_SUBWORD_SIZE, DEFAULT_MAX_SUBWORD_SIZE, onlyLongestMatch)
        {
        }

        protected CompoundWordTokenFilterBase(LuceneVersion matchVersion, TokenStream input, CharArraySet dictionary)
            : this(matchVersion, input, dictionary, DEFAULT_MIN_WORD_SIZE, DEFAULT_MIN_SUBWORD_SIZE, DEFAULT_MAX_SUBWORD_SIZE, false)
        {
        }

        protected CompoundWordTokenFilterBase(LuceneVersion matchVersion, TokenStream input, CharArraySet dictionary, int minWordSize, int minSubwordSize, int maxSubwordSize, bool onlyLongestMatch)
            : base(input)
        {
            m_termAtt = AddAttribute<ICharTermAttribute>();
            m_offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();

            this.m_matchVersion = matchVersion;
            this.m_tokens = new Queue<CompoundToken>();
            if (minWordSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minWordSize), "minWordSize cannot be negative"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.m_minWordSize = minWordSize;
            if (minSubwordSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minSubwordSize), "minSubwordSize cannot be negative"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.m_minSubwordSize = minSubwordSize;
            if (maxSubwordSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSubwordSize), "maxSubwordSize cannot be negative"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.m_maxSubwordSize = maxSubwordSize;
            this.m_onlyLongestMatch = onlyLongestMatch;
            this.m_dictionary = dictionary;
        }

        public override sealed bool IncrementToken()
        {
            if (m_tokens.Count > 0)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(current != null);
                CompoundToken token = m_tokens.Dequeue();
                RestoreState(current); // keep all other attributes untouched
                m_termAtt.SetEmpty().Append(token.Text);
                m_offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                posIncAtt.PositionIncrement = 0;
                return true;
            }

            current = null; // not really needed, but for safety
            if (m_input.IncrementToken())
            {
                // Only words longer than minWordSize get processed
                if (m_termAtt.Length >= this.m_minWordSize)
                {
                    Decompose();
                    // only capture the state if we really need it for producing new tokens
                    if (m_tokens.Count > 0)
                    {
                        current = CaptureState();
                    }
                }
                // return original token:
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Decomposes the current <see cref="m_termAtt"/> and places <see cref="CompoundToken"/> instances in the <see cref="m_tokens"/> list.
        /// The original token may not be placed in the list, as it is automatically passed through this filter.
        /// </summary>
        protected abstract void Decompose();

        public override void Reset()
        {
            base.Reset();
            m_tokens.Clear();
            current = null;
        }

        /// <summary>
        /// Helper class to hold decompounded token information
        /// </summary>
        protected class CompoundToken
        {
            private readonly ICharSequence txt;
            private readonly int startOffset, endOffset;

            public ICharSequence Text => txt; // LUCENENET specific: changed public field into property backed by private field

            public int StartOffset => startOffset; // LUCENENET specific: changed public field into property backed by private field

            public int EndOffset => endOffset; // LUCENENET specific: changed public field into property backed by private field

            /// <summary>
            /// Construct the compound token based on a slice of the current <see cref="CompoundWordTokenFilterBase.m_termAtt"/>. </summary>
            public CompoundToken(CompoundWordTokenFilterBase compoundWordTokenFilterBase, int offset, int length)
            {
                this.txt = compoundWordTokenFilterBase.m_termAtt.Subsequence(offset, length); // LUCENENET: Corrected 2nd Subsequence parameter

                // offsets of the original word
                int startOff = compoundWordTokenFilterBase.m_offsetAtt.StartOffset;
                int endOff = compoundWordTokenFilterBase.m_offsetAtt.EndOffset;

#pragma warning disable 612, 618
                if (compoundWordTokenFilterBase.m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_44) || endOff - startOff != compoundWordTokenFilterBase.m_termAtt.Length)
#pragma warning restore 612, 618
                {
                    // if length by start + end offsets doesn't match the term text then assume
                    // this is a synonym and don't adjust the offsets.
                    this.startOffset = startOff;
                    this.endOffset = endOff;
                }
                else
                {
                    int newStart = startOff + offset;
                    this.startOffset = newStart;
                    this.endOffset = newStart + length;
                }
            }
        }
    }
}