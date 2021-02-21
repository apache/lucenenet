// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Facet.Taxonomy
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
    /// Returns 3 arrays for traversing the taxonomy:
    /// <list type="bullet">
    /// <item><description> <see cref="Parents"/>: <c>Parents[i]</c> denotes the parent of category
    /// ordinal <c>i</c>.</description></item>
    /// <item><description> <see cref="Children"/>: <c>Children[i]</c> denotes a child of category ordinal
    /// <c>i</c>.</description></item>
    /// <item><description> <see cref="Siblings"/>: <c>Siblings[i]</c> denotes the sibling of category
    /// ordinal <c>i</c>.</description></item>
    /// </list>
    /// 
    /// To traverse the taxonomy tree, you typically start with <c>Children[0]</c>
    /// (ordinal 0 is reserved for ROOT), and then depends if you want to do DFS or
    /// BFS, you call <c>Children[Children[0]]</c> or <c>Siblings[Children[0]]</c>
    /// and so forth, respectively.
    /// 
    /// <para>
    /// <b>NOTE:</b> you are not expected to modify the values of the arrays, since
    /// the arrays are shared with other threads.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public abstract class ParallelTaxonomyArrays
    {
        /// <summary>
        /// Sole constructor.
        /// </summary>
        protected ParallelTaxonomyArrays() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Returns the parents array, where <c>Parents[i]</c> denotes the parent of
        /// category ordinal <c>i</c>.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public abstract int[] Parents { get; }

        /// <summary>
        /// Returns the children array, where <c>Children[i]</c> denotes a child of
        /// category ordinal <c>i</c>.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public abstract int[] Children { get; }

        /// <summary>
        /// Returns the siblings array, where <c>Siblings[i]</c> denotes the sibling
        /// of category ordinal <c>i</c>.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public abstract int[] Siblings { get; }
    }
}