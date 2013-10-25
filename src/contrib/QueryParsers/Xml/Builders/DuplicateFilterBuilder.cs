using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml.Builders
{
    public class DuplicateFilterBuilder : IFilterBuilder
    {
        public Filter GetFilter(XElement e)
        {
            string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
            DuplicateFilter df = new DuplicateFilter(fieldName);
            string keepMode = DOMUtils.GetAttribute(e, "keepMode", "first");
            if (keepMode.EqualsIgnoreCase("first"))
            {
                df.KeepModeValue = DuplicateFilter.KeepMode.KM_USE_FIRST_OCCURRENCE;
            }
            else if (keepMode.EqualsIgnoreCase(@"last"))
            {
                df.KeepModeValue = DuplicateFilter.KeepMode.KM_USE_LAST_OCCURRENCE;
            }
            else
            {
                throw new ParserException(@"Illegal keepMode attribute in DuplicateFilter:" + keepMode);
            }

            string processingMode = DOMUtils.GetAttribute(e, "processingMode", "full");
            if (processingMode.EqualsIgnoreCase(@"full"))
            {
                df.ProcessingModeValue = DuplicateFilter.ProcessingMode.PM_FULL_VALIDATION;
            }
            else if (processingMode.EqualsIgnoreCase(@"fast"))
            {
                df.ProcessingModeValue = DuplicateFilter.ProcessingMode.PM_FAST_INVALIDATION;
            }
            else
            {
                throw new ParserException(@"Illegal processingMode attribute in DuplicateFilter:" + processingMode);
            }

            return df;
        }
    }
}
