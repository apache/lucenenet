/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Sandbox.Queries.FuzzyLikeThisQuery">Lucene.Net.Sandbox.Queries.FuzzyLikeThisQuery
	/// 	</see>
	/// </summary>
	public class FuzzyLikeThisQueryBuilder : QueryBuilder
	{
		private const int DEFAULT_MAX_NUM_TERMS = 50;

		private const float DEFAULT_MIN_SIMILARITY = SlowFuzzyQuery.defaultMinSimilarity;

		private const int DEFAULT_PREFIX_LENGTH = 1;

		private const bool DEFAULT_IGNORE_TF = false;

		private readonly Analyzer analyzer;

		public FuzzyLikeThisQueryBuilder(Analyzer analyzer)
		{
			this.analyzer = analyzer;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			NodeList nl = e.GetElementsByTagName("Field");
			int maxNumTerms = DOMUtils.GetAttribute(e, "maxNumTerms", DEFAULT_MAX_NUM_TERMS);
			FuzzyLikeThisQuery fbq = new FuzzyLikeThisQuery(maxNumTerms, analyzer);
			fbq.SetIgnoreTF(DOMUtils.GetAttribute(e, "ignoreTF", DEFAULT_IGNORE_TF));
			for (int i = 0; i < nl.GetLength(); i++)
			{
				Element fieldElem = (Element)nl.Item(i);
				float minSimilarity = DOMUtils.GetAttribute(fieldElem, "minSimilarity", DEFAULT_MIN_SIMILARITY
					);
				int prefixLength = DOMUtils.GetAttribute(fieldElem, "prefixLength", DEFAULT_PREFIX_LENGTH
					);
				string fieldName = DOMUtils.GetAttributeWithInheritance(fieldElem, "fieldName");
				string value = DOMUtils.GetText(fieldElem);
				fbq.AddTerms(value, fieldName, minSimilarity, prefixLength);
			}
			fbq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return fbq;
		}
	}
}
