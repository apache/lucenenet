/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Support;
using IndexReader = Lucene.Net.Index.IndexReader;
using NumericUtils = Lucene.Net.Util.NumericUtils;
using Lucene.Net.Util;
using System.Text;
using System.Diagnostics;

namespace Lucene.Net.Search
{

    /// <summary> A range filter built on top of a cached single term field (in <see cref="FieldCache" />).
    /// 
    /// <p/><see cref="FieldCacheRangeFilter" /> builds a single cache for the field the first time it is used.
    /// Each subsequent <see cref="FieldCacheRangeFilter" /> on the same field then reuses this cache,
    /// even if the range itself changes. 
    /// 
    /// <p/>This means that <see cref="FieldCacheRangeFilter" /> is much faster (sometimes more than 100x as fast) 
    /// as building a <see cref="TermRangeFilter" /> if using a <see cref="NewStringRange" />. However, if the range never changes it
    /// is slower (around 2x as slow) than building a CachingWrapperFilter on top of a single <see cref="TermRangeFilter" />.
    /// 
    /// For numeric data types, this filter may be significantly faster than <see cref="NumericRangeFilter{T}" />.
    /// Furthermore, it does not need the numeric values encoded by <see cref="NumericField" />. But
    /// it has the problem that it only works with exact one value/document (see below).
    /// 
    /// <p/>As with all <see cref="FieldCache" /> based functionality, <see cref="FieldCacheRangeFilter" /> is only valid for 
    /// fields which exact one term for each document (except for <see cref="NewStringRange" />
    /// where 0 terms are also allowed). Due to a restriction of <see cref="FieldCache" />, for numeric ranges
    /// all terms that do not have a numeric value, 0 is assumed.
    /// 
    /// <p/>Thus it works on dates, prices and other single value fields but will not work on
    /// regular text fields. It is preferable to use a <c>NOT_ANALYZED</c> field to ensure that
    /// there is only a single term. 
    /// 
    /// <p/>This class does not have an constructor, use one of the static factory methods available,
    /// that create a correct instance for different data types supported by <see cref="FieldCache" />.
    /// </summary>

    public static class FieldCacheRangeFilter
    {
        [Serializable]
        private class AnonymousStringRangeFilter : FieldCacheRangeFilter<string>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private SortedDocValues fcsi;
                private int inclusiveLowerPoint;
                private int inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(SortedDocValues fcsi, int inclusiveLowerPoint, int inclusiveUpperPoint, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.fcsi = fcsi;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    int docOrd = fcsi.GetOrd(doc);
                    return docOrd >= inclusiveLowerPoint && docOrd <= inclusiveUpperPoint;
                }
            }

