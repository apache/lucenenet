using Microsoft.DocAsCode.MarkdownLite;
using System;
using System.Text;
using System.Text.RegularExpressions;

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
    /// Replaces an environment variables that are nested within the text, href, or title of a Markdown link.
    /// </summary>
    public class EnvironmentVariableInLinkInlineRule : MarkdownLinkInlineRule
    {
        public override string Name => "EnvVar.Link";

        public static readonly Regex EnvVarRegex = EnvironmentVariableUtil.EnvVarRegexInline;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            // NOTE: This copies the logic from MarkdownLinkInlineRule, but does not match if there are no links to replace.
            // This isn't perfect, but at least it ensures we only match the one [Changes]() link it is intended to match
            // and replaces multiple environment variables if they exist.

            // The when the Consume() method below is called, there doesn't appear to be any way to make the cursor backtrack,
            // and the scan is only performed once. This is why we have to resort to this. Not sure if there is a better way to make
            // nested tokens than this, but it doesn't seem like there is.

            var match = Link.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            if (MarkdownInlineContext.GetIsInLink(parser.Context) && match.Value[0] != '!')
            {
                return null;
            }
            if (IsEscape(match.Groups[1].Value) || IsEscape(match.Groups[2].Value))
            {
                return null;
            }

            var text = match.Groups[1].Value;
            var title = match.Groups[4].Value;
            var href = match.Groups[2].Value;

            var textMatch = EnvVarRegex.Match(text);
            var titleMatch = EnvVarRegex.Match(title);
            var hrefMatch = EnvVarRegex.Match(href);

            // Don't match unless we have a match for our EnvVar pattern in any part of the link
            if (!textMatch.Success && !titleMatch.Success && !hrefMatch.Success)
                return null;

            StringBuilder scratch = null;

            if (textMatch.Success || titleMatch.Success || hrefMatch.Success)
                scratch = new StringBuilder();

            if (textMatch.Success)
                text = EnvironmentVariableUtil.ReplaceEnvironmentVariables(text, textMatch, scratch);
            if (titleMatch.Success)
                title = EnvironmentVariableUtil.ReplaceEnvironmentVariables(title, titleMatch, scratch);
            if (hrefMatch.Success)
                href = EnvironmentVariableUtil.ReplaceEnvironmentVariables(href, hrefMatch, scratch);

            var sourceInfo = context.Consume(match.Length);
            return GenerateToken(parser, href, title, text, match.Value[0] == '!', sourceInfo, MarkdownLinkType.NormalLink, null);
        }

        private bool IsEscape(string text)
        {
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] != '\\')
                {
                    return (text.Length - i) % 2 == 0;
                }
            }
            return text.Length % 2 == 1;
        }
    }
}
