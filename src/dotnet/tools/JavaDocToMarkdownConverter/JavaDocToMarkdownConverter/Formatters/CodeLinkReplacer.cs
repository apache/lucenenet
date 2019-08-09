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
using System.Globalization;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{

    //TODO: This could instead be done with the LuceneDocsPlugins and our custom markdown parsing

    public class CodeLinkReplacer : IReplacer
    {
        private static readonly Regex LinkRegex = new Regex(@"{@link\s*?(?<cref>org\.apache\.lucene\.[\w\.]*)\s?(?<text>[^}]*)}", RegexOptions.Compiled);
        private static readonly Regex JavaCodeExtension = new Regex(@".java$", RegexOptions.Compiled);

        public string Replace(string html)
        {
            return ReplaceCodeLinks(html);
        }

        private string ReplaceCodeLinks(string markdown)
        {
            Match link = LinkRegex.Match(markdown);
            if (link.Success)
            {
                do
                {
                    string cref = link.Groups["cref"].Value.CorrectCRef();
                    string newLink;

                    //see https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html?tabs=tabid-1%2Ctabid-a#cross-reference 
                    //for xref syntax support

                    var text = link.Groups["text"].Value;
                    
                    if (HasLinkText(text, cref, out var methodName, out var methodLink))
                    {
                        if (string.IsNullOrWhiteSpace(methodName))
                        {
                            //string linkText = link.Groups[2].Value;
                            var linkText = JavaCodeExtension.Replace(text, ".cs");
                            newLink = "[" + linkText + "](xref:" + cref + ")";
                        }
                        else
                        {
                            newLink = "[" + methodName + "](xref:" + cref + "#" + methodLink + ")";
                        }                        
                    }
                    else
                    {
                        newLink = "<xref:" + cref + ">";
                    }

                    markdown = LinkRegex.Replace(markdown, newLink, 1);


                } while ((link = LinkRegex.Match(markdown)).Success);
            }

            return markdown;
        }

        private bool HasLinkText(string text, string cref, out string methodName, out string link)
        {
            methodName = null;
            link = null;
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (text.Contains("#"))
                {
                    var lastSpace = text.LastIndexOf(' ');
                    if (lastSpace >= 0)
                    {
                        methodName = text.Substring(lastSpace + 1);
                        var lastBracket = methodName.LastIndexOf('(');
                        if (lastBracket >= 0)
                            methodName = methodName.Substring(0, lastBracket);
                        if (char.IsLower(methodName[0]))
                            methodName = char.ToUpper(methodName[0]) + methodName.Substring(1);

                        link = text.Substring(1, lastSpace - 1).CorrectCRef();
                        if (char.IsLower(link[0]))
                            link = char.ToUpper(link[0]) + link.Substring(1);

                        //the method link needs to be in a full namespace format but delimited by _
                        //HOWEVER, there's no way we can make this work because the lucene parameters are simple like `iterator` but 
                        //the docfx method links require fully qualified types like System_Collections_Generic_IEnumerable_Lucene_Net_Index_IIndexableField
                        //and we don't have that information to extract. The best we can do is just deep link to the #methods of the class.
                        //link = $"{string.Join("_", cref.Split('.'))}_{methodName}";

                        link = "methods";
                    }
                }
                return true;
            }
            return false;
        }
    }
}
