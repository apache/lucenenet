using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Position
{
    /** Set the positionIncrement of all tokens to the "positionIncrement",
     * except the first return token which retains its original positionIncrement value.
     * The default positionIncrement value is zero.
     */
    public sealed class PositionFilter : TokenFilter
    {

        /** Position increment to assign to all but the first token - default = 0 */
        private int positionIncrement = 0;

        /** The first token must have non-zero positionIncrement **/
        private bool firstTokenPositioned = false;

        private PositionIncrementAttribute posIncrAtt;

        /**
         * Constructs a PositionFilter that assigns a position increment of zero to
         * all but the first token from the given input stream.
         * 
         * @param input the input stream
         */
        public PositionFilter(TokenStream input)
            : base(input)
        {
            posIncrAtt = AddAttribute<PositionIncrementAttribute>();
        }

        /**
         * Constructs a PositionFilter that assigns the given position increment to
         * all but the first token from the given input stream.
         * 
         * @param input the input stream
         * @param positionIncrement position increment to assign to all but the first
         *  token from the input stream
         */
        public PositionFilter(TokenStream input, int positionIncrement)
            : this(input)
        {
            this.positionIncrement = positionIncrement;
        }

        public sealed override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                if (firstTokenPositioned)
                {
                    posIncrAtt.SetPositionIncrement(positionIncrement);
                }
                else
                {
                    firstTokenPositioned = true;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            firstTokenPositioned = false;
        }
    }
}