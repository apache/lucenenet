using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Util.Fst
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
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using FSDirectory = Lucene.Net.Store.FSDirectory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using Slow = Lucene.Net.Util.LuceneTestCase.Slow;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using Automaton = Lucene.Net.Util.Automaton.Automaton;
	using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;
	using InputOutput = Lucene.Net.Util.Fst.BytesRefFSTEnum.InputOutput;
	using Arc = Lucene.Net.Util.Fst.FST.Arc;
	using BytesReader = Lucene.Net.Util.Fst.FST.BytesReader;
	using Pair = Lucene.Net.Util.Fst.PairOutputs.Pair;
	using Result = Lucene.Net.Util.Fst.Util.Result;
	using PackedInts = Lucene.Net.Util.Packed.PackedInts;


//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Util.Fst.FSTTester.getRandomString;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Util.Fst.FSTTester.simpleRandomString;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Util.Fst.FSTTester.toIntsRef;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "SimpleText", "Memory", "Direct" }) @Slow public class TestFSTs extends Lucene.Net.Util.LuceneTestCase
	public class TestFSTs : LuceneTestCase
	{

	  private MockDirectoryWrapper Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newMockDirectory();
		Dir.PreventDoubleWrite = false;
	  }

	  public override void TearDown()
	  {
		// can be null if we force simpletext (funky, some kind of bug in test runner maybe)
		if (Dir != null)
		{
			Dir.close();
		}
		base.tearDown();
	  }

	  public virtual void TestBasicFSA()
	  {
		string[] strings = new string[] {"station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation", "stat"};
		string[] strings2 = new string[] {"station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation"};
		IntsRef[] terms = new IntsRef[strings.Length];
		IntsRef[] terms2 = new IntsRef[strings2.Length];
		for (int inputMode = 0;inputMode < 2;inputMode++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: inputMode=" + InputModeToString(inputMode));
		  }

		  for (int idx = 0;idx < strings.Length;idx++)
		  {
			terms[idx] = toIntsRef(strings[idx], inputMode);
		  }
		  for (int idx = 0;idx < strings2.Length;idx++)
		  {
			terms2[idx] = toIntsRef(strings2[idx], inputMode);
		  }
		  Arrays.sort(terms2);

		  DoTest(inputMode, terms);

		  // Test pre-determined FST sizes to make sure we haven't lost minimality (at least on this trivial set of terms):

		  // FSA
		  {
			Outputs<object> outputs = NoOutputs.Singleton;
			object NO_OUTPUT = outputs.NoOutput;
			IList<FSTTester.InputOutput<object>> pairs = new List<FSTTester.InputOutput<object>>(terms2.Length);
			foreach (IntsRef term in terms2)
			{
			  pairs.Add(new FSTTester.InputOutput<>(term, NO_OUTPUT));
			}
			FST<object> fst = (new FSTTester<object>(random(), Dir, inputMode, pairs, outputs, false)).doTest(0, 0, false);
			Assert.IsNotNull(fst);
			Assert.AreEqual(22, fst.NodeCount);
			Assert.AreEqual(27, fst.ArcCount);
		  }

		  // FST ord pos int
		  {
			PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
			IList<FSTTester.InputOutput<long?>> pairs = new List<FSTTester.InputOutput<long?>>(terms2.Length);
			for (int idx = 0;idx < terms2.Length;idx++)
			{
			  pairs.Add(new FSTTester.InputOutput<>(terms2[idx], (long) idx));
			}
			FST<long?> fst = (new FSTTester<long?>(random(), Dir, inputMode, pairs, outputs, true)).doTest(0, 0, false);
			Assert.IsNotNull(fst);
			Assert.AreEqual(22, fst.NodeCount);
			Assert.AreEqual(27, fst.ArcCount);
		  }

		  // FST byte sequence ord
		  {
			ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
			BytesRef NO_OUTPUT = outputs.NoOutput;
			IList<FSTTester.InputOutput<BytesRef>> pairs = new List<FSTTester.InputOutput<BytesRef>>(terms2.Length);
			for (int idx = 0;idx < terms2.Length;idx++)
			{
			  BytesRef output = random().Next(30) == 17 ? NO_OUTPUT : new BytesRef(Convert.ToString(idx));
			  pairs.Add(new FSTTester.InputOutput<>(terms2[idx], output));
			}
			FST<BytesRef> fst = (new FSTTester<BytesRef>(random(), Dir, inputMode, pairs, outputs, false)).doTest(0, 0, false);
			Assert.IsNotNull(fst);
			Assert.AreEqual(24, fst.NodeCount);
			Assert.AreEqual(30, fst.ArcCount);
		  }
		}
	  }

	  // given set of terms, test the different outputs for them
	  private void DoTest(int inputMode, IntsRef[] terms)
	  {
		Arrays.sort(terms);

		// NoOutputs (simple FSA)
		{
		  Outputs<object> outputs = NoOutputs.Singleton;
		  object NO_OUTPUT = outputs.NoOutput;
		  IList<FSTTester.InputOutput<object>> pairs = new List<FSTTester.InputOutput<object>>(terms.Length);
		  foreach (IntsRef term in terms)
		  {
			pairs.Add(new FSTTester.InputOutput<>(term, NO_OUTPUT));
		  }
		  (new FSTTester<>(random(), Dir, inputMode, pairs, outputs, false)).doTest(true);
		}

		// PositiveIntOutput (ord)
		{
		  PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		  IList<FSTTester.InputOutput<long?>> pairs = new List<FSTTester.InputOutput<long?>>(terms.Length);
		  for (int idx = 0;idx < terms.Length;idx++)
		  {
			pairs.Add(new FSTTester.InputOutput<>(terms[idx], (long) idx));
		  }
		  (new FSTTester<>(random(), Dir, inputMode, pairs, outputs, true)).doTest(true);
		}

		// PositiveIntOutput (random monotonically increasing positive number)
		{
		  PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		  IList<FSTTester.InputOutput<long?>> pairs = new List<FSTTester.InputOutput<long?>>(terms.Length);
		  long lastOutput = 0;
		  for (int idx = 0;idx < terms.Length;idx++)
		  {
			long value = lastOutput + TestUtil.Next(random(), 1, 1000);
			lastOutput = value;
			pairs.Add(new FSTTester.InputOutput<>(terms[idx], value));
		  }
		  (new FSTTester<>(random(), Dir, inputMode, pairs, outputs, true)).doTest(true);
		}

		// PositiveIntOutput (random positive number)
		{
		  PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		  IList<FSTTester.InputOutput<long?>> pairs = new List<FSTTester.InputOutput<long?>>(terms.Length);
		  for (int idx = 0;idx < terms.Length;idx++)
		  {
			pairs.Add(new FSTTester.InputOutput<>(terms[idx], TestUtil.nextLong(random(), 0, long.MaxValue)));
		  }
		  (new FSTTester<>(random(), Dir, inputMode, pairs, outputs, false)).doTest(true);
		}

		// Pair<ord, (random monotonically increasing positive number>
		{
		  PositiveIntOutputs o1 = PositiveIntOutputs.Singleton;
		  PositiveIntOutputs o2 = PositiveIntOutputs.Singleton;
		  PairOutputs<long?, long?> outputs = new PairOutputs<long?, long?>(o1, o2);
		  IList<FSTTester.InputOutput<PairOutputs.Pair<long?, long?>>> pairs = new List<FSTTester.InputOutput<PairOutputs.Pair<long?, long?>>>(terms.Length);
		  long lastOutput = 0;
		  for (int idx = 0;idx < terms.Length;idx++)
		  {
			long value = lastOutput + TestUtil.Next(random(), 1, 1000);
			lastOutput = value;
			pairs.Add(new FSTTester.InputOutput<>(terms[idx], outputs.newPair((long) idx, value)));
		  }
		  (new FSTTester<>(random(), Dir, inputMode, pairs, outputs, false)).doTest(true);
		}

		// Sequence-of-bytes
		{
		  ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
		  BytesRef NO_OUTPUT = outputs.NoOutput;
		  IList<FSTTester.InputOutput<BytesRef>> pairs = new List<FSTTester.InputOutput<BytesRef>>(terms.Length);
		  for (int idx = 0;idx < terms.Length;idx++)
		  {
			BytesRef output = random().Next(30) == 17 ? NO_OUTPUT : new BytesRef(Convert.ToString(idx));
			pairs.Add(new FSTTester.InputOutput<>(terms[idx], output));
		  }
		  (new FSTTester<>(random(), Dir, inputMode, pairs, outputs, false)).doTest(true);
		}

		// Sequence-of-ints
		{
		  IntSequenceOutputs outputs = IntSequenceOutputs.Singleton;
		  IList<FSTTester.InputOutput<IntsRef>> pairs = new List<FSTTester.InputOutput<IntsRef>>(terms.Length);
		  for (int idx = 0;idx < terms.Length;idx++)
		  {
			string s = Convert.ToString(idx);
			IntsRef output = new IntsRef(s.Length);
			output.length = s.Length;
			for (int idx2 = 0;idx2 < output.length;idx2++)
			{
			  output.ints[idx2] = s[idx2];
			}
			pairs.Add(new FSTTester.InputOutput<>(terms[idx], output));
		  }
		  (new FSTTester<>(random(), Dir, inputMode, pairs, outputs, false)).doTest(true);
		}

	  }


	  public virtual void TestRandomWords()
	  {
		TestRandomWords(1000, atLeast(2));
		//testRandomWords(100, 1);
	  }

	  internal virtual string InputModeToString(int mode)
	  {
		if (mode == 0)
		{
		  return "utf8";
		}
		else
		{
		  return "utf32";
		}
	  }

	  private void TestRandomWords(int maxNumWords, int numIter)
	  {
		Random random = new Random(random().nextLong());
		for (int iter = 0;iter < numIter;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter " + iter);
		  }
		  for (int inputMode = 0;inputMode < 2;inputMode++)
		  {
			int numWords = random.Next(maxNumWords + 1);
			Set<IntsRef> termsSet = new HashSet<IntsRef>();
			IntsRef[] terms = new IntsRef[numWords];
			while (termsSet.size() < numWords)
			{
			  string term = getRandomString(random);
			  termsSet.add(toIntsRef(term, inputMode));
			}
			DoTest(inputMode, termsSet.toArray(new IntsRef[termsSet.size()]));
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void testBigSet() throws java.io.IOException
	  public virtual void TestBigSet()
	  {
		TestRandomWords(TestUtil.Next(random(), 50000, 60000), 1);
	  }

	  // Build FST for all unique terms in the test line docs
	  // file, up until a time limit
	  public virtual void TestRealTerms()
	  {

		LineFileDocs docs = new LineFileDocs(random(), defaultCodecSupportsDocValues());
		int RUN_TIME_MSEC = atLeast(500);
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);

		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(-1).setRAMBufferSizeMB(64);
		File tempDir = createTempDir("fstlines");
		Directory dir = newFSDirectory(tempDir);
		IndexWriter writer = new IndexWriter(dir, conf);
		long stopTime = System.currentTimeMillis() + RUN_TIME_MSEC;
		Document doc;
		int docCount = 0;
		while ((doc = docs.nextDoc()) != null && System.currentTimeMillis() < stopTime)
		{
		  writer.addDocument(doc);
		  docCount++;
		}
		IndexReader r = DirectoryReader.open(writer, true);
		writer.close();
		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;

		bool doRewrite = random().nextBoolean();

		Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, doRewrite, PackedInts.DEFAULT, true, 15);

		bool storeOrd = random().nextBoolean();
		if (VERBOSE)
		{
		  if (storeOrd)
		  {
			Console.WriteLine("FST stores ord");
		  }
		  else
		  {
			Console.WriteLine("FST stores docFreq");
		  }
		}
		Terms terms = MultiFields.getTerms(r, "body");
		if (terms != null)
		{
		  IntsRef scratchIntsRef = new IntsRef();
		  TermsEnum termsEnum = terms.iterator(null);
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: got termsEnum=" + termsEnum);
		  }
		  BytesRef term;
		  int ord = 0;

		  Automaton automaton = (new RegExp(".*", RegExp.NONE)).toAutomaton();
		  TermsEnum termsEnum2 = terms.intersect(new CompiledAutomaton(automaton, false, false), null);

		  while ((term = termsEnum.next()) != null)
		  {
			BytesRef term2 = termsEnum2.next();
			Assert.IsNotNull(term2);
			Assert.AreEqual(term, term2);
			Assert.AreEqual(termsEnum.docFreq(), termsEnum2.docFreq());
			Assert.AreEqual(termsEnum.totalTermFreq(), termsEnum2.totalTermFreq());

			if (ord == 0)
			{
			  try
			  {
				termsEnum.ord();
			  }
			  catch (System.NotSupportedException uoe)
			  {
				if (VERBOSE)
				{
				  Console.WriteLine("TEST: codec doesn't support ord; FST stores docFreq");
				}
				storeOrd = false;
			  }
			}
			int output;
			if (storeOrd)
			{
			  output = ord;
			}
			else
			{
			  output = termsEnum.docFreq();
			}
			builder.add(Util.toIntsRef(term, scratchIntsRef), (long) output);
			ord++;
			if (VERBOSE && ord % 100000 == 0 && LuceneTestCase.TEST_NIGHTLY)
			{
			  Console.WriteLine(ord + " terms...");
			}
		  }
		  FST<long?> fst = builder.finish();
		  if (VERBOSE)
		  {
			Console.WriteLine("FST: " + docCount + " docs; " + ord + " terms; " + fst.NodeCount + " nodes; " + fst.ArcCount + " arcs;" + " " + fst.sizeInBytes() + " bytes");
		  }

		  if (ord > 0)
		  {
			Random random = new Random(random().nextLong());
			// Now confirm BytesRefFSTEnum and TermsEnum act the
			// same:
			BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);
			int num = atLeast(1000);
			for (int iter = 0;iter < num;iter++)
			{
			  BytesRef randomTerm = new BytesRef(getRandomString(random));

			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: seek non-exist " + randomTerm.utf8ToString() + " " + randomTerm);
			  }

			  TermsEnum.SeekStatus seekResult = termsEnum.seekCeil(randomTerm);
			  InputOutput<long?> fstSeekResult = fstEnum.seekCeil(randomTerm);

			  if (seekResult == TermsEnum.SeekStatus.END)
			  {
				assertNull("got " + (fstSeekResult == null ? "null" : fstSeekResult.input.utf8ToString()) + " but expected null", fstSeekResult);
			  }
			  else
			  {
				AssertSame(termsEnum, fstEnum, storeOrd);
				for (int nextIter = 0;nextIter < 10;nextIter++)
				{
				  if (VERBOSE)
				  {
					Console.WriteLine("TEST: next");
					if (storeOrd)
					{
					  Console.WriteLine("  ord=" + termsEnum.ord());
					}
				  }
				  if (termsEnum.next() != null)
				  {
					if (VERBOSE)
					{
					  Console.WriteLine("  term=" + termsEnum.term().utf8ToString());
					}
					Assert.IsNotNull(fstEnum.next());
					AssertSame(termsEnum, fstEnum, storeOrd);
				  }
				  else
				  {
					if (VERBOSE)
					{
					  Console.WriteLine("  end!");
					}
					BytesRefFSTEnum.InputOutput<long?> nextResult = fstEnum.next();
					if (nextResult != null)
					{
					  Console.WriteLine("expected null but got: input=" + nextResult.input.utf8ToString() + " output=" + outputs.outputToString(nextResult.output));
					  Assert.Fail();
					}
					break;
				  }
				}
			  }
			}

		  }
		}

		r.close();
		dir.close();
	  }

	  private void assertSame<T1>(TermsEnum termsEnum, BytesRefFSTEnum<T1> fstEnum, bool storeOrd)
	  {
		if (termsEnum.term() == null)
		{
		  assertNull(fstEnum.current());
		}
		else
		{
		  Assert.IsNotNull(fstEnum.current());
		  Assert.AreEqual(termsEnum.term().utf8ToString() + " != " + fstEnum.current().input.utf8ToString(), termsEnum.term(), fstEnum.current().input);
		  if (storeOrd)
		  {
			// fst stored the ord
			Assert.AreEqual("term=" + termsEnum.term().utf8ToString() + " " + termsEnum.term(), termsEnum.ord(), (long)((long?) fstEnum.current().output));
		  }
		  else
		  {
			// fst stored the docFreq
			Assert.AreEqual("term=" + termsEnum.term().utf8ToString() + " " + termsEnum.term(), termsEnum.docFreq(), (int)((long)((long?) fstEnum.current().output)));
		  }
		}
	  }

	  private abstract class VisitTerms<T>
	  {
		internal readonly string DirOut;
		internal readonly string WordsFileIn;
		internal int InputMode;
		internal readonly Outputs<T> Outputs;
		internal readonly Builder<T> Builder;
		internal readonly bool DoPack;

		public VisitTerms(string dirOut, string wordsFileIn, int inputMode, int prune, Outputs<T> outputs, bool doPack, bool noArcArrays)
		{
		  this.DirOut = dirOut;
		  this.WordsFileIn = wordsFileIn;
		  this.InputMode = inputMode;
		  this.Outputs = outputs;
		  this.DoPack = doPack;

		  Builder = new Builder<>(inputMode == 0 ? FST.INPUT_TYPE.BYTE1 : FST.INPUT_TYPE.BYTE4, 0, prune, prune == 0, true, int.MaxValue, outputs, null, doPack, PackedInts.DEFAULT, !noArcArrays, 15);
		}

		protected internal abstract T GetOutput(IntsRef input, int ord);

		public virtual void Run(int limit, bool verify, bool verifyByOutput)
		{
		  BufferedReader @is = new BufferedReader(new InputStreamReader(new FileInputStream(WordsFileIn), StandardCharsets.UTF_8), 65536);
		  try
		  {
			IntsRef intsRef = new IntsRef(10);
			long tStart = System.currentTimeMillis();
			int ord = 0;
			while (true)
			{
			  string w = @is.readLine();
			  if (w == null)
			  {
				break;
			  }
			  toIntsRef(w, InputMode, intsRef);
			  Builder.add(intsRef, GetOutput(intsRef, ord));

			  ord++;
			  if (ord % 500000 == 0)
			  {
				Console.WriteLine(string.format(Locale.ROOT, "%6.2fs: %9d...", ((System.currentTimeMillis() - tStart) / 1000.0), ord));
			  }
			  if (ord >= limit)
			  {
				break;
			  }
			}

			long tMid = System.currentTimeMillis();
			Console.WriteLine(((tMid - tStart) / 1000.0) + " sec to add all terms");

			Debug.Assert(Builder.TermCount == ord);
			FST<T> fst = Builder.finish();
			long tEnd = System.currentTimeMillis();
			Console.WriteLine(((tEnd - tMid) / 1000.0) + " sec to finish/pack");
			if (fst == null)
			{
			  Console.WriteLine("FST was fully pruned!");
			  Environment.Exit(0);
			}

			if (DirOut == null)
			{
			  return;
			}

			Console.WriteLine(ord + " terms; " + fst.NodeCount + " nodes; " + fst.ArcCount + " arcs; " + fst.ArcWithOutputCount + " arcs w/ output; tot size " + fst.sizeInBytes());
			if (fst.NodeCount < 100)
			{
			  Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"), StandardCharsets.UTF_8);
			  Util.toDot(fst, w, false, false);
			  w.close();
			  Console.WriteLine("Wrote FST to out.dot");
			}

			Directory dir = FSDirectory.open(new File(DirOut));
			IndexOutput @out = dir.createOutput("fst.bin", IOContext.DEFAULT);
			fst.save(@out);
			@out.close();
			Console.WriteLine("Saved FST to fst.bin.");

			if (!verify)
			{
			  return;
			}

			/*
			IndexInput in = dir.openInput("fst.bin", IOContext.DEFAULT);
			fst = new FST<T>(in, outputs);
			in.close();
			*/

			Console.WriteLine("\nNow verify...");

			while (true)
			{
			  for (int iter = 0;iter < 2;iter++)
			  {
				@is.close();
				@is = new BufferedReader(new InputStreamReader(new FileInputStream(WordsFileIn), StandardCharsets.UTF_8), 65536);

				ord = 0;
				tStart = System.currentTimeMillis();
				while (true)
				{
				  string w = @is.readLine();
				  if (w == null)
				  {
					break;
				  }
				  toIntsRef(w, InputMode, intsRef);
				  if (iter == 0)
				  {
					T expected = GetOutput(intsRef, ord);
					T actual = Util.get(fst, intsRef);
					if (actual == null)
					{
					  throw new Exception("unexpected null output on input=" + w);
					}
					if (!actual.Equals(expected))
					{
					  throw new Exception("wrong output (got " + Outputs.outputToString(actual) + " but expected " + Outputs.outputToString(expected) + ") on input=" + w);
					}
				  }
				  else
				  {
					// Get by output
					long? output = (long?) GetOutput(intsRef, ord);
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") final Lucene.Net.Util.IntsRef actual = Util.getByOutput((FST<Long>) fst, output.longValue());
					IntsRef actual = Util.getByOutput((FST<long?>) fst, (long)output);
					if (actual == null)
					{
					  throw new Exception("unexpected null input from output=" + output);
					}
					if (!actual.Equals(intsRef))
					{
					  throw new Exception("wrong input (got " + actual + " but expected " + intsRef + " from output=" + output);
					}
				  }

				  ord++;
				  if (ord % 500000 == 0)
				  {
					Console.WriteLine(((System.currentTimeMillis() - tStart) / 1000.0) + "s: " + ord + "...");
				  }
				  if (ord >= limit)
				  {
					break;
				  }
				}

				double totSec = ((System.currentTimeMillis() - tStart) / 1000.0);
				Console.WriteLine("Verify " + (iter == 1 ? "(by output) " : "") + "took " + totSec + " sec + (" + (int)((totSec * 1000000000 / ord)) + " nsec per lookup)");

				if (!verifyByOutput)
				{
				  break;
				}
			  }

			  // NOTE: comment out to profile lookup...
			  break;
			}

		  }
		  finally
		  {
			@is.close();
		  }
		}
	  }

	  // TODO: try experiment: reverse terms before
	  // compressing -- how much smaller?

	  // TODO: can FST be used to index all internal substrings,
	  // mapping to term?

	  // java -cp ../build/codecs/classes/java:../test-framework/lib/randomizedtesting-runner-*.jar:../build/core/classes/test:../build/core/classes/test-framework:../build/core/classes/java:../build/test-framework/classes/java:../test-framework/lib/junit-4.10.jar Lucene.Net.Util.Fst.TestFSTs /xold/tmp/allTerms3.txt out
	  public static void Main(string[] args)
	  {
		int prune = 0;
		int limit = int.MaxValue;
		int inputMode = 0; // utf8
		bool storeOrds = false;
		bool storeDocFreqs = false;
		bool verify = true;
		bool doPack = false;
		bool noArcArrays = false;
		string wordsFileIn = null;
		string dirOut = null;

		int idx = 0;
		while (idx < args.Length)
		{
		  if (args[idx].Equals("-prune"))
		  {
			prune = Convert.ToInt32(args[1 + idx]);
			idx++;
		  }
		  else if (args[idx].Equals("-limit"))
		  {
			limit = Convert.ToInt32(args[1 + idx]);
			idx++;
		  }
		  else if (args[idx].Equals("-utf8"))
		  {
			inputMode = 0;
		  }
		  else if (args[idx].Equals("-utf32"))
		  {
			inputMode = 1;
		  }
		  else if (args[idx].Equals("-docFreq"))
		  {
			storeDocFreqs = true;
		  }
		  else if (args[idx].Equals("-noArcArrays"))
		  {
			noArcArrays = true;
		  }
		  else if (args[idx].Equals("-ords"))
		  {
			storeOrds = true;
		  }
		  else if (args[idx].Equals("-noverify"))
		  {
			verify = false;
		  }
		  else if (args[idx].Equals("-pack"))
		  {
			doPack = true;
		  }
		  else if (args[idx].StartsWith("-"))
		  {
			Console.Error.WriteLine("Unrecognized option: " + args[idx]);
			Environment.Exit(-1);
		  }
		  else
		  {
			if (wordsFileIn == null)
			{
			  wordsFileIn = args[idx];
			}
			else if (dirOut == null)
			{
			  dirOut = args[idx];
			}
			else
			{
			  Console.Error.WriteLine("Too many arguments, expected: input [output]");
			  Environment.Exit(-1);
			}
		  }
		  idx++;
		}

		if (wordsFileIn == null)
		{
		  Console.Error.WriteLine("No input file.");
		  Environment.Exit(-1);
		}

		// ord benefits from share, docFreqs don't:

		if (storeOrds && storeDocFreqs)
		{
		  // Store both ord & docFreq:
		  PositiveIntOutputs o1 = PositiveIntOutputs.Singleton;
		  PositiveIntOutputs o2 = PositiveIntOutputs.Singleton;
		  PairOutputs<long?, long?> outputs = new PairOutputs<long?, long?>(o1, o2);
		  new VisitTermsAnonymousInnerClassHelper(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
		  .run(limit, verify, false);
		}
		else if (storeOrds)
		{
		  // Store only ords
		  PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		  new VisitTermsAnonymousInnerClassHelper2(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
		  .run(limit, verify, true);
		}
		else if (storeDocFreqs)
		{
		  // Store only docFreq
		  PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		  new VisitTermsAnonymousInnerClassHelper3(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
		  .run(limit, verify, false);
		}
		else
		{
		  // Store nothing
		  NoOutputs outputs = NoOutputs.Singleton;
		  object NO_OUTPUT = outputs.NoOutput;
		  new VisitTermsAnonymousInnerClassHelper4(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays, NO_OUTPUT)
		  .run(limit, verify, false);
		}
	  }

	  private class VisitTermsAnonymousInnerClassHelper : VisitTerms<PairOutputs.Pair<long?, long?>>
	  {
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private PairOutputs<long?, long?> outputs;
		  private PairOutputs<long?, long?> Outputs;

		  public VisitTermsAnonymousInnerClassHelper<T1>(string dirOut, string wordsFileIn, int inputMode, int prune, PairOutputs<T1> outputs, bool doPack, bool noArcArrays) : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
		  {
			  this.Outputs = outputs;
		  }

		  internal Random rand;
		  public override PairOutputs.Pair<long?, long?> GetOutput(IntsRef input, int ord)
		  {
			if (ord == 0)
			{
			  rand = new Random(17);
			}
			return Outputs.newPair((long) ord, (long) TestUtil.Next(rand, 1, 5000));
		  }
	  }

	  private class VisitTermsAnonymousInnerClassHelper2 : VisitTerms<long?>
	  {
		  public VisitTermsAnonymousInnerClassHelper2(string dirOut, string wordsFileIn, int inputMode, int prune, PositiveIntOutputs outputs, bool doPack, bool noArcArrays) : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
		  {
		  }

		  public override long? GetOutput(IntsRef input, int ord)
		  {
			return (long) ord;
		  }
	  }

	  private class VisitTermsAnonymousInnerClassHelper3 : VisitTerms<long?>
	  {
		  public VisitTermsAnonymousInnerClassHelper3(string dirOut, string wordsFileIn, int inputMode, int prune, PositiveIntOutputs outputs, bool doPack, bool noArcArrays) : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
		  {
		  }

		  internal Random rand;
		  public override long? GetOutput(IntsRef input, int ord)
		  {
			if (ord == 0)
			{
			  rand = new Random(17);
			}
			return (long) TestUtil.Next(rand, 1, 5000);
		  }
	  }

	  private class VisitTermsAnonymousInnerClassHelper4 : VisitTerms<object>
	  {
		  private object NO_OUTPUT;

		  public VisitTermsAnonymousInnerClassHelper4(string dirOut, string wordsFileIn, int inputMode, int prune, NoOutputs outputs, bool doPack, bool noArcArrays, object NO_OUTPUT) : base(dirOut, wordsFileIn, inputMode, prune, outputs, doPack, noArcArrays)
		  {
			  this.NO_OUTPUT = NO_OUTPUT;
		  }

		  public override object GetOutput(IntsRef input, int ord)
		  {
			return NO_OUTPUT;
		  }
	  }

	  public virtual void TestSingleString()
	  {
		Outputs<object> outputs = NoOutputs.Singleton;
		Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);
		b.add(Util.toIntsRef(new BytesRef("foobar"), new IntsRef()), outputs.NoOutput);
		BytesRefFSTEnum<object> fstEnum = new BytesRefFSTEnum<object>(b.finish());
		assertNull(fstEnum.seekFloor(new BytesRef("foo")));
		assertNull(fstEnum.seekCeil(new BytesRef("foobaz")));
	  }


	  public virtual void TestDuplicateFSAString()
	  {
		string str = "foobar";
		Outputs<object> outputs = NoOutputs.Singleton;
		Builder<object> b = new Builder<object>(FST.INPUT_TYPE.BYTE1, outputs);
		IntsRef ints = new IntsRef();
		for (int i = 0; i < 10; i++)
		{
		  b.add(Util.toIntsRef(new BytesRef(str), ints), outputs.NoOutput);
		}
		FST<object> fst = b.finish();

		// count the input paths
		int count = 0;
		BytesRefFSTEnum<object> fstEnum = new BytesRefFSTEnum<object>(fst);
		while (fstEnum.next() != null)
		{
		  count++;
		}
		Assert.AreEqual(1, count);

		Assert.IsNotNull(Util.get(fst, new BytesRef(str)));
		assertNull(Util.get(fst, new BytesRef("foobaz")));
	  }

	  /*
	  public void testTrivial() throws Exception {
	
	    // Get outputs -- passing true means FST will share
	    // (delta code) the outputs.  this should result in
	    // smaller FST if the outputs grow monotonically.  But
	    // if numbers are "random", false should give smaller
	    // final size:
	    final NoOutputs outputs = NoOutputs.getSingleton();
	
	    String[] strings = new String[] {"station", "commotion", "elation", "elastic", "plastic", "stop", "ftop", "ftation", "stat"};
	
	    final Builder<Object> builder = new Builder<Object>(FST.INPUT_TYPE.BYTE1,
	                                                        0, 0,
	                                                        true,
	                                                        true,
	                                                        Integer.MAX_VALUE,
	                                                        outputs,
	                                                        null,
	                                                        true);
	    Arrays.sort(strings);
	    final IntsRef scratch = new IntsRef();
	    for(String s : strings) {
	      builder.add(Util.toIntsRef(new BytesRef(s), scratch), outputs.getNoOutput());
	    }
	    final FST<Object> fst = builder.finish();
	    System.out.println("DOT before rewrite");
	    Writer w = new OutputStreamWriter(new FileOutputStream("/mnt/scratch/before.dot"));
	    Util.toDot(fst, w, false, false);
	    w.close();
	
	    final FST<Object> rewrite = new FST<Object>(fst, 1, 100);
	
	    System.out.println("DOT after rewrite");
	    w = new OutputStreamWriter(new FileOutputStream("/mnt/scratch/after.dot"));
	    Util.toDot(rewrite, w, false, false);
	    w.close();
	  }
	  */

	  public virtual void TestSimple()
	  {

		// Get outputs -- passing true means FST will share
		// (delta code) the outputs.  this should result in
		// smaller FST if the outputs grow monotonically.  But
		// if numbers are "random", false should give smaller
		// final size:
		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;

		// Build an FST mapping BytesRef -> Long
		Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

		BytesRef a = new BytesRef("a");
		BytesRef b = new BytesRef("b");
		BytesRef c = new BytesRef("c");

		builder.add(Util.toIntsRef(a, new IntsRef()), 17L);
		builder.add(Util.toIntsRef(b, new IntsRef()), 42L);
		builder.add(Util.toIntsRef(c, new IntsRef()), 13824324872317238L);

		FST<long?> fst = builder.finish();

		Assert.AreEqual(13824324872317238L, (long) Util.get(fst, c));
		Assert.AreEqual(42, (long) Util.get(fst, b));
		Assert.AreEqual(17, (long) Util.get(fst, a));

		BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);
		BytesRefFSTEnum.InputOutput<long?> seekResult;
		seekResult = fstEnum.seekFloor(a);
		Assert.IsNotNull(seekResult);
		Assert.AreEqual(17, (long) seekResult.output);

		// goes to a
		seekResult = fstEnum.seekFloor(new BytesRef("aa"));
		Assert.IsNotNull(seekResult);
		Assert.AreEqual(17, (long) seekResult.output);

		// goes to b
		seekResult = fstEnum.seekCeil(new BytesRef("aa"));
		Assert.IsNotNull(seekResult);
		Assert.AreEqual(b, seekResult.input);
		Assert.AreEqual(42, (long) seekResult.output);

		Assert.AreEqual(Util.toIntsRef(new BytesRef("c"), new IntsRef()), Util.getByOutput(fst, 13824324872317238L));
		assertNull(Util.getByOutput(fst, 47));
		Assert.AreEqual(Util.toIntsRef(new BytesRef("b"), new IntsRef()), Util.getByOutput(fst, 42));
		Assert.AreEqual(Util.toIntsRef(new BytesRef("a"), new IntsRef()), Util.getByOutput(fst, 17));
	  }

	  public virtual void TestPrimaryKeys()
	  {
		Directory dir = newDirectory();

		for (int cycle = 0;cycle < 2;cycle++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: cycle=" + cycle);
		  }
		  RandomIndexWriter w = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(IndexWriterConfig.OpenMode.CREATE));
		  Document doc = new Document();
		  Field idField = newStringField("id", "", Field.Store.NO);
		  doc.add(idField);

		  int NUM_IDS = atLeast(200);
		  //final int NUM_IDS = (int) (377 * (1.0+random.nextDouble()));
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: NUM_IDS=" + NUM_IDS);
		  }
		  Set<string> allIDs = new HashSet<string>();
		  for (int id = 0;id < NUM_IDS;id++)
		  {
			string idString;
			if (cycle == 0)
			{
			  // PKs are assigned sequentially
			  idString = string.format(Locale.ROOT, "%07d", id);
			}
			else
			{
			  while (true)
			  {
				string s = Convert.ToString(random().nextLong());
				if (!allIDs.contains(s))
				{
				  idString = s;
				  break;
				}
			  }
			}
			allIDs.add(idString);
			idField.StringValue = idString;
			w.addDocument(doc);
		  }

		  //w.forceMerge(1);

		  // turn writer into reader:
		  IndexReader r = w.Reader;
		  IndexSearcher s = newSearcher(r);
		  w.close();

		  IList<string> allIDsList = new List<string>(allIDs);
		  IList<string> sortedAllIDsList = new List<string>(allIDsList);
		  sortedAllIDsList.Sort();

		  // Sprinkle in some non-existent PKs:
		  Set<string> outOfBounds = new HashSet<string>();
		  for (int idx = 0;idx < NUM_IDS / 10;idx++)
		  {
			string idString;
			if (cycle == 0)
			{
			  idString = string.format(Locale.ROOT, "%07d", (NUM_IDS + idx));
			}
			else
			{
			  while (true)
			  {
				idString = Convert.ToString(random().nextLong());
				if (!allIDs.contains(idString))
				{
				  break;
				}
			  }
			}
			outOfBounds.add(idString);
			allIDsList.Add(idString);
		  }

		  // Verify w/ TermQuery
		  for (int iter = 0;iter < 2 * NUM_IDS;iter++)
		  {
			string id = allIDsList[random().Next(allIDsList.Count)];
			bool exists = !outOfBounds.contains(id);
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: TermQuery " + (exists ? "" : "non-exist ") + " id=" + id);
			}
			Assert.AreEqual((exists ? "" : "non-exist ") + "id=" + id, exists ? 1 : 0, s.search(new TermQuery(new Term("id", id)), 1).totalHits);
		  }

		  // Verify w/ MultiTermsEnum
		  TermsEnum termsEnum = MultiFields.getTerms(r, "id").iterator(null);
		  for (int iter = 0;iter < 2 * NUM_IDS;iter++)
		  {
			string id;
			string nextID;
			bool exists;

			if (random().nextBoolean())
			{
			  id = allIDsList[random().Next(allIDsList.Count)];
			  exists = !outOfBounds.contains(id);
			  nextID = null;
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: exactOnly " + (exists ? "" : "non-exist ") + "id=" + id);
			  }
			}
			else
			{
			  // Pick ID between two IDs:
			  exists = false;
			  int idv = random().Next(NUM_IDS - 1);
			  if (cycle == 0)
			  {
				id = string.format(Locale.ROOT, "%07da", idv);
				nextID = string.format(Locale.ROOT, "%07d", idv + 1);
			  }
			  else
			  {
				id = sortedAllIDsList[idv] + "a";
				nextID = sortedAllIDsList[idv + 1];
			  }
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: not exactOnly id=" + id + " nextID=" + nextID);
			  }
			}

			TermsEnum.SeekStatus status;
			if (nextID == null)
			{
			  if (termsEnum.seekExact(new BytesRef(id)))
			  {
				status = TermsEnum.SeekStatus.FOUND;
			  }
			  else
			  {
				status = TermsEnum.SeekStatus.NOT_FOUND;
			  }
			}
			else
			{
			  status = termsEnum.seekCeil(new BytesRef(id));
			}

			if (nextID != null)
			{
			  Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, status);
			  Assert.AreEqual("expected=" + nextID + " actual=" + termsEnum.term().utf8ToString(), new BytesRef(nextID), termsEnum.term());
			}
			else if (!exists)
			{
			  Assert.IsTrue(status == TermsEnum.SeekStatus.NOT_FOUND || status == TermsEnum.SeekStatus.END);
			}
			else
			{
			  Assert.AreEqual(TermsEnum.SeekStatus.FOUND, status);
			}
		  }

		  r.close();
		}
		dir.close();
	  }

	  public virtual void TestRandomTermLookup()
	  {
		Directory dir = newDirectory();

		RandomIndexWriter w = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(IndexWriterConfig.OpenMode.CREATE));
		Document doc = new Document();
		Field f = newStringField("field", "", Field.Store.NO);
		doc.add(f);

		int NUM_TERMS = (int)(1000 * RANDOM_MULTIPLIER * (1 + random().NextDouble()));
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: NUM_TERMS=" + NUM_TERMS);
		}

		Set<string> allTerms = new HashSet<string>();
		while (allTerms.size() < NUM_TERMS)
		{
		  allTerms.add(simpleRandomString(random()));
		}

		foreach (string term in allTerms)
		{
		  f.StringValue = term;
		  w.addDocument(doc);
		}

		// turn writer into reader:
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: get reader");
		}
		IndexReader r = w.Reader;
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: got reader=" + r);
		}
		IndexSearcher s = newSearcher(r);
		w.close();

		IList<string> allTermsList = new List<string>(allTerms);
		Collections.shuffle(allTermsList, random());

		// verify exact lookup
		foreach (string term in allTermsList)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: term=" + term);
		  }
		  Assert.AreEqual("term=" + term, 1, s.search(new TermQuery(new Term("field", term)), 1).totalHits);
		}

		r.close();
		dir.close();
	  }


	  /// <summary>
	  /// Test state expansion (array format) on close-to-root states. Creates
	  /// synthetic input that has one expanded state on each level.
	  /// </summary>
	  /// <seealso cref= "https://issues.apache.org/jira/browse/LUCENE-2933" </seealso>
	  public virtual void TestExpandedCloseToRoot()
	  {
//JAVA TO C# CONVERTER TODO TASK: Local classes are not converted by Java to C# Converter:
//		class SyntheticData
	//	{
	//	  FST<Object> compile(String[] lines) throws IOException
	//	  {
	//		final NoOutputs outputs = NoOutputs.getSingleton();
	//		final Object nothing = outputs.getNoOutput();
	//		final Builder<Object> b = new Builder<>(FST.INPUT_TYPE.BYTE1, outputs);
	//
	//		int line = 0;
	//		final BytesRef term = new BytesRef();
	//		final IntsRef scratchIntsRef = new IntsRef();
	//		while (line < lines.length)
	//		{
	//		  String w = lines[line++];
	//		  if (w == null)
	//		  {
	//			break;
	//		  }
	//		  term.copyChars(w);
	//		  b.add(Util.toIntsRef(term, scratchIntsRef), nothing);
	//		}
	//
	//		return b.finish();
	//	  }
	//
	//	  void generate(ArrayList<String> @out, StringBuilder b, char from, char to, int depth)
	//	  {
	//		if (depth == 0 || from == to)
	//		{
	//		  String seq = b.toString() + "_" + @out.size() + "_end";
	//		  @out.add(seq);
	//		}
	//		else
	//		{
	//		  for (char c = from; c <= to; c++)
	//		  {
	//			b.append(c);
	//			generate(@out, b, from, c == to ? to : from, depth - 1);
	//			b.deleteCharAt(b.length() - 1);
	//		  }
	//		}
	//	  }
	//
	//	  public int verifyStateAndBelow(FST<Object> fst, Arc<Object> arc, int depth) throws IOException
	//	  {
	//		if (FST.targetHasArcs(arc))
	//		{
	//		  int childCount = 0;
	//		  BytesReader fstReader = fst.getBytesReader();
	//		  for (arc = fst.readFirstTargetArc(arc, arc, fstReader); arc = fst.readNextArc(arc, fstReader), childCount++)
	//		  {
	//			boolean expanded = fst.isExpandedTarget(arc, fstReader);
	//			int children = verifyStateAndBelow(fst, new FST.Arc<>().copyFrom(arc), depth + 1);
	//
	//			Assert.AreEqual(expanded, (depth <= FST.FIXED_ARRAY_SHALLOW_DISTANCE && children >= FST.FIXED_ARRAY_NUM_ARCS_SHALLOW) || children >= FST.FIXED_ARRAY_NUM_ARCS_DEEP);
	//			if (arc.isLast())
	//				break;
	//		  }
	//
	//		  return childCount;
	//		}
	//		return 0;
	//	  }
	//	}

		// Sanity check.
		Assert.IsTrue(FST.FIXED_ARRAY_NUM_ARCS_SHALLOW < FST.FIXED_ARRAY_NUM_ARCS_DEEP);
		Assert.IsTrue(FST.FIXED_ARRAY_SHALLOW_DISTANCE >= 0);

		SyntheticData s = new SyntheticData();

		List<string> @out = new List<string>();
		StringBuilder b = new StringBuilder();
		s.generate(@out, b, 'a', 'i', 10);
		string[] input = @out.ToArray();
		Arrays.sort(input);
		FST<object> fst = s.compile(input);
		FST.Arc<object> arc = fst.getFirstArc(new FST.Arc<object>());
		s.verifyStateAndBelow(fst, arc, 1);
	  }

	  public virtual void TestFinalOutputOnEndState()
	  {
		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;

		Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE4, 2, 0, true, true, int.MaxValue, outputs, null, random().nextBoolean(), PackedInts.DEFAULT, true, 15);
		builder.add(Util.toUTF32("stat", new IntsRef()), 17L);
		builder.add(Util.toUTF32("station", new IntsRef()), 10L);
		FST<long?> fst = builder.finish();
		//Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp3/out.dot"));
		StringWriter w = new StringWriter();
		Util.toDot(fst, w, false, false);
		w.close();
		//System.out.println(w.toString());
		Assert.IsTrue(w.ToString().IndexOf("label=\"t/[7]\"") != -1);
	  }

	  public virtual void TestInternalFinalState()
	  {
		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		bool willRewrite = random().nextBoolean();
		Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, 0, 0, true, true, int.MaxValue, outputs, null, willRewrite, PackedInts.DEFAULT, true, 15);
		builder.add(Util.toIntsRef(new BytesRef("stat"), new IntsRef()), outputs.NoOutput);
		builder.add(Util.toIntsRef(new BytesRef("station"), new IntsRef()), outputs.NoOutput);
		FST<long?> fst = builder.finish();
		StringWriter w = new StringWriter();
		//Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp/out.dot"));
		Util.toDot(fst, w, false, false);
		w.close();
		//System.out.println(w.toString());

		// check for accept state at label t
		Assert.IsTrue(w.ToString().IndexOf("[label=\"t\" style=\"bold\"") != -1);
		// check for accept state at label n
		Assert.IsTrue(w.ToString().IndexOf("[label=\"n\" style=\"bold\"") != -1);
	  }

	  // Make sure raw FST can differentiate between final vs
	  // non-final end nodes
	  public virtual void TestNonFinalStopNode()
	  {
		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		long? nothing = outputs.NoOutput;
		Builder<long?> b = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

		FST<long?> fst = new FST<long?>(FST.INPUT_TYPE.BYTE1, outputs, false, PackedInts.COMPACT, true, 15);

		Builder.UnCompiledNode<long?> rootNode = new Builder.UnCompiledNode<long?>(b, 0);

		// Add final stop node
		{
		  Builder.UnCompiledNode<long?> node = new Builder.UnCompiledNode<long?>(b, 0);
		  node.isFinal = true;
		  rootNode.addArc('a', node);
		  Builder.CompiledNode frozen = new Builder.CompiledNode();
		  frozen.node = fst.addNode(node);
		  rootNode.arcs[0].nextFinalOutput = 17L;
		  rootNode.arcs[0].isFinal = true;
		  rootNode.arcs[0].output = nothing;
		  rootNode.arcs[0].target = frozen;
		}

		// Add non-final stop node
		{
		  Builder.UnCompiledNode<long?> node = new Builder.UnCompiledNode<long?>(b, 0);
		  rootNode.addArc('b', node);
		  Builder.CompiledNode frozen = new Builder.CompiledNode();
		  frozen.node = fst.addNode(node);
		  rootNode.arcs[1].nextFinalOutput = nothing;
		  rootNode.arcs[1].output = 42L;
		  rootNode.arcs[1].target = frozen;
		}

		fst.finish(fst.addNode(rootNode));

		StringWriter w = new StringWriter();
		//Writer w = new OutputStreamWriter(new FileOutputStream("/x/tmp3/out.dot"));
		Util.toDot(fst, w, false, false);
		w.close();

		CheckStopNodes(fst, outputs);

		// Make sure it still works after save/load:
		Directory dir = newDirectory();
		IndexOutput @out = dir.createOutput("fst", IOContext.DEFAULT);
		fst.save(@out);
		@out.close();

		IndexInput @in = dir.openInput("fst", IOContext.DEFAULT);
		FST<long?> fst2 = new FST<long?>(@in, outputs);
		CheckStopNodes(fst2, outputs);
		@in.close();
		dir.close();
	  }

	  private void CheckStopNodes(FST<long?> fst, PositiveIntOutputs outputs)
	  {
		long? nothing = outputs.NoOutput;
		FST.Arc<long?> startArc = fst.getFirstArc(new FST.Arc<long?>());
		Assert.AreEqual(nothing, startArc.output);
		Assert.AreEqual(nothing, startArc.nextFinalOutput);

		FST.Arc<long?> arc = fst.readFirstTargetArc(startArc, new FST.Arc<long?>(), fst.BytesReader);
		Assert.AreEqual('a', arc.label);
		Assert.AreEqual(17, (long)arc.nextFinalOutput);
		Assert.IsTrue(arc.Final);

		arc = fst.readNextArc(arc, fst.BytesReader);
		Assert.AreEqual('b', arc.label);
		Assert.IsFalse(arc.Final);
		Assert.AreEqual(42, (long)arc.output);
	  }

	  internal static readonly IComparer<long?> minLongComparator = new ComparatorAnonymousInnerClassHelper();

	  private class ComparatorAnonymousInnerClassHelper : IComparer<long?>
	  {
		  public ComparatorAnonymousInnerClassHelper()
		  {
		  }

		  public virtual int Compare(long? left, long? right)
		  {
			return left.compareTo(right);
		  }
	  }

	  public virtual void TestShortestPaths()
	  {
		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

		IntsRef scratch = new IntsRef();
		builder.add(Util.toIntsRef(new BytesRef("aab"), scratch), 22L);
		builder.add(Util.toIntsRef(new BytesRef("aac"), scratch), 7L);
		builder.add(Util.toIntsRef(new BytesRef("ax"), scratch), 17L);
		FST<long?> fst = builder.finish();
		//Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
		//Util.toDot(fst, w, false, false);
		//w.close();

		Util.TopResults<long?> res = Util.shortestPaths(fst, fst.getFirstArc(new FST.Arc<long?>()), outputs.NoOutput, minLongComparator, 3, true);
		Assert.IsTrue(res.isComplete);
		Assert.AreEqual(3, res.topN.size());
		Assert.AreEqual(Util.toIntsRef(new BytesRef("aac"), scratch), res.topN.get(0).input);
		Assert.AreEqual(7L, (long)res.topN.get(0).output);

		Assert.AreEqual(Util.toIntsRef(new BytesRef("ax"), scratch), res.topN.get(1).input);
		Assert.AreEqual(17L,(long)res.topN.get(1).output);

		Assert.AreEqual(Util.toIntsRef(new BytesRef("aab"), scratch), res.topN.get(2).input);
		Assert.AreEqual(22L, (long)res.topN.get(2).output);
	  }

	  public virtual void TestRejectNoLimits()
	  {
		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);

		IntsRef scratch = new IntsRef();
		builder.add(Util.toIntsRef(new BytesRef("aab"), scratch), 22L);
		builder.add(Util.toIntsRef(new BytesRef("aac"), scratch), 7L);
		builder.add(Util.toIntsRef(new BytesRef("adcd"), scratch), 17L);
		builder.add(Util.toIntsRef(new BytesRef("adcde"), scratch), 17L);

		builder.add(Util.toIntsRef(new BytesRef("ax"), scratch), 17L);
		FST<long?> fst = builder.finish();
		AtomicInteger rejectCount = new AtomicInteger();
		Util.TopNSearcher<long?> searcher = new TopNSearcherAnonymousInnerClassHelper(this, fst, minLongComparator, rejectCount);

		searcher.addStartPaths(fst.getFirstArc(new FST.Arc<long?>()), outputs.NoOutput, true, new IntsRef());
		Util.TopResults<long?> res = searcher.search();
		Assert.AreEqual(rejectCount.get(), 4);
		Assert.IsTrue(res.isComplete); // rejected(4) + topN(2) <= maxQueueSize(6)

		Assert.AreEqual(1, res.topN.size());
		Assert.AreEqual(Util.toIntsRef(new BytesRef("aac"), scratch), res.topN.get(0).input);
		Assert.AreEqual(7L, (long)res.topN.get(0).output);
		rejectCount.set(0);
		searcher = new TopNSearcherAnonymousInnerClassHelper2(this, fst, minLongComparator, rejectCount);

		searcher.addStartPaths(fst.getFirstArc(new FST.Arc<long?>()), outputs.NoOutput, true, new IntsRef());
		res = searcher.search();
		Assert.AreEqual(rejectCount.get(), 4);
		Assert.IsFalse(res.isComplete); // rejected(4) + topN(2) > maxQueueSize(5)
	  }

	  private class TopNSearcherAnonymousInnerClassHelper : Util.TopNSearcher<long?>
	  {
		  private readonly TestFSTs OuterInstance;

		  private AtomicInteger RejectCount;

		  public TopNSearcherAnonymousInnerClassHelper<T1>(TestFSTs outerInstance, FST<T1> fst, UnknownType minLongComparator, AtomicInteger rejectCount) : base(fst, 2, 6, minLongComparator)
		  {
			  this.OuterInstance = outerInstance;
			  this.RejectCount = rejectCount;
		  }

		  protected internal override bool AcceptResult(IntsRef input, long? output)
		  {
			bool accept = (int)output == 7;
			if (!accept)
			{
			  RejectCount.incrementAndGet();
			}
			return accept;
		  }
	  }

	  private class TopNSearcherAnonymousInnerClassHelper2 : Util.TopNSearcher<long?>
	  {
		  private readonly TestFSTs OuterInstance;

		  private AtomicInteger RejectCount;

		  public TopNSearcherAnonymousInnerClassHelper2<T1>(TestFSTs outerInstance, FST<T1> fst, UnknownType minLongComparator, AtomicInteger rejectCount) : base(fst, 2, 5, minLongComparator)
		  {
			  this.OuterInstance = outerInstance;
			  this.RejectCount = rejectCount;
		  }

		  protected internal override bool AcceptResult(IntsRef input, long? output)
		  {
			bool accept = (int)output == 7;
			if (!accept)
			{
			  RejectCount.incrementAndGet();
			}
			return accept;
		  }
	  }

	  // compares just the weight side of the pair
	  internal static readonly IComparer<Pair<long?, long?>> minPairWeightComparator = new ComparatorAnonymousInnerClassHelper2();

	  private class ComparatorAnonymousInnerClassHelper2 : IComparer<Pair<long?, long?>>
	  {
		  public ComparatorAnonymousInnerClassHelper2()
		  {
		  }

		  public virtual int Compare(Pair<long?, long?> left, Pair<long?, long?> right)
		  {
			return left.output1.compareTo(right.output1);
		  }
	  }

	  /// <summary>
	  /// like testShortestPaths, but uses pairoutputs so we have both a weight and an output </summary>
	  public virtual void TestShortestPathsWFST()
	  {

		PairOutputs<long?, long?> outputs = new PairOutputs<long?, long?>(PositiveIntOutputs.Singleton, PositiveIntOutputs.Singleton); // output -  weight

		Builder<Pair<long?, long?>> builder = new Builder<Pair<long?, long?>>(FST.INPUT_TYPE.BYTE1, outputs);

		IntsRef scratch = new IntsRef();
		builder.add(Util.toIntsRef(new BytesRef("aab"), scratch), outputs.newPair(22L, 57L));
		builder.add(Util.toIntsRef(new BytesRef("aac"), scratch), outputs.newPair(7L, 36L));
		builder.add(Util.toIntsRef(new BytesRef("ax"), scratch), outputs.newPair(17L, 85L));
		FST<Pair<long?, long?>> fst = builder.finish();
		//Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
		//Util.toDot(fst, w, false, false);
		//w.close();

		Util.TopResults<Pair<long?, long?>> res = Util.shortestPaths(fst, fst.getFirstArc(new FST.Arc<Pair<long?, long?>>()), outputs.NoOutput, minPairWeightComparator, 3, true);
		Assert.IsTrue(res.isComplete);
		Assert.AreEqual(3, res.topN.size());

		Assert.AreEqual(Util.toIntsRef(new BytesRef("aac"), scratch), res.topN.get(0).input);
		Assert.AreEqual(7L, (long)res.topN.get(0).output.output1); // weight
		Assert.AreEqual(36L, (long)res.topN.get(0).output.output2); // output

		Assert.AreEqual(Util.toIntsRef(new BytesRef("ax"), scratch), res.topN.get(1).input);
		Assert.AreEqual(17L, (long)res.topN.get(1).output.output1); // weight
		Assert.AreEqual(85L, (long)res.topN.get(1).output.output2); // output

		Assert.AreEqual(Util.toIntsRef(new BytesRef("aab"), scratch), res.topN.get(2).input);
		Assert.AreEqual(22L, (long)res.topN.get(2).output.output1); // weight
		Assert.AreEqual(57L, (long)res.topN.get(2).output.output2); // output
	  }

	  public virtual void TestShortestPathsRandom()
	  {
		Random random = random();
		int numWords = atLeast(1000);

		SortedDictionary<string, long?> slowCompletor = new SortedDictionary<string, long?>();
		SortedSet<string> allPrefixes = new SortedSet<string>();

		PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
		Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);
		IntsRef scratch = new IntsRef();

		for (int i = 0; i < numWords; i++)
		{
		  string s;
		  while (true)
		  {
			s = TestUtil.randomSimpleString(random);
			if (!slowCompletor.ContainsKey(s))
			{
			  break;
			}
		  }

		  for (int j = 1; j < s.Length; j++)
		  {
			allPrefixes.Add(s.Substring(0, j));
		  }
		  int weight = TestUtil.Next(random, 1, 100); // weights 1..100
		  slowCompletor[s] = (long)weight;
		}

		foreach (KeyValuePair<string, long?> e in slowCompletor)
		{
		  //System.out.println("add: " + e);
		  builder.add(Util.toIntsRef(new BytesRef(e.Key), scratch), e.Value);
		}

		FST<long?> fst = builder.finish();
		//System.out.println("SAVE out.dot");
		//Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
		//Util.toDot(fst, w, false, false);
		//w.close();

		BytesReader reader = fst.BytesReader;

		//System.out.println("testing: " + allPrefixes.size() + " prefixes");
		foreach (string prefix in allPrefixes)
		{
		  // 1. run prefix against fst, then complete by value
		  //System.out.println("TEST: " + prefix);

		  long prefixOutput = 0;
		  FST.Arc<long?> arc = fst.getFirstArc(new FST.Arc<long?>());
		  for (int idx = 0;idx < prefix.Length;idx++)
		  {
			if (fst.findTargetArc((int) prefix[idx], arc, arc, reader) == null)
			{
			  Assert.Fail();
			}
			prefixOutput += arc.output;
		  }

		  int topN = TestUtil.Next(random, 1, 10);

		  Util.TopResults<long?> r = Util.shortestPaths(fst, arc, fst.outputs.NoOutput, minLongComparator, topN, true);
		  Assert.IsTrue(r.isComplete);

		  // 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
		  IList<Result<long?>> matches = new List<Result<long?>>();

		  // TODO: could be faster... but its slowCompletor for a reason
		  foreach (KeyValuePair<string, long?> e in slowCompletor)
		  {
			if (e.Key.StartsWith(prefix))
			{
			  //System.out.println("  consider " + e.getKey());
			  matches.Add(new Result<>(Util.toIntsRef(new BytesRef(e.Key.Substring(prefix.Length)), new IntsRef()), e.Value - prefixOutput));
			}
		  }

		  Assert.IsTrue(matches.Count > 0);
		  matches.Sort(new TieBreakByInputComparator<>(minLongComparator));
		  if (matches.Count > topN)
		  {
			matches.subList(topN, matches.Count).clear();
		  }

		  Assert.AreEqual(matches.Count, r.topN.size());

		  for (int hit = 0;hit < r.topN.size();hit++)
		  {
			//System.out.println("  check hit " + hit);
			Assert.AreEqual(matches[hit].input, r.topN.get(hit).input);
			Assert.AreEqual(matches[hit].output, r.topN.get(hit).output);
		  }
		}
	  }

	  private class TieBreakByInputComparator<T> : Comparator<Result<T>>
	  {
		internal readonly IComparer<T> Comparator;
		public TieBreakByInputComparator(IComparer<T> comparator)
		{
		  this.Comparator = comparator;
		}

		public virtual int Compare(Result<T> a, Result<T> b)
		{
		  int cmp = Comparator.Compare(a.output, b.output);
		  if (cmp == 0)
		  {
			return a.input.compareTo(b.input);
		  }
		  else
		  {
			return cmp;
		  }
		}
	  }

	  // used by slowcompletor
	  internal class TwoLongs
	  {
		  private readonly TestFSTs OuterInstance;

		internal long a;
		internal long b;

		internal TwoLongs(TestFSTs outerInstance, long a, long b)
		{
			this.OuterInstance = outerInstance;
		  this.a = a;
		  this.b = b;
		}
	  }

	  /// <summary>
	  /// like testShortestPathsRandom, but uses pairoutputs so we have both a weight and an output </summary>
	  public virtual void TestShortestPathsWFSTRandom()
	  {
		int numWords = atLeast(1000);

		SortedDictionary<string, TwoLongs> slowCompletor = new SortedDictionary<string, TwoLongs>();
		SortedSet<string> allPrefixes = new SortedSet<string>();

		PairOutputs<long?, long?> outputs = new PairOutputs<long?, long?>(PositiveIntOutputs.Singleton, PositiveIntOutputs.Singleton); // output -  weight
		Builder<Pair<long?, long?>> builder = new Builder<Pair<long?, long?>>(FST.INPUT_TYPE.BYTE1, outputs);
		IntsRef scratch = new IntsRef();

		Random random = random();
		for (int i = 0; i < numWords; i++)
		{
		  string s;
		  while (true)
		  {
			s = TestUtil.randomSimpleString(random);
			if (!slowCompletor.ContainsKey(s))
			{
			  break;
			}
		  }

		  for (int j = 1; j < s.Length; j++)
		  {
			allPrefixes.Add(s.Substring(0, j));
		  }
		  int weight = TestUtil.Next(random, 1, 100); // weights 1..100
		  int output = TestUtil.Next(random, 0, 500); // outputs 0..500
		  slowCompletor[s] = new TwoLongs(this, weight, output);
		}

		foreach (KeyValuePair<string, TwoLongs> e in slowCompletor)
		{
		  //System.out.println("add: " + e);
		  long weight = e.Value.a;
		  long output = e.Value.b;
		  builder.add(Util.toIntsRef(new BytesRef(e.Key), scratch), outputs.newPair(weight, output));
		}

		FST<Pair<long?, long?>> fst = builder.finish();
		//System.out.println("SAVE out.dot");
		//Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
		//Util.toDot(fst, w, false, false);
		//w.close();

		BytesReader reader = fst.BytesReader;

		//System.out.println("testing: " + allPrefixes.size() + " prefixes");
		foreach (string prefix in allPrefixes)
		{
		  // 1. run prefix against fst, then complete by value
		  //System.out.println("TEST: " + prefix);

		  Pair<long?, long?> prefixOutput = outputs.NoOutput;
		  FST.Arc<Pair<long?, long?>> arc = fst.getFirstArc(new FST.Arc<Pair<long?, long?>>());
		  for (int idx = 0;idx < prefix.Length;idx++)
		  {
			if (fst.findTargetArc((int) prefix[idx], arc, arc, reader) == null)
			{
			  Assert.Fail();
			}
			prefixOutput = outputs.add(prefixOutput, arc.output);
		  }

		  int topN = TestUtil.Next(random, 1, 10);

		  Util.TopResults<Pair<long?, long?>> r = Util.shortestPaths(fst, arc, fst.outputs.NoOutput, minPairWeightComparator, topN, true);
		  Assert.IsTrue(r.isComplete);
		  // 2. go thru whole treemap (slowCompletor) and check its actually the best suggestion
		  IList<Result<Pair<long?, long?>>> matches = new List<Result<Pair<long?, long?>>>();

		  // TODO: could be faster... but its slowCompletor for a reason
		  foreach (KeyValuePair<string, TwoLongs> e in slowCompletor)
		  {
			if (e.Key.StartsWith(prefix))
			{
			  //System.out.println("  consider " + e.getKey());
			  matches.Add(new Result<>(Util.toIntsRef(new BytesRef(e.Key.Substring(prefix.Length)), new IntsRef()), outputs.newPair(e.Value.a - prefixOutput.output1, e.Value.b - prefixOutput.output2)));
			}
		  }

		  Assert.IsTrue(matches.Count > 0);
		  matches.Sort(new TieBreakByInputComparator<>(minPairWeightComparator));
		  if (matches.Count > topN)
		  {
			matches.subList(topN, matches.Count).clear();
		  }

		  Assert.AreEqual(matches.Count, r.topN.size());

		  for (int hit = 0;hit < r.topN.size();hit++)
		  {
			//System.out.println("  check hit " + hit);
			Assert.AreEqual(matches[hit].input, r.topN.get(hit).input);
			Assert.AreEqual(matches[hit].output, r.topN.get(hit).output);
		  }
		}
	  }

	  public virtual void TestLargeOutputsOnArrayArcs()
	  {
		ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
		Builder<BytesRef> builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1, outputs);

		sbyte[] bytes = new sbyte[300];
		IntsRef input = new IntsRef();
		input.grow(1);
		input.length = 1;
		BytesRef output = new BytesRef(bytes);
		for (int arc = 0;arc < 6;arc++)
		{
		  input.ints[0] = arc;
		  output.bytes[0] = (sbyte) arc;
		  builder.add(input, BytesRef.deepCopyOf(output));
		}

		FST<BytesRef> fst = builder.finish();
		for (int arc = 0;arc < 6;arc++)
		{
		  input.ints[0] = arc;
		  BytesRef result = Util.get(fst, input);
		  Assert.IsNotNull(result);
		  Assert.AreEqual(300, result.length);
		  Assert.AreEqual(result.bytes[result.offset], arc);
		  for (int byteIDX = 1;byteIDX < result.length;byteIDX++)
		  {
			Assert.AreEqual(0, result.bytes[result.offset + byteIDX]);
		  }
		}
	  }

	}

}