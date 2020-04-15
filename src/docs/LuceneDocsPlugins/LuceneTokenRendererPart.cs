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

using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;

namespace LuceneDocsPlugins
{
    /// <summary>
    /// Used to replace custom Lucene tokens with custom HTML markup
    /// </summary>
    public sealed class LuceneTokenRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, LuceneNoteBlockToken, MarkdownBlockContext>
    {
        private const string Message = "This is a Lucene.NET {0} API, use at your own risk";

        public override string Name => "LuceneTokenRendererPart";

        public override bool Match(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) => true;

        public override StringBuffer Render(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) 
            => "<div class=\"lucene-block lucene-" + token.Label.ToLower() + "\">" + string.Format(Message, token.Label.ToUpper()) + "</div>";
    }
}