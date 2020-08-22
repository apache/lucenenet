// Lucene version compatibility level 8.6.1
using ICU4N;
using ICU4N.Globalization;
using ICU4N.Text;

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
    /// An iterator that locates ISO 15924 script boundaries in text. 
    /// </summary>
    /// <remarks>
    /// This is not the same as simply looking at the Unicode block, or even the 
    /// Script property. Some characters are 'common' across multiple scripts, and
    /// some 'inherit' the script value of text surrounding them.
    /// <para/>
    /// This is similar to ICU (internal-only) UScriptRun, with the following
    /// differences:
    /// <list type="bullet">
    ///     <item><description>
    ///         Doesn't attempt to match paired punctuation. For tokenization purposes, this
    ///         is not necessary. Its also quite expensive. 
    ///     </description></item>
    ///     <item><description>
    ///         Non-spacing marks inherit the script of their base character, following 
    ///         recommendations from UTR #24.
    ///     </description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </remarks>
    internal sealed class ScriptIterator
    {
        private char[] text;
        private int start;
        private int limit;
        private int index;

        private int scriptStart;
        private int scriptLimit;
        private int scriptCode;

        private readonly bool combineCJ;

        /// <param name="combineCJ">if true: Han,Hiragana,Katakana will all return as <see cref="UScript.Japanese"/>.</param>
        internal ScriptIterator(bool combineCJ)
        {
            this.combineCJ = combineCJ;
        }

        /// <summary>
        /// Gets the start of this script run.
        /// </summary>
        public int ScriptStart => scriptStart;

        /// <summary>
        /// Get the index of the first character after the end of this script run.
        /// </summary>
        public int ScriptLimit => scriptLimit;

        /// <summary>
        /// Get the UScript script code for this script run.
        /// </summary>
        public int ScriptCode => scriptCode;

        /// <summary>
        /// Iterates to the next script run, returning true if one exists.
        /// </summary>
        /// <returns>true if there is another script run, false otherwise.</returns>
        public bool Next()
        {
            if (scriptLimit >= limit)
                return false;

            scriptCode = UScript.Common;
            scriptStart = scriptLimit;

            while (index < limit)
            {
                int ch = UTF16.CharAt(text, start, limit, index - start);
                int sc = GetScript(ch);

                /*
                 * From UTR #24: Implementations that determine the boundaries between
                 * characters of given scripts should never break between a non-spacing
                 * mark and its base character. Thus for boundary determinations and
                 * similar sorts of processing, a non-spacing mark — whatever its script
                 * value — should inherit the script value of its base character.
                 */
                if (IsSameScript(scriptCode, sc)
                    || UChar.GetUnicodeCategory(ch) == UUnicodeCategory.NonSpacingMark)
                {
                    index += UTF16.GetCharCount(ch);

                    /*
                     * Inherited or Common becomes the script code of the surrounding text.
                     */
                    if (scriptCode <= UScript.Inherited && sc > UScript.Inherited)
                    {
                        scriptCode = sc;
                    }

                }
                else
                {
                    break;
                }
            }

            scriptLimit = index;
            return true;
        }

        /// <summary>Determine if two scripts are compatible.</summary>
        private static bool IsSameScript(int scriptOne, int scriptTwo)
        {
            return scriptOne <= UScript.Inherited || scriptTwo <= UScript.Inherited
                || scriptOne == scriptTwo;
        }

        /// <summary>
        /// Set a new region of text to be examined by this iterator.
        /// </summary>
        /// <param name="text">Text buffer to examine.</param>
        /// <param name="start">Offset into buffer.</param>
        /// <param name="length">Maximum length to examine.</param>
        public void SetText(char[] text, int start, int length)
        {
            this.text = text;
            this.start = start;
            this.index = start;
            this.limit = start + length;
            this.scriptStart = start;
            this.scriptLimit = start;
            this.scriptCode = UScript.InvalidCode;
        }

        /// <summary>Linear fast-path for basic latin case.</summary>
        private static readonly int[] basicLatin = LoadBasicLatin();

        private static int[] LoadBasicLatin() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            var basicLatin = new int[128];
            for (int i = 0; i < basicLatin.Length; i++)
                basicLatin[i] = UScript.GetScript(i);
            return basicLatin;
        }

        /// <summary>Fast version of <see cref="UScript.GetScript(int)"/>. Basic Latin is an array lookup.</summary>
        private int GetScript(int codepoint)
        {
            if (0 <= codepoint && codepoint < basicLatin.Length)
            {
                return basicLatin[codepoint];
            }
            else
            {
                int script = UScript.GetScript(codepoint);
                if (combineCJ)
                {
                    if (script == UScript.Han || script == UScript.Hiragana || script == UScript.Katakana)
                    {
                        return UScript.Japanese;
                    }
                    else if (codepoint >= 0xFF10 && codepoint <= 0xFF19)
                    {
                        // when using CJK dictionary breaking, don't let full width numbers go to it, otherwise
                        // they are treated as punctuation. we currently have no cleaner way to fix this!
                        return UScript.Latin;
                    }
                    else
                    {
                        return script;
                    }
                }
                else
                {
                    return script;
                }
            }
        }
    }
}
