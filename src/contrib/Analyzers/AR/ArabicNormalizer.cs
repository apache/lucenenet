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

using System;
using System.IO;
using System.Collections;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;


namespace Lucene.Net.Analysis.AR
{
    /**
     *  Normalizer for Arabic.
     *  <p/>
     *  Normalization is done in-place for efficiency, operating on a termbuffer.
     *  <p/>
     *  Normalization is defined as:
     *  <ul>
     *  <li> Normalization of hamza with alef seat to a bare alef.</li>
     *  <li> Normalization of teh marbuta to heh</li>
     *  <li> Normalization of dotless yeh (alef maksura) to yeh.</li>
     *  <li> Removal of Arabic diacritics (the harakat)</li>
     *  <li> Removal of tatweel (stretching character).</li>
     * </ul>
     *
     */
    public class ArabicNormalizer
    {
        public static char ALEF = '\u0627';
        public static char ALEF_MADDA = '\u0622';
        public static char ALEF_HAMZA_ABOVE = '\u0623';
        public static char ALEF_HAMZA_BELOW = '\u0625';

        public static char YEH = '\u064A';
        public static char DOTLESS_YEH = '\u0649';

        public static char TEH_MARBUTA = '\u0629';
        public static char HEH = '\u0647';

        public static char TATWEEL = '\u0640';

        public static char FATHATAN = '\u064B';
        public static char DAMMATAN = '\u064C';
        public static char KASRATAN = '\u064D';
        public static char FATHA = '\u064E';
        public static char DAMMA = '\u064F';
        public static char KASRA = '\u0650';
        public static char SHADDA = '\u0651';
        public static char SUKUN = '\u0652';

        /**
         * Normalize an input buffer of Arabic text
         * 
         * <param name="s">input buffer</param>
         * <param name="len">length of input buffer</param>
         * <returns>length of input buffer after normalization</returns>
         */
        public int Normalize(char[] s, int len)
        {

            for (int i = 0; i < len; i++)
            {
                if (s[i] == ALEF_MADDA || s[i] == ALEF_HAMZA_ABOVE || s[i] == ALEF_HAMZA_BELOW)
                    s[i] = ALEF;

                if (s[i] == DOTLESS_YEH)
                    s[i] = YEH;

                if (s[i] == TEH_MARBUTA)
                    s[i] = HEH;

                if (s[i] == TATWEEL || s[i] == KASRATAN || s[i] == DAMMATAN || s[i] == FATHATAN ||
                    s[i] == FATHA || s[i] == DAMMA || s[i] == KASRA || s[i] == SHADDA || s[i] == SUKUN)
                {
                    len = Delete(s, i, len);
                    i--;
                }
            }

            return len;
        }

        /**
         * Delete a character in-place
         * 
         * <param name="s">Input Buffer</param>
         * <param name="pos">Position of character to delete</param>
         * <param name="len">length of input buffer</param>
         * <returns>length of input buffer after deletion</returns>
         */
        protected int Delete(char[] s, int pos, int len)
        {
            if (pos < len)
                Array.Copy(s, pos + 1, s, pos, len - pos - 1); 

            return len - 1;
        }

    }
}