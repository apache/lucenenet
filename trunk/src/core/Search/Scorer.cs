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
using Lucene.Net.Index;

namespace Lucene.Net.Search
{
	
	/// <summary> Expert: Common scoring functionality for different types of queries.
	/// 
	/// <p/>
	/// A <c>Scorer</c> iterates over documents matching a
	/// query in increasing order of doc Id.
	/// <p/>
	/// <p/>
	/// Document scores are computed using a given <c>Similarity</c>
	/// implementation.
	/// <p/>
	/// 
	/// <p/><b>NOTE</b>: The values Float.Nan,
	/// Float.NEGATIVE_INFINITY and Float.POSITIVE_INFINITY are
	/// not valid scores.  Certain collectors (eg <see cref="TopScoreDocCollector" />
	///) will not properly collect hits
	/// with these scores.
	/// 
	/// </summary>
	/// <seealso cref="BooleanQuery.SetAllowDocsOutOfOrder">
	/// </seealso>
	public abstract class Scorer:DocIdSetIterator
	{
		private Similarity similarity;
		
		/// <summary>Constructs a Scorer.</summary>
		/// <param name="similarity">The <c>Similarity</c> implementation used by this scorer.
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
		/// <see cref="HitCollector.Collect(int, float)" />.
		/// <br/>When this method is used the <see cref="Explain(int)" /> method should not be used.
		/// </param>
		/// <deprecated> use <see cref="Score(Collector)" /> instead.
		/// </deprecated>
        [Obsolete("use Score(Collector) instead.")]
		public virtual void  Score(HitCollector hc)
		{
			Score(new HitCollectorWrapper(hc));
		}
		
		/// <summary>Scores and collects all matching documents.</summary>
		/// <param name="collector">The collector to which all matching documents are passed.
		/// <br/>When this method is used the <see cref="Explain(int)" /> method should not be used.
		/// </param>
		public virtual void  Score(Collector collector)
		{
			collector.SetScorer(this);
			int doc;
			while ((doc = NextDoc()) != NO_MORE_DOCS)
			{
				collector.Collect(doc);
			}
		}
		
		/// <summary>Expert: Collects matching documents in a range.  Hook for optimization.
		/// Note that <see cref="DocIdSetIterator.Next()" /> must be called once before this method is called
		/// for the first time.
		/// </summary>
		/// <param name="hc">The collector to which all matching documents are passed through
		/// <see cref="HitCollector.Collect(int, float)" />.
		/// </param>
		/// <param name="max">Do not score documents past this.
		/// </param>
		/// <returns> true if more matching documents may remain.
		/// </returns>
		/// <deprecated> use <see cref="Score(Collector, int, int)" /> instead.
		/// </deprecated>
        [Obsolete("use Score(Collector, int, int) instead")]
		protected internal virtual bool Score(HitCollector hc, int max)
		{
			return Score(new HitCollectorWrapper(hc), max, DocID());
		}
		
		/// <summary> Expert: Collects matching documents in a range. Hook for optimization.
		/// Note, <paramref name="firstDocID" /> is added to ensure that <see cref="DocIdSetIterator.NextDoc()" />
		/// was called before this method.
		/// 
		/// </summary>
		/// <param name="collector">The collector to which all matching documents are passed.
		/// </param>
		/// <param name="max">Do not score documents past this.
		/// </param>
		/// <param name="firstDocID">
        /// The first document ID (ensures <see cref="DocIdSetIterator.NextDoc()" /> is called before
		/// this method.
		/// </param>
		/// <returns> true if more matching documents may remain.
		/// </returns>
		public /*protected internal*/ virtual bool Score(Collector collector, int max, int firstDocID)
		{
			collector.SetScorer(this);
			int doc = firstDocID;
			while (doc < max)
			{
				collector.Collect(doc);
				doc = NextDoc();
			}
			return doc != NO_MORE_DOCS;
		}
		
		/// <summary>Returns the score of the current document matching the query.
        /// Initially invalid, until <see cref="DocIdSetIterator.Next()" /> or <see cref="DocIdSetIterator.SkipTo(int)" />
		/// is called the first time, or when called from within
		/// <see cref="Collector.Collect(int)" />.
		/// </summary>
		public abstract float Score();
		
		/// <summary>Returns an explanation of the score for a document.
        /// <br/>When this method is used, the <see cref="DocIdSetIterator.Next()" />, <see cref="DocIdSetIterator.SkipTo(int)" /> and
		/// <see cref="Score(HitCollector)" /> methods should not be used.
		/// </summary>
		/// <param name="doc">The document number for the explanation.
		/// 
		/// </param>
		/// <deprecated> Please use <see cref="IndexSearcher.Explain(Lucene.Net.Search.Weight,int)" />
		/// or <see cref="Weight.Explain(IndexReader, int)" /> instead.
		/// </deprecated>
		public virtual Explanation Explain(int doc)
		{
			throw new System.NotSupportedException();
		}
	}
}