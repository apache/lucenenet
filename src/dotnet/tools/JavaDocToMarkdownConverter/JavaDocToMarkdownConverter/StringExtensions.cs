using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter
{
    public static class StringExtensions
    {
        public static string CorrectCRef(this string cref)
        {
            var caseCorrected = CorrectCRefCase(cref);
            var temp = caseCorrected.Replace("org.Apache.Lucene.", "Lucene.Net.");
            foreach (var item in namespaceCorrections)
            {
                temp = temp.Replace(item.Key, item.Value);
            }


            //TODO: Not sure if this is necessary? The # delimits a method name and we already take care of that
            int index = temp.IndexOf('#');
            if (index > -1)
            {
                var sb = new StringBuilder(temp);
                // special case - capitalize char after #
                sb[index + 1] = char.ToUpperInvariant(sb[index + 1]);
                // special case - replace Java # with .
                temp = sb.ToString().Replace('#', '.');
            }

            return temp;
        }

        public static string CorrectCRefCase(this string cref)
        {
            var sb = new StringBuilder(cref);
            for (int i = 0; i < sb.Length - 1; i++)
            {
                if (sb[i] == '.')
                    sb[i + 1] = char.ToUpper(sb[i + 1]);
            }
            return sb.ToString();
        }

        public static string CorrectRepoCRef(this string cref)
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

        private static readonly IDictionary<string, string> packageToProjectName = new Dictionary<string, string>()
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

        private static readonly IDictionary<string, string> namespaceCorrections = new Dictionary<string, string>()
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
    }
}
