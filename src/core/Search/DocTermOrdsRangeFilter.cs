using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public abstract class DocTermOrdsRangeFilter : Filter
    {
        internal readonly string field;
        internal readonly BytesRef lowerVal;
        internal readonly BytesRef upperVal;
        internal readonly bool includeLower;
        internal readonly bool includeUpper;

        private DocTermOrdsRangeFilter(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
        {
            this.field = field;
            this.lowerVal = lowerVal;
            this.upperVal = upperVal;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
        }

        public abstract DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs);

        public static DocTermOrdsRangeFilter NewBytesRefRange(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousBytesRefRangeFilter(field, lowerVal, upperVal, includeLower, includeUpper);
        }

        private sealed class AnonymousBytesRefRangeFilter : DocTermOrdsRangeFilter
        {
            public AnonymousBytesRefRangeFilter(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
                : base(field, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedSetDocValues docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.Reader, field);
                long lowerPoint = lowerVal == null ? -1 : docTermOrds.LookupTerm(lowerVal);
                long upperPoint = upperVal == null ? -1 : docTermOrds.LookupTerm(upperVal);

                long inclusiveLowerPoint, inclusiveUpperPoint;

                // Hints:
                // * binarySearchLookup returns -1, if value was null.
                // * the value is <0 if no exact hit was found, the returned value
                //   is (-(insertion point) - 1)
                if (lowerPoint == -1 && lowerVal == null)
                {
                    inclusiveLowerPoint = 0;
                }
                else if (includeLower && lowerPoint >= 0)
                {
                    inclusiveLowerPoint = lowerPoint;
                }
                else if (lowerPoint >= 0)
                {
                    inclusiveLowerPoint = lowerPoint + 1;
                }
                else
                {
                    inclusiveLowerPoint = Math.Max(0, -lowerPoint - 1);
                }

                if (upperPoint == -1 && upperVal == null)
                {
                    inclusiveUpperPoint = long.MaxValue;
                }
                else if (includeUpper && upperPoint >= 0)
                {
                    inclusiveUpperPoint = upperPoint;
                }
                else if (upperPoint >= 0)
                {
                    inclusiveUpperPoint = upperPoint - 1;
                }
                else
                {
                    inclusiveUpperPoint = -upperPoint - 2;
                }

                if (inclusiveUpperPoint < 0 || inclusiveLowerPoint > inclusiveUpperPoint)
                {
                    return DocIdSet.EMPTY_DOCIDSET;
                }

                //assert inclusiveLowerPoint >= 0 && inclusiveUpperPoint >= 0;

                return new AnonymousFieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs);
            }

            private sealed class AnonymousFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private readonly SortedSetDocValues docTermOrds;
                private readonly long inclusiveLowerPoint, inclusiveUpperPoint;

                public AnonymousFieldCacheDocIdSet(int maxDoc, IBits acceptDocs, SortedSetDocValues docTermOrds,
                    long inclusiveLowerPoint, long inclusiveUpperPoint)
                    : base(maxDoc, acceptDocs)
                {
                    this.docTermOrds = docTermOrds;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    docTermOrds.SetDocument(doc);
                    long ord;
                    while ((ord = docTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        if (ord > inclusiveUpperPoint)
                        {
                            return false;
                        }
                        else if (ord >= inclusiveLowerPoint)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(field).Append(":");
            return sb.Append(includeLower ? '[' : '{')
              .Append((lowerVal == null) ? "*" : lowerVal.ToString())
              .Append(" TO ")
              .Append((upperVal == null) ? "*" : upperVal.ToString())
              .Append(includeUpper ? ']' : '}')
              .ToString();
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (!(o is DocTermOrdsRangeFilter)) return false;
            DocTermOrdsRangeFilter other = (DocTermOrdsRangeFilter)o;

            if (!this.field.Equals(other.field)
                || this.includeLower != other.includeLower
                || this.includeUpper != other.includeUpper
            ) { return false; }
            if (this.lowerVal != null ? !this.lowerVal.Equals(other.lowerVal) : other.lowerVal != null) return false;
            if (this.upperVal != null ? !this.upperVal.Equals(other.upperVal) : other.upperVal != null) return false;
            return true;
        }

        public override int GetHashCode()
        {
            int h = field.GetHashCode();
            h ^= (lowerVal != null) ? lowerVal.GetHashCode() : 550356204;
            h = (h << 1) | Number.URShift(h, 31);  // rotate to distinguish lower from upper
            h ^= (upperVal != null) ? upperVal.GetHashCode() : -1674416163;
            h ^= (includeLower ? 1549299360 : -365038026) ^ (includeUpper ? 1721088258 : 1948649653);
            return h;
        }

        public string Field
        {
            get { return field; }
        }

        public bool IncludesLower
        {
            get { return includeLower; }
        }

        public bool IncludesUpper
        {
            get { return includeUpper; }
        }

        public BytesRef LowerVal
        {
            get { return lowerVal; }
        }

        public BytesRef UpperVal
        {
            get { return upperVal; }
        }
    }
}
