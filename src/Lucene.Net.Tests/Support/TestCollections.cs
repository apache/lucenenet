// Some tests adapted from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/CollectionsTest.java

using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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

    [TestFixture]
    public class TestCollections : LuceneTestCase
    {
        private List<object> ll = null!; // LUCENENET specific: was LinkedList in Harmony tests, !: will be initialized in SetUp

        // LUCENENET - omitting unused fields

        private static object[] objArray = LoadObjArray(); // LUCENENET - use static loader method instead of static ctor

        private static object[] LoadObjArray()
        {
            object[] objArray = new object[1000];
            for (int i = 0; i < objArray.Length; i++)
            {
                objArray[i] = i;
            }

            return objArray;
        }

        [Test, LuceneNetSpecific]
        public void TestEmptyList()
        {
            IList<object> list = Collections.EmptyList<object>();

            Assert.AreEqual(0, list.Count);
            Assert.IsTrue(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(new object()));

            IList<object> list2 = Collections.EmptyList<object>();

            Assert.AreSame(list, list2); // ensure it does not allocate
        }

        [Test, LuceneNetSpecific]
        public void TestEmptyMap()
        {
            IDictionary<object, object> map = Collections.EmptyMap<object, object>();

            Assert.AreEqual(0, map.Count);
            Assert.IsTrue(map.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => map.Add(new object(), new object()));

            IDictionary<object, object> map2 = Collections.EmptyMap<object, object>();

            Assert.AreSame(map, map2); // ensure it does not allocate
        }

        [Test, LuceneNetSpecific]
        public void TestEmptySet()
        {
            ISet<object> set = Collections.EmptySet<object>();

            Assert.AreEqual(0, set.Count);
            Assert.IsTrue(set.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => set.Add(new object()));

            ISet<object> set2 = Collections.EmptySet<object>();

            Assert.AreSame(set, set2); // ensure it does not allocate
        }

        /// <summary>
        /// Adapted from Harmony test_reverseLjava_util_List()
        /// </summary>
        [Test]
        public void TestReverse()
        {
            // Test for method void java.util.Collections.reverse(java.util.List)
            try
            {
                Collections.Reverse<object>(null!);
                fail("Expected NullPointerException for null list parameter");
            }
            catch (Exception e) when (e.IsNullPointerException())
            {
                //Expected
            }

            Collections.Reverse(ll);
            using var i = ll.GetEnumerator();
            int count = objArray.Length - 1;
            while (i.MoveNext())
            {
                assertEquals("Failed to reverse collection", objArray[count], i.Current);
                --count;
            }

            var myList = new List<object?>
            {
                null,
                20,
            };
            Collections.Reverse(myList);
            assertEquals($"Did not reverse correctly--first element is: {myList[0]}", 20, myList[0]);
            assertNull($"Did not reverse correctly--second element is: {myList[1]}", myList[1]);
        }

        /// <summary>
        /// Adapted from Harmony test_reverseOrder()
        /// </summary>
        [Test]
        public void TestReverseOrder() {
            // Test for method IComparer<T>
            // Collections.ReverseOrder()
            // assumes no duplicates in ll
            IComparer<object> comp = Collections.ReverseOrder<object>();
            var list2 = new List<object>(ll); // LUCENENET - was LinkedList in Harmony
            list2.Sort(comp);
            int llSize = ll.Count;
            for (int counter = 0; counter < llSize; counter++)
            {
                assertEquals("New comparator does not reverse sorting order", list2[llSize - counter - 1], ll[counter]);
            }
        }

        [Test, LuceneNetSpecific]
        public void TestReverseOrder_WithComparer()
        {
            IComparer<string> comp = Collections.ReverseOrder<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string> { "B", "c", "a", "D" };
            list.Sort(comp);
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual("D", list[0]);
            Assert.AreEqual("c", list[1]);
            Assert.AreEqual("B", list[2]);
            Assert.AreEqual("a", list[3]);
        }

        [Test, LuceneNetSpecific]
        public void TestReverseOrder_NullableValueTypes()
        {
            IComparer<int?> comp = Collections.ReverseOrder<int?>();
            var list = new List<int?> { 5, null, 2, 8, null, 1, 3 };
            list.Sort(comp);

            Assert.AreEqual(7, list.Count);
            Assert.AreEqual(8, list[0]);
            Assert.AreEqual(5, list[1]);
            Assert.AreEqual(3, list[2]);
            Assert.AreEqual(2, list[3]);
            Assert.AreEqual(1, list[4]);
            Assert.IsNull(list[5]);
            Assert.IsNull(list[6]);
        }

        [Test, LuceneNetSpecific]
        public void TestReverseOrder_NullableValueTypes_WithComparer()
        {
            IComparer<double?> baseComparer = Comparer<double?>.Default;
            IComparer<double?> comp = Collections.ReverseOrder(baseComparer);
            var list = new List<double?> { 3.14, null, 2.71, null, 1.41, 0.0 };
            list.Sort(comp);

            Assert.AreEqual(6, list.Count);
            Assert.AreEqual(3.14, list[0]);
            Assert.AreEqual(2.71, list[1]);
            Assert.AreEqual(1.41, list[2]);
            Assert.AreEqual(0.0, list[3]);
            Assert.IsNull(list[4]);
            Assert.IsNull(list[5]);
        }

        [Test, LuceneNetSpecific]
        public void TestReverseOrder_NullableReferenceTypes()
        {
            IComparer<string?> comp = Collections.ReverseOrder<string?>();
            var list = new List<string?> { "zebra", null, "apple", "mango", null, "banana" };
            list.Sort(comp);

            Assert.AreEqual(6, list.Count);
            Assert.AreEqual("zebra", list[0]);
            Assert.AreEqual("mango", list[1]);
            Assert.AreEqual("banana", list[2]);
            Assert.AreEqual("apple", list[3]);
            Assert.IsNull(list[4]);
            Assert.IsNull(list[5]);
        }

        [Test, LuceneNetSpecific]
        public void TestReverseOrder_NullableReferenceTypes_WithComparer()
        {
            IComparer<string?> comp = Collections.ReverseOrder(StringComparer.OrdinalIgnoreCase);
            var list = new List<string?> { "Zebra", null, "apple", "Mango", null, "BANANA" };
            list.Sort(comp);

            Assert.AreEqual(6, list.Count);
            Assert.AreEqual("Zebra", list[0]);
            Assert.AreEqual("Mango", list[1]);
            Assert.AreEqual("BANANA", list[2]);
            Assert.AreEqual("apple", list[3]);
            Assert.IsNull(list[4]);
            Assert.IsNull(list[5]);
        }

        [Test, LuceneNetSpecific]
        public void TestSingletonMap()
        {
            IDictionary<string, string> map = Collections.SingletonMap("key", "value");

            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => map.Add("key2", "value2"));
            Assert.Throws<NotSupportedException>(() => map["key"] = "value2");

            Assert.AreEqual("value", map["key"]);
        }

        [Test, LuceneNetSpecific]
        public void TestToString_Collection_Null()
        {
            Assert.AreEqual("null", Collections.ToString<object>(null!));
        }

        [Test, LuceneNetSpecific]
        public void TestToString_Collection_Empty()
        {
            Assert.AreEqual("[]", Collections.ToString(new List<object>()));
        }

        [Test, LuceneNetSpecific]
        public void TestToString_Collection()
        {
            var list = new List<object?>();
            list.Add(list);
            list.Add(1);
            list.Add('a');
            list.Add(2.1);
            list.Add("xyz");
            list.Add(new List<int> { 1, 2, 3 });
            list.Add(null);

            Assert.AreEqual("[(this Collection), 1, a, 2.1, xyz, [1, 2, 3], null]", Collections.ToString(list));
        }

        [Test, LuceneNetSpecific]
        public void TestToString_Dictionary()
        {
            var dict = new Dictionary<object, object?>()
            {
                { "key1", "value1" },
                { "key2", 2 },
                { "key3", 'a' },
                { "key4", 3.1 },
                { "key5", new List<int> { 1, 2, 3 } },
                { "key6", null }
            };

            Assert.AreEqual("{key1=value1, key2=2, key3=a, key4=3.1, key5=[1, 2, 3], key6=null}", Collections.ToString(dict));
        }

        [Test, LuceneNetSpecific]
        public void TestToString_Object_Null()
        {
            Assert.AreEqual("null", Collections.ToString(null));
        }

        [Test, LuceneNetSpecific]
        public void TestToString_Object()
        {
            Assert.AreEqual("1", Collections.ToString(1));
            Assert.AreEqual("a", Collections.ToString('a'));
            Assert.AreEqual("2.1", Collections.ToString(2.1));
            Assert.AreEqual("xyz", Collections.ToString("xyz"));
            Assert.AreEqual("[1, 2, 3]", Collections.ToString(new List<int> { 1, 2, 3 }));
        }

        [Test, LuceneNetSpecific]
        public void TestAsReadOnly_List()
        {
            var list = new List<object> { 1, 2, 3 };
            IList<object> readOnlyList = Collections.AsReadOnly(list);

            Assert.AreEqual(3, readOnlyList.Count);
            Assert.IsTrue(readOnlyList.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => readOnlyList.Add(4));
            Assert.Throws<NotSupportedException>(() => readOnlyList[0] = 5);
        }

        [Test, LuceneNetSpecific]
        public void TestAsReadOnly_Dictionary()
        {
            var dict = new Dictionary<object, object>
            {
                { "key1", "value1" },
                { "key2", 2 },
                { "key3", 'a' }
            };
            IDictionary<object, object> readOnlyDict = Collections.AsReadOnly(dict);

            Assert.AreEqual(3, readOnlyDict.Count);
            Assert.IsTrue(readOnlyDict.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => readOnlyDict.Add("key4", 4));
            Assert.Throws<NotSupportedException>(() => readOnlyDict["key1"] = "value2");
        }

        public override void SetUp()
        {
            base.SetUp();

            ll = new List<object>();
            // LUCENENET - omitting unused fields

            for (int i = 0; i < objArray.Length; i++)
            {
                ll.Add(objArray[i]);
            }
        }
    }
}
