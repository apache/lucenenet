using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class SpanTermBuilder : SpanBuilderBase
    {
        public override SpanQuery GetSpanQuery(XElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string value = DOMUtils.GetNonBlankTextOrFail(e);
            SpanTermQuery stq = new SpanTermQuery(new Term(fieldName, value));
            stq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
            return stq;
        }
    }
}
