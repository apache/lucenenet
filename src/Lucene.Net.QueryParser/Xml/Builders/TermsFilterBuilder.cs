/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Queries;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Queries.TermsFilter">Lucene.Net.Queries.TermsFilter
	/// 	</see>
	/// </summary>
	public class TermsFilterBuilder : FilterBuilder
	{
		private readonly Analyzer analyzer;

		public TermsFilterBuilder(Analyzer analyzer)
		{
			this.analyzer = analyzer;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Filter GetFilter(Element e)
		{
			IList<BytesRef> terms = new AList<BytesRef>();
			string text = DOMUtils.GetNonBlankTextOrFail(e);
			string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
			TokenStream ts = null;
			try
			{
				ts = analyzer.TokenStream(fieldName, text);
				TermToBytesRefAttribute termAtt = ts.AddAttribute<TermToBytesRefAttribute>();
				BytesRef bytes = termAtt.GetBytesRef();
				ts.Reset();
				while (ts.IncrementToken())
				{
					termAtt.FillBytesRef();
					terms.AddItem(BytesRef.DeepCopyOf(bytes));
				}
				ts.End();
			}
			catch (IOException ioe)
			{
				throw new RuntimeException("Error constructing terms from index:" + ioe);
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(ts);
			}
			return new TermsFilter(fieldName, terms);
		}
	}
}
