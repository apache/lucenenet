/*
 * Copyright 2004 The Apache Software Foundation
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
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;

namespace Lucene.Net.Analysis
{
	[TestFixture]
	public class TestStandardAnalyzer
	{
		
		public virtual void  AssertAnalyzesTo(Analyzer a, System.String input, System.String[] expected)
		{
			TokenStream ts = a.TokenStream("dummy", new System.IO.StringReader(input));
			for (int i = 0; i < expected.Length; i++)
			{
				Token t = ts.Next();
				Assert.IsNotNull(t);
				Assert.AreEqual(expected[i], t.TermText());
			}
			Assert.IsNull(ts.Next());
			ts.Close();
		}
		
		[Test]
		public virtual void  TestStandard()
		{
			Analyzer a = new StandardAnalyzer();
			
			// alphanumeric tokens
			AssertAnalyzesTo(a, "B2B", new System.String[]{"b2b"});
			AssertAnalyzesTo(a, "2B", new System.String[]{"2b"});
			
			// underscores are delimiters, but not in email addresses (below)
			AssertAnalyzesTo(a, "word_having_underscore", new System.String[]{"word", "having", "underscore"});
			AssertAnalyzesTo(a, "word_with_underscore_and_stopwords", new System.String[]{"word", "underscore", "stopwords"});
			
			// other delimiters: "-", "/", ","
			AssertAnalyzesTo(a, "some-dashed-phrase", new System.String[]{"some", "dashed", "phrase"});
			AssertAnalyzesTo(a, "dogs,chase,cats", new System.String[]{"dogs", "chase", "cats"});
			AssertAnalyzesTo(a, "ac/dc", new System.String[]{"ac", "dc"});
			
			// internal apostrophes: O'Reilly, you're, O'Reilly's
			// possessives are actually removed by StardardFilter, not the tokenizer
			AssertAnalyzesTo(a, "O'Reilly", new System.String[]{"o'reilly"});
			AssertAnalyzesTo(a, "you're", new System.String[]{"you're"});
			AssertAnalyzesTo(a, "O'Reilly's", new System.String[]{"o'reilly"});
			
			// company names
			AssertAnalyzesTo(a, "AT&T", new System.String[]{"at&t"});
			AssertAnalyzesTo(a, "Excite@Home", new System.String[]{"excite@home"});
			
			// domain names
			AssertAnalyzesTo(a, "www.nutch.org", new System.String[]{"www.nutch.org"});
			
			// email addresses, possibly with underscores, periods, etc
			AssertAnalyzesTo(a, "test@example.com", new System.String[]{"test@example.com"});
			AssertAnalyzesTo(a, "first.lastname@example.com", new System.String[]{"first.lastname@example.com"});
			AssertAnalyzesTo(a, "first_lastname@example.com", new System.String[]{"first_lastname@example.com"});
			
			// floating point, serial, model numbers, ip addresses, etc.
			// every other segment must have at least one digit
			AssertAnalyzesTo(a, "21.35", new System.String[]{"21.35"});
			AssertAnalyzesTo(a, "R2D2 C3PO", new System.String[]{"r2d2", "c3po"});
			AssertAnalyzesTo(a, "216.239.63.104", new System.String[]{"216.239.63.104"});
			AssertAnalyzesTo(a, "1-2-3", new System.String[]{"1-2-3"});
			AssertAnalyzesTo(a, "a1-b2-c3", new System.String[]{"a1-b2-c3"});
			AssertAnalyzesTo(a, "a1-b-c3", new System.String[]{"a1-b-c3"});
			
			// numbers
			AssertAnalyzesTo(a, "David has 5000 bones", new System.String[]{"david", "has", "5000", "bones"});
			
			// various
			AssertAnalyzesTo(a, "C embedded developers wanted", new System.String[]{"c", "embedded", "developers", "wanted"});
			AssertAnalyzesTo(a, "foo bar FOO BAR", new System.String[]{"foo", "bar", "foo", "bar"});
			AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new System.String[]{"foo", "bar", "foo", "bar"});
			AssertAnalyzesTo(a, "\"QUOTED\" word", new System.String[]{"quoted", "word"});
			
			// acronyms have their dots stripped
			AssertAnalyzesTo(a, "U.S.A.", new System.String[]{"usa"});
			
			// It would be nice to change the grammar in StandardTokenizer.jj to make "C#" and "C++" end up as tokens.
			AssertAnalyzesTo(a, "C++", new System.String[]{"c"});
			AssertAnalyzesTo(a, "C#", new System.String[]{"c"});
			
			// Korean words
			AssertAnalyzesTo(a, "안녕하세요 한글입니다", new System.String[]{"안녕하세요", "한글입니다"});
		}
	}
}