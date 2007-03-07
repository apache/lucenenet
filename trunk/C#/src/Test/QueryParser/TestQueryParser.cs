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
using Analyzer = Lucene.Net.Analysis.Analyzer;
using LowerCaseTokenizer = Lucene.Net.Analysis.LowerCaseTokenizer;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Token = Lucene.Net.Analysis.Token;
using TokenFilter = Lucene.Net.Analysis.TokenFilter;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using DateField = Lucene.Net.Documents.DateField;
using ParseException = Lucene.Net.QueryParsers.ParseException;
using QueryParser = Lucene.Net.QueryParsers.QueryParser;
using BooleanQuery = Lucene.Net.Search.BooleanQuery;
using FuzzyQuery = Lucene.Net.Search.FuzzyQuery;
using PhraseQuery = Lucene.Net.Search.PhraseQuery;
using PrefixQuery = Lucene.Net.Search.PrefixQuery;
using Query = Lucene.Net.Search.Query;
using RangeQuery = Lucene.Net.Search.RangeQuery;
using TermQuery = Lucene.Net.Search.TermQuery;
using WildcardQuery = Lucene.Net.Search.WildcardQuery;

namespace Lucene.Net.QueryParser
{
	
	/// <summary> Tests QueryParser.</summary>
	[TestFixture]
    public class TestQueryParser
	{
		
		public static Analyzer qpAnalyzer = new QPTestAnalyzer();
		
		public class QPTestFilter : TokenFilter
		{
			/// <summary> Filter which discards the token 'stop' and which expands the
			/// token 'phrase' into 'phrase1 phrase2'
			/// </summary>
			public QPTestFilter(TokenStream in_Renamed) : base(in_Renamed)
			{
			}
			
			internal bool inPhrase = false;
			internal int savedStart = 0, savedEnd = 0;
			
			public override Token Next()
			{
				if (inPhrase)
				{
					inPhrase = false;
					return new Token("phrase2", savedStart, savedEnd);
				}
				else
					for (Token token = input.Next(); token != null; token = input.Next())
					{
						if (token.TermText().Equals("phrase"))
						{
							inPhrase = true;
							savedStart = token.StartOffset();
							savedEnd = token.EndOffset();
							return new Token("phrase1", savedStart, savedEnd);
						}
						else if (!token.TermText().Equals("stop"))
							return token;
					}
				return null;
			}
		}
		
		public class QPTestAnalyzer : Analyzer
		{
			
			/// <summary>Filters LowerCaseTokenizer with StopFilter. </summary>
			public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
			{
				return new QPTestFilter(new LowerCaseTokenizer(reader));
			}
		}
		
		public class QPTestParser : Lucene.Net.QueryParsers.QueryParser
		{
			public QPTestParser(System.String f, Analyzer a):base(f, a)
			{
			}
			
			protected override Query GetFuzzyQuery(System.String field, System.String termStr, float minSimilarity)
			{
				throw new ParseException("Fuzzy queries not allowed");
			}
			
			protected override Query GetWildcardQuery(System.String field, System.String termStr)
			{
				throw new ParseException("Wildcard queries not allowed");
			}
		}
		
		private int originalMaxClauses;
		
		[SetUp]
        public virtual void  SetUp()
		{
			originalMaxClauses = BooleanQuery.GetMaxClauseCount();
		}
		
		public virtual Lucene.Net.QueryParsers.QueryParser GetParser(Analyzer a)
		{
			if (a == null)
				a = new SimpleAnalyzer();
			Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser("field", a);
			qp.SetDefaultOperator(Lucene.Net.QueryParsers.QueryParser.OR_OPERATOR);
			return qp;
		}
		
		public virtual Query GetQuery(System.String query, Analyzer a)
		{
			return GetParser(a).Parse(query);
		}
		
		public virtual void  AssertQueryEquals(System.String query, Analyzer a, System.String result)
		{
			Query q = GetQuery(query, a);
			System.String s = q.ToString("field");
			if (!s.Equals(result))
			{
				Assert.Fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result + "/");
			}
		}
		
        public virtual void  AssertEscapedQueryEquals(System.String query, Analyzer a, System.String result)
        {
            System.String escapedQuery = Lucene.Net.QueryParsers.QueryParser.Escape(query);
            if (!escapedQuery.Equals(result))
            {
                Assert.Fail("Query /" + query + "/ yielded /" + escapedQuery + "/, expecting /" + result + "/");
            }
        }
		
