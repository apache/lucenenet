// Lucene version compatibility level 4.8.1
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
    /// Tokenizes the given token into n-grams of given size(s).
    /// <para>
    /// This <see cref="TokenFilter"/> create n-grams from the beginning edge or ending edge of a input token.
    /// </para>
    /// <para>As of Lucene 4.4, this filter does not support
    /// <see cref="Side.BACK"/> (you can use <see cref="Reverse.ReverseStringFilter"/> up-front and
    /// afterward to get the same behavior), handles supplementary characters
    /// correctly and does not update offsets anymore.
    /// </para>
    /// </summary>
    public sealed class EdgeNGramTokenFilter : TokenFilter
    {
        public const Side DEFAULT_SIDE = Side.FRONT;
        public const int DEFAULT_MAX_GRAM_SIZE = 1;
        public const int DEFAULT_MIN_GRAM_SIZE = 1;

        /// <summary>
        /// Specifies which side of the input the n-gram should be generated from </summary>
        public enum Side
        {
            /// <summary>
            /// Get the n-gram from the front of the input </summary>
            FRONT,

            /// <summary>
            /// Get the n-gram from the end of the input </summary>
            [System.Obsolete]
            BACK,
        }

        /// <summary>
        /// Get the appropriate <see cref="Side"/> from a string
        /// </summary>
        public static Side GetSide(string sideName)
        {
            Side result;
            if (!Enum.TryParse(sideName, true, out result))
            {
                result = Side.FRONT;
            }
            return result;
        }

        private readonly LuceneVersion version;
        private readonly CharacterUtils charUtils;
        private readonly int minGram;
        private readonly int maxGram;
        private readonly Side side;
        private char[] curTermBuffer;
        private int curTermLength;
        private int curCodePointCount;
        private int curGramSize;
        private int tokStart;
        private int tokEnd; // only used if the length changed before this filter
        private bool updateOffsets; // never if the length changed before this filter
        private int savePosIncr;
        private int savePosLen;

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IPositionLengthAttribute posLenAtt;

        /// <summary>
        /// Creates <see cref="EdgeNGramTokenFilter"/> that can generate n-grams in the sizes of the given range
        /// </summary>
        /// <param name="version"> the Lucene match version - See <see cref="LuceneVersion"/> </param>
        /// <param name="input"> <see cref="TokenStream"/> holding the input to be tokenized </param>
        /// <param name="side"> the <see cref="Side"/> from which to chop off an n-gram </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        [Obsolete]
        public EdgeNGramTokenFilter(LuceneVersion version, TokenStream input, Side side, int minGram, int maxGram)
              : base(input)
        {
            // LUCENENET specific - version cannot be null because it is a value type.

            if (version.OnOrAfter(LuceneVersion.LUCENE_44) && side == Side.BACK)
            {
                throw new ArgumentException("Side.BACK is not supported anymore as of Lucene 4.4, use ReverseStringFilter up-front and afterward");
            }

            if (!side.IsDefined())
            {
                throw new ArgumentOutOfRangeException(nameof(side), "sideLabel must be either front or back"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            if (minGram < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minGram), "minGram must be greater than zero"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            if (minGram > maxGram)
            {
                throw new ArgumentException("minGram must not be greater than maxGram");
            }

            this.version = version;
            this.charUtils = version.OnOrAfter(LuceneVersion.LUCENE_44) ? CharacterUtils.GetInstance(version) : CharacterUtils.GetJava4Instance(version);
            this.minGram = minGram;
            this.maxGram = maxGram;
            this.side = side;

            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            this.posLenAtt = AddAttribute<IPositionLengthAttribute>();
        }

        /// <summary>
        /// Creates <see cref="EdgeNGramTokenFilter"/> that can generate n-grams in the sizes of the given range
        /// </summary>
        /// <param name="version"> the Lucene match version - See <see cref="LuceneVersion"/> </param>
        /// <param name="input"> <see cref="TokenStream"/> holding the input to be tokenized </param>
        /// <param name="sideLabel"> the name of the <see cref="Side"/> from which to chop off an n-gram </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        [Obsolete]
        public EdgeNGramTokenFilter(LuceneVersion version, TokenStream input, string sideLabel, int minGram, int maxGram)
              : this(version, input, GetSide(sideLabel), minGram, maxGram)
        {
        }

        /// <summary>
        /// Creates <see cref="EdgeNGramTokenFilter"/> that can generate n-grams in the sizes of the given range
        /// </summary>
        /// <param name="version"> the Lucene match version - See <see cref="LuceneVersion"/> </param>
        /// <param name="input"> <see cref="TokenStream"/> holding the input to be tokenized </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        public EdgeNGramTokenFilter(LuceneVersion version, TokenStream input, int minGram, int maxGram)
#pragma warning disable 612, 618
              : this(version, input, Side.FRONT, minGram, maxGram)
#pragma warning restore 612, 618
        {
        }

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
                        tokStart = offsetAtt.StartOffset;
                        tokEnd = offsetAtt.EndOffset;
#pragma warning disable 612, 618
                        if (version.OnOrAfter(LuceneVersion.LUCENE_44))
#pragma warning restore 612, 618
                        {
                            // Never update offsets
                            updateOffsets = false;
                        }
                        else
                        {
                            // if length by start + end offsets doesn't match the term text then assume
                            // this is a synonym and don't adjust the offsets.
                            updateOffsets = (tokStart + curTermLength) == tokEnd;
                        }
                        savePosIncr += posIncrAtt.PositionIncrement;
                        savePosLen = posLenAtt.PositionLength;
                    }
                }
                if (curGramSize <= maxGram) // if we have hit the end of our n-gram size range, quit
                {
                    if (curGramSize <= curCodePointCount) // if the remaining input is too short, we can't generate any n-grams
                    {
                        // grab gramSize chars from front or back
                        int start = side == Side.FRONT ? 0 : charUtils.OffsetByCodePoints(curTermBuffer, 0, curTermLength, curTermLength, -curGramSize);
                        int end = charUtils.OffsetByCodePoints(curTermBuffer, 0, curTermLength, start, curGramSize);
                        ClearAttributes();
                        if (updateOffsets)
                        {
                            offsetAtt.SetOffset(tokStart + start, tokStart + end);
                        }
                        else
                        {
                            offsetAtt.SetOffset(tokStart, tokEnd);
                        }
                        // first ngram gets increment, others don't
                        if (curGramSize == minGram)
                        {
                            posIncrAtt.PositionIncrement = savePosIncr;
                            savePosIncr = 0;
                        }
                        else
                        {
                            posIncrAtt.PositionIncrement = 0;
                        }
                        posLenAtt.PositionLength = savePosLen;
                        termAtt.CopyBuffer(curTermBuffer, start, end - start);
                        curGramSize++;
                        return true;
                    }
                }
                curTermBuffer = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            curTermBuffer = null;
            savePosIncr = 0;
        }
    }

    // LUCENENET: added this to avoid the Enum.IsDefined() method, which requires boxing
    internal static partial class SideExtensions
    {
        internal static bool IsDefined(this EdgeNGramTokenFilter.Side side)
        {
            return side >= EdgeNGramTokenFilter.Side.FRONT &&
#pragma warning disable CS0612 // Type or member is obsolete
                side <= EdgeNGramTokenFilter.Side.BACK;
#pragma warning restore CS0612 // Type or member is obsolete
        }
    }
}