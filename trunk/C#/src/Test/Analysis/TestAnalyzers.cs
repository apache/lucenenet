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

using Payload = Lucene.Net.Index.Payload;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
{
	
	[TestFixture]
	public class TestAnalyzers : LuceneTestCase
	{
		
		public virtual void  AssertAnalyzesTo(Analyzer a, System.String input, System.String[] output)
		{
			TokenStream ts = a.TokenStream("dummy", new System.IO.StringReader(input));
			for (int i = 0; i < output.Length; i++)
			{
				Token t = ts.Next();
				Assert.IsNotNull(t);
				Assert.AreEqual(t.TermText(), output[i]);
			}
			Assert.IsNull(ts.Next());
			ts.Close();
		}
		
		[Test]
		public virtual void  TestSimple()
		{
			Analyzer a = new SimpleAnalyzer();
			AssertAnalyzesTo(a, "foo bar FOO BAR", new System.String[]{"foo", "bar", "foo", "bar"});
			AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new System.String[]{"foo", "bar", "foo", "bar"});
			AssertAnalyzesTo(a, "foo.bar.FOO.BAR", new System.String[]{"foo", "bar", "foo", "bar"});
			AssertAnalyzesTo(a, "U.S.A.", new System.String[]{"u", "s", "a"});
			AssertAnalyzesTo(a, "C++", new System.String[]{"c"});
			AssertAnalyzesTo(a, "B2B", new System.String[]{"b", "b"});
			AssertAnalyzesTo(a, "2B", new System.String[]{"b"});
			AssertAnalyzesTo(a, "\"QUOTED\" word", new System.String[]{"quoted", "word"});
		}
		
		[Test]
		public virtual void  TestNull()
		{
			Analyzer a = new WhitespaceAnalyzer();
			AssertAnalyzesTo(a, "foo bar FOO BAR", new System.String[]{"foo", "bar", "FOO", "BAR"});
			AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new System.String[]{"foo", "bar", ".", "FOO", "<>", "BAR"});
			AssertAnalyzesTo(a, "foo.bar.FOO.BAR", new System.String[]{"foo.bar.FOO.BAR"});
			AssertAnalyzesTo(a, "U.S.A.", new System.String[]{"U.S.A."});
			AssertAnalyzesTo(a, "C++", new System.String[]{"C++"});
			AssertAnalyzesTo(a, "B2B", new System.String[]{"B2B"});
			AssertAnalyzesTo(a, "2B", new System.String[]{"2B"});
			AssertAnalyzesTo(a, "\"QUOTED\" word", new System.String[]{"\"QUOTED\"", "word"});
		}
		
		[Test]
		public virtual void  TestStop()
		{
			Analyzer a = new StopAnalyzer();
			AssertAnalyzesTo(a, "foo bar FOO BAR", new System.String[]{"foo", "bar", "foo", "bar"});
			AssertAnalyzesTo(a, "foo a bar such FOO THESE BAR", new System.String[]{"foo", "bar", "foo", "bar"});
		}
		
		internal virtual void  VerifyPayload(TokenStream ts)
		{
			Token t = new Token();
			for (byte b = 1; ; b++)
			{
				t.Clear();
				t = ts.Next(t);
				if (t == null)
					break;
				// System.out.println("id="+System.identityHashCode(t) + " " + t);
				// System.out.println("payload=" + (int)t.getPayload().toByteArray()[0]);
				Assert.AreEqual(b, t.GetPayload().ToByteArray()[0]);
			}
		}
		
		// Make sure old style next() calls result in a new copy of payloads
		[Test]
		public virtual void  TestPayloadCopy()
		{
			System.String s = "how now brown cow";
			TokenStream ts;
			ts = new WhitespaceTokenizer(new System.IO.StringReader(s));
			ts = new BuffTokenFilter(ts);
			ts = new PayloadSetter(ts);
			VerifyPayload(ts);
			
			ts = new WhitespaceTokenizer(new System.IO.StringReader(s));
			ts = new PayloadSetter(ts);
			ts = new BuffTokenFilter(ts);
			VerifyPayload(ts);
		}
	}
	
	class BuffTokenFilter : TokenFilter
	{
		internal System.Collections.IList lst;
		
		public BuffTokenFilter(TokenStream input) : base(input)
		{
		}
		
		public override Token Next()
		{
			if (lst == null)
			{
				lst = new System.Collections.ArrayList();
				for (; ; )
				{
					Token t = input.Next();
					if (t == null)
						break;
					lst.Add(t);
				}
			}
			System.Object tempObject;
			tempObject = lst[0];
			lst.RemoveAt(0);
			return lst.Count == 0 ? null : (Token) tempObject;
		}
	}
	
	class PayloadSetter : TokenFilter
	{
		private void  InitBlock()
		{
			p = new Payload(data, 0, 1);
		}
		public PayloadSetter(TokenStream input) : base(input)
		{
			InitBlock();
		}
		
		internal byte[] data = new byte[1];
		internal Payload p;
		
		public override Token Next(Token target)
		{
			target = input.Next(target);
			if (target == null)
				return null;
			target.SetPayload(p); // reuse the payload / byte[]
			data[0]++;
			return target;
		}
	}
}