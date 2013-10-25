using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public interface ISpanQueryBuilder : IQueryBuilder
    {
        SpanQuery GetSpanQuery(XElement e);
    }
}
