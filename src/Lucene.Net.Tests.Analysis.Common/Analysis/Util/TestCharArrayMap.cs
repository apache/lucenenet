// Lucene version compatibility level 4.8.1
using J2N.Collections;
using J2N.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

                assertEquals(val, map[key, 0, key.Length]); // LUCENENET: Changed Get() to this[]
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
            Dictionary<string, int?> hm = new Dictionary<string, int?>();
            hm["foo"] = 1;
            hm["bar"] = 2;
            cm.PutAll(hm);
            assertEquals(hm.Count, cm.Count);
            hm["baz"] = 3;
            cm.PutAll(hm);
            assertEquals(hm.Count, cm.Count);

            CharArraySet cs = cm.Keys;
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
                object key = entry.Key;
                int? val = entry.Value;
                assertEquals(cm[key], val); // LUCENENET: Changed Get() to this[]
                iter1.SetValue(val * 100);
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
                var keyc = iter2.Current.Key;
                int? val = iter2.Current.Value;
                assertEquals(hm[keyc], val);
                iter2.SetValue(val * 100);
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

            try
            {
                map.Put(new StringBuilder(NOT_IN_MAP), 3, out _);
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
                map[new StringBuilder(NOT_IN_MAP)] = 3;
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
                map.Keys.Clear();
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

            try
            {
                map.Put((object)NOT_IN_MAP, 3, out _);
                fail("Modified unmodifiable map");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable map", map.ContainsKey(NOT_IN_MAP));
                assertFalse("Test String has been added to unmodifiable map", map.TryGetValue(NOT_IN_MAP, out int? _)); // LUCENENET: Changed Get() to TryGetValue()
                assertEquals("Size of unmodifiable map has changed", size, map.size());
            }

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

            assertFalse(target.IsReadOnly);
            assertTrue(target.Keys.IsReadOnly); // KeyCollection is always read-only
            assertTrue(readOnlyTarget.IsReadOnly);
            assertTrue(readOnlyTarget.Keys.IsReadOnly);
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

            // Checks to ensure our Current property throws when outside of the enumeration
            using (var iter = map.GetEnumerator())
            {
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.Current; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentKey; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentKeyCharSequence; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentKeyString; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentValue; });

                while (iter.MoveNext())
                {
                    Assert.DoesNotThrow(() => { var _ = iter.Current; });
                    Assert.DoesNotThrow(() => { var _ = iter.CurrentKey; });
                    Assert.DoesNotThrow(() => { var _ = iter.CurrentKeyCharSequence; });
                    Assert.DoesNotThrow(() => { var _ = iter.CurrentKeyString; });
                    Assert.DoesNotThrow(() => { var _ = iter.CurrentValue; });
                }

                Assert.Throws<InvalidOperationException>(() => { var _ = iter.Current; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentKey; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentKeyCharSequence; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentKeyString; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentValue; });
            }

            using (var ours = map.GetEnumerator())
            {
                using var theirs = map.GetEnumerator();

                assertTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                assertTrue(ours.MoveNext());
                ours.SetValue(1);
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => ours.SetValue(1));
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
            }

            using (var ours = map.GetEnumerator())
            {
                using var theirs = map.GetEnumerator();

                assertTrue(ours.MoveNext());
                ours.SetValue(1);
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map["baz"] = 2; });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());
            }

            using (var ours = map.GetEnumerator())
            {
                using var theirs = map.GetEnumerator();

                assertTrue(ours.MoveNext());
                ours.SetValue(1);
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map.Clear(); });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());

                Assert.Throws<InvalidOperationException>(() => { var _ = ours.Current; });
                Assert.Throws<InvalidOperationException>(() => { var _ = ours.CurrentKey; });
                Assert.Throws<InvalidOperationException>(() => { var _ = ours.CurrentKeyCharSequence; });
                Assert.Throws<InvalidOperationException>(() => { var _ = ours.CurrentKeyString; });
                Assert.Throws<InvalidOperationException>(() => { var _ = ours.CurrentValue; });
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


            // Checks to ensure our Current property throws when outside of the enumeration
            using (var iter = map.Keys.GetEnumerator())
            {
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.Current; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentValue; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentValueCharSequence; });

                while (iter.MoveNext())
                {
                    Assert.DoesNotThrow(() => { var _ = iter.Current; });
                    Assert.DoesNotThrow(() => { var _ = iter.CurrentValue; });
                    Assert.DoesNotThrow(() => { var _ = iter.CurrentValueCharSequence; });
                }

                Assert.Throws<InvalidOperationException>(() => { var _ = iter.Current; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentValue; });
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.CurrentValueCharSequence; });
            }

            using (var ours = map.Keys.GetEnumerator())
            {
                using var theirs = map.Keys.GetEnumerator();

                assertTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map["baz"] = 2; });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());
            }

            using (var ours = map.Keys.GetEnumerator())
            {
                using var theirs = map.Keys.GetEnumerator();

                assertTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map.Clear(); });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());

                Assert.Throws<InvalidOperationException>(() => { var _ = ours.Current; });
                Assert.Throws<InvalidOperationException>(() => { var _ = ours.CurrentValue; });
                Assert.Throws<InvalidOperationException>(() => { var _ = ours.CurrentValueCharSequence; });
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


            // Checks to ensure our Current property throws when outside of the enumeration
            using (var iter = map.Values.GetEnumerator())
            {
                Assert.Throws<InvalidOperationException>(() => { var _ = iter.Current; });

                while (iter.MoveNext())
                {
                    Assert.DoesNotThrow(() => { var _ = iter.Current; });
                }

                Assert.Throws<InvalidOperationException>(() => { var _ = iter.Current; });
            }

            using (var ours = map.Values.GetEnumerator())
            {
                using var theirs = map.Values.GetEnumerator();

                assertTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map["baz"] = 2; });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());
            }

            using (var ours = map.Values.GetEnumerator())
            {
                using var theirs = map.Values.GetEnumerator();

                assertTrue(ours.MoveNext());
                Assert.DoesNotThrow(() => theirs.MoveNext());

                Assert.DoesNotThrow(() => ours.MoveNext());
                Assert.DoesNotThrow(() => { map.Clear(); });
                Assert.Throws<InvalidOperationException>(() => theirs.MoveNext());
                Assert.Throws<InvalidOperationException>(() => ours.MoveNext());

                Assert.Throws<InvalidOperationException>(() => { var _ = ours.Current; });
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

        private class KvpCharArrayEqualityComparer : IEqualityComparer<KeyValuePair<char[], int?>>
        {
            public static readonly KvpCharArrayEqualityComparer Instance = new KvpCharArrayEqualityComparer();

            public bool Equals(KeyValuePair<char[], int?> x, KeyValuePair<char[], int?> y)
            {
                return ArrayEqualityComparer<char>.OneDimensional.Equals(x.Key, y.Key);
            }
            public int GetHashCode([DisallowNull] KeyValuePair<char[], int?> obj)
            {
                return ArrayEqualityComparer<char>.OneDimensional.GetHashCode(obj.Key);
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestCopyTo_CharArray_Int32()
        {
            var stopwords = new JCG.HashSet<KeyValuePair<char[], int?>>(
                TestCharArraySet.TEST_STOP_WORDS.Select(x => new KeyValuePair<char[], int?>(key: x.ToCharArray(), value: 0)),
                KvpCharArrayEqualityComparer.Instance);
            var target = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, stopwords.Count, ignoreCase: false);
            foreach (var kvp in stopwords)
                target.Put(kvp.Key, kvp.Value, out _);

            // Full array
            var array1 = new KeyValuePair<char[], int?>[target.Count];
            target.CopyTo(array1, 0);
            assertTrue(stopwords.SetEquals(array1));

            // Bounded to lower start index
            int startIndex = 3;
            var array2 = new KeyValuePair<char[], int?>[target.Count + startIndex];
            target.CopyTo(array2, startIndex);

            assertEquals(default, array2[0]);
            assertEquals(default, array2[1]);
            assertEquals(default, array2[2]);
            assertTrue(stopwords.IsProperSubsetOf(array2));
            assertTrue(stopwords.SetEquals(array2.Skip(startIndex).ToArray()));
        }

        private class KvpCharSequenceEqualityComparer : IEqualityComparer<KeyValuePair<ICharSequence, int?>>
        {
            public static readonly KvpCharSequenceEqualityComparer Instance = new KvpCharSequenceEqualityComparer();

            public bool Equals(KeyValuePair<ICharSequence, int?> x, KeyValuePair<ICharSequence, int?> y)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(x.Key.ToString(), y.Key.ToString());
            }
            public int GetHashCode([DisallowNull] KeyValuePair<ICharSequence, int?> obj)
            {
                return obj.Key is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key.ToString());
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestCopyTo_ICharSequence_Int32()
        {
            var stopwords = new JCG.HashSet<KeyValuePair<ICharSequence, int?>>(
                TestCharArraySet.TEST_STOP_WORDS.Select(x => new KeyValuePair<ICharSequence, int?>(key: new StringCharSequence(x), value: 0)),
                KvpCharSequenceEqualityComparer.Instance);
            var target = new CharArrayDictionary<int?>(TEST_VERSION_CURRENT, stopwords.Count, ignoreCase: false);
            foreach (var kvp in stopwords)
                target.Put(kvp.Key, kvp.Value, out _);

            // Full array
            var array1 = new KeyValuePair<ICharSequence, int?>[target.Count];
            target.CopyTo(array1, 0);
            assertTrue(stopwords.SetEquals(array1));

            // Bounded to lower start index
            int startIndex = 3;
            var array2 = new KeyValuePair<ICharSequence, int?>[target.Count + startIndex];
            target.CopyTo(array2, startIndex);

            assertEquals(default, array2[0]);
            assertEquals(default, array2[1]);
            assertEquals(default, array2[2]);
            assertTrue(stopwords.IsProperSubsetOf(array2));
            assertTrue(stopwords.SetEquals(array2.Skip(startIndex).ToArray()));
        }
    }
}