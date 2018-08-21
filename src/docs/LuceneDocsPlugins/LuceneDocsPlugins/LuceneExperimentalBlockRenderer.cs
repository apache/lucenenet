using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace LuceneDocsPlugins
{
    public class LuceneExperimentalBlockRenderer : HtmlObjectRenderer<LuceneExperimentalBlock>
    {
        private const string Message = "This is a Lucene.NET {0} API, use at your own risk";

        protected override void Write(HtmlRenderer renderer, LuceneExperimentalBlock inclusion)
        {
            renderer.Write("<div class=\"lucene-block lucene-" + inclusion.MatchType.ToLower() + "\">" + string.Format(Message, inclusion.MatchType.ToUpper()) + "</div>");
        }
    }
}