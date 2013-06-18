using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    public abstract class SortedSetDocValues
    {
        protected SortedSetDocValues()
        {
        }

        public const long NO_MORE_ORDS = -1;

        public abstract long NextOrd();

        public abstract void SetDocument(int docID);

        public abstract void LookupOrd(long ord, BytesRef result);

        public abstract long ValueCount { get; }

        private sealed class AnonymousEmptySortedSetDocValues : SortedSetDocValues
        {
            public override long NextOrd()
            {
                return NO_MORE_ORDS;
            }

            public override void SetDocument(int docID)
            {
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                throw new IndexOutOfRangeException();
            }

            public override long ValueCount
            {
                get { return 0; }
            }
        }

        public static readonly SortedSetDocValues EMPTY = new AnonymousEmptySortedSetDocValues();

        public long LookupTerm(BytesRef key)
        {
            BytesRef spare = new BytesRef();
            long low = 0;
            long high = ValueCount - 1;

            while (low <= high)
            {
                long mid = Number.URShift((low + high), 1);
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

        public TermsEnum TermsEnum
        {
            get { return new SortedSetDocValuesTermsEnum(this); }
        }
    }
}
