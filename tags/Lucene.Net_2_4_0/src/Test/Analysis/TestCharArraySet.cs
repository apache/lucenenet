/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
{
	
	[TestFixture]
	public class TestCharArraySet : LuceneTestCase
	{
		[Test]
		public virtual void  TestRehash()
		{
			CharArraySet cas = new CharArraySet(0, true);
			for (int i = 0; i < StopAnalyzer.ENGLISH_STOP_WORDS.Length; i++)
				cas.Add(StopAnalyzer.ENGLISH_STOP_WORDS[i]);
			Assert.AreEqual(StopAnalyzer.ENGLISH_STOP_WORDS.Length, cas.Count);
			for (int i = 0; i < StopAnalyzer.ENGLISH_STOP_WORDS.Length; i++)
				Assert.IsTrue(cas.Contains(StopAnalyzer.ENGLISH_STOP_WORDS[i]));
		}
		
		[Test]
		public virtual void  TestNonZeroOffset()
		{
			System.String[] words = new System.String[]{"Hello", "World", "this", "is", "a", "test"};
			char[] findme = "xthisy".ToCharArray();
			CharArraySet set_Renamed = new CharArraySet(10, true);
			for (int i = 0; i < words.Length; i++) { set_Renamed.Add(words[i]); }
			Assert.IsTrue(set_Renamed.Contains(findme, 1, 4));
			Assert.IsTrue(set_Renamed.Contains(new System.String(findme, 1, 4)));
		}
	}
}