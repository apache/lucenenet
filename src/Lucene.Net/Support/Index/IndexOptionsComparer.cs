using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    /// Represents an <see cref="IndexOptions"/> comparison operation that uses <see cref="int"/> comparison rules.
    /// <para/>
    /// Since in .NET the standard comparers will do boxing when comparing enum types,
    /// this class was created as a more performant alternative than calling <c>CompareTo()</c> on <see cref="IndexOptions"/>.
    /// </summary>
    // See: GH-376

    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public sealed class IndexOptionsComparer : IComparer<IndexOptions>
    {
        private IndexOptionsComparer() { } // No instance

        /// <summary>
        /// Gets the default static singleton instance of <see cref="IndexOptionsComparer"/>.
        /// </summary>
        public static IComparer<IndexOptions> Default { get; } = new IndexOptionsComparer();

        /// <summary>
        /// Compares two <see cref="IndexOptions"/> enums and returns an indication of their relative sort order.
        /// </summary>
        /// <param name="x">An <see cref="IndexOptions"/> enum to compare to <paramref name="y"/>.</param>
        /// <param name="y">An <see cref="IndexOptions"/> enum to compare to <paramref name="x"/>.</param>
        /// <returns>
        /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Value</term>
        ///         <term>Meaning</term>
        ///     </listheader>
        ///     <item>
        ///         <term>Less than zero </term>
        ///         <term><paramref name="x"/> precedes y in the sort order.</term>
        ///     </item>
        ///     <item>
        ///         <term>Zero </term>
        ///         <term><paramref name="x"/> is equal to <paramref name="y"/>.</term>
        ///     </item>
        ///     <item>
        ///         <term>Greater than zero </term>
        ///         <term><paramref name="x"/> follows <paramref name="y"/> in the sort order.</term>
        ///     </item>
        /// </list>
        /// </returns>
        public int Compare([DisallowNull] IndexOptions x, [DisallowNull] IndexOptions y)
        {
            return ((int)x).CompareTo((int)y);
        }
    }
}
