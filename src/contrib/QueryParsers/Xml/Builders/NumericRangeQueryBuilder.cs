using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class NumericRangeQueryBuilder : IQueryBuilder
    {
        public Query GetQuery(XElement e)
        {
            string field = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            string lowerTerm = DOMUtils.GetAttributeOrFail(e, "lowerTerm");
            string upperTerm = DOMUtils.GetAttributeOrFail(e, "upperTerm");
            bool lowerInclusive = DOMUtils.GetAttribute(e, "includeLower", true);
            bool upperInclusive = DOMUtils.GetAttribute(e, "includeUpper", true);
            int precisionStep = DOMUtils.GetAttribute(e, "precisionStep", NumericUtils.PRECISION_STEP_DEFAULT);
            string type = DOMUtils.GetAttribute(e, "type", "int");
            try
            {
                Query filter;
                if (type.EqualsIgnoreCase(@"int"))
                {
                    filter = NumericRangeQuery.NewIntRange(field, precisionStep, int.Parse(lowerTerm), int.Parse(upperTerm), lowerInclusive, upperInclusive);
                }
                else if (type.EqualsIgnoreCase(@"long"))
                {
                    filter = NumericRangeQuery.NewLongRange(field, precisionStep, long.Parse(lowerTerm), long.Parse(upperTerm), lowerInclusive, upperInclusive);
                }
                else if (type.EqualsIgnoreCase(@"double"))
                {
                    filter = NumericRangeQuery.NewDoubleRange(field, precisionStep, double.Parse(lowerTerm), double.Parse(upperTerm), lowerInclusive, upperInclusive);
                }
                else if (type.EqualsIgnoreCase(@"float"))
                {
                    filter = NumericRangeQuery.NewFloatRange(field, precisionStep, float.Parse(lowerTerm), float.Parse(upperTerm), lowerInclusive, upperInclusive);
                }
                else
                {
                    throw new ParserException(@"type attribute must be one of: [long, int, double, float]");
                }

                return filter;
            }
            catch (FormatException nfe)
            {
                throw new ParserException(@"Could not parse lowerTerm or upperTerm into a number", nfe);
            }
        }
    }
}
