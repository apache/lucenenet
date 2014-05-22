using System;

namespace org.apache.lucene
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
	using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
	using Lucene46Codec = Lucene.Net.Codecs.lucene46.Lucene46Codec;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using Term = Lucene.Net.Index.Term;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;


	/* Intentionally outside of oal.index to verify fully
	   external codecs work fine */

	public class TestExternalCodecs : LuceneTestCase
	{

	  private sealed class CustomPerFieldCodec : Lucene46Codec
	  {

		internal readonly PostingsFormat RamFormat = PostingsFormat.forName("RAMOnly");
		internal readonly PostingsFormat DefaultFormat = PostingsFormat.forName("Lucene41");
		internal readonly PostingsFormat PulsingFormat = PostingsFormat.forName("Pulsing41");

		public override PostingsFormat GetPostingsFormatForField(string field)
		{
		  if (field.Equals("field2") || field.Equals("id"))
		  {
			return PulsingFormat;
		  }
		  else if (field.Equals("field1"))
		  {
			return DefaultFormat;
		  }
		  else
		  {
			return RamFormat;
		  }
		}
	  }

	  // tests storing "id" and "field2" fields as pulsing codec,
	  // whose term sort is backwards unicode code point, and
	  // storing "field1" as a custom entirely-in-RAM codec
	  public virtual void TestPerFieldCodec()
	  {

		int NUM_DOCS = atLeast(173);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
		}

		BaseDirectoryWrapper dir = newDirectory();
		dir.CheckIndexOnClose = false; // we use a custom codec provider
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(new CustomPerFieldCodec()).setMergePolicy(newLogMergePolicy(3)));
		Document doc = new Document();
		// uses default codec:
		doc.add(newTextField("field1", "this field uses the standard codec as the test", Field.Store.NO));
		// uses pulsing codec:
		Field field2 = newTextField("field2", "this field uses the pulsing codec as the test", Field.Store.NO);
		doc.add(field2);

		Field idField = newStringField("id", "", Field.Store.NO);

		doc.add(idField);
		for (int i = 0;i < NUM_DOCS;i++)
		{
		  idField.StringValue = "" + i;
		  w.addDocument(doc);
		  if ((i + 1) % 10 == 0)
		  {
			w.commit();
		  }
		}
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: now delete id=77");
		}
		w.deleteDocuments(new Term("id", "77"));

		IndexReader r = DirectoryReader.open(w, true);

		Assert.AreEqual(NUM_DOCS - 1, r.numDocs());
		IndexSearcher s = newSearcher(r);
		Assert.AreEqual(NUM_DOCS - 1, s.search(new TermQuery(new Term("field1", "standard")), 1).totalHits);
		Assert.AreEqual(NUM_DOCS - 1, s.search(new TermQuery(new Term("field2", "pulsing")), 1).totalHits);
		r.close();

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now delete 2nd doc");
		}
		w.deleteDocuments(new Term("id", "44"));

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now force merge");
		}
		w.forceMerge(1);
		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now open reader");
		}
		r = DirectoryReader.open(w, true);
		Assert.AreEqual(NUM_DOCS - 2, r.maxDoc());
		Assert.AreEqual(NUM_DOCS - 2, r.numDocs());
		s = newSearcher(r);
		Assert.AreEqual(NUM_DOCS - 2, s.search(new TermQuery(new Term("field1", "standard")), 1).totalHits);
		Assert.AreEqual(NUM_DOCS - 2, s.search(new TermQuery(new Term("field2", "pulsing")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("id", "76")), 1).totalHits);
		Assert.AreEqual(0, s.search(new TermQuery(new Term("id", "77")), 1).totalHits);
		Assert.AreEqual(0, s.search(new TermQuery(new Term("id", "44")), 1).totalHits);

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now close NRT reader");
		}
		r.close();

		w.close();

		dir.close();
	  }
	}

}