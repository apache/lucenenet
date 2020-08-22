// Lucene version compatibility level 8.6.1
using ICU4N.Text;
using J2N;

namespace Lucene.Net.Analysis.Icu.Segmentation
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
    /// Wraps <see cref="RuleBasedBreakIterator"/>, making object reuse convenient and
    /// emitting a rule status for emoji sequences.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal sealed class BreakIteratorWrapper
    {
        private readonly CharArrayIterator textIterator = new CharArrayIterator();
        private readonly RuleBasedBreakIterator rbbi;
        private char[] text;
        private int start;
        private int status;

        internal BreakIteratorWrapper(RuleBasedBreakIterator rbbi)
        {
            this.rbbi = rbbi;
        }

        public int Current => rbbi.Current;

        public int RuleStatus => status;

        public int Next()
        {
            int current = rbbi.Current;
            int next = rbbi.Next();
            status = CalcStatus(current, next);
            return next;
        }

        /// <summary>Returns current rule status for the text between breaks. (determines token type)</summary>
        private int CalcStatus(int current, int next)
        {
            // to support presentation selectors, we need to handle alphanum, num, and none at least, so currently not worth optimizing.
            // https://unicode.org/cldr/utility/list-unicodeset.jsp?a=%5B%3AEmoji%3A%5D-%5B%3AEmoji_Presentation%3A%5D&g=Word_Break&i=
            if (next != BreakIterator.Done && IsEmoji(current, next))
            {
                return ICUTokenizerConfig.EMOJI_SEQUENCE_STATUS;
            }
            else
            {
                return rbbi.RuleStatus;
            }
        }

        // See unicode doc L2/16-315 for rationale.
        // basically for us the ambiguous cases (keycap/etc) as far as types go.
        internal static readonly UnicodeSet EMOJI_RK = new UnicodeSet("[\u002a\u00230-9©®™〰〽]").Freeze();
        // faster than doing hasBinaryProperty() checks, at the cost of 1KB ram
        //internal static readonly UnicodeSet EMOJI = new UnicodeSet("[[:Emoji:][:Extended_Pictographic:]]").Freeze(); // LUCENENET: Extended_Pictographic wasn't added until ICU 62
        internal static readonly UnicodeSet EMOJI = new UnicodeSet("[[:Emoji:]]").Freeze();

        /// <summary>Returns <c>true</c> if the current text represents emoji character or sequence.</summary>
        private bool IsEmoji(int current, int next)
        {
            int begin = start + current;
            int end = start + next;
            int codepoint = UTF16.CharAt(text, 0, end, begin);
            if (EMOJI.Contains(codepoint))
            {
                if (EMOJI_RK.Contains(codepoint))
                {
                    // if its in EmojiRK, we don't treat it as emoji unless there is evidence it forms emoji sequence,
                    // an emoji presentation selector or keycap follows.
                    int trailer = begin + Character.CharCount(codepoint);
                    return trailer < end && (text[trailer] == 0xFE0F || text[trailer] == 0x20E3);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public void SetText(char[] text, int start, int length)
        {
            this.text = text;
            this.start = start;
            textIterator.SetText(text, start, length);
            rbbi.SetText(textIterator);
            status = RuleBasedBreakIterator.WordNone;
        }
    }
}
