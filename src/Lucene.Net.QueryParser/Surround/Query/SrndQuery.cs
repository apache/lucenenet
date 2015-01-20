/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Lucene.Net.Queryparser.Surround.Query;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
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

		public virtual Lucene.Net.Search.Query MakeLuceneQueryField(string fieldName
			, BasicQueryFactory qf)
		{
			Lucene.Net.Search.Query q = MakeLuceneQueryFieldNoBoost(fieldName, qf);
			if (IsWeighted())
			{
				q.SetBoost(GetWeight() * q.GetBoost());
			}
			return q;
		}

		public abstract Lucene.Net.Search.Query MakeLuceneQueryFieldNoBoost(string
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

		public virtual Lucene.Net.Queryparser.Surround.Query.SrndQuery Clone()
		{
			return (Lucene.Net.Queryparser.Surround.Query.SrndQuery)base.Clone();
		}

		/// <summary>
		/// For subclasses of
		/// <see cref="SrndQuery">SrndQuery</see>
		/// within the package
		/// <see cref="Lucene.Net.Queryparser.Surround.Query">Lucene.Net.Queryparser.Surround.Query
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
		/// <see cref="Lucene.Net.Queryparser.Surround.Query">Lucene.Net.Queryparser.Surround.Query
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

			public override void Add(Lucene.Net.Search.Query query, BooleanClause.Occur
				 occur)
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>An empty Lucene query</summary>
		public static readonly Lucene.Net.Search.Query theEmptyLcnQuery = new _BooleanQuery_99
			();
	}
}
