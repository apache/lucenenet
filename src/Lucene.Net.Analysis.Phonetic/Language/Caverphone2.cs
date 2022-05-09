// commons-codec version compatibility level: 1.9
using System.Globalization;

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
    /// Encodes a string into a Caverphone 2.0 value.
    /// <para/>
    /// This is an algorithm created by the Caversham Project at the University of Otago. It implements the Caverphone 2.0
    /// algorithm:
    /// <para/>
    /// See: <a href="http://en.wikipedia.org/wiki/Caverphone">Wikipedia - Caverphone</a>
    /// <para/>
    /// See: <a href="http://caversham.otago.ac.nz/files/working/ctp150804.pdf">Caverphone 2.0 specification</a>
    /// <para/>
    /// This class is immutable and thread-safe.
    /// </summary>
    public class Caverphone2 : AbstractCaverphone
    {
        private const string TEN_1 = "1111111111";

        private static readonly CultureInfo LOCALE_ENGLISH = new CultureInfo("en");

        /// <summary>
        /// Encodes the given string into a Caverphone 2.0 value.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <returns>A caverphone code for the given string.</returns>
        public override string Encode(string source)
        {
            string txt = source;
            if (txt is null || txt.Length == 0)
            {
                return TEN_1;
            }

            // 1. Convert to lowercase
            txt = LOCALE_ENGLISH.TextInfo.ToLower(txt);

            // 2. - 5 - Use pre-compiled regexes for replacements
            foreach (var replacement in REPLACEMENTS)
            {
                txt = replacement.Replace(txt);
            }

            // 6. put ten 1s on the end
            txt = txt + TEN_1;

            // 7. take the first ten characters as the code
            return txt.Substring(0, TEN_1.Length);
        }

        private static readonly Replacement[] REPLACEMENTS = new Replacement[]
        {
            // 2. Remove anything not A-Z
            new Replacement("[^a-z]", string.Empty),

            // 2.5. Remove final e
            new Replacement("e$", string.Empty), // 2.0 only

            // 3. Handle various start options
            new Replacement("^cough", "cou2f"),
            new Replacement("^rough", "rou2f"),
            new Replacement("^tough", "tou2f"),
            new Replacement("^enough", "enou2f"), // 2.0 only
            new Replacement("^trough", "trou2f"), // 2.0 only
                                                       // note the spec says ^enough here again, c+p error I assume
            new Replacement("^gn", "2n"),

            // End
            new Replacement("mb$", "m2"),

            // 4. Handle replacements
            new Replacement("cq", "2q"),
            new Replacement("ci", "si"),
            new Replacement("ce", "se"),
            new Replacement("cy", "sy"),
            new Replacement("tch", "2ch"),
            new Replacement("c", "k"),
            new Replacement("q", "k"),
            new Replacement("x", "k"),
            new Replacement("v", "f"),
            new Replacement("dg", "2g"),
            new Replacement("tio", "sio"),
            new Replacement("tia", "sia"),
            new Replacement("d", "t"),
            new Replacement("ph", "fh"),
            new Replacement("b", "p"),
            new Replacement("sh", "s2"),
            new Replacement("z", "s"),
            new Replacement("^[aeiou]", "A"),
            new Replacement("[aeiou]", "3"),
            new Replacement("j", "y"), // 2.0 only
            new Replacement("^y3", "Y3"), // 2.0 only
            new Replacement("^y", "A"), // 2.0 only
            new Replacement("y", "3"), // 2.0 only
            new Replacement("3gh3", "3kh3"),
            new Replacement("gh", "22"),
            new Replacement("g", "k"),
            new Replacement("s+", "S"),
            new Replacement("t+", "T"),
            new Replacement("p+", "P"),
            new Replacement("k+", "K"),
            new Replacement("f+", "F"),
            new Replacement("m+", "M"),
            new Replacement("n+", "N"),
            new Replacement("w3", "W3"),
            new Replacement("wh3", "Wh3"),
            new Replacement("w$", "3"), // 2.0 only
            new Replacement("w", "2"),
            new Replacement("^h", "A"),
            new Replacement("h", "2"),
            new Replacement("r3", "R3"),
            new Replacement("r$", "3"), // 2.0 only
            new Replacement("r", "2"),
            new Replacement("l3", "L3"),
            new Replacement("l$", "3"), // 2.0 only
            new Replacement("l", "2"),

            // 5. Handle removals
            new Replacement("2", string.Empty),
            new Replacement("3$", "A"), // 2.0 only
            new Replacement("3", string.Empty),
        };
    }
}
