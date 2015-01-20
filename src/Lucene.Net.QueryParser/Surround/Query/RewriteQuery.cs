/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Queryparser.Surround.Query;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	internal abstract class RewriteQuery<SQ> : Lucene.Net.Search.Query where SQ:
		SrndQuery
	{
		protected internal readonly SQ srndQuery;

		protected internal readonly string fieldName;

		protected internal readonly BasicQueryFactory qf;

		internal RewriteQuery(SQ srndQuery, string fieldName, BasicQueryFactory qf)
		{
			this.srndQuery = srndQuery;
			this.fieldName = fieldName;
			this.qf = qf;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public abstract override Lucene.Net.Search.Query Rewrite(IndexReader reader
			);

		public override string ToString()
		{
			return ToString(null);
		}

		public override string ToString(string field)
		{
			return GetType().FullName + (field == null ? string.Empty : "(unused: " + field +
				 ")") + "(" + fieldName + ", " + srndQuery.ToString() + ", " + qf.ToString() + ")";
		}

		public override int GetHashCode()
		{
			return GetType().GetHashCode() ^ fieldName.GetHashCode() ^ qf.GetHashCode() ^ srndQuery
				.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (!GetType().Equals(obj.GetType()))
			{
				return false;
			}
			Lucene.Net.Queryparser.Surround.Query.RewriteQuery other = (Lucene.Net.Queryparser.Surround.Query.RewriteQuery
				)obj;
			return fieldName.Equals(other.fieldName) && qf.Equals(other.qf) && srndQuery.Equals
				(other.srndQuery);
		}

		/// <summary>Not supported by this query.</summary>
		/// <remarks>Not supported by this query.</remarks>
		/// <exception cref="System.NotSupportedException">always: clone is not supported.</exception>
		public override Lucene.Net.Search.Query Clone()
		{
			throw new NotSupportedException();
		}
	}
}
