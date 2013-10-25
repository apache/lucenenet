using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class SpanFirstBuilder : SpanBuilderBase
    {
        private readonly ISpanQueryBuilder factory;

        public SpanFirstBuilder(ISpanQueryBuilder factory)
        {
            this.factory = factory;
        }

        public override SpanQuery GetSpanQuery(XElement e)
        {
            int end = DOMUtils.GetAttribute(e, "end", 1);
            XElement child = DOMUtils.GetFirstChildElement(e);
            SpanQuery q = factory.GetSpanQuery(child);
            SpanFirstQuery sfq = new SpanFirstQuery(q, end);
            sfq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
            return sfq;
        }
    }
}
