// Lucene version compatibility level 4.8.1
using J2N.Threading;
using J2N.Threading.Atomic;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Globalization;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
    using Directory = Lucene.Net.Store.Directory;
    using DiskOrdinalMap = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.DiskOrdinalMap;
    using IOrdinalMap = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.IOrdinalMap;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MemoryOrdinalMap = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.MemoryOrdinalMap;
    using TestUtil = Lucene.Net.Util.TestUtil;

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
    public class TestAddTaxonomy : FacetTestCase
    {
        private void Dotest(int ncats, int range)
        {
            AtomicInt32 numCats = new AtomicInt32(ncats);
            Directory[] dirs = new Directory[2];
            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = NewDirectory();
                var tw = new DirectoryTaxonomyWriter(dirs[i]);
                ThreadJob[] addThreads = new ThreadJob[4];
                for (int j = 0; j < addThreads.Length; j++)
                {
                    addThreads[j] = new ThreadAnonymousClass(this, range, numCats, tw);
                }

                foreach (ThreadJob t in addThreads)
                {
                    t.Start();
                }
                foreach (ThreadJob t in addThreads)
                {
                    t.Join();
                }

                tw.Dispose();
            }

            var tw1 = new DirectoryTaxonomyWriter(dirs[0]);
            IOrdinalMap map = randomOrdinalMap();
            tw1.AddTaxonomy(dirs[1], map);
            tw1.Dispose();

            validate(dirs[0], dirs[1], map);

            IOUtils.Dispose(dirs);
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestAddTaxonomy outerInstance;

            private int range;
            private AtomicInt32 numCats;
            private DirectoryTaxonomyWriter tw;

            public ThreadAnonymousClass(TestAddTaxonomy outerInstance, int range, AtomicInt32 numCats, DirectoryTaxonomyWriter tw)
            {
                this.outerInstance = outerInstance;
                this.range = range;
                this.numCats = numCats;
                this.tw = tw;
            }

            public override void Run()
            {
                Random random = Random;
                while (numCats.DecrementAndGet() > 0)
                {
                    string cat = Convert.ToString(random.Next(range), CultureInfo.InvariantCulture);
                    try
                    {
                        tw.AddCategory(new FacetLabel("a", cat));
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        throw RuntimeException.Create(e);
                    }
                }
            }
        }


        private IOrdinalMap randomOrdinalMap()
        {
            if (Random.NextBoolean())
            {
                return new DiskOrdinalMap(CreateTempFile("taxoMap", "").FullName);
            }
            else
            {
                return new MemoryOrdinalMap();
            }
        }

        private void validate(Directory dest, Directory src, IOrdinalMap ordMap)
        {
            using var destTr = new DirectoryTaxonomyReader(dest);
            int destSize = destTr.Count;
            using var srcTR = new DirectoryTaxonomyReader(src);
            var map = ordMap.GetMap();

            // validate taxo sizes
            int srcSize = srcTR.Count;
            Assert.IsTrue(destSize >= srcSize, "destination taxonomy expected to be larger than source; dest=" + destSize + " src=" + srcSize);

            // validate that all source categories exist in destination, and their
            // ordinals are as expected.
            for (int j = 1; j < srcSize; j++)
            {
                FacetLabel cp = srcTR.GetPath(j);
                int destOrdinal = destTr.GetOrdinal(cp);
                Assert.IsTrue(destOrdinal > 0, cp + " not found in destination");
                Assert.AreEqual(destOrdinal, map[j]);
            }
        }

        [Test]
        public virtual void TestAddEmpty()
        {
            Directory dest = NewDirectory();
            var destTW = new DirectoryTaxonomyWriter(dest);
            destTW.AddCategory(new FacetLabel("Author", "Rob Pike"));
            destTW.AddCategory(new FacetLabel("Aardvarks", "Bob"));
            destTW.Commit();

            Directory src = NewDirectory();
            new DirectoryTaxonomyWriter(src).Dispose(); // create an empty taxonomy

            IOrdinalMap map = randomOrdinalMap();
            destTW.AddTaxonomy(src, map);
            destTW.Dispose();

            validate(dest, src, map);

            IOUtils.Dispose(dest, src);
        }

        [Test]
        public virtual void TestAddToEmpty()
        {
            Directory dest = NewDirectory();

            Directory src = NewDirectory();
            DirectoryTaxonomyWriter srcTW = new DirectoryTaxonomyWriter(src);
            srcTW.AddCategory(new FacetLabel("Author", "Rob Pike"));
            srcTW.AddCategory(new FacetLabel("Aardvarks", "Bob"));
            srcTW.Dispose();

            DirectoryTaxonomyWriter destTW = new DirectoryTaxonomyWriter(dest);
            IOrdinalMap map = randomOrdinalMap();
            destTW.AddTaxonomy(src, map);
            destTW.Dispose();

            validate(dest, src, map);

            IOUtils.Dispose(dest, src);
        }

        // A more comprehensive and big random test.
        [Test]
        [Slow]
        public virtual void TestBig()
        {
            Dotest(200, 10000);
            Dotest(1000, 20000);
            Dotest(400000, 1000000);
        }

        // a reasonable random test
        [Test]
        public virtual void TestMedium()
        {
            Random random = Random;
            int numTests = AtLeast(3);
            for (int i = 0; i < numTests; i++)
            {
                Dotest(TestUtil.NextInt32(random, 2, 100),
                    TestUtil.NextInt32(random, 100, 1000));
            }
        }

        [Test]
        public virtual void TestSimple()
        {
            Directory dest = NewDirectory();
            var tw1 = new DirectoryTaxonomyWriter(dest);
            tw1.AddCategory(new FacetLabel("Author", "Mark Twain"));
            tw1.AddCategory(new FacetLabel("Animals", "Dog"));
            tw1.AddCategory(new FacetLabel("Author", "Rob Pike"));

            Directory src = NewDirectory();
            var tw2 = new DirectoryTaxonomyWriter(src);
            tw2.AddCategory(new FacetLabel("Author", "Rob Pike"));
            tw2.AddCategory(new FacetLabel("Aardvarks", "Bob"));
            tw2.Dispose();

            IOrdinalMap map = randomOrdinalMap();

            tw1.AddTaxonomy(src, map);
            tw1.Dispose();

            validate(dest, src, map);

            IOUtils.Dispose(dest, src);
        }

        [Test]
        public virtual void TestConcurrency()
        {
            // tests that addTaxonomy and addCategory work in parallel
            int numCategories = AtLeast(10000);

            // build an input taxonomy index
            Directory src = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(src);
            for (int i = 0; i < numCategories; i++)
            {
                tw.AddCategory(new FacetLabel("a", Convert.ToString(i, CultureInfo.InvariantCulture)));
            }
            tw.Dispose();

            // now add the taxonomy to an empty taxonomy, while adding the categories
            // again, in parallel -- in the end, no duplicate categories should exist.
            Directory dest = NewDirectory();
            var destTw = new DirectoryTaxonomyWriter(dest);
            var t = new ThreadAnonymousClass2(this, numCategories, destTw);
            t.Start();

            IOrdinalMap map = new MemoryOrdinalMap();
            destTw.AddTaxonomy(src, map);
            t.Join();
            destTw.Dispose();

            // now validate

            var dtr = new DirectoryTaxonomyReader(dest);
            // +2 to account for the root category + "a"
            Assert.AreEqual(numCategories + 2, dtr.Count);
            var categories = new JCG.HashSet<FacetLabel>();
            for (int i = 1; i < dtr.Count; i++)
            {
                FacetLabel cat = dtr.GetPath(i);
                Assert.IsTrue(categories.Add(cat), "category " + cat + " already existed");
            }
            dtr.Dispose();

            IOUtils.Dispose(src, dest);
        }

        private sealed class ThreadAnonymousClass2 : ThreadJob
        {
            private readonly TestAddTaxonomy outerInstance;

            private readonly int numCategories;
            private readonly DirectoryTaxonomyWriter destTW;

            public ThreadAnonymousClass2(TestAddTaxonomy outerInstance, int numCategories, DirectoryTaxonomyWriter destTW)
            {
                this.outerInstance = outerInstance;
                this.numCategories = numCategories;
                this.destTW = destTW;
            }

            public override void Run()
            {
                for (int i = 0; i < numCategories; i++)
                {
                    try
                    {
                        destTW.AddCategory(new FacetLabel("a", Convert.ToString(i, CultureInfo.InvariantCulture)));
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        // shouldn't happen - if it does, let the test fail on uncaught exception.
                        throw RuntimeException.Create(e);
                    }
                }
            }
        }
    }
}