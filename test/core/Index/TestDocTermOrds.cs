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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Codec = Lucene.Net.Codecs.Codec;
	using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IntField = Lucene.Net.Document.IntField;
	using StringField = Lucene.Net.Document.StringField;
	using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using StringHelper = Lucene.Net.Util.StringHelper;
	using TestUtil = Lucene.Net.Util.TestUtil;

	// TODO:
	//   - test w/ del docs
	//   - test prefix
	//   - test w/ cutoff
	//   - crank docs way up so we get some merging sometimes

	public class TestDocTermOrds : LuceneTestCase
	{

	  public virtual void TestSimple()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		Document doc = new Document();
		Field field = newTextField("field", "", Field.Store.NO);
		doc.add(field);
		field.StringValue = "a b c";
		w.addDocument(doc);

		field.StringValue = "d e f";
		w.addDocument(doc);

		field.StringValue = "a f";
		w.addDocument(doc);

		IndexReader r = w.Reader;
		w.close();

		AtomicReader ar = SlowCompositeReaderWrapper.wrap(r);
		DocTermOrds dto = new DocTermOrds(ar, ar.LiveDocs, "field");
		SortedSetDocValues iter = dto.iterator(ar);

		iter.Document = 0;
		Assert.AreEqual(0, iter.nextOrd());
		Assert.AreEqual(1, iter.nextOrd());
		Assert.AreEqual(2, iter.nextOrd());
		Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.nextOrd());

		iter.Document = 1;
		Assert.AreEqual(3, iter.nextOrd());
		Assert.AreEqual(4, iter.nextOrd());
		Assert.AreEqual(5, iter.nextOrd());
		Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.nextOrd());

		iter.Document = 2;
		Assert.AreEqual(0, iter.nextOrd());
		Assert.AreEqual(5, iter.nextOrd());
		Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.nextOrd());

		r.close();
		dir.close();
	  }

	  public virtual void TestRandom()
	  {
		Directory dir = newDirectory();

		int NUM_TERMS = atLeast(20);
		Set<BytesRef> terms = new HashSet<BytesRef>();
		while (terms.size() < NUM_TERMS)
		{
		  string s = TestUtil.randomRealisticUnicodeString(random());
		  //final String s = TestUtil.randomSimpleString(random);
		  if (s.Length > 0)
		  {
			terms.add(new BytesRef(s));
		  }
		}
		BytesRef[] termsArray = terms.toArray(new BytesRef[terms.size()]);
		Arrays.sort(termsArray);

		int NUM_DOCS = atLeast(100);

		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		// Sometimes swap in codec that impls ord():
		if (random().Next(10) == 7)
		{
		  // Make sure terms index has ords:
		  Codec codec = TestUtil.alwaysPostingsFormat(PostingsFormat.forName("Lucene41WithOrds"));
		  conf.Codec = codec;
		}

		RandomIndexWriter w = new RandomIndexWriter(random(), dir, conf);

		int[][] idToOrds = new int[NUM_DOCS][];
		Set<int?> ordsForDocSet = new HashSet<int?>();

		for (int id = 0;id < NUM_DOCS;id++)
		{
		  Document doc = new Document();

		  doc.add(new IntField("id", id, Field.Store.NO));

		  int termCount = TestUtil.Next(random(), 0, 20 * RANDOM_MULTIPLIER);
		  while (ordsForDocSet.size() < termCount)
		  {
			ordsForDocSet.add(random().Next(termsArray.Length));
		  }
		  int[] ordsForDoc = new int[termCount];
		  int upto = 0;
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: doc id=" + id);
		  }
		  foreach (int ord in ordsForDocSet)
		  {
			ordsForDoc[upto++] = ord;
			Field field = newStringField("field", termsArray[ord].utf8ToString(), Field.Store.NO);
			if (VERBOSE)
			{
			  Console.WriteLine("  f=" + termsArray[ord].utf8ToString());
			}
			doc.add(field);
		  }
		  ordsForDocSet.clear();
		  Arrays.sort(ordsForDoc);
		  idToOrds[id] = ordsForDoc;
		  w.addDocument(doc);
		}

		DirectoryReader r = w.Reader;
		w.close();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: reader=" + r);
		}

		foreach (AtomicReaderContext ctx in r.leaves())
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: sub=" + ctx.reader());
		  }
		  Verify(ctx.reader(), idToOrds, termsArray, null);
		}

		// Also test top-level reader: its enum does not support
		// ord, so this forces the OrdWrapper to run:
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: top reader");
		}
		AtomicReader slowR = SlowCompositeReaderWrapper.wrap(r);
		Verify(slowR, idToOrds, termsArray, null);

		FieldCache.DEFAULT.purgeByCacheKey(slowR.CoreCacheKey);

		r.close();
		dir.close();
	  }

	  public virtual void TestRandomWithPrefix()
	  {
		Directory dir = newDirectory();

		Set<string> prefixes = new HashSet<string>();
		int numPrefix = TestUtil.Next(random(), 2, 7);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: use " + numPrefix + " prefixes");
		}
		while (prefixes.size() < numPrefix)
		{
		  prefixes.add(TestUtil.randomRealisticUnicodeString(random()));
		  //prefixes.add(TestUtil.randomSimpleString(random));
		}
		string[] prefixesArray = prefixes.toArray(new string[prefixes.size()]);

		int NUM_TERMS = atLeast(20);
		Set<BytesRef> terms = new HashSet<BytesRef>();
		while (terms.size() < NUM_TERMS)
		{
		  string s = prefixesArray[random().Next(prefixesArray.Length)] + TestUtil.randomRealisticUnicodeString(random());
		  //final String s = prefixesArray[random.nextInt(prefixesArray.length)] + TestUtil.randomSimpleString(random);
		  if (s.Length > 0)
		  {
			terms.add(new BytesRef(s));
		  }
		}
		BytesRef[] termsArray = terms.toArray(new BytesRef[terms.size()]);
		Arrays.sort(termsArray);

		int NUM_DOCS = atLeast(100);

		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		// Sometimes swap in codec that impls ord():
		if (random().Next(10) == 7)
		{
		  Codec codec = TestUtil.alwaysPostingsFormat(PostingsFormat.forName("Lucene41WithOrds"));
		  conf.Codec = codec;
		}

		RandomIndexWriter w = new RandomIndexWriter(random(), dir, conf);

		int[][] idToOrds = new int[NUM_DOCS][];
		Set<int?> ordsForDocSet = new HashSet<int?>();

		for (int id = 0;id < NUM_DOCS;id++)
		{
		  Document doc = new Document();

		  doc.add(new IntField("id", id, Field.Store.NO));

		  int termCount = TestUtil.Next(random(), 0, 20 * RANDOM_MULTIPLIER);
		  while (ordsForDocSet.size() < termCount)
		  {
			ordsForDocSet.add(random().Next(termsArray.Length));
		  }
		  int[] ordsForDoc = new int[termCount];
		  int upto = 0;
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: doc id=" + id);
		  }
		  foreach (int ord in ordsForDocSet)
		  {
			ordsForDoc[upto++] = ord;
			Field field = newStringField("field", termsArray[ord].utf8ToString(), Field.Store.NO);
			if (VERBOSE)
			{
			  Console.WriteLine("  f=" + termsArray[ord].utf8ToString());
			}
			doc.add(field);
		  }
		  ordsForDocSet.clear();
		  Arrays.sort(ordsForDoc);
		  idToOrds[id] = ordsForDoc;
		  w.addDocument(doc);
		}

		DirectoryReader r = w.Reader;
		w.close();

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: reader=" + r);
		}

		AtomicReader slowR = SlowCompositeReaderWrapper.wrap(r);
		foreach (string prefix in prefixesArray)
		{

		  BytesRef prefixRef = prefix == null ? null : new BytesRef(prefix);

		  int[][] idToOrdsPrefix = new int[NUM_DOCS][];
		  for (int id = 0;id < NUM_DOCS;id++)
		  {
			int[] docOrds = idToOrds[id];
			IList<int?> newOrds = new List<int?>();
			foreach (int ord in idToOrds[id])
			{
			  if (StringHelper.StartsWith(termsArray[ord], prefixRef))
			  {
				newOrds.Add(ord);
			  }
			}
			int[] newOrdsArray = new int[newOrds.Count];
			int upto = 0;
			foreach (int ord in newOrds)
			{
			  newOrdsArray[upto++] = ord;
			}
			idToOrdsPrefix[id] = newOrdsArray;
		  }

		  foreach (AtomicReaderContext ctx in r.leaves())
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: sub=" + ctx.reader());
			}
			Verify(ctx.reader(), idToOrdsPrefix, termsArray, prefixRef);
		  }

		  // Also test top-level reader: its enum does not support
		  // ord, so this forces the OrdWrapper to run:
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: top reader");
		  }
		  Verify(slowR, idToOrdsPrefix, termsArray, prefixRef);
		}

		FieldCache.DEFAULT.purgeByCacheKey(slowR.CoreCacheKey);

		r.close();
		dir.close();
	  }

	  private void Verify(AtomicReader r, int[][] idToOrds, BytesRef[] termsArray, BytesRef prefixRef)
	  {

		DocTermOrds dto = new DocTermOrds(r, r.LiveDocs, "field", prefixRef, int.MaxValue, TestUtil.Next(random(), 2, 10));


		FieldCache.Ints docIDToID = FieldCache.DEFAULT.getInts(r, "id", false);
		/*
		  for(int docID=0;docID<subR.maxDoc();docID++) {
		  System.out.println("  docID=" + docID + " id=" + docIDToID[docID]);
		  }
		*/

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: verify prefix=" + (prefixRef == null ? "null" : prefixRef.utf8ToString()));
		  Console.WriteLine("TEST: all TERMS:");
		  TermsEnum allTE = MultiFields.getTerms(r, "field").iterator(null);
		  int ord = 0;
		  while (allTE.next() != null)
		  {
			Console.WriteLine("  ord=" + (ord++) + " term=" + allTE.term().utf8ToString());
		  }
		}

		//final TermsEnum te = subR.fields().terms("field").iterator();
		TermsEnum te = dto.getOrdTermsEnum(r);
		if (dto.numTerms() == 0)
		{
		  if (prefixRef == null)
		  {
			assertNull(MultiFields.getTerms(r, "field"));
		  }
		  else
		  {
			Terms terms = MultiFields.getTerms(r, "field");
			if (terms != null)
			{
			  TermsEnum termsEnum = terms.iterator(null);
			  TermsEnum.SeekStatus result = termsEnum.seekCeil(prefixRef);
			  if (result != TermsEnum.SeekStatus.END)
			  {
				Assert.IsFalse("term=" + termsEnum.term().utf8ToString() + " matches prefix=" + prefixRef.utf8ToString(), StringHelper.StartsWith(termsEnum.term(), prefixRef));
			  }
			  else
			  {
				// ok
			  }
			}
			else
			{
			  // ok
			}
		  }
		  return;
		}

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: TERMS:");
		  te.seekExact(0);
		  while (true)
		  {
			Console.WriteLine("  ord=" + te.ord() + " term=" + te.term().utf8ToString());
			if (te.next() == null)
			{
			  break;
			}
		  }
		}

		SortedSetDocValues iter = dto.iterator(r);
		for (int docID = 0;docID < r.maxDoc();docID++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: docID=" + docID + " of " + r.maxDoc() + " (id=" + docIDToID.get(docID) + ")");
		  }
		  iter.Document = docID;
		  int[] answers = idToOrds[docIDToID.get(docID)];
		  int upto = 0;
		  long ord;
		  while ((ord = iter.nextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
		  {
			te.seekExact(ord);
			BytesRef expected = termsArray[answers[upto++]];
			if (VERBOSE)
			{
			  Console.WriteLine("  exp=" + expected.utf8ToString() + " actual=" + te.term().utf8ToString());
			}
			Assert.AreEqual("expected=" + expected.utf8ToString() + " actual=" + te.term().utf8ToString() + " ord=" + ord, expected, te.term());
		  }
		  Assert.AreEqual(answers.Length, upto);
		}
	  }

	  public virtual void TestBackToTheFuture()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null));

		Document doc = new Document();
		doc.add(newStringField("foo", "bar", Field.Store.NO));
		iw.addDocument(doc);

		doc = new Document();
		doc.add(newStringField("foo", "baz", Field.Store.NO));
		iw.addDocument(doc);

		DirectoryReader r1 = DirectoryReader.open(iw, true);

		iw.deleteDocuments(new Term("foo", "baz"));
		DirectoryReader r2 = DirectoryReader.open(iw, true);

		FieldCache.DEFAULT.getDocTermOrds(getOnlySegmentReader(r2), "foo");

		SortedSetDocValues v = FieldCache.DEFAULT.getDocTermOrds(getOnlySegmentReader(r1), "foo");
		Assert.AreEqual(2, v.ValueCount);
		v.Document = 1;
		Assert.AreEqual(1, v.nextOrd());

		iw.close();
		r1.close();
		r2.close();
		dir.close();
	  }

	  public virtual void TestSortedTermsEnum()
	  {
		Directory directory = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriterConfig iwconfig = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new StringField("field", "hello", Field.Store.NO));
		iwriter.addDocument(doc);

		doc = new Document();
		doc.add(new StringField("field", "world", Field.Store.NO));
		iwriter.addDocument(doc);

		doc = new Document();
		doc.add(new StringField("field", "beer", Field.Store.NO));
		iwriter.addDocument(doc);
		iwriter.forceMerge(1);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.close();

		AtomicReader ar = getOnlySegmentReader(ireader);
		SortedSetDocValues dv = FieldCache.DEFAULT.getDocTermOrds(ar, "field");
		Assert.AreEqual(3, dv.ValueCount);

		TermsEnum termsEnum = dv.termsEnum();

		// next()
		Assert.AreEqual("beer", termsEnum.next().utf8ToString());
		Assert.AreEqual(0, termsEnum.ord());
		Assert.AreEqual("hello", termsEnum.next().utf8ToString());
		Assert.AreEqual(1, termsEnum.ord());
		Assert.AreEqual("world", termsEnum.next().utf8ToString());
		Assert.AreEqual(2, termsEnum.ord());

		// seekCeil()
		Assert.AreEqual(SeekStatus.NOT_FOUND, termsEnum.seekCeil(new BytesRef("ha!")));
		Assert.AreEqual("hello", termsEnum.term().utf8ToString());
		Assert.AreEqual(1, termsEnum.ord());
		Assert.AreEqual(SeekStatus.FOUND, termsEnum.seekCeil(new BytesRef("beer")));
		Assert.AreEqual("beer", termsEnum.term().utf8ToString());
		Assert.AreEqual(0, termsEnum.ord());
		Assert.AreEqual(SeekStatus.END, termsEnum.seekCeil(new BytesRef("zzz")));

		// seekExact()
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("beer")));
		Assert.AreEqual("beer", termsEnum.term().utf8ToString());
		Assert.AreEqual(0, termsEnum.ord());
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("hello")));
		Assert.AreEqual("hello", termsEnum.term().utf8ToString());
		Assert.AreEqual(1, termsEnum.ord());
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("world")));
		Assert.AreEqual("world", termsEnum.term().utf8ToString());
		Assert.AreEqual(2, termsEnum.ord());
		Assert.IsFalse(termsEnum.seekExact(new BytesRef("bogus")));

		// seek(ord)
		termsEnum.seekExact(0);
		Assert.AreEqual("beer", termsEnum.term().utf8ToString());
		Assert.AreEqual(0, termsEnum.ord());
		termsEnum.seekExact(1);
		Assert.AreEqual("hello", termsEnum.term().utf8ToString());
		Assert.AreEqual(1, termsEnum.ord());
		termsEnum.seekExact(2);
		Assert.AreEqual("world", termsEnum.term().utf8ToString());
		Assert.AreEqual(2, termsEnum.ord());
		ireader.close();
		directory.close();
	  }
	}

}