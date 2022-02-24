using System.Text.RegularExpressions;
using Microsoft.DocAsCode.MarkdownLite;

namespace LuceneDocsPlugins
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
    /// The regex rule to parse out the custom Lucene tokens
    /// </summary>
    public class LuceneNoteBlockRule : IMarkdownRule
    {
        // TODO: I think there's an issue with this regex and multi-line or something, for example see: DrillDownQuery class
        // since this isn't matching it's experimental tag (there's lots of others too)
        public virtual Regex LabelRegex { get; } = new Regex("^@lucene\\.(?<notetype>(experimental|internal))", RegexOptions.Compiled);

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {

            var match = LabelRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new LuceneNoteBlockToken(this, parser.Context, match.Groups[1].Value, sourceInfo);
        }

        public virtual string Name => "LuceneNote";
    }
}