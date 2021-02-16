using J2N.Numerics;
using System.Collections.Generic;

namespace Lucene.Net.Index
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
    /// Common util methods for dealing with <see cref="IndexReader"/>s and <see cref="IndexReaderContext"/>s.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class ReaderUtil
    {
        private ReaderUtil() // no instance
        {
        }

        /// <summary>
        /// Walks up the reader tree and return the given context's top level reader
        /// context, or in other words the reader tree's root context.
        /// </summary>
        public static IndexReaderContext GetTopLevelContext(IndexReaderContext context)
        {
            while (context.Parent != null)
            {
                context = context.Parent;
            }
            return context;
        }

        /// <summary>
        /// Returns index of the searcher/reader for document <c>n</c> in the
        /// array used to construct this searcher/reader.
        /// </summary>
        public static int SubIndex(int n, int[] docStarts) // find
        {
            // searcher/reader for doc n:
            int size = docStarts.Length;
            int lo = 0; // search starts array
            int hi = size - 1; // for first element less than n, return its index
            while (hi >= lo)
            {
                int mid = (lo + hi).TripleShift(1);
                int midValue = docStarts[mid];
                if (n < midValue)
                {
                    hi = mid - 1;
                }
                else if (n > midValue)
                {
                    lo = mid + 1;
                }
                else // found a match
                {
                    while (mid + 1 < size && docStarts[mid + 1] == midValue)
                    {
                        mid++; // scan to last match
                    }
                    return mid;
                }
            }
            return hi;
        }

        /// <summary>
        /// Returns index of the searcher/reader for document <c>n</c> in the
        /// array used to construct this searcher/reader.
        /// </summary>
        public static int SubIndex(int n, IList<AtomicReaderContext> leaves) // find
        {
            // searcher/reader for doc n:
            int size = leaves.Count;
            int lo = 0; // search starts array
            int hi = size - 1; // for first element less than n, return its index
            while (hi >= lo)
            {
                int mid = (lo + hi).TripleShift(1);
                int midValue = leaves[mid].DocBase;
                if (n < midValue)
                {
                    hi = mid - 1;
                }
                else if (n > midValue)
                {
                    lo = mid + 1;
                }
                else // found a match
                {
                    while (mid + 1 < size && leaves[mid + 1].DocBase == midValue)
                    {
                        mid++; // scan to last match
                    }
                    return mid;
                }
            }
            return hi;
        }
    }
}