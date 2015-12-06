using System;
using System.Text;

namespace org.apache.lucene.analysis.sinks
{

	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>


	using org.apache.lucene.analysis;
	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using StandardFilter = org.apache.lucene.analysis.standard.StandardFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = org.apache.lucene.analysis.tokenattributes.PositionIncrementAttribute;
	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using FieldType = org.apache.lucene.document.FieldType;
	using TextField = org.apache.lucene.document.TextField;
	using DirectoryReader = org.apache.lucene.index.DirectoryReader;
	using DocsAndPositionsEnum = org.apache.lucene.index.DocsAndPositionsEnum;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using IndexWriter = org.apache.lucene.index.IndexWriter;
	using Terms = org.apache.lucene.index.Terms;
	using TermsEnum = org.apache.lucene.index.TermsEnum;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using Directory = org.apache.lucene.store.Directory;
	using AttributeSource = org.apache.lucene.util.AttributeSource;
	using English = org.apache.lucene.util.English;


	/// <summary>
	/// tests for the TestTeeSinkTokenFilter
	/// </summary>
	public class TestTeeSinkTokenFilter : BaseTokenStreamTestCase
	{
	  protected internal StringBuilder buffer1;
	  protected internal StringBuilder buffer2;
	  protected internal string[] tokens1;
	  protected internal string[] tokens2;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		tokens1 = new string[]{"The", "quick", "Burgundy", "Fox", "jumped", "over", "the", "lazy", "Red", "Dogs"};
		tokens2 = new string[]{"The", "Lazy", "Dogs", "should", "stay", "on", "the", "porch"};
		buffer1 = new StringBuilder();

		for (int i = 0; i < tokens1.Length; i++)
		{
		  buffer1.Append(tokens1[i]).Append(' ');
		}
		buffer2 = new StringBuilder();
		for (int i = 0; i < tokens2.Length; i++)
		{
		  buffer2.Append(tokens2[i]).Append(' ');
		}
	  }

	  internal static readonly TeeSinkTokenFilter.SinkFilter theFilter = new SinkFilterAnonymousInnerClassHelper();

	  private class SinkFilterAnonymousInnerClassHelper : TeeSinkTokenFilter.SinkFilter
	  {
		  public SinkFilterAnonymousInnerClassHelper()
		  {
		  }

		  public override bool accept(AttributeSource a)
		  {
			CharTermAttribute termAtt = a.getAttribute(typeof(CharTermAttribute));
			return termAtt.ToString().Equals("The", StringComparison.CurrentCultureIgnoreCase);
		  }
	  }

	  internal static readonly TeeSinkTokenFilter.SinkFilter dogFilter = new SinkFilterAnonymousInnerClassHelper2();

	  private class SinkFilterAnonymousInnerClassHelper2 : TeeSinkTokenFilter.SinkFilter
	  {
		  public SinkFilterAnonymousInnerClassHelper2()
		  {
		  }

		  public override bool accept(AttributeSource a)
		  {
			CharTermAttribute termAtt = a.getAttribute(typeof(CharTermAttribute));
			return termAtt.ToString().Equals("Dogs", StringComparison.CurrentCultureIgnoreCase);
		  }
	  }

