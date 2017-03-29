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
    /// Represents a strongly typed list of objects that can be accessed by index.
    /// Provides methods to manipulate lists. Also provides functionality
    /// to compare lists against each other through an implementations of
    /// <see cref="IEquatable{T}"/>, or to wrap an existing list to use
    /// the same comparison logic as this one while not affecting any of its
    /// other functionality.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class EquatableList<T> : IList<T>, IEquatable<IList<T>>
#if !NETSTANDARD
        , ICloneable
#endif
    {
        private readonly IList<T> list;

        /// <summary>Initializes a new instance of the
        /// <see cref="EquatableList{T}"/> class that is empty and has the
        /// default initial capacity.</summary>
        public EquatableList()
        {
            list = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EquatableList{T}"/>.
        /// <para/>
        /// If the <paramref name="wrap"/> parameter is <c>true</c>, the
        /// <paramref name="collection"/> is used as is without doing
        /// a copy operation. Otherwise, the collection is copied 
        /// (which is the same operation as the 
        /// <see cref="EquatableList{T}.EquatableList(IEnumerable{T})"/> overload). 
        /// <para/>
        /// The internal <paramref name="collection"/> is used for
        /// all operations except for <see cref="Equals()"/>, <see cref="GetHashCode()"/>,
        /// and <see cref="ToString()"/>, which are all based on deep analysis
        /// of this collection and any nested collections.
        /// </summary>
        /// <param name="collection">The collection that will either be wrapped or copied 
        /// depending on the value of <paramref name="wrap"/>.</param>
        /// <param name="wrap"><c>true</c> to wrap an existing <see cref="IList{T}"/> without copying,
        /// or <c>false</c> to copy the collection into a new <see cref="List{T}"/>.</param>
        public EquatableList(IList<T> collection, bool wrap)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (wrap)
            {
                this.list = collection;
            }
            else
            {
                this.list = new List<T>(collection);
            }
        }

        /// <summary>
        /// Initializes a new 
        /// instance of the <see cref="EquatableList{T}"/>
        /// class that contains elements copied from the specified collection and has
        /// sufficient capacity to accommodate the number of elements copied. 
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        public EquatableList(IEnumerable<T> collection)
        {
            list = new List<T>(collection);
        }

        /// <summary>Initializes a new instance of the <see cref="EquatableList{T}"/>
        /// class that is empty and has the specified initial capacity.</summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        public EquatableList(int capacity)
        {
            list = new List<T>(capacity);
        }

        #region IList<T> members

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public virtual T this[int index]
        {
            get { return list[index]; }
            set { list[index] = value; }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="EquatableList{T}"/>.
        /// </summary>
        public virtual int Count
        {
            get { return list.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="EquatableList{T}"/> is read-only.
        /// </summary>
        public virtual bool IsReadOnly
        {
            get { return list.IsReadOnly; }
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="EquatableList{T}"/>.
        /// </summary>
        /// <param name="item">The object to be added to the end of the <see cref="EquatableList{T}"/>. The value can be <c>null</c> for reference types.</param>
        public virtual void Add(T item)
        {
            list.Add(item);
        }

        /// <summary>
        /// Removes all items from the <see cref="EquatableList{T}"/>.
        /// </summary>
        public virtual void Clear()
        {
            list.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="EquatableList{T}"/> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="EquatableList{T}"/>.</param>
        /// <returns><c>true</c> if the Object is found in the <see cref="EquatableList{T}"/>; otherwise, <c>false</c>.</returns>
        public virtual bool Contains(T item)
        {
            return list.Contains(item);
        }

        /// <summary>
        /// Copies the entire <see cref="EquatableList{T}"/> to a compatible one-dimensional array, 
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the 
        /// destination of the elements copied from <see cref="EquatableList{T}"/>. 
        /// The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="EquatableList{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the <see cref="EquatableList{T}"/>.</returns>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        /// <summary>
        /// Determines the index of a specific <paramref name="item"/> in the <see cref="EquatableList{T}"/>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="EquatableList{T}"/>.</param>
        /// <returns>The index of <paramref name="item"/> if found in the list; otherwise, -1.</returns>
        public virtual int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the <see cref="EquatableList{T}"/> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="EquatableList{T}"/>.</param>
        public virtual void Insert(int index, T item)
        {
            list.Insert(index, item);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="EquatableList{T}"/>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="EquatableList{T}"/>.</param>
        /// <returns><c>true</c> if <paramref name="item"/> was successfully removed from the 
        /// <see cref="EquatableList{T}"/>; otherwise, <c>false</c>. This method also returns 
        /// <c>false</c> if item is not found in the original <see cref="EquatableList{T}"/>.</returns>
        public virtual bool Remove(T item)
        {
            return list.Remove(item);
        }

        /// <summary>
        /// Removes the <see cref="EquatableList{T}"/> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public virtual void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="EquatableList{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> for the <see cref="EquatableList{T}"/>.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion

        #region IEquatable<T> members

        /// <summary>
        /// Compares this sequence to another <see cref="IList{T}"/>
        /// implementation, returning <c>true</c> if they are equal, <c>false</c> otherwise.
        /// <para/>
        /// The comparison takes into consideration any values in this collection and values
        /// of any nested collections, but does not take into consideration the data type.
        /// Therefore, <see cref="EquatableList{T}"/> can equal any <see cref="IList{T}"/>
        /// with the exact same values in the same order.
        /// </summary>
        /// <param name="other">The other <see cref="IList{T}"/> implementation
        /// to compare against.</param>
        /// <returns><c>true</c> if the sequence in <paramref name="other"/>
        /// is the same as this one.</returns>
        public virtual bool Equals(IList<T> other)
        {
            return Collections.Equals(this, other);
        }

        #endregion

        #region IClonable members

        /// <summary>Clones the <see cref="EquatableList{T}"/>.</summary>
        /// <remarks>This is a shallow clone.</remarks>
        /// <returns>A new shallow clone of this
        /// <see cref="EquatableList{T}"/>.</returns>
        public virtual object Clone()
        {
            // Just create a new one, passing this to the constructor.
            return new EquatableList<T>(this);
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
        /// Therefore, <see cref="EquatableList{T}"/> can equal any <see cref="IList{T}"/>
        /// with the exact same values in the same order.
        /// </summary>
        /// <param name="obj">The other object
        /// to compare against.</param>
        /// <returns><c>true</c> if the sequence in <paramref name="other"/>
        /// is the same as this one.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is IList<T>))
            {
                return false;
            }
            return this.Equals(obj as IList<T>);
        }

        /// <summary>
        /// Returns the hash code value for this list.
        /// <para/>
        /// The hash code determination takes into consideration any values in
        /// this collection and values of any nested collections, but does not
        /// take into consideration the data type. Therefore, the hash codes will
        /// be exactly the same for this <see cref="EquatableList{T}"/> and another
        /// <see cref="IList{T}"/> (including arrays) with the same values in the
        /// same order.
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

    //    public class EquatableList<T> : System.Collections.Generic.List<T>,
    //                                    IEquatable<IEnumerable<T>>
    //    {
    //        /// <summary>Initializes a new instance of the
    //        /// <see cref="EquatableList{T}"/> class that is empty and has the
    //        /// default initial capacity.</summary>
    //        public EquatableList()
    //            : base()
    //        {
    //        }

    //        /// <summary>Initializes a new instance of the <see cref="EquatableList{T}"/>
    //        /// class that contains elements copied from the specified collection and has
    //        /// sufficient capacity to accommodate the number of elements copied.</summary>
    //        /// <param name="collection">The collection whose elements are copied to the new list.</param>
    //        public EquatableList(System.Collections.Generic.IEnumerable<T> collection)
    //            : base(collection)
    //        {
    //        }

    //        /// <summary>Initializes a new instance of the <see cref="EquatableList{T}"/>
    //        /// class that is empty and has the specified initial capacity.</summary>
    //        /// <param name="capacity">The number of elements that the new list can initially store.</param>
    //        public EquatableList(int capacity)
    //            : base(capacity)
    //        {
    //        }

    //        /// <summary>Adds a range of objects represented by the <see cref="ICollection"/>
    //        /// implementation.</summary>
    //        /// <param name="c">The <see cref="ICollection"/>
    //        /// implementation to add to this list.</param>
    //        public void AddRange(ICollection c)
    //        {
    //            // If the collection is null, throw an exception.
    //            if (c == null) throw new ArgumentNullException("c");

    //            // Pre-compute capacity.
    //            Capacity = Math.Max(c.Count + Count, Capacity);

    //            // Cycle through the items and add.
    //            foreach (T item in c)
    //            {
    //                // Add the item.
    //                Add(item);
    //            }
    //        }

    //        /// <summary>Compares the counts of two <see cref="System.Collections.Generic.IEnumerable{T}"/>
    //        /// implementations.</summary>
    //        /// <remarks>This uses a trick in LINQ, sniffing types for implementations
    //        /// of interfaces that might supply shortcuts when trying to make comparisons.
    //        /// In this case, that is the <see cref="System.Collections.Generic.ICollection{T}"/> and
    //        /// <see cref="ICollection"/> interfaces, either of which can provide a count
    //        /// which can be used in determining the equality of sequences (if they don't have
    //        /// the same count, then they can't be equal).</remarks>
    //        /// <param name="x">The <see cref="System.Collections.Generic.IEnumerable{T}"/> from the left hand side of the
    //        /// comparison to check the count of.</param>
    //        /// <param name="y">The <see cref="System.Collections.Generic.IEnumerable{T}"/> from the right hand side of the
    //        /// comparison to check the count of.</param>
    //        /// <returns>Null if the result is indeterminate.  This occurs when either <paramref name="x"/>
    //        /// or <paramref name="y"/> doesn't implement <see cref="ICollection"/> or <see cref="System.Collections.Generic.ICollection{T}"/>.
    //        /// Otherwise, it will get the count from each and return true if they are equal, false otherwise.</returns>
    //        private static bool? EnumerableCountsEqual(System.Collections.Generic.IEnumerable<T> x, System.Collections.Generic.IEnumerable<T> y)
    //        {
    //            // Get the ICollection<T> and ICollection interfaces.
    //            System.Collections.Generic.ICollection<T> xOfTCollection = x as System.Collections.Generic.ICollection<T>;
    //            System.Collections.Generic.ICollection<T> yOfTCollection = y as System.Collections.Generic.ICollection<T>;
    //            ICollection xCollection = x as ICollection;
    //            ICollection yCollection = y as ICollection;

    //            // The count in x and y.
    //            int? xCount = xOfTCollection != null ? xOfTCollection.Count : xCollection != null ? xCollection.Count : (int?)null;
    //            int? yCount = yOfTCollection != null ? yOfTCollection.Count : yCollection != null ? yCollection.Count : (int?)null;

    //            // If either are null, return null, the result is indeterminate.
    //            if (xCount == null || yCount == null)
    //            {
    //                // Return null, indeterminate.
    //                return null;
    //            }

    //            // Both counts are non-null, compare.
    //            return xCount == yCount;
    //        }

    //        /// <summary>Compares the contents of a <see cref="System.Collections.Generic.IEnumerable{T}"/>
    //        /// implementation to another one to determine equality.</summary>
    //        /// <remarks>Thinking of the <see cref="System.Collections.Generic.IEnumerable{T}"/> implementation as
    //        /// a string with any number of characters, the algorithm checks
    //        /// each item in each list.  If any item of the list is not equal (or
    //        /// one list contains all the elements of another list), then that list
    //        /// element is compared to the other list element to see which
    //        /// list is greater.</remarks>
    //        /// <param name="x">The <see cref="System.Collections.Generic.IEnumerable{T}"/> implementation
    //        /// that is considered the left hand side.</param>
    //        /// <param name="y">The <see cref="System.Collections.Generic.IEnumerable{T}"/> implementation
    //        /// that is considered the right hand side.</param>
    //        /// <returns>True if the items are equal, false otherwise.</returns>
    //        private static bool Equals(System.Collections.Generic.IEnumerable<T> x,
    //                                   System.Collections.Generic.IEnumerable<T> y)
    //        {
    //            // If x and y are null, then return true, they are the same.
    //            if (x == null && y == null)
    //            {
    //                // They are the same, return 0.
    //                return true;
    //            }

    //            // If one is null, then return a value based on whether or not
    //            // one is null or not.
    //            if (x == null || y == null)
    //            {
    //                // Return false, one is null, the other is not.
    //                return false;
    //            }

    //            // Check to see if the counts on the IEnumerable implementations are equal.
    //            // This is a shortcut, if they are not equal, then the lists are not equal.
    //            // If the result is indeterminate, then get out.
    //            bool? enumerableCountsEqual = EnumerableCountsEqual(x, y);

    //            // If the enumerable counts have been able to be calculated (indicated by
    //            // a non-null value) and it is false, then no need to iterate through the items.
    //            if (enumerableCountsEqual != null && !enumerableCountsEqual.Value)
    //            {
    //                // The sequences are not equal.
    //                return false;
    //            }

    //            // The counts of the items in the enumerations are equal, or indeterminate
    //            // so a full iteration needs to be made to compare each item.
    //            // Get the default comparer for T first.
    //            System.Collections.Generic.EqualityComparer<T> defaultComparer =
    //                EqualityComparer<T>.Default;

    //            // Get the enumerator for y.
    //            System.Collections.Generic.IEnumerator<T> otherEnumerator = y.GetEnumerator();

    //            // Call Dispose on IDisposable if there is an implementation on the
    //            // IEnumerator<T> returned by a call to y.GetEnumerator().
    //            using (otherEnumerator as IDisposable)
    //            {
    //                // Cycle through the items in this list.
    //                foreach (T item in x)
    //                {
    //                    // If there isn't an item to get, then this has more
    //                    // items than that, they are not equal.
    //                    if (!otherEnumerator.MoveNext())
    //                    {
    //                        // Return false.
    //                        return false;
    //                    }

    //                    // Perform a comparison.  Must check this on the left hand side
    //                    // and that on the right hand side.
    //                    bool comparison = defaultComparer.Equals(item, otherEnumerator.Current);

    //                    // If the value is false, return false.
    //                    if (!comparison)
    //                    {
    //                        // Return the value.
    //                        return comparison;
    //                    }
    //                }

    //                // If there are no more items, then return true, the sequences
    //                // are equal.
    //                if (!otherEnumerator.MoveNext())
    //                {
    //                    // The sequences are equal.
    //                    return true;
    //                }

    //                // The other sequence has more items than this one, return
    //                // false, these are not equal.
    //                return false;
    //            }
    //        }

    //        #region IEquatable<IEnumerable<T>> Members

    //        /// <summary>Compares this sequence to another <see cref="System.Collections.Generic.IEnumerable{T}"/>
    //        /// implementation, returning true if they are equal, false otherwise.</summary>
    //        /// <param name="other">The other <see cref="System.Collections.Generic.IEnumerable{T}"/> implementation
    //        /// to compare against.</param>
    //        /// <returns>True if the sequence in <paramref name="other"/>
    //        /// is the same as this one.</returns>
    //        public bool Equals(System.Collections.Generic.IEnumerable<T> other)
    //        {
    //            // Compare to the other sequence.  If 0, then equal.
    //            return Equals(this, other);
    //        }

    //        #endregion IEquatable<IEnumerable<T>> Members

    //        /// <summary>Compares this object for equality against other.</summary>
    //        /// <param name="obj">The other object to compare this object against.</param>
    //        /// <returns>True if this object and <paramref name="obj"/> are equal, false
    //        /// otherwise.</returns>
    //        public override bool Equals(object obj)
    //        {
    //            // Call the strongly typed version.
    //            return Equals(obj as System.Collections.Generic.IEnumerable<T>);
    //        }

    //        /// <summary>Gets the hash code for the list.</summary>
    //        /// <returns>The hash code value.</returns>
    //        public override int GetHashCode()
    //        {
    //            // Call the static method, passing this.
    //            return GetHashCode(this);
    //        }

    //#if __MonoCS__
    //        public static int GetHashCode<T>(System.Collections.Generic.IEnumerable<T> source)
    //#else

    //        /// <summary>Gets the hash code for the list.</summary>
    //        /// <param name="source">The <see cref="System.Collections.Generic.IEnumerable{T}"/>
    //        /// implementation which will have all the contents hashed.</param>
    //        /// <returns>The hash code value.</returns>
    //        public static int GetHashCode(System.Collections.Generic.IEnumerable<T> source)
    //#endif
    //        {
    //            // If source is null, then return 0.
    //            if (source == null) return 0;

    //            // Seed the hash code with the hash code of the type.
    //            // This is done so that you don't have a lot of collisions of empty
    //            // ComparableList instances when placed in dictionaries
    //            // and things that rely on hashcodes.
    //            int hashCode = typeof(T).GetHashCode();

    //            // Iterate through the items in this implementation.
    //            foreach (T item in source)
    //            {
    //                // Adjust the hash code.
    //                hashCode = 31 * hashCode + (item == null ? 0 : item.GetHashCode());
    //            }

    //            // Return the hash code.
    //            return hashCode;
    //        }

    //        // TODO: When diverging from Java version of Lucene, can uncomment these to adhere to best practices when overriding the Equals method and implementing IEquatable<T>.
    //        ///// <summary>Overload of the == operator, it compares a
    //        ///// <see cref="ComparableList{T}"/> to an <see cref="IEnumerable{T}"/>
    //        ///// implementation.</summary>
    //        ///// <param name="x">The <see cref="ComparableList{T}"/> to compare
    //        ///// against <paramref name="y"/>.</param>
    //        ///// <param name="y">The <see cref="IEnumerable{T}"/> to compare
    //        ///// against <paramref name="x"/>.</param>
    //        ///// <returns>True if the instances are equal, false otherwise.</returns>
    //        //public static bool operator ==(EquatableList<T> x, System.Collections.Generic.IEnumerable<T> y)
    //        //{
    //        //    // Call Equals.
    //        //    return Equals(x, y);
    //        //}

    //        ///// <summary>Overload of the == operator, it compares a
    //        ///// <see cref="ComparableList{T}"/> to an <see cref="IEnumerable{T}"/>
    //        ///// implementation.</summary>
    //        ///// <param name="y">The <see cref="ComparableList{T}"/> to compare
    //        ///// against <paramref name="x"/>.</param>
    //        ///// <param name="x">The <see cref="IEnumerable{T}"/> to compare
    //        ///// against <paramref name="y"/>.</param>
    //        ///// <returns>True if the instances are equal, false otherwise.</returns>
    //        //public static bool operator ==(System.Collections.Generic.IEnumerable<T> x, EquatableList<T> y)
    //        //{
    //        //    // Call equals.
    //        //    return Equals(x, y);
    //        //}

    //        ///// <summary>Overload of the != operator, it compares a
    //        ///// <see cref="ComparableList{T}"/> to an <see cref="IEnumerable{T}"/>
    //        ///// implementation.</summary>
    //        ///// <param name="x">The <see cref="ComparableList{T}"/> to compare
    //        ///// against <paramref name="y"/>.</param>
    //        ///// <param name="y">The <see cref="IEnumerable{T}"/> to compare
    //        ///// against <paramref name="x"/>.</param>
    //        ///// <returns>True if the instances are not equal, false otherwise.</returns>
    //        //public static bool operator !=(EquatableList<T> x, System.Collections.Generic.IEnumerable<T> y)
    //        //{
    //        //    // Return the negative of the equals operation.
    //        //    return !(x == y);
    //        //}

    //        ///// <summary>Overload of the != operator, it compares a
    //        ///// <see cref="ComparableList{T}"/> to an <see cref="IEnumerable{T}"/>
    //        ///// implementation.</summary>
    //        ///// <param name="y">The <see cref="ComparableList{T}"/> to compare
    //        ///// against <paramref name="x"/>.</param>
    //        ///// <param name="x">The <see cref="IEnumerable{T}"/> to compare
    //        ///// against <paramref name="y"/>.</param>
    //        ///// <returns>True if the instances are not equal, false otherwise.</returns>
    //        //public static bool operator !=(System.Collections.Generic.IEnumerable<T> x, EquatableList<T> y)
    //        //{
    //        //    // Return the negative of the equals operation.
    //        //    return !(x == y);
    //        //}

    //        #region ICloneable Members

    //        /// <summary>Clones the <see cref="EquatableList{T}"/>.</summary>
    //        /// <remarks>This is a shallow clone.</remarks>
    //        /// <returns>A new shallow clone of this
    //        /// <see cref="EquatableList{T}"/>.</returns>
    //        public object Clone()
    //        {
    //            // Just create a new one, passing this to the constructor.
    //            return new EquatableList<T>(this);
    //        }

    //        #endregion ICloneable Members
    //    }
}