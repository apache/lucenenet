using Microsoft.DocAsCode.Dfm;
using Microsoft.DocAsCode.MarkdownLite;

namespace LuceneDocsPlugins
{
    public sealed class LuceneTokenRendererPart : DfmCustomizedRendererPartBase<IMarkdownRenderer, LuceneNoteBlockToken, MarkdownBlockContext>
    {
        public override string Name => "LuceneTokenRendererPart";

        public override bool Match(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) => true;

        public override StringBuffer Render(IMarkdownRenderer renderer, LuceneNoteBlockToken token, MarkdownBlockContext context) 
            => "<div class=\"lucene-" + token.Label.ToLower() + "\">HELLO WORLD</div>";
    }
}