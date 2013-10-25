using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class RangeFilterBuilder : IFilterBuilder
    {
        public Filter GetFilter(XElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritance(e, "fieldName");
            string lowerTerm = DOMUtils.GetAttribute(e, "lowerTerm", "");
            string upperTerm = DOMUtils.GetAttribute(e, "upperTerm", "");
            bool includeLower = DOMUtils.GetAttribute(e, "includeLower", true);
            bool includeUpper = DOMUtils.GetAttribute(e, "includeUpper", true);
            return TermRangeFilter.NewStringRange(fieldName, lowerTerm, upperTerm, includeLower, includeUpper);
        }
    }
}
