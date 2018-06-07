using System.Collections.Generic;

namespace JavaDocToMarkdownConverter.Formatters
{
    
    public class JavaDocFormatters
    {
        public static IEnumerable<IReplacer> Replacers => new IReplacer[]
            {
                new CodeLinkReplacer(),
                new RepoLinkReplacer(),
                new DocTypeReplacer(),
                new ExtraHtmlElementReplacer()
            };
    }
}
