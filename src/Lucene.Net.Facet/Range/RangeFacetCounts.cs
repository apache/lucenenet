using System.Collections.Generic;

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
    ///  @lucene.experimental 
    /// </summary>
    public abstract class RangeFacetCounts : Facets
    {
        /// <summary>
        /// Ranges passed to constructor. </summary>
        protected internal readonly Range[] Ranges;

        /// <summary>
        /// Counts, initialized in by subclass. </summary>
        protected internal readonly int[] Counts;

        /// <summary>
        /// Optional: if specified, we first test this Filter to
        ///  see whether the document should be checked for
        ///  matching ranges.  If this is null, all documents are
        ///  checked. 
        /// </summary>
        protected internal readonly Filter FastMatchFilter;

        /// <summary>
        /// Our field name. </summary>
        protected internal readonly string Field;

        /// <summary>
        /// Total number of hits. </summary>
        protected internal int TotCount;

        /// <summary>
        /// Create {@code RangeFacetCounts} </summary>
        protected internal RangeFacetCounts(string field, Range[] ranges, Filter fastMatchFilter)
        {
            this.Field = field;
            this.Ranges = ranges;
            this.FastMatchFilter = fastMatchFilter;
            Counts = new int[ranges.Length];
        }

        public override FacetResult GetTopChildren(int topN, string dim, params string[] path)
        {
            if (dim.Equals(Field) == false)
            {
                throw new System.ArgumentException("invalid dim \"" + dim + "\"; should be \"" + Field + "\"");
            }
            if (path.Length != 0)
            {
                throw new System.ArgumentException("path.length should be 0");
            }
            LabelAndValue[] labelValues = new LabelAndValue[Counts.Length];
            for (int i = 0; i < Counts.Length; i++)
            {
                labelValues[i] = new LabelAndValue(Ranges[i].Label, Counts[i]);
            }
            return new FacetResult(dim, path, TotCount, labelValues, labelValues.Length);
        }

        public override float GetSpecificValue(string dim, params string[] path)
        {
            // TODO: should we impl this?
            throw new System.NotSupportedException();
        }

        public override IList<FacetResult> GetAllDims(int topN)
        {
            return new[] { GetTopChildren(topN, null) };
        }
    }
}