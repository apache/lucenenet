// Lucene version compatibility level 4.8.1
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
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    /// <summary>
    /// Represents a range over <see cref="long"/> values.
    /// <para/>
    /// NOTE: This was LongRange in Lucene
    /// 
    /// @lucene.experimental 
    /// </summary>
    public sealed class Int64Range : Range
    {
        internal readonly long minIncl;
        internal readonly long maxIncl;

        /// <summary>
        /// Minimum. </summary>
        public long Min { get; private set; }

        /// <summary>
        /// Maximum. </summary>
        public long Max { get; private set; }

        /// <summary>
        /// True if the minimum value is inclusive. </summary>
        public bool MinInclusive { get; private set; }

        /// <summary>
        /// True if the maximum value is inclusive. </summary>
        public bool MaxInclusive { get; private set; }

        // TODO: can we require fewer args? (same for
        // Double/FloatRange too)

        /// <summary>
        /// Create a <see cref="Int64Range"/>. </summary>
        public Int64Range(string label, long minIn, bool minInclusive, long maxIn, bool maxInclusive)
            : base(label)
        {
            this.Min = minIn;
            this.Max = maxIn;
            this.MinInclusive = minInclusive;
            this.MaxInclusive = maxInclusive;

            if (!minInclusive)
            {
                if (minIn != long.MaxValue)
                {
                    minIn++;
                }
                else
                {
                    FailNoMatch();
                }
            }

            if (!maxInclusive)
            {
                if (maxIn != long.MinValue)
                {
                    maxIn--;
                }
                else
                {
                    FailNoMatch();
                }
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
        public bool Accept(long value)
        {
            return value >= minIncl && value <= maxIncl;
        }

        public override string ToString()
        {
            return "LongRange(" + minIncl + " to " + maxIncl + ")";
        }

        public override Filter GetFilter(Filter fastMatchFilter, ValueSource valueSource)
        {
            return new FilterAnonymousClass(this, fastMatchFilter, valueSource);
        }

        private sealed class FilterAnonymousClass : Filter
        {
            private readonly Int64Range outerInstance;

            private readonly Filter fastMatchFilter;
            private readonly ValueSource valueSource;

            public FilterAnonymousClass(Int64Range outerInstance, Filter fastMatchFilter, ValueSource valueSource)
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

                FunctionValues values = valueSource.GetValues(Collections.EmptyMap<string, object>(), context);

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
                        return outerInstance.outerInstance.outerInstance.Accept(outerInstance.values.Int64Val(docID));
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