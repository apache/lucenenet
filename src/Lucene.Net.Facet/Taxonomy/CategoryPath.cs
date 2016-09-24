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
        /// An empty <seealso cref="CategoryPath"/> </summary>
        public static readonly CategoryPath EMPTY = new CategoryPath();

        /// <summary>
        /// The components of this <seealso cref="CategoryPath"/>. Note that this array may be
        /// shared with other <seealso cref="CategoryPath"/> instances, e.g. as a result of
        /// <seealso cref="#subpath(int)"/>, therefore you should traverse the array up to
        /// <seealso cref="#length"/> for this path's components.
        /// </summary>
        public readonly string[] components;

        /// <summary>
        /// The number of components of this <seealso cref="CategoryPath"/>. </summary>
        public readonly int length;

        // Used by singleton EMPTY
        private CategoryPath()
        {
            components = null;
            length = 0;
        }

        // Used by subpath
        private CategoryPath(CategoryPath copyFrom, int prefixLen)
        {
            // while the code which calls this method is safe, at some point a test
            // tripped on AIOOBE in toString, but we failed to reproduce. adding the
            // assert as a safety check.
            Debug.Assert(prefixLen > 0 && prefixLen <= copyFrom.components.Length, "prefixLen cannot be negative nor larger than the given components' length: prefixLen=" + prefixLen + " components.length=" + copyFrom.components.Length);
            this.components = copyFrom.components;
            length = prefixLen;
        }

        /// <summary>
        /// Construct from the given path components. </summary>
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
            this.components = components;
            length = components.Length;
        }

        /// <summary>
        /// Construct from a given path, separating path components with {@code delimiter}. </summary>
        public CategoryPath(string pathString, char delimiter)
        {
            string[] comps = pathString.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
            if (comps.Length == 1 && comps[0].Length == 0)
            {
                components = null;
                length = 0;
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
                components = comps;
                length = components.Length;
            }
        }

        /// <summary>
        /// Returns the number of characters needed to represent the path, including
        /// delimiter characters, for using with
        /// <seealso cref="#copyFullPath(char[], int, char)"/>.
        /// </summary>
        public virtual int FullPathLength()
        {
            if (length == 0)
            {
                return 0;
            }

            int charsNeeded = 0;
            for (int i = 0; i < length; i++)
            {
                charsNeeded += components[i].Length;
            }
            charsNeeded += length - 1; // num delimter chars
            return charsNeeded;
        }

        /// <summary>
        /// Compares this path with another <seealso cref="CategoryPath"/> for lexicographic
        /// order.
        /// </summary>
        public virtual int CompareTo(CategoryPath other)
        {
            int len = length < other.length ? length : other.length;
            for (int i = 0, j = 0; i < len; i++, j++)
            {
                int cmp = components[i].CompareTo(other.components[j]);
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
            return length - other.length;
        }

        private void hasDelimiter(string offender, char delimiter)
        {
            throw new System.ArgumentException("delimiter character '" + delimiter + 
                "' (U+" + delimiter.ToString() + ") appears in path component \"" + offender + "\"");
        }

        private void noDelimiter(char[] buf, int offset, int len, char delimiter)
        {
            for (int idx = 0; idx < len; idx++)
            {
                if (buf[offset + idx] == delimiter)
                {
                    hasDelimiter(new string(buf, offset, len), delimiter);
                }
            }
        }

        /// <summary>
        /// Copies the path components to the given {@code char[]}, starting at index
        /// {@code start}. {@code delimiter} is copied between the path components.
        /// Returns the number of chars copied.
        /// 
        /// <para>
        /// <b>NOTE:</b> this method relies on the array being large enough to hold the
        /// components and separators - the amount of needed space can be calculated
        /// with <seealso cref="#fullPathLength()"/>.
        /// </para>
        /// </summary>
        public virtual int CopyFullPath(char[] buf, int start, char delimiter)
        {
            if (length == 0)
            {
                return 0;
            }

            int idx = start;
            int upto = length - 1;
            for (int i = 0; i < upto; i++)
            {
                int len = components[i].Length;
                components[i].CopyTo(0, buf, idx, len - 0);
                noDelimiter(buf, idx, len, delimiter);
                idx += len;
                buf[idx++] = delimiter;
            }
            components[upto].CopyTo(0, buf, idx, components[upto].Length - 0);
            noDelimiter(buf, idx, components[upto].Length, delimiter);

            return idx + components[upto].Length - start;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CategoryPath))
            {
                return false;
            }

            CategoryPath other = (CategoryPath)obj;
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
        /// Calculate a 64-bit hash function for this path. </summary>
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
        public virtual CategoryPath Subpath(int length)
        {
            if (length >= this.length || length < 0)
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
        /// <seealso cref= #toString(char) </seealso>
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
            if (length == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                if (components[i].IndexOf(delimiter) != -1)
                {
                    hasDelimiter(components[i], delimiter);
                }
                sb.Append(components[i]).Append(delimiter);
            }
            sb.Length = sb.Length - 1; // remove last delimiter
            return sb.ToString();
        }
    }
}