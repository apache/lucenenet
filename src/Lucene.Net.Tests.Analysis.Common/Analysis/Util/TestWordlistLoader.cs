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
using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Analysis.Common.Analysis.Util
{
	[TestFixture]
	public class TestWordlistLoader : LuceneTestCase
	{
		[Test]
		public virtual void TestWordlistLoading()
		{
			var s = "ONE\n  two \nthree";

			var wordSet1 = WordlistLoader.GetWordSet(new StringReader(s), LuceneTestCase.TEST_VERSION_CURRENT);
			CheckSet(wordSet1);

			//var wordSet2 = WordlistLoader.GetWordSet(new StreamReader(new StringReader(s)), LuceneTestCase.TEST_VERSION_CURRENT);
			//this.CheckSet(wordSet2);
		}

		[Test]
		public virtual void TestComments()
		{
			var s = "ONE\n  two \nthree\n#comment";

			var wordSet1 = WordlistLoader.GetWordSet(new StringReader(s), "#", LuceneTestCase.TEST_VERSION_CURRENT);
			CheckSet(wordSet1);

			assertFalse(wordSet1.contains("#comment"));
			assertFalse(wordSet1.contains("comment"));
		}
		
		private static void CheckSet(CharArraySet wordset)
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
		[Test]
		public virtual void TestSnowballListLoading()
		{
			//multiple stopwords + comment -  multiple stopwords -  stopword with leading/trailing space -  stopword, in uppercase -  commented line with comment -  line with only whitespace -  blank line -  commented line with leading whitespace -  commented line
			var s = "|comment\n" + " |comment\n" + "\n" + "  \t\n" + " |comment | comment\n" + "ONE\n" + "   two   \n" + " three   four five \n" + "six seven | comment\n";

			var wordset = WordlistLoader.GetSnowballWordSet(new StringReader(s), LuceneTestCase.TEST_VERSION_CURRENT);

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