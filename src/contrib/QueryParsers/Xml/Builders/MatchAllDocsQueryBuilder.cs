using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class MatchAllDocsQueryBuilder : IQueryBuilder
    {
        public Query GetQuery(XElement e)
        {
            return new MatchAllDocsQuery();
        }
    }
}
