using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
{
    public class SlowCollatedTermRangeQuery : MultiTermQuery
    {
        private string lowerTerm;
        private string upperTerm;
        private bool includeLower;
        private bool includeUpper;
        private StringComparer collator;

        public SlowCollatedTermRangeQuery(string field, string lowerTerm, string upperTerm, bool includeLower, bool includeUpper, StringComparer collator)
            : base(field)
        {
            this.lowerTerm = lowerTerm;
            this.upperTerm = upperTerm;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
            this.collator = collator;
        }

        public virtual string GetLowerTerm()
        {
            return lowerTerm;
        }

        public virtual string GetUpperTerm()
        {
            return upperTerm;
        }

        public virtual bool IncludesLower()
        {
            return includeLower;
        }

        public virtual bool IncludesUpper()
        {
            return includeUpper;
        }

        public virtual StringComparer GetCollator()
        {
            return collator;
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (lowerTerm != null && upperTerm != null && collator.Compare(lowerTerm, upperTerm) > 0)
            {
                return TermsEnum.EMPTY;
            }

            TermsEnum tenum = terms.Iterator(null);
            if (lowerTerm == null && upperTerm == null)
            {
                return tenum;
            }

            return new SlowCollatedTermRangeTermsEnum(tenum, lowerTerm, upperTerm, includeLower, includeUpper, collator);
        }

        public override string Field
        {
            get
            {
                return base.Field;
            }
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!Field.Equals(field))
            {
                buffer.Append(Field);
                buffer.Append(@":");
            }

            buffer.Append(includeLower ? '[' : '{');
            buffer.Append(lowerTerm != null ? lowerTerm : @"*");
            buffer.Append(@" TO ");
            buffer.Append(upperTerm != null ? upperTerm : @"*");
            buffer.Append(includeUpper ? ']' : '}');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((collator == null) ? 0 : collator.GetHashCode());
            result = prime * result + (includeLower ? 1231 : 1237);
            result = prime * result + (includeUpper ? 1231 : 1237);
            result = prime * result + ((lowerTerm == null) ? 0 : lowerTerm.GetHashCode());
            result = prime * result + ((upperTerm == null) ? 0 : upperTerm.GetHashCode());
            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            SlowCollatedTermRangeQuery other = (SlowCollatedTermRangeQuery)obj;
            if (collator == null)
            {
                if (other.collator != null)
                    return false;
            }
            else if (!collator.Equals(other.collator))
                return false;
            if (includeLower != other.includeLower)
                return false;
            if (includeUpper != other.includeUpper)
                return false;
            if (lowerTerm == null)
            {
                if (other.lowerTerm != null)
                    return false;
            }
            else if (!lowerTerm.Equals(other.lowerTerm))
                return false;
            if (upperTerm == null)
            {
                if (other.upperTerm != null)
                    return false;
            }
            else if (!upperTerm.Equals(other.upperTerm))
                return false;
            return true;
        }
    }
}
