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
using Document = Lucene.Net.Documents.Document;
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;

namespace Lucene.Net.Search
{
	
	/// <summary>The interface for search implementations.
	/// 
	/// <p>Searchable is the abstract network protocol for searching. 
	/// Implementations provide search over a single index, over multiple
	/// indices, and over indices on remote servers.
	/// 
	/// <p>Queries, filters and sort criteria are designed to be compact so that
	/// they may be efficiently passed to a remote index, with only the top-scoring
	/// hits being returned, rather than every non-zero scoring hit.
	/// </summary>
	
    public interface Searchable
    {
        /// <summary>Lower-level search API.
        /// 
        /// <p>{@link HitCollector#Collect(int,float)} is called for every non-zero
        /// scoring document.
        /// <br>HitCollector-based access to remote indexes is discouraged.
        /// 
        /// <p>Applications should only use this if they need <i>all</i> of the
        /// matching documents.  The high-level search API ({@link
        /// Searcher#Search(Query)}) is usually more efficient, as it skips
        /// non-high-scoring hits.
        /// 
        /// </summary>
        /// <param name="weight">to match documents
        /// </param>
        /// <param name="filter">if non-null, a bitset used to eliminate some documents
        /// </param>
        /// <param name="results">to receive hits
        /// </param>
        /// <throws>  BooleanQuery.TooManyClauses </throws>
        void  Search(Weight weight, Filter filter, HitCollector results);


        /// <summary>Frees resources associated with this Searcher.
        /// Be careful not to call this method while you are still using objects
        /// like {@link Hits}.
        /// </summary>
        void  Close();
		
        /// <summary>Expert: Returns the number of documents containing <code>term</code>.
        /// Called by search code to compute term weights.
        /// </summary>
        /// <seealso cref="IndexReader#docFreq(Term)">
        /// </seealso>
        int DocFreq(Term term);
		
        /// <summary>Expert: For each term in the terms array, calculates the number of
        /// documents containing <code>term</code>. Returns an array with these
        /// document frequencies. Used to minimize number of remote calls.
        /// </summary>
        int[] DocFreqs(Term[] terms);
		
        /// <summary>Expert: Returns one greater than the largest possible document number.
        /// Called by search code to compute term weights.
        /// </summary>
        /// <seealso cref="IndexReader#maxDoc()">
        /// </seealso>
        int MaxDoc();
		
        /// <summary>Expert: Low-level search implementation.  Finds the top <code>n</code>
        /// hits for <code>query</code>, applying <code>filter</code> if non-null.
        /// 
        /// <p>Called by {@link Hits}.
        /// 
        /// <p>Applications should usually call {@link Searcher#Search(Query)} or
        /// {@link Searcher#Search(Query,Filter)} instead.
        /// </summary>
        /// <throws>  BooleanQuery.TooManyClauses </throws>
        TopDocs Search(Weight weight, Filter filter, int n);
		
        /// <summary>Expert: Returns the stored fields of document <code>i</code>.
        /// Called by {@link HitCollector} implementations.
        /// </summary>
        /// <seealso cref="IndexReader#document(int)">
        /// </seealso>
        Document Doc(int i);
		
        /// <summary>Expert: called to re-write queries into primitive queries.</summary>
        /// <throws>  BooleanQuery.TooManyClauses </throws>
        Query Rewrite(Query query);
		
        /// <summary>Expert: low-level implementation method
        /// Returns an Explanation that describes how <code>doc</code> scored against
        /// <code>weight</code>.
        /// 
        /// <p>This is intended to be used in developing Similarity implementations,
        /// and, for good performance, should not be displayed with every hit.
        /// Computing an explanation is as expensive as executing the query over the
        /// entire index.
        /// <p>Applications should call {@link Searcher#Explain(Query, int)}.
        /// </summary>
        /// <throws>  BooleanQuery.TooManyClauses </throws>
        Explanation Explain(Weight weight, int doc);
		
        /// <summary>Expert: Low-level search implementation with arbitrary sorting.  Finds
        /// the top <code>n</code> hits for <code>query</code>, applying
        /// <code>filter</code> if non-null, and sorting the hits by the criteria in
        /// <code>sort</code>.
        /// 
        /// <p>Applications should usually call {@link
        /// Searcher#search(Query,Filter,Sort)} instead.
        /// </summary>
        /// <throws>  BooleanQuery.TooManyClauses </throws>
        TopFieldDocs Search(Weight weight, Filter filter, int n, Sort sort);
    }
}