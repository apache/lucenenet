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

        public IEnumerator<U> GetEnumerator() => new CastingEnumeratorAdapter<T, U>(set.GetEnumerator());

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<U>.Add(U item) => set.Add(((T)item)!);

        public void UnionWith(IEnumerable<U> other) => set.UnionWith((IEnumerable<T>)other);

        public void IntersectWith(IEnumerable<U> other) => set.IntersectWith((IEnumerable<T>)other);

        public void ExceptWith(IEnumerable<U> other) => set.ExceptWith((IEnumerable<T>)other);

        public void SymmetricExceptWith(IEnumerable<U> other) => set.SymmetricExceptWith((IEnumerable<T>)other);

        public bool IsSubsetOf(IEnumerable<U> other) => set.IsSubsetOf((IEnumerable<T>)other);

        public bool IsSupersetOf(IEnumerable<U> other) => set.IsSupersetOf((IEnumerable<T>)other);

        public bool IsProperSupersetOf(IEnumerable<U> other) => set.IsProperSupersetOf((IEnumerable<T>)other);

        public bool IsProperSubsetOf(IEnumerable<U> other) => set.IsProperSubsetOf((IEnumerable<T>)other);

        public bool Overlaps(IEnumerable<U> other) => set.Overlaps((IEnumerable<T>)other);

        public bool SetEquals(IEnumerable<U> other) => set.SetEquals((IEnumerable<T>)other);

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
