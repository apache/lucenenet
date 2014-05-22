using System;
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

	using Lucene.Net.Util;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Lucene.Net.Store;
	using Lucene.Net.Document;

	public class TestStressAdvance : LuceneTestCase
	{

	  public virtual void TestStressAdvance()
	  {
		for (int iter = 0;iter < 3;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter);
		  }
		  Directory dir = newDirectory();
		  RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		  Set<int?> aDocs = new HashSet<int?>();
		  Document doc = new Document();
		  Field f = newStringField("field", "", Field.Store.NO);
		  doc.add(f);
		  Field idField = newStringField("id", "", Field.Store.YES);
		  doc.add(idField);
		  int num = atLeast(4097);
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: numDocs=" + num);
		  }
		  for (int id = 0;id < num;id++)
		  {
			if (random().Next(4) == 3)
			{
			  f.StringValue = "a";
			  aDocs.add(id);
			}
			else
			{
			  f.StringValue = "b";
			}
			idField.StringValue = "" + id;
			w.addDocument(doc);
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: doc upto " + id);
			}
		  }

		  w.forceMerge(1);

		  IList<int?> aDocIDs = new List<int?>();
		  IList<int?> bDocIDs = new List<int?>();

		  DirectoryReader r = w.Reader;
		  int[] idToDocID = new int[r.maxDoc()];
		  for (int docID = 0;docID < idToDocID.Length;docID++)
		  {
			int id = Convert.ToInt32(r.document(docID).get("id"));
			if (aDocs.contains(id))
			{
			  aDocIDs.Add(docID);
			}
			else
			{
			  bDocIDs.Add(docID);
			}
		  }
		  TermsEnum te = getOnlySegmentReader(r).fields().terms("field").iterator(null);

		  DocsEnum de = null;
		  for (int iter2 = 0;iter2 < 10;iter2++)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: iter=" + iter + " iter2=" + iter2);
			}
			Assert.AreEqual(TermsEnum.SeekStatus.FOUND, te.seekCeil(new BytesRef("a")));
			de = TestUtil.docs(random(), te, null, de, DocsEnum.FLAG_NONE);
			TestOne(de, aDocIDs);

			Assert.AreEqual(TermsEnum.SeekStatus.FOUND, te.seekCeil(new BytesRef("b")));
			de = TestUtil.docs(random(), te, null, de, DocsEnum.FLAG_NONE);
			TestOne(de, bDocIDs);
		  }

		  w.close();
		  r.close();
		  dir.close();
		}
	  }

	  private void TestOne(DocsEnum docs, IList<int?> expected)
	  {
		if (VERBOSE)
		{
		  Console.WriteLine("test");
		}
		int upto = -1;
		while (upto < expected.Count)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("  cycle upto=" + upto + " of " + expected.Count);
		  }
		  int docID;
		  if (random().Next(4) == 1 || upto == expected.Count - 1)
		  {
			// test nextDoc()
			if (VERBOSE)
			{
			  Console.WriteLine("    do nextDoc");
			}
			upto++;
			docID = docs.nextDoc();
		  }
		  else
		  {
			// test advance()
			int inc = TestUtil.Next(random(), 1, expected.Count - 1 - upto);
			if (VERBOSE)
			{
			  Console.WriteLine("    do advance inc=" + inc);
			}
			upto += inc;
			docID = docs.advance(expected[upto]);
		  }
		  if (upto == expected.Count)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  expect docID=" + DocIdSetIterator.NO_MORE_DOCS + " actual=" + docID);
			}
			Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docID);
		  }
		  else
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  expect docID=" + expected[upto] + " actual=" + docID);
			}
			Assert.IsTrue(docID != DocIdSetIterator.NO_MORE_DOCS);
			Assert.AreEqual((int)expected[upto], docID);
		  }
		}
	  }
	}

}