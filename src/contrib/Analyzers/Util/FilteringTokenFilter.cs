using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;


namespace Lucene.Net.Analysis.Util
{
    public abstract class FilteringTokenFilter : TokenFilter
    {
        private readonly IPositionIncrementAttribute posIncrAtt;
        
        
        /// <summary>
        /// If <code>true</code>, this TokenFilter will preserve positions of the incoming tokens (ie, accumulate and set position increments of the removed tokens).
        /// Generally, <code>true</code> is best as it does not lose information (positions of the original tokens) during indexing.
        /// When set, when a token is stopped (omitted), the position increment of the following token is incremented.
        /// </summary>
        public bool EnablePositionIncrements { get; set; } // no init needed, as ctor enforces setting value!
        private bool first = true; // only used when not preserving gaps

        protected FilteringTokenFilter(bool enablePositionIncrements, TokenStream input) : base(input)
        {
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            EnablePositionIncrements = enablePositionIncrements;
        }

        protected abstract bool Accept();

        public override bool IncrementToken()
        {
            if (EnablePositionIncrements)
            {
                int skippedPositions = 0;
                while (input.IncrementToken())
                {
                    if (Accept())
                    {
                        if (skippedPositions != 0)
                        {
                            posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
                        }
                        return true;
                    }
                    skippedPositions += posIncrAtt.PositionIncrement;
                }
            }
            else
            {
                while (input.IncrementToken())
                {
                    if (Accept())
                    {
                        if (first)
                        {
                            // first token having posinc=0 is illegal.
                            if (posIncrAtt.PositionIncrement == 0)
                            {
                                posIncrAtt.PositionIncrement = 1;
                            }
                            first = false;
                        }
                        return true;
                    }
                }
            }
            // reached EOS -- return false
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            first = true;
        }
    }
}
