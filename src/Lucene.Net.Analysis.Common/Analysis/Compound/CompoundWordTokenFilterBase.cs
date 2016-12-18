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
    /// <para>
    /// 
    /// <a name="version"></a>
    /// You must specify the required <seealso cref="LuceneVersion"/> compatibility when creating
    /// CompoundWordTokenFilterBase:
    /// <ul>
    /// <li>As of 3.1, CompoundWordTokenFilterBase correctly handles Unicode 4.0
    /// supplementary characters in strings and char arrays provided as compound word
    /// dictionaries.
    /// <li>As of 4.4, <seealso cref="CompoundWordTokenFilterBase"/> doesn't update offsets.
    /// </ul>
    /// </para>
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

        protected internal readonly LuceneVersion matchVersion;
        protected internal readonly CharArraySet dictionary;
        protected internal readonly LinkedList<CompoundToken> tokens;
        protected internal readonly int minWordSize;
        protected internal readonly int minSubwordSize;
        protected internal readonly int maxSubwordSize;
        protected internal readonly bool onlyLongestMatch;

        protected internal readonly ICharTermAttribute termAtt;
        protected internal readonly IOffsetAttribute offsetAtt;
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
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();

            this.matchVersion = matchVersion;
            this.tokens = new LinkedList<CompoundToken>();
            if (minWordSize < 0)
            {
                throw new System.ArgumentException("minWordSize cannot be negative");
            }
            this.minWordSize = minWordSize;
            if (minSubwordSize < 0)
            {
                throw new System.ArgumentException("minSubwordSize cannot be negative");
            }
            this.minSubwordSize = minSubwordSize;
            if (maxSubwordSize < 0)
            {
                throw new System.ArgumentException("maxSubwordSize cannot be negative");
            }
            this.maxSubwordSize = maxSubwordSize;
            this.onlyLongestMatch = onlyLongestMatch;
            this.dictionary = dictionary;
        }

        public override sealed bool IncrementToken()
        {
            if (tokens.Count > 0)
            {
                Debug.Assert(current != null);
                CompoundToken token = tokens.First.Value; tokens.RemoveFirst();
                RestoreState(current); // keep all other attributes untouched
                termAtt.SetEmpty().Append(token.txt);
                offsetAtt.SetOffset(token.startOffset, token.endOffset);
                posIncAtt.PositionIncrement = 0;
                return true;
            }

            current = null; // not really needed, but for safety
            if (input.IncrementToken())
            {
                // Only words longer than minWordSize get processed
                if (termAtt.Length >= this.minWordSize)
                {
                    Decompose();
                    // only capture the state if we really need it for producing new tokens
                    if (tokens.Count > 0)
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
        /// Decomposes the current <seealso cref="#termAtt"/> and places <seealso cref="CompoundToken"/> instances in the <seealso cref="#tokens"/> list.
        /// The original token may not be placed in the list, as it is automatically passed through this filter.
        /// </summary>
        protected abstract void Decompose();

        public override void Reset()
        {
            base.Reset();
            tokens.Clear();
            current = null;
        }

        /// <summary>
        /// Helper class to hold decompounded token information
        /// </summary>
        protected internal class CompoundToken
        {
            public readonly ICharSequence txt;
            public readonly int startOffset, endOffset;

            /// <summary>
            /// Construct the compound token based on a slice of the current <seealso cref="CompoundWordTokenFilterBase#termAtt"/>. </summary>
            public CompoundToken(CompoundWordTokenFilterBase outerInstance, int offset, int length)
            {
                this.txt = outerInstance.termAtt.SubSequence(offset, offset + length);

                // offsets of the original word
                int startOff = outerInstance.offsetAtt.StartOffset;
                int endOff = outerInstance.offsetAtt.EndOffset;

#pragma warning disable 612, 618
                if (outerInstance.matchVersion.OnOrAfter(LuceneVersion.LUCENE_44) || endOff - startOff != outerInstance.termAtt.Length)
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