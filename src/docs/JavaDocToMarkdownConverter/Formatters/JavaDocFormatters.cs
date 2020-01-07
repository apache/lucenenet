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
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{

    public class JavaDocFormatters
    {
        

        /// <summary>
        /// A list of custom replacers for specific uid files
        /// </summary>
        /// <remarks>The Key is the Namespace</remarks>
        public static IDictionary<string, IReplacer[]> CustomReplacers => new Dictionary<string, IReplacer[]>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["Lucene.Net"] = new IReplacer[]
            {
                new PatternReplacer(new Regex("To demonstrate these, try something like:.*$", RegexOptions.Singleline))
            },
            ["Lucene.Net.Demo"] = new IReplacer[]
            {
                new PatternReplacer(new Regex("(## Setting your CLASSPATH.*?)(##)", RegexOptions.Singleline), "$2"),
                new PatternReplacer(new Regex(@"\*\s+?\[.+?CLASSPATH\].+?$", RegexOptions.Multiline)),
                new PatternReplacer(new Regex(@"\[IndexFiles.*?\]\(.+?\)", RegexOptions.Singleline), "[IndexFiles](xref:Lucene.Net.Demo.IndexFiles)"),
                new PatternReplacer(new Regex(@"\[SearchFiles.*?\]\(.+?\)", RegexOptions.Singleline), "[SearchFiles](xref:Lucene.Net.Demo.SearchFiles)")
            },
        };

        /// <summary>
        /// A list of custom processors for specific uid files
        /// </summary>
        /// <remarks>The Key is the Namespace</remarks>
        public static IDictionary<string, Action<ConvertedDocument>> CustomProcessors => new Dictionary<string, Action<ConvertedDocument>>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["Lucene.Net.Demo"] = DemoCodeFormatter.ProcessDemoFiles,
        };
    }
}
