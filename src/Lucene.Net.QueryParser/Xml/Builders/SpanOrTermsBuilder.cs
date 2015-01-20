/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder that analyzes the text into a
	/// <see cref="Lucene.Net.Search.Spans.SpanOrQuery">Lucene.Net.Search.Spans.SpanOrQuery
	/// 	</see>
	/// </summary>
	public class SpanOrTermsBuilder : SpanBuilderBase
	{
		private readonly Analyzer analyzer;

		public SpanOrTermsBuilder(Analyzer analyzer)
		{
			this.analyzer = analyzer;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public override SpanQuery GetSpanQuery(Element e)
		{
			string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
			string value = DOMUtils.GetNonBlankTextOrFail(e);
			IList<SpanQuery> clausesList = new AList<SpanQuery>();
			TokenStream ts = null;
			try
			{
				ts = analyzer.TokenStream(fieldName, value);
				TermToBytesRefAttribute termAtt = ts.AddAttribute<TermToBytesRefAttribute>();
				BytesRef bytes = termAtt.GetBytesRef();
				ts.Reset();
				while (ts.IncrementToken())
				{
					termAtt.FillBytesRef();
					SpanTermQuery stq = new SpanTermQuery(new Term(fieldName, BytesRef.DeepCopyOf(bytes
						)));
					clausesList.AddItem(stq);
				}
				ts.End();
				SpanOrQuery soq = new SpanOrQuery(Sharpen.Collections.ToArray(clausesList, new SpanQuery
					[clausesList.Count]));
				soq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
				return soq;
			}
			catch (IOException)
			{
				throw new ParserException("IOException parsing value:" + value);
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(ts);
			}
		}
	}
}
