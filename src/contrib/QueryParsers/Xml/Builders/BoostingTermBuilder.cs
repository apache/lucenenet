using Lucene.Net.Index;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class BoostingTermBuilder : SpanBuilderBase
    {
        public override SpanQuery GetSpanQuery(XElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string value = DOMUtils.GetNonBlankTextOrFail(e);
            PayloadTermQuery btq = new PayloadTermQuery(new Term(fieldName, value), new AveragePayloadFunction());
            btq.Boost = DOMUtils.GetAttribute(e, "boost", 1.0f);
            return btq;
        }
    }
}
