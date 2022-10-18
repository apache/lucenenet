// Lucene version compatibility level 4.8.1
using Lucene.Net.Search;
using Lucene.Net.Support;
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Filter = Lucene.Net.Search.Filter;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using IBits = Lucene.Net.Util.IBits;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    /// <summary>
    /// Represents a range over double values.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public sealed class DoubleRange : Range
    {
        internal readonly double minIncl;
        internal readonly double maxIncl;

        /// <summary>
        /// Minimum. </summary>
        public double Min { get; private set; }

        /// <summary>
        /// Maximum. </summary>
        public double Max { get; private set; }

        /// <summary>
        /// True if the minimum value is inclusive. </summary>
        public bool MinInclusive { get; private set; }

        /// <summary>
        /// True if the maximum value is inclusive. </summary>
        public bool MaxInclusive { get; private set; }

        private const double EPSILON = 1E-14;
        /// <summary>
        /// Create a DoubleRange. </summary>
        public DoubleRange(string label, double minIn, bool minInclusive, double maxIn, bool maxInclusive)
            : base(label)
        {
            this.Min = minIn;
            this.Max = maxIn;
            this.MinInclusive = minInclusive;
            this.MaxInclusive = maxInclusive;

            // TODO: if DoubleDocValuesField used
            // NumericUtils.doubleToSortableLong format (instead of
            // Double.doubleToRawLongBits) we could do comparisons
            // in long space 

            if (double.IsNaN(Min))
            {
                throw new ArgumentOutOfRangeException(nameof(minIn), "min cannot be NaN"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (!minInclusive)
            {
                minIn += EPSILON;
            }

            if (double.IsNaN(Max))
            {
                throw new ArgumentOutOfRangeException(nameof(maxIn), "max cannot be NaN"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (!maxInclusive)
            {
                // Why no Math.nextDown?
                maxIn = maxIn -= EPSILON;
            }

            if (minIn > maxIn)
            {
                FailNoMatch();
            }

            this.minIncl = minIn;
            this.maxIncl = maxIn;
        }

        /// <summary>
        /// True if this range accepts the provided value. </summary>
        public bool Accept(double value)
        {
            return value >= minIncl && value <= maxIncl;
        }

        /// <summary>
        /// NOTE: This was toLongRange() in Lucene
        /// </summary>
        internal Int64Range ToInt64Range()
        {
            return new Int64Range(Label, NumericUtils.DoubleToSortableInt64(minIncl), true, NumericUtils.DoubleToSortableInt64(maxIncl), true);
        }

        public override string ToString()
        {
            return "DoubleRange(" + minIncl + " to " + maxIncl + ")";
        }

        public override Filter GetFilter(Filter fastMatchFilter, ValueSource valueSource)
        {
            return new FilterAnonymousClass(this, fastMatchFilter, valueSource);
        }

        private sealed class FilterAnonymousClass : Filter
        {
            private readonly DoubleRange outerInstance;

            private readonly Filter fastMatchFilter;
            private readonly ValueSource valueSource;

            public FilterAnonymousClass(DoubleRange outerInstance, Filter fastMatchFilter, ValueSource valueSource)
            {
                this.outerInstance = outerInstance;
                this.fastMatchFilter = fastMatchFilter;
                this.valueSource = valueSource;
            }

            public override string ToString()
            {
                return "Filter(" + outerInstance.ToString() + ")";
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {

                // TODO: this is just like ValueSourceScorer,
                // ValueSourceFilter (spatial),
                // ValueSourceRangeFilter (solr); also,
                // https://issues.apache.org/jira/browse/LUCENE-4251

                var values = valueSource.GetValues(Collections.EmptyMap<string, Scorer>(), context);

                int maxDoc = context.Reader.MaxDoc;

                IBits fastMatchBits;
                if (fastMatchFilter != null)
                {
                    DocIdSet dis = fastMatchFilter.GetDocIdSet(context, null);
                    if (dis is null)
                    {
                        // No documents match
                        return null;
                    }
                    fastMatchBits = dis.Bits;
                    if (fastMatchBits is null)
                    {
                        throw new ArgumentException("fastMatchFilter does not implement DocIdSet.Bits");
                    }
                }
                else
                {
                    fastMatchBits = null;
                }

                return new DocIdSetAnonymousClass(this, acceptDocs, values, maxDoc, fastMatchBits);
            }

            private sealed class DocIdSetAnonymousClass : DocIdSet
            {
                private readonly FilterAnonymousClass outerInstance;

                private readonly IBits acceptDocs;
                private readonly FunctionValues values;
                private readonly int maxDoc;
                private readonly IBits fastMatchBits;

                public DocIdSetAnonymousClass(FilterAnonymousClass outerInstance, IBits acceptDocs, FunctionValues values, int maxDoc, IBits fastMatchBits)
                {
                    this.outerInstance = outerInstance;
                    this.acceptDocs = acceptDocs;
                    this.values = values;
                    this.maxDoc = maxDoc;
                    this.fastMatchBits = fastMatchBits;
                }

                public override IBits Bits => new BitsAnonymousClass(this);

                private sealed class BitsAnonymousClass : IBits
                {
                    private readonly DocIdSetAnonymousClass outerInstance;

                    public BitsAnonymousClass(DocIdSetAnonymousClass outerInstance)
                    {
                        this.outerInstance = outerInstance;
                    }

                    public bool Get(int docID)
                    {
                        if (outerInstance.acceptDocs != null && outerInstance.acceptDocs.Get(docID) == false)
                        {
                            return false;
                        }
                        if (outerInstance.fastMatchBits != null && outerInstance.fastMatchBits.Get(docID) == false)
                        {
                            return false;
                        }
                        return outerInstance.outerInstance.outerInstance.Accept(outerInstance.values.DoubleVal(docID));
                    }

                    public int Length => outerInstance.maxDoc;
                }

                public override DocIdSetIterator GetIterator()
                {
                    throw UnsupportedOperationException.Create("this filter can only be accessed via Bits");
                }
            }
        }
    }
}