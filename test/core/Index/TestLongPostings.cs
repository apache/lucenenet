using System;
using System.Diagnostics;

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
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using TermToBytesRefAttribute = Lucene.Net.Analysis.Tokenattributes.TermToBytesRefAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using TextField = Lucene.Net.Document.TextField;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) public class TestLongPostings extends Lucene.Net.Util.LuceneTestCase
	public class TestLongPostings : LuceneTestCase
	{

	  // Produces a realistic unicode random string that
	  // survives MockAnalyzer unchanged:
	  private string GetRandomTerm(string other)
	  {
		Analyzer a = new MockAnalyzer(random());
		while (true)
		{
		  string s = TestUtil.randomRealisticUnicodeString(random());
		  if (other != null && s.Equals(other))
		  {
			continue;
		  }
		  IOException priorException = null;
		  TokenStream ts = a.tokenStream("foo", s);
		  try
		  {
			TermToBytesRefAttribute termAtt = ts.getAttribute(typeof(TermToBytesRefAttribute));
			BytesRef termBytes = termAtt.BytesRef;
			ts.reset();

			int count = 0;
			bool changed = false;

			while (ts.IncrementToken())
			{
			  termAtt.fillBytesRef();
			  if (count == 0 && !termBytes.utf8ToString().Equals(s))
			  {
				// The value was changed during analysis.  Keep iterating so the
				// tokenStream is exhausted.
				changed = true;
			  }
			  count++;
			}

			ts.end();
			// Did we iterate just once and the value was unchanged?
			if (!changed && count == 1)
			{
			  return s;
			}
		  }
		  catch (IOException e)
		  {
			priorException = e;
		  }
		  finally
		  {
			IOUtils.CloseWhileHandlingException(priorException, ts);
		  }
		}
	  }

	  public virtual void TestLongPostings()
	  {
		// Don't use TestUtil.getTempDir so that we own the
		// randomness (ie same seed will point to same dir):
		Directory dir = newFSDirectory(createTempDir("longpostings" + "." + random().nextLong()));

		int NUM_DOCS = atLeast(2000);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
		}

		string s1 = GetRandomTerm(null);
		string s2 = GetRandomTerm(s1);

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: s1=" + s1 + " s2=" + s2);
		  /*
		  for(int idx=0;idx<s1.length();idx++) {
		    System.out.println("  s1 ch=0x" + Integer.toHexString(s1.charAt(idx)));
		  }
		  for(int idx=0;idx<s2.length();idx++) {
		    System.out.println("  s2 ch=0x" + Integer.toHexString(s2.charAt(idx)));
		  }
		  */
		}

		FixedBitSet isS1 = new FixedBitSet(NUM_DOCS);
		for (int idx = 0;idx < NUM_DOCS;idx++)
		{
		  if (random().nextBoolean())
		  {
			isS1.set(idx);
		  }
		}

		IndexReader r;
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE).setMergePolicy(newLogMergePolicy());
		iwc.RAMBufferSizeMB = 16.0 + 16.0 * random().NextDouble();
		iwc.MaxBufferedDocs = -1;
		RandomIndexWriter riw = new RandomIndexWriter(random(), dir, iwc);

		for (int idx = 0;idx < NUM_DOCS;idx++)
		{
		  Document doc = new Document();
		  string s = isS1.get(idx) ? s1 : s2;
		  Field f = newTextField("field", s, Field.Store.NO);
		  int count = TestUtil.Next(random(), 1, 4);
		  for (int ct = 0;ct < count;ct++)
		  {
			doc.add(f);
		  }
		  riw.addDocument(doc);
		}

		r = riw.Reader;
		riw.close();

		/*
		if (VERBOSE) {
		  System.out.println("TEST: terms");
		  TermEnum termEnum = r.terms();
		  while(termEnum.next()) {
		    System.out.println("  term=" + termEnum.term() + " len=" + termEnum.term().text().length());
		    Assert.IsTrue(termEnum.docFreq() > 0);
		    System.out.println("    s1?=" + (termEnum.term().text().equals(s1)) + " s1len=" + s1.length());
		    System.out.println("    s2?=" + (termEnum.term().text().equals(s2)) + " s2len=" + s2.length());
		    final String s = termEnum.term().text();
		    for(int idx=0;idx<s.length();idx++) {
		      System.out.println("      ch=0x" + Integer.toHexString(s.charAt(idx)));
		    }
		  }
		}
		*/

		Assert.AreEqual(NUM_DOCS, r.numDocs());
		Assert.IsTrue(r.docFreq(new Term("field", s1)) > 0);
		Assert.IsTrue(r.docFreq(new Term("field", s2)) > 0);

		int num = atLeast(1000);
		for (int iter = 0;iter < num;iter++)
		{

		  string term;
		  bool doS1;
		  if (random().nextBoolean())
		  {
			term = s1;
			doS1 = true;
		  }
		  else
		  {
			term = s2;
			doS1 = false;
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter + " doS1=" + doS1);
		  }

		  DocsAndPositionsEnum postings = MultiFields.getTermPositionsEnum(r, null, "field", new BytesRef(term));

		  int docID = -1;
		  while (docID < DocIdSetIterator.NO_MORE_DOCS)
		  {
			int what = random().Next(3);
			if (what == 0)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: docID=" + docID + "; do next()");
			  }
			  // nextDoc
			  int expected = docID + 1;
			  while (true)
			  {
				if (expected == NUM_DOCS)
				{
				  expected = int.MaxValue;
				  break;
				}
				else if (isS1.get(expected) == doS1)
				{
				  break;
				}
				else
				{
				  expected++;
				}
			  }
			  docID = postings.nextDoc();
			  if (VERBOSE)
			  {
				Console.WriteLine("  got docID=" + docID);
			  }
			  Assert.AreEqual(expected, docID);
			  if (docID == DocIdSetIterator.NO_MORE_DOCS)
			  {
				break;
			  }

			  if (random().Next(6) == 3)
			  {
				int freq = postings.freq();
				Assert.IsTrue(freq >= 1 && freq <= 4);
				for (int pos = 0;pos < freq;pos++)
				{
				  Assert.AreEqual(pos, postings.nextPosition());
				  if (random().nextBoolean())
				  {
					postings.Payload;
					if (random().nextBoolean())
					{
					  postings.Payload; // get it again
					}
				  }
				}
			  }
			}
			else
			{
			  // advance
			  int targetDocID;
			  if (docID == -1)
			  {
				targetDocID = random().Next(NUM_DOCS + 1);
			  }
			  else
			  {
				targetDocID = docID + TestUtil.Next(random(), 1, NUM_DOCS - docID);
			  }
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: docID=" + docID + "; do advance(" + targetDocID + ")");
			  }
			  int expected = targetDocID;
			  while (true)
			  {
				if (expected == NUM_DOCS)
				{
				  expected = int.MaxValue;
				  break;
				}
				else if (isS1.get(expected) == doS1)
				{
				  break;
				}
				else
				{
				  expected++;
				}
			  }

			  docID = postings.advance(targetDocID);
			  if (VERBOSE)
			  {
				Console.WriteLine("  got docID=" + docID);
			  }
			  Assert.AreEqual(expected, docID);
			  if (docID == DocIdSetIterator.NO_MORE_DOCS)
			  {
				break;
			  }

			  if (random().Next(6) == 3)
			  {
				int freq = postings.freq();
				Assert.IsTrue(freq >= 1 && freq <= 4);
				for (int pos = 0;pos < freq;pos++)
				{
				  Assert.AreEqual(pos, postings.nextPosition());
				  if (random().nextBoolean())
				  {
					postings.Payload;
					if (random().nextBoolean())
					{
					  postings.Payload; // get it again
					}
				  }
				}
			  }
			}
		  }
		}
		r.close();
		dir.close();
	  }

	  // a weaker form of testLongPostings, that doesnt check positions
	  public virtual void TestLongPostingsNoPositions()
	  {
		DoTestLongPostingsNoPositions(IndexOptions.DOCS_ONLY);
		DoTestLongPostingsNoPositions(IndexOptions.DOCS_AND_FREQS);
	  }

	  public virtual void DoTestLongPostingsNoPositions(IndexOptions options)
	  {
		// Don't use TestUtil.getTempDir so that we own the
		// randomness (ie same seed will point to same dir):
		Directory dir = newFSDirectory(createTempDir("longpostings" + "." + random().nextLong()));

		int NUM_DOCS = atLeast(2000);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
		}

		string s1 = GetRandomTerm(null);
		string s2 = GetRandomTerm(s1);

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: s1=" + s1 + " s2=" + s2);
		  /*
		  for(int idx=0;idx<s1.length();idx++) {
		    System.out.println("  s1 ch=0x" + Integer.toHexString(s1.charAt(idx)));
		  }
		  for(int idx=0;idx<s2.length();idx++) {
		    System.out.println("  s2 ch=0x" + Integer.toHexString(s2.charAt(idx)));
		  }
		  */
		}

		FixedBitSet isS1 = new FixedBitSet(NUM_DOCS);
		for (int idx = 0;idx < NUM_DOCS;idx++)
		{
		  if (random().nextBoolean())
		  {
			isS1.set(idx);
		  }
		}

		IndexReader r;
		if (true)
		{
		  IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(IndexWriterConfig.OpenMode_e.CREATE).setMergePolicy(newLogMergePolicy());
		  iwc.RAMBufferSizeMB = 16.0 + 16.0 * random().NextDouble();
		  iwc.MaxBufferedDocs = -1;
		  RandomIndexWriter riw = new RandomIndexWriter(random(), dir, iwc);

		  FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		  ft.IndexOptions = options;
		  for (int idx = 0;idx < NUM_DOCS;idx++)
		  {
			Document doc = new Document();
			string s = isS1.get(idx) ? s1 : s2;
			Field f = newField("field", s, ft);
			int count = TestUtil.Next(random(), 1, 4);
			for (int ct = 0;ct < count;ct++)
			{
			  doc.add(f);
			}
			riw.addDocument(doc);
		  }

		  r = riw.Reader;
		  riw.close();
		}
		else
		{
		  r = DirectoryReader.open(dir);
		}

		/*
		if (VERBOSE) {
		  System.out.println("TEST: terms");
		  TermEnum termEnum = r.terms();
		  while(termEnum.next()) {
		    System.out.println("  term=" + termEnum.term() + " len=" + termEnum.term().text().length());
		    Assert.IsTrue(termEnum.docFreq() > 0);
		    System.out.println("    s1?=" + (termEnum.term().text().equals(s1)) + " s1len=" + s1.length());
		    System.out.println("    s2?=" + (termEnum.term().text().equals(s2)) + " s2len=" + s2.length());
		    final String s = termEnum.term().text();
		    for(int idx=0;idx<s.length();idx++) {
		      System.out.println("      ch=0x" + Integer.toHexString(s.charAt(idx)));
		    }
		  }
		}
		*/

		Assert.AreEqual(NUM_DOCS, r.numDocs());
		Assert.IsTrue(r.docFreq(new Term("field", s1)) > 0);
		Assert.IsTrue(r.docFreq(new Term("field", s2)) > 0);

		int num = atLeast(1000);
		for (int iter = 0;iter < num;iter++)
		{

		  string term;
		  bool doS1;
		  if (random().nextBoolean())
		  {
			term = s1;
			doS1 = true;
		  }
		  else
		  {
			term = s2;
			doS1 = false;
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter + " doS1=" + doS1 + " term=" + term);
		  }

		  DocsEnum docs;
		  DocsEnum postings;

		  if (options == IndexOptions.DOCS_ONLY)
		  {
			docs = TestUtil.docs(random(), r, "field", new BytesRef(term), null, null, DocsEnum.FLAG_NONE);
			postings = null;
		  }
		  else
		  {
			docs = postings = TestUtil.docs(random(), r, "field", new BytesRef(term), null, null, DocsEnum.FLAG_FREQS);
			Debug.Assert(postings != null);
		  }
		  Debug.Assert(docs != null);

		  int docID = -1;
		  while (docID < DocIdSetIterator.NO_MORE_DOCS)
		  {
			int what = random().Next(3);
			if (what == 0)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: docID=" + docID + "; do next()");
			  }
			  // nextDoc
			  int expected = docID + 1;
			  while (true)
			  {
				if (expected == NUM_DOCS)
				{
				  expected = int.MaxValue;
				  break;
				}
				else if (isS1.get(expected) == doS1)
				{
				  break;
				}
				else
				{
				  expected++;
				}
			  }
			  docID = docs.nextDoc();
			  if (VERBOSE)
			  {
				Console.WriteLine("  got docID=" + docID);
			  }
			  Assert.AreEqual(expected, docID);
			  if (docID == DocIdSetIterator.NO_MORE_DOCS)
			  {
				break;
			  }

			  if (random().Next(6) == 3 && postings != null)
			  {
				int freq = postings.freq();
				Assert.IsTrue(freq >= 1 && freq <= 4);
			  }
			}
			else
			{
			  // advance
			  int targetDocID;
			  if (docID == -1)
			  {
				targetDocID = random().Next(NUM_DOCS + 1);
			  }
			  else
			  {
				targetDocID = docID + TestUtil.Next(random(), 1, NUM_DOCS - docID);
			  }
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: docID=" + docID + "; do advance(" + targetDocID + ")");
			  }
			  int expected = targetDocID;
			  while (true)
			  {
				if (expected == NUM_DOCS)
				{
				  expected = int.MaxValue;
				  break;
				}
				else if (isS1.get(expected) == doS1)
				{
				  break;
				}
				else
				{
				  expected++;
				}
			  }

			  docID = docs.advance(targetDocID);
			  if (VERBOSE)
			  {
				Console.WriteLine("  got docID=" + docID);
			  }
			  Assert.AreEqual(expected, docID);
			  if (docID == DocIdSetIterator.NO_MORE_DOCS)
			  {
				break;
			  }

			  if (random().Next(6) == 3 && postings != null)
			  {
				int freq = postings.freq();
				Assert.IsTrue("got invalid freq=" + freq, freq >= 1 && freq <= 4);
			  }
			}
		  }
		}
		r.close();
		dir.close();
	  }
	}

}