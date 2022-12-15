using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using J2N.Collections.Generic;

namespace Lucene.Net
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for Additional information regarding copyright ownership.
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

    public class TestMultiset : LuceneTestCase
    {
        [Test]
        public void TestDuplicatesMatter() {
            Multiset<int> s1 = new Multiset<int>();
            Multiset<int> s2 = new Multiset<int>();
            assertEquals(s1.size(), s2.size());
            assertEquals(s1, s2);

            s1.Add(42);
            s2.Add(42);
            assertEquals(s1, s2);

            s2.Add(42);
            assertFalse(s1.equals(s2));

            s1.Add(43);
            s1.Add(43);
            s2.Add(43);
            assertEquals(s1.size(), s2.size());
            assertFalse(s1.equals(s2));
        }

        private static Dictionary<T, int> ToCountMap<T>(Multiset<T> set) {
            Dictionary<T, int> map = new();
            int recomputedSize = 0;

            foreach (T element in set) {
                Add(map, element);
                recomputedSize += 1;
            }
            assertEquals(set.toString(), recomputedSize, set.size());
            return map;
        }

        private static  void Add<T>(Dictionary<T, int> map, T element)
        {
            map.TryGetValue(element, out int value);
            map.Put(element, value + 1);
        }

        private static void Remove<T>(Dictionary<T, int> map, T element) {
            if (element is null)
            {
                return;
            }

            map.TryGetValue(element, out int cnt);
            switch (cnt)
            {
                case 0:
                    return;
                case 1:
                    map.Remove(element);
                    break;
                default:
                    map.Put((T)element, cnt - 1);
                    break;
            }
        }

        [Test]
        public void TestRandom() {
            Dictionary<int, int> reference = new();
            Multiset<int> multiset = new();
            int iters = AtLeast(100);
            for (int i = 0; i < iters; ++i) {
                int value = Random.Next(10);
                switch (Random.Next(10)) {
                    case 0:
                    case 1:
                    case 2:
                        Remove(reference, value);
                        multiset.Remove(value);
                        break;
                    case 3:
                        reference.Clear();
                        multiset.Clear();
                        break;
                    default:
                        Add(reference, value);
                        multiset.Add(value);
                        break;
                }
                assertEquals(reference, ToCountMap(multiset));
            }
        }
    }
}