            internal AnonymousStringRangeFilter(string field, FieldCache.IParser parser, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex((AtomicReader)context.Reader, field);
                int lowerPoint = lowerVal == null ? -1 : fcsi.LookupTerm(new BytesRef(lowerVal));
                int upperPoint = upperVal == null ? -1 : fcsi.LookupTerm(new BytesRef(upperVal));

                int inclusiveLowerPoint, inclusiveUpperPoint;

                // Hints:
                // * binarySearchLookup returns 0, if value was null.
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
                    inclusiveUpperPoint = int.MaxValue;
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

                Debug.Assert(inclusiveLowerPoint > 0 && inclusiveUpperPoint > 0);

                // for this DocIdSet, we never need to use TermDocs,
                // because deleted docs have an order of 0 (null entry in StringIndex)
                return new AnonymousClassFieldCacheDocIdSet(fcsi, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

        [Serializable]
        private class AnonymousBytesRefRangeFilter : FieldCacheRangeFilter<BytesRef>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private SortedDocValues fcsi;
                private int inclusiveLowerPoint;
                private int inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(SortedDocValues fcsi, int inclusiveLowerPoint, int inclusiveUpperPoint, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.fcsi = fcsi;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    int docOrd = fcsi.GetOrd(doc);
                    return docOrd >= inclusiveLowerPoint && docOrd <= inclusiveUpperPoint;
                }
            }

            public AnonymousBytesRefRangeFilter(string field, FieldCache.IParser parser, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex((AtomicReader)context.Reader, field);
                int lowerPoint = lowerVal == null ? -1 : fcsi.LookupTerm(lowerVal);
                int upperPoint = upperVal == null ? -1 : fcsi.LookupTerm(upperVal);

                int inclusiveLowerPoint, inclusiveUpperPoint;

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
                    inclusiveUpperPoint = int.MaxValue;
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
                return new AnonymousClassFieldCacheDocIdSet(fcsi, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

        [Serializable]
        private class AnonymousByteRangeFilter : FieldCacheRangeFilter<sbyte?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Bytes values;
                private sbyte inclusiveLowerPoint;
                private sbyte inclusiveUpperPoint;
                
                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Bytes values, sbyte inclusiveLowerPoint, sbyte inclusiveUpperPoint, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    sbyte value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousByteRangeFilter(string field, FieldCache.IParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                sbyte inclusiveLowerPoint, inclusiveUpperPoint;
                if (lowerVal != null)
                {
                    sbyte i = (sbyte)lowerVal;
                    if (!includeLower && i == sbyte.MaxValue)
                        return DocIdSet.EMPTY_DOCIDSET;
                    inclusiveLowerPoint = (sbyte)(includeLower ? i : (i + 1));
                }
                else
                {
                    inclusiveLowerPoint = sbyte.MinValue;
                }
                if (upperVal != null)
                {
                    sbyte i = (sbyte)upperVal;
                    if (!includeUpper && i == sbyte.MinValue)
                        return DocIdSet.EMPTY_DOCIDSET;
                    inclusiveUpperPoint = (sbyte)(includeUpper ? i : (i - 1));
                }
                else
                {
                    inclusiveUpperPoint = sbyte.MaxValue;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return DocIdSet.EMPTY_DOCIDSET;

                FieldCache.Bytes values = FieldCache.DEFAULT.GetBytes((AtomicReader)context.Reader, field, (FieldCache.IByteParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

        [Serializable]
        private class AnonymousShortRangeFilter : FieldCacheRangeFilter<short?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Shorts values;
                private short inclusiveLowerPoint;
                private short inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Shorts values, short inclusiveLowerPoint, short inclusiveUpperPoint, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    short value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousShortRangeFilter(string field, FieldCache.IParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                short inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal != null)
                {
                    short i = (short)lowerVal;
                    if (!includeLower && i == short.MaxValue)
                        return DocIdSet.EMPTY_DOCIDSET;
                    inclusiveLowerPoint = (short)(includeLower ? i : (i + 1));
                }
                else
                {
                    inclusiveLowerPoint = short.MinValue;
                }
                if (upperVal != null)
                {
                    short i = (short)upperVal;
                    if (!includeUpper && i == short.MinValue)
                        return DocIdSet.EMPTY_DOCIDSET;
                    inclusiveUpperPoint = (short)(includeUpper ? i : (i - 1));
                }
                else
                {
                    inclusiveUpperPoint = short.MaxValue;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return DocIdSet.EMPTY_DOCIDSET;

                FieldCache.Shorts values = FieldCache.DEFAULT.GetShorts((AtomicReader)context.Reader, field, (FieldCache.IShortParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }
        
        [Serializable]
        private class AnonymousIntRangeFilter : FieldCacheRangeFilter<int?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Ints values;
                private int inclusiveLowerPoint;
                private int inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Ints values, int inclusiveLowerPoint, 
                    int inclusiveUpperPoint, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    int value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousIntRangeFilter(string field, FieldCache.IParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                int inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal != null)
                {
                    int i = (int)lowerVal;
                    if (!includeLower && i == int.MaxValue)
                        return DocIdSet.EMPTY_DOCIDSET;
                    inclusiveLowerPoint = includeLower ? i : (i + 1);
                }
                else
                {
                    inclusiveLowerPoint = int.MinValue;
                }
                if (upperVal != null)
                {
                    int i = (int)upperVal;
                    if (!includeUpper && i == int.MinValue)
                        return DocIdSet.EMPTY_DOCIDSET;
                    inclusiveUpperPoint = includeUpper ? i : (i - 1);
                }
                else
                {
                    inclusiveUpperPoint = int.MaxValue;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return DocIdSet.EMPTY_DOCIDSET;

                FieldCache.Ints values = FieldCache.DEFAULT.GetInts((AtomicReader)context.Reader, field, (FieldCache.IIntParser)parser, false);
                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }
        
        [Serializable]
        private class AnonymousLongRangeFilter : FieldCacheRangeFilter<long?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Longs values;
                private long inclusiveLowerPoint;
                private long inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Longs values, long inclusiveLowerPoint, 
                    long inclusiveUpperPoint, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    long value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousLongRangeFilter(string field, FieldCache.IParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                long inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal != null)
                {
                    long i = (long)lowerVal;
                    if (!includeLower && i == long.MaxValue)
                        return DocIdSet.EMPTY_DOCIDSET;
                    inclusiveLowerPoint = includeLower ? i : (i + 1L);
                }
                else
                {
                    inclusiveLowerPoint = long.MinValue;
                }
                if (upperVal != null)
                {
                    long i = (long)upperVal;
                    if (!includeUpper && i == long.MinValue)
                        return DocIdSet.EMPTY_DOCIDSET;
                    inclusiveUpperPoint = includeUpper ? i : (i - 1L);
                }
                else
                {
                    inclusiveUpperPoint = long.MaxValue;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return DocIdSet.EMPTY_DOCIDSET;

                FieldCache.Longs values = FieldCache.DEFAULT.GetLongs((AtomicReader)context.Reader, field, (FieldCache.ILongParser)parser, false);
                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }
        
        [Serializable]
        private class AnonymousFloatRangeFilter : FieldCacheRangeFilter<float?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Floats values;
                private float inclusiveLowerPoint;
                private float inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Floats values, float inclusiveLowerPoint, 
                    float inclusiveUpperPoint, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    float value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousFloatRangeFilter(string field, FieldCache.IParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                // we transform the floating point numbers to sortable integers
                // using NumericUtils to easier find the next bigger/lower value
                float inclusiveLowerPoint;
                float inclusiveUpperPoint;
                if (lowerVal != null)
                {
                    float f = (float)lowerVal;
                    if (!includeUpper && f > 0.0f && float.IsInfinity(f))
                        return DocIdSet.EMPTY_DOCIDSET;
                    int i = NumericUtils.FloatToSortableInt(f);
                    inclusiveLowerPoint = NumericUtils.SortableIntToFloat(includeLower ? i : (i + 1));
                }
                else
                {
                    inclusiveLowerPoint = float.NegativeInfinity;
                }
                if (upperVal != null)
                {
                    float f = (float)upperVal;
                    if (!includeUpper && f < 0.0f && float.IsInfinity(f))
                        return DocIdSet.EMPTY_DOCIDSET;
                    int i = NumericUtils.FloatToSortableInt(f);
                    inclusiveUpperPoint = NumericUtils.SortableIntToFloat(includeUpper ? i : (i - 1));
                }
                else
                {
                    inclusiveUpperPoint = float.PositiveInfinity;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return DocIdSet.EMPTY_DOCIDSET;

                FieldCache.Floats values = FieldCache.DEFAULT.GetFloats((AtomicReader)context.Reader, field, (FieldCache.IFloatParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }
        
        [Serializable]
        private class AnonymousDoubleRangeFilter : FieldCacheRangeFilter<double?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Doubles values;
                private double inclusiveLowerPoint;
                private double inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Doubles values, double inclusiveLowerPoint, 
                    double inclusiveUpperPoint, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected override bool MatchDoc(int doc)
                {
                    double value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousDoubleRangeFilter(string field, FieldCache.IParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                // we transform the floating point numbers to sortable integers
                // using NumericUtils to easier find the next bigger/lower value
                double inclusiveLowerPoint;
                double inclusiveUpperPoint;
                if (lowerVal != null)
                {
                    double f = (double)lowerVal;
                    if (!includeUpper && f > 0.0 && double.IsInfinity(f))
                        return DocIdSet.EMPTY_DOCIDSET;
                    long i = NumericUtils.DoubleToSortableLong(f);
                    inclusiveLowerPoint = NumericUtils.SortableLongToDouble(includeLower ? i : (i + 1L));
                }
                else
                {
                    inclusiveLowerPoint = double.NegativeInfinity;
                }
                if (upperVal != null)
                {
                    double f = (double)upperVal;
                    if (!includeUpper && f < 0.0 && double.IsInfinity(f))
                        return DocIdSet.EMPTY_DOCIDSET;
                    long i = NumericUtils.DoubleToSortableLong(f);
                    inclusiveUpperPoint = NumericUtils.SortableLongToDouble(includeUpper ? i : (i - 1L));
                }
                else
                {
                    inclusiveUpperPoint = double.PositiveInfinity;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return DocIdSet.EMPTY_DOCIDSET;

                FieldCache.Doubles values = FieldCache.DEFAULT.GetDoubles((AtomicReader)context.Reader, field, (FieldCache.IDoubleParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

        /// <summary> Creates a string range filter using <see cref="FieldCache.GetStringIndex(IndexReader,string)" />. This works with all
        /// fields containing zero or one term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<string> NewStringRange(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousStringRangeFilter(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        public static FieldCacheRangeFilter<BytesRef> NewBytesRefRange(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousBytesRefRangeFilter(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range filter using <see cref="FieldCache.GetBytes(IndexReader,String)" />. This works with all
        /// byte fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
        {
            return NewByteRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range filter using <see cref="FieldCache.GetBytes(IndexReader,String,ByteParser)" />. This works with all
        /// byte fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, FieldCache.IByteParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousByteRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetShorts(IndexReader,String)" />. This works with all
        /// short fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<short?> NewShortRange(string field, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
        {
            return NewShortRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetShorts(IndexReader,String,ShortParser)" />. This works with all
        /// short fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<short?> NewShortRange(string field, FieldCache.IShortParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousShortRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetInts(IndexReader,String)" />. This works with all
        /// int fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<int?> NewIntRange(string field, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
        {
            return NewIntRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetInts(IndexReader,String,IntParser)" />. This works with all
        /// int fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<int?> NewIntRange(string field, FieldCache.IIntParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousIntRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetLongs(IndexReader,String)" />. This works with all
        /// long fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<long?> NewLongRange(string field, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
        {
            return NewLongRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetLongs(IndexReader,String,LongParser)" />. This works with all
        /// long fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<long?> NewLongRange(string field, FieldCache.ILongParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousLongRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetFloats(IndexReader,String)" />. This works with all
        /// float fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<float?> NewFloatRange(string field, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
        {
            return NewFloatRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetFloats(IndexReader,String,FloatParser)" />. This works with all
        /// float fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<float?> NewFloatRange(string field, FieldCache.IFloatParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousFloatRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetDoubles(IndexReader,String)" />. This works with all
        /// double fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
        {
            return NewDoubleRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary> Creates a numeric range query using <see cref="FieldCache.GetDoubles(IndexReader,String,DoubleParser)" />. This works with all
        /// double fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, FieldCache.IDoubleParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousDoubleRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }
    }

    [Serializable]
    public abstract class FieldCacheRangeFilter<T> : Filter
    {
        internal readonly string field;
        internal readonly FieldCache.IParser parser;
        internal readonly T lowerVal;
        internal readonly T upperVal;
        internal readonly bool includeLower;
        internal readonly bool includeUpper;

        protected internal FieldCacheRangeFilter(string field, FieldCache.IParser parser, T lowerVal, T upperVal,
            bool includeLower, bool includeUpper)
        {
            this.field = field;
            this.parser = parser;
            this.lowerVal = lowerVal;
            this.upperVal = upperVal;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
        }

        /// <summary>This method is implemented for each data type </summary>
        public abstract override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs);

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
            if (this == o)
                return true;
            if (!(o is FieldCacheRangeFilter<T>))
                return false;
            FieldCacheRangeFilter<T> other = (FieldCacheRangeFilter<T>)o;

            if (!this.field.Equals(other.field) || this.includeLower != other.includeLower || this.includeUpper != other.includeUpper)
            {
                return false;
            }
            if (this.lowerVal != null ? !this.lowerVal.Equals(other.lowerVal) : other.lowerVal != null)
                return false;
            if (this.upperVal != null ? !this.upperVal.Equals(other.upperVal) : other.upperVal != null)
                return false;
            if (this.parser != null ? !this.parser.Equals(other.parser) : other.parser != null)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int h = field.GetHashCode();
            h ^= ((lowerVal != null) ? lowerVal.GetHashCode() : 550356204);
            h = (h << 1) | (Number.URShift(h, 31)); // rotate to distinguish lower from upper
            h ^= ((upperVal != null) ? upperVal.GetHashCode() : -1674416163);
            h ^= ((parser != null) ? parser.GetHashCode() : -1572457324);
            h ^= (includeLower ? 1549299360 : -365038026) ^ (includeUpper ? 1721088258 : 1948649653);
            return h;
        }

        /// <summary>
        /// Returns the field name for this filter
        /// </summary>
        public string GetField { get { return field; } }

        /// <summary>
        /// Returns <c>true</c> if the lower endpoint is inclusive
        /// </summary>
        public bool IncludesLower { get { return includeLower; } }

        /// <summary>
        /// Returns <c>true</c> if the upper endpoint is inclusive
        /// </summary>
        public bool IncludesUpper { get { return includeUpper; } }

        /// <summary>
        /// Returns the lower value of the range filter
        /// </summary>
        public T LowerValue { get { return lowerVal; } }

        /// <summary>
        /// Returns the upper value of this range filter
        /// </summary>
        public T UpperValue { get { return upperVal; } }

        public FieldCache.IParser Parser { get { return parser; } }
    }
}