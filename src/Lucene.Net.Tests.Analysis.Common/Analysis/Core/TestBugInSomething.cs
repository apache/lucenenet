using System;
using System.Collections.Generic;

namespace org.apache.lucene.analysis.core
{


	using MappingCharFilter = org.apache.lucene.analysis.charfilter.MappingCharFilter;
	using NormalizeCharMap = org.apache.lucene.analysis.charfilter.NormalizeCharMap;
	using CommonGramsFilter = org.apache.lucene.analysis.commongrams.CommonGramsFilter;
	using WordDelimiterFilter = org.apache.lucene.analysis.miscellaneous.WordDelimiterFilter;
	using EdgeNGramTokenizer = org.apache.lucene.analysis.ngram.EdgeNGramTokenizer;
	using NGramTokenFilter = org.apache.lucene.analysis.ngram.NGramTokenFilter;
	using ShingleFilter = org.apache.lucene.analysis.shingle.ShingleFilter;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using WikipediaTokenizer = org.apache.lucene.analysis.wikipedia.WikipediaTokenizer;
	using SuppressCodecs = org.apache.lucene.util.LuceneTestCase.SuppressCodecs;

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

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Direct") public class TestBugInSomething extends org.apache.lucene.analysis.BaseTokenStreamTestCase
	public class TestBugInSomething : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws Exception
	  public virtual void test()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.util.CharArraySet cas = new org.apache.lucene.analysis.util.CharArraySet(TEST_VERSION_CURRENT, 3, false);
		CharArraySet cas = new CharArraySet(TEST_VERSION_CURRENT, 3, false);
		cas.add("jjp");
		cas.add("wlmwoknt");
		cas.add("tcgyreo");

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.charfilter.NormalizeCharMap.Builder builder = new org.apache.lucene.analysis.charfilter.NormalizeCharMap.Builder();
		NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		builder.add("mtqlpi", "");
		builder.add("mwoknt", "jjp");
		builder.add("tcgyreo", "zpfpajyws");
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.charfilter.NormalizeCharMap map = builder.build();
		NormalizeCharMap map = builder.build();

		Analyzer a = new AnalyzerAnonymousInnerClassHelper(this, cas, map);
		checkAnalysisConsistency(random(), a, false, "wmgddzunizdomqyj");
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestBugInSomething outerInstance;

		  private CharArraySet cas;
		  private NormalizeCharMap map;

		  public AnalyzerAnonymousInnerClassHelper(TestBugInSomething outerInstance, CharArraySet cas, NormalizeCharMap map)
		  {
			  this.outerInstance = outerInstance;
			  this.cas = cas;
			  this.map = map;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new MockTokenizer(new TestRandomChains.CheckThatYouDidntReadAnythingReaderWrapper(reader), MockTokenFilter.ENGLISH_STOPSET, false, -65);
			TokenFilter f = new CommonGramsFilter(TEST_VERSION_CURRENT, t, cas);
			return new TokenStreamComponents(t, f);
		  }

