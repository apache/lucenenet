/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function
{
	/// <summary>
	/// Returns a score for each document based on a ValueSource,
	/// often some function of the value of a field.
	/// </summary>
	/// <remarks>
	/// Returns a score for each document based on a ValueSource,
	/// often some function of the value of a field.
	/// <b>Note: This API is experimental and may change in non backward-compatible ways in the future</b>
	/// </remarks>
	public class FunctionQuery : Query
	{
		internal readonly ValueSource func;

		/// <param name="func">defines the function to be used for scoring</param>
		public FunctionQuery(ValueSource func)
		{
			this.func = func;
		}

		/// <returns>The associated ValueSource</returns>
		public virtual ValueSource GetValueSource()
		{
			return func;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			return this;
		}

		public override void ExtractTerms(ICollection<Term> terms)
		{
		}

		protected internal class FunctionWeight : Weight
		{
			protected internal readonly IndexSearcher searcher;

			protected internal float queryNorm;

			protected internal float queryWeight;

			protected internal readonly IDictionary context;

			/// <exception cref="System.IO.IOException"></exception>
			public FunctionWeight(FunctionQuery _enclosing, IndexSearcher searcher)
			{
				this._enclosing = _enclosing;
				this.searcher = searcher;
				this.context = ValueSource.NewContext(searcher);
				this._enclosing.func.CreateWeight(this.context, searcher);
			}

			public override Query GetQuery()
			{
				return this._enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float GetValueForNormalization()
			{
				this.queryWeight = this._enclosing.GetBoost();
				return this.queryWeight * this.queryWeight;
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				this.queryNorm = norm * topLevelBoost;
				this.queryWeight *= this.queryNorm;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Org.Apache.Lucene.Search.Scorer Scorer(AtomicReaderContext context
				, Bits acceptDocs)
			{
				return new FunctionQuery.AllScorer(this, context, acceptDocs, this, this.queryWeight
					);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Explanation Explain(AtomicReaderContext context, int doc)
			{
				return ((FunctionQuery.AllScorer)this.Scorer(context, ((AtomicReader)context.Reader
					()).GetLiveDocs())).Explain(doc);
			}

			private readonly FunctionQuery _enclosing;
		}

		protected internal class AllScorer : Scorer
		{
			internal readonly IndexReader reader;

			internal readonly FunctionQuery.FunctionWeight weight;

			internal readonly int maxDoc;

			internal readonly float qWeight;

			internal int doc = -1;

			internal readonly FunctionValues vals;

			internal readonly Bits acceptDocs;

			/// <exception cref="System.IO.IOException"></exception>
			public AllScorer(FunctionQuery _enclosing, AtomicReaderContext context, Bits acceptDocs
				, FunctionQuery.FunctionWeight w, float qWeight) : base(w)
			{
				this._enclosing = _enclosing;
				this.weight = w;
				this.qWeight = qWeight;
				this.reader = ((AtomicReader)context.Reader());
				this.maxDoc = this.reader.MaxDoc();
				this.acceptDocs = acceptDocs;
				this.vals = this._enclosing.func.GetValues(this.weight.context, context);
			}

			public override int DocID()
			{
				return this.doc;
			}

			// instead of matching all docs, we could also embed a query.
			// the score could either ignore the subscore, or boost it.
			// Containment:  floatline(foo:myTerm, "myFloatField", 1.0, 0.0f)
			// Boost:        foo:myTerm^floatline("myFloatField",1.0,0.0f)
			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				for (; ; )
				{
					++this.doc;
					if (this.doc >= this.maxDoc)
					{
						return this.doc = DocIdSetIterator.NO_MORE_DOCS;
					}
					if (this.acceptDocs != null && !this.acceptDocs.Get(this.doc))
					{
						continue;
					}
					return this.doc;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// this will work even if target==NO_MORE_DOCS
				this.doc = target - 1;
				return this.NextDoc();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				float score = this.qWeight * this.vals.FloatVal(this.doc);
				// Current Lucene priority queues can't handle NaN and -Infinity, so
				// map to -Float.MAX_VALUE. This conditional handles both -infinity
				// and NaN since comparisons with NaN are always false.
				return score > float.NegativeInfinity ? score : -float.MaxValue;
			}

			public override long Cost()
			{
				return this.maxDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return 1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual Explanation Explain(int doc)
			{
				float sc = this.qWeight * this.vals.FloatVal(doc);
				Explanation result = new ComplexExplanation(true, sc, "FunctionQuery(" + this._enclosing
					.func + "), product of:");
				result.AddDetail(this.vals.Explain(doc));
				result.AddDetail(new Explanation(this._enclosing.GetBoost(), "boost"));
				result.AddDetail(new Explanation(this.weight.queryNorm, "queryNorm"));
				return result;
			}

			private readonly FunctionQuery _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			return new FunctionQuery.FunctionWeight(this, searcher);
		}

		/// <summary>Prints a user-readable version of this query.</summary>
		/// <remarks>Prints a user-readable version of this query.</remarks>
		public override string ToString(string field)
		{
			float boost = GetBoost();
			return (boost != 1.0 ? "(" : string.Empty) + func.ToString() + (boost == 1.0 ? string.Empty
				 : ")^" + boost);
		}

		/// <summary>Returns true if <code>o</code> is equal to this.</summary>
		/// <remarks>Returns true if <code>o</code> is equal to this.</remarks>
		public override bool Equals(object o)
		{
			if (!typeof(FunctionQuery).IsInstanceOfType(o))
			{
				return false;
			}
			FunctionQuery other = (FunctionQuery)o;
			return this.GetBoost() == other.GetBoost() && this.func.Equals(other.func);
		}

		/// <summary>Returns a hash code value for this object.</summary>
		/// <remarks>Returns a hash code value for this object.</remarks>
		public override int GetHashCode()
		{
			return func.GetHashCode() * 31 + Sharpen.Runtime.FloatToIntBits(GetBoost());
		}
	}
}
