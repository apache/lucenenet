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
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using StandardTokenizer = Lucene.Net.Analysis.Standard.StandardTokenizer;
using Payload = Lucene.Net.Index.Payload;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis
{
	
	[TestFixture]
	public class TestAnalyzers : BaseTokenStreamTestCase
	{
        //public TestAnalyzers(System.String name) : base(name)
        //{
        //}
		
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
			Analyzer a = new StopAnalyzer(Version.LUCENE_CURRENT);
			AssertAnalyzesTo(a, "foo bar FOO BAR", new System.String[]{"foo", "bar", "foo", "bar"});
			AssertAnalyzesTo(a, "foo a bar such FOO THESE BAR", new System.String[]{"foo", "bar", "foo", "bar"});
		}
		
		internal virtual void  VerifyPayload(TokenStream ts)
		{
            IPayloadAttribute payloadAtt = ts.GetAttribute<IPayloadAttribute>();
			for (byte b = 1; ; b++)
			{
				bool hasNext = ts.IncrementToken();
				if (!hasNext)
					break;
				// System.out.println("id="+System.identityHashCode(nextToken) + " " + t);
				// System.out.println("payload=" + (int)nextToken.getPayload().toByteArray()[0]);
				Assert.AreEqual(b, payloadAtt.Payload.ToByteArray()[0]);
			}
		}
		
		// Make sure old style next() calls result in a new copy of payloads
		[Test]
		public virtual void  TestPayloadCopy()
		{
			System.String s = "how now brown cow";
			TokenStream ts;
			ts = new WhitespaceTokenizer(new System.IO.StringReader(s));
			ts = new PayloadSetter(ts);
			VerifyPayload(ts);
			
			ts = new WhitespaceTokenizer(new System.IO.StringReader(s));
			ts = new PayloadSetter(ts);
			VerifyPayload(ts);
		}
		
		// LUCENE-1150: Just a compile time test, to ensure the
		// StandardAnalyzer constants remain publicly accessible
		public virtual void  _testStandardConstants()
		{
			int x = StandardTokenizer.ALPHANUM;
			x = StandardTokenizer.APOSTROPHE;
			x = StandardTokenizer.ACRONYM;
			x = StandardTokenizer.COMPANY;
			x = StandardTokenizer.EMAIL;
			x = StandardTokenizer.HOST;
			x = StandardTokenizer.NUM;
			x = StandardTokenizer.CJ;
			System.String[] y = StandardTokenizer.TOKEN_TYPES;
		}
		
		private class MyStandardAnalyzer:StandardAnalyzer
		{
		    public MyStandardAnalyzer() : base(Version.LUCENE_CURRENT)
		    {
		        
		    }
			public override TokenStream TokenStream(System.String field, System.IO.TextReader reader)
			{
				return new WhitespaceAnalyzer().TokenStream(field, reader);
			}
		}
		
		[Test]
		public virtual void  TestSubclassOverridingOnlyTokenStream()
		{
			Analyzer a = new MyStandardAnalyzer();
			TokenStream ts = a.ReusableTokenStream("field", new System.IO.StringReader("the"));
			// StandardAnalyzer will discard "the" (it's a
			// stopword), by my subclass will not:
			Assert.IsTrue(ts.IncrementToken());
			Assert.IsFalse(ts.IncrementToken());
		}

        
        [Test]
        public void Test_LUCENE_3042_LUCENENET_433()
        {
            var testString = "t";

            Analyzer analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Version.LUCENE_30);
            var stream = analyzer.ReusableTokenStream("dummy", new System.IO.StringReader(testString));
            stream.Reset();
            while (stream.IncrementToken())
            {
                // consume
            }
            stream.End();
            stream.Close();

            AssertAnalyzesToReuse(analyzer, testString, new String[] { "t" });
        }

	}
	
	class PayloadSetter:TokenFilter
	{
		private void  InitBlock()
		{
			p = new Payload(data, 0, 1);
		}
		internal IPayloadAttribute payloadAtt;
		public PayloadSetter(TokenStream input):base(input)
		{
			InitBlock();
            payloadAtt = AddAttribute<IPayloadAttribute>();
		}
		
		internal byte[] data = new byte[1];
		internal Payload p;
		
		public override bool IncrementToken()
		{
			bool hasNext = input.IncrementToken();
			if (!hasNext)
				return false;
			payloadAtt.Payload = p; // reuse the payload / byte[]
			data[0]++;
			return true;
		}
	}
}