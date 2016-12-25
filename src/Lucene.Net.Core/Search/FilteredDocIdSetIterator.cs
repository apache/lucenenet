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

    /// <summary>
    /// Abstract decorator class of a DocIdSetIterator
    /// implementation that provides on-demand filter/validation
    /// mechanism on an underlying DocIdSetIterator.  See {@link
    /// FilteredDocIdSet}.
    /// </summary>
    public abstract class FilteredDocIdSetIterator : DocIdSetIterator
    {
        protected DocIdSetIterator _innerIter; // LUCENENET TODO: Rename
        private int Doc;

        /// <summary>
        /// Constructor. </summary>
        /// <param name="innerIter"> Underlying DocIdSetIterator. </param>
        public FilteredDocIdSetIterator(DocIdSetIterator innerIter)
        {
            if (innerIter == null)
            {
                throw new System.ArgumentException("null iterator");
            }
            _innerIter = innerIter;
            Doc = -1;
        }

        /// <summary>
        /// Validation method to determine whether a docid should be in the result set. </summary>
        /// <param name="doc"> docid to be tested </param>
        /// <returns> true if input docid should be in the result set, false otherwise. </returns>
        /// <seealso cref= #FilteredDocIdSetIterator(DocIdSetIterator) </seealso>
        protected abstract bool Match(int doc);

        public override int DocID
        {
            get { return Doc; }
        }

        public override int NextDoc()
        {
            while ((Doc = _innerIter.NextDoc()) != NO_MORE_DOCS)
            {
                if (Match(Doc))
                {
                    return Doc;
                }
            }
            return Doc;
        }

        public override int Advance(int target)
        {
            Doc = _innerIter.Advance(target);
            if (Doc != NO_MORE_DOCS)
            {
                if (Match(Doc))
                {
                    return Doc;
                }
                else
                {
                    while ((Doc = _innerIter.NextDoc()) != NO_MORE_DOCS)
                    {
                        if (Match(Doc))
                        {
                            return Doc;
                        }
                    }
                    return Doc;
                }
            }
            return Doc;
        }

        public override long Cost()
        {
            return _innerIter.Cost();
        }
    }
}