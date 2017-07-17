using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;

namespace LuceneDocsPlugins
{
    /// <summary>
    /// Used to replace custom Lucene tokens with custom HTML markup
    /// </summary>
    public sealed class LuceneTokenRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, LuceneNoteBlockToken, MarkdownBlockContext>
    {
        private const string Message = "This is a Lucene.NET {0} API, use at your own risk";

        public override string Name => "LuceneTokenRendererPart";

        public override bool Match(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) => true;

        public override StringBuffer Render(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) 
            => "<div class=\"lucene-block lucene-" + token.Label.ToLower() + "\">" + string.Format(Message, token.Label.ToUpper()) + "</div>";
    }
}