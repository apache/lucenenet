using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class BooleanFilterBuilder : IFilterBuilder
    {
        private readonly IFilterBuilder factory;

        public BooleanFilterBuilder(IFilterBuilder factory)
        {
            this.factory = factory;
        }

        public Filter GetFilter(XElement e)
        {
            BooleanFilter bf = new BooleanFilter();
            var nl = e.Elements().ToList();

            for (int i = 0; i < nl.Count; i++)
            {
                var node = nl[i];
                if (node.Name.LocalName.Equals("Clause"))
                {
                    XElement clauseElem = node;
                    Occur occurs = BooleanQueryBuilder.GetOccursValue(clauseElem);

                    XElement clauseFilter = DOMUtils.GetFirstChildOrFail(clauseElem);
                    Filter f = factory.GetFilter(clauseFilter);
                    bf.Add(new FilterClause(f, occurs));
                }
            }

            return bf;
        }
    }
}
