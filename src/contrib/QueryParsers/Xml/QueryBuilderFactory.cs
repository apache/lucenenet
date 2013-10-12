using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml
{
    public class QueryBuilderFactory : IQueryBuilder
    {
        HashMap<string, IQueryBuilder> builders = new HashMap<string, IQueryBuilder>();
        
        public Query GetQuery(XElement n)
        {
            IQueryBuilder builder = builders[n.Name.LocalName];
            if (builder == null)
            {
                throw new ParserException("No QueryObjectBuilder defined for node " + n.Name);
            }
            return builder.GetQuery(n);
        }

        public void AddBuilder(string nodeName, IQueryBuilder builder)
        {
            builders[nodeName] = builder;
        }

        public IQueryBuilder GetQueryBuilder(string nodeName)
        {
            return builders[nodeName];
        }
    }
}
