using System.Diagnostics;

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
    /// this abstract class defines methods to iterate over a set of non-decreasing
    /// doc ids. Note that this class assumes it iterates on doc Ids, and therefore
    /// <seealso cref="#NO_MORE_DOCS"/> is set to {@value #NO_MORE_DOCS} in order to be used as
    /// a sentinel object. Implementations of this class are expected to consider
    /// <seealso cref="Integer#MAX_VALUE"/> as an invalid value.
    /// </summary>
    public abstract class DocIdSetIterator
    {
        /// <summary>
        /// An empty {@code DocIdSetIterator} instance </summary>
        public static DocIdSetIterator Empty() // LUCENENET TODO: Rename GetEmpty() ? Empty() is a verb that makes it confusing
        {
            return new DocIdSetIteratorAnonymousInnerClassHelper();
        }

        private class DocIdSetIteratorAnonymousInnerClassHelper : DocIdSetIterator
        {
            public DocIdSetIteratorAnonymousInnerClassHelper()
            {
            }

            internal bool exhausted = false;

            public override int Advance(int target)
            {
                Debug.Assert(!exhausted);
                Debug.Assert(target >= 0);
                exhausted = true;
                return NO_MORE_DOCS;
            }

            public override int DocID()
            {
                return exhausted ? NO_MORE_DOCS : -1;
            }

            public override int NextDoc()
            {
                Debug.Assert(!exhausted);
                exhausted = true;
                return NO_MORE_DOCS;
            }

            public override long Cost()
            {
                return 0;
            }
        }

        /// <summary>
        /// When returned by <seealso cref="#nextDoc()"/>, <seealso cref="#advance(int)"/> and
        /// <seealso cref="#docID()"/> it means there are no more docs in the iterator.
        /// </summary>
        public const int NO_MORE_DOCS = int.MaxValue;

        /// <summary>
        /// Returns the following:
        /// <ul>
        /// <li>-1 or <seealso cref="#NO_MORE_DOCS"/> if <seealso cref="#nextDoc()"/> or
        /// <seealso cref="#advance(int)"/> were not called yet.
        /// <li><seealso cref="#NO_MORE_DOCS"/> if the iterator has exhausted.
        /// <li>Otherwise it should return the doc ID it is currently on.
        /// </ul>
        /// <p>
        ///
        /// @since 2.9
        /// </summary>
        public abstract int DocID(); // LUCENENET TODO: Change to property getter

        /// <summary>
        /// Advances to the next document in the set and returns the doc it is
        /// currently on, or <seealso cref="#NO_MORE_DOCS"/> if there are no more docs in the
        /// set.<br>
        ///
        /// <b>NOTE:</b> after the iterator has exhausted you should not call this
        /// method, as it may result in unpredicted behavior.
        ///
        /// @since 2.9
        /// </summary>
        public abstract int NextDoc();

        /// <summary>
        /// Advances to the first beyond the current whose document number is greater
        /// than or equal to <i>target</i>, and returns the document number itself.
        /// Exhausts the iterator and returns <seealso cref="#NO_MORE_DOCS"/> if <i>target</i>
        /// is greater than the highest document number in the set.
        /// <p>
        /// The behavior of this method is <b>undefined</b> when called with
        /// <code> target &lt;= current</code>, or after the iterator has exhausted.
        /// Both cases may result in unpredicted behavior.
        /// <p>
        /// When <code> target &gt; current</code> it behaves as if written:
        ///
        /// <pre class="prettyprint">
        /// int advance(int target) {
        ///   int doc;
        ///   while ((doc = nextDoc()) &lt; target) {
        ///   }
        ///   return doc;
        /// }
        /// </pre>
        ///
        /// Some implementations are considerably more efficient than that.
        /// <p>
        /// <b>NOTE:</b> this method may be called with <seealso cref="#NO_MORE_DOCS"/> for
        /// efficiency by some Scorers. If your implementation cannot efficiently
        /// determine that it should exhaust, it is recommended that you check for that
        /// value in each call to this method.
        /// <p>
        ///
        /// @since 2.9
        /// </summary>
        public abstract int Advance(int target);

        /// <summary>
        /// Slow (linear) implementation of <seealso cref="#advance"/> relying on
        ///  <seealso cref="#nextDoc()"/> to advance beyond the target position.
        /// </summary>
        protected internal int SlowAdvance(int target)
        {
            Debug.Assert(DocID() == NO_MORE_DOCS || DocID() < target); // can happen when the enum is not positioned yet
            int doc;
            do
            {
                doc = NextDoc();
            } while (doc < target);
            return doc;
        }

        /// <summary>
        /// Returns the estimated cost of this <seealso cref="DocIdSetIterator"/>.
        /// <p>
        /// this is generally an upper bound of the number of documents this iterator
        /// might match, but may be a rough heuristic, hardcoded value, or otherwise
        /// completely inaccurate.
        /// </summary>
        public abstract long Cost(); // LUCENENET TODO: Change to GetCost() ? Should be consistent with Scorer.GetScore()
    }
}