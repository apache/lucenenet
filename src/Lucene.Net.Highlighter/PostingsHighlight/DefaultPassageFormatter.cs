using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.PostingsHighlight
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
    /// Creates a formatted snippet from the top passages.
    /// <para/>
    /// The default implementation marks the query terms as bold, and places
    /// ellipses between unconnected passages.
    /// </summary>
    public class DefaultPassageFormatter : PassageFormatter
    {
        /** text that will appear before highlighted terms */
        protected readonly string preTag;
        /** text that will appear after highlighted terms */
        protected readonly string postTag;
        /** text that will appear between two unconnected passages */
        protected readonly string ellipsis;
        /** true if we should escape for html */
        protected readonly bool escape;

        /**
         * Creates a new DefaultPassageFormatter with the default tags.
         */
        public DefaultPassageFormatter()
                : this("<b>", "</b>", "... ", false)
        {

        }

        /**
         * Creates a new DefaultPassageFormatter with custom tags.
         * @param preTag text which should appear before a highlighted term.
         * @param postTag text which should appear after a highlighted term.
         * @param ellipsis text which should be used to connect two unconnected passages.
         * @param escape true if text should be html-escaped
         */
        public DefaultPassageFormatter(String preTag, String postTag, String ellipsis, bool escape)
        {
            if (preTag == null || postTag == null || ellipsis == null)
            {
                throw new ArgumentException(); //throw new NullPointerException();
            }
            this.preTag = preTag;
            this.postTag = postTag;
            this.ellipsis = ellipsis;
            this.escape = escape;
        }


        public override object Format(Passage[] passages, string content)
        {
            StringBuilder sb = new StringBuilder();
            int pos = 0;
            foreach (Passage passage in passages)
            {
                // don't add ellipsis if its the first one, or if its connected.
                if (passage.startOffset > pos && pos > 0)
                {
                    sb.Append(ellipsis);
                }
                pos = passage.startOffset;
                for (int i = 0; i < passage.numMatches; i++)
                {
                    int start = passage.matchStarts[i];
                    int end = passage.matchEnds[i];
                    // its possible to have overlapping terms
                    if (start > pos)
                    {
                        append(sb, content, pos, start);
                    }
                    if (end > pos)
                    {
                        sb.Append(preTag);
                        append(sb, content, Math.Max(pos, start), end);
                        sb.Append(postTag);
                        pos = end;
                    }
                }
                // its possible a "term" from the analyzer could span a sentence boundary.
                append(sb, content, pos, Math.Max(pos, passage.endOffset));
                pos = passage.endOffset;
            }
            return sb.ToString();
        }

        /** 
         * Appends original text to the response.
         * @param dest resulting text, possibly transformed or encoded
         * @param content original text content
         * @param start index of the first character in content
         * @param end index of the character following the last character in content
         */
        protected void append(StringBuilder dest, String content, int start, int end)
        {
            if (escape)
            {
                // note: these are the rules from owasp.org
                for (int i = start; i < end; i++)
                {
                    char ch = content[i];
                    switch (ch)
                    {
                        case '&':
                            dest.Append("&amp;");
                            break;
                        case '<':
                            dest.Append("&lt;");
                            break;
                        case '>':
                            dest.Append("&gt;");
                            break;
                        case '"':
                            dest.Append("&quot;");
                            break;
                        case '\'':
                            dest.Append("&#x27;");
                            break;
                        case '/':
                            dest.Append("&#x2F;");
                            break;
                        default:
                            if (ch >= 0x30 && ch <= 0x39 || ch >= 0x41 && ch <= 0x5A || ch >= 0x61 && ch <= 0x7A)
                            {
                                dest.Append(ch);
                            }
                            else if (ch < 0xff)
                            {
                                dest.Append("&#");
                                dest.Append((int)ch);
                                dest.Append(";");
                            }
                            else
                            {
                                dest.Append(ch);
                            }
                            break;
                    }
                }
            }
            else
            {
                dest.Append(content, start, end - start);
            }
        }
    }
}
