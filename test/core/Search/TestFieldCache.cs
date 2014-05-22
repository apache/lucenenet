using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Search
{

	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Store = Lucene.Net.Document.Field.Store;
	using IntField = Lucene.Net.Document.IntField;
	using LongField = Lucene.Net.Document.LongField;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using StoredField = Lucene.Net.Document.StoredField;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using DocTermOrds = Lucene.Net.Index.DocTermOrds;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using SortedDocValues = Lucene.Net.Index.SortedDocValues;
	using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Bytes = Lucene.Net.Search.FieldCache.Bytes;
	using Doubles = Lucene.Net.Search.FieldCache.Doubles;
	using Floats = Lucene.Net.Search.FieldCache.Floats;
	using Ints = Lucene.Net.Search.FieldCache.Ints;
	using Longs = Lucene.Net.Search.FieldCache.Longs;
	using Shorts = Lucene.Net.Search.FieldCache.Shorts;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestFieldCache : LuceneTestCase
	{
	  private static AtomicReader Reader;
	  private static int NUM_DOCS;
	  private static int NUM_ORDS;
	  private static string[] UnicodeStrings;
	  private static BytesRef[][] MultiValued;
	  private static Directory Directory;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		NUM_DOCS = atLeast(500);
		NUM_ORDS = atLeast(2);
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		long theLong = long.MaxValue;
		double theDouble = double.MaxValue;
		sbyte theByte = sbyte.MaxValue;
		short theShort = short.MaxValue;
		int theInt = int.MaxValue;
		float theFloat = float.MaxValue;
		UnicodeStrings = new string[NUM_DOCS];
//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: MultiValued = new BytesRef[NUM_DOCS][NUM_ORDS];
		MultiValued = RectangularArrays.ReturnRectangularBytesRefArray(NUM_DOCS, NUM_ORDS);
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: setUp");
		}
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("theLong", Convert.ToString(theLong--), Field.Store.NO));
		  doc.add(newStringField("theDouble", Convert.ToString(theDouble--), Field.Store.NO));
		  doc.add(newStringField("theByte", Convert.ToString(theByte--), Field.Store.NO));
		  doc.add(newStringField("theShort", Convert.ToString(theShort--), Field.Store.NO));
		  doc.add(newStringField("theInt", Convert.ToString(theInt--), Field.Store.NO));
		  doc.add(newStringField("theFloat", Convert.ToString(theFloat--), Field.Store.NO));
		  if (i % 2 == 0)
		  {
			doc.add(newStringField("sparse", Convert.ToString(i), Field.Store.NO));
		  }

		  if (i % 2 == 0)
		  {
			doc.add(new IntField("numInt", i, Field.Store.NO));
		  }

		  // sometimes skip the field:
		  if (random().Next(40) != 17)
		  {
			UnicodeStrings[i] = GenerateString(i);
			doc.add(newStringField("theRandomUnicodeString", UnicodeStrings[i], Field.Store.YES));
		  }

		  // sometimes skip the field:
		  if (random().Next(10) != 8)
		  {
			for (int j = 0; j < NUM_ORDS; j++)
			{
			  string newValue = GenerateString(i);
			  MultiValued[i][j] = new BytesRef(newValue);
			  doc.add(newStringField("theRandomUnicodeMultiValuedField", newValue, Field.Store.YES));
			}
			Arrays.sort(MultiValued[i]);
		  }
		  writer.addDocument(doc);
		}
		IndexReader r = writer.Reader;
		Reader = SlowCompositeReaderWrapper.wrap(r);
		writer.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Reader.close();
		Reader = null;
		Directory.close();
		Directory = null;
		UnicodeStrings = null;
		MultiValued = null;
	  }

	  public virtual void TestInfoStream()
	  {
		try
		{
		  FieldCache cache = FieldCache.DEFAULT;
		  ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
		  cache.InfoStream = new PrintStream(bos, false, IOUtils.UTF_8);
		  cache.getDoubles(Reader, "theDouble", false);
		  cache.getFloats(Reader, "theDouble", false);
		  Assert.IsTrue(bos.ToString(IOUtils.UTF_8).IndexOf("WARNING") != -1);
		}
		finally
		{
		  FieldCache.DEFAULT.purgeAllCaches();
		}
	  }

	  public virtual void Test()
	  {
		FieldCache cache = FieldCache.DEFAULT;
		FieldCache.Doubles doubles = cache.getDoubles(Reader, "theDouble", random().nextBoolean());
		assertSame("Second request to cache return same array", doubles, cache.getDoubles(Reader, "theDouble", random().nextBoolean()));
		assertSame("Second request with explicit parser return same array", doubles, cache.getDoubles(Reader, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, random().nextBoolean()));
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Assert.IsTrue(doubles.get(i) + " does not equal: " + (double.MaxValue - i), doubles.get(i) == (double.MaxValue - i));
		}

		FieldCache.Longs longs = cache.getLongs(Reader, "theLong", random().nextBoolean());
		assertSame("Second request to cache return same array", longs, cache.getLongs(Reader, "theLong", random().nextBoolean()));
		assertSame("Second request with explicit parser return same array", longs, cache.getLongs(Reader, "theLong", FieldCache.DEFAULT_LONG_PARSER, random().nextBoolean()));
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Assert.IsTrue(longs.get(i) + " does not equal: " + (long.MaxValue - i) + " i=" + i, longs.get(i) == (long.MaxValue - i));
		}

		FieldCache.Bytes bytes = cache.getBytes(Reader, "theByte", random().nextBoolean());
		assertSame("Second request to cache return same array", bytes, cache.getBytes(Reader, "theByte", random().nextBoolean()));
		assertSame("Second request with explicit parser return same array", bytes, cache.getBytes(Reader, "theByte", FieldCache.DEFAULT_BYTE_PARSER, random().nextBoolean()));
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Assert.IsTrue(bytes.get(i) + " does not equal: " + (sbyte.MaxValue - i), bytes.get(i) == (sbyte)(sbyte.MaxValue - i));
		}

		FieldCache.Shorts shorts = cache.getShorts(Reader, "theShort", random().nextBoolean());
		assertSame("Second request to cache return same array", shorts, cache.getShorts(Reader, "theShort", random().nextBoolean()));
		assertSame("Second request with explicit parser return same array", shorts, cache.getShorts(Reader, "theShort", FieldCache.DEFAULT_SHORT_PARSER, random().nextBoolean()));
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Assert.IsTrue(shorts.get(i) + " does not equal: " + (short.MaxValue - i), shorts.get(i) == (short)(short.MaxValue - i));
		}

		FieldCache.Ints ints = cache.getInts(Reader, "theInt", random().nextBoolean());
		assertSame("Second request to cache return same array", ints, cache.getInts(Reader, "theInt", random().nextBoolean()));
		assertSame("Second request with explicit parser return same array", ints, cache.getInts(Reader, "theInt", FieldCache.DEFAULT_INT_PARSER, random().nextBoolean()));
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Assert.IsTrue(ints.get(i) + " does not equal: " + (int.MaxValue - i), ints.get(i) == (int.MaxValue - i));
		}

		FieldCache.Floats floats = cache.getFloats(Reader, "theFloat", random().nextBoolean());
		assertSame("Second request to cache return same array", floats, cache.getFloats(Reader, "theFloat", random().nextBoolean()));
		assertSame("Second request with explicit parser return same array", floats, cache.getFloats(Reader, "theFloat", FieldCache.DEFAULT_FLOAT_PARSER, random().nextBoolean()));
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Assert.IsTrue(floats.get(i) + " does not equal: " + (float.MaxValue - i), floats.get(i) == (float.MaxValue - i));
		}

		Bits docsWithField = cache.getDocsWithField(Reader, "theLong");
		assertSame("Second request to cache return same array", docsWithField, cache.getDocsWithField(Reader, "theLong"));
		Assert.IsTrue("docsWithField(theLong) must be class Bits.MatchAllBits", docsWithField is Bits.MatchAllBits);
		Assert.IsTrue("docsWithField(theLong) Size: " + docsWithField.length() + " is not: " + NUM_DOCS, docsWithField.length() == NUM_DOCS);
		for (int i = 0; i < docsWithField.length(); i++)
		{
		  Assert.IsTrue(docsWithField.get(i));
		}

		docsWithField = cache.getDocsWithField(Reader, "sparse");
		assertSame("Second request to cache return same array", docsWithField, cache.getDocsWithField(Reader, "sparse"));
		Assert.IsFalse("docsWithField(sparse) must not be class Bits.MatchAllBits", docsWithField is Bits.MatchAllBits);
		Assert.IsTrue("docsWithField(sparse) Size: " + docsWithField.length() + " is not: " + NUM_DOCS, docsWithField.length() == NUM_DOCS);
		for (int i = 0; i < docsWithField.length(); i++)
		{
		  Assert.AreEqual(i % 2 == 0, docsWithField.get(i));
		}

		// getTermsIndex
		SortedDocValues termsIndex = cache.getTermsIndex(Reader, "theRandomUnicodeString");
		assertSame("Second request to cache return same array", termsIndex, cache.getTermsIndex(Reader, "theRandomUnicodeString"));
		BytesRef br = new BytesRef();
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  BytesRef term;
		  int ord = termsIndex.getOrd(i);
		  if (ord == -1)
		  {
			term = null;
		  }
		  else
		  {
			termsIndex.lookupOrd(ord, br);
			term = br;
		  }
		  string s = term == null ? null : term.utf8ToString();
		  Assert.IsTrue("for doc " + i + ": " + s + " does not equal: " + UnicodeStrings[i], UnicodeStrings[i] == null || UnicodeStrings[i].Equals(s));
		}

		int nTerms = termsIndex.ValueCount;

		TermsEnum tenum = termsIndex.termsEnum();
		BytesRef val = new BytesRef();
		for (int i = 0; i < nTerms; i++)
		{
		  BytesRef val1 = tenum.next();
		  termsIndex.lookupOrd(i, val);
		  // System.out.println("i="+i);
		  Assert.AreEqual(val, val1);
		}

		// seek the enum around (note this isn't a great test here)
		int num = atLeast(100);
		for (int i = 0; i < num; i++)
		{
		  int k = random().Next(nTerms);
		  termsIndex.lookupOrd(k, val);
		  Assert.AreEqual(TermsEnum.SeekStatus.FOUND, tenum.seekCeil(val));
		  Assert.AreEqual(val, tenum.term());
		}

		for (int i = 0;i < nTerms;i++)
		{
		  termsIndex.lookupOrd(i, val);
		  Assert.AreEqual(TermsEnum.SeekStatus.FOUND, tenum.seekCeil(val));
		  Assert.AreEqual(val, tenum.term());
		}

		// test bad field
		termsIndex = cache.getTermsIndex(Reader, "bogusfield");

		// getTerms
		BinaryDocValues terms = cache.getTerms(Reader, "theRandomUnicodeString", true);
		assertSame("Second request to cache return same array", terms, cache.getTerms(Reader, "theRandomUnicodeString", true));
		Bits bits = cache.getDocsWithField(Reader, "theRandomUnicodeString");
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  terms.get(i, br);
		  BytesRef term;
		  if (!bits.get(i))
		  {
			term = null;
		  }
		  else
		  {
			term = br;
		  }
		  string s = term == null ? null : term.utf8ToString();
		  Assert.IsTrue("for doc " + i + ": " + s + " does not equal: " + UnicodeStrings[i], UnicodeStrings[i] == null || UnicodeStrings[i].Equals(s));
		}

		// test bad field
		terms = cache.getTerms(Reader, "bogusfield", false);

		// getDocTermOrds
		SortedSetDocValues termOrds = cache.getDocTermOrds(Reader, "theRandomUnicodeMultiValuedField");
		int numEntries = cache.CacheEntries.length;
		// ask for it again, and check that we didnt create any additional entries:
		termOrds = cache.getDocTermOrds(Reader, "theRandomUnicodeMultiValuedField");
		Assert.AreEqual(numEntries, cache.CacheEntries.length);

		for (int i = 0; i < NUM_DOCS; i++)
		{
		  termOrds.Document = i;
		  // this will remove identical terms. A DocTermOrds doesn't return duplicate ords for a docId
		  IList<BytesRef> values = new List<BytesRef>(new LinkedHashSet<>(Arrays.asList(MultiValued[i])));
		  foreach (BytesRef v in values)
		  {
			if (v == null)
			{
			  // why does this test use null values... instead of an empty list: confusing
			  break;
			}
			long ord = termOrds.nextOrd();
			Debug.Assert(ord != SortedSetDocValues.NO_MORE_ORDS);
			BytesRef scratch = new BytesRef();
			termOrds.lookupOrd(ord, scratch);
			Assert.AreEqual(v, scratch);
		  }
		  Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, termOrds.nextOrd());
		}

		// test bad field
		termOrds = cache.getDocTermOrds(Reader, "bogusfield");
		Assert.IsTrue(termOrds.ValueCount == 0);

		FieldCache.DEFAULT.purgeByCacheKey(Reader.CoreCacheKey);
	  }

	  public virtual void TestEmptyIndex()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(500));
		writer.close();
		IndexReader r = DirectoryReader.open(dir);
		AtomicReader reader = SlowCompositeReaderWrapper.wrap(r);
		FieldCache.DEFAULT.getTerms(reader, "foobar", true);
		FieldCache.DEFAULT.getTermsIndex(reader, "foobar");
		FieldCache.DEFAULT.purgeByCacheKey(reader.CoreCacheKey);
		r.close();
		dir.close();
	  }

	  private static string GenerateString(int i)
	  {
		string s = null;
		if (i > 0 && random().Next(3) == 1)
		{
		  // reuse past string -- try to find one that's not null
		  for (int iter = 0; iter < 10 && s == null;iter++)
		  {
			s = UnicodeStrings[random().Next(i)];
		  }
		  if (s == null)
		  {
			s = TestUtil.randomUnicodeString(random());
		  }
		}
		else
		{
		  s = TestUtil.randomUnicodeString(random());
		}
		return s;
	  }

	  public virtual void TestDocsWithField()
	  {
		FieldCache cache = FieldCache.DEFAULT;
		cache.purgeAllCaches();
		Assert.AreEqual(0, cache.CacheEntries.length);
		cache.getDoubles(Reader, "theDouble", true);

		// The double[] takes two slots (one w/ null parser, one
		// w/ real parser), and docsWithField should also
		// have been populated:
		Assert.AreEqual(3, cache.CacheEntries.length);
		Bits bits = cache.getDocsWithField(Reader, "theDouble");

		// No new entries should appear:
		Assert.AreEqual(3, cache.CacheEntries.length);
		Assert.IsTrue(bits is Bits.MatchAllBits);

		FieldCache.Ints ints = cache.getInts(Reader, "sparse", true);
		Assert.AreEqual(6, cache.CacheEntries.length);
		Bits docsWithField = cache.getDocsWithField(Reader, "sparse");
		Assert.AreEqual(6, cache.CacheEntries.length);
		for (int i = 0; i < docsWithField.length(); i++)
		{
		  if (i % 2 == 0)
		  {
			Assert.IsTrue(docsWithField.get(i));
			Assert.AreEqual(i, ints.get(i));
		  }
		  else
		  {
			Assert.IsFalse(docsWithField.get(i));
		  }
		}

		FieldCache.Ints numInts = cache.getInts(Reader, "numInt", random().nextBoolean());
		docsWithField = cache.getDocsWithField(Reader, "numInt");
		for (int i = 0; i < docsWithField.length(); i++)
		{
		  if (i % 2 == 0)
		  {
			Assert.IsTrue(docsWithField.get(i));
			Assert.AreEqual(i, numInts.get(i));
		  }
		  else
		  {
			Assert.IsFalse(docsWithField.get(i));
		  }
		}
	  }

	  public virtual void TestGetDocsWithFieldThreadSafety()
	  {
		FieldCache cache = FieldCache.DEFAULT;
		cache.purgeAllCaches();

		int NUM_THREADS = 3;
		Thread[] threads = new Thread[NUM_THREADS];
		AtomicBoolean failed = new AtomicBoolean();
		AtomicInteger iters = new AtomicInteger();
		int NUM_ITER = 200 * RANDOM_MULTIPLIER;
		CyclicBarrier restart = new CyclicBarrier(NUM_THREADS, new RunnableAnonymousInnerClassHelper(this, cache, iters));
		for (int threadIDX = 0;threadIDX < NUM_THREADS;threadIDX++)
		{
		  threads[threadIDX] = new ThreadAnonymousInnerClassHelper(this, cache, failed, iters, NUM_ITER, restart);
		  threads[threadIDX].Start();
		}

		for (int threadIDX = 0;threadIDX < NUM_THREADS;threadIDX++)
		{
		  threads[threadIDX].Join();
		}
		Assert.IsFalse(failed.get());
	  }

	  private class RunnableAnonymousInnerClassHelper : Runnable
	  {
		  private readonly TestFieldCache OuterInstance;

		  private FieldCache Cache;
		  private AtomicInteger Iters;

		  public RunnableAnonymousInnerClassHelper(TestFieldCache outerInstance, FieldCache cache, AtomicInteger iters)
		  {
			  this.OuterInstance = outerInstance;
			  this.Cache = cache;
			  this.Iters = iters;
		  }

		  public override void Run()
		  {
			Cache.purgeAllCaches();
			Iters.incrementAndGet();
		  }
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestFieldCache OuterInstance;

		  private FieldCache Cache;
		  private AtomicBoolean Failed;
		  private AtomicInteger Iters;
		  private int NUM_ITER;
		  private CyclicBarrier Restart;

		  public ThreadAnonymousInnerClassHelper(TestFieldCache outerInstance, FieldCache cache, AtomicBoolean failed, AtomicInteger iters, int NUM_ITER, CyclicBarrier restart)
		  {
			  this.OuterInstance = outerInstance;
			  this.Cache = cache;
			  this.Failed = failed;
			  this.Iters = iters;
			  this.NUM_ITER = NUM_ITER;
			  this.Restart = restart;
		  }

		  public override void Run()
		  {

			try
			{
			  while (!Failed.get())
			  {
				int op = random().Next(3);
				if (op == 0)
				{
				  // Purge all caches & resume, once all
				  // threads get here:
				  Restart.@await();
				  if (Iters.get() >= NUM_ITER)
				  {
					break;
				  }
				}
				else if (op == 1)
				{
				  Bits docsWithField = Cache.getDocsWithField(Reader, "sparse");
				  for (int i = 0; i < docsWithField.length(); i++)
				  {
					Assert.AreEqual(i % 2 == 0, docsWithField.get(i));
				  }
				}
				else
				{
				  FieldCache.Ints ints = Cache.getInts(Reader, "sparse", true);
				  Bits docsWithField = Cache.getDocsWithField(Reader, "sparse");
				  for (int i = 0; i < docsWithField.length(); i++)
				  {
					if (i % 2 == 0)
					{
					  Assert.IsTrue(docsWithField.get(i));
					  Assert.AreEqual(i, ints.get(i));
					}
					else
					{
					  Assert.IsFalse(docsWithField.get(i));
					}
				  }
				}
			  }
			}
			catch (Exception t)
			{
			  Failed.set(true);
			  Restart.reset();
			  throw new Exception(t);
			}
		  }
	  }

	  public virtual void TestDocValuesIntegration()
	  {
		assumeTrue("3.x does not support docvalues", defaultCodecSupportsDocValues());
		Directory dir = newDirectory();
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, null);
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);
		Document doc = new Document();
		doc.add(new BinaryDocValuesField("binary", new BytesRef("binary value")));
		doc.add(new SortedDocValuesField("sorted", new BytesRef("sorted value")));
		doc.add(new NumericDocValuesField("numeric", 42));
		if (defaultCodecSupportsSortedSet())
		{
		  doc.add(new SortedSetDocValuesField("sortedset", new BytesRef("sortedset value1")));
		  doc.add(new SortedSetDocValuesField("sortedset", new BytesRef("sortedset value2")));
		}
		iw.addDocument(doc);
		DirectoryReader ir = iw.Reader;
		iw.close();
		AtomicReader ar = getOnlySegmentReader(ir);

		BytesRef scratch = new BytesRef();

		// Binary type: can be retrieved via getTerms()
		try
		{
		  FieldCache.DEFAULT.getInts(ar, "binary", false);
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		BinaryDocValues binary = FieldCache.DEFAULT.getTerms(ar, "binary", true);
		binary.get(0, scratch);
		Assert.AreEqual("binary value", scratch.utf8ToString());

		try
		{
		  FieldCache.DEFAULT.getTermsIndex(ar, "binary");
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		try
		{
		  FieldCache.DEFAULT.getDocTermOrds(ar, "binary");
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		try
		{
		  new DocTermOrds(ar, null, "binary");
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		Bits bits = FieldCache.DEFAULT.getDocsWithField(ar, "binary");
		Assert.IsTrue(bits.get(0));

		// Sorted type: can be retrieved via getTerms(), getTermsIndex(), getDocTermOrds()
		try
		{
		  FieldCache.DEFAULT.getInts(ar, "sorted", false);
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		try
		{
		  new DocTermOrds(ar, null, "sorted");
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		binary = FieldCache.DEFAULT.getTerms(ar, "sorted", true);
		binary.get(0, scratch);
		Assert.AreEqual("sorted value", scratch.utf8ToString());

		SortedDocValues sorted = FieldCache.DEFAULT.getTermsIndex(ar, "sorted");
		Assert.AreEqual(0, sorted.getOrd(0));
		Assert.AreEqual(1, sorted.ValueCount);
		sorted.get(0, scratch);
		Assert.AreEqual("sorted value", scratch.utf8ToString());

		SortedSetDocValues sortedSet = FieldCache.DEFAULT.getDocTermOrds(ar, "sorted");
		sortedSet.Document = 0;
		Assert.AreEqual(0, sortedSet.nextOrd());
		Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.nextOrd());
		Assert.AreEqual(1, sortedSet.ValueCount);

		bits = FieldCache.DEFAULT.getDocsWithField(ar, "sorted");
		Assert.IsTrue(bits.get(0));

		// Numeric type: can be retrieved via getInts() and so on
		Ints numeric = FieldCache.DEFAULT.getInts(ar, "numeric", false);
		Assert.AreEqual(42, numeric.get(0));

		try
		{
		  FieldCache.DEFAULT.getTerms(ar, "numeric", true);
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		try
		{
		  FieldCache.DEFAULT.getTermsIndex(ar, "numeric");
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		try
		{
		  FieldCache.DEFAULT.getDocTermOrds(ar, "numeric");
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		try
		{
		  new DocTermOrds(ar, null, "numeric");
		  Assert.Fail();
		}
		catch (IllegalStateException expected)
		{
		}

		bits = FieldCache.DEFAULT.getDocsWithField(ar, "numeric");
		Assert.IsTrue(bits.get(0));

		// SortedSet type: can be retrieved via getDocTermOrds() 
		if (defaultCodecSupportsSortedSet())
		{
		  try
		  {
			FieldCache.DEFAULT.getInts(ar, "sortedset", false);
			Assert.Fail();
		  }
		  catch (IllegalStateException expected)
		  {
		  }

		  try
		  {
			FieldCache.DEFAULT.getTerms(ar, "sortedset", true);
			Assert.Fail();
		  }
		  catch (IllegalStateException expected)
		  {
		  }

		  try
		  {
			FieldCache.DEFAULT.getTermsIndex(ar, "sortedset");
			Assert.Fail();
		  }
		  catch (IllegalStateException expected)
		  {
		  }

		  try
		  {
			new DocTermOrds(ar, null, "sortedset");
			Assert.Fail();
		  }
		  catch (IllegalStateException expected)
		  {
		  }

		  sortedSet = FieldCache.DEFAULT.getDocTermOrds(ar, "sortedset");
		  sortedSet.Document = 0;
		  Assert.AreEqual(0, sortedSet.nextOrd());
		  Assert.AreEqual(1, sortedSet.nextOrd());
		  Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.nextOrd());
		  Assert.AreEqual(2, sortedSet.ValueCount);

		  bits = FieldCache.DEFAULT.getDocsWithField(ar, "sortedset");
		  Assert.IsTrue(bits.get(0));
		}

		ir.close();
		dir.close();
	  }

	  public virtual void TestNonexistantFields()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		iw.addDocument(doc);
		DirectoryReader ir = iw.Reader;
		iw.close();

		AtomicReader ar = getOnlySegmentReader(ir);

		FieldCache cache = FieldCache.DEFAULT;
		cache.purgeAllCaches();
		Assert.AreEqual(0, cache.CacheEntries.length);

		Bytes bytes = cache.getBytes(ar, "bogusbytes", true);
		Assert.AreEqual(0, bytes.get(0));

		Shorts shorts = cache.getShorts(ar, "bogusshorts", true);
		Assert.AreEqual(0, shorts.get(0));

		Ints ints = cache.getInts(ar, "bogusints", true);
		Assert.AreEqual(0, ints.get(0));

		Longs longs = cache.getLongs(ar, "boguslongs", true);
		Assert.AreEqual(0, longs.get(0));

		Floats floats = cache.getFloats(ar, "bogusfloats", true);
		Assert.AreEqual(0, floats.get(0), 0.0f);

		Doubles doubles = cache.getDoubles(ar, "bogusdoubles", true);
		Assert.AreEqual(0, doubles.get(0), 0.0D);

		BytesRef scratch = new BytesRef();
		BinaryDocValues binaries = cache.getTerms(ar, "bogusterms", true);
		binaries.get(0, scratch);
		Assert.AreEqual(0, scratch.length);

		SortedDocValues sorted = cache.getTermsIndex(ar, "bogustermsindex");
		Assert.AreEqual(-1, sorted.getOrd(0));
		sorted.get(0, scratch);
		Assert.AreEqual(0, scratch.length);

		SortedSetDocValues sortedSet = cache.getDocTermOrds(ar, "bogusmultivalued");
		sortedSet.Document = 0;
		Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.nextOrd());

		Bits bits = cache.getDocsWithField(ar, "bogusbits");
		Assert.IsFalse(bits.get(0));

		// check that we cached nothing
		Assert.AreEqual(0, cache.CacheEntries.length);
		ir.close();
		dir.close();
	  }

	  public virtual void TestNonIndexedFields()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new StoredField("bogusbytes", "bogus"));
		doc.add(new StoredField("bogusshorts", "bogus"));
		doc.add(new StoredField("bogusints", "bogus"));
		doc.add(new StoredField("boguslongs", "bogus"));
		doc.add(new StoredField("bogusfloats", "bogus"));
		doc.add(new StoredField("bogusdoubles", "bogus"));
		doc.add(new StoredField("bogusterms", "bogus"));
		doc.add(new StoredField("bogustermsindex", "bogus"));
		doc.add(new StoredField("bogusmultivalued", "bogus"));
		doc.add(new StoredField("bogusbits", "bogus"));
		iw.addDocument(doc);
		DirectoryReader ir = iw.Reader;
		iw.close();

		AtomicReader ar = getOnlySegmentReader(ir);

		FieldCache cache = FieldCache.DEFAULT;
		cache.purgeAllCaches();
		Assert.AreEqual(0, cache.CacheEntries.length);

		Bytes bytes = cache.getBytes(ar, "bogusbytes", true);
		Assert.AreEqual(0, bytes.get(0));

		Shorts shorts = cache.getShorts(ar, "bogusshorts", true);
		Assert.AreEqual(0, shorts.get(0));

		Ints ints = cache.getInts(ar, "bogusints", true);
		Assert.AreEqual(0, ints.get(0));

		Longs longs = cache.getLongs(ar, "boguslongs", true);
		Assert.AreEqual(0, longs.get(0));

		Floats floats = cache.getFloats(ar, "bogusfloats", true);
		Assert.AreEqual(0, floats.get(0), 0.0f);

		Doubles doubles = cache.getDoubles(ar, "bogusdoubles", true);
		Assert.AreEqual(0, doubles.get(0), 0.0D);

		BytesRef scratch = new BytesRef();
		BinaryDocValues binaries = cache.getTerms(ar, "bogusterms", true);
		binaries.get(0, scratch);
		Assert.AreEqual(0, scratch.length);

		SortedDocValues sorted = cache.getTermsIndex(ar, "bogustermsindex");
		Assert.AreEqual(-1, sorted.getOrd(0));
		sorted.get(0, scratch);
		Assert.AreEqual(0, scratch.length);

		SortedSetDocValues sortedSet = cache.getDocTermOrds(ar, "bogusmultivalued");
		sortedSet.Document = 0;
		Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.nextOrd());

		Bits bits = cache.getDocsWithField(ar, "bogusbits");
		Assert.IsFalse(bits.get(0));

		// check that we cached nothing
		Assert.AreEqual(0, cache.CacheEntries.length);
		ir.close();
		dir.close();
	  }

	  // Make sure that the use of GrowableWriter doesn't prevent from using the full long range
	  public virtual void TestLongFieldCache()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig cfg = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		cfg.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, cfg);
		Document doc = new Document();
		LongField field = new LongField("f", 0L, Field.Store.YES);
		doc.add(field);
		long[] values = new long[TestUtil.Next(random(), 1, 10)];
		for (int i = 0; i < values.Length; ++i)
		{
		  long v;
		  switch (random().Next(10))
		  {
			case 0:
			  v = long.MinValue;
			  break;
			case 1:
			  v = 0;
			  break;
			case 2:
			  v = long.MaxValue;
			  break;
			default:
			  v = TestUtil.nextLong(random(), -10, 10);
			  break;
		  }
		  values[i] = v;
		  if (v == 0 && random().nextBoolean())
		  {
			// missing
			iw.addDocument(new Document());
		  }
		  else
		  {
			field.LongValue = v;
			iw.addDocument(doc);
		  }
		}
		iw.forceMerge(1);
		DirectoryReader reader = iw.Reader;
		FieldCache.Longs longs = FieldCache.DEFAULT.getLongs(getOnlySegmentReader(reader), "f", false);
		for (int i = 0; i < values.Length; ++i)
		{
		  Assert.AreEqual(values[i], longs.get(i));
		}
		reader.close();
		iw.close();
		dir.close();
	  }

	  // Make sure that the use of GrowableWriter doesn't prevent from using the full int range
	  public virtual void TestIntFieldCache()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig cfg = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		cfg.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, cfg);
		Document doc = new Document();
		IntField field = new IntField("f", 0, Field.Store.YES);
		doc.add(field);
		int[] values = new int[TestUtil.Next(random(), 1, 10)];
		for (int i = 0; i < values.Length; ++i)
		{
		  int v;
		  switch (random().Next(10))
		  {
			case 0:
			  v = int.MinValue;
			  break;
			case 1:
			  v = 0;
			  break;
			case 2:
			  v = int.MaxValue;
			  break;
			default:
			  v = TestUtil.Next(random(), -10, 10);
			  break;
		  }
		  values[i] = v;
		  if (v == 0 && random().nextBoolean())
		  {
			// missing
			iw.addDocument(new Document());
		  }
		  else
		  {
			field.IntValue = v;
			iw.addDocument(doc);
		  }
		}
		iw.forceMerge(1);
		DirectoryReader reader = iw.Reader;
		FieldCache.Ints ints = FieldCache.DEFAULT.getInts(getOnlySegmentReader(reader), "f", false);
		for (int i = 0; i < values.Length; ++i)
		{
		  Assert.AreEqual(values[i], ints.get(i));
		}
		reader.close();
		iw.close();
		dir.close();
	  }

	}

}