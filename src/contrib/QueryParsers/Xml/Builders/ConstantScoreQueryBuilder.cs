using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class ConstantScoreQueryBuilder : IQueryBuilder
    {
        private readonly FilterBuilderFactory filterFactory;

        public ConstantScoreQueryBuilder(FilterBuilderFactory filterFactory)
        {
            this.filterFactory = filterFactory;
        }

        public Query GetQuery(XElement e)
        {
            XElement filterElem = DOMUtils.GetFirstChildOrFail(e);
            Query q = new ConstantScoreQuery(filterFactory.GetFilter(filterElem));
            q.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return q;
        }
    }
}
