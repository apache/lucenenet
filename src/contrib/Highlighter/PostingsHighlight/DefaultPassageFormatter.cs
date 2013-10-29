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
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.PostingsHighlight
{
    public class DefaultPassageFormatter : PassageFormatter
    {
        protected readonly string preTag;
        protected readonly string postTag;
        protected readonly string ellipsis;
        protected readonly bool escape;

        public DefaultPassageFormatter()
            : this (@"<b>", @"</b>", @"... ", false)
        {
        }

        public DefaultPassageFormatter(string preTag, string postTag, string ellipsis, bool escape)
        {
            if (preTag == null || postTag == null || ellipsis == null)
            {
                throw new NullReferenceException();
            }

            this.preTag = preTag;
            this.postTag = postTag;
            this.ellipsis = ellipsis;
            this.escape = escape;
        }

        public override string Format(Passage[] passages, string content)
        {
            StringBuilder sb = new StringBuilder();
            int pos = 0;
            foreach (Passage passage in passages)
            {
                if (passage.StartOffset > pos && pos > 0)
                {
                    sb.Append(ellipsis);
                }

                pos = passage.StartOffset;
                for (int i = 0; i < passage.NumMatches; i++)
                {
                    int start = passage.MatchStarts[i];
                    int end = passage.MatchEnds[i];
                    if (start > pos)
                    {
                        Append(sb, content, pos, start);
                    }

                    if (end > pos)
                    {
                        sb.Append(preTag);
                        Append(sb, content, Math.Max(pos, start), end);
                        sb.Append(postTag);
                        pos = end;
                    }
                }

                Append(sb, content, pos, Math.Max(pos, passage.EndOffset));
                pos = passage.EndOffset;
            }

            return sb.ToString();
        }

        protected virtual void Append(StringBuilder dest, string content, int start, int end)
        {
            if (escape)
            {
                for (int i = start; i < end; i++)
                {
                    char ch = content[i];
                    switch (ch)
                    {
                        case '&':
                            dest.Append(@"&amp;");
                            break;
                        case '<':
                            dest.Append(@"&lt;");
                            break;
                        case '>':
                            dest.Append(@"&gt;");
                            break;
                        case '"':
                            dest.Append(@"&quot;");
                            break;
                        case '\\':
                            dest.Append(@"&#x27;");
                            break;
                        case '/':
                            dest.Append(@"&#x2F;");
                            break;
                        default:
                            if (ch >= 0x30 && ch <= 0x39 || ch >= 0x41 && ch <= 0x5A || ch >= 0x61 && ch <= 0x7A)
                            {
                                dest.Append(ch);
                            }
                            else if (ch < 0xff)
                            {
                                dest.Append(@"&#");
                                dest.Append((int)ch);
                                dest.Append(@";");
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
                dest.Append(content, start, end);
            }
        }
    }
}
