/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>Builds a BooleanQuery from all of the terms found in the XML element using the choice of analyzer
	/// 	</summary>
	public class TermsQueryBuilder : QueryBuilder
	{
		private readonly Analyzer analyzer;

		public TermsQueryBuilder(Analyzer analyzer)
		{
			this.analyzer = analyzer;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
			string text = DOMUtils.GetNonBlankTextOrFail(e);
			BooleanQuery bq = new BooleanQuery(DOMUtils.GetAttribute(e, "disableCoord", false
				));
			bq.SetMinimumNumberShouldMatch(DOMUtils.GetAttribute(e, "minimumNumberShouldMatch"
				, 0));
			TokenStream ts = null;
			try
			{
				ts = analyzer.TokenStream(fieldName, text);
				TermToBytesRefAttribute termAtt = ts.AddAttribute<TermToBytesRefAttribute>();
				Term term = null;
				BytesRef bytes = termAtt.GetBytesRef();
				ts.Reset();
				while (ts.IncrementToken())
				{
					termAtt.FillBytesRef();
					term = new Term(fieldName, BytesRef.DeepCopyOf(bytes));
					bq.Add(new BooleanClause(new TermQuery(term), BooleanClause.Occur.SHOULD));
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
			bq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return bq;
		}
	}
}
