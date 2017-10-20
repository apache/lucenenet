// LUCENENET TODO: Port issues - missing dependencies

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU.Segmentation
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// An iterator that locates ISO 15924 script boundaries in text. 
//    /// </summary>
//    /// <remarks>
//    /// This is not the same as simply looking at the Unicode block, or even the 
//    /// Script property. Some characters are 'common' across multiple scripts, and
//    /// some 'inherit' the script value of text surrounding them.
//    /// <para/>
//    /// This is similar to ICU (internal-only) UScriptRun, with the following
//    /// differences:
//    /// <list type="bullet">
//    ///     <item><description>
//    ///         Doesn't attempt to match paired punctuation. For tokenization purposes, this
//    ///         is not necessary. Its also quite expensive. 
//    ///     </description></item>
//    ///     <item><description>
//    ///         Non-spacing marks inherit the script of their base character, following 
//    ///         recommendations from UTR #24.
//    ///     </description></item>
//    /// </list>
//    /// <para/>
//    /// @lucene.experimental
//    /// </remarks>
//    internal sealed class ScriptIterator
//    {
//        private char[] text;
//        private int start;
//        private int limit;
//        private int index;

//        private int scriptStart;
//        private int scriptLimit;
//        private int scriptCode;

//        private readonly bool combineCJ;

//        /**
//         * @param combineCJ if true: Han,Hiragana,Katakana will all return as {@link UScript#JAPANESE}
//         */
//        internal ScriptIterator(bool combineCJ)
//        {
//            this.combineCJ = combineCJ;
//        }

//        /**
//         * Get the start of this script run
//         * 
//         * @return start position of script run
//         */
//        public int ScriptStart
//        {
//            get { return scriptStart; }
//        }

//        /**
//         * Get the index of the first character after the end of this script run
//         * 
//         * @return position of the first character after this script run
//         */
//        public int ScriptLimit
//        {
//            get { return scriptLimit; }
//        }

//        /**
//         * Get the UScript script code for this script run
//         * 
//         * @return code for the script of the current run
//         */
//        public int ScriptCode
//        {
//            get { return scriptCode; }
//        }

//        /**
//         * Iterates to the next script run, returning true if one exists.
//         * 
//         * @return true if there is another script run, false otherwise.
//         */
//        public bool Next()
//        {
//            if (scriptLimit >= limit)
//                return false;

//            scriptCode = UScript.COMMON;
//            scriptStart = scriptLimit;

//            while (index < limit)
//            {
//                //int ch = UTF16.charAt(text, start, limit, index - start);
//                int ch = Encoding.Unicode.(text, start, limit);
//                int sc = GetScript(ch);

//                /*
//                 * From UTR #24: Implementations that determine the boundaries between
//                 * characters of given scripts should never break between a non-spacing
//                 * mark and its base character. Thus for boundary determinations and
//                 * similar sorts of processing, a non-spacing mark — whatever its script
//                 * value — should inherit the script value of its base character.
//                 */
//                if (isSameScript(scriptCode, sc)
//                    || UCharacter.getType(ch) == ECharacterCategory.NON_SPACING_MARK)
//                {
//                    //index += UTF16.getCharCount(ch);
//                    index += Encoding.Unicode.GetCharCount()

//                    /*
//                     * Inherited or Common becomes the script code of the surrounding text.
//                     */
//                    if (scriptCode <= UScript.INHERITED && sc > UScript.INHERITED)
//                    {
//                        scriptCode = sc;
//                    }

//                }
//                else
//                {
//                    break;
//                }
//            }

//            scriptLimit = index;
//            return true;
//        }

//        /** Determine if two scripts are compatible. */
//        private static bool IsSameScript(int scriptOne, int scriptTwo)
//        {
//            return scriptOne <= UScript.INHERITED || scriptTwo <= UScript.INHERITED
//                || scriptOne == scriptTwo;
//        }

//        /**
//         * Set a new region of text to be examined by this iterator
//         * 
//         * @param text text buffer to examine
//         * @param start offset into buffer
//         * @param length maximum length to examine
//         */
//        public void SetText(char[] text, int start, int length)
//        {
//            this.text = text;
//            this.start = start;
//            this.index = start;
//            this.limit = start + length;
//            this.scriptStart = start;
//            this.scriptLimit = start;
//            this.scriptCode = UScript.INVALID_CODE;
//        }

//        /** linear fast-path for basic latin case */
//        private static readonly int[] basicLatin = new int[128];

//        static ScriptIterator()
//        {
//            for (int i = 0; i < basicLatin.Length; i++)
//                basicLatin[i] = UScript.GetScript(i);
//        }

//        /** fast version of UScript.getScript(). Basic Latin is an array lookup */
//        private int GetScript(int codepoint)
//        {
//            if (0 <= codepoint && codepoint < basicLatin.Length)
//            {
//                return basicLatin[codepoint];
//            }
//            else
//            {
//                //int script = UScript.GetScript(codepoint);
//                if (combineCJ)
//                {
//                    if (Regex.IsMatch(new string(Support.Character.ToChars(codepoint)), @"\p{IsHangulCompatibilityJamo}+|\p{IsHiragana}+|\p{IsKatakana}+"))
//                    //if (script == UScript.HAN || script == UScript.HIRAGANA || script == UScript.KATAKANA)
//                    {
//                        return UScript.JAPANESE;
//                    }
//                    else if (codepoint >= 0xFF10 && codepoint <= 0xFF19)
//                    {
//                        // when using CJK dictionary breaking, don't let full width numbers go to it, otherwise
//                        // they are treated as punctuation. we currently have no cleaner way to fix this!
//                        return UScript.LATIN;
//                    }
//                    else
//                    {
//                        return script;
//                    }
//                }
//                else
//                {
//                    return script;
//                }
//            }
//        }
//    }
//}
