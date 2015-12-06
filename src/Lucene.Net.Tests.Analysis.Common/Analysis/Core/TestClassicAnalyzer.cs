using System;

namespace org.apache.lucene.analysis.core
{

	using ClassicAnalyzer = org.apache.lucene.analysis.standard.ClassicAnalyzer;
	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using TextField = org.apache.lucene.document.TextField;
	using DocsAndPositionsEnum = org.apache.lucene.index.DocsAndPositionsEnum;
	using DocsEnum = org.apache.lucene.index.DocsEnum;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using IndexWriter = org.apache.lucene.index.IndexWriter;
	using IndexWriterConfig = org.apache.lucene.index.IndexWriterConfig;
	using MultiFields = org.apache.lucene.index.MultiFields;
	using Term = org.apache.lucene.index.Term;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using RAMDirectory = org.apache.lucene.store.RAMDirectory;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using Version = org.apache.lucene.util.Version;



	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
	/// <p/>
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// <p/>
	/// http://www.apache.org/licenses/LICENSE-2.0
	/// <p/>
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	public class TestClassicAnalyzer : BaseTokenStreamTestCase
	{

	  private Analyzer a = new ClassicAnalyzer(TEST_VERSION_CURRENT);

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxTermLength() throws Exception
	  public virtual void testMaxTermLength()
	  {
		ClassicAnalyzer sa = new ClassicAnalyzer(TEST_VERSION_CURRENT);
		sa.MaxTokenLength = 5;
		assertAnalyzesTo(sa, "ab cd toolong xy z", new string[]{"ab", "cd", "xy", "z"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxTermLength2() throws Exception
	  public virtual void testMaxTermLength2()
	  {
		ClassicAnalyzer sa = new ClassicAnalyzer(TEST_VERSION_CURRENT);
		assertAnalyzesTo(sa, "ab cd toolong xy z", new string[]{"ab", "cd", "toolong", "xy", "z"});
		sa.MaxTokenLength = 5;

		assertAnalyzesTo(sa, "ab cd toolong xy z", new string[]{"ab", "cd", "xy", "z"}, new int[]{1, 1, 2, 1});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxTermLength3() throws Exception
	  public virtual void testMaxTermLength3()
	  {
		char[] chars = new char[255];
		for (int i = 0;i < 255;i++)
		{
		  chars[i] = 'a';
		}
		string longTerm = new string(chars, 0, 255);

		assertAnalyzesTo(a, "ab cd " + longTerm + " xy z", new string[]{"ab", "cd", longTerm, "xy", "z"});
		assertAnalyzesTo(a, "ab cd " + longTerm + "a xy z", new string[]{"ab", "cd", "xy", "z"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAlphanumeric() throws Exception
	  public virtual void testAlphanumeric()
	  {
		// alphanumeric tokens
		assertAnalyzesTo(a, "B2B", new string[]{"b2b"});
		assertAnalyzesTo(a, "2B", new string[]{"2b"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUnderscores() throws Exception
	  public virtual void testUnderscores()
	  {
		// underscores are delimiters, but not in email addresses (below)
		assertAnalyzesTo(a, "word_having_underscore", new string[]{"word", "having", "underscore"});
		assertAnalyzesTo(a, "word_with_underscore_and_stopwords", new string[]{"word", "underscore", "stopwords"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDelimiters() throws Exception
	  public virtual void testDelimiters()
	  {
		// other delimiters: "-", "/", ","
		assertAnalyzesTo(a, "some-dashed-phrase", new string[]{"some", "dashed", "phrase"});
		assertAnalyzesTo(a, "dogs,chase,cats", new string[]{"dogs", "chase", "cats"});
		assertAnalyzesTo(a, "ac/dc", new string[]{"ac", "dc"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testApostrophes() throws Exception
	  public virtual void testApostrophes()
	  {
		// internal apostrophes: O'Reilly, you're, O'Reilly's
		// possessives are actually removed by StardardFilter, not the tokenizer
		assertAnalyzesTo(a, "O'Reilly", new string[]{"o'reilly"});
		assertAnalyzesTo(a, "you're", new string[]{"you're"});
		assertAnalyzesTo(a, "she's", new string[]{"she"});
		assertAnalyzesTo(a, "Jim's", new string[]{"jim"});
		assertAnalyzesTo(a, "don't", new string[]{"don't"});
		assertAnalyzesTo(a, "O'Reilly's", new string[]{"o'reilly"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTSADash() throws Exception
	  public virtual void testTSADash()
	  {
		// t and s had been stopwords in Lucene <= 2.0, which made it impossible
		// to correctly search for these terms:
		assertAnalyzesTo(a, "s-class", new string[]{"s", "class"});
		assertAnalyzesTo(a, "t-com", new string[]{"t", "com"});
		// 'a' is still a stopword:
		assertAnalyzesTo(a, "a-class", new string[]{"class"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCompanyNames() throws Exception
	  public virtual void testCompanyNames()
	  {
		// company names
		assertAnalyzesTo(a, "AT&T", new string[]{"at&t"});
		assertAnalyzesTo(a, "Excite@Home", new string[]{"excite@home"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testLucene1140() throws Exception
	  public virtual void testLucene1140()
	  {
		try
		{
		  ClassicAnalyzer analyzer = new ClassicAnalyzer(TEST_VERSION_CURRENT);
		  assertAnalyzesTo(analyzer, "www.nutch.org.", new string[]{"www.nutch.org"}, new string[] {"<HOST>"});
		}
		catch (System.NullReferenceException)
		{
		  fail("Should not throw an NPE and it did");
		}

	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDomainNames() throws Exception
	  public virtual void testDomainNames()
	  {
		// Current lucene should not show the bug
		ClassicAnalyzer a2 = new ClassicAnalyzer(TEST_VERSION_CURRENT);

		// domain names
		assertAnalyzesTo(a2, "www.nutch.org", new string[]{"www.nutch.org"});
		//Notice the trailing .  See https://issues.apache.org/jira/browse/LUCENE-1068.
		// the following should be recognized as HOST:
		assertAnalyzesTo(a2, "www.nutch.org.", new string[]{"www.nutch.org"}, new string[] {"<HOST>"});

		// 2.3 should show the bug. But, alas, it's obsolete, we don't support it.
		// a2 = new ClassicAnalyzer(org.apache.lucene.util.Version.LUCENE_23);
		// assertAnalyzesTo(a2, "www.nutch.org.", new String[]{ "wwwnutchorg" }, new String[] { "<ACRONYM>" });

		// 2.4 should not show the bug. But, alas, it's also obsolete,
		// so we check latest released (Robert's gonna break this on 4.0 soon :) )
		a2 = new ClassicAnalyzer(Version.LUCENE_31);
		assertAnalyzesTo(a2, "www.nutch.org.", new string[]{"www.nutch.org"}, new string[] {"<HOST>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEMailAddresses() throws Exception
	  public virtual void testEMailAddresses()
	  {
		// email addresses, possibly with underscores, periods, etc
		assertAnalyzesTo(a, "test@example.com", new string[]{"test@example.com"});
		assertAnalyzesTo(a, "first.lastname@example.com", new string[]{"first.lastname@example.com"});
		assertAnalyzesTo(a, "first_lastname@example.com", new string[]{"first_lastname@example.com"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNumeric() throws Exception
	  public virtual void testNumeric()
	  {
		// floating point, serial, model numbers, ip addresses, etc.
		// every other segment must have at least one digit
		assertAnalyzesTo(a, "21.35", new string[]{"21.35"});
		assertAnalyzesTo(a, "R2D2 C3PO", new string[]{"r2d2", "c3po"});
		assertAnalyzesTo(a, "216.239.63.104", new string[]{"216.239.63.104"});
		assertAnalyzesTo(a, "1-2-3", new string[]{"1-2-3"});
		assertAnalyzesTo(a, "a1-b2-c3", new string[]{"a1-b2-c3"});
		assertAnalyzesTo(a, "a1-b-c3", new string[]{"a1-b-c3"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTextWithNumbers() throws Exception
	  public virtual void testTextWithNumbers()
	  {
		// numbers
		assertAnalyzesTo(a, "David has 5000 bones", new string[]{"david", "has", "5000", "bones"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVariousText() throws Exception
	  public virtual void testVariousText()
	  {
		// various
		assertAnalyzesTo(a, "C embedded developers wanted", new string[]{"c", "embedded", "developers", "wanted"});
		assertAnalyzesTo(a, "foo bar FOO BAR", new string[]{"foo", "bar", "foo", "bar"});
		assertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[]{"foo", "bar", "foo", "bar"});
		assertAnalyzesTo(a, "\"QUOTED\" word", new string[]{"quoted", "word"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testAcronyms() throws Exception
	  public virtual void testAcronyms()
	  {
		// acronyms have their dots stripped
		assertAnalyzesTo(a, "U.S.A.", new string[]{"usa"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testCPlusPlusHash() throws Exception
	  public virtual void testCPlusPlusHash()
	  {
		// It would be nice to change the grammar in StandardTokenizer.jj to make "C#" and "C++" end up as tokens.
		assertAnalyzesTo(a, "C++", new string[]{"c"});
		assertAnalyzesTo(a, "C#", new string[]{"c"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKorean() throws Exception
	  public virtual void testKorean()
	  {
		// Korean words
		assertAnalyzesTo(a, "안녕하세요 한글입니다", new string[]{"안녕하세요", "한글입니다"});
	  }

	  // Compliance with the "old" JavaCC-based analyzer, see:
	  // https://issues.apache.org/jira/browse/LUCENE-966#action_12516752

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComplianceFileName() throws Exception
	  public virtual void testComplianceFileName()
	  {
		assertAnalyzesTo(a, "2004.jpg", new string[]{"2004.jpg"}, new string[]{"<HOST>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComplianceNumericIncorrect() throws Exception
	  public virtual void testComplianceNumericIncorrect()
	  {
		assertAnalyzesTo(a, "62.46", new string[]{"62.46"}, new string[]{"<HOST>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComplianceNumericLong() throws Exception
	  public virtual void testComplianceNumericLong()
	  {
		assertAnalyzesTo(a, "978-0-94045043-1", new string[]{"978-0-94045043-1"}, new string[]{"<NUM>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComplianceNumericFile() throws Exception
	  public virtual void testComplianceNumericFile()
	  {
		assertAnalyzesTo(a, "78academyawards/rules/rule02.html", new string[]{"78academyawards/rules/rule02.html"}, new string[]{"<NUM>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComplianceNumericWithUnderscores() throws Exception
	  public virtual void testComplianceNumericWithUnderscores()
	  {
		assertAnalyzesTo(a, "2006-03-11t082958z_01_ban130523_rtridst_0_ozabs", new string[]{"2006-03-11t082958z_01_ban130523_rtridst_0_ozabs"}, new string[]{"<NUM>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComplianceNumericWithDash() throws Exception
	  public virtual void testComplianceNumericWithDash()
	  {
		assertAnalyzesTo(a, "mid-20th", new string[]{"mid-20th"}, new string[]{"<NUM>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testComplianceManyTokens() throws Exception
	  public virtual void testComplianceManyTokens()
	  {
		assertAnalyzesTo(a, "/money.cnn.com/magazines/fortune/fortune_archive/2007/03/19/8402357/index.htm " + "safari-0-sheikh-zayed-grand-mosque.jpg", new string[]{"money.cnn.com", "magazines", "fortune", "fortune", "archive/2007/03/19/8402357", "index.htm", "safari-0-sheikh", "zayed", "grand", "mosque.jpg"}, new string[]{"<HOST>", "<ALPHANUM>", "<ALPHANUM>", "<ALPHANUM>", "<NUM>", "<HOST>", "<NUM>", "<ALPHANUM>", "<ALPHANUM>", "<HOST>"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testJava14BWCompatibility() throws Exception
	  public virtual void testJava14BWCompatibility()
	  {
		ClassicAnalyzer sa = new ClassicAnalyzer(Version.LUCENE_30);
		assertAnalyzesTo(sa, "test\u02C6test", new string[] {"test", "test"});
	  }

	  /// <summary>
	  /// Make sure we skip wicked long terms.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testWickedLongTerm() throws java.io.IOException
	  public virtual void testWickedLongTerm()
	  {
		RAMDirectory dir = new RAMDirectory();
		IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new ClassicAnalyzer(TEST_VERSION_CURRENT)));

		char[] chars = new char[IndexWriter.MAX_TERM_LENGTH];
		Arrays.fill(chars, 'x');
		Document doc = new Document();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String bigTerm = new String(chars);
		string bigTerm = new string(chars);

		// This produces a too-long term:
		string contents = "abc xyz x" + bigTerm + " another term";
		doc.add(new TextField("content", contents, Field.Store.NO));
		writer.addDocument(doc);

		// Make sure we can add another normal document
		doc = new Document();
		doc.add(new TextField("content", "abc bbb ccc", Field.Store.NO));
		writer.addDocument(doc);
		writer.close();

		IndexReader reader = IndexReader.open(dir);

		// Make sure all terms < max size were indexed
		assertEquals(2, reader.docFreq(new Term("content", "abc")));
		assertEquals(1, reader.docFreq(new Term("content", "bbb")));
		assertEquals(1, reader.docFreq(new Term("content", "term")));
		assertEquals(1, reader.docFreq(new Term("content", "another")));

		// Make sure position is still incremented when
		// massive term is skipped:
		DocsAndPositionsEnum tps = MultiFields.getTermPositionsEnum(reader, MultiFields.getLiveDocs(reader), "content", new BytesRef("another"));
		assertTrue(tps.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		assertEquals(1, tps.freq());
		assertEquals(3, tps.nextPosition());

		// Make sure the doc that has the massive term is in
		// the index:
		assertEquals("document with wicked long term should is not in the index!", 2, reader.numDocs());

		reader.close();

		// Make sure we can add a document with exactly the
		// maximum length term, and search on that term:
		doc = new Document();
		doc.add(new TextField("content", bigTerm, Field.Store.NO));
		ClassicAnalyzer sa = new ClassicAnalyzer(TEST_VERSION_CURRENT);
		sa.MaxTokenLength = 100000;
		writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, sa));
		writer.addDocument(doc);
		writer.close();
		reader = IndexReader.open(dir);
		assertEquals(1, reader.docFreq(new Term("content", bigTerm)));
		reader.close();

		dir.close();
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new ClassicAnalyzer(TEST_VERSION_CURRENT), 1000 * RANDOM_MULTIPLIER);
	  }

	  /// <summary>
	  /// blast some random large strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomHugeStrings() throws Exception
	  public virtual void testRandomHugeStrings()
	  {
		Random random = random();
		checkRandomData(random, new ClassicAnalyzer(TEST_VERSION_CURRENT), 100 * RANDOM_MULTIPLIER, 8192);
	  }
	}

}