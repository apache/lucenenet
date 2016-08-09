using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
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
    /// A simple class that stores Strings as char[]'s in a
    /// hash table.  Note that this is not a general purpose
    /// class.  For example, it cannot remove items from the
    /// set, nor does it resize its hash table to be smaller,
    /// etc.  It is designed to be quick to test if a char[]
    /// is in the set without the necessity of converting it
    /// to a String first.
    /// 
    /// <a name="version"></a>
    /// <p>You must specify the required <seealso cref="LuceneVersion"/>
    /// compatibility when creating <seealso cref="CharArraySet"/>:
    /// <ul>
    ///   <li> As of 3.1, supplementary characters are
    ///       properly lowercased.</li>
    /// </ul>
    /// Before 3.1 supplementary characters could not be
    /// lowercased correctly due to the lack of Unicode 4
    /// support in JDK 1.4. To use instances of
    /// <seealso cref="CharArraySet"/> with the behavior before Lucene
    /// 3.1 pass a <seealso cref="LuceneVersion"/> to the constructors.
    /// <p>
    /// <em>Please note:</em> This class implements <seealso cref="java.util.Set Set"/> but
    /// does not behave like it should in all cases. The generic type is
    /// {@code Set<Object>}, because you can add any object to it,
    /// that has a string representation. The add methods will use
    /// <seealso cref="object#toString"/> and store the result using a {@code char[]}
    /// buffer. The same behavior have the {@code contains()} methods.
    /// The <seealso cref="#iterator()"/> returns an {@code Iterator<char[]>}.
    /// </p>
    /// </summary>
    public class CharArraySet : ISet<object>
    {
        public static readonly CharArraySet EMPTY_SET = new CharArraySet(CharArrayMap<object>.EmptyMap());
        private static readonly object PLACEHOLDER = new object();

        internal readonly CharArrayMap<object> map;

        /// <summary>
        /// Create set with enough capacity to hold startSize terms
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="startSize">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <code>false</code> if and only if the set should be case sensitive
        ///          otherwise <code>true</code>. </param>
        public CharArraySet(LuceneVersion matchVersion, int startSize, bool ignoreCase)
            : this(new CharArrayMap<object>(matchVersion, startSize, ignoreCase))
        {
        }

        /// <summary>
        /// Creates a set from a Collection of objects. 
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="c">
        ///          a collection whose elements to be placed into the set </param>
        /// <param name="ignoreCase">
        ///          <code>false</code> if and only if the set should be case sensitive
        ///          otherwise <code>true</code>. </param>
        public CharArraySet(LuceneVersion matchVersion, IEnumerable<object> c, bool ignoreCase)
            : this(matchVersion, c.Count(), ignoreCase)
        {
            this.AddAll(c);
        }

        /// <summary>
        /// Create set from the specified map (internal only), used also by <seealso cref="CharArrayMap#KeySet()"/>
        /// </summary>
        internal CharArraySet(CharArrayMap<object> map)
        {
            this.map = map;
        }

        /// <summary>
        /// Clears all entries in this set. This method is supported for reusing, but not <seealso cref="Set#Remove"/>.
        /// </summary>
        public virtual void Clear()
        {
            map.Clear();
        }

        /// <summary>
        /// true if the <code>len</code> chars of <code>text</code> starting at <code>off</code>
        /// are in the set 
        /// </summary>
        public virtual bool Contains(char[] text, int off, int len)
        {
            return map.ContainsKey(text, off, len);
        }

        /// <summary>
        /// true if the <code>CharSequence</code> is in the set </summary>
        public virtual bool Contains(string cs)
        {
            return map.ContainsKey(cs);
        }

        public virtual bool Contains(object o)
        {
            return map.ContainsKey(o);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public virtual bool Remove(object item)
        {
            return map.Remove(item);
        }

        public virtual bool Add(object o)
        {
            return map.Put(o, PLACEHOLDER) == null;
        }

        /// <summary>
        /// Add this String into the set </summary>
        public virtual bool Add(string text)
        {
            return map.Put(text, PLACEHOLDER) == null;
        }

        /// <summary>
        /// Add this char[] directly to the set.
        /// If ignoreCase is true for this Set, the text array will be directly modified.
        /// The user should never modify this text array after calling this method.
        /// </summary>
        public virtual bool Add(char[] text)
        {
            return map.Put(text, PLACEHOLDER) == null;
        }

        void ICollection<object>.Add(object item)
        {
            Add(item);
        }

        public virtual int Count
        {
            get { return map.Count; }
        }

        public virtual bool IsReadOnly { get; private set; }

        /// <summary>
        /// Returns an unmodifiable <seealso cref="CharArraySet"/>. This allows to provide
        /// unmodifiable views of internal sets for "read-only" use.
        /// </summary>
        /// <param name="set">
        ///          a set for which the unmodifiable set is returned. </param>
        /// <returns> an new unmodifiable <seealso cref="CharArraySet"/>. </returns>
        /// <exception cref="NullPointerException">
        ///           if the given set is <code>null</code>. </exception>
        public static CharArraySet UnmodifiableSet(CharArraySet set)
        {
            if (set == null)
            {
                throw new System.NullReferenceException("Given set is null");
            }
            if (set == EMPTY_SET)
            {
                return EMPTY_SET;
            }
            if (set.map is CharArrayMap<object>.UnmodifiableCharArrayMap<object>)
            {
                return set;
            }
            return new CharArraySet(CharArrayMap<object>.UnmodifiableMap(set.map));
        }

        /// <summary>
        /// Returns a copy of the given set as a <seealso cref="CharArraySet"/>. If the given set
        /// is a <seealso cref="CharArraySet"/> the ignoreCase property will be preserved.
        /// <para>
        /// <b>Note:</b> If you intend to create a copy of another <seealso cref="CharArraySet"/> where
        /// the <seealso cref="LuceneVersion"/> of the source set differs from its copy
        /// <seealso cref="#CharArraySet(Version, Collection, boolean)"/> should be used instead.
        /// The <seealso cref="#copy(Version, Set)"/> will preserve the <seealso cref="LuceneVersion"/> of the
        /// source set it is an instance of <seealso cref="CharArraySet"/>.
        /// </para>
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. This argument will be ignored if the
        ///          given set is a <seealso cref="CharArraySet"/>. </param>
        /// <param name="set">
        ///          a set to copy </param>
        /// <returns> a copy of the given set as a <seealso cref="CharArraySet"/>. If the given set
        ///         is a <seealso cref="CharArraySet"/> the ignoreCase property as well as the
        ///         matchVersion will be of the given set will be preserved. </returns>
        public static CharArraySet Copy<T1>(LuceneVersion matchVersion, ISet<T1> set)
        {
            if (set == EMPTY_SET)
            {
                return EMPTY_SET;
            }

            var source = set as CharArraySet;
            if (source != null)
            {
                return new CharArraySet(CharArrayMap<object>.Copy(source.map.matchVersion, source.map));
            }

            return new CharArraySet(matchVersion, set.Cast<object>(), false);
        }

        /// <summary>
        /// Returns an <seealso cref="IEnumerator"/> for {@code char[]} instances in this set.
        /// </summary>
        public virtual IEnumerator GetEnumerator()
        {
            // use the AbstractSet#keySet()'s iterator (to not produce endless recursion)
            return map.OriginalKeySet().GetEnumerator();
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            // use the AbstractSet#keySet()'s iterator (to not produce endless recursion)
            return map.OriginalKeySet().GetEnumerator();
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            foreach (object item in this)
            {
                if (sb.Length > 1)
                {
                    sb.Append(", ");
                }
                if (item is char[])
                {
                    sb.Append((char[])item);
                }
                else
                {
                    sb.Append(item);
                }
            }
            return sb.Append(']').ToString();
        }

        // LUCENENET - Added to ensure equality checking works in tests
        public bool SetEquals(IEnumerable<object> other)
        {
            var otherSet = other as CharArraySet;
            if (otherSet == null)
                return false;

            if (this.Count != otherSet.Count)
                return false;

            foreach (var kvp in this.map)
            {
                if (!otherSet.map.ContainsKey(kvp.Key))
                    return false;

                if (!otherSet.map[kvp.Key].Equals(kvp.Value))
                    return false;
            }

            return true;
        }

        #region Not used by the Java implementation anyway
        public void UnionWith(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        public void IntersectWith(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        public void ExceptWith(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        public bool Overlaps(IEnumerable<object> other)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}