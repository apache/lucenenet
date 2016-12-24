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

    using Bits = Lucene.Net.Util.Bits;

    /// <summary>
    /// this implementation supplies a filtered DocIdSet, that excludes all
    /// docids which are not in a Bits instance. this is especially useful in
    /// <seealso cref="Lucene.Net.Search.Filter"/> to apply the {@code acceptDocs}
    /// passed to {@code getDocIdSet()} before returning the final DocIdSet.
    /// </summary>
    /// <seealso cref= DocIdSet </seealso>
    /// <seealso cref= Lucene.Net.Search.Filter </seealso>

    public sealed class BitsFilteredDocIdSet : FilteredDocIdSet
    {
        private readonly Bits acceptDocs;

        /// <summary>
        /// Convenience wrapper method: If {@code acceptDocs == null} it returns the original set without wrapping. </summary>
        /// <param name="set"> Underlying DocIdSet. If {@code null}, this method returns {@code null} </param>
        /// <param name="acceptDocs"> Allowed docs, all docids not in this set will not be returned by this DocIdSet.
        /// If {@code null}, this method returns the original set without wrapping. </param>
        public static DocIdSet Wrap(DocIdSet set, Bits acceptDocs)
        {
            return (set == null || acceptDocs == null) ? set : new BitsFilteredDocIdSet(set, acceptDocs);
        }

        /// <summary>
        /// Constructor. </summary>
        /// <param name="innerSet"> Underlying DocIdSet </param>
        /// <param name="acceptDocs"> Allowed docs, all docids not in this set will not be returned by this DocIdSet </param>
        public BitsFilteredDocIdSet(DocIdSet innerSet, Bits acceptDocs)
            : base(innerSet)
        {
            if (acceptDocs == null)
            {
                throw new System.NullReferenceException("acceptDocs is null");
            }
            this.acceptDocs = acceptDocs;
        }

        protected override bool Match(int docid)
        {
            return acceptDocs.Get(docid);
        }
    }
}