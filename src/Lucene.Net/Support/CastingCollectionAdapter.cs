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
    /// LUCENENET specific class used to adapt a <see cref="ICollection{T}"/> to one with a different type parameter
    /// where the elements are cast to the new type where needed.
    /// </summary>
    /// <typeparam name="T">The type of elements in the original collection.</typeparam>
    /// <typeparam name="U">The type of elements in the adapted collection.</typeparam>
    internal class CastingCollectionAdapter<T, U> : ICollection<U>
        where T : U
    {
        private readonly ICollection<T> collection;

        public CastingCollectionAdapter(ICollection<T> collection)
        {
            this.collection = collection;
        }

        public int Count => collection.Count;

        public bool IsReadOnly => collection.IsReadOnly;

        public void Add(U item) => collection.Add((T)item);

        public void Clear() => collection.Clear();

        public bool Contains(U item) => collection.Contains((T)item);

        public void CopyTo(U[] array, int arrayIndex)
        {
            int i = 0;
            foreach (T item in collection)
            {
                array[arrayIndex + i] = item;
                i++;
            }
        }

        public IEnumerator<U> GetEnumerator() => new CastingEnumeratorAdapter<T, U>(collection.GetEnumerator());

        public bool Remove(U item) => collection.Remove((T)item);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
