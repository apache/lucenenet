using System;

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

    /// <summary>
    /// A <see cref="Filter"/> that only accepts numeric values within
    /// a specified range. To use this, you must first index the
    /// numeric values using <see cref="Documents.Int32Field"/>, 
    /// <see cref="Documents.SingleField"/>, <see cref="Documents.Int64Field"/> or <see cref="Documents.DoubleField"/> (expert:
    /// <see cref="Analysis.NumericTokenStream"/>).
    ///
    /// <para/>You create a new <see cref="NumericRangeFilter"/> with the static
    /// factory methods, eg:
    ///
    /// <code>
    /// Filter f = NumericRangeFilter.NewFloatRange("weight", 0.03f, 0.10f, true, true);
    /// </code>
    ///
    /// Accepts all documents whose float valued "weight" field
    /// ranges from 0.03 to 0.10, inclusive.
    /// See <see cref="NumericRangeQuery"/> for details on how Lucene
    /// indexes and searches numeric valued fields.
    /// <para/>
    /// @since 2.9
    /// </summary>
    public sealed class NumericRangeFilter<T> : MultiTermQueryWrapperFilter<NumericRangeQuery<T>>
        where T : struct, IComparable<T>
    // real numbers in C# are structs and IComparable with themselves, best constraint we have
    {
        internal NumericRangeFilter(NumericRangeQuery<T> query)
            : base(query)
        {
        }

        // LUCENENET NOTE: Static methods were moved into NumericRangeFilter class

        /// <summary>
        /// Returns <c>true</c> if the lower endpoint is inclusive </summary>
        public bool IncludesMin => m_query.IncludesMin;

        /// <summary>
        /// Returns <c>true</c> if the upper endpoint is inclusive </summary>
        public bool IncludesMax => m_query.IncludesMax;

        /// <summary>
        /// Returns the lower value of this range filter </summary>
        public T? Min => m_query.Min;

        /// <summary>
        /// Returns the upper value of this range filter </summary>
        public T? Max => m_query.Max;

        /// <summary>
        /// Returns the precision step. </summary>
        public int PrecisionStep => m_query.PrecisionStep;
    }

    /// <summary>
    /// LUCENENET specific static class to provide access to static methods without referring to the
    /// <see cref="NumericRangeFilter{T}"/>'s generic closing type.
    /// </summary>
    public static class NumericRangeFilter
    {
        /// <summary>
        /// Factory that creates a <see cref="NumericRangeFilter"/>, that filters a <see cref="long"/>
        /// range using the given <see cref="NumericRangeQuery{T}.PrecisionStep"/>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newLongRange() in Lucene
        /// </summary>
        public static NumericRangeFilter<long> NewInt64Range(string field, int precisionStep, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<long>(NumericRangeQuery.NewInt64Range(field, precisionStep, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeFilter"/>, that queries a <see cref="long"/>
        /// range using the default <see cref="NumericRangeQuery{T}.PrecisionStep"/> <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newLongRange() in Lucene
        /// </summary>
        public static NumericRangeFilter<long> NewInt64Range(string field, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<long>(NumericRangeQuery.NewInt64Range(field, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeFilter"/>, that filters a <see cref="int"/>
        /// range using the given <see cref="NumericRangeQuery{T}.PrecisionStep"/>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newIntRange() in Lucene
        /// </summary>
        public static NumericRangeFilter<int> NewInt32Range(string field, int precisionStep, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<int>(NumericRangeQuery.NewInt32Range(field, precisionStep, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeFilter"/>, that queries a <see cref="int"/>
        /// range using the default <see cref="NumericRangeQuery{T}.PrecisionStep"/> <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newIntRange() in Lucene
        /// </summary>
        public static NumericRangeFilter<int> NewInt32Range(string field, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<int>(NumericRangeQuery.NewInt32Range(field, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeFilter"/>, that filters a <see cref="double"/>
        /// range using the given <see cref="NumericRangeQuery{T}.PrecisionStep"/>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>.
        /// <see cref="double.NaN"/> will never match a half-open range, to hit <c>NaN</c> use a query
        /// with <c>min == max == System.Double.NaN</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeFilter<double> NewDoubleRange(string field, int precisionStep, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<double>(NumericRangeQuery.NewDoubleRange(field, precisionStep, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeFilter"/>, that queries a <see cref="double"/>
        /// range using the default <see cref="NumericRangeQuery{T}.PrecisionStep"/> <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>.
        /// <see cref="double.NaN"/> will never match a half-open range, to hit <c>NaN</c> use a query
        /// with <c>min == max == System.Double.NaN</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeFilter<double> NewDoubleRange(string field, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<double>(NumericRangeQuery.NewDoubleRange(field, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeFilter"/>, that filters a <see cref="float"/>
        /// range using the given <see cref="NumericRangeQuery{T}.PrecisionStep"/>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>.
        /// <see cref="float.NaN"/> will never match a half-open range, to hit <c>NaN</c> use a query
        /// with <c>min == max == System.Single.NaN</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newFloatRange() in Lucene
        /// </summary>
        public static NumericRangeFilter<float> NewSingleRange(string field, int precisionStep, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<float>(NumericRangeQuery.NewSingleRange(field, precisionStep, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeFilter"/>, that queries a <see cref="float"/>
        /// range using the default <see cref="NumericRangeQuery{T}.PrecisionStep"/> <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>.
        /// <see cref="float.NaN"/> will never match a half-open range, to hit <c>NaN</c> use a query
        /// with <c>min == max == System.Single.NaN</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newFloatRange() in Lucene
        /// </summary>
        public static NumericRangeFilter<float> NewSingleRange(string field, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<float>(NumericRangeQuery.NewSingleRange(field, min, max, minInclusive, maxInclusive));
        }
    }
}