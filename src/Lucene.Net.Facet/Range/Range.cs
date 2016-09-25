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
    ///  @lucene.experimental 
    /// </summary>
    public abstract class Range
    {

        /// <summary>
        /// Label that identifies this range. </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Sole constructor. </summary>
        protected internal Range(string label)
        {
            if (label == null)
            {
                throw new System.NullReferenceException("label cannot be null");
            }
            this.Label = label;
        }

        /// <summary>
        /// Returns a new <seealso cref="Filter"/> accepting only documents
        ///  in this range.  This filter is not general-purpose;
        ///  you should either use it with <seealso cref="DrillSideways"/> by
        ///  adding it to <seealso cref="DrillDownQuery#add"/>, or pass it to
        ///  <seealso cref="FilteredQuery"/> using its {@link
        ///  FilteredQuery#QUERY_FIRST_FILTER_STRATEGY}.  If the
        ///  <seealso cref="ValueSource"/> is static, e.g. an indexed numeric
        ///  field, then it may be more efficient to use {@link
        ///  NumericRangeFilter}.  The provided fastMatchFilter,
        ///  if non-null, will first be consulted, and only if
        ///  that is set for each document will the range then be
        ///  checked. 
        /// </summary>
        public abstract Filter GetFilter(Filter fastMatchFilter, ValueSource valueSource);

        /// <summary>
        /// Returns a new <seealso cref="Filter"/> accepting only documents
        ///  in this range.  This filter is not general-purpose;
        ///  you should either use it with <seealso cref="DrillSideways"/> by
        ///  adding it to <seealso cref="DrillDownQuery#add"/>, or pass it to
        ///  <seealso cref="FilteredQuery"/> using its {@link
        ///  FilteredQuery#QUERY_FIRST_FILTER_STRATEGY}.  If the
        ///  <seealso cref="ValueSource"/> is static, e.g. an indexed numeric
        ///  field, then it may be more efficient to use <seealso cref="NumericRangeFilter"/>. 
        /// </summary>
        public virtual Filter GetFilter(ValueSource valueSource)
        {
            return GetFilter(null, valueSource);
        }

        /// <summary>
        /// Invoke this for a useless range. </summary>
        protected internal virtual void FailNoMatch()
        {
            throw new System.ArgumentException("range \"" + Label + "\" matches nothing");
        }
    }
}