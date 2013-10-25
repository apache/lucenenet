using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class DisjunctionMaxQueryBuilder : IQueryBuilder
    {
        private readonly IQueryBuilder factory;

        public DisjunctionMaxQueryBuilder(IQueryBuilder factory)
        {
            this.factory = factory;
        }

        public Query GetQuery(XElement e)
        {
            float tieBreaker = DOMUtils.GetAttribute(e, "tieBreaker", 0.0f);
            DisjunctionMaxQuery dq = new DisjunctionMaxQuery(tieBreaker);
            dq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            var nl = e.DescendantNodes();
            foreach (XNode node in nl)
            {
                if (node is XElement)
                {
                    XElement queryElem = (XElement)node;
                    Query q = factory.GetQuery(queryElem);
                    dq.Add(q);
                }
            }

            return dq;
        }
    }
}
