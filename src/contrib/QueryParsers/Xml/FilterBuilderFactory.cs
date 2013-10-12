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
    public class FilterBuilderFactory : IFilterBuilder
    {
        HashMap<string, IFilterBuilder> builders = new HashMap<string, IFilterBuilder>();
        
        public Filter GetFilter(XElement n)
        {
            IFilterBuilder builder = builders[n.Name.LocalName];
            if (builder == null)
            {
                throw new ParserException("No FilterBuilder defined for node " + n.Name);
            }
            return builder.GetFilter(n);
        }

        public void AddBuilder(string nodeName, IFilterBuilder builder)
        {
            builders[nodeName] = builder;
        }

        public IFilterBuilder GetFilterBuilder(string nodeName)
        {
            return builders[nodeName];
        }
    }
}
