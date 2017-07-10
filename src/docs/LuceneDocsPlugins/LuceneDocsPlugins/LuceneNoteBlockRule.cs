using System.Text.RegularExpressions;
using Microsoft.DocAsCode.MarkdownLite;

namespace LuceneDocsPlugins
{
    public class LuceneNoteBlockRule : IMarkdownRule
    {       
        public virtual Regex LabelRegex { get; } = new Regex("^@lucene\\.(?<notetype>(experimental|internal))$");

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = LabelRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new LuceneNoteBlockToken(this, parser.Context, match.Groups[1].Value, sourceInfo);
        }

        public virtual string Name => "LuceneNote";
    }
}