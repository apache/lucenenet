using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{
    public class PatternReplacer : IReplacer
    {
        private readonly Regex pattern;
        private readonly string replacement;

        public PatternReplacer(Regex pattern, string replacement = null)
        {
            this.pattern = pattern;
            this.replacement = replacement;
        }

        public string Replace(string html)
        {
            return pattern.Replace(html, replacement ?? string.Empty);
        }
    }
}
