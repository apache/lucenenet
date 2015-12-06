namespace org.apache.lucene.analysis.query
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

	using org.apache.lucene.analysis;
	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using TextField = org.apache.lucene.document.TextField;
	using DirectoryReader = org.apache.lucene.index.DirectoryReader;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using IndexWriter = org.apache.lucene.index.IndexWriter;
	using IndexWriterConfig = org.apache.lucene.index.IndexWriterConfig;
	using RAMDirectory = org.apache.lucene.store.RAMDirectory;


	public class QueryAutoStopWordAnalyzerTest : BaseTokenStreamTestCase
	{
	  internal string[] variedFieldValues = new string[] {"the", "quick", "brown", "fox", "jumped", "over", "the", "lazy", "boring", "dog"};
	  internal string[] repetitiveFieldValues = new string[] {"boring", "boring", "vaguelyboring"};
	  internal RAMDirectory dir;
	  internal Analyzer appAnalyzer;
	  internal IndexReader reader;
	  internal QueryAutoStopWordAnalyzer protectedAnalyzer;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		dir = new RAMDirectory();
		appAnalyzer = new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false);
		IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, appAnalyzer));
		int numDocs = 200;
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  string variedFieldValue = variedFieldValues[i % variedFieldValues.Length];
		  string repetitiveFieldValue = repetitiveFieldValues[i % repetitiveFieldValues.Length];
		  doc.add(new TextField("variedField", variedFieldValue, Field.Store.YES));
		  doc.add(new TextField("repetitiveField", repetitiveFieldValue, Field.Store.YES));
		  writer.addDocument(doc);
		}
		writer.close();
		reader = DirectoryReader.open(dir);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void tearDown() throws Exception
	  public override void tearDown()
	  {
		reader.close();
		base.tearDown();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoStopwords() throws Exception
	  public virtual void testNoStopwords()
	  {
		// Note: an empty list of fields passed in
		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, System.Linq.Enumerable.Empty<string>(), 1);
		TokenStream protectedTokenStream = protectedAnalyzer.tokenStream("variedField", "quick");
		assertTokenStreamContents(protectedTokenStream, new string[]{"quick"});

		protectedTokenStream = protectedAnalyzer.tokenStream("repetitiveField", "boring");
		assertTokenStreamContents(protectedTokenStream, new string[]{"boring"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testDefaultStopwordsAllFields() throws Exception
	  public virtual void testDefaultStopwordsAllFields()
	  {
		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader);
		TokenStream protectedTokenStream = protectedAnalyzer.tokenStream("repetitiveField", "boring");
		assertTokenStreamContents(protectedTokenStream, new string[0]); // Default stop word filtering will remove boring
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopwordsAllFieldsMaxPercentDocs() throws Exception
	  public virtual void testStopwordsAllFieldsMaxPercentDocs()
	  {
		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, 1f / 2f);

		TokenStream protectedTokenStream = protectedAnalyzer.tokenStream("repetitiveField", "boring");
		// A filter on terms in > one half of docs remove boring
		assertTokenStreamContents(protectedTokenStream, new string[0]);

		protectedTokenStream = protectedAnalyzer.tokenStream("repetitiveField", "vaguelyboring");
		 // A filter on terms in > half of docs should not remove vaguelyBoring
		assertTokenStreamContents(protectedTokenStream, new string[]{"vaguelyboring"});

		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, 1f / 4f);
		protectedTokenStream = protectedAnalyzer.tokenStream("repetitiveField", "vaguelyboring");
		 // A filter on terms in > quarter of docs should remove vaguelyBoring
		assertTokenStreamContents(protectedTokenStream, new string[0]);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopwordsPerFieldMaxPercentDocs() throws Exception
	  public virtual void testStopwordsPerFieldMaxPercentDocs()
	  {
		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, Arrays.asList("variedField"), 1f / 2f);
		TokenStream protectedTokenStream = protectedAnalyzer.tokenStream("repetitiveField", "boring");
		// A filter on one Field should not affect queries on another
		assertTokenStreamContents(protectedTokenStream, new string[]{"boring"});

		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, Arrays.asList("variedField", "repetitiveField"), 1f / 2f);
		protectedTokenStream = protectedAnalyzer.tokenStream("repetitiveField", "boring");
		// A filter on the right Field should affect queries on it
		assertTokenStreamContents(protectedTokenStream, new string[0]);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testStopwordsPerFieldMaxDocFreq() throws Exception
	  public virtual void testStopwordsPerFieldMaxDocFreq()
	  {
		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, Arrays.asList("repetitiveField"), 10);
		int numStopWords = protectedAnalyzer.getStopWords("repetitiveField").length;
		assertTrue("Should have identified stop words", numStopWords > 0);

		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, Arrays.asList("repetitiveField", "variedField"), 10);
		int numNewStopWords = protectedAnalyzer.getStopWords("repetitiveField").length + protectedAnalyzer.getStopWords("variedField").length;
		assertTrue("Should have identified more stop words", numNewStopWords > numStopWords);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNoFieldNamePollution() throws Exception
	  public virtual void testNoFieldNamePollution()
	  {
		protectedAnalyzer = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, appAnalyzer, reader, Arrays.asList("repetitiveField"), 10);

		TokenStream protectedTokenStream = protectedAnalyzer.tokenStream("repetitiveField", "boring");
		// Check filter set up OK
		assertTokenStreamContents(protectedTokenStream, new string[0]);

		protectedTokenStream = protectedAnalyzer.tokenStream("variedField", "boring");
		// Filter should not prevent stopwords in one field being used in another
		assertTokenStreamContents(protectedTokenStream, new string[]{"boring"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testTokenStream() throws Exception
	  public virtual void testTokenStream()
	  {
		QueryAutoStopWordAnalyzer a = new QueryAutoStopWordAnalyzer(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false), reader, 10);
		TokenStream ts = a.tokenStream("repetitiveField", "this boring");
		assertTokenStreamContents(ts, new string[] {"this"});
	  }
	}

}