        public virtual void  AssertWildcardQueryEquals(System.String query, bool lowercase, System.String result)
		{
			Lucene.Net.QueryParsers.QueryParser qp = GetParser(null);
			qp.SetLowercaseExpandedTerms(lowercase);
			Query q = qp.Parse(query);
			System.String s = q.ToString("field");
			if (!s.Equals(result))
			{
				Assert.Fail("WildcardQuery /" + query + "/ yielded /" + s + "/, expecting /" + result + "/");
			}
		}
		
		public virtual void  AssertWildcardQueryEquals(System.String query, System.String result)
		{
			Lucene.Net.QueryParsers.QueryParser qp = GetParser(null);
			Query q = qp.Parse(query);
			System.String s = q.ToString("field");
			if (!s.Equals(result))
			{
				Assert.Fail("WildcardQuery /" + query + "/ yielded /" + s + "/, expecting /" + result + "/");
			}
		}
		
		public virtual Query GetQueryDOA(System.String query, Analyzer a)
		{
			if (a == null)
				a = new SimpleAnalyzer();
			Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser("field", a);
			qp.SetDefaultOperator(Lucene.Net.QueryParsers.QueryParser.AND_OPERATOR);
			return qp.Parse(query);
		}
		
		public virtual void  AssertQueryEqualsDOA(System.String query, Analyzer a, System.String result)
		{
			Query q = GetQueryDOA(query, a);
			System.String s = q.ToString("field");
			if (!s.Equals(result))
			{
				Assert.Fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result + "/");
			}
		}
		
		[Test]
        public virtual void  TestSimple()
		{
			AssertQueryEquals("term term term", null, "term term term");
			AssertQueryEquals("türm term term", null, "türm term term");
			AssertQueryEquals("ümlaut", null, "ümlaut");
			
			AssertQueryEquals("a AND b", null, "+a +b");
			AssertQueryEquals("(a AND b)", null, "+a +b");
			AssertQueryEquals("c OR (a AND b)", null, "c (+a +b)");
			AssertQueryEquals("a AND NOT b", null, "+a -b");
			AssertQueryEquals("a AND -b", null, "+a -b");
			AssertQueryEquals("a AND !b", null, "+a -b");
			AssertQueryEquals("a && b", null, "+a +b");
			AssertQueryEquals("a && ! b", null, "+a -b");
			
			AssertQueryEquals("a OR b", null, "a b");
			AssertQueryEquals("a || b", null, "a b");
			AssertQueryEquals("a OR !b", null, "a -b");
			AssertQueryEquals("a OR ! b", null, "a -b");
			AssertQueryEquals("a OR -b", null, "a -b");
			
			AssertQueryEquals("+term -term term", null, "+term -term term");
			AssertQueryEquals("foo:term AND field:anotherTerm", null, "+foo:term +anotherterm");
			AssertQueryEquals("term AND \"phrase phrase\"", null, "+term +\"phrase phrase\"");
			AssertQueryEquals("\"hello there\"", null, "\"hello there\"");
			Assert.IsTrue(GetQuery("a AND b", null) is BooleanQuery);
			Assert.IsTrue(GetQuery("hello", null) is TermQuery);
			Assert.IsTrue(GetQuery("\"hello there\"", null) is PhraseQuery);
			
			AssertQueryEquals("germ term^2.0", null, "germ term^2.0");
			AssertQueryEquals("(term)^2.0", null, "term^2.0");
			AssertQueryEquals("(germ term)^2.0", null, "(germ term)^2.0");
			AssertQueryEquals("term^2.0", null, "term^2.0");
			AssertQueryEquals("term^2", null, "term^2.0");
			AssertQueryEquals("\"germ term\"^2.0", null, "\"germ term\"^2.0");
			AssertQueryEquals("\"term germ\"^2", null, "\"term germ\"^2.0");
			
			AssertQueryEquals("(foo OR bar) AND (baz OR boo)", null, "+(foo bar) +(baz boo)");
			AssertQueryEquals("((a OR b) AND NOT c) OR d", null, "(+(a b) -c) d");
			AssertQueryEquals("+(apple \"steve jobs\") -(foo bar baz)", null, "+(apple \"steve jobs\") -(foo bar baz)");
			AssertQueryEquals("+title:(dog OR cat) -author:\"bob dole\"", null, "+(title:dog title:cat) -author:\"bob dole\"");
			
			Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser("field", new StandardAnalyzer());
			// make sure OR is the default:
			Assert.AreEqual(Lucene.Net.QueryParsers.QueryParser.OR_OPERATOR, qp.GetDefaultOperator());
			qp.SetDefaultOperator(Lucene.Net.QueryParsers.QueryParser.AND_OPERATOR);
			Assert.AreEqual(Lucene.Net.QueryParsers.QueryParser.AND_OPERATOR, qp.GetDefaultOperator());
			qp.SetDefaultOperator(Lucene.Net.QueryParsers.QueryParser.OR_OPERATOR);
			Assert.AreEqual(Lucene.Net.QueryParsers.QueryParser.OR_OPERATOR, qp.GetDefaultOperator());
		}
		
		[Test]
        public virtual void  TestPunct()
		{
			Analyzer a = new WhitespaceAnalyzer();
			AssertQueryEquals("a&b", a, "a&b");
			AssertQueryEquals("a&&b", a, "a&&b");
			AssertQueryEquals(".NET", a, ".NET");
		}
		
		[Test]
        public virtual void  TestSlop()
		{
			AssertQueryEquals("\"term germ\"~2", null, "\"term germ\"~2");
			AssertQueryEquals("\"term germ\"~2 flork", null, "\"term germ\"~2 flork");
			AssertQueryEquals("\"term\"~2", null, "term");
			AssertQueryEquals("\" \"~2 germ", null, "germ");
			AssertQueryEquals("\"term germ\"~2^2", null, "\"term germ\"~2^2.0");
		}
		
		[Test]
        public virtual void  TestNumber()
		{
			// The numbers go away because SimpleAnalzyer ignores them
			AssertQueryEquals("3", null, "");
			AssertQueryEquals("term 1.0 1 2", null, "term");
			AssertQueryEquals("term term1 term2", null, "term term term");
			
			Analyzer a = new StandardAnalyzer();
			AssertQueryEquals("3", a, "3");
			AssertQueryEquals("term 1.0 1 2", a, "term 1.0 1 2");
			AssertQueryEquals("term term1 term2", a, "term term1 term2");
		}
		
		[Test]
        public virtual void  TestWildcard()
		{
			AssertQueryEquals("term*", null, "term*");
			AssertQueryEquals("term*^2", null, "term*^2.0");
			AssertQueryEquals("term~", null, "term~0.5");
			AssertQueryEquals("term~0.7", null, "term~0.7");
			AssertQueryEquals("term~^2", null, "term~0.5^2.0");
			AssertQueryEquals("term^2~", null, "term~0.5^2.0");
			AssertQueryEquals("term*germ", null, "term*germ");
			AssertQueryEquals("term*germ^3", null, "term*germ^3.0");
			
			Assert.IsTrue(GetQuery("term*", null) is PrefixQuery);
			Assert.IsTrue(GetQuery("term*^2", null) is PrefixQuery);
			Assert.IsTrue(GetQuery("term~", null) is FuzzyQuery);
			Assert.IsTrue(GetQuery("term~0.7", null) is FuzzyQuery);
			FuzzyQuery fq = (FuzzyQuery) GetQuery("term~0.7", null);
			Assert.AreEqual(0.7f, fq.GetMinSimilarity(), 0.1f);
			Assert.AreEqual(FuzzyQuery.defaultPrefixLength, fq.GetPrefixLength());
			fq = (FuzzyQuery) GetQuery("term~", null);
			Assert.AreEqual(0.5f, fq.GetMinSimilarity(), 0.1f);
			Assert.AreEqual(FuzzyQuery.defaultPrefixLength, fq.GetPrefixLength());
			try
			{
				GetQuery("term~1.1", null); // value > 1, throws exception
				Assert.Fail();
			}
			catch (ParseException pe)
			{
				// expected exception
			}
			Assert.IsTrue(GetQuery("term*germ", null) is WildcardQuery);
			
			/* Tests to see that wild card terms are (or are not) properly
			* lower-cased with propery parser configuration
			*/
			// First prefix queries:
			// by default, convert to lowercase:
			AssertWildcardQueryEquals("Term*", true, "term*");
			// explicitly set lowercase:
			AssertWildcardQueryEquals("term*", true, "term*");
			AssertWildcardQueryEquals("Term*", true, "term*");
			AssertWildcardQueryEquals("TERM*", true, "term*");
			// explicitly disable lowercase conversion:
			AssertWildcardQueryEquals("term*", false, "term*");
			AssertWildcardQueryEquals("Term*", false, "Term*");
			AssertWildcardQueryEquals("TERM*", false, "TERM*");
			// Then 'full' wildcard queries:
			// by default, convert to lowercase:
			AssertWildcardQueryEquals("Te?m", "te?m");
			// explicitly set lowercase:
			AssertWildcardQueryEquals("te?m", true, "te?m");
			AssertWildcardQueryEquals("Te?m", true, "te?m");
			AssertWildcardQueryEquals("TE?M", true, "te?m");
			AssertWildcardQueryEquals("Te?m*gerM", true, "te?m*germ");
			// explicitly disable lowercase conversion:
			AssertWildcardQueryEquals("te?m", false, "te?m");
			AssertWildcardQueryEquals("Te?m", false, "Te?m");
			AssertWildcardQueryEquals("TE?M", false, "TE?M");
			AssertWildcardQueryEquals("Te?m*gerM", false, "Te?m*gerM");
			//  Fuzzy queries:
			AssertWildcardQueryEquals("Term~", "term~0.5");
			AssertWildcardQueryEquals("Term~", true, "term~0.5");
			AssertWildcardQueryEquals("Term~", false, "Term~0.5");
			//  Range queries:
			AssertWildcardQueryEquals("[A TO C]", "[a TO c]");
			AssertWildcardQueryEquals("[A TO C]", true, "[a TO c]");
			AssertWildcardQueryEquals("[A TO C]", false, "[A TO C]");
		}
		
		[Test]
        public virtual void  TestQPA()
		{
			AssertQueryEquals("term term term", qpAnalyzer, "term term term");
			AssertQueryEquals("term +stop term", qpAnalyzer, "term term");
			AssertQueryEquals("term -stop term", qpAnalyzer, "term term");
			AssertQueryEquals("drop AND stop AND roll", qpAnalyzer, "+drop +roll");
			AssertQueryEquals("term phrase term", qpAnalyzer, "term \"phrase1 phrase2\" term");
			AssertQueryEquals("term AND NOT phrase term", qpAnalyzer, "+term -\"phrase1 phrase2\" term");
			AssertQueryEquals("stop", qpAnalyzer, "");
			Assert.IsTrue(GetQuery("term term term", qpAnalyzer) is BooleanQuery);
			Assert.IsTrue(GetQuery("term +stop", qpAnalyzer) is TermQuery);
		}
		
		[Test]
        public virtual void  TestRange()
		{
			AssertQueryEquals("[ a TO z]", null, "[a TO z]");
			Assert.IsTrue(GetQuery("[ a TO z]", null) is RangeQuery);
			AssertQueryEquals("[ a TO z ]", null, "[a TO z]");
			AssertQueryEquals("{ a TO z}", null, "{a TO z}");
			AssertQueryEquals("{ a TO z }", null, "{a TO z}");
			AssertQueryEquals("{ a TO z }^2.0", null, "{a TO z}^2.0");
			AssertQueryEquals("[ a TO z] OR bar", null, "[a TO z] bar");
			AssertQueryEquals("[ a TO z] AND bar", null, "+[a TO z] +bar");
			AssertQueryEquals("( bar blar { a TO z}) ", null, "bar blar {a TO z}");
			AssertQueryEquals("gack ( bar blar { a TO z}) ", null, "gack (bar blar {a TO z})");
		}
		
		public virtual System.String GetDate(System.String s)
		{
            System.DateTime tempAux = System.DateTime.Parse(s);
            return DateField.DateToString(tempAux);
		}
		
		private System.String GetLocalizedDate(int year, int month, int day, bool extendLastDate)
		{
            System.DateTime temp = new System.DateTime(year, month, day);
			if (extendLastDate)
			{
                temp = temp.AddHours(23);
                temp = temp.AddMinutes(59);
                temp = temp.AddSeconds(59);
                temp = temp.AddMilliseconds(999);
			}
            return temp.ToString("MM/d/yyy");
		}
		
		[Test]
        public virtual void  TestDateRange()
		{
            System.String startDate = GetLocalizedDate(2002, 2, 1, false);
            System.String endDate = GetLocalizedDate(2002, 2, 4, false);
            System.DateTime endDateExpected = new System.DateTime(2002, 2, 4, 23, 59, 59);
            endDateExpected = endDateExpected.AddMilliseconds(999);
            AssertQueryEquals("[ " + startDate + " TO " + endDate + "]", null, "[" + GetDate(startDate) + " TO " + DateField.DateToString(endDateExpected) + "]");
            AssertQueryEquals("{  " + startDate + "    " + endDate + "   }", null, "{" + GetDate(startDate) + " TO " + GetDate(endDate) + "}");
        }
		
		[Test]
        public virtual void  TestEscaped()
		{
			Analyzer a = new WhitespaceAnalyzer();
			
			/*assertQueryEquals("\\[brackets", a, "\\[brackets");
			assertQueryEquals("\\[brackets", null, "brackets");
			assertQueryEquals("\\\\", a, "\\\\");
			assertQueryEquals("\\+blah", a, "\\+blah");
			assertQueryEquals("\\(blah", a, "\\(blah");
			
			assertQueryEquals("\\-blah", a, "\\-blah");
			assertQueryEquals("\\!blah", a, "\\!blah");
			assertQueryEquals("\\{blah", a, "\\{blah");
			assertQueryEquals("\\}blah", a, "\\}blah");
			assertQueryEquals("\\:blah", a, "\\:blah");
			assertQueryEquals("\\^blah", a, "\\^blah");
			assertQueryEquals("\\[blah", a, "\\[blah");
			assertQueryEquals("\\]blah", a, "\\]blah");
			assertQueryEquals("\\\"blah", a, "\\\"blah");
			assertQueryEquals("\\(blah", a, "\\(blah");
			assertQueryEquals("\\)blah", a, "\\)blah");
			assertQueryEquals("\\~blah", a, "\\~blah");
			assertQueryEquals("\\*blah", a, "\\*blah");
			assertQueryEquals("\\?blah", a, "\\?blah");
			//assertQueryEquals("foo \\&\\& bar", a, "foo \\&\\& bar");
			//assertQueryEquals("foo \\|| bar", a, "foo \\|| bar");
			//assertQueryEquals("foo \\AND bar", a, "foo \\AND bar");*/
			
			AssertQueryEquals("a\\-b:c", a, "a-b:c");
			AssertQueryEquals("a\\+b:c", a, "a+b:c");
			AssertQueryEquals("a\\:b:c", a, "a:b:c");
			AssertQueryEquals("a\\\\b:c", a, "a\\b:c");
			
			AssertQueryEquals("a:b\\-c", a, "a:b-c");
			AssertQueryEquals("a:b\\+c", a, "a:b+c");
			AssertQueryEquals("a:b\\:c", a, "a:b:c");
			AssertQueryEquals("a:b\\\\c", a, "a:b\\c");
			
			AssertQueryEquals("a:b\\-c*", a, "a:b-c*");
			AssertQueryEquals("a:b\\+c*", a, "a:b+c*");
			AssertQueryEquals("a:b\\:c*", a, "a:b:c*");
			
			AssertQueryEquals("a:b\\\\c*", a, "a:b\\c*");
			
			AssertQueryEquals("a:b\\-?c", a, "a:b-?c");
			AssertQueryEquals("a:b\\+?c", a, "a:b+?c");
			AssertQueryEquals("a:b\\:?c", a, "a:b:?c");
			
			AssertQueryEquals("a:b\\\\?c", a, "a:b\\?c");
			
			AssertQueryEquals("a:b\\-c~", a, "a:b-c~0.5");
			AssertQueryEquals("a:b\\+c~", a, "a:b+c~0.5");
			AssertQueryEquals("a:b\\:c~", a, "a:b:c~0.5");
			AssertQueryEquals("a:b\\\\c~", a, "a:b\\c~0.5");
			
			AssertQueryEquals("[ a\\- TO a\\+ ]", null, "[a- TO a+]");
			AssertQueryEquals("[ a\\: TO a\\~ ]", null, "[a: TO a~]");
			AssertQueryEquals("[ a\\\\ TO a\\* ]", null, "[a\\ TO a*]");
		}
		
        [Test]
        public virtual void  TestQueryStringEscaping()
        {
            Analyzer a = new WhitespaceAnalyzer();
			
            AssertEscapedQueryEquals("a-b:c", a, "a\\-b\\:c");
            AssertEscapedQueryEquals("a+b:c", a, "a\\+b\\:c");
            AssertEscapedQueryEquals("a:b:c", a, "a\\:b\\:c");
            AssertEscapedQueryEquals("a\\b:c", a, "a\\\\b\\:c");
			
            AssertEscapedQueryEquals("a:b-c", a, "a\\:b\\-c");
            AssertEscapedQueryEquals("a:b+c", a, "a\\:b\\+c");
            AssertEscapedQueryEquals("a:b:c", a, "a\\:b\\:c");
            AssertEscapedQueryEquals("a:b\\c", a, "a\\:b\\\\c");
			
            AssertEscapedQueryEquals("a:b-c*", a, "a\\:b\\-c\\*");
            AssertEscapedQueryEquals("a:b+c*", a, "a\\:b\\+c\\*");
            AssertEscapedQueryEquals("a:b:c*", a, "a\\:b\\:c\\*");
			
            AssertEscapedQueryEquals("a:b\\\\c*", a, "a\\:b\\\\\\\\c\\*");
			
            AssertEscapedQueryEquals("a:b-?c", a, "a\\:b\\-\\?c");
            AssertEscapedQueryEquals("a:b+?c", a, "a\\:b\\+\\?c");
            AssertEscapedQueryEquals("a:b:?c", a, "a\\:b\\:\\?c");
			
            AssertEscapedQueryEquals("a:b?c", a, "a\\:b\\?c");
			
            AssertEscapedQueryEquals("a:b-c~", a, "a\\:b\\-c\\~");
            AssertEscapedQueryEquals("a:b+c~", a, "a\\:b\\+c\\~");
            AssertEscapedQueryEquals("a:b:c~", a, "a\\:b\\:c\\~");
            AssertEscapedQueryEquals("a:b\\c~", a, "a\\:b\\\\c\\~");
			
            AssertEscapedQueryEquals("[ a - TO a+ ]", null, "\\[ a \\- TO a\\+ \\]");
            AssertEscapedQueryEquals("[ a : TO a~ ]", null, "\\[ a \\: TO a\\~ \\]");
            AssertEscapedQueryEquals("[ a\\ TO a* ]", null, "\\[ a\\\\ TO a\\* \\]");
        }
		
        [Test]
        public virtual void  TestTabNewlineCarriageReturn()
		{
			AssertQueryEqualsDOA("+weltbank +worlbank", null, "+weltbank +worlbank");
			
			AssertQueryEqualsDOA("+weltbank\n+worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \n+worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \n +worlbank", null, "+weltbank +worlbank");
			
			AssertQueryEqualsDOA("+weltbank\r+worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \r+worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \r +worlbank", null, "+weltbank +worlbank");
			
			AssertQueryEqualsDOA("+weltbank\r\n+worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \r\n+worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \r\n +worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \r \n +worlbank", null, "+weltbank +worlbank");
			
			AssertQueryEqualsDOA("+weltbank\t+worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \t+worlbank", null, "+weltbank +worlbank");
			AssertQueryEqualsDOA("weltbank \t +worlbank", null, "+weltbank +worlbank");
		}
		
		[Test]
        public virtual void  TestSimpleDAO()
		{
			AssertQueryEqualsDOA("term term term", null, "+term +term +term");
			AssertQueryEqualsDOA("term +term term", null, "+term +term +term");
			AssertQueryEqualsDOA("term term +term", null, "+term +term +term");
			AssertQueryEqualsDOA("term +term +term", null, "+term +term +term");
			AssertQueryEqualsDOA("-term term term", null, "-term +term +term");
		}
		
		[Test]
        public virtual void  TestBoost()
		{
			StandardAnalyzer oneStopAnalyzer = new StandardAnalyzer(new System.String[]{"on"});
			Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser("field", oneStopAnalyzer);
			Query q = qp.Parse("on^1.0");
			Assert.IsNotNull(q);
			q = qp.Parse("\"hello\"^2.0");
			Assert.IsNotNull(q);
			Assert.AreEqual(q.GetBoost(), (float) 2.0, (float) 0.5);
			q = qp.Parse("hello^2.0");
			Assert.IsNotNull(q);
			Assert.AreEqual(q.GetBoost(), (float) 2.0, (float) 0.5);
			q = qp.Parse("\"on\"^1.0");
			Assert.IsNotNull(q);
			
			Lucene.Net.QueryParsers.QueryParser qp2 = new Lucene.Net.QueryParsers.QueryParser("field", new StandardAnalyzer());
			q = qp2.Parse("the^3");
			// "the" is a stop word so the result is an empty query:
			Assert.IsNotNull(q);
			Assert.AreEqual("", q.ToString());
			Assert.AreEqual(1.0f, q.GetBoost(), 0.01f);
		}
		
		[Test]
        public virtual void  TestException()
		{
			try
			{
				AssertQueryEquals("\"some phrase", null, "abc");
				Assert.Fail("ParseException expected, not thrown");
			}
			catch (ParseException expected)
			{
			}
		}
		
		[Test]
        public virtual void  TestCustomQueryParserWildcard()
		{
			try
			{
				new QPTestParser("contents", new WhitespaceAnalyzer()).Parse("a?t");
				Assert.Fail("Wildcard queries should not be allowed");
			}
			catch (ParseException expected)
			{
				// expected exception
			}
		}
		
		[Test]
        public virtual void  TestCustomQueryParserFuzzy()
		{
			try
			{
				new QPTestParser("contents", new WhitespaceAnalyzer()).Parse("xunit~");
				Assert.Fail("Fuzzy queries should not be allowed");
			}
			catch (ParseException expected)
			{
				// expected exception
			}
		}
		
		[Test]
        public virtual void  TestBooleanQuery()
		{
			BooleanQuery.SetMaxClauseCount(2);
			try
			{
				Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser("field", new WhitespaceAnalyzer());
				qp.Parse("one two three");
				Assert.Fail("ParseException expected due to too many boolean clauses");
			}
			catch (ParseException expected)
			{
				// too many boolean clauses, so ParseException is expected
			}
		}
		
		/// <summary> This test differs from TestPrecedenceQueryParser</summary>
		[Test]
        public virtual void  TestPrecedence()
		{
			Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser("field", new WhitespaceAnalyzer());
			Query query1 = qp.Parse("A AND B OR C AND D");
			Query query2 = qp.Parse("+A +B +C +D");
			Assert.AreEqual(query1, query2);
		}
		
        [Test]
        public virtual void  TestLocalDateFormat()
        {
            Lucene.Net.Store.RAMDirectory ramDir = new Lucene.Net.Store.RAMDirectory();
            Lucene.Net.Index.IndexWriter iw = new Lucene.Net.Index.IndexWriter(ramDir, new WhitespaceAnalyzer(), true);
            AddDateDoc("a", 2005, 12, 2, 10, 15, 33, iw);
            AddDateDoc("b", 2005, 12, 4, 22, 15, 0, iw);
            iw.Close();
            Lucene.Net.Search.IndexSearcher is_Renamed = new Lucene.Net.Search.IndexSearcher(ramDir);
            AssertHits(1, "[12/1/2005 TO 12/3/2005]", is_Renamed);
            AssertHits(2, "[12/1/2005 TO 12/4/2005]", is_Renamed);
            AssertHits(1, "[12/3/2005 TO 12/4/2005]", is_Renamed);
            AssertHits(1, "{12/1/2005 TO 12/3/2005}", is_Renamed);
            AssertHits(1, "{12/1/2005 TO 12/4/2005}", is_Renamed);
            AssertHits(0, "{12/3/2005 TO 12/4/2005}", is_Renamed);
            is_Renamed.Close();
        }
		
        private void  AssertHits(int expected, System.String query, Lucene.Net.Search.IndexSearcher is_Renamed)
        {
            Lucene.Net.QueryParsers.QueryParser qp = new Lucene.Net.QueryParsers.QueryParser("date", new WhitespaceAnalyzer());
            qp.SetLocale(new System.Globalization.CultureInfo("en"));
            Query q = qp.Parse(query);
            Lucene.Net.Search.Hits hits = is_Renamed.Search(q);
            Assert.AreEqual(expected, hits.Length());
        }
		
        private static void  AddDateDoc(System.String content, int year, int month, int day, int hour, int minute, int second, Lucene.Net.Index.IndexWriter iw)
        {
            Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
            d.Add(new Lucene.Net.Documents.Field("f", content, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.TOKENIZED));
            System.DateTime tempAux = new System.DateTime(year, month, day, hour, minute, second);
            d.Add(new Lucene.Net.Documents.Field("date", DateField.DateToString(tempAux), Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.UN_TOKENIZED));
            iw.AddDocument(d);
        }
		
        [TearDown]
		public virtual void  TearDown()
		{
			BooleanQuery.SetMaxClauseCount(originalMaxClauses);
		}
	}
}