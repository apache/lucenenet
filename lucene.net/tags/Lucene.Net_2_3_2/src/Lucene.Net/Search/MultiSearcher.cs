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
using FieldSelector = Lucene.Net.Documents.FieldSelector;
using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
using Term = Lucene.Net.Index.Term;

namespace Lucene.Net.Search
{
	
	/// <summary>Implements search over a set of <code>Searchables</code>.
	/// 
	/// <p>Applications usually need only call the inherited {@link #Search(Query)}
	/// or {@link #search(Query,Filter)} methods.
	/// </summary>
	public class MultiSearcher : Searcher
	{
		private class AnonymousClassHitCollector : HitCollector
		{
			public AnonymousClassHitCollector(Lucene.Net.Search.HitCollector results, int start, MultiSearcher enclosingInstance)
			{
				InitBlock(results, start, enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Search.HitCollector results, int start, MultiSearcher enclosingInstance)
			{
				this.results = results;
				this.start = start;
				this.enclosingInstance = enclosingInstance;
			}
			private Lucene.Net.Search.HitCollector results;
			private int start;
			private MultiSearcher enclosingInstance;
			public MultiSearcher Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override void  Collect(int doc, float score)
			{
				results.Collect(doc + start, score);
			}
		}
		/// <summary> Document Frequency cache acting as a Dummy-Searcher.
		/// This class is no full-fledged Searcher, but only supports
		/// the methods necessary to initialize Weights.
		/// </summary>
		private class CachedDfSource:Searcher
		{
			private System.Collections.IDictionary dfMap; // Map from Terms to corresponding doc freqs
			private int maxDoc; // document count
			
			public CachedDfSource(System.Collections.IDictionary dfMap, int maxDoc, Similarity similarity)
			{
				this.dfMap = dfMap;
				this.maxDoc = maxDoc;
				SetSimilarity(similarity);
			}
			
			public override int DocFreq(Term term)
			{
				int df;
				try
				{
					df = ((System.Int32) dfMap[term]);
				}
				catch (System.NullReferenceException)
				{
					throw new System.ArgumentException("df for term " + term.Text() + " not available");
				}
				return df;
			}
			
			public override int[] DocFreqs(Term[] terms)
			{
				int[] result = new int[terms.Length];
				for (int i = 0; i < terms.Length; i++)
				{
					result[i] = DocFreq(terms[i]);
				}
				return result;
			}
			
			public override int MaxDoc()
			{
				return maxDoc;
			}
			
			public override Query Rewrite(Query query)
			{
				// this is a bit of a hack. We know that a query which
				// creates a Weight based on this Dummy-Searcher is
				// always already rewritten (see preparedWeight()).
				// Therefore we just return the unmodified query here
				return query;
			}
			
			public override void  Close()
			{
				throw new System.NotSupportedException();
			}
			
			public override Document Doc(int i)
			{
				throw new System.NotSupportedException();
			}
			
			public override Document Doc(int i, FieldSelector fieldSelector)
			{
				throw new System.NotSupportedException();
			}
			
			public override Explanation Explain(Weight weight, int doc)
			{
				throw new System.NotSupportedException();
			}
			
			public override void  Search(Weight weight, Filter filter, HitCollector results)
			{
				throw new System.NotSupportedException();
			}
			
			public override TopDocs Search(Weight weight, Filter filter, int n)
			{
				throw new System.NotSupportedException();
			}
			
			public override TopFieldDocs Search(Weight weight, Filter filter, int n, Sort sort)
			{
				throw new System.NotSupportedException();
			}
		}
		
		
		private Lucene.Net.Search.Searchable[] searchables;
		private int[] starts;
		private int maxDoc = 0;
		
