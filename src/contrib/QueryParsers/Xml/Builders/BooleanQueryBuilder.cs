using Lucene.Net.Search;
using Lucene.Net.QueryParsers.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class BooleanQueryBuilder : IQueryBuilder
    {
        private readonly IQueryBuilder factory;

        public BooleanQueryBuilder(IQueryBuilder factory)
        {
            this.factory = factory;
        }

        public Query GetQuery(XElement e)
        {
            BooleanQuery bq = new BooleanQuery(DOMUtils.GetAttribute(e, "disableCoord", false));
            bq.MinimumNumberShouldMatch = DOMUtils.GetAttribute(e, "minimumNumberShouldMatch", 0);
            bq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);

            var nl = e.Elements().ToList();
            for (int i = 0; i < nl.Count; i++)
            {
                var node = nl[i];
                if (node.Name.LocalName.Equals("Clause"))
                {
                    XElement clauseElem = node;
                    Occur occurs = GetOccursValue(clauseElem);

                    XElement clauseQuery = DOMUtils.GetFirstChildOrFail(clauseElem);
                    Query q = factory.GetQuery(clauseQuery);
                    bq.Add(new BooleanClause(q, occurs));
                }
            }

            return bq;
        }

        internal static Occur GetOccursValue(XElement clauseElem)
        {
            var occs = clauseElem.Attribute(XName.Get("occurs"));
            Occur occurs = Occur.SHOULD;
            if ("must".EqualsIgnoreCase(occs.Value))
            {
                occurs = Occur.MUST;
            }
            else
            {
                if ("mustNot".EqualsIgnoreCase(occs.Value))
                {
                    occurs = Occur.MUST_NOT;
                }
                else
                {
                    if (("should".EqualsIgnoreCase(occs.Value)) || ("".Equals(occs.Value)))
                    {
                        occurs = Occur.SHOULD;
                    }
                    else
                    {
                        if (occs != null)
                        {
                            throw new ParserException("Invalid value for \"occurs\" attribute of clause:" + occs);
                        }
                    }
                }
            }
            return occurs;

        }
    }
}
