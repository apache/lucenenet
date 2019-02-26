using Html2Markdown.Replacement;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{

    public class DocTypeReplacer : IReplacer
    {
        private static readonly Regex DocType = new Regex(@"<!doctype[^>]*>", RegexOptions.Compiled);

        public string Replace(string html)
        {
            return DocType.Replace(html, string.Empty);

        }
    }
}
