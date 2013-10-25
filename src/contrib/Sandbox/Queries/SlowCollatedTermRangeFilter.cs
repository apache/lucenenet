using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
{
    public class SlowCollatedTermRangeFilter : MultiTermQueryWrapperFilter<SlowCollatedTermRangeQuery>
    {
        public SlowCollatedTermRangeFilter(string fieldName, string lowerTerm, string upperTerm, bool includeLower, bool includeUpper, StringComparer collator)
            : base(new SlowCollatedTermRangeQuery(fieldName, lowerTerm, upperTerm, includeLower, includeUpper, collator))
        {
        }

        public virtual string GetLowerTerm()
        {
            return query.GetLowerTerm();
        }

        public virtual string GetUpperTerm()
        {
            return query.GetUpperTerm();
        }

        public virtual bool IncludesLower()
        {
            return query.IncludesLower();
        }

        public virtual bool IncludesUpper()
        {
            return query.IncludesUpper();
        }

        public virtual StringComparer GetCollator()
        {
            return query.GetCollator();
        }
    }
}
