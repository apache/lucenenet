using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.In
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
    /// Normalizes the Unicode representation of text in Indian languages.
    /// <para>
    /// Follows guidelines from Unicode 5.2, chapter 6, South Asian Scripts I
    /// and graphical decompositions from http://ldc.upenn.edu/myl/IndianScriptsUnicode.html
    /// </para>
    /// </summary>
    public class IndicNormalizer
    {
        // LUCENENET NOTE: This class was refactored from its Java couterpart,
        // favoring the .NET Regex class to determine the "Unicode Block" rather than 
        // porting over that part of the Java Character class.
        // References: 
        // https://msdn.microsoft.com/en-us/library/20bw873z.aspx#SupportedNamedBlocks
        // http://stackoverflow.com/a/11414800/181087

        private class ScriptData
        {
            internal readonly UnicodeBlock flag;
            internal readonly int @base;
            internal OpenBitSet decompMask;

            internal ScriptData(UnicodeBlock flag, int @base)
            {
                this.flag = flag;
                this.@base = @base;
            }
        }

        private static readonly IDictionary<Regex, ScriptData> scripts = new Dictionary<Regex, ScriptData>();

        [Flags]
        internal enum UnicodeBlock
        {
            DEVANAGARI = 1,
            BENGALI = 2,
            GURMUKHI = 4,
            GUJARATI = 8,
            ORIYA = 16,
            TAMIL = 32,
            TELUGU = 64,
            KANNADA = 128,
            MALAYALAM = 256
        }

        static IndicNormalizer()
        {
            // Initialize jagged array
            decompositions = new int[72][]
            {
                /* devanagari, gujarati vowel candra O */
                new int[] { 0x05, 0x3E, 0x45, 0x11, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI },
                /* devanagari short O */
                new int[] { 0x05, 0x3E, 0x46, 0x12, (int)UnicodeBlock.DEVANAGARI }, 
                /* devanagari, gujarati letter O */
                new int[] { 0x05, 0x3E, 0x47, 0x13, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI },
                /* devanagari letter AI, gujarati letter AU */
                new int[] { 0x05, 0x3E, 0x48, 0x14, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI }, 
                /* devanagari, bengali, gurmukhi, gujarati, oriya AA */
                new int[] { 0x05, 0x3E,   -1, 0x06, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.BENGALI | (int)UnicodeBlock.GURMUKHI | (int)UnicodeBlock.GUJARATI | (int)UnicodeBlock.ORIYA }, 
                /* devanagari letter candra A */
                new int[] { 0x05, 0x45,   -1, 0x72, (int)UnicodeBlock.DEVANAGARI },
                /* gujarati vowel candra E */
                new int[] { 0x05, 0x45,   -1, 0x0D, (int)UnicodeBlock.GUJARATI },
                /* devanagari letter short A */
                new int[] { 0x05, 0x46,   -1, 0x04, (int)UnicodeBlock.DEVANAGARI },
                /* gujarati letter E */
                new int[] { 0x05, 0x47,   -1, 0x0F, (int)UnicodeBlock.GUJARATI }, 
                /* gurmukhi, gujarati letter AI */
                new int[] { 0x05, 0x48,   -1, 0x10, (int)UnicodeBlock.GURMUKHI | (int)UnicodeBlock.GUJARATI }, 
                /* devanagari, gujarati vowel candra O */
                new int[] { 0x05, 0x49,   -1, 0x11, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI }, 
                /* devanagari short O */
                new int[] { 0x05, 0x4A,   -1, 0x12, (int)UnicodeBlock.DEVANAGARI }, 
                /* devanagari, gujarati letter O */
                new int[] { 0x05, 0x4B,   -1, 0x13, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI }, 
                /* devanagari letter AI, gurmukhi letter AU, gujarati letter AU */
                new int[] { 0x05, 0x4C,   -1, 0x14, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GURMUKHI | (int)UnicodeBlock.GUJARATI }, 
                /* devanagari, gujarati vowel candra O */
                new int[] { 0x06, 0x45,   -1, 0x11, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI },  
                /* devanagari short O */
                new int[] { 0x06, 0x46,   -1, 0x12, (int)UnicodeBlock.DEVANAGARI },
                /* devanagari, gujarati letter O */
                new int[] { 0x06, 0x47,   -1, 0x13, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI },
                /* devanagari letter AI, gujarati letter AU */
                new int[] { 0x06, 0x48,   -1, 0x14, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI },
                /* malayalam letter II */
                new int[] { 0x07, 0x57,   -1, 0x08, (int)UnicodeBlock.MALAYALAM },
                /* devanagari letter UU */
                new int[] { 0x09, 0x41,   -1, 0x0A, (int)UnicodeBlock.DEVANAGARI },
                /* tamil, malayalam letter UU (some styles) */
                new int[] { 0x09, 0x57,   -1, 0x0A, (int)UnicodeBlock.TAMIL | (int)UnicodeBlock.MALAYALAM },
                /* malayalam letter AI */
                new int[] { 0x0E, 0x46,   -1, 0x10, (int)UnicodeBlock.MALAYALAM },
                /* devanagari candra E */
                new int[] { 0x0F, 0x45,   -1, 0x0D, (int)UnicodeBlock.DEVANAGARI }, 
                /* devanagari short E */
                new int[] { 0x0F, 0x46,   -1, 0x0E, (int)UnicodeBlock.DEVANAGARI },
                /* devanagari AI */
                new int[] { 0x0F, 0x47,   -1, 0x10, (int)UnicodeBlock.DEVANAGARI },
                /* oriya AI */
                new int[] { 0x0F, 0x57,   -1, 0x10, (int)UnicodeBlock.ORIYA },
                /* malayalam letter OO */
                new int[] { 0x12, 0x3E,   -1, 0x13, (int)UnicodeBlock.MALAYALAM }, 
                /* telugu, kannada letter AU */
                new int[] { 0x12, 0x4C,   -1, 0x14, (int)UnicodeBlock.TELUGU | (int)UnicodeBlock.KANNADA }, 
                /* telugu letter OO */
                new int[] { 0x12, 0x55,   -1, 0x13, (int)UnicodeBlock.TELUGU },
                /* tamil, malayalam letter AU */
                new int[] { 0x12, 0x57,   -1, 0x14, (int)UnicodeBlock.TAMIL | (int)UnicodeBlock.MALAYALAM },
                /* oriya letter AU */
                new int[] { 0x13, 0x57,   -1, 0x14, (int)UnicodeBlock.ORIYA },
                /* devanagari qa */
                new int[] { 0x15, 0x3C,   -1, 0x58, (int)UnicodeBlock.DEVANAGARI },
                /* devanagari, gurmukhi khha */
                new int[] { 0x16, 0x3C,   -1, 0x59, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GURMUKHI },
                /* devanagari, gurmukhi ghha */
                new int[] { 0x17, 0x3C,   -1, 0x5A, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GURMUKHI },
                /* devanagari, gurmukhi za */
                new int[] { 0x1C, 0x3C,   -1, 0x5B, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GURMUKHI },
                /* devanagari dddha, bengali, oriya rra */
                new int[] { 0x21, 0x3C,   -1, 0x5C, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.BENGALI | (int)UnicodeBlock.ORIYA },
                /* devanagari, bengali, oriya rha */
                new int[] { 0x22, 0x3C,   -1, 0x5D, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.BENGALI | (int)UnicodeBlock.ORIYA },
                /* malayalam chillu nn */
                new int[] { 0x23, 0x4D, 0xFF, 0x7A, (int)UnicodeBlock.MALAYALAM },
                /* bengali khanda ta */
                new int[] { 0x24, 0x4D, 0xFF, 0x4E, (int)UnicodeBlock.BENGALI },
                /* devanagari nnna */
                new int[] { 0x28, 0x3C,   -1, 0x29, (int)UnicodeBlock.DEVANAGARI },
                /* malayalam chillu n */
                new int[] { 0x28, 0x4D, 0xFF, 0x7B, (int)UnicodeBlock.MALAYALAM },
                /* devanagari, gurmukhi fa */
                new int[] { 0x2B, 0x3C,   -1, 0x5E, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GURMUKHI },
                /* devanagari, bengali yya */
                new int[] { 0x2F, 0x3C,   -1, 0x5F, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.BENGALI },
                /* telugu letter vocalic R */
                new int[] { 0x2C, 0x41, 0x41, 0x0B, (int)UnicodeBlock.TELUGU },
                /* devanagari rra */
                new int[] { 0x30, 0x3C,   -1, 0x31, (int)UnicodeBlock.DEVANAGARI },
                /* malayalam chillu rr */
                new int[] { 0x30, 0x4D, 0xFF, 0x7C, (int)UnicodeBlock.MALAYALAM },
                /* malayalam chillu l */
                new int[] { 0x32, 0x4D, 0xFF, 0x7D, (int)UnicodeBlock.MALAYALAM },
                /* devanagari llla */
                new int[] { 0x33, 0x3C,   -1, 0x34, (int)UnicodeBlock.DEVANAGARI },
                /* malayalam chillu ll */
                new int[] { 0x33, 0x4D, 0xFF, 0x7E, (int)UnicodeBlock.MALAYALAM },
                /* telugu letter MA */ 
                new int[] { 0x35, 0x41,   -1, 0x2E, (int)UnicodeBlock.TELUGU },
                /* devanagari, gujarati vowel sign candra O */
                new int[] { 0x3E, 0x45,   -1, 0x49, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI },
                /* devanagari vowel sign short O */
                new int[] { 0x3E, 0x46,   -1, 0x4A, (int)UnicodeBlock.DEVANAGARI },
                /* devanagari, gujarati vowel sign O */
                new int[] { 0x3E, 0x47,   -1, 0x4B, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI },
                /* devanagari, gujarati vowel sign AU */ 
                new int[] { 0x3E, 0x48,   -1, 0x4C, (int)UnicodeBlock.DEVANAGARI | (int)UnicodeBlock.GUJARATI },
                /* kannada vowel sign II */ 
                new int[] { 0x3F, 0x55,   -1, 0x40, (int)UnicodeBlock.KANNADA },
                /* gurmukhi vowel sign UU (when stacking) */
                new int[] { 0x41, 0x41,   -1, 0x42, (int)UnicodeBlock.GURMUKHI },
                /* tamil, malayalam vowel sign O */
                new int[] { 0x46, 0x3E,   -1, 0x4A, (int)UnicodeBlock.TAMIL | (int)UnicodeBlock.MALAYALAM },
                /* kannada vowel sign OO */
                new int[] { 0x46, 0x42, 0x55, 0x4B, (int)UnicodeBlock.KANNADA },
                /* kannada vowel sign O */
                new int[] { 0x46, 0x42,   -1, 0x4A, (int)UnicodeBlock.KANNADA },
                /* malayalam vowel sign AI (if reordered twice) */
                new int[]  { 0x46, 0x46,   -1, 0x48, (int)UnicodeBlock.MALAYALAM },
                /* telugu, kannada vowel sign EE */
                new int[] { 0x46, 0x55,   -1, 0x47, (int)UnicodeBlock.TELUGU | (int)UnicodeBlock.KANNADA },
                /* telugu, kannada vowel sign AI */
                new int[] { 0x46, 0x56,   -1, 0x48, (int)UnicodeBlock.TELUGU | (int)UnicodeBlock.KANNADA },
                /* tamil, malayalam vowel sign AU */
                new int[] { 0x46, 0x57,   -1, 0x4C, (int)UnicodeBlock.TAMIL | (int)UnicodeBlock.MALAYALAM },
                /* bengali, oriya vowel sign O, tamil, malayalam vowel sign OO */
                new int[] { 0x47, 0x3E,   -1, 0x4B, (int)UnicodeBlock.BENGALI | (int)UnicodeBlock.ORIYA | (int)UnicodeBlock.TAMIL | (int)UnicodeBlock.MALAYALAM },
                /* bengali, oriya vowel sign AU */
                new int[] { 0x47, 0x57,   -1, 0x4C, (int)UnicodeBlock.BENGALI | (int)UnicodeBlock.ORIYA },
                /* kannada vowel sign OO */   
                new int[] { 0x4A, 0x55,   -1, 0x4B, (int)UnicodeBlock.KANNADA },
                /* gurmukhi letter I */
                new int[] { 0x72, 0x3F,   -1, 0x07, (int)UnicodeBlock.GURMUKHI },
                /* gurmukhi letter II */
                new int[] { 0x72, 0x40,   -1, 0x08, (int)UnicodeBlock.GURMUKHI },
                /* gurmukhi letter EE */
                new int[] { 0x72, 0x47,   -1, 0x0F, (int)UnicodeBlock.GURMUKHI },
                /* gurmukhi letter U */
                new int[] { 0x73, 0x41,   -1, 0x09, (int)UnicodeBlock.GURMUKHI },
                /* gurmukhi letter UU */
                new int[] { 0x73, 0x42,   -1, 0x0A, (int)UnicodeBlock.GURMUKHI },
                /* gurmukhi letter OO */
                new int[] { 0x73, 0x4B,   -1, 0x13, (int)UnicodeBlock.GURMUKHI }
            };

            scripts[new Regex(@"\p{IsDevanagari}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.DEVANAGARI, 0x0900);
            scripts[new Regex(@"\p{IsBengali}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.BENGALI, 0x0980);
            scripts[new Regex(@"\p{IsGurmukhi}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.GURMUKHI, 0x0A00);
            scripts[new Regex(@"\p{IsGujarati}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.GUJARATI, 0x0A80);
            scripts[new Regex(@"\p{IsOriya}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.ORIYA, 0x0B00);
            scripts[new Regex(@"\p{IsTamil}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.TAMIL, 0x0B80);
            scripts[new Regex(@"\p{IsTelugu}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.TELUGU, 0x0C00);
            scripts[new Regex(@"\p{IsKannada}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.KANNADA, 0x0C80);
            scripts[new Regex(@"\p{IsMalayalam}", RegexOptions.Compiled)] = new ScriptData(UnicodeBlock.MALAYALAM, 0x0D00);

            foreach (ScriptData sd in scripts.Values)
            {
                sd.decompMask = new OpenBitSet(0x7F);
                for (int i = 0; i < decompositions.Length; i++)
                {
                    int ch = decompositions[i][0];
                    int flags = decompositions[i][4];
                    if ((flags & (int)sd.flag) != 0)
                    {
                        sd.decompMask.Set(ch);
                    }
                }
            }
        }

        /// <summary>
        /// Decompositions according to Unicode 5.2, 
        /// and http://ldc.upenn.edu/myl/IndianScriptsUnicode.html
        /// 
        /// Most of these are not handled by unicode normalization anyway.
        /// 
        /// The numbers here represent offsets into the respective codepages,
        /// with -1 representing null and 0xFF representing zero-width joiner.
        /// 
        /// the columns are: ch1, ch2, ch3, res, flags
        /// ch1, ch2, and ch3 are the decomposition
        /// res is the composition, and flags are the scripts to which it applies.
        /// </summary>
        private static readonly int[][] decompositions = { };


        /// <summary>
        /// Normalizes input text, and returns the new length.
        /// The length will always be less than or equal to the existing length.
        /// </summary>
        /// <param name="text"> input text </param>
        /// <param name="len"> valid length </param>
        /// <returns> normalized length </returns>
        public virtual int Normalize(char[] text, int len)
        {
            for (int i = 0; i < len; i++)
            {
                var block = GetBlockForChar(text[i]);
                ScriptData sd;
                if (scripts.TryGetValue(block, out sd) && sd != null)
                {
                    int ch = text[i] - sd.@base;
                    if (sd.decompMask.Get(ch))
                    {
                        len = Compose(ch, block, sd, text, i, len);
                    }
                }
            }
            return len;
        }

        /// <summary>
        /// Compose into standard form any compositions in the decompositions table.
        /// </summary>
        private int Compose(int ch0, Regex block0, ScriptData sd, char[] text, int pos, int len)
        {
            if (pos + 1 >= len) // need at least 2 chars!
            {
                return len;
            }

            int ch1 = text[pos + 1] - sd.@base;
            var block1 = GetBlockForChar(text[pos + 1]);
            if (block1 != block0) // needs to be the same writing system
            {
                return len;
            }

            int ch2 = -1;

            if (pos + 2 < len)
            {
                ch2 = text[pos + 2] - sd.@base;
                var block2 = GetBlockForChar(text[pos + 2]);
                if (text[pos + 2] == '\u200D') // ZWJ
                {
                    ch2 = 0xFF;
                }
                else if (block2 != block1) // still allow a 2-char match
                {
                    ch2 = -1;
                }
            }

            for (int i = 0; i < decompositions.Length; i++)
            {
                if (decompositions[i][0] == ch0 && (decompositions[i][4] & (int)sd.flag) != 0)
                {
                    if (decompositions[i][1] == ch1 && (decompositions[i][2] < 0 || decompositions[i][2] == ch2))
                    {
                        text[pos] = (char)(sd.@base + decompositions[i][3]);
                        len = StemmerUtil.Delete(text, pos + 1, len);
                        if (decompositions[i][2] >= 0)
                        {
                            len = StemmerUtil.Delete(text, pos + 1, len);
                        }
                        return len;
                    }
                }
            }

            return len;
        }

        /// <summary>
        /// LUCENENET: Returns the unicode block for the specified character
        /// </summary>
        private Regex GetBlockForChar(char c)
        {
            string charAsString = c.ToString();
            foreach (var block in scripts.Keys)
            {
                if (block.IsMatch(charAsString))
                {
                    return block;
                }
            }

            // return a regex that never matches, nor is in our scripts dictionary
            return new Regex(@"[^\S\s]");
        }
    }
}