using System.Composition;
using Microsoft.DocAsCode.MarkdigEngine;
using Microsoft.DocAsCode.Plugins;

namespace LuceneDocsPlugins
{
    [Export("markdig-custom", typeof(IMarkdownServiceProvider))]
    public class LuceneMarkdigServiceProvider : IMarkdownServiceProvider
    {
        [Import]
        public ICompositionContainer Container { get; set; }

        public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
        {
            return new LuceneMarkdownService(parameters, this.Container);
        }
    }
}