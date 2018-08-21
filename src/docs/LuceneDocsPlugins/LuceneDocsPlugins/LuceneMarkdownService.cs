using System;
using System.Collections.Immutable;
using System.Linq;
using Markdig;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.MarkdigEngine;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.DocAsCode.Plugins;

namespace LuceneDocsPlugins
{
    public class LuceneMarkdownService : MarkdigMarkdownService, IMarkdownService
    {
        public LuceneMarkdownService(MarkdownServiceParameters parameters, ICompositionContainer container = null)
            : base(parameters, container)
        {
        }

        string IMarkdownService.Name => "markdig-custom";

        MarkupResult IMarkdownService.Markup(string content, string filePath)
        {
            return this.Markup(content, filePath, false);
        }

        MarkupResult IMarkdownService.Markup(string content, string filePath, bool enableValidation)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (filePath == null)
                throw new ArgumentException("file path can't be null or empty.");
            
            //TODO: Need to use reflection here because of how docfx is structured right now
            var markdownPipeline = (MarkdownPipeline)ReflectionHelper.CallMethod(this, "CreateMarkdownPipeline", false, enableValidation);
            //Add our extesions directly to the MarkdownPipeline (they don't need to be added to the MarkdownPipelineBuilder
            markdownPipeline.UseLuceneExperimental();

            using (InclusionContext.PushFile((RelativePath)filePath))
                return new MarkupResult()
                {
                    Html = Markdown.ToHtml(content, markdownPipeline),
                    Dependency = InclusionContext.Dependencies.Select(file => (string)(RelativePath)file).ToImmutableArray()
                };
        }
    }
}