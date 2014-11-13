using System;
using System.Diagnostics;
using Lucene.Net.Support;

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

    using LruTaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.LruTaxonomyWriterCache; 
    using NameHashIntCacheLRU = Lucene.Net.Facet.Taxonomy.WriterCache.NameHashIntCacheLRU; 

    /// <summary>
    /// Holds a sequence of string components, specifying the hierarchical name of a
    /// category.
    /// 
    /// @lucene.internal
    /// </summary>
    public class FacetLabel : IComparable<FacetLabel>
    {
        private static readonly int BYTE_BLOCK_SIZE = Lucene.Net.Util.ByteBlockPool.BYTE_BLOCK_SIZE;
        /*
         * copied from DocumentWriterPerThread -- if a FacetLabel is resolved to a
         * drill-down term which is encoded to a larger term than that length, it is
         * silently dropped! Therefore we limit the number of characters to MAX/4 to
         * be on the safe side.
         */
        /// <summary>
        /// The maximum number of characters a <seealso cref="FacetLabel"/> can have.
        /// </summary>
        public static readonly int MAX_CATEGORY_PATH_LENGTH = (BYTE_BLOCK_SIZE - 2) / 4;

        /// <summary>
        /// The components of this <seealso cref="FacetLabel"/>. Note that this array may be
        /// shared with other <seealso cref="FacetLabel"/> instances, e.g. as a result of
        /// <seealso cref="#subpath(int)"/>, therefore you should traverse the array up to
        /// <seealso cref="#length"/> for this path's components.
        /// </summary>
        public readonly string[] components;

        /// <summary>
        /// The number of components of this <seealso cref="FacetLabel"/>. </summary>
        public readonly int length;

        // Used by subpath
        private FacetLabel(FacetLabel copyFrom, int prefixLen)
        {
            // while the code which calls this method is safe, at some point a test
            // tripped on AIOOBE in toString, but we failed to reproduce. adding the
            // assert as a safety check.
            Debug.Assert(prefixLen >= 0 && prefixLen <= copyFrom.components.Length, "prefixLen cannot be negative nor larger than the given components' length: prefixLen=" + prefixLen + " components.length=" + copyFrom.components.Length);
            this.components = copyFrom.components;
            length = prefixLen;
        }

        /// <summary>
        /// Construct from the given path components. </summary>
        public FacetLabel(params string[] components)
        {
            this.components = components;
            length = components.Length;
            CheckComponents();
        }

        /// <summary>
        /// Construct from the dimension plus the given path components. </summary>
        public FacetLabel(string dim, string[] path)
        {
            components = new string[1 + path.Length];
            components[0] = dim;
            Array.Copy(path, 0, components, 1, path.Length);
            length = components.Length;
            CheckComponents();
        }

        private void CheckComponents()
        {
            long len = 0;
            foreach (string comp in components)
            {
                if (string.IsNullOrEmpty(comp))
                {
                    throw new System.ArgumentException("empty or null components not allowed: " + Arrays.ToString(components));
                }
                len += comp.Length;
            }
            len += components.Length - 1; // add separators
            if (len > MAX_CATEGORY_PATH_LENGTH)
            {
                throw new System.ArgumentException("category path exceeds maximum allowed path length: max=" + MAX_CATEGORY_PATH_LENGTH + " len=" + len + " path=" + Arrays.ToString(components).Substring(0, 30) + "...");
            }
        }

        /// <summary>
        /// Compares this path with another <seealso cref="FacetLabel"/> for lexicographic
        /// order.
        /// </summary>
        public virtual int CompareTo(FacetLabel other)
        {
            int len = length < other.length ? length : other.length;
            for (int i = 0, j = 0; i < len; i++, j++)
            {
                int cmp = components[i].CompareTo(other.components[j]);
                if (cmp < 0)
                {
                    return -1; // this is 'before'
                }
                if (cmp > 0)
                {
                    return 1; // this is 'after'
                }
            }

            // one is a prefix of the other
            return length - other.length;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FacetLabel))
            {
                return false;
            }

            FacetLabel other = (FacetLabel)obj;
            if (length != other.length)
            {
                return false; // not same length, cannot be equal
            }

            // CategoryPaths are more likely to differ at the last components, so start
            // from last-first
            for (int i = length - 1; i >= 0; i--)
            {
                if (!components[i].Equals(other.components[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            if (length == 0)
            {
                return 0;
            }

            int hash = length;
            for (int i = 0; i < length; i++)
            {
                hash = hash * 31 + components[i].GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Calculate a 64-bit hash function for this path.  This
        ///  is necessary for <seealso cref="NameHashIntCacheLRU"/> (the
        ///  default cache impl for {@link
        ///  LruTaxonomyWriterCache}) to reduce the chance of
        ///  "silent but deadly" collisions. 
        /// </summary>
        public virtual long LongHashCode()
        {
            if (length == 0)
            {
                return 0;
            }

            long hash = length;
            for (int i = 0; i < length; i++)
            {
                hash = hash * 65599 + components[i].GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Returns a sub-path of this path up to {@code length} components. </summary>
        public virtual FacetLabel Subpath(int len)
        {
            if (len >= this.length || len < 0)
            {
                return this;
            }
            else
            {
                return new FacetLabel(this, len);
            }
        }

        /// <summary>
        /// Returns a string representation of the path.
        /// </summary>
        public override string ToString()
        {
            if (length == 0)
            {
                return "FacetLabel: []";
            }
            string[] parts = new string[length];
            Array.Copy(components, 0, parts, 0, length);
            return "FacetLabel: [" + Arrays.ToString(parts) + "]";
        }
    }

}