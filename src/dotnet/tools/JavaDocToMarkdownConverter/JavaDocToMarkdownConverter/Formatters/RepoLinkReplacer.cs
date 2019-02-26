using Html2Markdown.Replacement;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{

    //TODO: This could instead be done with the LuceneDocsPlugins and our custom markdown parsing
    //TODO: We need to pass in a tag here

    public class RepoLinkReplacer : IReplacer
    {
        private static readonly Regex RepoLinkRegex = new Regex(@"(?<=\()(?<cref>src-html/[^)]*)", RegexOptions.Compiled);

        public string Replace(string html)
        {
            return ReplaceRepoLinks(html);
        }

        //https://github.com/apache/lucenenet/blob/Lucene.Net_4_8_0_beta00004/src/Lucene.Net.Analysis.Common/Analysis/Ar/ArabicAnalyzer.cs
        private string ReplaceRepoLinks(string markdown)
        {
            Match link = RepoLinkRegex.Match(markdown);
            if (link.Success)
            {
                do
                {
                    string cref = link.Groups["cref"].Value.CorrectRepoCRef();
                    cref = "https://github.com/apache/lucenenet/blob/{tag}/src/" + cref;

                    markdown = RepoLinkRegex.Replace(markdown, cref, 1);


                } while ((link = RepoLinkRegex.Match(markdown)).Success);
            }

            return markdown;
        }
    }
}
