/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>A container Filter that allows Boolean composition of Filters.</summary>
	/// <remarks>
	/// A container Filter that allows Boolean composition of Filters.
	/// Filters are allocated into one of three logical constructs;
	/// SHOULD, MUST NOT, MUST
	/// The results Filter BitSet is constructed as follows:
	/// SHOULD Filters are OR'd together
	/// The resulting Filter is NOT'd with the NOT Filters
	/// The resulting Filter is AND'd with the MUST Filters
	/// </remarks>
	public class BooleanFilter : Filter, Iterable<FilterClause>
	{
		private readonly IList<FilterClause> clauses = new AList<FilterClause>();

		/// <summary>
		/// Returns the a DocIdSetIterator representing the Boolean composition
		/// of the filters that have been added.
		/// </summary>
		/// <remarks>
		/// Returns the a DocIdSetIterator representing the Boolean composition
		/// of the filters that have been added.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			FixedBitSet res = null;
			AtomicReader reader = ((AtomicReader)context.Reader());
			bool hasShouldClauses = false;
			foreach (FilterClause fc in clauses)
			{
				if (fc.GetOccur() == BooleanClause.Occur.SHOULD)
				{
					hasShouldClauses = true;
					DocIdSetIterator disi = GetDISI(fc.GetFilter(), context);
					if (disi == null)
					{
						continue;
					}
					if (res == null)
					{
						res = new FixedBitSet(reader.MaxDoc());
					}
					res.Or(disi);
				}
			}
			if (hasShouldClauses && res == null)
			{
				return null;
			}
			foreach (FilterClause fc_1 in clauses)
			{
				if (fc_1.GetOccur() == BooleanClause.Occur.MUST_NOT)
				{
					if (res == null)
					{
						!hasShouldClauses = new FixedBitSet(reader.MaxDoc());
						res.Set(0, reader.MaxDoc());
					}
					// NOTE: may set bits on deleted docs
					DocIdSetIterator disi = GetDISI(fc_1.GetFilter(), context);
					if (disi != null)
					{
						res.AndNot(disi);
					}
				}
			}
			foreach (FilterClause fc_2 in clauses)
			{
				if (fc_2.GetOccur() == BooleanClause.Occur.MUST)
				{
					DocIdSetIterator disi = GetDISI(fc_2.GetFilter(), context);
					if (disi == null)
					{
						return null;
					}
					// no documents can match
					if (res == null)
					{
						res = new FixedBitSet(reader.MaxDoc());
						res.Or(disi);
					}
					else
					{
						res.And(disi);
					}
				}
			}
			return BitsFilteredDocIdSet.Wrap(res, acceptDocs);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static DocIdSetIterator GetDISI(Filter filter, AtomicReaderContext context
			)
		{
			// we dont pass acceptDocs, we will filter at the end using an additional filter
			DocIdSet set = filter.GetDocIdSet(context, null);
			return set == null ? null : set.Iterator();
		}

		/// <summary>Adds a new FilterClause to the Boolean Filter container</summary>
		/// <param name="filterClause">A FilterClause object containing a Filter and an Occur parameter
		/// 	</param>
		public virtual void Add(FilterClause filterClause)
		{
			clauses.AddItem(filterClause);
		}

		public void Add(Filter filter, BooleanClause.Occur occur)
		{
			Add(new FilterClause(filter, occur));
		}

		/// <summary>Returns the list of clauses</summary>
		public virtual IList<FilterClause> Clauses()
		{
			return clauses;
		}

		/// <summary>Returns an iterator on the clauses in this query.</summary>
		/// <remarks>
		/// Returns an iterator on the clauses in this query. It implements the
		/// <see cref="Sharpen.Iterable{T}">Sharpen.Iterable&lt;T&gt;</see>
		/// interface to
		/// make it possible to do:
		/// <pre class="prettyprint">for (FilterClause clause : booleanFilter) {}</pre>
		/// </remarks>
		public sealed override Sharpen.Iterator<FilterClause> Iterator()
		{
			return Clauses().Iterator();
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if ((obj == null) || (obj.GetType() != this.GetType()))
			{
				return false;
			}
			BooleanFilter other = (BooleanFilter)obj;
			return clauses.Equals(other.clauses);
		}

		public override int GetHashCode()
		{
			return 657153718 ^ clauses.GetHashCode();
		}

		/// <summary>Prints a user-readable version of this Filter.</summary>
		/// <remarks>Prints a user-readable version of this Filter.</remarks>
		public override string ToString()
		{
			StringBuilder buffer = new StringBuilder("BooleanFilter(");
			int minLen = buffer.Length;
			foreach (FilterClause c in clauses)
			{
				if (buffer.Length > minLen)
				{
					buffer.Append(' ');
				}
				buffer.Append(c);
			}
			return buffer.Append(')').ToString();
		}
	}
}
