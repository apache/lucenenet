#if FEATURE_BREAKITERATOR
using System;
using System.Text;

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
        /// <summary>text that will appear before highlighted terms</summary>
        protected readonly string m_preTag;
        /// <summary>text that will appear after highlighted terms</summary>
        protected readonly string m_postTag;
        /// <summary>text that will appear between two unconnected passages</summary>
        protected readonly string m_ellipsis;
        /// <summary>true if we should escape for html</summary>
        protected readonly bool m_escape;

        /// <summary>
        /// Creates a new DefaultPassageFormatter with the default tags.
        /// </summary>
        public DefaultPassageFormatter()
                : this("<b>", "</b>", "... ", false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DefaultPassageFormatter"/> with custom tags.
        /// </summary>
        /// <param name="preTag">text which should appear before a highlighted term.</param>
        /// <param name="postTag">text which should appear after a highlighted term.</param>
        /// <param name="ellipsis">text which should be used to connect two unconnected passages.</param>
        /// <param name="escape">true if text should be html-escaped</param>
        public DefaultPassageFormatter(string preTag, string postTag, string ellipsis, bool escape)
        {
            // LUCENENET specific - changed from NullPointerException to ArgumentNullException (.NET convention)
            this.m_preTag = preTag ?? throw new ArgumentNullException(nameof(preTag));
            this.m_postTag = postTag ?? throw new ArgumentNullException(nameof(postTag));
            this.m_ellipsis = ellipsis ?? throw new ArgumentNullException(nameof(ellipsis));
            this.m_escape = escape;
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
                    sb.Append(m_ellipsis);
                }
                pos = passage.startOffset;
                for (int i = 0; i < passage.numMatches; i++)
                {
                    int start = passage.matchStarts[i];
                    int end = passage.matchEnds[i];
                    // its possible to have overlapping terms
                    if (start > pos)
                    {
                        Append(sb, content, pos, start);
                    }
                    if (end > pos)
                    {
                        sb.Append(m_preTag);
                        Append(sb, content, Math.Max(pos, start), end);
                        sb.Append(m_postTag);
                        pos = end;
                    }
                }
                // its possible a "term" from the analyzer could span a sentence boundary.
                Append(sb, content, pos, Math.Max(pos, passage.endOffset));
                pos = passage.endOffset;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Appends original text to the response.
        /// </summary>
        /// <param name="dest">resulting text, possibly transformed or encoded</param>
        /// <param name="content">original text content</param>
        /// <param name="start">index of the first character in content</param>
        /// <param name="end">index of the character following the last character in content</param>
        protected virtual void Append(StringBuilder dest, string content, int start, int end)
        {
            if (m_escape)
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
                                dest.Append(';');
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
#endif