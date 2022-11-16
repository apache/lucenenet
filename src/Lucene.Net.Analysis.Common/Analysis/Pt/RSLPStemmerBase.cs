// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Pt
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
    /// Base class for stemmers that use a set of RSLP-like stemming steps.
    /// <para>
    /// RSLP (Removedor de Sufixos da Lingua Portuguesa) is an algorithm designed
    /// originally for stemming the Portuguese language, described in the paper
    /// <c>A Stemming Algorithm for the Portuguese Language</c>, Orengo et. al.
    /// </para>
    /// <para>
    /// Since this time a plural-only modification (RSLP-S) as well as a modification
    /// for the Galician language have been implemented. This class parses a configuration
    /// file that describes <see cref="Step"/>s, where each <see cref="Step"/> contains a set of <see cref="Rule"/>s.
    /// </para>
    /// <para>
    /// The general rule format is: 
    /// <code>{ "suffix", N, "replacement", { "exception1", "exception2", ...}}</code>
    /// where:
    /// <list type="bullet">
    ///   <item><description><c>suffix</c> is the suffix to be removed (such as "inho").</description></item>
    ///   <item><description><c>N</c> is the min stem size, where stem is defined as the candidate stem 
    ///       after removing the suffix (but before appending the replacement!)</description></item>
    ///   <item><description><c>replacement</c> is an optimal string to append after removing the suffix.
    ///       This can be the empty string.</description></item>
    ///   <item><description><c>exceptions</c> is an optional list of exceptions, patterns that should 
    ///       not be stemmed. These patterns can be specified as whole word or suffix (ends-with) 
    ///       patterns, depending upon the exceptions format flag in the step header.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// A step is an ordered list of rules, with a structure in this format:
    /// <blockquote>{ "name", N, B, { "cond1", "cond2", ... }
    ///               ... rules ... };
    /// </blockquote>
    /// where:
    /// <list type="bullet">
    ///   <item><description><c>name</c> is a name for the step (such as "Plural").</description></item>
    ///   <item><description><c>N</c> is the min word size. Words that are less than this length bypass
    ///       the step completely, as an optimization. Note: N can be zero, in this case this 
    ///       implementation will automatically calculate the appropriate value from the underlying 
    ///       rules.</description></item>
    ///   <item><description><c>B</c> is a "boolean" flag specifying how exceptions in the rules are matched.
    ///       A value of 1 indicates whole-word pattern matching, a value of 0 indicates that 
    ///       exceptions are actually suffixes and should be matched with ends-with.</description></item>
    ///   <item><description><c>conds</c> are an optional list of conditions to enter the step at all. If
    ///       the list is non-empty, then a word must end with one of these conditions or it will
    ///       bypass the step completely as an optimization.</description></item>
    /// </list>
    /// </para>
    /// <a href="http://www.inf.ufrgs.br/~viviane/rslp/index.htm">RSLP description</a>
    /// @lucene.internal
    /// </summary>
    public abstract class RSLPStemmerBase
    {
        /// <summary>
        /// A basic rule, with no exceptions.
        /// </summary>
        protected class Rule
        {
            protected internal readonly char[] m_suffix;
            protected readonly char[] m_replacement;
            protected internal readonly int m_min;

            /// <summary>
            /// Create a rule. </summary>
            /// <param name="suffix"> suffix to remove </param>
            /// <param name="min"> minimum stem length </param>
            /// <param name="replacement"> replacement string </param>
            public Rule(string suffix, int min, string replacement)
            {
                this.m_suffix = suffix.ToCharArray();
                this.m_replacement = replacement.ToCharArray();
                this.m_min = min;
            }

            /// <returns> true if the word matches this rule. </returns>
            public virtual bool Matches(char[] s, int len)
            {
                return (len - m_suffix.Length >= m_min && StemmerUtil.EndsWith(s, len, m_suffix));
            }

            /// <returns> new valid length of the string after firing this rule. </returns>
            public virtual int Replace(char[] s, int len)
            {
                if (m_replacement.Length > 0)
                {
                    Arrays.Copy(m_replacement, 0, s, len - m_suffix.Length, m_replacement.Length);
                }
                return len - m_suffix.Length + m_replacement.Length;
            }
        }

        /// <summary>
        /// A rule with a set of whole-word exceptions.
        /// </summary>
        protected class RuleWithSetExceptions : Rule
        {
            protected readonly CharArraySet m_exceptions;

            public RuleWithSetExceptions(string suffix, int min, string replacement, string[] exceptions) : base(suffix, min, replacement)
            {
                for (int i = 0; i < exceptions.Length; i++)
                {
                    if (!exceptions[i].EndsWith(suffix, StringComparison.Ordinal))
                    {
                        throw RuntimeException.Create("useless exception '" + exceptions[i] + "' does not end with '" + suffix + "'");
                    }
                }
                this.m_exceptions = new CharArraySet(
#pragma warning disable 612, 618
                    LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                    exceptions, false);
            }

            public override bool Matches(char[] s, int len)
            {
                return base.Matches(s, len) && !m_exceptions.Contains(s, 0, len);
            }
        }

        /// <summary>
        /// A rule with a set of exceptional suffixes.
        /// </summary>
        protected class RuleWithSuffixExceptions : Rule
        {
            // TODO: use a more efficient datastructure: automaton?
            protected readonly char[][] m_exceptions;

            public RuleWithSuffixExceptions(string suffix, int min, string replacement, string[] exceptions) : base(suffix, min, replacement)
            {
                for (int i = 0; i < exceptions.Length; i++)
                {
                    if (!exceptions[i].EndsWith(suffix, StringComparison.Ordinal))
                    {
                        throw RuntimeException.Create("warning: useless exception '" + exceptions[i] + "' does not end with '" + suffix + "'");
                    }
                }
                this.m_exceptions = new char[exceptions.Length][];
                for (int i = 0; i < exceptions.Length; i++)
                {
                    this.m_exceptions[i] = exceptions[i].ToCharArray();
                }
            }

            public override bool Matches(char[] s, int len)
            {
                if (!base.Matches(s, len))
                {
                    return false;
                }

                for (int i = 0; i < m_exceptions.Length; i++)
                {
                    if (StemmerUtil.EndsWith(s, len, m_exceptions[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// A step containing a list of rules.
        /// </summary>
        protected class Step
        {
            protected internal readonly string m_name;
            protected readonly Rule[] m_rules;
            protected readonly int m_min;
            protected readonly char[][] m_suffixes;

            /// <summary>
            /// Create a new step </summary>
            /// <param name="name"> Step's name. </param>
            /// <param name="rules"> an ordered list of rules. </param>
            /// <param name="min"> minimum word size. if this is 0 it is automatically calculated. </param>
            /// <param name="suffixes"> optional list of conditional suffixes. may be null. </param>
            public Step(string name, Rule[] rules, int min, string[] suffixes)
            {
                this.m_name = name;
                this.m_rules = rules;
                if (min == 0)
                {
                    min = int.MaxValue;
                    foreach (Rule r in rules)
                    {
                        min = Math.Min(min, r.m_min + r.m_suffix.Length);
                    }
                }
                this.m_min = min;

                if (suffixes is null || suffixes.Length == 0)
                {
                    this.m_suffixes = null;
                }
                else
                {
                    this.m_suffixes = new char[suffixes.Length][];
                    for (int i = 0; i < suffixes.Length; i++)
                    {
                        this.m_suffixes[i] = suffixes[i].ToCharArray();
                    }
                }
            }

            /// <returns> new valid length of the string after applying the entire step. </returns>
            public virtual int Apply(char[] s, int len)
            {
                if (len < m_min)
                {
                    return len;
                }

                if (m_suffixes != null)
                {
                    bool found = false;

                    for (int i = 0; i < m_suffixes.Length; i++)
                    {
                        if (StemmerUtil.EndsWith(s, len, m_suffixes[i]))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return len;
                    }
                }

                for (int i = 0; i < m_rules.Length; i++)
                {
                    if (m_rules[i].Matches(s, len))
                    {
                        return m_rules[i].Replace(s, len);
                    }
                }

                return len;
            }
        }

        /// <summary>
        /// Parse a resource file into an RSLP stemmer description. </summary>
        /// <returns> a Map containing the named <see cref="Step"/>s in this description. </returns>
        protected static IDictionary<string, Step> Parse(Type clazz, string resource)
        {
            try
            {
                IDictionary<string, Step> steps = new Dictionary<string, Step>();

                using (TextReader r = IOUtils.GetDecodingReader(clazz, resource, Encoding.UTF8))
                {
                    string step;
                    while ((step = ReadLine(r)) != null)
                    {
                        Step s = ParseStep(r, step);
                        steps[s.m_name] = s;
                    }
                }
                return steps;
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        private static readonly Regex headerPattern = new Regex("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*(0|1),\\s*\\{(.*)\\},\\s*$", RegexOptions.Compiled);
        private static readonly Regex stripPattern = new Regex("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+)\\s*\\}\\s*(,|(\\}\\s*;))$", RegexOptions.Compiled);
        private static readonly Regex repPattern = new Regex("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*\"([^\"]*)\"\\}\\s*(,|(\\}\\s*;))$", RegexOptions.Compiled);
        private static readonly Regex excPattern = new Regex("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*\"([^\"]*)\",\\s*\\{(.*)\\}\\s*\\}\\s*(,|(\\}\\s*;))$", RegexOptions.Compiled);

        private static Step ParseStep(TextReader r, string header)
        {
            Match matcher = headerPattern.Match(header);
            if (!matcher.Success)
            {
                throw RuntimeException.Create("Illegal Step header specified at line " /*+ r.LineNumber*/); // TODO Line number
            }
            //if (Debugging.AssertsEnabled) Debugging.Assert(headerPattern.GetGroupNumbers().Length == 4); // Not possible to read the number of groups that matched in .NET
            string name = matcher.Groups[1].Value;
            int min = int.Parse(matcher.Groups[2].Value, CultureInfo.InvariantCulture);
            int type = int.Parse(matcher.Groups[3].Value, CultureInfo.InvariantCulture);
            string[] suffixes = ParseList(matcher.Groups[4].Value);
            Rule[] rules = ParseRules(r, type);
            return new Step(name, rules, min, suffixes);
        }

        private static Rule[] ParseRules(TextReader r, int type)
        {
            IList<Rule> rules = new JCG.List<Rule>();
            string line;
            while ((line = ReadLine(r)) != null)
            {
                Match matcher = stripPattern.Match(line);
                if (matcher.Success)
                {
                    rules.Add(new Rule(matcher.Groups[1].Value, int.Parse(matcher.Groups[2].Value, CultureInfo.InvariantCulture), ""));
                }
                else
                {
                    matcher = repPattern.Match(line);
                    if (matcher.Success)
                    {
                        rules.Add(new Rule(matcher.Groups[1].Value, int.Parse(matcher.Groups[2].Value, CultureInfo.InvariantCulture), matcher.Groups[3].Value));
                    }
                    else
                    {
                        matcher = excPattern.Match(line);
                        if (matcher.Success)
                        {
                            if (type == 0)
                            {
                                rules.Add(new RuleWithSuffixExceptions(matcher.Groups[1].Value, int.Parse(matcher.Groups[2].Value, CultureInfo.InvariantCulture), matcher.Groups[3].Value, ParseList(matcher.Groups[4].Value)));
                            }
                            else
                            {
                                rules.Add(new RuleWithSetExceptions(matcher.Groups[1].Value, int.Parse(matcher.Groups[2].Value, CultureInfo.InvariantCulture), matcher.Groups[3].Value, ParseList(matcher.Groups[4].Value)));
                            }
                        }
                        else
                        {
                            throw RuntimeException.Create("Illegal Step rule specified at line " /*+ r.LineNumber*/);
                        }
                    }
                }
                if (line.EndsWith(";", StringComparison.Ordinal))
                {
                    return rules.ToArray();
                }
            }
            return null;
        }

        private static string[] ParseList(string s)
        {
            if (s.Length == 0)
            {
                return null;
            }
            string[] list = s.Split(',').TrimEnd();
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = ParseString(list[i].Trim());
            }
            return list;
        }

        private static string ParseString(string s)
        {
            return s.Substring(1, (s.Length - 1) - 1);
        }

        private static string ReadLine(TextReader r)
        {
            string line = null;
            while ((line = r.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length > 0 && line[0] != '#')
                {
                    return line;
                }
            }
            return line;
        }
    }
}