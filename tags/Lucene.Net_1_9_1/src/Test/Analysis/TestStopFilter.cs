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

namespace Lucene.Net.Analysis
{
	
	/// <author>  yonik
	/// </author>
	[TestFixture]
    public class TestStopFilter
	{
		
		// other StopFilter functionality is already tested by TestStopAnalyzer
		[Test]
		public virtual void  TestExactCase()
		{
			System.IO.StringReader reader = new System.IO.StringReader("Now is The Time");
			System.String[] stopWords = new System.String[]{"is", "the", "Time"};
			TokenStream stream = new StopFilter(new WhitespaceTokenizer(reader), stopWords);
			Assert.AreEqual("Now", stream.Next().TermText());
			Assert.AreEqual("The", stream.Next().TermText());
			Assert.AreEqual(null, stream.Next());
		}
		
		public virtual void  TestIgnoreCase()
		{
			System.IO.StringReader reader = new System.IO.StringReader("Now is The Time");
			System.String[] stopWords = new System.String[]{"is", "the", "Time"};
			TokenStream stream = new StopFilter(new WhitespaceTokenizer(reader), stopWords, true);
			Assert.AreEqual("Now", stream.Next().TermText());
			Assert.AreEqual(null, stream.Next());
		}
	}
}