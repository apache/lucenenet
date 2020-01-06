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
    /// For some reason the normal markdown converter doesn't get a few html elements so this removes those
    /// </summary>
    public class ExtraHtmlElementReplacer : IReplacer
    {
        private static readonly Regex MetaTag = new Regex(@"<meta\s+.*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TitleTag = new Regex(@"<title>.*?</title>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex HeadTag = new Regex(@"<head>.*?</head>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex BodyStart = new Regex(@"<body>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BodyEnd = new Regex(@"</body>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HtmlStart = new Regex(@"<html>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HtmlEnd = new Regex(@"</html>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Replace(string html)
        {
            foreach(var r in new[] { MetaTag, TitleTag, HeadTag,BodyStart, BodyEnd, HtmlStart, HtmlEnd })
            {
                html = r.Replace(html, string.Empty);
            }

            return html;
            

            //var htmlDoc = new HtmlDocument();
            //using (var input = new StringReader(html))
            //{
            //    htmlDoc.Load(input);
            //}

            //foreach(var e in HtmlElements)
            //{
            //    RemoveElements(htmlDoc, e);
            //}

            //var sb = new StringBuilder();
            //using (var output = new StringWriter(sb))
            //{
            //    htmlDoc.Save(output);
            //}
            //return sb.ToString();
        }

        //private void RemoveElements(HtmlDocument htmlDoc, string elementMatch)
        //{
        //    var metaTags = htmlDoc.DocumentNode.SelectNodes(elementMatch);
        //    if (metaTags == null) return;
        //    foreach (var m in metaTags)
        //    {
        //        m.ParentNode.RemoveChild(m);
        //    }
        //}
    }
}
