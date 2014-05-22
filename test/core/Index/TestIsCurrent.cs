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

	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Lucene.Net.Util;
	using Lucene.Net.Store;

	using Test = org.junit.Test;

	public class TestIsCurrent : LuceneTestCase
	{

	  private RandomIndexWriter Writer;

	  private Directory Directory;

	  public override void SetUp()
	  {
		base.setUp();

		// initialize directory
		Directory = newDirectory();
		Writer = new RandomIndexWriter(random(), Directory);

		// write document
		Document doc = new Document();
		doc.add(newTextField("UUID", "1", Field.Store.YES));
		Writer.addDocument(doc);
		Writer.commit();
	  }

	  public override void TearDown()
	  {
		base.tearDown();
		Writer.close();
		Directory.close();
	  }

	  /// <summary>
	  /// Failing testcase showing the trouble
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDeleteByTermIsCurrent() throws java.io.IOException
	  public virtual void TestDeleteByTermIsCurrent()
	  {

		// get reader
		DirectoryReader reader = Writer.Reader;

		// assert index has a document and reader is up2date 
		Assert.AreEqual("One document should be in the index", 1, Writer.numDocs());
		Assert.IsTrue("One document added, reader should be current", reader.Current);

		// remove document
		Term idTerm = new Term("UUID", "1");
		Writer.deleteDocuments(idTerm);
		Writer.commit();

		// assert document has been deleted (index changed), reader is stale
		Assert.AreEqual("Document should be removed", 0, Writer.numDocs());
		Assert.IsFalse("Reader should be stale", reader.Current);

		reader.close();
	  }

	  /// <summary>
	  /// Testcase for example to show that writer.deleteAll() is working as expected
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDeleteAllIsCurrent() throws java.io.IOException
	  public virtual void TestDeleteAllIsCurrent()
	  {

		// get reader
		DirectoryReader reader = Writer.Reader;

		// assert index has a document and reader is up2date 
		Assert.AreEqual("One document should be in the index", 1, Writer.numDocs());
		Assert.IsTrue("Document added, reader should be stale ", reader.Current);

		// remove all documents
		Writer.deleteAll();
		Writer.commit();

		// assert document has been deleted (index changed), reader is stale
		Assert.AreEqual("Document should be removed", 0, Writer.numDocs());
		Assert.IsFalse("Reader should be stale", reader.Current);

		reader.close();
	  }
	}

}