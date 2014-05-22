using System;

namespace Lucene.Net.Analysis
{

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


	using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using Fields = Lucene.Net.Index.Fields;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Automaton = Lucene.Net.Util.Automaton.Automaton;
	using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
	using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
	using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
	using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using System.IO;
    using NUnit.Framework;


	public class TestMockAnalyzer : BaseTokenStreamTestCase
	{

	  /// <summary>
	  /// Test a configuration that behaves a lot like WhitespaceAnalyzer </summary>
	  public virtual void TestWhitespace()
	  {
		Analyzer a = new MockAnalyzer(new Random());
		AssertAnalyzesTo(a, "A bc defg hiJklmn opqrstuv wxy z ", new string[] {"a", "bc", "defg", "hijklmn", "opqrstuv", "wxy", "z"});
		AssertAnalyzesTo(a, "aba cadaba shazam", new string[] {"aba", "cadaba", "shazam"});
		AssertAnalyzesTo(a, "break on whitespace", new string[] {"break", "on", "whitespace"});
	  }

	  /// <summary>
	  /// Test a configuration that behaves a lot like SimpleAnalyzer </summary>
	  public virtual void TestSimple()
	  {
		Analyzer a = new MockAnalyzer(new Random(), MockTokenizer.SIMPLE, true);
		AssertAnalyzesTo(a, "a-bc123 defg+hijklmn567opqrstuv78wxy_z ", new string[] {"a", "bc", "defg", "hijklmn", "opqrstuv", "wxy", "z"});
		AssertAnalyzesTo(a, "aba4cadaba-Shazam", new string[] {"aba", "cadaba", "shazam"});
		AssertAnalyzesTo(a, "break+on/Letters", new string[] {"break", "on", "letters"});
	  }

	  /// <summary>
	  /// Test a configuration that behaves a lot like KeywordAnalyzer </summary>
	  public virtual void TestKeyword()
	  {
		Analyzer a = new MockAnalyzer(new Random(), MockTokenizer.KEYWORD, false);
		AssertAnalyzesTo(a, "a-bc123 defg+hijklmn567opqrstuv78wxy_z ", new string[] {"a-bc123 defg+hijklmn567opqrstuv78wxy_z "});
		AssertAnalyzesTo(a, "aba4cadaba-Shazam", new string[] {"aba4cadaba-Shazam"});
		AssertAnalyzesTo(a, "break+on/Nothing", new string[] {"break+on/Nothing"});
		// currently though emits no tokens for empty string: maybe we can do it,
		// but we don't want to emit tokens infinitely...
		AssertAnalyzesTo(a, "", new string[0]);
	  }

	  // Test some regular expressions as tokenization patterns
	  /// <summary>
	  /// Test a configuration where each character is a term </summary>
	  public virtual void TestSingleChar()
	  {
		CharacterRunAutomaton single = new CharacterRunAutomaton((new RegExp(".")).ToAutomaton());
        Analyzer a = new MockAnalyzer(new Random(), single, false);
		AssertAnalyzesTo(a, "foobar", new string[] {"f", "o", "o", "b", "a", "r"}, new int[] {0, 1, 2, 3, 4, 5}, new int[] {1, 2, 3, 4, 5, 6});
        CheckRandomData(new Random(), a, 100);
	  }

	  /// <summary>
	  /// Test a configuration where two characters makes a term </summary>
	  public virtual void TestTwoChars()
	  {
		CharacterRunAutomaton single = new CharacterRunAutomaton((new RegExp("..")).ToAutomaton());
        Analyzer a = new MockAnalyzer(new Random(), single, false);
		AssertAnalyzesTo(a, "foobar", new string[] {"fo", "ob", "ar"}, new int[] {0, 2, 4}, new int[] {2, 4, 6});
		// make sure when last term is a "partial" match that End() is correct
		AssertTokenStreamContents(a.tokenStream("bogus", "fooba"), new string[] {"fo", "ob"}, new int[] {0, 2}, new int[] {2, 4}, new int[] {1, 1}, new int?(5)
	   );
        CheckRandomData(new Random(), a, 100);
	  }

	  /// <summary>
	  /// Test a configuration where three characters makes a term </summary>
	  public virtual void TestThreeChars()
	  {
		CharacterRunAutomaton single = new CharacterRunAutomaton((new RegExp("...")).ToAutomaton());
        Analyzer a = new MockAnalyzer(new Random(), single, false);
		AssertAnalyzesTo(a, "foobar", new string[] {"foo", "bar"}, new int[] {0, 3}, new int[] {3, 6});
		// make sure when last term is a "partial" match that End() is correct
		AssertTokenStreamContents(a.tokenStream("bogus", "fooba"), new string[] {"foo"}, new int[] {0}, new int[] {3}, new int[] {1}, new int?(5)
	   );
        CheckRandomData(new Random(), a, 100);
	  }

