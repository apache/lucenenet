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
	public class TestToken : LuceneTestCase
	{
		[Test]
		public virtual void  TestToString()
		{
			char[] b = new char[]{'a', 'l', 'o', 'h', 'a'};
			Token t = new Token("", 0, 5);
			t.SetTermBuffer(b, 0, 5);
			Assert.AreEqual("(aloha,0,5)", t.ToString());
			
			t.SetTermText("hi there");
			Assert.AreEqual("(hi there,0,5)", t.ToString());
		}
		
		[Test]
		public virtual void  TestMixedStringArray()
		{
			Token t = new Token("hello", 0, 5);
			Assert.AreEqual(t.TermText(), "hello");
			Assert.AreEqual(t.TermLength(), 5);
			Assert.AreEqual(new System.String(t.TermBuffer(), 0, 5), "hello");
			t.SetTermText("hello2");
			Assert.AreEqual(t.TermLength(), 6);
			Assert.AreEqual(new System.String(t.TermBuffer(), 0, 6), "hello2");
			t.SetTermBuffer("hello3".ToCharArray(), 0, 6);
			Assert.AreEqual(t.TermText(), "hello3");
			
			// Make sure if we get the buffer and change a character
			// that termText() reflects the change
			char[] buffer = t.TermBuffer();
			buffer[1] = 'o';
			Assert.AreEqual(t.TermText(), "hollo3");
		}
	}
}