using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class FilteredQueryBuilder : IQueryBuilder
    {
        private readonly IFilterBuilder filterFactory;
        private readonly IQueryBuilder queryFactory;

        public FilteredQueryBuilder(IFilterBuilder filterFactory, IQueryBuilder queryFactory)
        {
            this.filterFactory = filterFactory;
            this.queryFactory = queryFactory;
        }

        public Query GetQuery(XElement e)
        {
            XElement filterElement = DOMUtils.GetChildByTagOrFail(e, "Filter");
            filterElement = DOMUtils.GetFirstChildOrFail(filterElement);
            Filter f = filterFactory.GetFilter(filterElement);
            XElement queryElement = DOMUtils.GetChildByTagOrFail(e, "Query");
            queryElement = DOMUtils.GetFirstChildOrFail(queryElement);
            Query q = queryFactory.GetQuery(queryElement);
            FilteredQuery fq = new FilteredQuery(q, f);
            fq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return fq;
        }
    }
}
