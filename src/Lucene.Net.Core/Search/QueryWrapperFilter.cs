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
    using Bits = Lucene.Net.Util.Bits;

    /// <summary>
    /// Constrains search results to only match those which also match a provided
    /// query.
    ///
    /// <p> this could be used, for example, with a <seealso cref="NumericRangeQuery"/> on a suitably
    /// formatted date field to implement date filtering.  One could re-use a single
    /// CachingWrapperFilter(QueryWrapperFilter) that matches, e.g., only documents modified
    /// within the last week.  this would only need to be reconstructed once per day.
    /// </summary>
    public class QueryWrapperFilter : Filter
    {
        private readonly Query Query_Renamed; // LUCENENET TODO: Rename (private)

        /// <summary>
        /// Constructs a filter which only matches documents matching
        /// <code>query</code>.
        /// </summary>
        public QueryWrapperFilter(Query query)
        {
            if (query == null)
            {
                throw new System.NullReferenceException("Query may not be null");
            }
            this.Query_Renamed = query;
        }

        /// <summary>
        /// returns the inner Query </summary>
        public Query Query
        {
            get
            {
                return Query_Renamed;
            }
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
        {
            // get a private context that is used to rewrite, createWeight and score eventually
            AtomicReaderContext privateContext = context.AtomicReader.AtomicContext;
            Weight weight = (new IndexSearcher(privateContext)).CreateNormalizedWeight(Query_Renamed);
            return new DocIdSetAnonymousInnerClassHelper(this, acceptDocs, privateContext, weight);
        }

        private class DocIdSetAnonymousInnerClassHelper : DocIdSet
        {
            private readonly QueryWrapperFilter OuterInstance; // LUCENENET TODO: Rename (private)

            private Bits AcceptDocs; // LUCENENET TODO: Rename (private)
            private AtomicReaderContext PrivateContext; // LUCENENET TODO: Rename (private)
            private Lucene.Net.Search.Weight Weight; // LUCENENET TODO: Rename (private)

            public DocIdSetAnonymousInnerClassHelper(QueryWrapperFilter outerInstance, Bits acceptDocs, AtomicReaderContext privateContext, Lucene.Net.Search.Weight weight)
            {
                this.OuterInstance = outerInstance;
                this.AcceptDocs = acceptDocs;
                this.PrivateContext = privateContext;
                this.Weight = weight;
            }

            public override DocIdSetIterator GetIterator()
            {
                return Weight.Scorer(PrivateContext, AcceptDocs);
            }

            public override bool Cacheable
            {
                get
                {
                    return false;
                }
            }
        }

        public override string ToString()
        {
            return "QueryWrapperFilter(" + Query_Renamed + ")";
        }

        public override bool Equals(object o)
        {
            if (!(o is QueryWrapperFilter))
            {
                return false;
            }
            return this.Query_Renamed.Equals(((QueryWrapperFilter)o).Query_Renamed);
        }

        public override int GetHashCode()
        {
            return Query_Renamed.GetHashCode() ^ unchecked((int)0x923F64B9);
        }
    }
}