using System;
using System.Diagnostics;
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
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// A range filter built on top of a cached multi-valued term field (in <seealso cref="IFieldCache"/>).
    ///
    /// <p>Like <seealso cref="FieldCacheRangeFilter"/>, this is just a specialized range query versus
    ///    using a TermRangeQuery with <seealso cref="DocTermOrdsRewriteMethod"/>: it will only do
    ///    two ordinal to term lookups.</p>
    /// </summary>

    public abstract class DocTermOrdsRangeFilter : Filter
    {
        internal readonly string Field_Renamed; // LUCENENET TODO: rename (private)
        internal readonly BytesRef LowerVal_Renamed; // LUCENENET TODO: rename (private)
        internal readonly BytesRef UpperVal_Renamed; // LUCENENET TODO: rename (private)
        internal readonly bool IncludeLower; // LUCENENET TODO: rename (private)
        internal readonly bool IncludeUpper; // LUCENENET TODO: rename (private)

        private DocTermOrdsRangeFilter(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
        {
            this.Field_Renamed = field;
            this.LowerVal_Renamed = lowerVal;
            this.UpperVal_Renamed = upperVal;
            this.IncludeLower = includeLower;
            this.IncludeUpper = includeUpper;
        }

        /// <summary>
        /// this method is implemented for each data type </summary>
        public override abstract DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs);

        /// <summary>
        /// Creates a BytesRef range filter using <seealso cref="IFieldCache#getTermsIndex"/>. this works with all
        /// fields containing zero or one term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static DocTermOrdsRangeFilter NewBytesRefRange(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
        {
            return new DocTermOrdsRangeFilterAnonymousInnerClassHelper(field, lowerVal, upperVal, includeLower, includeUpper);
        }

        private class DocTermOrdsRangeFilterAnonymousInnerClassHelper : DocTermOrdsRangeFilter
        {
            // LUCENENET TODO: should rely on base class for these variables
            private new string Field;
            private new BytesRef LowerVal;
            private new BytesRef UpperVal;
            private new bool IncludeLower;
            private new bool IncludeUpper;

            public DocTermOrdsRangeFilterAnonymousInnerClassHelper(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
                : base(field, lowerVal, upperVal, includeLower, includeUpper)
            {
                this.Field = field;
                this.LowerVal = lowerVal;
                this.UpperVal = upperVal;
                this.IncludeLower = includeLower;
                this.IncludeUpper = includeUpper;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                SortedSetDocValues docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, Field);
                long lowerPoint = LowerVal == null ? -1 : docTermOrds.LookupTerm(LowerVal);
                long upperPoint = UpperVal == null ? -1 : docTermOrds.LookupTerm(UpperVal);

                long inclusiveLowerPoint, inclusiveUpperPoint;

                // Hints:
                // * binarySearchLookup returns -1, if value was null.
                // * the value is <0 if no exact hit was found, the returned value
                //   is (-(insertion point) - 1)
                if (lowerPoint == -1 && LowerVal == null)
                {
                    inclusiveLowerPoint = 0;
                }
                else if (IncludeLower && lowerPoint >= 0)
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

                if (upperPoint == -1 && UpperVal == null)
                {
                    inclusiveUpperPoint = long.MaxValue;
                }
                else if (IncludeUpper && upperPoint >= 0)
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

                Debug.Assert(inclusiveLowerPoint >= 0 && inclusiveUpperPoint >= 0);

                return new FieldCacheDocIdSetAnonymousInnerClassHelper(this, context.AtomicReader.MaxDoc, acceptDocs, docTermOrds, inclusiveLowerPoint, inclusiveUpperPoint);
            }

            private class FieldCacheDocIdSetAnonymousInnerClassHelper : FieldCacheDocIdSet
            {
                private readonly DocTermOrdsRangeFilterAnonymousInnerClassHelper OuterInstance; // LUCENENET TODO: rename (private)

                private readonly SortedSetDocValues DocTermOrds; // LUCENENET TODO: rename (private)
                private readonly long InclusiveLowerPoint; // LUCENENET TODO: rename (private)
                private readonly long InclusiveUpperPoint; // LUCENENET TODO: rename (private)

                public FieldCacheDocIdSetAnonymousInnerClassHelper(DocTermOrdsRangeFilterAnonymousInnerClassHelper outerInstance, int maxDoc, Bits acceptDocs, SortedSetDocValues docTermOrds, long inclusiveLowerPoint, long inclusiveUpperPoint)
                    : base(maxDoc, acceptDocs)
                {
                    this.OuterInstance = outerInstance;
                    this.DocTermOrds = docTermOrds;
                    this.InclusiveLowerPoint = inclusiveLowerPoint;
                    this.InclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override sealed bool MatchDoc(int doc)
                {
                    DocTermOrds.SetDocument(doc);
                    long ord;
                    while ((ord = DocTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        if (ord > InclusiveUpperPoint)
                        {
                            return false;
                        }
                        else if (ord >= InclusiveLowerPoint)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        public override sealed string ToString()
        {
            StringBuilder sb = (new StringBuilder(Field_Renamed)).Append(":");
            return sb.Append(IncludeLower ? '[' : '{')
                .Append((LowerVal_Renamed == null) ? "*" : LowerVal_Renamed.ToString())
                .Append(" TO ")
                .Append((UpperVal_Renamed == null) ? "*" : UpperVal_Renamed.ToString())
                .Append(IncludeUpper ? ']' : '}')
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

            if (!this.Field_Renamed.Equals(other.Field_Renamed) || this.IncludeLower != other.IncludeLower || this.IncludeUpper != other.IncludeUpper)
            {
                return false;
            }
            if (this.LowerVal_Renamed != null ? !this.LowerVal_Renamed.Equals(other.LowerVal_Renamed) : other.LowerVal_Renamed != null)
            {
                return false;
            }
            if (this.UpperVal_Renamed != null ? !this.UpperVal_Renamed.Equals(other.UpperVal_Renamed) : other.UpperVal_Renamed != null)
            {
                return false;
            }
            return true;
        }

        public override sealed int GetHashCode()
        {
            int h = Field_Renamed.GetHashCode();
            h ^= (LowerVal_Renamed != null) ? LowerVal_Renamed.GetHashCode() : 550356204;
            h = (h << 1) | ((int)((uint)h >> 31)); // rotate to distinguish lower from upper
            h ^= (UpperVal_Renamed != null) ? UpperVal_Renamed.GetHashCode() : -1674416163;
            h ^= (IncludeLower ? 1549299360 : -365038026) ^ (IncludeUpper ? 1721088258 : 1948649653);
            return h;
        }

        /// <summary>
        /// Returns the field name for this filter </summary>
        public virtual string Field
        {
            get
            {
                return Field_Renamed;
            }
        }

        /// <summary>
        /// Returns <code>true</code> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower
        {
            get { return IncludeLower; }
        }

        /// <summary>
        /// Returns <code>true</code> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper
        {
            get { return IncludeUpper; }
        }

        /// <summary>
        /// Returns the lower value of this range filter </summary>
        public virtual BytesRef LowerVal
        {
            get
            {
                return LowerVal_Renamed;
            }
        }

        /// <summary>
        /// Returns the upper value of this range filter </summary>
        public virtual BytesRef UpperVal
        {
            get
            {
                return UpperVal_Renamed;
            }
        }
    }
}