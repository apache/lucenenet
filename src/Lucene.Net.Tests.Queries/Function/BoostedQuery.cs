/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function
{
	/// <summary>Query that is boosted by a ValueSource</summary>
	public class BoostedQuery : Query
	{
		private Query q;

		private readonly ValueSource boostVal;

		public BoostedQuery(Query subQuery, ValueSource boostVal)
		{
			// TODO: BoostedQuery and BoostingQuery in the same module? 
			// something has to give
			// optional, can be null
			this.q = subQuery;
			this.boostVal = boostVal;
		}

		public virtual Query GetQuery()
		{
			return q;
		}

		public virtual ValueSource GetValueSource()
		{
			return boostVal;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			Query newQ = q.Rewrite(reader);
			if (newQ == q)
			{
				return this;
			}
			Org.Apache.Lucene.Queries.Function.BoostedQuery bq = (Org.Apache.Lucene.Queries.Function.BoostedQuery
				)this.Clone();
			bq.q = newQ;
			return bq;
		}

		public override void ExtractTerms(ICollection<Term> terms)
		{
			q.ExtractTerms(terms);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			return new BoostedQuery.BoostedWeight(this, searcher);
		}

		private class BoostedWeight : Weight
		{
			internal readonly IndexSearcher searcher;

			internal Weight qWeight;

			internal IDictionary fcontext;

			/// <exception cref="System.IO.IOException"></exception>
			public BoostedWeight(BoostedQuery _enclosing, IndexSearcher searcher)
			{
				this._enclosing = _enclosing;
				this.searcher = searcher;
				this.qWeight = this._enclosing.q.CreateWeight(searcher);
				this.fcontext = ValueSource.NewContext(searcher);
				this._enclosing.boostVal.CreateWeight(this.fcontext, searcher);
			}

			public override Query GetQuery()
			{
				return this._enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float GetValueForNormalization()
			{
				float sum = this.qWeight.GetValueForNormalization();
				sum *= this._enclosing.GetBoost() * this._enclosing.GetBoost();
				return sum;
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				topLevelBoost *= this._enclosing.GetBoost();
				this.qWeight.Normalize(norm, topLevelBoost);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Org.Apache.Lucene.Search.Scorer Scorer(AtomicReaderContext context
				, Bits acceptDocs)
			{
				Org.Apache.Lucene.Search.Scorer subQueryScorer = this.qWeight.Scorer(context, acceptDocs
					);
				if (subQueryScorer == null)
				{
					return null;
				}
				return new BoostedQuery.CustomScorer(this, context, this, this._enclosing.GetBoost
					(), subQueryScorer, this._enclosing.boostVal);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Explanation Explain(AtomicReaderContext readerContext, int doc)
			{
				Explanation subQueryExpl = this.qWeight.Explain(readerContext, doc);
				if (!subQueryExpl.IsMatch())
				{
					return subQueryExpl;
				}
				FunctionValues vals = this._enclosing.boostVal.GetValues(this.fcontext, readerContext
					);
				float sc = subQueryExpl.GetValue() * vals.FloatVal(doc);
				Explanation res = new ComplexExplanation(true, sc, this._enclosing.ToString() + ", product of:"
					);
				res.AddDetail(subQueryExpl);
				res.AddDetail(vals.Explain(doc));
				return res;
			}

			private readonly BoostedQuery _enclosing;
		}

		private class CustomScorer : Scorer
		{
			private readonly BoostedQuery.BoostedWeight weight;

			private readonly float qWeight;

			private readonly Scorer scorer;

			private readonly FunctionValues vals;

			private readonly AtomicReaderContext readerContext;

			/// <exception cref="System.IO.IOException"></exception>
			private CustomScorer(BoostedQuery _enclosing, AtomicReaderContext readerContext, 
				BoostedQuery.BoostedWeight w, float qWeight, Scorer scorer, ValueSource vs) : base
				(w)
			{
				this._enclosing = _enclosing;
				this.weight = w;
				this.qWeight = qWeight;
				this.scorer = scorer;
				this.readerContext = readerContext;
				this.vals = vs.GetValues(this.weight.fcontext, readerContext);
			}

			public override int DocID()
			{
				return this.scorer.DocID();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return this.scorer.Advance(target);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				return this.scorer.NextDoc();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				float score = this.qWeight * this.scorer.Score() * this.vals.FloatVal(this.scorer
					.DocID());
				// Current Lucene priority queues can't handle NaN and -Infinity, so
				// map to -Float.MAX_VALUE. This conditional handles both -infinity
				// and NaN since comparisons with NaN are always false.
				return score > float.NegativeInfinity ? score : -float.MaxValue;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return this.scorer.Freq();
			}

			public override ICollection<Scorer.ChildScorer> GetChildren()
			{
				return Sharpen.Collections.Singleton(new Scorer.ChildScorer(this.scorer, "CUSTOM"
					));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual Explanation Explain(int doc)
			{
				Explanation subQueryExpl = this.weight.qWeight.Explain(this.readerContext, doc);
				if (!subQueryExpl.IsMatch())
				{
					return subQueryExpl;
				}
				float sc = subQueryExpl.GetValue() * this.vals.FloatVal(doc);
				Explanation res = new ComplexExplanation(true, sc, this._enclosing.ToString() + ", product of:"
					);
				res.AddDetail(subQueryExpl);
				res.AddDetail(this.vals.Explain(doc));
				return res;
			}

			public override long Cost()
			{
				return this.scorer.Cost();
			}

			private readonly BoostedQuery _enclosing;
		}

		public override string ToString(string field)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("boost(").Append(q.ToString(field)).Append(',').Append(boostVal).Append
				(')');
			sb.Append(ToStringUtils.Boost(GetBoost()));
			return sb.ToString();
		}

		public override bool Equals(object o)
		{
			if (!base.Equals(o))
			{
				return false;
			}
			BoostedQuery other = (BoostedQuery)o;
			return this.q.Equals(other.q) && this.boostVal.Equals(other.boostVal);
		}

		public override int GetHashCode()
		{
			int h = q.GetHashCode();
			h ^= (h << 17) | ((int)(((uint)h) >> 16));
			h += boostVal.GetHashCode();
			h ^= (h << 8) | ((int)(((uint)h) >> 25));
			h += Sharpen.Runtime.FloatToIntBits(GetBoost());
			return h;
		}
	}
}
