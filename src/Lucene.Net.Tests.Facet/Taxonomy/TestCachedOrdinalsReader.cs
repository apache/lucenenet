// Lucene version compatibility level 4.8.1
using J2N.Threading;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Facet.Taxonomy
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
    using Document = Lucene.Net.Documents.Document;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;
    [TestFixture]
    public class TestCachedOrdinalsReader : FacetTestCase
    {

        [Test]
        public virtual void TestWithThreads()
        {
            // LUCENE-5303: OrdinalsCache used the ThreadLocal BinaryDV instead of reader.getCoreCacheKey().
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            IndexWriter writer = new IndexWriter(indexDir, conf);
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(new FacetField("A", "1"));
            writer.AddDocument(config.Build(taxoWriter, doc));
            doc = new Document();
            doc.Add(new FacetField("A", "2"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            var reader = DirectoryReader.Open(writer, true);
            CachedOrdinalsReader ordsReader = new CachedOrdinalsReader(new DocValuesOrdinalsReader(FacetsConfig.DEFAULT_INDEX_FIELD_NAME));
            ThreadJob[] threads = new ThreadJob[3];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThreadAnonymousClass(this, "CachedOrdsThread-" + i, reader, ordsReader);
            }

            long ramBytesUsed = 0;
            foreach (ThreadJob t in threads)
            {
                t.Start();
                t.Join();
                if (ramBytesUsed == 0)
                {
                    ramBytesUsed = ordsReader.RamBytesUsed();
                }
                else
                {
                    Assert.AreEqual(ramBytesUsed, ordsReader.RamBytesUsed());
                }
            }

            IOUtils.Dispose(writer, taxoWriter, reader, indexDir, taxoDir);
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestCachedOrdinalsReader outerInstance;

            private readonly DirectoryReader reader;
            private readonly CachedOrdinalsReader ordsReader;

            public ThreadAnonymousClass(TestCachedOrdinalsReader outerInstance, string threadName, DirectoryReader reader, CachedOrdinalsReader ordsReader)
                : base(threadName)
            {
                this.outerInstance = outerInstance;
                this.reader = reader;
                this.ordsReader = ordsReader;
            }

            public override void Run()
            {
                foreach (var context in reader.Leaves)
                {
                    try
                    {
                        ordsReader.GetReader(context);
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        throw RuntimeException.Create(e);
                    }
                }
            }
        }
    }
}