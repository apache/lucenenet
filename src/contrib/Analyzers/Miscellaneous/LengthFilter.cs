using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public sealed class LengthFilter : FilteringTokenFilter
    {
        private readonly int min;
        private readonly int max;

        private readonly ICharTermAttribute termAtt;

        public LengthFilter(bool enablePositionIncrements, TokenStream input, int min, int max) : base(enablePositionIncrements, input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            this.min = min;
            this.max = max;
        }

        protected override bool Accept()
        {
            int len = termAtt.Length;
            return (len >= min && len <= max);
        }
    }
}
