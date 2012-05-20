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
using System.Collections.Generic;
using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
{
	
    [TestFixture]
	public class TestCharArraySet:LuceneTestCase
	{
		
		internal static readonly System.String[] TEST_STOP_WORDS = new System.String[]{"a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with"};
		
		
        [Test]
		public virtual void  TestRehash()
		{
			CharArraySet cas = new CharArraySet(0, true);
			for (int i = 0; i < TEST_STOP_WORDS.Length; i++)
				cas.Add(TEST_STOP_WORDS[i]);
			Assert.AreEqual(TEST_STOP_WORDS.Length, cas.Count);
			for (int i = 0; i < TEST_STOP_WORDS.Length; i++)
				Assert.IsTrue(cas.Contains(TEST_STOP_WORDS[i]));
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
			
			// test unmodifiable
			set_Renamed = CharArraySet.UnmodifiableSet(set_Renamed);
			Assert.IsTrue(set_Renamed.Contains(findme, 1, 4));
			Assert.IsTrue(set_Renamed.Contains(new System.String(findme, 1, 4)));
		}
		
        [Test]
		public virtual void  TestObjectContains()
		{
			CharArraySet set_Renamed = new CharArraySet(10, true);
			System.Int32 val = 1;
			set_Renamed.Add(val);
			Assert.IsTrue(set_Renamed.Contains(val));
			Assert.IsTrue(set_Renamed.Contains(1));
			// test unmodifiable
			set_Renamed = CharArraySet.UnmodifiableSet(set_Renamed);
			Assert.IsTrue(set_Renamed.Contains(val));
			Assert.IsTrue(set_Renamed.Contains(1));
		}
		
        [Test]
		public virtual void  TestClear()
		{
			var set = new CharArraySet(10, true);
			for (int i = 0; i < TEST_STOP_WORDS.Length; i++) { set.Add(TEST_STOP_WORDS[i]); }
			Assert.AreEqual(TEST_STOP_WORDS.Length, set.Count, "Not all words added");

            Assert.Throws<NotSupportedException>(set.Clear, "remove is not supported");
			Assert.AreEqual(TEST_STOP_WORDS.Length, set.Count, "Not all words added");
		}
		
        [Test]
		public virtual void  TestModifyOnUnmodifiable()
		{
            //System.Diagnostics.Debugger.Break();
            CharArraySet set = new CharArraySet(10, true);
			set.AddAll(TEST_STOP_WORDS);
			int size = set.Count;
			set = CharArraySet.UnmodifiableSet(set);

			Assert.AreEqual(size, set.Count, "Set size changed due to UnmodifiableSet call");
			System.String NOT_IN_SET = "SirGallahad";
			Assert.IsFalse(set.Contains(NOT_IN_SET), "Test String already exists in set");
			
            Assert.Throws<NotSupportedException>(() => set.Add(NOT_IN_SET.ToCharArray()), "Modified unmodifiable set");
			Assert.IsFalse(set.Contains(NOT_IN_SET), "Test String has been added to unmodifiable set");
			Assert.AreEqual(size, set.Count, "Size of unmodifiable set has changed");
			
            Assert.Throws<NotSupportedException>(() => set.Add(NOT_IN_SET), "Modified unmodifiable set");
			Assert.IsFalse(set.Contains(NOT_IN_SET), "Test String has been added to unmodifiable set");
			Assert.AreEqual(size, set.Count, "Size of unmodifiable set has changed");
			
            Assert.Throws<NotSupportedException>(() => set.Add(new System.Text.StringBuilder(NOT_IN_SET)), "Modified unmodifiable set");
			Assert.IsFalse(set.Contains(NOT_IN_SET), "Test String has been added to unmodifiable set");
			Assert.AreEqual(size, set.Count, "Size of unmodifiable set has changed");
			
            Assert.Throws<NotSupportedException>(() => set.Clear(), "Modified unmodifiable set");
			Assert.IsFalse(set.Contains(NOT_IN_SET), "Changed unmodifiable set");
			Assert.AreEqual(size, set.Count, "Size of unmodifiable set has changed");

            Assert.Throws<NotSupportedException>(() => set.Add((object)NOT_IN_SET), "Modified unmodifiable set");
			Assert.IsFalse(set.Contains(NOT_IN_SET), "Test String has been added to unmodifiable set");
			Assert.AreEqual(size, set.Count, "Size of unmodifiable set has changed");

            Assert.Throws<NotSupportedException>(() => set.RemoveAll(new List<string>(TEST_STOP_WORDS)), "Modified unmodifiable set");
			Assert.AreEqual(size, set.Count, "Size of unmodifiable set has changed");
			
            Assert.Throws<NotSupportedException>(() => set.RetainAll(new List<string>(new[] { NOT_IN_SET })), "Modified unmodifiable set");
			Assert.AreEqual(size, set.Count, "Size of unmodifiable set has changed");
			
            Assert.Throws<NotSupportedException>(() => set.AddAll(new List<string>(new[] { NOT_IN_SET })), "Modified unmodifiable set");
			Assert.IsFalse(set.Contains(NOT_IN_SET), "Test String has been added to unmodifiable set");
			
			for (int i = 0; i < TEST_STOP_WORDS.Length; i++)
			{
				Assert.IsTrue(set.Contains(TEST_STOP_WORDS[i]));
			}
		}
		
        [Test]
		public virtual void  TestUnmodifiableSet()
		{
			CharArraySet set_Renamed = new CharArraySet(10, true);
            set_Renamed.AddAll(new List<string>(TEST_STOP_WORDS));
			int size = set_Renamed.Count;
			set_Renamed = CharArraySet.UnmodifiableSet(set_Renamed);
			Assert.AreEqual(size, set_Renamed.Count, "Set size changed due to UnmodifiableSet call");
			
			Assert.Throws<ArgumentNullException>(() => CharArraySet.UnmodifiableSet(null), "can not make null unmodifiable");
		}
	}
}