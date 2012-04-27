using System;
using Lucene.Net.Index;

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
			return t != null && t.Field() == fieldName ? t : null;
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
			return (t.Text().Equals(value)) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
		}
	}
}
