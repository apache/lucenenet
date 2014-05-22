using System;
using System.Diagnostics;
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
	using IntField = Lucene.Net.Document.IntField;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Automaton = Lucene.Net.Util.Automaton.Automaton;
	using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
	using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) public class TestTermsEnum extends Lucene.Net.Util.LuceneTestCase
	public class TestTermsEnum : LuceneTestCase
	{

	  public virtual void Test()
	  {
		Random random = new Random(random().nextLong());
		LineFileDocs docs = new LineFileDocs(random, defaultCodecSupportsDocValues());
		Directory d = newDirectory();
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);
		RandomIndexWriter w = new RandomIndexWriter(random(), d, analyzer);
		int numDocs = atLeast(10);
		for (int docCount = 0;docCount < numDocs;docCount++)
		{
		  w.addDocument(docs.nextDoc());
		}
		IndexReader r = w.Reader;
		w.close();

		IList<BytesRef> terms = new List<BytesRef>();
		TermsEnum termsEnum = MultiFields.getTerms(r, "body").iterator(null);
		BytesRef term;
		while ((term = termsEnum.next()) != null)
		{
		  terms.Add(BytesRef.deepCopyOf(term));
		}
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: " + terms.Count + " terms");
		}

		int upto = -1;
		int iters = atLeast(200);
		for (int iter = 0;iter < iters;iter++)
		{
		  bool isEnd;
		  if (upto != -1 && random().nextBoolean())
		  {
			// next
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: iter next");
			}
			isEnd = termsEnum.next() == null;
			upto++;
			if (isEnd)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("  end");
			  }
			  Assert.AreEqual(upto, terms.Count);
			  upto = -1;
			}
			else
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("  got term=" + termsEnum.term().utf8ToString() + " expected=" + terms[upto].utf8ToString());
			  }
			  Assert.IsTrue(upto < terms.Count);
			  Assert.AreEqual(terms[upto], termsEnum.term());
			}
		  }
		  else
		  {

			BytesRef target;
			string exists;
			if (random().nextBoolean())
			{
			  // likely fake term
			  if (random().nextBoolean())
			  {
				target = new BytesRef(TestUtil.randomSimpleString(random()));
			  }
			  else
			  {
				target = new BytesRef(TestUtil.randomRealisticUnicodeString(random()));
			  }
			  exists = "likely not";
			}
			else
			{
			  // real term
			  target = terms[random().Next(terms.Count)];
			  exists = "yes";
			}

			upto = Collections.binarySearch(terms, target);

			if (random().nextBoolean())
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: iter seekCeil target=" + target.utf8ToString() + " exists=" + exists);
			  }
			  // seekCeil
			  TermsEnum.SeekStatus status = termsEnum.seekCeil(target);
			  if (VERBOSE)
			  {
				Console.WriteLine("  got " + status);
			  }

			  if (upto < 0)
			  {
				upto = -(upto + 1);
				if (upto >= terms.Count)
				{
				  Assert.AreEqual(TermsEnum.SeekStatus.END, status);
				  upto = -1;
				}
				else
				{
				  Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, status);
				  Assert.AreEqual(terms[upto], termsEnum.term());
				}
			  }
			  else
			  {
				Assert.AreEqual(TermsEnum.SeekStatus.FOUND, status);
				Assert.AreEqual(terms[upto], termsEnum.term());
			  }
			}
			else
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: iter seekExact target=" + target.utf8ToString() + " exists=" + exists);
			  }
			  // seekExact
			  bool result = termsEnum.seekExact(target);
			  if (VERBOSE)
			  {
				Console.WriteLine("  got " + result);
			  }
			  if (upto < 0)
			  {
				Assert.IsFalse(result);
				upto = -1;
			  }
			  else
			  {
				Assert.IsTrue(result);
				Assert.AreEqual(target, termsEnum.term());
			  }
			}
		  }
		}

		r.close();
		d.close();
		docs.close();
	  }

	  private void AddDoc(RandomIndexWriter w, ICollection<string> terms, IDictionary<BytesRef, int?> termToID, int id)
	  {
		Document doc = new Document();
		doc.add(new IntField("id", id, Field.Store.NO));
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: addDoc id:" + id + " terms=" + terms);
		}
		foreach (string s2 in terms)
		{
		  doc.add(newStringField("f", s2, Field.Store.NO));
		  termToID[new BytesRef(s2)] = id;
		}
		w.addDocument(doc);
		terms.Clear();
	  }

	  private bool Accepts(CompiledAutomaton c, BytesRef b)
	  {
		int state = c.runAutomaton.InitialState;
		for (int idx = 0;idx < b.length;idx++)
		{
		  Assert.IsTrue(state != -1);
		  state = c.runAutomaton.step(state, b.bytes[b.offset + idx] & 0xff);
		}
		return c.runAutomaton.isAccept(state);
	  }

	  // Tests Terms.intersect
	  public virtual void TestIntersectRandom()
	  {

		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		int numTerms = atLeast(300);
		//final int numTerms = 50;

		Set<string> terms = new HashSet<string>();
		ICollection<string> pendingTerms = new List<string>();
		IDictionary<BytesRef, int?> termToID = new Dictionary<BytesRef, int?>();
		int id = 0;
		while (terms.size() != numTerms)
		{
		  string s = RandomString;
		  if (!terms.contains(s))
		  {
			terms.add(s);
			pendingTerms.Add(s);
			if (random().Next(20) == 7)
			{
			  AddDoc(w, pendingTerms, termToID, id++);
			}
		  }
		}
		AddDoc(w, pendingTerms, termToID, id++);

		BytesRef[] termsArray = new BytesRef[terms.size()];
		Set<BytesRef> termsSet = new HashSet<BytesRef>();
		{
		  int upto = 0;
		  foreach (string s in terms)
		  {
			BytesRef b = new BytesRef(s);
			termsArray[upto++] = b;
			termsSet.add(b);
		  }
		  Array.Sort(termsArray);
		}

		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: indexed terms (unicode order):");
		  foreach (BytesRef t in termsArray)
		  {
			Console.WriteLine("  " + t.utf8ToString() + " -> id:" + termToID[t]);
		  }
		}

		IndexReader r = w.Reader;
		w.close();

		// NOTE: intentional insanity!!
		FieldCache.Ints docIDToID = FieldCache.DEFAULT.getInts(SlowCompositeReaderWrapper.wrap(r), "id", false);

		for (int iter = 0;iter < 10 * RANDOM_MULTIPLIER;iter++)
		{

		  // TODO: can we also test infinite As here...?

		  // From the random terms, pick some ratio and compile an
		  // automaton:
		  Set<string> acceptTerms = new HashSet<string>();
		  SortedSet<BytesRef> sortedAcceptTerms = new SortedSet<BytesRef>();
		  double keepPct = random().NextDouble();
		  Automaton a;
		  if (iter == 0)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: empty automaton");
			}
			a = BasicAutomata.makeEmpty();
		  }
		  else
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: keepPct=" + keepPct);
			}
			foreach (string s in terms)
			{
			  string s2;
			  if (random().NextDouble() <= keepPct)
			  {
				s2 = s;
			  }
			  else
			  {
				s2 = RandomString;
			  }
			  acceptTerms.add(s2);
			  sortedAcceptTerms.Add(new BytesRef(s2));
			}
			a = BasicAutomata.makeStringUnion(sortedAcceptTerms);
		  }

		  if (random().nextBoolean())
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: reduce the automaton");
			}
			a.reduce();
		  }

		  CompiledAutomaton c = new CompiledAutomaton(a, true, false);

		  BytesRef[] acceptTermsArray = new BytesRef[acceptTerms.size()];
		  Set<BytesRef> acceptTermsSet = new HashSet<BytesRef>();
		  int upto = 0;
		  foreach (string s in acceptTerms)
		  {
			BytesRef b = new BytesRef(s);
			acceptTermsArray[upto++] = b;
			acceptTermsSet.add(b);
			Assert.IsTrue(Accepts(c, b));
		  }
		  Array.Sort(acceptTermsArray);

		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: accept terms (unicode order):");
			foreach (BytesRef t in acceptTermsArray)
			{
			  Console.WriteLine("  " + t.utf8ToString() + (termsSet.contains(t) ? " (exists)" : ""));
			}
			Console.WriteLine(a.toDot());
		  }

		  for (int iter2 = 0;iter2 < 100;iter2++)
		  {
			BytesRef startTerm = acceptTermsArray.Length == 0 || random().nextBoolean() ? null : acceptTermsArray[random().Next(acceptTermsArray.Length)];

			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: iter2=" + iter2 + " startTerm=" + (startTerm == null ? "<null>" : startTerm.utf8ToString()));

			  if (startTerm != null)
			  {
				int state = c.runAutomaton.InitialState;
				for (int idx = 0;idx < startTerm.length;idx++)
				{
				  int label = startTerm.bytes[startTerm.offset + idx] & 0xff;
				  Console.WriteLine("  state=" + state + " label=" + label);
				  state = c.runAutomaton.step(state, label);
				  Assert.IsTrue(state != -1);
				}
				Console.WriteLine("  state=" + state);
			  }
			}

			TermsEnum te = MultiFields.getTerms(r, "f").intersect(c, startTerm);

			int loc;
			if (startTerm == null)
			{
			  loc = 0;
			}
			else
			{
			  loc = Array.BinarySearch(termsArray, BytesRef.deepCopyOf(startTerm));
			  if (loc < 0)
			  {
				loc = -(loc + 1);
			  }
			  else
			  {
				// startTerm exists in index
				loc++;
			  }
			}
			while (loc < termsArray.Length && !acceptTermsSet.contains(termsArray[loc]))
			{
			  loc++;
			}

			DocsEnum docsEnum = null;
			while (loc < termsArray.Length)
			{
			  BytesRef expected = termsArray[loc];
			  BytesRef actual = te.next();
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST:   next() expected=" + expected.utf8ToString() + " actual=" + (actual == null ? "null" : actual.utf8ToString()));
			  }
			  Assert.AreEqual(expected, actual);
			  Assert.AreEqual(1, te.docFreq());
			  docsEnum = TestUtil.docs(random(), te, null, docsEnum, DocsEnum.FLAG_NONE);
			  int docID = docsEnum.nextDoc();
			  Assert.IsTrue(docID != DocIdSetIterator.NO_MORE_DOCS);
			  Assert.AreEqual(docIDToID.get(docID), (int)termToID[expected]);
			  do
			  {
				loc++;
			  } while (loc < termsArray.Length && !acceptTermsSet.contains(termsArray[loc]));
			}
			assertNull(te.next());
		  }
		}

		r.close();
		dir.close();
	  }

	  private Directory d;
	  private IndexReader r;

	  private readonly string FIELD = "field";

	  private IndexReader MakeIndex(params string[] terms)
	  {
		d = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		/*
		iwc.setCodec(new StandardCodec(minTermsInBlock, maxTermsInBlock));
		*/

		RandomIndexWriter w = new RandomIndexWriter(random(), d, iwc);
		foreach (string term in terms)
		{
		  Document doc = new Document();
		  Field f = newStringField(FIELD, term, Field.Store.NO);
		  doc.add(f);
		  w.addDocument(doc);
		}
		if (r != null)
		{
		  Close();
		}
		r = w.Reader;
		w.close();
		return r;
	  }

	  private void Close()
	  {
		r.close();
		d.close();
	  }

	  private int DocFreq(IndexReader r, string term)
	  {
		return r.docFreq(new Term(FIELD, term));
	  }

	  public virtual void TestEasy()
	  {
		// No floor arcs:
		r = makeIndex("aa0", "aa1", "aa2", "aa3", "bb0", "bb1", "bb2", "bb3", "aa");

		// First term in block:
		Assert.AreEqual(1, DocFreq(r, "aa0"));

		// Scan forward to another term in same block
		Assert.AreEqual(1, DocFreq(r, "aa2"));

		Assert.AreEqual(1, DocFreq(r, "aa"));

		// Reset same block then scan forwards
		Assert.AreEqual(1, DocFreq(r, "aa1"));

		// Not found, in same block
		Assert.AreEqual(0, DocFreq(r, "aa5"));

		// Found, in same block
		Assert.AreEqual(1, DocFreq(r, "aa2"));

		// Not found in index:
		Assert.AreEqual(0, DocFreq(r, "b0"));

		// Found:
		Assert.AreEqual(1, DocFreq(r, "aa2"));

		// Found, rewind:
		Assert.AreEqual(1, DocFreq(r, "aa0"));


		// First term in block:
		Assert.AreEqual(1, DocFreq(r, "bb0"));

		// Scan forward to another term in same block
		Assert.AreEqual(1, DocFreq(r, "bb2"));

		// Reset same block then scan forwards
		Assert.AreEqual(1, DocFreq(r, "bb1"));

		// Not found, in same block
		Assert.AreEqual(0, DocFreq(r, "bb5"));

		// Found, in same block
		Assert.AreEqual(1, DocFreq(r, "bb2"));

		// Not found in index:
		Assert.AreEqual(0, DocFreq(r, "b0"));

		// Found:
		Assert.AreEqual(1, DocFreq(r, "bb2"));

		// Found, rewind:
		Assert.AreEqual(1, DocFreq(r, "bb0"));

		Close();
	  }

	  // tests:
	  //   - test same prefix has non-floor block and floor block (ie, has 2 long outputs on same term prefix)
	  //   - term that's entirely in the index

	  public virtual void TestFloorBlocks()
	  {
		string[] terms = new string[] {"aa0", "aa1", "aa2", "aa3", "aa4", "aa5", "aa6", "aa7", "aa8", "aa9", "aa", "xx"};
		r = MakeIndex(terms);
		//r = makeIndex("aa0", "aa1", "aa2", "aa3", "aa4", "aa5", "aa6", "aa7", "aa8", "aa9");

		// First term in first block:
		Assert.AreEqual(1, DocFreq(r, "aa0"));
		Assert.AreEqual(1, DocFreq(r, "aa4"));

		// No block
		Assert.AreEqual(0, DocFreq(r, "bb0"));

		// Second block
		Assert.AreEqual(1, DocFreq(r, "aa4"));

		// Backwards to prior floor block:
		Assert.AreEqual(1, DocFreq(r, "aa0"));

		// Forwards to last floor block:
		Assert.AreEqual(1, DocFreq(r, "aa9"));

		Assert.AreEqual(0, DocFreq(r, "a"));
		Assert.AreEqual(1, DocFreq(r, "aa"));
		Assert.AreEqual(0, DocFreq(r, "a"));
		Assert.AreEqual(1, DocFreq(r, "aa"));

		// Forwards to last floor block:
		Assert.AreEqual(1, DocFreq(r, "xx"));
		Assert.AreEqual(1, DocFreq(r, "aa1"));
		Assert.AreEqual(0, DocFreq(r, "yy"));

		Assert.AreEqual(1, DocFreq(r, "xx"));
		Assert.AreEqual(1, DocFreq(r, "aa9"));

		Assert.AreEqual(1, DocFreq(r, "xx"));
		Assert.AreEqual(1, DocFreq(r, "aa4"));

		TermsEnum te = MultiFields.getTerms(r, FIELD).iterator(null);
		while (te.next() != null)
		{
		  //System.out.println("TEST: next term=" + te.term().utf8ToString());
		}

		Assert.IsTrue(SeekExact(te, "aa1"));
		Assert.AreEqual("aa2", Next(te));
		Assert.IsTrue(SeekExact(te, "aa8"));
		Assert.AreEqual("aa9", Next(te));
		Assert.AreEqual("xx", Next(te));

		TestRandomSeeks(r, terms);
		Close();
	  }

	  public virtual void TestZeroTerms()
	  {
		d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		Document doc = new Document();
		doc.add(newTextField("field", "one two three", Field.Store.NO));
		doc = new Document();
		doc.add(newTextField("field2", "one two three", Field.Store.NO));
		w.addDocument(doc);
		w.commit();
		w.deleteDocuments(new Term("field", "one"));
		w.forceMerge(1);
		IndexReader r = w.Reader;
		w.close();
		Assert.AreEqual(1, r.numDocs());
		Assert.AreEqual(1, r.maxDoc());
		Terms terms = MultiFields.getTerms(r, "field");
		if (terms != null)
		{
		  assertNull(terms.iterator(null).next());
		}
		r.close();
		d.close();
	  }

	  private string RandomString
	  {
		  get
		  {
			//return TestUtil.randomSimpleString(random());
			return TestUtil.randomRealisticUnicodeString(random());
		  }
	  }

	  public virtual void TestRandomTerms()
	  {
		string[] terms = new string[TestUtil.Next(random(), 1, atLeast(1000))];
		Set<string> seen = new HashSet<string>();

		bool allowEmptyString = random().nextBoolean();

		if (random().Next(10) == 7 && terms.Length > 2)
		{
		  // Sometimes add a bunch of terms sharing a longish common prefix:
		  int numTermsSamePrefix = random().Next(terms.Length / 2);
		  if (numTermsSamePrefix > 0)
		  {
			string prefix;
			while (true)
			{
			  prefix = RandomString;
			  if (prefix.Length < 5)
			  {
				continue;
			  }
			  else
			  {
				break;
			  }
			}
			while (seen.size() < numTermsSamePrefix)
			{
			  string t = prefix + RandomString;
			  if (!seen.contains(t))
			  {
				terms[seen.size()] = t;
				seen.add(t);
			  }
			}
		  }
		}

		while (seen.size() < terms.Length)
		{
		  string t = RandomString;
		  if (!seen.contains(t) && (allowEmptyString || t.Length != 0))
		  {
			terms[seen.size()] = t;
			seen.add(t);
		  }
		}
		r = MakeIndex(terms);
		TestRandomSeeks(r, terms);
		Close();
	  }

	  // sugar
	  private bool SeekExact(TermsEnum te, string term)
	  {
		return te.seekExact(new BytesRef(term));
	  }

	  // sugar
	  private string Next(TermsEnum te)
	  {
		BytesRef br = te.next();
		if (br == null)
		{
		  return null;
		}
		else
		{
		  return br.utf8ToString();
		}
	  }

	  private BytesRef GetNonExistTerm(BytesRef[] terms)
	  {
		BytesRef t = null;
		while (true)
		{
		  string ts = RandomString;
		  t = new BytesRef(ts);
		  if (Array.BinarySearch(terms, t) < 0)
		  {
			return t;
		  }
		}
	  }

	  private class TermAndState
	  {
		public readonly BytesRef Term;
		public readonly TermState State;

		public TermAndState(BytesRef term, TermState state)
		{
		  this.Term = term;
		  this.State = state;
		}
	  }

	  private void TestRandomSeeks(IndexReader r, params string[] validTermStrings)
	  {
		BytesRef[] validTerms = new BytesRef[validTermStrings.Length];
		for (int termIDX = 0;termIDX < validTermStrings.Length;termIDX++)
		{
		  validTerms[termIDX] = new BytesRef(validTermStrings[termIDX]);
		}
		Array.Sort(validTerms);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: " + validTerms.Length + " terms:");
		  foreach (BytesRef t in validTerms)
		  {
			Console.WriteLine("  " + t.utf8ToString() + " " + t);
		  }
		}
		TermsEnum te = MultiFields.getTerms(r, FIELD).iterator(null);

		int END_LOC = -validTerms.Length - 1;

		IList<TermAndState> termStates = new List<TermAndState>();

		for (int iter = 0;iter < 100 * RANDOM_MULTIPLIER;iter++)
		{

		  BytesRef t;
		  int loc;
		  TermState termState;
		  if (random().Next(6) == 4)
		  {
			// pick term that doens't exist:
			t = GetNonExistTerm(validTerms);
			termState = null;
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: invalid term=" + t.utf8ToString());
			}
			loc = Array.BinarySearch(validTerms, t);
		  }
		  else if (termStates.Count != 0 && random().Next(4) == 1)
		  {
			TermAndState ts = termStates[random().Next(termStates.Count)];
			t = ts.Term;
			loc = Array.BinarySearch(validTerms, t);
			Assert.IsTrue(loc >= 0);
			termState = ts.State;
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: valid termState term=" + t.utf8ToString());
			}
		  }
		  else
		  {
			// pick valid term
			loc = random().Next(validTerms.Length);
			t = BytesRef.deepCopyOf(validTerms[loc]);
			termState = null;
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: valid term=" + t.utf8ToString());
			}
		  }

		  // seekCeil or seekExact:
		  bool doSeekExact = random().nextBoolean();
		  if (termState != null)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  seekExact termState");
			}
			te.seekExact(t, termState);
		  }
		  else if (doSeekExact)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  seekExact");
			}
			Assert.AreEqual(loc >= 0, te.seekExact(t));
		  }
		  else
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  seekCeil");
			}

			TermsEnum.SeekStatus result = te.seekCeil(t);
			if (VERBOSE)
			{
			  Console.WriteLine("  got " + result);
			}

			if (loc >= 0)
			{
			  Assert.AreEqual(TermsEnum.SeekStatus.FOUND, result);
			}
			else if (loc == END_LOC)
			{
			  Assert.AreEqual(TermsEnum.SeekStatus.END, result);
			}
			else
			{
			  Debug.Assert(loc >= -validTerms.Length);
			  Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, result);
			}
		  }

		  if (loc >= 0)
		  {
			Assert.AreEqual(t, te.term());
		  }
		  else if (doSeekExact)
		  {
			// TermsEnum is unpositioned if seekExact returns false
			continue;
		  }
		  else if (loc == END_LOC)
		  {
			continue;
		  }
		  else
		  {
			loc = -loc - 1;
			Assert.AreEqual(validTerms[loc], te.term());
		  }

		  // Do a bunch of next's after the seek
		  int numNext = random().Next(validTerms.Length);

		  for (int nextCount = 0;nextCount < numNext;nextCount++)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("\nTEST: next loc=" + loc + " of " + validTerms.Length);
			}
			BytesRef t2 = te.next();
			loc++;
			if (loc == validTerms.Length)
			{
			  assertNull(t2);
			  break;
			}
			else
			{
			  Assert.AreEqual(validTerms[loc], t2);
			  if (random().Next(40) == 17 && termStates.Count < 100)
			  {
				termStates.Add(new TermAndState(validTerms[loc], te.termState()));
			  }
			}
		  }
		}
	  }

	  public virtual void TestIntersectBasic()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MergePolicy = new LogDocMergePolicy();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, iwc);
		Document doc = new Document();
		doc.add(newTextField("field", "aaa", Field.Store.NO));
		w.addDocument(doc);

		doc = new Document();
		doc.add(newStringField("field", "bbb", Field.Store.NO));
		w.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("field", "ccc", Field.Store.NO));
		w.addDocument(doc);

		w.forceMerge(1);
		DirectoryReader r = w.Reader;
		w.close();
		AtomicReader sub = getOnlySegmentReader(r);
		Terms terms = sub.fields().terms("field");
		Automaton automaton = (new RegExp(".*", RegExp.NONE)).toAutomaton();
		CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
		TermsEnum te = terms.intersect(ca, null);
		Assert.AreEqual("aaa", te.next().utf8ToString());
		Assert.AreEqual(0, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		Assert.AreEqual("bbb", te.next().utf8ToString());
		Assert.AreEqual(1, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		Assert.AreEqual("ccc", te.next().utf8ToString());
		Assert.AreEqual(2, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		assertNull(te.next());

		te = terms.intersect(ca, new BytesRef("abc"));
		Assert.AreEqual("bbb", te.next().utf8ToString());
		Assert.AreEqual(1, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		Assert.AreEqual("ccc", te.next().utf8ToString());
		Assert.AreEqual(2, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		assertNull(te.next());

		te = terms.intersect(ca, new BytesRef("aaa"));
		Assert.AreEqual("bbb", te.next().utf8ToString());
		Assert.AreEqual(1, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		Assert.AreEqual("ccc", te.next().utf8ToString());
		Assert.AreEqual(2, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		assertNull(te.next());

		r.close();
		dir.close();
	  }
	  public virtual void TestIntersectStartTerm()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MergePolicy = new LogDocMergePolicy();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, iwc);
		Document doc = new Document();
		doc.add(newStringField("field", "abc", Field.Store.NO));
		w.addDocument(doc);

		doc = new Document();
		doc.add(newStringField("field", "abd", Field.Store.NO));
		w.addDocument(doc);

		doc = new Document();
		doc.add(newStringField("field", "acd", Field.Store.NO));
		w.addDocument(doc);

		doc = new Document();
		doc.add(newStringField("field", "bcd", Field.Store.NO));
		w.addDocument(doc);

		w.forceMerge(1);
		DirectoryReader r = w.Reader;
		w.close();
		AtomicReader sub = getOnlySegmentReader(r);
		Terms terms = sub.fields().terms("field");

		Automaton automaton = (new RegExp(".*d", RegExp.NONE)).toAutomaton();
		CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
		TermsEnum te;

		// should seek to startTerm
		te = terms.intersect(ca, new BytesRef("aad"));
		Assert.AreEqual("abd", te.next().utf8ToString());
		Assert.AreEqual(1, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		Assert.AreEqual("acd", te.next().utf8ToString());
		Assert.AreEqual(2, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		Assert.AreEqual("bcd", te.next().utf8ToString());
		Assert.AreEqual(3, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		assertNull(te.next());

		// should fail to find ceil label on second arc, rewind 
		te = terms.intersect(ca, new BytesRef("add"));
		Assert.AreEqual("bcd", te.next().utf8ToString());
		Assert.AreEqual(3, te.docs(null, null, DocsEnum.FLAG_NONE).nextDoc());
		assertNull(te.next());

		// should reach end
		te = terms.intersect(ca, new BytesRef("bcd"));
		assertNull(te.next());
		te = terms.intersect(ca, new BytesRef("ddd"));
		assertNull(te.next());

		r.close();
		dir.close();
	  }

	  public virtual void TestIntersectEmptyString()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MergePolicy = new LogDocMergePolicy();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, iwc);
		Document doc = new Document();
		doc.add(newStringField("field", "", Field.Store.NO));
		doc.add(newStringField("field", "abc", Field.Store.NO));
		w.addDocument(doc);

		doc = new Document();
		// add empty string to both documents, so that singletonDocID == -1.
		// For a FST-based term dict, we'll expect to see the first arc is 
		// flaged with HAS_FINAL_OUTPUT
		doc.add(newStringField("field", "abc", Field.Store.NO));
		doc.add(newStringField("field", "", Field.Store.NO));
		w.addDocument(doc);

		w.forceMerge(1);
		DirectoryReader r = w.Reader;
		w.close();
		AtomicReader sub = getOnlySegmentReader(r);
		Terms terms = sub.fields().terms("field");

		Automaton automaton = (new RegExp(".*", RegExp.NONE)).toAutomaton(); // accept ALL
		CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);

		TermsEnum te = terms.intersect(ca, null);
		DocsEnum de;

		Assert.AreEqual("", te.next().utf8ToString());
		de = te.docs(null, null, DocsEnum.FLAG_NONE);
		Assert.AreEqual(0, de.nextDoc());
		Assert.AreEqual(1, de.nextDoc());

		Assert.AreEqual("abc", te.next().utf8ToString());
		de = te.docs(null, null, DocsEnum.FLAG_NONE);
		Assert.AreEqual(0, de.nextDoc());
		Assert.AreEqual(1, de.nextDoc());

		assertNull(te.next());

		// pass empty string
		te = terms.intersect(ca, new BytesRef(""));

		Assert.AreEqual("abc", te.next().utf8ToString());
		de = te.docs(null, null, DocsEnum.FLAG_NONE);
		Assert.AreEqual(0, de.nextDoc());
		Assert.AreEqual(1, de.nextDoc());

		assertNull(te.next());

		r.close();
		dir.close();
	  }
	}

}