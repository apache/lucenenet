// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;
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
    /// Parser for the Solr synonyms format.
    /// <list type="bullet">
    ///     <item><description> Blank lines and lines starting with '#' are comments.</description></item>
    ///     <item><description> Explicit mappings match any token sequence on the LHS of "=>"
    ///         and replace with all alternatives on the RHS.  These types of mappings
    ///         ignore the expand parameter in the constructor.
    ///         Example:
    ///         <code>i-pod, i pod => ipod</code>
    ///     </description></item>
    ///     <item><description> Equivalent synonyms may be separated with commas and give
    ///         no explicit mapping.  In this case the mapping behavior will
    ///         be taken from the expand parameter in the constructor.  This allows
    ///         the same synonym file to be used in different synonym handling strategies.
    ///         Example:
    ///         <code>ipod, i-pod, i pod</code>
    ///     </description></item>
    ///     <item><description> Multiple synonym mapping entries are merged.
    ///         Example:
    ///         <code>
    ///             foo => foo bar
    ///             foo => baz
    ///             is equivalent to
    ///             foo => foo bar, baz
    ///         </code>
    ///     </description></item>
    /// </list>
    /// @lucene.experimental
    /// </summary>
    public class SolrSynonymParser : SynonymMap.Parser
    {
        private readonly bool expand;

        public SolrSynonymParser(bool dedup, bool expand, Analyzer analyzer) 
            : base(dedup, analyzer)
        {
            this.expand = expand;
        }

        public override void Parse(TextReader @in)
        {
            int lineNumber = 0;
            try
            {
                string line = null;
                while ((line = @in.ReadLine()) != null)
                {
                    lineNumber++;
                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue; // ignore empty lines and comments
                    }

                    CharsRef[] inputs;
                    CharsRef[] outputs;

                    // TODO: we could process this more efficiently.
                    string[] sides = Split(line, "=>");
                    if (sides.Length > 1) // explicit mapping
                    {
                        if (sides.Length != 2)
                        {
                            throw new ArgumentException("more than one explicit mapping specified on the same line");
                        }
                        string[] inputStrings = Split(sides[0], ",");
                        inputs = new CharsRef[inputStrings.Length];
                        for (int i = 0; i < inputs.Length; i++)
                        {
                            inputs[i] = Analyze(Unescape(inputStrings[i]).Trim(), new CharsRef());
                        }

                        string[] outputStrings = Split(sides[1], ",");
                        outputs = new CharsRef[outputStrings.Length];
                        for (int i = 0; i < outputs.Length; i++)
                        {
                            outputs[i] = Analyze(Unescape(outputStrings[i]).Trim(), new CharsRef());
                        }
                    }
                    else
                    {
                        string[] inputStrings = Split(line, ",");
                        inputs = new CharsRef[inputStrings.Length];
                        for (int i = 0; i < inputs.Length; i++)
                        {
                            inputs[i] = Analyze(Unescape(inputStrings[i]).Trim(), new CharsRef());
                        }
                        if (expand)
                        {
                            outputs = inputs;
                        }
                        else
                        {
                            outputs = new CharsRef[1];
                            outputs[0] = inputs[0];
                        }
                    }

                    // currently we include the term itself in the map,
                    // and use includeOrig = false always.
                    // this is how the existing filter does it, but its actually a bug,
                    // especially if combined with ignoreCase = true
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        for (int j = 0; j < outputs.Length; j++)
                        {
                            Add(inputs[i], outputs[j], false);
                        }
                    }
                }
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                throw new ParseException("Invalid synonym rule at line " + lineNumber, 0, e);
                //ex.initCause(e);
                //throw ex;
            }
            finally
            {
                @in.Dispose();
            }
        }

        private static string[] Split(string s, string separator)
        {
            JCG.List<string> list = new JCG.List<string>(2);
            StringBuilder sb = new StringBuilder();
            int pos = 0, end = s.Length;
            while (pos < end)
            {
                //if (s.StartsWith(separator, pos))
                if (s.Substring(pos).StartsWith(separator, StringComparison.Ordinal))
                {
                    if (sb.Length > 0)
                    {
                        list.Add(sb.ToString());
                        sb = new StringBuilder();
                    }
                    pos += separator.Length;
                    continue;
                }

                char ch = s[pos++];
                if (ch == '\\')
                {
                    sb.Append(ch);
                    if (pos >= end) // ERROR, or let it go?
                    {
                        break;
                    }
                    ch = s[pos++];
                }

                sb.Append(ch);
            }

            if (sb.Length > 0)
            {
                list.Add(sb.ToString());
            }

            return list.ToArray();
        }

        private static string Unescape(string s) // LUCENENET: CA1822: Mark members as static
        {
            if (s.IndexOf("\\", StringComparison.Ordinal) >= 0)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < s.Length; i++)
                {
                    char ch = s[i];
                    if (ch == '\\' && i < s.Length - 1)
                    {
                        sb.Append(s[++i]);
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                return sb.ToString();
            }
            return s;
        }
    }
}