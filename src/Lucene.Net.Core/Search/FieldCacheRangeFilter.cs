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

    // for javadocs
    // for javadocs
    // for javadocs
    // for javadocs
    using AtomicReader = Lucene.Net.Index.AtomicReader; // for javadocs
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;

    /// <summary>
    /// A range filter built on top of a cached single term field (in <seealso cref="IFieldCache"/>).
    ///
    /// <p>{@code FieldCacheRangeFilter} builds a single cache for the field the first time it is used.
    /// Each subsequent {@code FieldCacheRangeFilter} on the same field then reuses this cache,
    /// even if the range itself changes.
    ///
    /// <p>this means that {@code FieldCacheRangeFilter} is much faster (sometimes more than 100x as fast)
    /// as building a <seealso cref="TermRangeFilter"/>, if using a <seealso cref="#newStringRange"/>.
    /// However, if the range never changes it is slower (around 2x as slow) than building
    /// a CachingWrapperFilter on top of a single <seealso cref="TermRangeFilter"/>.
    ///
    /// For numeric data types, this filter may be significantly faster than <seealso cref="NumericRangeFilter"/>.
    /// Furthermore, it does not need the numeric values encoded
    /// by <seealso cref="IntField"/>, <seealso cref="FloatField"/>, {@link
    /// LongField} or <seealso cref="DoubleField"/>. But
    /// it has the problem that it only works with exact one value/document (see below).
    ///
    /// <p>As with all <seealso cref="IFieldCache"/> based functionality, {@code FieldCacheRangeFilter} is only valid for
    /// fields which exact one term for each document (except for <seealso cref="#newStringRange"/>
    /// where 0 terms are also allowed). Due to a restriction of <seealso cref="IFieldCache"/>, for numeric ranges
    /// all terms that do not have a numeric value, 0 is assumed.
    ///
    /// <p>Thus it works on dates, prices and other single value fields but will not work on
    /// regular text fields. It is preferable to use a <code>NOT_ANALYZED</code> field to ensure that
    /// there is only a single term.
    ///
    /// <p>this class does not have an constructor, use one of the static factory methods available,
    /// that create a correct instance for different data types supported by <seealso cref="IFieldCache"/>.
    /// </summary>

    public static class FieldCacheRangeFilter
    {
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class AnonymousStringFieldCacheRangeFilter : FieldCacheRangeFilter<string>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private SortedDocValues fcsi;
                private int inclusiveLowerPoint;
                private int inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(SortedDocValues fcsi, int inclusiveLowerPoint, int inclusiveUpperPoint, int maxDoc, Bits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.fcsi = fcsi;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override bool MatchDoc(int doc)
                {
                    int docOrd = fcsi.GetOrd(doc);
                    return docOrd >= inclusiveLowerPoint && docOrd <= inclusiveUpperPoint;
                }
            }

            internal AnonymousStringFieldCacheRangeFilter(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
                : base(field, null, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, field);
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
                    return null;
                }

                Debug.Assert(inclusiveLowerPoint > 0 && inclusiveUpperPoint > 0);

                return new AnonymousClassFieldCacheDocIdSet(fcsi, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class AnonymousBytesRefFieldCacheRangeFilter : FieldCacheRangeFilter<BytesRef>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private SortedDocValues fcsi;
                private int inclusiveLowerPoint;
                private int inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(SortedDocValues fcsi, int inclusiveLowerPoint, int inclusiveUpperPoint, int maxDoc, Bits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.fcsi = fcsi;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override bool MatchDoc(int doc)
                {
                    int docOrd = fcsi.GetOrd(doc);
                    return docOrd >= inclusiveLowerPoint && docOrd <= inclusiveUpperPoint;
                }
            }

            internal AnonymousBytesRefFieldCacheRangeFilter(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
                : base(field, null, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, field);
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
                    return null; ;
                }

                //assert inclusiveLowerPoint >= 0 && inclusiveUpperPoint >= 0;
                return new AnonymousClassFieldCacheDocIdSet(fcsi, inclusiveLowerPoint, inclusiveUpperPoint, context.AtomicReader.MaxDoc, acceptDocs);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class AnonymousSbyteFieldCacheRangeFilter : FieldCacheRangeFilter<sbyte?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Bytes values;
                private sbyte inclusiveLowerPoint;
                private sbyte inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Bytes values, sbyte inclusiveLowerPoint, sbyte inclusiveUpperPoint, int maxDoc, Bits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override bool MatchDoc(int doc)
                {
                    sbyte value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousSbyteFieldCacheRangeFilter(string field, FieldCache.IParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                sbyte inclusiveLowerPoint, inclusiveUpperPoint;
                if (lowerVal != null)
                {
                    sbyte i = (sbyte)lowerVal;
                    if (!includeLower && i == sbyte.MaxValue)
                        return null;
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
                        return null;
                    inclusiveUpperPoint = (sbyte)(includeUpper ? i : (i - 1));
                }
                else
                {
                    inclusiveUpperPoint = sbyte.MaxValue;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return null;

                var values = FieldCache.DEFAULT.GetBytes(context.AtomicReader, field, (FieldCache.IByteParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.AtomicReader.MaxDoc, acceptDocs);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class AnonymousShortFieldCacheRangeFilter : FieldCacheRangeFilter<short?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Shorts values;
                private short inclusiveLowerPoint;
                private short inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Shorts values, short inclusiveLowerPoint, short inclusiveUpperPoint, int maxDoc, Bits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override bool MatchDoc(int doc)
                {
                    short value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousShortFieldCacheRangeFilter(string field, FieldCache.IParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                short inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal != null)
                {
                    short i = (short)lowerVal;
                    if (!includeLower && i == short.MaxValue)
                        return null;
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
                        return null;
                    inclusiveUpperPoint = (short)(includeUpper ? i : (i - 1));
                }
                else
                {
                    inclusiveUpperPoint = short.MaxValue;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return null;

                FieldCache.Shorts values = FieldCache.DEFAULT.GetShorts(context.AtomicReader, field, (FieldCache.IShortParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class AnonymousIntFieldCacheRangeFilter : FieldCacheRangeFilter<int?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Ints values;
                private int inclusiveLowerPoint;
                private int inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Ints values, int inclusiveLowerPoint, int inclusiveUpperPoint, int maxDoc, Bits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override bool MatchDoc(int doc)
                {
                    int value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousIntFieldCacheRangeFilter(string field, FieldCache.IParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                int inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal != null)
                {
                    int i = (int)lowerVal;
                    if (!includeLower && i == int.MaxValue)
                        return null;
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
                        return null;
                    inclusiveUpperPoint = includeUpper ? i : (i - 1);
                }
                else
                {
                    inclusiveUpperPoint = int.MaxValue;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return null;

                FieldCache.Ints values = FieldCache.DEFAULT.GetInts(context.AtomicReader, field, (FieldCache.IIntParser)parser, false);
                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class AnonymousLongFieldCacheRangeFilter : FieldCacheRangeFilter<long?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Longs values;
                private long inclusiveLowerPoint;
                private long inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Longs values, long inclusiveLowerPoint, long inclusiveUpperPoint, int maxDoc, Bits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override bool MatchDoc(int doc)
                {
                    long value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousLongFieldCacheRangeFilter(string field, FieldCache.IParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                long inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal != null)
                {
                    long i = (long)lowerVal;
                    if (!includeLower && i == long.MaxValue)
                        return null;
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
                        return null;
                    inclusiveUpperPoint = includeUpper ? i : (i - 1L);
                }
                else
                {
                    inclusiveUpperPoint = long.MaxValue;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return null;

                FieldCache.Longs values = FieldCache.DEFAULT.GetLongs(context.AtomicReader, field, (FieldCache.ILongParser)parser, false);
                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class AnonymousFloatFieldCacheRangeFilter : FieldCacheRangeFilter<float?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Floats values;
                private float inclusiveLowerPoint;
                private float inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Floats values, float inclusiveLowerPoint, float inclusiveUpperPoint, int maxDoc, Bits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override bool MatchDoc(int doc)
                {
                    float value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousFloatFieldCacheRangeFilter(string field, FieldCache.IParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                // we transform the floating point numbers to sortable integers
                // using NumericUtils to easier find the next bigger/lower value
                float inclusiveLowerPoint;
                float inclusiveUpperPoint;
                if (lowerVal != null)
                {
                    float f = (float)lowerVal;
                    if (!includeUpper && f > 0.0f && float.IsInfinity(f))
                        return null;
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
                        return null;
                    int i = NumericUtils.FloatToSortableInt(f);
                    inclusiveUpperPoint = NumericUtils.SortableIntToFloat(includeUpper ? i : (i - 1));
                }
                else
                {
                    inclusiveUpperPoint = float.PositiveInfinity;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return null;

                FieldCache.Floats values = FieldCache.DEFAULT.GetFloats(context.AtomicReader, field, (FieldCache.IFloatParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        private class AnonymousDoubleFieldCacheRangeFilter : FieldCacheRangeFilter<double?>
        {
            private class AnonymousClassFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private FieldCache.Doubles values;
                private double inclusiveLowerPoint;
                private double inclusiveUpperPoint;

                internal AnonymousClassFieldCacheDocIdSet(FieldCache.Doubles values, double inclusiveLowerPoint, double inclusiveUpperPoint, int maxDoc, Bits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.values = values;
                    this.inclusiveLowerPoint = inclusiveLowerPoint;
                    this.inclusiveUpperPoint = inclusiveUpperPoint;
                }

                protected internal override bool MatchDoc(int doc)
                {
                    double value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                }
            }

            internal AnonymousDoubleFieldCacheRangeFilter(string field, FieldCache.IParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                // we transform the floating point numbers to sortable integers
                // using NumericUtils to easier find the next bigger/lower value
                double inclusiveLowerPoint;
                double inclusiveUpperPoint;
                if (lowerVal != null)
                {
                    double f = (double)lowerVal;
                    if (!includeUpper && f > 0.0 && double.IsInfinity(f))
                        return null;
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
                        return null;
                    long i = NumericUtils.DoubleToSortableLong(f);
                    inclusiveUpperPoint = NumericUtils.SortableLongToDouble(includeUpper ? i : (i - 1L));
                }
                else
                {
                    inclusiveUpperPoint = double.PositiveInfinity;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return null;

                FieldCache.Doubles values = FieldCache.DEFAULT.GetDoubles(context.AtomicReader, field, (FieldCache.IDoubleParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new AnonymousClassFieldCacheDocIdSet(values, inclusiveLowerPoint, inclusiveUpperPoint, context.Reader.MaxDoc, acceptDocs);
            }
        }

        //The functions

        /// <summary>
        /// Creates a string range filter using <seealso cref="IFieldCache#getTermsIndex"/>. this works with all
        /// fields containing zero or one term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<string> NewStringRange(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousStringFieldCacheRangeFilter(field, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a BytesRef range filter using <seealso cref="IFieldCache#getTermsIndex"/>. this works with all
        /// fields containing zero or one term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        // TODO: bogus that newStringRange doesnt share this code... generics hell
        public static FieldCacheRangeFilter<BytesRef> NewBytesRefRange(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousBytesRefFieldCacheRangeFilter(field, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getBytes(AtomicReader,String,boolean)"/>. this works with all
        /// byte fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        [Obsolete]
        public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
        {
            return NewByteRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getBytes(AtomicReader,String,FieldCache.ByteParser,boolean)"/>. this works with all
        /// byte fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        [Obsolete]
        public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, FieldCache.IByteParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousSbyteFieldCacheRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getShorts(AtomicReader,String,boolean)"/>. this works with all
        /// short fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        [Obsolete]
        public static FieldCacheRangeFilter<short?> NewShortRange(string field, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
        {
            return NewShortRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getShorts(AtomicReader,String,FieldCache.ShortParser,boolean)"/>. this works with all
        /// short fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        [Obsolete]
        public static FieldCacheRangeFilter<short?> NewShortRange(string field, FieldCache.IShortParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousShortFieldCacheRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getInts(AtomicReader,String,boolean)"/>. this works with all
        /// int fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<int?> NewIntRange(string field, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
        {
            return NewIntRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getInts(AtomicReader,String,FieldCache.IntParser,boolean)"/>. this works with all
        /// int fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<int?> NewIntRange(string field, FieldCache.IIntParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousIntFieldCacheRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getLongs(AtomicReader,String,boolean)"/>. this works with all
        /// long fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<long?> NewLongRange(string field, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
        {
            return NewLongRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getLongs(AtomicReader,String,FieldCache.LongParser,boolean)"/>. this works with all
        /// long fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<long?> NewLongRange(string field, FieldCache.ILongParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousLongFieldCacheRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getFloats(AtomicReader,String,boolean)"/>. this works with all
        /// float fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<float?> NewFloatRange(string field, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
        {
            return NewFloatRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getFloats(AtomicReader,String,FieldCache.FloatParser,boolean)"/>. this works with all
        /// float fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<float?> NewFloatRange(string field, FieldCache.IFloatParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousFloatFieldCacheRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getDoubles(AtomicReader,String,boolean)"/>. this works with all
        /// double fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
        {
            return NewDoubleRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <seealso cref="IFieldCache#getDoubles(AtomicReader,String,FieldCache.DoubleParser,boolean)"/>. this works with all
        /// double fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <code>null</code>.
        /// </summary>
        public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, FieldCache.IDoubleParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
        {
            return new AnonymousDoubleFieldCacheRangeFilter(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }
    }

    public abstract class FieldCacheRangeFilter<T> : Filter
    {
        internal readonly string field;
        internal readonly FieldCache.IParser parser;
        internal readonly T lowerVal;
        internal readonly T upperVal;
        internal readonly bool includeLower;
        internal readonly bool includeUpper;

        protected internal FieldCacheRangeFilter(string field, FieldCache.IParser parser, T lowerVal, T upperVal, bool includeLower, bool includeUpper)
        {
            this.field = field;
            this.parser = parser;
            this.lowerVal = lowerVal;
            this.upperVal = upperVal;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
        }

        /// <summary>
        /// this method is implemented for each data type </summary>
        public override abstract DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs);

        /*
	  /// <summary>
	  /// Creates a string range filter using <seealso cref="FieldCache#getTermsIndex"/>. this works with all
	  /// fields containing zero or one term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<string> NewStringRange(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper(field, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper : FieldCacheRangeFilter<string>
	  {
		  private string Field;
		  private string LowerVal;
		  private string UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper) : base(field, null, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			SortedDocValues fcsi = FieldCache_Fields.DEFAULT.GetTermsIndex((context.AtomicReader), Field);
			int lowerPoint = LowerVal == null ? - 1 : fcsi.LookupTerm(new BytesRef(LowerVal));
			int upperPoint = UpperVal == null ? - 1 : fcsi.LookupTerm(new BytesRef(UpperVal));

			int inclusiveLowerPoint, inclusiveUpperPoint;

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
			  inclusiveUpperPoint = int.MaxValue;
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

			return new FieldCacheDocIdSetAnonymousInnerClassHelper(this, context.Reader.MaxDoc, acceptDocs, fcsi, inclusiveLowerPoint, inclusiveUpperPoint);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper OuterInstance;

			  private SortedDocValues Fcsi;
			  private int InclusiveLowerPoint;
			  private int InclusiveUpperPoint;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper(FieldCacheRangeFilterAnonymousInnerClassHelper outerInstance, int maxDoc, Bits acceptDocs, SortedDocValues fcsi, int inclusiveLowerPoint, int inclusiveUpperPoint) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.Fcsi = fcsi;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
			  }

			  protected internal override sealed bool MatchDoc(int doc)
			  {
				int docOrd = Fcsi.GetOrd(doc);
				return docOrd >= InclusiveLowerPoint && docOrd <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a BytesRef range filter using <seealso cref="FieldCache#getTermsIndex"/>. this works with all
	  /// fields containing zero or one term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  // TODO: bogus that newStringRange doesnt share this code... generics hell
	  public static FieldCacheRangeFilter<BytesRef> NewBytesRefRange(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper2(field, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper2 : FieldCacheRangeFilter<BytesRef>
	  {
		  private string Field;
		  private BytesRef LowerVal;
		  private BytesRef UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

		  public FieldCacheRangeFilterAnonymousInnerClassHelper2(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper) : base(field, null, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			SortedDocValues fcsi = FieldCache_Fields.DEFAULT.GetTermsIndex((context.AtomicReader), Field);
			int lowerPoint = LowerVal == null ? - 1 : fcsi.LookupTerm(LowerVal);
			int upperPoint = UpperVal == null ? - 1 : fcsi.LookupTerm(UpperVal);

			int inclusiveLowerPoint, inclusiveUpperPoint;

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
			  inclusiveUpperPoint = int.MaxValue;
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

			return new FieldCacheDocIdSetAnonymousInnerClassHelper2(this, context.Reader.MaxDoc, acceptDocs, fcsi, inclusiveLowerPoint, inclusiveUpperPoint);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper2 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper2 OuterInstance;

			  private SortedDocValues Fcsi;
			  private int InclusiveLowerPoint;
			  private int InclusiveUpperPoint;

			  public FieldCacheDocIdSetAnonymousInnerClassHelper2(FieldCacheRangeFilterAnonymousInnerClassHelper2 outerInstance, int maxDoc, Bits acceptDocs, SortedDocValues fcsi, int inclusiveLowerPoint, int inclusiveUpperPoint) : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.Fcsi = fcsi;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
			  }

			  protected internal override sealed bool MatchDoc(int doc)
			  {
				int docOrd = Fcsi.GetOrd(doc);
				return docOrd >= InclusiveLowerPoint && docOrd <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getBytes(AtomicReader,String,boolean)"/>. this works with all
	  /// byte fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  [Obsolete]
	  public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewByteRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getBytes(AtomicReader,String,FieldCache.ByteParser,boolean)"/>. this works with all
	  /// byte fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  [Obsolete]
      public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, FieldCache_Fields.IByteParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper3(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper3 : FieldCacheRangeFilter<sbyte?>
	  {
		  private string Field;
          private FieldCache_Fields.IByteParser Parser;
		  private sbyte? LowerVal;
		  private sbyte? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

          public FieldCacheRangeFilterAnonymousInnerClassHelper3(string field, FieldCache_Fields.IByteParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
              : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			sbyte inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  sbyte i = (sbyte)LowerVal;
			  if (!IncludeLower && i == sbyte.MaxValue)
			  {
				return null;
			  }
			  inclusiveLowerPoint = (sbyte)(IncludeLower ? i : (i + 1));
			}
			else
			{
			  inclusiveLowerPoint = sbyte.MinValue;
			}
			if (UpperVal != null)
			{
			  sbyte i = (sbyte)UpperVal;
			  if (!IncludeUpper && i == sbyte.MinValue)
			  {
				return null;
			  }
			  inclusiveUpperPoint = (sbyte)(IncludeUpper ? i : (i - 1));
			}
			else
			{
			  inclusiveUpperPoint = sbyte.MaxValue;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

            FieldCache_Fields.Bytes values = FieldCache_Fields.DEFAULT.GetBytes((context.AtomicReader), Field, (FieldCache_Fields.IByteParser)Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper3(this, context.Reader.MaxDoc, acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper3 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper3 OuterInstance;

			  private sbyte InclusiveLowerPoint;
			  private sbyte InclusiveUpperPoint;
              private FieldCache_Fields.Bytes Values;

              public FieldCacheDocIdSetAnonymousInnerClassHelper3(FieldCacheRangeFilterAnonymousInnerClassHelper3 outerInstance, int maxDoc, Bits acceptDocs, sbyte inclusiveLowerPoint, sbyte inclusiveUpperPoint, FieldCache_Fields.Bytes values)
                  : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				sbyte value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getShorts(AtomicReader,String,boolean)"/>. this works with all
	  /// short fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  [Obsolete]
	  public static FieldCacheRangeFilter<short?> NewShortRange(string field, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewShortRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getShorts(AtomicReader,String,FieldCache.ShortParser,boolean)"/>. this works with all
	  /// short fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  [Obsolete]
      public static FieldCacheRangeFilter<short?> NewShortRange(string field, FieldCache_Fields.IShortParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper4(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper4 : FieldCacheRangeFilter<short?>
	  {
		  private string Field;
          private FieldCache_Fields.IShortParser Parser;
		  private short? LowerVal;
		  private short? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

          public FieldCacheRangeFilterAnonymousInnerClassHelper4(string field, FieldCache_Fields.IShortParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
              : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			short inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  short i = (short)LowerVal;
			  if (!IncludeLower && i == short.MaxValue)
			  {
				return null;
			  }
			  inclusiveLowerPoint = (short)(IncludeLower ? i : (i + 1));
			}
			else
			{
			  inclusiveLowerPoint = short.MinValue;
			}
			if (UpperVal != null)
			{
			  short i = (short)UpperVal;
			  if (!IncludeUpper && i == short.MinValue)
			  {
				return null;
			  }
			  inclusiveUpperPoint = (short)(IncludeUpper ? i : (i - 1));
			}
			else
			{
			  inclusiveUpperPoint = short.MaxValue;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

            FieldCache_Fields.Shorts values = FieldCache_Fields.DEFAULT.GetShorts((context.AtomicReader), Field, (FieldCache_Fields.IShortParser)Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper4(this, context.Reader.MaxDoc, acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper4 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper4 OuterInstance;

			  private short InclusiveLowerPoint;
			  private short InclusiveUpperPoint;
              private FieldCache_Fields.Shorts Values;

              public FieldCacheDocIdSetAnonymousInnerClassHelper4(FieldCacheRangeFilterAnonymousInnerClassHelper4 outerInstance, int maxDoc, Bits acceptDocs, short inclusiveLowerPoint, short inclusiveUpperPoint, FieldCache_Fields.Shorts values)
                  : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				short value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getInts(AtomicReader,String,boolean)"/>. this works with all
	  /// int fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<int?> NewIntRange(string field, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewIntRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getInts(AtomicReader,String,FieldCache.IntParser,boolean)"/>. this works with all
	  /// int fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
      public static FieldCacheRangeFilter<int?> NewIntRange(string field, FieldCache_Fields.IIntParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper5(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper5 : FieldCacheRangeFilter<int?>
	  {
		  private string Field;
          private FieldCache_Fields.IIntParser Parser;
		  private int? LowerVal;
		  private int? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

          public FieldCacheRangeFilterAnonymousInnerClassHelper5(string field, FieldCache_Fields.IIntParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
              : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			int inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  int i = (int)LowerVal;
			  if (!IncludeLower && i == int.MaxValue)
			  {
				return null;
			  }
			  inclusiveLowerPoint = IncludeLower ? i : (i + 1);
			}
			else
			{
			  inclusiveLowerPoint = int.MinValue;
			}
			if (UpperVal != null)
			{
			  int i = (int)UpperVal;
			  if (!IncludeUpper && i == int.MinValue)
			  {
				return null;
			  }
			  inclusiveUpperPoint = IncludeUpper ? i : (i - 1);
			}
			else
			{
			  inclusiveUpperPoint = int.MaxValue;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

            FieldCache_Fields.Ints values = FieldCache_Fields.DEFAULT.GetInts((context.AtomicReader), Field, (FieldCache_Fields.IIntParser)Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper5(this, context.Reader.MaxDoc, acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper5 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper5 OuterInstance;

			  private int InclusiveLowerPoint;
			  private int InclusiveUpperPoint;
              private FieldCache_Fields.Ints Values;

              public FieldCacheDocIdSetAnonymousInnerClassHelper5(FieldCacheRangeFilterAnonymousInnerClassHelper5 outerInstance, int maxDoc, Bits acceptDocs, int inclusiveLowerPoint, int inclusiveUpperPoint, FieldCache_Fields.Ints values)
                  : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				int value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getLongs(AtomicReader,String,boolean)"/>. this works with all
	  /// long fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<long?> NewLongRange(string field, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewLongRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getLongs(AtomicReader,String,FieldCache.LongParser,boolean)"/>. this works with all
	  /// long fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
      public static FieldCacheRangeFilter<long?> NewLongRange(string field, FieldCache_Fields.ILongParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper6(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper6 : FieldCacheRangeFilter<long?>
	  {
		  private string Field;
          private FieldCache_Fields.ILongParser Parser;
		  private long? LowerVal;
		  private long? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

          public FieldCacheRangeFilterAnonymousInnerClassHelper6(string field, FieldCache_Fields.ILongParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
              : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			long inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  long i = (long)LowerVal;
			  if (!IncludeLower && i == long.MaxValue)
			  {
				return null;
			  }
			  inclusiveLowerPoint = IncludeLower ? i : (i + 1L);
			}
			else
			{
			  inclusiveLowerPoint = long.MinValue;
			}
			if (UpperVal != null)
			{
			  long i = (long)UpperVal;
			  if (!IncludeUpper && i == long.MinValue)
			  {
				return null;
			  }
			  inclusiveUpperPoint = IncludeUpper ? i : (i - 1L);
			}
			else
			{
			  inclusiveUpperPoint = long.MaxValue;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

            FieldCache_Fields.Longs values = FieldCache_Fields.DEFAULT.GetLongs((context.AtomicReader), Field, (FieldCache_Fields.ILongParser)Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper6(this, context.Reader.MaxDoc, acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper6 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper6 OuterInstance;

			  private long InclusiveLowerPoint;
			  private long InclusiveUpperPoint;
              private FieldCache_Fields.Longs Values;

              public FieldCacheDocIdSetAnonymousInnerClassHelper6(FieldCacheRangeFilterAnonymousInnerClassHelper6 outerInstance, int maxDoc, Bits acceptDocs, long inclusiveLowerPoint, long inclusiveUpperPoint, FieldCache_Fields.Longs values)
                  : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				long value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getFloats(AtomicReader,String,boolean)"/>. this works with all
	  /// float fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<float?> NewFloatRange(string field, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewFloatRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getFloats(AtomicReader,String,FieldCache.FloatParser,boolean)"/>. this works with all
	  /// float fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
      public static FieldCacheRangeFilter<float?> NewFloatRange(string field, FieldCache_Fields.IFloatParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper7(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper7 : FieldCacheRangeFilter<float?>
	  {
		  private string Field;
          private FieldCache_Fields.IFloatParser Parser;
		  private float? LowerVal;
		  private float? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

          public FieldCacheRangeFilterAnonymousInnerClassHelper7(string field, FieldCache_Fields.IFloatParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
              : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			// we transform the floating point numbers to sortable integers
			// using NumericUtils to easier find the next bigger/lower value
			float inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  float f = (float)LowerVal;
			  if (!IncludeUpper && f > 0.0f && float.IsInfinity(f))
			  {
				return null;
			  }
			  int i = NumericUtils.FloatToSortableInt(f);
			  inclusiveLowerPoint = NumericUtils.SortableIntToFloat(IncludeLower ? i : (i + 1));
			}
			else
			{
			  inclusiveLowerPoint = float.NegativeInfinity;
			}
			if (UpperVal != null)
			{
			  float f = (float)UpperVal;
			  if (!IncludeUpper && f < 0.0f && float.IsInfinity(f))
			  {
				return null;
			  }
			  int i = NumericUtils.FloatToSortableInt(f);
			  inclusiveUpperPoint = NumericUtils.SortableIntToFloat(IncludeUpper ? i : (i - 1));
			}
			else
			{
			  inclusiveUpperPoint = float.PositiveInfinity;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

            FieldCache_Fields.Floats values = FieldCache_Fields.DEFAULT.GetFloats((context.AtomicReader), Field, (FieldCache_Fields.IFloatParser)Parser, false);
			return new FieldCacheDocIdSetAnonymousInnerClassHelper7(this, context.Reader.MaxDoc, acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper7 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper7 OuterInstance;

			  private float InclusiveLowerPoint;
			  private float InclusiveUpperPoint;
              private FieldCache_Fields.Floats Values;

              public FieldCacheDocIdSetAnonymousInnerClassHelper7(FieldCacheRangeFilterAnonymousInnerClassHelper7 outerInstance, int maxDoc, Bits acceptDocs, float inclusiveLowerPoint, float inclusiveUpperPoint, FieldCache_Fields.Floats values)
                  : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				float value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getDoubles(AtomicReader,String,boolean)"/>. this works with all
	  /// double fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
	  public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
	  {
		return NewDoubleRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  /// <summary>
	  /// Creates a numeric range filter using <seealso cref="FieldCache#getDoubles(AtomicReader,String,FieldCache.DoubleParser,boolean)"/>. this works with all
	  /// double fields containing exactly one numeric term in the field. The range can be half-open by setting one
	  /// of the values to <code>null</code>.
	  /// </summary>
      public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, FieldCache_Fields.IDoubleParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
	  {
		return new FieldCacheRangeFilterAnonymousInnerClassHelper8(field, parser, lowerVal, upperVal, includeLower, includeUpper);
	  }

	  private class FieldCacheRangeFilterAnonymousInnerClassHelper8 : FieldCacheRangeFilter<double?>
	  {
		  private string Field;
          private FieldCache_Fields.IDoubleParser Parser;
		  private double? LowerVal;
		  private double? UpperVal;
		  private bool IncludeLower;
		  private bool IncludeUpper;

          public FieldCacheRangeFilterAnonymousInnerClassHelper8(string field, FieldCache_Fields.IDoubleParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
              : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
		  {
			  this.Field = field;
			  this.Parser = parser;
			  this.LowerVal = lowerVal;
			  this.UpperVal = upperVal;
			  this.IncludeLower = includeLower;
			  this.IncludeUpper = includeUpper;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			// we transform the floating point numbers to sortable integers
			// using NumericUtils to easier find the next bigger/lower value
			double inclusiveLowerPoint, inclusiveUpperPoint;
			if (LowerVal != null)
			{
			  double f = (double)LowerVal;
			  if (!IncludeUpper && f > 0.0 && double.IsInfinity(f))
			  {
				return null;
			  }
			  long i = NumericUtils.DoubleToSortableLong(f);
			  inclusiveLowerPoint = NumericUtils.SortableLongToDouble(IncludeLower ? i : (i + 1L));
			}
			else
			{
			  inclusiveLowerPoint = double.NegativeInfinity;
			}
			if (UpperVal != null)
			{
			  double f = (double)UpperVal;
			  if (!IncludeUpper && f < 0.0 && double.IsInfinity(f))
			  {
				return null;
			  }
			  long i = NumericUtils.DoubleToSortableLong(f);
			  inclusiveUpperPoint = NumericUtils.SortableLongToDouble(IncludeUpper ? i : (i - 1L));
			}
			else
			{
			  inclusiveUpperPoint = double.PositiveInfinity;
			}

			if (inclusiveLowerPoint > inclusiveUpperPoint)
			{
			  return null;
			}

            FieldCache_Fields.Doubles values = FieldCache_Fields.DEFAULT.GetDoubles((context.AtomicReader), Field, (FieldCache_Fields.IDoubleParser)Parser, false);
			// ignore deleted docs if range doesn't contain 0
			return new FieldCacheDocIdSetAnonymousInnerClassHelper8(this, context.Reader.MaxDoc, acceptDocs, inclusiveLowerPoint, inclusiveUpperPoint, values);
		  }

		  private class FieldCacheDocIdSetAnonymousInnerClassHelper8 : FieldCacheDocIdSet
		  {
			  private readonly FieldCacheRangeFilterAnonymousInnerClassHelper8 OuterInstance;

			  private double InclusiveLowerPoint;
			  private double InclusiveUpperPoint;
              private FieldCache_Fields.Doubles Values;

              public FieldCacheDocIdSetAnonymousInnerClassHelper8(FieldCacheRangeFilterAnonymousInnerClassHelper8 outerInstance, int maxDoc, Bits acceptDocs, double inclusiveLowerPoint, double inclusiveUpperPoint, FieldCache_Fields.Doubles values)
                  : base(maxDoc, acceptDocs)
			  {
                  this.OuterInstance = outerInstance;
				  this.InclusiveLowerPoint = inclusiveLowerPoint;
				  this.InclusiveUpperPoint = inclusiveUpperPoint;
				  this.Values = values;
			  }

			  protected internal override bool MatchDoc(int doc)
			  {
				double value = Values.Get(doc);
				return value >= InclusiveLowerPoint && value <= InclusiveUpperPoint;
			  }
		  }
	  }*/

        public override sealed string ToString()
        {
            StringBuilder sb = (new StringBuilder(field)).Append(":");
            return sb.Append(includeLower ? '[' : '{').Append((lowerVal == null) ? "*" : lowerVal.ToString()).Append(" TO ").Append((upperVal == null) ? "*" : upperVal.ToString()).Append(includeUpper ? ']' : '}').ToString();
        }

        public override sealed bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is FieldCacheRangeFilter<T>))
            {
                return false;
            }
            FieldCacheRangeFilter<T> other = (FieldCacheRangeFilter<T>)o;

            if (!this.field.Equals(other.field) || this.includeLower != other.includeLower || this.includeUpper != other.includeUpper)
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
            if (this.parser != null ? !this.parser.Equals(other.parser) : other.parser != null)
            {
                return false;
            }
            return true;
        }

        public override sealed int GetHashCode()
        {
            int h = field.GetHashCode();
            h ^= (lowerVal != null) ? lowerVal.GetHashCode() : 550356204;
            h = (h << 1) | ((int)((uint)h >> 31)); // rotate to distinguish lower from upper
            h ^= (upperVal != null) ? upperVal.GetHashCode() : -1674416163;
            h ^= (parser != null) ? parser.GetHashCode() : -1572457324;
            h ^= (includeLower ? 1549299360 : -365038026) ^ (includeUpper ? 1721088258 : 1948649653);
            return h;
        }

        /// <summary>
        /// Returns the field name for this filter </summary>
        public virtual string Field
        {
            get
            {
                return field;
            }
        }

        /// <summary>
        /// Returns <code>true</code> if the lower endpoint is inclusive </summary>
        public virtual bool IncludesLower
        {
            get { return includeLower; }
        }

        /// <summary>
        /// Returns <code>true</code> if the upper endpoint is inclusive </summary>
        public virtual bool IncludesUpper
        {
            get { return includeUpper; }
        }

        /// <summary>
        /// Returns the lower value of this range filter </summary>
        public virtual T LowerVal
        {
            get
            {
                return lowerVal;
            }
        }

        /// <summary>
        /// Returns the upper value of this range filter </summary>
        public virtual T UpperVal
        {
            get
            {
                return upperVal;
            }
        }

        /// <summary>
        /// Returns the current numeric parser ({@code null} for {@code T} is {@code String}} </summary>
        public virtual FieldCache.IParser Parser
        {
            get
            {
                return parser;
            }
        }
    }
}