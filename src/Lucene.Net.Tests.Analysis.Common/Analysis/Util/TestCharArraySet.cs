using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using Version = Lucene.Net.Util.LuceneVersion;

namespace Lucene.Net.Tests.Analysis.Common.Analysis.Util
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
            string[] words = { "Hello", "World", "this", "is", "a", "test" };
            char[] findme = "xthisy".ToCharArray();
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            set.AddAll(words);
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
            set.AddAll(TEST_STOP_WORDS);
            assertEquals("Not all words added", TEST_STOP_WORDS.Length, set.size());
            set.Clear();
            assertEquals("not empty", 0, set.size());
            for (var i = 0; i < TEST_STOP_WORDS.Length; i++)
            {
                assertFalse(set.Contains(TEST_STOP_WORDS[i]));
            }
            set.AddAll(TEST_STOP_WORDS);
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
            set.AddAll(TEST_STOP_WORDS);
            int size = set.size();
            set = CharArraySet.UnmodifiableSet(set);
            assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
            string NOT_IN_SET = "SirGallahad";
            assertFalse("Test String already exists in set", set.Contains(NOT_IN_SET));

            try
            {
                set.add(NOT_IN_SET.ToCharArray());
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
                set.add(new StringBuilder(NOT_IN_SET));
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

            // This test was changed in 3.1, as a contains() call on the given Collection using the "correct" iterator's
            // current key (now a char[]) on a Set<String> would not hit any element of the CAS and therefor never call
            // remove() on the iterator
            try
            {
                set.removeAll(new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, true));
                fail("Modified unmodifiable set");
            }
            catch (System.NotSupportedException)
            {
                // expected
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

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

            for (int i = 0; i < TEST_STOP_WORDS.Length; i++)
            {
                assertTrue(set.contains(TEST_STOP_WORDS[i]));
            }
        }

        [Test]
        public virtual void TestUnmodifiableSet()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            set.AddAll(TEST_STOP_WORDS);
            set.Add(Convert.ToInt32(1));
            int size = set.size();
            set = CharArraySet.UnmodifiableSet(set);
            assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
            foreach (var stopword in TEST_STOP_WORDS)
            {
                assertTrue(set.contains(stopword));
            }
            assertTrue(set.contains(Convert.ToInt32(1)));
            assertTrue(set.contains("1"));
            assertTrue(set.contains(new[] { '1' }));

            try
            {
                CharArraySet.UnmodifiableSet(null);
                fail("can not make null unmodifiable");
            }
            catch (System.NullReferenceException)
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
        [Obsolete("(3.1) remove this test when lucene 3.0 'broken unicode 4' support is")]
        public virtual void testSupplementaryCharsBWCompat()
        {
            string missing = "Term %s is missing in the set";
            string falsePos = "Term %s is in the set but shouldn't";
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
        [Obsolete("(3.1) remove this test when lucene 3.0 'broken unicode 4' support is")]
        public virtual void TestSingleHighSurrogateBWComapt()
        {
            string missing = "Term %s is missing in the set";
            string falsePos = "Term %s is in the set but shouldn't";
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
            setIngoreCase.add(Convert.ToInt32(1));
            setCaseSensitive.addAll(TEST_STOP_WORDS);
            setCaseSensitive.add(Convert.ToInt32(1));

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
            setIngoreCase.add(Convert.ToInt32(1));
            setCaseSensitive.addAll(TEST_STOP_WORDS);
            setCaseSensitive.add(Convert.ToInt32(1));

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
            ISet<string> set = new HashSet<string>();

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
                assertFalse(CharArraySet.EMPTY_SET.contains(stopword));
            }
            assertFalse(CharArraySet.EMPTY_SET.contains("foo"));
            assertFalse(CharArraySet.EMPTY_SET.contains((object)"foo"));
            assertFalse(CharArraySet.EMPTY_SET.contains("foo".ToCharArray()));
            assertFalse(CharArraySet.EMPTY_SET.Contains("foo".ToCharArray(), 0, 3));
        }

        /// <summary>
        /// Test for NPE
        /// </summary>
//        [Test]
//        public virtual void TestContainsWithNull()
//        {
//            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
//            try
//            {
//                set.contains((char[])null, 0, 10);
//                fail("null value must raise NPE");
//            }
//            catch (System.NullReferenceException)
//            {
//            }
//            try
//            {
//                set.contains(null);
//                fail("null value must raise NPE");
//            }
//            catch (System.NullReferenceException)
//            {
//            }
//            try
//            {
//                set.contains((object)null);
//                fail("null value must raise NPE");
//            }
//            catch (System.NullReferenceException)
//            {
//            }
//        }

        [Test]
        public virtual void TestToString()
        {
            CharArraySet set = CharArraySet.Copy(TEST_VERSION_CURRENT, Collections.Singleton("test"));
            assertEquals("[test]", set.ToString());
            set.add("test2");
            assertTrue(set.ToString().Contains(", "));

            set = CharArraySet.Copy(Version.LUCENE_30, Collections.Singleton("test"));
            assertEquals("[test]", set.ToString());
            set.add("test2");
            assertTrue(set.ToString().Contains(", "));
        }
    }

}