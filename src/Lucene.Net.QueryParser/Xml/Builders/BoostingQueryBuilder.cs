/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queries;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Queries.BoostingQuery">Lucene.Net.Queries.BoostingQuery
	/// 	</see>
	/// </summary>
	public class BoostingQueryBuilder : QueryBuilder
	{
		private static float DEFAULT_BOOST = 0.01f;

		private readonly QueryBuilder factory;

		public BoostingQueryBuilder(QueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			Element mainQueryElem = DOMUtils.GetChildByTagOrFail(e, "Query");
			mainQueryElem = DOMUtils.GetFirstChildOrFail(mainQueryElem);
			Query mainQuery = factory.GetQuery(mainQueryElem);
			Element boostQueryElem = DOMUtils.GetChildByTagOrFail(e, "BoostQuery");
			float boost = DOMUtils.GetAttribute(boostQueryElem, "boost", DEFAULT_BOOST);
			boostQueryElem = DOMUtils.GetFirstChildOrFail(boostQueryElem);
			Query boostQuery = factory.GetQuery(boostQueryElem);
			BoostingQuery bq = new BoostingQuery(mainQuery, boostQuery, boost);
			bq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return bq;
		}
	}
}
