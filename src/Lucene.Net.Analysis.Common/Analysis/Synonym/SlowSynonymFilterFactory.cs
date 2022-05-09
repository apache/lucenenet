﻿// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Synonym
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
    /// Factory for <see cref="SlowSynonymFilter"/> (only used with luceneMatchVersion &lt; 3.4)
    /// <code>
    /// &lt;fieldType name="text_synonym" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.SynonymFilterFactory" synonyms="synonyms.txt" ignoreCase="false"
    ///             expand="true" tokenizerFactory="solr.WhitespaceTokenizerFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    /// @deprecated (3.4) use SynonymFilterFactory instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0 
    [Obsolete("(3.4) use SynonymFilterFactory instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0")]
    internal sealed class SlowSynonymFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private readonly string synonyms;
        private readonly bool ignoreCase;
        private readonly bool expand;
        private readonly string tf;
        private readonly IDictionary<string, string> tokArgs = new Dictionary<string, string>();

        // LUCENENET: Optimized by pre-comiling regex and lazy-loading
        private class Holder
        {
            public static readonly Regex TOKENIZER_FACTORY_REPLACEMENT_PATTERN = new Regex("^tokenizerFactory\\.", RegexOptions.Compiled);
        }

        public SlowSynonymFilterFactory(IDictionary<string, string> args) 
            : base(args)
        {
            synonyms = Require(args, "synonyms");
            ignoreCase = GetBoolean(args, "ignoreCase", false);
            expand = GetBoolean(args, "expand", true);

            tf = Get(args, "tokenizerFactory");
            if (tf != null)
            {
                AssureMatchVersion();
                tokArgs["luceneMatchVersion"] = LuceneMatchVersion.ToString();

                var keys = new JCG.List<string>(args.Keys);
                foreach (string key in keys)
                {
                    tokArgs[Holder.TOKENIZER_FACTORY_REPLACEMENT_PATTERN.Replace(key, "")] = args[key];
                    args.Remove(key);
                }
            }
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public void Inform(IResourceLoader loader)
        {
            TokenizerFactory tokFactory = null;
            if (tf != null)
            {
                tokFactory = LoadTokenizerFactory(loader, tf);
            }

            IEnumerable<string> wlist = LoadRules(synonyms, loader);

            synMap = new SlowSynonymMap(ignoreCase);
            ParseRules(wlist, synMap, "=>", ",", expand, tokFactory);
        }

        /// <returns> a list of all rules </returns>
        private IEnumerable<string> LoadRules(string synonyms, IResourceLoader loader)
        {
            JCG.List<string> wlist = null;
            if (File.Exists(synonyms))
            {
                wlist = new JCG.List<string>(GetLines(loader, synonyms));
            }
            else
            {
                IList<string> files = SplitFileNames(synonyms);
                wlist = new JCG.List<string>();
                foreach (string file in files)
                {
                    IList<string> lines = GetLines(loader, file.Trim());
                    wlist.AddRange(lines);
                }
            }
            return wlist;
        }

        private SlowSynonymMap synMap;

        internal static void ParseRules(IEnumerable<string> rules, SlowSynonymMap map, string mappingSep, string synSep, bool expansion, TokenizerFactory tokFactory)
        {
            int count = 0;
            foreach (string rule in rules)
            {
                // To use regexes, we need an expression that specifies an odd number of chars.
                // This can't really be done with string.split(), and since we need to
                // do unescaping at some point anyway, we wouldn't be saving any effort
                // by using regexes.

                IList<string> mapping = SplitSmart(rule, mappingSep, false);

                IList<IList<string>> source;
                IList<IList<string>> target;

                if (mapping.Count > 2)
                {
                    throw new ArgumentException("Invalid Synonym Rule:" + rule);
                }
                else if (mapping.Count == 2)
                {
                    source = GetSynList(mapping[0], synSep, tokFactory);
                    target = GetSynList(mapping[1], synSep, tokFactory);
                }
                else
                {
                    source = GetSynList(mapping[0], synSep, tokFactory);
                    if (expansion)
                    {
                        // expand to all arguments
                        target = source;
                    }
                    else
                    {
                        // reduce to first argument
                        target = new JCG.List<IList<string>>(1)
                        {
                            source[0]
                        };
                    }
                }

                bool includeOrig = false;
                foreach (IList<string> fromToks in source)
                {
                    count++;
                    foreach (IList<string> toToks in target)
                    {
                        map.Add(fromToks, SlowSynonymMap.MakeTokens(toToks), includeOrig, true);
                    }
                }
            }
        }

        // a , b c , d e f => [[a],[b,c],[d,e,f]]
        private static IList<IList<string>> GetSynList(string str, string separator, TokenizerFactory tokFactory)
        {
            IList<string> strList = SplitSmart(str, separator, false);
            // now split on whitespace to get a list of token strings
            IList<IList<string>> synList = new JCG.List<IList<string>>();
            foreach (string toks in strList)
            {
                IList<string> tokList = tokFactory is null ? SplitWS(toks, true) : SplitByTokenizer(toks, tokFactory);
                synList.Add(tokList);
            }
            return synList;
        }

        private static IList<string> SplitByTokenizer(string source, TokenizerFactory tokFactory)
        {
            StringReader reader = new StringReader(source);
            TokenStream ts = LoadTokenizer(tokFactory, reader);
            IList<string> tokList = new JCG.List<string>();
            try
            {
                ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();
                ts.Reset();
                while (ts.IncrementToken())
                {
                    if (termAtt.Length > 0)
                    {
                        tokList.Add(termAtt.ToString());
                    }
                }
            }
            finally
            {
                reader.Dispose();
            }
            return tokList;
        }

        private TokenizerFactory LoadTokenizerFactory(IResourceLoader loader, string cname)
        {
            Type clazz = loader.FindType(cname);
            try
            {
                TokenizerFactory tokFactory = (TokenizerFactory)Activator.CreateInstance(clazz, new object[] { tokArgs });

                if (tokFactory is IResourceLoaderAware resourceLoaderAware)
                {
                    resourceLoaderAware.Inform(loader);
                }
                return tokFactory;
            }
            catch (Exception e) when (e.IsException())
            {
                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
            }
        }

        private static TokenStream LoadTokenizer(TokenizerFactory tokFactory, TextReader reader)
        {
            return tokFactory.Create(reader);
        }

        public SlowSynonymMap SynonymMap => synMap;

        public override TokenStream Create(TokenStream input)
        {
            return new SlowSynonymFilter(input, synMap);
        }

        public static IList<string> SplitWS(string s, bool decode)
        {
            IList<string> lst = new JCG.List<string>(2);
            StringBuilder sb = new StringBuilder();
            int pos = 0, end = s.Length;
            while (pos < end)
            {
                char ch = s[pos++];
                if (char.IsWhiteSpace(ch))
                {
                    if (sb.Length > 0)
                    {
                        lst.Add(sb.ToString());
                        sb = new StringBuilder();
                    }
                    continue;
                }

                if (ch == '\\')
                {
                    if (!decode)
                    {
                        sb.Append(ch);
                    }
                    if (pos >= end) // ERROR, or let it go?
                    {
                        break;
                    }
                    ch = s[pos++];
                    if (decode)
                    {
                        switch (ch)
                        {
                            case 'n':
                                ch = '\n';
                                break;
                            case 't':
                                ch = '\t';
                                break;
                            case 'r':
                                ch = '\r';
                                break;
                            case 'b':
                                ch = '\b';
                                break;
                            case 'f':
                                ch = '\f';
                                break;
                        }
                    }
                }

                sb.Append(ch);
            }

            if (sb.Length > 0)
            {
                lst.Add(sb.ToString());
            }

            return lst;
        }

        /// <summary>
        /// Splits a backslash escaped string on the separator.
        /// <para/>
        /// Current backslash escaping supported:
        /// <para/> \n \t \r \b \f are escaped the same as a .NET string
        /// <para/> Other characters following a backslash are produced verbatim (\c => c)
        /// </summary>
        /// <param name="s">  the string to split </param>
        /// <param name="separator"> the separator to split on </param>
        /// <param name="decode"> decode backslash escaping </param>
        public static IList<string> SplitSmart(string s, string separator, bool decode)
        {
            IList<string> lst = new JCG.List<string>(2);
            StringBuilder sb = new StringBuilder();
            int pos = 0, end = s.Length;
            while (pos < end)
            {
                //if (s.StartsWith(separator,pos))
                if (s.Substring(pos).StartsWith(separator, StringComparison.Ordinal))
                {
                    if (sb.Length > 0)
                    {
                        lst.Add(sb.ToString());
                        sb = new StringBuilder();
                    }
                    pos += separator.Length;
                    continue;
                }

                char ch = s[pos++];
                if (ch == '\\')
                {
                    if (!decode)
                    {
                        sb.Append(ch);
                    }
                    if (pos >= end) // ERROR, or let it go?
                    {
                        break;
                    }
                    ch = s[pos++];
                    if (decode)
                    {
                        switch (ch)
                        {
                            case 'n':
                                ch = '\n';
                                break;
                            case 't':
                                ch = '\t';
                                break;
                            case 'r':
                                ch = '\r';
                                break;
                            case 'b':
                                ch = '\b';
                                break;
                            case 'f':
                                ch = '\f';
                                break;
                        }
                    }
                }

                sb.Append(ch);
            }

            if (sb.Length > 0)
            {
                lst.Add(sb.ToString());
            }

            return lst;
        }
    }
}