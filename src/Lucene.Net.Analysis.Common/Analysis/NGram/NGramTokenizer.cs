// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Diagnostics;
using System.IO;

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
    /// <para>On the contrary to <see cref="NGramTokenFilter"/>, this class sets offsets so
    /// that characters between startOffset and endOffset in the original stream are
    /// the same as the term chars.
    /// </para>
    /// <para>For example, "abcde" would be tokenized as (minGram=2, maxGram=3):
    /// <list type="table">
    ///     <listheader>
    ///         <term>Term</term>
    ///         <term>Position increment</term>
    ///         <term>Position length</term>
    ///         <term>Offsets</term>
    ///     </listheader>
    ///     <item>
    ///         <term>ab</term>
    ///         <term>1</term>
    ///         <term>1</term>
    ///         <term>[0,2[</term>
    ///     </item>
    ///     <item>
    ///         <term>abc</term>
    ///         <term>1</term>
    ///         <term>1</term>
    ///         <term>[0,3[</term>
    ///     </item>
    ///     <item>
    ///         <term>bc</term>
    ///         <term>1</term>
    ///         <term>1</term>
    ///         <term>[1,3[</term>
    ///     </item>
    ///     <item>
    ///         <term>bcd</term>
    ///         <term>1</term>
    ///         <term>1</term>
    ///         <term>[1,4[</term>
    ///     </item>
    ///     <item>
    ///         <term>cd</term>
    ///         <term>1</term>
    ///         <term>1</term>
    ///         <term>[2,4[</term>
    ///     </item>
    ///     <item>
    ///         <term>cde</term>
    ///         <term>1</term>
    ///         <term>1</term>
    ///         <term>[2,5[</term>
    ///     </item>
    ///     <item>
    ///         <term>de</term>
    ///         <term>1</term>
    ///         <term>1</term>
    ///         <term>[3,5[</term>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>This tokenizer changed a lot in Lucene 4.4 in order to:
    /// <list type="bullet">
    ///     <item><description>tokenize in a streaming fashion to support streams which are larger
    ///         than 1024 chars (limit of the previous version),</description></item>
    ///     <item><description>count grams based on unicode code points instead of java chars (and
    ///         never split in the middle of surrogate pairs),</description></item>
    ///     <item><description>give the ability to pre-tokenize the stream (<see cref="IsTokenChar(int)"/>)
    ///         before computing n-grams.</description></item>
    /// </list>
    /// </para>
    /// <para>Additionally, this class doesn't trim trailing whitespaces and emits
    /// tokens in a different order, tokens are now emitted by increasing start
    /// offsets while they used to be emitted by increasing lengths (which prevented
    /// from supporting large input streams).
    /// </para>
    /// <para>Although <b style="color:red">highly</b> discouraged, it is still possible
    /// to use the old behavior through <see cref="Lucene43NGramTokenizer"/>.
    /// </para>
    /// </summary>
    // non-sealed to allow for overriding IsTokenChar, but all other methods should be sealed
    public class NGramTokenizer : Tokenizer
    {
        public const int DEFAULT_MIN_NGRAM_SIZE = 1;
        public const int DEFAULT_MAX_NGRAM_SIZE = 2;

        private CharacterUtils charUtils;
        private CharacterUtils.CharacterBuffer charBuffer;
        private int[] buffer; // like charBuffer, but converted to code points
        private int bufferStart, bufferEnd; // remaining slice in buffer
        private int offset;
        private int gramSize;
        private int minGram, maxGram;
        private bool exhausted;
        private int lastCheckedChar; // last offset in the buffer that we checked
        private int lastNonTokenChar; // last offset that we found to not be a token char
        private bool edgesOnly; // leading edges n-grams only

        private ICharTermAttribute termAtt;
        private IPositionIncrementAttribute posIncAtt;
        private IPositionLengthAttribute posLenAtt;
        private IOffsetAttribute offsetAtt;

        internal NGramTokenizer(LuceneVersion version, TextReader input, int minGram, int maxGram, bool edgesOnly)
              : base(input)
        {
            Init(version, minGram, maxGram, edgesOnly);
        }

        /// <summary>
        /// Creates <see cref="NGramTokenizer"/> with given min and max n-grams. </summary>
        /// <param name="version"> the lucene compatibility version </param>
        /// <param name="input"> <see cref="TextReader"/> holding the input to be tokenized </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        public NGramTokenizer(LuceneVersion version, TextReader input, int minGram, int maxGram)
              : this(version, input, minGram, maxGram, false)
        {
        }

        internal NGramTokenizer(LuceneVersion version, AttributeFactory factory, TextReader input, int minGram, int maxGram, bool edgesOnly)
              : base(factory, input)
        {
            Init(version, minGram, maxGram, edgesOnly);
        }

        /// <summary>
        /// Creates <see cref="NGramTokenizer"/> with given min and max n-grams. </summary>
        /// <param name="version"> the lucene compatibility version </param>
        /// <param name="factory"> <see cref="AttributeSource.AttributeFactory"/> to use </param>
        /// <param name="input"> <see cref="TextReader"/> holding the input to be tokenized </param>
        /// <param name="minGram"> the smallest n-gram to generate </param>
        /// <param name="maxGram"> the largest n-gram to generate </param>
        public NGramTokenizer(LuceneVersion version, AttributeFactory factory, TextReader input, int minGram, int maxGram)
              : this(version, factory, input, minGram, maxGram, false)
        {
        }

        /// <summary>
        /// Creates <see cref="NGramTokenizer"/> with default min and max n-grams. </summary>
        /// <param name="version"> the lucene compatibility version </param>
        /// <param name="input"> <see cref="TextReader"/> holding the input to be tokenized </param>
        public NGramTokenizer(LuceneVersion version, TextReader input)
              : this(version, input, DEFAULT_MIN_NGRAM_SIZE, DEFAULT_MAX_NGRAM_SIZE)
        {
        }

        private void Init(LuceneVersion version, int minGram, int maxGram, bool edgesOnly)
        {
#pragma warning disable 612, 618
            if (!version.OnOrAfter(LuceneVersion.LUCENE_44))
#pragma warning restore 612, 618
            {
                throw new ArgumentException("This class only works with Lucene 4.4+. To emulate the old (broken) behavior of NGramTokenizer, use Lucene43NGramTokenizer/Lucene43EdgeNGramTokenizer");
            }
#pragma warning disable 612, 618
            charUtils = version.OnOrAfter(LuceneVersion.LUCENE_44) ?
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
            termAtt = AddAttribute<ICharTermAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            posLenAtt = AddAttribute<IPositionLengthAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            this.minGram = minGram;
            this.maxGram = maxGram;
            this.edgesOnly = edgesOnly;
            charBuffer = CharacterUtils.NewCharacterBuffer(2 * maxGram + 1024); // 2 * maxGram in case all code points require 2 chars and + 1024 for buffering to not keep polling the Reader
            buffer = new int[charBuffer.Buffer.Length];

            // Make the term att large enough
            termAtt.ResizeBuffer(2 * maxGram);
        }

        public override sealed bool IncrementToken()
        {
            ClearAttributes();

            // termination of this loop is guaranteed by the fact that every iteration
            // either advances the buffer (calls consumes()) or increases gramSize
            while (true)
            {
                // compact
                if (bufferStart >= bufferEnd - maxGram - 1 && !exhausted)
                {
                    Arrays.Copy(buffer, bufferStart, buffer, 0, bufferEnd - bufferStart);
                    bufferEnd -= bufferStart;
                    lastCheckedChar -= bufferStart;
                    lastNonTokenChar -= bufferStart;
                    bufferStart = 0;

                    // fill in remaining space
                    exhausted = !charUtils.Fill(charBuffer, m_input, buffer.Length - bufferEnd);
                    // convert to code points
                    bufferEnd += charUtils.ToCodePoints(charBuffer.Buffer, 0, charBuffer.Length, buffer, bufferEnd);
                }

                // should we go to the next offset?
                if (gramSize > maxGram || (bufferStart + gramSize) > bufferEnd)
                {
                    if (bufferStart + 1 + minGram > bufferEnd)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(exhausted);
                        return false;
                    }
                    Consume();
                    gramSize = minGram;
                }

                UpdateLastNonTokenChar();

                // retry if the token to be emitted was going to not only contain token chars
                bool termContainsNonTokenChar = lastNonTokenChar >= bufferStart && lastNonTokenChar < (bufferStart + gramSize);
                bool isEdgeAndPreviousCharIsTokenChar = edgesOnly && lastNonTokenChar != bufferStart - 1;
                if (termContainsNonTokenChar || isEdgeAndPreviousCharIsTokenChar)
                {
                    Consume();
                    gramSize = minGram;
                    continue;
                }

                int length = charUtils.ToChars(buffer, bufferStart, gramSize, termAtt.Buffer, 0);
                termAtt.Length = length;
                posIncAtt.PositionIncrement = 1;
                posLenAtt.PositionLength = 1;
                offsetAtt.SetOffset(CorrectOffset(offset), CorrectOffset(offset + length));
                ++gramSize;
                return true;
            }
        }

        private void UpdateLastNonTokenChar()
        {
            int termEnd = bufferStart + gramSize - 1;
            if (termEnd > lastCheckedChar)
            {
                for (int i = termEnd; i > lastCheckedChar; --i)
                {
                    if (!IsTokenChar(buffer[i]))
                    {
                        lastNonTokenChar = i;
                        break;
                    }
                }
                lastCheckedChar = termEnd;
            }
        }

        /// <summary>
        /// Consume one code point. </summary>
        private void Consume()
        {
            offset += Character.CharCount(buffer[bufferStart++]);
        }

        /// <summary>
        /// Only collect characters which satisfy this condition. </summary>
        protected virtual bool IsTokenChar(int chr)
        {
            return true;
        }

        public override sealed void End()
        {
            base.End();
            if (Debugging.AssertsEnabled) Debugging.Assert(bufferStart <= bufferEnd);
            int endOffset = offset;
            for (int i = bufferStart; i < bufferEnd; ++i)
            {
                endOffset += Character.CharCount(buffer[i]);
            }
            endOffset = CorrectOffset(endOffset);
            // set final offset
            offsetAtt.SetOffset(endOffset, endOffset);
        }

        public override sealed void Reset()
        {
            base.Reset();
            bufferStart = bufferEnd = buffer.Length;
            lastNonTokenChar = lastCheckedChar = bufferStart - 1;
            offset = 0;
            gramSize = minGram;
            exhausted = false;
            charBuffer.Reset();
        }
    }
}