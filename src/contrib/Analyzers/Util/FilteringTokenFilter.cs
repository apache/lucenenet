using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public abstract class FilteringTokenFilter : TokenFilter
    {
        private readonly IPositionIncrementAttribute posIncrAtt; // = addAttribute(PositionIncrementAttribute.class);
        private bool enablePositionIncrements; // no init needed, as ctor enforces setting value!
        private bool first = true; // only used when not preserving gaps

        public FilteringTokenFilter(bool enablePositionIncrements, TokenStream input)
            : base(input)
        {
            this.enablePositionIncrements = enablePositionIncrements;
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        protected abstract bool Accept();

        public override bool IncrementToken()
        {
            if (enablePositionIncrements)
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

        public bool EnablePositionIncrements
        {
            get { return enablePositionIncrements; }
            set { enablePositionIncrements = value; }
        }
    }
}
