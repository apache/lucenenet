/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Queryparser.Surround.Query;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>Factory for creating basic term queries</summary>
	public class BasicQueryFactory
	{
		public BasicQueryFactory(int maxBasicQueries)
		{
			this.maxBasicQueries = maxBasicQueries;
			this.queriesMade = 0;
		}

		public BasicQueryFactory() : this(1024)
		{
		}

		private int maxBasicQueries;

		private int queriesMade;

		public virtual int GetNrQueriesMade()
		{
			return queriesMade;
		}

		public virtual int GetMaxBasicQueries()
		{
			return maxBasicQueries;
		}

		public override string ToString()
		{
			return GetType().FullName + "(maxBasicQueries: " + maxBasicQueries + ", queriesMade: "
				 + queriesMade + ")";
		}

		private bool AtMax()
		{
			return queriesMade >= maxBasicQueries;
		}

		/// <exception cref="Lucene.Net.Queryparser.Surround.Query.TooManyBasicQueries
		/// 	"></exception>
		protected internal virtual void CheckMax()
		{
			lock (this)
			{
				if (AtMax())
				{
					throw new TooManyBasicQueries(GetMaxBasicQueries());
				}
				queriesMade++;
			}
		}

		/// <exception cref="Lucene.Net.Queryparser.Surround.Query.TooManyBasicQueries
		/// 	"></exception>
		public virtual TermQuery NewTermQuery(Term term)
		{
			CheckMax();
			return new TermQuery(term);
		}

		/// <exception cref="Lucene.Net.Queryparser.Surround.Query.TooManyBasicQueries
		/// 	"></exception>
		public virtual SpanTermQuery NewSpanTermQuery(Term term)
		{
			CheckMax();
			return new SpanTermQuery(term);
		}

		public override int GetHashCode()
		{
			return GetType().GetHashCode() ^ (AtMax() ? 7 : 31 * 32);
		}

		/// <summary>
		/// Two BasicQueryFactory's are equal when they generate
		/// the same types of basic queries, or both cannot generate queries anymore.
		/// </summary>
		/// <remarks>
		/// Two BasicQueryFactory's are equal when they generate
		/// the same types of basic queries, or both cannot generate queries anymore.
		/// </remarks>
		public override bool Equals(object obj)
		{
			if (!(obj is Lucene.Net.Queryparser.Surround.Query.BasicQueryFactory))
			{
				return false;
			}
			Lucene.Net.Queryparser.Surround.Query.BasicQueryFactory other = (Lucene.Net.Queryparser.Surround.Query.BasicQueryFactory
				)obj;
			return AtMax() == other.AtMax();
		}
	}
}
