/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
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

using FieldInvertState = Lucene.Net.Index.FieldInvertState;

namespace Lucene.Net.Search
{
	
	/// <summary>Expert: Default scoring implementation. </summary>
	[Serializable]
	public class DefaultSimilarity:Similarity
	{
		
		/// <summary>Implemented as
		/// <c>state.getBoost()*lengthNorm(numTerms)</c>, where
		/// <c>numTerms</c> is <see cref="FieldInvertState.GetLength()" /> if <see cref="SetDiscountOverlaps" />
		/// is false, else it's <see cref="FieldInvertState.GetLength()" />
		/// - <see cref="FieldInvertState.GetNumOverlap()" />
		///.
		/// 
		/// <p/><b>WARNING</b>: This API is new and experimental, and may suddenly
		/// change.<p/> 
		/// </summary>
		public override float ComputeNorm(System.String field, FieldInvertState state)
		{
			int numTerms;
			if (discountOverlaps)
				numTerms = state.GetLength() - state.GetNumOverlap();
			else
				numTerms = state.GetLength();
			return (float) (state.GetBoost() * LengthNorm(field, numTerms));
		}
		
		/// <summary>Implemented as <c>1/sqrt(numTerms)</c>. </summary>
		public override float LengthNorm(System.String fieldName, int numTerms)
		{
			return (float) (1.0 / System.Math.Sqrt(numTerms));
		}
		
		/// <summary>Implemented as <c>1/sqrt(sumOfSquaredWeights)</c>. </summary>
		public override float QueryNorm(float sumOfSquaredWeights)
		{
			return (float) (1.0 / System.Math.Sqrt(sumOfSquaredWeights));
		}
		
		/// <summary>Implemented as <c>sqrt(freq)</c>. </summary>
		public override float Tf(float freq)
		{
			return (float) System.Math.Sqrt(freq);
		}
		
		/// <summary>Implemented as <c>1 / (distance + 1)</c>. </summary>
		public override float SloppyFreq(int distance)
		{
			return 1.0f / (distance + 1);
		}
		
		/// <summary>Implemented as <c>log(numDocs/(docFreq+1)) + 1</c>. </summary>
		public override float Idf(int docFreq, int numDocs)
		{
			return (float) (System.Math.Log(numDocs / (double) (docFreq + 1)) + 1.0);
		}
		
		/// <summary>Implemented as <c>overlap / maxOverlap</c>. </summary>
		public override float Coord(int overlap, int maxOverlap)
		{
			return overlap / (float) maxOverlap;
		}
		
		// Default false
		protected internal bool discountOverlaps;
		
		/// <summary>Determines whether overlap tokens (Tokens with
		/// 0 position increment) are ignored when computing
		/// norm.  By default this is false, meaning overlap
		/// tokens are counted just like non-overlap tokens.
		/// 
		/// <p/><b>WARNING</b>: This API is new and experimental, and may suddenly
		/// change.<p/>
		/// 
		/// </summary>
		/// <seealso cref="ComputeNorm">
		/// </seealso>
		public virtual void  SetDiscountOverlaps(bool v)
		{
			discountOverlaps = v;
		}
		
		/// <seealso cref="SetDiscountOverlaps">
		/// </seealso>
		public virtual bool GetDiscountOverlaps()
		{
			return discountOverlaps;
		}
	}
}