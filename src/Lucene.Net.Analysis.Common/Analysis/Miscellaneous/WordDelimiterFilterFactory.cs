// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Integer = J2N.Numerics.Int32;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Factory for <see cref="WordDelimiterFilter"/>.
    /// <code>
    /// &lt;fieldType name="text_wd" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.WordDelimiterFilterFactory" protected="protectedword.txt"
    ///             preserveOriginal="0" splitOnNumerics="1" splitOnCaseChange="1"
    ///             catenateWords="0" catenateNumbers="0" catenateAll="0"
    ///             generateWordParts="1" generateNumberParts="1" stemEnglishPossessive="1"
    ///             types="wdfftypes.txt" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </summary>
    public class WordDelimiterFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public const string PROTECTED_TOKENS = "protected";
        public const string TYPES = "types";

        private readonly string wordFiles;
        private readonly string types;
        private readonly WordDelimiterFlags flags;
        private byte[] typeTable = null;
        private CharArraySet protectedWords = null;

        /// <summary>
        /// Creates a new <see cref="WordDelimiterFilterFactory"/> </summary>
        public WordDelimiterFilterFactory(IDictionary<string, string> args) 
            : base(args)
        {
            AssureMatchVersion();
            WordDelimiterFlags flags = 0;
            if (GetInt32(args, "generateWordParts", 1) != 0)
            {
                flags |= WordDelimiterFlags.GENERATE_WORD_PARTS;
            }
            if (GetInt32(args, "generateNumberParts", 1) != 0)
            {
                flags |= WordDelimiterFlags.GENERATE_NUMBER_PARTS;
            }
            if (GetInt32(args, "catenateWords", 0) != 0)
            {
                flags |= WordDelimiterFlags.CATENATE_WORDS;
            }
            if (GetInt32(args, "catenateNumbers", 0) != 0)
            {
                flags |= WordDelimiterFlags.CATENATE_NUMBERS;
            }
            if (GetInt32(args, "catenateAll", 0) != 0)
            {
                flags |= WordDelimiterFlags.CATENATE_ALL;
            }
            if (GetInt32(args, "splitOnCaseChange", 1) != 0)
            {
                flags |= WordDelimiterFlags.SPLIT_ON_CASE_CHANGE;
            }
            if (GetInt32(args, "splitOnNumerics", 1) != 0)
            {
                flags |= WordDelimiterFlags.SPLIT_ON_NUMERICS;
            }
            if (GetInt32(args, "preserveOriginal", 0) != 0)
            {
                flags |= WordDelimiterFlags.PRESERVE_ORIGINAL;
            }
            if (GetInt32(args, "stemEnglishPossessive", 1) != 0)
            {
                flags |= WordDelimiterFlags.STEM_ENGLISH_POSSESSIVE;
            }
            wordFiles = Get(args, PROTECTED_TOKENS);
            types = Get(args, TYPES);
            this.flags = flags;
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (wordFiles != null)
            {
                protectedWords = GetWordSet(loader, wordFiles, false);
            }
            if (types != null)
            {
                IList<string> files = SplitFileNames(types);
                IList<string> wlist = new JCG.List<string>();
                foreach (string file in files)
                {
                    IList<string> lines = GetLines(loader, file.Trim());
                    wlist.AddRange(lines);
                }
                typeTable = ParseTypes(wlist);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            if (m_luceneMatchVersion.OnOrAfter(LuceneVersion.LUCENE_48))
            {
                return new WordDelimiterFilter(m_luceneMatchVersion, input, typeTable is null ? WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE : typeTable, flags, protectedWords);
            }
            else
            {
#pragma warning disable 612, 618
                return new Lucene47WordDelimiterFilter(
#pragma warning restore 612, 618
                    input, typeTable ?? WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, protectedWords);
            }
        }

        // source => type
        private static readonly Regex typePattern = new Regex("(.*)\\s*=>\\s*(.*)\\s*$", RegexOptions.Compiled);

        // parses a list of MappingCharFilter style rules into a custom byte[] type table
        private byte[] ParseTypes(IList<string> rules)
        {
            IDictionary<char, byte> typeMap = new JCG.SortedDictionary<char, byte>();
            foreach (string rule in rules)
            {
                Match m = typePattern.Match(rule);
                if (!m.Success)
                {
                    throw new ArgumentException("Invalid Mapping Rule : [" + rule + "]");
                }
                string lhs = ParseString(m.Groups[1].Value.Trim());
                byte rhs = ParseType(m.Groups[2].Value.Trim());
                if (lhs.Length != 1)
                {
                    throw new ArgumentException("Invalid Mapping Rule : [" + rule + "]. Only a single character is allowed.");
                }
                if (rhs == WordDelimiterFilter.NOT_SET)
                {
                    throw new ArgumentException("Invalid Mapping Rule : [" + rule + "]. Illegal type.");
                }
                typeMap[lhs[0]] = rhs;
            }

            // ensure the table is always at least as big as DEFAULT_WORD_DELIM_TABLE for performance
            byte[] types = new byte[Math.Max(typeMap.Keys.LastOrDefault() + 1, WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE.Length)];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = WordDelimiterIterator.GetType(i);
            }
            foreach (var mapping in typeMap)
            {
                types[mapping.Key] = mapping.Value;
            }
            return types;
        }

        private static byte ParseType(string s) // LUCENENET: CA1822: Mark members as static
        {
            if (s.Equals("LOWER", StringComparison.Ordinal))
            {
                return WordDelimiterFilter.LOWER;
            }
            else if (s.Equals("UPPER", StringComparison.Ordinal))
            {
                return WordDelimiterFilter.UPPER;
            }
            else if (s.Equals("ALPHA", StringComparison.Ordinal))
            {
                return WordDelimiterFilter.ALPHA;
            }
            else if (s.Equals("DIGIT", StringComparison.Ordinal))
            {
                return WordDelimiterFilter.DIGIT;
            }
            else if (s.Equals("ALPHANUM", StringComparison.Ordinal))
            {
                return WordDelimiterFilter.ALPHANUM;
            }
            else if (s.Equals("SUBWORD_DELIM", StringComparison.Ordinal))
            {
                return WordDelimiterFilter.SUBWORD_DELIM;
            }
            else
            {
                //return null;
                return WordDelimiterFilter.NOT_SET;

            }
        }

        internal char[] @out = new char[256];

        private string ParseString(string s)
        {
            int readPos = 0;
            int len = s.Length;
            int writePos = 0;
            while (readPos < len)
            {
                char c = s[readPos++];
                if (c == '\\')
                {
                    if (readPos >= len)
                    {
                        throw new ArgumentException("Invalid escaped char in [" + s + "]");
                    }
                    c = s[readPos++];
                    switch (c)
                    {
                        case '\\':
                            c = '\\';
                            break;
                        case 'n':
                            c = '\n';
                            break;
                        case 't':
                            c = '\t';
                            break;
                        case 'r':
                            c = '\r';
                            break;
                        case 'b':
                            c = '\b';
                            break;
                        case 'f':
                            c = '\f';
                            break;
                        case 'u':
                            if (readPos + 3 >= len)
                            {
                                throw new ArgumentException("Invalid escaped char in [" + s + "]");
                            }
                            // LUCENENET: Optimized parse so we don't allocate a substring
                            c = (char)Integer.Parse(s, readPos, 4, radix: 16);
                            readPos += 4;
                            break;
                    }
                }
                @out[writePos++] = c;
            }
            return new string(@out, 0, writePos);
        }
    }
}