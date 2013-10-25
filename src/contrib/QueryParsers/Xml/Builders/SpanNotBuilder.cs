using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class SpanNotBuilder : SpanBuilderBase
    {
        private readonly ISpanQueryBuilder factory;

        public SpanNotBuilder(ISpanQueryBuilder factory)
        {
            this.factory = factory;
        }

        public override SpanQuery GetSpanQuery(XElement e)
        {
            XElement includeElem = DOMUtils.GetChildByTagOrFail(e, "Include");
            includeElem = DOMUtils.GetFirstChildOrFail(includeElem);
            XElement excludeElem = DOMUtils.GetChildByTagOrFail(e, "Exclude");
            excludeElem = DOMUtils.GetFirstChildOrFail(excludeElem);
            SpanQuery include = factory.GetSpanQuery(includeElem);
            SpanQuery exclude = factory.GetSpanQuery(excludeElem);
            SpanNotQuery snq = new SpanNotQuery(include, exclude);
            snq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
            return snq;
        }
    }
}
