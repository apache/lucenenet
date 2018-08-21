using Markdig;
using Markdig.Renderers;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace LuceneDocsPlugins
{
    public class LuceneExperimentalMarkdownExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.BlockParsers.Insert(0, new LuceneExperimentalBlockParser());
            pipeline.InlineParsers.Insert(0, new LuceneExperimentalInlineParser());

            //pipeline.BlockParsers.AddIfNotAlready<LuceneExperimentalBlockParser>();
            //pipeline.InlineParsers.AddIfNotAlready<LuceneExperimentalInlineParser>();
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (!(renderer is HtmlRenderer htmlRenderer)) return;

            if (!htmlRenderer.ObjectRenderers.Contains<LuceneExperimentalBlockRenderer>())
            {
                htmlRenderer.ObjectRenderers.Insert(0, new LuceneExperimentalBlockRenderer());
            }

            if (!htmlRenderer.ObjectRenderers.Contains<LuceneExperimentalInlineRenderer>())
            {
                htmlRenderer.ObjectRenderers.Insert(0, new LuceneExperimentalInlineRenderer());
            }
        }
    }
}