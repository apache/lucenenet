// commons-codec version compatibility level: 1.9
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Phonetic.Language
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
    /// Encodes a string into a Caverphone 1.0 value.
    /// <para/>
    /// This is an algorithm created by the Caversham Project at the University of Otago. It implements the Caverphone 1.0
    /// algorithm:
    /// <para/>
    /// See: <a href="http://en.wikipedia.org/wiki/Caverphone">Wikipedia - Caverphone</a>
    /// <para/>
    /// See: <a href="http://caversham.otago.ac.nz/files/working/ctp060902.pdf">Caverphone 1.0 specification</a>
    /// <para/>
    /// This class is immutable and thread-safe.
    /// <para/>
    /// since 1.5
    /// </summary>
    public class Caverphone1 : AbstractCaverphone
    {
        private static readonly string SIX_1 = "111111";

        /// <summary>
        /// Encodes the given string into a Caverphone value.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <returns>A caverphone code for the given string.</returns>
        public override string Encode(string source)
        {
            string txt = source;
            if (txt == null || txt.Length == 0)
            {
                return SIX_1;
            }

            // 1. Convert to lowercase
            txt = txt.ToLowerInvariant(); // LUCENENET NOTE: This doesn't work right under "en" language, but does under invariant

            // 2. Remove anything not A-Z
            txt = Regex.Replace(txt, "[^a-z]", "");

            // 3. Handle various start options
            // 2 is a temporary placeholder to indicate a consonant which we are no longer interested in.
            txt = Regex.Replace(txt, "^cough", "cou2f");
            txt = Regex.Replace(txt, "^rough", "rou2f");
            txt = Regex.Replace(txt, "^tough", "tou2f");
            txt = Regex.Replace(txt, "^enough", "enou2f");
            txt = Regex.Replace(txt, "^gn", "2n");

            // End
            txt = Regex.Replace(txt, "mb$", "m2");

            // 4. Handle replacements
            txt = Regex.Replace(txt, "cq", "2q");
            txt = Regex.Replace(txt, "ci", "si");
            txt = Regex.Replace(txt, "ce", "se");
            txt = Regex.Replace(txt, "cy", "sy");
            txt = Regex.Replace(txt, "tch", "2ch");
            txt = Regex.Replace(txt, "c", "k");
            txt = Regex.Replace(txt, "q", "k");
            txt = Regex.Replace(txt, "x", "k");
            txt = Regex.Replace(txt, "v", "f");
            txt = Regex.Replace(txt, "dg", "2g");
            txt = Regex.Replace(txt, "tio", "sio");
            txt = Regex.Replace(txt, "tia", "sia");
            txt = Regex.Replace(txt, "d", "t");
            txt = Regex.Replace(txt, "ph", "fh");
            txt = Regex.Replace(txt, "b", "p");
            txt = Regex.Replace(txt, "sh", "s2");
            txt = Regex.Replace(txt, "z", "s");
            txt = Regex.Replace(txt, "^[aeiou]", "A");
            // 3 is a temporary placeholder marking a vowel
            txt = Regex.Replace(txt, "[aeiou]", "3");
            txt = Regex.Replace(txt, "3gh3", "3kh3");
            txt = Regex.Replace(txt, "gh", "22");
            txt = Regex.Replace(txt, "g", "k");
            txt = Regex.Replace(txt, "s+", "S");
            txt = Regex.Replace(txt, "t+", "T");
            txt = Regex.Replace(txt, "p+", "P");
            txt = Regex.Replace(txt, "k+", "K");
            txt = Regex.Replace(txt, "f+", "F");
            txt = Regex.Replace(txt, "m+", "M");
            txt = Regex.Replace(txt, "n+", "N");
            txt = Regex.Replace(txt, "w3", "W3");
            txt = Regex.Replace(txt, "wy", "Wy"); // 1.0 only
            txt = Regex.Replace(txt, "wh3", "Wh3");
            txt = Regex.Replace(txt, "why", "Why"); // 1.0 only
            txt = Regex.Replace(txt, "w", "2");
            txt = Regex.Replace(txt, "^h", "A");
            txt = Regex.Replace(txt, "h", "2");
            txt = Regex.Replace(txt, "r3", "R3");
            txt = Regex.Replace(txt, "ry", "Ry"); // 1.0 only
            txt = Regex.Replace(txt, "r", "2");
            txt = Regex.Replace(txt, "l3", "L3");
            txt = Regex.Replace(txt, "ly", "Ly"); // 1.0 only
            txt = Regex.Replace(txt, "l", "2");
            txt = Regex.Replace(txt, "j", "y"); // 1.0 only
            txt = Regex.Replace(txt, "y3", "Y3"); // 1.0 only
            txt = Regex.Replace(txt, "y", "2"); // 1.0 only

            // 5. Handle removals
            txt = Regex.Replace(txt, "2", "");
            txt = Regex.Replace(txt, "3", "");

            // 6. put ten 1s on the end
            txt = txt + SIX_1;

            // 7. take the first six characters as the code
            return txt.Substring(0, SIX_1.Length - 0);
        }
    }
}
