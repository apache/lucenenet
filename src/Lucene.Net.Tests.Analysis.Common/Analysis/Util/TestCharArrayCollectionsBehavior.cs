using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
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

    /// <summary>
    /// Behavioral tests for <see cref="CharArraySet"/> and <see cref="CharArrayDictionary{TValue}"/>
    /// that pin down case folding, stored key form, copy, read-only and null semantics.
    /// <para/>
    /// These tests only use APIs that are common to the original open-addressing implementation
    /// and the J2N-backed implementation, so they guard against behavioral regressions
    /// when the backing collections are swapped (see #1478).
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestCharArrayCollectionsBehavior : LuceneTestCase
    {
#pragma warning disable 612, 618
        private const LuceneVersion Lucene30 = LuceneVersion.LUCENE_30;
#pragma warning restore 612, 618

        // Pairs of (added, probe, expectedMatchWhenIgnoreCase). The expectation follows
        // invariant lowercasing per code point (Java's Character.toLowerCase), which is NOT the
        // same as StringComparison.OrdinalIgnoreCase (which folds to uppercase).
        private static readonly object[] CaseFoldingCases =
        {
            new object[] { "ABC", "abc", true },
            new object[] { "aBc", "AbC", true },
            new object[] { "ÀÉÎ", "àéî", true }, // ÀÉÎ / àéî
            new object[] { "ЖИВ", "жив", true }, // Cyrillic ЖИВ / жив
            new object[] { "ΣΟΦ", "σοφ", true }, // Greek ΣΟΦ / σοφ
            new object[] { "K", "k", true }, // KELVIN SIGN lowercases to 'k'
            new object[] { "K", "K", true },
            new object[] { "Ω", "ω", true }, // OHM SIGN lowercases to omega
            new object[] { "Å", "å", true }, // ANGSTROM SIGN lowercases to å
            new object[] { "𐐀", "𐐨", true }, // DESERET CAPITAL/SMALL LONG I (supplementary)
            new object[] { "x𐐀y", "X𐐨Y", true },
            new object[] { "a\uD800b", "A\uD800B", true }, // lone high surrogate
            new object[] { "\uDC00X", "\uDC00x", true }, // lone low surrogate
            new object[] { "ı", "i", false }, // dotless i does not lowercase-match i
            new object[] { "ı", "I", false },
            new object[] { "ſ", "s", false }, // long s does not lowercase-match s
            new object[] { "ς", "σ", false }, // final sigma does not lowercase-match sigma
            new object[] { "abc", "abcd", false },
            new object[] { "abc", "ab", false },
        };

        private static CharArraySet CopySet(LuceneVersion matchVersion, IEnumerable<string> source)
            => new CharArraySet(matchVersion, source);

        private static CharArrayDictionary<TValue> CopyDictionary<TValue>(LuceneVersion matchVersion, IDictionary<string, TValue> source)
            => new CharArrayDictionary<TValue>(matchVersion, source);

        /// <summary>
        /// Reference fold that matches the expected semantics for the given version.
        /// </summary>
        private static string Fold(string s, LuceneVersion matchVersion)
        {
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                return s.ToLowerInvariant();
            }
            // Java 4 semantics: each char is lowercased on its own, so surrogate pairs are left alone
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                sb.Append(char.IsSurrogate(c) ? c : char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        [Test]
        [TestCaseSource(nameof(CaseFoldingCases))]
        public void TestCaseFoldingFollowsInvariantLowercase(string added, string probe, bool expectedIgnoreCaseMatch)
        {
            // Sanity check: the expectation must agree with the reference model on this platform
            assertEquals(expectedIgnoreCaseMatch, Fold(added, TEST_VERSION_CURRENT) == Fold(probe, TEST_VERSION_CURRENT));

            foreach (bool ignoreCase in new[] { true, false })
            {
                bool expected = ignoreCase ? expectedIgnoreCaseMatch : added == probe;
                string because = $"added={Escape(added)}, probe={Escape(probe)}, ignoreCase={ignoreCase}";

                // Add via string, probe both ways
                var set = new CharArraySet(TEST_VERSION_CURRENT, 4, ignoreCase);
                assertTrue(set.Add(added));
                assertEquals(because, expected, set.Contains(probe));
                assertEquals(because, expected, set.Contains(probe.ToCharArray()));

                // Add via char[], probe both ways
                set = new CharArraySet(TEST_VERSION_CURRENT, 4, ignoreCase);
                assertTrue(set.Add(added.ToCharArray()));
                assertEquals(because, expected, set.Contains(probe));
                assertEquals(because, expected, set.Contains(probe.ToCharArray()));

                // Adding the probe reports whether it was a new element
                assertEquals(because, !expected, set.Add(probe));
                assertEquals(because, expected ? 1 : 2, set.Count);

                var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, ignoreCase);
                dict.Add(added, 42);
                assertEquals(because, expected, dict.ContainsKey(probe));
                assertEquals(because, expected, dict.ContainsKey(probe.ToCharArray()));
                assertEquals(because, expected, dict.TryGetValue(probe, out int value));
                assertEquals(because, expected ? 42 : 0, value);
                assertEquals(because, expected, dict.TryGetValue(probe.ToCharArray(), out value));
                assertEquals(because, expected ? 42 : 0, value);

                dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, ignoreCase);
                dict.Add(added.ToCharArray(), 7);
                assertEquals(because, expected, dict.ContainsKey(probe));
                if (expected)
                {
                    assertEquals(because, 7, dict[probe]);
                    assertEquals(because, 7, dict[probe.ToCharArray()]);
                }
            }
        }

        [Test]
        [TestCaseSource(nameof(CaseFoldingCases))]
        public void TestCaseFoldingIsSymmetric(string added, string probe, bool expectedIgnoreCaseMatch)
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 4, true);
            set.Add(probe);
            assertEquals(expectedIgnoreCaseMatch, set.Contains(added));

            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            dict.Add(probe, 1);
            assertEquals(expectedIgnoreCaseMatch, dict.ContainsKey(added));
        }

        [Test]
        public void TestLucene30SupplementaryCharsAreNotCaseFolded()
        {
            var set = new CharArraySet(Lucene30, 4, true);
            set.Add("x𐐀y");
            assertTrue(set.Contains("X𐐀Y"));
            assertFalse(set.Contains("x𐐨y"));
            assertTrue(set.Contains("x𐐀y".ToCharArray()));

            var dict = new CharArrayDictionary<int>(Lucene30, 4, true);
            dict.Add("x𐐀y", 1);
            assertTrue(dict.ContainsKey("X𐐀Y"));
            assertFalse(dict.ContainsKey("x𐐨y"));

            // BMP chars are still folded
            set.Add("ÀBC");
            assertTrue(set.Contains("àbc"));
        }

        [Test]
        [Repeat(3)]
        public void TestRandomizedSetMatchesReferenceModel()
        {
            foreach (LuceneVersion version in new[] { TEST_VERSION_CURRENT, Lucene30 })
            {
                foreach (bool ignoreCase in new[] { true, false })
                {
                    var set = new CharArraySet(version, Random.Next(16), ignoreCase);
                    var model = new JCG.HashSet<string>(StringComparer.Ordinal);
                    int iterations = AtLeast(500);
                    for (int i = 0; i < iterations; i++)
                    {
                        string s = RandomFoldingString(Random);
                        string key = ignoreCase ? Fold(s, version) : s;
                        string because = $"s={Escape(s)}, version={version}, ignoreCase={ignoreCase}";
                        if (Random.NextBoolean())
                        {
                            bool expectedAdded = model.Add(key);
                            bool added = Random.NextBoolean() ? set.Add(s) : set.Add(s.ToCharArray());
                            assertEquals(because, expectedAdded, added);
                        }
                        else
                        {
                            bool expected = model.Contains(key);
                            assertEquals(because, expected, set.Contains(s));
                            assertEquals(because, expected, set.Contains(s.ToCharArray()));
                        }
                        assertEquals(because, model.Count, set.Count);
                    }

                    // Every model element is found, and the set enumerates exactly the model
                    foreach (string key in model)
                    {
                        assertTrue(set.Contains(key));
                    }
                    var enumerated = new JCG.HashSet<string>(StringComparer.Ordinal);
                    foreach (string s in set)
                    {
                        enumerated.Add(s);
                    }
                    assertTrue(model.SetEquals(enumerated));
                }
            }
        }

        [Test]
        [Repeat(3)]
        public void TestRandomizedDictionaryMatchesReferenceModel()
        {
            foreach (LuceneVersion version in new[] { TEST_VERSION_CURRENT, Lucene30 })
            {
                foreach (bool ignoreCase in new[] { true, false })
                {
                    var dict = new CharArrayDictionary<int>(version, Random.Next(16), ignoreCase);
                    var model = new JCG.Dictionary<string, int>(StringComparer.Ordinal);
                    int iterations = AtLeast(500);
                    for (int i = 0; i < iterations; i++)
                    {
                        string s = RandomFoldingString(Random);
                        string key = ignoreCase ? Fold(s, version) : s;
                        string because = $"s={Escape(s)}, version={version}, ignoreCase={ignoreCase}";
                        switch (Random.Next(3))
                        {
                            case 0:
                                {
                                    int value = Random.Next();
                                    bool expectedNew = !model.TryGetValue(key, out int expectedPrevious);
                                    model[key] = value;
                                    bool isNew = Random.NextBoolean()
                                        ? dict.Put(s, value, out int previous)
                                        : dict.Put(s.ToCharArray(), value, out previous);
                                    assertEquals(because, expectedNew, isNew);
                                    if (!expectedNew)
                                    {
                                        assertEquals(because, expectedPrevious, previous);
                                    }
                                    break;
                                }
                            case 1:
                                {
                                    int value = Random.Next();
                                    model[key] = value;
                                    if (Random.NextBoolean())
                                        dict[s] = value;
                                    else
                                        dict[s.ToCharArray()] = value;
                                    break;
                                }
                            default:
                                {
                                    bool expected = model.TryGetValue(key, out int expectedValue);
                                    assertEquals(because, expected, dict.ContainsKey(s));
                                    assertEquals(because, expected, dict.ContainsKey(s.ToCharArray()));
                                    assertEquals(because, expected, dict.TryGetValue(s, out int actual));
                                    if (expected)
                                    {
                                        assertEquals(because, expectedValue, actual);
                                        assertEquals(because, expectedValue, dict[s]);
                                        assertEquals(because, expectedValue, dict[s.ToCharArray()]);
                                    }
                                    assertEquals(because, expected, dict.TryGetValue(s.ToCharArray(), out actual));
                                    break;
                                }
                        }
                        assertEquals(because, model.Count, dict.Count);
                    }

                    var enumerated = new JCG.Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var pair in dict)
                    {
                        enumerated.Add(pair.Key, pair.Value);
                    }
                    assertEquals(model.Count, enumerated.Count);
                    foreach (var pair in model)
                    {
                        assertTrue(enumerated.TryGetValue(pair.Key, out int value));
                        assertEquals(pair.Value, value);
                    }
                }
            }
        }

        [Test]
        public void TestIgnoreCaseSetStoresLowercasedKeys()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 4, true);
            set.Add("HeLLo");
            set.Add("WORLD".ToCharArray());
            set.Add("K"); // KELVIN SIGN

            var expected = new JCG.HashSet<string> { "hello", "world", "k" };
            assertTrue(expected.SetEquals(set.ToArray()));

            var array = new string[3];
            set.CopyTo(array, 0);
            assertTrue(expected.SetEquals(array));

            string str = set.ToString();
            assertTrue(str, str.Contains("hello"));
            assertFalse(str, str.Contains("HeLLo"));

            assertTrue(set.Equals(expected));
            assertTrue(expected.Equals(set));
            assertEquals(expected.GetHashCode(), set.GetHashCode());
        }

        [Test]
        public void TestCaseSensitiveSetStoresKeysAsGiven()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 4, false);
            set.Add("HeLLo");
            set.Add("WORLD".ToCharArray());

            var expected = new JCG.HashSet<string> { "HeLLo", "WORLD" };
            assertTrue(expected.SetEquals(set.ToArray()));
            assertTrue(set.Equals(expected));
            assertEquals(expected.GetHashCode(), set.GetHashCode());
        }

        [Test]
        public void TestIgnoreCaseDictionaryStoresLowercasedKeys()
        {
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            dict.Add("HeLLo", 1);
            dict.Put("WORLD".ToCharArray(), 2, out _);
            dict["FoO"] = 3;

            var keys = new JCG.HashSet<string>(dict.Select(p => p.Key));
            assertTrue(keys.SetEquals(new[] { "hello", "world", "foo" }));

            var keyCollectionKeys = new JCG.HashSet<string>(dict.Keys);
            assertTrue(keyCollectionKeys.SetEquals(new[] { "hello", "world", "foo" }));

            string str = dict.ToString();
            assertTrue(str, str.Contains("hello=1"));
            assertFalse(str, str.Contains("HeLLo"));

            // Updating through a differently-cased key keeps the single lowercased entry
            dict["HELLO"] = 10;
            assertEquals(3, dict.Count);
            assertEquals(10, dict["hello"]);
        }

        [Test]
        public void TestDictionaryEqualsMatchesStructuralDictionary()
        {
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            dict.Add("One", 1);
            dict.Add("Two", 2);

            var expected = new JCG.Dictionary<string, int> { { "one", 1 }, { "two", 2 } };
            assertTrue(dict.Equals(expected));

            var different = new JCG.Dictionary<string, int> { { "one", 1 }, { "two", 3 } };
            assertFalse(dict.Equals(different));
        }

        [Test]
        public void TestDictionaryHashCodeMatchesStructuralDictionary()
        {
            var small = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 1, false);
            var large = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 1000, false);
            var expected = new JCG.Dictionary<string, int>();
            for (int i = 0; i < 50; i++)
            {
                string key = "key" + i;
                small.Add(key, i);
                large.Add(key, i);
                expected.Add(key, i);
            }
            assertEquals(expected.GetHashCode(), small.GetHashCode());
            assertEquals(small.GetHashCode(), large.GetHashCode());
        }

        [Test]
        public void TestCopyOfIgnoreCaseSetIsIgnoreCaseAndIndependent()
        {
            var source = new CharArraySet(TEST_VERSION_CURRENT, 4, true);
            source.Add("Hello");

            CharArraySet copy = CopySet(TEST_VERSION_CURRENT, source);
            assertTrue(copy.Contains("HELLO"));
            assertTrue(copy.Contains("hello"));
            assertTrue(copy.Equals(source));

            copy.Add("World");
            assertFalse(source.Contains("world"));
            source.Add("Other");
            assertFalse(copy.Contains("other"));
        }

        [Test]
        public void TestCopyOfCaseSensitiveCharArraySetIsCaseSensitive()
        {
            var source = new CharArraySet(TEST_VERSION_CURRENT, 4, false);
            source.Add("Hello");

            CharArraySet copy = CopySet(TEST_VERSION_CURRENT, source);
            assertTrue(copy.Contains("Hello"));
            assertFalse(copy.Contains("hello"));
        }

        [Test]
        public void TestCopyOfNonCharArraySetIsCaseSensitive()
        {
            var source = new JCG.HashSet<string> { "Hello" };

            CharArraySet copy = CopySet(TEST_VERSION_CURRENT, source);
            assertTrue(copy.Contains("Hello"));
            assertFalse(copy.Contains("hello"));
            assertFalse(copy.IsReadOnly);
            assertTrue(copy.Add("World"));
            assertFalse(source.Contains("World"));
        }

        [Test]
        public void TestCopyOfEmptySetIsEmpty()
        {
            CharArraySet copy = CopySet(TEST_VERSION_CURRENT, CharArraySet.Empty);
            assertEquals(0, copy.Count);
            assertFalse(copy.Contains("anything"));
        }

        [Test]
        public void TestCopyOfLucene30SetKeepsLucene30Folding()
        {
            var source = new CharArraySet(Lucene30, 4, true);
            source.Add("x𐐀y");

            CharArraySet copy = CopySet(TEST_VERSION_CURRENT, source);
            assertTrue(copy.Contains("X𐐀Y"));
            assertFalse(copy.Contains("x𐐨y"));
        }

        [Test]
        public void TestCopyOfIgnoreCaseDictionaryIsIgnoreCaseAndIndependent()
        {
            var source = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            source.Add("Hello", 1);

            CharArrayDictionary<int> copy = CopyDictionary(TEST_VERSION_CURRENT, source);
            assertEquals(1, copy["HELLO"]);

            copy["World"] = 2;
            assertFalse(source.ContainsKey("world"));
            copy["hello"] = 5;
            assertEquals(1, source["hello"]);
        }

        [Test]
        public void TestCopyOfNonCharArrayDictionaryIsCaseSensitive()
        {
            var source = new JCG.Dictionary<string, int> { { "Hello", 1 } };

            CharArrayDictionary<int> copy = CopyDictionary(TEST_VERSION_CURRENT, source);
            assertTrue(copy.ContainsKey("Hello"));
            assertFalse(copy.ContainsKey("hello"));
        }

        [Test]
        public void TestDictionaryConstructorRejectsKeysThatCollideAfterFolding()
        {
            var source = new JCG.Dictionary<string, int> { { "Hello", 1 }, { "HELLO", 2 } };
            Assert.Throws<ArgumentException>(() => new CharArrayDictionary<int>(TEST_VERSION_CURRENT, source, true));

            // Case sensitive is fine
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, source, false);
            assertEquals(2, dict.Count);
        }

        [Test]
        public void TestSetConstructorFoldsDuplicates()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, new[] { "Hello", "HELLO", "hello", "World" }, true);
            assertEquals(2, set.Count);

            set = new CharArraySet(TEST_VERSION_CURRENT, new[] { "Hello", "HELLO", "hello", "World" }, false);
            assertEquals(4, set.Count);
        }

        [Test]
        public void TestReadOnlySetRejectsMutation()
        {
            var source = new CharArraySet(TEST_VERSION_CURRENT, 4, true);
            source.Add("Hello");
            CharArraySet readOnly = source.AsReadOnly();

            assertTrue(readOnly.IsReadOnly);
            assertFalse(source.IsReadOnly);
            assertTrue(readOnly.Contains("HELLO"));
            assertTrue(readOnly.Contains("hello".ToCharArray()));
            assertEquals(1, readOnly.Count);

            Assert.Throws<NotSupportedException>(() => readOnly.Add("World"));
            Assert.Throws<NotSupportedException>(() => readOnly.Add("World".ToCharArray()));
            Assert.Throws<NotSupportedException>(() => readOnly.Clear());
            Assert.Throws<NotSupportedException>(() => readOnly.UnionWith(new[] { "World" }));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)readOnly).Add("World"));
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)readOnly).Remove("Hello"));
            Assert.Throws<NotSupportedException>(() => ((ISet<string>)readOnly).UnionWith(new[] { "World" }));

            assertEquals(1, readOnly.Count);
            assertSame(readOnly, readOnly.AsReadOnly());
        }

        [Test]
        public void TestReadOnlySetIsLiveView()
        {
            var source = new CharArraySet(TEST_VERSION_CURRENT, 4, true);
            source.Add("Hello");
            CharArraySet readOnly = source.AsReadOnly();

            // Enough adds to force the source to grow
            for (int i = 0; i < 100; i++)
            {
                source.Add("word" + i);
            }
            assertEquals(source.Count, readOnly.Count);
            assertTrue(readOnly.Contains("WORD99"));
        }

        [Test]
        public void TestEmptySetIsReadOnlyAndEmpty()
        {
            assertTrue(CharArraySet.Empty.IsReadOnly);
            assertEquals(0, CharArraySet.Empty.Count);
            assertFalse(CharArraySet.Empty.Contains("a"));
            assertFalse(CharArraySet.Empty.Contains("a".ToCharArray()));
            Assert.Throws<NotSupportedException>(() => CharArraySet.Empty.Add("a"));
            assertEquals("[]", CharArraySet.Empty.ToString());
        }

        [Test]
        public void TestEmptyDictionaryIsReadOnlyAndEmpty()
        {
            var empty = CharArrayDictionary<int>.Empty;
            assertTrue(empty.IsReadOnly);
            assertEquals(0, empty.Count);
            assertFalse(empty.ContainsKey("a"));
            assertFalse(empty.TryGetValue("a", out _));
            Assert.Throws<NotSupportedException>(() => empty.Add("a", 1));
            Assert.Throws<NotSupportedException>(() => empty["a"] = 1);
            assertEquals("{}", empty.ToString());
        }

        [Test]
        public void TestReadOnlyDictionaryRejectsMutation()
        {
            var source = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            source.Add("Hello", 1);
            CharArrayDictionary<int> readOnly = source.AsReadOnly();

            assertTrue(readOnly.IsReadOnly);
            assertFalse(source.IsReadOnly);
            assertEquals(1, readOnly["HELLO"]);
            assertTrue(readOnly.TryGetValue("hello".ToCharArray(), out int value));
            assertEquals(1, value);

            Assert.Throws<NotSupportedException>(() => readOnly.Add("World", 2));
            Assert.Throws<NotSupportedException>(() => readOnly["World"] = 2);
            Assert.Throws<NotSupportedException>(() => readOnly["Hello"] = 2);
            Assert.Throws<NotSupportedException>(() => readOnly.Put("World", 2, out _));
            Assert.Throws<NotSupportedException>(() => readOnly.Clear());
            Assert.Throws<NotSupportedException>(() => readOnly.PutAll(new Dictionary<string, int> { { "World", 2 } }));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)readOnly).Remove("Hello"));

            assertEquals(1, readOnly.Count);
            assertEquals(1, readOnly["hello"]);
        }

        [Test]
        public void TestSetRejectsNullStrings()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 4, true);
            Assert.Throws<ArgumentNullException>(() => set.Add((string)null));
            Assert.Throws<ArgumentNullException>(() => set.Contains((string)null));
        }

        [Test]
        public void TestDictionaryRejectsNullStrings()
        {
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            Assert.Throws<ArgumentNullException>(() => dict.Add((string)null, 1));
            Assert.Throws<ArgumentNullException>(() => dict.ContainsKey((string)null));
            Assert.Throws<ArgumentNullException>(() => dict.TryGetValue((string)null, out _));
            Assert.Throws<ArgumentNullException>(() => dict.Put((string)null, 1, out _));
            Assert.Throws<ArgumentNullException>(() => _ = dict[(string)null]);
            Assert.Throws<ArgumentNullException>(() => dict[(string)null] = 1);
        }

        [Test]
        public void TestEmptyStringIsAValidKey()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 4, true);
            assertFalse(set.Contains(""));
            assertTrue(set.Add(""));
            assertTrue(set.Contains(""));
            assertTrue(set.Contains(Array.Empty<char>()));
            assertFalse(set.Add(Array.Empty<char>()));

            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            dict[""] = 5;
            assertEquals(5, dict[Array.Empty<char>()]);
        }

        [Test]
        public void TestDictionaryPutReportsPreviousValue()
        {
            var dict = new CharArrayDictionary<string>(TEST_VERSION_CURRENT, 4, true);
            assertTrue(dict.Put("Key", "one", out string previous));
            assertNull(previous);
            assertFalse(dict.Put("KEY", "two", out previous));
            assertEquals("one", previous);
            assertFalse(dict.Put("key".ToCharArray(), "three", out previous));
            assertEquals("two", previous);
            assertEquals("three", dict["kEy"]);
            assertEquals(1, dict.Count);
        }

        [Test]
        public void TestDictionaryIndexerThrowsForMissingKey()
        {
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            dict["present"] = 1;
            Assert.Throws<KeyNotFoundException>(() => _ = dict["missing"]);
            Assert.Throws<KeyNotFoundException>(() => _ = dict["missing".ToCharArray()]);
        }

        [Test]
        public void TestDictionaryAddRejectsDuplicateAfterFolding()
        {
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, true);
            dict.Add("Key", 1);
            Assert.Throws<ArgumentException>(() => dict.Add("KEY", 2));
            Assert.Throws<ArgumentException>(() => dict.Add("key".ToCharArray(), 2));
            assertEquals(1, dict["key"]);

            var caseSensitive = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, false);
            caseSensitive.Add("Key", 1);
            caseSensitive.Add("KEY", 2);
            assertEquals(2, caseSensitive.Count);
        }

        [Test]
        public void TestDictionaryValuesToString()
        {
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, false);
            dict["a"] = 1;
            assertEquals("[1]", dict.Values.ToString());
            assertEquals("{a=1}", dict.ToString());
        }

        [Test]
        public void TestDictionaryValuesContainsFindsValues()
        {
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, false);
            dict["a"] = 1;
            assertTrue(dict.Values.Contains(1));
            assertFalse(dict.Values.Contains(2));
        }

        [Test]
        public void TestToCharArrayDictionaryWithIgnoreCaseCopiesEntries()
        {
            IDictionary<string, int> source = new JCG.Dictionary<string, int> { { "Hello", 1 } };
            CharArrayDictionary<int> dict = source.ToCharArrayDictionary(TEST_VERSION_CURRENT, true);
            assertEquals(1, dict.Count);
            assertEquals(1, dict["HELLO"]);
        }

        [Test]
        public void TestToCharArraySetExtensionKeepsIgnoreCaseOfCharArraySetSource()
        {
            var source = new CharArraySet(TEST_VERSION_CURRENT, 4, true);
            source.Add("Hello");
            CharArraySet copy = EnumerableExtensions.ToCharArraySet((IEnumerable<string>)source, TEST_VERSION_CURRENT);
            assertTrue(copy.Contains("HELLO"));
        }

        [Test]
        public void TestSetOperationsHonorCaseFolding()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, new[] { "Hello", "World" }, true);

            assertTrue(set.SetEquals(new[] { "HELLO", "world" }));
            assertFalse(set.SetEquals(new[] { "HELLO" }));
            assertTrue(set.IsSupersetOf(new[] { "HELLO" }));
            assertTrue(set.IsProperSupersetOf(new[] { "HELLO" }));
            assertTrue(set.IsSubsetOf(new[] { "HELLO", "WORLD", "other" }));
            assertTrue(set.IsProperSubsetOf(new[] { "HELLO", "WORLD", "other" }));
            assertTrue(set.Overlaps(new[] { "nope", "WORLD" }));
            assertFalse(set.Overlaps(new[] { "nope" }));

            assertFalse(set.UnionWith(new[] { "HELLO", "WORLD" }));
            assertTrue(set.UnionWith(new[] { "HELLO", "Other" }));
            assertEquals(3, set.Count);
            assertTrue(set.Contains("other"));
        }

        [Test]
        public void TestUnsupportedSetMutationsThrow()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, new[] { "a", "b" }, false);
            Assert.Throws<NotSupportedException>(() => ((ICollection<string>)set).Remove("a"));
            Assert.Throws<NotSupportedException>(() => ((ISet<string>)set).IntersectWith(new[] { "a" }));
            Assert.Throws<NotSupportedException>(() => ((ISet<string>)set).ExceptWith(new[] { "a" }));
            Assert.Throws<NotSupportedException>(() => ((ISet<string>)set).SymmetricExceptWith(new[] { "a" }));
            assertEquals(2, set.Count);
        }

        [Test]
        public void TestUnsupportedDictionaryRemovalsThrow()
        {
            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, false);
            dict["a"] = 1;
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)dict).Remove("a"));
            Assert.Throws<NotSupportedException>(() => ((ICollection<KeyValuePair<string, int>>)dict).Remove(new KeyValuePair<string, int>("a", 1)));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.IDictionary)dict).Remove("a"));
            assertEquals(1, dict.Count);
        }

        [Test]
        public void TestModificationDuringEnumerationThrows()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, new[] { "a", "b" }, false);
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string s in set)
                {
                    set.Add(s + "x");
                }
            });

            var dict = new CharArrayDictionary<int>(TEST_VERSION_CURRENT, 4, false);
            dict["a"] = 1;
            dict["b"] = 2;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var pair in dict)
                {
                    dict[pair.Key + "x"] = 3;
                }
            });
        }

        private const string FoldingAlphabet =
            "abcxyzABCXYZ" +
            "ÀàÉéß" + // Latin-1, including sharp s
            "ΑαΣσςΩω" + // Greek, including final sigma
            "ЖжИи" + // Cyrillic
            "KΩÅıİſ" + // Kelvin, Ohm, Angstrom, dotless i, dotted I, long s
            "𐀀"; // lone surrogates (pairs are added separately)

        private static string RandomFoldingString(Random random)
        {
            int length = random.Next(6);
            var sb = new StringBuilder(length * 2);
            for (int i = 0; i < length; i++)
            {
                int choice = random.Next(10);
                if (choice == 0)
                {
                    sb.Append(random.NextBoolean() ? "𐐀" : "𐐨"); // Deseret capital/small long i
                }
                else
                {
                    sb.Append(FoldingAlphabet[random.Next(FoldingAlphabet.Length)]);
                }
            }
            return sb.ToString();
        }

        private static string Escape(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c < 0x20 || c > 0x7E)
                    sb.Append("\\u").Append(((int)c).ToString("X4"));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
