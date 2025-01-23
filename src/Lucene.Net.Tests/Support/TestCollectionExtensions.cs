using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
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

    /// <summary>
    /// Tests for <see cref="CollectionExtensions"/>
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestCollectionExtensions : LuceneTestCase
    {
        [Test]
        public void RetainAll_EmptySource()
        {
            ISet<int> set = new JCG.HashSet<int>(); // empty
            bool result = set.RetainAll(new[] { 1, 2, 3 });
            Assert.IsFalse(result);
            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void RetainAll_Set()
        {
            ISet<int> set = new JCG.HashSet<int> { 1, 2, 3, 4, 5 };
            bool result = set.RetainAll(new[] { 1, 2, 3 });
            Assert.IsTrue(result);
            Assert.AreEqual(3, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
            Assert.IsTrue(set.Contains(3));
            Assert.IsFalse(set.Contains(4));
            Assert.IsFalse(set.Contains(5));
        }

        [Test]
        public void RetainAll_List()
        {
            IList<int> list = new JCG.List<int> { 1, 2, 3, 4, 5 };
            bool result = list.RetainAll(new[] { 1, 2, 3 });
            Assert.IsTrue(result);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void RetainAll_Collection()
        {
            ICollection<int> collection = new TestCollection<int> { 1, 2, 3, 4, 5 };
            bool result = collection.RetainAll(new[] { 1, 2, 3 });
            Assert.IsTrue(result);
            Assert.AreEqual(3, collection.Count);
            Assert.IsTrue(collection.Contains(1));
            Assert.IsTrue(collection.Contains(2));
            Assert.IsTrue(collection.Contains(3));
            Assert.IsFalse(collection.Contains(4));
            Assert.IsFalse(collection.Contains(5));
        }

        [Test]
        public void RemoveAll_EmptySource()
        {
            ISet<int> set = new JCG.HashSet<int>(); // empty
            bool result = set.RemoveAll(new[] { 1, 2, 3 });
            Assert.IsFalse(result);
            Assert.AreEqual(0, set.Count);
        }

        [Test]
        public void RemoveAll_Set()
        {
            ISet<int> set = new JCG.HashSet<int> { 1, 2, 3, 4, 5 };
            bool result = set.RemoveAll(new[] { 1, 2, 3 });
            Assert.IsTrue(result);
            Assert.AreEqual(2, set.Count);
            Assert.IsFalse(set.Contains(1));
            Assert.IsFalse(set.Contains(2));
            Assert.IsFalse(set.Contains(3));
            Assert.IsTrue(set.Contains(4));
            Assert.IsTrue(set.Contains(5));
        }

        [Test]
        public void RemoveAll_List()
        {
            IList<int> list = new JCG.List<int> { 1, 2, 3, 4, 5 };
            bool result = list.RemoveAll(new[] { 1, 2, 3 });
            Assert.IsTrue(result);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(4, list[0]);
            Assert.AreEqual(5, list[1]);
        }

        [Test]
        public void RemoveAll_Collection()
        {
            ICollection<int> collection = new TestCollection<int> { 1, 2, 3, 4, 5 };
            bool result = collection.RemoveAll(new[] { 1, 2, 3 });
            Assert.IsTrue(result);
            Assert.AreEqual(2, collection.Count);
            Assert.IsFalse(collection.Contains(1));
            Assert.IsFalse(collection.Contains(2));
            Assert.IsFalse(collection.Contains(3));
            Assert.IsTrue(collection.Contains(4));
            Assert.IsTrue(collection.Contains(5));
        }

        /// <summary>
        /// A simple implementation of <see cref="ICollection{T}"/> for testing purposes,
        /// that intentionally does not implement <see cref="IList{T}"/> or <see cref="ISet{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <see cref="ICollection{T}"/>.</typeparam>
        private class TestCollection<T> : ICollection<T>
        {
            private readonly List<T> _items = new();

            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(T item) => _items.Add(item);

            public void Clear() => _items.Clear();

            public bool Contains(T item) => _items.Contains(item);

            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

            public bool Remove(T item) => _items.Remove(item);

            public int Count => _items.Count;

            public bool IsReadOnly => false;
        }
    }
}
