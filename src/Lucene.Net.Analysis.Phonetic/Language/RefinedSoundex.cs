// commons-codec version compatibility level: 1.9
using Lucene.Net.Support;
using System.Globalization;
using System.Text;

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
    /// Encodes a string into a Refined Soundex value. A refined soundex code is
    /// optimized for spell checking words. Soundex method originally developed by
    /// <c>Margaret Odell</c> and <c>Robert Russell</c>.
    /// <para/>
    /// This class is immutable and thread-safe.
    /// </summary>
    public class RefinedSoundex : IStringEncoder
    {
        /// <summary>
        /// since 1.4
        /// </summary>
        public static readonly string US_ENGLISH_MAPPING_STRING = "01360240043788015936020505";

        /// <summary>
        /// RefinedSoundex is *refined* for a number of reasons one being that the
        /// mappings have been altered. This implementation contains default
        /// mappings for US English.
        /// </summary>
        private static readonly char[] US_ENGLISH_MAPPING = US_ENGLISH_MAPPING_STRING.ToCharArray();

        /// <summary>
        /// Every letter of the alphabet is "mapped" to a numerical value. This char
        /// array holds the values to which each letter is mapped. This
        /// implementation contains a default map for US_ENGLISH.
        /// </summary>
        private readonly char[] soundexMapping;

        /// <summary>
        /// This static variable contains an instance of the RefinedSoundex using
        /// the US_ENGLISH mapping.
        /// </summary>
        public static readonly RefinedSoundex US_ENGLISH = new RefinedSoundex();

        /// <summary>
        /// Creates an instance of the <see cref="RefinedSoundex"/> object using the default US
        /// English mapping.
        /// </summary>
        public RefinedSoundex()
        {
            this.soundexMapping = US_ENGLISH_MAPPING;
        }

        /// <summary>
        /// Creates a refined soundex instance using a custom mapping. This
        /// constructor can be used to customize the mapping, and/or possibly
        /// provide an internationalized mapping for a non-Western character set.
        /// </summary>
        /// <param name="mapping">Mapping array to use when finding the corresponding code for a given character.</param>
        public RefinedSoundex(char[] mapping)
        {
            this.soundexMapping = new char[mapping.Length];
            Arrays.Copy(mapping, 0, this.soundexMapping, 0, mapping.Length);
        }

        /// <summary>
        /// Creates a refined Soundex instance using a custom mapping. This constructor can be used to customize the mapping,
        /// and/or possibly provide an internationalized mapping for a non-Western character set.
        /// </summary>
        /// <param name="mapping">Mapping string to use when finding the corresponding code for a given character.</param>
        public RefinedSoundex(string mapping)
        {
            this.soundexMapping = mapping.ToCharArray();
        }

        /// <summary>
        /// Returns the number of characters in the two encoded strings that are the
        /// same. This return value ranges from 0 to the length of the shortest
        /// encoded string: 0 indicates little or no similarity, and 4 out of 4 (for
        /// example) indicates strong similarity or identical values. For refined
        /// Soundex, the return value can be greater than 4.
        /// <para/>
        /// See: <a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_de-dz_8co5.asp">
        ///     MS T-SQL DIFFERENCE</a>
        /// <para/>
        /// since 1.3
        /// </summary>
        /// <param name="s1">A string that will be encoded and compared.</param>
        /// <param name="s2">A string that will be encoded and compared.</param>
        /// <returns>The number of characters in the two encoded strings that are the same from 0 to to the length of the shortest encoded string.</returns>
        /// <seealso cref="SoundexUtils.Difference(IStringEncoder, string, string)"/>
        public virtual int Difference(string s1, string s2)
        {
            return SoundexUtils.Difference(this, s1, s2);
        }

        // LUCENENET specific - in .NET we don't need an object overload of Encode(), since strings are sealed anyway.

        /// <summary>
        /// Encodes a string using the refined soundex algorithm.
        /// </summary>
        /// <param name="str">A string object to encode.</param>
        /// <returns>A Soundex code corresponding to the string supplied.</returns>
        public virtual string Encode(string str)
        {
            return GetSoundex(str);
        }

        /// <summary>
        /// Returns the mapping code for a given character. The mapping codes are
        /// maintained in an internal char array named soundexMapping, and the
        /// default values of these mappings are US English.
        /// </summary>
        /// <param name="c"><see cref="char"/> to get mapping for.</param>
        /// <returns>A character (really a numeral) to return for the given <see cref="char"/>.</returns>
        internal char GetMappingCode(char c)
        {
            if (!char.IsLetter(c))
            {
                return (char)0;
            }
            return this.soundexMapping[char.ToUpperInvariant(c) - 'A'];
        }

        /// <summary>
        /// Retrieves the Refined Soundex code for a given string.
        /// </summary>
        /// <param name="str">String to encode using the Refined Soundex algorithm.</param>
        /// <returns>A soundex code for the string supplied.</returns>
        public virtual string GetSoundex(string str)
        {
            if (str is null)
            {
                return null;
            }
            str = SoundexUtils.Clean(str);
            if (str.Length == 0)
            {
                return str;
            }

            StringBuilder sBuf = new StringBuilder();
            sBuf.Append(str[0]);

            char last, current;
            last = '*';

            for (int i = 0; i < str.Length; i++)
            {

                current = GetMappingCode(str[i]);
                if (current == last)
                {
                    continue;
                }
                else if (current != 0)
                {
                    sBuf.Append(current);
                }

                last = current;

            }

            return sBuf.ToString();
        }
    }
}
