using System;

namespace Lucene.Net.Search.Suggest.Analyzing
{
    /// <summary>
    /// LUCENENET specific type for specifying <see cref="AnalyzingSuggester"/> 
    /// and <see cref="FuzzySuggester"/> options. 
    /// </summary>
    [Flags]
    public enum SuggesterOptions
    {
        /// <summary>
        /// Always return the exact match first, regardless of score.  
        /// This has no performance impact but could result in
        /// low-quality suggestions. 
        /// </summary>
        EXACT_FIRST = 1,
        /// <summary>
        /// Preserve token separators when matching. 
        /// </summary>
        PRESERVE_SEP = 2
    }
}
