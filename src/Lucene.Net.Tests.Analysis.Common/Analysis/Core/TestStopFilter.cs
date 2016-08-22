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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Analysis.Common.Analysis.Core
{
	[TestFixture]
	public class TestStopFilter : BaseTokenStreamTestCase
	{
		// other StopFilter functionality is already tested by TestStopAnalyzer
		
		[Test]
		public virtual void TestExactCase()
		{
			var reader = new StringReader("Now is The Time");
			var stopWords = new CharArraySet(LuceneTestCase.TEST_VERSION_CURRENT, AsSet("is", "the", "Time"), false);
			TokenStream stream = new StopFilter(LuceneTestCase.TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopWords);
			AssertTokenStreamContents(stream, new string[] { "Now", "The" });
		}

		[Test]
		public virtual void TestStopFilt()
		{
			var reader = new StringReader("Now is The Time");
			var stopWords = new string[] { "is", "the", "Time" };
			var stopSet = StopFilter.MakeStopSet(LuceneTestCase.TEST_VERSION_CURRENT, stopWords);
			TokenStream stream = new StopFilter(LuceneTestCase.TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
			AssertTokenStreamContents(stream, new string[] { "Now", "The" });
		}

		/// <summary>
		/// Test Position increments applied by StopFilter with and without enabling this option.
		/// </summary>
		[Test]
		public virtual void TestStopPositons()
		{
			var sb = new StringBuilder();
			var a = new List<string>();
			for (var i = 0; i < 20; i++)
			{
				var w = English.IntToEnglish(i).Trim();
				sb.Append(w).Append(" ");
				if (i % 3 != 0)
				{
					a.Add(w);
				}
			}

			Log(sb.ToString());

			var stopWords = a.ToArray();
			for (var i = 0; i < a.Count; i++)
			{
				Log("Stop: " + stopWords[i]);
			}

			var stopSet = StopFilter.MakeStopSet(LuceneTestCase.TEST_VERSION_CURRENT, stopWords);

			// with increments
			var reader = new StringReader(sb.ToString());
			var stpf = new StopFilter(LuceneVersion.LUCENE_40, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
			DoTestStopPositons(stpf, true);

			// without increments
			reader = new StringReader(sb.ToString());
			stpf = new StopFilter(LuceneVersion.LUCENE_43, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet);
			DoTestStopPositons(stpf, false);

			// with increments, concatenating two stop filters
			var a0 = new List<string>();
			var a1 = new List<string>();

			for (var i = 0; i < a.Count; i++)
			{
				if (i % 2 == 0)
				{
					a0.Add(a[i]);
				}
				else
				{
					a1.Add(a[i]);
				}
			}

			var stopWords0 = a0.ToArray();
			for (var i = 0; i < a0.Count; i++)
			{
				Log("Stop0: " + stopWords0[i]);
			}

			var stopWords1 = a1.ToArray();
			for (var i = 0; i < a1.Count; i++)
			{
				Log("Stop1: " + stopWords1[i]);
			}

			var stopSet0 = StopFilter.MakeStopSet(LuceneTestCase.TEST_VERSION_CURRENT, stopWords0);
			var stopSet1 = StopFilter.MakeStopSet(LuceneTestCase.TEST_VERSION_CURRENT, stopWords1);

			reader = new StringReader(sb.ToString());
			var stpf0 = new StopFilter(LuceneTestCase.TEST_VERSION_CURRENT, new MockTokenizer(reader, MockTokenizer.WHITESPACE, false), stopSet0); // first part of the set
			stpf0.EnablePositionIncrements = true;

			var stpf01 = new StopFilter(LuceneTestCase.TEST_VERSION_CURRENT, stpf0, stopSet1); // two stop filters concatenated!

			DoTestStopPositons(stpf01, true);
		}

		// LUCENE-3849: make sure after .end() we see the "ending" posInc
		[Test]
		public virtual void TestEndStopword()
		{
			var stopSet = StopFilter.MakeStopSet(LuceneTestCase.TEST_VERSION_CURRENT, "of");
			var stpf = new StopFilter(LuceneTestCase.TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("test of"), MockTokenizer.WHITESPACE, false), stopSet);

			AssertTokenStreamContents(stpf, new string[] { "test" }, new int[] { 0 }, new int[] { 4 }, null, new int[] { 1 }, null, 7, 1, null, true);
		}

		private static void DoTestStopPositons(StopFilter stpf, bool enableIcrements)
		{
			Log("---> test with enable-increments-" + (enableIcrements ? "enabled" : "disabled"));
			stpf.EnablePositionIncrements = enableIcrements;
			var termAtt = stpf.GetAttribute<ICharTermAttribute>();
			var posIncrAtt = stpf.GetAttribute<IPositionIncrementAttribute>();
			stpf.Reset();

			for (var i = 0; i < 20; i += 3)
			{
				assertTrue(stpf.IncrementToken());
				Log("Token " + i + ": " + stpf);
				var w = English.IntToEnglish(i).Trim();
				assertEquals("expecting token " + i + " to be " + w, w, termAtt.ToString());
				assertEquals("all but first token must have position increment of 3", enableIcrements ? (i == 0 ? 1 : 3) : 1, posIncrAtt.PositionIncrement);
			}

			assertFalse(stpf.IncrementToken());
			stpf.End();
			stpf.Dispose();
		}

		// print debug info depending on VERBOSE
		private static void Log(string s)
		{
			if (LuceneTestCase.VERBOSE)
			{
				Console.WriteLine(s);
			}
		}

		// stupid filter that inserts synonym of 'hte' for 'the'
		private sealed class MockSynonymFilter : TokenFilter
		{
			private readonly TestStopFilter outerInstance;

			private State bufferedState;
			private readonly ICharTermAttribute termAtt;
			private readonly IPositionIncrementAttribute posIncAtt;

			internal MockSynonymFilter(TestStopFilter outerInstance, TokenStream input) : base(input)
			{
				this.outerInstance = outerInstance;
				this.termAtt = this.AddAttribute<ICharTermAttribute>();
				this.posIncAtt = this.AddAttribute<IPositionIncrementAttribute>();
			}
			
			public override bool IncrementToken()
			{
				if (this.bufferedState != null)
				{
					this.RestoreState(this.bufferedState);
					this.posIncAtt.PositionIncrement = 0;
					this.termAtt.SetEmpty().Append("hte");
					this.bufferedState = null;

					return true;
				}
				else if (this.input.IncrementToken())
				{
					if (this.termAtt.ToString().Equals("the"))
					{
						this.bufferedState = this.CaptureState();
					}
					return true;
				}
				else
				{
					return false;
				}
			}
			
			public override void Reset()
			{
				base.Reset();
				this.bufferedState = null;
			}
		}
		
		[Test]
		public virtual void TestFirstPosInc()
		{
			Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);

			AssertAnalyzesTo(analyzer, "the quick brown fox", new string[] { "hte", "quick", "brown", "fox" }, new int[] { 1, 1, 1, 1 });
		}

		private class AnalyzerAnonymousInnerClassHelper : Analyzer
		{
			private readonly TestStopFilter outerInstance;

			public AnalyzerAnonymousInnerClassHelper(TestStopFilter outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
				TokenFilter filter = new MockSynonymFilter(this.outerInstance, tokenizer);
				var stopfilter = new StopFilter(LuceneVersion.LUCENE_43, filter, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
				stopfilter.EnablePositionIncrements = false;
				return new TokenStreamComponents(tokenizer, stopfilter);
			}
		}
	}
}