using Markdig.Syntax.Inlines;

namespace LuceneDocsPlugins
{
    public class LuceneExperimentalInline : LeafInline
    {
        public string MatchType { get; }

        public LuceneExperimentalInline(string matchType)
        {
            MatchType = matchType;
        }
    }
}