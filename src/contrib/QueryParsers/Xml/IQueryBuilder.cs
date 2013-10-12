using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml
{
    public interface IQueryBuilder
    {
        public Query GetQuery(XElement e);
    }
}
