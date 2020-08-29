using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Search.Suggest.Fst
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
    /// Collects <see cref="BytesRef"/> and then allows one to iterate over their sorted order. Implementations
    /// of this interface will be called in a single-threaded scenario.
    /// </summary>
    public interface IBytesRefSorter
    {
        /// <summary>
        /// Adds a single suggestion entry (possibly compound with its bucket).
        /// </summary>
        /// <exception cref="IOException"> If an I/O exception occurs. </exception>
        /// <exception cref="InvalidOperationException"> If an addition attempt is performed after
        /// a call to <see cref="GetEnumerator()"/> has been made. </exception>
        void Add(BytesRef utf8);

        /// <summary>
        /// Sorts the entries added in <see cref="Add(BytesRef)"/> and returns 
        /// an enumerator over all sorted entries.
        /// </summary>
        /// <exception cref="IOException"> If an I/O exception occurs. </exception>
        IBytesRefEnumerator GetEnumerator();

        /// <summary>
        /// Comparer used to determine the sort order of entries.
        /// </summary>
        IComparer<BytesRef> Comparer { get; }
    }
}
