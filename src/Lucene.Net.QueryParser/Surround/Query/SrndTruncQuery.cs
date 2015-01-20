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
	/// <summary>Query that matches wildcards</summary>
	public class SrndTruncQuery : SimpleTerm
	{
		public SrndTruncQuery(string truncated, char unlimited, char mask) : base(false)
		{
			this.truncated = truncated;
			this.unlimited = unlimited;
			this.mask = mask;
			TruncatedToPrefixAndPattern();
		}

		private readonly string truncated;

		private readonly char unlimited;

		private readonly char mask;

		private string prefix;

		private BytesRef prefixRef;

		private Sharpen.Pattern pattern;

		public virtual string GetTruncated()
		{
			return truncated;
		}

		public override string ToStringUnquoted()
		{
			return GetTruncated();
		}

		protected internal virtual bool MatchingChar(char c)
		{
			return (c != unlimited) && (c != mask);
		}

		protected internal virtual void AppendRegExpForChar(char c, StringBuilder re)
		{
			if (c == unlimited)
			{
				re.Append(".*");
			}
			else
			{
				if (c == mask)
				{
					re.Append(".");
				}
				else
				{
					re.Append(c);
				}
			}
		}

		protected internal virtual void TruncatedToPrefixAndPattern()
		{
			int i = 0;
			while ((i < truncated.Length) && MatchingChar(truncated[i]))
			{
				i++;
			}
			prefix = Sharpen.Runtime.Substring(truncated, 0, i);
			prefixRef = new BytesRef(prefix);
			StringBuilder re = new StringBuilder();
			while (i < truncated.Length)
			{
				AppendRegExpForChar(truncated[i], re);
				i++;
			}
			pattern = Sharpen.Pattern.Compile(re.ToString());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void VisitMatchingTerms(IndexReader reader, string fieldName, SimpleTerm.MatchingTermVisitor
			 mtv)
		{
			int prefixLength = prefix.Length;
			Terms terms = MultiFields.GetTerms(reader, fieldName);
			if (terms != null)
			{
				Matcher matcher = pattern.Matcher(string.Empty);
				try
				{
					TermsEnum termsEnum = terms.Iterator(null);
					TermsEnum.SeekStatus status = termsEnum.SeekCeil(prefixRef);
					BytesRef text;
					if (status == TermsEnum.SeekStatus.FOUND)
					{
						text = prefixRef;
					}
					else
					{
						if (status == TermsEnum.SeekStatus.NOT_FOUND)
						{
							text = termsEnum.Term();
						}
						else
						{
							text = null;
						}
					}
					while (text != null)
					{
						if (text != null && StringHelper.StartsWith(text, prefixRef))
						{
							string textString = text.Utf8ToString();
							matcher.Reset(Sharpen.Runtime.Substring(textString, prefixLength));
							if (matcher.Matches())
							{
								mtv.VisitMatchingTerm(new Term(fieldName, textString));
							}
						}
						else
						{
							break;
						}
						text = termsEnum.Next();
					}
				}
				finally
				{
					matcher.Reset();
				}
			}
		}
	}
}
