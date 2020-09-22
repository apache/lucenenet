using Lucene.Net.Index;
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
    /// Abstract base class for restricting which documents may
    /// be returned during searching.
    /// </summary>
    public abstract class Filter
    {
        /// <summary>
        /// Creates a <see cref="DocIdSet"/> enumerating the documents that should be
        /// permitted in search results. <b>NOTE:</b> <c>null</c> can be
        /// returned if no documents are accepted by this <see cref="Filter"/>.
        /// <para/>
        /// Note: this method will be called once per segment in
        /// the index during searching.  The returned <see cref="DocIdSet"/>
        /// must refer to document IDs for that segment, not for
        /// the top-level reader.
        /// </summary>
        /// <param name="context"> a <see cref="AtomicReaderContext"/> instance opened on the index currently
        ///         searched on. Note, it is likely that the provided reader info does not
        ///         represent the whole underlying index i.e. if the index has more than
        ///         one segment the given reader only represents a single segment.
        ///         The provided context is always an atomic context, so you can call
        ///         <see cref="AtomicReader.Fields"/>
        ///         on the context's reader, for example.
        /// </param>
        /// <param name="acceptDocs">
        ///          <see cref="IBits"/> that represent the allowable docs to match (typically deleted docs
        ///          but possibly filtering other documents)
        /// </param>
        /// <returns> A <see cref="DocIdSet"/> that provides the documents which should be permitted or
        ///         prohibited in search results. <b>NOTE:</b> <c>null</c> should be returned if
        ///         the filter doesn't accept any documents otherwise internal optimization might not apply
        ///         in the case an <i>empty</i> <see cref="DocIdSet"/> is returned. </returns>
        public abstract DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs);


        /// <summary>
        /// Creates a new instance with the ability to specify the body of the <see cref="GetDocIdSet(AtomicReaderContext, IBits)"/>
        /// method through the <paramref name="getDocIdSet"/> parameter.
        /// Simple example:
        /// <code>
        ///     var filter = Filter.NewAnonymous(getDocIdSet: (context, acceptDocs) =>
        ///     {
        ///         if (acceptDocs is null) acceptDocs = new Bits.MatchAllBits(5);
        ///         OpenBitSet bitset = new OpenBitSet(5);
        ///         if (acceptDocs.Get(1)) bitset.Set(1);
        ///         if (acceptDocs.Get(3)) bitset.Set(3);
        ///         return new DocIdBitSet(bitset);
        ///     });
        /// </code>
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <param name="getDocIdSet">
        /// A delegate method that represents (is called by) the <see cref="GetDocIdSet(AtomicReaderContext, IBits)"/>
        /// method. It accepts a <see cref="AtomicReaderContext"/> context and a <see cref="IBits"/> acceptDocs and
        /// returns the <see cref="DocIdSet"/> for this filter.
        /// </param>
        /// <returns></returns>
        public static Filter NewAnonymous(Func<AtomicReaderContext, IBits, DocIdSet> getDocIdSet)
        {
            return new AnonymousFilter(getDocIdSet);
        }

        // LUCENENET specific
        private class AnonymousFilter : Filter
        {
            private readonly Func<AtomicReaderContext, IBits, DocIdSet> getDocIdSet;

            public AnonymousFilter(Func<AtomicReaderContext, IBits, DocIdSet> getDocIdSet)
            {
                this.getDocIdSet = getDocIdSet;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return this.getDocIdSet(context, acceptDocs);
            }
        }
    }
}