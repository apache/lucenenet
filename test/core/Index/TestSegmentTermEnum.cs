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

	using Field = Lucene.Net.Document.Field;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
	using Document = Lucene.Net.Document.Document;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using Directory = Lucene.Net.Store.Directory;


	public class TestSegmentTermEnum : LuceneTestCase
	{

	  internal Directory Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
	  }

	  public override void TearDown()
	  {
		Dir.close();
		base.tearDown();
	  }

	  public virtual void TestTermEnum()
	  {
		IndexWriter writer = null;

		writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		// ADD 100 documents with term : aaa
		// add 100 documents with terms: aaa bbb
		// Therefore, term 'aaa' has document frequency of 200 and term 'bbb' 100
		for (int i = 0; i < 100; i++)
		{
		  AddDoc(writer, "aaa");
		  AddDoc(writer, "aaa bbb");
		}

		writer.close();

		// verify document frequency of terms in an multi segment index
		VerifyDocFreq();

		// merge segments
		writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		writer.forceMerge(1);
		writer.close();

		// verify document frequency of terms in a single segment index
		VerifyDocFreq();
	  }

	  public virtual void TestPrevTermAtEnd()
	  {
		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat())));
		AddDoc(writer, "aaa bbb");
		writer.close();
		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(Dir));
		TermsEnum terms = reader.fields().terms("content").iterator(null);
		Assert.IsNotNull(terms.next());
		Assert.AreEqual("aaa", terms.term().utf8ToString());
		Assert.IsNotNull(terms.next());
		long ordB;
		try
		{
		  ordB = terms.ord();
		}
		catch (System.NotSupportedException uoe)
		{
		  // ok -- codec is not required to support ord
		  reader.close();
		  return;
		}
		Assert.AreEqual("bbb", terms.term().utf8ToString());
		assertNull(terms.next());

		terms.seekExact(ordB);
		Assert.AreEqual("bbb", terms.term().utf8ToString());
		reader.close();
	  }

	  private void VerifyDocFreq()
	  {
		  IndexReader reader = DirectoryReader.open(Dir);
		  TermsEnum termEnum = MultiFields.getTerms(reader, "content").iterator(null);

		// create enumeration of all terms
		// go to the first term (aaa)
		termEnum.next();
		// assert that term is 'aaa'
		Assert.AreEqual("aaa", termEnum.term().utf8ToString());
		Assert.AreEqual(200, termEnum.docFreq());
		// go to the second term (bbb)
		termEnum.next();
		// assert that term is 'bbb'
		Assert.AreEqual("bbb", termEnum.term().utf8ToString());
		Assert.AreEqual(100, termEnum.docFreq());


		// create enumeration of terms after term 'aaa',
		// including 'aaa'
		termEnum.seekCeil(new BytesRef("aaa"));
		// assert that term is 'aaa'
		Assert.AreEqual("aaa", termEnum.term().utf8ToString());
		Assert.AreEqual(200, termEnum.docFreq());
		// go to term 'bbb'
		termEnum.next();
		// assert that term is 'bbb'
		Assert.AreEqual("bbb", termEnum.term().utf8ToString());
		Assert.AreEqual(100, termEnum.docFreq());
		reader.close();
	  }

	  private void AddDoc(IndexWriter writer, string value)
	  {
		Document doc = new Document();
		doc.add(newTextField("content", value, Field.Store.NO));
		writer.addDocument(doc);
	  }
	}

}