/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using Html2Markdown.Replacement;
using System.Text;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{
    /// <summary>
    /// Custom replacer to deal with prefixed whitespace text on each line within an element
    /// </summary>
    /// <remarks>
    /// Custom replacer to deal with odd cases, such as when there is this:
    /// <div>
    ///      Blah blah
    ///      Hello world
    /// </div>
    /// Which will end up being rendered as <pre><code> because it's left indented (even though it shouldn't since a div should be rendered as a div).
    /// So this is a work around.
    /// </remarks>
    public class ElementWhitespacePrefixReplacer : IReplacer
    {
        public ElementWhitespacePrefixReplacer(string elementName)
        {
            elementMatch = new Regex($@"<{elementName}>(.*?)</{elementName}>", RegexOptions.Compiled | RegexOptions.Singleline); ;
        }
        private readonly Regex elementMatch;

        public string Replace(string html)
        {
            var result = elementMatch.Replace(html, match =>
            {
                var sb = new StringBuilder();

                var txt = match.Groups[1].Value;

                //replace with linux line breaks
                txt = txt.Replace("\r\n", "\n");

                var trim = true;

                foreach (var line in txt.Split('\n'))
                {
                    // don't trim if the first char is a special char
                    // '<' = probably start of another html element
                    // '*' or '-' = probably start of a list item
                    // ##. = a number with a dot is probably start of a list item
                    // once we encounter one of these we stop trimming.

                    if (trim)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            switch (trimmed[0])
                            {
                                case '<':
                                case '*':
                                case '-':
                                    trim = false;
                                    break;
                                case var firstChar when char.IsDigit(firstChar) && trimmed.Length > 1 && trimmed[1] == '.':
                                    trim = false;
                                    break;
                            }
                        }
                        sb.AppendLine(trim ? trimmed : line);
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }

                return sb.ToString();

            });

            return result;
        }
    }
}
