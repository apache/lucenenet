using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;
using System.Text;

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
    /// Used to replace custom Lucene tokens with custom HTML markup
    /// </summary>
    public sealed class LuceneNoteTokenRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, LuceneNoteBlockToken, MarkdownBlockContext>
    {
        private const string ExperimentalMessage = "This API is experimental and might change in incompatible ways in the next release.";
        private const string InternalMessage = "This API is for internal purposes only and might change in incompatible ways in the next release.";

        private readonly StringBuilder builder = new StringBuilder();

        public override string Name => "LuceneTokenRendererPart";

        public override bool Match(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) 
            => true;

        public override StringBuffer Render(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context)
        {
            string message = token.Label.ToUpperInvariant() == "INTERNAL" ? InternalMessage : ExperimentalMessage;

            builder.Clear(); // Reuse string builder
            builder.AppendLine("<div class=\"NOTE\">");
            builder.AppendLine("<h5>Note</h5>");
            builder.Append("<p>").Append(message).AppendLine("</p>");
            builder.AppendLine("</div>");
            return builder.ToString();
            //return "<div class=\"lucene-block lucene-" + token.Label.ToLower() + "\">" + string.Format(Message, token.Label.ToUpper()) + "</div>";
        }
    }
}