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

    public class DivWrapperReplacer : IReplacer
    {
        private static readonly Regex TocArea = new Regex(@"<div id=""minitoc-area"">(.*?)</div>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex SectionArea = new Regex(@"<div class=""section"">(.*?)</div>", RegexOptions.Compiled | RegexOptions.Singleline);

        public string Replace(string html)
        {
            foreach(var r in new[] { TocArea , SectionArea })
            {
                html = r.Replace(html, "$1");
            }
            return html;
        }
    }
}
