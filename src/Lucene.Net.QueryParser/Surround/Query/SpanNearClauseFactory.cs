/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queryparser.Surround.Query;
using Lucene.Net.Search.Spans;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>
	/// Factory for
	/// <see cref="Lucene.Net.Search.Spans.SpanOrQuery">Lucene.Net.Search.Spans.SpanOrQuery
	/// 	</see>
	/// </summary>
	public class SpanNearClauseFactory
	{
		public SpanNearClauseFactory(IndexReader reader, string fieldName, BasicQueryFactory
			 qf)
		{
			// FIXME: rename to SpanClauseFactory
			this.reader = reader;
			this.fieldName = fieldName;
			this.weightBySpanQuery = new Dictionary<SpanQuery, float>();
			this.qf = qf;
		}

		private IndexReader reader;

		private string fieldName;

		private Dictionary<SpanQuery, float> weightBySpanQuery;

		private BasicQueryFactory qf;

		public virtual IndexReader GetIndexReader()
		{
			return reader;
		}

		public virtual string GetFieldName()
		{
			return fieldName;
		}

		public virtual BasicQueryFactory GetBasicQueryFactory()
		{
			return qf;
		}

		public virtual int Size()
		{
			return weightBySpanQuery.Count;
		}

		public virtual void Clear()
		{
			weightBySpanQuery.Clear();
		}

		protected internal virtual void AddSpanQueryWeighted(SpanQuery sq, float weight)
		{
			float w = weightBySpanQuery.Get(sq);
			if (w != null)
			{
				w = float.ValueOf(w + weight);
			}
			else
			{
				w = float.ValueOf(weight);
			}
			weightBySpanQuery.Put(sq, w);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AddTermWeighted(Term t, float weight)
		{
			SpanTermQuery stq = qf.NewSpanTermQuery(t);
			AddSpanQueryWeighted(stq, weight);
		}

		public virtual void AddSpanQuery(Lucene.Net.Search.Query q)
		{
			if (q == SrndQuery.theEmptyLcnQuery)
			{
				return;
			}
			if (!(q is SpanQuery))
			{
				throw new Exception("Expected SpanQuery: " + q.ToString(GetFieldName()));
			}
			AddSpanQueryWeighted((SpanQuery)q, q.GetBoost());
		}

		public virtual SpanQuery MakeSpanClause()
		{
			SpanQuery[] spanQueries = new SpanQuery[Size()];
			Iterator<SpanQuery> sqi = weightBySpanQuery.Keys.Iterator();
			int i = 0;
			while (sqi.HasNext())
			{
				SpanQuery sq = sqi.Next();
				sq.SetBoost(weightBySpanQuery.Get(sq));
				spanQueries[i++] = sq;
			}
			if (spanQueries.Length == 1)
			{
				return spanQueries[0];
			}
			else
			{
				return new SpanOrQuery(spanQueries);
			}
		}
	}
}
