using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Util
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
    public class TestCharArrayMap_ : LuceneTestCase
    {
        public virtual void DoRandom(int iter, bool ignoreCase)
        {
            CharArrayMap<int?> map = new CharArrayMap<int?>(TEST_VERSION_CURRENT, 1, ignoreCase);
            IDictionary<string, int?> hmap = new JCG.Dictionary<string, int?>();

            char[] key;
            for (int i = 0; i < iter; i++)
            {
                int len = Random.Next(5);
                key = new char[len];
                for (int j = 0; j < key.Length; j++)
                {
                    key[j] = (char)Random.Next(127);
                }
                string keyStr = new string(key);
                string hmapKey = ignoreCase ? keyStr.ToLowerInvariant() : keyStr;

                int val = Random.Next();

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
            Dictionary<string, int?> hm = new Dictionary<string, int?>();
            hm["foo"] = 1;
            hm["bar"] = 2;
            cm.PutAll(hm);
            assertEquals(hm.Count, cm.Count);
            hm["baz"] = 3;
            cm.PutAll(hm);
            assertEquals(hm.Count, cm.Count);

            // LUCENENET: Need to cast here - no implicit conversion.
            CharArraySet cs = cm.Keys as CharArraySet;
            int n = 0;
            foreach (string o in cs)
            {
                assertTrue(cm.ContainsKey(o));
                char[] co = o.ToCharArray();
                assertTrue(cm.ContainsKey(co, 0, co.Length));
                n++;
            }
            assertEquals(hm.Count, n);
            assertEquals(hm.Count, cs.Count);
            assertEquals(cm.Count, cs.Count);
            cs.Clear();
            assertEquals(0, cs.Count);
            assertEquals(0, cm.Count);
            try
            {
                cs.Add("test");
                fail("keySet() allows adding new keys");
            }
            catch (NotSupportedException)
            {
                // pass
            }
            cm.PutAll(hm);
            assertEquals(hm.Count, cs.Count);
            assertEquals(cm.Count, cs.Count);
            // LUCENENET: Need to cast here - no implicit conversion
            IEnumerator<KeyValuePair<string, int?>> iter1 = (IEnumerator<KeyValuePair<string, int?>>)cm.EntrySet().GetEnumerator();
            n = 0;
            while (iter1.MoveNext())
            {
                KeyValuePair<string, int?> entry = iter1.Current;
                object key = entry.Key;
                int? val = entry.Value;
                assertEquals(cm.Get(key), val);
                // LUCENENET: Need a cast to get to this method because it is not part of the IEnumerator<T> interface
                ((CharArrayMap<int?>.EntryIterator)iter1).SetValue(val * 100);
                assertEquals(val * 100, (int)cm.Get(key));
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
                var keyc = iter2.Current.Key;
                int? val = iter2.Current.Value;
                assertEquals(hm[keyc], val);
                iter2.SetValue(val * 100);
                assertEquals(val * 100, (int)cm.Get(keyc));
                n++;
            }
            assertEquals(hm.Count, n);

            cm.EntrySet().Clear();
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

            map = CharArrayMap.UnmodifiableMap(map);
            assertEquals("Map size changed due to unmodifiableMap call", size, map.Count);
            var NOT_IN_MAP = "SirGallahad";
            assertFalse("Test String already exists in map", map.ContainsKey(NOT_IN_MAP));
            assertNull("Test String already exists in map", map.Get(NOT_IN_MAP));

            try
            {
                map.Put(NOT_IN_MAP.ToCharArray(), 3);
                fail("Modified unmodifiable map");
            }
            catch (NotSupportedException)
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
            catch (NotSupportedException)
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
            catch (NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            #region Added for better .NET support
            try
            {
                map.Add(NOT_IN_MAP, 3);
                fail("Modified unmodifiable map");
            }
            catch (NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                map.Add(new KeyValuePair<string, int?>(NOT_IN_MAP, 3));
                fail("Modified unmodifiable map");
            }
            catch (NotSupportedException)
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
            catch (NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
#pragma warning disable 612, 618
                map.Remove(new KeyValuePair<string, int?>("foo", 1));
#pragma warning restore 612, 618
                fail("Modified unmodifiable map");
            }
            catch (NotSupportedException)
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
            catch (NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                map.EntrySet().Clear();
                fail("Modified unmodifiable map");
            }
            catch (NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                map.Keys.Clear();
                fail("Modified unmodifiable map");
            }
            catch (NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                map.Put((object)NOT_IN_MAP, 3);
                fail("Modified unmodifiable map");
            }
            catch (NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                map.PutAll(Collections.SingletonMap<string, int?>(NOT_IN_MAP, 3));
                fail("Modified unmodifiable map");
            }
            catch (NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertNull("Test String has been added to unmodifiable map", map.Get(NOT_IN_MAP));
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            assertTrue(map.ContainsKey("foo"));
            assertEquals(1, map.Get("foo"));
            assertTrue(map.ContainsKey("bar"));
            assertEquals(2, map.Get("bar"));
        }

        [Test]
        public virtual void TestToString()
        {
            CharArrayMap<int?> cm = new CharArrayMap<int?>(TEST_VERSION_CURRENT, Collections.SingletonMap<string, int?>("test", 1), false);
            assertEquals("[test]", cm.Keys.ToString());
            assertEquals("[1]", cm.Values.ToString());
            assertEquals("[test=1]", cm.EntrySet().ToString());
            assertEquals("{test=1}", cm.ToString());
            cm.Put("test2", 2);
            assertTrue(cm.Keys.ToString().Contains(", "));
            assertTrue(cm.Values.ToString().Contains(", "));
            assertTrue(cm.EntrySet().ToString().Contains(", "));
            assertTrue(cm.ToString().Contains(", "));
        }
    }
}