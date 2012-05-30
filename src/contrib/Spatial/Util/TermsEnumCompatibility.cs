using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// Wraps Lucene 3 TermEnum to make it look like a Lucene 4 TermsEnum
	/// SOLR-2155
	/// @author dsmiley
	/// </summary>
	public class TermsEnumCompatibility
	{
		private readonly IndexReader reader;
		private readonly String fieldName;
		private TermEnum termEnum;
		private bool initialState = true;

		public TermsEnumCompatibility(IndexReader reader, String fieldName)
		{
			this.reader = reader;
			this.fieldName = string.Intern(fieldName);
			this.termEnum = reader.Terms(new Term(this.fieldName));
		}

		public TermEnum GetTermEnum()
		{
			return termEnum;
		}

		public Term Term()
		{
			Term t = termEnum.Term();
			return t != null && t.Field == fieldName ? t : null;
		}

		public Term Next()
		{
			//in Lucene 3, a call to reader.terms(term) is already pre-positioned, you don't call next first
			if (initialState)
			{
				initialState = false;
				return Term();
			}
			else
			{
				return termEnum.Next() ? Term() : null;
			}
		}

		public void Close()
		{
			termEnum.Close();
		}

		public enum SeekStatus
		{
			END,
			FOUND,
			NOT_FOUND
		}

		public SeekStatus Seek(String value)
		{
			termEnum = reader.Terms(new Term(this.fieldName, value));
			Term t = Term();
			if (t == null)
				return SeekStatus.END;
			return (t.Text.Equals(value)) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
		}

		/// <summary>
		/// Seeks to the specified term, if it exists, or to the
		/// next (ceiling) term.  Returns SeekStatus to
		/// indicate whether exact term was found, a different
		/// term was found, or EOF was hit.  The target term may
		/// be before or after the current term.  If this returns
		/// SeekStatus.END, the enum is unpositioned.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public SeekStatus SeekCeil(String value)
		{
			return Seek(value);
		}

		/// <summary>
		/// Returns the number of documents that have at least one
		/// term for this field, or -1 if this measure isn't
		/// stored by the codec.  Note that, just like other term
		/// measures, this measure does not take deleted documents
		/// into account.
		/// </summary>
		/// <returns></returns>
		public int GetDocCount()
		{
			return -1; // TODO find a way to efficiently determine this
		}

		public void Docs(OpenBitSet bits)
		{
			var termDocs = reader.TermDocs(new Term(fieldName, Term().Text));
			while (termDocs.Next())
			{
				bits.FastSet(termDocs.Doc);
			}
		}
	}
}