	  /// <summary>
	  /// Test a configuration where word starts with one uppercase </summary>
	  public virtual void TestUppercase()
	  {
		CharacterRunAutomaton single = new CharacterRunAutomaton((new RegExp("[A-Z][a-z]*")).ToAutomaton());
        Analyzer a = new MockAnalyzer(new Random(), single, false);
		AssertAnalyzesTo(a, "FooBarBAZ", new string[] {"Foo", "Bar", "B", "A", "Z"}, new int[] {0, 3, 6, 7, 8}, new int[] {3, 6, 7, 8, 9});
		AssertAnalyzesTo(a, "aFooBar", new string[] {"Foo", "Bar"}, new int[] {1, 4}, new int[] {4, 7});
        CheckRandomData(new Random(), a, 100);
	  }

	  /// <summary>
	  /// Test a configuration that behaves a lot like StopAnalyzer </summary>
	  public virtual void TestStop()
	  {
          Analyzer a = new MockAnalyzer(new Random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
		AssertAnalyzesTo(a, "the quick brown a fox", new string[] {"quick", "brown", "fox"}, new int[] {2, 1, 2});
	  }

	  /// <summary>
	  /// Test a configuration that behaves a lot like KeepWordFilter </summary>
	  public virtual void TestKeep()
	  {
		CharacterRunAutomaton keepWords = new CharacterRunAutomaton(BasicOperations.Complement(Automaton.Union(Arrays.asList(BasicAutomata.MakeString("foo"), BasicAutomata.MakeString("bar")))));
        Analyzer a = new MockAnalyzer(new Random(), MockTokenizer.SIMPLE, true, keepWords);
		AssertAnalyzesTo(a, "quick foo brown bar bar fox foo", new string[] {"foo", "bar", "bar", "foo"}, new int[] {2, 2, 1, 2});
	  }

	  /// <summary>
	  /// Test a configuration that behaves a lot like LengthFilter </summary>
	  public virtual void TestLength()
	  {
		CharacterRunAutomaton length5 = new CharacterRunAutomaton((new RegExp(".{5,}")).ToAutomaton());
        Analyzer a = new MockAnalyzer(new Random(), MockTokenizer.WHITESPACE, true, length5);
		AssertAnalyzesTo(a, "ok toolong fine notfine", new string[] {"ok", "fine"}, new int[] {1, 2});
	  }

	  /// <summary>
	  /// Test MockTokenizer encountering a too long token </summary>
	  public virtual void TestTooLongToken()
	  {
		Analyzer whitespace = new AnalyzerAnonymousInnerClassHelper(this);

		AssertTokenStreamContents(whitespace.tokenStream("bogus", "test 123 toolong ok "), new string[] {"test", "123", "toolo", "ng", "ok"}, new int[] {0, 5, 9, 14, 17}, new int[] {4, 8, 14, 16, 19}, new int?(20));

		AssertTokenStreamContents(whitespace.tokenStream("bogus", "test 123 toolo"), new string[] {"test", "123", "toolo"}, new int[] {0, 5, 9}, new int[] {4, 8, 14}, new int?(14));
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly TestMockAnalyzer OuterInstance;

		  public AnalyzerAnonymousInnerClassHelper(TestMockAnalyzer outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false, 5);
			return new TokenStreamComponents(t, t);
		  }
	  }

	  public virtual void TestLUCENE_3042()
	  {
		string testString = "t";

        Analyzer analyzer = new MockAnalyzer(new Random());
		Exception priorException = null;
		TokenStream stream = analyzer.tokenStream("dummy", testString);
		try
		{
		  stream.Reset();
		  while (stream.IncrementToken())
		  {
			// consume
		  }
		  stream.End();
		}
		catch (Exception e)
		{
		  priorException = e;
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(priorException, stream);
		}

		AssertAnalyzesTo(analyzer, testString, new string[] {"t"});
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
	  public virtual void TestRandomStrings()
	  {
          CheckRandomData(new Random(), new MockAnalyzer(new Random()), atLeast(1000));
	  }

	  /// <summary>
	  /// blast some random strings through differently configured tokenizers </summary>
	  public virtual void TestRandomRegexps()
	  {
		int iters = atLeast(30);
		for (int i = 0; i < iters; i++)
		{
		  CharacterRunAutomaton dfa = new CharacterRunAutomaton(AutomatonTestUtil.RandomAutomaton(Random()));
		  bool lowercase = Random().nextBoolean();
          int limit = TestUtil.Next(new Random(), 0, 500);
		  Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, dfa, lowercase, limit);
          CheckRandomData(new Random(), a, 100);
		  a.Close();
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestMockAnalyzer OuterInstance;

		  private CharacterRunAutomaton Dfa;
		  private bool Lowercase;
		  private int Limit;

		  public AnalyzerAnonymousInnerClassHelper2(TestMockAnalyzer outerInstance, CharacterRunAutomaton dfa, bool lowercase, int limit)
		  {
			  this.OuterInstance = outerInstance;
			  this.Dfa = dfa;
			  this.Lowercase = lowercase;
			  this.Limit = limit;
		  }

		  protected internal override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			Tokenizer t = new MockTokenizer(reader, Dfa, Lowercase, Limit);
			return new TokenStreamComponents(t, t);
		  }
	  }

