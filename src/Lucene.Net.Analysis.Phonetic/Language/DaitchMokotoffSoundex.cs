// commons-codec version compatibility level: 1.10
using J2N;
using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

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
    /// Encodes a string into a Daitch-Mokotoff Soundex value.
    /// </summary>
    /// <remarks>
    /// The Daitch-Mokotoff Soundex algorithm is a refinement of the Russel and American Soundex algorithms, yielding greater
    /// accuracy in matching especially Slavish and Yiddish surnames with similar pronunciation but differences in spelling.
    /// <para/>
    /// The main differences compared to the other soundex variants are:
    /// <list type="bullet">
    ///     <item><description>coded names are 6 digits long</description></item>
    ///     <item><description>the initial character of the name is coded</description></item>
    ///     <item><description>rules to encoded multi-character n-grams</description></item>
    ///     <item><description>multiple possible encodings for the same name (branching)</description></item>
    /// </list>
    /// <para/>
    /// This implementation supports branching, depending on the used method:
    /// <list type="bullet">
    ///     <item><term><see cref="Encode(string)"/></term><description>branching disabled, only the first code will be returned</description></item>
    ///     <item><term><see cref="GetSoundex(string)"/></term><description>branching enabled, all codes will be returned, separated by '|'</description></item>
    /// </list>
    /// <para/>
    /// Note: this implementation has additional branching rules compared to the original description of the algorithm. The
    /// rules can be customized by overriding the default rules contained in the resource file
    /// <c>Lucene.Net.Analysis.Phonetic.Language.dmrules.txt</c>.
    /// <para/>
    /// This class is thread-safe.
    /// <para/>
    /// See: <a href="http://en.wikipedia.org/wiki/Daitch%E2%80%93Mokotoff_Soundex"> Wikipedia - Daitch-Mokotoff Soundex</a>
    /// <para/>
    /// See: <a href="http://www.avotaynu.com/soundex.htm">Avotaynu - Soundexing and Genealogy</a>
    /// <para/>
    /// since 1.10
    /// </remarks>
    /// <seealso cref="Soundex"/>
    public class DaitchMokotoffSoundex : IStringEncoder
    {
        /// <summary>
        /// Inner class representing a branch during DM soundex encoding.
        /// </summary>
        private sealed class Branch
        {
            private readonly StringBuilder builder;
            private string cachedString;
            private string lastReplacement;

            internal Branch()
            {
                builder = new StringBuilder();
                lastReplacement = null;
                cachedString = null;
            }

            /// <summary>
            /// Creates a new branch, identical to this branch.
            /// </summary>
            /// <returns>A new, identical branch.</returns>
            public Branch CreateBranch()
            {
                Branch branch = new Branch();
                branch.builder.Append(ToString());
                branch.lastReplacement = this.lastReplacement;
                return branch;
            }

            public override bool Equals(object other)
            {
                if (this == other)
                {
                    return true;
                }
                if (!(other is Branch))
                {
                    return false;
                }

                return ToString().Equals(((Branch)other).ToString(), StringComparison.Ordinal);
            }

            /// <summary>
            /// Finish this branch by appending '0's until the maximum code length has been reached.
            /// </summary>
            public void Finish()
            {
                while (builder.Length < MAX_LENGTH)
                {
                    builder.Append('0');
                    cachedString = null;
                }
            }

            public override int GetHashCode()
            {
                return ToString().GetHashCode();
            }

            /// <summary>
            /// Process the next replacement to be added to this branch.
            /// </summary>
            /// <param name="replacement">The next replacement to append.</param>
            /// <param name="forceAppend">Indicates if the default processing shall be overridden.</param>
            public void ProcessNextReplacement(string replacement, bool forceAppend)
            {
                bool append = lastReplacement is null || !lastReplacement.EndsWith(replacement, StringComparison.Ordinal) || forceAppend;

                if (append && builder.Length < MAX_LENGTH)
                {
                    builder.Append(replacement);
                    // remove all characters after the maximum length
                    if (builder.Length > MAX_LENGTH)
                    {
                        //builder.delete(MAX_LENGTH, builder.Length);
                        builder.Remove(MAX_LENGTH, builder.Length - MAX_LENGTH);
                    }
                    cachedString = null;
                }

                lastReplacement = replacement;
            }

            public override string ToString()
            {
                if (cachedString is null)
                {
                    cachedString = builder.ToString();
                }
                return cachedString;
            }
        }

        /// <summary>
        /// Inner class for storing rules.
        /// </summary>
        private sealed class Rule
        {
            private readonly static Regex PIPE = new Regex(@"\|", RegexOptions.Compiled);

            private readonly string pattern;
            private readonly string[] replacementAtStart;
            private readonly string[] replacementBeforeVowel;
            private readonly string[] replacementDefault;

            internal Rule(string pattern, string replacementAtStart, string replacementBeforeVowel,
                    string replacementDefault)
            {
                this.pattern = pattern;
                this.replacementAtStart = PIPE.Split(replacementAtStart);
                this.replacementBeforeVowel = PIPE.Split(replacementBeforeVowel);
                this.replacementDefault = PIPE.Split(replacementDefault);
            }

            // LUCENENET specific - need read access to pattern
            public string Pattern => pattern;

            public int PatternLength => pattern.Length;

            public string[] GetReplacements(string context, bool atStart)
            {
                if (atStart)
                {
                    return replacementAtStart;
                }

                int nextIndex = PatternLength;
                bool nextCharIsVowel = nextIndex < context.Length ? IsVowel(context[nextIndex]) : false;
                if (nextCharIsVowel)
                {
                    return replacementBeforeVowel;
                }

                return replacementDefault;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsVowel(char ch) // LUCENENET: CA1822: Mark members as static
            {
                return ch == 'a' || ch == 'e' || ch == 'i' || ch == 'o' || ch == 'u';
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Matches(string context)
            {
                return context.StartsWith(pattern, StringComparison.Ordinal);
            }

            public override string ToString()
            {
                return string.Format("{0}=({1},{2},{3})", pattern, Collections.ToString(replacementAtStart),
                    Collections.ToString(replacementBeforeVowel), Collections.ToString(replacementDefault));
            }
        }

        private const string COMMENT = "//";
        private const string DOUBLE_QUOTE = "\"";

        private const string MULTILINE_COMMENT_END = "*/";

        private const string MULTILINE_COMMENT_START = "/*";

        /// <summary>The resource file containing the replacement and folding rules</summary>
        private const string RESOURCE_FILE = "dmrules.txt";

        /// <summary>The code length of a DM soundex value.</summary>
        private const int MAX_LENGTH = 6;

        /// <summary>Transformation rules indexed by the first character of their pattern.</summary>
        private static readonly IDictionary<char, IList<Rule>> RULES = new Dictionary<char, IList<Rule>>();

        /// <summary>Folding rules.</summary>
        private static readonly IDictionary<char, char> FOLDINGS = new Dictionary<char, char>();

        private static readonly Regex WHITESPACE = new Regex(@"\s+", RegexOptions.Compiled);

        private class DaitchMokotoffRuleComparer : IComparer<Rule>
        {
            private DaitchMokotoffRuleComparer() { } // LUCENENET: Made into singleton

            public static IComparer<Rule> Default { get; } = new DaitchMokotoffRuleComparer();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(Rule rule1, Rule rule2)
            {
                return rule2.PatternLength - rule1.PatternLength;
            }
        }

        static DaitchMokotoffSoundex()
        {
            Stream rulesIS = typeof(DaitchMokotoffSoundex).FindAndGetManifestResourceStream(RESOURCE_FILE);
            if (rulesIS is null)
            {
                throw new ArgumentException("Unable to load resource: " + RESOURCE_FILE);
            }

            using (TextReader scanner = new StreamReader(rulesIS, Encoding.UTF8))
            {
                ParseRules(scanner, RESOURCE_FILE, RULES, FOLDINGS);
            }

            // sort RULES by pattern length in descending order
            foreach (var rule in RULES)
            {
                IList<Rule> ruleList = rule.Value;
                ruleList.Sort(DaitchMokotoffRuleComparer.Default);
            }
        }

        private static void ParseRules(TextReader scanner, string location,
            IDictionary<char, IList<Rule>> ruleMapping, IDictionary<char, char> asciiFoldings)
        {
            int currentLine = 0;
            bool inMultilineComment = false;

            string rawLine;
            while ((rawLine = scanner.ReadLine()) != null)
            {
                currentLine++;
                string line = rawLine;

                if (inMultilineComment)
                {
                    if (line.EndsWith(MULTILINE_COMMENT_END, StringComparison.Ordinal))
                    {
                        inMultilineComment = false;
                    }
                    continue;
                }

                if (line.StartsWith(MULTILINE_COMMENT_START, StringComparison.Ordinal))
                {
                    inMultilineComment = true;
                }
                else
                {
                    // discard comments
                    int cmtI = line.IndexOf(COMMENT, StringComparison.Ordinal);
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

                    if (line.Contains("="))
                    {
                        // folding
                        string[] parts = line.Split('=').TrimEnd();
                        if (parts.Length != 2)
                        {
                            throw new ArgumentException("Malformed folding statement split into " + parts.Length +
                                    " parts: " + rawLine + " in " + location);
                        }
                        else
                        {
                            string leftCharacter = parts[0];
                            string rightCharacter = parts[1];

                            if (leftCharacter.Length != 1 || rightCharacter.Length != 1)
                            {
                                throw new ArgumentException("Malformed folding statement - " +
                                        "patterns are not single characters: " + rawLine + " in " + location);
                            }

                            asciiFoldings[leftCharacter[0]] = rightCharacter[0];
                        }
                    }
                    else
                    {
                        // rule
                        string[] parts = WHITESPACE.Split(line).TrimEnd();
                        if (parts.Length != 4)
                        {
                            throw new ArgumentException("Malformed rule statement split into " + parts.Length +
                                    " parts: " + rawLine + " in " + location);
                        }
                        else
                        {
                            try
                            {
                                string pattern = StripQuotes(parts[0]);
                                string replacement1 = StripQuotes(parts[1]);
                                string replacement2 = StripQuotes(parts[2]);
                                string replacement3 = StripQuotes(parts[3]);

                                Rule r = new Rule(pattern, replacement1, replacement2, replacement3);
                                char patternKey = r.Pattern[0];
                                if (!ruleMapping.TryGetValue(patternKey, out IList<Rule> rules) || rules is null)
                                {
                                    rules = new JCG.List<Rule>();
                                    ruleMapping[patternKey] = rules;
                                }
                                rules.Add(r);
                            }
                            catch (Exception e) when (e.IsIllegalArgumentException())
                            {
                                throw new InvalidOperationException(
                                        "Problem parsing line '" + currentLine + "' in " + location, e);
                            }
                        }
                    }
                }
            }
        }

        private static string StripQuotes(string str)
        {
            if (str.StartsWith(DOUBLE_QUOTE, StringComparison.Ordinal))
            {
                str = str.Substring(1);
            }

            if (str.EndsWith(DOUBLE_QUOTE, StringComparison.Ordinal))
            {
                str = str.Substring(0, str.Length - 1);
            }

            return str;
        }

        /// <summary>Whether to use ASCII folding prior to encoding.</summary>
        private readonly bool folding;

        /// <summary>
        /// Creates a new instance with ASCII-folding enabled.
        /// </summary>
        public DaitchMokotoffSoundex()
            : this(true)
        {
        }

        /// <summary>
        /// Creates a new instance.
        /// <para/>
        /// With ASCII-folding enabled, certain accented characters will be transformed to equivalent ASCII characters, e.g.
        /// è -&gt; e.
        /// </summary>
        /// <param name="folding">If ASCII-folding shall be performed before encoding.</param>
        public DaitchMokotoffSoundex(bool folding)
        {
            this.folding = folding;
        }

        /// <summary>
        /// Performs a cleanup of the input string before the actual soundex transformation.
        /// <para/>
        /// Removes all whitespace characters and performs ASCII folding if enabled.
        /// </summary>
        /// <param name="input">The input string to cleanup.</param>
        /// <returns>A cleaned up string.</returns>
        private string Cleanup(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input.ToCharArray())
            {
                char ch = c;
                if (char.IsWhiteSpace(ch))
                {
                    continue;
                }

                ch = char.ToLowerInvariant(ch);
                if (folding && FOLDINGS.TryGetValue(ch, out char newChar))
                {
                    ch = newChar;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }

        // LUCENENET specific - in .NET we don't need an object overload of Encode(), since strings are sealed anyway.

        /// <summary>
        /// Encodes a string using the Daitch-Mokotoff soundex algorithm without branching.
        /// </summary>
        /// <param name="source">A string to encode.</param>
        /// <returns>A DM Soundex code corresponding to the string supplied.</returns>
        /// <exception cref="ArgumentException">If a character is not mapped.</exception>
        /// <seealso cref="GetSoundex(string)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string Encode(string source)
        {
            if (source is null)
            {
                return null;
            }
            return GetSoundex(source, false)[0];
        }

        /// <summary>
        /// Encodes a string using the Daitch-Mokotoff soundex algorithm with branching.
        /// <para/>
        /// In case a string is encoded into multiple codes (see branching rules), the result will contain all codes,
        /// separated by '|'.
        /// <para/>
        /// Example: the name "AUERBACH" is encoded as both
        /// <list type="bullet">
        ///     <item><description>097400</description></item>
        ///     <item><description>097500</description></item>
        /// </list>
        /// <para/>
        /// Thus the result will be "097400|097500".
        /// </summary>
        /// <param name="source">A string to encode.</param>
        /// <returns>A string containing a set of DM Soundex codes corresponding to the string supplied.</returns>
        /// <exception cref="ArgumentException">If a character is not mapped.</exception>
        public virtual string GetSoundex(string source)
        {
            string[] branches = GetSoundex(source, true);
            StringBuilder sb = new StringBuilder();
            int index = 0;
            foreach (string branch in branches)
            {
                sb.Append(branch);
                if (++index < branches.Length)
                {
                    sb.Append('|');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Perform the actual DM Soundex algorithm on the input string.
        /// </summary>
        /// <param name="source">A string to encode.</param>
        /// <param name="branching">If branching shall be performed.</param>
        /// <returns>A string array containing all DM Soundex codes corresponding to the string supplied depending on the selected branching mode.</returns>
        /// <exception cref="ArgumentException">If a character is not mapped.</exception>
        private string[] GetSoundex(string source, bool branching)
        {
            if (source is null)
            {
                return null;
            }

            string input = Cleanup(source);

            // LinkedHashSet preserves input order. In .NET we can use List for that purpose.
            IList<Branch> currentBranches = new JCG.List<Branch>
            {
                new Branch()
            };

            char lastChar = '\0';
            for (int index = 0; index < input.Length; index++)
            {
                char ch = input[index];

                // ignore whitespace inside a name
                if (char.IsWhiteSpace(ch))
                {
                    continue;
                }

                string inputContext = input.Substring(index);
                if (!RULES.TryGetValue(ch, out IList<Rule> rules) || rules is null)
                {
                    continue;
                }

                // use an EMPTY_LIST to avoid false positive warnings wrt potential null pointer access
                IList<Branch> nextBranches = branching ? new JCG.List<Branch>() : Collections.EmptyList<Branch>() as IList<Branch>;

                foreach (Rule rule in rules)
                {
                    if (rule.Matches(inputContext))
                    {
                        if (branching)
                        {
                            nextBranches.Clear();
                        }
                        string[] replacements = rule.GetReplacements(inputContext, lastChar == '\0');
                        bool branchingRequired = replacements.Length > 1 && branching;

                        foreach (Branch branch in currentBranches)
                        {
                            foreach (string nextReplacement in replacements)
                            {
                                // if we have multiple replacements, always create a new branch
                                Branch nextBranch = branchingRequired ? branch.CreateBranch() : branch;

                                // special rule: occurrences of mn or nm are treated differently
                                bool force = (lastChar == 'm' && ch == 'n') || (lastChar == 'n' && ch == 'm');

                                nextBranch.ProcessNextReplacement(nextReplacement, force);

                                if (branching)
                                {
                                    if (!nextBranches.Contains(nextBranch))
                                    {
                                        nextBranches.Add(nextBranch);
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        if (branching)
                        {
                            currentBranches.Clear();
                            currentBranches.AddRange(nextBranches);
                        }
                        index += rule.PatternLength - 1;
                        break;
                    }
                }

                lastChar = ch;
            }

            string[] result = new string[currentBranches.Count];
            int idx = 0;
            foreach (Branch branch in currentBranches)
            {
                branch.Finish();
                result[idx++] = branch.ToString();
            }

            return result;
        }
    }
}
