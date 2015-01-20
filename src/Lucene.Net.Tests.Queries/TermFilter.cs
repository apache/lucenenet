/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>A filter that includes documents that match with a specific term.</summary>
	/// <remarks>A filter that includes documents that match with a specific term.</remarks>
	public sealed class TermFilter : Filter
	{
		private readonly Term term;

		/// <param name="term">The term documents need to have in order to be a match for this filter.
		/// 	</param>
		public TermFilter(Term term)
		{
			if (term == null)
			{
				throw new ArgumentException("Term must not be null");
			}
			else
			{
				if (term.Field() == null)
				{
					throw new ArgumentException("Field must not be null");
				}
			}
			this.term = term;
		}

		/// <returns>The term this filter includes documents with.</returns>
		public Term GetTerm()
		{
			return term;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			Terms terms = ((AtomicReader)context.Reader()).Terms(term.Field());
			if (terms == null)
			{
				return null;
			}
			TermsEnum termsEnum = terms.Iterator(null);
			if (!termsEnum.SeekExact(term.Bytes()))
			{
				return null;
			}
			return new _DocIdSet_69(termsEnum, acceptDocs);
		}

		private sealed class _DocIdSet_69 : DocIdSet
		{
			public _DocIdSet_69(TermsEnum termsEnum, Bits acceptDocs)
			{
				this.termsEnum = termsEnum;
				this.acceptDocs = acceptDocs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocIdSetIterator Iterator()
			{
				return termsEnum.Docs(acceptDocs, null, DocsEnum.FLAG_NONE);
			}

			private readonly TermsEnum termsEnum;

			private readonly Bits acceptDocs;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o == null || GetType() != o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.TermFilter that = (Org.Apache.Lucene.Queries.TermFilter
				)o;
			if (term != null ? !term.Equals(that.term) : that.term != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return term != null ? term.GetHashCode() : 0;
		}

		public override string ToString()
		{
			return term.Field() + ":" + term.Text();
		}
	}
}
