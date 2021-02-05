// Lucene version compatibility level 4.8.1
using System.Collections.Generic;

namespace Lucene.Net.Facet.SortedSet
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

    using IndexReader = Lucene.Net.Index.IndexReader;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// Wraps a <see cref="IndexReader"/> and resolves ords
    /// using existing <see cref="SortedSetDocValues"/> APIs without a
    /// separate taxonomy index.  This only supports flat facets
    /// (dimension + label), and it makes faceting a bit
    /// slower, adds some cost at reopen time, but avoids
    /// managing the separate taxonomy index.  It also requires
    /// less RAM than the taxonomy index, as it manages the flat
    /// (2-level) hierarchy more efficiently.  In addition, the
    /// tie-break during faceting is now meaningful (in label
    /// sorted order).
    /// 
    /// <para><b>NOTE</b>: creating an instance of this class is
    /// somewhat costly, as it computes per-segment ordinal maps,
    /// so you should create it once and re-use that one instance
    /// for a given <see cref="IndexReader"/>. 
    /// </para>
    /// </summary>
    public abstract class SortedSetDocValuesReaderState
    {
        /// <summary>
        /// Holds start/end range of ords, which maps to one
        /// dimension (someday we may generalize it to map to
        /// hierarchies within one dimension). 
        /// </summary>
        public sealed class OrdRange
        {
            /// <summary>
            /// Start of range, inclusive: </summary>
            public int Start { get; private set; }
            /// <summary>
            /// End of range, inclusive: </summary>
            public int End { get; private set; }

            /// <summary>
            /// Start and end are inclusive. </summary>
            public OrdRange(int start, int end)
            {
                this.Start = start;
                this.End = end;
            }
        }

        /// <summary>
        /// Sole constructor. </summary>
        protected SortedSetDocValuesReaderState()
        {
        }

        /// <summary>
        /// Return top-level doc values. </summary>
        public abstract SortedSetDocValues GetDocValues();

        /// <summary>
        /// Indexed field we are reading. </summary>
        public abstract string Field { get; }

        /// <summary>
        /// Returns the <see cref="OrdRange"/> for this dimension. </summary>
        public abstract OrdRange GetOrdRange(string dim);

        /// <summary>
        /// Returns mapping from prefix to <see cref="OrdRange"/>. </summary>
        public abstract IDictionary<string, OrdRange> PrefixToOrdRange { get; }

        /// <summary>
        /// Returns top-level index reader. </summary>
        public abstract IndexReader OrigReader { get; }

        /// <summary>
        /// Number of unique labels. </summary>
        public abstract int Count { get; }
    }
}