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
    /// LUCENENET specific class used to adapt a <see cref="IList{T}"/> to one with a different type parameter
    /// where the elements are cast to the new type where needed.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the original list.</typeparam>
    /// <typeparam name="U">The type of the elements in the adapted list.</typeparam>
    internal class CastingListAdapter<T, U> : IList<U>
        where T : U
    {
        private readonly IList<T> list;

        public CastingListAdapter(IList<T> list)
        {
            this.list = list;
        }

        public U this[int index]
        {
            get => list[index];
            set => list[index] = (T)value;
        }

        public int Count => list.Count;

        public bool IsReadOnly => list.IsReadOnly;

        public void Add(U item) => list.Add((T)item);

        public void Clear() => list.Clear();

        public bool Contains(U item) => list.Contains((T)item);

        public void CopyTo(U[] array, int arrayIndex)
        {
            for (int i = 0; i < list.Count; i++)
            {
                array[arrayIndex + i] = list[i];
            }
        }

        public CastingEnumeratorAdapter<T, U> GetEnumerator() => new CastingEnumeratorAdapter<T, U>(list.GetEnumerator());

        IEnumerator<U> IEnumerable<U>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int IndexOf(U item) => list.IndexOf((T)item);

        public void Insert(int index, U item) => list.Insert(index, (T)item);

        public bool Remove(U item) => list.Remove((T)item);

        public void RemoveAt(int index) => list.RemoveAt(index);
    }
}
