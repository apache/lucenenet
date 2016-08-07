using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

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

namespace Lucene.Net.Tests.Analysis.Common.Analysis.Util
{
    [TestFixture]
    public class TestCharArrayMap_ : LuceneTestCase
    {
        public virtual void DoRandom(int iter, bool ignoreCase)
        {
            CharArrayMap<int?> map = new CharArrayMap<int?>(TEST_VERSION_CURRENT, 1, ignoreCase);
            HashMap<string, int?> hmap = new HashMap<string, int?>();

            char[] key;
            for (int i = 0; i < iter; i++)
            {
                int len = Random().Next(5);
                key = new char[len];
                for (int j = 0; j < key.Length; j++)
                {
                    key[j] = (char)Random().Next(127);
                }
                string keyStr = new string(key);
                string hmapKey = ignoreCase ? keyStr.ToLower() : keyStr;

                int val = Random().Next();

                object o1 = map.Put(key, val);
                object o2 = hmap.Put(hmapKey, val);
                assertEquals(o1, o2);

                // add it again with the string method
                assertEquals(val, map.Put(keyStr, val));

                assertEquals(val, map.Get(key, 0, key.Length));
                assertEquals(val, map.Get(key));
                assertEquals(val, map.Get(keyStr));

                assertEquals(hmap.Count, map.size());
            }
        }

        [Test]
        public virtual void TestCharArrayMap()
        {
            int num = 5 * RANDOM_MULTIPLIER;
            for (int i = 0; i < num; i++)
            { // pump this up for more random testing
                DoRandom(1000, false);
                DoRandom(1000, true);
            }
        }

        [Test]
        public virtual void TestMethods()
        {
            CharArrayMap<int?> cm = new CharArrayMap<int?>(TEST_VERSION_CURRENT, 2, false);
            //Dictionary<string, int?> hm = new Dictionary<string, int?>();
            Dictionary<object, int?> hm = new Dictionary<object, int?>(); // TODO: In .NET, we cannot implicitly convert from string to object using generics
            hm["foo"] = 1;
            hm["bar"] = 2;
            cm.PutAll(hm);
            assertEquals(hm.Count, cm.Count);
            hm["baz"] = 3;
            cm.PutAll(hm);
            assertEquals(hm.Count, cm.Count);

            // TODO: In .NET we cannot make this conversion implicitly.
            CharArraySet cs = cm.Keys as CharArraySet;
            int n = 0;
            foreach (object o in cs)
            {
                assertTrue(cm.ContainsKey(o));
                char[] co = (char[])o;
                assertTrue(cm.ContainsKey(co, 0, co.Length));
                n++;
            }
            assertEquals(hm.Count, n);
            assertEquals(hm.Count, cs.Count);
            assertEquals(cm.Count, cs.Count);

            // TODO: This directly contradicts the TestModifyOnUnmodifiable test,
            // where clear is not allowed from the Keys property.
            //cs.Clear();
            //assertEquals(0, cs.Count);
            //assertEquals(0, cm.Count);
            try
            {
                cs.Add("test");
                fail("keySet() allows adding new keys");
            }
            catch (System.NotSupportedException)
            {
                // pass
            }
            cm.PutAll(hm);
            assertEquals(hm.Count, cs.Count);
            assertEquals(cm.Count, cs.Count);

            IEnumerator<KeyValuePair<object, int?>> iter1 = IDictionaryExtensions.EntrySet(cm).GetEnumerator();
            n = 0;
            while (iter1.MoveNext())
            {
                KeyValuePair<object, int?> entry = iter1.Current;
                object key = entry.Key;
                int? val = entry.Value;
                assertEquals(cm.Get(key), val);

                // TODO: In .NET the Value property of KeyValuePair is read-only. Do we need a solution?
                //entry.Value = val * 100;
                //assertEquals(val * 100, (int)cm.Get(key));
                n++;
            }
            assertEquals(hm.Count, n);
            cm.Clear();
            cm.PutAll(hm);
            assertEquals(cm.size(), n);

            CharArrayMap<int?>.EntryIterator iter2 = cm.EntrySet().GetEnumerator() as CharArrayMap<int?>.EntryIterator;
            n = 0;
            while (iter2.MoveNext())
            {
                char[] keyc = (char[])iter2.Current.Key;
                int? val = iter2.Current.Value;
                assertEquals(hm[new string(keyc)], val);

                // TODO: In .NET the Value property of KeyValuePair is read-only. Do we need a solution?
                //iter2.Value = val * 100;
                //assertEquals(val * 100, (int)cm.Get(keyc));
                n++;
            }
            assertEquals(hm.Count, n);

            // TODO: In .NET, the EntrySet extension method makes a copy of the data
            // so clearing it won't work like this.
            cm.EntrySet().clear();
            assertEquals(0, cm.size());
            assertEquals(0, cm.EntrySet().size());
            assertTrue(cm.Count == 0);
        }

