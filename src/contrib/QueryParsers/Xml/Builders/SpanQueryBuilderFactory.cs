using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class SpanQueryBuilderFactory : ISpanQueryBuilder
    {
        private readonly IDictionary<String, ISpanQueryBuilder> builders = new HashMap<String, ISpanQueryBuilder>();

        public Query GetQuery(XElement e)
        {
            return GetSpanQuery(e);
        }

        public virtual void AddBuilder(string nodeName, ISpanQueryBuilder builder)
        {
            builders[nodeName] = builder;
        }

        public SpanQuery GetSpanQuery(XElement e)
        {
            ISpanQueryBuilder builder = builders[e.Name.LocalName];
            if (builder == null)
            {
                throw new ParserException(@"No SpanQueryObjectBuilder defined for node " + e.Name.LocalName);
            }

            return builder.GetSpanQuery(e);
        }
    }
}
