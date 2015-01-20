/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>
	/// A Filter that wrapped with an indication of how that filter
	/// is used when composed with another filter.
	/// </summary>
	/// <remarks>
	/// A Filter that wrapped with an indication of how that filter
	/// is used when composed with another filter.
	/// (Follows the boolean logic in BooleanClause for composition
	/// of queries.)
	/// </remarks>
	public sealed class FilterClause
	{
		private readonly BooleanClause.Occur occur;

		private readonly Filter filter;

		/// <summary>Create a new FilterClause</summary>
		/// <param name="filter">A Filter object containing a BitSet</param>
		/// <param name="occur">A parameter implementation indicating SHOULD, MUST or MUST NOT
		/// 	</param>
		public FilterClause(Filter filter, BooleanClause.Occur occur)
		{
			this.occur = occur;
			this.filter = filter;
		}

		/// <summary>Returns this FilterClause's filter</summary>
		/// <returns>A Filter object</returns>
		public Filter GetFilter()
		{
			return filter;
		}

		/// <summary>Returns this FilterClause's occur parameter</summary>
		/// <returns>An Occur object</returns>
		public BooleanClause.Occur GetOccur()
		{
			return occur;
		}

		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (o == null || !(o is Org.Apache.Lucene.Queries.FilterClause))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.FilterClause other = (Org.Apache.Lucene.Queries.FilterClause
				)o;
			return this.filter.Equals(other.filter) && this.occur == other.occur;
		}

		public override int GetHashCode()
		{
			return filter.GetHashCode() ^ occur.GetHashCode();
		}

		public override string ToString()
		{
			return occur.ToString() + filter.ToString();
		}
	}
}
