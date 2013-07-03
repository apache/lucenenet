using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public class FieldValueFilter : Filter
    {
        private readonly string field;
        private readonly bool negate;

        public FieldValueFilter(string field)
            : this(field, false)
        {
        }

        public FieldValueFilter(string field, bool negate)
        {
            this.field = field;
            this.negate = negate;
        }

        public string Field
        {
            get { return field; }
        }

        public bool Negate
        {
            get { return negate; }
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            IBits docsWithField = FieldCache.DEFAULT.GetDocsWithField(context.Reader, field);

            if (negate)
            {
                if (docsWithField is Bits.MatchAllBits)
                {
                    return null;
                }

                return new AnonymousFieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, docsWithField, true);
            }
            else
            {
                if (docsWithField is Bits.MatchNoBits)
                {
                    return null;
                }
                if (docsWithField is DocIdSet)
                {
                    // UweSays: this is always the case for our current impl - but who knows
                    // :-)
                    return BitsFilteredDocIdSet.Wrap((DocIdSet)docsWithField, acceptDocs);
                }

                return new AnonymousFieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, docsWithField, false);
            }
        }

        private sealed class AnonymousFieldCacheDocIdSet : FieldCacheDocIdSet
        {
            private readonly IBits docsWithField;
            private bool negate;

            public AnonymousFieldCacheDocIdSet(int maxDoc, IBits acceptDocs, IBits docsWithField, bool negate)
                : base(maxDoc, acceptDocs)
            {
                this.docsWithField = docsWithField;
                this.negate = negate;
            }

            protected override bool MatchDoc(int doc)
            {
                if (negate)
                    return !docsWithField[doc];
                else
                    return docsWithField[doc];
            }
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((field == null) ? 0 : field.GetHashCode());
            result = prime * result + (negate ? 1231 : 1237);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            FieldValueFilter other = (FieldValueFilter)obj;
            if (field == null)
            {
                if (other.field != null)
                    return false;
            }
            else if (!field.Equals(other.field))
                return false;
            if (negate != other.negate)
                return false;
            return true;
        }

        public override string ToString()
        {
            return "FieldValueFilter [field=" + field + ", negate=" + negate + "]";
        }
    }
}
