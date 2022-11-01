﻿// Lucene version compatibility level 4.8.1
using J2N.Globalization;
using J2N.Text;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// A simple class that stores <see cref="string"/>s as <see cref="T:char[]"/>'s in a
    /// hash table.  Note that this is not a general purpose
    /// class.  For example, it cannot remove items from the
    /// set, nor does it resize its hash table to be smaller,
    /// etc.  It is designed to be quick to test if a <see cref="T:char[]"/>
    /// is in the set without the necessity of converting it
    /// to a <see cref="string"/> first.
    /// 
    /// <a name="version"></a>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="CharArraySet"/>:
    /// <ul>
    ///   <li> As of 3.1, supplementary characters are
    ///       properly lowercased.</li>
    /// </ul>
    /// Before 3.1 supplementary characters could not be
    /// lowercased correctly due to the lack of Unicode 4
    /// support in JDK 1.4. To use instances of
    /// <see cref="CharArraySet"/> with the behavior before Lucene
    /// 3.1 pass a <see cref="LuceneVersion"/> to the constructors.
    /// </para>
    /// <para>
    /// <em>Please note:</em> This class implements <see cref="ISet{T}"/> but
    /// does not behave like it should in all cases. The generic type is
    /// <see cref="string"/>, because you can add any object to it,
    /// that has a string representation (which is converted to a string). The add methods will use
    /// <see cref="object.ToString()"/> and store the result using a <see cref="T:char[]"/>
    /// buffer. The same behavior have the <see cref="Contains(string)"/> methods.
    /// The <see cref="GetEnumerator()"/> returns an <see cref="T:IEnumerator{char[]}"/>
    /// </para>
    /// </summary>
    public class CharArraySet : ISet<string>
    {
        [SuppressMessage("Performance", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly CharArraySet EMPTY_SET = new CharArraySet(CharArrayMap<string>.EmptyMap());
        // LUCENENET: PLACEHOLDER moved to CharArrayMap

        internal readonly ICharArrayMap map;

        /// <summary>
        /// Create set with enough capacity to hold <paramref name="startSize"/> terms
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <see cref="CharArraySet"/> for details. </param>
        /// <param name="startSize">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive
        ///          otherwise <c>true</c>. </param>
        public CharArraySet(LuceneVersion matchVersion, int startSize, bool ignoreCase)
            : this(new CharArrayMap<object>(matchVersion, startSize, ignoreCase))
        {
        }

        /// <summary>
        /// Creates a set from a collection of objects. 
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <see cref="CharArraySet"/> for details. </param>
        /// <param name="c">
        ///          a collection whose elements to be placed into the set </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive
        ///          otherwise <c>true</c>. </param>
        public CharArraySet(LuceneVersion matchVersion, ICollection<string> c, bool ignoreCase)
            : this(matchVersion, c.Count, ignoreCase)
        {
            this.UnionWith(c);
        }

        /// <summary>
        /// Create set from the specified map (internal only), used also by <see cref="CharArrayMap{TValue}.Keys"/>
        /// </summary>
        internal CharArraySet(ICharArrayMap map)
        {
            this.map = map;
        }

        /// <summary>
        /// Clears all entries in this set. This method is supported for reusing, but not <see cref="M:ICollection{string}.Remove(string)"/>.
        /// </summary>
        public virtual void Clear()
        {
            map.Clear();
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="length"/> chars of <paramref name="text"/> starting at <paramref name="offset"/>
        /// are in the set 
        /// </summary>
        public virtual bool Contains(char[] text, int offset, int length)
        {
            return map.ContainsKey(text, offset, length);
        }

        /// <summary>
        /// <c>true</c> if the <see cref="T:char[]"/>s 
        /// are in the set 
        /// </summary>
        public virtual bool Contains(char[] text)
        {
            return map.ContainsKey(text, 0, text.Length);
        }

        /// <summary>
        /// <c>true</c> if the <see cref="ICharSequence"/> is in the set
        /// </summary>
        public virtual bool Contains(ICharSequence cs)
        {
            return map.ContainsKey(cs);
        }

        /// <summary>
        /// <c>true</c> if the <see cref="string"/> is in the set
        /// </summary>
        public virtual bool Contains(string cs)
        {
            return map.ContainsKey(cs);
        }

        /// <summary>
        /// <c>true</c> if the <see cref="object.ToString()"/> representation of <paramref name="o"/> is in the set
        /// </summary>
        public virtual bool Contains(object o)
        {
            return map.ContainsKey(o);
        }

        /// <summary>
        /// Add the <see cref="object.ToString()"/> representation of <paramref name="o"/> into the set.
        /// The <see cref="object.ToString()"/> method is called after setting the thread to <see cref="CultureInfo.InvariantCulture"/>.
        /// If the type of <paramref name="o"/> is a value type, it will be converted using the 
        /// <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="o">A string-able object</param>
        /// <returns><c>true</c> if <paramref name="o"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public virtual bool Add(object o)
        {
            return map.Put(o);
        }

        /// <summary>
        /// Add this <see cref="ICharSequence"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public virtual bool Add(ICharSequence text)
        {
            return map.Put(text);
        }

        /// <summary>
        /// Add this <see cref="string"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public virtual bool Add(string text)
        {
            return map.Put(text);
        }

        /// <summary>
        /// Add this <see cref="T:char[]"/> directly to the set.
        /// If <c>ignoreCase</c> is true for this <see cref="CharArraySet"/>, the text array will be directly modified.
        /// The user should never modify this text array after calling this method.
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public virtual bool Add(char[] text)
        {
            return map.Put(text);
        }

        /// <summary>
        /// LUCENENET specific for supporting <see cref="ICollection{T}"/>.
        /// </summary>
        void ICollection<string>.Add(string item)
        {
            Add(item);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CharArraySet"/>.
        /// </summary>
        public virtual int Count => map.Count;

        /// <summary>
        /// <c>true</c> if the <see cref="CharArraySet"/> is read-only; otherwise <c>false</c>.
        /// </summary>
        public virtual bool IsReadOnly { get; private set; }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArraySet"/>. This allows to provide
        /// unmodifiable views of internal sets for "read-only" use.
        /// </summary>
        /// <param name="set">
        ///          a set for which the unmodifiable set is returned. </param>
        /// <returns> an new unmodifiable <see cref="CharArraySet"/>. </returns>
        /// <exception cref="ArgumentNullException">
        ///           if the given set is <c>null</c>. </exception>
        [Obsolete("Use the AsReadOnly() instance method instead. This method will be removed in 4.8.0 release candidate."), EditorBrowsable(EditorBrowsableState.Never)]
        public static CharArraySet UnmodifiableSet(CharArraySet set)
        {
            if (set is null)
            {
                throw new ArgumentNullException(nameof(set), "Given set is null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (set == EMPTY_SET)
            {
                return EMPTY_SET;
            }
            if (set.map is CharArrayMap.UnmodifiableCharArrayMap<object>)
            {
                return set;
            }
            return new CharArraySet(CharArrayMap.UnmodifiableMap<object>(set.map));
        }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArraySet"/>. This allows to provide
        /// unmodifiable views of internal sets for "read-only" use.
        /// </summary>
        /// <returns>A new unmodifiable <see cref="CharArraySet"/>.</returns>
        // LUCENENET specific - allow .NET-like syntax for creating immutable collections
        public virtual CharArraySet AsReadOnly()
        {
            if (this == EMPTY_SET)
            {
                return EMPTY_SET;
            }
            if (this.map is CharArrayMap.UnmodifiableCharArrayMap<object>)
            {
                return this;
            }
            return new CharArraySet(CharArrayMap.UnmodifiableMap<object>(this.map));
        }

        /// <summary>
        /// Returns a copy of the given set as a <see cref="CharArraySet"/>. If the given set
        /// is a <see cref="CharArraySet"/> the ignoreCase property will be preserved.
        /// <para>
        /// <b>Note:</b> If you intend to create a copy of another <see cref="CharArraySet"/> where
        /// the <see cref="LuceneVersion"/> of the source set differs from its copy
        /// <see cref="CharArraySet.CharArraySet(LuceneVersion, ICollection{string}, bool)"/> should be used instead.
        /// The <see cref="Copy{T}(LuceneVersion, ICollection{T})"/> will preserve the <see cref="LuceneVersion"/> of the
        /// source set it is an instance of <see cref="CharArraySet"/>.
        /// </para>
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version. This argument will be ignored if the
        ///          given set is a <see cref="CharArraySet"/>. </param>
        /// <param name="set">
        ///          a set to copy </param>
        /// <returns> a copy of the given set as a <see cref="CharArraySet"/>. If the given set
        ///         is a <see cref="CharArraySet"/> the <see cref="CharArrayMap{TValue}.ignoreCase"/> field as well as the
        ///         <see cref="CharArrayMap{TValue}.MatchVersion"/> will be preserved. </returns>
        public static CharArraySet Copy<T>(LuceneVersion matchVersion, ICollection<T> set)
        {
            if (set == EMPTY_SET)
            {
                return EMPTY_SET;
            }

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (set is CharArraySet)
            {
                var source = set as CharArraySet;
                return new CharArraySet(CharArrayMap.Copy<object>(source.map.MatchVersion, source.map));
            }

            // Convert the elements in the collection to string in the invariant context.
            string[] stringSet;
            using (var context = new CultureContext(CultureInfo.InvariantCulture))
            {
                stringSet = set.Select(x => x.ToString()).ToArray(); // LUCENENET TODO: Performance - this approach can probably be improved
            }

            return new CharArraySet(matchVersion, stringSet, false);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> for <see cref="T:char[]"/> instances in this set.
        /// </summary>
        public virtual IEnumerator GetEnumerator()
        {
            // use the OriginalKeySet's enumerator (to not produce endless recursion)
            return map.OriginalKeySet.GetEnumerator();
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            // use the OriginalKeySet's enumerator (to not produce endless recursion)
            return (IEnumerator<string>)map.OriginalKeySet.GetEnumerator();
        }

        /// <summary>
        /// Returns a string that represents the current object. (Inherited from <see cref="object"/>.)
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder("[");
            foreach (var item in this)
            {
                if (sb.Length > 1)
                {
                    sb.Append(", ");
                }
                sb.Append(item);
            }
            return sb.Append(']').ToString();
        }

        #region LUCENENET specific members

        /// <summary>
        /// Compares the specified object with this set for equality. Returns <c>true</c> if the 
        /// given object is also a set, the two sets have the same size, and every member of the 
        /// given set is contained in this set. This ensures that the equals method works properly 
        /// across different implementations of the <see cref="T:ISet{string}"/> interface.
        /// <para/>
        /// This implementation first checks if the specified object is this set; if so it 
        /// returns <c>true</c>. Then, it checks if the specified object is a set whose 
        /// size is identical to the size of this set; if not, it returns <c>false</c>. If so, 
        /// it uses the enumerator of this set and the specified object to determine if all of the
        /// contained values are present (using <see cref="string.Equals(string)"/>).
        /// </summary>
        /// <param name="obj">object to be compared for equality with this set</param>
        /// <returns><c>true</c> if the specified object is equal to this set</returns>
        public override bool Equals(object obj)
        {
            if (obj is ISet<string> other)
                return JCG.SetEqualityComparer<string>.Default.Equals(this, other);
            return false;
        }

        /// <summary>
        /// Returns the hash code value for this set. The hash code of a set 
        /// is defined to be the sum of the hash codes of the elements in the 
        /// set, where the hash code of a <c>null</c> element is defined to be zero. 
        /// This ensures that <c>s1.Equals(s2)</c> implies that 
        /// <c>s1.GetHashCode()==s2.GetHashCode()</c> for any two sets s1 and s2.
        /// This implementation iterates over the set, calling the GetHashCode() 
        /// method on each element in the set, and adding up the results.
        /// </summary>
        /// <returns>the hash code value for this set</returns>
        public override int GetHashCode()
        {
            return JCG.SetEqualityComparer<string>.Default.GetHashCode(this);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:string[]"/> array, 
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:string[]"/> Array that is the destination of the 
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(string[] array, int arrayIndex)
        {
            using (var iter = map.OriginalKeySet.GetEnumerator())
            {
                for (int i = arrayIndex; iter.MoveNext(); i++)
                {
                    array[i] = iter.Current;
                }
            }
        }

        [Obsolete("Not applicable in this class.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool Remove(string item) // LUCENENET TODO: API - make an explicit implementation that isn't public
        {
            // LUCENENET NOTE: According to the documentation header, Remove should not be supported
            throw UnsupportedOperationException.Create();
        }

        // LUCENENET - Added to ensure equality checking works in tests
        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is equal to other; otherwise, <c>false</c>.</returns>
        public virtual bool SetEquals(IEnumerable<string> other)
        {
            if (!(other is CharArraySet otherSet))
                return false;

            // Invoke the implementation on CharArrayMap that
            // tests the dictionaries to ensure they contain
            // the same keys and values.
            return this.map.Equals(otherSet.map);
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        public virtual bool UnionWith(IEnumerable<char[]> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (Add(item))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        public virtual bool UnionWith(IEnumerable<ICharSequence> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (Add(item))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        public virtual void UnionWith(IEnumerable<string> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            foreach (var item in other)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        public virtual bool UnionWith<T>(IEnumerable<T> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (item is char[])
                {
                    if (Add(item as char[]))
                    {
                        modified = true;
                    }
                    continue;
                }

                // Convert the item to a string in the invariant culture
                string stringItem;
                using (var context = new CultureContext(CultureInfo.InvariantCulture))
                {
                    stringItem = item.ToString();
                }

                if (Add(stringItem))
                {
                    modified = true;
                }
            }
            return modified;
        }

        // LUCENENET - no modifications should be made outside of original
        // Java implmentation's methods.
        [Obsolete("Not applicable in this class.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public void IntersectWith(IEnumerable<string> other) // LUCENENET TODO: API - make an explicit implementation that isn't public
        {
            throw UnsupportedOperationException.Create();
        }

        // LUCENENET - no modifications should be made outside of original
        // Java implmentation's methods.
        [Obsolete("Not applicable in this class.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public void ExceptWith(IEnumerable<string> other) // LUCENENET TODO: API - make an explicit implementation that isn't public
        {
            throw UnsupportedOperationException.Create();
        }

        // LUCENENET - no modifications should be made outside of original
        // Java implmentation's methods.
        [Obsolete("Not applicable in this class.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public void SymmetricExceptWith(IEnumerable<string> other) // LUCENENET TODO: API - make an explicit implementation that isn't public
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        public virtual bool IsSubsetOf(IEnumerable<string> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (this.Count == 0)
            {
                return true;
            }
            CharArraySet set = other as CharArraySet;
            if (set != null)
            {
                if (this.Count > set.Count)
                {
                    return false;
                }
                return this.IsSubsetOfCharArraySet(set);
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int _);
            return foundCount == this.Count;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        public virtual bool IsSubsetOf<T>(IEnumerable<T> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (this.Count == 0)
            {
                return true;
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int _);
            return foundCount == this.Count;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        public virtual bool IsSupersetOf(IEnumerable<string> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            ICollection<string> is2 = other as ICollection<string>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                CharArraySet set = other as CharArraySet;
                if ((set != null) && (set.Count > this.Count))
                {
                    return false;
                }
            }
            return this.ContainsAll(other);
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        public virtual bool IsSupersetOf<T>(IEnumerable<T> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            ICollection<T> is2 = other as ICollection<T>;
            if (is2 != null && is2.Count == 0)
            {
                return true;
            }
            return this.ContainsAll(other);
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        public virtual bool IsProperSubsetOf(IEnumerable<string> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            ICollection<string> is2 = other as ICollection<string>;
            if (is2 != null)
            {
                if (this.Count == 0)
                {
                    return (is2.Count > 0);
                }
                CharArraySet set = other as CharArraySet;
                if (set != null)
                {
                    if (this.Count >= set.Count)
                    {
                        return false;
                    }
                    return this.IsSubsetOfCharArraySet(set);
                }
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set plus at least one more,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount == this.Count && unfoundCount > 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        public virtual bool IsProperSubsetOf<T>(IEnumerable<T> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            ICollection<T> is2 = other as ICollection<T>;
            if (is2 != null && this.Count == 0)
            {
                return (is2.Count > 0);
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set plus at least one more,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount == this.Count && unfoundCount > 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        public virtual bool IsProperSupersetOf(IEnumerable<string> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (this.Count == 0)
            {
                return false;
            }
            ICollection<string> is2 = other as ICollection<string>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                CharArraySet set = other as CharArraySet;
                if (set != null)
                {
                    if (set.Count >= this.Count)
                    {
                        return false;
                    }
                    return this.ContainsAll(set);
                }
            }
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount < this.Count && unfoundCount == 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        public virtual bool IsProperSupersetOf<T>(IEnumerable<T> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (this.Count == 0)
            {
                return false;
            }
            ICollection<T> is2 = other as ICollection<T>;
            if (is2 != null && is2.Count == 0)
            {
                return true;
            }
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount < this.Count && unfoundCount == 0;
        }

        /// <summary>
        /// Determines whether the current <see cref="CharArraySet"/> object and a specified collection share common elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if the <see cref="CharArraySet"/> object and <paramref name="other"/> share at least one common element; otherwise, <c>false</c>.</returns>
        public virtual bool Overlaps(IEnumerable<string> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (this.Count != 0)
            {
                foreach (var local in other)
                {
                    if (this.Contains(local))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the current <see cref="CharArraySet"/> object and a specified collection share common elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object and <paramref name="other"/> share at least one common element; otherwise, <c>false</c>.</returns>
        public virtual bool Overlaps<T>(IEnumerable<T> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (this.Count != 0)
            {
                foreach (var local in other)
                {
                    if (this.Contains(local))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        /// <param name="other">collection to be checked for containment in this collection</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> contains all of the elements in the specified collection; otherwise, <c>false</c>.</returns>
        public virtual bool ContainsAll(IEnumerable<string> other)
        {
            foreach (var local in other)
            {
                if (!this.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        /// <param name="other">collection to be checked for containment in this collection</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> contains all of the elements in the specified collection; otherwise, <c>false</c>.</returns>
        public virtual bool ContainsAll<T>(IEnumerable<T> other)
        {
            foreach (var local in other)
            {
                if (!this.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsSubsetOfCharArraySet(CharArraySet other)
        {
            foreach (var local in this)
            {
                if (!other.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        private void GetFoundAndUnfoundCounts<T>(IEnumerable<T> other, out int foundCount, out int unfoundCount)
        {
            foundCount = 0;
            unfoundCount = 0;
            foreach (var item in other)
            {
                if (this.Contains(item))
                {
                    foundCount++;
                }
                else
                {
                    unfoundCount++;
                }
            }
        }

#endregion
    }

    /// <summary>
    /// LUCENENET specific extension methods for CharArraySet
    /// </summary>
    public static class CharArraySetExtensions
    {
#region Add

        /// <summary>
        /// Add this <see cref="bool"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public static bool Add(this CharArraySet set, bool text)
        {
            return set.map.Put(text.ToString());
        }

        /// <summary>
        /// Add this <see cref="byte"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public static bool Add(this CharArraySet set, byte text)
        {
            return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add this <see cref="char"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public static bool Add(this CharArraySet set, char text)
        {
            return set.map.Put("" + text);
        }

        ///// <summary>
        ///// Add this <see cref="decimal"/> into the set
        ///// </summary>
        ///// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        //public static bool Add(this CharArraySet set, decimal text)
        //{
        //    return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        //}

        ///// <summary>
        ///// Add this <see cref="double"/> into the set
        ///// </summary>
        ///// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        //public static bool Add(this CharArraySet set, double text)
        //{
        //    return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        //}

        ///// <summary>
        ///// Add this <see cref="float"/> into the set
        ///// </summary>
        ///// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        //public static bool Add(this CharArraySet set, float text)
        //{
        //    return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        //}

        /// <summary>
        /// Add this <see cref="int"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public static bool Add(this CharArraySet set, int text)
        {
            return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add this <see cref="long"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public static bool Add(this CharArraySet set, long text)
        {
            return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add this <see cref="sbyte"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        [CLSCompliant(false)]
        public static bool Add(this CharArraySet set, sbyte text)
        {
            return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add this <see cref="short"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        public static bool Add(this CharArraySet set, short text)
        {
            return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add this <see cref="uint"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        [CLSCompliant(false)]
        public static bool Add(this CharArraySet set, uint text)
        {
            return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add this <see cref="ulong"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        [CLSCompliant(false)]
        public static bool Add(this CharArraySet set, ulong text)
        {
            return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add this <see cref="ushort"/> into the set
        /// </summary>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call</returns>
        [CLSCompliant(false)]
        public static bool Add(this CharArraySet set, ushort text)
        {
            return set.map.Put(text.ToString(CultureInfo.InvariantCulture));
        }

#endregion

#region Contains

        /// <summary>
        /// <c>true</c> if the <see cref="bool"/> is in the set
        /// </summary>
        public static bool Contains(this CharArraySet set, bool text)
        {
            return set.map.ContainsKey(text.ToString());
        }

        /// <summary>
        /// <c>true</c> if the <see cref="byte"/> is in the set
        /// </summary>
        public static bool Contains(this CharArraySet set, byte text)
        {
            return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <see cref="char"/> is in the set
        /// </summary>
        public static bool Contains(this CharArraySet set, char text)
        {
            return set.map.ContainsKey("" + text);
        }

        ///// <summary>
        ///// <c>true</c> if the <see cref="decimal"/> is in the set
        ///// </summary>
        //public static bool Contains(this CharArraySet set, decimal text)
        //{
        //    return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        //}

        ///// <summary>
        ///// <c>true</c> if the <see cref="double"/> is in the set
        ///// </summary>
        //public static bool Contains(this CharArraySet set, double text)
        //{
        //    return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        //}

        ///// <summary>
        ///// <c>true</c> if the <see cref="float"/> is in the set
        ///// </summary>
        //public static bool Contains(this CharArraySet set, float text)
        //{
        //    return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        //}

        /// <summary>
        /// <c>true</c> if the <see cref="int"/> is in the set
        /// </summary>
        public static bool Contains(this CharArraySet set, int text)
        {
            return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <see cref="long"/> is in the set
        /// </summary>
        public static bool Contains(this CharArraySet set, long text)
        {
            return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <see cref="sbyte"/> is in the set
        /// </summary>
        [CLSCompliant(false)]
        public static bool Contains(this CharArraySet set, sbyte text)
        {
            return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <see cref="short"/> is in the set
        /// </summary>
        public static bool Contains(this CharArraySet set, short text)
        {
            return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <see cref="uint"/> is in the set
        /// </summary>
        [CLSCompliant(false)]
        public static bool Contains(this CharArraySet set, uint text)
        {
            return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <see cref="ulong"/> is in the set
        /// </summary>
        [CLSCompliant(false)]
        public static bool Contains(this CharArraySet set, ulong text)
        {
            return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <see cref="ushort"/> is in the set
        /// </summary>
        [CLSCompliant(false)]
        public static bool Contains(this CharArraySet set, ushort text)
        {
            return set.map.ContainsKey(text.ToString(CultureInfo.InvariantCulture));
        }

#endregion

#region UnionWith

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        public static bool UnionWith(this CharArraySet set, IEnumerable<byte> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        public static bool UnionWith(this CharArraySet set, IEnumerable<char> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add("" + item))
                {
                    modified = true;
                }
            }
            return modified;
        }

        ///// <summary>
        ///// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        ///// in itself, the specified collection, or both.
        ///// </summary>
        ///// <param name="set">this <see cref="CharArraySet"/></param>
        ///// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        ///// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        //public static bool UnionWith(this CharArraySet set, IEnumerable<decimal> other)
        //{
        //    if (other is null)
        //    {
        //        throw new ArgumentNullException(nameof(other));
        //    }
        //    if (set.IsReadOnly)
        //    {
        //        throw UnsupportedOperationException.Create("CharArraySet is readonly");
        //    }
        //    bool modified = false;
        //    foreach (var item in other)
        //    {
        //        if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
        //        {
        //            modified = true;
        //        }
        //    }
        //    return modified;
        //}

        ///// <summary>
        ///// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        ///// in itself, the specified collection, or both.
        ///// </summary>
        ///// <param name="set">this <see cref="CharArraySet"/></param>
        ///// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        ///// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        //public static bool UnionWith(this CharArraySet set, IEnumerable<double> other)
        //{
        //    if (other is null)
        //    {
        //        throw new ArgumentNullException(nameof(other));
        //    }
        //    if (set.IsReadOnly)
        //    {
        //        throw UnsupportedOperationException.Create("CharArraySet is readonly");
        //    }
        //    bool modified = false;
        //    foreach (var item in other)
        //    {
        //        if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
        //        {
        //            modified = true;
        //        }
        //    }
        //    return modified;
        //}

        ///// <summary>
        ///// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        ///// in itself, the specified collection, or both.
        ///// </summary>
        ///// <param name="set">this <see cref="CharArraySet"/></param>
        ///// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        ///// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        //public static bool UnionWith(this CharArraySet set, IEnumerable<float> other)
        //{
        //    if (other is null)
        //    {
        //        throw new ArgumentNullException(nameof(other));
        //    }
        //    if (set.IsReadOnly)
        //    {
        //        throw UnsupportedOperationException.Create("CharArraySet is readonly");
        //    }
        //    bool modified = false;
        //    foreach (var item in other)
        //    {
        //        if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
        //        {
        //            modified = true;
        //        }
        //    }
        //    return modified;
        //}

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        public static bool UnionWith(this CharArraySet set, IEnumerable<int> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        public static bool UnionWith(this CharArraySet set, IEnumerable<long> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        [CLSCompliant(false)]
        public static bool UnionWith(this CharArraySet set, IEnumerable<sbyte> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        public static bool UnionWith(this CharArraySet set, IEnumerable<short> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        [CLSCompliant(false)]
        public static bool UnionWith(this CharArraySet set, IEnumerable<uint> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        [CLSCompliant(false)]
        public static bool UnionWith(this CharArraySet set, IEnumerable<ulong> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
                {
                    modified = true;
                }
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="set">this <see cref="CharArraySet"/></param>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call</returns>
        [CLSCompliant(false)]
        public static bool UnionWith(this CharArraySet set, IEnumerable<ushort> other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (set.IsReadOnly)
            {
                throw UnsupportedOperationException.Create("CharArraySet is readonly");
            }
            bool modified = false;
            foreach (var item in other)
            {
                if (set.Add(item.ToString(CultureInfo.InvariantCulture)))
                {
                    modified = true;
                }
            }
            return modified;
        }

#endregion
    }
}