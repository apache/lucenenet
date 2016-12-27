using Lucene.Net.Index;

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
    ///  Abstract base class for restricting which documents may
    ///  be returned during searching.
    /// </summary>
    public abstract class Filter
    {
        /// <summary>
        /// Creates a <seealso cref="DocIdSet"/> enumerating the documents that should be
        /// permitted in search results. <b>NOTE:</b> null can be
        /// returned if no documents are accepted by this Filter.
        /// <p>
        /// Note: this method will be called once per segment in
        /// the index during searching.  The returned <seealso cref="DocIdSet"/>
        /// must refer to document IDs for that segment, not for
        /// the top-level reader.
        /// </summary>
        /// <param name="context"> a <seealso cref="AtomicReaderContext"/> instance opened on the index currently
        ///         searched on. Note, it is likely that the provided reader info does not
        ///         represent the whole underlying index i.e. if the index has more than
        ///         one segment the given reader only represents a single segment.
        ///         The provided context is always an atomic context, so you can call
        ///         <seealso cref="AtomicReader#fields()"/>
        ///         on the context's reader, for example.
        /// </param>
        /// <param name="acceptDocs">
        ///          Bits that represent the allowable docs to match (typically deleted docs
        ///          but possibly filtering other documents)
        /// </param>
        /// <returns> a DocIdSet that provides the documents which should be permitted or
        ///         prohibited in search results. <b>NOTE:</b> <code>null</code> should be returned if
        ///         the filter doesn't accept any documents otherwise internal optimization might not apply
        ///         in the case an <i>empty</i> <seealso cref="DocIdSet"/> is returned. </returns>
        public abstract DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs);
    }
}