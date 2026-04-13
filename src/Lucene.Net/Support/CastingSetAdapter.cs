using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    /// LUCENENET specific class used to adapt a <see cref="ISet{T}"/> to one with a different type parameter.
    /// </summary>
    /// <typeparam name="T">The type of elements in the original set.</typeparam>
    /// <typeparam name="U">The type of elements in the adapted set.</typeparam>
    internal class CastingSetAdapter<T, U> : ISet<U>
        where T : U
    {
        private readonly ISet<T> set;

        public CastingSetAdapter(ISet<T> set)
        {
            this.set = set;
        }

        public CastingEnumeratorAdapter<T, U> GetEnumerator() => new CastingEnumeratorAdapter<T, U>(set.GetEnumerator());

        IEnumerator<U> IEnumerable<U>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<U>.Add(U item) => set.Add(((T)item)!);

        public void UnionWith(IEnumerable<U> other) => set.UnionWith(other.Cast<T>());

        public void IntersectWith(IEnumerable<U> other) => set.IntersectWith(other.Cast<T>());

        public void ExceptWith(IEnumerable<U> other) => set.ExceptWith(other.Cast<T>());

        public void SymmetricExceptWith(IEnumerable<U> other) => set.SymmetricExceptWith(other.Cast<T>());

        public bool IsSubsetOf(IEnumerable<U> other) => set.IsSubsetOf(other.Cast<T>());

        public bool IsSupersetOf(IEnumerable<U> other) => set.IsSupersetOf(other.Cast<T>());

        public bool IsProperSupersetOf(IEnumerable<U> other) => set.IsProperSupersetOf(other.Cast<T>());

        public bool IsProperSubsetOf(IEnumerable<U> other) => set.IsProperSubsetOf(other.Cast<T>());

        public bool Overlaps(IEnumerable<U> other) => set.Overlaps(other.Cast<T>());

        public bool SetEquals(IEnumerable<U> other) => set.SetEquals(other.Cast<T>());

        bool ISet<U>.Add(U item) => set.Add((T)item);

        public void Clear() => set.Clear();

        public bool Contains(U item) => set.Contains((T)item);

        public void CopyTo(U[] array, int arrayIndex)
        {
            int i = 0;
            foreach (T item in set)
            {
                array[i + arrayIndex] = item;
                i++;
            }
        }

        public bool Remove(U item) => set.Remove((T)item);

        public int Count => set.Count;

        public bool IsReadOnly => set.IsReadOnly;
    }
}
