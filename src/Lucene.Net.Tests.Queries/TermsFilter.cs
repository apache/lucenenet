/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>Constructs a filter for docs matching any of the terms added to this class.
	/// 	</summary>
	/// <remarks>
	/// Constructs a filter for docs matching any of the terms added to this class.
	/// Unlike a RangeFilter this can be used for filtering on multiple terms that are not necessarily in
	/// a sequence. An example might be a collection of primary keys from a database query result or perhaps
	/// a choice of "category" labels picked by the end user. As a filter, this is much faster than the
	/// equivalent query (a BooleanQuery with many "should" TermQueries)
	/// </remarks>
	public sealed class TermsFilter : Filter
	{
		private readonly int[] offsets;

		private readonly byte[] termsBytes;

		private readonly TermsFilter.TermsAndField[] termsAndFields;

		private readonly int hashCode;

		private const int PRIME = 31;

		/// <summary>
		/// Creates a new
		/// <see cref="TermsFilter">TermsFilter</see>
		/// from the given list. The list
		/// can contain duplicate terms and multiple fields.
		/// </summary>
		public TermsFilter(IList<Term> terms) : this(new _FieldAndTermEnum_66(terms), terms
			.Count)
		{
		}

		private sealed class _FieldAndTermEnum_66 : TermsFilter.FieldAndTermEnum
		{
			public _FieldAndTermEnum_66(IList<Term> terms)
			{
				this.terms = terms;
				this.iter = Org.Apache.Lucene.Queries.TermsFilter.Sort(terms).Iterator();
			}

			internal readonly Iterator<Term> iter;

			// cached hashcode for fast cache lookups
			// we need to sort for deduplication and to have a common cache key
			public override BytesRef Next()
			{
				if (this.iter.HasNext())
				{
					Term next = this.iter.Next();
					this.field = next.Field();
					return next.Bytes();
				}
				return null;
			}

			private readonly IList<Term> terms;
		}

		/// <summary>
		/// Creates a new
		/// <see cref="TermsFilter">TermsFilter</see>
		/// from the given
		/// <see cref="Org.Apache.Lucene.Util.BytesRef">Org.Apache.Lucene.Util.BytesRef</see>
		/// list for
		/// a single field.
		/// </summary>
		public TermsFilter(string field, IList<BytesRef> terms) : this(new _FieldAndTermEnum_85
			(terms, field), terms.Count)
		{
		}

		private sealed class _FieldAndTermEnum_85 : TermsFilter.FieldAndTermEnum
		{
			public _FieldAndTermEnum_85(IList<BytesRef> terms, string baseArg1) : base(baseArg1
				)
			{
				this.terms = terms;
				this.iter = Org.Apache.Lucene.Queries.TermsFilter.Sort(terms).Iterator();
			}

			internal readonly Iterator<BytesRef> iter;

			// we need to sort for deduplication and to have a common cache key
			public override BytesRef Next()
			{
				if (this.iter.HasNext())
				{
					return this.iter.Next();
				}
				return null;
			}

			private readonly IList<BytesRef> terms;
		}

		/// <summary>
		/// Creates a new
		/// <see cref="TermsFilter">TermsFilter</see>
		/// from the given
		/// <see cref="Org.Apache.Lucene.Util.BytesRef">Org.Apache.Lucene.Util.BytesRef</see>
		/// array for
		/// a single field.
		/// </summary>
		public TermsFilter(string field, params BytesRef[] terms) : this(field, Arrays.AsList
			(terms))
		{
		}

		/// <summary>
		/// Creates a new
		/// <see cref="TermsFilter">TermsFilter</see>
		/// from the given array. The array can
		/// contain duplicate terms and multiple fields.
		/// </summary>
		public TermsFilter(params Term[] terms) : this(Arrays.AsList(terms))
		{
		}

		private TermsFilter(TermsFilter.FieldAndTermEnum iter, int length)
		{
			// this ctor prevents unnecessary Term creations
			// TODO: maybe use oal.index.PrefixCodedTerms instead?
			// If number of terms is more than a few hundred it
			// should be a win
			// TODO: we also pack terms in FieldCache/DocValues
			// ... maybe we can refactor to share that code
			// TODO: yet another option is to build the union of the terms in
			// an automaton an call intersect on the termsenum if the density is high
			int hash = 9;
			byte[] serializedTerms = new byte[0];
			this.offsets = new int[length + 1];
			int lastEndOffset = 0;
			int index = 0;
			AList<TermsFilter.TermsAndField> termsAndFields = new AList<TermsFilter.TermsAndField
				>();
			TermsFilter.TermsAndField lastTermsAndField = null;
			BytesRef previousTerm = null;
			string previousField = null;
			BytesRef currentTerm;
			string currentField;
			while ((currentTerm = iter.Next()) != null)
			{
				currentField = iter.Field();
				if (currentField == null)
				{
					throw new ArgumentException("Field must not be null");
				}
				if (previousField != null)
				{
					// deduplicate
					if (previousField.Equals(currentField))
					{
						if (previousTerm.BytesEquals(currentTerm))
						{
							continue;
						}
					}
					else
					{
						int start = lastTermsAndField == null ? 0 : lastTermsAndField.end;
						lastTermsAndField = new TermsFilter.TermsAndField(start, index, previousField);
						termsAndFields.AddItem(lastTermsAndField);
					}
				}
				hash = PRIME * hash + currentField.GetHashCode();
				hash = PRIME * hash + currentTerm.GetHashCode();
				if (serializedTerms.Length < lastEndOffset + currentTerm.length)
				{
					serializedTerms = ArrayUtil.Grow(serializedTerms, lastEndOffset + currentTerm.length
						);
				}
				System.Array.Copy(currentTerm.bytes, currentTerm.offset, serializedTerms, lastEndOffset
					, currentTerm.length);
				offsets[index] = lastEndOffset;
				lastEndOffset += currentTerm.length;
				index++;
				previousTerm = currentTerm;
				previousField = currentField;
			}
			offsets[index] = lastEndOffset;
			int start_1 = lastTermsAndField == null ? 0 : lastTermsAndField.end;
			lastTermsAndField = new TermsFilter.TermsAndField(start_1, index, previousField);
			termsAndFields.AddItem(lastTermsAndField);
			this.termsBytes = ArrayUtil.Shrink(serializedTerms, lastEndOffset);
			this.termsAndFields = Sharpen.Collections.ToArray(termsAndFields, new TermsFilter.TermsAndField
				[termsAndFields.Count]);
			this.hashCode = hash;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			AtomicReader reader = ((AtomicReader)context.Reader());
			FixedBitSet result = null;
			// lazy init if needed - no need to create a big bitset ahead of time
			Fields fields = reader.Fields();
			BytesRef spare = new BytesRef(this.termsBytes);
			if (fields == null)
			{
				return result;
			}
			Terms terms = null;
			TermsEnum termsEnum = null;
			DocsEnum docs = null;
			foreach (TermsFilter.TermsAndField termsAndField in this.termsAndFields)
			{
				if ((terms = fields.Terms(termsAndField.field)) != null)
				{
					termsEnum = terms.Iterator(termsEnum);
					// this won't return null
					for (int i = termsAndField.start; i < termsAndField.end; i++)
					{
						spare.offset = offsets[i];
						spare.length = offsets[i + 1] - offsets[i];
						if (termsEnum.SeekExact(spare))
						{
							docs = termsEnum.Docs(acceptDocs, docs, DocsEnum.FLAG_NONE);
							// no freq since we don't need them
							if (result == null)
							{
								if (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
								{
									result = new FixedBitSet(reader.MaxDoc());
									// lazy init but don't do it in the hot loop since we could read many docs
									result.Set(docs.DocID());
								}
							}
							while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
							{
								result.Set(docs.DocID());
							}
						}
					}
				}
			}
			return result;
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
			Org.Apache.Lucene.Queries.TermsFilter test = (Org.Apache.Lucene.Queries.TermsFilter
				)obj;
			// first check the fields before even comparing the bytes
			if (test.hashCode == hashCode && Arrays.Equals(termsAndFields, test.termsAndFields
				))
			{
				int lastOffset = termsAndFields[termsAndFields.Length - 1].end;
				// compare offsets since we sort they must be identical
				if (ArrayUtil.Equals(offsets, 0, test.offsets, 0, lastOffset + 1))
				{
					// straight byte comparison since we sort they must be identical
					return ArrayUtil.Equals(termsBytes, 0, test.termsBytes, 0, offsets[lastOffset]);
				}
			}
			return false;
		}

		public override int GetHashCode()
		{
			return hashCode;
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			BytesRef spare = new BytesRef(termsBytes);
			bool first = true;
			for (int i = 0; i < termsAndFields.Length; i++)
			{
				TermsFilter.TermsAndField current = termsAndFields[i];
				for (int j = current.start; j < current.end; j++)
				{
					spare.offset = offsets[j];
					spare.length = offsets[j + 1] - offsets[j];
					if (!first)
					{
						builder.Append(' ');
					}
					first = false;
					builder.Append(current.field).Append(':');
					builder.Append(spare.Utf8ToString());
				}
			}
			return builder.ToString();
		}

		private sealed class TermsAndField
		{
			internal readonly int start;

			internal readonly int end;

			internal readonly string field;

			internal TermsAndField(int start, int end, string field) : base()
			{
				this.start = start;
				this.end = end;
				this.field = field;
			}

			public override int GetHashCode()
			{
				int prime = 31;
				int result = 1;
				result = prime * result + ((field == null) ? 0 : field.GetHashCode());
				result = prime * result + end;
				result = prime * result + start;
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
				TermsFilter.TermsAndField other = (TermsFilter.TermsAndField)obj;
				if (field == null)
				{
					if (other.field != null)
					{
						return false;
					}
				}
				else
				{
					if (!field.Equals(other.field))
					{
						return false;
					}
				}
				if (end != other.end)
				{
					return false;
				}
				if (start != other.start)
				{
					return false;
				}
				return true;
			}
		}

		private abstract class FieldAndTermEnum
		{
			protected internal string field;

			public abstract BytesRef Next();

			public FieldAndTermEnum()
			{
			}

			public FieldAndTermEnum(string field)
			{
				this.field = field;
			}

			public virtual string Field()
			{
				return field;
			}
		}

		private static IList<T> Sort<T>(IList<T> toSort) where T:Comparable<T>
		{
			if (toSort.IsEmpty())
			{
				throw new ArgumentException("no terms provided");
			}
			toSort.Sort();
			return toSort;
		}
	}
}
