namespace org.apache.lucene.analysis.core
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

	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using StringField = org.apache.lucene.document.StringField;
	using TextField = org.apache.lucene.document.TextField;
	using DirectoryReader = org.apache.lucene.index.DirectoryReader;
	using DocsEnum = org.apache.lucene.index.DocsEnum;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using IndexWriter = org.apache.lucene.index.IndexWriter;
	using IndexWriterConfig = org.apache.lucene.index.IndexWriterConfig;
	using MultiFields = org.apache.lucene.index.MultiFields;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;
	using Directory = org.apache.lucene.store.Directory;
	using RAMDirectory = org.apache.lucene.store.RAMDirectory;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using TestUtil = org.apache.lucene.util.TestUtil;

	public class TestKeywordAnalyzer : BaseTokenStreamTestCase
	{

	  private Directory directory;
	  private IndexSearcher searcher;
	  private IndexReader reader;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setUp() throws Exception
	  public override void setUp()
	  {
		base.setUp();
		directory = newDirectory();
		IndexWriter writer = new IndexWriter(directory, new IndexWriterConfig(TEST_VERSION_CURRENT, new SimpleAnalyzer(TEST_VERSION_CURRENT)));

		Document doc = new Document();
		doc.add(new StringField("partnum", "Q36", Field.Store.YES));
		doc.add(new TextField("description", "Illidium Space Modulator", Field.Store.YES));
		writer.addDocument(doc);

		writer.close();

		reader = DirectoryReader.open(directory);
		searcher = newSearcher(reader);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void tearDown() throws Exception
	  public override void tearDown()
	  {
		reader.close();
		directory.close();
		base.tearDown();
	  }

	  /*
	  public void testPerFieldAnalyzer() throws Exception {
	    PerFieldAnalyzerWrapper analyzer = new PerFieldAnalyzerWrapper(new SimpleAnalyzer(TEST_VERSION_CURRENT));
	    analyzer.addAnalyzer("partnum", new KeywordAnalyzer());
	
	    QueryParser queryParser = new QueryParser(TEST_VERSION_CURRENT, "description", analyzer);
	    Query query = queryParser.parse("partnum:Q36 AND SPACE");
	
	    ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
	    assertEquals("Q36 kept as-is",
	              "+partnum:Q36 +space", query.toString("description"));
	    assertEquals("doc found!", 1, hits.length);
	  }
	  */

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMutipleDocument() throws Exception
	  public virtual void testMutipleDocument()
	  {
		RAMDirectory dir = new RAMDirectory();
		IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new KeywordAnalyzer()));
		Document doc = new Document();
		doc.add(new TextField("partnum", "Q36", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new TextField("partnum", "Q37", Field.Store.YES));
		writer.addDocument(doc);
		writer.close();

		IndexReader reader = DirectoryReader.open(dir);
		DocsEnum td = TestUtil.docs(random(), reader, "partnum", new BytesRef("Q36"), MultiFields.getLiveDocs(reader), null, 0);
		assertTrue(td.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		td = TestUtil.docs(random(), reader, "partnum", new BytesRef("Q37"), MultiFields.getLiveDocs(reader), null, 0);
		assertTrue(td.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
	  }

	  // LUCENE-1441
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOffsets() throws Exception
	  public virtual void testOffsets()
	  {
		TokenStream stream = (new KeywordAnalyzer()).tokenStream("field", new StringReader("abcd"));
		try
		{
		  OffsetAttribute offsetAtt = stream.addAttribute(typeof(OffsetAttribute));
		  stream.reset();
		  assertTrue(stream.incrementToken());
		  assertEquals(0, offsetAtt.startOffset());
		  assertEquals(4, offsetAtt.endOffset());
		  assertFalse(stream.incrementToken());
		  stream.end();
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(stream);
		}
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), new KeywordAnalyzer(), 1000 * RANDOM_MULTIPLIER);
	  }
	}

}