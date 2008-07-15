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

using English = Lucene.Net.Util.English;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
{
	
	/// <author>  yonik
	/// </author>
	[TestFixture]
	public class TestStopFilter : LuceneTestCase
	{
		
		private const bool VERBOSE = false;
		
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
		
		[Test]
		public virtual void  TestIgnoreCase()
		{
			System.IO.StringReader reader = new System.IO.StringReader("Now is The Time");
			System.String[] stopWords = new System.String[]{"is", "the", "Time"};
			TokenStream stream = new StopFilter(new WhitespaceTokenizer(reader), stopWords, true);
			Assert.AreEqual("Now", stream.Next().TermText());
			Assert.AreEqual(null, stream.Next());
		}
		
		[Test]
		public virtual void  TestStopFilt()
		{
			System.IO.StringReader reader = new System.IO.StringReader("Now is The Time");
			System.String[] stopWords = new System.String[]{"is", "the", "Time"};
			System.Collections.Hashtable stopSet = StopFilter.MakeStopSet(stopWords);
			TokenStream stream = new StopFilter(new WhitespaceTokenizer(reader), stopSet);
			Assert.AreEqual("Now", stream.Next().TermText());
			Assert.AreEqual("The", stream.Next().TermText());
			Assert.AreEqual(null, stream.Next());
		}
		
		/// <summary> Test Position increments applied by StopFilter with and without enabling this option.</summary>
		[Test]
		public virtual void  TestStopPositons()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			System.Collections.Generic.List<string> a = new System.Collections.Generic.List<string>();
			for (int i = 0; i < 20; i++)
			{
				System.String w = English.IntToEnglish(i).Trim();
				sb.Append(w).Append(" ");
				if (i % 3 != 0)
					a.Add(w);
			}
			Log(sb.ToString());
			System.String[] stopWords = (System.String[]) a.ToArray();
			for (int i = 0; i < a.Count; i++)
				Log("Stop: " + stopWords[i]);
			System.Collections.Hashtable stopSet = StopFilter.MakeStopSet(stopWords);
			// with increments
			System.IO.StringReader reader = new System.IO.StringReader(sb.ToString());
			StopFilter stpf = new StopFilter(new WhitespaceTokenizer(reader), stopSet);
			DoTestStopPositons(stpf, true);
			// without increments
			reader = new System.IO.StringReader(sb.ToString());
			stpf = new StopFilter(new WhitespaceTokenizer(reader), stopSet);
			DoTestStopPositons(stpf, false);
			// with increments, concatenating two stop filters
			System.Collections.Generic.List<string> a0 = new System.Collections.Generic.List<string>();
			System.Collections.Generic.List<string> a1 = new System.Collections.Generic.List<string>();
			for (int i = 0; i < a.Count; i++)
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
			System.String[] stopWords0 = (System.String[]) a0.ToArray();
			for (int i = 0; i < a0.Count; i++)
				Log("Stop0: " + stopWords0[i]);
			System.String[] stopWords1 = (System.String[]) a1.ToArray();
			for (int i = 0; i < a1.Count; i++)
				Log("Stop1: " + stopWords1[i]);
			System.Collections.Hashtable stopSet0 = StopFilter.MakeStopSet(stopWords0);
			System.Collections.Hashtable stopSet1 = StopFilter.MakeStopSet(stopWords1);
			reader = new System.IO.StringReader(sb.ToString());
			StopFilter stpf0 = new StopFilter(new WhitespaceTokenizer(reader), stopSet0); // first part of the set
			stpf0.SetEnablePositionIncrements(true);
			StopFilter stpf01 = new StopFilter(stpf0, stopSet1); // two stop filters concatenated!
			DoTestStopPositons(stpf01, true);
		}
		
		private void  DoTestStopPositons(StopFilter stpf, bool enableIcrements)
		{
			Log("---> test with enable-increments-" + (enableIcrements?"enabled":"disabled"));
			stpf.SetEnablePositionIncrements(enableIcrements);
			for (int i = 0; i < 20; i += 3)
			{
				Token t = stpf.Next();
				Log("Token " + i + ": " + t);
				System.String w = English.IntToEnglish(i).Trim();
				Assert.AreEqual(w, t.TermText(), "expecting token " + i + " to be " + w);
				Assert.AreEqual(enableIcrements ? (i == 0 ? 1 : 3) : 1, t.GetPositionIncrement(), "all but first token must have position increment of 3");
			}
			Assert.IsNull(stpf.Next());
		}
		
		// print debug info depending on VERBOSE
		private static void  Log(System.String s)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine(s);
			}
		}
	}
}