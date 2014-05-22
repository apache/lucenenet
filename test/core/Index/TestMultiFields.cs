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

	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Lucene.Net.Store;
	using Lucene.Net.Util;
	using Lucene.Net.Document;
	using Lucene.Net.Analysis;

	public class TestMultiFields : LuceneTestCase
	{

	  public virtual void TestRandom()
	  {

		int num = atLeast(2);
		for (int iter = 0; iter < num; iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + iter);
		  }

		  Directory dir = newDirectory();

		  IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.COMPOUND_FILES));
		  // we can do this because we use NoMergePolicy (and dont merge to "nothing")
		  w.KeepFullyDeletedSegments = true;

		  IDictionary<BytesRef, IList<int?>> docs = new Dictionary<BytesRef, IList<int?>>();
		  Set<int?> deleted = new HashSet<int?>();
		  IList<BytesRef> terms = new List<BytesRef>();

		  int numDocs = TestUtil.Next(random(), 1, 100 * RANDOM_MULTIPLIER);
		  Document doc = new Document();
		  Field f = newStringField("field", "", Field.Store.NO);
		  doc.add(f);
		  Field id = newStringField("id", "", Field.Store.NO);
		  doc.add(id);

		  bool onlyUniqueTerms = random().nextBoolean();
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: onlyUniqueTerms=" + onlyUniqueTerms + " numDocs=" + numDocs);
		  }
		  Set<BytesRef> uniqueTerms = new HashSet<BytesRef>();
		  for (int i = 0;i < numDocs;i++)
		  {

			if (!onlyUniqueTerms && random().nextBoolean() && terms.Count > 0)
			{
			  // re-use existing term
			  BytesRef term = terms[random().Next(terms.Count)];
			  docs[term].Add(i);
			  f.StringValue = term.utf8ToString();
			}
			else
			{
			  string s = TestUtil.randomUnicodeString(random(), 10);
			  BytesRef term = new BytesRef(s);
			  if (!docs.ContainsKey(term))
			  {
				docs[term] = new List<int?>();
			  }
			  docs[term].Add(i);
			  terms.Add(term);
			  uniqueTerms.add(term);
			  f.StringValue = s;
			}
			id.StringValue = "" + i;
			w.addDocument(doc);
			if (random().Next(4) == 1)
			{
			  w.commit();
			}
			if (i > 0 && random().Next(20) == 1)
			{
			  int delID = random().Next(i);
			  deleted.add(delID);
			  w.deleteDocuments(new Term("id", "" + delID));
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: delete " + delID);
			  }
			}
		  }

		  if (VERBOSE)
		  {
			IList<BytesRef> termsList = new List<BytesRef>(uniqueTerms);
			termsList.Sort(BytesRef.UTF8SortedAsUTF16Comparator);
			Console.WriteLine("TEST: terms in UTF16 order:");
			foreach (BytesRef b in termsList)
			{
			  Console.WriteLine("  " + UnicodeUtil.toHexString(b.utf8ToString()) + " " + b);
			  foreach (int docID in docs[b])
			  {
				if (deleted.contains(docID))
				{
				  Console.WriteLine("    " + docID + " (deleted)");
				}
				else
				{
				  Console.WriteLine("    " + docID);
				}
			  }
			}
		  }

		  IndexReader reader = w.Reader;
		  w.close();
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: reader=" + reader);
		  }

		  Bits liveDocs = MultiFields.getLiveDocs(reader);
		  foreach (int delDoc in deleted)
		  {
			Assert.IsFalse(liveDocs.get(delDoc));
		  }

		  for (int i = 0;i < 100;i++)
		  {
			BytesRef term = terms[random().Next(terms.Count)];
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: seek term=" + UnicodeUtil.toHexString(term.utf8ToString()) + " " + term);
			}

			DocsEnum docsEnum = TestUtil.docs(random(), reader, "field", term, liveDocs, null, DocsEnum.FLAG_NONE);
			Assert.IsNotNull(docsEnum);

			foreach (int docID in docs[term])
			{
			  if (!deleted.contains(docID))
			  {
				Assert.AreEqual(docID, docsEnum.nextDoc());
			  }
			}
			Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.nextDoc());
		  }

		  reader.close();
		  dir.close();
		}
	  }

	  /*
	  private void verify(IndexReader r, String term, List<Integer> expected) throws Exception {
	    DocsEnum docs = TestUtil.docs(random, r,
	                                   "field",
	                                   new BytesRef(term),
	                                   MultiFields.getLiveDocs(r),
	                                   null,
	                                   false);
	    for(int docID : expected) {
	      Assert.AreEqual(docID, docs.nextDoc());
	    }
	    Assert.AreEqual(docs.NO_MORE_DOCS, docs.nextDoc());
	  }
	  */

	  public virtual void TestSeparateEnums()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document d = new Document();
		d.add(newStringField("f", "j", Field.Store.NO));
		w.addDocument(d);
		w.commit();
		w.addDocument(d);
		IndexReader r = w.Reader;
		w.close();
		DocsEnum d1 = TestUtil.docs(random(), r, "f", new BytesRef("j"), null, null, DocsEnum.FLAG_NONE);
		DocsEnum d2 = TestUtil.docs(random(), r, "f", new BytesRef("j"), null, null, DocsEnum.FLAG_NONE);
		Assert.AreEqual(0, d1.nextDoc());
		Assert.AreEqual(0, d2.nextDoc());
		r.close();
		dir.close();
	  }

	  public virtual void TestTermDocsEnum()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document d = new Document();
		d.add(newStringField("f", "j", Field.Store.NO));
		w.addDocument(d);
		w.commit();
		w.addDocument(d);
		IndexReader r = w.Reader;
		w.close();
		DocsEnum de = MultiFields.getTermDocsEnum(r, null, "f", new BytesRef("j"));
		Assert.AreEqual(0, de.nextDoc());
		Assert.AreEqual(1, de.nextDoc());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, de.nextDoc());
		r.close();
		dir.close();
	  }
	}

}