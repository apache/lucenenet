/*
 * Copyright 2005 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using WordlistLoader = Lucene.Net.Analysis.WordlistLoader;

namespace Lucene.Net.Index
{
	[TestFixture]
	public class TestWordlistLoader
	{
		[Test]
		public virtual void  TestWordlistLoading()
		{
			System.String s = "ONE\n  two \nthree";
			System.Collections.Hashtable wordSet1 = WordlistLoader.GetWordSet(new System.IO.StringReader(s));
			CheckSet(wordSet1);
			//UPGRADE_ISSUE: Constructor 'java.io.BufferedReader.BufferedReader' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javaioBufferedReaderBufferedReader_javaioReader'"
			System.Collections.Hashtable wordSet2 = WordlistLoader.GetWordSet(new System.IO.StringReader(s));
			CheckSet(wordSet2);
		}
		
		private void  CheckSet(System.Collections.Hashtable wordset)
		{
			Assert.AreEqual(3, wordset.Count);
			Assert.IsTrue(wordset.Contains("ONE")); // case is not modified
			Assert.IsTrue(wordset.Contains("two")); // surrounding whitespace is removed
			Assert.IsTrue(wordset.Contains("three"));
			Assert.IsFalse(wordset.Contains("four"));
		}
	}
}