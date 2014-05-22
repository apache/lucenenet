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

	using Lucene.Net.Store;
	using Lucene.Net.Analysis;
	using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
	using Lucene.Net.Document;
	using Lucene.Net.Util;

	public class TestFlex : LuceneTestCase
	{

	  // Test non-flex API emulated on flex index
	  public virtual void TestNonFlex()
	  {
		Directory d = newDirectory();

		const int DOC_COUNT = 177;

		IndexWriter w = new IndexWriter(d, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMaxBufferedDocs(7).setMergePolicy(newLogMergePolicy()));

		for (int iter = 0;iter < 2;iter++)
		{
		  if (iter == 0)
		  {
			Document doc = new Document();
			doc.add(newTextField("field1", "this is field1", Field.Store.NO));
			doc.add(newTextField("field2", "this is field2", Field.Store.NO));
			doc.add(newTextField("field3", "aaa", Field.Store.NO));
			doc.add(newTextField("field4", "bbb", Field.Store.NO));
			for (int i = 0;i < DOC_COUNT;i++)
			{
			  w.addDocument(doc);
			}
		  }
		  else
		  {
			w.forceMerge(1);
		  }

		  IndexReader r = w.Reader;

		  TermsEnum terms = MultiFields.getTerms(r, "field3").iterator(null);
		  Assert.AreEqual(TermsEnum.SeekStatus.END, terms.seekCeil(new BytesRef("abc")));
		  r.close();
		}

		w.close();
		d.close();
	  }

	  public virtual void TestTermOrd()
	  {
		Directory d = newDirectory();
		IndexWriter w = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat())));
		Document doc = new Document();
		doc.add(newTextField("f", "a b c", Field.Store.NO));
		w.addDocument(doc);
		w.forceMerge(1);
		DirectoryReader r = w.Reader;
		TermsEnum terms = getOnlySegmentReader(r).fields().terms("f").iterator(null);
		Assert.IsTrue(terms.next() != null);
		try
		{
		  Assert.AreEqual(0, terms.ord());
		}
		catch (System.NotSupportedException uoe)
		{
		  // ok -- codec is not required to support this op
		}
		r.close();
		w.close();
		d.close();
	  }
	}


}