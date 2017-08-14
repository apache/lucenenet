using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Support;

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
    /// A simple iterator interface for <see cref="BytesRef"/> iteration.
    /// </summary>
    public interface IBytesRefIterator : IEnumerable<BytesRef>
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
        /// <exception cref="System.IO.IOException"> If there is a low-level I/O error. </exception>
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
    /// var iter = BytesRefIterator.Empty;
    /// </code>
    /// </summary>
    public class BytesRefIterator
    {
        private BytesRefIterator() { } // Disallow creation

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

            public IComparer<BytesRef> Comparer
            {
                get { return null; }
            }

            public IEnumerator<BytesRef> GetEnumerator()
            {
                return new EnumEnumerator<BytesRef>(() => false, () => null);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}