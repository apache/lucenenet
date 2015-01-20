/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Join;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Join
{
	/// <summary>A query that has an array of terms from a specific field.</summary>
	/// <remarks>
	/// A query that has an array of terms from a specific field. This query will match documents have one or more terms in
	/// the specified field that match with the terms specified in the array.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	internal class TermsQuery : MultiTermQuery
	{
		private readonly BytesRefHash terms;

		private readonly int[] ords;

		private readonly Query fromQuery;

		/// <param name="field">The field that should contain terms that are specified in the previous parameter
		/// 	</param>
		/// <param name="terms">The terms that matching documents should have. The terms must be sorted by natural order.
		/// 	</param>
		internal TermsQuery(string field, Query fromQuery, BytesRefHash terms) : base(field
			)
		{
			// Used for equals() only
			this.fromQuery = fromQuery;
			this.terms = terms;
			ords = terms.Sort(BytesRef.GetUTF8SortedAsUnicodeComparator());
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
		{
			if (this.terms.Size() == 0)
			{
				return TermsEnum.EMPTY;
			}
			return new TermsQuery.SeekingTermSetTermsEnum(terms.Iterator(null), this.terms, ords
				);
		}

		public override string ToString(string @string)
		{
			return "TermsQuery{" + "field=" + field + '}';
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (!base.Equals(obj))
			{
				return false;
			}
			if (GetType() != obj.GetType())
			{
				return false;
			}
			Lucene.Net.Search.Join.TermsQuery other = (Lucene.Net.Search.Join.TermsQuery
				)obj;
			if (!fromQuery.Equals(other.fromQuery))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result += prime * fromQuery.GetHashCode();
			return result;
		}

		internal class SeekingTermSetTermsEnum : FilteredTermsEnum
		{
			private readonly BytesRefHash terms;

			private readonly int[] ords;

			private readonly int lastElement;

			private readonly BytesRef lastTerm;

			private readonly BytesRef spare = new BytesRef();

			private readonly IComparer<BytesRef> comparator;

			private BytesRef seekTerm;

			private int upto = 0;

			internal SeekingTermSetTermsEnum(TermsEnum tenum, BytesRefHash terms, int[] ords)
				 : base(tenum)
			{
				this.terms = terms;
				this.ords = ords;
				comparator = BytesRef.GetUTF8SortedAsUnicodeComparator();
				lastElement = terms.Size() - 1;
				lastTerm = terms.Get(ords[lastElement], new BytesRef());
				seekTerm = terms.Get(ords[upto], spare);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override BytesRef NextSeekTerm(BytesRef currentTerm)
			{
				BytesRef temp = seekTerm;
				seekTerm = null;
				return temp;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override FilteredTermsEnum.AcceptStatus Accept(BytesRef term)
			{
				if (comparator.Compare(term, lastTerm) > 0)
				{
					return FilteredTermsEnum.AcceptStatus.END;
				}
				BytesRef currentTerm = terms.Get(ords[upto], spare);
				if (comparator.Compare(term, currentTerm) == 0)
				{
					if (upto == lastElement)
					{
						return FilteredTermsEnum.AcceptStatus.YES;
					}
					else
					{
						seekTerm = terms.Get(ords[++upto], spare);
						return FilteredTermsEnum.AcceptStatus.YES_AND_SEEK;
					}
				}
				else
				{
					if (upto == lastElement)
					{
						return FilteredTermsEnum.AcceptStatus.NO;
					}
					else
					{
						// Our current term doesn't match the the given term.
						int cmp;
						do
						{
							// We maybe are behind the given term by more than one step. Keep incrementing till we're the same or higher.
							if (upto == lastElement)
							{
								return FilteredTermsEnum.AcceptStatus.NO;
							}
							// typically the terms dict is a superset of query's terms so it's unusual that we have to skip many of
							// our terms so we don't do a binary search here
							seekTerm = terms.Get(ords[++upto], spare);
						}
						while ((cmp = comparator.Compare(seekTerm, term)) < 0);
						if (cmp == 0)
						{
							if (upto == lastElement)
							{
								return FilteredTermsEnum.AcceptStatus.YES;
							}
							seekTerm = terms.Get(ords[++upto], spare);
							return FilteredTermsEnum.AcceptStatus.YES_AND_SEEK;
						}
						else
						{
							return FilteredTermsEnum.AcceptStatus.NO_AND_SEEK;
						}
					}
				}
			}
		}
	}
}
