using Markdig.Parsers;
using Markdig.Syntax.Inlines;

namespace LuceneDocsPlugins
{
    //TODO: Make an inline parser and see if that works

    public class LuceneExperimentalBlockParser : BlockParser
    {   

        public LuceneExperimentalBlockParser()
        {
            OpeningCharacters = new[] { '@' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var line = processor.Line;

            var matchType = TagMatcher.GetMatch(line);

            if (matchType == null)
                return BlockState.None;

            var luceneExperimentalBlock = new LuceneExperimentalBlock(this, matchType);

            processor.NewBlocks.Push(luceneExperimentalBlock);

            return BlockState.BreakDiscard;
        }
    }
}