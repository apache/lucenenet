using System;
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

        /// <summary>
        /// A list of custom replacers for specific uid files
        /// </summary>
        public static IDictionary<string, IReplacer> CustomReplacers => new Dictionary<string, IReplacer>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["Lucene.Net"] = new PatternReplacer(new System.Text.RegularExpressions.Regex("To demonstrate these, try something like:.*$", System.Text.RegularExpressions.RegexOptions.Singleline))
        };
    }
}
