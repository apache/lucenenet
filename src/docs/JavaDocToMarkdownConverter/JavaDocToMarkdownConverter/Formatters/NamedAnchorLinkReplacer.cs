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
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{
    /// <summary>
    /// Replaces named anchors like [About this Document](#About_this_Document) to [About this Document](#about-this-document)
    /// </summary>
    public class NamedAnchorLinkReplacer : IReplacer
    {
        private static readonly Regex NamedAnchorLink = new Regex(@"\[([\w\s]+)\]\(#([\w_]+)\)", RegexOptions.Compiled);


        public string Replace(string html)
        {
            html = NamedAnchorLink.Replace(html, m => $"[{m.Groups[1].Value}](#{m.Groups[2].Value.ToLowerInvariant().Replace("_", "-")})");
            return html;
        }
    }
}
