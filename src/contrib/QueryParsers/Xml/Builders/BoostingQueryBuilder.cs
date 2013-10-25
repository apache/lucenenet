using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class BoostingQueryBuilder : IQueryBuilder
    {
        private const float DEFAULT_BOOST = 0.01f;

        private readonly IQueryBuilder factory;

        public BoostingQueryBuilder(IQueryBuilder factory)
        {
            this.factory = factory;
        }

        public Query GetQuery(XElement e)
        {
            XElement mainQueryElem = DOMUtils.GetChildByTagOrFail(e, "Query");
            mainQueryElem = DOMUtils.GetFirstChildOrFail(mainQueryElem);
            Query mainQuery = factory.GetQuery(mainQueryElem);
            XElement boostQueryElem = DOMUtils.GetChildByTagOrFail(e, "BoostQuery");
            float boost = DOMUtils.GetAttribute(boostQueryElem, "boost", DEFAULT_BOOST);
            boostQueryElem = DOMUtils.GetFirstChildOrFail(boostQueryElem);
            Query boostQuery = factory.GetQuery(boostQueryElem);
            BoostingQuery bq = new BoostingQuery(mainQuery, boostQuery, boost);
            bq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
            return bq;
        }
    }
}
