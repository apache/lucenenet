namespace Lucene.Net.Index
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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermRangeQuery = Lucene.Net.Search.TermRangeQuery;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestReaderClosed : LuceneTestCase
	{
	  private IndexReader Reader;
	  private Directory Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.KEYWORD, false)).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));

		Document doc = new Document();
		Field field = newStringField("field", "", Field.Store.NO);
		doc.add(field);

		// we generate aweful prefixes: good for testing.
		// but for preflex codec, the test can be very slow, so use less iterations.
		int num = atLeast(10);
		for (int i = 0; i < num; i++)
		{
		  field.StringValue = TestUtil.randomUnicodeString(random(), 10);
		  writer.addDocument(doc);
		}
		Reader = writer.Reader;
		writer.close();
	  }

	  public virtual void Test()
	  {
		Assert.IsTrue(Reader.RefCount > 0);
		IndexSearcher searcher = newSearcher(Reader);
		TermRangeQuery query = TermRangeQuery.newStringRange("field", "a", "z", true, true);
		searcher.search(query, 5);
		Reader.close();
		try
		{
		  searcher.search(query, 5);
		}
		catch (AlreadyClosedException ace)
		{
		  // expected
		}
	  }

	  // LUCENE-3800
	  public virtual void TestReaderChaining()
	  {
		Assert.IsTrue(Reader.RefCount > 0);
		IndexReader wrappedReader = SlowCompositeReaderWrapper.wrap(Reader);
		wrappedReader = new ParallelAtomicReader((AtomicReader) wrappedReader);

		IndexSearcher searcher = newSearcher(wrappedReader);
		TermRangeQuery query = TermRangeQuery.newStringRange("field", "a", "z", true, true);
		searcher.search(query, 5);
		Reader.close(); // close original child reader
		try
		{
		  searcher.search(query, 5);
		}
		catch (AlreadyClosedException ace)
		{
		  Assert.AreEqual("this IndexReader cannot be used anymore as one of its child readers was closed", ace.Message);
		}
		finally
		{
		  // shutdown executor: in case of wrap-wrap-wrapping
		  searcher.IndexReader.close();
		}
	  }

	  public override void TearDown()
	  {
		Dir.close();
		base.tearDown();
	  }
	}

}