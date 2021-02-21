// Lucene version compatibility level 4.8.1
using System;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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
    /// Utilities for use of <see cref="FacetLabel"/> by <see cref="CompactLabelToOrdinal"/>.
    /// </summary>
    internal class CategoryPathUtils
    {
        /// <summary>
        /// Serializes the given <see cref="FacetLabel"/> to the <see cref="CharBlockArray"/>.
        /// </summary>
        public static void Serialize(FacetLabel cp, CharBlockArray charBlockArray)
        {
            charBlockArray.Append((char)cp.Length);
            if (cp.Length == 0)
            {
                return;
            }
            for (int i = 0; i < cp.Length; i++)
            {
                charBlockArray.Append((char)cp.Components[i].Length);
                charBlockArray.Append(cp.Components[i]);
            }
        }

        /// <summary>
        /// Calculates a hash function of a path that was serialized with
        /// <see cref="Serialize(FacetLabel, CharBlockArray)"/>.
        /// </summary>
        public static int HashCodeOfSerialized(CharBlockArray charBlockArray, int offset)
        {
            int length = charBlockArray[offset++];
            if (length == 0)
            {
                return 0;
            }

            int hash = length;
            for (int i = 0; i < length; i++)
            {
                int len = charBlockArray[offset++];
                hash = hash * 31 + charBlockArray.Subsequence(offset, len).GetHashCode(); // LUCENENET: Corrected 2nd Subsequence parameter
                offset += len;
            }
            return hash;
        }

        /// <summary>
        /// Check whether the <see cref="FacetLabel"/> is equal to the one serialized in
        /// <see cref="CharBlockArray"/>.
        /// </summary>
        public static bool EqualsToSerialized(FacetLabel cp, CharBlockArray charBlockArray, int offset)
        {
            int n = charBlockArray[offset++];
            if (cp.Length != n)
            {
                return false;
            }
            if (cp.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < cp.Length; i++)
            {
                int len = charBlockArray[offset++];
                if (len != cp.Components[i].Length)
                {
                    return false;
                }

                if (!cp.Components[i].Equals(charBlockArray.Subsequence(offset, len).ToString(), StringComparison.Ordinal)) // LUCENENET: Corrected 2nd Subsequence parameter
                {
                    return false;
                }
                offset += len;
            }
            return true;
        }
    }
}