using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Highlight
{
    public sealed class TokenStreamFromTermPositionVector : TokenStream
    {
        private readonly List<Token> positionedTokens = new List<Token>();

        private IEnumerator<Token> tokensAtCurrentPosition;

        private ICharTermAttribute termAttribute;

        private IPositionIncrementAttribute positionIncrementAttribute;

        private IOffsetAttribute offsetAttribute;

        private IPayloadAttribute payloadAttribute;

        ///<summary>Constructor</summary>
        /// <param name="vector">Terms that contains the data for
        /// creating the TokenStream.Must have positions and offsets.</param>
        public TokenStreamFromTermPositionVector(Terms vector)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            positionIncrementAttribute = AddAttribute<IPositionIncrementAttribute>();
            offsetAttribute = AddAttribute<IOffsetAttribute>();
            payloadAttribute = AddAttribute<IPayloadAttribute>();

            bool hasOffsets = vector.HasOffsets();
            bool hasPayloads = vector.HasPayloads();
            TermsEnum termsEnum = vector.Iterator(null);
            BytesRef text;
            DocsAndPositionsEnum dpEnum = null;

            while ((text = termsEnum.Next()) != null)
            {
                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                dpEnum.NextDoc();
                int freq = dpEnum.Freq();
                for (int j = 0; j < freq; j++)
                {
                    int pos = dpEnum.NextPosition();
                    Token token;
                    if (hasOffsets)
                    {
                        token = new Token(text.Utf8ToString(),
                            dpEnum.StartOffset(),
                            dpEnum.EndOffset());
                    }
                    else
                    {
                        token = new Token();
                        token.SetEmpty().Append(text.Utf8ToString());
                    }
                    if (hasPayloads)
                    {
                        // Must make a deep copy of the returned payload,
                        // since D&PEnum API is allowed to re-use on every
                        // call:
                        token.Payload = BytesRef.DeepCopyOf(dpEnum.Payload);
                    }

                    // Yes - this is the position, not the increment! This is for
                    // sorting. This value
                    // will be corrected before use.
                    token.PositionIncrement = pos;
                    this.positionedTokens.Add(token);
                }
            }

            this.positionedTokens = positionedTokens.OrderBy(t => t, new TokenComparator()).ToList();

            int lastPosition = -1;
            foreach (Token token in this.positionedTokens)
            {
                int thisPosition = token.PositionIncrement;
                token.PositionIncrement = thisPosition - lastPosition;
                lastPosition = thisPosition;
            }
            this.tokensAtCurrentPosition = this.positionedTokens.GetEnumerator();
        }

        public override bool IncrementToken()
        {
            if (this.tokensAtCurrentPosition.MoveNext())
            {
                Token next = this.tokensAtCurrentPosition.Current;
                ClearAttributes();
                termAttribute.SetEmpty().Append(next);
                positionIncrementAttribute.PositionIncrement = next.PositionIncrement;
                offsetAttribute.SetOffset(next.StartOffset(), next.EndOffset());
                payloadAttribute.Payload = next.Payload;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            this.tokensAtCurrentPosition = this.positionedTokens.GetEnumerator();
        }

        private class TokenComparator : IComparer<Token>
        {
            public int Compare(Token o1, Token o2)
            {
                return o1.PositionIncrement - o2.PositionIncrement;
            }
        }
    }
}
