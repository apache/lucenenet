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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.Charfilter
{
    public class MappingCharFilterFactory : CharFilterFactory, IResourceLoaderAware, IMultiTermAwareComponent
    {
        protected NormalizeCharMap _normMap;
        private readonly string _mapping;

        public MappingCharFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            _mapping = Get(args, "mapping");
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public void Inform(IResourceLoader loader)
        {
            if (_mapping != null)
            {
                IList<string> wlist = null;
                var mappingFile = new FileInfo(_mapping);
                if (mappingFile.Exists)
                {
                    wlist = GetLines(loader, _mapping);
                }
                else
                {
                    IList<string> files = SplitFileNames(_mapping);
                    wlist = new List<string>();
                    foreach (var file in files)
                    {
                        var lines = GetLines(loader, file.Trim());
                        wlist.AddRange(lines);
                    }
                }
                var builder = new NormalizeCharMap.Builder();
                ParseRules(wlist, builder);
                _normMap = builder.Build();
                if (_normMap.map == null)
                {
                    _normMap = null;
                }
            }
        }

        public override StreamReader Create(StreamReader input)
        {
            return _normMap == null ? input : new MappingCharFilter(_normMap, input);
        }

        protected internal static Regex _p = new Regex("\"(.*)\"\\s*=>\\s*\"(.*)\"\\s*$");

        protected void ParseRules(IList<string> rules, NormalizeCharMap.Builder builder)
        {
            foreach (var rule in rules)
            {
                if (!_p.IsMatch(rule))
                {
                    throw new ArgumentException(string.Format("Invalid Mapping Rule : [{0}], file = {1}", rule, _mapping));
                }
                var match = _p.Match(rule);
                builder.Add(ParseString(match.Groups[1].Value), ParseString(match.Groups[2].Value));
            }
        }

        protected internal char[] _out = new char[256];

        protected string ParseString(string s)
        {
            var readPos = 0;
            var len = s.Length;
            var writePos = 0;
            while (readPos < len)
            {
                var c = s[readPos++];
                if (c == '\\')
                {
                    if (readPos >= len)
                    {
                        throw new ArgumentException(string.Format("Invalid excaped char in [{0}]", s));
                    }
                    c = s[readPos++];
                    switch (c)
                    {
                        case '\\': c = '\\'; break;
                        case '"': c = '"'; break;
                        case 'n': c = '\n'; break;
                        case 't': c = '\t'; break;
                        case 'r': c = '\r'; break;
                        case 'b': c = '\b'; break;
                        case 'f': c = '\f'; break;
                        case 'u':
                            if (readPos + 3 >= len)
                                throw new ArgumentException("Invalid escaped char in [" + s + "]");
                            c = (char)int.Parse(s.Substring(readPos, readPos + 4), NumberStyles.HexNumber);
                            readPos += 4;
                            break;
                    }
                }
                _out[writePos++] = c;
            }
            return new string(_out, 0, writePos);
        }

        public AbstractAnalysisFactory MultiTermComponent
        {
            get { return this; }
        }
    }
}
