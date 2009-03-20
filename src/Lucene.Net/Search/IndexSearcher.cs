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
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Search
{
	
	/// <summary>Implements search over a single IndexReader.
	/// 
	/// <p>Applications usually need only call the inherited {@link #Search(Query)}
	/// or {@link #search(Query,Filter)} methods. For performance reasons it is 
	/// recommended to open only one IndexSearcher and use it for all of your searches.
	/// 
	/// <p>Note that you can only access Hits from an IndexSearcher as long as it is
	/// not yet closed, otherwise an IOException will be thrown. 
	/// </summary>
	public class IndexSearcher : Searcher
	{
		private class AnonymousClassHitCollector : HitCollector
		{
			public AnonymousClassHitCollector(System.Collections.BitArray bits, Lucene.Net.Search.HitCollector results, IndexSearcher enclosingInstance)
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

		/// <summary>Creates a searcher searching the index in the named directory.</summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
		public IndexSearcher(System.String path) : this(IndexReader.Open(path), true)
		{
		}
		
		/// <summary>Creates a searcher searching the index in the provided directory.</summary>
		/// <throws>  CorruptIndexException if the index is corrupt </throws>
		/// <throws>  IOException if there is a low-level IO error </throws>
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
				reader.Close();
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
		public override Document Doc(int i, FieldSelector fieldSelector)
		{
			return reader.Document(i, fieldSelector);
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
			
			TopDocCollector collector = new TopDocCollector(nDocs);
			Search(weight, filter, collector);
			return collector.TopDocs();
		}
		
		// inherit javadoc
		public override TopFieldDocs Search(Weight weight, Filter filter, int nDocs, Sort sort)
		{
			
			TopFieldDocCollector collector = new TopFieldDocCollector(reader, sort, nDocs);
			Search(weight, filter, collector);
			return (TopFieldDocs) collector.TopDocs();
		}
		
		// inherit javadoc
		public override void  Search(Weight weight, Filter filter, HitCollector results)
		{
			HitCollector collector = results;
			if (filter != null)
			{
				System.Collections.BitArray bits = filter.Bits(reader);
				collector = new AnonymousClassHitCollector(bits, results, this);
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