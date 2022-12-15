using J2N.Collections.Generic.Extensions;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Lucene.Net.Search
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

    public class Multiset<T> : ICollection<T>
    {
        private readonly J2N.Collections.Generic.Dictionary<T, int> map = new();

        private int count = 0;

        public Multiset()
        {
        }

        public Multiset(Multiset<T> multiset)
        {
            if (multiset == null)
                throw new ArgumentNullException(nameof(multiset));

            foreach (var element in multiset)
            {
                Add(element);
            }
        }



        public IEnumerator<T> GetEnumerator() => new MultiSetEnumerator(map);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private class MultiSetEnumerator : IEnumerator<T>
        {
            private int remaining;
            private T current;
            private IEnumerator<KeyValuePair<T, int>> mapEnumerator;

            public MultiSetEnumerator(J2N.Collections.Generic.Dictionary<T, int> map)
            {
                mapEnumerator = map.GetEnumerator();
                current = mapEnumerator.Current.Key;
                remaining = mapEnumerator.Current.Value;
            }

            public T Current => current;

            object IEnumerator.Current => current;

            public void Dispose() { }

            public bool MoveNext()
            {
                if (remaining == 0)
                {
                    if (mapEnumerator.MoveNext())
                    {
                        current = mapEnumerator.Current.Key;
                        remaining = mapEnumerator.Current.Value;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (remaining <= 0)
                {
                    throw AssertionError.Create("Inner error: remaining count should be positive");
                }
                --remaining;
                return true;
            }

            public void Reset()
            {
                mapEnumerator.Reset();
                current = mapEnumerator.Current.Key;
                remaining = mapEnumerator.Current.Value;
            }

        }

        public void Add(T item)
        {
            if (item is null)
            {
                return;
            }

            map.TryGetValue(item, out int preValue);
            map.Put(item, preValue + 1);
            count += 1;
        }

        public void AddRange(ICollection<T> items)
        {
            if (items == null) return;
                foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Clear()
        {
            map.Clear();
            count = 0;
        }

        public bool Contains(T item)
        {
            return item is not null && map.ContainsKey(item);
        }


        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array.Length < arrayIndex + count)
                throw new IndexOutOfRangeException("Array is too small");

            int i = 0;
            foreach (var item in this)
                array[arrayIndex + i++] = item;
        }

        public bool Remove(T item)
        {
            if (item is null)
            {
                return false;
            }

            map.TryGetValue(item, out int cnt);
            switch (cnt)
            {
                case 0:
                    return false;
                case 1:
                    map.Remove(item);
                    break;
                default:
                    map.Put((T)item, cnt - 1);
                    break;
            }

            count -= 1;
            return true;
        }

        public int Count => count;

        public bool IsReadOnly => false;

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType()) {
                return false;
            }

            Multiset<T> that = (Multiset<T>) obj;
            return count == that.count // not necessary but helps escaping early
                   && map.Equals(that.map);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() + map.GetHashCode();
        }
    }
}