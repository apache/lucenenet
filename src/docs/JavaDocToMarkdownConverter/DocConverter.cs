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

using Html2Markdown;
using JavaDocToMarkdownConverter.Formatters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter
{

    public class DocConverter
    {

        public DocConverter()
        {
            _converter = new Converter(new CustomMarkdownScheme());
        }

        private readonly Converter _converter;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputDirectory">The /lucene directory in the Java source code.</param>
        /// <param name="rootOutputDirectory">The root directory of the Lucene.Net repository.</param>
        public void Convert(string inputDirectory, string rootOutputDirectory)
        {
            var dir = new DirectoryInfo(inputDirectory);
            if (!dir.Exists)
            {
                Console.WriteLine("Directory Doesn't Exist: '" + dir.FullName + "'");
                return;
            }

            foreach (var file in dir.EnumerateFiles("overview.html", SearchOption.AllDirectories))
            {
                ConvertDoc(file.FullName, rootOutputDirectory);
            }
            foreach (var file in dir.EnumerateFiles("package.html", SearchOption.AllDirectories))
            {
                ConvertDoc(file.FullName, rootOutputDirectory);
            }
        }

        private void ConvertDoc(string inputDoc, string rootOutputDirectory)
        {
            var outputDir = GetOutputDirectory(inputDoc, rootOutputDirectory);
            var outputFile = Path.Combine(outputDir, GetOuputFilename(inputDoc));
            var inputFileInfo = new FileInfo(inputDoc);

            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine("Output Directory Doesn't Exist: '" + outputDir + "'");
                return;
            }
            if (!inputFileInfo.Exists)
            {
                Console.WriteLine("Input File Doesn't Exist: '" + inputDoc + "'");
                return;
            }

            var markdown = _converter.ConvertFile(inputDoc);

            var ns = ExtractNamespaceFromFile(outputFile);

            // we might need to convert this namespace to an explicit value
            if (PackageNamespaceToStandalone.TryGetValue(ns, out var standaloneNs))
                ns = standaloneNs;

            if (JavaDocFormatters.CustomReplacers.TryGetValue(ns, out var replacers))
            {
                foreach (var r in replacers)
                    markdown = r.Replace(markdown);
            }

            var doc = new ConvertedDocument(inputFileInfo, new FileInfo(outputFile), ns, markdown);
            if (JavaDocFormatters.CustomProcessors.TryGetValue(ns, out var processor))
            {
                processor(doc);
            }
            markdown = doc.Markdown; // it may have changed

            var fileContent = AppendYamlHeader(ns, markdown);

            File.WriteAllText(outputFile, fileContent, Encoding.UTF8);
        }

        /// <summary>
        /// Explicit mappings of namespaced package files to standalone files
        /// </summary>
        /// <remarks>
        /// This is really edge case stuff
        /// </remarks>
        private static readonly Dictionary<string, string> PackageNamespaceToStandalone = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["Lucene.Net.Search.Grouping"] = "Lucene.Net.Grouping"
        };

        /// <summary>
        /// These aren't real namespaces but they have overview.md files and in this case we need to prepend an H1 header
        /// to the overview.md file.
        /// </summary>
        private static readonly List<string> StandaloneOverviews = new List<string>
            {
                "Lucene.Net",
                "Lucene.Net.Analysis.Common",
                "Lucene.Net.Analysis.Morfologik",
                "Lucene.Net.Analysis.OpenNLP",
                "Lucene.Net.Highlighter",
                "Lucene.Net.Grouping",
            };

        /// <summary>
        /// Appends the YAML front-matter header
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="fileContent"></param>
        /// <param name="rawTitle">
        /// If specified will add the "title" front-matter, this is used for overview.md files that are
        /// standalone (not part of a real namespace) (aka conceptual files in docfx)
        /// </param>
        /// <returns></returns>
        private string AppendYamlHeader(string ns, string fileContent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.Append("uid: ");
            sb.AppendLine(ns);

            // Add "title" yaml front-matter if a standalone file
            if (StandaloneOverviews.Contains(ns))
            {
                sb.Append("title: ");
                sb.AppendLine(ns);
            }

            sb.AppendLine("summary: *content");
            sb.AppendLine("---");
            sb.AppendLine();

            return sb + fileContent;
        }

        private static Regex CSharpNamespaceMatch = new Regex(@"^\s*namespace\s*([\w\.]+)", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Normally the files would be in the same folder name as their namespace but this isn't the case so we need to try to figure it out
        /// </summary>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        private string ExtractNamespaceFromFile(string outputFile)
        {
            var folder = Path.GetDirectoryName(outputFile);

            // First check if there are c# files beside this file
            var csharpFiles = Directory.GetFiles(folder, "*.cs");
            if (csharpFiles.Length > 0)
            {
                // extract the namespace from a file
                var csharpFile = File.ReadAllText(csharpFiles[0]);
                var nsMatches = CSharpNamespaceMatch.Matches(csharpFile);
                if (nsMatches.Count > 0)
                {
                    if (nsMatches[0].Groups.Count == 2)
                        return nsMatches[0].Groups[1].Value;
                }
            }

            // Else we'll fall back to trying to determine namespace by folder

            var folderParts = folder.Split(Path.DirectorySeparatorChar);

            var index = folderParts.Length - 1;
            for (int i = index; i >= 0; i--)
            {
                if (folderParts[i].StartsWith("Lucene.Net", StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            var nsParts = new List<string>();
            for (var i = index; i < folderParts.Length; i++)
            {
                var innerParts = folderParts[i].Split('.');
                foreach (var innerPart in innerParts)
                {
                    nsParts.Add(innerPart);
                }
            }

            var textInfo = new CultureInfo("en-US", false).TextInfo;
            return string.Join(".", nsParts.Select(x => textInfo.ToTitleCase(x)).ToArray());
        }


        private string GetOuputFilename(string inputDoc)
        {
            return Path.GetFileNameWithoutExtension(inputDoc) + ".md";
        }

        private string GetOutputDirectory(string inputDoc, string rootOutputDirectory)
        {
            string project = Path.Combine(rootOutputDirectory, @"src\Lucene.Net");
            var file = new FileInfo(inputDoc);
            var dir = file.Directory.FullName;
            var segments = dir.Split(Path.DirectorySeparatorChar);
            int i;
            bool inLucene = false;
            string lastSegment = string.Empty;
            for (i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (segment.Equals("lucene"))
                {
                    inLucene = true;
                    continue;
                }
                if (!inLucene)
                    continue;
                if (segment.Equals("core"))
                    break;
                project += "." + segment;
                lastSegment = segment;

                if (segment.Equals("analysis"))
                    continue;
                break;
            }

            //if (project.EndsWith("analysis.icu", StringComparison.OrdinalIgnoreCase))
            //{
            //    project = project.Replace("Lucene.Net.analysis.icu", @"dotnet\Lucene.Net.ICU");
            //}

            if (project.EndsWith("test-framework", StringComparison.OrdinalIgnoreCase))
            {
                project = project.Replace("test-framework", "TestFramework");
            }

            // Now we have the project directory and segment that it equates to.
            // We need to walk up the tree and ignore the java-ish deep directories.
            var ignore = new List<string>() { "src", "java", "org", "apache", "lucene" };
            string path = project;

            for (int j = i + 1; j < segments.Length; j++)
            {
                var segment = segments[j];
                if (ignore.Contains(segment))
                {
                    continue;
                }

                // Special Cases
                switch (lastSegment.ToLower())
                {
                    case "morfologik":
                        if (segment.Equals("analysis")) continue;
                        if (segment.Equals("morfologik")) continue;
                        break;
                    case "stempel":
                        if (segment.Equals("analysis")) continue;
                        if (segment.Equals("egothor")) segment = "Egothor.Stemmer";
                        if (segment.Equals("stemmer")) continue;
                        break;
                    case "kuromoji":
                        if (segment.Equals("analysis") || segment.Equals("ja")) continue;
                        break;
                    case "phonetic":
                        if (segment.Equals("analysis") || segment.Equals("phonetic")) continue;
                        break;
                    case "smartcn":
                        if (segment.Equals("analysis") || segment.Equals("cn") || segment.Equals("smart")) continue;
                        break;
                    case "benchmark":
                        if (segment.Equals("benchmark")) continue;
                        break;
                    case "classification":
                        if (segment.Equals("classification")) continue;
                        break;
                    case "codecs":
                        if (segment.Equals("codecs")) continue;
                        break;
                    case "demo":
                        if (segment.Equals("demo")) continue;
                        break;
                    case "expressions":
                        if (segment.Equals("expressions")) continue;
                        break;
                    case "facet":
                        if (segment.Equals("facet")) continue;
                        break;
                    case "grouping":
                        if (segment.Equals("search") || segment.Equals("grouping")) continue;
                        break;
                    case "highlighter":
                        if (segment.Equals("search")) continue;
                        break;
                    case "join":
                        if (segment.Equals("search") || segment.Equals("join")) continue;
                        break;
                    case "memory":
                        if (segment.Equals("index") || segment.Equals("memory")) continue;
                        break;
                    case "queries":
                        if (segment.Equals("queries")) continue;
                        if (segment.Equals("valuesource")) segment = "ValueSources";
                        break;
                    case "queryparser":
                        if (segment.Equals("queryparser")) continue;
                        break;
                    case "replicator":
                        if (segment.Equals("replicator")) continue;
                        break;
                    case "sandbox":
                        if (segment.Equals("sandbox")) continue;
                        break;
                    case "spatial":
                        if (segment.Equals("spatial")) continue;
                        break;
                    case "suggest":
                        if (segment.Equals("search")) continue;
                        break;
                }

                path = Path.Combine(path, segment);
            }

            return path;
        }
    }
}
