using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;

    /// <summary>
    /// A range filter built on top of a cached single term field (in <see cref="IFieldCache"/>).
    ///
    /// <para/><see cref="FieldCacheRangeFilter"/> builds a single cache for the field the first time it is used.
    /// Each subsequent <see cref="FieldCacheRangeFilter"/> on the same field then reuses this cache,
    /// even if the range itself changes.
    ///
    /// <para/>this means that <see cref="FieldCacheRangeFilter"/> is much faster (sometimes more than 100x as fast)
    /// as building a <see cref="TermRangeFilter"/>, if using a <see cref="NewStringRange(string, string, string, bool, bool)"/>.
    /// However, if the range never changes it is slower (around 2x as slow) than building
    /// a <see cref="CachingWrapperFilter"/> on top of a single <see cref="TermRangeFilter"/>.
    ///
    /// <para/>For numeric data types, this filter may be significantly faster than <see cref="NumericRangeFilter"/>.
    /// Furthermore, it does not need the numeric values encoded
    /// by <see cref="Documents.Int32Field"/>, <see cref="Documents.SingleField"/>,
    /// <see cref="Documents.Int64Field"/> or <see cref="Documents.DoubleField"/>. But
    /// it has the problem that it only works with exact one value/document (see below).
    ///
    /// <para/>As with all <see cref="IFieldCache"/> based functionality, <see cref="FieldCacheRangeFilter"/> is only valid for
    /// fields which exact one term for each document (except for <see cref="NewStringRange(string, string, string, bool, bool)"/>
    /// where 0 terms are also allowed). Due to a restriction of <see cref="IFieldCache"/>, for numeric ranges
    /// all terms that do not have a numeric value, 0 is assumed.
    ///
    /// <para/>Thus it works on dates, prices and other single value fields but will not work on
    /// regular text fields. It is preferable to use a <see cref="Documents.Field.Index.NOT_ANALYZED"/> field to ensure that
    /// there is only a single term.
    ///
    /// <para/>This class does not have an constructor, use one of the static factory methods available,
    /// that create a correct instance for different data types supported by <see cref="IFieldCache"/>.
    /// </summary>

    public static class FieldCacheRangeFilter
    {
        private sealed class StringFieldCacheRangeFilterAnonymousClass : FieldCacheRangeFilter<string>
        {
            internal StringFieldCacheRangeFilterAnonymousClass(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
                : base(field, null, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, field);
                int lowerPoint = lowerVal is null ? -1 : fcsi.LookupTerm(new BytesRef(lowerVal));
                int upperPoint = upperVal is null ? -1 : fcsi.LookupTerm(new BytesRef(upperVal));

                int inclusiveLowerPoint, inclusiveUpperPoint;

                // Hints:
                // * binarySearchLookup returns 0, if value was null.
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

                if (Debugging.AssertsEnabled) Debugging.Assert(inclusiveLowerPoint >= 0 && inclusiveUpperPoint >= 0);

                return new FieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, (doc) =>
                {
                    int docOrd = fcsi.GetOrd(doc);
                    return docOrd >= inclusiveLowerPoint && docOrd <= inclusiveUpperPoint;
                });
            }
        }

        private sealed class BytesRefFieldCacheRangeFilterAnonymousClass : FieldCacheRangeFilter<BytesRef>
        {
            internal BytesRefFieldCacheRangeFilterAnonymousClass(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
                : base(field, null, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, field);
                int lowerPoint = lowerVal is null ? -1 : fcsi.LookupTerm(lowerVal);
                int upperPoint = upperVal is null ? -1 : fcsi.LookupTerm(upperVal);

                int inclusiveLowerPoint, inclusiveUpperPoint;

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

                if (Debugging.AssertsEnabled) Debugging.Assert(inclusiveLowerPoint >= 0 && inclusiveUpperPoint >= 0);

                return new FieldCacheDocIdSet(context.AtomicReader.MaxDoc, acceptDocs, (doc) =>
                {
                    int docOrd = fcsi.GetOrd(doc);
                    return docOrd >= inclusiveLowerPoint && docOrd <= inclusiveUpperPoint;
                });
            }
        }

        private sealed class SByteFieldCacheRangeFilterAnonymousClass : FieldCacheRangeFilter<sbyte?>
        {
            internal SByteFieldCacheRangeFilterAnonymousClass(string field, FieldCache.IParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                sbyte inclusiveLowerPoint, inclusiveUpperPoint;
                if (lowerVal.HasValue)
                {
                    sbyte i = lowerVal.Value;
                    if (!includeLower && i == sbyte.MaxValue)
                        return null;
                    inclusiveLowerPoint = (sbyte)(includeLower ? i : (i + 1));
                }
                else
                {
                    inclusiveLowerPoint = sbyte.MinValue;
                }
                if (upperVal.HasValue)
                {
                    sbyte i = upperVal.Value;
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

#pragma warning disable 612, 618
                var values = FieldCache.DEFAULT.GetBytes(context.AtomicReader, field, (FieldCache.IByteParser)parser, false);
#pragma warning restore 612, 618

                // we only request the usage of termDocs, if the range contains 0
                return new FieldCacheDocIdSet(context.AtomicReader.MaxDoc, acceptDocs, (doc) =>
                {
                    sbyte value = (sbyte)values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                });
            }

            // LUCENENET specific - since we use value types, we need to use special handling to avoid boxing.
            protected override bool Equals(sbyte? objA, sbyte? objB)
            {
                if (!objA.HasValue) return !objB.HasValue;
                else if (!objB.HasValue) return false;
                return JCG.EqualityComparer<sbyte>.Default.Equals(objA.Value, objB.Value);
            }
        }

        private sealed class Int16FieldCacheRangeFilterAnonymousClass : FieldCacheRangeFilter<short?>
        {
            internal Int16FieldCacheRangeFilterAnonymousClass(string field, FieldCache.IParser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                short inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal.HasValue)
                {
                    short i = lowerVal.Value;
                    if (!includeLower && i == short.MaxValue)
                        return null;
                    inclusiveLowerPoint = (short)(includeLower ? i : (i + 1));
                }
                else
                {
                    inclusiveLowerPoint = short.MinValue;
                }
                if (upperVal.HasValue)
                {
                    short i = upperVal.Value;
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

#pragma warning disable 612, 618
                FieldCache.Int16s values = FieldCache.DEFAULT.GetInt16s(context.AtomicReader, field, (FieldCache.IInt16Parser)parser, false);
#pragma warning restore 612, 618

                // we only request the usage of termDocs, if the range contains 0
                return new FieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, (doc) =>
                {
                    short value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                });
            }

            // LUCENENET specific - since we use value types, we need to use special handling to avoid boxing.
            protected override bool Equals(short? objA, short? objB)
            {
                if (!objA.HasValue) return !objB.HasValue;
                else if (!objB.HasValue) return false;
                return JCG.EqualityComparer<short>.Default.Equals(objA.Value, objB.Value);
            }
        }

        private sealed class Int32FieldCacheRangeFilterAnonymousClass : FieldCacheRangeFilter<int?>
        {
            internal Int32FieldCacheRangeFilterAnonymousClass(string field, FieldCache.IParser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                int inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal.HasValue)
                {
                    int i = lowerVal.Value;
                    if (!includeLower && i == int.MaxValue)
                        return null;
                    inclusiveLowerPoint = includeLower ? i : (i + 1);
                }
                else
                {
                    inclusiveLowerPoint = int.MinValue;
                }
                if (upperVal.HasValue)
                {
                    int i = upperVal.Value;
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

                FieldCache.Int32s values = FieldCache.DEFAULT.GetInt32s(context.AtomicReader, field, (FieldCache.IInt32Parser)parser, false);
                // we only request the usage of termDocs, if the range contains 0
                return new FieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, (doc) =>
                {
                    int value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                });
            }

            // LUCENENET specific - since we use value types, we need to use special handling to avoid boxing.
            protected override bool Equals(int? objA, int? objB)
            {
                if (!objA.HasValue) return !objB.HasValue;
                else if (!objB.HasValue) return false;
                return JCG.EqualityComparer<int>.Default.Equals(objA.Value, objB.Value);
            }
        }

        private sealed class Int64FieldCacheRangeFilterAnonymousClass : FieldCacheRangeFilter<long?>
        {
            internal Int64FieldCacheRangeFilterAnonymousClass(string field, FieldCache.IParser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                long inclusiveLowerPoint, inclusiveUpperPoint;

                if (lowerVal.HasValue)
                {
                    long i = lowerVal.Value;
                    if (!includeLower && i == long.MaxValue)
                        return null;
                    inclusiveLowerPoint = includeLower ? i : (i + 1L);
                }
                else
                {
                    inclusiveLowerPoint = long.MinValue;
                }
                if (upperVal.HasValue)
                {
                    long i = upperVal.Value;
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

                FieldCache.Int64s values = FieldCache.DEFAULT.GetInt64s(context.AtomicReader, field, (FieldCache.IInt64Parser)parser, false);
                // we only request the usage of termDocs, if the range contains 0
                return new FieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, (doc) =>
                {
                    long value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                });
            }

            // LUCENENET specific - since we use value types, we need to use special handling to avoid boxing.
            protected override bool Equals(long? objA, long? objB)
            {
                if (!objA.HasValue) return !objB.HasValue;
                else if (!objB.HasValue) return false;
                return JCG.EqualityComparer<long>.Default.Equals(objA.Value, objB.Value);
            }
        }

        private sealed class SingleFieldCacheRangeFilterAnonymousClass : FieldCacheRangeFilter<float?>
        {
            internal SingleFieldCacheRangeFilterAnonymousClass(string field, FieldCache.IParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                // we transform the floating point numbers to sortable integers
                // using NumericUtils to easier find the next bigger/lower value
                float inclusiveLowerPoint;
                float inclusiveUpperPoint;
                if (lowerVal.HasValue)
                {
                    float f = lowerVal.Value;
                    if (!includeUpper && f > 0.0f && float.IsInfinity(f))
                        return null;
                    int i = NumericUtils.SingleToSortableInt32(f);
                    inclusiveLowerPoint = NumericUtils.SortableInt32ToSingle(includeLower ? i : (i + 1));
                }
                else
                {
                    inclusiveLowerPoint = float.NegativeInfinity;
                }
                if (upperVal.HasValue)
                {
                    float f = upperVal.Value;
                    if (!includeUpper && f < 0.0f && float.IsInfinity(f))
                        return null;
                    int i = NumericUtils.SingleToSortableInt32(f);
                    inclusiveUpperPoint = NumericUtils.SortableInt32ToSingle(includeUpper ? i : (i - 1));
                }
                else
                {
                    inclusiveUpperPoint = float.PositiveInfinity;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return null;

                FieldCache.Singles values = FieldCache.DEFAULT.GetSingles(context.AtomicReader, field, (FieldCache.ISingleParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new FieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, (doc) =>
                {
                    float value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                });
            }

            // LUCENENET specific - since we use value types, we need to use special handling to avoid boxing.
            protected override bool Equals(float? objA, float? objB)
            {
                if (!objA.HasValue) return !objB.HasValue;
                else if (!objB.HasValue) return false;
                return JCG.EqualityComparer<float>.Default.Equals(objA.Value, objB.Value);
            }
        }

        private sealed class DoubleFieldCacheRangeFilterAnonymousClass : FieldCacheRangeFilter<double?>
        {
            internal DoubleFieldCacheRangeFilterAnonymousClass(string field, FieldCache.IParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
                : base(field, parser, lowerVal, upperVal, includeLower, includeUpper)
            {
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                // we transform the floating point numbers to sortable integers
                // using NumericUtils to easier find the next bigger/lower value
                double inclusiveLowerPoint;
                double inclusiveUpperPoint;
                if (lowerVal.HasValue)
                {
                    double f = lowerVal.Value;
                    if (!includeUpper && f > 0.0 && double.IsInfinity(f))
                        return null;
                    long i = NumericUtils.DoubleToSortableInt64(f);
                    inclusiveLowerPoint = NumericUtils.SortableInt64ToDouble(includeLower ? i : (i + 1L));
                }
                else
                {
                    inclusiveLowerPoint = double.NegativeInfinity;
                }
                if (upperVal.HasValue)
                {
                    double f = upperVal.Value;
                    if (!includeUpper && f < 0.0 && double.IsInfinity(f))
                        return null;
                    long i = NumericUtils.DoubleToSortableInt64(f);
                    inclusiveUpperPoint = NumericUtils.SortableInt64ToDouble(includeUpper ? i : (i - 1L));
                }
                else
                {
                    inclusiveUpperPoint = double.PositiveInfinity;
                }

                if (inclusiveLowerPoint > inclusiveUpperPoint)
                    return null;

                FieldCache.Doubles values = FieldCache.DEFAULT.GetDoubles(context.AtomicReader, field, (FieldCache.IDoubleParser)parser, false);

                // we only request the usage of termDocs, if the range contains 0
                return new FieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, (doc) =>
                {
                    double value = values.Get(doc);
                    return value >= inclusiveLowerPoint && value <= inclusiveUpperPoint;
                });
            }

            // LUCENENET specific - since we use value types, we need to use special handling to avoid boxing.
            protected override bool Equals(double? objA, double? objB)
            {
                if (!objA.HasValue) return !objB.HasValue;
                else if (!objB.HasValue) return false;
                return JCG.EqualityComparer<double>.Default.Equals(objA.Value, objB.Value);
            }
        }

        //The functions (Starting on line 84 in Lucene)

        /// <summary>
        /// Creates a string range filter using <see cref="IFieldCache.GetTermsIndex(Index.AtomicReader, string, float)"/>. This works with all
        /// fields containing zero or one term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<string> NewStringRange(string field, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            return new StringFieldCacheRangeFilterAnonymousClass(field, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a <see cref="BytesRef"/> range filter using <see cref="IFieldCache.GetTermsIndex(Index.AtomicReader, string, float)"/>. This works with all
        /// fields containing zero or one term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        // TODO: bogus that newStringRange doesnt share this code... generics hell
        public static FieldCacheRangeFilter<BytesRef> NewBytesRefRange(string field, BytesRef lowerVal, BytesRef upperVal, bool includeLower, bool includeUpper)
        {
            return new BytesRefFieldCacheRangeFilterAnonymousClass(field, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetBytes(Index.AtomicReader,string,bool)"/>. This works with all
        /// <see cref="byte"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        [Obsolete, CLSCompliant(false)] // LUCENENET NOTE: marking non-CLS compliant because it is sbyte, but obsolete anyway
        public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
        {
            return NewByteRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetBytes(Index.AtomicReader,string,FieldCache.IByteParser,bool)"/>. This works with all
        /// <see cref="byte"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        [Obsolete, CLSCompliant(false)]  // LUCENENET NOTE: marking non-CLS compliant because it is sbyte, but obsolete anyway
        public static FieldCacheRangeFilter<sbyte?> NewByteRange(string field, FieldCache.IByteParser parser, sbyte? lowerVal, sbyte? upperVal, bool includeLower, bool includeUpper)
        {
            return new SByteFieldCacheRangeFilterAnonymousClass(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetInt16s(Index.AtomicReader,string,bool)"/>. This works with all
        /// <see cref="short"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// <para/>
        /// NOTE: this was newShortRange() in Lucene
        /// </summary>
        [Obsolete]
        public static FieldCacheRangeFilter<short?> NewInt16Range(string field, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
        {
            return NewInt16Range(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetInt16s(Index.AtomicReader, string, FieldCache.IInt16Parser, bool)"/>. This works with all
        /// <see cref="short"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// <para/>
        /// NOTE: this was newShortRange() in Lucene
        /// </summary>
        [Obsolete]
        public static FieldCacheRangeFilter<short?> NewInt16Range(string field, FieldCache.IInt16Parser parser, short? lowerVal, short? upperVal, bool includeLower, bool includeUpper)
        {
            return new Int16FieldCacheRangeFilterAnonymousClass(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetInt32s(Index.AtomicReader,string,bool)"/>. This works with all
        /// <see cref="int"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// <para/>
        /// NOTE: this was newIntRange() in Lucene
        /// </summary>
        public static FieldCacheRangeFilter<int?> NewInt32Range(string field, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
        {
            return NewInt32Range(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetInt32s(Index.AtomicReader,string,FieldCache.IInt32Parser,bool)"/>. This works with all
        /// <see cref="int"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// <para/>
        /// NOTE: this was newIntRange() in Lucene
        /// </summary>
        public static FieldCacheRangeFilter<int?> NewInt32Range(string field, FieldCache.IInt32Parser parser, int? lowerVal, int? upperVal, bool includeLower, bool includeUpper)
        {
            return new Int32FieldCacheRangeFilterAnonymousClass(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetInt64s(Index.AtomicReader,string,bool)"/>. This works with all
        /// <see cref="long"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<long?> NewInt64Range(string field, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
        {
            return NewInt64Range(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetInt64s(Index.AtomicReader,string,FieldCache.IInt64Parser,bool)"/>. This works with all
        /// <see cref="long"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// <para/>
        /// NOTE: this was newLongRange() in Lucene
        /// </summary>
        public static FieldCacheRangeFilter<long?> NewInt64Range(string field, FieldCache.IInt64Parser parser, long? lowerVal, long? upperVal, bool includeLower, bool includeUpper)
        {
            return new Int64FieldCacheRangeFilterAnonymousClass(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetSingles(Index.AtomicReader,string,bool)"/>. This works with all
        /// <see cref="float"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// <para/>
        /// NOTE: this was newFloatRange() in Lucene
        /// </summary>
        public static FieldCacheRangeFilter<float?> NewSingleRange(string field, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
        {
            return NewSingleRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetSingles(Index.AtomicReader,string,FieldCache.ISingleParser,bool)"/>. This works with all
        /// <see cref="float"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// <para/>
        /// NOTE: this was newFloatRange() in Lucene
        /// </summary>
        public static FieldCacheRangeFilter<float?> NewSingleRange(string field, FieldCache.ISingleParser parser, float? lowerVal, float? upperVal, bool includeLower, bool includeUpper)
        {
            return new SingleFieldCacheRangeFilterAnonymousClass(field, parser, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetDoubles(Index.AtomicReader,string,bool)"/>. This works with all
        /// <see cref="double"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
        {
            return NewDoubleRange(field, null, lowerVal, upperVal, includeLower, includeUpper);
        }

        /// <summary>
        /// Creates a numeric range filter using <see cref="IFieldCache.GetDoubles(Index.AtomicReader,string,FieldCache.IDoubleParser,bool)"/>. This works with all
        /// <see cref="double"/> fields containing exactly one numeric term in the field. The range can be half-open by setting one
        /// of the values to <c>null</c>.
        /// </summary>
        public static FieldCacheRangeFilter<double?> NewDoubleRange(string field, FieldCache.IDoubleParser parser, double? lowerVal, double? upperVal, bool includeLower, bool includeUpper)
        {
            return new DoubleFieldCacheRangeFilterAnonymousClass(field, parser, lowerVal, upperVal, includeLower, includeUpper);
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

        private protected FieldCacheRangeFilter(string field, FieldCache.IParser parser, T lowerVal, T upperVal, bool includeLower, bool includeUpper)
        {
            this.field = field;
            this.parser = parser;
            this.lowerVal = lowerVal;
            this.upperVal = upperVal;
            this.includeLower = includeLower;
            this.includeUpper = includeUpper;
        }

        /// <summary>
        /// This method is implemented for each data type </summary>
        public override abstract DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs);



        // From line 516 in Lucene
        public override sealed string ToString()
        {
            StringBuilder sb = (new StringBuilder(field)).Append(':');
            return sb.Append(includeLower ? '[' : '{').Append((lowerVal is null) ? "*" : lowerVal.ToString()).Append(" TO ").Append((upperVal is null) ? "*" : upperVal.ToString()).Append(includeUpper ? ']' : '}').ToString();
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

            if (!this.field.Equals(other.field, StringComparison.Ordinal) || this.includeLower != other.includeLower || this.includeUpper != other.includeUpper)
            {
                return false;
            }
            // LUCENENET specific - since we use value types, we need to use special handling to avoid boxing.
            if (!Equals(this.lowerVal, other.lowerVal))
            {
                return false;
            }
            // LUCENENET specific - since we use value types, we need to use special handling to avoid boxing.
            if (!Equals(this.upperVal, other.upperVal))
            {
                return false;
            }
            if (this.parser != null ? !this.parser.Equals(other.parser) : other.parser != null)
            {
                return false;
            }
            return true;
        }

        // LUCENENET specific - override this method to eliminate boxing on value types
        protected virtual bool Equals(T objA, T objB)
        {
            return objA != null ? objA.Equals(objB) : objB != null;
        }

        public override sealed int GetHashCode()
        {
            int h = field.GetHashCode();
            h ^= (lowerVal != null) ? lowerVal.GetHashCode() : 550356204;
            h = (h << 1) | (h.TripleShift(31)); // rotate to distinguish lower from upper
            h ^= (upperVal != null) ? upperVal.GetHashCode() : -1674416163;
            h ^= (parser != null) ? parser.GetHashCode() : -1572457324;
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
        public virtual T LowerVal => lowerVal;

        /// <summary>
        /// Returns the upper value of this range filter </summary>
        public virtual T UpperVal => upperVal;

        /// <summary>
        /// Returns the current numeric parser (<c>null</c> for <typeparamref name="T"/> is <see cref="string"/>) </summary>
        public virtual FieldCache.IParser Parser => parser;
    }
}