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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Filter = Lucene.Net.Search.Filter;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    /// <summary>
    /// Represents a range over long values.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public sealed class LongRange : Range
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
        /// Create a LongRange. </summary>
        public LongRange(string label, long minIn, bool minInclusive, long maxIn, bool maxInclusive)
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
            return new FilterAnonymousInnerClassHelper(this, fastMatchFilter, valueSource);
        }

        private class FilterAnonymousInnerClassHelper : Filter
        {
            private readonly LongRange outerInstance;

            private Filter fastMatchFilter;
            private ValueSource valueSource;

            public FilterAnonymousInnerClassHelper(LongRange outerInstance, Filter fastMatchFilter, ValueSource valueSource)
            {
                this.outerInstance = outerInstance;
                this.fastMatchFilter = fastMatchFilter;
                this.valueSource = valueSource;
            }


            public override string ToString()
            {
                return "Filter(" + outerInstance.ToString() + ")";
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {

                // TODO: this is just like ValueSourceScorer,
                // ValueSourceFilter (spatial),
                // ValueSourceRangeFilter (solr); also,
                // https://issues.apache.org/jira/browse/LUCENE-4251

                FunctionValues values = valueSource.GetValues(new Dictionary<string, object>(), context);

                int maxDoc = context.Reader.MaxDoc;

                Bits fastMatchBits;
                if (fastMatchFilter != null)
                {
                    DocIdSet dis = fastMatchFilter.GetDocIdSet(context, null);
                    if (dis == null)
                    {
                        // No documents match
                        return null;
                    }
                    fastMatchBits = dis.GetBits();
                    if (fastMatchBits == null)
                    {
                        throw new System.ArgumentException("fastMatchFilter does not implement DocIdSet.bits");
                    }
                }
                else
                {
                    fastMatchBits = null;
                }

                return new DocIdSetAnonymousInnerClassHelper(this, acceptDocs, values, maxDoc, fastMatchBits);
            }

            private class DocIdSetAnonymousInnerClassHelper : DocIdSet
            {
                private readonly FilterAnonymousInnerClassHelper outerInstance;

                private Bits acceptDocs;
                private FunctionValues values;
                private int maxDoc;
                private Bits fastMatchBits;

                public DocIdSetAnonymousInnerClassHelper(FilterAnonymousInnerClassHelper outerInstance, Bits acceptDocs, FunctionValues values, int maxDoc, Bits fastMatchBits)
                {
                    this.outerInstance = outerInstance;
                    this.acceptDocs = acceptDocs;
                    this.values = values;
                    this.maxDoc = maxDoc;
                    this.fastMatchBits = fastMatchBits;
                }


                public override Bits GetBits()
                {
                    return new BitsAnonymousInnerClassHelper(this);
                }

                private class BitsAnonymousInnerClassHelper : Bits
                {
                    private readonly DocIdSetAnonymousInnerClassHelper outerInstance;

                    public BitsAnonymousInnerClassHelper(DocIdSetAnonymousInnerClassHelper outerInstance)
                    {
                        this.outerInstance = outerInstance;
                    }

                    public virtual bool Get(int docID)
                    {
                        if (outerInstance.acceptDocs != null && outerInstance.acceptDocs.Get(docID) == false)
                        {
                            return false;
                        }
                        if (outerInstance.fastMatchBits != null && outerInstance.fastMatchBits.Get(docID) == false)
                        {
                            return false;
                        }
                        return outerInstance.outerInstance.outerInstance.Accept(outerInstance.values.LongVal(docID));
                    }


                    public virtual int Length()
                    {
                        return outerInstance.maxDoc;
                    }
                }

                public override DocIdSetIterator GetIterator()
                {
                    throw new System.NotSupportedException("this filter can only be accessed via bits()");
                }
            }
        }
    }
}