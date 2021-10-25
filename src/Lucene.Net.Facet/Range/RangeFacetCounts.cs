// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// Base class for range faceting.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public abstract class RangeFacetCounts : Facets
    {
        /// <summary>
        /// Ranges passed to constructor. </summary>
        protected readonly Range[] m_ranges;

        /// <summary>
        /// Counts, initialized in by subclass. </summary>
        protected readonly int[] m_counts;

        /// <summary>
        /// Optional: if specified, we first test this Filter to
        /// see whether the document should be checked for
        /// matching ranges.  If this is null, all documents are
        /// checked. 
        /// </summary>
        protected readonly Filter m_fastMatchFilter;

        /// <summary>
        /// Our field name. </summary>
        protected readonly string m_field;

        /// <summary>
        /// Total number of hits. </summary>
        protected int m_totCount;

        /// <summary>
        /// Create <see cref="RangeFacetCounts"/> </summary>
        protected RangeFacetCounts(string field, Range[] ranges, Filter fastMatchFilter)
        {
            this.m_field = field;
            this.m_ranges = ranges;
            this.m_fastMatchFilter = fastMatchFilter;
            m_counts = new int[ranges.Length];
        }

        public override FacetResult GetTopChildren(int topN, string dim, params string[] path)
        {
            if (dim.Equals(m_field, StringComparison.Ordinal) == false)
            {
                throw new ArgumentException("invalid dim \"" + dim + "\"; should be \"" + m_field + "\"");
            }
            if (path.Length != 0)
            {
                throw new ArgumentException("path.Length should be 0");
            }
            LabelAndValue[] labelValues = new LabelAndValue[m_counts.Length];
            for (int i = 0; i < m_counts.Length; i++)
            {
                labelValues[i] = new LabelAndValue(m_ranges[i].Label, m_counts[i]);
            }
            return new FacetResult(dim, path, m_totCount, labelValues, labelValues.Length);
        }

        public override float GetSpecificValue(string dim, params string[] path)
        {
            // TODO: should we impl this?
            throw UnsupportedOperationException.Create();
        }

        public override IList<FacetResult> GetAllDims(int topN)
        {
            return new JCG.List<FacetResult> { GetTopChildren(topN, null) };
        }
    }
}