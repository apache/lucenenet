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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// Constrains search results to only match those which also match a provided
    /// query.
    ///
    /// <para/> This could be used, for example, with a <see cref="NumericRangeQuery"/> on a suitably
    /// formatted date field to implement date filtering.  One could re-use a single
    /// <c>CachingWrapperFilter(QueryWrapperFilter)</c> that matches, e.g., only documents modified
    /// within the last week.  This would only need to be reconstructed once per day.
    /// </summary>
    public class QueryWrapperFilter : Filter
    {
        private readonly Query query;

        /// <summary>
        /// Constructs a filter which only matches documents matching
        /// <paramref name="query"/>.
        /// </summary>
        public QueryWrapperFilter(Query query)
        {
            this.query = query ?? throw new ArgumentNullException(nameof(query), "Query may not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Returns the inner Query </summary>
        public Query Query => query;

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            // get a private context that is used to rewrite, createWeight and score eventually
            AtomicReaderContext privateContext = context.AtomicReader.AtomicContext;
            Weight weight = (new IndexSearcher(privateContext)).CreateNormalizedWeight(query);
            return new DocIdSetAnonymousClass(acceptDocs, privateContext, weight);
        }

        private sealed class DocIdSetAnonymousClass : DocIdSet
        {
            private readonly IBits acceptDocs;
            private readonly AtomicReaderContext privateContext;
            private readonly Weight weight;

            public DocIdSetAnonymousClass(IBits acceptDocs, AtomicReaderContext privateContext, Weight weight)
            {
                this.acceptDocs = acceptDocs;
                this.privateContext = privateContext;
                this.weight = weight;
            }

            public override DocIdSetIterator GetIterator()
            {
                return weight.GetScorer(privateContext, acceptDocs);
            }

            public override bool IsCacheable => false;
        }

        public override string ToString()
        {
            return "QueryWrapperFilter(" + query + ")";
        }

        public override bool Equals(object o)
        {
            if (!(o is QueryWrapperFilter))
            {
                return false;
            }
            return this.query.Equals(((QueryWrapperFilter)o).query);
        }

        public override int GetHashCode()
        {
            return query.GetHashCode() ^ unchecked((int)0x923F64B9);
        }
    }
}