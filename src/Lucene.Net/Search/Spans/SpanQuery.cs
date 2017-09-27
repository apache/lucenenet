using System.Collections.Generic;

namespace Lucene.Net.Search.Spans
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
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Base class for span-based queries. </summary>
    public abstract class SpanQuery : Query
    {
        /// <summary>
        /// Expert: Returns the matches for this query in an index.  Used internally
        /// to search for spans.
        /// </summary>
        public abstract Spans GetSpans(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts);

        /// <summary>
        /// Returns the name of the field matched by this query.
        /// <para/>
        /// Note that this may return <c>null</c> if the query matches no terms.
        /// </summary>
        public abstract string Field { get; }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new SpanWeight(this, searcher);
        }
    }
}