	  // LUCENE-1448
	  // TODO: instead of testing it this way, we can test 
	  // with BaseTokenStreamTestCase now...
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEndOffsetPositionWithTeeSinkTokenFilter() throws Exception
	  public virtual void testEndOffsetPositionWithTeeSinkTokenFilter()
	  {
		Directory dir = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false);
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		Document doc = new Document();
		TokenStream tokenStream = analyzer.tokenStream("field", "abcd   ");
		TeeSinkTokenFilter tee = new TeeSinkTokenFilter(tokenStream);
		TokenStream sink = tee.newSinkTokenStream();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.StoreTermVectors = true;
		ft.StoreTermVectorOffsets = true;
		ft.StoreTermVectorPositions = true;
		Field f1 = new Field("field", tee, ft);
		Field f2 = new Field("field", sink, ft);
		doc.add(f1);
		doc.add(f2);
		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		Terms vector = r.getTermVectors(0).terms("field");
		assertEquals(1, vector.size());
		TermsEnum termsEnum = vector.iterator(null);
		termsEnum.next();
		assertEquals(2, termsEnum.totalTermFreq());
		DocsAndPositionsEnum positions = termsEnum.docsAndPositions(null, null);
		assertTrue(positions.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		assertEquals(2, positions.freq());
		positions.nextPosition();
		assertEquals(0, positions.startOffset());
		assertEquals(4, positions.endOffset());
		positions.nextPosition();
		assertEquals(8, positions.startOffset());
		assertEquals(12, positions.endOffset());
		assertEquals(DocIdSetIterator.NO_MORE_DOCS, positions.nextDoc());
		r.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testGeneral() throws java.io.IOException
	  public virtual void testGeneral()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TeeSinkTokenFilter source = new TeeSinkTokenFilter(new MockTokenizer(new java.io.StringReader(buffer1.toString()), MockTokenizer.WHITESPACE, false));
		TeeSinkTokenFilter source = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(buffer1.ToString()), MockTokenizer.WHITESPACE, false));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TokenStream sink1 = source.newSinkTokenStream();
		TokenStream sink1 = source.newSinkTokenStream();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TokenStream sink2 = source.newSinkTokenStream(theFilter);
		TokenStream sink2 = source.newSinkTokenStream(theFilter);

		source.addAttribute(typeof(CheckClearAttributesAttribute));
		sink1.addAttribute(typeof(CheckClearAttributesAttribute));
		sink2.addAttribute(typeof(CheckClearAttributesAttribute));

		assertTokenStreamContents(source, tokens1);
		assertTokenStreamContents(sink1, tokens1);
		assertTokenStreamContents(sink2, new string[]{"The", "the"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMultipleSources() throws Exception
	  public virtual void testMultipleSources()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TeeSinkTokenFilter tee1 = new TeeSinkTokenFilter(new MockTokenizer(new java.io.StringReader(buffer1.toString()), MockTokenizer.WHITESPACE, false));
		TeeSinkTokenFilter tee1 = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(buffer1.ToString()), MockTokenizer.WHITESPACE, false));
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TeeSinkTokenFilter.SinkTokenStream dogDetector = tee1.newSinkTokenStream(dogFilter);
		TeeSinkTokenFilter.SinkTokenStream dogDetector = tee1.newSinkTokenStream(dogFilter);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TeeSinkTokenFilter.SinkTokenStream theDetector = tee1.newSinkTokenStream(theFilter);
		TeeSinkTokenFilter.SinkTokenStream theDetector = tee1.newSinkTokenStream(theFilter);
		tee1.reset();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TokenStream source1 = new CachingTokenFilter(tee1);
		TokenStream source1 = new CachingTokenFilter(tee1);

		tee1.addAttribute(typeof(CheckClearAttributesAttribute));
		dogDetector.addAttribute(typeof(CheckClearAttributesAttribute));
		theDetector.addAttribute(typeof(CheckClearAttributesAttribute));

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TeeSinkTokenFilter tee2 = new TeeSinkTokenFilter(new MockTokenizer(new java.io.StringReader(buffer2.toString()), MockTokenizer.WHITESPACE, false));
		TeeSinkTokenFilter tee2 = new TeeSinkTokenFilter(new MockTokenizer(new StringReader(buffer2.ToString()), MockTokenizer.WHITESPACE, false));
		tee2.addSinkTokenStream(dogDetector);
		tee2.addSinkTokenStream(theDetector);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TokenStream source2 = tee2;
		TokenStream source2 = tee2;

		assertTokenStreamContents(source1, tokens1);
		assertTokenStreamContents(source2, tokens2);

		assertTokenStreamContents(theDetector, new string[]{"The", "the", "The", "the"});
		assertTokenStreamContents(dogDetector, new string[]{"Dogs", "Dogs"});

		source1.reset();
		TokenStream lowerCasing = new LowerCaseFilter(TEST_VERSION_CURRENT, source1);
		string[] lowerCaseTokens = new string[tokens1.Length];
		for (int i = 0; i < tokens1.Length; i++)
		{
		  lowerCaseTokens[i] = tokens1[i].ToLower(Locale.ROOT);
		}
		assertTokenStreamContents(lowerCasing, lowerCaseTokens);
	  }

	  /// <summary>
	  /// Not an explicit test, just useful to print out some info on performance
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void performance() throws Exception
	  public virtual void performance()
	  {
		int[] tokCount = new int[] {100, 500, 1000, 2000, 5000, 10000};
		int[] modCounts = new int[] {1, 2, 5, 10, 20, 50, 100, 200, 500};
		for (int k = 0; k < tokCount.Length; k++)
		{
		  StringBuilder buffer = new StringBuilder();
		  Console.WriteLine("-----Tokens: " + tokCount[k] + "-----");
		  for (int i = 0; i < tokCount[k]; i++)
		  {
			buffer.Append(English.intToEnglish(i).toUpperCase(Locale.ROOT)).Append(' ');
		  }
		  //make sure we produce the same tokens
		  TeeSinkTokenFilter teeStream = new TeeSinkTokenFilter(new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString()))));
		  TokenStream sink = teeStream.newSinkTokenStream(new ModuloSinkFilter(this, 100));
		  teeStream.consumeAllTokens();
		  TokenStream stream = new ModuloTokenFilter(this, new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString()))), 100);
		  CharTermAttribute tfTok = stream.addAttribute(typeof(CharTermAttribute));
		  CharTermAttribute sinkTok = sink.addAttribute(typeof(CharTermAttribute));
		  for (int i = 0; stream.incrementToken(); i++)
		  {
			assertTrue(sink.incrementToken());
			assertTrue(tfTok + " is not equal to " + sinkTok + " at token: " + i, tfTok.Equals(sinkTok) == true);
		  }

		  //simulate two fields, each being analyzed once, for 20 documents
		  for (int j = 0; j < modCounts.Length; j++)
		  {
			int tfPos = 0;
			long start = DateTimeHelperClass.CurrentUnixTimeMillis();
			for (int i = 0; i < 20; i++)
			{
			  stream = new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString())));
			  PositionIncrementAttribute posIncrAtt = stream.getAttribute(typeof(PositionIncrementAttribute));
			  while (stream.incrementToken())
			  {
				tfPos += posIncrAtt.PositionIncrement;
			  }
			  stream = new ModuloTokenFilter(this, new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString()))), modCounts[j]);
			  posIncrAtt = stream.getAttribute(typeof(PositionIncrementAttribute));
			  while (stream.incrementToken())
			  {
				tfPos += posIncrAtt.PositionIncrement;
			  }
			}
			long finish = DateTimeHelperClass.CurrentUnixTimeMillis();
			Console.WriteLine("ModCount: " + modCounts[j] + " Two fields took " + (finish - start) + " ms");
			int sinkPos = 0;
			//simulate one field with one sink
			start = DateTimeHelperClass.CurrentUnixTimeMillis();
			for (int i = 0; i < 20; i++)
			{
			  teeStream = new TeeSinkTokenFilter(new StandardFilter(TEST_VERSION_CURRENT, new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(buffer.ToString()))));
			  sink = teeStream.newSinkTokenStream(new ModuloSinkFilter(this, modCounts[j]));
			  PositionIncrementAttribute posIncrAtt = teeStream.getAttribute(typeof(PositionIncrementAttribute));
			  while (teeStream.incrementToken())
			  {
				sinkPos += posIncrAtt.PositionIncrement;
			  }
			  //System.out.println("Modulo--------");
			  posIncrAtt = sink.getAttribute(typeof(PositionIncrementAttribute));
			  while (sink.incrementToken())
			  {
				sinkPos += posIncrAtt.PositionIncrement;
			  }
			}
			finish = DateTimeHelperClass.CurrentUnixTimeMillis();
			Console.WriteLine("ModCount: " + modCounts[j] + " Tee fields took " + (finish - start) + " ms");
			assertTrue(sinkPos + " does not equal: " + tfPos, sinkPos == tfPos);

		  }
		  Console.WriteLine("- End Tokens: " + tokCount[k] + "-----");
		}

	  }


	  internal class ModuloTokenFilter : TokenFilter
	  {
		  private readonly TestTeeSinkTokenFilter outerInstance;


		internal int modCount;

		internal ModuloTokenFilter(TestTeeSinkTokenFilter outerInstance, TokenStream input, int mc) : base(input)
		{
			this.outerInstance = outerInstance;
		  modCount = mc;
		}

		internal int count = 0;

		//return every 100 tokens
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
		public override bool incrementToken()
		{
		  bool hasNext;
		  for (hasNext = input.incrementToken(); hasNext && count % modCount != 0; hasNext = input.incrementToken())
		  {
			count++;
		  }
		  count++;
		  return hasNext;
		}
	  }

	  internal class ModuloSinkFilter : TeeSinkTokenFilter.SinkFilter
	  {
		  private readonly TestTeeSinkTokenFilter outerInstance;

		internal int count = 0;
		internal int modCount;

		internal ModuloSinkFilter(TestTeeSinkTokenFilter outerInstance, int mc)
		{
			this.outerInstance = outerInstance;
		  modCount = mc;
		}

		public override bool accept(AttributeSource a)
		{
		  bool b = (a != null && count % modCount == 0);
		  count++;
		  return b;
		}

	  }
	}


}