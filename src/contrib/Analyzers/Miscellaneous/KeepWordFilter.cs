using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    /// <summary>
    /// A TokenFilter that only keeps tokens with text contained in the required words.  This filter behaves like the inverse of StopFilter.
    /// </summary>
    public sealed class KeepWordFilter : FilteringTokenFilter
    {
        private readonly CharArraySet words;
        private readonly ICharTermAttribute termAtt;

        /// <summary>
        /// Create a new KeepWordFilter.
        /// NOTE: The words set passed to this constructor will be directly used by this filter and should not be modified.
        /// </summary>
        /// <param name="version">the Lucene match version</param>
        /// <param name="input">the TokenStream to consume</param>
        /// <param name="words">the words to keep</param>
        public KeepWordFilter(bool enablePositionIncrements, TokenStream input, CharArraySet words)
            : base(enablePositionIncrements, input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            this.words = words;
        }

        protected override bool Accept()
        {
            return words.Contains(termAtt.Buffer, 0, termAtt.Length);
        }
    }
}
