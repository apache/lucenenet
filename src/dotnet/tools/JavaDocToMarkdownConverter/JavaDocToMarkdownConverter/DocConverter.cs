using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavaDocToMarkdownConverter
{
    public class DocConverter
    {
        private static Regex LinkRegex = new Regex(@"{@link\s*?(?<cref>org\.apache\.lucene\.[^}]*)\s?(?<text>[^}]*)}", RegexOptions.Compiled);
        private static Regex RepoLinkRegex = new Regex(@"(?<=\()(?<cref>src-html/[^)]*)", RegexOptions.Compiled);

        private static Regex JavaCodeExtension = new Regex(@".java$", RegexOptions.Compiled);
        private static Regex DocType = new Regex(@"<!doctype[^>]*>", RegexOptions.Compiled);

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

            markdown = ReplaceCodeLinks(markdown);
            markdown = ReplaceRepoLinks(markdown);

            // Remove <doctype>
            markdown = DocType.Replace(markdown, string.Empty);

            File.WriteAllText(outputFile, markdown, Encoding.UTF8);
        }

        private string ReplaceCodeLinks(string markdown)
        {
            Match link = LinkRegex.Match(markdown);
            if (link.Success)
            {
                do
                {
                    string cref = CorrectCRef(link.Groups["cref"].Value);
                    string newLink;
                    if (!string.IsNullOrWhiteSpace(link.Groups["text"].Value))
                    {
                        string linkText = link.Groups[2].Value;
                        linkText = JavaCodeExtension.Replace(linkText, ".cs");
                        //newLink = "<see cref=\"" + cref + "\">" + linkText + "</see>";
                        newLink = "[" + linkText + "](xref:" + cref + ")";
                    }
                    else
                    {
                        //newLink = "<see cref=\"" + cref + "\"/>";
                        newLink = "[](xref:" + cref + ")";
                    }

                    markdown = LinkRegex.Replace(markdown, newLink, 1);


                } while ((link = LinkRegex.Match(markdown)).Success);
            }

            return markdown;
        }

        //https://github.com/apache/lucenenet/blob/Lucene.Net_4_8_0_beta00004/src/Lucene.Net.Analysis.Common/Analysis/Ar/ArabicAnalyzer.cs
        private string ReplaceRepoLinks(string markdown)
        {
            Match link = RepoLinkRegex.Match(markdown);
            if (link.Success)
            {
                do
                {
                    string cref = CorrectRepoCRef(link.Groups["cref"].Value);
                    cref = "https://github.com/apache/lucenenet/blob/{tag}/src/" + cref;

                    markdown = RepoLinkRegex.Replace(markdown, cref, 1);


                } while ((link = RepoLinkRegex.Match(markdown)).Success);
            }

            return markdown;
        }

        private IDictionary<string, string> packageToProjectName = new Dictionary<string, string>()
        {
            { "analysis.common" , "Lucene.Net.Analysis.Common"},
            { "analysis.icu" , "Lucene.Net.Analysis.ICU"},
            { "analysis.kuromoji" , "Lucene.Net.Analysis.Kuromoji"},
            { "analysis.morfologik" , "Lucene.Net.Analysis.Morfologik"},
            { "analysis.phonetic" , "Lucene.Net.Analysis.Phonetic"},
            { "analysis.smartcn" , "Lucene.Net.Analysis.SmartCn"},
            { "analysis.stempel" , "Lucene.Net.Analysis.Stempel"},
            { "analysis.uima" , "Lucene.Net.Analysis.UIMA"},
            { "benchmark" , "Lucene.Net.Benchmark"},
            { "classification" , "Lucene.Net.Classification"},
            { "codecs" , "Lucene.Net.Codecs"},
            { "core" , "Lucene.Net"},
            { "demo" , "Lucene.Net.Demo"},
            { "expressions" , "Lucene.Net.Expressions"},
            { "facet" , "Lucene.Net.Facet"},
            { "grouping" , "Lucene.Net.Grouping"},
            { "highlighter" , "Lucene.Net.Highlighter"},
            { "join" , "Lucene.Net.Join"},
            { "memory" , "Lucene.Net.Memory"},
            { "misc" , "Lucene.Net.Misc"},
            { "queries" , "Lucene.Net.Queries"},
            { "queryparser" , "Lucene.Net.QueryParser"},
            { "replicator" , "Lucene.Net.Replicator"},
            { "sandbox" , "Lucene.Net.Sandbox"},
            { "spatial" , "Lucene.Net.Spatial"},
            { "suggest" , "Lucene.Net.Suggest"},
            { "test-framework" , "Lucene.Net.TestFramework"},
        };

        private string CorrectRepoCRef(string cref)
        {
            string temp = cref;
            if (temp.StartsWith("src-html"))
            {
                temp = temp.Replace("src-html/", "");
            }

            temp = temp.Replace("/", ".");
            temp = temp.Replace(".html", ".cs");

            var segments = temp.Split('.');

            if (temp.StartsWith("analysis"))
            {
                string project;
                if (packageToProjectName.TryGetValue(segments[3] + "." + segments[4], out project))
                    temp = project + "/" + string.Join("/", segments.Skip(5).ToArray());
            }
            else
            {
                string project;
                if (packageToProjectName.TryGetValue(segments[3], out project))
                    temp = project + "/" + string.Join("/", segments.Skip(4).ToArray());
            }

            temp = CorrectCRefCase(temp);
            foreach (var item in namespaceCorrections)
            {
                if (!item.Key.StartsWith("Lucene.Net"))
                    temp = temp.Replace(item.Key, item.Value);
            }

            temp = Regex.Replace(temp, "/[Cc]s", ".cs");

            return temp;
        }

        private string CorrectCRef(string cref)
        {
            var caseCorrected = CorrectCRefCase(cref);
            var temp = caseCorrected.Replace("org.Apache.Lucene.", "Lucene.Net.");
            foreach (var item in namespaceCorrections)
            {
                temp = temp.Replace(item.Key, item.Value);
            }

            return temp;
        }

        private IDictionary<string, string> namespaceCorrections = new Dictionary<string, string>()
        {
            { "Lucene.Net.Document", "Lucene.Net.Documents" },
            { "Lucene.Net.Benchmark", "Lucene.Net.Benchmarks" },
            { "Lucene.Net.Queryparser", "Lucene.Net.QueryParsers" },
            { ".Tokenattributes", ".TokenAttributes" },
            { ".Charfilter", ".CharFilter" },
            { ".Commongrams", ".CommonGrams" },
            { ".Ngram", ".NGram" },
            { ".Hhmm", ".HHMM" },
            { ".Blockterms", ".BlockTerms" },
            { ".Diskdv", ".DiskDV" },
            { ".Intblock", ".IntBlock" },
            { ".Simpletext", ".SimpleText" },
            { ".Postingshighlight", ".PostingsHighlight" },
            { ".Vectorhighlight", ".VectorHighlight" },
            { ".Complexphrase", ".ComplexPhrase" },
            { ".Valuesource", ".ValueSources" },
        };

        private string CorrectCRefCase(string cref)
        {
            var sb = new StringBuilder(cref);
            for (int i = 0; i < sb.Length - 1; i++)
            {
                if (sb[i] == '.')
                    sb[i + 1] = char.ToUpper(sb[i + 1]);
            }
            return sb.ToString();
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
