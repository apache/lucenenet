/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// One, or several overlapping tokens, along with the score(s) and the scope of
	/// the original text
	/// </summary>
	public class TokenGroup
	{
		private const int MAX_NUM_TOKENS_PER_GROUP = 50;

		internal Token[] tokens = new Token[MAX_NUM_TOKENS_PER_GROUP];

		internal float[] scores = new float[MAX_NUM_TOKENS_PER_GROUP];

		internal int numTokens = 0;

		internal int startOffset = 0;

		internal int endOffset = 0;

		internal float tot;

		internal int matchStartOffset;

		internal int matchEndOffset;

		private OffsetAttribute offsetAtt;

		private CharTermAttribute termAtt;

		public TokenGroup(TokenStream tokenStream)
		{
			offsetAtt = tokenStream.AddAttribute<OffsetAttribute>();
			termAtt = tokenStream.AddAttribute<CharTermAttribute>();
		}

		internal virtual void AddToken(float score)
		{
			if (numTokens < MAX_NUM_TOKENS_PER_GROUP)
			{
				int termStartOffset = offsetAtt.StartOffset();
				int termEndOffset = offsetAtt.EndOffset();
				if (numTokens == 0)
				{
					startOffset = matchStartOffset = termStartOffset;
					endOffset = matchEndOffset = termEndOffset;
					tot += score;
				}
				else
				{
					startOffset = Math.Min(startOffset, termStartOffset);
					endOffset = Math.Max(endOffset, termEndOffset);
					if (score > 0)
					{
						if (tot == 0)
						{
							matchStartOffset = offsetAtt.StartOffset();
							matchEndOffset = offsetAtt.EndOffset();
						}
						else
						{
							matchStartOffset = Math.Min(matchStartOffset, termStartOffset);
							matchEndOffset = Math.Max(matchEndOffset, termEndOffset);
						}
						tot += score;
					}
				}
				Token token = new Token(termStartOffset, termEndOffset);
				token.SetEmpty().Append(termAtt);
				tokens[numTokens] = token;
				scores[numTokens] = score;
				numTokens++;
			}
		}

		internal virtual bool IsDistinct()
		{
			return offsetAtt.StartOffset() >= endOffset;
		}

		internal virtual void Clear()
		{
			numTokens = 0;
			tot = 0;
		}

		public virtual Token GetToken(int index)
		{
			return tokens[index];
		}

		/// <param name="index">a value between 0 and numTokens -1</param>
		/// <returns>the "n"th score</returns>
		public virtual float GetScore(int index)
		{
			return scores[index];
		}

		/// <returns>the end position in the original text</returns>
		public virtual int GetEndOffset()
		{
			return endOffset;
		}

		/// <returns>the number of tokens in this group</returns>
		public virtual int GetNumTokens()
		{
			return numTokens;
		}

		/// <returns>the start position in the original text</returns>
		public virtual int GetStartOffset()
		{
			return startOffset;
		}

		/// <returns>all tokens' scores summed up</returns>
		public virtual float GetTotalScore()
		{
			return tot;
		}
	}
}
