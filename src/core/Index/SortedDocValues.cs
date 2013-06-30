using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    public abstract class SortedDocValues : BinaryDocValues
    {
        protected SortedDocValues()
        {
        }

        public abstract int GetOrd(int docID);

        public abstract void LookupOrd(int ord, BytesRef result);

        public abstract int ValueCount { get; }

        public override void Get(int docID, BytesRef result)
        {
            int ord = GetOrd(docID);
            if (ord == -1)
            {
                result.bytes = MISSING;
                result.length = 0;
                result.offset = 0;
            }
            else
            {
                LookupOrd(ord, result);
            }
        }

        private sealed class AnonymousEmptySortedDocValues : SortedDocValues
        {
            public override int GetOrd(int docID)
            {
                return 0;
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                result.bytes = SortedDocValues.MISSING;
                result.offset = 0;
                result.length = 0;
            }

            public override int ValueCount
            {
                get { return 1; }
            }
        }

        public static readonly SortedDocValues EMPTY = new AnonymousEmptySortedDocValues();

        public virtual int LookupTerm(BytesRef key)
        {
            BytesRef spare = new BytesRef();
            int low = 0;
            int high = ValueCount - 1;

            while (low <= high)
            {
                int mid = Number.URShift((low + high), 1);
                LookupOrd(mid, spare);
                int cmp = spare.CompareTo(key);

                if (cmp < 0)
                {
                    low = mid + 1;
                }
                else if (cmp > 0)
                {
                    high = mid - 1;
                }
                else
                {
                    return mid; // key found
                }
            }

            return -(low + 1);  // key not found.
        }

        public virtual TermsEnum TermsEnum
        {
            get { return new SortedDocValuesTermsEnum(this); }
        }
    }
}
