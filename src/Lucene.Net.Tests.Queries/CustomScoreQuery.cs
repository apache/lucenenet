/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>
	/// Query that sets document score as a programmatic function of several (sub) scores:
	/// <ol>
	/// <li>the score of its subQuery (any query)</li>
	/// <li>(optional) the score of its
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
	/// 	</see>
	/// (or queries).</li>
	/// </ol>
	/// Subclasses can modify the computation by overriding
	/// <see cref="GetCustomScoreProvider(Org.Apache.Lucene.Index.AtomicReaderContext)">GetCustomScoreProvider(Org.Apache.Lucene.Index.AtomicReaderContext)
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class CustomScoreQuery : Query
	{
		private Query subQuery;

		private Query[] scoringQueries;

		private bool strict = false;

		/// <summary>Create a CustomScoreQuery over input subQuery.</summary>
		/// <remarks>Create a CustomScoreQuery over input subQuery.</remarks>
		/// <param name="subQuery">the sub query whose scored is being customized. Must not be null.
		/// 	</param>
		public CustomScoreQuery(Query subQuery) : this(subQuery, new FunctionQuery[0])
		{
		}

		/// <summary>
		/// Create a CustomScoreQuery over input subQuery and a
		/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="subQuery">the sub query whose score is being customized. Must not be null.
		/// 	</param>
		/// <param name="scoringQuery">
		/// a value source query whose scores are used in the custom score
		/// computation.  This parameter is optional - it can be null.
		/// </param>
		public CustomScoreQuery(Query subQuery, FunctionQuery scoringQuery) : this(subQuery
			, scoringQuery != null ? new FunctionQuery[] { scoringQuery } : new FunctionQuery
			[0])
		{
		}

		/// <summary>
		/// Create a CustomScoreQuery over input subQuery and a
		/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="subQuery">the sub query whose score is being customized. Must not be null.
		/// 	</param>
		/// <param name="scoringQueries">
		/// value source queries whose scores are used in the custom score
		/// computation.  This parameter is optional - it can be null or even an empty array.
		/// </param>
		public CustomScoreQuery(Query subQuery, params FunctionQuery[] scoringQueries)
		{
			// never null (empty array if there are no valSrcQueries).
			// if true, valueSource part of query does not take part in weights normalization.
			// don't want an array that contains a single null..
			this.subQuery = subQuery;
			this.scoringQueries = scoringQueries != null ? scoringQueries : new Query[0];
			if (subQuery == null)
			{
				throw new ArgumentException("<subquery> must not be null!");
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			Org.Apache.Lucene.Queries.CustomScoreQuery clone = null;
			Query sq = subQuery.Rewrite(reader);
			if (sq != subQuery)
			{
				clone = ((Org.Apache.Lucene.Queries.CustomScoreQuery)Clone());
				clone.subQuery = sq;
			}
			for (int i = 0; i < scoringQueries.Length; i++)
			{
				Query v = scoringQueries[i].Rewrite(reader);
				if (v != scoringQueries[i])
				{
					if (clone == null)
					{
						clone = ((Org.Apache.Lucene.Queries.CustomScoreQuery)Clone());
					}
					clone.scoringQueries[i] = v;
				}
			}
			return (clone == null) ? this : clone;
		}

		public override void ExtractTerms(ICollection<Term> terms)
		{
			subQuery.ExtractTerms(terms);
			foreach (Query scoringQuery in scoringQueries)
			{
				scoringQuery.ExtractTerms(terms);
			}
		}

		public override Query Clone()
		{
			Org.Apache.Lucene.Queries.CustomScoreQuery clone = (Org.Apache.Lucene.Queries.CustomScoreQuery
				)base.Clone();
			clone.subQuery = subQuery.Clone();
			clone.scoringQueries = new Query[scoringQueries.Length];
			for (int i = 0; i < scoringQueries.Length; i++)
			{
				clone.scoringQueries[i] = scoringQueries[i].Clone();
			}
			return clone;
		}

		public override string ToString(string field)
		{
			StringBuilder sb = new StringBuilder(Name()).Append("(");
			sb.Append(subQuery.ToString(field));
			foreach (Query scoringQuery in scoringQueries)
			{
				sb.Append(", ").Append(scoringQuery.ToString(field));
			}
			sb.Append(")");
			sb.Append(strict ? " STRICT" : string.Empty);
			return sb.ToString() + ToStringUtils.Boost(GetBoost());
		}

		/// <summary>Returns true if <code>o</code> is equal to this.</summary>
		/// <remarks>Returns true if <code>o</code> is equal to this.</remarks>
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
			if (GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.CustomScoreQuery other = (Org.Apache.Lucene.Queries.CustomScoreQuery
				)o;
			if (this.GetBoost() != other.GetBoost() || !this.subQuery.Equals(other.subQuery) 
				|| this.strict != other.strict || this.scoringQueries.Length != other.scoringQueries
				.Length)
			{
				return false;
			}
			return Arrays.Equals(scoringQueries, other.scoringQueries);
		}

		/// <summary>Returns a hash code value for this object.</summary>
		/// <remarks>Returns a hash code value for this object.</remarks>
		public override int GetHashCode()
		{
			return (GetType().GetHashCode() + subQuery.GetHashCode() + Arrays.HashCode(scoringQueries
				)) ^ Sharpen.Runtime.FloatToIntBits(GetBoost()) ^ (strict ? 1234 : 4321);
		}

		/// <summary>
		/// Returns a
		/// <see cref="CustomScoreProvider">CustomScoreProvider</see>
		/// that calculates the custom scores
		/// for the given
		/// <see cref="Org.Apache.Lucene.Index.IndexReader">Org.Apache.Lucene.Index.IndexReader
		/// 	</see>
		/// . The default implementation returns a default
		/// implementation as specified in the docs of
		/// <see cref="CustomScoreProvider">CustomScoreProvider</see>
		/// .
		/// </summary>
		/// <since>2.9.2</since>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual CustomScoreProvider GetCustomScoreProvider(AtomicReaderContext
			 context)
		{
			return new CustomScoreProvider(context);
		}

		private class CustomWeight : Weight
		{
			internal Weight subQueryWeight;

			internal Weight[] valSrcWeights;

			internal bool qStrict;

			internal float queryWeight;

			/// <exception cref="System.IO.IOException"></exception>
			public CustomWeight(CustomScoreQuery _enclosing, IndexSearcher searcher)
			{
				this._enclosing = _enclosing;
				//=========================== W E I G H T ============================
				this.subQueryWeight = this._enclosing.subQuery.CreateWeight(searcher);
				this.valSrcWeights = new Weight[this._enclosing.scoringQueries.Length];
				for (int i = 0; i < this._enclosing.scoringQueries.Length; i++)
				{
					this.valSrcWeights[i] = this._enclosing.scoringQueries[i].CreateWeight(searcher);
				}
				this.qStrict = this._enclosing.strict;
			}

			public override Query GetQuery()
			{
				return this._enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float GetValueForNormalization()
			{
				float sum = this.subQueryWeight.GetValueForNormalization();
				foreach (Weight valSrcWeight in this.valSrcWeights)
				{
					if (this.qStrict)
					{
						valSrcWeight.GetValueForNormalization();
					}
					else
					{
						// do not include ValueSource part in the query normalization
						sum += valSrcWeight.GetValueForNormalization();
					}
				}
				return sum;
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				// note we DONT incorporate our boost, nor pass down any topLevelBoost 
				// (e.g. from outer BQ), as there is no guarantee that the CustomScoreProvider's 
				// function obeys the distributive law... it might call sqrt() on the subQuery score
				// or some other arbitrary function other than multiplication.
				// so, instead boosts are applied directly in score()
				this.subQueryWeight.Normalize(norm, 1f);
				foreach (Weight valSrcWeight in this.valSrcWeights)
				{
					if (this.qStrict)
					{
						valSrcWeight.Normalize(1, 1);
					}
					else
					{
						// do not normalize the ValueSource part
						valSrcWeight.Normalize(norm, 1f);
					}
				}
				this.queryWeight = topLevelBoost * this._enclosing.GetBoost();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Org.Apache.Lucene.Search.Scorer Scorer(AtomicReaderContext context
				, Bits acceptDocs)
			{
				Org.Apache.Lucene.Search.Scorer subQueryScorer = this.subQueryWeight.Scorer(context
					, acceptDocs);
				if (subQueryScorer == null)
				{
					return null;
				}
				Org.Apache.Lucene.Search.Scorer[] valSrcScorers = new Org.Apache.Lucene.Search.Scorer
					[this.valSrcWeights.Length];
				for (int i = 0; i < valSrcScorers.Length; i++)
				{
					valSrcScorers[i] = this.valSrcWeights[i].Scorer(context, acceptDocs);
				}
				return new CustomScoreQuery.CustomScorer(this, this._enclosing.GetCustomScoreProvider
					(context), this, this.queryWeight, subQueryScorer, valSrcScorers);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Explanation Explain(AtomicReaderContext context, int doc)
			{
				Explanation explain = this.DoExplain(context, doc);
				return explain == null ? new Explanation(0.0f, "no matching docs") : explain;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private Explanation DoExplain(AtomicReaderContext info, int doc)
			{
				Explanation subQueryExpl = this.subQueryWeight.Explain(info, doc);
				if (!subQueryExpl.IsMatch())
				{
					return subQueryExpl;
				}
				// match
				Explanation[] valSrcExpls = new Explanation[this.valSrcWeights.Length];
				for (int i = 0; i < this.valSrcWeights.Length; i++)
				{
					valSrcExpls[i] = this.valSrcWeights[i].Explain(info, doc);
				}
				Explanation customExp = this._enclosing.GetCustomScoreProvider(info).CustomExplain
					(doc, subQueryExpl, valSrcExpls);
				float sc = this._enclosing.GetBoost() * customExp.GetValue();
				Explanation res = new ComplexExplanation(true, sc, this._enclosing.ToString() + ", product of:"
					);
				res.AddDetail(customExp);
				res.AddDetail(new Explanation(this._enclosing.GetBoost(), "queryBoost"));
				// actually using the q boost as q weight (== weight value)
				return res;
			}

			public override bool ScoresDocsOutOfOrder()
			{
				return false;
			}

			private readonly CustomScoreQuery _enclosing;
		}

		/// <summary>A scorer that applies a (callback) function on scores of the subQuery.</summary>
		/// <remarks>A scorer that applies a (callback) function on scores of the subQuery.</remarks>
		private class CustomScorer : Scorer
		{
			private readonly float qWeight;

			private readonly Scorer subQueryScorer;

			private readonly Scorer[] valSrcScorers;

			private readonly CustomScoreProvider provider;

			private readonly float[] vScores;

			private CustomScorer(CustomScoreQuery _enclosing, CustomScoreProvider provider, CustomScoreQuery.CustomWeight
				 w, float qWeight, Scorer subQueryScorer, Scorer[] valSrcScorers) : base(w)
			{
				this._enclosing = _enclosing;
				//=========================== S C O R E R ============================
				// reused in score() to avoid allocating this array for each doc
				// constructor
				this.qWeight = qWeight;
				this.subQueryScorer = subQueryScorer;
				this.valSrcScorers = valSrcScorers;
				this.vScores = new float[valSrcScorers.Length];
				this.provider = provider;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				int doc = this.subQueryScorer.NextDoc();
				if (doc != DocIdSetIterator.NO_MORE_DOCS)
				{
					foreach (Scorer valSrcScorer in this.valSrcScorers)
					{
						valSrcScorer.Advance(doc);
					}
				}
				return doc;
			}

			public override int DocID()
			{
				return this.subQueryScorer.DocID();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				for (int i = 0; i < this.valSrcScorers.Length; i++)
				{
					this.vScores[i] = this.valSrcScorers[i].Score();
				}
				return this.qWeight * this.provider.CustomScore(this.subQueryScorer.DocID(), this
					.subQueryScorer.Score(), this.vScores);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return this.subQueryScorer.Freq();
			}

			public override ICollection<Scorer.ChildScorer> GetChildren()
			{
				return Sharpen.Collections.Singleton(new Scorer.ChildScorer(this.subQueryScorer, 
					"CUSTOM"));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				int doc = this.subQueryScorer.Advance(target);
				if (doc != DocIdSetIterator.NO_MORE_DOCS)
				{
					foreach (Scorer valSrcScorer in this.valSrcScorers)
					{
						valSrcScorer.Advance(doc);
					}
				}
				return doc;
			}

			public override long Cost()
			{
				return this.subQueryScorer.Cost();
			}

			private readonly CustomScoreQuery _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			return new CustomScoreQuery.CustomWeight(this, searcher);
		}

		/// <summary>Checks if this is strict custom scoring.</summary>
		/// <remarks>
		/// Checks if this is strict custom scoring.
		/// In strict custom scoring, the
		/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
		/// 	</see>
		/// part does not participate in weight normalization.
		/// This may be useful when one wants full control over how scores are modified, and does
		/// not care about normalizing by the
		/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
		/// 	</see>
		/// part.
		/// One particular case where this is useful if for testing this query.
		/// <P>
		/// Note: only has effect when the
		/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
		/// 	</see>
		/// part is not null.
		/// </remarks>
		public virtual bool IsStrict()
		{
			return strict;
		}

		/// <summary>Set the strict mode of this query.</summary>
		/// <remarks>Set the strict mode of this query.</remarks>
		/// <param name="strict">The strict mode to set.</param>
		/// <seealso cref="IsStrict()">IsStrict()</seealso>
		public virtual void SetStrict(bool strict)
		{
			this.strict = strict;
		}

		/// <summary>The sub-query that CustomScoreQuery wraps, affecting both the score and which documents match.
		/// 	</summary>
		/// <remarks>The sub-query that CustomScoreQuery wraps, affecting both the score and which documents match.
		/// 	</remarks>
		public virtual Query GetSubQuery()
		{
			return subQuery;
		}

		/// <summary>The scoring queries that only affect the score of CustomScoreQuery.</summary>
		/// <remarks>The scoring queries that only affect the score of CustomScoreQuery.</remarks>
		public virtual Query[] GetScoringQueries()
		{
			return scoringQueries;
		}

		/// <summary>
		/// A short name of this query, used in
		/// <see cref="ToString(string)">ToString(string)</see>
		/// .
		/// </summary>
		public virtual string Name()
		{
			return "custom";
		}
	}
}
