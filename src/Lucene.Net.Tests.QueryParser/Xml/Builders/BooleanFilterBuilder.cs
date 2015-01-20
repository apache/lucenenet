/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queries;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Queries.BooleanFilter">Org.Apache.Lucene.Queries.BooleanFilter
	/// 	</see>
	/// </summary>
	public class BooleanFilterBuilder : FilterBuilder
	{
		private readonly FilterBuilder factory;

		public BooleanFilterBuilder(FilterBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Filter GetFilter(Element e)
		{
			BooleanFilter bf = new BooleanFilter();
			NodeList nl = e.GetChildNodes();
			for (int i = 0; i < nl.GetLength(); i++)
			{
				Node node = nl.Item(i);
				if (node.GetNodeName().Equals("Clause"))
				{
					Element clauseElem = (Element)node;
					BooleanClause.Occur occurs = BooleanQueryBuilder.GetOccursValue(clauseElem);
					Element clauseFilter = DOMUtils.GetFirstChildOrFail(clauseElem);
					Filter f = factory.GetFilter(clauseFilter);
					bf.Add(new FilterClause(f, occurs));
				}
			}
			return bf;
		}
	}
}