	  public virtual void TestForwardOffsets()
	  {
		int num = atLeast(10000);
		for (int i = 0; i < num; i++)
		{
            string s = TestUtil.RandomHtmlishString(new Random(), 20);
		  StringReader reader = new StringReader(s);
		  MockCharFilter charfilter = new MockCharFilter(reader, 2);
		  MockAnalyzer analyzer = new MockAnalyzer(Random());
		  Exception priorException = null;
		  TokenStream ts = analyzer.tokenStream("bogus", charfilter);
		  try
		  {
			ts.Reset();
			while (ts.IncrementToken())
			{
			  ;
			}
			ts.End();
		  }
		  catch (Exception e)
		  {
			priorException = e;
		  }
		  finally
		  {
			IOUtils.closeWhileHandlingException(priorException, ts);
		  }
		}
	  }

	  public virtual void TestWrapReader()
	  {
		// LUCENE-5153: test that wrapping an analyzer's reader is allowed
          Random random = new Random();

		Analyzer @delegate = new MockAnalyzer(random);
		Analyzer a = new AnalyzerWrapperAnonymousInnerClassHelper(this, @delegate.ReuseStrategy, @delegate);

		CheckOneTerm(a, "abc", "aabc");
	  }

	  private class AnalyzerWrapperAnonymousInnerClassHelper : AnalyzerWrapper
	  {
		  private readonly TestMockAnalyzer OuterInstance;

		  private Analyzer @delegate;

		  public AnalyzerWrapperAnonymousInnerClassHelper(TestMockAnalyzer outerInstance, UnknownType getReuseStrategy, Analyzer @delegate) : base(getReuseStrategy)
		  {
			  this.OuterInstance = outerInstance;
			  this.@delegate = @delegate;
		  }


		  protected internal override Reader WrapReader(string fieldName, Reader reader)
		  {
			return new MockCharFilter(reader, 7);
		  }

		  protected internal override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
		  {
			return components;
		  }

		  protected internal override Analyzer GetWrappedAnalyzer(string fieldName)
		  {
			return @delegate;
		  }
	  }

	  public virtual void TestChangeGaps()
	  {
		// LUCENE-5324: check that it is possible to change the wrapper's gaps
		int positionGap = Random().Next(1000);
		int offsetGap = Random().Next(1000);
        Analyzer @delegate = new MockAnalyzer(new Random());
		Analyzer a = new AnalyzerWrapperAnonymousInnerClassHelper2(this, @delegate.ReuseStrategy, positionGap, offsetGap, @delegate);

		RandomIndexWriter writer = new RandomIndexWriter(Random(), newDirectory());
		Document doc = new Document();
		FieldType ft = new FieldType();
		ft.Indexed = true;
		ft.IndexOptions = IndexOptions.DOCS_ONLY;
		ft.Tokenized = true;
		ft.StoreTermVectors = true;
		ft.StoreTermVectorPositions = true;
		ft.StoreTermVectorOffsets = true;
		doc.Add(new Field("f", "a", ft));
		doc.Add(new Field("f", "a", ft));
		writer.addDocument(doc, a);
		AtomicReader reader = GetOnlySegmentReader(writer.Reader);
		Fields fields = reader.GetTermVectors(0);
		Terms terms = fields.Terms("f");
		TermsEnum te = terms.Iterator(null);
		Assert.AreEqual(new BytesRef("a"), te.Next());
		DocsAndPositionsEnum dpe = te.DocsAndPositions(null, null);
		Assert.AreEqual(0, dpe.NextDoc());
		Assert.AreEqual(2, dpe.Freq());
		Assert.AreEqual(0, dpe.NextPosition());
		Assert.AreEqual(0, dpe.StartOffset());
		int endOffset = dpe.EndOffset();
		Assert.AreEqual(1 + positionGap, dpe.NextPosition());
		Assert.AreEqual(1 + endOffset + offsetGap, dpe.EndOffset());
		Assert.AreEqual(null, te.Next());
		reader.Close();
		writer.Close();
		writer.w.Directory.Close();
	  }

	  private class AnalyzerWrapperAnonymousInnerClassHelper2 : AnalyzerWrapper
	  {
		  private readonly TestMockAnalyzer OuterInstance;

		  private int PositionGap;
		  private int OffsetGap;
		  private Analyzer @delegate;

		  public AnalyzerWrapperAnonymousInnerClassHelper2(TestMockAnalyzer outerInstance, UnknownType getReuseStrategy, int positionGap, int offsetGap, Analyzer @delegate) : base(getReuseStrategy)
		  {
			  this.OuterInstance = outerInstance;
			  this.PositionGap = positionGap;
			  this.OffsetGap = offsetGap;
			  this.@delegate = @delegate;
		  }

		  protected internal override Analyzer GetWrappedAnalyzer(string fieldName)
		  {
			return @delegate;
		  }
		  public override int GetPositionIncrementGap(string fieldName)
		  {
			return PositionGap;
		  }
		  public override int GetOffsetGap(string fieldName)
		  {
			return OffsetGap;
		  }
	  }

	}

}