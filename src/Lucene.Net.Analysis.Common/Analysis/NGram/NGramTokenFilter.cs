// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.NGram
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
    /// Tokenizes the input into n-grams of the given size(s).
    /// <para>You must specify the required <see cref="LuceneVersion"/> compatibility when
    /// creating a <see cref="NGramTokenFilter"/>. As of Lucene 4.4, this token filters:
    /// <list type="bullet">
    ///     <item><description>handles supplementary characters correctly,</description></item>
    ///     <item><description>emits all n-grams for the same token at the same position,</description></item>
    ///     <item><description>does not modify offsets,</description></item>
    ///     <item><description>sorts n-grams by their offset in the original token first, then
    ///         increasing length (meaning that "abc" will give "a", "ab", "abc", "b", "bc",
    ///         "c").</description></item>
    /// </list>
    /// </para>
    /// <para>You can make this filter use the old behavior by providing a version &lt;
    /// <see cref="LuceneVersion.LUCENE_44"/> in the constructor but this is not recommended as
    /// it will lead to broken <see cref="TokenStream"/>s that will cause highlighting
    /// bugs.
    /// </para>
    /// <para>If you were using this <see cref="TokenFilter"/> to perform partial highlighting,
    /// this won't work anymore since this filter doesn't update offsets. You should
    /// modify your analysis chain to use <see cref="NGramTokenizer"/>, and potentially
    /// override <see cref="NGramTokenizer.IsTokenChar(int)"/> to perform pre-tokenization.
    /// </para>
    /// </summary>
    public sealed class NGramTokenFilter : TokenFilter
    {
        public const int DEFAULT_MIN_NGRAM_SIZE = 1;
        public const int DEFAULT_MAX_NGRAM_SIZE = 2;

        private readonly int minGram, maxGram;

        private char[] curTermBuffer;
        private int curTermLength;
        private int curCodePointCount;
        private int curGramSize;
        private int curPos;
        private int curPosInc, curPosLen;
        private int tokStart;
        private int tokEnd;
        private bool hasIllegalOffsets; // only if the length changed before this filter

        private readonly LuceneVersion version;
        private readonly CharacterUtils charUtils;
        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly IPositionLengthAttribute posLenAtt;
        private readonly IOffsetAttribute offsetAtt;

        /// <summary>
        /// Creates <see cref="NGramTokenFilter"/> with given min and max n-grams. </summary>
        /// <param name="version"> Lucene version to enable correct position increments.
        ///                See <see cref="NGramTokenFilter"/> for details. </param>
        /// <param name="input"> <see cref="TokenStream"/> holding the input to be tokenized </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        public NGramTokenFilter(LuceneVersion version, TokenStream input, int minGram, int maxGram)
            : base(new CodepointCountFilter(version, input, minGram, int.MaxValue))
        {
            this.version = version;
            this.charUtils = version.OnOrAfter(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_44) ?
#pragma warning restore 612, 618
                CharacterUtils.GetInstance(version) : CharacterUtils.GetJava4Instance(version);
            if (minGram < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minGram), "minGram must be greater than zero"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (minGram > maxGram)
            {
                throw new ArgumentException("minGram must not be greater than maxGram");
            }
            this.minGram = minGram;
            this.maxGram = maxGram;
#pragma warning disable 612, 618
            if (version.OnOrAfter(LuceneVersion.LUCENE_44))
#pragma warning restore 612, 618
            {
                posIncAtt = AddAttribute<IPositionIncrementAttribute>();
                posLenAtt = AddAttribute<IPositionLengthAttribute>();
            }
            else
            {
                posIncAtt = new PositionIncrementAttributeAnonymousClass();
                posLenAtt = new PositionLengthAttributeAnonymousClass();
            }
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        private sealed class PositionIncrementAttributeAnonymousClass : IPositionIncrementAttribute
        {
            public int PositionIncrement
            {
                get => 0;
                set => _ = value;
            }

            // LUCENENET specific - The interface requires this to be implemented, since we added it to avoid casts.
            public void CopyTo(IAttribute target) => _ = target;
        }

        private sealed class PositionLengthAttributeAnonymousClass : IPositionLengthAttribute
        {
            public int PositionLength
            {
                get => 0;
                set => _ = value;
            }

            // LUCENENET specific - The interface requires this to be implemented, since we added it to avoid casts.
            public void CopyTo(IAttribute target) => _ = target;
        }

        /// <summary>
        /// Creates <see cref="NGramTokenFilter"/> with default min and max n-grams. </summary>
        /// <param name="version"> Lucene version to enable correct position increments.
        ///                See <see cref="NGramTokenFilter"/> for details. </param>
        /// <param name="input"> <see cref="TokenStream"/> holding the input to be tokenized </param>
        public NGramTokenFilter(LuceneVersion version, TokenStream input)
            : this(version, input, DEFAULT_MIN_NGRAM_SIZE, DEFAULT_MAX_NGRAM_SIZE)
        {
        }

        /// <summary>
        /// Returns the next token in the stream, or null at EOS.
        /// </summary>
        public override sealed bool IncrementToken()
        {
            while (true)
            {
                if (curTermBuffer is null)
                {
                    if (!m_input.IncrementToken())
                    {
                        return false;
                    }
                    else
                    {
                        curTermBuffer = (char[])termAtt.Buffer.Clone();
                        curTermLength = termAtt.Length;
                        curCodePointCount = charUtils.CodePointCount(termAtt);
                        curGramSize = minGram;
                        curPos = 0;
                        curPosInc = posIncAtt.PositionIncrement;
                        curPosLen = posLenAtt.PositionLength;
                        tokStart = offsetAtt.StartOffset;
                        tokEnd = offsetAtt.EndOffset;
                        // if length by start + end offsets doesn't match the term text then assume
                        // this is a synonym and don't adjust the offsets.
                        hasIllegalOffsets = (tokStart + curTermLength) != tokEnd;
                    }
                }
#pragma warning disable 612, 618
                if (version.OnOrAfter(LuceneVersion.LUCENE_44))
#pragma warning restore 612, 618
                {
                    if (curGramSize > maxGram || (curPos + curGramSize) > curCodePointCount)
                    {
                        ++curPos;
                        curGramSize = minGram;
                    }
                    if ((curPos + curGramSize) <= curCodePointCount)
                    {
                        ClearAttributes();
                        int start = charUtils.OffsetByCodePoints(curTermBuffer, 0, curTermLength, 0, curPos);
                        int end = charUtils.OffsetByCodePoints(curTermBuffer, 0, curTermLength, start, curGramSize);
                        termAtt.CopyBuffer(curTermBuffer, start, end - start);
                        posIncAtt.PositionIncrement = curPosInc;
                        curPosInc = 0;
                        posLenAtt.PositionLength = curPosLen;
                        offsetAtt.SetOffset(tokStart, tokEnd);
                        curGramSize++;
                        return true;
                    }
                }
                else
                {
                    while (curGramSize <= maxGram)
                    {
                        while (curPos + curGramSize <= curTermLength) // while there is input
                        {
                            ClearAttributes();
                            termAtt.CopyBuffer(curTermBuffer, curPos, curGramSize);
                            if (hasIllegalOffsets)
                            {
                                offsetAtt.SetOffset(tokStart, tokEnd);
                            }
                            else
                            {
                                offsetAtt.SetOffset(tokStart + curPos, tokStart + curPos + curGramSize);
                            }
                            curPos++;
                            return true;
                        }
                        curGramSize++; // increase n-gram size
                        curPos = 0;
                    }
                }
                curTermBuffer = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            curTermBuffer = null;
        }
    }
}