// Lucene version compatibility level 4.8.1
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
    using Directory = Lucene.Net.Store.Directory;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LogByteSizeMergePolicy = Lucene.Net.Index.LogByteSizeMergePolicy;
    using LogMergePolicy = Lucene.Net.Index.LogMergePolicy;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;

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

    public class TestDirectoryTaxonomyReader : FacetTestCase
    {

        [Test]
        public virtual void TestCloseAfterIncRef()
        {
            Directory dir = NewDirectory();
            var ltw = new DirectoryTaxonomyWriter(dir);
            ltw.AddCategory(new FacetLabel("a"));
            ltw.Dispose();

            DirectoryTaxonomyReader ltr = new DirectoryTaxonomyReader(dir);
            ltr.IncRef();
            ltr.Dispose();

            // should not fail as we IncRef() before close
            var _ = ltr.Count;
            ltr.DecRef();

            dir.Dispose();
        }

        [Test]
        public virtual void TestCloseTwice()
        {
            Directory dir = NewDirectory();
            var ltw = new DirectoryTaxonomyWriter(dir);
            ltw.AddCategory(new FacetLabel("a"));
            ltw.Dispose();

            var ltr = new DirectoryTaxonomyReader(dir);
            ltr.Dispose();
            ltr.Dispose(); // no exception should be thrown

            dir.Dispose();
        }

        [Test]
        public virtual void TestOpenIfChangedResult()
        {
            Directory dir = null;
            DirectoryTaxonomyWriter ltw = null;
            DirectoryTaxonomyReader ltr = null;

            try
            {
                dir = NewDirectory();
                ltw = new DirectoryTaxonomyWriter(dir);

                ltw.AddCategory(new FacetLabel("a"));
                ltw.Commit();

                ltr = new DirectoryTaxonomyReader(dir);
                Assert.IsNull(TaxonomyReader.OpenIfChanged(ltr), "Nothing has changed");

                ltw.AddCategory(new FacetLabel("b"));
                ltw.Commit();

                DirectoryTaxonomyReader newtr = TaxonomyReader.OpenIfChanged(ltr);
                Assert.IsNotNull(newtr, "changes were committed");
                Assert.IsNull(TaxonomyReader.OpenIfChanged(newtr), "Nothing has changed");
                newtr.Dispose();
            }
            finally
            {
                IOUtils.Dispose(ltw, ltr, dir);
            }
        }

        [Test]
        public virtual void TestAlreadyClosed()
        {
            Directory dir = NewDirectory();
            var ltw = new DirectoryTaxonomyWriter(dir);
            ltw.AddCategory(new FacetLabel("a"));
            ltw.Dispose();

            var ltr = new DirectoryTaxonomyReader(dir);
            ltr.Dispose();
            try
            {
                var _ = ltr.Count;
                fail("An ObjectDisposedException should have been thrown here");
            }
            catch (Exception ace) when (ace.IsAlreadyClosedException())
            {
                // good!
            }
            dir.Dispose();
        }

        /// <summary>
        /// recreating a taxonomy should work well with a freshly opened taxonomy reader 
        /// </summary>
        [Test]
        public virtual void TestFreshReadRecreatedTaxonomy()
        {
            doTestReadRecreatedTaxonomy(Random, true);
        }

        [Test]
        public virtual void TestOpenIfChangedReadRecreatedTaxonomy()
        {
            doTestReadRecreatedTaxonomy(Random, false);
        }

        private void doTestReadRecreatedTaxonomy(Random random, bool closeReader)
        {
            Directory dir = null;
            ITaxonomyWriter tw = null;
            TaxonomyReader tr = null;

            // prepare a few categories
            int n = 10;
            FacetLabel[] cp = new FacetLabel[n];
            for (int i = 0; i < n; i++)
            {
                cp[i] = new FacetLabel("a", Convert.ToString(i, CultureInfo.InvariantCulture));
            }

            try
            {
                dir = NewDirectory();

                tw = new DirectoryTaxonomyWriter(dir);
                tw.AddCategory(new FacetLabel("a"));
                tw.Dispose();

                tr = new DirectoryTaxonomyReader(dir);
                int baseNumCategories = tr.Count;

                for (int i = 0; i < n; i++)
                {
                    int k = random.Next(n);
                    tw = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE);
                    for (int j = 0; j <= k; j++)
                    {
                        tw.AddCategory(cp[j]);
                    }
                    tw.Dispose();
                    if (closeReader)
                    {
                        tr.Dispose();
                        tr = new DirectoryTaxonomyReader(dir);
                    }
                    else
                    {
                        var newtr = TaxonomyReader.OpenIfChanged(tr);
                        Assert.IsNotNull(newtr);
                        tr.Dispose();
                        tr = newtr;
                    }
                    Assert.AreEqual(baseNumCategories + 1 + k, tr.Count, "Wrong #categories in taxonomy (i=" + i + ", k=" + k + ")");
                }
            }
            finally
            {
                IOUtils.Dispose(tr, tw, dir);
            }
        }

        [Test]
        public virtual void TestOpenIfChangedAndRefCount()
        {
            Directory dir = new RAMDirectory(); // no need for random directories here

            var taxoWriter = new DirectoryTaxonomyWriter(dir);
            taxoWriter.AddCategory(new FacetLabel("a"));
            taxoWriter.Commit();

            var taxoReader = new DirectoryTaxonomyReader(dir);
            Assert.AreEqual(1, taxoReader.RefCount, "wrong refCount");

            taxoReader.IncRef();
            Assert.AreEqual(2, taxoReader.RefCount, "wrong refCount");

            taxoWriter.AddCategory(new FacetLabel("a", "b"));
            taxoWriter.Commit();
            var newtr = TaxonomyReader.OpenIfChanged(taxoReader);
            Assert.IsNotNull(newtr);
            taxoReader.Dispose();
            taxoReader = newtr;
            Assert.AreEqual(1, taxoReader.RefCount, "wrong refCount");

            taxoWriter.Dispose();
            taxoReader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestOpenIfChangedManySegments()
        {
            // test openIfChanged() when the taxonomy contains many segments
            Directory dir = NewDirectory();

            // LUCENENET specific: Added DirectoryTaxonomyIndexWriterFactory constructor parameter
            var writer = new DirectoryTaxonomyWriter(new TestDirectoryTaxonomyIndexWriterFactory(), dir);
            var reader = new DirectoryTaxonomyReader(writer);

            int numRounds = Random.Next(10) + 10;
            int numCategories = 1; // one for root
            for (int i = 0; i < numRounds; i++)
            {
                int numCats = Random.Next(4) + 1;
                for (int j = 0; j < numCats; j++)
                {
                    writer.AddCategory(new FacetLabel(Convert.ToString(i, CultureInfo.InvariantCulture), Convert.ToString(j, CultureInfo.InvariantCulture)));
                }
                numCategories += numCats + 1; // one for round-parent
                var newtr = TaxonomyReader.OpenIfChanged(reader);
                Assert.IsNotNull(newtr);
                reader.Dispose();
                reader = newtr;

                // assert categories
                Assert.AreEqual(numCategories, reader.Count);
                int roundOrdinal = reader.GetOrdinal(new FacetLabel(Convert.ToString(i, CultureInfo.InvariantCulture)));
                int[] parents = reader.ParallelTaxonomyArrays.Parents;
                Assert.AreEqual(0, parents[roundOrdinal]); // round's parent is root
                for (int j = 0; j < numCats; j++)
                {
                    int ord = reader.GetOrdinal(new FacetLabel(Convert.ToString(i, CultureInfo.InvariantCulture), Convert.ToString(j, CultureInfo.InvariantCulture)));
                    Assert.AreEqual(roundOrdinal, parents[ord]); // round's parent is root
                }
            }

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // LUCENENET specific: Converted DirectoryTaxonomyWriter anonymous class into a DirectoryTaxonomyIndexWriterFactory subclass
        private sealed class TestDirectoryTaxonomyIndexWriterFactory : DirectoryTaxonomyIndexWriterFactory
        {
            public override IndexWriterConfig CreateIndexWriterConfig(OpenMode openMode)
            {
                IndexWriterConfig conf = base.CreateIndexWriterConfig(openMode);
                LogMergePolicy lmp = (LogMergePolicy)conf.MergePolicy;
                lmp.MergeFactor = 2;
                return conf;
            }
        }

        [Test]
        public virtual void TestOpenIfChangedMergedSegment()
        {
            // test openIfChanged() when all index segments were merged - used to be
            // a bug in ParentArray, caught by testOpenIfChangedManySegments - only
            // this test is not random
            Directory dir = NewDirectory();

            // hold onto IW to forceMerge
            // note how we don't close it, since DTW will close it.
            // LUCENENET: Moved the creation of IndexWriter to TestDirectoryTaxonomyIndexWriterFactory2
            var indexWriterFactory = new TestDirectoryTaxonomyIndexWriterFactory2(Random, TEST_VERSION_CURRENT);
            var writer = new DirectoryTaxonomyWriter(indexWriterFactory, dir);
            Assert.NotNull(indexWriterFactory.iw, "iw should be set via DirectoryTaxonomyIndexWriter constructor calling factory");

            var reader = new DirectoryTaxonomyReader(writer);
            Assert.AreEqual(1, reader.Count);
            Assert.AreEqual(1, reader.ParallelTaxonomyArrays.Parents.Length);

            // add category and call forceMerge -- this should flush IW and merge segments down to 1
            // in ParentArray.initFromReader, this used to fail assuming there are no parents.
            writer.AddCategory(new FacetLabel("1"));
            indexWriterFactory.iw.ForceMerge(1);

            // now calling openIfChanged should trip on the bug
            var newtr = TaxonomyReader.OpenIfChanged(reader);
            Assert.IsNotNull(newtr);
            reader.Dispose();
            reader = newtr;
            Assert.AreEqual(2, reader.Count);
            Assert.AreEqual(2, reader.ParallelTaxonomyArrays.Parents.Length);

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        // LUCENENET specific: Converted DirectoryTaxonomyWriter anonymous class into a DirectoryTaxonomyIndexWriterFactory subclass
        private sealed class TestDirectoryTaxonomyIndexWriterFactory2 : DirectoryTaxonomyIndexWriterFactory
        {
            internal IndexWriter iw = null;
            private readonly Random random;
            private readonly Util.LuceneVersion luceneVersion;

            public TestDirectoryTaxonomyIndexWriterFactory2(
                Random random,
                Util.LuceneVersion luceneVersion)
            {
                this.random = random;
                this.luceneVersion = luceneVersion;
            }

            public override IndexWriterConfig CreateIndexWriterConfig(OpenMode openMode)
            {
                return new IndexWriterConfig(luceneVersion, new MockAnalyzer(random))
                            .SetMergePolicy(new LogByteSizeMergePolicy());
            }

            public override IndexWriter OpenIndexWriter(Directory directory, IndexWriterConfig config)
            {
                iw = base.OpenIndexWriter(directory, config);
                return iw;
            }
        }

        [Test]
        public virtual void TestOpenIfChangedNoChangesButSegmentMerges()
        {
            // test openIfChanged() when the taxonomy hasn't really changed, but segments
            // were merged. The NRT reader will be reopened, and ParentArray used to assert
            // that the new reader contains more ordinals than were given from the old
            // TaxReader version
            Directory dir = NewDirectory();

            // hold onto IW to forceMerge
            // note how we don't close it, since DTW will close it.
            // LUCENENET: Moved the creation of IndexWriter to TestDirectoryTaxonomyIndexWriterFactory2
            var indexWriterFactory = new TestDirectoryTaxonomyIndexWriterFactory2(Random, TEST_VERSION_CURRENT);
            var writer = new DirectoryTaxonomyWriter(indexWriterFactory, dir);
            Assert.NotNull(indexWriterFactory.iw, "iw should be set via DirectoryTaxonomyIndexWriter constructor calling factory");
            
            // add a category so that the following DTR open will cause a flush and 
            // a new segment will be created
            writer.AddCategory(new FacetLabel("a"));

            var reader = new DirectoryTaxonomyReader(writer);
            Assert.AreEqual(2, reader.Count);
            Assert.AreEqual(2, reader.ParallelTaxonomyArrays.Parents.Length);

            // merge all the segments so that NRT reader thinks there's a change 
            indexWriterFactory.iw.ForceMerge(1);

            // now calling openIfChanged should trip on the wrong assert in ParetArray's ctor
            var newtr = TaxonomyReader.OpenIfChanged(reader);
            Assert.IsNotNull(newtr);
            reader.Dispose();
            reader = newtr;
            Assert.AreEqual(2, reader.Count);
            Assert.AreEqual(2, reader.ParallelTaxonomyArrays.Parents.Length);

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestOpenIfChangedReuseAfterRecreate()
        {
            // tests that if the taxonomy is recreated, no data is reused from the previous taxonomy
            Directory dir = NewDirectory();
            DirectoryTaxonomyWriter writer = new DirectoryTaxonomyWriter(dir);
            FacetLabel cp_a = new FacetLabel("a");
            writer.AddCategory(cp_a);
            writer.Dispose();

            DirectoryTaxonomyReader r1 = new DirectoryTaxonomyReader(dir);
            // fill r1's caches
            Assert.AreEqual(1, r1.GetOrdinal(cp_a));
            Assert.AreEqual(cp_a, r1.GetPath(1));

            // now recreate, add a different category
            writer = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE);
            FacetLabel cp_b = new FacetLabel("b");
            writer.AddCategory(cp_b);
            writer.Dispose();

            DirectoryTaxonomyReader r2 = TaxonomyReader.OpenIfChanged(r1);
            Assert.IsNotNull(r2);

            // fill r2's caches
            Assert.AreEqual(1, r2.GetOrdinal(cp_b));
            Assert.AreEqual(cp_b, r2.GetPath(1));

            // check that r1 doesn't see cp_b
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, r1.GetOrdinal(cp_b));
            Assert.AreEqual(cp_a, r1.GetPath(1));

            // check that r2 doesn't see cp_a
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, r2.GetOrdinal(cp_a));
            Assert.AreEqual(cp_b, r2.GetPath(1));

            r2.Dispose();
            r1.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestOpenIfChangedReuse()
        {
            // test the reuse of data from the old DTR instance
            foreach (bool nrt in new bool[] { false, true })
            {
                Directory dir = NewDirectory();
                DirectoryTaxonomyWriter writer = new DirectoryTaxonomyWriter(dir);

                FacetLabel cp_a = new FacetLabel("a");
                writer.AddCategory(cp_a);
                if (!nrt)
                {
                    writer.Commit();
                }

                DirectoryTaxonomyReader r1 = nrt ? new DirectoryTaxonomyReader(writer) : new DirectoryTaxonomyReader(dir);
                // fill r1's caches
                Assert.AreEqual(1, r1.GetOrdinal(cp_a));
                Assert.AreEqual(cp_a, r1.GetPath(1));

                FacetLabel cp_b = new FacetLabel("b");
                writer.AddCategory(cp_b);
                if (!nrt)
                {
                    writer.Commit();
                }

                DirectoryTaxonomyReader r2 = TaxonomyReader.OpenIfChanged(r1);
                Assert.IsNotNull(r2);

                // add r2's categories to the caches
                Assert.AreEqual(2, r2.GetOrdinal(cp_b));
                Assert.AreEqual(cp_b, r2.GetPath(2));

                // check that r1 doesn't see cp_b
                Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, r1.GetOrdinal(cp_b));
                Assert.IsNull(r1.GetPath(2));

                r1.Dispose();
                r2.Dispose();
                writer.Dispose();
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestOpenIfChangedReplaceTaxonomy()
        {
            // test openIfChanged when replaceTaxonomy is called, which is equivalent to recreate
            // only can work with NRT as well
            Directory src = NewDirectory();
            DirectoryTaxonomyWriter w = new DirectoryTaxonomyWriter(src);
            FacetLabel cp_b = new FacetLabel("b");
            w.AddCategory(cp_b);
            w.Dispose();

            foreach (bool nrt in new bool[] { false, true })
            {
                Directory dir = NewDirectory();
                var writer = new DirectoryTaxonomyWriter(dir);

                FacetLabel cp_a = new FacetLabel("a");
                writer.AddCategory(cp_a);
                if (!nrt)
                {
                    writer.Commit();
                }

                DirectoryTaxonomyReader r1 = nrt ? new DirectoryTaxonomyReader(writer) : new DirectoryTaxonomyReader(dir);
                // fill r1's caches
                Assert.AreEqual(1, r1.GetOrdinal(cp_a));
                Assert.AreEqual(cp_a, r1.GetPath(1));

                // now replace taxonomy
                writer.ReplaceTaxonomy(src);
                if (!nrt)
                {
                    writer.Commit();
                }

                DirectoryTaxonomyReader r2 = TaxonomyReader.OpenIfChanged(r1);
                Assert.IsNotNull(r2);

                // fill r2's caches
                Assert.AreEqual(1, r2.GetOrdinal(cp_b));
                Assert.AreEqual(cp_b, r2.GetPath(1));

                // check that r1 doesn't see cp_b
                Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, r1.GetOrdinal(cp_b));
                Assert.AreEqual(cp_a, r1.GetPath(1));

                // check that r2 doesn't see cp_a
                Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, r2.GetOrdinal(cp_a));
                Assert.AreEqual(cp_b, r2.GetPath(1));

                r2.Dispose();
                r1.Dispose();
                writer.Dispose();
                dir.Dispose();
            }

            src.Dispose();
        }

        [Test]
        public virtual void TestGetChildren()
        {
            Directory dir = NewDirectory();
            var taxoWriter = new DirectoryTaxonomyWriter(dir);
            int numCategories = AtLeast(10);
            int numA = 0, numB = 0;
            Random random = Random;
            // add the two categories for which we'll also add children (so asserts are simpler)
            taxoWriter.AddCategory(new FacetLabel("a"));
            taxoWriter.AddCategory(new FacetLabel("b"));
            for (int i = 0; i < numCategories; i++)
            {
                if (random.NextBoolean())
                {
                    taxoWriter.AddCategory(new FacetLabel("a", Convert.ToString(i, CultureInfo.InvariantCulture)));
                    ++numA;
                }
                else
                {
                    taxoWriter.AddCategory(new FacetLabel("b", Convert.ToString(i, CultureInfo.InvariantCulture)));
                    ++numB;
                }
            }
            // add category with no children
            taxoWriter.AddCategory(new FacetLabel("c"));
            taxoWriter.Dispose();

            var taxoReader = new DirectoryTaxonomyReader(dir);

            // non existing category
            TaxonomyReader.ChildrenEnumerator it = taxoReader.GetChildren(taxoReader.GetOrdinal(new FacetLabel("invalid")));

            Assert.AreEqual(false, it.MoveNext());

            // a category with no children
            it = taxoReader.GetChildren(taxoReader.GetOrdinal(new FacetLabel("c")));
            Assert.AreEqual(false, it.MoveNext());

            // arbitrary negative ordinal
            it = taxoReader.GetChildren(-2);
            Assert.AreEqual(false, it.MoveNext());

            // root's children
            var roots = new JCG.HashSet<string> { "a", "b", "c" };
            it = taxoReader.GetChildren(TaxonomyReader.ROOT_ORDINAL);
            while (roots.Count > 0)
            {
                it.MoveNext();
                FacetLabel root = taxoReader.GetPath(it.Current);
                Assert.AreEqual(1, root.Length);
                Assert.IsTrue(roots.Remove(root.Components[0]));
            }
            Assert.AreEqual(false, it.MoveNext());

            for (int i = 0; i < 2; i++)
            {
                FacetLabel cp = i == 0 ? new FacetLabel("a") : new FacetLabel("b");
                int ordinal = taxoReader.GetOrdinal(cp);
                it = taxoReader.GetChildren(ordinal);
                int numChildren = 0;
                int child;
                while (it.MoveNext())
                {
                    child = it.Current;
                    FacetLabel path = taxoReader.GetPath(child);
                    Assert.AreEqual(2, path.Length);
                    Assert.AreEqual(path.Components[0], i == 0 ? "a" : "b");
                    ++numChildren;
                }
                int expected = i == 0 ? numA : numB;
                Assert.AreEqual(expected, numChildren, "invalid num children");
            }
            taxoReader.Dispose();

            dir.Dispose();
        }
    }
}