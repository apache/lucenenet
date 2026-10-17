// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JCG = J2N.Collections.Generic;
#nullable enable

// LUCENENET specific - this class was significantly refactored from its Java counterpart to look and act more like collections in .NET.
// It is backed by a J2N HashSet<string> rather than a custom hash table of char[] (see #1478).

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
    /// A simple class that stores <see cref="string"/>s in a hash set. Note that this is not a general purpose
    /// class.  For example, it cannot remove items from the set, etc.  It is designed to be quick to test if
    /// a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> (such as a slice of a term buffer) is in the set
    /// without the necessity of converting it to a <see cref="string"/> first.
    ///
    /// <a name="version"></a>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="CharArraySet"/>:
    /// <list type="bullet">
    ///   <item><description> As of 3.1, supplementary characters are
    ///       properly lowercased.</description></item>
    /// </list>
    /// Before 3.1 supplementary characters could not be
    /// lowercased correctly due to the lack of Unicode 4
    /// support in JDK 1.4. To use instances of
    /// <see cref="CharArraySet"/> with the behavior before Lucene
    /// 3.1 pass a <see cref="LuceneVersion"/> to the constructors.
    /// </para>
    /// <para>
    /// When the set is case insensitive, elements are lowercased per code point in the invariant culture when
    /// they are added, so enumerating the set returns the lowercased form.
    /// </para>
    /// </summary>
    [DebuggerDisplay("Count = {Count}, Values = {ToString()}")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
    [SuppressMessage("CodeQuality", "S1939:Inheritance list should not be redundant", Justification = "Following Microsoft's code style for collections")]
    public sealed class CharArraySet : ISet<string>, ICollection<string>, ICollection, IReadOnlyCollection<string>
#if FEATURE_READONLYSET
        , IReadOnlySet<string>
#endif
    {
        /// <summary>
        /// An empty, read-only <see cref="CharArraySet"/>.
        /// </summary>
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
#pragma warning disable 612, 618
        public static readonly CharArraySet Empty = new CharArraySet(LuceneVersion.LUCENE_CURRENT, ignoreCase: false, CreateSet(0, comparer: null), isReadOnly: true);
#pragma warning restore 612, 618

        private const int DefaultSetSize = 8; // LUCENENET specific

        private readonly JCG.HashSet<string> set;
        private readonly JCG.HashSet<string>.SpanAlternateLookup<char> lookup;
        private readonly CharArrayComparer? comparer; // null if case sensitive
        private readonly LuceneVersion matchVersion;
        private readonly bool ignoreCase;
        private readonly bool isReadOnly;

        /// <summary>
        /// Create set with enough capacity to hold <paramref name="capacity"/> terms
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <see cref="CharArraySet"/> for details. </param>
        /// <param name="capacity">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
        public CharArraySet(LuceneVersion matchVersion, int capacity, bool ignoreCase)
            : this(matchVersion, ignoreCase, CreateSet(capacity, GetComparer(matchVersion, ignoreCase)), isReadOnly: false)
        {
        }

        /// <summary>
        /// Creates a set from a collection of <see cref="string"/>s.
        /// </summary>
        /// <param name="matchVersion">
        ///          Compatibility match version see <see cref="CharArraySet"/> for details. </param>
        /// <param name="collection">
        ///          A collection whose elements to be placed into the set. </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// A given element within the <paramref name="collection"/> is <c>null</c>.
        /// </exception>
        public CharArraySet(LuceneVersion matchVersion, IEnumerable<string> collection, bool ignoreCase)
            : this(matchVersion, ignoreCase, CreateSet(collection, GetComparer(matchVersion, ignoreCase)), isReadOnly: false)
        {
        }

        /// <summary>
        /// Creates a copy of the given collection as a new <see cref="CharArraySet"/>.
        /// <para/>
        /// If <paramref name="collection"/> is a <see cref="CharArraySet"/>, its <c>ignoreCase</c> and
        /// match version are preserved and <paramref name="matchVersion"/> is ignored. Otherwise, the
        /// new set is case sensitive.
        /// <para/>
        /// <b>Note:</b> If you intend to create a copy of another <see cref="CharArraySet"/> where
        /// the <see cref="LuceneVersion"/> of the source set differs from its copy,
        /// <see cref="CharArraySet(LuceneVersion, IEnumerable{string}, bool)"/> should be used instead.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version. </param>
        /// <param name="collection">
        ///          a collection to copy </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// A given element within the <paramref name="collection"/> is <c>null</c>.
        /// </exception>
        // LUCENENET: Replaces the static Copy() method
        public CharArraySet(LuceneVersion matchVersion, IEnumerable<string> collection)
            : this(collection is CharArraySet source ? source.matchVersion : matchVersion,
                  collection is CharArraySet source2 && source2.ignoreCase,
                  CreateCopy(matchVersion, collection), isReadOnly: false)
        {
        }

        /// <summary>
        /// Creates a copy of the given <see cref="CharArraySet"/>. The copy preserves the <c>ignoreCase</c>
        /// and match version of <paramref name="collection"/>, and <paramref name="matchVersion"/> is ignored.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version. </param>
        /// <param name="collection">
        ///          a set to copy </param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        // LUCENENET: Replaces the static Copy() method
        public CharArraySet(LuceneVersion matchVersion, CharArraySet collection)
            : this(matchVersion, (IEnumerable<string>)collection)
        {
        }

        /// <summary>
        /// Creates a copy of the given <see cref="CharArraySet"/>, preserving its <c>ignoreCase</c>
        /// and match version.
        /// </summary>
        /// <param name="collection">a set to copy</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public CharArraySet(CharArraySet collection)
            : this(collection is null ? throw new ArgumentNullException(nameof(collection)) : collection.matchVersion, (IEnumerable<string>)collection)
        {
        }

        private CharArraySet(LuceneVersion matchVersion, bool ignoreCase, JCG.HashSet<string> set, bool isReadOnly)
        {
            this.matchVersion = matchVersion;
            this.ignoreCase = ignoreCase;
            this.comparer = GetComparer(matchVersion, ignoreCase);
            this.set = set;
            this.lookup = set.GetSpanAlternateLookup<char>();
            this.isReadOnly = isReadOnly;
        }

        private static CharArrayComparer? GetComparer(LuceneVersion matchVersion, bool ignoreCase)
            => ignoreCase ? CharArrayComparer.GetInstance(matchVersion) : null;

        private static JCG.HashSet<string> CreateSet(int capacity, CharArrayComparer? comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, SR.ArgumentOutOfRange_NeedNonNegNum);

            return new JCG.HashSet<string>(capacity, comparer ?? (IEqualityComparer<string>)StringComparer.Ordinal);
        }

        private static JCG.HashSet<string> CreateSet(IEnumerable<string> collection, CharArrayComparer? comparer)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            // Elements of a CharArraySet that uses the same comparer are already folded
            if (collection is CharArraySet other && other.comparer == comparer)
                return new JCG.HashSet<string>(other.set, comparer ?? (IEqualityComparer<string>)StringComparer.Ordinal);

            var result = CreateSet(collection is ICollection<string> c ? c.Count : DefaultSetSize, comparer);
            foreach (string text in collection)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(collection), "A given element within the collection is null.");
                result.Add(comparer is null ? text : comparer.Fold(text));
            }
            return result;
        }

        private static JCG.HashSet<string> CreateCopy(LuceneVersion matchVersion, IEnumerable<string> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is CharArraySet source)
                return CreateSet(source, source.comparer);
            return CreateSet(collection, comparer: null);
        }

        /// <summary>
        /// Clears all entries in this set. This method is supported for reusing, but not
        /// <see cref="ICollection{T}.Remove(T)"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">The set is read-only.</exception>
        public void Clear()
        {
            ThrowIfReadOnly();
            set.Clear();
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> is in the set.
        /// </summary>
        public bool Contains(ReadOnlySpan<char> text)
        {
            return lookup.Contains(text);
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> is in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public bool Contains(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            return lookup.Contains(text.AsSpan());
        }

        /// <summary>
        /// Adds <paramref name="text"/> into the set. If the set is case insensitive, the lowercased
        /// form of <paramref name="text"/> is stored.
        /// </summary>
        /// <param name="text">The text to be added to the set.</param>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call.</returns>
        /// <exception cref="NotSupportedException">The set is read-only.</exception>
        public bool Add(ReadOnlySpan<char> text)
        {
            ThrowIfReadOnly();
            return lookup.Add(text);
        }

        /// <summary>
        /// Adds <paramref name="text"/> into the set. If the set is case insensitive, the lowercased
        /// form of <paramref name="text"/> is stored.
        /// </summary>
        /// <param name="text">The text to be added to the set.</param>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The set is read-only.</exception>
        public bool Add(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            ThrowIfReadOnly();

            return set.Add(comparer is null ? text : comparer.Fold(text));
        }

        void ICollection<string>.Add(string item) => Add(item);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CharArraySet"/>.
        /// </summary>
        public int Count => set.Count;

        /// <summary>
        /// <c>true</c> if the <see cref="CharArraySet"/> is read-only; otherwise <c>false</c>.
        /// </summary>
        public bool IsReadOnly => isReadOnly;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        /// <summary>
        /// Returns an unmodifiable view of this <see cref="CharArraySet"/>. This allows to provide
        /// unmodifiable views of internal sets for "read-only" use.
        /// <para/>
        /// The returned set reflects later changes made to this set.
        /// </summary>
        /// <returns> a read-only view of this <see cref="CharArraySet"/>. </returns>
        public CharArraySet AsReadOnly()
        {
            return isReadOnly ? this : new CharArraySet(matchVersion, ignoreCase, set, isReadOnly: true);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CharArraySet"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="CharArraySet"/>.</returns>
        public Enumerator GetEnumerator() => new Enumerator(set.GetEnumerator());

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates the elements of a <see cref="CharArraySet"/> object.
        /// </summary>
        // LUCENENET: J2N's HashSet<T>.GetEnumerator() currently returns IEnumerator<T>, so this wraps it.
        public readonly struct Enumerator : IEnumerator<string>, IEnumerator
        {
            private readonly IEnumerator<string> enumerator;

            internal Enumerator(IEnumerator<string> enumerator)
            {
                this.enumerator = enumerator;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public string Current => enumerator.Current;

            object IEnumerator.Current => enumerator.Current;

            /// <summary>
            /// Releases all resources used by the <see cref="Enumerator"/>.
            /// </summary>
            public void Dispose() => enumerator.Dispose();

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="CharArraySet"/>.
            /// </summary>
            /// <returns><c>true</c> if the enumerator was successfully advanced to the next element;
            /// <c>false</c> if the enumerator has passed the end of the collection.</returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext() => enumerator.MoveNext();

            void IEnumerator.Reset() => enumerator.Reset();
        }

        /// <summary>
        /// Returns a string that represents the current collection, in the form <c>[a, b, c]</c>.
        /// </summary>
        public override string ToString()
        {
            return set.ToString(StringFormatter.InvariantCulture);
        }

        /// <summary>
        /// Compares the specified object with this set for equality. Returns <c>true</c> if the
        /// given object is also a set, the two sets have the same size, and every member of the
        /// given set is contained in this set. This ensures that the equals method works properly
        /// across different implementations of the <see cref="ISet{T}"/> interface.
        /// <para/>
        /// This implementation first checks if the specified object is this set; if so it
        /// returns <c>true</c>. Then, it checks if the specified object is a set whose
        /// size is identical to the size of this set; if not, it returns <c>false</c>. If so,
        /// it uses the enumerator of this set and the specified object to determine if all of the
        /// contained values are present (using <see cref="string.Equals(string)"/>).
        /// </summary>
        /// <param name="obj">object to be compared for equality with this set</param>
        /// <returns><c>true</c> if the specified object is equal to this set</returns>
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
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
        /// </summary>
        /// <returns>the hash code value for this set</returns>
        public override int GetHashCode()
        {
            return JCG.SetEqualityComparer<string>.Default.GetHashCode(this);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:string[]"/> array,
        /// starting at the beginning of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:string[]"/> Array that is the destination of the
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space in the destination array.</exception>
        public void CopyTo(string[] array)
        {
            CopyTo(array, 0, set.Count);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:string[]"/> array,
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:string[]"/> Array that is the destination of the
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="arrayIndex"/> to the end of the destination array.</exception>
        public void CopyTo(string[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, set.Count);
        }

        /// <summary>
        /// Copies the specified number of elements of the <see cref="CharArraySet"/> to a one-dimensional
        /// <see cref="T:string[]"/> array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:string[]"/> Array that is the destination of the
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count">The number of elements to copy to <paramref name="array"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="count"/> is greater
        /// than the available space from <paramref name="arrayIndex"/> to the end of the destination array.</exception>
        public void CopyTo(string[] array, int arrayIndex, int count)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            set.CopyTo(array, arrayIndex, count);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - index < Count)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            if (array is string[] strings)
            {
                CopyTo(strings, index);
                return;
            }

            if (array is not object?[] objects)
                throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));

            try
            {
                foreach (string item in set)
                    objects[index++] = item;
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
            }
        }

        /// <summary>
        /// Not supported by <see cref="CharArraySet"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        bool ICollection<string>.Remove(string item)
        {
            throw UnsupportedOperationException.Create();
        }

        #region ISet<string> members

        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is equal to other; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public bool SetEquals(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is CharArraySet otherSet && otherSet.comparer == comparer)
            {
                if (this.Count != otherSet.Count)
                    return false;

                // already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal
                return ContainsAllElements(otherSet);
            }

            GetFoundAndUnfoundCounts(other, out int uniqueFoundCount, out int unfoundCount);
            return uniqueFoundCount == this.Count && unfoundCount == 0;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="other"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// A given element within the collection is <c>null</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">This set instance is read-only.</exception>
        public bool UnionWith(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            ThrowIfReadOnly();

            bool modified = false;
            foreach (string item in other)
            {
                modified |= Add(item);
            }
            return modified;
        }

        void ISet<string>.UnionWith(IEnumerable<string> other)
        {
            UnionWith(other);
        }

        /// <summary>
        /// Not supported by <see cref="CharArraySet"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        void ISet<string>.IntersectWith(IEnumerable<string> other)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported by <see cref="CharArraySet"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        void ISet<string>.ExceptWith(IEnumerable<string> other)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported by <see cref="CharArraySet"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        void ISet<string>.SymmetricExceptWith(IEnumerable<string> other)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public bool IsSubsetOf(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
                return true;

            if (other is CharArraySet otherSet)
            {
                if (this.Count > otherSet.Count)
                    return false;
                return this.IsSubsetOfCharArraySet(otherSet);
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set,
            // but we need to use the comparison rules of the current set.
            GetFoundAndUnfoundCounts(other, out int uniqueFoundCount, out _);
            return uniqueFoundCount == this.Count;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public bool IsSupersetOf(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is ICollection<string> otherAsCollection)
            {
                if (otherAsCollection.Count == 0)
                    return true;
                if (other is CharArraySet otherSet && otherSet.Count > this.Count)
                    return false;
            }
            return this.ContainsAllElements(other);
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public bool IsProperSubsetOf(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is ICollection<string> otherAsCollection)
            {
                if (this.Count == 0)
                    return otherAsCollection.Count > 0;
                if (other is CharArraySet otherSet)
                {
                    if (this.Count >= otherSet.Count)
                        return false;
                    return this.IsSubsetOfCharArraySet(otherSet);
                }
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set plus at least one more,
            // but we need to use the comparison rules of the current set.
            GetFoundAndUnfoundCounts(other, out int uniqueFoundCount, out int unfoundCount);
            return uniqueFoundCount == this.Count && unfoundCount > 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public bool IsProperSupersetOf(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
                return false;

            if (other is ICollection<string> otherAsCollection)
            {
                if (otherAsCollection.Count == 0)
                    return true;
                if (other is CharArraySet otherSet)
                {
                    if (otherSet.Count >= this.Count)
                        return false;
                    return this.ContainsAllElements(otherSet);
                }
            }
            GetFoundAndUnfoundCounts(other, out int uniqueFoundCount, out int unfoundCount);
            return uniqueFoundCount < this.Count && unfoundCount == 0;
        }

        /// <summary>
        /// Determines whether the current <see cref="CharArraySet"/> object and a specified collection share common elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if the <see cref="CharArraySet"/> object and <paramref name="other"/> share at least one common element; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public bool Overlaps(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count != 0)
            {
                foreach (string item in other)
                {
                    if (item is not null && this.Contains(item))
                        return true;
                }
            }
            return false;
        }

        #endregion

        #region Private helpers

        private void ThrowIfReadOnly()
        {
            if (isReadOnly)
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
        }

        /// <summary>
        /// Checks if this <see cref="CharArraySet"/> is a subset of <paramref name="other"/>.
        /// </summary>
        private bool IsSubsetOfCharArraySet(CharArraySet other)
        {
            foreach (string item in set)
            {
                if (!other.Contains(item))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        private bool ContainsAllElements(IEnumerable<string> other)
        {
            foreach (string item in other)
            {
                if (item is null || !this.Contains(item))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Enumerates <paramref name="other"/> once, counting the number of distinct elements of this set
        /// that it contains (using the comparison rules of this set) and the number of its elements that
        /// are not in this set.
        /// </summary>
        private void GetFoundAndUnfoundCounts(IEnumerable<string> other, out int uniqueFoundCount, out int unfoundCount)
        {
            JCG.HashSet<string>? found = null;
            unfoundCount = 0;
            foreach (string item in other)
            {
                if (item is not null && this.Contains(item))
                    (found ??= new JCG.HashSet<string>(set.EqualityComparer)).Add(comparer is null ? item : comparer.Fold(item));
                else
                    unfoundCount++;
            }
            uniqueFoundCount = found?.Count ?? 0;
        }

        #endregion
    }

    /// <summary>
    /// Extensions to <see cref="IEnumerable{T}"/> for <see cref="CharArraySet"/>.
    /// </summary>
    // LUCENENET specific
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns a copy of this <see cref="IEnumerable{T}"/> as a new instance of <see cref="CharArraySet"/> with the
        /// specified <paramref name="matchVersion"/>. If <paramref name="collection"/> is a <see cref="CharArraySet"/>,
        /// its <c>ignoreCase</c> and match version are preserved; otherwise, the new set is case sensitive.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <param name="matchVersion">Compatibility match version.</param>
        /// <returns>A copy of this <see cref="IEnumerable{T}"/> as a <see cref="CharArraySet"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public static CharArraySet ToCharArraySet(this IEnumerable<string> collection, LuceneVersion matchVersion)
        {
            return new CharArraySet(matchVersion, collection);
        }

        /// <summary>
        /// Returns a copy of this <see cref="IEnumerable{T}"/> as a new instance of <see cref="CharArraySet"/> with the
        /// specified <paramref name="matchVersion"/> and <paramref name="ignoreCase"/>.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <param name="matchVersion">Compatibility match version.</param>
        /// <param name="ignoreCase"><c>false</c> if and only if the set should be case sensitive otherwise <c>true</c>.</param>
        /// <returns>A copy of this <see cref="IEnumerable{T}"/> as a <see cref="CharArraySet"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public static CharArraySet ToCharArraySet(this IEnumerable<string> collection, LuceneVersion matchVersion, bool ignoreCase)
        {
            return new CharArraySet(matchVersion, collection, ignoreCase);
        }
    }
}
