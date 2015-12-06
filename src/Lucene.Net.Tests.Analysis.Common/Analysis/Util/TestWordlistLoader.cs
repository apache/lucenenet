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

	public class TestWordlistLoader : LuceneTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWordlistLoading() throws java.io.IOException
	  public virtual void testWordlistLoading()
	  {
		string s = "ONE\n  two \nthree";
		CharArraySet wordSet1 = WordlistLoader.getWordSet(new StringReader(s), TEST_VERSION_CURRENT);
		checkSet(wordSet1);
		CharArraySet wordSet2 = WordlistLoader.getWordSet(new System.IO.StreamReader(new StringReader(s)), TEST_VERSION_CURRENT);
		checkSet(wordSet2);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComments() throws Exception
	  public virtual void testComments()
	  {
		string s = "ONE\n  two \nthree\n#comment";
		CharArraySet wordSet1 = WordlistLoader.getWordSet(new StringReader(s), "#", TEST_VERSION_CURRENT);
		checkSet(wordSet1);
		assertFalse(wordSet1.contains("#comment"));
		assertFalse(wordSet1.contains("comment"));
	  }


	  private void checkSet(CharArraySet wordset)
	  {
		assertEquals(3, wordset.size());
		assertTrue(wordset.contains("ONE")); // case is not modified
		assertTrue(wordset.contains("two")); // surrounding whitespace is removed
		assertTrue(wordset.contains("three"));
		assertFalse(wordset.contains("four"));
	  }

	  /// <summary>
	  /// Test stopwords in snowball format
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testSnowballListLoading() throws java.io.IOException
	  public virtual void testSnowballListLoading()
	  {
		string s = "|comment\n" + " |comment\n" + "\n" + "  \t\n" + " |comment | comment\n" + "ONE\n" + "   two   \n" + " three   four five \n" + "six seven | comment\n"; //multiple stopwords + comment -  multiple stopwords -  stopword with leading/trailing space -  stopword, in uppercase -  commented line with comment -  line with only whitespace -  blank line -  commented line with leading whitespace -  commented line
		CharArraySet wordset = WordlistLoader.getSnowballWordSet(new StringReader(s), TEST_VERSION_CURRENT);
		assertEquals(7, wordset.size());
		assertTrue(wordset.contains("ONE"));
		assertTrue(wordset.contains("two"));
		assertTrue(wordset.contains("three"));
		assertTrue(wordset.contains("four"));
		assertTrue(wordset.contains("five"));
		assertTrue(wordset.contains("six"));
		assertTrue(wordset.contains("seven"));
	  }
	}

}