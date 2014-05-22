using System.Collections.Generic;

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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestParallelTermEnum : LuceneTestCase
	{
	  private AtomicReader Ir1;
	  private AtomicReader Ir2;
	  private Directory Rd1;
	  private Directory Rd2;

	  public override void SetUp()
	  {
		base.setUp();
		Document doc;
		Rd1 = newDirectory();
		IndexWriter iw1 = new IndexWriter(Rd1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		doc = new Document();
		doc.add(newTextField("field1", "the quick brown fox jumps", Field.Store.YES));
		doc.add(newTextField("field2", "the quick brown fox jumps", Field.Store.YES));
		iw1.addDocument(doc);

		iw1.close();
		Rd2 = newDirectory();
		IndexWriter iw2 = new IndexWriter(Rd2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		doc = new Document();
		doc.add(newTextField("field1", "the fox jumps over the lazy dog", Field.Store.YES));
		doc.add(newTextField("field3", "the fox jumps over the lazy dog", Field.Store.YES));
		iw2.addDocument(doc);

		iw2.close();

		this.Ir1 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(Rd1));
		this.Ir2 = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(Rd2));
	  }

	  public override void TearDown()
	  {
		Ir1.close();
		Ir2.close();
		Rd1.close();
		Rd2.close();
		base.tearDown();
	  }

	  private void CheckTerms(Terms terms, Bits liveDocs, params string[] termsList)
	  {
		Assert.IsNotNull(terms);
		TermsEnum te = terms.iterator(null);

		foreach (string t in termsList)
		{
		  BytesRef b = te.next();
		  Assert.IsNotNull(b);
		  Assert.AreEqual(t, b.utf8ToString());
		  DocsEnum td = TestUtil.docs(random(), te, liveDocs, null, DocsEnum.FLAG_NONE);
		  Assert.IsTrue(td.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  Assert.AreEqual(0, td.docID());
		  Assert.AreEqual(td.nextDoc(), DocIdSetIterator.NO_MORE_DOCS);
		}
		assertNull(te.next());
	  }

	  public virtual void Test1()
	  {
		ParallelAtomicReader pr = new ParallelAtomicReader(Ir1, Ir2);

		Bits liveDocs = pr.LiveDocs;

		Fields fields = pr.fields();
		IEnumerator<string> fe = fields.GetEnumerator();

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		string f = fe.next();
		Assert.AreEqual("field1", f);
		checkTerms(fields.terms(f), liveDocs, "brown", "fox", "jumps", "quick", "the");

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		f = fe.next();
		Assert.AreEqual("field2", f);
		checkTerms(fields.terms(f), liveDocs, "brown", "fox", "jumps", "quick", "the");

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		f = fe.next();
		Assert.AreEqual("field3", f);
		checkTerms(fields.terms(f), liveDocs, "dog", "fox", "jumps", "lazy", "over", "the");

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(fe.hasNext());
	  }
	}

}