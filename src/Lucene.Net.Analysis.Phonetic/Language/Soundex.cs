// commons-codec version compatibility level: 1.10
using Lucene.Net.Support;
using System;

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
    /// Encodes a string into a Soundex value. Soundex is an encoding used to relate similar names, but can also be used as a
    /// general purpose scheme to find word with similar phonemes.
    /// <para/>
    /// This class is thread-safe.
    /// Although not strictly immutable, the <see cref="maxLength"/> field is not actually used.
    /// </summary>
    public class Soundex : IStringEncoder
    {
        /// <summary>
        /// The marker character used to indicate a silent (ignored) character.
        /// These are ignored except when they appear as the first character.
        /// <para/>
        /// Note: the <see cref="US_ENGLISH_MAPPING_STRING"/> does not use this mechanism
        /// because changing it might break existing code. Mappings that don't contain
        /// a silent marker code are treated as though H and W are silent.
        /// <para/>
        /// To override this, use the <see cref="Soundex(string, bool)"/> constructor.
        /// <para/>
        /// since 1.11
        /// </summary>
        public static readonly char SILENT_MARKER = '-';

        /// <summary>
        /// This is a default mapping of the 26 letters used in US English. A value of <c>0</c> for a letter position
        /// means do not encode, but treat as a separator when it occurs between consonants with the same code.
        /// <para/>
        /// (This constant is provided as both an implementation convenience and to allow documentation to pick
        /// up the value for the constant values page.)
        /// <para/>
        /// <b>Note that letters H and W are treated specially.</b>
        /// They are ignored (after the first letter) and don't act as separators
        /// between consonants with the same code.
        /// </summary>
        /// <seealso cref="US_ENGLISH_MAPPING"/>
        //                                                      ABCDEFGHIJKLMNOPQRSTUVWXYZ
        public static readonly string US_ENGLISH_MAPPING_STRING = "01230120022455012623010202";

        /// <summary>
        /// This is a default mapping of the 26 letters used in US English. A value of <c>0</c> for a letter position
        /// means do not encode.
        /// </summary>
        /// <seealso cref="Soundex.Soundex(char[])"/>
        private static readonly char[] US_ENGLISH_MAPPING = US_ENGLISH_MAPPING_STRING.ToCharArray();

        /// <summary>
        /// An instance of Soundex using the US_ENGLISH_MAPPING mapping.
        /// This treats H and W as silent letters.
        /// Apart from when they appear as the first letter, they are ignored.
        /// They don't act as separators between duplicate codes.
        /// </summary>
        /// <seealso cref="US_ENGLISH_MAPPING"/>
        /// <seealso cref="US_ENGLISH_MAPPING_STRING"/>
        public static readonly Soundex US_ENGLISH = new Soundex();

        /// <summary>
        /// An instance of Soundex using the Simplified Soundex mapping, as described here:
        /// http://west-penwith.org.uk/misc/soundex.htm
        /// <para/>
        /// This treats H and W the same as vowels (AEIOUY).
        /// Such letters aren't encoded (after the first), but they do
        /// act as separators when dropping duplicate codes.
        /// The mapping is otherwise the same as for <see cref="US_ENGLISH"/>.
        /// <para/>
        /// since 1.11
        /// </summary>
        public static readonly Soundex US_ENGLISH_SIMPLIFIED = new Soundex(US_ENGLISH_MAPPING_STRING, false);

        /// <summary>
        /// An instance of Soundex using the mapping as per the Genealogy site:
        /// http://www.genealogy.com/articles/research/00000060.html
        /// <para/>
        /// This treats vowels (AEIOUY), H and W as silent letters.
        /// Such letters are ignored (after the first) and do not
        /// act as separators when dropping duplicate codes.
        /// <para/>
        /// The codes for consonants are otherwise the same as for 
        /// <see cref="US_ENGLISH_MAPPING_STRING"/> and <see cref="US_ENGLISH_SIMPLIFIED"/>.
        /// <para/>
        /// since 1.11
        /// </summary>
        public static readonly Soundex US_ENGLISH_GENEALOGY = new Soundex("-123-12--22455-12623-1-2-2");
        //                                                              ABCDEFGHIJKLMNOPQRSTUVWXYZ

        /// <summary>
        /// The maximum length of a Soundex code - Soundex codes are only four characters by definition.
        /// </summary>
        [Obsolete("This feature is not needed since the encoding size must be constant. Will be removed in 2.0.")]
        private int maxLength = 4;

        /// <summary>
        /// Every letter of the alphabet is "mapped" to a numerical value. This char array holds the values to which each
        /// letter is mapped. This implementation contains a default map for US_ENGLISH
        /// </summary>
        private readonly char[] soundexMapping;

        /// <summary>
        /// Should H and W be treated specially?
        /// <para/>
        /// In versions of the code prior to 1.11,
        /// the code always treated H and W as silent (ignored) letters.
        /// If this field is false, H and W are no longer special-cased.
        /// </summary>
        private readonly bool specialCaseHW;

        /// <summary>
        /// Creates an instance using <see cref="US_ENGLISH_MAPPING"/>.
        /// </summary>
        /// <seealso cref="Soundex.Soundex(char[])"/>
        /// <seealso cref="US_ENGLISH_MAPPING"/>
        public Soundex()
        {
            this.soundexMapping = US_ENGLISH_MAPPING;
            this.specialCaseHW = true;
        }

        /// <summary>
        /// Creates a soundex instance using the given mapping. This constructor can be used to provide an internationalized
        /// mapping for a non-Western character set.
        /// <para/>
        /// Every letter of the alphabet is "mapped" to a numerical value. This char array holds the values to which each
        /// letter is mapped. This implementation contains a default map for <see cref="US_ENGLISH"/>.
        /// <para/>
        /// If the mapping contains an instance of <see cref="SILENT_MARKER"/> then H and W are not given special treatment.
        /// </summary>
        /// <param name="mapping"> Mapping array to use when finding the corresponding code for a given character.</param>
        public Soundex(char[] mapping)
        {
            this.soundexMapping = new char[mapping.Length];
            Arrays.Copy(mapping, 0, this.soundexMapping, 0, mapping.Length);
            this.specialCaseHW = !HasMarker(this.soundexMapping);
        }

        private static bool HasMarker(char[] mapping) // LUCENENET: CA1822: Mark members as static
        {
            foreach (char ch in mapping)
            {
                if (ch == SILENT_MARKER)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a refined soundex instance using a custom mapping. This constructor can be used to customize the mapping,
        /// and/or possibly provide an internationalized mapping for a non-Western character set.
        /// <para/>
        /// If the mapping contains an instance of <see cref="SILENT_MARKER"/> then H and W are not given special treatment.
        /// <para/>
        /// since 1.4
        /// </summary>
        /// <param name="mapping">Mapping string to use when finding the corresponding code for a given character.</param>
        public Soundex(string mapping)
        {
            this.soundexMapping = mapping.ToCharArray();
            this.specialCaseHW = !HasMarker(this.soundexMapping);
        }

        /// <summary>
        /// Creates a refined soundex instance using a custom mapping. This constructor can be used to customize the mapping,
        /// and/or possibly provide an internationalized mapping for a non-Western character set.
        /// <para/>
        /// since 1.11
        /// </summary>
        /// <param name="mapping">Mapping string to use when finding the corresponding code for a given character.</param>
        /// <param name="specialCaseHW">if true, then </param>
        public Soundex(string mapping, bool specialCaseHW)
        {
            this.soundexMapping = mapping.ToCharArray();
            this.specialCaseHW = specialCaseHW;
        }

        /// <summary>
        /// Encodes the strings and returns the number of characters in the two encoded strings that are the same. This
        /// return value ranges from 0 through 4: 0 indicates little or no similarity, and 4 indicates strong similarity or
        /// identical values.
        /// <para/>
        /// See: <a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_de-dz_8co5.asp"> MS
        /// T-SQL DIFFERENCE </a>
        /// <para/>
        /// since 1.3
        /// </summary>
        /// <param name="s1">A string that will be encoded and compared.</param>
        /// <param name="s2">A string that will be encoded and compared.</param>
        /// <returns>The number of characters in the two encoded strings that are the same from 0 to 4.</returns>
        /// <seealso cref="SoundexUtils.Difference(IStringEncoder, string, string)"/>
        public virtual int Difference(string s1, string s2)
        {
            return SoundexUtils.Difference(this, s1, s2);
        }

        // LUCENENET specific - in .NET we don't need an object overload of Encode(), since strings are sealed anyway.

        /// <summary>
        /// Encodes a string using the soundex algorithm.
        /// </summary>
        /// <param name="str">A string to encode.</param>
        /// <returns>A Soundex code corresponding to the string supplied.</returns>
        /// <exception cref="ArgumentException">If a character is not mapped.</exception>
        public virtual string Encode(string str)
        {
            return GetSoundex(str);
        }

        /// <summary>
        /// Gets or Sets the maxLength. Standard Soundex
        /// </summary>
        [Obsolete("This feature is not needed since the encoding size must be constant. Will be removed in 2.0.")]
        public virtual int MaxLength
        {
            get => this.maxLength;
            set => this.maxLength = value;
        }

        /// <summary>
        ///  Maps the given upper-case character to its Soundex code.
        /// </summary>
        /// <param name="ch">An upper-case character.</param>
        /// <returns>A Soundex code.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="ch"/> is not mapped.</exception>
        private char Map(char ch)
        {
            int index = ch - 'A';
            if (index < 0 || index >= this.soundexMapping.Length)
            {
                throw new ArgumentException("The character is not mapped: " + ch + " (index=" + index + ")");
            }
            return this.soundexMapping[index];
        }

        /// <summary>
        /// Retrieves the Soundex code for a given string.
        /// </summary>
        /// <param name="str">String to encode using the Soundex algorithm.</param>
        /// <returns>A soundex code for the string supplied.</returns>
        /// <exception cref="ArgumentException">If a character is not mapped.</exception>
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
            char[] output = { '0', '0', '0', '0' };
            int count = 0;
            char first = str[0];
            output[count++] = first;
            char lastDigit = Map(first); // previous digit
            for (int i = 1; i < str.Length && count < output.Length; i++)
            {
                char ch = str[i];
                if ((this.specialCaseHW) && (ch == 'H' || ch == 'W'))
                { // these are ignored completely
                    continue;
                }
                char digit = Map(ch);
                if (digit == SILENT_MARKER)
                {
                    continue;
                }
                if (digit != '0' && digit != lastDigit)
                { // don't store vowels or repeats
                    output[count++] = digit;
                }
                lastDigit = digit;
            }
            return new string(output);
        }
    }
}
