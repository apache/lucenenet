// Lucene version compatibility level 4.8.1
using System;

namespace Lucene.Net.Facet.Range
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

    using Filter = Lucene.Net.Search.Filter;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    /// <summary>
    /// Base class for a single labeled range.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public abstract class Range
    {
        /// <summary>
        /// Label that identifies this range. </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Sole constructor. </summary>
        protected Range(string label)
        {
            this.Label = label ?? throw new ArgumentNullException(nameof(label)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Returns a new <see cref="Filter"/> accepting only documents
        /// in this range.  This filter is not general-purpose;
        /// you should either use it with <see cref="DrillSideways"/> by
        /// adding it to <see cref="DrillDownQuery.Add(string, Filter)"/>, or pass it to
        /// <see cref="Search.FilteredQuery"/> using its 
        /// <see cref="Search.FilteredQuery.QUERY_FIRST_FILTER_STRATEGY"/>.
        /// If the <see cref="ValueSource"/> is static, e.g. an indexed numeric
        /// field, then it may be more efficient to use 
        /// <see cref="Search.NumericRangeFilter"/>.  The provided <paramref name="fastMatchFilter"/>,
        /// if non-null, will first be consulted, and only if
        /// that is set for each document will the range then be
        /// checked. 
        /// </summary>
        public abstract Filter GetFilter(Filter fastMatchFilter, ValueSource valueSource);

        /// <summary>
        /// Returns a new <see cref="Filter"/> accepting only documents
        ///  in this range.  This filter is not general-purpose;
        ///  you should either use it with <see cref="DrillSideways"/> by
        ///  adding it to <see cref="DrillDownQuery.Add(string, Filter)"/>, or pass it to
        ///  <see cref="Search.FilteredQuery"/> using its 
        ///  <see cref="Search.FilteredQuery.QUERY_FIRST_FILTER_STRATEGY"/>.  If the
        ///  <see cref="ValueSource"/> is static, e.g. an indexed numeric
        ///  field, then it may be more efficient to use <see cref="Search.NumericRangeFilter"/>. 
        /// </summary>
        public virtual Filter GetFilter(ValueSource valueSource)
        {
            return GetFilter(null, valueSource);
        }

        /// <summary>
        /// Invoke this for a useless range.
        /// </summary>
        protected virtual void FailNoMatch()
        {
            throw new ArgumentException("range \"" + Label + "\" matches nothing");
        }
    }
}