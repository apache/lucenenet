using Markdig.Helpers;
using Markdig.Parsers;

namespace LuceneDocsPlugins
{
    public class LuceneExperimentalInlineParser : InlineParser
    {
        public LuceneExperimentalInlineParser()
        {
            OpeningCharacters = new[] { '@' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var matchType = TagMatcher.GetMatch(slice);

            if (matchType == null)
                return false;

            var luceneExperimentalInline = new LuceneExperimentalInline(matchType);
            
            processor.Inline = luceneExperimentalInline;
            return true;
        }
    }
}