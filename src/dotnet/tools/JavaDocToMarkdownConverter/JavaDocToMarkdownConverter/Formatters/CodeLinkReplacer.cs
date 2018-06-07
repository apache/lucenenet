using Html2Markdown.Replacement;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{

    public class CodeLinkReplacer : IReplacer
    {
        private static readonly Regex LinkRegex = new Regex(@"{@link\s*?(?<cref>org\.apache\.lucene\.[^}]*)\s?(?<text>[^}]*)}", RegexOptions.Compiled);
        private static readonly Regex JavaCodeExtension = new Regex(@".java$", RegexOptions.Compiled);

        public string Replace(string html)
        {
            return ReplaceCodeLinks(html);
        }

        private string ReplaceCodeLinks(string markdown)
        {
            Match link = LinkRegex.Match(markdown);
            if (link.Success)
            {
                do
                {
                    string cref = link.Groups["cref"].Value.CorrectCRef();
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
    }
}