		/// <summary>Creates a searcher which searches <i>searchables</i>. </summary>
		public MultiSearcher(Lucene.Net.Search.Searchable[] searchables)
		{
			this.searchables = searchables;
			
			starts = new int[searchables.Length + 1]; // build starts array
			for (int i = 0; i < searchables.Length; i++)
			{
				starts[i] = maxDoc;
				maxDoc += searchables[i].MaxDoc(); // compute maxDocs
			}
			starts[searchables.Length] = maxDoc;
		}
		
		/// <summary>Return the array of {@link Searchable}s this searches. </summary>
		public virtual Lucene.Net.Search.Searchable[] GetSearchables()
		{
			return searchables;
		}
		
		protected internal virtual int[] GetStarts()
		{
			return starts;
		}
		
		// inherit javadoc
		public override void  Close()
		{
			for (int i = 0; i < searchables.Length; i++)
				searchables[i].Close();
		}
		
		public override int DocFreq(Term term)
		{
			int docFreq = 0;
			for (int i = 0; i < searchables.Length; i++)
				docFreq += searchables[i].DocFreq(term);
			return docFreq;
		}
		
		// inherit javadoc
		public override Document Doc(int n)
		{
			int i = SubSearcher(n); // find searcher index
			return searchables[i].Doc(n - starts[i]); // dispatch to searcher
		}
		
		// inherit javadoc
		public override Document Doc(int n, FieldSelector fieldSelector)
		{
			int i = SubSearcher(n); // find searcher index
			return searchables[i].Doc(n - starts[i], fieldSelector); // dispatch to searcher
		}
		
		/// <summary>Returns index of the searcher for document <code>n</code> in the array
		/// used to construct this searcher. 
		/// </summary>
		public virtual int SubSearcher(int n)
		{
			// find searcher for doc n:
			// replace w/ call to Arrays.binarySearch in Java 1.2
			int lo = 0; // search starts array
			int hi = searchables.Length - 1; // for first element less
			// than n, return its index
			while (hi >= lo)
			{
				int mid = (lo + hi) >> 1;
				int midValue = starts[mid];
				if (n < midValue)
					hi = mid - 1;
				else if (n > midValue)
					lo = mid + 1;
				else
				{
					// found a match
					while (mid + 1 < searchables.Length && starts[mid + 1] == midValue)
					{
						mid++; // scan to last match
					}
					return mid;
				}
			}
			return hi;
		}
		
		/// <summary>Returns the document number of document <code>n</code> within its
		/// sub-index. 
		/// </summary>
		public virtual int SubDoc(int n)
		{
			return n - starts[SubSearcher(n)];
		}
		
		public override int MaxDoc()
		{
			return maxDoc;
		}
		
		public override TopDocs Search(Weight weight, Filter filter, int nDocs)
		{
			
			HitQueue hq = new HitQueue(nDocs);
			int totalHits = 0;
			
			for (int i = 0; i < searchables.Length; i++)
			{
				// search each searcher
				TopDocs docs = searchables[i].Search(weight, filter, nDocs);
				totalHits += docs.totalHits; // update totalHits
				ScoreDoc[] scoreDocs = docs.scoreDocs;
				for (int j = 0; j < scoreDocs.Length; j++)
				{
					// merge scoreDocs into hq
					ScoreDoc scoreDoc = scoreDocs[j];
					scoreDoc.doc += starts[i]; // convert doc
					if (!hq.Insert(scoreDoc))
						break; // no more scores > minScore
				}
			}
			
			ScoreDoc[] scoreDocs2 = new ScoreDoc[hq.Size()];
			for (int i = hq.Size() - 1; i >= 0; i--)
				// put docs in array
				scoreDocs2[i] = (ScoreDoc) hq.Pop();
			
			float maxScore = (totalHits == 0) ? System.Single.NegativeInfinity : scoreDocs2[0].score;
			
			return new TopDocs(totalHits, scoreDocs2, maxScore);
		}
		
