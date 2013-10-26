using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Highlight
{
    public sealed class TokenStreamFromTermPositionVector : TokenStream
    {
        private readonly IList<Token> positionedTokens = new List<Token>();
        private IEnumerator<Token> tokensAtCurrentPosition;
        private ICharTermAttribute termAttribute;
        private IPositionIncrementAttribute positionIncrementAttribute;
        private IOffsetAttribute offsetAttribute;

        public TokenStreamFromTermPositionVector(Terms vector)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            positionIncrementAttribute = AddAttribute<IPositionIncrementAttribute>();
            offsetAttribute = AddAttribute<IOffsetAttribute>();
            bool hasOffsets = vector.HasOffsets;
            TermsEnum termsEnum = vector.Iterator(null);
            BytesRef text;
            DocsAndPositionsEnum dpEnum = null;
            while ((text = termsEnum.Next()) != null)
            {
                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                ;
                dpEnum.NextDoc();
                int freq = dpEnum.Freq;
                for (int j = 0; j < freq; j++)
                {
                    int pos = dpEnum.NextPosition();
                    Token token;
                    if (hasOffsets)
                    {
                        token = new Token(text.Utf8ToString(), dpEnum.StartOffset, dpEnum.EndOffset);
                    }
                    else
                    {
                        token = new Token();
                        token.SetEmpty().Append(text.Utf8ToString());
                    }

                    token.PositionIncrement = pos;
                    this.positionedTokens.Add(token);
                }
            }

            CollectionUtil.MergeSort(this.positionedTokens, tokenComparator);
            int lastPosition = -1;
            foreach (Token token in this.positionedTokens)
            {
                int thisPosition = token.PositionIncrement;
                token.PositionIncrement = thisPosition - lastPosition;
                lastPosition = thisPosition;
            }

            this.tokensAtCurrentPosition = this.positionedTokens.GetEnumerator();
        }

        private static readonly IComparer<Token> tokenComparator = new AnonymousTokenComparator();

        private sealed class AnonymousTokenComparator : IComparer<Token>
        {
            public int Compare(Token o1, Token o2)
            {
                return o1.PositionIncrement - o2.PositionIncrement;
            }
        }

        public override bool IncrementToken()
        {
            if (this.tokensAtCurrentPosition.MoveNext())
            {
                Token next = this.tokensAtCurrentPosition.Current;
                ClearAttributes();
                termAttribute.SetEmpty().Append(next);
                positionIncrementAttribute.PositionIncrement = next.PositionIncrement;
                offsetAttribute.SetOffset(next.StartOffset, next.EndOffset);
                return true;
            }

            return false;
        }

        public override void Reset()
        {
            this.tokensAtCurrentPosition = this.positionedTokens.GetEnumerator();
        }
    }
}
