namespace org.apache.lucene.analysis.miscellaneous
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

	using LuceneTestCase = org.apache.lucene.util.LuceneTestCase;
	using TermToBytesRefAttribute = org.apache.lucene.analysis.tokenattributes.TermToBytesRefAttribute;
	using Document = org.apache.lucene.document.Document;
	using Field = org.apache.lucene.document.Field;
	using StringField = org.apache.lucene.document.StringField;
	using TextField = org.apache.lucene.document.TextField;
	using IndexWriter = org.apache.lucene.index.IndexWriter;
	using IndexWriterConfig = org.apache.lucene.index.IndexWriterConfig;
	using Directory = org.apache.lucene.store.Directory;

	public class TestEmptyTokenStream : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testConsume() throws java.io.IOException
	  public virtual void testConsume()
	  {
		TokenStream ts = new EmptyTokenStream();
		ts.reset();
		assertFalse(ts.incrementToken());
		ts.end();
		ts.close();
		// try again with reuse:
		ts.reset();
		assertFalse(ts.incrementToken());
		ts.end();
		ts.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testConsume2() throws java.io.IOException
	  public virtual void testConsume2()
	  {
		BaseTokenStreamTestCase.assertTokenStreamContents(new EmptyTokenStream(), new string[0]);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIndexWriter_LUCENE4656() throws java.io.IOException
	  public virtual void testIndexWriter_LUCENE4656()
	  {
		Directory directory = newDirectory();
		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, null));

		TokenStream ts = new EmptyTokenStream();
		assertFalse(ts.hasAttribute(typeof(TermToBytesRefAttribute)));

		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.YES));
		doc.add(new TextField("description", ts));

		// this should not fail because we have no TermToBytesRefAttribute
		writer.addDocument(doc);

		assertEquals(1, writer.numDocs());

		writer.close();
		directory.close();
	  }

	}

}