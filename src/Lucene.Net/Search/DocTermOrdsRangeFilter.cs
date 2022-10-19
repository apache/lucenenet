using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.Text;

namespace Lucene.Net.Search
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IBits = Lucene.Net.Util.IBits;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// A range filter built on top of a cached multi-valued term field (in <see cref="IFieldCache"/>).
    ///
    /// <para>Like <see cref="FieldCacheRangeFilter"/>, this is just a specialized range query versus
    ///    using a <see cref="TermRangeQuery"/> with <see cref="DocTermOrdsRewriteMethod"/>: it will only do
    ///    two ordinal to term lookups.</para>
    /// </summary>

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

        /// <summary>
        /// This method is implemented for each data type </summary>
        public override abstract DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs);

        /// <summary>
        /// Creates a BytesRef range filter using <see cref="IFieldCache.GetTermsIndex(Index.AtomicReader, string, float)"/>. This works with all
        /// fields containing zero or one term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static DocTermOrdsRangeFilter NewBytesRefRange(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
        {
            return new DocTermOrdsRangeFilterAnonymousClass(field, lowerVal, upperVal, includeLower, includeUpper);
        }

        private sealed class DocTermOrdsRangeFilterAnonymousClass : DocTermOrdsRangeFilter
        {
            public DocTermOrdsRangeFilterAnonymousClass(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
                : base(field, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedSetDocValues docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, field);
                long lowerPoint = lowerVal is null ? -1 : docTermOrds.LookupTerm(lowerVal);
                long upperPoint = upperVal is null ? -1 : docTermOrds.LookupTerm(upperVal);

                long inclusiveLowerPoint, inclusiveUpperPoint;

                // Hints:
                // * binarySearchLookup returns -1, if value was null.
                // * the value is <0 if no exact hit was found, the returned value
                //   is (-(insertion point) - 1)
                if (lowerPoint == -1 && lowerVal is null)
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

                if (upperPoint == -1 && upperVal is null)
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
                    return null;
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(inclusiveLowerPoint >= 0 && inclusiveUpperPoint >= 0);

                return new FieldCacheDocIdSet(context.AtomicReader.MaxDoc, acceptDocs, (doc) =>
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
                });
            }
        }

        public override sealed string ToString()
        {
            StringBuilder sb = (new StringBuilder(field)).Append(':');
            return sb.Append(includeLower ? '[' : '{')
                .Append((lowerVal is null) ? "*" : lowerVal.ToString())
                .Append(" TO ")
                .Append((upperVal is null) ? "*" : upperVal.ToString())
                .Append(includeUpper ? ']' : '}')
                .ToString();
        }

        public override sealed bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is DocTermOrdsRangeFilter))
            {
                return false;
            }
            DocTermOrdsRangeFilter other = (DocTermOrdsRangeFilter)o;

            if (!this.field.Equals(other.field, StringComparison.Ordinal) || this.includeLower != other.includeLower || this.includeUpper != other.includeUpper)
            {
                return false;
            }
            if (this.lowerVal != null ? !this.lowerVal.Equals(other.lowerVal) : other.lowerVal != null)
            {
                return false;
            }
            if (this.upperVal != null ? !this.upperVal.Equals(other.upperVal) : other.upperVal != null)
            {
                return false;
            }
            return true;
        }

        public override sealed int GetHashCode()
        {
            int h = field.GetHashCode();
            h ^= (lowerVal != null) ? lowerVal.GetHashCode() : 550356204;
            h = (h << 1) | (h.TripleShift(31)); // rotate to distinguish lower from upper
            h ^= (upperVal != null) ? upperVal.GetHashCode() : -1674416163;
            h ^= (includeLower ? 1549299360 : -365038026) ^ (includeUpper ? 1721088258 : 1948649653);
            return h;
        }

        /// <summary>
        /// Returns the field name for this filter </summary>
        public virtual string Field => field;

        /// <summary>
        /// Returns <c>true</c> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower => includeLower;

        /// <summary>
        /// Returns <c>true</c> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper => includeUpper;

        /// <summary>
        /// Returns the lower value of this range filter </summary>
        public virtual BytesRef LowerVal => lowerVal;

        /// <summary>
        /// Returns the upper value of this range filter </summary>
        public virtual BytesRef UpperVal => upperVal;
    }
}