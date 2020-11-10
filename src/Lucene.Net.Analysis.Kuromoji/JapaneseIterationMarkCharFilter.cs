using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using System.IO;

namespace Lucene.Net.Analysis.Ja
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
    /// Normalizes Japanese horizontal iteration marks (odoriji) to their expanded form.
    /// </summary>
    /// <remarks>
    /// Sequences of iteration marks are supported.  In case an illegal sequence of iteration
    /// marks is encountered, the implementation emits the illegal source character as-is
    /// without considering its script.  For example, with input "&#63;&#12445;", we get
    /// "&#63;&#63;" even though "&#63;" isn't hiragana.
    /// <para/>
    /// Note that a full stop punctuation character "&#x3002;" (U+3002) can not be iterated
    /// (see below). Iteration marks themselves can be emitted in case they are illegal,
    /// i.e. if they go back past the beginning of the character stream.
    /// <para/>
    /// The implementation buffers input until a full stop punctuation character (U+3002)
    /// or EOF is reached in order to not keep a copy of the character stream in memory.
    /// Vertical iteration marks, which are even rarer than horizontal iteration marks in
    /// contemporary Japanese, are unsupported.
    /// </remarks>
    public class JapaneseIterationMarkCharFilter : CharFilter
    {
        /// <summary>Normalize kanji iteration marks by default</summary>
        public static readonly bool NORMALIZE_KANJI_DEFAULT = true;

        /// <summary>Normalize kana iteration marks by default</summary>
        public static readonly bool NORMALIZE_KANA_DEFAULT = true;

        private const char KANJI_ITERATION_MARK = '\u3005';           // 々

        private const char HIRAGANA_ITERATION_MARK = '\u309d';        // ゝ

        private const char HIRAGANA_VOICED_ITERATION_MARK = '\u309e'; // ゞ

        private const char KATAKANA_ITERATION_MARK = '\u30fd';        // ヽ

        private const char KATAKANA_VOICED_ITERATION_MARK = '\u30fe'; // ヾ

        private const char FULL_STOP_PUNCTUATION = '\u3002';           // 。

        // Hiragana to dakuten map (lookup using code point - 0x30ab（か）*/
        private static readonly char[] h2d = new char[50]; // LUCENENET: marked readonly

        // Katakana to dakuten map (lookup using code point - 0x30ab（カ
        private static readonly char[] k2d = new char[50]; // LUCENENET: marked readonly

        private readonly RollingCharBuffer buffer = new RollingCharBuffer();

        private int bufferPosition = 0;

        private int iterationMarksSpanSize = 0;

        private int iterationMarkSpanEndPosition = 0;

        private readonly bool normalizeKanji; // LUCENENET: marked readonly

        private readonly bool normalizeKana; // LUCENENET: marked readonly

        static JapaneseIterationMarkCharFilter()
        {
            // Hiragana dakuten map
            h2d[0] = '\u304c';  // か => が
            h2d[1] = '\u304c';  // が => が
            h2d[2] = '\u304e';  // き => ぎ
            h2d[3] = '\u304e';  // ぎ => ぎ
            h2d[4] = '\u3050';  // く => ぐ
            h2d[5] = '\u3050';  // ぐ => ぐ
            h2d[6] = '\u3052';  // け => げ
            h2d[7] = '\u3052';  // げ => げ
            h2d[8] = '\u3054';  // こ => ご
            h2d[9] = '\u3054';  // ご => ご
            h2d[10] = '\u3056'; // さ => ざ
            h2d[11] = '\u3056'; // ざ => ざ
            h2d[12] = '\u3058'; // し => じ
            h2d[13] = '\u3058'; // じ => じ
            h2d[14] = '\u305a'; // す => ず
            h2d[15] = '\u305a'; // ず => ず
            h2d[16] = '\u305c'; // せ => ぜ
            h2d[17] = '\u305c'; // ぜ => ぜ
            h2d[18] = '\u305e'; // そ => ぞ
            h2d[19] = '\u305e'; // ぞ => ぞ
            h2d[20] = '\u3060'; // た => だ
            h2d[21] = '\u3060'; // だ => だ
            h2d[22] = '\u3062'; // ち => ぢ
            h2d[23] = '\u3062'; // ぢ => ぢ
            h2d[24] = '\u3063';
            h2d[25] = '\u3065'; // つ => づ
            h2d[26] = '\u3065'; // づ => づ
            h2d[27] = '\u3067'; // て => で
            h2d[28] = '\u3067'; // で => で
            h2d[29] = '\u3069'; // と => ど
            h2d[30] = '\u3069'; // ど => ど
            h2d[31] = '\u306a';
            h2d[32] = '\u306b';
            h2d[33] = '\u306c';
            h2d[34] = '\u306d';
            h2d[35] = '\u306e';
            h2d[36] = '\u3070'; // は => ば
            h2d[37] = '\u3070'; // ば => ば
            h2d[38] = '\u3071';
            h2d[39] = '\u3073'; // ひ => び
            h2d[40] = '\u3073'; // び => び
            h2d[41] = '\u3074';
            h2d[42] = '\u3076'; // ふ => ぶ
            h2d[43] = '\u3076'; // ぶ => ぶ
            h2d[44] = '\u3077';
            h2d[45] = '\u3079'; // へ => べ
            h2d[46] = '\u3079'; // べ => べ
            h2d[47] = '\u307a';
            h2d[48] = '\u307c'; // ほ => ぼ
            h2d[49] = '\u307c'; // ぼ => ぼ

            // Make katakana dakuten map from hiragana map
            char codePointDifference = (char)('\u30ab' - '\u304b'); // カ - か
            if (Debugging.AssertsEnabled) Debugging.Assert(h2d.Length == k2d.Length);
            for (int i = 0; i < k2d.Length; i++)
            {
                k2d[i] = (char)(h2d[i] + codePointDifference);
            }
        }

        /// <summary>
        /// Constructor. Normalizes both kanji and kana iteration marks by default.
        /// </summary>
        /// <param name="input">Char stream.</param>
        public JapaneseIterationMarkCharFilter(TextReader input)
            : this(input, NORMALIZE_KANJI_DEFAULT, NORMALIZE_KANA_DEFAULT)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="input">Char stream.</param>
        /// <param name="normalizeKanji">Indicates whether kanji iteration marks should be normalized.</param>
        /// <param name="normalizeKana">Indicates whether kana iteration marks should be normalized.</param>
        public JapaneseIterationMarkCharFilter(TextReader input, bool normalizeKanji, bool normalizeKana)
            : base(input)
        {
            this.normalizeKanji = normalizeKanji;
            this.normalizeKana = normalizeKana;
            buffer.Reset(input);
        }

        /// <summary>
        /// Reads a specified maximum number of characters from the current reader and writes the data to a buffer, beginning at the specified index.
        /// </summary>
        /// <param name="buffer">
        /// When this method returns, contains the specified character array with the values between index and (index + count - 1) 
        /// replaced by the characters read from the current source.</param>
        /// <param name="offset">
        /// The position in buffer at which to begin writing.
        /// </param>
        /// <param name="length">
        /// The maximum number of characters to read. If the end of the reader is reached before the specified number of characters is 
        /// read into the buffer, the method returns.
        /// </param>
        /// <returns>
        /// The number of characters that have been read. The number will be less than or equal to count, depending on whether the data is 
        /// available within the reader. This method returns 0 (zero) if it is called when no more characters are left to read.
        /// </returns>
        public override int Read(char[] buffer, int offset, int length)
        {
            int read = 0;

            for (int i = offset; i < offset + length; i++)
            {
                int c = Read();
                if (c == -1)
                {
                    break;
                }
                buffer[i] = (char)c;
                read++;
            }

            return read == 0 ? -1 : read;
        }

        /// <summary>
        /// Reads the next character from the text reader and advances the character position by one character.
        /// </summary>
        /// <returns>The next character from the text reader, or -1 if no more characters are available.</returns>
        public override int Read()
        {
            int ic = buffer.Get(bufferPosition);

            // End of input
            if (ic == -1)
            {
                buffer.FreeBefore(bufferPosition);
                return ic;
            }

            char c = (char)ic;

            // Skip surrogate pair characters
            if (char.IsHighSurrogate(c) || char.IsLowSurrogate(c))
            {
                iterationMarkSpanEndPosition = bufferPosition + 1;
            }

            // Free rolling buffer on full stop
            if (c == FULL_STOP_PUNCTUATION)
            {
                buffer.FreeBefore(bufferPosition);
                iterationMarkSpanEndPosition = bufferPosition + 1;
            }

            // Normalize iteration mark
            if (IsIterationMark(c))
            {
                c = NormalizeIterationMark(c);
            }

            bufferPosition++;
            return c;
        }

        /// <summary>
        /// Normalizes the iteration mark character <paramref name="c"/>
        /// </summary>
        /// <param name="c">Iteration mark character to normalize.</param>
        /// <returns>Normalized iteration mark.</returns>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        private char NormalizeIterationMark(char c)
        {

            // Case 1: Inside an iteration mark span
            if (bufferPosition < iterationMarkSpanEndPosition)
            {
                return Normalize(SourceCharacter(bufferPosition, iterationMarksSpanSize), c);
            }

            // Case 2: New iteration mark spans starts where the previous one ended, which is illegal
            if (bufferPosition == iterationMarkSpanEndPosition)
            {
                // Emit the illegal iteration mark and increase end position to indicate that we can't
                // start a new span on the next position either
                iterationMarkSpanEndPosition++;
                return c;
            }

            // Case 3: New iteration mark span
            iterationMarksSpanSize = NextIterationMarkSpanSize();
            iterationMarkSpanEndPosition = bufferPosition + iterationMarksSpanSize;
            return Normalize(SourceCharacter(bufferPosition, iterationMarksSpanSize), c);
        }

        /// <summary>
        /// Finds the number of subsequent next iteration marks
        /// </summary>
        /// <returns>Number of iteration marks starting at the current buffer position.</returns>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        private int NextIterationMarkSpanSize()
        {
            int spanSize = 0;
            for (int i = bufferPosition; buffer.Get(i) != -1 && IsIterationMark((char)(buffer.Get(i))); i++)
            {
                spanSize++;
            }
            // Restrict span size so that we don't go past the previous end position
            if (bufferPosition - spanSize < iterationMarkSpanEndPosition)
            {
                spanSize = bufferPosition - iterationMarkSpanEndPosition;
            }
            return spanSize;
        }

        /// <summary>
        /// Returns the source character for a given position and iteration mark span size.
        /// </summary>
        /// <param name="position">Buffer position (should not exceed bufferPosition).</param>
        /// <param name="spanSize">Iteration mark span size.</param>
        /// <returns>Source character.</returns>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        private char SourceCharacter(int position, int spanSize)
        {
            return (char)buffer.Get(position - spanSize);
        }

        /// <summary>
        /// Normalize a character.
        /// </summary>
        /// <param name="c">Character to normalize.</param>
        /// <param name="m">Repetition mark referring to <paramref name="c"/>.</param>
        /// <returns>Normalized character - return c on illegal iteration marks.</returns>
        private char Normalize(char c, char m)
        {
            if (IsHiraganaIterationMark(m))
            {
                return NormalizedHiragana(c, m);
            }

            if (IsKatakanaIterationMark(m))
            {
                return NormalizedKatakana(c, m);
            }

            return c; // If m is not kana and we are to normalize it, we assume it is kanji and simply return it
        }

        /// <summary>
        /// Normalize hiragana character.
        /// </summary>
        /// <param name="c">Hiragana character.</param>
        /// <param name="m">Repetition mark referring to <paramref name="c"/>.</param>
        /// <returns>Normalized character - return <paramref name="c"/> on illegal iteration marks.</returns>
        private static char NormalizedHiragana(char c, char m) // LUCENENET: CA1822: Mark members as static
        {
            switch (m)
            {
                case HIRAGANA_ITERATION_MARK:
                    return IsHiraganaDakuten(c) ? (char)(c - 1) : c;
                case HIRAGANA_VOICED_ITERATION_MARK:
                    return LookupHiraganaDakuten(c);
                default:
                    return c;
            }
        }

        /// <summary>
        /// Normalize katakana character.
        /// </summary>
        /// <param name="c">Katakana character.</param>
        /// <param name="m">Repetition mark referring to <paramref name="c"/>.</param>
        /// <returns>Normalized character - return <paramref name="c"/> on illegal iteration marks.</returns>
        private static char NormalizedKatakana(char c, char m) // LUCENENET: CA1822: Mark members as static
        {
            switch (m)
            {
                case KATAKANA_ITERATION_MARK:
                    return IsKatakanaDakuten(c) ? (char)(c - 1) : c;
                case KATAKANA_VOICED_ITERATION_MARK:
                    return LookupKatakanaDakuten(c);
                default:
                    return c;
            }
        }

        /// <summary>
        /// Iteration mark character predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if <paramref name="c"/> is an iteration mark character.  Otherwise <c>false</c>.</returns>
        private bool IsIterationMark(char c)
        {
            return IsKanjiIterationMark(c) || IsHiraganaIterationMark(c) || IsKatakanaIterationMark(c);
        }

        /// <summary>
        /// Hiragana iteration mark character predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if <paramref name="c"/> is a hiragana iteration mark character.  Otherwise <c>false</c>.</returns>
        private bool IsHiraganaIterationMark(char c)
        {
            if (normalizeKana)
            {
                return c == HIRAGANA_ITERATION_MARK || c == HIRAGANA_VOICED_ITERATION_MARK;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Katakana iteration mark character predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if c is a katakana iteration mark character.  Otherwise <c>false</c>.</returns>
        private bool IsKatakanaIterationMark(char c)
        {
            if (normalizeKana)
            {
                return c == KATAKANA_ITERATION_MARK || c == KATAKANA_VOICED_ITERATION_MARK;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Kanji iteration mark character predicate.
        /// </summary>
        /// <param name="c">Character to test.</param>
        /// <returns><c>true</c> if c is a kanji iteration mark character.  Otherwise <c>false</c>.</returns>
        private bool IsKanjiIterationMark(char c)
        {
            if (normalizeKanji)
            {
                return c == KANJI_ITERATION_MARK;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Look up hiragana dakuten.
        /// </summary>
        /// <param name="c">Character to look up.</param>
        /// <returns>Hiragana dakuten variant of c or c itself if no dakuten variant exists.</returns>
        private static char LookupHiraganaDakuten(char c) // LUCENENET: CA1822: Mark members as static
        {
            return Lookup(c, h2d, '\u304b'); // Code point is for か
        }

        /// <summary>
        /// Look up katakana dakuten. Only full-width katakana are supported.
        /// </summary>
        /// <param name="c">Character to look up.</param>
        /// <returns>Katakana dakuten variant of <paramref name="c"/> or <paramref name="c"/> itself if no dakuten variant exists.</returns>
        private static char LookupKatakanaDakuten(char c) // LUCENENET: CA1822: Mark members as static
        {
            return Lookup(c, k2d, '\u30ab'); // Code point is for カ
        }

        /// <summary>
        /// Hiragana dakuten predicate.
        /// </summary>
        /// <param name="c">Character to check.</param>
        /// <returns><c>true</c> if c is a hiragana dakuten and otherwise <c>false</c>.</returns>
        private static bool IsHiraganaDakuten(char c) // LUCENENET: CA1822: Mark members as static
        {
            return Inside(c, h2d, '\u304b') && c == LookupHiraganaDakuten(c);
        }

        /// <summary>
        /// Katakana dakuten predicate.
        /// </summary>
        /// <param name="c">Character to check.</param>
        /// <returns><c>true</c> if c is a hiragana dakuten and otherwise <c>false</c>.</returns>
        private static bool IsKatakanaDakuten(char c) // LUCENENET: CA1822: Mark members as static
        {
            return Inside(c, k2d, '\u30ab') && c == LookupKatakanaDakuten(c);
        }

        /// <summary>
        /// Looks up a character in dakuten map and returns the dakuten variant if it exists.
        /// Otherwise return the character being looked up itself.
        /// </summary>
        /// <param name="c">Character to look up.</param>
        /// <param name="map">Dakuten map.</param>
        /// <param name="offset">Code point offset from <paramref name="c"/>.</param>
        /// <returns>Mapped character or <paramref name="c"/> if no mapping exists.</returns>
        private static char Lookup(char c, char[] map, char offset) // LUCENENET: CA1822: Mark members as static
        {
            if (!Inside(c, map, offset))
            {
                return c;
            }
            else
            {
                return map[c - offset];
            }
        }

        /// <summary>
        /// Predicate indicating if the lookup character is within dakuten map range.
        /// </summary>
        /// <param name="c">Character to look up.</param>
        /// <param name="map">Dakuten map.</param>
        /// <param name="offset">Code point offset from <paramref name="c"/>.</param>
        /// <returns><c>true</c> if <paramref name="c"/> is mapped by map and otherwise <c>false</c>.</returns>
        private static bool Inside(char c, char[] map, char offset) // LUCENENET: CA1822: Mark members as static
        {
            return c >= offset && c < offset + map.Length;
        }

        protected override int Correct(int currentOff)
        {
            return currentOff; // this filter doesn't change the length of strings
        }
    }
}
