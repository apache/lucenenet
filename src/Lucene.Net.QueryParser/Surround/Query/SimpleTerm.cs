/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Queryparser.Surround.Query;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>Base class for queries that expand to sets of simple terms.</summary>
	/// <remarks>Base class for queries that expand to sets of simple terms.</remarks>
	public abstract class SimpleTerm : SrndQuery, DistanceSubQuery, Comparable<Lucene.Net.Queryparser.Surround.Query.SimpleTerm
		>
	{
		public SimpleTerm(bool q)
		{
			quoted = q;
		}

		private bool quoted;

		internal virtual bool IsQuoted()
		{
			return quoted;
		}

		public virtual string GetQuote()
		{
			return "\"";
		}

		public virtual string GetFieldOperator()
		{
			return "/";
		}

		public abstract string ToStringUnquoted();

		[Obsolete]
		[System.ObsoleteAttribute(@"(March 2011) Not normally used, to be removed from Lucene 4.0. This class implementing Comparable is to be removed at the same time."
			)]
		public virtual int CompareTo(Lucene.Net.Queryparser.Surround.Query.SimpleTerm
			 ost)
		{
			return Sharpen.Runtime.CompareOrdinal(this.ToStringUnquoted(), ost.ToStringUnquoted
				());
		}

		protected internal virtual void SuffixToString(StringBuilder r)
		{
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			if (IsQuoted())
			{
				r.Append(GetQuote());
			}
			r.Append(ToStringUnquoted());
			if (IsQuoted())
			{
				r.Append(GetQuote());
			}
			SuffixToString(r);
			WeightToString(r);
			return r.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public abstract void VisitMatchingTerms(IndexReader reader, string fieldName, SimpleTerm.MatchingTermVisitor
			 mtv);

		/// <summary>
		/// Callback to visit each matching term during "rewrite"
		/// in
		/// <see cref="VisitMatchingTerm(Lucene.Net.Index.Term)">VisitMatchingTerm(Lucene.Net.Index.Term)
		/// 	</see>
		/// </summary>
		public interface MatchingTermVisitor
		{
			/// <exception cref="System.IO.IOException"></exception>
			void VisitMatchingTerm(Term t);
		}

		public virtual string DistanceSubQueryNotAllowed()
		{
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AddSpanQueries(SpanNearClauseFactory sncf)
		{
			VisitMatchingTerms(sncf.GetIndexReader(), sncf.GetFieldName(), new _MatchingTermVisitor_90
				(this, sncf));
		}

		private sealed class _MatchingTermVisitor_90 : SimpleTerm.MatchingTermVisitor
		{
			public _MatchingTermVisitor_90(SimpleTerm _enclosing, SpanNearClauseFactory sncf)
			{
				this._enclosing = _enclosing;
				this.sncf = sncf;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void VisitMatchingTerm(Term term)
			{
				sncf.AddTermWeighted(term, this._enclosing.GetWeight());
			}

			private readonly SimpleTerm _enclosing;

			private readonly SpanNearClauseFactory sncf;
		}

		public override Lucene.Net.Search.Query MakeLuceneQueryFieldNoBoost(string
			 fieldName, BasicQueryFactory qf)
		{
			return new SimpleTermRewriteQuery(this, fieldName, qf);
		}
	}
}
