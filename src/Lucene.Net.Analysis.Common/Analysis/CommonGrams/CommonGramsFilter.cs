using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.CommonGrams
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

    /*
	 * TODO: Consider implementing https://issues.apache.org/jira/browse/LUCENE-1688 changes to stop list and associated constructors 
	 */

    /// <summary>
    /// Construct bigrams for frequently occurring terms while indexing. Single terms
    /// are still indexed too, with bigrams overlaid. This is achieved through the
    /// use of <seealso cref="PositionIncrementAttribute#setPositionIncrement(int)"/>. Bigrams have a type
    /// of <seealso cref="#GRAM_TYPE"/> Example:
    /// <ul>
    /// <li>input:"the quick brown fox"</li>
    /// <li>output:|"the","the-quick"|"brown"|"fox"|</li>
    /// <li>"the-quick" has a position increment of 0 so it is in the same position
    /// as "the" "the-quick" has a term.type() of "gram"</li>
    /// 
    /// </ul>
    /// </summary>

    /*
     * Constructors and makeCommonSet based on similar code in StopFilter
     */
    public sealed class CommonGramsFilter : TokenFilter
    {

        public const string GRAM_TYPE = "gram";
        private const char SEPARATOR = '_';

        private readonly CharArraySet commonWords;

        private readonly StringBuilder buffer = new StringBuilder();

        private readonly ICharTermAttribute termAttribute;
        private readonly IOffsetAttribute offsetAttribute;
        private readonly ITypeAttribute typeAttribute;
        private readonly IPositionIncrementAttribute posIncAttribute;
        private readonly IPositionLengthAttribute posLenAttribute;

        private int lastStartOffset;
        private bool lastWasCommon;
        private State savedState;

        /// <summary>
        /// Construct a token stream filtering the given input using a Set of common
        /// words to create bigrams. Outputs both unigrams with position increment and
        /// bigrams with position increment 0 type=gram where one or both of the words
        /// in a potential bigram are in the set of common words .
        /// </summary>
        /// <param name="input"> TokenStream input in filter chain </param>
        /// <param name="commonWords"> The set of common words. </param>
        public CommonGramsFilter(LuceneVersion matchVersion, TokenStream input, CharArraySet commonWords)
            : base(input)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            offsetAttribute = AddAttribute<IOffsetAttribute>();
            typeAttribute = AddAttribute<ITypeAttribute>();
            posIncAttribute = AddAttribute<IPositionIncrementAttribute>();
            posLenAttribute = AddAttribute<IPositionLengthAttribute>();
            this.commonWords = commonWords;
        }

        /// <summary>
        /// Inserts bigrams for common words into a token stream. For each input token,
        /// output the token. If the token and/or the following token are in the list
        /// of common words also output a bigram with position increment 0 and
        /// type="gram"
        /// 
        /// TODO:Consider adding an option to not emit unigram stopwords
        /// as in CDL XTF BigramStopFilter, CommonGramsQueryFilter would need to be
        /// changed to work with this.
        /// 
        /// TODO: Consider optimizing for the case of three
        /// commongrams i.e "man of the year" normally produces 3 bigrams: "man-of",
        /// "of-the", "the-year" but with proper management of positions we could
        /// eliminate the middle bigram "of-the"and save a disk seek and a whole set of
        /// position lookups.
        /// </summary>
        public override bool IncrementToken()
        {
            // get the next piece of input
            if (savedState != null)
            {
                RestoreState(savedState);
                savedState = null;
                SaveTermBuffer();
                return true;
            }
            else if (!input.IncrementToken())
            {
                return false;
            }

            /* We build n-grams before and after stopwords. 
             * When valid, the buffer always contains at least the separator.
             * If its empty, there is nothing before this stopword.
             */
            if (lastWasCommon || (Common && buffer.Length > 0))
            {
                savedState = CaptureState();
                GramToken();
                return true;
            }

            SaveTermBuffer();
            return true;
        }

        /// <summary>
        /// {@inheritDoc}
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            lastWasCommon = false;
            savedState = null;
            buffer.Length = 0;
        }

        // ================================================= Helper Methods ================================================

        /// <summary>
        /// Determines if the current token is a common term
        /// </summary>
        /// <returns> {@code true} if the current token is a common term, {@code false} otherwise </returns>
        private bool Common
        {
            get
            {
                return commonWords != null && commonWords.Contains(termAttribute.Buffer(), 0, termAttribute.Length);
            }
        }

        /// <summary>
        /// Saves this information to form the left part of a gram
        /// </summary>
        private void SaveTermBuffer()
        {
            buffer.Length = 0;
            buffer.Append(termAttribute.Buffer(), 0, termAttribute.Length);
            buffer.Append(SEPARATOR);
            lastStartOffset = offsetAttribute.StartOffset();
            lastWasCommon = Common;
        }

        /// <summary>
        /// Constructs a compound token.
        /// </summary>
        private void GramToken()
        {
            buffer.Append(termAttribute.Buffer(), 0, termAttribute.Length);
            int endOffset = offsetAttribute.EndOffset();

            ClearAttributes();

            var length = buffer.Length;
            var termText = termAttribute.Buffer();
            if (length > termText.Length)
            {
                termText = termAttribute.ResizeBuffer(length);
            }

            buffer.CopyTo(0, termText, 0, length);
            termAttribute.Length = length;
            posIncAttribute.PositionIncrement = 0;
            posLenAttribute.PositionLength = 2; // bigram
            offsetAttribute.SetOffset(lastStartOffset, endOffset);
            typeAttribute.Type = GRAM_TYPE;
            buffer.Length = 0;
        }
    }
}