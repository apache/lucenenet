using Lucene.Net.Support;
using System;
using System.Diagnostics;
using System.Text;

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
    /// Holds a sequence of string components, specifying the hierarchical name of a
    /// category.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class CategoryPath : IComparable<CategoryPath>
    {
        /// <summary>
        /// An empty <see cref="CategoryPath"/>
        /// </summary>
        public static readonly CategoryPath EMPTY = new CategoryPath();

        /// <summary>
        /// The components of this <see cref="CategoryPath"/>. Note that this array may be
        /// shared with other <see cref="CategoryPath"/> instances, e.g. as a result of
        /// <see cref="Subpath(int)"/>, therefore you should traverse the array up to
        /// <see cref="Length"/> for this path's components.
        /// </summary>
        public string[] Components { get; private set; }

        /// <summary>
        /// The number of components of this <see cref="CategoryPath"/>. </summary>
        public int Length { get; private set; }

        // Used by singleton EMPTY
        private CategoryPath()
        {
            Components = null;
            Length = 0;
        }

        // Used by subpath
        private CategoryPath(CategoryPath copyFrom, int prefixLen)
        {
            // while the code which calls this method is safe, at some point a test
            // tripped on AIOOBE in toString, but we failed to reproduce. adding the
            // assert as a safety check.
            Debug.Assert(prefixLen > 0 && prefixLen <= copyFrom.Components.Length, "prefixLen cannot be negative nor larger than the given components' length: prefixLen=" + prefixLen + " components.length=" + copyFrom.Components.Length);
            this.Components = copyFrom.Components;
            Length = prefixLen;
        }

        /// <summary>
        /// Construct from the given path <paramref name="components"/>.
        /// </summary>
        public CategoryPath(params string[] components)
        {
            Debug.Assert(components.Length > 0, "use CategoryPath.EMPTY to create an empty path");
            foreach (string comp in components)
            {
                if (string.IsNullOrEmpty(comp))
                {
                    throw new System.ArgumentException("empty or null components not allowed: " + Arrays.ToString(components));
                }
            }
            this.Components = components;
            Length = components.Length;
        }

        /// <summary>
        /// Construct from a given path, separating path components with <paramref name="delimiter"/>.
        /// </summary>
        public CategoryPath(string pathString, char delimiter)
        {
            string[] comps = pathString.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
            if (comps.Length == 1 && comps[0].Length == 0)
            {
                Components = null;
                Length = 0;
            }
            else
            {
                foreach (string comp in comps)
                {
                    if (string.IsNullOrEmpty(comp))
                    {
                        throw new System.ArgumentException("empty or null components not allowed: " + Arrays.ToString(comps));
                    }
                }
                Components = comps;
                Length = Components.Length;
            }
        }

        /// <summary>
        /// Returns the number of characters needed to represent the path, including
        /// delimiter characters, for using with
        /// <see cref="CopyFullPath(char[], int, char)"/>.
        /// </summary>
        public virtual int FullPathLength()
        {
            if (Length == 0)
            {
                return 0;
            }

            int charsNeeded = 0;
            for (int i = 0; i < Length; i++)
            {
                charsNeeded += Components[i].Length;
            }
            charsNeeded += Length - 1; // num delimter chars
            return charsNeeded;
        }

        /// <summary>
        /// Compares this path with another <see cref="CategoryPath"/> for lexicographic
        /// order.
        /// </summary>
        public virtual int CompareTo(CategoryPath other)
        {
            int len = Length < other.Length ? Length : other.Length;
            for (int i = 0, j = 0; i < len; i++, j++)
            {
                int cmp = Components[i].CompareToOrdinal(other.Components[j]);
                if (cmp < 0) // this is 'before'
                {
                    return -1;
                }
                if (cmp > 0) // this is 'after'
                {
                    return 1;
                }
            }

            // one is a prefix of the other
            return Length - other.Length;
        }

        private void HasDelimiter(string offender, char delimiter)
        {
            throw new System.ArgumentException("delimiter character '" + delimiter + 
                "' (U+" + delimiter.ToString() + ") appears in path component \"" + offender + "\"");
        }

        private void NoDelimiter(char[] buf, int offset, int len, char delimiter)
        {
            for (int idx = 0; idx < len; idx++)
            {
                if (buf[offset + idx] == delimiter)
                {
                    HasDelimiter(new string(buf, offset, len), delimiter);
                }
            }
        }

        /// <summary>
        /// Copies the path components to the given <see cref="char[]"/>, starting at index
        /// <paramref name="start"/>. <paramref name="delimiter"/> is copied between the path components.
        /// Returns the number of chars copied.
        /// 
        /// <para>
        /// <b>NOTE:</b> this method relies on the array being large enough to hold the
        /// components and separators - the amount of needed space can be calculated
        /// with <see cref="FullPathLength()"/>.
        /// </para>
        /// </summary>
        public virtual int CopyFullPath(char[] buf, int start, char delimiter)
        {
            if (Length == 0)
            {
                return 0;
            }

            int idx = start;
            int upto = Length - 1;
            for (int i = 0; i < upto; i++)
            {
                int len = Components[i].Length;
                Components[i].CopyTo(0, buf, idx, len - 0);
                NoDelimiter(buf, idx, len, delimiter);
                idx += len;
                buf[idx++] = delimiter;
            }
            Components[upto].CopyTo(0, buf, idx, Components[upto].Length - 0);
            NoDelimiter(buf, idx, Components[upto].Length, delimiter);

            return idx + Components[upto].Length - start;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CategoryPath))
            {
                return false;
            }

            CategoryPath other = (CategoryPath)obj;
            if (Length != other.Length)
            {
                return false; // not same length, cannot be equal
            }

            // CategoryPaths are more likely to differ at the last components, so start
            // from last-first
            for (int i = Length - 1; i >= 0; i--)
            {
                if (!Components[i].Equals(other.Components[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            if (Length == 0)
            {
                return 0;
            }

            int hash = Length;
            for (int i = 0; i < Length; i++)
            {
                hash = hash * 31 + Components[i].GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Calculate a 64-bit hash function for this path.
        /// </summary>
        public virtual long LongHashCode()
        {
            if (Length == 0)
            {
                return 0;
            }

            long hash = Length;
            for (int i = 0; i < Length; i++)
            {
                hash = hash * 65599 + Components[i].GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Returns a sub-path of this path up to <paramref name="length"/> components.
        /// </summary>
        public virtual CategoryPath Subpath(int length)
        {
            if (length >= this.Length || length < 0)
            {
                return this;
            }
            else if (length == 0)
            {
                return EMPTY;
            }
            else
            {
                return new CategoryPath(this, length);
            }
        }

        /// <summary>
        /// Returns a string representation of the path, separating components with
        /// '/'.
        /// </summary>
        /// <see cref= #toString(char) </seealso>
        public override string ToString()
        {
            return ToString('/');
        }

        /// <summary>
        /// Returns a string representation of the path, separating components with the
        /// given delimiter.
        /// </summary>
        public virtual string ToString(char delimiter)
        {
            if (Length == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Length; i++)
            {
                if (Components[i].IndexOf(delimiter) != -1)
                {
                    HasDelimiter(Components[i], delimiter);
                }
                sb.Append(Components[i]).Append(delimiter);
            }
            sb.Length = sb.Length - 1; // remove last delimiter
            return sb.ToString();
        }
    }
}