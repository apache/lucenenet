/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Queryparser.Surround.Query;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Surround.Query
{
	/// <summary>Base class for composite queries (such as AND/OR/NOT)</summary>
	public abstract class ComposedQuery : SrndQuery
	{
		public ComposedQuery(IList<SrndQuery> qs, bool operatorInfix, string opName)
		{
			Recompose(qs);
			this.operatorInfix = operatorInfix;
			this.opName = opName;
		}

		protected internal virtual void Recompose(IList<SrndQuery> queries)
		{
			if (queries.Count < 2)
			{
				throw new Exception("Too few subqueries");
			}
			this.queries = queries;
		}

		protected internal string opName;

		public virtual string GetOperatorName()
		{
			return opName;
		}

		protected internal IList<SrndQuery> queries;

		public virtual Iterator<SrndQuery> GetSubQueriesIterator()
		{
			return queries.ListIterator();
		}

		public virtual int GetNrSubQueries()
		{
			return queries.Count;
		}

		public virtual SrndQuery GetSubQuery(int qn)
		{
			return queries[qn];
		}

		private bool operatorInfix;

		public virtual bool IsOperatorInfix()
		{
			return operatorInfix;
		}

		public virtual IList<Org.Apache.Lucene.Search.Query> MakeLuceneSubQueriesField(string
			 fn, BasicQueryFactory qf)
		{
			IList<Org.Apache.Lucene.Search.Query> luceneSubQueries = new AList<Org.Apache.Lucene.Search.Query
				>();
			Iterator<SrndQuery> sqi = GetSubQueriesIterator();
			while (sqi.HasNext())
			{
				luceneSubQueries.AddItem((sqi.Next()).MakeLuceneQueryField(fn, qf));
			}
			return luceneSubQueries;
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			if (IsOperatorInfix())
			{
				InfixToString(r);
			}
			else
			{
				PrefixToString(r);
			}
			WeightToString(r);
			return r.ToString();
		}

		protected internal virtual string GetPrefixSeparator()
		{
			return ", ";
		}

		protected internal virtual string GetBracketOpen()
		{
			return "(";
		}

		protected internal virtual string GetBracketClose()
		{
			return ")";
		}

		protected internal virtual void InfixToString(StringBuilder r)
		{
			Iterator<SrndQuery> sqi = GetSubQueriesIterator();
			r.Append(GetBracketOpen());
			if (sqi.HasNext())
			{
				r.Append(sqi.Next().ToString());
				while (sqi.HasNext())
				{
					r.Append(" ");
					r.Append(GetOperatorName());
					r.Append(" ");
					r.Append(sqi.Next().ToString());
				}
			}
			r.Append(GetBracketClose());
		}

		protected internal virtual void PrefixToString(StringBuilder r)
		{
			Iterator<SrndQuery> sqi = GetSubQueriesIterator();
			r.Append(GetOperatorName());
			r.Append(GetBracketOpen());
			if (sqi.HasNext())
			{
				r.Append(sqi.Next().ToString());
				while (sqi.HasNext())
				{
					r.Append(GetPrefixSeparator());
					r.Append(sqi.Next().ToString());
				}
			}
			r.Append(GetBracketClose());
		}

		public override bool IsFieldsSubQueryAcceptable()
		{
			Iterator<SrndQuery> sqi = GetSubQueriesIterator();
			while (sqi.HasNext())
			{
				if ((sqi.Next()).IsFieldsSubQueryAcceptable())
				{
					return true;
				}
			}
			return false;
		}
	}
}