		  protected internal override Reader initReader(string fieldName, Reader reader)
		  {
			reader = new MockCharFilter(reader, 0);
			reader = new MappingCharFilter(map, reader);
			return reader;
		  }
	  }

	  internal CharFilter wrappedStream = new CharFilterAnonymousInnerClassHelper(new StringReader("bogus"));

	  private class CharFilterAnonymousInnerClassHelper : CharFilter
	  {
		  public CharFilterAnonymousInnerClassHelper(StringReader java) : base(StringReader)
		  {
		  }


		  public override void mark(int readAheadLimit)
		  {
			throw new System.NotSupportedException("mark(int)");
		  }

		  public override bool markSupported()
		  {
			throw new System.NotSupportedException("markSupported()");
		  }

		  public override int read()
		  {
			throw new System.NotSupportedException("read()");
		  }

		  public override int read(char[] cbuf)
		  {
			throw new System.NotSupportedException("read(char[])");
		  }

		  public override int read(CharBuffer target)
		  {
			throw new System.NotSupportedException("read(CharBuffer)");
		  }

		  public override bool ready()
		  {
			throw new System.NotSupportedException("ready()");
		  }

		  public override void reset()
		  {
			throw new System.NotSupportedException("reset()");
		  }

		  public override long skip(long n)
		  {
			throw new System.NotSupportedException("skip(long)");
		  }

		  public override int correct(int currentOff)
		  {
			throw new System.NotSupportedException("correct(int)");
		  }

		  public override void close()
		  {
			throw new System.NotSupportedException("close()");
		  }

		  public override int read(char[] arg0, int arg1, int arg2)
		  {
			throw new System.NotSupportedException("read(char[], int, int)");
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWrapping() throws Exception
	  public virtual void testWrapping()
	  {
		CharFilter cs = new TestRandomChains.CheckThatYouDidntReadAnythingReaderWrapper(wrappedStream);
		try
		{
		  cs.mark(1);
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("mark(int)", e.Message);
		}

		try
		{
		  cs.markSupported();
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("markSupported()", e.Message);
		}

		try
		{
		  cs.read();
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("read()", e.Message);
		}

		try
		{
		  cs.read(new char[0]);
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("read(char[])", e.Message);
		}

		try
		{
		  cs.read(CharBuffer.wrap(new char[0]));
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("read(CharBuffer)", e.Message);
		}

		try
		{
		  cs.reset();
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("reset()", e.Message);
		}

		try
		{
		  cs.skip(1);
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("skip(long)", e.Message);
		}

		try
		{
		  cs.correctOffset(1);
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("correct(int)", e.Message);
		}

		try
		{
		  cs.close();
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("close()", e.Message);
		}

		try
		{
		  cs.read(new char[0], 0, 0);
		  fail();
		}
		catch (Exception e)
		{
		  assertEquals("read(char[], int, int)", e.Message);
		}
	  }

	  // todo: test framework?

	  internal sealed class SopTokenFilter : TokenFilter
	  {

		internal SopTokenFilter(TokenStream input) : base(input)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  if (input.incrementToken())
		  {
			Console.WriteLine(input.GetType().Name + "->" + this.reflectAsString(false));
			return true;
		  }
		  else
		  {
			return false;
		  }
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void end() throws java.io.IOException
		public override void end()
		{
		  base.end();
		  Console.WriteLine(input.GetType().Name + ".end()");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
		public override void close()
		{
		  base.close();
		  Console.WriteLine(input.GetType().Name + ".close()");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void reset() throws java.io.IOException
		public override void reset()
		{
		  base.reset();
		  Console.WriteLine(input.GetType().Name + ".reset()");
		}
	  }

	  // LUCENE-5269
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUnicodeShinglesAndNgrams() throws Exception
	  public virtual void testUnicodeShinglesAndNgrams()
	  {
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this);
		checkRandomData(random(), analyzer, 2000);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestBugInSomething outerInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestBugInSomething outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new EdgeNGramTokenizer(TEST_VERSION_CURRENT, reader, 2, 94);
			//TokenStream stream = new SopTokenFilter(tokenizer);
			TokenStream stream = new ShingleFilter(tokenizer, 5);
			//stream = new SopTokenFilter(stream);
			stream = new NGramTokenFilter(TEST_VERSION_CURRENT, stream, 55, 83);
			//stream = new SopTokenFilter(stream);
			return new TokenStreamComponents(tokenizer, stream);
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCuriousWikipediaString() throws Exception
	  public virtual void testCuriousWikipediaString()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.util.CharArraySet protWords = new org.apache.lucene.analysis.util.CharArraySet(TEST_VERSION_CURRENT, new java.util.HashSet<>(java.util.Arrays.asList("rrdpafa", "pupmmlu", "xlq", "dyy", "zqrxrrck", "o", "hsrlfvcha")), false);
		CharArraySet protWords = new CharArraySet(TEST_VERSION_CURRENT, new HashSet<>(Arrays.asList("rrdpafa", "pupmmlu", "xlq", "dyy", "zqrxrrck", "o", "hsrlfvcha")), false);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte table[] = new byte[] { -57, 26, 1, 48, 63, -23, 55, -84, 18, 120, -97, 103, 58, 13, 84, 89, 57, -13, -63, 5, 28, 97, -54, -94, 102, -108, -5, 5, 46, 40, 43, 78, 43, -72, 36, 29, 124, -106, -22, -51, 65, 5, 31, -42, 6, -99, 97, 14, 81, -128, 74, 100, 54, -55, -25, 53, -71, -98, 44, 33, 86, 106, -42, 47, 115, -89, -18, -26, 22, -95, -43, 83, -125, 105, -104, -24, 106, -16, 126, 115, -105, 97, 65, -33, 57, 44, -1, 123, -68, 100, 13, -41, -64, -119, 0, 92, 94, -36, 53, -9, -102, -18, 90, 94, -26, 31, 71, -20 };
		sbyte[] table = new sbyte[] {-57, 26, 1, 48, 63, -23, 55, -84, 18, 120, -97, 103, 58, 13, 84, 89, 57, -13, -63, 5, 28, 97, -54, -94, 102, -108, -5, 5, 46, 40, 43, 78, 43, -72, 36, 29, 124, -106, -22, -51, 65, 5, 31, -42, 6, -99, 97, 14, 81, -128, 74, 100, 54, -55, -25, 53, -71, -98, 44, 33, 86, 106, -42, 47, 115, -89, -18, -26, 22, -95, -43, 83, -125, 105, -104, -24, 106, -16, 126, 115, -105, 97, 65, -33, 57, 44, -1, 123, -68, 100, 13, -41, -64, -119, 0, 92, 94, -36, 53, -9, -102, -18, 90, 94, -26, 31, 71, -20};
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, protWords, table);
		checkAnalysisConsistency(random(), a, false, "B\u28c3\ue0f8[ \ud800\udfc2 </p> jb");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestBugInSomething outerInstance;

		  private CharArraySet protWords;
		  private sbyte[] table;

		  public AnalyzerAnonymousInnerClassHelper2(TestBugInSomething outerInstance, CharArraySet protWords, sbyte[] table)
		  {
			  this.outerInstance = outerInstance;
			  this.protWords = protWords;
			  this.table = table;
		  }

		  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new WikipediaTokenizer(reader);
			TokenStream stream = new SopTokenFilter(tokenizer);
			stream = new WordDelimiterFilter(TEST_VERSION_CURRENT, stream, table, -50, protWords);
			stream = new SopTokenFilter(stream);
			return new TokenStreamComponents(tokenizer, stream);
		  }
	  }
	}

}