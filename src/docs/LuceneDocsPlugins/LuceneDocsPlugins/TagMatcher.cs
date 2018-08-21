using Markdig.Helpers;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace LuceneDocsPlugins
{
    public static class TagMatcher
    {
        public const string ExperimentalMatch = "@lucene.experimental";
        public const string InternalMatch = "@lucene.internal";

        public static string GetMatch(StringSlice slice)
        {
            string matchType = null;
            if (ExtensionsHelper.MatchStart(ref slice, TagMatcher.ExperimentalMatch, false))
                matchType = "experimental";
            if (ExtensionsHelper.MatchStart(ref slice, TagMatcher.InternalMatch, false))
                matchType = "internal";
            return matchType;
        }
    }
}