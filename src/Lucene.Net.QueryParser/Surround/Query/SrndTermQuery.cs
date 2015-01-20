/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Queryparser.Surround.Query;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>Simple single-term clause</summary>
	public class SrndTermQuery : SimpleTerm
	{
		public SrndTermQuery(string termText, bool quoted) : base(quoted)
		{
			this.termText = termText;
		}

		private readonly string termText;

		public virtual string GetTermText()
		{
			return termText;
		}

		public virtual Term GetLuceneTerm(string fieldName)
		{
			return new Term(fieldName, GetTermText());
		}

		public override string ToStringUnquoted()
		{
			return GetTermText();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void VisitMatchingTerms(IndexReader reader, string fieldName, SimpleTerm.MatchingTermVisitor
			 mtv)
		{
			Terms terms = MultiFields.GetTerms(reader, fieldName);
			if (terms != null)
			{
				TermsEnum termsEnum = terms.Iterator(null);
				TermsEnum.SeekStatus status = termsEnum.SeekCeil(new BytesRef(GetTermText()));
				if (status == TermsEnum.SeekStatus.FOUND)
				{
					mtv.VisitMatchingTerm(GetLuceneTerm(fieldName));
				}
			}
		}
	}
}
