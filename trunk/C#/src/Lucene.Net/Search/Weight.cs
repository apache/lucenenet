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

using IndexReader = Lucene.Net.Index.IndexReader;

namespace Lucene.Net.Search
{
	
	/// <summary>Expert: Calculate query weights and build query scorers.
	/// <p>
	/// The purpose of Weight is to make it so that searching does not modify
	/// a Query, so that a Query instance can be reused. <br>
	/// Searcher dependent state of the query should reside in the Weight. <br>
	/// IndexReader dependent state should reside in the Scorer.
	/// <p>
	/// A <code>Weight</code> is used in the following way:
	/// <ol>
	/// <li>A <code>Weight</code> is constructed by a top-level query,
	/// given a <code>Searcher</code> ({@link Query#CreateWeight(Searcher)}).
	/// <li>The {@link #SumOfSquaredWeights()} method is called
	/// on the <code>Weight</code> to compute
	/// the query normalization factor {@link Similarity#QueryNorm(float)}
	/// of the query clauses contained in the query.
	/// <li>The query normalization factor is passed to {@link #Normalize(float)}.
	/// At this point the weighting is complete.
	/// <li>A <code>Scorer</code> is constructed by {@link #Scorer(IndexReader)}.
	/// </ol>
	/// </summary>
	public interface Weight
	{
		/// <summary>The query that this concerns. </summary>
		Query GetQuery();
		
		/// <summary>The weight for this query. </summary>
		float GetValue();
		
		/// <summary>The sum of squared weights of contained query clauses. </summary>
		float SumOfSquaredWeights();
		
		/// <summary>Assigns the query normalization factor to this. </summary>
		void  Normalize(float norm);
		
		/// <summary>Constructs a scorer for this. </summary>
		Scorer Scorer(IndexReader reader);
		
		/// <summary>An explanation of the score computation for the named document. </summary>
		Explanation Explain(IndexReader reader, int doc);
	}
}