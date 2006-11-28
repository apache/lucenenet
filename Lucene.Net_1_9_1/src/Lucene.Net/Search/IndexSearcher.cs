/*
 * Copyright 2004 The Apache Software Foundation
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
using Document = Lucene.Net.Documents.Document;
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Search
{
	
	/// <summary>Implements search over a single IndexReader.
	/// 
	/// <p>Applications usually need only call the inherited {@link #Search(Query)}
	/// or {@link #Search(Query,Filter)} methods. For performance reasons it is 
	/// recommended to open only one IndexSearcher and use it for all of your searches.
	/// 
	/// <p>Note that you can only access Hits from an IndexSearcher as long as it is
	/// not yet closed, otherwise an IOException will be thrown. 
	/// </summary>
	public class IndexSearcher : Searcher
	{
		private class AnonymousClassHitCollector : HitCollector
		{
			public AnonymousClassHitCollector(System.Collections.BitArray bits, int[] totalHits, Lucene.Net.Search.HitQueue hq, int nDocs, IndexSearcher enclosingInstance)
			{
				InitBlock(bits, totalHits, hq, nDocs, enclosingInstance);
			}
			private void  InitBlock(System.Collections.BitArray bits, int[] totalHits, Lucene.Net.Search.HitQueue hq, int nDocs, IndexSearcher enclosingInstance)
			{
				this.bits = bits;
				this.totalHits = totalHits;
				this.hq = hq;
				this.nDocs = nDocs;
				this.enclosingInstance = enclosingInstance;
			}
			private System.Collections.BitArray bits;
			private int[] totalHits;
			private Lucene.Net.Search.HitQueue hq;
			private int nDocs;
			private IndexSearcher enclosingInstance;
			public IndexSearcher Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private float minScore = 0.0f;
			public override void  Collect(int doc, float score)
			{
				if (score > 0.0f && (bits == null || bits.Get(doc)))
				{
					// skip docs not in bits
					totalHits[0]++;
					if (hq.Size() < nDocs || score >= minScore)
					{
						hq.Insert(new ScoreDoc(doc, score));
						minScore = ((ScoreDoc) hq.Top()).score; // maintain minScore
					}
				}
			}
		}
		private class AnonymousClassHitCollector1 : HitCollector
		{
			public AnonymousClassHitCollector1(System.Collections.BitArray bits, int[] totalHits, Lucene.Net.Search.FieldSortedHitQueue hq, IndexSearcher enclosingInstance)
			{
				InitBlock(bits, totalHits, hq, enclosingInstance);
			}
			private void  InitBlock(System.Collections.BitArray bits, int[] totalHits, Lucene.Net.Search.FieldSortedHitQueue hq, IndexSearcher enclosingInstance)
			{
				this.bits = bits;
				this.totalHits = totalHits;
				this.hq = hq;
				this.enclosingInstance = enclosingInstance;
			}
			private System.Collections.BitArray bits;
			private int[] totalHits;
			private Lucene.Net.Search.FieldSortedHitQueue hq;
			private IndexSearcher enclosingInstance;
			public IndexSearcher Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override void  Collect(int doc, float score)
			{
				if (score > 0.0f && (bits == null || bits.Get(doc)))
				{
					// skip docs not in bits
					totalHits[0]++;
					hq.Insert(new FieldDoc(doc, score));
				}
			}
		}
		private class AnonymousClassHitCollector2 : HitCollector
		{
			public AnonymousClassHitCollector2(System.Collections.BitArray bits, Lucene.Net.Search.HitCollector results, IndexSearcher enclosingInstance)
			{
				InitBlock(bits, results, enclosingInstance);
			}
			private void  InitBlock(System.Collections.BitArray bits, Lucene.Net.Search.HitCollector results, IndexSearcher enclosingInstance)
			{
				this.bits = bits;
				this.results = results;
				this.enclosingInstance = enclosingInstance;
			}
			private System.Collections.BitArray bits;
			private Lucene.Net.Search.HitCollector results;
			private IndexSearcher enclosingInstance;
			public IndexSearcher Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public override void  Collect(int doc, float score)
			{
				if (bits.Get(doc))
				{
					// skip docs not in bits
					results.Collect(doc, score);
				}
			}
		}
		internal IndexReader reader;
		private bool closeReader;
		
        public IndexReader Reader
        {
            get {   return reader;  }
        }

		/// <summary>Creates a searcher searching the index in the named directory. </summary>
		public IndexSearcher(System.String path) : this(IndexReader.Open(path), true)
		{
		}
		
		/// <summary>Creates a searcher searching the index in the provided directory. </summary>
		public IndexSearcher(Directory directory) : this(IndexReader.Open(directory), true)
		{
		}
		
		/// <summary>Creates a searcher searching the provided index. </summary>
		public IndexSearcher(IndexReader r) : this(r, false)
		{
		}
		
		private IndexSearcher(IndexReader r, bool closeReader)
		{
			reader = r;
			this.closeReader = closeReader;
		}
		
		/// <summary>Return the {@link IndexReader} this searches. </summary>
		public virtual IndexReader GetIndexReader()
		{
			return reader;
		}
		
		/// <summary> Note that the underlying IndexReader is not closed, if
		/// IndexSearcher was constructed with IndexSearcher(IndexReader r).
		/// If the IndexReader was supplied implicitly by specifying a directory, then
		/// the IndexReader gets closed.
		/// </summary>
		public override void  Close()
		{
            if (closeReader)
            {
                FieldSortedHitQueue.Close(reader); 
                Lucene.Net.Search.FieldCache_Fields.DEFAULT.Close(reader);

                reader.Close();
            }
		}
		
		// inherit javadoc
		public override int DocFreq(Term term)
		{
			return reader.DocFreq(term);
		}
		
		// inherit javadoc
		public override Document Doc(int i)
		{
			return reader.Document(i);
		}
		
		// inherit javadoc
		public override int MaxDoc()
		{
			return reader.MaxDoc();
		}
		
		// inherit javadoc
		public override TopDocs Search(Weight weight, Filter filter, int nDocs)
		{
			
			if (nDocs <= 0)
			    // null might be returned from hq.top() below.
				throw new System.ArgumentException("nDocs must be > 0");
			
			Scorer scorer = weight.Scorer(reader);
			if (scorer == null)
				return new TopDocs(0, new ScoreDoc[0], System.Single.NegativeInfinity);
			
			System.Collections.BitArray bits = filter != null?filter.Bits(reader):null;
			HitQueue hq = new HitQueue(nDocs);
			int[] totalHits = new int[1];
			scorer.Score(new AnonymousClassHitCollector(bits, totalHits, hq, nDocs, this));
			
			ScoreDoc[] scoreDocs = new ScoreDoc[hq.Size()];
			for (int i = hq.Size() - 1; i >= 0; i--)
			    // put docs in array
				scoreDocs[i] = (ScoreDoc) hq.Pop();
			
			float maxScore = (totalHits[0] == 0) ? System.Single.NegativeInfinity : scoreDocs[0].score;
			
			return new TopDocs(totalHits[0], scoreDocs, maxScore);
		}
		
		// inherit javadoc
		public override TopFieldDocs Search(Weight weight, Filter filter, int nDocs, Sort sort)
		{
			Scorer scorer = weight.Scorer(reader);
			if (scorer == null)
				return new TopFieldDocs(0, new ScoreDoc[0], sort.fields, System.Single.NegativeInfinity);
			
			System.Collections.BitArray bits = filter != null ? filter.Bits(reader) : null;
			FieldSortedHitQueue hq = new FieldSortedHitQueue(reader, sort.fields, nDocs);
			int[] totalHits = new int[1];
			scorer.Score(new AnonymousClassHitCollector1(bits, totalHits, hq, this));
			
			ScoreDoc[] scoreDocs = new ScoreDoc[hq.Size()];
			for (int i = hq.Size() - 1; i >= 0; i--)
			// put docs in array
				scoreDocs[i] = hq.FillFields((FieldDoc) hq.Pop());
			
			return new TopFieldDocs(totalHits[0], scoreDocs, hq.GetFields(), hq.GetMaxScore());
		}
		
		// inherit javadoc
		public override void  Search(Weight weight, Filter filter, HitCollector results)
		{
			HitCollector collector = results;
			if (filter != null)
			{
				System.Collections.BitArray bits = filter.Bits(reader);
				collector = new AnonymousClassHitCollector2(bits, results, this);
			}
			
			Scorer scorer = weight.Scorer(reader);
			if (scorer == null)
				return ;
			scorer.Score(collector);
		}
		
		public override Query Rewrite(Query original)
		{
			Query query = original;
			for (Query rewrittenQuery = query.Rewrite(reader); rewrittenQuery != query; rewrittenQuery = query.Rewrite(reader))
			{
				query = rewrittenQuery;
			}
			return query;
		}
		
		public override Explanation Explain(Weight weight, int doc)
		{
			return weight.Explain(reader, doc);
		}
	}
}