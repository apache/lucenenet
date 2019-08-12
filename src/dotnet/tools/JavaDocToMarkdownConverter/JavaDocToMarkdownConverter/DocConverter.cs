//using JavaDocToMarkdownConverter.MarkdownParsers;
using JavaDocToMarkdownConverter.Formatters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavaDocToMarkdownConverter
{

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

    public class DocConverter
    {   

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

        public void ConvertDoc(string inputDoc, string rootOutputDirectory)
        {
            var outputDir = GetOutputDirectory(inputDoc, rootOutputDirectory);
            var outputFile = Path.Combine(outputDir, GetOuputFilename(inputDoc));

            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine("Output Directory Doesn't Exist: '" + outputDir + "'");
                return;
            }
            if (!File.Exists(inputDoc))
            {
                Console.WriteLine("Input File Doesn't Exist: '" + inputDoc + "'");
                return;
            }

            var converter = new Html2Markdown.Converter();
            var markdown = converter.ConvertFile(inputDoc);

            var ns = ExtractNamespaceFromFile(outputFile);
            if (NamespaceFileMappings.TryGetValue(ns, out var realNs))
                ns = realNs;

            foreach (var r in JavaDocFormatters.Replacers)
            {
                markdown = r.Replace(markdown);
            }
            if (JavaDocFormatters.CustomReplacers.TryGetValue(ns, out var cr))
            {
                markdown = cr.Replace(markdown);
            }

            var appendYamlHeader = ShouldAppendYamlHeader(inputDoc, ns);

            var fileContent = appendYamlHeader ? AppendYamlHeader(ns, markdown) : markdown;

            File.WriteAllText(outputFile, fileContent, Encoding.UTF8);
        }

        /// <summary>
        /// Normally yaml headers are applied to "overview" files but in some cases it's the equivalent "package" file that 
        /// contains the documentation we want
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        private bool ShouldAppendYamlHeader(string inputFile, string ns)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputFile); //should be either "overview" or "package"
            if (string.Equals("overview", fileName, StringComparison.InvariantCultureIgnoreCase))
            {
                if (YamlHeadersForPackageFiles.Contains(ns, StringComparer.InvariantCultureIgnoreCase)) 
                    return false; //don't append yaml header for this overview file, it will be put on the package file

                return true; //the default for 'overview' files
            }
            else if (string.Equals("package", fileName, StringComparison.InvariantCultureIgnoreCase))
            {
                if (YamlHeadersForPackageFiles.Contains(ns, StringComparer.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// For these namespaces we'll use the package.md file instead of the overview.md as the doc file
        /// </summary>
        private static readonly List<string> YamlHeadersForPackageFiles = new List<string>
            {
                "Lucene.Net.Analysis.SmartCn",
                "Lucene.Net.Facet",
                "Lucene.Net.Grouping",
                "Lucene.Net.Join",
                "Lucene.Net.Index.Memory",
                "Lucene.Net.Replicator",
                "Lucene.Net.QueryParsers.Classic",
                "Lucene.Net.Codecs",
                "Lucene.Net.Analysis",
                "Lucene.Net.Codecs.Compressing",
                "Lucene.Net.Codecs.Lucene3x",
                "Lucene.Net.Codecs.Lucene40",
                "Lucene.Net.Codecs.Lucene41",
                "Lucene.Net.Codecs.Lucene42",
                "Lucene.Net.Codecs.Lucene45",
                "Lucene.Net.Codecs.Lucene46",
                "Lucene.Net.Documents",
                "Lucene.Net.Index",
                "Lucene.Net.Search",
                "Lucene.Net.Search.Payloads",
                "Lucene.Net.Search.Similarities",
                "Lucene.Net.Search.Spans",
                "Lucene.Net.Store",
                "Lucene.Net.Util",
            };

        /// <summary>
        /// Maps the file based namespace folders to the actual namespaces
        /// </summary>
        private static readonly Dictionary<string, string> NamespaceFileMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        { 
            ["Lucene.Net.Memory"] = "Lucene.Net.Index.Memory",
            ["Lucene.Net.QueryParser.Classic"] = "Lucene.Net.QueryParsers.Classic",
            ["Lucene.Net.Document"] = "Lucene.Net.Documents",
            ["Lucene.Net.Analysis.Kuromoji"] = "Lucene.Net.Analysis.Ja",
            ["Lucene.Net.Analysis.SmartCn"] = "Lucene.Net.Analysis.Cn.Smart",
        };

        private string AppendYamlHeader(string ns, string fileContent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.Append("uid: ");
            if (NamespaceFileMappings.TryGetValue(ns, out var realNs))
                sb.AppendLine(realNs);
            else
                sb.AppendLine(ns);            
            sb.AppendLine("summary: *content");
            sb.AppendLine("---");
            sb.AppendLine();

            return sb + fileContent;
        }

        /// <summary>
        /// Normally the files would be in the same folder name as their namespace but this isn't the case so we need to try to figure it out
        /// </summary>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        private string ExtractNamespaceFromFile(string outputFile)
        {
            var folder = Path.GetDirectoryName(outputFile);
            var folderParts = folder.Split(Path.DirectorySeparatorChar);

            var index = folderParts.Length - 1;
            for(int i = index; i >= 0;i--)
            {
                if (folderParts[i].StartsWith("Lucene.Net", StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            var nsParts = new List<string>();
            for(var i = index;i< folderParts.Length;i++)
            {
                var innerParts = folderParts[i].Split('.');
                foreach(var innerPart in innerParts)
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
