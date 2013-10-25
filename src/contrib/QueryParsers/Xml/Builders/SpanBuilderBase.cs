using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public abstract class SpanBuilderBase : ISpanQueryBuilder
    {
        public Query GetQuery(XElement e)
        {
            return GetSpanQuery(e);
        }

        public abstract SpanQuery GetSpanQuery(XElement e);
    }
}
