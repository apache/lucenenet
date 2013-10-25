using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class TermQueryBuilder : IQueryBuilder
    {
        public Query GetQuery(XElement e)
        {
            string field = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string value = DOMUtils.GetNonBlankTextOrFail(e);
            TermQuery tq = new TermQuery(new Term(field, value));
            tq.Boost = DOMUtils.GetAttribute(e, @"boost", 1.0f);
            return tq;
        }
    }
}
