using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class KeywordRepeatFilter : TokenFilter
    {
        private readonly IKeywordAttribute keywordAttribute;
        private readonly IPositionIncrementAttribute posIncAttr;
        private State state;

        public KeywordRepeatFilter(TokenStream input) : base(input)
        {
            posIncAttr = AddAttribute<PositionIncrementAttribute>();
            keywordAttribute = AddAttribute<KeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (state != null)
            {
                RestoreState(state);
                posIncAttr.PositionIncrement = 0;
                keywordAttribute.IsKeyword = false;
                state = null;
                return true;
            }
            if (input.IncrementToken())
            {
                state = CaptureState();
                keywordAttribute.IsKeyword = true;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            state = null;
        }
    }
}
