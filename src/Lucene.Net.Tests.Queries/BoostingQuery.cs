/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>The BoostingQuery class can be used to effectively demote results that match a given query.
	/// 	</summary>
	/// <remarks>
	/// The BoostingQuery class can be used to effectively demote results that match a given query.
	/// Unlike the "NOT" clause, this still selects documents that contain undesirable terms,
	/// but reduces their overall score:
	/// Query balancedQuery = new BoostingQuery(positiveQuery, negativeQuery, 0.01f);
	/// In this scenario the positiveQuery contains the mandatory, desirable criteria which is used to
	/// select all matching documents, and the negativeQuery contains the undesirable elements which
	/// are simply used to lessen the scores. Documents that match the negativeQuery have their score
	/// multiplied by the supplied "boost" parameter, so this should be less than 1 to achieve a
	/// demoting effect
	/// This code was originally made available here: [WWW] http://marc.theaimsgroup.com/?l=lucene-user&m=108058407130459&w=2
	/// and is documented here: http://wiki.apache.org/lucene-java/CommunityContributions
	/// </remarks>
	public class BoostingQuery : Query
	{
		private readonly float boost;

		private readonly Query match;

		private readonly Query context;

		public BoostingQuery(Query match, Query context, float boost)
		{
			// the amount to boost by
			// query to match
			// boost when matches too
			this.match = match;
			this.context = context.Clone();
			// clone before boost
			this.boost = boost;
			this.context.SetBoost(0.0f);
		}

		// ignore context-only matches
		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			BooleanQuery result = new _BooleanQuery_54(this);
			// matched only one clause
			// use the score as-is
			// matched both clauses
			// multiply by boost
			result.Add(match, BooleanClause.Occur.MUST);
			result.Add(context, BooleanClause.Occur.SHOULD);
			return result;
		}

		private sealed class _BooleanQuery_54 : BooleanQuery
		{
			public _BooleanQuery_54(BoostingQuery _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Weight CreateWeight(IndexSearcher searcher)
			{
				return new _BooleanWeight_57(this, searcher, false);
			}

			private sealed class _BooleanWeight_57 : BooleanQuery.BooleanWeight
			{
				public _BooleanWeight_57(_BooleanQuery_54 _enclosing, IndexSearcher baseArg1, bool
					 baseArg2) : base(_enclosing, baseArg1, baseArg2)
				{
					this._enclosing = _enclosing;
				}

				public override float Coord(int overlap, int max)
				{
					switch (overlap)
					{
						case 1:
						{
							return 1.0f;
						}

						case 2:
						{
							return this._enclosing._enclosing.boost;
						}

						default:
						{
							return 0.0f;
							break;
						}
					}
				}

				private readonly _BooleanQuery_54 _enclosing;
			}

			private readonly BoostingQuery _enclosing;
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + Sharpen.Runtime.FloatToIntBits(boost);
			result = prime * result + ((context == null) ? 0 : context.GetHashCode());
			result = prime * result + ((match == null) ? 0 : match.GetHashCode());
			return result;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (obj == null)
			{
				return false;
			}
			if (GetType() != obj.GetType())
			{
				return false;
			}
			if (!base.Equals(obj))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.BoostingQuery other = (Org.Apache.Lucene.Queries.BoostingQuery
				)obj;
			if (Sharpen.Runtime.FloatToIntBits(boost) != Sharpen.Runtime.FloatToIntBits(other
				.boost))
			{
				return false;
			}
			if (context == null)
			{
				if (other.context != null)
				{
					return false;
				}
			}
			else
			{
				if (!context.Equals(other.context))
				{
					return false;
				}
			}
			if (match == null)
			{
				if (other.match != null)
				{
					return false;
				}
			}
			else
			{
				if (!match.Equals(other.match))
				{
					return false;
				}
			}
			return true;
		}

		public override string ToString(string field)
		{
			return match.ToString(field) + "/" + context.ToString(field);
		}
	}
}
