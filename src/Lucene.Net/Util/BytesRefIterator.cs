using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Util
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
    /// A simple enumerator interface for <see cref="BytesRef"/> iteration.
    /// </summary>
    public interface IBytesRefEnumerator
    {
        /// <summary>
        /// Increments the iteration to the next <see cref="BytesRef"/> in the enumerator.
        /// </summary>
        /// <returns><c>true</c> if the enumerator was successfully advanced to the next element;
        /// <c>false</c> if the enumerator has passed the end of the collection.</returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        bool MoveNext();

        /// <summary>
        /// Gets the <see cref="BytesRef"/> for the current iteration. The returned
        /// <see cref="BytesRef"/> may be reused across calls to <see cref="MoveNext()"/>.
        /// </summary>
        BytesRef Current { get; }

        /// <summary>
        /// Return the <see cref="BytesRef"/> Comparer used to sort terms provided by the
        /// iterator. This may return <c>null</c> if there are no items or the iterator is not
        /// sorted. Callers may invoke this method many times, so it's best to cache a
        /// single instance &amp; reuse it.
        /// </summary>
        IComparer<BytesRef> Comparer { get; }
    }

    /// <summary>
    /// LUCENENET specific class to make the syntax of creating an empty
    /// <see cref="IBytesRefEnumerator"/> the same as it was in Lucene. Example:
    /// <code>
    /// var iter = BytesRefEnumerator.EMPTY;
    /// </code>
    /// </summary>
    public static class BytesRefEnumerator
    {
        /// <summary>
        /// Singleton <see cref="BytesRefEnumerator"/> that iterates over 0 BytesRefs.
        /// </summary>
        public static readonly IBytesRefEnumerator EMPTY = new EmptyBytesRefEnumerator();

        private class EmptyBytesRefEnumerator : IBytesRefEnumerator
        {
            public bool MoveNext() => false;

            public IComparer<BytesRef> Comparer => null;

            public BytesRef Current => null;
        }
    }

    /// <summary>
    /// A simple iterator interface for <see cref="BytesRef"/> iteration.
    /// </summary>
    [Obsolete("Use IBytesRefEnumerator instead. This interface will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface IBytesRefIterator
    {
        /// <summary>
        /// Increments the iteration to the next <see cref="BytesRef"/> in the iterator.
        /// Returns the resulting <see cref="BytesRef"/> or <c>null</c> if the end of
        /// the iterator is reached. The returned <see cref="BytesRef"/> may be re-used across calls
        /// to <see cref="Next()"/>. After this method returns <c>null</c>, do not call it again: the results
        /// are undefined.
        /// </summary>
        /// <returns> The next <see cref="BytesRef"/> in the iterator or <c>null</c> if
        ///         the end of the iterator is reached. </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        BytesRef Next();

        /// <summary>
        /// Return the <see cref="BytesRef"/> Comparer used to sort terms provided by the
        /// iterator. This may return <c>null</c> if there are no items or the iterator is not
        /// sorted. Callers may invoke this method many times, so it's best to cache a
        /// single instance &amp; reuse it.
        /// </summary>
        IComparer<BytesRef> Comparer { get; }
    }

    /// <summary>
    /// LUCENENET specific class to make the syntax of creating an empty
    /// <see cref="IBytesRefIterator"/> the same as it was in Lucene. Example:
    /// <code>
    /// var iter = BytesRefIterator.EMPTY;
    /// </code>
    /// </summary>
    [Obsolete("Use BytesRefEnumerator instead. This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class BytesRefIterator
    {
        /// <summary>
        /// Singleton <see cref="BytesRefIterator"/> that iterates over 0 BytesRefs.
        /// </summary>
        public static readonly IBytesRefIterator EMPTY = new EmptyBytesRefIterator();

        private class EmptyBytesRefIterator : IBytesRefIterator
        {
            public BytesRef Next()
            {
                return null;
            }

            public IComparer<BytesRef> Comparer => null;
        }
    }
}