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
            assertTrue(set.Contains(findme, 1, 4));
            assertTrue(set.Contains(new string(findme, 1, 4)));

            // test unmodifiable
            set = set.AsReadOnly();
            assertTrue(set.Contains(findme, 1, 4));
            assertTrue(set.Contains(new string(findme, 1, 4)));
        }

        [Test]
        public virtual void TestObjectContains()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            J2N.Numerics.Int32 val = J2N.Numerics.Int32.GetInstance(1);
            set.Add(val);
            assertTrue(set.Contains(val));
            assertTrue(set.Contains(J2N.Numerics.Int32.GetInstance(1))); // another integer
            assertTrue(set.Contains("1"));
            assertTrue(set.Contains(new char[] { '1' }));
            // test unmodifiable
            set = set.AsReadOnly();
            assertTrue(set.Contains(val));
            assertTrue(set.Contains(J2N.Numerics.Int32.GetInstance(1))); // another integer
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
                set.Add(new StringBuilder(NOT_IN_SET));
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
                ((ISet<string>)set).ExceptWith(new CharArraySet(TEST_VERSION_CURRENT, new [] { NOT_IN_SET }, true));
                fail("Modified unmodifiable set");
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
                assertEquals("Size of unmodifiable set has changed", size, set.size());
            }

            try
            {
                set.UnionWith(new[] { NOT_IN_SET});
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
            set.Add(Convert.ToInt32(1));
            int size = set.size();
            set = set.AsReadOnly();
            assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
            foreach (var stopword in TEST_STOP_WORDS)
            {
                assertTrue(set.Contains(stopword));
            }
            assertTrue(set.Contains(Convert.ToInt32(1)));
            assertTrue(set.Contains("1"));
            assertTrue(set.Contains(new[] { '1' }));

            // LUCENENET specific - we use an instance method, so this is not a valid call
            //try
            //{
            //    CharArraySet.UnmodifiableSet(null);
            //    fail("can not make null unmodifiable");
            //}
            //catch (ArgumentNullException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
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
            CharArraySet setIngoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
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
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
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
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
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
            assertSame(CharArraySet.Empty, CharArraySet.Copy(TEST_VERSION_CURRENT, CharArraySet.Empty));
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
            assertFalse(CharArraySet.Empty.Contains((object)"foo"));
            assertFalse(CharArraySet.Empty.Contains("foo".ToCharArray()));
            assertFalse(CharArraySet.Empty.Contains("foo".ToCharArray(), 0, 3));
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
            catch (ArgumentException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
            {
            }
            try
            {
                set.Contains((ICharSequence)null);
                fail("null value must raise NPE");
            }
            catch (ArgumentException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
            {
            }
            // LUCENENET Specific test for string (since it does not implement ICharSequence)
            try
            {
                set.Contains((string)null);
                fail("null value must raise NPE");
            }
            catch (ArgumentException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
            {
            }
            try
            {
                set.Contains((object)null);
                fail("null value must raise NPE");
            }
            catch (ArgumentException) // NOTE: In .NET we throw an ArgumentExcpetion, not a NullReferenceExeption
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

        [Test, LuceneNetSpecific]
        public virtual void TestSetEqualsObject()
        {
            var originalValues = new object[] { "sally".ToCharArray(), "sells".AsCharSequence(), "seashells".ToCharArray(), "by", "the".AsCharSequence(), "sea".ToCharArray(), "shore" };
            var unequalValues = new object[] { "sally", "sells", "seashells", "by", "the", "sea", "sometimes" };
            var unequalCaseValues = new object[] { "Sally".AsCharSequence(), "Sells".ToCharArray(), "Seashells", "by".ToCharArray(), "the", "Sea".ToCharArray(), "Shore".AsCharSequence() };
            var unequalValueCount = new object[] { "sally".ToCharArray(), "sells".AsCharSequence(), "seashells".ToCharArray(), "by".AsCharSequence(), "the", "strange".AsCharSequence(), "sea", "shore".ToCharArray() };

            TestSetEqualsObject(ignoreCase: false, originalValues, unequalValues, unequalCaseValues, unequalValueCount);
            TestSetEqualsObject(ignoreCase: true, originalValues, unequalValues, unequalCaseValues, unequalValueCount);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestSetEqualsCharArray()
        {
            var originalValues = new List<char[]> { "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var unequalValues = new List<char[]> { "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "sometimes".ToCharArray() };
            var unequalCaseValues = new List<char[]> { "Sally".ToCharArray(), "Sells".ToCharArray(), "Seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "Sea".ToCharArray(), "Shore".ToCharArray() };
            var unequalValueCount = new List<char[]> { "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "strange".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };

            // LUCENENET NOTE: We must test the ignoreCase false case first here, because the true
            // case will actually lowercase our input char arrays.
            TestSetEqualsCharArray(ignoreCase: false, originalValues, unequalValues, unequalCaseValues, unequalValueCount);
            TestSetEqualsCharArray(ignoreCase: true, originalValues, unequalValues, unequalCaseValues, unequalValueCount);
        }

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

        [Test, LuceneNetSpecific]
        public virtual void TestSetEqualsCharSequence()
        {
            var originalValues = new ICharSequence[] { "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence() };
            var unequalValues = new ICharSequence[] { "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "sometimes".AsCharSequence() };
            var unequalCaseValues = new ICharSequence[] { "Sally".AsCharSequence(), "Sells".AsCharSequence(), "Seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "Sea".AsCharSequence(), "Shore".AsCharSequence() };
            var unequalValueCount = new ICharSequence[] { "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "strange".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence() };

            TestSetEqualsCharSequence(ignoreCase: false, originalValues, unequalValues, unequalCaseValues, unequalValueCount);
            TestSetEqualsCharSequence(ignoreCase: true, originalValues, unequalValues, unequalCaseValues, unequalValueCount);
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

        private static CharArraySet CreateCharArraySet<T>(Version matchVersion, IList<T> values, bool ignoreCase)
        {
            var type = typeof(T);
            if (type.Equals(typeof(string)))
            {
                return new CharArraySet(TEST_VERSION_CURRENT, (IList<string>)(object)values, ignoreCase);
            }
            else if (type.Equals(typeof(char[])))
            {
                return new CharArraySet(TEST_VERSION_CURRENT, (IList<char[]>)(object)values, ignoreCase);
            }
            else if (type.Equals(typeof(ICharSequence)))
            {
                return new CharArraySet(TEST_VERSION_CURRENT, (IList<ICharSequence>)(object)values, ignoreCase);
            }

            var result = new CharArraySet(TEST_VERSION_CURRENT, values.Count, ignoreCase);

            foreach (var value in values)
            {
                result.Add(value);
            }

            return result;
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

        public virtual void TestSetEqualsCharArray(bool ignoreCase, IList<char[]> originalValues, IList<char[]> unequalValues, IList<char[]> unequalCaseValues, IList<char[]> unequalValueCount)
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

            var set_Equal = new HashSet<char[]>(originalValuesShuffled);
            var set_EqualWithNull = new HashSet<char[]>(originalValuesShuffled) { null };
            var set_Unequal = new HashSet<char[]>(unequalValues);
            var set_UnequalCase = new HashSet<char[]>(unequalCaseValues);
            var set_UnequalValueCount = new HashSet<char[]>(unequalValueCount);

            assertTrue(target.SetEquals(set_Equal));
            assertFalse(target.SetEquals(set_EqualWithNull));
            assertFalse(target.SetEquals(set_Unequal));
            assertEquals(target.SetEquals(set_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(set_UnequalValueCount));

            var list_Equal = new List<char[]>(originalValuesShuffled);
            var list_EqualWithNull = new List<char[]>(originalValuesShuffled) { null };
            var list_Unequal = new List<char[]>(unequalValues);
            var list_UnequalCase = new List<char[]>(unequalCaseValues);
            var list_UnequalValueCount = new List<char[]>(unequalValueCount);

            assertTrue(target.SetEquals(list_Equal));
            assertFalse(target.SetEquals(list_EqualWithNull));
            assertFalse(target.SetEquals(list_Unequal));
            assertEquals(target.SetEquals(list_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(list_UnequalValueCount));

            var enumerable_Equal = new TestEnumerable<char[]>(list_Equal);
            var enumerable_EqualWithNull = new TestEnumerable<char[]>(list_EqualWithNull);
            var enumerable_Unequal = new TestEnumerable<char[]>(list_Unequal);
            var enumerable_UnequalCase = new TestEnumerable<char[]>(list_UnequalCase);
            var enumerable_UnequalValueCount = new TestEnumerable<char[]>(unequalValueCount);

            assertTrue(target.SetEquals(enumerable_Equal));
            assertFalse(target.SetEquals(enumerable_EqualWithNull));
            assertFalse(target.SetEquals(enumerable_Unequal));
            assertEquals(target.SetEquals(enumerable_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(enumerable_UnequalValueCount));
        }

        public virtual void TestSetEqualsCharSequence(bool ignoreCase, IList<ICharSequence> originalValues, IList<ICharSequence> unequalValues, IList<ICharSequence> unequalCaseValues, IList<ICharSequence> unequalValueCount)
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

            var set_Equal = new HashSet<ICharSequence>(originalValuesShuffled);
            var set_EqualWithNull = new HashSet<ICharSequence>(originalValuesShuffled) { null, new CharArrayCharSequence(null) };
            var set_Unequal = new HashSet<ICharSequence>(unequalValues);
            var set_UnequalCase = new HashSet<ICharSequence>(unequalCaseValues);
            var set_UnequalValueCount = new HashSet<ICharSequence>(unequalValueCount);

            assertTrue(target.SetEquals(set_Equal));
            assertFalse(target.SetEquals(set_EqualWithNull));
            assertFalse(target.SetEquals(set_Unequal));
            assertEquals(target.SetEquals(set_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(set_UnequalValueCount));

            var list_Equal = new List<ICharSequence>(originalValuesShuffled);
            var list_EqualWithNull = new List<ICharSequence>(originalValuesShuffled) { null, new CharArrayCharSequence(null) };
            var list_Unequal = new List<ICharSequence>(unequalValues);
            var list_UnequalCase = new List<ICharSequence>(unequalCaseValues);
            var list_UnequalValueCount = new List<ICharSequence>(unequalValueCount);

            assertTrue(target.SetEquals(list_Equal));
            assertFalse(target.SetEquals(list_EqualWithNull));
            assertFalse(target.SetEquals(list_Unequal));
            assertEquals(target.SetEquals(list_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(list_UnequalValueCount));

            var enumerable_Equal = new TestEnumerable<ICharSequence>(list_Equal);
            var enumerable_EqualWithNull = new TestEnumerable<ICharSequence>(list_EqualWithNull);
            var enumerable_Unequal = new TestEnumerable<ICharSequence>(list_Unequal);
            var enumerable_UnequalCase = new TestEnumerable<ICharSequence>(list_UnequalCase);
            var enumerable_UnequalValueCount = new TestEnumerable<ICharSequence>(unequalValueCount);

            assertTrue(target.SetEquals(enumerable_Equal));
            assertFalse(target.SetEquals(enumerable_EqualWithNull));
            assertFalse(target.SetEquals(enumerable_Unequal));
            assertEquals(target.SetEquals(enumerable_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(enumerable_UnequalValueCount));
        }

        public virtual void TestSetEqualsObject(bool ignoreCase, IList<object> originalValues, IList<object> unequalValues, IList<object> unequalCaseValues, IList<object> unequalValueCount)
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

            var set_Equal = new HashSet<object>(originalValuesShuffled);
            var set_EqualWithNull = new HashSet<object>(originalValuesShuffled) { null };
            var set_Unequal = new HashSet<object>(unequalValues);
            var set_UnequalCase = new HashSet<object>(unequalCaseValues);
            var set_UnequalValueCount = new HashSet<object>(unequalValueCount);

            assertTrue(target.SetEquals(set_Equal));
            assertFalse(target.SetEquals(set_EqualWithNull));
            assertFalse(target.SetEquals(set_Unequal));
            assertEquals(target.SetEquals(set_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(set_UnequalValueCount));

            var list_Equal = new List<object>(originalValuesShuffled);
            var list_EqualWithNull = new List<object>(originalValuesShuffled) { null };
            var list_Unequal = new List<object>(unequalValues);
            var list_UnequalCase = new List<object>(unequalCaseValues);
            var list_UnequalValueCount = new List<object>(unequalValueCount);

            assertTrue(target.SetEquals(list_Equal));
            assertFalse(target.SetEquals(list_EqualWithNull));
            assertFalse(target.SetEquals(list_Unequal));
            assertEquals(target.SetEquals(list_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(list_UnequalValueCount));

            var enumerable_Equal = new TestEnumerable<object>(list_Equal);
            var enumerable_EqualWithNull = new TestEnumerable<object>(list_EqualWithNull);
            var enumerable_Unequal = new TestEnumerable<object>(list_Unequal);
            var enumerable_UnequalCase = new TestEnumerable<object>(list_UnequalCase);
            var enumerable_UnequalValueCount = new TestEnumerable<object>(unequalValueCount);

            assertTrue(target.SetEquals(enumerable_Equal));
            assertFalse(target.SetEquals(enumerable_EqualWithNull));
            assertFalse(target.SetEquals(enumerable_Unequal));
            assertEquals(target.SetEquals(enumerable_UnequalCase), ignoreCase);
            assertFalse(target.SetEquals(enumerable_UnequalValueCount));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUnionWithObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var existingValuesAsObject = new JCG.List<object> { "seashells", "sea", "shore" };
            var mixedExistingNonExistingValuesAsObject = new JCG.List<object> { "true", "set", "of", "unique", "values", "except", "sells" };
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
            var existingValues = new JCG.List<char[]> { "seashells".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var mixedExistingNonExistingValues = new JCG.List<char[]> { "true".ToCharArray(), "set".ToCharArray(), "of".ToCharArray(), "unique".ToCharArray(), "values".ToCharArray(), "except".ToCharArray(), "sells".ToCharArray() };

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
            var existingValues = new JCG.List<string> { "seashells", "sea", "shore" };
            var mixedExistingNonExistingValues = new JCG.List<string> { "true", "set", "of", "unique", "values", "except", "sells" };

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
            var existingValues = new JCG.List<ICharSequence> { new StringCharSequence("seashells"), new StringCharSequence("sea"), new StringCharSequence("shore") };
            var mixedExistingNonExistingValues = new JCG.List<ICharSequence> { new StringCharSequence("true"), new StringCharSequence("set"), new StringCharSequence("of"), new StringCharSequence("unique"), new StringCharSequence("values"), new StringCharSequence("except"), new StringCharSequence("sells") };

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
        public virtual void TestIsSubsetOfCharArray()
        {
            var originalValues = new JCG.List<char[]> { "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var originalValuesCopy = originalValues.Select(x => (char[])x.Clone()).ToList();
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<char[]> { "seashells".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray(), null };
            var superset = new JCG.List<char[]> { "introducing".ToCharArray(), "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray(), "and".ToCharArray(), "more".ToCharArray(), null };

            assertFalse(target.IsSubsetOf(subset));
            assertTrue(target.IsSubsetOf(superset));
            assertTrue(target.IsSubsetOf(originalValuesCopy));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSubsetOfCharSequence()
        {
            var originalValues = new JCG.List<ICharSequence> { "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence() };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<ICharSequence> { "seashells".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence(), null, new CharArrayCharSequence(null) };
            var superset = new JCG.List<ICharSequence> { "introducing".AsCharSequence(), "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence(), "and".AsCharSequence(), "more".AsCharSequence(), null, new CharArrayCharSequence(null) };

            assertFalse(target.IsSubsetOf(subset));
            assertTrue(target.IsSubsetOf(superset));
            assertTrue(target.IsSubsetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSubsetOfObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<object> { "seashells", "sea", "shore", null };
            var superset = new JCG.List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

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
        public virtual void TestIsProperSubsetOfCharArray()
        {
            var originalValues = new JCG.List<char[]> { "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var originalValuesCopy = originalValues.Select(x => (char[])x.Clone()).ToList();
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<char[]> { "seashells".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray(), null };
            var superset = new JCG.List<char[]> { "introducing".ToCharArray(), "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray(), "and".ToCharArray(), "more".ToCharArray(), null };

            assertFalse(target.IsProperSubsetOf(subset));
            assertTrue(target.IsProperSubsetOf(superset));
            assertFalse(target.IsProperSubsetOf(originalValuesCopy));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSubsetOfCharSequence()
        {
            var originalValues = new JCG.List<ICharSequence> { "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence() };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<ICharSequence> { "seashells".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence(), null, new CharArrayCharSequence(null) };
            var superset = new JCG.List<ICharSequence> { "introducing".AsCharSequence(), "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence(), "and".AsCharSequence(), "more".AsCharSequence(), null, new CharArrayCharSequence(null) };

            assertFalse(target.IsProperSubsetOf(subset));
            assertTrue(target.IsProperSubsetOf(superset));
            assertFalse(target.IsProperSubsetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSubsetOfObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<object> { "seashells", "sea", "shore", null };
            var superset = new JCG.List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

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
        public virtual void TestIsSupersetOfCharArray()
        {
            var originalValues = new JCG.List<char[]> { "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var originalValuesCopy = originalValues.Select(x => (char[])x.Clone()).ToList();
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<char[]> { "seashells".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var superset = new JCG.List<char[]> { "introducing".ToCharArray(), "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray(), "and".ToCharArray(), "more".ToCharArray(), null };

            assertTrue(target.IsSupersetOf(subset));
            assertFalse(target.IsSupersetOf(superset));
            assertTrue(target.IsSupersetOf(originalValuesCopy));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSupersetOfCharSequence()
        {
            var originalValues = new JCG.List<ICharSequence> { "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence() };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<ICharSequence> { "seashells".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence() };
            var superset = new JCG.List<ICharSequence> { "introducing".AsCharSequence(), "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence(), "and".AsCharSequence(), "more".AsCharSequence(), null, new CharArrayCharSequence(null) };

            assertTrue(target.IsSupersetOf(subset));
            assertFalse(target.IsSupersetOf(superset));
            assertTrue(target.IsSupersetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsSupersetOfObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<object> { "seashells", "sea", "shore" };
            var superset = new JCG.List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

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
        public virtual void TestIsProperSupersetOfCharArray()
        {
            var originalValues = new JCG.List<char[]> { "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var originalValuesCopy = originalValues.Select(x => (char[])x.Clone()).ToList();
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<char[]> { "seashells".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray() };
            var superset = new JCG.List<char[]> { "introducing".ToCharArray(), "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray(), "and".ToCharArray(), "more".ToCharArray(), null };

            assertTrue(target.IsProperSupersetOf(subset));
            assertFalse(target.IsProperSupersetOf(superset));
            assertFalse(target.IsProperSupersetOf(originalValuesCopy));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSupersetOfCharSequence()
        {
            var originalValues = new JCG.List<ICharSequence> { "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence() };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<ICharSequence> { "seashells".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence() };
            var superset = new JCG.List<ICharSequence> { "introducing".AsCharSequence(), "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence(), "and".AsCharSequence(), "more".AsCharSequence(), null, new CharArrayCharSequence(null) };

            assertTrue(target.IsProperSupersetOf(subset));
            assertFalse(target.IsProperSupersetOf(superset));
            assertFalse(target.IsProperSupersetOf(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsProperSupersetOfObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var subset = new JCG.List<object> { "seashells", "sea", "shore" };
            var superset = new JCG.List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

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
        public virtual void TestOverlapsCharArray()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var nonOverlapping = new JCG.List<char[]> { "peter".ToCharArray(), "piper".ToCharArray(), "picks".ToCharArray(), "a".ToCharArray(), "pack".ToCharArray(), "of".ToCharArray(), "pickled".ToCharArray(), "peppers".ToCharArray(), null };
            var overlapping = new JCG.List<char[]> { "introducing".ToCharArray(), "sally".ToCharArray(), "sells".ToCharArray(), "seashells".ToCharArray(), "by".ToCharArray(), "the".ToCharArray(), "sea".ToCharArray(), "shore".ToCharArray(), "and".ToCharArray(), "more".ToCharArray(), null };

            assertFalse(target.Overlaps(nonOverlapping));
            assertTrue(target.Overlaps(overlapping));
            assertTrue(target.Overlaps(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestOverlapsCharSequence()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var nonOverlapping = new JCG.List<ICharSequence> { "peter".AsCharSequence(), "piper".AsCharSequence(), "picks".AsCharSequence(), "a".AsCharSequence(), "pack".AsCharSequence(), "of".AsCharSequence(), "pickled".AsCharSequence(), "peppers".AsCharSequence(), null, new CharArrayCharSequence(null) };
            var overlapping = new JCG.List<ICharSequence> { "introducing".AsCharSequence(), "sally".AsCharSequence(), "sells".AsCharSequence(), "seashells".AsCharSequence(), "by".AsCharSequence(), "the".AsCharSequence(), "sea".AsCharSequence(), "shore".AsCharSequence(), "and".AsCharSequence(), "more".AsCharSequence(), null, new CharArrayCharSequence(null) };

            assertFalse(target.Overlaps(nonOverlapping));
            assertTrue(target.Overlaps(overlapping));
            assertTrue(target.Overlaps(originalValues));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestOverlapsObject()
        {
            var originalValues = new string[] { "sally", "sells", "seashells", "by", "the", "sea", "shore" };
            CharArraySet target = new CharArraySet(TEST_VERSION_CURRENT, originalValues, false);
            var nonOverlapping = new JCG.List<object> { "peter", "piper", "picks", "a", "pack", "of", "pickled", "peppers", null };
            var overlapping = new JCG.List<object> { "introducing", "sally", "sells", "seashells", "by", "the", "sea", "shore", "and", "more", null };

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
            CharArraySet setIngoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            setIngoreCase.UnionWith(TEST_STOP_WORDS);
            setIngoreCase.Add(Convert.ToInt32(1));
            setCaseSensitive.UnionWith(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1));

            CharArraySet copy = setIngoreCase.ToCharArraySet(TEST_VERSION_CURRENT);
            CharArraySet copyCaseSens = setCaseSensitive.ToCharArraySet(TEST_VERSION_CURRENT);

            assertEquals(setIngoreCase.Count, copy.Count);
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
                assertFalse(setIngoreCase.Contains(@string));
                assertFalse(setCaseSensitive.Contains(@string));

            }
        }

        /// <summary>
        /// Test the ToCharArraySet function with a CharArraySet as a source
        /// </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestToCharArraySet_CharArraySet()
        {
            CharArraySet setIngoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            setIngoreCase.UnionWith(TEST_STOP_WORDS);
            setIngoreCase.Add(Convert.ToInt32(1));
            setCaseSensitive.UnionWith(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1));

            CharArraySet copy = CharArraySet.Copy(TEST_VERSION_CURRENT, setIngoreCase);
            CharArraySet copyCaseSens = CharArraySet.Copy(TEST_VERSION_CURRENT, setCaseSensitive);

            assertEquals(setIngoreCase.Count, copy.Count);
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
                assertFalse(setIngoreCase.Contains(@string));
                assertFalse(setCaseSensitive.Contains(@string));
            }
        }

        /// <summary>
        /// Test the ToCharArraySet function with a CharArraySet as a source
        /// </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestToCharArray_MatchVersion_IgnoreCase_SetCharArray()
        {
            CharArraySet setIngoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
            CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

            IList<string> stopwords = TEST_STOP_WORDS;
            IList<string> stopwordsUpper = new JCG.List<string>();
            foreach (string @string in stopwords)
            {
                stopwordsUpper.Add(@string.ToUpperInvariant());
            }
            setIngoreCase.UnionWith(TEST_STOP_WORDS);
            setIngoreCase.Add(Convert.ToInt32(1));
            setCaseSensitive.UnionWith(TEST_STOP_WORDS);
            setCaseSensitive.Add(Convert.ToInt32(1));

            CharArraySet copy = setCaseSensitive.ToCharArraySet(TEST_VERSION_CURRENT, ignoreCase: true);
            CharArraySet copyCaseSens = setCaseSensitive.ToCharArraySet(TEST_VERSION_CURRENT, ignoreCase: false);

            assertEquals(setIngoreCase.Count, copy.Count);
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
                assertFalse(setIngoreCase.Contains(@string));
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
        /// Tests a special case of <see cref="CharArraySet.ToCharArraySet()"/> where the
        /// set to copy is the <see cref="CharArraySet.Empty"/>
        /// </summary>
        [Test, LuceneNetSpecific]
        public virtual void TestToCharArraySetEmptySet()
        {
            assertSame(CharArraySet.Empty, CharArraySet.Empty.ToCharArraySet(TEST_VERSION_CURRENT));
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

        [Test, LuceneNetSpecific]
        public virtual void TestCopyToStringCharArray()
        {
            var stopwords = new JCG.HashSet<char[]>(TEST_STOP_WORDS.Select(x => x.ToCharArray()));
            var target = new CharArraySet(TEST_VERSION_CURRENT, stopwords, ignoreCase: false);

            // Full array
            var array1 = new char[target.Count][];
            target.CopyTo(array1);
            assertTrue(target.SetEquals(array1));

            // Bounded to lower start index
            int startIndex = 3;
            var array2 = new char[target.Count + startIndex][];
            target.CopyTo(array2, startIndex);

            assertNull(array2[0]);
            assertNull(array2[1]);
            assertNull(array2[2]);
            assertTrue(target.IsProperSubsetOf(array2));
            assertTrue(target.SetEquals(array2.Skip(startIndex).ToArray()));

            // Constrianed both start index and count
            startIndex = 5;
            int count = 7;
            var array3 = new char[count + startIndex][];
            target.CopyTo(array3, startIndex, count);

            assertNull(array3[0]);
            assertNull(array3[1]);
            assertNull(array3[2]);
            assertNull(array3[3]);
            assertNull(array3[4]);
            assertTrue(target.IsProperSupersetOf(array3.Skip(startIndex).Take(count).ToArray()));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestCopyToCharSequence()
        {
            var stopwords = new HashSet<ICharSequence>(TEST_STOP_WORDS.Select(x => x.AsCharSequence()));
            var target = new CharArraySet(TEST_VERSION_CURRENT, stopwords, ignoreCase: false);

            // Full array
            var array1 = new ICharSequence[target.Count];
            target.CopyTo(array1);
            assertTrue(stopwords.SetEquals(array1));

            // Bounded to lower start index
            int startIndex = 3;
            var array2 = new ICharSequence[target.Count + startIndex];
            target.CopyTo(array2, startIndex);

            assertNull(array2[0]);
            assertNull(array2[1]);
            assertNull(array2[2]);
            assertTrue(stopwords.IsProperSubsetOf(array2));
            assertTrue(stopwords.SetEquals(array2.Skip(startIndex).ToArray()));

            // Constrianed both start index and count
            startIndex = 5;
            int count = 7;
            var array3 = new ICharSequence[count + startIndex];
            target.CopyTo(array3, startIndex, count);

            assertNull(array3[0]);
            assertNull(array3[1]);
            assertNull(array3[2]);
            assertNull(array3[3]);
            assertNull(array3[4]);
            assertTrue(stopwords.IsProperSupersetOf(array3.Skip(startIndex).Take(count).ToArray()));
        }

        #endregion
    }
}