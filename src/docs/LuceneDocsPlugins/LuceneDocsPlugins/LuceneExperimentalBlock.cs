using Markdig.Parsers;
using Markdig.Syntax;

namespace LuceneDocsPlugins
{
    public class LuceneExperimentalBlock : LeafBlock
    {
        public string MatchType { get; }

        public LuceneExperimentalBlock(BlockParser parser, string matchType) : base(parser)
        {
            MatchType = matchType;
        }
    }
}