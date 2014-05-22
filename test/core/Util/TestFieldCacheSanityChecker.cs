using System;

namespace Lucene.Net.Util
{

	/// <summary>
	/// Copyright 2009 The Apache Software Foundation
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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using MultiReader = Lucene.Net.Index.MultiReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using Insanity = Lucene.Net.Util.FieldCacheSanityChecker.Insanity;
	using InsanityType = Lucene.Net.Util.FieldCacheSanityChecker.InsanityType;

	public class TestFieldCacheSanityChecker : LuceneTestCase
	{

	  protected internal AtomicReader ReaderA;
	  protected internal AtomicReader ReaderB;
	  protected internal AtomicReader ReaderX;
	  protected internal AtomicReader ReaderAclone;
	  protected internal Directory DirA, DirB;
	  private const int NUM_DOCS = 1000;

	  public override void SetUp()
	  {
		base.setUp();
		DirA = newDirectory();
		DirB = newDirectory();

		IndexWriter wA = new IndexWriter(DirA, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		IndexWriter wB = new IndexWriter(DirB, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		long theLong = long.MaxValue;
		double theDouble = double.MaxValue;
		sbyte theByte = sbyte.MaxValue;
		short theShort = short.MaxValue;
		int theInt = int.MaxValue;
		float theFloat = float.MaxValue;
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("theLong", Convert.ToString(theLong--), Field.Store.NO));
		  doc.add(newStringField("theDouble", Convert.ToString(theDouble--), Field.Store.NO));
		  doc.add(newStringField("theByte", Convert.ToString(theByte--), Field.Store.NO));
		  doc.add(newStringField("theShort", Convert.ToString(theShort--), Field.Store.NO));
		  doc.add(newStringField("theInt", Convert.ToString(theInt--), Field.Store.NO));
		  doc.add(newStringField("theFloat", Convert.ToString(theFloat--), Field.Store.NO));
		  if (0 == i % 3)
		  {
			wA.addDocument(doc);
		  }
		  else
		  {
			wB.addDocument(doc);
		  }
		}
		wA.close();
		wB.close();
		DirectoryReader rA = DirectoryReader.open(DirA);
		ReaderA = SlowCompositeReaderWrapper.wrap(rA);
		ReaderAclone = SlowCompositeReaderWrapper.wrap(rA);
		ReaderA = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(DirA));
		ReaderB = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(DirB));
		ReaderX = SlowCompositeReaderWrapper.wrap(new MultiReader(ReaderA, ReaderB));
	  }

	  public override void TearDown()
	  {
		ReaderA.close();
		ReaderAclone.close();
		ReaderB.close();
		ReaderX.close();
		DirA.close();
		DirB.close();
		base.tearDown();
	  }

	  public virtual void TestSanity()
	  {
		FieldCache cache = FieldCache.DEFAULT;
		cache.purgeAllCaches();

		cache.getDoubles(ReaderA, "theDouble", false);
		cache.getDoubles(ReaderA, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false);
		cache.getDoubles(ReaderAclone, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false);
		cache.getDoubles(ReaderB, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false);

		cache.getInts(ReaderX, "theInt", false);
		cache.getInts(ReaderX, "theInt", FieldCache.DEFAULT_INT_PARSER, false);

		// // // 

		Insanity[] insanity = FieldCacheSanityChecker.checkSanity(cache.CacheEntries);

		if (0 < insanity.Length)
		{
		  dumpArray(TestClass.Name + "#" + TestName + " INSANITY", insanity, System.err);
		}

		Assert.AreEqual("shouldn't be any cache insanity", 0, insanity.Length);
		cache.purgeAllCaches();
	  }

	  public virtual void TestInsanity1()
	  {
		FieldCache cache = FieldCache.DEFAULT;
		cache.purgeAllCaches();

		cache.getInts(ReaderX, "theInt", FieldCache.DEFAULT_INT_PARSER, false);
		cache.getTerms(ReaderX, "theInt", false);
		cache.getBytes(ReaderX, "theByte", false);

		// // // 

		Insanity[] insanity = FieldCacheSanityChecker.checkSanity(cache.CacheEntries);

		Assert.AreEqual("wrong number of cache errors", 1, insanity.Length);
		Assert.AreEqual("wrong type of cache error", InsanityType.VALUEMISMATCH, insanity[0].Type);
		Assert.AreEqual("wrong number of entries in cache error", 2, insanity[0].CacheEntries.length);

		// we expect bad things, don't let tearDown complain about them
		cache.purgeAllCaches();
	  }

	  public virtual void TestInsanity2()
	  {
		FieldCache cache = FieldCache.DEFAULT;
		cache.purgeAllCaches();

		cache.getTerms(ReaderA, "theInt", false);
		cache.getTerms(ReaderB, "theInt", false);
		cache.getTerms(ReaderX, "theInt", false);

		cache.getBytes(ReaderX, "theByte", false);


		// // // 

		Insanity[] insanity = FieldCacheSanityChecker.checkSanity(cache.CacheEntries);

		Assert.AreEqual("wrong number of cache errors", 1, insanity.Length);
		Assert.AreEqual("wrong type of cache error", InsanityType.SUBREADER, insanity[0].Type);
		Assert.AreEqual("wrong number of entries in cache error", 3, insanity[0].CacheEntries.length);

		// we expect bad things, don't let tearDown complain about them
		cache.purgeAllCaches();
	  }

	}

}