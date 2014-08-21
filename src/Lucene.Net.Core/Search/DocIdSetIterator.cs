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


namespace Lucene.Net.Search
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;


    // ReSharper disable CSharpWarnings::CS1574
    /// <summary>
    /// The abstract class defines methods to iterate over a set of non-decreasing
    /// doc ids. Note that this class assumes it iterates on doc Ids, and therefore
    /// <seealso cref="NO_MORE_DOCS"/> is set to <see cref="NO_MORE_DOCS"/> in order to be used as
    /// a sentinel object. Implementations of this class are expected to consider

    /// <seealso cref="System.Int32.MAX_VALUE"/> as an invalid value.
    /// </summary>
    public abstract class DocIdSetIterator : IEnumerator<int>
    {
        /// <summary>
        /// An empty <see cref="DocIdSetIterator"/> instance. 
        /// </summary>
        public static DocIdSetIterator Empty()
        {
            return new EmptyDocIdSetIterator();
        }

        /// <summary>
        /// Class EmptyDocIdSetIterator.
        /// </summary>
        private class EmptyDocIdSetIterator : DocIdSetIterator
        {
            private bool exhausted;

            /// <summary>
            /// Advances to the first beyond the current whose document number is greater
            /// than or equal to <i>target</i>, and returns the document number itself.
            /// Exhausts the iterator and returns <seealso cref="DocIdSetIterator.NO_MORE_DOCS" /> if <i>target</i>
            /// is greater than the highest document number in the set.
            /// </summary>
            /// <param name="target">The target.</param>
            /// <returns>System.Int32.</returns>
            /// <remarks><para>
            /// The behavior of this method is <b>undefined</b> when called with
            /// <c> target &lt; current</c>, or after the iterator has exhausted.
            /// Both cases may result in unpredicted behavior.
            /// </para>
            /// <para>
            /// When <code> target &gt; current</code> it behaves as if written:
            /// </para>
            /// <example>
            ///   <code language="c#">
            /// int advance(int target) {
            /// int doc;
            /// while ((doc = nextDoc()) &lt; target) {
            /// }
            /// return doc;
            /// }
            /// </code>
            ///   <para>
            /// Some implementations are considerably more efficient than that.
            /// </para>
            /// </example>
            /// <para>
            ///   <b>NOTE:</b> this method may be called with <seealso cref="DocIdSetIterator.NO_MORE_DOCS" /> for
            /// efficiency by some Scorers. If your implementation cannot efficiently
            /// determine that it should exhaust, it is recommended that you check for that
            /// value in each call to this method.
            /// </para></remarks>
            public override int Advance(int target)
            {
                Debug.Assert(!exhausted);
                Debug.Assert(target >= 0);
                exhausted = true;
                return NO_MORE_DOCS;
            }

            /// <summary>
            /// Returns the following document id. It will return -1 if <see cref="NextDoc()" />
            /// has not been called or <see cref="DocIdSetIterator.NO_MORE_DOCS" /> if the iterator has been
            /// exhausted.
            /// </summary>
            /// <value>The document identifier.</value>
            /// <remarks>@since 2.9</remarks>
            public override int DocId
            {
                get { return exhausted ? NO_MORE_DOCS : -1; }
                
            }

            /// <summary>
            /// Advances to the next document in the set and returns the doc it is
            /// currently on, or <seealso cref="DocIdSetIterator.NO_MORE_DOCS" /> if there are no more docs in the
            /// set.
            /// </summary>
            /// <returns>System.Int32.</returns>
            /// <remarks><para>
            ///   <b>NOTE:</b> after the iterator has exhausted you should not call this
            /// method, as it may result in unpredicted behavior.
            /// </para>
            /// @since 2.9</remarks>
            public override int NextDoc()
            {
                Debug.Assert(!exhausted);
                exhausted = true;
                return NO_MORE_DOCS;
            }

            /// <summary>
            /// Returns the estimated cost of this <seealso cref="DocIdSetIterator" />.
            /// </summary>
            /// <returns>System.Int64.</returns>
            /// <remarks>this is generally an upper bound of the number of documents this iterator
            /// might match, but may be a rough heuristic, hardcoded value, or otherwise
            /// completely inaccurate.</remarks>
            public override long Cost()
            {
                return 0;
            }
        }

        /// <summary>
        /// When returned by <seealso cref="NextDoc" />, <seealso cref="Advance(int)" /> and
        /// <seealso cref="DocId" /> it means there are no more docs in the iterator.
        /// </summary>
        public const int NO_MORE_DOCS = int.MaxValue;

        /// <summary>
        /// Returns the following document id. It will return -1 if <see cref="NextDoc()" />
        /// has not been called or <see cref="NO_MORE_DOCS" /> if the iterator has been
        /// exhausted.
        /// </summary>
        /// <value>The document identifier.</value>
        /// <remarks>@since 2.9</remarks>
        public abstract int DocId { get; }

        /// <summary>
        /// Advances to the next document in the set and returns the doc it is
        /// currently on, or <seealso cref="NO_MORE_DOCS" /> if there are no more docs in the
        /// set.
        /// </summary>
        /// <returns>System.Int32.</returns>
        /// <remarks><para>
        ///   <b>NOTE:</b> after the iterator has exhausted you should not call this
        /// method, as it may result in unpredicted behavior.
        /// </para>
        /// @since 2.9</remarks>
        public abstract int NextDoc();

        /// <summary>
        /// Advances to the first beyond the current whose document number is greater
        /// than or equal to <i>target</i>, and returns the document number itself.
        /// Exhausts the iterator and returns <seealso cref="NO_MORE_DOCS" /> if <i>target</i>
        /// is greater than the highest document number in the set.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Int32.</returns>
        /// <remarks><para>
        /// The behavior of this method is <b>undefined</b> when called with
        /// <c> target &lt; current</c>, or after the iterator has exhausted.
        /// Both cases may result in unpredicted behavior.
        /// </para>
        /// <para>
        /// When <code> target &gt; current</code> it behaves as if written:
        /// </para>
        /// <example>
        ///   <code language="c#">
        /// int advance(int target) {
        /// int doc;
        /// while ((doc = nextDoc()) &lt; target) {
        /// }
        /// return doc;
        /// }
        /// </code>
        ///   <para>
        /// Some implementations are considerably more efficient than that.
        /// </para>
        /// </example>
        /// <para>
        ///   <b>NOTE:</b> this method may be called with <seealso cref="NO_MORE_DOCS" /> for
        /// efficiency by some Scorers. If your implementation cannot efficiently
        /// determine that it should exhaust, it is recommended that you check for that
        /// value in each call to this method.
        /// </para></remarks>
        public abstract int Advance(int target);

        /// <summary>
        /// Slow (linear) implementation of <seealso cref="Advance(int)" /> relying on
        /// <seealso cref="NextDoc()" /> to advance beyond the target position.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>System.Int32.</returns>
        protected internal int SlowAdvance(int target)
        {
            Debug.Assert(DocId == NO_MORE_DOCS || DocId < target); // can happen when the enum is not positioned yet
            int doc;
            do
            {
                doc = NextDoc();
            } while (doc < target);
            return doc;
        }

        /// <summary>
        /// Returns the estimated cost of this <seealso cref="DocIdSetIterator"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         this is generally an upper bound of the number of documents this iterator
        ///         might match, but may be a rough heuristic, hardcoded value, or otherwise
        ///         completely inaccurate.
        ///     </para>
        /// </remarks>
        public abstract long Cost();

        int IEnumerator<int>.Current
        {
            get { return this.DocId; }
        }

        object IEnumerator.Current
        {
            get { return this.DocId; }
        }

        bool IEnumerator.MoveNext()
        {
            return this.NextDoc() != NO_MORE_DOCS;
        }

        void IEnumerator.Reset()
        {
            this.Reset();
        }

        void IDisposable.Dispose()
        {
           GC.SuppressFinalize(this);
           this.Dispose(true);
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        protected virtual void Reset()
        {
            
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DocIdSetIterator"/> class.
        /// </summary>
        ~DocIdSetIterator()
        {
            this.Dispose(false);
        }
    }
}