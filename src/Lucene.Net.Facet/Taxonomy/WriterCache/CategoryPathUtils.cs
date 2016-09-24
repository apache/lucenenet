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
    /// Utilities for use of <seealso cref="FacetLabel"/> by <seealso cref="CompactLabelToOrdinal"/>. </summary>
    internal class CategoryPathUtils
    {
        /// <summary>
        /// Serializes the given <seealso cref="FacetLabel"/> to the <seealso cref="CharBlockArray"/>. </summary>
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
        /// <seealso cref="#serialize(FacetLabel, CharBlockArray)"/>.
        /// </summary>
        public static int HashCodeOfSerialized(CharBlockArray charBlockArray, int offset)
        {
            int length = charBlockArray.CharAt(offset++);
            if (length == 0)
            {
                return 0;
            }

            int hash = length;
            for (int i = 0; i < length; i++)
            {
                int len = charBlockArray.CharAt(offset++);
                hash = hash * 31 + charBlockArray.SubSequence(offset, offset + len).GetHashCode();
                offset += len;
            }
            return hash;
        }

        /// <summary>
        /// Check whether the <seealso cref="FacetLabel"/> is equal to the one serialized in
        /// <seealso cref="CharBlockArray"/>.
        /// </summary>
        public static bool EqualsToSerialized(FacetLabel cp, CharBlockArray charBlockArray, int offset)
        {
            int n = charBlockArray.CharAt(offset++);
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
                int len = charBlockArray.CharAt(offset++);
                if (len != cp.Components[i].Length)
                {
                    return false;
                }

                if (!cp.Components[i].Equals(charBlockArray.SubSequence(offset, offset + len), StringComparison.Ordinal))
                {
                    return false;
                }
                offset += len;
            }
            return true;
        }
    }
}