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

namespace Lucene.Net.Search
{	
	/// <summary> Expert: Common scoring functionality for different types of queries.
	/// 
	/// <p>
	/// A <code>Scorer</code> either iterates over documents matching a
	/// query in increasing order of doc Id, or provides an explanation of
	/// the score for a query for a given document.
	/// </p>
	/// <p>
	/// Document scores are computed using a given <code>Similarity</code>
	/// implementation.
	/// </p>
	/// </summary>
	/// <seealso cref="BooleanQuery.setAllowDocsOutOfOrder">
	/// </seealso>
	public abstract class Scorer : DocIdSetIterator
	{
		private Similarity similarity;
		
		/// <summary>Constructs a Scorer.</summary>
		/// <param name="similarity">The <code>Similarity</code> implementation used by this scorer.
		/// </param>
		protected internal Scorer(Similarity similarity)
		{
			this.similarity = similarity;
		}
		
		/// <summary>Returns the Similarity implementation used by this scorer. </summary>
		public virtual Similarity GetSimilarity()
		{
			return this.similarity;
		}
		
		/// <summary>Scores and collects all matching documents.</summary>
		/// <param name="hc">The collector to which all matching documents are passed through
		/// {@link HitCollector#Collect(int, float)}.
		/// <br>When this method is used the {@link #Explain(int)} method should not be used.
		/// </param>
		public virtual void  Score(HitCollector hc)
		{
			while (Next())
			{
				hc.Collect(Doc(), Score());
			}
		}
		
		/// <summary>Expert: Collects matching documents in a range.  Hook for optimization.
		/// Note that {@link #Next()} must be called once before this method is called
		/// for the first time.
		/// </summary>
		/// <param name="hc">The collector to which all matching documents are passed through
		/// {@link HitCollector#Collect(int, float)}.
		/// </param>
		/// <param name="max">Do not score documents past this.
		/// </param>
		/// <returns> true if more matching documents may remain.
		/// </returns>
		protected internal virtual bool Score(HitCollector hc, int max)
		{
			while (Doc() < max)
			{
				hc.Collect(Doc(), Score());
				if (!Next())
					return false;
			}
			return true;
		}
		
		/// <summary>Returns the score of the current document matching the query.
		/// Initially invalid, until {@link #Next()} or {@link #SkipTo(int)}
		/// is called the first time.
		/// </summary>
		public abstract float Score();

		/// <summary>Returns an explanation of the score for a document.
		/// <br>When this method is used, the {@link #Next()}, {@link #SkipTo(int)} and
		/// {@link #Score(HitCollector)} methods should not be used.
		/// </summary>
		/// <param name="doc">The document number for the explanation.
		/// </param>
		public abstract Explanation Explain(int doc);
	}
}