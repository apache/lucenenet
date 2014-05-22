using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;
	using Bits = Lucene.Net.Util.Bits;
	using ToStringUtils = Lucene.Net.Util.ToStringUtils;


	/// <summary>
	/// A query that wraps another query or a filter and simply returns a constant score equal to the
	/// query boost for every document that matches the filter or query.
	/// For queries it therefore simply strips of all scores and returns a constant one.
	/// </summary>
	public class ConstantScoreQuery : Query
	{
	  protected internal readonly Filter Filter_Renamed;
	  protected internal readonly Query Query_Renamed;

	  /// <summary>
	  /// Strips off scores from the passed in Query. The hits will get a constant score
	  /// dependent on the boost factor of this query. 
	  /// </summary>
	  public ConstantScoreQuery(Query query)
	  {
		if (query == null)
		{
		  throw new System.NullReferenceException("Query may not be null");
		}
		this.Filter_Renamed = null;
		this.Query_Renamed = query;
	  }

	  /// <summary>
	  /// Wraps a Filter as a Query. The hits will get a constant score
	  /// dependent on the boost factor of this query.
	  /// If you simply want to strip off scores from a Query, no longer use
	  /// {@code new ConstantScoreQuery(new QueryWrapperFilter(query))}, instead
	  /// use <seealso cref="#ConstantScoreQuery(Query)"/>!
	  /// </summary>
	  public ConstantScoreQuery(Filter filter)
	  {
		if (filter == null)
		{
		  throw new System.NullReferenceException("Filter may not be null");
		}
		this.Filter_Renamed = filter;
		this.Query_Renamed = null;
	  }

	  /// <summary>
	  /// Returns the encapsulated filter, returns {@code null} if a query is wrapped. </summary>
	  public virtual Filter Filter
	  {
		  get
		  {
			return Filter_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns the encapsulated query, returns {@code null} if a filter is wrapped. </summary>
	  public virtual Query Query
	  {
		  get
		  {
			return Query_Renamed;
		  }
	  }

	  public override Query Rewrite(IndexReader reader)
	  {
		if (Query_Renamed != null)
		{
		  Query rewritten = Query_Renamed.Rewrite(reader);
		  if (rewritten != Query_Renamed)
		  {
			rewritten = new ConstantScoreQuery(rewritten);
			rewritten.Boost = this.Boost;
			return rewritten;
		  }
		}
		else
		{
		  Debug.Assert(Filter_Renamed != null);
		  // Fix outdated usage pattern from Lucene 2.x/early-3.x:
		  // because ConstantScoreQuery only accepted filters,
		  // QueryWrapperFilter was used to wrap queries.
		  if (Filter_Renamed is QueryWrapperFilter)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final QueryWrapperFilter qwf = (QueryWrapperFilter) filter;
			QueryWrapperFilter qwf = (QueryWrapperFilter) Filter_Renamed;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Query rewritten = new ConstantScoreQuery(qwf.getQuery().rewrite(reader));
			Query rewritten = new ConstantScoreQuery(qwf.Query.Rewrite(reader));
			rewritten.Boost = this.Boost;
			return rewritten;
		  }
		}
		return this;
	  }

	  public override void ExtractTerms(Set<Term> terms)
	  {
		// TODO: OK to not add any terms when wrapped a filter
		// and used with MultiSearcher, but may not be OK for
		// highlighting.
		// If a query was wrapped, we delegate to query.
		if (Query_Renamed != null)
		{
		  Query_Renamed.ExtractTerms(terms);
		}
	  }

	  protected internal class ConstantWeight : Weight
	  {
		  private readonly ConstantScoreQuery OuterInstance;

		internal readonly Weight InnerWeight;
		internal float QueryNorm;
		internal float QueryWeight;

		public ConstantWeight(ConstantScoreQuery outerInstance, IndexSearcher searcher)
		{
			this.OuterInstance = outerInstance;
		  this.InnerWeight = (outerInstance.Query_Renamed == null) ? null : outerInstance.Query_Renamed.CreateWeight(searcher);
		}

		public override Query Query
		{
			get
			{
			  return OuterInstance;
			}
		}

		public override float ValueForNormalization
		{
			get
			{
			  // we calculate sumOfSquaredWeights of the inner weight, but ignore it (just to initialize everything)
			  if (InnerWeight != null)
			  {
				  InnerWeight.ValueForNormalization;
			  }
			  QueryWeight = outerInstance.Boost;
			  return QueryWeight * QueryWeight;
			}
		}

		public override void Normalize(float norm, float topLevelBoost)
		{
		  this.QueryNorm = norm * topLevelBoost;
		  QueryWeight *= this.QueryNorm;
		  // we normalize the inner weight, but ignore it (just to initialize everything)
		  if (InnerWeight != null)
		  {
			  InnerWeight.Normalize(norm, topLevelBoost);
		  }
		}

		public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, Bits acceptDocs)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final DocIdSetIterator disi;
		  DocIdSetIterator disi;
		  if (outerInstance.Filter_Renamed != null)
		  {
			Debug.Assert(outerInstance.Query_Renamed == null);
			return base.BulkScorer(context, scoreDocsInOrder, acceptDocs);
		  }
		  else
		  {
			Debug.Assert(outerInstance.Query_Renamed != null && InnerWeight != null);
			BulkScorer bulkScorer = InnerWeight.BulkScorer(context, scoreDocsInOrder, acceptDocs);
			if (bulkScorer == null)
			{
			  return null;
			}
			return new ConstantBulkScorer(OuterInstance, bulkScorer, this, QueryWeight);
		  }
		}

		public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final DocIdSetIterator disi;
		  DocIdSetIterator disi;
		  if (outerInstance.Filter_Renamed != null)
		  {
			Debug.Assert(outerInstance.Query_Renamed == null);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final DocIdSet dis = filter.getDocIdSet(context, acceptDocs);
			DocIdSet dis = outerInstance.Filter_Renamed.GetDocIdSet(context, acceptDocs);
			if (dis == null)
			{
			  return null;
			}
			disi = dis.Iterator();
		  }
		  else
		  {
			Debug.Assert(outerInstance.Query_Renamed != null && InnerWeight != null);
			disi = InnerWeight.Scorer(context, acceptDocs);
		  }

		  if (disi == null)
		  {
			return null;
		  }
		  return new ConstantScorer(OuterInstance, disi, this, QueryWeight);
		}

		public override bool ScoresDocsOutOfOrder()
		{
		  return (InnerWeight != null) ? InnerWeight.ScoresDocsOutOfOrder() : false;
		}

		public override Explanation Explain(AtomicReaderContext context, int doc)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Scorer cs = scorer(context, context.reader().getLiveDocs());
		  Scorer cs = Scorer(context, context.Reader().LiveDocs);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean exists = (cs != null && cs.advance(doc) == doc);
		  bool exists = (cs != null && cs.Advance(doc) == doc);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ComplexExplanation result = new ComplexExplanation();
		  ComplexExplanation result = new ComplexExplanation();
		  if (exists)
		  {
			result.Description = OuterInstance.ToString() + ", product of:";
			result.Value = QueryWeight;
			result.Match = true;
			result.AddDetail(new Explanation(outerInstance.Boost, "boost"));
			result.AddDetail(new Explanation(QueryNorm, "queryNorm"));
		  }
		  else
		  {
			result.Description = OuterInstance.ToString() + " doesn't match id " + doc;
			result.Value = 0;
			result.Match = false;
		  }
		  return result;
		}
	  }

	  /// <summary>
	  /// We return this as our <seealso cref="BulkScorer"/> so that if the CSQ
	  ///  wraps a query with its own optimized top-level
	  ///  scorer (e.g. BooleanScorer) we can use that
	  ///  top-level scorer. 
	  /// </summary>
	  protected internal class ConstantBulkScorer : BulkScorer
	  {
		  private readonly ConstantScoreQuery OuterInstance;

		internal readonly BulkScorer BulkScorer;
		internal readonly Weight Weight;
		internal readonly float TheScore;

		public ConstantBulkScorer(ConstantScoreQuery outerInstance, BulkScorer bulkScorer, Weight weight, float theScore)
		{
			this.OuterInstance = outerInstance;
		  this.BulkScorer = bulkScorer;
		  this.Weight = weight;
		  this.TheScore = theScore;
		}

		public override bool Score(Collector collector, int max)
		{
		  return BulkScorer.Score(WrapCollector(collector), max);
		}

		internal virtual Collector WrapCollector(Collector collector)
		{
		  return new CollectorAnonymousInnerClassHelper(this, collector);
		}

		private class CollectorAnonymousInnerClassHelper : Collector
		{
			private readonly ConstantBulkScorer OuterInstance;

			private Lucene.Net.Search.Collector Collector;

			public CollectorAnonymousInnerClassHelper(ConstantBulkScorer outerInstance, Lucene.Net.Search.Collector collector)
			{
				this.OuterInstance = outerInstance;
				this.Collector = collector;
			}

			public override Scorer Scorer
			{
				set
				{
				  // we must wrap again here, but using the value passed in as parameter:
				  Collector.Scorer = new ConstantScorer(OuterInstance.outerInstance, value, OuterInstance.Weight, OuterInstance.TheScore);
				}
			}

			public override void Collect(int doc)
			{
			  Collector.Collect(doc);
			}

			public override AtomicReaderContext NextReader
			{
				set
				{
				  Collector.NextReader = value;
				}
			}

			public override bool AcceptsDocsOutOfOrder()
			{
			  return Collector.AcceptsDocsOutOfOrder();
			}
		}
	  }

	  protected internal class ConstantScorer : Scorer
	  {
		  private readonly ConstantScoreQuery OuterInstance;

		internal readonly DocIdSetIterator DocIdSetIterator;
		internal readonly float TheScore;

		public ConstantScorer(ConstantScoreQuery outerInstance, DocIdSetIterator docIdSetIterator, Weight w, float theScore) : base(w)
		{
			this.OuterInstance = outerInstance;
		  this.TheScore = theScore;
		  this.DocIdSetIterator = docIdSetIterator;
		}

		public override int NextDoc()
		{
		  return DocIdSetIterator.NextDoc();
		}

		public override int DocID()
		{
		  return DocIdSetIterator.DocID();
		}

		public override float Score()
		{
		  Debug.Assert(DocIdSetIterator.DocID() != NO_MORE_DOCS);
		  return TheScore;
		}

		public override int Freq()
		{
		  return 1;
		}

		public override int Advance(int target)
		{
		  return DocIdSetIterator.Advance(target);
		}

		public override long Cost()
		{
		  return DocIdSetIterator.Cost();
		}

		public override ICollection<ChildScorer> Children
		{
			get
			{
			  if (outerInstance.Query_Renamed != null)
			  {
				return Collections.singletonList(new ChildScorer((Scorer) DocIdSetIterator, "constant"));
			  }
			  else
			  {
				return Collections.emptyList();
			  }
			}
		}
	  }

	  public override Weight CreateWeight(IndexSearcher searcher)
	  {
		return new ConstantScoreQuery.ConstantWeight(this, searcher);
	  }

	  public override string ToString(string field)
	  {
		return (new StringBuilder("ConstantScore(")).Append((Query_Renamed == null) ? Filter_Renamed.ToString() : Query_Renamed.ToString(field)).Append(')').Append(ToStringUtils.Boost(Boost)).ToString();
	  }

	  public override bool Equals(object o)
	  {
		if (this == o)
		{
			return true;
		}
		if (!base.Equals(o))
		{
		  return false;
		}
		if (o is ConstantScoreQuery)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ConstantScoreQuery other = (ConstantScoreQuery) o;
		  ConstantScoreQuery other = (ConstantScoreQuery) o;
		  return ((this.Filter_Renamed == null) ? other.Filter_Renamed == null : this.Filter_Renamed.Equals(other.Filter_Renamed)) && ((this.Query_Renamed == null) ? other.Query_Renamed == null : this.Query_Renamed.Equals(other.Query_Renamed));
		}
		return false;
	  }

	  public override int HashCode()
	  {
		return 31 * base.HashCode() + ((Query_Renamed == null) ? Filter_Renamed : Query_Renamed).HashCode();
	  }

	}

}