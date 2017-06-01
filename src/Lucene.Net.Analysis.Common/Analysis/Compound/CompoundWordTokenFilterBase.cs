using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Diagnostics;

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
        protected readonly LinkedList<CompoundToken> m_tokens;
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
            this.m_tokens = new LinkedList<CompoundToken>();
            if (minWordSize < 0)
            {
                throw new System.ArgumentException("minWordSize cannot be negative");
            }
            this.m_minWordSize = minWordSize;
            if (minSubwordSize < 0)
            {
                throw new System.ArgumentException("minSubwordSize cannot be negative");
            }
            this.m_minSubwordSize = minSubwordSize;
            if (maxSubwordSize < 0)
            {
                throw new System.ArgumentException("maxSubwordSize cannot be negative");
            }
            this.m_maxSubwordSize = maxSubwordSize;
            this.m_onlyLongestMatch = onlyLongestMatch;
            this.m_dictionary = dictionary;
        }

        public override sealed bool IncrementToken()
        {
            if (m_tokens.Count > 0)
            {
                Debug.Assert(current != null);
                CompoundToken token = m_tokens.First.Value;
                m_tokens.Remove(token);
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

            public ICharSequence Text // LUCENENET specific: changed public field into property backed by private field
            {
                get { return txt; }
            }

            public int StartOffset // LUCENENET specific: changed public field into property backed by private field
            {
                get { return startOffset; }
            }

            public int EndOffset // LUCENENET specific: changed public field into property backed by private field
            {
                get { return endOffset; }
            }

            /// <summary>
            /// Construct the compound token based on a slice of the current <see cref="CompoundWordTokenFilterBase.m_termAtt"/>. </summary>
            public CompoundToken(CompoundWordTokenFilterBase outerInstance, int offset, int length)
            {
                this.txt = outerInstance.m_termAtt.SubSequence(offset, offset + length);

                // offsets of the original word
                int startOff = outerInstance.m_offsetAtt.StartOffset;
                int endOff = outerInstance.m_offsetAtt.EndOffset;

#pragma warning disable 612, 618
                if (outerInstance.m_matchVersion.OnOrAfter(LuceneVersion.LUCENE_44) || endOff - startOff != outerInstance.m_termAtt.Length)
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