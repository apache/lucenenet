using J2N.Text;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JCG = J2N.Collections.Generic;
using Version = Lucene.Net.Util.LuceneVersion;

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
    public class TestCharArraySet : LuceneTestCase
    {

        internal static readonly string[] TEST_STOP_WORDS = { "a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with" };

        [Test]
        public virtual void TestRehash()
        {
            CharArraySet cas = new CharArraySet(TEST_VERSION_CURRENT, 0, true);
            for (int i = 0; i < TEST_STOP_WORDS.Length; i++)
            {
                cas.Add(TEST_STOP_WORDS[i]);
            }
            assertEquals(TEST_STOP_WORDS.Length, cas.size());
            for (int i = 0; i < TEST_STOP_WORDS.Length; i++)
            {
                assertTrue(cas.Contains(TEST_STOP_WORDS[i]));
            }
        }

        [Test]
        public virtual void TestNonZeroOffset()
        {
            string[] words = new string[] { "Hello", "World", "this", "is", "a", "test" };
            char[] findme = "xthisy".ToCharArray();
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            set.UnionWith(words);
            assertTrue(set.Contains(findme, 1, 4));
            assertTrue(set.Contains(new string(findme, 1, 4)));

            // test unmodifiable
            set = CharArraySet.UnmodifiableSet(set);
            assertTrue(set.Contains(findme, 1, 4));
            assertTrue(set.Contains(new string(findme, 1, 4)));
        }

        [Test]
        public virtual void TestObjectContains()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            int? val = Convert.ToInt32(1);
            set.Add(val);
            assertTrue(set.Contains(val));
            assertTrue(set.Contains(new int?(1))); // another integer
            assertTrue(set.Contains("1"));
            assertTrue(set.Contains(new char[] { '1' }));
            // test unmodifiable
            set = CharArraySet.UnmodifiableSet(set);
            assertTrue(set.Contains(val));
            assertTrue(set.Contains(new int?(1))); // another integer
            assertTrue(set.Contains("1"));
            assertTrue(set.Contains(new char[] { '1' }));
        }

        [Test]
        public virtual void TestClear()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            set.UnionWith(TEST_STOP_WORDS);
            assertEquals("Not all words added", TEST_STOP_WORDS.Length, set.size());
            set.Clear();
            assertEquals("not empty", 0, set.size());
            for (var i = 0; i < TEST_STOP_WORDS.Length; i++)
            {
                assertFalse(set.Contains(TEST_STOP_WORDS[i]));
            }
            set.UnionWith(TEST_STOP_WORDS);
            assertEquals("Not all words added", TEST_STOP_WORDS.Length, set.size());
            for (var i = 0; i < TEST_STOP_WORDS.Length; i++)
            {
                assertTrue("Set doesn't contain " + TEST_STOP_WORDS[i], set.Contains(TEST_STOP_WORDS[i]));
            }
        }

        [Test]
        public virtual void TestModifyOnUnmodifiable()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            set.UnionWith(TEST_STOP_WORDS);
            int size = set.size();
            set = CharArraySet.UnmodifiableSet(set);
            assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
            string NOT_IN_SET = "SirGallahad";
            assertFalse("Test String already exists in set", set.Contains(NOT_IN_SET));

            try
            {
                set.Add(NOT_IN_SET.ToCharArray());
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            try
            {
                set.add(NOT_IN_SET);
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            try
            {
                set.Add(new StringBuilder(NOT_IN_SET));
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            try
            {
                set.clear();
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Changed unmodifiable set", set.contains(NOT_IN_SET));
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }
            try
            {
                set.add(NOT_IN_SET);
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            // NOTE: This results in a StackOverflow exception. Since this is not a public member of CharArraySet,
            // but an extension method for the test fixture (which apparently has a bug), this test is non-critical
            //// This test was changed in 3.1, as a contains() call on the given Collection using the "correct" iterator's
            //// current key (now a char[]) on a Set<String> would not hit any element of the CAS and therefor never call
            //// remove() on the iterator
            //try
            //{
            //    set.removeAll(new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, true));
            //    fail("Modified unmodifiable set");
            //}
            //catch (System.NotSupportedException)
            //{
            //    // expected
            //    assertEquals("Size of unmodifiable set has changed", size, set.size());
            //}

            #region Added for better .NET support
            // This test was added for .NET to check the Remove method, since the extension method
            // above fails to execute.
            try
            {
#pragma warning disable 612, 618
                set.Remove(TEST_STOP_WORDS[0]);
#pragma warning restore 612, 618
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }
            #endregion

            try
            {
                set.retainAll(new CharArraySet(TEST_VERSION_CURRENT, new [] { NOT_IN_SET }, true));
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            try
            {
                set.addAll(new[] { NOT_IN_SET});
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
            }

            // LUCENENET Specific - added to test .NETified UnionWith method
            try
            {
                set.UnionWith(new[] { NOT_IN_SET });
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
            }

            for (int i = 0; i < TEST_STOP_WORDS.Length; i++)
            {
                assertTrue(set.contains(TEST_STOP_WORDS[i]));
            }
        }

        [Test]
        public virtual void TestUnmodifiableSet()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            set.UnionWith(TEST_STOP_WORDS);
            set.Add(Convert.ToInt32(1));
            int size = set.size();
            set = CharArraySet.UnmodifiableSet(set);
            assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
            foreach (var stopword in TEST_STOP_WORDS)
            {
                assertTrue(set.Contains(stopword));
            }
            assertTrue(set.Contains(Convert.ToInt32(1)));
            assertTrue(set.Contains("1"));
            assertTrue(set.Contains(new[] { '1' }));

            try
            {
                CharArraySet.UnmodifiableSet(null);
                fail("can not make null unmodifiable");
            }
            catch (System.ArgumentNullException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
            {
                // expected
            }
        }

        [Test]
        public virtual void TestSupplementaryChars()
        {
            string missing = "Term {0} is missing in the set";
            string falsePos = "Term {0} is in the set but shouldn't";
            // for reference see
            // http://unicode.org/cldr/utility/list-unicodeset.jsp?a=[[%3ACase_Sensitive%3DTrue%3A]%26[^[\u0000-\uFFFF]]]&esc=on
            string[] upperArr = new string[] { "Abc\ud801\udc1c", "\ud801\udc1c\ud801\udc1cCDE", "A\ud801\udc1cB" };
            string[] lowerArr = new string[] { "abc\ud801\udc44", "\ud801\udc44\ud801\udc44cde", "a\ud801\udc44b" };
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, true);
            foreach (string upper in upperArr)
            {
                set.add(upper);
            }
            for (int i = 0; i < upperArr.Length; i++)
            {
                assertTrue(string.Format(missing, upperArr[i]), set.contains(upperArr[i]));
                assertTrue(string.Format(missing, lowerArr[i]), set.contains(lowerArr[i]));
            }
            set = new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, false);
            foreach (string upper in upperArr)
            {
                set.add(upper);
            }
            for (int i = 0; i < upperArr.Length; i++)
            {
                assertTrue(string.Format(missing, upperArr[i]), set.contains(upperArr[i]));
                assertFalse(string.Format(falsePos, lowerArr[i]), set.contains(lowerArr[i]));
            }
        }

        [Test]
        public virtual void TestSingleHighSurrogate()
        {
            string missing = "Term {0} is missing in the set";
            string falsePos = "Term {0} is in the set but shouldn't";
            string[] upperArr = { "ABC\uD800", "ABC\uD800EfG", "\uD800EfG", "\uD800\ud801\udc1cB" };

            string[] lowerArr = { "abc\uD800", "abc\uD800efg", "\uD800efg", "\uD800\ud801\udc44b" };
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, true);
            foreach (string upper in upperArr)
            {
                set.add(upper);
            }
            for (int i = 0; i < upperArr.Length; i++)
            {
                assertTrue(string.Format(missing, upperArr[i]), set.contains(upperArr[i]));
                assertTrue(string.Format(missing, lowerArr[i]), set.contains(lowerArr[i]));
            }
            set = new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, false);
            foreach (string upper in upperArr)
            {
                set.add(upper);
            }
            for (int i = 0; i < upperArr.Length; i++)
            {
                assertTrue(string.Format(missing, upperArr[i]), set.contains(upperArr[i]));
                assertFalse(string.Format(falsePos, upperArr[i]), set.contains(lowerArr[i]));
            }
        }

        /// @deprecated (3.1) remove this test when lucene 3.0 "broken unicode 4" support is
        ///             no longer needed. 
        [Test]
        [Obsolete("(3.1) remove this test when lucene 3.0 'broken unicode 4' support is no longer needed.")]
        public virtual void TestSupplementaryCharsBWCompat()
        {
            string missing = "Term {0} is missing in the set";
            string falsePos = "Term {0} is in the set but shouldn't";
            // for reference see
            // http://unicode.org/cldr/utility/list-unicodeset.jsp?a=[[%3ACase_Sensitive%3DTrue%3A]%26[^[\u0000-\uFFFF]]]&esc=on
            string[] upperArr = new string[] { "Abc\ud801\udc1c", "\ud801\udc1c\ud801\udc1cCDE", "A\ud801\udc1cB" };
            string[] lowerArr = new string[] { "abc\ud801\udc44", "\ud801\udc44\ud801\udc44cde", "a\ud801\udc44b" };
            CharArraySet set = new CharArraySet(Version.LUCENE_30, TEST_STOP_WORDS, true);
            foreach (string upper in upperArr)
            {
                set.add(upper);
            }
            for (int i = 0; i < upperArr.Length; i++)
            {
                assertTrue(string.Format(missing, upperArr[i]), set.contains(upperArr[i]));
                assertFalse(string.Format(falsePos, lowerArr[i]), set.contains(lowerArr[i]));
            }
            set = new CharArraySet(Version.LUCENE_30, TEST_STOP_WORDS, false);
            foreach (string upper in upperArr)
            {
                set.add(upper);
            }
            for (int i = 0; i < upperArr.Length; i++)
            {
                assertTrue(string.Format(missing, upperArr[i]), set.contains(upperArr[i]));
                assertFalse(string.Format(falsePos, lowerArr[i]), set.contains(lowerArr[i]));
            }
        }

        /// @deprecated (3.1) remove this test when lucene 3.0 "broken unicode 4" support is
        ///             no longer needed. 
        [Test]
        [Obsolete("(3.1) remove this test when lucene 3.0 'broken unicode 4' support is no longer needed.")]
        public virtual void TestSingleHighSurrogateBWComapt()
        {
            string missing = "Term {0} is missing in the set";
            string falsePos = "Term {0} is in the set but shouldn't";
            string[] upperArr = new string[] { "ABC\uD800", "ABC\uD800EfG", "\uD800EfG", "\uD800\ud801\udc1cB" };

            string[] lowerArr = new string[] { "abc\uD800", "abc\uD800efg", "\uD800efg", "\uD800\ud801\udc44b" };
            CharArraySet set = new CharArraySet(Version.LUCENE_30, TEST_STOP_WORDS, true);
            foreach (string upper in upperArr)
            {
                set.add(upper);
            }
            for (int i = 0; i < upperArr.Length; i++)
            {
                assertTrue(string.Format(missing, upperArr[i]), set.contains(upperArr[i]));
                if (i == lowerArr.Length - 1)
                {
                    assertFalse(string.Format(falsePos, lowerArr[i]), set.contains(lowerArr[i]));
                }
                else
                {
                    assertTrue(string.Format(missing, lowerArr[i]), set.contains(lowerArr[i]));
                }
            }
            set = new CharArraySet(Version.LUCENE_30, TEST_STOP_WORDS, false);
            foreach (string upper in upperArr)
            {
                set.add(upper);
            }
            for (int i = 0; i < upperArr.Length; i++)
            {
                assertTrue(string.Format(missing, upperArr[i]), set.contains(upperArr[i]));
                assertFalse(string.Format(falsePos, lowerArr[i]), set.contains(lowerArr[i]));
            }
        }

        [Test]
        public virtual void TestCopyCharArraySetBWCompat()
        {
            CharArraySet setIngoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpper());
            }
            setIngoreCase.addAll(TEST_STOP_WORDS);
            setIngoreCase.Add(Convert.ToInt32(1));
            setCaseSensitive.addAll(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1));

            CharArraySet copy = CharArraySet.Copy(TEST_VERSION_CURRENT, setIngoreCase);
            CharArraySet copyCaseSens = CharArraySet.Copy(TEST_VERSION_CURRENT, setCaseSensitive);

            assertEquals(setIngoreCase.size(), copy.size());
            assertEquals(setCaseSensitive.size(), copy.size());

            assertTrue(copy.containsAll(stopwords));
            assertTrue(copy.containsAll(stopwordsUpper));
            assertTrue(copyCaseSens.containsAll(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.contains(@string));
            }
            // test adding terms to the copy
            IList<string> newWords = new List<string>();
            foreach (string @string in stopwords)
            {
                newWords.Add(@string + "_1");
            }
            copy.addAll(newWords);

            assertTrue(copy.containsAll(stopwords));
            assertTrue(copy.containsAll(stopwordsUpper));
            assertTrue(copy.containsAll(newWords));
            // new added terms are not in the source set
            foreach (string @string in newWords)
            {
                assertFalse(setIngoreCase.contains(@string));
                assertFalse(setCaseSensitive.contains(@string));

            }
        }

        /// <summary>
        /// Test the static #copy() function with a CharArraySet as a source
        /// </summary>
        [Test]
        public virtual void TestCopyCharArraySet()
        {
            CharArraySet setIngoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpper());
            }
            setIngoreCase.addAll(TEST_STOP_WORDS);
            setIngoreCase.Add(Convert.ToInt32(1));
            setCaseSensitive.addAll(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1));

            CharArraySet copy = CharArraySet.Copy(TEST_VERSION_CURRENT, setIngoreCase);
            CharArraySet copyCaseSens = CharArraySet.Copy(TEST_VERSION_CURRENT, setCaseSensitive);

            assertEquals(setIngoreCase.size(), copy.size());
            assertEquals(setCaseSensitive.size(), copy.size());

            assertTrue(copy.containsAll(stopwords));
            assertTrue(copy.containsAll(stopwordsUpper));
            assertTrue(copyCaseSens.containsAll(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.contains(@string));
            }
            // test adding terms to the copy
            IList<string> newWords = new List<string>();
            foreach (string @string in stopwords)
            {
                newWords.Add(@string + "_1");
            }
            copy.addAll(newWords);

            assertTrue(copy.containsAll(stopwords));
            assertTrue(copy.containsAll(stopwordsUpper));
            assertTrue(copy.containsAll(newWords));
            // new added terms are not in the source set
            foreach (string @string in newWords)
            {
                assertFalse(setIngoreCase.contains(@string));
                assertFalse(setCaseSensitive.contains(@string));
            }
        }

        /// <summary>
        /// Test the static #copy() function with a JDK <seealso cref="Set"/> as a source
        /// </summary>
        [Test]
        public virtual void TestCopyJDKSet()
        {
            ISet<string> set = new JCG.HashSet<string>();

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpper());
            }
            set.addAll(TEST_STOP_WORDS);

            CharArraySet copy = CharArraySet.Copy(TEST_VERSION_CURRENT, set);

            assertEquals(set.Count, copy.size());
            assertEquals(set.Count, copy.size());

            assertTrue(copy.containsAll(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copy.contains(@string));
            }

            IList<string> newWords = new List<string>();
            foreach (string @string in stopwords)
            {
                newWords.Add(@string + "_1");
            }
            copy.addAll(newWords);

            assertTrue(copy.containsAll(stopwords));
            assertTrue(copy.containsAll(newWords));
            // new added terms are not in the source set
            foreach (string @string in newWords)
            {
                assertFalse(set.Contains(@string));
            }
        }

        /// <summary>
        /// Tests a special case of <seealso cref="CharArraySet#copy(Version, Set)"/> where the
        /// set to copy is the <seealso cref="CharArraySet#EMPTY_SET"/>
        /// </summary>
        [Test]
        public virtual void TestCopyEmptySet()
        {
            assertSame(CharArraySet.EMPTY_SET, CharArraySet.Copy(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET));
        }

        /// <summary>
        /// Smoketests the static empty set
        /// </summary>
        [Test]
        public virtual void TestEmptySet()
        {
            assertEquals(0, CharArraySet.EMPTY_SET.size());

            assertTrue(CharArraySet.EMPTY_SET.Count == 0);
            foreach (string stopword in TEST_STOP_WORDS)
            {
                assertFalse(CharArraySet.EMPTY_SET.Contains(stopword));
            }
            assertFalse(CharArraySet.EMPTY_SET.Contains("foo"));
            assertFalse(CharArraySet.EMPTY_SET.Contains((object)"foo"));
            assertFalse(CharArraySet.EMPTY_SET.Contains("foo".ToCharArray()));
            assertFalse(CharArraySet.EMPTY_SET.Contains("foo".ToCharArray(), 0, 3));
        }

        /// <summary>
        /// Test for NPE
        /// </summary>
        [Test]
        public virtual void TestContainsWithNull()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            try
            {
                set.Contains((char[])null, 0, 10);
                fail("null value must raise NPE");
            }
            catch (System.ArgumentException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
            {
            }
            try
            {
                set.Contains((ICharSequence)null);
                fail("null value must raise NPE");
            }
            catch (System.ArgumentException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
            {
            }
            // LUCENENET Specific test for string (since it does not implement ICharSequence)
            try
            {
                set.Contains((string)null);
                fail("null value must raise NPE");
            }
            catch (System.ArgumentException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
            {
            }
            try
            {
                set.Contains((object)null);
                fail("null value must raise NPE");
            }
            catch (System.ArgumentException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
            {
            }
        }

        [Test]
        public virtual void TestToString()
        {
            CharArraySet set = CharArraySet.Copy(TEST_VERSION_CURRENT, new JCG.List<string> { "test" });
            assertEquals("[test]", set.ToString());
            set.add("test2");
            assertTrue(set.ToString().Contains(", "));

#pragma warning disable 612, 618
            set = CharArraySet.Copy(Version.LUCENE_30, new JCG.List<string> { "test" });
#pragma warning restore 612, 618
            assertEquals("[test]", set.ToString());
            set.add("test2");
            assertTrue(set.ToString().Contains(", "));
        }


        #region LUCENENET specific tests

        [Test, LuceneNetSpecific]
        public virtual void TestEquality()
        {
            var values = new List<string> { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            var charArraySet = new CharArraySet(TEST_VERSION_CURRENT, values, false);
            var charArraySetCopy = new CharArraySet(TEST_VERSION_CURRENT, values, false);
            values.Reverse();
            var charArraySetReverse = new CharArraySet(TEST_VERSION_CURRENT, values, false);
            var equatableSetReverse = new JCG.HashSet<string>(values);

            assertTrue(charArraySet.GetHashCode().Equals(charArraySetCopy.GetHashCode()));
            assertTrue(charArraySet.Equals(charArraySetCopy));
            assertTrue(charArraySet.GetHashCode().Equals(charArraySetReverse.GetHashCode()));
            assertTrue(charArraySet.Equals(charArraySetReverse));
            assertTrue(charArraySet.GetHashCode().Equals(equatableSetReverse.GetHashCode()));
            assertTrue(charArraySet.Equals(equatableSetReverse));

            values = new List<string> { "sally", "seashells", "by", "the", "sea", "shore" };
            charArraySet.Clear();
            charArraySet.UnionWith(values);

            assertFalse(charArraySet.GetHashCode().Equals(charArraySetCopy.GetHashCode()));
            assertFalse(charArraySet.Equals(charArraySetCopy));
            assertFalse(charArraySet.GetHashCode().Equals(charArraySetReverse.GetHashCode()));
            assertFalse(charArraySet.Equals(charArraySetReverse));
            assertFalse(charArraySet.GetHashCode().Equals(equatableSetReverse.GetHashCode()));
            assertFalse(charArraySet.Equals(equatableSetReverse));

            equatableSetReverse.Remove("sells");
            assertTrue(charArraySet.GetHashCode().Equals(equatableSetReverse.GetHashCode()));
            assertTrue(charArraySet.Equals(equatableSetReverse));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUnionWithObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var existingValuesAsObject = new List<object> { "seashells", "sea", "shore" };
            var mixedExistingNonExistingValuesAsObject = new List<object> { "true", "set", "of", "unique", "values", "except", "sells" };
            var nonExistingMixedTypes = new object[] { true, (byte)55, (short)44, (int)33, (sbyte)22, (long)11, (char)'\n', "hurray", (uint)99, (ulong)89, (ushort)79, new char[] { 't', 'w', 'o' }, new StringCharSequence("testing") };

            // Add existing values
            assertFalse(target.UnionWith(existingValuesAsObject));
            assertEquals(7, target.Count);
            CollectionAssert.AreEquivalent(originalValues, target);

            // Add mixed existing/non-existing values
            assertTrue(target.UnionWith(mixedExistingNonExistingValuesAsObject));
            assertEquals(13, target.Count);
            CollectionAssert.AreEquivalent(new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore",
                "true", "set", "of", "unique", "values", "except"}, target);

            target.Clear();
            assertEquals(0, target.Count);
            assertTrue(target.UnionWith(originalValues.Cast<object>())); // Need to cast here because the .NET return type is void for UnionWith.
            CollectionAssert.AreEquivalent(originalValues, target);

            // Add mixed types as object
            assertTrue(target.UnionWith(nonExistingMixedTypes));
            assertEquals(20, target.Count);
            assertTrue(target.Contains(true));
            assertTrue(target.Contains((byte)55));
            assertTrue(target.Contains((short)44));
            assertTrue(target.Contains((int)33));
            assertTrue(target.Contains((sbyte)22));
            assertTrue(target.Contains((long)11));
            assertTrue(target.Contains((char)'\n'));
            assertTrue(target.Contains("hurray"));
            assertTrue(target.Contains((uint)99));
            assertTrue(target.Contains((ulong)89));
            assertTrue(target.Contains((ushort)79));
            assertTrue(target.Contains(new char[] { 't', 'w', 'o' }));
            assertTrue(target.Contains(new StringCharSequence("testing")));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUnionWithCharArray()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var existingValues = new List<char[]> { "seashells".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var mixedExistingNonExistingValues = new List<char[]> { "true".ToCharArray(), "set".ToCharArray(), "of".ToCharArray(), "unique".ToCharArray(), "values".ToCharArray(), "except".ToCharArray(), "sells".ToCharArray() };

            // Add existing values
            assertFalse(target.UnionWith(existingValues));
            assertEquals(7, target.Count);
            CollectionAssert.AreEquivalent(originalValues, target);

            // Add mixed existing/non-existing values
            assertTrue(target.UnionWith(mixedExistingNonExistingValues));
            assertEquals(13, target.Count);
            CollectionAssert.AreEquivalent(new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore",
                "true", "set", "of", "unique", "values", "except"}, target);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUnionWithString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var existingValues = new List<string> { "seashells", "sea", "shore" };
            var mixedExistingNonExistingValues = new List<string> { "true", "set", "of", "unique", "values", "except", "sells" };

            // Add existing values
            //assertFalse(target.UnionWith(existingValues));
            target.UnionWith(existingValues);
            assertEquals(7, target.Count);
            CollectionAssert.AreEquivalent(originalValues, target);

            // Add mixed existing/non-existing values
            //assertTrue(target.UnionWith(mixedExistingNonExistingValues));
            target.UnionWith(mixedExistingNonExistingValues);
            assertEquals(13, target.Count);
            CollectionAssert.AreEquivalent(new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore",
                "true", "set", "of", "unique", "values", "except"}, target);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUnionWithCharSequence()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var existingValues = new List<ICharSequence> { new StringCharSequence("seashells"), new StringCharSequence("sea"), new StringCharSequence("shore") };
            var mixedExistingNonExistingValues = new List<ICharSequence> { new StringCharSequence("true"), new StringCharSequence("set"), new StringCharSequence("of"), new StringCharSequence("unique"), new StringCharSequence("values"), new StringCharSequence("except"), new StringCharSequence("sells") };

            // Add existing values
            assertFalse(target.UnionWith(existingValues));
            assertEquals(7, target.Count);
            CollectionAssert.AreEquivalent(originalValues, target);

            // Add mixed existing/non-existing values
            assertTrue(target.UnionWith(mixedExistingNonExistingValues));
            assertEquals(13, target.Count);
            CollectionAssert.AreEquivalent(new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore",
                "true", "set", "of", "unique", "values", "except"}, target);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSubsetOfString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new List<string> { "seashells", "sea", "shore" };
            var superset = new List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertFalse(target.IsSubsetOf(subset));
            assertTrue(target.IsSubsetOf(superset));
            assertTrue(target.IsSubsetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSubsetOfObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new List<object> { "seashells", "sea", "shore" };
            var superset = new List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertFalse(target.IsSubsetOf(subset));
            assertTrue(target.IsSubsetOf(superset));
            assertTrue(target.IsSubsetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSubsetOfString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new List<string> { "seashells", "sea", "shore" };
            var superset = new List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertFalse(target.IsProperSubsetOf(subset));
            assertTrue(target.IsProperSubsetOf(superset));
            assertFalse(target.IsProperSubsetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSubsetOfObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new List<object> { "seashells", "sea", "shore" };
            var superset = new List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertFalse(target.IsProperSubsetOf(subset));
            assertTrue(target.IsProperSubsetOf(superset));
            assertFalse(target.IsProperSubsetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSupersetOfString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new List<string> { "seashells", "sea", "shore" };
            var superset = new List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertTrue(target.IsSupersetOf(subset));
            assertFalse(target.IsSupersetOf(superset));
            assertTrue(target.IsSupersetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSupersetOfObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new List<object> { "seashells", "sea", "shore" };
            var superset = new List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertTrue(target.IsSupersetOf(subset));
            assertFalse(target.IsSupersetOf(superset));
            assertTrue(target.IsSupersetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSupersetOfString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new List<string> { "seashells", "sea", "shore" };
            var superset = new List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertTrue(target.IsProperSupersetOf(subset));
            assertFalse(target.IsProperSupersetOf(superset));
            assertFalse(target.IsProperSupersetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSupersetOfObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new List<object> { "seashells", "sea", "shore" };
            var superset = new List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertTrue(target.IsProperSupersetOf(subset));
            assertFalse(target.IsProperSupersetOf(superset));
            assertFalse(target.IsProperSupersetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestOverlapsString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var nonOverlapping = new List<string> { "peter", "piper", "picks", "a", "pack", "of", "pickled", "peppers" };
            var overlapping = new List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertFalse(target.Overlaps(nonOverlapping));
            assertTrue(target.Overlaps(overlapping));
            assertTrue(target.Overlaps(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestOverlapsObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var nonOverlapping = new List<object> { "peter", "piper", "picks", "a", "pack", "of", "pickled", "peppers" };
            var overlapping = new List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more" };

            assertFalse(target.Overlaps(nonOverlapping));
            assertTrue(target.Overlaps(overlapping));
            assertTrue(target.Overlaps(originalValues));
        }

        #endregion
    }
}