namespace Lucene.Net.Analysis.Miscellaneous
{
    /// <summary>
    /// This TokenFilter limits the number of tokens while indexing. It is a replacement for the maximum field length setting inside IndexWriter.
    /// </summary>
    public sealed class LimitTokenCountFilter : TokenFilter
    {
        private readonly int maxTokenCount;
        private readonly bool consumeAllTokens;
        private int tokenCount = 0;
        private bool exhausted = false;

        public LimitTokenCountFilter(TokenStream input, int maxTokenCount)
            : this(input, maxTokenCount, false)
        {
        }

        public LimitTokenCountFilter(TokenStream input, int maxTokenCount, bool consumeAllTokens)
            : base(input)
        {
            this.maxTokenCount = maxTokenCount;
            this.consumeAllTokens = consumeAllTokens;
        }

        public override bool IncrementToken()
        {
            if (exhausted)
            {
                return false;
            }
            else if (tokenCount < maxTokenCount)
            {
                if (input.IncrementToken())
                {
                    tokenCount++;
                    return true;
                }
                else
                {
                    exhausted = true;
                    return false;
                }
            }
            else
            {
                while (consumeAllTokens && input.IncrementToken()) { /* NOOP */ }
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            tokenCount = 0;
            exhausted = false;
        }
    }
}
