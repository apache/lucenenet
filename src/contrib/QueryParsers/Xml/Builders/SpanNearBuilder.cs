using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class SpanNearBuilder : SpanBuilderBase
    {
        private readonly ISpanQueryBuilder factory;

        public SpanNearBuilder(ISpanQueryBuilder factory)
        {
            this.factory = factory;
        }

        public override SpanQuery GetSpanQuery(XElement e)
        {
            string slopString = DOMUtils.GetAttributeOrFail(e, "slop");
            int slop = int.Parse(slopString);
            bool inOrder = DOMUtils.GetAttribute(e, "inOrder", false);
            IList<SpanQuery> spans = new List<SpanQuery>();
            for (XNode kid = e.FirstNode; kid != null; kid = kid.NextNode)
            {
                if (kid.NodeType == XmlNodeType.Element)
                {
                    spans.Add(factory.GetSpanQuery((XElement)kid));
                }
            }

            SpanQuery[] spanQueries = spans.ToArray();
            return new SpanNearQuery(spanQueries, slop, inOrder);
        }
    }
}
