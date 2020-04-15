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

namespace JavaDocToMarkdownConverter.Formatters
{
    public class AllWhitespacePrefixReplacer : IReplacer
    {
        
        public string Replace(string html)
        {
            var sb = new StringBuilder();

            var txt = html;

            //replace with linux line breaks
            txt = txt.Replace("\r\n", "\n");

            var trim = true;
            var inComment = false;

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
                        if (trimmed.StartsWith("<!--"))
                        {
                            inComment = true;
                            sb.AppendLine(line);
                            continue;
                        }
                            
                        if (trimmed.StartsWith("-->"))
                        {
                            inComment = false;
                            sb.AppendLine(line);
                            continue;
                        }   

                        if (inComment)
                        {
                            sb.AppendLine(line);
                            continue;
                        };

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
        }
    }
}
