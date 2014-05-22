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
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;

	/// <summary>
	/// Tests the Terms.docCount statistic
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") public class TestDocCount extends Lucene.Net.Util.LuceneTestCase
	public class TestDocCount : LuceneTestCase
	{
	  public virtual void TestSimple()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		int numDocs = atLeast(100);
		for (int i = 0; i < numDocs; i++)
		{
		  iw.addDocument(Doc());
		}
		IndexReader ir = iw.Reader;
		VerifyCount(ir);
		ir.close();
		iw.forceMerge(1);
		ir = iw.Reader;
		VerifyCount(ir);
		ir.close();
		iw.close();
		dir.close();
	  }

	  private Document Doc()
	  {
		Document doc = new Document();
		int numFields = TestUtil.Next(random(), 1, 10);
		for (int i = 0; i < numFields; i++)
		{
		  doc.add(newStringField("" + TestUtil.Next(random(), 'a', 'z'), "" + TestUtil.Next(random(), 'a', 'z'), Field.Store.NO));
		}
		return doc;
	  }

	  private void VerifyCount(IndexReader ir)
	  {
		Fields fields = MultiFields.getFields(ir);
		if (fields == null)
		{
		  return;
		}
		foreach (string field in fields)
		{
		  Terms terms = fields.terms(field);
		  if (terms == null)
		  {
			continue;
		  }
		  int docCount = terms.DocCount;
		  FixedBitSet visited = new FixedBitSet(ir.maxDoc());
		  TermsEnum te = terms.iterator(null);
		  while (te.next() != null)
		  {
			DocsEnum de = TestUtil.docs(random(), te, null, null, DocsEnum.FLAG_NONE);
			while (de.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
			  visited.set(de.docID());
			}
		  }
		  Assert.AreEqual(visited.cardinality(), docCount);
		}
	  }
	}

}