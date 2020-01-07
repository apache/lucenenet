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

using Html2Markdown.Replacement;
using Html2Markdown.Scheme;
using JavaDocToMarkdownConverter.Formatters;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter
{    
    /// <summary>
    /// Custom markdown conversion scheme
    /// </summary>
    /// <remarks>
    /// This uses the original replacers but substitues our own for a few things and adds a lot of custom replacers.
    /// </remarks>
    public class CustomMarkdownScheme : IScheme
    {
        public CustomMarkdownScheme()
        {
            _replacers = new Markdown().Replacers(); //originals
            _replacers.RemoveAt(0); //this is the 'strong' one
            _replacers.RemoveAt(0); //this is the 'em' one
            _replacers.Insert(0, new PatternReplacer(new Regex("</?(strong|b)>"), "__")); //re-insert with underscore syntax
            _replacers.Insert(0, new PatternReplacer(new Regex("</?(em|i)>"), "_")); //re-insert with underscore syntax

            //TODO: Find out why lists aren't parsed nicely, see https://github.com/baynezy/Html2Markdown/blob/690e1f6b9cbcba2333acfef68c05795d698040ad/src/Html2Markdown/Replacement/HtmlParser.cs
            // http://localhost:8080/api/Lucene.Net/Lucene.Net.Search.Spans.html
            // source: C:\Users\Shannon\Documents\_Projects\Lucene.Net\lucene-solr-releases-lucene-solr-4.8.0\lucene\core\src\java\org\apache\lucene\search\spans\package.html

            
            _replacers.Add(new WhitespacePrefixReplacer("div"));

            _replacers.Add(new CodeLinkReplacer());
            _replacers.Add(new RepoLinkReplacer());
            _replacers.Add(new DocTypeReplacer());
            _replacers.Add(new ExtraHtmlElementReplacer());
            _replacers.Add(new NamedAnchorLinkReplacer());
            _replacers.Add(new DivWrapperReplacer());
        }

        private readonly IList<IReplacer> _replacers;

        public IList<IReplacer> Replacers() => _replacers;
    }
}