        [Test]
        public void TestModifyOnUnmodifiable()
        {
            CharArrayMap<int?> map = new CharArrayMap<int?>(TEST_VERSION_CURRENT, 2, false);
            map.Put("foo", 1);
            map.Put("bar", 2);
            int size = map.Count;
            assertEquals(2, size);
            assertTrue(map.ContainsKey("foo"));
            assertEquals(1, map.Get("foo"));
            assertTrue(map.ContainsKey("bar"));
            assertEquals(2, map.Get("bar"));

            map = CharArrayMap<int?>.UnmodifiableMap(map);
            assertEquals("Map size changed due to unmodifiableMap call", size, map.Count);
            var NOT_IN_MAP = "SirGallahad";
            assertFalse("Test String already exists in map", map.ContainsKey(NOT_IN_MAP));
            assertNull("Test String already exists in map", map.Get(NOT_IN_MAP));

            try
            {
                map.Put(NOT_IN_MAP.ToCharArray(), 3);
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                map.Put(NOT_IN_MAP, 3);
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                map.Put(new StringBuilder(NOT_IN_MAP), 3);
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            #region Added for better .NET support
            try
            {
                map.Add(new StringBuilder(NOT_IN_MAP), 3);
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                map.Add(new KeyValuePair<object, int?>(NOT_IN_MAP, 3));
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                map[new StringBuilder(NOT_IN_MAP)] = 3;
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                map.Remove(new KeyValuePair<object, int?>("foo", 1));
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }
            #endregion

            try
            {
                map.Clear();
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                map.EntrySet().Clear();
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                map.Keys.Clear();
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                map.Put((object)NOT_IN_MAP, 3);
                fail("Modified unmodifiable map");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            // TODO: In .NET we don't have an overload of PutAll that will convert this.
            //try
            //{
            //    map.PutAll<string, int>(Collections.SingletonMap(NOT_IN_MAP, 3));
            //    fail("Modified unmodifiable map");
            //}
            //catch (System.NotSupportedException)
            //{
            //    // expected
            //    assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
            //    assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
            //    assertEquals("Size of unmodifiable map has changed", size, map.size());
            //}

            assertTrue(map.ContainsKey("foo"));
            assertEquals(1, map.Get("foo"));
            assertTrue(map.ContainsKey("bar"));
            assertEquals(2, map.Get("bar"));
        }

        [Test]
        public virtual void TestToString()
        {
            CharArrayMap<int?> cm = new CharArrayMap<int?>(TEST_VERSION_CURRENT, Collections.SingletonMap<object, int?>("test", 1), false);
            assertEquals("[test]", cm.Keys.ToString());
            //assertEquals("[1]", cm.Values.ToString()); // TODO: In .NET it would not be possible to make a generic type override the ToString() method to customize it like this without wrapping the result.
            assertEquals("[test=1]", cm.EntrySet().ToString());
            assertEquals("{test=1}", cm.ToString());
            cm.Put("test2", 2);
            assertTrue(cm.Keys.ToString().Contains(", ")); // NOTE: See the note in the KeySet() method as for why this test fails.
            //assertTrue(cm.Values.ToString().Contains(", ")); // TODO: In .NET it would not be possible to make a generic type override the ToString() method to customize it like this without wrapping the result.
            assertTrue(cm.EntrySet().ToString().Contains(", "));
            assertTrue(cm.ToString().Contains(", "));
        }
    }
}