/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Org.Apache.Lucene.Queryparser.Surround.Query;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Surround.Query
{
	/// <summary>Lowest level base class for surround queries</summary>
	public abstract class SrndQuery : ICloneable
	{
		public SrndQuery()
		{
		}

		private float weight = (float)1.0;

		private bool weighted = false;

		public virtual void SetWeight(float w)
		{
			weight = w;
			weighted = true;
		}

		public virtual bool IsWeighted()
		{
			return weighted;
		}

		public virtual float GetWeight()
		{
			return weight;
		}

		public virtual string GetWeightString()
		{
			return float.ToString(GetWeight());
		}

		public virtual string GetWeightOperator()
		{
			return "^";
		}

		protected internal virtual void WeightToString(StringBuilder r)
		{
			if (IsWeighted())
			{
				r.Append(GetWeightOperator());
				r.Append(GetWeightString());
			}
		}

		public virtual Org.Apache.Lucene.Search.Query MakeLuceneQueryField(string fieldName
			, BasicQueryFactory qf)
		{
			Org.Apache.Lucene.Search.Query q = MakeLuceneQueryFieldNoBoost(fieldName, qf);
			if (IsWeighted())
			{
				q.SetBoost(GetWeight() * q.GetBoost());
			}
			return q;
		}

		public abstract Org.Apache.Lucene.Search.Query MakeLuceneQueryFieldNoBoost(string
			 fieldName, BasicQueryFactory qf);

		/// <summary>
		/// This method is used by
		/// <see cref="GetHashCode()">GetHashCode()</see>
		/// and
		/// <see cref="Equals(object)">Equals(object)</see>
		/// ,
		/// see LUCENE-2945.
		/// </summary>
		public abstract override string ToString();

		public virtual bool IsFieldsSubQueryAcceptable()
		{
			return true;
		}

		public virtual Org.Apache.Lucene.Queryparser.Surround.Query.SrndQuery Clone()
		{
			return (Org.Apache.Lucene.Queryparser.Surround.Query.SrndQuery)base.Clone();
		}

		/// <summary>
		/// For subclasses of
		/// <see cref="SrndQuery">SrndQuery</see>
		/// within the package
		/// <see cref="Org.Apache.Lucene.Queryparser.Surround.Query">Org.Apache.Lucene.Queryparser.Surround.Query
		/// 	</see>
		/// it is not necessary to override this method,
		/// </summary>
		/// <seealso cref="ToString()">ToString()</seealso>
		public override int GetHashCode()
		{
			return GetType().GetHashCode() ^ ToString().GetHashCode();
		}

		/// <summary>
		/// For subclasses of
		/// <see cref="SrndQuery">SrndQuery</see>
		/// within the package
		/// <see cref="Org.Apache.Lucene.Queryparser.Surround.Query">Org.Apache.Lucene.Queryparser.Surround.Query
		/// 	</see>
		/// it is not necessary to override this method,
		/// </summary>
		/// <seealso cref="ToString()">ToString()</seealso>
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
			return ToString().Equals(obj.ToString());
		}

		private sealed class _BooleanQuery_99 : BooleanQuery
		{
			public _BooleanQuery_99()
			{
			}

			public override void SetBoost(float boost)
			{
				throw new NotSupportedException();
			}

			public override void Add(BooleanClause clause)
			{
				throw new NotSupportedException();
			}

			public override void Add(Org.Apache.Lucene.Search.Query query, BooleanClause.Occur
				 occur)
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>An empty Lucene query</summary>
		public static readonly Org.Apache.Lucene.Search.Query theEmptyLcnQuery = new _BooleanQuery_99
			();
	}
}
