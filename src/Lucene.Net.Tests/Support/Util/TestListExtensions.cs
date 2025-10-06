using J2N.Collections.Generic.Extensions;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Util
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

    [TestFixture]
    public class TestListExtensions : LuceneTestCase
    {
        /// <summary>
        /// A custom list for the ListExtensions tests that should fall back to the unoptimized path.
        /// </summary>
        private class MyCustomList : IList<int>
        {
            private readonly List<int> _list = new();

            public IEnumerator<int> GetEnumerator() => _list.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(int item) => _list.Add(item);

            public void Clear() => _list.Clear();

            public bool Contains(int item) => _list.Contains(item);

            public void CopyTo(int[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

            public bool Remove(int item) => _list.Remove(item);

            public int Count => _list.Count;

            public bool IsReadOnly => false;

            public int IndexOf(int item) => _list.IndexOf(item);

            public void Insert(int index, int item) => _list.Insert(index, item);

            public void RemoveAt(int index) => _list.RemoveAt(index);

            public int this[int index]
            {
                get => _list[index];
                set => _list[index] = value;
            }
        }

        [Test, LuceneNetSpecific]
        public void TestAddRange_SCGList()
        {
            IList<int> list = new List<int> { 1, 2, 3 };
            list.AddRange(new[] { 4, 5, 6 });
            Assert.AreEqual(6, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
            Assert.AreEqual(4, list[3]);
            Assert.AreEqual(5, list[4]);
            Assert.AreEqual(6, list[5]);
        }

        [Test, LuceneNetSpecific]
        public void TestAddRange_JCGList()
        {
            IList<int> list = new J2N.Collections.Generic.List<int> { 1, 2, 3 };
            list.AddRange(new[] { 4, 5, 6 });
            Assert.AreEqual(6, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
            Assert.AreEqual(4, list[3]);
            Assert.AreEqual(5, list[4]);
            Assert.AreEqual(6, list[5]);
        }

        [Test, LuceneNetSpecific]
        public void TestAddRange_NonOptimized()
        {
            IList<int> list = new MyCustomList { 1, 2, 3 };
            list.AddRange(new[] { 4, 5, 6 });
            Assert.AreEqual(6, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
            Assert.AreEqual(4, list[3]);
            Assert.AreEqual(5, list[4]);
            Assert.AreEqual(6, list[5]);
        }

        [Test, LuceneNetSpecific]
        public void TestSort_SCGList()
        {
            IList<int> list = new List<int> { 3, 2, 1 };
            list.Sort();
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test, LuceneNetSpecific]
        public void TestSort_JCGList()
        {
            IList<int> list = new J2N.Collections.Generic.List<int> { 3, 2, 1 };
            list.Sort();
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test, LuceneNetSpecific]
        public void TestSort_NonOptimized()
        {
            IList<int> list = new MyCustomList { 3, 2, 1 };
            list.Sort();
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        private class ReverseComparer<T> : IComparer<T>
        {
            public int Compare(T x, T y) => Comparer<T>.Default.Compare(y, x);
        }

        [Test, LuceneNetSpecific]
        public void TestSortWithComparer_SCGList()
        {
            IList<int> list = new List<int> { 2, 1, 3 };
            list.Sort(new ReverseComparer<int>());
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);
        }

        [Test, LuceneNetSpecific]
        public void TestSortWithComparer_JCGList()
        {
            IList<int> list = new J2N.Collections.Generic.List<int> { 2, 1, 3 };
            list.Sort(new ReverseComparer<int>());
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);
        }

        [Test, LuceneNetSpecific]
        public void TestSortWithComparer_NonOptimized()
        {
            IList<int> list = new MyCustomList { 2, 1, 3 };
            list.Sort(new ReverseComparer<int>());
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);
        }

        [Test, LuceneNetSpecific]
        public void TestSortWithComparison_SCGList()
        {
            IList<int> list = new List<int> { 2, 1, 3 };
            list.Sort((x, y) => y.CompareTo(x));
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);
        }

        [Test, LuceneNetSpecific]
        public void TestSortWithComparison_JCGList()
        {
            IList<int> list = new J2N.Collections.Generic.List<int> { 2, 1, 3 };
            list.Sort((x, y) => y.CompareTo(x));
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);
        }

        [Test, LuceneNetSpecific]
        public void TestSortWithComparison_NonOptimized()
        {
            IList<int> list = new MyCustomList { 2, 1, 3 };
            list.Sort((x, y) => y.CompareTo(x));
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(1, list[2]);
        }

        [Test, LuceneNetSpecific]
        public void TestTimSort()
        {
            // ensuring our list is big enough to trigger TimSort
            List<int> list = Enumerable.Range(1, 1000).ToList();
            list.Shuffle();
            list.TimSort();
            for (int i = 0; i < list.Count - 1; i++)
            {
                Assert.LessOrEqual(list[i], list[i + 1]);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestTimSortWithComparer()
        {
            // ensuring our list is big enough to trigger TimSort
            List<int> list = Enumerable.Range(1, 1000).ToList();
            list.Shuffle();
            list.TimSort(new ReverseComparer<int>());
            for (int i = 0; i < list.Count - 1; i++)
            {
                Assert.GreaterOrEqual(list[i], list[i + 1]);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestIntroSort()
        {
            // ensuring our list is big enough to trigger IntroSort
            List<int> list = Enumerable.Range(1, 1000).ToList();
            list.Shuffle();
            list.IntroSort();
            for (int i = 0; i < list.Count - 1; i++)
            {
                Assert.LessOrEqual(list[i], list[i + 1]);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestIntroSortWithComparer()
        {
            // ensuring our list is big enough to trigger IntroSort
            List<int> list = Enumerable.Range(1, 1000).ToList();
            list.Shuffle();
            list.IntroSort(new ReverseComparer<int>());
            for (int i = 0; i < list.Count - 1; i++)
            {
                Assert.GreaterOrEqual(list[i], list[i + 1]);
            }
        }
    }
}
