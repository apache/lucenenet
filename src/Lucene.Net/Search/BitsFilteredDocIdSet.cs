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

    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// This implementation supplies a filtered <see cref="DocIdSet"/>, that excludes all
    /// docids which are not in a <see cref="IBits"/> instance. This is especially useful in
    /// <see cref="Lucene.Net.Search.Filter"/> to apply the <see cref="acceptDocs"/>
    /// passed to <see cref="Filter.GetDocIdSet(Index.AtomicReaderContext, IBits)"/> before returning the final <see cref="DocIdSet"/>.
    /// </summary>
    /// <seealso cref="DocIdSet"/>
    /// <seealso cref="Lucene.Net.Search.Filter"/>
    public sealed class BitsFilteredDocIdSet : FilteredDocIdSet
    {
        private readonly IBits acceptDocs;

        /// <summary>
        /// Convenience wrapper method: If <c>acceptDocs is null</c> it returns the original set without wrapping. </summary>
        /// <param name="set"> Underlying DocIdSet. If <c>null</c>, this method returns <c>null</c> </param>
        /// <param name="acceptDocs"> Allowed docs, all docids not in this set will not be returned by this <see cref="DocIdSet"/>.
        /// If <c>null</c>, this method returns the original set without wrapping. </param>
        public static DocIdSet Wrap(DocIdSet set, IBits acceptDocs)
        {
            return (set is null || acceptDocs is null) ? set : new BitsFilteredDocIdSet(set, acceptDocs);
        }

        /// <summary>
        /// Constructor. </summary>
        /// <param name="innerSet"> Underlying <see cref="DocIdSet"/> </param>
        /// <param name="acceptDocs"> Allowed docs, all docids not in this set will not be returned by this <see cref="DocIdSet"/> </param>
        public BitsFilteredDocIdSet(DocIdSet innerSet, IBits acceptDocs)
            : base(innerSet)
        {
            this.acceptDocs = acceptDocs ?? throw new ArgumentNullException(nameof(acceptDocs), "acceptDocs can not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        protected override bool Match(int docid)
        {
            return acceptDocs.Get(docid);
        }
    }
}