// commons-codec version compatibility level: 1.9
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Phonetic.Language.Bm
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
    /// Converts words into potential phonetic representations.
    /// </summary>
    /// <remarks>
    /// This is a two-stage process. Firstly, the word is converted into a phonetic representation that takes
    /// into account the likely source language. Next, this phonetic representation is converted into a
    /// pan-European 'average' representation, allowing comparison between different versions of essentially
    /// the same word from different languages.
    /// <para/>
    /// This class is intentionally immutable and thread-safe.
    /// If you wish to alter the settings for a PhoneticEngine, you
    /// must make a new one with the updated settings.
    /// <para/>
    /// Ported from phoneticengine.php
    /// <para/>
    /// since 1.6
    /// </remarks>
    public class PhoneticEngine
    {
        internal Regex WHITESPACE = new Regex("\\s+", RegexOptions.Compiled);

        /// <summary>
        /// Utility for manipulating a set of phonemes as they are being built up. Not intended for use outside
        /// this package, and probably not outside the <see cref="PhoneticEngine"/> class.
        /// <para/>
        /// since 1.6
        /// </summary>
        internal sealed class PhonemeBuilder
        {
            /// <summary>
            /// An empty builder where all phonemes must come from some set of languages. This will contain a single
            /// phoneme of zero characters. This can then be appended to. This should be the only way to create a new
            /// phoneme from scratch.
            /// </summary>
            /// <param name="languages">The set of languages.</param>
            /// <returns>A new, empty phoneme builder.</returns>
            public static PhonemeBuilder Empty(LanguageSet languages)
            {
                return new PhonemeBuilder(new Phoneme("", languages));
            }

            private readonly ISet<Phoneme> phonemes;

            private PhonemeBuilder(Phoneme phoneme)
            {
                this.phonemes = new JCG.LinkedHashSet<Phoneme>
                {
                    phoneme
                };
            }

            internal PhonemeBuilder(ISet<Phoneme> phonemes)
            {
                this.phonemes = phonemes;
            }

            /// <summary>
            /// Creates a new phoneme builder containing all phonemes in this one extended by <paramref name="str"/>.
            /// </summary>
            /// <param name="str">The characters to append to the phonemes.</param>
            public void Append(ICharSequence str)
            {
                foreach (Phoneme ph in this.phonemes)
                {
                    ph.Append(str.ToString());
                }
            }

            /// <summary>
            /// Creates a new phoneme builder containing all phonemes in this one extended by <paramref name="str"/>.
            /// </summary>
            /// <param name="str">The characters to append to the phonemes.</param>
            // LUCENENET specific
            public void Append(string str)
            {
                foreach (Phoneme ph in this.phonemes)
                {
                    ph.Append(str);
                }
            }

            /// <summary>
            /// Creates a new phoneme builder containing all phonemes in this one extended by <paramref name="str"/>.
            /// </summary>
            /// <param name="str">The characters to append to the phonemes.</param>
            // LUCENENET specific
            public void Append(StringBuilder str)
            {
                foreach (Phoneme ph in this.phonemes)
                {
                    ph.Append(str.ToString());
                }
            }

            /// <summary>
            /// Applies the given phoneme expression to all phonemes in this phoneme builder.
            /// <para/>
            /// This will lengthen phonemes that have compatible language sets to the expression, and drop those that are
            /// incompatible.
            /// </summary>
            /// <param name="phonemeExpr">The expression to apply.</param>
            /// <param name="maxPhonemes">The maximum number of phonemes to build up.</param>
            public void Apply(IPhonemeExpr phonemeExpr, int maxPhonemes)
            {
                ISet<Phoneme> newPhonemes = new JCG.LinkedHashSet<Phoneme>(maxPhonemes);

                //EXPR_continue:
                foreach (Phoneme left in this.phonemes)
                {
                    foreach (Phoneme right in phonemeExpr.Phonemes)
                    {
                        LanguageSet languages = left.Languages.RestrictTo(right.Languages);
                        if (!languages.IsEmpty)
                        {
                            Phoneme join = new Phoneme(left, right, languages);
                            if (newPhonemes.Count < maxPhonemes)
                            {
                                newPhonemes.Add(join);
                                if (newPhonemes.Count >= maxPhonemes)
                                {
                                    goto EXPR_break;
                                }
                            }
                        }
                    }
                }
                EXPR_break: { }

                this.phonemes.Clear();
                this.phonemes.UnionWith(newPhonemes);
            }

            /// <summary>
            /// Gets underlying phoneme set. Please don't mutate.
            /// </summary>
            public ISet<Phoneme> Phonemes => phonemes;

            /// <summary>
            /// Stringifies the phoneme set. This produces a single string of the strings of each phoneme,
            /// joined with a pipe. This is explicitly provided in place of <see cref="object.ToString()"/> as it is a potentially
            /// expensive operation, which should be avoided when debugging.
            /// </summary>
            /// <returns>The stringified phoneme set.</returns>
            public string MakeString()
            {
                StringBuilder sb = new StringBuilder();

                foreach (Phoneme ph in this.phonemes)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append('|');
                    }
                    sb.Append(ph.GetPhonemeText());
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// A function closure capturing the application of a list of rules to an input sequence at a particular offset.
        /// After invocation, the values <c>i</c> and <c>found</c> are updated. <c>i</c> points to the
        /// index of the next char in <c>input</c> that must be processed next (the input up to that index having been
        /// processed already), and <c>found</c> indicates if a matching rule was found or not. In the case where a
        /// matching rule was found, <c>phonemeBuilder</c> is replaced with a new builder containing the phonemes
        /// updated by the matching rule.
        /// <para/>
        /// Although this class is not thread-safe (it has mutable unprotected fields), it is not shared between threads
        /// as it is constructed as needed by the calling methods.
        /// <para/>
        /// since 1.6
        /// </summary>
        private sealed class RulesApplication
        {
            private readonly IDictionary<string, IList<Rule>> finalRules;
            private readonly string input;

            private readonly PhonemeBuilder phonemeBuilder;
            private int i;
            private readonly int maxPhonemes;
            private bool found;

            public RulesApplication(IDictionary<string, IList<Rule>> finalRules, string input,
                                    PhonemeBuilder phonemeBuilder, int i, int maxPhonemes)
            {
                this.finalRules = finalRules ?? throw new ArgumentNullException(nameof(finalRules), "The finalRules argument must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
                this.phonemeBuilder = phonemeBuilder;
                this.input = input;
                this.i = i;
                this.maxPhonemes = maxPhonemes;
            }

            public int I => i;

            public PhonemeBuilder PhonemeBuilder => phonemeBuilder;

            /// <summary>
            /// Invokes the rules. Loops over the rules list, stopping at the first one that has a matching context
            /// and pattern. Then applies this rule to the phoneme builder to produce updated phonemes. If there was no
            /// match, <c>i</c> is advanced one and the character is silently dropped from the phonetic spelling.
            /// </summary>
            /// <returns><c>this</c></returns>
            public RulesApplication Invoke()
            {
                this.found = false;
                int patternLength = 1;
                if (this.finalRules.TryGetValue(input.Substring(i, patternLength), out IList<Rule> rules) && rules != null)
                {
                    foreach (Rule rule in rules)
                    {
                        string pattern = rule.Pattern;
                        patternLength = pattern.Length;
                        if (rule.PatternAndContextMatches(this.input, this.i))
                        {
                            this.phonemeBuilder.Apply(rule.Phoneme, maxPhonemes);
                            this.found = true;
                            break;
                        }
                    }
                }

                if (!this.found)
                {
                    patternLength = 1;
                }

                this.i += patternLength;
                return this;
            }

            public bool IsFound => found;
        }

        private static readonly IDictionary<NameType, ISet<string>> NAME_PREFIXES = LoadNamePrefixes();

        private static IDictionary<NameType, ISet<string>> LoadNamePrefixes() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            return new Dictionary<NameType, ISet<string>>
            {
                [NameType.ASHKENAZI] = new JCG.HashSet<string>() { "bar", "ben", "da", "de", "van", "von" }.AsReadOnly(),
                [NameType.SEPHARDIC] = new JCG.HashSet<string>() { "al", "el", "da", "dal", "de", "del", "dela", "de la",
                                                              "della", "des", "di", "do", "dos", "du", "van", "von" }.AsReadOnly(),
                [NameType.GENERIC] = new JCG.HashSet<string>() { "da", "dal", "de", "del", "dela", "de la", "della",
                                                          "des", "di", "do", "dos", "du", "van", "von" }.AsReadOnly()
            };
        }

        /// <summary>
        /// Joins some strings with an internal separator.
        /// </summary>
        /// <param name="strings">Strings to join.</param>
        /// <param name="sep">String to separate them with.</param>
        /// <returns>A single string consisting of each element of <paramref name="strings"/> interleaved by <paramref name="sep"/>.</returns>
        private static string Join(IEnumerable<string> strings, string sep)
        {
            StringBuilder sb = new StringBuilder();
            using (IEnumerator<string> si = strings.GetEnumerator())
            {
                if (si.MoveNext())
                {
                    sb.Append(si.Current);
                }
                while (si.MoveNext())
                {
                    sb.Append(sep).Append(si.Current);
                }
            }

            return sb.ToString();
        }

        private const int DEFAULT_MAX_PHONEMES = 20;

        private readonly Lang lang;

        private readonly NameType nameType;

        private readonly RuleType ruleType;

        private readonly bool concat;

        private readonly int maxPhonemes;

        /// <summary>
        /// Generates a new, fully-configured phonetic engine.
        /// </summary>
        /// <param name="nameType">The type of names it will use.</param>
        /// <param name="ruleType">The type of rules it will apply.</param>
        /// <param name="concat">If it will concatenate multiple encodings.</param>
        public PhoneticEngine(NameType nameType, RuleType ruleType, bool concat)
            : this(nameType, ruleType, concat, DEFAULT_MAX_PHONEMES)
        {
        }

        /// <summary>
        /// Generates a new, fully-configured phonetic engine.
        /// <para/>
        /// since 1.7
        /// </summary>
        /// <param name="nameType">The type of names it will use.</param>
        /// <param name="ruleType">The type of rules it will apply.</param>
        /// <param name="concat">If it will concatenate multiple encodings.</param>
        /// <param name="maxPhonemes">The maximum number of phonemes that will be handled.</param>
        public PhoneticEngine(NameType nameType, RuleType ruleType, bool concat,
                              int maxPhonemes)
        {
            if (ruleType == RuleType.RULES)
            {
                throw new ArgumentException("ruleType must not be " + RuleType.RULES);
            }
            this.nameType = nameType;
            this.ruleType = ruleType;
            this.concat = concat;
            this.lang = Lang.GetInstance(nameType);
            this.maxPhonemes = maxPhonemes;
        }

        /// <summary>
        /// Applies the final rules to convert from a language-specific phonetic representation to a
        /// language-independent representation.
        /// </summary>
        /// <param name="phonemeBuilder">The current phonemes.</param>
        /// <param name="finalRules">The final rules to apply.</param>
        /// <returns>The resulting phonemes.</returns>
        private PhonemeBuilder ApplyFinalRules(PhonemeBuilder phonemeBuilder,
                                               IDictionary<string, IList<Rule>> finalRules)
        {
            if (finalRules is null)
            {
                throw new ArgumentNullException("finalRules can not be null");// LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (finalRules.Count == 0)
            {
                return phonemeBuilder;
            }

            ISet<Phoneme> phonemes = new JCG.SortedSet<Phoneme>(Phoneme.COMPARER);

            foreach (Phoneme phoneme in phonemeBuilder.Phonemes)
            {
                PhonemeBuilder subBuilder = PhonemeBuilder.Empty(phoneme.Languages);
                string phonemeText = phoneme.GetPhonemeText();

                for (int i = 0; i < phonemeText.Length;)
                {
                    RulesApplication rulesApplication =
                            new RulesApplication(finalRules, phonemeText, subBuilder, i, maxPhonemes).Invoke();
                    bool found = rulesApplication.IsFound;
                    subBuilder = rulesApplication.PhonemeBuilder;

                    if (!found)
                    {
                        // not found, appending as-is
                        subBuilder.Append(phonemeText.Substring(i, 1));
                    }

                    i = rulesApplication.I;
                }

                phonemes.UnionWith(subBuilder.Phonemes);
            }

            return new PhonemeBuilder(phonemes);
        }

        /// <summary>
        /// Encodes a string to its phonetic representation.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <returns>The encoding of the input.</returns>
        public virtual string Encode(string input)
        {
            LanguageSet languageSet = this.lang.GuessLanguages(input);
            return Encode(input, languageSet);
        }

        /// <summary>
        /// Encodes an input string into an output phonetic representation, given a set of possible origin languages.
        /// </summary>
        /// <param name="input">String to phoneticise; a string with dashes or spaces separating each word.</param>
        /// <param name="languageSet"></param>
        /// <returns>A phonetic representation of the input; a string containing '-'-separated phonetic representations of the input.</returns>
        public virtual string Encode(string input, LanguageSet languageSet)
        {
            IDictionary<string, IList<Rule>> rules = Rule.GetInstanceMap(this.nameType, RuleType.RULES, languageSet);
            // rules common across many (all) languages
            IDictionary<string, IList<Rule>> finalRules1 = Rule.GetInstanceMap(this.nameType, this.ruleType, "common");
            // rules that apply to a specific language that may be ambiguous or wrong if applied to other languages
            IDictionary<string, IList<Rule>> finalRules2 = Rule.GetInstanceMap(this.nameType, this.ruleType, languageSet);

            // tidy the input
            // lower case is a locale-dependent operation
            input = input.ToLowerInvariant().Replace('-', ' ').Trim();

            if (this.nameType == NameType.GENERIC)
            {
                if (input.Length >= 2 && input.Substring(0, 2 - 0).Equals("d'", StringComparison.Ordinal))
                { // check for d'
                    string remainder = input.Substring(2);
                    string combined = "d" + remainder;
                    return "(" + Encode(remainder) + ")-(" + Encode(combined) + ")";
                }
                foreach (string l in NAME_PREFIXES[this.nameType])
                {
                    // handle generic prefixes
                    if (input.StartsWith(l + " ", StringComparison.Ordinal))
                    {
                        // check for any prefix in the words list
                        string remainder = input.Substring(l.Length + 1); // input without the prefix
                        string combined = l + remainder; // input with prefix without space
                        return "(" + Encode(remainder) + ")-(" + Encode(combined) + ")";
                    }
                }
            }

            IList<string> words = WHITESPACE.Split(input).TrimEnd();
            ISet<string> words2 = new JCG.HashSet<string>();

            // special-case handling of word prefixes based upon the name type
            switch (this.nameType)
            {
                case NameType.SEPHARDIC:
                    foreach (string aWord in words)
                    {
                        string[] parts = aWord.Split('\'').TrimEnd();
                        string lastPart = parts[parts.Length - 1];
                        words2.Add(lastPart);
                    }
                    words2.ExceptWith(NAME_PREFIXES[this.nameType]);
                    break;
                case NameType.ASHKENAZI:
                    words2.UnionWith(words);
                    words2.ExceptWith(NAME_PREFIXES[this.nameType]);
                    break;
                case NameType.GENERIC:
                    words2.UnionWith(words);
                    break;
                default:
                    throw new InvalidOperationException("Unreachable case: " + this.nameType);
            }

            if (this.concat)
            {
                // concat mode enabled
                input = Join(words2, " ");
            }
            else if (words2.Count == 1)
            {
                // not a multi-word name
                //input = words.iterator().next();
                input = words[0];
            }
            else
            {
                // encode each word in a multi-word name separately (normally used for approx matches)
                StringBuilder result = new StringBuilder();
                foreach (string word in words2)
                {
                    result.Append('-').Append(Encode(word));
                }
                // return the result without the leading "-"
                return result.ToString(1, result.Length - 1);
            }

            PhonemeBuilder phonemeBuilder = PhonemeBuilder.Empty(languageSet);

            // loop over each char in the input - we will handle the increment manually
            for (int i = 0; i < input.Length;)
            {
                RulesApplication rulesApplication =
                        new RulesApplication(rules, input, phonemeBuilder, i, maxPhonemes).Invoke();
                i = rulesApplication.I;
                phonemeBuilder = rulesApplication.PhonemeBuilder;
            }

            // Apply the general rules
            phonemeBuilder = ApplyFinalRules(phonemeBuilder, finalRules1);
            // Apply the language-specific rules
            phonemeBuilder = ApplyFinalRules(phonemeBuilder, finalRules2);

            return phonemeBuilder.MakeString();
        }

        /// <summary>
        /// Gets the Lang language guessing rules being used.
        /// </summary>
        public virtual Lang Lang => lang;

        /// <summary>
        /// Gets the <see cref="Bm.NameType"/> being used.
        /// </summary>
        public virtual NameType NameType => nameType;

        /// <summary>
        /// Gets the <see cref="Bm.RuleType"/> being used.
        /// </summary>
        public virtual RuleType RuleType => ruleType;

        /// <summary>
        /// Gets if multiple phonetic encodings are concatenated or if just the first one is kept.
        /// Returns <c>true</c> if multiple phonetic encodings are returned, <c>false</c> if just the first is.
        /// </summary>
        public virtual bool IsConcat => concat;

        /// <summary>
        /// Gets the maximum number of phonemes the engine will calculate for a given input.
        /// <para/>
        /// since 1.7
        /// </summary>
        public virtual int MaxPhonemes => maxPhonemes;
    }
}
