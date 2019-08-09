using ICU4N.Text;
using Lucene.Net.Support;
using System.Globalization;

namespace Lucene.Net.Search.PostingsHighlight
{
    /// <summary>
    /// Mock of the original Lucene <see cref="PostingsHighlighter"/> that is backed
    /// by a <see cref="JdkBreakIterator"/> with custom rules to act
    /// (sort of) like the JDK. This is just to verify we can make the behavior work
    /// similar to the implementation in Lucene by customizing the <see cref="BreakIterator"/>.
    /// </summary>
    public class PostingsHighlighter : ICUPostingsHighlighter
    {
        public PostingsHighlighter()
            : base()
        {
        }

        public PostingsHighlighter(int maxLength)
            : base(maxLength)
        {
        }
        protected override BreakIterator GetBreakIterator(string field)
        {
            return JdkBreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);
        }
    }
}
