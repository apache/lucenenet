using Lucene.Net.Search.Suggest.Fst;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Suggest
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
    /// An <see cref="IBytesRefSorter"/> that keeps all the entries in memory.
    /// @lucene.experimental
    /// @lucene.internal
    /// </summary>
    public sealed class InMemorySorter : IBytesRefSorter
    {
        private readonly BytesRefArray buffer = new BytesRefArray(Counter.NewCounter());
        private bool closed = false;
        private readonly IComparer<BytesRef> comparer;

        /// <summary>
        /// Creates an InMemorySorter, sorting entries by the
        /// provided comparer.
        /// </summary>
        public InMemorySorter(IComparer<BytesRef> comparer)
        {
            this.comparer = comparer;
        }

        public void Add(BytesRef utf8)
        {
            if (closed)
            {
                throw IllegalStateException.Create();
            }
            buffer.Append(utf8);
        }

        public IBytesRefEnumerator GetEnumerator()
        {
            closed = true;
            return buffer.GetEnumerator(comparer);
        }

        public IComparer<BytesRef> Comparer => comparer;
    }
}