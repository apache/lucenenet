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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{

    public class DemoCodeFormatter
    {
        public static void ProcessDemoFiles(ConvertedDocument convertedDoc)
        {
            //custom processor for processing the Demo *.cs files so that when the docs build the csharp is visible directly in the docs without requiring
            //to navigate to the GitHub source. This is done by extracting just the code part of the *.cs files and creating docfx override files for those
            //classes.

            var demoCsFolder = convertedDoc.OutputFile.Directory;
            var apiDocsFolder = Path.Combine(demoCsFolder.Parent.Parent.FullName, "websites\\apidocs");
            if (!Directory.Exists(apiDocsFolder))
                throw new InvalidOperationException($"The folder {apiDocsFolder} does not exist");
            var apiSpecFolder = Path.Combine(apiDocsFolder, "apiSpec");
            Directory.CreateDirectory(apiSpecFolder);
            foreach (var folder in demoCsFolder.EnumerateDirectories().Where(x => x.Name != "Properties" && x.Name != "obj" && x.Name != "bin"))
            {
                foreach (var file in folder.EnumerateFiles("*.cs"))
                {
                    var code = CodeExtractor.ExtractCode(file);
                    var ns = CodeExtractor.ExtractNamespace(code);
                    var c = CodeExtractor.ExtractClassName(code);
                    var type = $"{ns}.{c}";
                    //naming is based on a docfx convention - not a strict convention but we might as well be consistent
                    var overrideFileName = $"{ns.Replace(".", "_")}_{c}.md";
                    File.WriteAllText(Path.Combine(apiSpecFolder, overrideFileName), AppendYamlHeader(type, "hello", code));
                }
            }
            
        }

        private static string AppendYamlHeader(string typeName, string summary, string fileContent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.Append("uid: ");
            sb.AppendLine(typeName);
            sb.AppendLine("summary: *content");
            //sb.Append("summary: ");
            //sb.AppendLine(summary);
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(fileContent);
            sb.AppendLine("```");

            return sb.ToString();
        }
    }

    public class JavaDocFormatters
    {
        public static IEnumerable<IReplacer> Replacers => new IReplacer[]
            {
                new CodeLinkReplacer(),
                new RepoLinkReplacer(),
                new DocTypeReplacer(),
                new ExtraHtmlElementReplacer(),
                new NamedAnchorLinkReplacer(),
                new DivWrapperReplacer()
            };

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
            ["Lucene.Net.Demo"] = DemoCodeFormatter.ProcessDemoFiles
        };
    }
}
