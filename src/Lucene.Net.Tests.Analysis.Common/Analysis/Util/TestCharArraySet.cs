using System;
using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.analysis.util
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

	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using Version = org.apache.lucene.util.Version;


	public class TestCharArraySet : LuceneTestCase
	{

	  internal static readonly string[] TEST_STOP_WORDS = new string[] {"a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with"};


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRehash() throws Exception
	  public virtual void testRehash()
	  {
		CharArraySet cas = new CharArraySet(TEST_VERSION_CURRENT, 0, true);
		for (int i = 0;i < TEST_STOP_WORDS.Length;i++)
		{
		  cas.add(TEST_STOP_WORDS[i]);
		}
		assertEquals(TEST_STOP_WORDS.Length, cas.size());
		for (int i = 0;i < TEST_STOP_WORDS.Length;i++)
		{
		  assertTrue(cas.contains(TEST_STOP_WORDS[i]));
		}
	  }

	  public virtual void testNonZeroOffset()
	  {
		string[] words = new string[] {"Hello","World","this","is","a","test"};
		char[] findme = "xthisy".ToCharArray();
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
		set.addAll(words);
		assertTrue(set.contains(findme, 1, 4));
		assertTrue(set.contains(new string(findme,1,4)));

		// test unmodifiable
		set = CharArraySet.unmodifiableSet(set);
		assertTrue(set.contains(findme, 1, 4));
		assertTrue(set.contains(new string(findme,1,4)));
	  }

	  public virtual void testObjectContains()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
		int? val = Convert.ToInt32(1);
		set.add(val);
		assertTrue(set.contains(val));
		assertTrue(set.contains(new int?(1))); // another integer
		assertTrue(set.contains("1"));
		assertTrue(set.contains(new char[]{'1'}));
		// test unmodifiable
		set = CharArraySet.unmodifiableSet(set);
		assertTrue(set.contains(val));
		assertTrue(set.contains(new int?(1))); // another integer
		assertTrue(set.contains("1"));
		assertTrue(set.contains(new char[]{'1'}));
	  }

	  public virtual void testClear()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10,true);
		set.addAll(TEST_STOP_WORDS);
		assertEquals("Not all words added", TEST_STOP_WORDS.Length, set.size());
		set.clear();
		assertEquals("not empty", 0, set.size());
		for (int i = 0;i < TEST_STOP_WORDS.Length;i++)
		{
		  assertFalse(set.contains(TEST_STOP_WORDS[i]));
		}
		set.addAll(TEST_STOP_WORDS);
		assertEquals("Not all words added", TEST_STOP_WORDS.Length, set.size());
		for (int i = 0;i < TEST_STOP_WORDS.Length;i++)
		{
		  assertTrue(set.contains(TEST_STOP_WORDS[i]));
		}
	  }

	  public virtual void testModifyOnUnmodifiable()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
		set.addAll(TEST_STOP_WORDS);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = set.size();
		int size = set.size();
		set = CharArraySet.unmodifiableSet(set);
		assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
		string NOT_IN_SET = "SirGallahad";
		assertFalse("Test String already exists in set", set.contains(NOT_IN_SET));

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
		  set.add((object) NOT_IN_SET);
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
		  set.retainAll(new CharArraySet(TEST_VERSION_CURRENT, NOT_IN_SET, true));
		  fail("Modified unmodifiable set");
		}
		catch (System.NotSupportedException)
		{
		  // expected
		  assertEquals("Size of unmodifiable set has changed", size, set.size());
		}

		try
		{
		  set.addAll(NOT_IN_SET);
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

	  public virtual void testUnmodifiableSet()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 10,true);
		set.addAll(TEST_STOP_WORDS);
		set.add(Convert.ToInt32(1));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = set.size();
		int size = set.size();
		set = CharArraySet.unmodifiableSet(set);
		assertEquals("Set size changed due to unmodifiableSet call", size, set.size());
		foreach (string stopword in TEST_STOP_WORDS)
		{
		  assertTrue(set.contains(stopword));
		}
		assertTrue(set.contains(Convert.ToInt32(1)));
		assertTrue(set.contains("1"));
		assertTrue(set.contains(new char[]{'1'}));

		try
		{
		  CharArraySet.unmodifiableSet(null);
		  fail("can not make null unmodifiable");
		}
		catch (System.NullReferenceException)
		{
		  // expected
		}
	  }

	  public virtual void testSupplementaryChars()
	  {
		string missing = "Term %s is missing in the set";
		string falsePos = "Term %s is in the set but shouldn't";
		// for reference see
		// http://unicode.org/cldr/utility/list-unicodeset.jsp?a=[[%3ACase_Sensitive%3DTrue%3A]%26[^[\u0000-\uFFFF]]]&esc=on
		string[] upperArr = new string[] {"Abc\ud801\udc1c", "\ud801\udc1c\ud801\udc1cCDE", "A\ud801\udc1cB"};
		string[] lowerArr = new string[] {"abc\ud801\udc44", "\ud801\udc44\ud801\udc44cde", "a\ud801\udc44b"};
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, true);
		foreach (string upper in upperArr)
		{
		  set.add(upper);
		}
		for (int i = 0; i < upperArr.Length; i++)
		{
		  assertTrue(string.format(Locale.ROOT, missing, upperArr[i]), set.contains(upperArr[i]));
		  assertTrue(string.format(Locale.ROOT, missing, lowerArr[i]), set.contains(lowerArr[i]));
		}
		set = new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, false);
		foreach (string upper in upperArr)
		{
		  set.add(upper);
		}
		for (int i = 0; i < upperArr.Length; i++)
		{
		  assertTrue(string.format(Locale.ROOT, missing, upperArr[i]), set.contains(upperArr[i]));
		  assertFalse(string.format(Locale.ROOT, falsePos, lowerArr[i]), set.contains(lowerArr[i]));
		}
	  }

	  public virtual void testSingleHighSurrogate()
	  {
		string missing = "Term %s is missing in the set";
		string falsePos = "Term %s is in the set but shouldn't";
		string[] upperArr = new string[] {"ABC\uD800", "ABC\uD800EfG", "\uD800EfG", "\uD800\ud801\udc1cB"};

		string[] lowerArr = new string[] {"abc\uD800", "abc\uD800efg", "\uD800efg", "\uD800\ud801\udc44b"};
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, true);
		foreach (string upper in upperArr)
		{
		  set.add(upper);
		}
		for (int i = 0; i < upperArr.Length; i++)
		{
		  assertTrue(string.format(Locale.ROOT, missing, upperArr[i]), set.contains(upperArr[i]));
		  assertTrue(string.format(Locale.ROOT, missing, lowerArr[i]), set.contains(lowerArr[i]));
		}
		set = new CharArraySet(TEST_VERSION_CURRENT, TEST_STOP_WORDS, false);
		foreach (string upper in upperArr)
		{
		  set.add(upper);
		}
		for (int i = 0; i < upperArr.Length; i++)
		{
		  assertTrue(string.format(Locale.ROOT, missing, upperArr[i]), set.contains(upperArr[i]));
		  assertFalse(string.format(Locale.ROOT, falsePos, upperArr[i]), set.contains(lowerArr[i]));
		}
	  }

	  /// @deprecated (3.1) remove this test when lucene 3.0 "broken unicode 4" support is
	  ///             no longer needed. 
	  [Obsolete("(3.1) remove this test when lucene 3.0 "broken unicode 4" support is")]
	  public virtual void testSupplementaryCharsBWCompat()
	  {
		string missing = "Term %s is missing in the set";
		string falsePos = "Term %s is in the set but shouldn't";
		// for reference see
		// http://unicode.org/cldr/utility/list-unicodeset.jsp?a=[[%3ACase_Sensitive%3DTrue%3A]%26[^[\u0000-\uFFFF]]]&esc=on
		string[] upperArr = new string[] {"Abc\ud801\udc1c", "\ud801\udc1c\ud801\udc1cCDE", "A\ud801\udc1cB"};
		string[] lowerArr = new string[] {"abc\ud801\udc44", "\ud801\udc44\ud801\udc44cde", "a\ud801\udc44b"};
		CharArraySet set = new CharArraySet(Version.LUCENE_30, TEST_STOP_WORDS, true);
		foreach (string upper in upperArr)
		{
		  set.add(upper);
		}
		for (int i = 0; i < upperArr.Length; i++)
		{
		  assertTrue(string.format(Locale.ROOT, missing, upperArr[i]), set.contains(upperArr[i]));
		  assertFalse(string.format(Locale.ROOT, falsePos, lowerArr[i]), set.contains(lowerArr[i]));
		}
		set = new CharArraySet(Version.LUCENE_30, TEST_STOP_WORDS, false);
		foreach (string upper in upperArr)
		{
		  set.add(upper);
		}
		for (int i = 0; i < upperArr.Length; i++)
		{
		  assertTrue(string.format(Locale.ROOT,missing, upperArr[i]), set.contains(upperArr[i]));
		  assertFalse(string.format(Locale.ROOT, falsePos, lowerArr[i]), set.contains(lowerArr[i]));
		}
	  }

	  /// @deprecated (3.1) remove this test when lucene 3.0 "broken unicode 4" support is
	  ///             no longer needed. 
	  [Obsolete("(3.1) remove this test when lucene 3.0 "broken unicode 4" support is")]
	  public virtual void testSingleHighSurrogateBWComapt()
	  {
		string missing = "Term %s is missing in the set";
		string falsePos = "Term %s is in the set but shouldn't";
		string[] upperArr = new string[] {"ABC\uD800", "ABC\uD800EfG", "\uD800EfG", "\uD800\ud801\udc1cB"};

		string[] lowerArr = new string[] {"abc\uD800", "abc\uD800efg", "\uD800efg", "\uD800\ud801\udc44b"};
		CharArraySet set = new CharArraySet(Version.LUCENE_30, TEST_STOP_WORDS, true);
		foreach (string upper in upperArr)
		{
		  set.add(upper);
		}
		for (int i = 0; i < upperArr.Length; i++)
		{
		  assertTrue(string.format(Locale.ROOT, missing, upperArr[i]), set.contains(upperArr[i]));
		  if (i == lowerArr.Length - 1)
		  {
			assertFalse(string.format(Locale.ROOT, falsePos, lowerArr[i]), set.contains(lowerArr[i]));
		  }
		  else
		  {
			assertTrue(string.format(Locale.ROOT, missing, lowerArr[i]), set.contains(lowerArr[i]));
		  }
		}
		set = new CharArraySet(Version.LUCENE_30, TEST_STOP_WORDS, false);
		foreach (string upper in upperArr)
		{
		  set.add(upper);
		}
		for (int i = 0; i < upperArr.Length; i++)
		{
		  assertTrue(string.format(Locale.ROOT, missing, upperArr[i]), set.contains(upperArr[i]));
		  assertFalse(string.format(Locale.ROOT, falsePos, lowerArr[i]), set.contains(lowerArr[i]));
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("deprecated") public void testCopyCharArraySetBWCompat()
	  public virtual void testCopyCharArraySetBWCompat()
	  {
		CharArraySet setIngoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
		CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

		IList<string> stopwords = TEST_STOP_WORDS;
		IList<string> stopwordsUpper = new List<string>();
		foreach (string @string in stopwords)
		{
		  stopwordsUpper.Add(@string.ToUpper(Locale.ROOT));
		}
		setIngoreCase.addAll(TEST_STOP_WORDS);
		setIngoreCase.add(Convert.ToInt32(1));
		setCaseSensitive.addAll(TEST_STOP_WORDS);
		setCaseSensitive.add(Convert.ToInt32(1));

		CharArraySet copy = CharArraySet.copy(TEST_VERSION_CURRENT, setIngoreCase);
		CharArraySet copyCaseSens = CharArraySet.copy(TEST_VERSION_CURRENT, setCaseSensitive);

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
	  public virtual void testCopyCharArraySet()
	  {
		CharArraySet setIngoreCase = new CharArraySet(TEST_VERSION_CURRENT, 10, true);
		CharArraySet setCaseSensitive = new CharArraySet(TEST_VERSION_CURRENT, 10, false);

		IList<string> stopwords = TEST_STOP_WORDS;
		IList<string> stopwordsUpper = new List<string>();
		foreach (string @string in stopwords)
		{
		  stopwordsUpper.Add(@string.ToUpper(Locale.ROOT));
		}
		setIngoreCase.addAll(TEST_STOP_WORDS);
		setIngoreCase.add(Convert.ToInt32(1));
		setCaseSensitive.addAll(TEST_STOP_WORDS);
		setCaseSensitive.add(Convert.ToInt32(1));

		CharArraySet copy = CharArraySet.copy(TEST_VERSION_CURRENT, setIngoreCase);
		CharArraySet copyCaseSens = CharArraySet.copy(TEST_VERSION_CURRENT, setCaseSensitive);

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
	  public virtual void testCopyJDKSet()
	  {
		ISet<string> set = new HashSet<string>();

		IList<string> stopwords = TEST_STOP_WORDS;
		IList<string> stopwordsUpper = new List<string>();
		foreach (string @string in stopwords)
		{
		  stopwordsUpper.Add(@string.ToUpper(Locale.ROOT));
		}
		set.addAll(TEST_STOP_WORDS);

		CharArraySet copy = CharArraySet.copy(TEST_VERSION_CURRENT, set);

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
	  public virtual void testCopyEmptySet()
	  {
		assertSame(CharArraySet.EMPTY_SET, CharArraySet.copy(TEST_VERSION_CURRENT, CharArraySet.EMPTY_SET));
	  }

	  /// <summary>
	  /// Smoketests the static empty set
	  /// </summary>
	  public virtual void testEmptySet()
	  {
		assertEquals(0, CharArraySet.EMPTY_SET.size());

		assertTrue(CharArraySet.EMPTY_SET.Empty);
		foreach (string stopword in TEST_STOP_WORDS)
		{
		  assertFalse(CharArraySet.EMPTY_SET.contains(stopword));
		}
		assertFalse(CharArraySet.EMPTY_SET.contains("foo"));
		assertFalse(CharArraySet.EMPTY_SET.contains((object) "foo"));
		assertFalse(CharArraySet.EMPTY_SET.contains("foo".ToCharArray()));
		assertFalse(CharArraySet.EMPTY_SET.contains("foo".ToCharArray(),0,3));
	  }

	  /// <summary>
	  /// Test for NPE
	  /// </summary>
	  public virtual void testContainsWithNull()
	  {
		CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
		try
		{
		  set.contains((char[]) null, 0, 10);
		  fail("null value must raise NPE");
		}
		catch (System.NullReferenceException)
		{
		}
		try
		{
		  set.contains((CharSequence) null);
		  fail("null value must raise NPE");
		}
		catch (System.NullReferenceException)
		{
		}
		try
		{
		  set.contains((object) null);
		  fail("null value must raise NPE");
		}
		catch (System.NullReferenceException)
		{
		}
	  }

	  public virtual void testToString()
	  {
		CharArraySet set = CharArraySet.copy(TEST_VERSION_CURRENT, Collections.singleton("test"));
		assertEquals("[test]", set.ToString());
		set.add("test2");
		assertTrue(set.ToString().Contains(", "));

		set = CharArraySet.copy(Version.LUCENE_30, Collections.singleton("test"));
		assertEquals("[test]", set.ToString());
		set.add("test2");
		assertTrue(set.ToString().Contains(", "));
	  }
	}

}