// Lucene version compatibility level 4.8.1
using J2N.Threading.Atomic;
using System;
using System.Collections.Concurrent;
using System.IO;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using J2N.Threading;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
    using Document = Lucene.Net.Documents.Document;
    using ITaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.ITaxonomyWriterCache;
    using Cl2oTaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.Cl2oTaxonomyWriterCache;
    using LruTaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.LruTaxonomyWriterCache;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;

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
    [TestFixture]
    public class TestConcurrentFacetedIndexing : FacetTestCase
    {

        // A No-Op ITaxonomyWriterCache which always discards all given categories, and
        // always returns true in put(), to indicate some cache entries were cleared.
        private static ITaxonomyWriterCache NO_OP_CACHE = new TaxonomyWriterCacheAnonymousClass();

        private sealed class TaxonomyWriterCacheAnonymousClass : ITaxonomyWriterCache
        {
            public TaxonomyWriterCacheAnonymousClass()
            {
            }


            public void Dispose()
            {
            }
            public int Get(FacetLabel categoryPath)
            {
                return -1;
            }
            public bool Put(FacetLabel categoryPath, int ordinal)
            {
                return true;
            }
            public bool IsFull => true;

            public void Clear()
            {
            }

        }

        internal static FacetField NewCategory()
        {
            Random r = Random;
            string l1 = "l1." + r.Next(10); // l1.0-l1.9 (10 categories)
            string l2 = "l2." + r.Next(30); // l2.0-l2.29 (30 categories)
            string l3 = "l3." + r.Next(100); // l3.0-l3.99 (100 categories)
            return new FacetField(l1, l2, l3);
        }

        internal static ITaxonomyWriterCache NewTaxoWriterCache(int ndocs)
        {
            double d = Random.NextDouble();
            if (d < 0.7)
            {
                // this is the fastest, yet most memory consuming
                return new Cl2oTaxonomyWriterCache(1024, 0.15f, 3);
            }
            // LUCENENET specific - this option takes too long to get under the 1 hour job limit in Azure DevOps
            //else if (TestNightly && d > 0.98)
            //{
            //    // this is the slowest, but tests the writer concurrency when no caching is done.
            //    // only pick it during NIGHTLY tests, and even then, with very low chances.
            //    return NO_OP_CACHE;
            //}
            else
            {
                // this is slower than CL2O, but less memory consuming, and exercises finding categories on disk too.
                return new LruTaxonomyWriterCache(ndocs / 10);
            }
        }

        [Test]
        public virtual void TestConcurrency()
        {
            AtomicInt32 numDocs = new AtomicInt32(AtLeast(10000));
            Directory indexDir = NewDirectory();
            Directory taxoDir = NewDirectory();
            ConcurrentDictionary<string, string> values = new ConcurrentDictionary<string, string>();
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            var tw = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE, NewTaxoWriterCache(numDocs));
            ThreadJob[] indexThreads = new ThreadJob[AtLeast(4)];
            FacetsConfig config = new FacetsConfig();
            for (int i = 0; i < 10; i++)
            {
                config.SetHierarchical("l1." + i, true);
                config.SetMultiValued("l1." + i, true);
            }

            for (int i = 0; i < indexThreads.Length; i++)
            {
                indexThreads[i] = new ThreadAnonymousClass(this, numDocs, values, iw, tw, config);
            }

            foreach (ThreadJob t in indexThreads)
            {
                t.Start();
            }
            foreach (ThreadJob t in indexThreads)
            {
                t.Join();
            }

            var tr = new DirectoryTaxonomyReader(tw);
            // +1 for root category
            if (values.Count + 1 != tr.Count)
            {
                foreach (string value in values.Keys)
                {
                    FacetLabel label = new FacetLabel(FacetsConfig.StringToPath(value));
                    if (tr.GetOrdinal(label) == -1)
                    {
                        Console.WriteLine("FAIL: path=" + label + " not recognized");
                    }
                }
                fail("mismatch number of categories");
            }
            int[] parents = tr.ParallelTaxonomyArrays.Parents;
            foreach (string cat in values.Keys)
            {
                FacetLabel cp = new FacetLabel(FacetsConfig.StringToPath(cat));
                Assert.IsTrue(tr.GetOrdinal(cp) > 0, "category not found " + cp);
                int level = cp.Length;
                int parentOrd = 0; // for root, parent is always virtual ROOT (ord=0)
                FacetLabel path = null;
                for (int i = 0; i < level; i++)
                {
                    path = cp.Subpath(i + 1);
                    int ord = tr.GetOrdinal(path);
                    Assert.AreEqual(parentOrd, parents[ord], "invalid parent for cp=" + path);
                    parentOrd = ord; // next level should have this parent
                }
            }

            IOUtils.Dispose(tw, iw, tr, taxoDir, indexDir);
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestConcurrentFacetedIndexing outerInstance;

            private readonly AtomicInt32 numDocs;
            private readonly ConcurrentDictionary<string, string> values;
            private readonly IndexWriter iw;
            private readonly DirectoryTaxonomyWriter tw;
            private readonly FacetsConfig config;

            public ThreadAnonymousClass(TestConcurrentFacetedIndexing outerInstance, AtomicInt32 numDocs, ConcurrentDictionary<string, string> values, IndexWriter iw, DirectoryTaxonomyWriter tw, FacetsConfig config)
            {
                this.outerInstance = outerInstance;
                this.numDocs = numDocs;
                this.values = values;
                this.iw = iw;
                this.tw = tw;
                this.config = config;
            }


            public override void Run()
            {
                Random random = Random;
                while (numDocs.DecrementAndGet() > 0)
                {
                    try
                    {
                        Document doc = new Document();
                        int numCats = random.Next(3) + 1; // 1-3
                        while (numCats-- > 0)
                        {
                            FacetField ff = NewCategory();
                            doc.Add(ff);

                            FacetLabel label = new FacetLabel(ff.Dim, ff.Path);
                            // add all prefixes to values
                            int level = label.Length;
                            while (level > 0)
                            {
                                string s = FacetsConfig.PathToString(label.Components, level);
                                values[s] = s;
                                --level;
                            }
                        }
                        iw.AddDocument(config.Build(tw, doc));
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