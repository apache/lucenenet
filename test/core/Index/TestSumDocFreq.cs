using System;

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
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Tests <seealso cref="Terms#getSumDocFreq()"/>
	/// @lucene.experimental
	/// </summary>
	public class TestSumDocFreq : LuceneTestCase
	{

	  public virtual void TestSumDocFreq()
	  {
		int numDocs = atLeast(500);

		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);

		Document doc = new Document();
		Field id = newStringField("id", "", Field.Store.NO);
		Field field1 = newTextField("foo", "", Field.Store.NO);
		Field field2 = newTextField("bar", "", Field.Store.NO);
		doc.add(id);
		doc.add(field1);
		doc.add(field2);
		for (int i = 0; i < numDocs; i++)
		{
		  id.StringValue = "" + i;
		  char ch1 = (char) TestUtil.Next(random(), 'a', 'z');
		  char ch2 = (char) TestUtil.Next(random(), 'a', 'z');
		  field1.StringValue = "" + ch1 + " " + ch2;
		  ch1 = (char) TestUtil.Next(random(), 'a', 'z');
		  ch2 = (char) TestUtil.Next(random(), 'a', 'z');
		  field2.StringValue = "" + ch1 + " " + ch2;
		  writer.addDocument(doc);
		}

		IndexReader ir = writer.Reader;

		AssertSumDocFreq(ir);
		ir.close();

		int numDeletions = atLeast(20);
		for (int i = 0; i < numDeletions; i++)
		{
		  writer.deleteDocuments(new Term("id", "" + random().Next(numDocs)));
		}
		writer.forceMerge(1);
		writer.close();

		ir = DirectoryReader.open(dir);
		AssertSumDocFreq(ir);
		ir.close();
		dir.close();
	  }

	  private void AssertSumDocFreq(IndexReader ir)
	  {
		// compute sumDocFreq across all fields
		Fields fields = MultiFields.getFields(ir);

		foreach (string f in fields)
		{
		  Terms terms = fields.terms(f);
		  long sumDocFreq = terms.SumDocFreq;
		  if (sumDocFreq == -1)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("skipping field: " + f + ", codec does not support sumDocFreq");
			}
			continue;
		  }

		  long computedSumDocFreq = 0;
		  TermsEnum termsEnum = terms.iterator(null);
		  while (termsEnum.next() != null)
		  {
			computedSumDocFreq += termsEnum.docFreq();
		  }
		  Assert.AreEqual(computedSumDocFreq, sumDocFreq);
		}
	  }
	}

}