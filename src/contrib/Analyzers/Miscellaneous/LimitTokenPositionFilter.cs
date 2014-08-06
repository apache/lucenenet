using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Miscellaneous
{
    /// <summary>
    /// This TokenFilter limits its emitted tokens to those with positions that are not greater than the configured limit.
    /// </summary>
    public sealed class LimitTokenPositionFilter : TokenFilter
    {
        private readonly int maxTokenPosition;
        private readonly bool consumeAllTokens;
        private int tokenPosition = 0;
        private bool exhausted = false;
        private readonly IPositionIncrementAttribute posIncAtt;

        /// <summary>
        /// Build a filter that only accepts tokens up to and including the given maximum position. This filter will not consume any tokens with position greater than the maxTokenPosition limit.
        /// </summary>
        /// <param name="input">the stream to wrap</param>
        /// <param name="maxTokenPosition">max position of tokens to produce (1st token always has position 1)</param>
        public LimitTokenPositionFilter(TokenStream input, int maxTokenPosition) : this(input, maxTokenPosition, false)
        {
        }

        /// <summary>
        /// Build a filter that limits the maximum position of tokens to emit.
        /// </summary>
        /// <param name="input">the stream to wrap</param>
        /// <param name="maxTokenPosition">max position of tokens to produce (1st token always has position 1)</param>
        /// <param name="consumeAllTokens">whether all tokens from the wrapped input stream must be consumed even if maxTokenPosition is exceeded.</param>
        public LimitTokenPositionFilter(TokenStream input, int maxTokenPosition, bool consumeAllTokens) : base(input)
        {
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            this.maxTokenPosition = maxTokenPosition;
            this.consumeAllTokens = consumeAllTokens;
        }

        public override bool IncrementToken()
        {
            if (exhausted)
            {
                return false;
            }
            if (input.IncrementToken())
            {
                tokenPosition += posIncAtt.PositionIncrement;
                if (tokenPosition <= maxTokenPosition)
                {
                    return true;
                }
                else
                {
                    while (consumeAllTokens && input.IncrementToken()) { /* NOOP */ }
                    exhausted = true;
                    return false;
                }
            }
            else
            {
                exhausted = true;
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            tokenPosition = 0;
            exhausted = false;
        }
    }
}
