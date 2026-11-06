// Lucene version compatibility level 4.8.1
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            CharArrayDictionary<int?> map = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, 1, ignoreCase);
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

                map.Put(key, val, out int? o1);
                var o2 = hmap.Put(hmapKey, val);
                assertEquals(o1, o2);

                // add it again with the string method
                assertEquals(val, map.Put(keyStr, val, out int? previousValue) ? null : previousValue);

                assertEquals(val, map[key.AsSpan(0, key.Length)]); // LUCENENET: Changed Get() to this[]
                assertEquals(val, map[key]); // LUCENENET: Changed Get() to this[]
                assertEquals(val, map[keyStr]); // LUCENENET: Changed Get() to this[]

                assertEquals(hmap.Count, map.size());
            }
        }

        [Test]
        public virtual void TestCharArrayMap()
        {
            int num = 5 * RandomMultiplier;
            for (int i = 0; i < num; i++)
            { // pump this up for more random testing
                DoRandom(1000, false);
                DoRandom(1000, true);
            }
        }

        [Test]
        public virtual void TestMethods()
        {
            CharArrayDictionary<int?> cm = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, 2, false);
            JCG.Dictionary<string, int?> hm = new JCG.Dictionary<string, int?>();
            hm["foo"] = 1;
            hm["bar"] = 2;
            cm.PutAll(hm);
            assertEquals(hm.Count, cm.Count);
            hm["baz"] = 3;
            cm.PutAll(hm);
            assertEquals(hm.Count, cm.Count);

            // LUCENENET: Keys is a read-only ICollection<string> rather than a CharArraySet
            ICollection<string> cs = cm.Keys;
            int n = 0;
            foreach (string o in cs)
            {
                assertTrue(cm.ContainsKey(o));
                char[] co = o.ToCharArray();
                assertTrue(cm.ContainsKey(co.AsSpan(0, co.Length)));
                n++;
            }
            assertEquals(hm.Count, n);
            assertEquals(hm.Count, cs.Count);
            assertEquals(cm.Count, cs.Count);
            // LUCENENET: In Lucene, keySet().clear() cleared the map. KeyCollection is read-only,
            // so clear the dictionary instead and verify the keys reflect the change.
            //cs.Clear();
            cm.Clear();
            assertEquals(0, cs.Count);
            assertEquals(0, cm.Count);
            try
            {
                cs.Add("test");
                fail("keySet() allows adding new keys");
            }
            catch (Exception ue) when (ue.IsUnsupportedOperationException())
            {
                // pass
            }
            try
            {
                cs.Clear();
                fail("Keys allows clearing");
            }
            catch (Exception ue) when (ue.IsUnsupportedOperationException())
            {
                // pass
            }
            cm.PutAll(hm);
            assertEquals(hm.Count, cs.Count);
            assertEquals(cm.Count, cs.Count);
            CharArrayDictionary<int?>.Enumerator iter1 = cm.GetEnumerator();
            n = 0;
            while (iter1.MoveNext())
            {
                KeyValuePair<string, int?> entry = iter1.Current;
                string key = entry.Key;
                int? val = entry.Value;
                assertEquals(cm[key], val); // LUCENENET: Changed Get() to this[]
                cm[key] = val * 100; // LUCENENET: Changed entry.setValue() to this[], which does not invalidate the enumerator for an existing key
                assertEquals(val * 100, (int)cm[key]); // LUCENENET: Changed Get() to this[]
                n++;
            }
            assertEquals(hm.Count, n);
            cm.Clear();
            cm.PutAll(hm);
            assertEquals(cm.size(), n);

            CharArrayDictionary<int?>.Enumerator iter2 = cm.GetEnumerator();
            n = 0;
            while (iter2.MoveNext())
            {
                var keyc = iter2.Current.Key.ToCharArray();
                int? val = iter2.Current.Value;
                assertEquals(hm[new string(keyc)], val);
                cm[keyc] = val * 100; // LUCENENET: Changed iter2.setValue() to this[]
                assertEquals(val * 100, (int)cm[keyc]); // LUCENENET: Changed Get() to this[]
                n++;
            }
            assertEquals(hm.Count, n);

            //cm.EntrySet().Clear(); // LUCENENET: Removed EntrySet() method because .NET uses the dictionary instance
            cm.Clear();
            assertEquals(0, cm.size());
            //assertEquals(0, cm.EntrySet().size()); // LUCENENET: Removed EntrySet() method because .NET uses the dictionary instance
            assertTrue(cm.Count == 0);
        }

        [Test]
        public void TestModifyOnUnmodifiable()
        {
            CharArrayDictionary<int?> map = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, 2, false);
            map.Put("foo", 1, out _);
            map.Put("bar", 2, out _);
            int size = map.Count;
            assertEquals(2, size);
            assertTrue(map.ContainsKey("foo"));
            assertEquals(1, map["foo"]); // LUCENENET: Changed Get() to this[]
            assertTrue(map.ContainsKey("bar"));
            assertEquals(2, map["bar"]); // LUCENENET: Changed Get() to this[]

            map = map.AsReadOnly();
            assertEquals("Map size changed due to unmodifiableMap call", size, map.Count);
            var NOT_IN_MAP = "SirGallahad";
            assertFalse("Test String already exists in map", map.ContainsKey(NOT_IN_MAP));
            assertFalse("Test String already exists in map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()

            try
            {
                map.Put(NOT_IN_MAP.ToCharArray(), 3, out _);
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertFalse("Test String has been added to unmodifiable map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                map.Put(NOT_IN_MAP, 3, out _);
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertFalse("Test String has been added to unmodifiable map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            // LUCENENET: StringBuilder (CharSequence) overloads have been removed. A span over a
            // char[] is used to cover the ReadOnlySpan<char> overload instead.
            try
            {
                map.Put(NOT_IN_MAP.ToCharArray().AsSpan(), 3, out _);
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertFalse("Test String has been added to unmodifiable map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            #region LUCENENET Added for better .NET support
            try
            {
                map.Add(NOT_IN_MAP, 3);
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertFalse("Test String has been added to unmodifiable map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                ((IDictionary<string, int?>)map).Add(new KeyValuePair<string, int?>(NOT_IN_MAP, 3));
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertFalse("Test String has been added to unmodifiable map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                map[NOT_IN_MAP.ToCharArray().AsSpan()] = 3;
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertFalse("Test String has been added to unmodifiable map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }

            try
            {
                ((IDictionary<string, int?>)map).Remove(new KeyValuePair<string, int?>("foo", 1));
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.Count);
            }
            #endregion LUCENENET Added for better .NET support

            try
            {
                map.Clear();
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                //map.EntrySet().Clear(); // LUCENENET: Removed EntrySet() method because .NET uses the dictionary instance
                map.Clear();
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                ((ICollection<string>)map.Keys).Clear();
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            // LUCENENET: The Put((object)NOT_IN_MAP, ...) case was removed along with the object overloads

            try
            {
                map.PutAll(Collections.SingletonMap<string, int?>(NOT_IN_MAP, 3));
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertFalse("Test String has been added to unmodifiable map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            assertTrue(map.ContainsKey("foo"));
            assertEquals(1, map["foo"]); // LUCENENET: Changed Get() to this[]
            assertTrue(map.ContainsKey("bar"));
            assertEquals(2, map["bar"]); // LUCENENET: Changed Get() to this[]
        }

        [Test]
        public virtual void TestToString()
        {
            CharArrayDictionary<int?> cm = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, Collections.SingletonMap<string, int?>("test", 1), false);
            assertEquals("[test]", cm.Keys.ToString());
            assertEquals("[1]", cm.Values.ToString());
            //assertEquals("[test=1]", cm.EntrySet().ToString()); // LUCENENET: Removed EntrySet() method because .NET uses the dictionary instance
            assertEquals("{test=1}", cm.ToString());
            cm["test2"] = 2; // LUCENENET: Changed Put() to this[]
            assertTrue(cm.Keys.ToString().Contains(", "));
            assertTrue(cm.Values.ToString().Contains(", "));
            //assertTrue(cm.EntrySet().ToString().Contains(", ")); // LUCENENET: Removed EntrySet() method because .NET uses the dictionary instance
            assertTrue(cm.ToString().Contains(", "));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsReadOnly()
        {
            CharArrayDictionary<int?> target = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, Collections.SingletonMap<string, int?>("test", 1), false);
            CharArrayDictionary<int?> readOnlyTarget = target.AsReadOnly();

            Assert.IsFalse(target.IsReadOnly);
            Assert.IsTrue(((ICollection<string>)target.Keys).IsReadOnly); // KeyCollection is always read-only
            Assert.IsTrue(readOnlyTarget.IsReadOnly);
            Assert.IsTrue(((ICollection<string>)readOnlyTarget.Keys).IsReadOnly);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestEnumeratorExceptions()
        {
            CharArrayDictionary<int?> map = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, 3, ignoreCase: false)
            {
                ["foo"] = 0,
                ["bar"] = 0,
                ["baz"] = 0,
            };

            // Checks to ensure the non-generic Current property throws when outside of the enumeration
            using (IEnumerator<KeyValuePair<string, int?>> iter = map.GetEnumerator())
            {
                Assert.Throws<InvalidOperationException>(() => { var _ = ((IEnumerator)iter).Current; });

                while (iter.MoveNext())
                {
                    Assert.DoesNotThrow(() => { var _ = ((IEnumerator)iter).Current; });
                }

                Assert.Throws<InvalidOperationException>(() => { var _ = ((IEnumerator)iter).Current; });
            }

            // Replacing the value of an existing key does not invalidate enumerators
            using (IEnumerator<KeyValuePair<string, int?>> ours = map.GetEnumerator())
            {
                using IEnumerator<KeyValuePair<string, int?>> theirs = map.GetEnumerator();

                Assert.IsTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                map[ours.Current.Key] = 1;
                Assert.DoesNotThrow(() => theirs.MoveNext());
                Assert.DoesNotThrow(() => ours.MoveNext());
            }

            // Adding a key invalidates all enumerators
            using (IEnumerator<KeyValuePair<string, int?>> ours = map.GetEnumerator())
            {
                using IEnumerator<KeyValuePair<string, int?>> theirs = map.GetEnumerator();

                Assert.IsTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => { map["qux"] = 2; });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());
            }

            // Clearing does not invalidate enumerators (consistent with Dictionary<TKey, TValue> in .NET Core 3.0+),
            // they simply have no more elements
            using (IEnumerator<KeyValuePair<string, int?>> ours = map.GetEnumerator())
            {
                using IEnumerator<KeyValuePair<string, int?>> theirs = map.GetEnumerator();

                Assert.IsTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => { map.Clear(); });
                Assert.IsFalse(theirs.MoveNext());
                Assert.IsFalse(ours.MoveNext());
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestKeyCollectionEnumeratorExceptions()
        {
            var map = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, 3, ignoreCase: false)
            {
                ["foo"] = 0,
                ["bar"] = 0,
                ["baz"] = 0,
            };

            // Checks to ensure the non-generic Current property throws when outside of the enumeration
            using (IEnumerator<string> iter = map.Keys.GetEnumerator())
            {
                Assert.Throws<InvalidOperationException>(() => { var _ = ((IEnumerator)iter).Current; });

                while (iter.MoveNext())
                {
                    Assert.DoesNotThrow(() => { var _ = ((IEnumerator)iter).Current; });
                }

                Assert.Throws<InvalidOperationException>(() => { var _ = ((IEnumerator)iter).Current; });
            }

            using (IEnumerator<string> ours = map.Keys.GetEnumerator())
            {
                using IEnumerator<string> theirs = map.Keys.GetEnumerator();

                Assert.IsTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map["qux"] = 2; });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());
            }

            // Clearing does not invalidate enumerators (consistent with Dictionary<TKey, TValue> in .NET Core 3.0+),
            // they simply have no more elements
            using (IEnumerator<string> ours = map.Keys.GetEnumerator())
            {
                using IEnumerator<string> theirs = map.Keys.GetEnumerator();

                Assert.IsTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map.Clear(); });
                Assert.IsFalse(theirs.MoveNext());
                Assert.IsFalse(ours.MoveNext());
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestValueCollectionEnumeratorExceptions()
        {
            var map = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, 3, ignoreCase: false)
            {
                ["foo"] = 0,
                ["bar"] = 0,
                ["baz"] = 0,
            };

            // Checks to ensure the non-generic Current property throws when outside of the enumeration
            using (IEnumerator<int?> iter = map.Values.GetEnumerator())
            {
                Assert.Throws<InvalidOperationException>(() => { var _ = ((IEnumerator)iter).Current; });

                while (iter.MoveNext())
                {
                    Assert.DoesNotThrow(() => { var _ = ((IEnumerator)iter).Current; });
                }

                Assert.Throws<InvalidOperationException>(() => { var _ = ((IEnumerator)iter).Current; });
            }

            using (IEnumerator<int?> ours = map.Values.GetEnumerator())
            {
                using IEnumerator<int?> theirs = map.Values.GetEnumerator();

                Assert.IsTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map["qux"] = 2; });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());
            }

            // Clearing does not invalidate enumerators (consistent with Dictionary<TKey, TValue> in .NET Core 3.0+),
            // they simply have no more elements
            using (IEnumerator<int?> ours = map.Values.GetEnumerator())
            {
                using IEnumerator<int?> theirs = map.Values.GetEnumerator();

                Assert.IsTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map.Clear(); });
                Assert.IsFalse(theirs.MoveNext());
                Assert.IsFalse(ours.MoveNext());
            }
        }

        private class KvpStringEqualityComparer : IEqualityComparer<KeyValuePair<string, int?>>
        {
            public static readonly KvpStringEqualityComparer Instance = new KvpStringEqualityComparer();

            public bool Equals(KeyValuePair<string, int?> x, KeyValuePair<string, int?> y)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(x.Key, y.Key);
            }
            public int GetHashCode([DisallowNull] KeyValuePair<string, int?> obj)
            {
                return obj.Key is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key);
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestCopyTo_String_Int32()
        {
            var stopwords = new JCG.HashSet<KeyValuePair<string, int?>>(
                TestCharArraySet.TEST_STOP_WORDS.Select(x => new KeyValuePair<string, int?>(key: x, value: 0)),
                KvpStringEqualityComparer.Instance);
            var target = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, stopwords.Count, ignoreCase: false);
            foreach (var kvp in stopwords)
                target.Put(kvp.Key, kvp.Value, out _);

            // Full array
            var array1 = new KeyValuePair<string, int?>[target.Count];
            ((ICollection<KeyValuePair<string, int?>>)target).CopyTo(array1, 0);
            assertTrue(stopwords.SetEquals(array1));

            // Bounded to lower start index
            int startIndex = 3;
            var array2 = new KeyValuePair<string, int?>[target.Count + startIndex];
            ((ICollection<KeyValuePair<string, int?>>)target).CopyTo(array2, startIndex);

            assertEquals(default, array2[0]);
            assertEquals(default, array2[1]);
            assertEquals(default, array2[2]);
            assertTrue(stopwords.IsProperSubsetOf(array2));
            assertTrue(stopwords.SetEquals(array2.Skip(startIndex).ToArray()));
        }

        // LUCENENET: TestCopyTo_CharArray_Int32 and TestCopyTo_ICharSequence_Int32 were removed along with
        // the internal CopyTo(KeyValuePair<char[], TValue>[]) and CopyTo(KeyValuePair<ICharSequence, TValue>[]) overloads.

        [Test, LuceneNetSpecific]
        public virtual void TestSpanOverloadsMatchStringOverloads()
        {
            var map = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, ignoreCase: true);
            char[] buffer = "xxHelloxx".ToCharArray();
            ReadOnlySpan<char> hello = buffer.AsSpan(2, 5);

            map.Add(hello, 1);
            Assert.IsTrue(map.ContainsKey("hello"));
            Assert.IsTrue(map.ContainsKey(hello));
            Assert.AreEqual(1, map[hello]);
            Assert.IsTrue(map.TryGetValue(hello, out int value));
            Assert.AreEqual(1, value);

            Assert.IsFalse(map.Put(hello, 2, out int previous));
            Assert.AreEqual(1, previous);
            map[hello] = 3;
            Assert.AreEqual(3, map["HELLO"]);
            Assert.AreEqual(1, map.Count);

            // The stored key is the lowercased copy, not a view over the buffer
            buffer[2] = 'J';
            Assert.IsTrue(map.ContainsKey("hello"));
            Assert.AreEqual("hello", map.Keys.Single());

            Assert.Throws<ArgumentException>(() => map.Add("HELLO".AsSpan(), 4));
        }
    }
}
