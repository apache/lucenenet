using Lucene.Net.Diagnostics;

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
    /// This abstract class defines methods to iterate over a set of non-decreasing
    /// doc ids. Note that this class assumes it iterates on doc Ids, and therefore
    /// <see cref="NO_MORE_DOCS"/> is set to <see cref="int.MaxValue"/> in order to be used as
    /// a sentinel object. Implementations of this class are expected to consider
    /// <see cref="int.MaxValue"/> as an invalid value.
    /// </summary>
    public abstract class DocIdSetIterator
    {
        /// <summary>
        /// An empty <see cref="DocIdSetIterator"/> instance </summary>
        public static DocIdSetIterator GetEmpty()
        {
            return new DocIdSetIteratorAnonymousClass();
        }

        private sealed class DocIdSetIteratorAnonymousClass : DocIdSetIterator
        {
            public DocIdSetIteratorAnonymousClass()
            {
            }

            internal bool exhausted = false;

            public override int Advance(int target)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(!exhausted);
                    Debugging.Assert(target >= 0);
                }
                exhausted = true;
                return NO_MORE_DOCS;
            }

            public override int DocID => exhausted ? NO_MORE_DOCS : -1;

            public override int NextDoc()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!exhausted);
                exhausted = true;
                return NO_MORE_DOCS;
            }

            public override long GetCost()
            {
                return 0;
            }
        }

        /// <summary>
        /// When returned by <see cref="NextDoc()"/>, <see cref="Advance(int)"/> and
        /// <see cref="DocID()"/> it means there are no more docs in the iterator.
        /// </summary>
        public const int NO_MORE_DOCS = int.MaxValue;

        /// <summary>
        /// Returns the following:
        /// <list type="bullet">
        /// <item><description>-1 or <see cref="NO_MORE_DOCS"/> if <see cref="NextDoc()"/> or
        /// <seealso cref="Advance(int)"/> were not called yet.</description></item>
        /// <item><description><see cref="NO_MORE_DOCS"/> if the iterator has exhausted.</description></item>
        /// <item><description>Otherwise it should return the doc ID it is currently on.</description></item>
        /// </list>
        /// <para/>
        ///
        /// @since 2.9
        /// </summary>
        public abstract int DocID { get; }

        /// <summary>
        /// Advances to the next document in the set and returns the doc it is
        /// currently on, or <see cref="NO_MORE_DOCS"/> if there are no more docs in the
        /// set.
        ///
        /// <para/><b>NOTE:</b> after the iterator has exhausted you should not call this
        /// method, as it may result in unpredicted behavior.
        /// <para/>
        /// @since 2.9
        /// </summary>
        public abstract int NextDoc();

        /// <summary>
        /// Advances to the first beyond the current whose document number is greater
        /// than or equal to <i>target</i>, and returns the document number itself.
        /// Exhausts the iterator and returns <see cref="NO_MORE_DOCS"/> if <i>target</i>
        /// is greater than the highest document number in the set.
        /// <para/>
        /// The behavior of this method is <b>undefined</b> when called with
        /// <c> target &lt;= current</c>, or after the iterator has exhausted.
        /// Both cases may result in unpredicted behavior.
        /// <para/>
        /// When <c> target &gt; current</c> it behaves as if written:
        ///
        /// <code>
        /// int Advance(int target) 
        /// {
        ///     int doc;
        ///     while ((doc = NextDoc()) &lt; target) 
        ///     {
        ///     }
        ///     return doc;
        /// }
        /// </code>
        ///
        /// Some implementations are considerably more efficient than that.
        /// <para/>
        /// <b>NOTE:</b> this method may be called with <see cref="NO_MORE_DOCS"/> for
        /// efficiency by some <see cref="Scorer"/>s. If your implementation cannot efficiently
        /// determine that it should exhaust, it is recommended that you check for that
        /// value in each call to this method.
        /// <para/>
        ///
        /// @since 2.9
        /// </summary>
        public abstract int Advance(int target);

        /// <summary>
        /// Slow (linear) implementation of <see cref="Advance(int)"/> relying on
        /// <see cref="NextDoc()"/> to advance beyond the target position.
        /// </summary>
        protected internal int SlowAdvance(int target)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(DocID == NO_MORE_DOCS || DocID < target); // can happen when the enum is not positioned yet
            int doc;
            do
            {
                doc = NextDoc();
            } while (doc < target);
            return doc;
        }

        /// <summary>
        /// Returns the estimated cost of this <see cref="DocIdSetIterator"/>.
        /// <para/>
        /// This is generally an upper bound of the number of documents this iterator
        /// might match, but may be a rough heuristic, hardcoded value, or otherwise
        /// completely inaccurate.
        /// </summary>
        public abstract long GetCost();
    }
}