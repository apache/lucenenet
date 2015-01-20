/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Search.BooleanQuery">Org.Apache.Lucene.Search.BooleanQuery
	/// 	</see>
	/// </summary>
	public class BooleanQueryBuilder : QueryBuilder
	{
		private readonly QueryBuilder factory;

		public BooleanQueryBuilder(QueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			BooleanQuery bq = new BooleanQuery(DOMUtils.GetAttribute(e, "disableCoord", false
				));
			bq.SetMinimumNumberShouldMatch(DOMUtils.GetAttribute(e, "minimumNumberShouldMatch"
				, 0));
			bq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			NodeList nl = e.GetChildNodes();
			for (int i = 0; i < nl.GetLength(); i++)
			{
				Node node = nl.Item(i);
				if (node.GetNodeName().Equals("Clause"))
				{
					Element clauseElem = (Element)node;
					BooleanClause.Occur occurs = GetOccursValue(clauseElem);
					Element clauseQuery = DOMUtils.GetFirstChildOrFail(clauseElem);
					Query q = factory.GetQuery(clauseQuery);
					bq.Add(new BooleanClause(q, occurs));
				}
			}
			return bq;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		internal static BooleanClause.Occur GetOccursValue(Element clauseElem)
		{
			string occs = clauseElem.GetAttribute("occurs");
			BooleanClause.Occur occurs = BooleanClause.Occur.SHOULD;
			if (Sharpen.Runtime.EqualsIgnoreCase("must", occs))
			{
				occurs = BooleanClause.Occur.MUST;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase("mustNot", occs))
				{
					occurs = BooleanClause.Occur.MUST_NOT;
				}
				else
				{
					if ((Sharpen.Runtime.EqualsIgnoreCase("should", occs)) || (string.Empty.Equals(occs
						)))
					{
						occurs = BooleanClause.Occur.SHOULD;
					}
					else
					{
						if (occs != null)
						{
							throw new ParserException("Invalid value for \"occurs\" attribute of clause:" + occs
								);
						}
					}
				}
			}
			return occurs;
		}
	}
}
