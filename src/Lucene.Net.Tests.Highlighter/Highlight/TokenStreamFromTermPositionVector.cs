/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>TokenStream created from a term vector field.</summary>
	/// <remarks>TokenStream created from a term vector field.</remarks>
	public sealed class TokenStreamFromTermPositionVector : TokenStream
	{
		private readonly IList<Token> positionedTokens = new AList<Token>();

		private Iterator<Token> tokensAtCurrentPosition;

		private CharTermAttribute termAttribute;

		private PositionIncrementAttribute positionIncrementAttribute;

		private OffsetAttribute offsetAttribute;

		private PayloadAttribute payloadAttribute;

		/// <summary>Constructor.</summary>
		/// <remarks>Constructor.</remarks>
		/// <param name="vector">
		/// Terms that contains the data for
		/// creating the TokenStream. Must have positions and offsets.
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public TokenStreamFromTermPositionVector(Terms vector)
		{
			termAttribute = AddAttribute<CharTermAttribute>();
			positionIncrementAttribute = AddAttribute<PositionIncrementAttribute>();
			offsetAttribute = AddAttribute<OffsetAttribute>();
			payloadAttribute = AddAttribute<PayloadAttribute>();
			bool hasOffsets = vector.HasOffsets();
			bool hasPayloads = vector.HasPayloads();
			TermsEnum termsEnum = vector.Iterator(null);
			BytesRef text;
			DocsAndPositionsEnum dpEnum = null;
			while ((text = termsEnum.Next()) != null)
			{
				dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
				// presumably checked by TokenSources.hasPositions earlier
				dpEnum != null.NextDoc();
				int freq = dpEnum.Freq();
				for (int j = 0; j < freq; j++)
				{
					int pos = dpEnum.NextPosition();
					Token token;
					if (hasOffsets)
					{
						token = new Token(text.Utf8ToString(), dpEnum.StartOffset(), dpEnum.EndOffset());
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
						token.SetPayload(BytesRef.DeepCopyOf(dpEnum.GetPayload()));
					}
					// Yes - this is the position, not the increment! This is for
					// sorting. This value
					// will be corrected before use.
					token.SetPositionIncrement(pos);
					this.positionedTokens.AddItem(token);
				}
			}
			CollectionUtil.TimSort(this.positionedTokens, tokenComparator);
			int lastPosition = -1;
			foreach (Token token_1 in this.positionedTokens)
			{
				int thisPosition = token_1.GetPositionIncrement();
				token_1.SetPositionIncrement(thisPosition - lastPosition);
				lastPosition = thisPosition;
			}
			this.tokensAtCurrentPosition = this.positionedTokens.Iterator();
		}

		private sealed class _IComparer_111 : IComparer<Token>
		{
			public _IComparer_111()
			{
			}

			public int Compare(Token o1, Token o2)
			{
				return o1.GetPositionIncrement() - o2.GetPositionIncrement();
			}
		}

		private static readonly IComparer<Token> tokenComparator = new _IComparer_111();

		public override bool IncrementToken()
		{
			if (this.tokensAtCurrentPosition.HasNext())
			{
				Token next = this.tokensAtCurrentPosition.Next();
				ClearAttributes();
				termAttribute.SetEmpty().Append(next);
				positionIncrementAttribute.SetPositionIncrement(next.GetPositionIncrement());
				offsetAttribute.SetOffset(next.StartOffset(), next.EndOffset());
				payloadAttribute.SetPayload(next.GetPayload());
				return true;
			}
			return false;
		}

		public override void Reset()
		{
			this.tokensAtCurrentPosition = this.positionedTokens.Iterator();
		}
	}
}
