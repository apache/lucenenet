// commons-codec version compatibility level: 1.9
using J2N;
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Language guessing utility.
    /// </summary>
    /// <remarks>
    /// This class encapsulates rules used to guess the possible languages that a word originates from. This is
    /// done by reference to a whole series of rules distributed in resource files.
    /// <para/>
    /// Instances of this class are typically managed through the static factory method <see cref="GetInstance(NameType)"/>.
    /// Unless you are developing your own language guessing rules, you will not need to interact with this class directly.
    /// <para/>
    /// This class is intended to be immutable and thread-safe.
    /// <para/>
    /// <b>Lang resources</b>
    /// <para/>
    /// Language guessing rules are typically loaded from resource files. These are UTF-8 encoded text files.
    /// They are systematically named following the pattern:
    /// <c>Lucene.Net.Analysis.Phonetic.Language.Bm.lang.txt</c>
    /// The format of these resources is the following:
    /// <list type="table">
    ///     <item>
    ///         <term>Rules:</term>
    ///         <description>
    ///             Whitespace separated strings.
    ///             There should be 3 columns to each row, and these will be interpreted as:
    ///             <list type="number">
    ///                 <item><term>pattern:</term><description>a regular expression.</description></item>
    ///                 <item><term>languages:</term><description>a '+'-separated list of languages.</description></item>
    ///                 <item><term>acceptOnMatch:</term><description>'true' or 'false' indicating if a match rules in or rules out the language.</description></item>
    ///             </list>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>End-of-line comments:</term>
    ///         <description>Any occurrence of '//' will cause all text following on that line to be discarded as a comment.</description>
    ///     </item>
    ///     <item>
    ///         <term>Multi-line comments:</term>
    ///         <description>Any line starting with '/*' will start multi-line commenting mode. This will skip all content until a line ending in '*' and '/' is found.</description>
    ///     </item>
    ///     <item>
    ///         <term>Blank lines:</term>
    ///         <description>All blank lines will be skipped.</description>
    ///     </item>
    /// </list>
    /// <para/>
    /// Port of lang.php
    /// <para/>
    /// since 1.6
    /// </remarks>
    public class Lang
    {
        // Implementation note: This class is divided into two sections. The first part is a static factory interface that
        // exposes the LANGUAGE_RULES_RN resource as a Lang instance. The second part is the Lang instance methods that
        // encapsulate a particular language-guessing rule table and the language guessing itself.
        //
        // It may make sense in the future to expose the private constructor to allow power users to build custom language-
        // guessing rules, perhaps by marking it protected and allowing sub-classing. However, the vast majority of users
        // should be strongly encouraged to use the static factory <code>instance</code> method to get their Lang instances.

        private static readonly Regex WHITESPACE = new Regex("\\s+", RegexOptions.Compiled);
        private static readonly Regex TOKEN = new Regex("\\+", RegexOptions.Compiled);

        private sealed class LangRule
        {
            internal readonly bool acceptOnMatch;
            internal readonly ISet<string> languages;
            private readonly Regex pattern;

            internal LangRule(Regex pattern, ISet<string> languages, bool acceptOnMatch)
            {
                this.pattern = pattern;
                this.languages = languages;
                this.acceptOnMatch = acceptOnMatch;
            }

            public bool Matches(string txt)
            {
                Match matcher = this.pattern.Match(txt);
                return matcher.Success;
            }
        }

        // LUCENENET specific - need to load this first for LoadLangs() to work
        private const string LANGUAGE_RULES_RN = "lang.txt";

        private static readonly IDictionary<NameType, Lang> langs = LoadLangs();

        private static IDictionary<NameType, Lang> LoadLangs() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            IDictionary<NameType, Lang> langs = new Dictionary<NameType, Lang>();
            foreach (NameType s in Enum.GetValues(typeof(NameType)))
            {
                langs[s] = LoadFromResource(LANGUAGE_RULES_RN, Languages.GetInstance(s));
            }
            return langs;
        }

        /// <summary>
        /// Gets a Lang instance for one of the supported <see cref="NameType"/>s.
        /// </summary>
        /// <param name="nameType">The <see cref="NameType"/> to look up.</param>
        /// <returns>A Lang encapsulating the language guessing rules for that name type.</returns>
        public static Lang GetInstance(NameType nameType)
        {
            langs.TryGetValue(nameType, out Lang result);
            return result;
        }

        /// <summary>
        /// Loads language rules from a resource.
        /// <para/>
        /// In normal use, you will obtain instances of Lang through the <see cref="GetInstance(NameType)"/> method.
        /// You will only need to call this yourself if you are developing custom language mapping rules.
        /// </summary>
        /// <param name="languageRulesResourceName">The fully-qualified or partially-qualified resource name to load.</param>
        /// <param name="languages">The languages that these rules will support.</param>
        /// <returns>A Lang encapsulating the loaded language-guessing rules.</returns>
        public static Lang LoadFromResource(string languageRulesResourceName, Languages languages)
        {
            IList<LangRule> rules = new JCG.List<LangRule>();
            Stream lRulesIS = typeof(Lang).FindAndGetManifestResourceStream(languageRulesResourceName);

            if (lRulesIS is null)
            {
                throw new InvalidOperationException("Unable to resolve required resource:" + LANGUAGE_RULES_RN);
            }

            using (TextReader reader = new StreamReader(lRulesIS, ResourceConstants.ENCODING))
            {
                bool inExtendedComment = false;
                string rawLine;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    string line = rawLine;
                    if (inExtendedComment)
                    {
                        // check for closing comment marker, otherwise discard doc comment line
                        if (line.EndsWith(ResourceConstants.EXT_CMT_END, StringComparison.Ordinal))
                        {
                            inExtendedComment = false;
                        }
                    }
                    else
                    {
                        if (line.StartsWith(ResourceConstants.EXT_CMT_START, StringComparison.Ordinal))
                        {
                            inExtendedComment = true;
                        }
                        else
                        {
                            // discard comments
                            int cmtI = line.IndexOf(ResourceConstants.CMT, StringComparison.Ordinal);
                            if (cmtI >= 0)
                            {
                                line = line.Substring(0, cmtI - 0);
                            }

                            // trim leading-trailing whitespace
                            line = line.Trim();

                            if (line.Length == 0)
                            {
                                continue; // empty lines can be safely skipped
                            }

                            // split it up
                            string[] parts = WHITESPACE.Split(line).TrimEnd();

                            if (parts.Length != 3)
                            {
                                throw new ArgumentException("Malformed line '" + rawLine +
                                        "' in language resource '" + languageRulesResourceName + "'");
                            }

                            Regex pattern = new Regex(parts[0], RegexOptions.Compiled);
                            string[] langs = TOKEN.Split(parts[1]).TrimEnd();
                            bool accept = parts[2].Equals("true", StringComparison.Ordinal);

                            rules.Add(new LangRule(pattern, new JCG.HashSet<string>(langs), accept));
                        }
                    }
                }
            }
            return new Lang(rules, languages);
        }

        private readonly Languages languages;
        private readonly IList<LangRule> rules;

        private Lang(IList<LangRule> rules, Languages languages)
        {
            this.rules = rules.AsReadOnly();
            this.languages = languages;
        }

        /// <summary>
        /// Guesses the language of a word.
        /// </summary>
        /// <param name="text">The word.</param>
        /// <returns>The language that the word originates from or <see cref="Languages.ANY"/> if there was no unique match.</returns>
        public virtual string GuessLanguage(string text)
        {
            LanguageSet ls = GuessLanguages(text);
            return ls.IsSingleton ? ls.GetAny() : Languages.ANY;
        }

        /// <summary>
        /// Guesses the languages of a word.
        /// </summary>
        /// <param name="input">The word.</param>
        /// <returns>A Set of Strings of language names that are potential matches for the input word.</returns>
        public virtual LanguageSet GuessLanguages(string input)
        {
            string text = input.ToLowerInvariant();

            ISet<string> langs = new JCG.HashSet<string>(this.languages.GetLanguages());
            foreach (LangRule rule in this.rules)
            {
                if (rule.Matches(text))
                {
                    if (rule.acceptOnMatch)
                    {
                        IList<string> toRemove = new JCG.List<string>();
                        foreach (var item in langs)
                        {
                            if (!rule.languages.Contains(item))
                            {
                                toRemove.Add(item);
                            }
                        }
                        foreach (var item in toRemove)
                        {
                            langs.Remove(item);
                        }
                    }
                    else
                    {
                        foreach (var item in rule.languages)
                        {
                            langs.Remove(item);
                        }
                    }
                }
            }

            LanguageSet ls = LanguageSet.From(langs);
            return ls.Equals(Languages.NO_LANGUAGES) ? Languages.ANY_LANGUAGE : ls;
        }
    }
}
