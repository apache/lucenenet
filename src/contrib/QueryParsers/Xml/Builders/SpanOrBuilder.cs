using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class SpanOrBuilder : SpanBuilderBase
    {
        private readonly ISpanQueryBuilder factory;

        public SpanOrBuilder(ISpanQueryBuilder factory)
        {
            this.factory = factory;
        }

        public override SpanQuery GetSpanQuery(XElement e)
        {
            List<SpanQuery> clausesList = new List<SpanQuery>();
            for (XNode kid = e.FirstNode; kid != null; kid = kid.NextNode)
            {
                if (kid.NodeType == XmlNodeType.Element)
                {
                    SpanQuery clause = factory.GetSpanQuery((XElement)kid);
                    clausesList.Add(clause);
                }
            }

            SpanQuery[] clauses = clausesList.ToArray();
            SpanOrQuery soq = new SpanOrQuery(clauses);
            soq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
            return soq;
        }
    }
}
