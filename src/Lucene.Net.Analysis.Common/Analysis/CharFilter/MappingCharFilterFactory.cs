using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.CharFilters
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
    /// Factory for <see cref="MappingCharFilter"/>. 
    /// <code>
    /// &lt;fieldType name="text_map" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;charFilter class="solr.MappingCharFilterFactory" mapping="mapping.txt"/&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// 
    /// @since Solr 1.4
    /// </summary>
    public class MappingCharFilterFactory : CharFilterFactory, IResourceLoaderAware, IMultiTermAwareComponent
    {
        protected internal NormalizeCharMap m_normMap;
        private readonly string mapping;

        /// <summary>
        /// Creates a new <see cref="MappingCharFilterFactory"/> </summary>
        public MappingCharFilterFactory(IDictionary<string, string> args) : base(args)
        {
            mapping = Get(args, "mapping");
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        // TODO: this should use inputstreams from the loader, not File!
        public virtual void Inform(IResourceLoader loader)
        {
            if (mapping != null)
            {
                IList<string> wlist = null;
                if (File.Exists(mapping))
                {
                    wlist = new List<string>(GetLines(loader, mapping));
                }
                else
                {
                    var files = SplitFileNames(mapping);
                    wlist = new List<string>();
                    foreach (string file in files)
                    {
                        var lines = GetLines(loader, file.Trim());
                        wlist.AddRange(lines);
                    }
                }
                NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
                ParseRules(wlist, builder);
                m_normMap = builder.Build();
                if (m_normMap.map == null)
                {
                    // if the inner FST is null, it means it accepts nothing (e.g. the file is empty)
                    // so just set the whole map to null
                    m_normMap = null;
                }
            }
        }

        public override TextReader Create(TextReader input)
        {
            // if the map is null, it means there's actually no mappings... just return the original stream
            // as there is nothing to do here.
            return m_normMap == null ? input : new MappingCharFilter(m_normMap, input);
        }

        // "source" => "target"
        private static Regex p = new Regex(@"\""(.*)\""\s*=>\s*\""(.*)\""\s*$", RegexOptions.Compiled);

        protected virtual void ParseRules(IList<string> rules, NormalizeCharMap.Builder builder)
        {
            foreach (string rule in rules)
            {
                Match m = p.Match(rule);
                if (!m.Success)
                {
                    throw new System.ArgumentException("Invalid Mapping Rule : [" + rule + "], file = " + mapping);
                }
                builder.Add(ParseString(m.Groups[1].Value), ParseString(m.Groups[2].Value));
            }
        }

        private char[] @out = new char[256];

        protected internal virtual string ParseString(string s)
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
                        throw new System.ArgumentException("Invalid escaped char in [" + s + "]");
                    }
                    c = s[readPos++];
                    switch (c)
                    {
                        case '\\':
                            c = '\\';
                            break;
                        case '"':
                            c = '"';
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
                                throw new System.ArgumentException("Invalid escaped char in [" + s + "]");
                            }
                            //c = (char)int.Parse(s.Substring(readPos, 4), 16);
                            c = (char)int.Parse(s.Substring(readPos, 4), System.Globalization.NumberStyles.HexNumber);
                            readPos += 4;
                            break;
                    }
                }
                @out[writePos++] = c;
            }
            return new string(@out, 0, writePos);
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }
    }
}