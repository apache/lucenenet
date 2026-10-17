// Lucene version compatibility level 4.8.1
using J2N.Collections;
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections;
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
            assertTrue(set.Contains(findme.AsSpan(1, 4)));
            assertTrue(set.Contains(new string(findme, 1, 4)));

            // test unmodifiable
            set = set.AsReadOnly();
            assertTrue(set.Contains(findme.AsSpan(1, 4)));
            assertTrue(set.Contains(new string(findme, 1, 4)));
        }

        [Test]
        public virtual void TestObjectContains()
        {
            // LUCENENET: The overloads that converted arbitrary objects using ToString() have been removed,
            // so the integer is converted to a string by the caller.
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            string val = J2N.Numerics.Int32.GetInstance(1).ToString();
            set.Add(val);
            assertTrue(set.Contains(val));
            assertTrue(set.Contains(J2N.Numerics.Int32.GetInstance(1).ToString())); // another integer
            assertTrue(set.Contains("1"));
            assertTrue(set.Contains(new char[] { '1' }));
            // test unmodifiable
            set = set.AsReadOnly();
            assertTrue(set.Contains(val));
            assertTrue(set.Contains(J2N.Numerics.Int32.GetInstance(1).ToString())); // another integer
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
            set = set.AsReadOnly();
            assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
            string NOT_IN_SET = "SirGallahad";
            assertFalse("Test String already exists in set", set.Contains(NOT_IN_SET));

            try
            {
                set.Add(NOT_IN_SET.ToCharArray());
                fail("Modified unmodifiable set");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
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
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            try
            {
                set.Add(new StringBuilder(NOT_IN_SET).ToString()); // LUCENENET: The StringBuilder (CharSequence) overload has been removed
                fail("Modified unmodifiable set");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
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
            catch (Exception e) when (e.IsUnsupportedOperationException())
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
            catch (Exception e) when (e.IsUnsupportedOperationException())
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
            //catch (Exception e) when (e.IsUnsupportedOperationException())
            //{
            //    // expected
            //    assertEquals("Size of unmodifiable set has changed", size, set.size());
            //}

            #region LUCENENET Added for better .NET support
            // This test was added for .NET to check the Remove method, since the extension method
            // above fails to execute.
            try
            {
                ((ISet<string>)set).Remove(TEST_STOP_WORDS[0]);
                fail("Modified unmodifiable set");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            // LUCENENET Specific - added to test .NETified UnionWith method
            try
            {
                set.UnionWith(new[] { NOT_IN_SET });
                fail("Modified unmodifiable set");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
            }

            #endregion LUCENENET Added for better .NET support

            try
            {
                ((ISet<string>)set).ExceptWith(new CharArraySet(TEST_VERSION_CURRENT, new[] { NOT_IN_SET }, true));
                fail("Modified unmodifiable set");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            try
            {
                set.UnionWith(new[] { NOT_IN_SET });
                fail("Modified unmodifiable set");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertFalse("Test String has been added to unmodifiable set", set.contains(NOT_IN_SET));
            }

            for (int i = 0; i < TEST_STOP_WORDS.Length; i++)
            {
                assertTrue(set.Contains(TEST_STOP_WORDS[i]));
            }
        }

        [Test]
        public virtual void TestUnmodifiableSet()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            set.UnionWith(TEST_STOP_WORDS);
            set.Add(Convert.ToInt32(1).ToString()); // LUCENENET: Object overloads have been removed
            int size = set.size();
            set = set.AsReadOnly();
            assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
            foreach (var stopword in TEST_STOP_WORDS)
            {
                assertTrue(set.Contains(stopword));
            }
            assertTrue(set.Contains(Convert.ToInt32(1).ToString())); // LUCENENET: Object overloads have been removed
            assertTrue(set.Contains("1"));
            assertTrue(set.Contains(new[] { '1' }));

            // LUCENENET specific - we use an instance method, so this is not a valid call
            //try
            //{
            //    CharArraySet.UnmodifiableSet(null);
            //    fail("can not make null unmodifiable");
            //}
            //catch (ArgumentNullException) // NOTE: In .NET we throw an ArgumentException, not a NullReferenceException
            //{
            //    // expected
            //}
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
            CharArraySet setIgnoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            setIgnoreCase.addAll(TEST_STOP_WORDS);
            setIgnoreCase.Add(Convert.ToInt32(1).ToString()); // LUCENENET: Object overloads have been removed
            setCaseSensitive.addAll(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1).ToString()); // LUCENENET: Object overloads have been removed

            CharArraySet copy = new CharArraySet(TEST_VERSION_CURRENT, setIgnoreCase); // LUCENENET: Changed Copy() to copy constructor
            CharArraySet copyCaseSens = new CharArraySet(TEST_VERSION_CURRENT, setCaseSensitive); // LUCENENET: Changed Copy() to copy constructor

            assertEquals(setIgnoreCase.size(), copy.size());
            assertEquals(setCaseSensitive.size(), copy.size());

            assertTrue(copy.containsAll(stopwords));
            assertTrue(copy.containsAll(stopwordsUpper));
            assertTrue(copyCaseSens.containsAll(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.contains(@string));
            }
            // test adding terms to the copy
            IList<string> newWords = new JCG.List<string>();
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
                assertFalse(setIgnoreCase.contains(@string));
                assertFalse(setCaseSensitive.contains(@string));

            }
        }

        /// <summary>
        /// Test the static #copy() function with a CharArraySet as a source
        /// </summary>
        [Test]
        public virtual void TestCopyCharArraySet()
        {
            CharArraySet setIgnoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            setIgnoreCase.addAll(TEST_STOP_WORDS);
            setIgnoreCase.Add(Convert.ToInt32(1).ToString()); // LUCENENET: Object overloads have been removed
            setCaseSensitive.addAll(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1).ToString()); // LUCENENET: Object overloads have been removed

            CharArraySet copy = new CharArraySet(TEST_VERSION_CURRENT, setIgnoreCase); // LUCENENET: Changed Copy() to copy constructor
            CharArraySet copyCaseSens = new CharArraySet(TEST_VERSION_CURRENT, setCaseSensitive); // LUCENENET: Changed Copy() to copy constructor

            assertEquals(setIgnoreCase.size(), copy.size());
            assertEquals(setCaseSensitive.size(), copy.size());

            assertTrue(copy.containsAll(stopwords));
            assertTrue(copy.containsAll(stopwordsUpper));
            assertTrue(copyCaseSens.containsAll(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.contains(@string));
            }
            // test adding terms to the copy
            IList<string> newWords = new JCG.List<string>();
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
                assertFalse(setIgnoreCase.contains(@string));
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
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            set.addAll(TEST_STOP_WORDS);

            CharArraySet copy = new CharArraySet(TEST_VERSION_CURRENT, set); // LUCENENET: Changed Copy() to copy constructor

            assertEquals(set.Count, copy.size());
            assertEquals(set.Count, copy.size());

            assertTrue(copy.containsAll(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copy.contains(@string));
            }

            IList<string> newWords = new JCG.List<string>();
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
            // LUCENENET: Copy() was replaced by a copy constructor, which cannot return the same instance,
            // so we check that the copy is an empty, modifiable set instead.
            //assertSame(CharArraySet.Empty, CharArraySet.Copy(TEST_VERSION_CURRENT, CharArraySet.Empty));
            CharArraySet copy = new CharArraySet(TEST_VERSION_CURRENT, CharArraySet.Empty);
            assertEquals(0, copy.size());
            assertEquals(CharArraySet.Empty, copy);
            assertFalse(copy.IsReadOnly);
        }

        /// <summary>
        /// Smoketests the static empty set
        /// </summary>
        [Test]
        public virtual void TestEmptySet()
        {
            assertEquals(0, CharArraySet.Empty.size());

            assertTrue(CharArraySet.Empty.Count == 0);
            foreach (string stopword in TEST_STOP_WORDS)
            {
                assertFalse(CharArraySet.Empty.Contains(stopword));
            }
            assertFalse(CharArraySet.Empty.Contains("foo"));
            //assertFalse(CharArraySet.Empty.Contains((object)"foo")); // LUCENENET: Object overloads have been removed
            assertFalse(CharArraySet.Empty.Contains("foo".ToCharArray()));
            assertFalse(CharArraySet.Empty.Contains("foo".ToCharArray().AsSpan(0, 3)));
        }

        /// <summary>
        /// Test for NPE
        /// </summary>
        [Test]
        public virtual void TestContainsWithNull()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            // LUCENENET: The char[] with offset/length, ICharSequence and object overloads have been removed.
            // A ReadOnlySpan<char> cannot be null, so only the string overload is tested.
            //try
            //{
            //    set.Contains((char[])null, 0, 10);
            //    fail("null value must raise NPE");
            //}
            //catch (ArgumentException)
            //{
            //}
            //try
            //{
            //    set.Contains((ICharSequence)null);
            //    fail("null value must raise NPE");
            //}
            //catch (ArgumentException)
            //{
            //}
            // LUCENENET Specific test for string (since it does not implement ICharSequence)
            try
            {
                set.Contains((string)null);
                fail("null value must raise NPE");
            }
            catch (ArgumentException) // NOTE: In .NET we throw an ArgumentException, not a NullReferenceException
            {
            }
        }

        [Test]
        public virtual void TestToString()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, new JCG.List<string> { "test" }); // LUCENENET: Changed Copy() to copy constructor
            assertEquals("[test]", set.ToString());
            set.add("test2");
            assertTrue(set.ToString().Contains(", "));