		public override TopFieldDocs Search(Weight weight, Filter filter, int n, Sort sort)
		{
			FieldDocSortedHitQueue hq = null;
			int totalHits = 0;
			
			float maxScore = System.Single.NegativeInfinity;
			
			for (int i = 0; i < searchables.Length; i++)
			{
				// search each searcher
				TopFieldDocs docs = searchables[i].Search(weight, filter, n, sort);
				
				if (hq == null)
					hq = new FieldDocSortedHitQueue(docs.fields, n);
				totalHits += docs.totalHits; // update totalHits
				maxScore = System.Math.Max(maxScore, docs.GetMaxScore());
				ScoreDoc[] scoreDocs = docs.scoreDocs;
				for (int j = 0; j < scoreDocs.Length; j++)
				{
					// merge scoreDocs into hq
					ScoreDoc scoreDoc = scoreDocs[j];
					scoreDoc.doc += starts[i]; // convert doc
					if (!hq.Insert(scoreDoc))
						break; // no more scores > minScore
				}
			}
			
			ScoreDoc[] scoreDocs2 = new ScoreDoc[hq.Size()];
			for (int i = hq.Size() - 1; i >= 0; i--)
				// put docs in array
				scoreDocs2[i] = (ScoreDoc) hq.Pop();
			
			return new TopFieldDocs(totalHits, scoreDocs2, hq.GetFields(), maxScore);
		}
		
		
		// inherit javadoc
		public override void  Search(Weight weight, Filter filter, HitCollector results)
		{
			for (int i = 0; i < searchables.Length; i++)
			{
				
				int start = starts[i];
				
				searchables[i].Search(weight, filter, new AnonymousClassHitCollector(results, start, this));
			}
		}
		
		public override Query Rewrite(Query original)
		{
			Query[] queries = new Query[searchables.Length];
			for (int i = 0; i < searchables.Length; i++)
			{
				queries[i] = searchables[i].Rewrite(original);
			}
			return queries[0].Combine(queries);
		}
		
		public override Explanation Explain(Weight weight, int doc)
		{
			int i = SubSearcher(doc); // find searcher index
			return searchables[i].Explain(weight, doc - starts[i]); // dispatch to searcher
		}
		
		/// <summary> Create weight in multiple index scenario.
		/// 
		/// Distributed query processing is done in the following steps:
		/// 1. rewrite query
		/// 2. extract necessary terms
		/// 3. collect dfs for these terms from the Searchables
		/// 4. create query weight using aggregate dfs.
		/// 5. distribute that weight to Searchables
		/// 6. merge results
		/// 
		/// Steps 1-4 are done here, 5+6 in the search() methods
		/// 
		/// </summary>
		/// <returns> rewritten queries
		/// </returns>
		protected internal override Weight CreateWeight(Query original)
		{
			// step 1
			Query rewrittenQuery = Rewrite(original);
			
			// step 2
			System.Collections.Hashtable terms = new System.Collections.Hashtable();
			rewrittenQuery.ExtractTerms(terms);
			
			// step3
			Term[] allTermsArray = new Term[terms.Count];
            int index = 0;
            System.Collections.IEnumerator e = terms.Keys.GetEnumerator();
            while (e.MoveNext())
                allTermsArray[index++] = e.Current as Term;
            int[] aggregatedDfs = new int[terms.Count];
			for (int i = 0; i < searchables.Length; i++)
			{
				int[] dfs = searchables[i].DocFreqs(allTermsArray);
				for (int j = 0; j < aggregatedDfs.Length; j++)
				{
					aggregatedDfs[j] += dfs[j];
				}
			}
			
			System.Collections.Hashtable dfMap = new System.Collections.Hashtable();
			for (int i = 0; i < allTermsArray.Length; i++)
			{
				dfMap[allTermsArray[i]] = (System.Int32) aggregatedDfs[i];
			}
			
			// step4
			int numDocs = MaxDoc();
			CachedDfSource cacheSim = new CachedDfSource(dfMap, numDocs, GetSimilarity());
			
			return rewrittenQuery.Weight(cacheSim);
		}
	}
}