/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Queryparser.Surround.Query;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>Query that matches String prefixes</summary>
	public class SrndPrefixQuery : SimpleTerm
	{
		private readonly BytesRef prefixRef;

		public SrndPrefixQuery(string prefix, bool quoted, char truncator) : base(quoted)
		{
			this.prefix = prefix;
			prefixRef = new BytesRef(prefix);
			this.truncator = truncator;
		}

		private readonly string prefix;

		public virtual string GetPrefix()
		{
			return prefix;
		}

		private readonly char truncator;

		public virtual char GetSuffixOperator()
		{
			return truncator;
		}

		public virtual Term GetLucenePrefixTerm(string fieldName)
		{
			return new Term(fieldName, GetPrefix());
		}

		public override string ToStringUnquoted()
		{
			return GetPrefix();
		}

		protected internal override void SuffixToString(StringBuilder r)
		{
			r.Append(GetSuffixOperator());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void VisitMatchingTerms(IndexReader reader, string fieldName, SimpleTerm.MatchingTermVisitor
			 mtv)
		{
			Terms terms = MultiFields.GetTerms(reader, fieldName);
			if (terms != null)
			{
				TermsEnum termsEnum = terms.Iterator(null);
				bool skip = false;
				TermsEnum.SeekStatus status = termsEnum.SeekCeil(new BytesRef(GetPrefix()));
				if (status == TermsEnum.SeekStatus.FOUND)
				{
					mtv.VisitMatchingTerm(GetLucenePrefixTerm(fieldName));
				}
				else
				{
					if (status == TermsEnum.SeekStatus.NOT_FOUND)
					{
						if (StringHelper.StartsWith(termsEnum.Term(), prefixRef))
						{
							mtv.VisitMatchingTerm(new Term(fieldName, termsEnum.Term().Utf8ToString()));
						}
						else
						{
							skip = true;
						}
					}
					else
					{
						// EOF
						skip = true;
					}
				}
				if (!skip)
				{
					while (true)
					{
						BytesRef text = termsEnum.Next();
						if (text != null && StringHelper.StartsWith(text, prefixRef))
						{
							mtv.VisitMatchingTerm(new Term(fieldName, text.Utf8ToString()));
						}
						else
						{
							break;
						}
					}
				}
			}
		}
	}
}
