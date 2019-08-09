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

    //TODO: This could instead be done with the LuceneDocsPlugins and our custom markdown parsing
    //TODO: We need to pass in a tag here

    public class RepoLinkReplacer : IReplacer
    {
        private static readonly Regex RepoLinkRegex = new Regex(@"(?<=\()(?<cref>src-html/[^)]*)", RegexOptions.Compiled);

        public string Replace(string html)
        {
            return ReplaceRepoLinks(html);
        }

        //https://github.com/apache/lucenenet/blob/Lucene.Net_4_8_0_beta00004/src/Lucene.Net.Analysis.Common/Analysis/Ar/ArabicAnalyzer.cs
        private string ReplaceRepoLinks(string markdown)
        {
            Match link = RepoLinkRegex.Match(markdown);
            if (link.Success)
            {
                do
                {
                    string cref = link.Groups["cref"].Value.CorrectRepoCRef();
                    cref = "https://github.com/apache/lucenenet/blob/{tag}/src/" + cref;

                    markdown = RepoLinkRegex.Replace(markdown, cref, 1);


                } while ((link = RepoLinkRegex.Match(markdown)).Success);
            }

            return markdown;
        }
    }
}
