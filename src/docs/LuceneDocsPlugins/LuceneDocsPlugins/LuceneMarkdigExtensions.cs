using Markdig;

namespace LuceneDocsPlugins
{
    public static class LuceneMarkdigExtensions
    {
        public static MarkdownPipelineBuilder UseLuceneExperimental(this MarkdownPipelineBuilder pipeline)
        {
            var extensions = pipeline.Extensions;
            if (!extensions.Contains<LuceneExperimentalMarkdownExtension>())
            {
                extensions.Insert(0, new LuceneExperimentalMarkdownExtension());
            }
            return pipeline;
        }

        public static MarkdownPipeline UseLuceneExperimental(this MarkdownPipeline pipeline)
        {
            var extensions = pipeline.Extensions;
            if (!extensions.Contains<LuceneExperimentalMarkdownExtension>())
            {
                extensions.Insert(0, new LuceneExperimentalMarkdownExtension());
            }
            return pipeline;
        }
    }
}