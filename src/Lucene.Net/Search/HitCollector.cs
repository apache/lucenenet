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
	
	/// <summary>Lower-level search API.
	/// <br>HitCollectors are primarily meant to be used to implement queries,
	/// sorting and filtering.
	/// </summary>
	/// <seealso cref="Searcher.Search(Query,HitCollector)">
	/// </seealso>
	/// <version>  $Id: HitCollector.java 155607 2005-02-27 01:29:53Z otis $
	/// </version>
	public abstract class HitCollector
	{
		/// <summary>Called once for every non-zero scoring document, with the document number
		/// and its score.
		/// 
		/// <P>If, for example, an application wished to collect all of the hits for a
		/// query in a BitSet, then it might:<pre>
		/// Searcher searcher = new IndexSearcher(indexReader);
		/// final BitSet bits = new BitSet(indexReader.maxDoc());
		/// searcher.search(query, new HitCollector() {
		/// public void collect(int doc, float score) {
		/// bits.set(doc);
		/// }
		/// });
		/// </pre>
		/// 
		/// <p>Note: This is called in an inner search loop.  For good search
		/// performance, implementations of this method should not call
		/// {@link Searcher#Doc(int)} or
		/// {@link Lucene.Net.index.IndexReader#Document(int)} on every
		/// document number encountered.  Doing so can slow searches by an order
		/// of magnitude or more.
		/// <p>Note: The <code>score</code> passed to this method is a raw score.
		/// In other words, the score will not necessarily be a float whose value is
		/// between 0 and 1.
		/// </summary>
		public abstract void  Collect(int doc, float score);
	}
}