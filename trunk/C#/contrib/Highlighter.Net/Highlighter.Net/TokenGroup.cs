/*
 * Copyright 2002-2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Token = Lucene.Net.Analysis.Token;

namespace Lucene.Net.Highlight
{
	
	/// <summary> One, or several overlapping tokens, along with the score(s) and the
	/// scope of the original text
	/// </summary>
	/// <author>  MAHarwood
	/// </author>
	public class TokenGroup
	{
		
		private const int MAX_NUM_TOKENS_PER_GROUP = 50;
		internal Token[] tokens = new Token[MAX_NUM_TOKENS_PER_GROUP];
		internal float[] scores = new float[MAX_NUM_TOKENS_PER_GROUP];
		internal int numTokens = 0;
		internal int startOffset = 0;
		internal int endOffset = 0;
		
		
		internal virtual void  AddToken(Token token, float score)
		{
			if (numTokens < MAX_NUM_TOKENS_PER_GROUP)
			{
				if (numTokens == 0)
				{
					startOffset = token.StartOffset();
					endOffset = token.EndOffset();
				}
				else
				{
					startOffset = Math.Min(startOffset, token.StartOffset());
					endOffset = Math.Max(endOffset, token.EndOffset());
				}
				tokens[numTokens] = token;
				scores[numTokens] = score;
				numTokens++;
			}
		}
		
		internal virtual bool IsDistinct(Token token)
		{
			return token.StartOffset() >= endOffset;
		}
		
		
		internal virtual void  Clear()
		{
			numTokens = 0;
		}
		
		/// <summary> </summary>
		/// <param name="index">a value between 0 and numTokens -1
		/// </param>
		/// <returns> the "n"th token
		/// </returns>
		public virtual Token GetToken(int index)
		{
			return tokens[index];
		}
		
		/// <summary> </summary>
		/// <param name="index">a value between 0 and numTokens -1
		/// </param>
		/// <returns> the "n"th score
		/// </returns>
		public virtual float GetScore(int index)
		{
			return scores[index];
		}
		
		/// <returns> the end position in the original text
		/// </returns>
		public virtual int GetEndOffset()
		{
			return endOffset;
		}
		
		/// <returns> the number of tokens in this group
		/// </returns>
		public virtual int GetNumTokens()
		{
			return numTokens;
		}
		
		/// <returns> the start position in the original text
		/// </returns>
		public virtual int GetStartOffset()
		{
			return startOffset;
		}
		
		/// <returns> all tokens' scores summed up
		/// </returns>
		public virtual float GetTotalScore()
		{
			float total = 0;
			for (int i = 0; i < numTokens; i++)
			{
				total += scores[i];
			}
			return total;
		}
	}
}