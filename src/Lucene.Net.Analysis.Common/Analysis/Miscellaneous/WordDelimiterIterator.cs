// Lucene version compatibility level 4.8.1
using J2N;
using System.Globalization;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// A BreakIterator-like API for iterating over subwords in text, according to <see cref="WordDelimiterFilter"/> rules.
    /// @lucene.internal
    /// </summary>
    public sealed class WordDelimiterIterator
    {
        /// <summary>
        /// Indicates the end of iteration </summary>
        public const int DONE = -1;

        public static readonly byte[] DEFAULT_WORD_DELIM_TABLE = LoadDefaultWordDelimTable();

        internal char[] text;
        private int length;

        /// <summary>
        /// start position of text, excluding leading delimiters </summary>
        private int startBounds;
        /// <summary>
        /// end position of text, excluding trailing delimiters </summary>
        private int endBounds;

        /// <summary>
        /// Beginning of subword </summary>
        internal int current;
        /// <summary>
        /// End of subword </summary>
        internal int end;

        /// <summary>does this string end with a possessive such as 's</summary>
        private bool hasFinalPossessive = false;

        /// <summary>
        /// If false, causes case changes to be ignored (subwords will only be generated
        /// given SUBWORD_DELIM tokens). (Defaults to true)
        /// </summary>
        private readonly bool splitOnCaseChange;

        /// <summary>
        /// If false, causes numeric changes to be ignored (subwords will only be generated
        /// given SUBWORD_DELIM tokens). (Defaults to true)
        /// </summary>
        private readonly bool splitOnNumerics;

        /// <summary>
        /// If true, causes trailing "'s" to be removed for each subword. (Defaults to true)
        /// <p/>
        /// "O'Neil's" => "O", "Neil"
        /// </summary>
        private readonly bool stemEnglishPossessive;

        private readonly byte[] charTypeTable;

        /// <summary>
        /// if true, need to skip over a possessive found in the last call to next() </summary>
        private bool skipPossessive = false;

        // TODO: should there be a WORD_DELIM category for chars that only separate words (no catenation of subwords will be
        // done if separated by these chars?) "," would be an obvious candidate...
        private static byte[] LoadDefaultWordDelimTable() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            var tab = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                byte code = 0;
                if (Character.IsLower(i))
                {
                    code |= WordDelimiterFilter.LOWER;
                }
                else if (Character.IsUpper(i))
                {
                    code |= WordDelimiterFilter.UPPER;
                }
                else if (Character.IsDigit(i))
                {
                    code |= WordDelimiterFilter.DIGIT;
                }
                if (code == 0)
                {
                    code = WordDelimiterFilter.SUBWORD_DELIM;
                }
                tab[i] = code;
            }
            return tab;
        }

        /// <summary>
        /// Create a new <see cref="WordDelimiterIterator"/> operating with the supplied rules.
        /// </summary>
        /// <param name="charTypeTable"> table containing character types </param>
        /// <param name="splitOnCaseChange"> if true, causes "PowerShot" to be two tokens; ("Power-Shot" remains two parts regards) </param>
        /// <param name="splitOnNumerics"> if true, causes "j2se" to be three tokens; "j" "2" "se" </param>
        /// <param name="stemEnglishPossessive"> if true, causes trailing "'s" to be removed for each subword: "O'Neil's" => "O", "Neil" </param>
        internal WordDelimiterIterator(byte[] charTypeTable, bool splitOnCaseChange, bool splitOnNumerics, bool stemEnglishPossessive)
        {
            this.charTypeTable = charTypeTable;
            this.splitOnCaseChange = splitOnCaseChange;
            this.splitOnNumerics = splitOnNumerics;
            this.stemEnglishPossessive = stemEnglishPossessive;
        }

        /// <summary>
        /// Advance to the next subword in the string.
        /// </summary>
        /// <returns> index of the next subword, or <see cref="DONE"/> if all subwords have been returned </returns>
        internal int Next()
        {
            current = end;
            if (current == DONE)
            {
                return DONE;
            }

            if (skipPossessive)
            {
                current += 2;
                skipPossessive = false;
            }

            int lastType = 0;

            while (current < endBounds && (WordDelimiterFilter.IsSubwordDelim(lastType = CharType(text[current]))))
            {
                current++;
            }

            if (current >= endBounds)
            {
                return end = DONE;
            }

            for (end = current + 1; end < endBounds; end++)
            {
                int type = CharType(text[end]);
                if (IsBreak(lastType, type))
                {
                    break;
                }
                lastType = type;
            }

            if (end < endBounds - 1 && EndsWithPossessive(end + 2))
            {
                skipPossessive = true;
            }

            return end;
        }


        /// <summary>
        /// Return the type of the current subword.
        /// This currently uses the type of the first character in the subword.
        /// </summary>
        /// <returns> type of the current word </returns>
        internal int Type
        {
            get
            {
                if (end == DONE)
                {
                    return 0;
                }

                int type = CharType(text[current]);
                switch (type)
                {
                    // return ALPHA word type for both lower and upper
                    case WordDelimiterFilter.LOWER:
                    case WordDelimiterFilter.UPPER:
                        return WordDelimiterFilter.ALPHA;
                    default:
                        return type;
                }
            }
        }

        /// <summary>
        /// Reset the text to a new value, and reset all state
        /// </summary>
        /// <param name="text"> New text </param>
        /// <param name="length"> length of the text </param>
        internal void SetText(char[] text, int length)
        {
            this.text = text;
            this.length = this.endBounds = length;
            current = startBounds = end = 0;
            skipPossessive = hasFinalPossessive = false;
            SetBounds();
        }

        // ================================================= Helper Methods ================================================

        /// <summary>
        /// Determines whether the transition from lastType to type indicates a break
        /// </summary>
        /// <param name="lastType"> Last subword type </param>
        /// <param name="type"> Current subword type </param>
        /// <returns> <c>true</c> if the transition indicates a break, <c>false</c> otherwise </returns>
        private bool IsBreak(int lastType, int type)
        {
            if ((type & lastType) != 0)
            {
                return false;
            }

            if (!splitOnCaseChange && WordDelimiterFilter.IsAlpha(lastType) && WordDelimiterFilter.IsAlpha(type))
            {
                // ALPHA->ALPHA: always ignore if case isn't considered.
                return false;
            }
            else if (WordDelimiterFilter.IsUpper(lastType) && WordDelimiterFilter.IsAlpha(type))
            {
                // UPPER->letter: Don't split
                return false;
            }
            else if (!splitOnNumerics && ((WordDelimiterFilter.IsAlpha(lastType) && WordDelimiterFilter.IsDigit(type)) || (WordDelimiterFilter.IsDigit(lastType) && WordDelimiterFilter.IsAlpha(type))))
            {
                // ALPHA->NUMERIC, NUMERIC->ALPHA :Don't split
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if the current word contains only one subword.  Note, it could be potentially surrounded by delimiters
        /// </summary>
        /// <returns> <c>true</c> if the current word contains only one subword, <c>false</c> otherwise </returns>
        internal bool IsSingleWord() 
        {
            if (hasFinalPossessive)
            {
                return current == startBounds && end == endBounds - 2;
            }
            else
            {
                return current == startBounds && end == endBounds;
            }
        }

        /// <summary>
        /// Set the internal word bounds (remove leading and trailing delimiters). Note, if a possessive is found, don't remove
        /// it yet, simply note it.
        /// </summary>
        private void SetBounds()
        {
            while (startBounds < length && (WordDelimiterFilter.IsSubwordDelim(CharType(text[startBounds]))))
            {
                startBounds++;
            }

            while (endBounds > startBounds && (WordDelimiterFilter.IsSubwordDelim(CharType(text[endBounds - 1]))))
            {
                endBounds--;
            }
            if (EndsWithPossessive(endBounds))
            {
                hasFinalPossessive = true;
            }
            current = startBounds;
        }

        /// <summary>
        /// Determines if the text at the given position indicates an English possessive which should be removed
        /// </summary>
        /// <param name="pos"> Position in the text to check if it indicates an English possessive </param>
        /// <returns> <c>true</c> if the text at the position indicates an English posessive, <c>false</c> otherwise </returns>
        private bool EndsWithPossessive(int pos)
        {
            return (stemEnglishPossessive && 
                pos > 2 && 
                text[pos - 2] == '\'' && 
                (text[pos - 1] == 's' || text[pos - 1] == 'S') && 
                WordDelimiterFilter.IsAlpha(CharType(text[pos - 3])) && 
                (pos == endBounds || WordDelimiterFilter.IsSubwordDelim(CharType(text[pos]))));
        }

        /// <summary>
        /// Determines the type of the given character
        /// </summary>
        /// <param name="ch"> Character whose type is to be determined </param>
        /// <returns> Type of the character </returns>
        private int CharType(int ch)
        {
            if (ch < charTypeTable.Length)
            {
                return charTypeTable[ch];
            }
            return GetType(ch);
        }

        /// <summary>
        /// Computes the type of the given character
        /// </summary>
        /// <param name="ch"> Character whose type is to be determined </param>
        /// <returns> Type of the character </returns>
        public static byte GetType(int ch)
        {
            switch (Character.GetType(ch))
            {
                case UnicodeCategory.UppercaseLetter:
                    return WordDelimiterFilter.UPPER;
                case UnicodeCategory.LowercaseLetter:
                    return WordDelimiterFilter.LOWER;

                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.EnclosingMark: // depends what it encloses?
                case UnicodeCategory.SpacingCombiningMark:
                    return WordDelimiterFilter.ALPHA;

                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.OtherNumber:
                    return WordDelimiterFilter.DIGIT;

                // case Character.SPACE_SEPARATOR:
                // case Character.LINE_SEPARATOR:
                // case Character.PARAGRAPH_SEPARATOR:
                // case Character.CONTROL:
                // case Character.FORMAT:
                // case Character.PRIVATE_USE:

                case UnicodeCategory.Surrogate:
                    return WordDelimiterFilter.ALPHA | WordDelimiterFilter.DIGIT;

                // case Character.DASH_PUNCTUATION:
                // case Character.START_PUNCTUATION:
                // case Character.END_PUNCTUATION:
                // case Character.CONNECTOR_PUNCTUATION:
                // case Character.OTHER_PUNCTUATION:
                // case Character.MATH_SYMBOL:
                // case Character.CURRENCY_SYMBOL:
                // case Character.MODIFIER_SYMBOL:
                // case Character.OTHER_SYMBOL:
                // case Character.INITIAL_QUOTE_PUNCTUATION:
                // case Character.FINAL_QUOTE_PUNCTUATION:

                default:
                    return WordDelimiterFilter.SUBWORD_DELIM;

            }
        }
    }
}