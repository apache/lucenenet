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
	using Codec = Lucene.Net.Codecs.Codec;
	using FieldInfosFormat = Lucene.Net.Codecs.FieldInfosFormat;
	using StoredFieldsFormat = Lucene.Net.Codecs.StoredFieldsFormat;
	using Document = Lucene.Net.Document.Document;
	using Store = Lucene.Net.Document.Field.Store;
	using IndexingChain = Lucene.Net.Index.DocumentsWriterPerThread.IndexingChain;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using InfoStream = Lucene.Net.Util.InfoStream;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using AlreadySetException = Lucene.Net.Util.SetOnce.AlreadySetException;
	using Test = org.junit.Test;

	public class TestIndexWriterConfig : LuceneTestCase
	{

	  private sealed class MySimilarity : DefaultSimilarity
	  {
		// Does not implement anything - used only for type checking on IndexWriterConfig.
	  }

	  private sealed class MyIndexingChain : IndexingChain
	  {
		// Does not implement anything - used only for type checking on IndexWriterConfig.

		internal override DocConsumer GetChain(DocumentsWriterPerThread documentsWriter)
		{
		  return null;
		}

	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDefaults() throws Exception
	  public virtual void TestDefaults()
	  {
		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		Assert.AreEqual(typeof(MockAnalyzer), conf.Analyzer.GetType());
		assertNull(conf.IndexCommit);
		Assert.AreEqual(typeof(KeepOnlyLastCommitDeletionPolicy), conf.IndexDeletionPolicy.GetType());
		Assert.AreEqual(typeof(ConcurrentMergeScheduler), conf.MergeScheduler.GetType());
		Assert.AreEqual(OpenMode.CREATE_OR_APPEND, conf.OpenMode);
		// we don't need to assert this, it should be unspecified
		Assert.IsTrue(IndexSearcher.DefaultSimilarity == conf.Similarity);
		Assert.AreEqual(IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL, conf.TermIndexInterval);
		Assert.AreEqual(IndexWriterConfig.DefaultWriteLockTimeout, conf.WriteLockTimeout);
		Assert.AreEqual(IndexWriterConfig.WRITE_LOCK_TIMEOUT, IndexWriterConfig.DefaultWriteLockTimeout);
		Assert.AreEqual(IndexWriterConfig.DEFAULT_MAX_BUFFERED_DELETE_TERMS, conf.MaxBufferedDeleteTerms);
		Assert.AreEqual(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB, conf.RAMBufferSizeMB, 0.0);
		Assert.AreEqual(IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS, conf.MaxBufferedDocs);
		Assert.AreEqual(IndexWriterConfig.DEFAULT_READER_POOLING, conf.ReaderPooling);
		Assert.IsTrue(DocumentsWriterPerThread.defaultIndexingChain == conf.IndexingChain);
		assertNull(conf.MergedSegmentWarmer);
		Assert.AreEqual(IndexWriterConfig.DEFAULT_READER_TERMS_INDEX_DIVISOR, conf.ReaderTermsIndexDivisor);
		Assert.AreEqual(typeof(TieredMergePolicy), conf.MergePolicy.GetType());
		Assert.AreEqual(typeof(ThreadAffinityDocumentsWriterThreadPool), conf.IndexerThreadPool.GetType());
		Assert.AreEqual(typeof(FlushByRamOrCountsPolicy), conf.FlushPolicy.GetType());
		Assert.AreEqual(IndexWriterConfig.DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB, conf.RAMPerThreadHardLimitMB);
		Assert.AreEqual(Codec.Default, conf.Codec);
		Assert.AreEqual(InfoStream.Default, conf.InfoStream);
		Assert.AreEqual(IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM, conf.UseCompoundFile);
		// Sanity check - validate that all getters are covered.
		Set<string> getters = new HashSet<string>();
		getters.add("getAnalyzer");
		getters.add("getIndexCommit");
		getters.add("getIndexDeletionPolicy");
		getters.add("getMaxFieldLength");
		getters.add("getMergeScheduler");
		getters.add("getOpenMode");
		getters.add("getSimilarity");
		getters.add("getTermIndexInterval");
		getters.add("getWriteLockTimeout");
		getters.add("getDefaultWriteLockTimeout");
		getters.add("getMaxBufferedDeleteTerms");
		getters.add("getRAMBufferSizeMB");
		getters.add("getMaxBufferedDocs");
		getters.add("getIndexingChain");
		getters.add("getMergedSegmentWarmer");
		getters.add("getMergePolicy");
		getters.add("getMaxThreadStates");
		getters.add("getReaderPooling");
		getters.add("getIndexerThreadPool");
		getters.add("getReaderTermsIndexDivisor");
		getters.add("getFlushPolicy");
		getters.add("getRAMPerThreadHardLimitMB");
		getters.add("getCodec");
		getters.add("getInfoStream");
		getters.add("getUseCompoundFile");

		foreach (Method m in typeof(IndexWriterConfig).DeclaredMethods)
		{
		  if (m.DeclaringClass == typeof(IndexWriterConfig) && m.Name.StartsWith("get"))
		  {
			Assert.IsTrue("method " + m.Name + " is not tested for defaults", getters.contains(m.Name));
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSettersChaining() throws Exception
	  public virtual void TestSettersChaining()
	  {
		// Ensures that every setter returns IndexWriterConfig to allow chaining.
		HashSet<string> liveSetters = new HashSet<string>();
		HashSet<string> allSetters = new HashSet<string>();
		foreach (Method m in typeof(IndexWriterConfig).DeclaredMethods)
		{
		  if (m.Name.StartsWith("set") && !Modifier.isStatic(m.Modifiers))
		  {
			allSetters.Add(m.Name);
			// setters overridden from LiveIndexWriterConfig are returned twice, once with 
			// IndexWriterConfig return type and second with LiveIndexWriterConfig. The ones
			// from LiveIndexWriterConfig are marked 'synthetic', so just collect them and
			// assert in the end that we also received them from IWC.
			if (m.Synthetic)
			{
			  liveSetters.Add(m.Name);
			}
			else
			{
			  Assert.AreEqual("method " + m.Name + " does not return IndexWriterConfig", typeof(IndexWriterConfig), m.ReturnType);
			}
		  }
		}
		foreach (string setter in liveSetters)
		{
		  Assert.IsTrue("setter method not overridden by IndexWriterConfig: " + setter, allSetters.Contains(setter));
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testReuse() throws Exception
	  public virtual void TestReuse()
	  {
		Directory dir = newDirectory();
		// test that IWC cannot be reused across two IWs
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, null);
		(new RandomIndexWriter(random(), dir, conf)).close();

		// this should fail
		try
		{
		  Assert.IsNotNull(new RandomIndexWriter(random(), dir, conf));
		  Assert.Fail("should have hit AlreadySetException");
		}
		catch (AlreadySetException e)
		{
		  // expected
		}

		// also cloning it won't help, after it has been used already
		try
		{
		  Assert.IsNotNull(new RandomIndexWriter(random(), dir, conf.clone()));
		  Assert.Fail("should have hit AlreadySetException");
		}
		catch (AlreadySetException e)
		{
		  // expected
		}

		// if it's cloned in advance, it should be ok
		conf = newIndexWriterConfig(TEST_VERSION_CURRENT, null);
		(new RandomIndexWriter(random(), dir, conf.clone())).close();
		(new RandomIndexWriter(random(), dir, conf.clone())).close();

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testOverrideGetters() throws Exception
	  public virtual void TestOverrideGetters()
	  {
		// Test that IndexWriterConfig overrides all getters, so that javadocs
		// contain all methods for the users. Also, ensures that IndexWriterConfig
		// doesn't declare getters that are not declared on LiveIWC.
		HashSet<string> liveGetters = new HashSet<string>();
		foreach (Method m in typeof(LiveIndexWriterConfig).DeclaredMethods)
		{
		  if (m.Name.StartsWith("get") && !Modifier.isStatic(m.Modifiers))
		  {
			liveGetters.Add(m.Name);
		  }
		}

		foreach (Method m in typeof(IndexWriterConfig).DeclaredMethods)
		{
		  if (m.Name.StartsWith("get") && !Modifier.isStatic(m.Modifiers))
		  {
			Assert.AreEqual("method " + m.Name + " not overrided by IndexWriterConfig", typeof(IndexWriterConfig), m.DeclaringClass);
			Assert.IsTrue("method " + m.Name + " not declared on LiveIndexWriterConfig", liveGetters.Contains(m.Name));
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testConstants() throws Exception
	  public virtual void TestConstants()
	  {
		// Tests that the values of the constants does not change
		Assert.AreEqual(1000, IndexWriterConfig.WRITE_LOCK_TIMEOUT);
		Assert.AreEqual(32, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL);
		Assert.AreEqual(-1, IndexWriterConfig.DISABLE_AUTO_FLUSH);
		Assert.AreEqual(IndexWriterConfig.DISABLE_AUTO_FLUSH, IndexWriterConfig.DEFAULT_MAX_BUFFERED_DELETE_TERMS);
		Assert.AreEqual(IndexWriterConfig.DISABLE_AUTO_FLUSH, IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS);
		Assert.AreEqual(16.0, IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB, 0.0);
		Assert.AreEqual(false, IndexWriterConfig.DEFAULT_READER_POOLING);
		Assert.AreEqual(true, IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM);
		Assert.AreEqual(DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IndexWriterConfig.DEFAULT_READER_TERMS_INDEX_DIVISOR);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testToString() throws Exception
	  public virtual void TestToString()
	  {
		string str = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).ToString();
		foreach (Field f in typeof(IndexWriterConfig).DeclaredFields)
		{
		  int modifiers = f.Modifiers;
		  if (Modifier.isStatic(modifiers) && Modifier.isFinal(modifiers))
		  {
			// Skip static final fields, they are only constants
			continue;
		  }
		  else if ("indexingChain".Equals(f.Name))
		  {
			// indexingChain is a package-private setting and thus is not output by
			// toString.
			continue;
		  }
		  if (f.Name.Equals("inUseByIndexWriter"))
		  {
			continue;
		  }
		  Assert.IsTrue(f.Name + " not found in toString", str.IndexOf(f.Name) != -1);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testClone() throws Exception
	  public virtual void TestClone()
	  {
		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriterConfig clone = conf.clone();

		// Make sure parameters that can't be reused are cloned
		IndexDeletionPolicy delPolicy = conf.delPolicy;
		IndexDeletionPolicy delPolicyClone = clone.delPolicy;
		Assert.IsTrue(delPolicy.GetType() == delPolicyClone.GetType() && (delPolicy != delPolicyClone || delPolicy.clone() == delPolicyClone.clone()));

		FlushPolicy flushPolicy = conf.flushPolicy;
		FlushPolicy flushPolicyClone = clone.flushPolicy;
		Assert.IsTrue(flushPolicy.GetType() == flushPolicyClone.GetType() && (flushPolicy != flushPolicyClone || flushPolicy.clone() == flushPolicyClone.clone()));

		DocumentsWriterPerThreadPool pool = conf.indexerThreadPool;
		DocumentsWriterPerThreadPool poolClone = clone.indexerThreadPool;
		Assert.IsTrue(pool.GetType() == poolClone.GetType() && (pool != poolClone || pool.clone() == poolClone.clone()));

		MergePolicy mergePolicy = conf.mergePolicy;
		MergePolicy mergePolicyClone = clone.mergePolicy;
		Assert.IsTrue(mergePolicy.GetType() == mergePolicyClone.GetType() && (mergePolicy != mergePolicyClone || mergePolicy.clone() == mergePolicyClone.clone()));

		MergeScheduler mergeSched = conf.mergeScheduler;
		MergeScheduler mergeSchedClone = clone.mergeScheduler;
		Assert.IsTrue(mergeSched.GetType() == mergeSchedClone.GetType() && (mergeSched != mergeSchedClone || mergeSched.clone() == mergeSchedClone.clone()));

		conf.MergeScheduler = new SerialMergeScheduler();
		Assert.AreEqual(typeof(ConcurrentMergeScheduler), clone.MergeScheduler.GetType());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testInvalidValues() throws Exception
	  public virtual void TestInvalidValues()
	  {
		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		// Test IndexDeletionPolicy
		Assert.AreEqual(typeof(KeepOnlyLastCommitDeletionPolicy), conf.IndexDeletionPolicy.GetType());
		conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(null);
		Assert.AreEqual(typeof(SnapshotDeletionPolicy), conf.IndexDeletionPolicy.GetType());
		try
		{
		  conf.IndexDeletionPolicy = null;
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // ok
		}

		// Test MergeScheduler
		Assert.AreEqual(typeof(ConcurrentMergeScheduler), conf.MergeScheduler.GetType());
		conf.MergeScheduler = new SerialMergeScheduler();
		Assert.AreEqual(typeof(SerialMergeScheduler), conf.MergeScheduler.GetType());
		try
		{
		  conf.MergeScheduler = null;
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // ok
		}

		// Test Similarity: 
		// we shouldnt assert what the default is, just that its not null.
		Assert.IsTrue(IndexSearcher.DefaultSimilarity == conf.Similarity);
		conf.Similarity = new MySimilarity();
		Assert.AreEqual(typeof(MySimilarity), conf.Similarity.GetType());
		try
		{
		  conf.Similarity = null;
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // ok
		}

		// Test IndexingChain
		Assert.IsTrue(DocumentsWriterPerThread.defaultIndexingChain == conf.IndexingChain);
		conf.IndexingChain = new MyIndexingChain();
		Assert.AreEqual(typeof(MyIndexingChain), conf.IndexingChain.GetType());
		try
		{
		  conf.IndexingChain = null;
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // ok
		}

		try
		{
		  conf.MaxBufferedDeleteTerms = 0;
		  Assert.Fail("should not have succeeded to set maxBufferedDeleteTerms to 0");
		}
		catch (System.ArgumentException e)
		{
		  // this is expected
		}

		try
		{
		  conf.MaxBufferedDocs = 1;
		  Assert.Fail("should not have succeeded to set maxBufferedDocs to 1");
		}
		catch (System.ArgumentException e)
		{
		  // this is expected
		}

		try
		{
		  // Disable both MAX_BUF_DOCS and RAM_SIZE_MB
		  conf.MaxBufferedDocs = 4;
		  conf.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		  conf.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		  Assert.Fail("should not have succeeded to disable maxBufferedDocs when ramBufferSizeMB is disabled as well");
		}
		catch (System.ArgumentException e)
		{
		  // this is expected
		}

		conf.RAMBufferSizeMB = IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB;
		conf.MaxBufferedDocs = IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS;
		try
		{
		  conf.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		  Assert.Fail("should not have succeeded to disable ramBufferSizeMB when maxBufferedDocs is disabled as well");
		}
		catch (System.ArgumentException e)
		{
		  // this is expected
		}

		// Test setReaderTermsIndexDivisor
		try
		{
		  conf.ReaderTermsIndexDivisor = 0;
		  Assert.Fail("should not have succeeded to set termsIndexDivisor to 0");
		}
		catch (System.ArgumentException e)
		{
		  // this is expected
		}

		// Setting to -1 is ok
		conf.ReaderTermsIndexDivisor = -1;
		try
		{
		  conf.ReaderTermsIndexDivisor = -2;
		  Assert.Fail("should not have succeeded to set termsIndexDivisor to < -1");
		}
		catch (System.ArgumentException e)
		{
		  // this is expected
		}

		try
		{
		  conf.RAMPerThreadHardLimitMB = 2048;
		  Assert.Fail("should not have succeeded to set RAMPerThreadHardLimitMB to >= 2048");
		}
		catch (System.ArgumentException e)
		{
		  // this is expected
		}

		try
		{
		  conf.RAMPerThreadHardLimitMB = 0;
		  Assert.Fail("should not have succeeded to set RAMPerThreadHardLimitMB to 0");
		}
		catch (System.ArgumentException e)
		{
		  // this is expected
		}

		// Test MergePolicy
		Assert.AreEqual(typeof(TieredMergePolicy), conf.MergePolicy.GetType());
		conf.MergePolicy = new LogDocMergePolicy();
		Assert.AreEqual(typeof(LogDocMergePolicy), conf.MergePolicy.GetType());
		try
		{
		  conf.MergePolicy = null;
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // ok
		}
	  }

	  public virtual void TestLiveChangeToCFS()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.MergePolicy = newLogMergePolicy(true);
		// Start false:
		iwc.UseCompoundFile = false;
		iwc.MergePolicy.NoCFSRatio = 0.0d;
		IndexWriter w = new IndexWriter(dir, iwc);
		// Change to true:
		w.Config.UseCompoundFile = true;

		Document doc = new Document();
		doc.add(newStringField("field", "foo", Store.NO));
		w.addDocument(doc);
		w.commit();
		Assert.IsTrue("Expected CFS after commit", w.newestSegment().info.UseCompoundFile);

		doc.add(newStringField("field", "foo", Store.NO));
		w.addDocument(doc);
		w.commit();
		w.forceMerge(1);
		w.commit();

		// no compound files after merge
		Assert.IsFalse("Expected Non-CFS after merge", w.newestSegment().info.UseCompoundFile);

		MergePolicy lmp = w.Config.MergePolicy;
		lmp.NoCFSRatio = 1.0;
		lmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;

		w.addDocument(doc);
		w.forceMerge(1);
		w.commit();
		Assert.IsTrue("Expected CFS after merge", w.newestSegment().info.UseCompoundFile);
		w.close();
		dir.close();
	  }

	}

}