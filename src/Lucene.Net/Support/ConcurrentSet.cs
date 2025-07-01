using J2N.Text;
using Lucene.Net.Support.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
#nullable enable

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

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class ConcurrentSet<T> : ISet<T>, ICollection, IStructuralEquatable, IFormattable
    {
#if FEATURE_SERIALIZABLE
        [NonSerialized]
#endif
        private readonly object syncRoot = new object();
        private readonly ISet<T> set;

        public ConcurrentSet(ISet<T> set)
        {
            this.set = set ?? throw new ArgumentNullException(nameof(set));
        }

        public bool Add(T item)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.Add(item);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                set.ExceptWith(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                set.IntersectWith(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.IsProperSubsetOf(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.IsProperSupersetOf(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.IsSubsetOf(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.IsSupersetOf(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.Overlaps(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.SetEquals(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                set.SymmetricExceptWith(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                set.UnionWith(other);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        void ICollection<T>.Add(T item)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                set.Add(item);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public void Clear()
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                set.Clear();
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public bool Contains(T item)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.Contains(item);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                set.CopyTo(array, arrayIndex);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("Only single dimensional arrays are supported for the requested action.", nameof(array));
                //throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("The lower bound of target array must be zero.", nameof(array));
                //throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Non-negative number required.");
                //throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - index < Count)
                throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
            //throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            if (array is T[] tArray)
            {
                CopyTo(tArray, index);
            }
            else
            {
                if (array is not object?[] objects)
                {
                    throw new ArgumentException("Target array type is not compatible with the type of items in the collection.", nameof(array));
                    //throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }

                try
                {
                    UninterruptableMonitor.Enter(syncRoot);
                    try
                    {
                        foreach (var item in set)
                            objects[index++] = item;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(syncRoot);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException("Target array type is not compatible with the type of items in the collection.", nameof(array));
                    //throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }

        public int Count
        {
            get
            {
                UninterruptableMonitor.Enter(syncRoot);
                try
                {
                    return set.Count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(syncRoot);
                }
            }
        }

        public bool IsReadOnly => set.IsReadOnly;

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

        public bool Remove(T item)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return set.Remove(item);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                // Make a copy of the contents since enumeration is lazy and not thread-safe
                T[] array = new T[set.Count];
                set.CopyTo(array, 0);
                return ((IEnumerable<T>)array).GetEnumerator();
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Structural Equality

        /// <summary>
        /// Determines whether the specified object is structurally equal to the current set
        /// using rules provided by the specified <paramref name="comparer"/>.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer"/> implementation to use to determine
        /// whether the current object and <paramref name="other"/> are structurally equal.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is structurally equal to the current set;
        /// otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="comparer"/> is <c>null</c>.</exception>
        public bool Equals(object? other, IEqualityComparer comparer)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return JCG.SetEqualityComparer<T>.Equals(set, other, comparer);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        /// <summary>
        /// Gets the hash code representing the current set using rules specified by the
        /// provided <paramref name="comparer"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer"/> implementation to use to generate
        /// the hash code.</param>
        /// <returns>A hash code representing the current set.</returns>
        public int GetHashCode(IEqualityComparer comparer)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                return JCG.SetEqualityComparer<T>.GetHashCode(set, comparer);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        /// <summary>
        /// Determines whether the specified object is structurally equal to the current set
        /// using rules similar to those in the JDK's AbstractSet class. Two sets are considered
        /// equal when they both contain the same objects (in any order).
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object implements <see cref="ISet{T}"/>
        /// and it contains the same elements; otherwise, <c>false</c>.</returns>
        /// <seealso cref="Equals(object, IEqualityComparer)"/>
        public override bool Equals(object? obj)
            => Equals(obj, JCG.SetEqualityComparer<T>.Default);

        /// <summary>
        /// Gets the hash code for the current list. The hash code is calculated
        /// by taking each nested element's hash code into account.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <seealso cref="GetHashCode(IEqualityComparer)"/>
        public override int GetHashCode()
            => GetHashCode(JCG.SetEqualityComparer<T>.Default);

        #endregion

        #region ToString

        /// <summary>
        /// Returns a string that represents the current set using the specified
        /// <paramref name="format"/> and <paramref name="formatProvider"/>.
        /// </summary>
        /// <returns>A string that represents the current set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">
        /// <paramref name="format"/> is invalid.
        /// <para/>
        /// -or-
        /// <para/>
        /// The index of a format item is not zero.
        /// </exception>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            UninterruptableMonitor.Enter(syncRoot);
            try
            {
                if (set is IFormattable formattable)
                    return formattable.ToString(format ?? "{0}", formatProvider);

                return string.Format(formatProvider, format ?? "{0}", set);
            }
            finally
            {
                UninterruptableMonitor.Exit(syncRoot);
            }
        }

        /// <summary>
        /// Returns a string that represents the current set using
        /// <see cref="StringFormatter.CurrentCulture"/>.
        /// <para/>
        /// The presentation has a specific format. It is enclosed by square
        /// brackets ("[]"). Elements are separated by ', ' (comma and space).
        /// </summary>
        /// <returns>A string that represents the current set.</returns>
        public override string ToString()
            => ToString("{0}", StringFormatter.CurrentCulture);

        /// <summary>
        /// Returns a string that represents the current set using the specified
        /// <paramref name="formatProvider"/>.
        /// </summary>
        /// <returns>A string that represents the current set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="formatProvider"/> is <c>null</c>.</exception>
        public string ToString(IFormatProvider? formatProvider)
            => ToString("{0}", formatProvider);

        /// <summary>
        /// Returns a string that represents the current set using the specified
        /// <paramref name="format"/> and <see cref="StringFormatter.CurrentCulture"/>.
        /// <para/>
        /// The presentation has a specific format. It is enclosed by square
        /// brackets ("[]"). Elements are separated by ', ' (comma and space).
        /// </summary>
        /// <returns>A string that represents the current set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">
        /// <paramref name="format"/> is invalid.
        /// <para/>
        /// -or-
        /// <para/>
        /// The index of a format item is not zero.
        /// </exception>
        public string ToString(string? format)
            => ToString(format, StringFormatter.CurrentCulture);



        #endregion
    }
}