#pragma warning disable 612, 618
            set = new CharArraySet(Version.LUCENE_30, new JCG.List<string> { "test" }); // LUCENENET: Changed Copy() to copy constructor
#pragma warning restore 612, 618
            assertEquals("[test]", set.ToString());
            set.add("test2");
            assertTrue(set.ToString().Contains(", "));
        }


        #region LUCENENET specific tests

        [Test, LuceneNetSpecific]
        public virtual void TestEquality()
        {
            var values = new JCG.List<string> { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
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

            values = new JCG.List<string> { "sally", "seashells", "by", "the", "sea", "shore" };
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

        // LUCENENET: The SetEquals, UnionWith, IsSubsetOf, IsProperSubsetOf, IsSupersetOf, IsProperSupersetOf and Overlaps
        // tests for object, char[] and ICharSequence were removed along with those overloads.

        [Test, LuceneNetSpecific]
        public virtual void TestSetEqualsString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            var unequalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "sometimes" };
            var unequalCaseValues = new string[] { "Sally", "Sells", "Seashells", "by", "the", "Sea", "Shore" };
            var unequalValueCount = new string[] { "sally", "sells", "seashells", "by", "the", "strange", "sea", "shore" };

            TestSetEqualsString(ignoreCase: false, originalValues, unequalValues, unequalCaseValues, unequalValueCount);
            TestSetEqualsString(ignoreCase: true, originalValues, unequalValues, unequalCaseValues, unequalValueCount);
        }

        /// <summary>
        /// Class that only implements <see cref="IEnumerable{T}"/>, so we can bypass the optimized
        /// paths that cast to <see cref="ICollection{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class TestEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> source;
            public TestEnumerable(IEnumerable<T> source)
            {
                this.source = source ?? throw new ArgumentNullException(nameof(source));
            }

            public IEnumerator<T> GetEnumerator() => source.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static CharArraySet CreateCharArraySet(Version matchVersion, IList<string> values, bool ignoreCase)
        {
            return new CharArraySet(matchVersion, values, ignoreCase);
        }

        public virtual void TestSetEqualsString(bool ignoreCase, IList<string> originalValues, IList<string> unequalValues, IList<string> unequalCaseValues, IList<string> unequalValueCount)
        {
            var originalValuesShuffled = originalValues.ToArray();
            originalValuesShuffled.Shuffle(Random);

            CharArraySet target = CreateCharArraySet(TEST_VERSION_CURRENT, originalValues, ignoreCase);

            var charArraySet_Equal = CreateCharArraySet(TEST_VERSION_CURRENT, originalValuesShuffled, ignoreCase);
            var charArraySet_Unequal = CreateCharArraySet(TEST_VERSION_CURRENT, unequalValues, ignoreCase);
            var charArraySet_UnequalCase = CreateCharArraySet(TEST_VERSION_CURRENT, unequalCaseValues, ignoreCase);
            var charArraySet_UnequalValueCount = CreateCharArraySet(TEST_VERSION_CURRENT, unequalValueCount, ignoreCase);

            assertTrue(target.SetEquals(charArraySet_Equal));
            assertFalse(target.SetEquals(charArraySet_Unequal));
            assertEquals(target.SetEquals(charArraySet_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(charArraySet_UnequalValueCount));

            var set_Equal = new HashSet<string>(originalValuesShuffled);
            var set_EqualWithNull = new HashSet<string>(originalValuesShuffled) { null };
            var set_Unequal = new HashSet<string>(unequalValues);
            var set_UnequalCase = new HashSet<string>(unequalCaseValues);
            var set_UnequalValueCount = new HashSet<string>(unequalValueCount);

            assertTrue(target.SetEquals(set_Equal));
            assertFalse(target.SetEquals(set_EqualWithNull));
            assertFalse(target.SetEquals(set_Unequal));
            assertEquals(target.SetEquals(set_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(set_UnequalValueCount));

            var list_Equal = new List<string>(originalValuesShuffled);
            var list_EqualWithNull = new List<string>(originalValuesShuffled) { null };
            var list_Unequal = new List<string>(unequalValues);
            var list_UnequalCase = new List<string>(unequalCaseValues);
            var list_UnequalValueCount = new List<string>(unequalValueCount);

            assertTrue(target.SetEquals(list_Equal));
            assertFalse(target.SetEquals(list_EqualWithNull));
            assertFalse(target.SetEquals(list_Unequal));
            assertEquals(target.SetEquals(list_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(list_UnequalValueCount));

            var enumerable_Equal = new TestEnumerable<string>(list_Equal);
            var enumerable_EqualWithNull = new TestEnumerable<string>(list_EqualWithNull);
            var enumerable_Unequal = new TestEnumerable<string>(list_Unequal);
            var enumerable_UnequalCase = new TestEnumerable<string>(list_UnequalCase);
            var enumerable_UnequalValueCount = new TestEnumerable<string>(unequalValueCount);

            assertTrue(target.SetEquals(enumerable_Equal));
            assertFalse(target.SetEquals(enumerable_EqualWithNull));
            assertFalse(target.SetEquals(enumerable_Unequal));
            assertEquals(target.SetEquals(enumerable_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(enumerable_UnequalValueCount));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUnionWithString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var existingValues = new JCG.List<string> { "seashells", "sea", "shore" };
            var mixedExistingNonExistingValues = new JCG.List<string> { "true", "set", "of", "unique", "values", "except", "sells" };

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
            var subset = new JCG.List<string> { "seashells", "sea", "shore", null };
            var superset = new JCG.List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

            assertFalse(target.IsSubsetOf(subset));
            assertTrue(target.IsSubsetOf(superset));
            assertTrue(target.IsSubsetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSubsetOfString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<string> { "seashells", "sea", "shore", null };
            var superset = new JCG.List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

            assertFalse(target.IsProperSubsetOf(subset));
            assertTrue(target.IsProperSubsetOf(superset));
            assertFalse(target.IsProperSubsetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSupersetOfString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<string> { "seashells", "sea", "shore" };
            var superset = new JCG.List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

            assertTrue(target.IsSupersetOf(subset));
            assertFalse(target.IsSupersetOf(superset));
            assertTrue(target.IsSupersetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSupersetOfString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<string> { "seashells", "sea", "shore" };
            var superset = new JCG.List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

            assertTrue(target.IsProperSupersetOf(subset));
            assertFalse(target.IsProperSupersetOf(superset));
            assertFalse(target.IsProperSupersetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestOverlapsString()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var nonOverlapping = new JCG.List<string> { "peter", "piper", "picks", "a", "pack", "of", "pickled", "peppers", null };
            var overlapping = new JCG.List<string> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

            assertFalse(target.Overlaps(nonOverlapping));
            assertTrue(target.Overlaps(overlapping));
            assertTrue(target.Overlaps(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsReadOnly()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            CharArraySet readOnlyTarget = target.AsReadOnly();

            assertFalse(target.IsReadOnly);
            assertTrue(readOnlyTarget.IsReadOnly);
        }



        [Test, LuceneNetSpecific]
        public virtual void TestToCharArraySet_CharArraySet_BWCompat()
        {
            CharArraySet setIgnoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            setIgnoreCase.UnionWith(TEST_STOP_WORDS);
            setIgnoreCase.Add(Convert.ToInt32(1).ToString());
            setCaseSensitive.UnionWith(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1).ToString());

            CharArraySet copy = setIgnoreCase.ToCharArraySet(TEST_VERSION_CURRENT);
            CharArraySet copyCaseSens = setCaseSensitive.ToCharArraySet(TEST_VERSION_CURRENT);

            assertEquals(setIgnoreCase.Count, copy.Count);
            assertEquals(setCaseSensitive.Count, copy.Count);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copyCaseSens.IsSupersetOf(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.Contains(@string));
            }
            // test adding terms to the copy
            IList<string> newWords = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                newWords.Add(@string + "_1");
            }
            copy.UnionWith(newWords);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copy.IsSupersetOf(newWords));
            // new added terms are not in the source set
            foreach (string @string in newWords)
            {
                assertFalse(setIgnoreCase.Contains(@string));
                assertFalse(setCaseSensitive.Contains(@string));

            }
        }

        /// <summary>
        /// Test the ToCharArraySet function with a CharArraySet as a source
        /// </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestToCharArraySet_CharArraySet()
        {
            CharArraySet setIgnoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            setIgnoreCase.UnionWith(TEST_STOP_WORDS);
            setIgnoreCase.Add(Convert.ToInt32(1).ToString());
            setCaseSensitive.UnionWith(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1).ToString());

            CharArraySet copy = setIgnoreCase.ToCharArraySet(TEST_VERSION_CURRENT);
            CharArraySet copyCaseSens = setCaseSensitive.ToCharArraySet(TEST_VERSION_CURRENT);

            assertEquals(setIgnoreCase.Count, copy.Count);
            assertEquals(setCaseSensitive.Count, copy.Count);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copyCaseSens.IsSupersetOf(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.Contains(@string));
            }
            // test adding terms to the copy
            IList<string> newWords = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                newWords.Add(@string + "_1");
            }
            copy.UnionWith(newWords);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copy.IsSupersetOf(newWords));
            // new added terms are not in the source set
            foreach (string @string in newWords)
            {
                assertFalse(setIgnoreCase.Contains(@string));
                assertFalse(setCaseSensitive.Contains(@string));
            }
        }

        /// <summary>
        /// Test the ToCharArraySet function with a CharArraySet as a source
        /// </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestToCharArray_MatchVersion_IgnoreCase_SetCharArray()
        {
            CharArraySet setIgnoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            setIgnoreCase.UnionWith(TEST_STOP_WORDS);
            setIgnoreCase.Add(Convert.ToInt32(1).ToString());
            setCaseSensitive.UnionWith(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1).ToString());

            CharArraySet copy = setCaseSensitive.ToCharArraySet(TEST_VERSION_CURRENT, ignoreCase: true);
            CharArraySet copyCaseSens = setCaseSensitive.ToCharArraySet(TEST_VERSION_CURRENT, ignoreCase: false);

            assertEquals(setIgnoreCase.Count, copy.Count);
            assertEquals(setCaseSensitive.Count, copy.Count);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copyCaseSens.IsSupersetOf(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.Contains(@string));
            }
            // test adding terms to the copy
            IList<string> newWords = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                newWords.Add(@string + "_1");
            }
            copy.UnionWith(newWords);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copy.IsSupersetOf(newWords));
            // new added terms are not in the source set
            foreach (string @string in newWords)
            {
                assertFalse(setIgnoreCase.Contains(@string));
                assertFalse(setCaseSensitive.Contains(@string));
            }
        }

        /// <summary>
        /// Test the ToCharArraySet function with a .NET <seealso cref="ISet{T}"/> as a source
        /// </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestToCharArray_MatchVersion_SetJDKSet()
        {
            ISet<string> set = new JCG.HashSet<string>();

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            set.UnionWith(TEST_STOP_WORDS);

            CharArraySet copyCaseSens = set.ToCharArraySet(TEST_VERSION_CURRENT, ignoreCase: false);
            CharArraySet copy = stopwordsUpper.ToCharArraySet(TEST_VERSION_CURRENT, ignoreCase: true);

            assertEquals(set.Count, copyCaseSens.Count);
            assertEquals(set.Count, copy.Count);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copyCaseSens.IsSupersetOf(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.Contains(@string));
            }

            IList<string> newWords = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                newWords.Add(@string + "_1");
            }
            copy.UnionWith(newWords);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copy.IsSupersetOf(newWords));
            // new added terms are not in the source set
            foreach (string @string in newWords)
            {
                assertFalse(set.Contains(@string));
                assertFalse(stopwordsUpper.Contains(@string));
            }
        }

        /// <summary>
        /// Test the ToCharArraySet function with a .NET <seealso cref="ISet{T}"/> as a source
        /// </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestToCharArray_MatchVersion_IgnoreCase_SetJDKSet()
        {
            ISet<string> set = new JCG.HashSet<string>();

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            set.UnionWith(TEST_STOP_WORDS);

            CharArraySet copyCaseSens = set.ToCharArraySet(TEST_VERSION_CURRENT, ignoreCase: false);
            CharArraySet copy = stopwordsUpper.ToCharArraySet(TEST_VERSION_CURRENT, ignoreCase: true);

            assertEquals(set.Count, copyCaseSens.Count);
            assertEquals(set.Count, copy.Count);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copyCaseSens.IsSupersetOf(stopwords));
            foreach (string @string in stopwordsUpper)
            {
                assertFalse(copyCaseSens.Contains(@string));
            }

            IList<string> newWords = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                newWords.Add(@string + "_1");
            }
            copy.UnionWith(newWords);

            assertTrue(copy.IsSupersetOf(stopwords));
            assertTrue(copy.IsSupersetOf(stopwordsUpper));
            assertTrue(copy.IsSupersetOf(newWords));
            // new added terms are not in the source set
            foreach (string @string in newWords)
            {
                assertFalse(set.Contains(@string));
                assertFalse(stopwordsUpper.Contains(@string));
            }
        }

        /// <summary>
        /// Tests a special case of <see cref="EnumerableExtensions.ToCharArraySet(IEnumerable{string}, LuceneVersion)"/> where the
        /// set to copy is the <see cref="CharArraySet.Empty"/>
        /// </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestToCharArraySetEmptySet()
        {
            CharArraySet copy = CharArraySet.Empty.ToCharArraySet(TEST_VERSION_CURRENT);
            assertEquals(0, copy.Count);
            assertFalse(copy.IsReadOnly);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestCopyToStringArray()
        {
            var stopwords = new HashSet<string>(TEST_STOP_WORDS, StringComparer.OrdinalIgnoreCase);
            var target = new CharArraySet(TEST_VERSION_CURRENT, stopwords, ignoreCase: false);

            // Full array
            var array1 = new string[target.Count];
            target.CopyTo(array1);
            assertTrue(stopwords.SetEquals(array1));

            // Bounded to lower start index
            int startIndex = 3;
            var array2 = new string[target.Count + startIndex];
            target.CopyTo(array2, startIndex);

            assertNull(array2[0]);
            assertNull(array2[1]);
            assertNull(array2[2]);
            assertTrue(stopwords.IsProperSubsetOf(array2));
            assertTrue(stopwords.SetEquals(array2.Skip(startIndex).ToArray()));

            // Constrianed both start index and count
            startIndex = 5;
            int count = 7;
            var array3 = new string[count + startIndex];
            target.CopyTo(array3, startIndex, count);

            assertNull(array3[0]);
            assertNull(array3[1]);
            assertNull(array3[2]);
            assertNull(array3[3]);
            assertNull(array3[4]);
            assertTrue(stopwords.IsProperSupersetOf(array3.Skip(startIndex).Take(count).ToArray()));
        }

        // LUCENENET: TestCopyToStringCharArray and TestCopyToCharSequence were removed along with the
        // CopyTo(IList<char[]>) and CopyTo(ICharSequence[]) overloads.

        [Test, LuceneNetSpecific]
        public virtual void TestSpanOverloadsMatchStringOverloads()
        {
            var set = new CharArraySet(TEST_VERSION_CURRENT, 4, ignoreCase: true);
            char[] buffer = "xxHelloxx".ToCharArray();

            assertTrue(set.Add(buffer.AsSpan(2, 5)));
            assertFalse(set.Add(buffer.AsSpan(2, 5)));
            assertFalse(set.Add("HELLO"));
            assertTrue(set.Contains("hello"));
            assertTrue(set.Contains(buffer.AsSpan(2, 5)));
            assertFalse(set.Contains(buffer.AsSpan(2, 4)));
            assertFalse(set.Contains(buffer.AsSpan(1, 5)));
            assertEquals(1, set.Count);

            // The stored element is the lowercased copy, not a view over the buffer
            buffer[2] = 'J';
            assertTrue(set.Contains("hello"));
            assertEquals("hello", set.Single());
        }

        #endregion
    }
}
