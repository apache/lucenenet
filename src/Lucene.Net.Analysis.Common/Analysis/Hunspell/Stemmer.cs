using Lucene.Net.Analysis.Util;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Hunspell
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
    /// Stemmer uses the affix rules declared in the <see cref="Dictionary"/> to generate one or more stems for a word.  It
    /// conforms to the algorithm in the original hunspell algorithm, including recursive suffix stripping.
    /// </summary>
    internal sealed class Stemmer
    {
        private readonly Dictionary dictionary;
        private readonly BytesRef scratch = new BytesRef();
        private readonly StringBuilder segment = new StringBuilder();
        private readonly ByteArrayDataInput affixReader;

        // used for normalization
        private readonly StringBuilder scratchSegment = new StringBuilder();
        private char[] scratchBuffer = new char[32];

        /// <summary>
        /// Constructs a new Stemmer which will use the provided <see cref="Dictionary"/> to create its stems.
        /// </summary>
        /// <param name="dictionary"> <see cref="Dictionary"/> that will be used to create the stems </param>
        public Stemmer(Dictionary dictionary)
        {
            this.dictionary = dictionary;
            this.affixReader = new ByteArrayDataInput(dictionary.affixData);
        }

        /// <summary>
        /// Find the stem(s) of the provided word.
        /// </summary>
        /// <param name="word"> Word to find the stems for </param>
        /// <returns> <see cref="IList{CharsRef}"/> of stems for the word </returns>
        public IList<CharsRef> Stem(string word)
        {
            return Stem(word.ToCharArray(), word.Length);
        }

        /// <summary>
        /// Find the stem(s) of the provided word
        /// </summary>
        /// <param name="word"> Word to find the stems for </param>
        /// <param name="length"> length </param>
        /// <returns> <see cref="IList{CharsRef}"/> of stems for the word </returns>
        public IList<CharsRef> Stem(char[] word, int length)
        {

            if (dictionary.needsInputCleaning)
            {
                scratchSegment.Length = 0;
                scratchSegment.Append(word, 0, length);
                string cleaned = dictionary.CleanInput(scratchSegment.ToString(), segment);
                scratchBuffer = ArrayUtil.Grow(scratchBuffer, cleaned.Length);
                length = segment.Length;
                segment.CopyTo(0, scratchBuffer, 0, length);
                word = scratchBuffer;
            }

            List<CharsRef> stems = new List<CharsRef>();
            Int32sRef forms = dictionary.LookupWord(word, 0, length);
            if (forms != null)
            {
                // TODO: some forms should not be added, e.g. ONLYINCOMPOUND
                // just because it exists, does not make it valid...
                for (int i = 0; i < forms.Length; i++)
                {
                    stems.Add(NewStem(word, length));
                }
            }
            stems.AddRange(Stem(word, length, -1, -1, -1, 0, true, true, false, false));
            return stems;
        }

        /// <summary>
        /// Find the unique stem(s) of the provided word
        /// </summary>
        /// <param name="word"> Word to find the stems for </param>
        /// <param name="length"> length </param>
        /// <returns> <see cref="IList{CharsRef}"/> of stems for the word </returns>
        public IList<CharsRef> UniqueStems(char[] word, int length)
        {
            IList<CharsRef> stems = Stem(word, length);
            if (stems.Count < 2)
            {
                return stems;
            }
            CharArraySet terms = new CharArraySet(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT, 8, dictionary.ignoreCase);
#pragma warning restore 612, 618
            IList<CharsRef> deduped = new List<CharsRef>();
            foreach (CharsRef s in stems)
            {
                if (!terms.Contains(s))
                {
                    deduped.Add(s);
                    terms.Add(s);
                }
            }
            return deduped;
        }

        private CharsRef NewStem(char[] buffer, int length)
        {
            if (dictionary.needsOutputCleaning)
            {
                scratchSegment.Length = 0;
                scratchSegment.Append(buffer, 0, length);
                try
                {
                    Dictionary.ApplyMappings(dictionary.oconv, scratchSegment);
                }
                catch (IOException bogus)
                {
                    throw new Exception(bogus.Message, bogus);
                }
                char[] cleaned = new char[scratchSegment.Length];
                scratchSegment.CopyTo(0, cleaned, 0, cleaned.Length);
                return new CharsRef(cleaned, 0, cleaned.Length);
            }
            else
            {
                return new CharsRef(buffer, 0, length);
            }
        }

        // ================================================= Helper Methods ================================================

        /// <summary>
        /// Generates a list of stems for the provided word
        /// </summary>
        /// <param name="word"> Word to generate the stems for </param>
        /// <param name="length"> length </param>
        /// <param name="previous"> previous affix that was removed (so we dont remove same one twice) </param>
        /// <param name="prevFlag"> Flag from a previous stemming step that need to be cross-checked with any affixes in this recursive step </param>
        /// <param name="prefixFlag"> flag of the most inner removed prefix, so that when removing a suffix, its also checked against the word </param>
        /// <param name="recursionDepth"> current recursiondepth </param>
        /// <param name="doPrefix"> true if we should remove prefixes </param>
        /// <param name="doSuffix"> true if we should remove suffixes </param>
        /// <param name="previousWasPrefix"> true if the previous removal was a prefix:
        ///        if we are removing a suffix, and it has no continuation requirements, its ok.
        ///        but two prefixes (COMPLEXPREFIXES) or two suffixes must have continuation requirements to recurse. </param>
        /// <param name="circumfix"> true if the previous prefix removal was signed as a circumfix
        ///        this means inner most suffix must also contain circumfix flag. </param>
        /// <returns> <see cref="IList{CharsRef}"/> of stems, or empty list if no stems are found </returns>
        private IList<CharsRef> Stem(char[] word, int length, int previous, int prevFlag, int prefixFlag, int recursionDepth, bool doPrefix, bool doSuffix, bool previousWasPrefix, bool circumfix)
        {

            // TODO: allow this stuff to be reused by tokenfilter
            List<CharsRef> stems = new List<CharsRef>();

            if (doPrefix && dictionary.prefixes != null)
            {
                for (int i = length - 1; i >= 0; i--)
                {
                    Int32sRef prefixes = dictionary.LookupPrefix(word, 0, i);
                    if (prefixes == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < prefixes.Length; j++)
                    {
                        int prefix = prefixes.Int32s[prefixes.Offset + j];
                        if (prefix == previous)
                        {
                            continue;
                        }
                        affixReader.Position = 8 * prefix;
                        char flag = (char)(affixReader.ReadInt16() & 0xffff);
                        char stripOrd = (char)(affixReader.ReadInt16() & 0xffff);
                        int condition = (char)(affixReader.ReadInt16() & 0xffff);
                        bool crossProduct = (condition & 1) == 1;
                        condition = (int)((uint)condition >> 1);
                        char append = (char)(affixReader.ReadInt16() & 0xffff);

                        bool compatible;
                        if (recursionDepth == 0)
                        {
                            compatible = true;
                        }
                        else if (crossProduct)
                        {
                            // cross check incoming continuation class (flag of previous affix) against list.
                            dictionary.flagLookup.Get(append, scratch);
                            char[] appendFlags = Dictionary.DecodeFlags(scratch);
                            Debug.Assert(prevFlag >= 0);
                            compatible = HasCrossCheckedFlag((char)prevFlag, appendFlags, false);
                        }
                        else
                        {
                            compatible = false;
                        }

                        if (compatible)
                        {
                            int deAffixedStart = i;
                            int deAffixedLength = length - deAffixedStart;

                            int stripStart = dictionary.stripOffsets[stripOrd];
                            int stripEnd = dictionary.stripOffsets[stripOrd + 1];
                            int stripLength = stripEnd - stripStart;

                            if (!CheckCondition(condition, dictionary.stripData, stripStart, stripLength, word, deAffixedStart, deAffixedLength))
                            {
                                continue;
                            }

                            char[] strippedWord = new char[stripLength + deAffixedLength];
                            Array.Copy(dictionary.stripData, stripStart, strippedWord, 0, stripLength);
                            Array.Copy(word, deAffixedStart, strippedWord, stripLength, deAffixedLength);

                            IList<CharsRef> stemList = ApplyAffix(strippedWord, strippedWord.Length, prefix, -1, recursionDepth, true, circumfix);

                            stems.AddRange(stemList);
                        }
                    }
                }
            }

            if (doSuffix && dictionary.suffixes != null)
            {
                for (int i = 0; i < length; i++)
                {
                    Int32sRef suffixes = dictionary.LookupSuffix(word, i, length - i);
                    if (suffixes == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < suffixes.Length; j++)
                    {
                        int suffix = suffixes.Int32s[suffixes.Offset + j];
                        if (suffix == previous)
                        {
                            continue;
                        }
                        affixReader.Position = 8 * suffix;
                        char flag = (char)(affixReader.ReadInt16() & 0xffff);
                        char stripOrd = (char)(affixReader.ReadInt16() & 0xffff);
                        int condition = (char)(affixReader.ReadInt16() & 0xffff);
                        bool crossProduct = (condition & 1) == 1;
                        condition = (int)((uint)condition >> 1);
                        char append = (char)(affixReader.ReadInt16() & 0xffff);

                        bool compatible;
                        if (recursionDepth == 0)
                        {
                            compatible = true;
                        }
                        else if (crossProduct)
                        {
                            // cross check incoming continuation class (flag of previous affix) against list.
                            dictionary.flagLookup.Get(append, scratch);
                            char[] appendFlags = Dictionary.DecodeFlags(scratch);
                            Debug.Assert(prevFlag >= 0);
                            compatible = HasCrossCheckedFlag((char)prevFlag, appendFlags, previousWasPrefix);
                        }
                        else
                        {
                            compatible = false;
                        }

                        if (compatible)
                        {
                            int appendLength = length - i;
                            int deAffixedLength = length - appendLength;

                            int stripStart = dictionary.stripOffsets[stripOrd];
                            int stripEnd = dictionary.stripOffsets[stripOrd + 1];
                            int stripLength = stripEnd - stripStart;

                            if (!CheckCondition(condition, word, 0, deAffixedLength, dictionary.stripData, stripStart, stripLength))
                            {
                                continue;
                            }

                            char[] strippedWord = new char[stripLength + deAffixedLength];
                            Array.Copy(word, 0, strippedWord, 0, deAffixedLength);
                            Array.Copy(dictionary.stripData, stripStart, strippedWord, deAffixedLength, stripLength);

                            IList<CharsRef> stemList = ApplyAffix(strippedWord, strippedWord.Length, suffix, prefixFlag, recursionDepth, false, circumfix);

                            stems.AddRange(stemList);
                        }
                    }
                }
            }

            return stems;
        }

        /// <summary>
        /// checks condition of the concatenation of two strings </summary>
        // note: this is pretty stupid, we really should subtract strip from the condition up front and just check the stem
        // but this is a little bit more complicated.
        private bool CheckCondition(int condition, char[] c1, int c1off, int c1len, char[] c2, int c2off, int c2len)
        {
            if (condition != 0)
            {
                CharacterRunAutomaton pattern = dictionary.patterns[condition];
                int state = pattern.InitialState;
                for (int i = c1off; i < c1off + c1len; i++)
                {
                    state = pattern.Step(state, c1[i]);
                    if (state == -1)
                    {
                        return false;
                    }
                }
                for (int i = c2off; i < c2off + c2len; i++)
                {
                    state = pattern.Step(state, c2[i]);
                    if (state == -1)
                    {
                        return false;
                    }
                }
                return pattern.IsAccept(state);
            }
            return true;
        }

        /// <summary>
        /// Applies the affix rule to the given word, producing a list of stems if any are found
        /// </summary>
        /// <param name="strippedWord"> Word the affix has been removed and the strip added </param>
        /// <param name="length"> valid length of stripped word </param>
        /// <param name="affix"> HunspellAffix representing the affix rule itself </param>
        /// <param name="prefixFlag"> when we already stripped a prefix, we cant simply recurse and check the suffix, unless both are compatible
        ///                   so we must check dictionary form against both to add it as a stem! </param>
        /// <param name="recursionDepth"> current recursion depth </param>
        /// <param name="prefix"> true if we are removing a prefix (false if its a suffix) </param>
        /// <param name="circumfix"> true if the previous prefix removal was signed as a circumfix
        ///        this means inner most suffix must also contain circumfix flag. </param>
        /// <returns> <see cref="IList{CharsRef}"/> of stems for the word, or an empty list if none are found </returns>
        internal IList<CharsRef> ApplyAffix(char[] strippedWord, int length, int affix, int prefixFlag, int recursionDepth, bool prefix, bool circumfix)
        {
            // TODO: just pass this in from before, no need to decode it twice
            affixReader.Position = 8 * affix;
            char flag = (char)(affixReader.ReadInt16() & 0xffff);
            affixReader.SkipBytes(2); // strip
            int condition = (char)(affixReader.ReadInt16() & 0xffff);
            bool crossProduct = (condition & 1) == 1;
            condition = (int)((uint)condition >> 1);
            char append = (char)(affixReader.ReadInt16() & 0xffff);

            List<CharsRef> stems = new List<CharsRef>();

            Int32sRef forms = dictionary.LookupWord(strippedWord, 0, length);
            if (forms != null)
            {
                for (int i = 0; i < forms.Length; i++)
                {
                    dictionary.flagLookup.Get(forms.Int32s[forms.Offset + i], scratch);
                    char[] wordFlags = Dictionary.DecodeFlags(scratch);
                    if (Dictionary.HasFlag(wordFlags, flag))
                    {
                        // confusing: in this one exception, we already chained the first prefix against the second,
                        // so it doesnt need to be checked against the word
                        bool chainedPrefix = dictionary.complexPrefixes && recursionDepth == 1 && prefix;
                        if (chainedPrefix == false && prefixFlag >= 0 && !Dictionary.HasFlag(wordFlags, (char)prefixFlag))
                        {
                            // see if we can chain prefix thru the suffix continuation class (only if it has any!)
                            dictionary.flagLookup.Get(append, scratch);
                            char[] appendFlags = Dictionary.DecodeFlags(scratch);
                            if (!HasCrossCheckedFlag((char)prefixFlag, appendFlags, false))
                            {
                                continue;
                            }
                        }

                        // if circumfix was previously set by a prefix, we must check this suffix,
                        // to ensure it has it, and vice versa
                        if (dictionary.circumfix != -1)
                        {
                            dictionary.flagLookup.Get(append, scratch);
                            char[] appendFlags = Dictionary.DecodeFlags(scratch);
                            bool suffixCircumfix = Dictionary.HasFlag(appendFlags, (char)dictionary.circumfix);
                            if (circumfix != suffixCircumfix)
                            {
                                continue;
                            }
                        }
                        stems.Add(NewStem(strippedWord, length));
                    }
                }
            }

            // if a circumfix flag is defined in the dictionary, and we are a prefix, we need to check if we have that flag
            if (dictionary.circumfix != -1 && !circumfix && prefix)
            {
                dictionary.flagLookup.Get(append, scratch);
                char[] appendFlags = Dictionary.DecodeFlags(scratch);
                circumfix = Dictionary.HasFlag(appendFlags, (char)dictionary.circumfix);
            }

            if (crossProduct)
            {
                if (recursionDepth == 0)
                {
                    if (prefix)
                    {
                        // we took away the first prefix.
                        // COMPLEXPREFIXES = true:  combine with a second prefix and another suffix 
                        // COMPLEXPREFIXES = false: combine with a suffix
                        stems.AddRange(Stem(strippedWord, length, affix, flag, flag, ++recursionDepth, dictionary.complexPrefixes && dictionary.twoStageAffix, true, true, circumfix));
                    }
                    else if (dictionary.complexPrefixes == false && dictionary.twoStageAffix)
                    {
                        // we took away a suffix.
                        // COMPLEXPREFIXES = true: we don't recurse! only one suffix allowed
                        // COMPLEXPREFIXES = false: combine with another suffix
                        stems.AddRange(Stem(strippedWord, length, affix, flag, prefixFlag, ++recursionDepth, false, true, false, circumfix));
                    }
                }
                else if (recursionDepth == 1)
                {
                    if (prefix && dictionary.complexPrefixes)
                    {
                        // we took away the second prefix: go look for another suffix
                        stems.AddRange(Stem(strippedWord, length, affix, flag, flag, ++recursionDepth, false, true, true, circumfix));
                    }
                    else if (prefix == false && dictionary.complexPrefixes == false && dictionary.twoStageAffix)
                    {
                        // we took away a prefix, then a suffix: go look for another suffix
                        stems.AddRange(Stem(strippedWord, length, affix, flag, prefixFlag, ++recursionDepth, false, true, false, circumfix));
                    }
                }
            }

            return stems;
        }

        /// <summary>
        /// Checks if the given flag cross checks with the given array of flags
        /// </summary>
        /// <param name="flag"> Flag to cross check with the array of flags </param>
        /// <param name="flags"> Array of flags to cross check against.  Can be <c>null</c> </param>
        /// <param name="matchEmpty"> If true, will match a zero length flags array. </param>
        /// <returns> <c>true</c> if the flag is found in the array or the array is <c>null</c>, <c>false</c> otherwise </returns>
        private bool HasCrossCheckedFlag(char flag, char[] flags, bool matchEmpty)
        {
            return (flags.Length == 0 && matchEmpty) || Array.BinarySearch(flags, flag) >= 0;
        }
    }
}