using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using IndexingChain = Lucene.Net.Index.DocumentsWriterPerThread.IndexingChain;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Store = Field.Store;

    [TestFixture]
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

        [Test]
        public virtual void TestDefaults()
        {
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            Assert.AreEqual(typeof(MockAnalyzer), conf.Analyzer.GetType());
            Assert.IsNull(conf.IndexCommit);
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
            Assert.AreEqual(IndexWriterConfig.DEFAULT_READER_POOLING, conf.UseReaderPooling);
            Assert.IsTrue(DocumentsWriterPerThread.DefaultIndexingChain == conf.IndexingChain);
            Assert.IsNull(conf.MergedSegmentWarmer);
            Assert.AreEqual(IndexWriterConfig.DEFAULT_READER_TERMS_INDEX_DIVISOR, conf.ReaderTermsIndexDivisor);
            Assert.AreEqual(typeof(TieredMergePolicy), conf.MergePolicy.GetType());
            Assert.AreEqual(typeof(DocumentsWriterPerThreadPool), conf.IndexerThreadPool.GetType());
            Assert.AreEqual(typeof(FlushByRamOrCountsPolicy), conf.FlushPolicy.GetType());
            Assert.AreEqual(IndexWriterConfig.DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB, conf.RAMPerThreadHardLimitMB);
            Assert.AreEqual(Codec.Default, conf.Codec);
            Assert.AreEqual((object)InfoStream.Default, conf.InfoStream);
            Assert.AreEqual(IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM, conf.UseCompoundFile);
            // Sanity check - validate that all getters are covered.
            ISet<string> getters = new JCG.HashSet<string>();
            getters.Add("getAnalyzer");
            getters.Add("getIndexCommit");
            getters.Add("getIndexDeletionPolicy");
            getters.Add("getMaxFieldLength");
            getters.Add("getMergeScheduler");
            getters.Add("getOpenMode");
            getters.Add("getSimilarity");
            getters.Add("getTermIndexInterval");
            getters.Add("getWriteLockTimeout");
            getters.Add("getDefaultWriteLockTimeout");
            getters.Add("getMaxBufferedDeleteTerms");
            getters.Add("getRAMBufferSizeMB");
            getters.Add("getMaxBufferedDocs");
            getters.Add("getIndexingChain");
            getters.Add("getMergedSegmentWarmer");
            getters.Add("getMergePolicy");
            getters.Add("getMaxThreadStates");
            getters.Add("getReaderPooling");
            getters.Add("getIndexerThreadPool");
            getters.Add("getReaderTermsIndexDivisor");
            getters.Add("getFlushPolicy");
            getters.Add("getRAMPerThreadHardLimitMB");
            getters.Add("getCodec");
            getters.Add("getInfoStream");
            getters.Add("getUseCompoundFile");

            foreach (MethodInfo m in typeof(IndexWriterConfig).GetMethods())
            {
                if (m.DeclaringType == typeof(IndexWriterConfig) && m.Name.StartsWith("get", StringComparison.Ordinal) && !m.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    Assert.IsTrue(getters.Contains(m.Name), "method " + m.Name + " is not tested for defaults");
                }
            }
        }

        [Test]
        public virtual void TestSettersChaining()
        {
            // Ensures that every setter returns IndexWriterConfig to allow chaining.
            ISet<string> liveSetters = new JCG.HashSet<string>();
            ISet<string> allSetters = new JCG.HashSet<string>();
            foreach (MethodInfo m in typeof(IndexWriterConfig).GetMethods())
            {
                if (m.Name.StartsWith("Set", StringComparison.Ordinal) && !m.IsStatic)
                {
                    allSetters.Add(m.Name);
                    // setters overridden from LiveIndexWriterConfig are returned twice, once with
                    // IndexWriterConfig return type and second with LiveIndexWriterConfig. The ones
                    // from LiveIndexWriterConfig are marked 'synthetic', so just collect them and
                    // assert in the end that we also received them from IWC.
                    // In C# we do not have them marked synthetic so we look at the declaring type instead.
                    if (m.DeclaringType.Name == "LiveIndexWriterConfig")
                    {
                        liveSetters.Add(m.Name);
                    }
                    else
                    {
                        Assert.AreEqual(typeof(IndexWriterConfig), m.ReturnType, "method " + m.Name + " does not return IndexWriterConfig");
                    }
                }
            }
            foreach (string setter in liveSetters)
            {
                Assert.IsTrue(allSetters.Contains(setter), "setter method not overridden by IndexWriterConfig: " + setter);
            }
        }

        [Test]
        public virtual void TestReuse()
        {
            Directory dir = NewDirectory();
            // test that IWC cannot be reused across two IWs
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            (new RandomIndexWriter(Random, dir, conf)).Dispose();

            // this should fail
            try
            {
                Assert.IsNotNull(new RandomIndexWriter(Random, dir, conf));
                Assert.Fail("should have hit AlreadySetException");
            }
#pragma warning disable 168
            catch (AlreadySetException e)
#pragma warning restore 168
            {
                // expected
            }

            // also cloning it won't help, after it has been used already
            try
            {
                Assert.IsNotNull(new RandomIndexWriter(Random, dir, (IndexWriterConfig)conf.Clone()));
                Assert.Fail("should have hit AlreadySetException");
            }
#pragma warning disable 168
            catch (AlreadySetException e)
#pragma warning restore 168
            {
                // expected
            }

            // if it's cloned in advance, it should be ok
            conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
            (new RandomIndexWriter(Random, dir, (IndexWriterConfig)conf.Clone())).Dispose();
            (new RandomIndexWriter(Random, dir, (IndexWriterConfig)conf.Clone())).Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestOverrideGetters()
        {
            // Test that IndexWriterConfig overrides all getters, so that javadocs
            // contain all methods for the users. Also, ensures that IndexWriterConfig
            // doesn't declare getters that are not declared on LiveIWC.
            ISet<string> liveGetters = new JCG.HashSet<string>();
            foreach (MethodInfo m in typeof(LiveIndexWriterConfig).GetMethods())
            {
                if (m.Name.StartsWith("get", StringComparison.Ordinal) && !m.IsStatic)
                {
                    liveGetters.Add(m.Name);
                }
            }

            foreach (MethodInfo m in typeof(IndexWriterConfig).GetMethods())
            {
                if (m.Name.StartsWith("get", StringComparison.Ordinal) && !m.Name.StartsWith("get_", StringComparison.Ordinal) && !m.IsStatic)
                {
                    Assert.AreEqual(typeof(IndexWriterConfig), m.DeclaringType, "method " + m.Name + " not overrided by IndexWriterConfig");
                    Assert.IsTrue(liveGetters.Contains(m.Name), "method " + m.Name + " not declared on LiveIndexWriterConfig");
                }
            }
        }

        [Test]
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

        [Test]
        public virtual void TestToString()
        {
            string str = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).ToString();
            foreach (System.Reflection.FieldInfo f in (typeof(IndexWriterConfig).GetFields(
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly |
                BindingFlags.Static)))
            {
                if (f.IsStatic)
                {
                    // Skip static final fields, they are only constants
                    continue;
                }
                else if ("indexingChain".Equals(f.Name, StringComparison.Ordinal))
                {
                    // indexingChain is a package-private setting and thus is not output by
                    // toString.
                    continue;
                }
                if (f.Name.Equals("inUseByIndexWriter", StringComparison.Ordinal))
                {
                    continue;
                }
                Assert.IsTrue(str.IndexOf(f.Name, StringComparison.Ordinal) != -1, f.Name + " not found in toString");
            }
        }

        [Test]
        public virtual void TestClone()
        {
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriterConfig clone = (IndexWriterConfig)conf.Clone();

            // Make sure parameters that can't be reused are cloned
            IndexDeletionPolicy delPolicy = conf.IndexDeletionPolicy;
            IndexDeletionPolicy delPolicyClone = clone.IndexDeletionPolicy;
            Assert.IsTrue(delPolicy.GetType() == delPolicyClone.GetType() && (delPolicy != delPolicyClone || delPolicy.Clone() == delPolicyClone.Clone()));

            FlushPolicy flushPolicy = conf.FlushPolicy;
            FlushPolicy flushPolicyClone = clone.FlushPolicy;
            Assert.IsTrue(flushPolicy.GetType() == flushPolicyClone.GetType() && (flushPolicy != flushPolicyClone || flushPolicy.Clone() == flushPolicyClone.Clone()));

            DocumentsWriterPerThreadPool pool = conf.IndexerThreadPool;
            DocumentsWriterPerThreadPool poolClone = clone.IndexerThreadPool;
            Assert.IsTrue(pool.GetType() == poolClone.GetType() && (pool != poolClone || pool.Clone() == poolClone.Clone()));

            MergePolicy mergePolicy = conf.MergePolicy;
            MergePolicy mergePolicyClone = clone.MergePolicy;
            Assert.IsTrue(mergePolicy.GetType() == mergePolicyClone.GetType() && (mergePolicy != mergePolicyClone || mergePolicy.Clone() == mergePolicyClone.Clone()));

            IMergeScheduler mergeSched = conf.MergeScheduler;
            IMergeScheduler mergeSchedClone = clone.MergeScheduler;
            Assert.IsTrue(mergeSched.GetType() == mergeSchedClone.GetType() && (mergeSched != mergeSchedClone || mergeSched.Clone() == mergeSchedClone.Clone()));

            conf.SetMergeScheduler(new SerialMergeScheduler());
            Assert.AreEqual(typeof(ConcurrentMergeScheduler), clone.MergeScheduler.GetType());
        }

        [Test]
        public virtual void TestInvalidValues()
        {
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            // Test IndexDeletionPolicy
            Assert.AreEqual(typeof(KeepOnlyLastCommitDeletionPolicy), conf.IndexDeletionPolicy.GetType());
            conf.SetIndexDeletionPolicy(new SnapshotDeletionPolicy(null));
            Assert.AreEqual(typeof(SnapshotDeletionPolicy), conf.IndexDeletionPolicy.GetType());
            try
            {
                conf.SetIndexDeletionPolicy(null);
                Assert.Fail();
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // ok
            }

            // Test MergeScheduler
            Assert.AreEqual(typeof(ConcurrentMergeScheduler), conf.MergeScheduler.GetType());
            conf.SetMergeScheduler(new SerialMergeScheduler());
            Assert.AreEqual(typeof(SerialMergeScheduler), conf.MergeScheduler.GetType());
            try
            {
                conf.SetMergeScheduler(null);
                Assert.Fail();
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // ok
            }

            // Test Similarity:
            // we shouldnt assert what the default is, just that its not null.
            Assert.IsTrue(IndexSearcher.DefaultSimilarity == conf.Similarity);
            conf.SetSimilarity(new MySimilarity());
            Assert.AreEqual(typeof(MySimilarity), conf.Similarity.GetType());
            try
            {
                conf.SetSimilarity(null);
                Assert.Fail();
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // ok
            }

            // Test IndexingChain
            Assert.IsTrue(DocumentsWriterPerThread.DefaultIndexingChain == conf.IndexingChain);
            conf.SetIndexingChain(new MyIndexingChain());
            Assert.AreEqual(typeof(MyIndexingChain), conf.IndexingChain.GetType());
            try
            {
                conf.SetIndexingChain(null);
                Assert.Fail();
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // ok
            }

            try
            {
                conf.SetMaxBufferedDeleteTerms(0);
                Assert.Fail("should not have succeeded to set maxBufferedDeleteTerms to 0");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // this is expected
            }

            try
            {
                conf.SetMaxBufferedDocs(1);
                Assert.Fail("should not have succeeded to set maxBufferedDocs to 1");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // this is expected
            }

            try
            {
                // Disable both MAX_BUF_DOCS and RAM_SIZE_MB
                conf.SetMaxBufferedDocs(4);
                conf.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                conf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                Assert.Fail("should not have succeeded to disable maxBufferedDocs when ramBufferSizeMB is disabled as well");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // this is expected
            }

            conf.SetRAMBufferSizeMB(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
            conf.SetMaxBufferedDocs(IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS);
            try
            {
                conf.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
                Assert.Fail("should not have succeeded to disable ramBufferSizeMB when maxBufferedDocs is disabled as well");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // this is expected
            }

            // Test setReaderTermsIndexDivisor
            try
            {
                conf.SetReaderTermsIndexDivisor(0);
                Assert.Fail("should not have succeeded to set termsIndexDivisor to 0");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // this is expected
            }

            // Setting to -1 is ok
            conf.SetReaderTermsIndexDivisor(-1);
            try
            {
                conf.SetReaderTermsIndexDivisor(-2);
                Assert.Fail("should not have succeeded to set termsIndexDivisor to < -1");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // this is expected
            }

            try
            {
                conf.SetRAMPerThreadHardLimitMB(2048);
                Assert.Fail("should not have succeeded to set RAMPerThreadHardLimitMB to >= 2048");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // this is expected
            }

            try
            {
                conf.SetRAMPerThreadHardLimitMB(0);
                Assert.Fail("should not have succeeded to set RAMPerThreadHardLimitMB to 0");
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // this is expected
            }

            // Test MergePolicy
            Assert.AreEqual(typeof(TieredMergePolicy), conf.MergePolicy.GetType());
            conf.SetMergePolicy(new LogDocMergePolicy());
            Assert.AreEqual(typeof(LogDocMergePolicy), conf.MergePolicy.GetType());
            try
            {
                conf.SetMergePolicy(null);
                Assert.Fail();
            }
            catch (ArgumentNullException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            {
                // ok
            }
        }

        [Test]
        public virtual void TestLiveChangeToCFS()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy(true));
            // Start false:
            iwc.SetUseCompoundFile(false);
            iwc.MergePolicy.NoCFSRatio = 0.0d;
            IndexWriter w = new IndexWriter(dir, iwc);
            // Change to true:
            w.Config.SetUseCompoundFile(true);

            Document doc = new Document();
            doc.Add(NewStringField("field", "foo", Store.NO));
            w.AddDocument(doc);
            w.Commit();
            Assert.IsTrue(w.NewestSegment().Info.UseCompoundFile, "Expected CFS after commit");

            doc.Add(NewStringField("field", "foo", Store.NO));
            w.AddDocument(doc);
            w.Commit();
            w.ForceMerge(1);
            w.Commit();

            // no compound files after merge
            Assert.IsFalse(w.NewestSegment().Info.UseCompoundFile, "Expected Non-CFS after merge");

            MergePolicy lmp = w.Config.MergePolicy;
            lmp.NoCFSRatio = 1.0;
            lmp.MaxCFSSegmentSizeMB = double.PositiveInfinity;

            w.AddDocument(doc);
            w.ForceMerge(1);
            w.Commit();
            Assert.IsTrue(w.NewestSegment().Info.UseCompoundFile, "Expected CFS after merge");
            w.Dispose();
            dir.Dispose();
        }
    }
}