using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Support
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
    /// Represents a strongly typed set of objects.
    /// Provides methods to manipulate the set. Also provides functionality
    /// to compare sets against each other through an implementations of
    /// <see cref="IEquatable{T}"/>, or to wrap an existing set to use
    /// the same comparison logic as this one while not affecting any of its
    /// other functionality.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class EquatableSet<T> : ISet<T>, IEquatable<ISet<T>>, ICloneable
    {
        private readonly ISet<T> set;

        /// <summary>Initializes a new instance of the
        /// <see cref="EquatableSet{T}"/> class that is empty and has the
        /// default initial capacity.</summary>
        public EquatableSet()
        {
            set = new HashSet<T>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EquatableSet{T}"/>.
        /// <para/>
        /// If the <paramref name="wrap"/> parameter is <c>true</c>, the
        /// <paramref name="collection"/> is used as is without doing
        /// a copy operation. Otherwise, the collection is copied 
        /// (which is the same operation as the 
        /// <see cref="EquatableSet{T}.EquatableSet(ICollection{T})"/> overload). 
        /// <para/>
        /// The internal <paramref name="collection"/> is used for
        /// all operations except for <see cref="Equals(object)"/>, <see cref="GetHashCode()"/>,
        /// and <see cref="ToString()"/>, which are all based on deep analysis
        /// of this collection and any nested collections.
        /// </summary>
        /// <param name="collection">The collection that will either be wrapped or copied 
        /// depending on the value of <paramref name="wrap"/>.</param>
        /// <param name="wrap"><c>true</c> to wrap an existing <see cref="ISet{T}"/> without copying,
        /// or <c>false</c> to copy the collection into a new <see cref="HashSet{T}"/>.</param>
        public EquatableSet(ISet<T> collection, bool wrap)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (wrap)
            {
                this.set = collection;
            }
            else
            {
                this.set = new HashSet<T>(collection);
            }
        }

        /// <summary>
        /// Initializes a new 
        /// instance of the <see cref="EquatableSet{T}"/>
        /// class that contains elements copied from the specified collection and has
        /// sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new set.</param>
        public EquatableSet(ICollection<T> collection)
        {
            set = new HashSet<T>(collection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquatableSet{T}"/> class that is 
        /// empty and uses the specified equality comparer for the set type.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation 
        /// to use when comparing values in the set, or null to use the default 
        /// <see cref="EqualityComparer{T}"/> implementation for the set type.</param>
        public EquatableSet(IEqualityComparer<T> comparer)
        {
            set = new HashSet<T>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquatableSet{T}"/> class that uses the 
        /// specified equality comparer for the set type, contains elements 
        /// copied from the specified collection, and has sufficient capacity 
        /// to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new set.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use 
        /// when comparing values in the set, or <c>null</c> to use the default <see cref="EqualityComparer{T}"/>
        /// implementation for the set type.</param>
        public EquatableSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            set = new HashSet<T>(collection, comparer);
        }


        #region ISet<T> members

        /// <summary>
        /// Gets the number of elements contained in the <see cref="EquatableSet{T}"/>.
        /// </summary>
        public virtual int Count
        {
            get { return set.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="EquatableSet{T}"/> is read-only.
        /// </summary>
        public virtual bool IsReadOnly
        {
            get { return set.IsReadOnly; }
        }

        /// <summary>
        /// Adds an element to the current set and returns a value to indicate if the element was successfully added.
        /// </summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns><c>true</c> if the element is added to the set; <c>false</c> if the element is already in the set.</returns>
        public virtual bool Add(T item)
        {
            return set.Add(item);
        }

        /// <summary>
        /// Removes all items from the <see cref="EquatableSet{T}"/>.
        /// </summary>
        public virtual void Clear()
        {
            set.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="EquatableSet{T}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="EquatableSet{T}"/>.</param>
        /// <returns><c>true</c> if item is found in the <see cref="EquatableSet{T}"/>; otherwise, <c>false</c>.</returns>
        public virtual bool Contains(T item)
        {
            return set.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="EquatableSet{T}"/> to an <see cref="Array"/>, 
        /// starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements 
        /// copied from <see cref="EquatableSet{T}"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            set.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current set.
        /// </summary>
        /// <param name="other">The collection of items to remove from the set.</param>
        public virtual void ExceptWith(IEnumerable<T> other)
        {
            set.ExceptWith(other);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return set.GetEnumerator();
        }

        /// <summary>
        /// Modifies the current set so that it contains only elements that are also in a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        public virtual void IntersectWith(IEnumerable<T> other)
        {
            set.IntersectWith(other);
        }

        /// <summary>
        /// Determines whether the current set is a proper (strict) subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is a proper subset of other; otherwise, <c>false</c>.</returns>
        public virtual bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return set.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a proper (strict) superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is a proper superset of other; otherwise, <c>false</c>.</returns>
        public virtual bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return set.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether a set is a subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is a subset of other; otherwise, <c>false</c>.</returns>
        public virtual bool IsSubsetOf(IEnumerable<T> other)
        {
            return set.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether the current set is a superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is a superset of other; otherwise, <c>false</c>.</returns>
        public virtual bool IsSupersetOf(IEnumerable<T> other)
        {
            return set.IsSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the current set overlaps with the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set and other share at least one common element; otherwise, <c>false</c>.</returns>
        public virtual bool Overlaps(IEnumerable<T> other)
        {
            return set.Overlaps(other);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="EquatableSet{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="EquatableSet{T}"/>.</param>
        /// <returns><c>true</c> if item was successfully removed from the <see cref="EquatableSet{T}"/>; otherwise, <c>false</c>. 
        /// This method also returns <c>false</c> if item is not found in the original <see cref="EquatableSet{T}"/>.</returns>
        public virtual bool Remove(T item)
        {
            return set.Remove(item);
        }

        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is equal to other; otherwise, <c>false</c>.</returns>
        public virtual bool SetEquals(IEnumerable<T> other)
        {
            return set.SetEquals(other);
        }

        /// <summary>
        /// Modifies the current set so that it contains only elements that are present either in the 
        /// current set or in the specified collection, but not both.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        public virtual void SymmetricExceptWith(IEnumerable<T> other)
        {
            set.SymmetricExceptWith(other);
        }

        /// <summary>
        /// Modifies the current set so that it contains all elements that are present in the 
        /// current set, in the specified collection, or in both.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        public virtual void UnionWith(IEnumerable<T> other)
        {
            set.UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            ((ICollection<T>)set).Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)set).GetEnumerator();
        }

        #endregion

        #region Operator overrides

        // TODO: When diverging from Java version of Lucene, can uncomment these to adhere to best practices when overriding the Equals method and implementing IEquatable<T>.
        ///// <summary>Overload of the == operator, it compares a
        ///// <see cref="EquatableSet{T}"/> to an <see cref="IEnumerable{T}"/>
        ///// implementation.</summary>
        ///// <param name="x">The <see cref="EquatableSet{T}"/> to compare
        ///// against <paramref name="y"/>.</param>
        ///// <param name="y">The <see cref="IEnumerable{T}"/> to compare
        ///// against <paramref name="x"/>.</param>
        ///// <returns>True if the instances are equal, false otherwise.</returns>
        //public static bool operator ==(EquatableSet<T> x, System.Collections.Generic.IEnumerable<T> y)
        //{
        //    // Call Equals.
        //    return Equals(x, y);
        //}

        ///// <summary>Overload of the == operator, it compares a
        ///// <see cref="EquatableSet{T}"/> to an <see cref="IEnumerable{T}"/>
        ///// implementation.</summary>
        ///// <param name="y">The <see cref="EquatableSet{T}"/> to compare
        ///// against <paramref name="x"/>.</param>
        ///// <param name="x">The <see cref="IEnumerable{T}"/> to compare
        ///// against <paramref name="y"/>.</param>
        ///// <returns>True if the instances are equal, false otherwise.</returns>
        //public static bool operator ==(System.Collections.Generic.IEnumerable<T> x, EquatableSet<T> y)
        //{
        //    // Call equals.
        //    return Equals(x, y);
        //}

        ///// <summary>Overload of the != operator, it compares a
        ///// <see cref="EquatableSet{T}"/> to an <see cref="IEnumerable{T}"/>
        ///// implementation.</summary>
        ///// <param name="x">The <see cref="EquatableSet{T}"/> to compare
        ///// against <paramref name="y"/>.</param>
        ///// <param name="y">The <see cref="IEnumerable{T}"/> to compare
        ///// against <paramref name="x"/>.</param>
        ///// <returns>True if the instances are not equal, false otherwise.</returns>
        //public static bool operator !=(EquatableSet<T> x, System.Collections.Generic.IEnumerable<T> y)
        //{
        //    // Return the negative of the equals operation.
        //    return !(x == y);
        //}

        ///// <summary>Overload of the != operator, it compares a
        ///// <see cref="EquatableSet{T}"/> to an <see cref="IEnumerable{T}"/>
        ///// implementation.</summary>
        ///// <param name="y">The <see cref="EquatableSet{T}"/> to compare
        ///// against <paramref name="x"/>.</param>
        ///// <param name="x">The <see cref="IEnumerable{T}"/> to compare
        ///// against <paramref name="y"/>.</param>
        ///// <returns>True if the instances are not equal, false otherwise.</returns>
        //public static bool operator !=(System.Collections.Generic.IEnumerable<T> x, EquatableSet<T> y)
        //{
        //    // Return the negative of the equals operation.
        //    return !(x == y);
        //}

        #endregion

        #region IEquatable<T> members

        /// <summary>
        /// Compares this sequence to <paramref name="other"/>, returning <c>true</c> if they 
        /// are equal, <c>false</c> otherwise.
        /// <para/>
        /// The comparison takes into consideration any values in this collection and values
        /// of any nested collections, but does not take into consideration the data type.
        /// Therefore, <see cref="EquatableSet{T}"/> can equal any <see cref="ISet{T}"/>
        /// with the exact same values (in any order).
        /// </summary>
        /// <param name="other">The other object
        /// to compare against.</param>
        /// <returns><c>true</c> if the sequence in <paramref name="other"/>
        /// is the same as this one.</returns>
        public virtual bool Equals(ISet<T> other)
        {
            return Collections.Equals(this, other);
        }

        #endregion

        #region IClonable members

        /// <summary>Clones the <see cref="EquatableSet{T}"/>.</summary>
        /// <remarks>This is a shallow clone.</remarks>
        /// <returns>A new shallow clone of this
        /// <see cref="EquatableSet{T}"/>.</returns>
        public virtual object Clone()
        {
            // Just create a new one, passing this to the constructor.
            return new EquatableSet<T>(this);
        }

        #endregion

        #region Object overrides

        /// <summary>
        /// If the object passed implements <see cref="IList{T}"/>,
        /// compares this sequence to <paramref name="other"/>, returning <c>true</c> if they 
        /// are equal, <c>false</c> otherwise.
        /// <para/>
        /// The comparison takes into consideration any values in this collection and values
        /// of any nested collections, but does not take into consideration the data type.
        /// Therefore, <see cref="EquatableSet{T}"/> can equal any <see cref="ISet{T}"/>
        /// with the exact same values (in any order).
        /// </summary>
        /// <param name="other">The other object
        /// to compare against.</param>
        /// <returns><c>true</c> if the sequence in <paramref name="other"/>
        /// is the same as this one.</returns>
        public override bool Equals(object other)
        {
            if (!(other is ISet<T>))
            {
                return false;
            }
            return Equals(other as ISet<T>);
        }

        /// <summary>
        /// Returns the hash code value for this list.
        /// <para/>
        /// The hash code determination takes into consideration any values in
        /// this collection and values of any nested collections, but does not
        /// take into consideration the data type. Therefore, the hash codes will
        /// be exactly the same for this <see cref="EquatableSet{T}"/> and another
        /// <see cref="ISet{T}"/> with the same values (in any order).
        /// </summary>
        /// <returns>the hash code value for this list</returns>
        public override int GetHashCode()
        {
            return Collections.GetHashCode(this);
        }

        /// <summary>
        /// Returns a string representation of this collection (and any nested collections). 
        /// The string representation consists of a list of the collection's elements in 
        /// the order they are returned by its enumerator, enclosed in square brackets 
        /// ("[]"). Adjacent elements are separated by the characters ", " (comma and space).
        /// </summary>
        /// <returns>a string representation of this collection</returns>
        public override string ToString()
        {
            return Collections.ToString(this);
        }

        #endregion
    }
}
