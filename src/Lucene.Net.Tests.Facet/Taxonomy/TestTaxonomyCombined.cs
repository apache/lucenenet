using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Facet.Taxonomy
{


    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using Directory = Lucene.Net.Store.Directory;
    using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;

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
    [SuppressCodecs]
    public class TestTaxonomyCombined : FacetTestCase
    {

        /// <summary>
        ///  The following categories will be added to the taxonomy by
        ///  fillTaxonomy(), and tested by all tests below:
        /// </summary>
        private static readonly string[][] categories = new string[][]
	  {
		  new string[] {"Author", "Tom Clancy"},
		  new string[] {"Author", "Richard Dawkins"},
		  new string[] {"Author", "Richard Adams"},
		  new string[] {"Price", "10", "11"},
		  new string[] {"Price", "10", "12"},
		  new string[] {"Price", "20", "27"},
		  new string[] {"Date", "2006", "05"},
		  new string[] {"Date", "2005"},
		  new string[] {"Date", "2006"},
		  new string[] {"Subject", "Nonfiction", "Children", "Animals"},
		  new string[] {"Author", "Stephen Jay Gould"},
		  new string[] {"Author", "\u05e0\u05d3\u05d1\u3042\u0628"}
	  };

        /// <summary>
        ///  When adding the above categories with ITaxonomyWriter.AddCategory(), 
        ///  the following paths are expected to be returned:
        ///  (note that currently the full path is not returned, and therefore
        ///  not tested - rather, just the last component, the ordinal, is returned
        ///  and tested.
        /// </summary>
        private static readonly int[][] ExpectedPaths =
        {
            new int[] {1, 2},
            new int[] {1, 3},
            new int[] {1, 4},
            new int[] {5, 6, 7},
            new int[] {5, 6, 8},
            new int[] {5, 9, 10},
            new int[] {11, 12, 13},
            new int[] {11, 14},
            new int[] {11, 12},
            new int[] {15, 16, 17, 18},
            new int[] {1, 19},
            new int[] {1, 20}
        };

        /// <summary>
        ///  The taxonomy index is expected to then contain the following
        ///  generated categories, with increasing ordinals (note how parent
        ///  categories are be added automatically when subcategories are added).
        /// </summary>
        private static readonly string[][] ExpectedCategories = new string[][] { new string[] { }, new string[] { "Author" }, new string[] { "Author", "Tom Clancy" }, new string[] { "Author", "Richard Dawkins" }, new string[] { "Author", "Richard Adams" }, new string[] { "Price" }, new string[] { "Price", "10" }, new string[] { "Price", "10", "11" }, new string[] { "Price", "10", "12" }, new string[] { "Price", "20" }, new string[] { "Price", "20", "27" }, new string[] { "Date" }, new string[] { "Date", "2006" }, new string[] { "Date", "2006", "05" }, new string[] { "Date", "2005" }, new string[] { "Subject" }, new string[] { "Subject", "Nonfiction" }, new string[] { "Subject", "Nonfiction", "Children" }, new string[] { "Subject", "Nonfiction", "Children", "Animals" }, new string[] { "Author", "Stephen Jay Gould" }, new string[] { "Author", "\u05e0\u05d3\u05d1\u3042\u0628" } };

        /// <summary>
        ///  fillTaxonomy adds the categories in the categories[] array, and asserts
        ///  that the additions return exactly the ordinals (in the past - paths)
        ///  specified in expectedPaths[].
        ///  Note that this assumes that fillTaxonomy() is called on an empty taxonomy
        ///  index. Calling it after something else was already added to the taxonomy
        ///  index will surely have this method fail.
        /// </summary>

        public static void FillTaxonomy(ITaxonomyWriter tw)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                int ordinal = tw.AddCategory(new FacetLabel(categories[i]));
                int expectedOrdinal = ExpectedPaths[i][ExpectedPaths[i].Length - 1];
                if (ordinal != expectedOrdinal)
                {
                    Fail("For category " + Showcat(categories[i]) + " expected ordinal " + expectedOrdinal + ", but got " + ordinal);
                }
            }
        }

        public static string Showcat(string[] path)
        {
            if (path == null)
            {
                return "<null>";
            }
            if (path.Length == 0)
            {
                return "<empty>";
            }
            if (path.Length == 1 && path[0].Length == 0)
            {
                return "<\"\">";
            }
            StringBuilder sb = new StringBuilder(path[0]);
            for (int i = 1; i < path.Length; i++)
            {
                sb.Append('/');
                sb.Append(path[i]);
            }
            return sb.ToString();
        }

        private string Showcat(FacetLabel path)
        {
            if (path == null)
            {
                return "<null>";
            }
            if (path.Length == 0)
            {
                return "<empty>";
            }
            return "<" + path.ToString() + ">";
        }

        /// <summary>
        ///  Basic tests for ITaxonomyWriter. Basically, we test that
        ///  IndexWriter.AddCategory works, i.e. returns the expected ordinals
        ///  (this is tested by calling the fillTaxonomy() method above).
        ///  We do not test here that after writing the index can be read -
        ///  this will be done in more tests below.
        /// </summary>
        [Test]
        public virtual void TestWriter()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            // Also check ITaxonomyWriter.getSize() - see that the taxonomy's size
            // is what we expect it to be.
            Assert.AreEqual(ExpectedCategories.Length, tw.Count);
            tw.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        ///  testWriterTwice is exactly like testWriter, except that after adding
        ///  all the categories, we add them again, and see that we get the same
        ///  old ids again - not new categories.
        /// </summary>
        [Test]
        public virtual void TestWriterTwice()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            // run fillTaxonomy again - this will try to add the same categories
            // again, and check that we see the same ordinal paths again, not
            // different ones. 
            FillTaxonomy(tw);
            // Let's check the number of categories again, to see that no
            // extraneous categories were created:
            Assert.AreEqual(ExpectedCategories.Length, tw.Count);
            tw.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        ///  testWriterTwice2 is similar to testWriterTwice, except that the index
        ///  is closed and reopened before attempting to write to it the same
        ///  categories again. While testWriterTwice can get along with writing
        ///  and reading correctly just to the cache, testWriterTwice2 checks also
        ///  the actual disk read part of the writer:
        /// </summary>
        [Test]
        public virtual void TestWriterTwice2()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            tw.Dispose();
            tw = new DirectoryTaxonomyWriter(indexDir);
            // run fillTaxonomy again - this will try to add the same categories
            // again, and check that we see the same ordinals again, not different
            // ones, and that the number of categories hasn't grown by the new
            // additions
            FillTaxonomy(tw);
            Assert.AreEqual(ExpectedCategories.Length, tw.Count);
            tw.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        /// testWriterTwice3 is yet another test which tests creating a taxonomy
        /// in two separate writing sessions. This test used to fail because of
        /// a bug involving commit(), explained below, and now should succeed.
        /// </summary>
        [Test]
        public virtual void TestWriterTwice3()
        {
            var indexDir = NewDirectory();
            // First, create and fill the taxonomy
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            tw.Dispose();
            // Now, open the same taxonomy and add the same categories again.
            // After a few categories, the LuceneTaxonomyWriter implementation
            // will stop looking for each category on disk, and rather read them
            // all into memory and close it's reader. The bug was that it closed
            // the reader, but forgot that it did (because it didn't set the reader
            // reference to null).
            tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            // Add one new category, just to make commit() do something:
            tw.AddCategory(new FacetLabel("hi"));
            // Do a commit(). Here was a bug - if tw had a reader open, it should
            // be reopened after the commit. However, in our case the reader should
            // not be open (as explained above) but because it was not set to null,
            // we forgot that, tried to reopen it, and got an AlreadyClosedException.
            tw.Commit();
            Assert.AreEqual(ExpectedCategories.Length + 1, tw.Count);
            tw.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        ///  Another set of tests for the writer, which don't use an array and
        ///  try to distill the different cases, and therefore may be more helpful
        ///  for debugging a problem than testWriter() which is hard to know why
        ///  or where it failed. 
        /// </summary>
        [Test]
        public virtual void TestWriterSimpler()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            Assert.AreEqual(1, tw.Count); // the root only
            // Test that adding a new top-level category works
            Assert.AreEqual(1, tw.AddCategory(new FacetLabel("a")));
            Assert.AreEqual(2, tw.Count);
            // Test that adding the same category again is noticed, and the
            // same ordinal (and not a new one) is returned.
            Assert.AreEqual(1, tw.AddCategory(new FacetLabel("a")));
            Assert.AreEqual(2, tw.Count);
            // Test that adding another top-level category returns a new ordinal,
            // not the same one
            Assert.AreEqual(2, tw.AddCategory(new FacetLabel("b")));
            Assert.AreEqual(3, tw.Count);
            // Test that adding a category inside one of the above adds just one
            // new ordinal:
            Assert.AreEqual(3, tw.AddCategory(new FacetLabel("a", "c")));
            Assert.AreEqual(4, tw.Count);
            // Test that adding the same second-level category doesn't do anything:
            Assert.AreEqual(3, tw.AddCategory(new FacetLabel("a", "c")));
            Assert.AreEqual(4, tw.Count);
            // Test that adding a second-level category with two new components
            // indeed adds two categories
            Assert.AreEqual(5, tw.AddCategory(new FacetLabel("d", "e")));
            Assert.AreEqual(6, tw.Count);
            // Verify that the parents were added above in the order we expected
            Assert.AreEqual(4, tw.AddCategory(new FacetLabel("d")));
            // Similar, but inside a category that already exists:
            Assert.AreEqual(7, tw.AddCategory(new FacetLabel("b", "d", "e")));
            Assert.AreEqual(8, tw.Count);
            // And now inside two levels of categories that already exist:
            Assert.AreEqual(8, tw.AddCategory(new FacetLabel("b", "d", "f")));
            Assert.AreEqual(9, tw.Count);

            tw.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        ///  Test writing an empty index, and seeing that a reader finds in it
        ///  the root category, and only it. We check all the methods on that
        ///  root category return the expected results.
        /// </summary>
        [Test]
        public virtual void TestRootOnly()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            // right after opening the index, it should already contain the
            // root, so have size 1:
            Assert.AreEqual(1, tw.Count);
            tw.Dispose();
            var tr = new DirectoryTaxonomyReader(indexDir);
            Assert.AreEqual(1, tr.Count);
            Assert.AreEqual(0, tr.GetPath(0).Length);
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.ParallelTaxonomyArrays.Parents[0]);
            Assert.AreEqual(0, tr.GetOrdinal(new FacetLabel()));
            tr.Dispose(true);
            indexDir.Dispose();
        }

        /// <summary>
        ///  The following test is exactly the same as testRootOnly, except we
        ///  do not close the writer before opening the reader. We want to see
        ///  that the root is visible to the reader not only after the writer is
        ///  closed, but immediately after it is created.
        /// </summary>
        [Test]
        public virtual void TestRootOnly2()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            tw.Commit();
            var tr = new DirectoryTaxonomyReader(indexDir);
            Assert.AreEqual(1, tr.Count);
            Assert.AreEqual(0, tr.GetPath(0).Length);
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.ParallelTaxonomyArrays.Parents[0]);
            Assert.AreEqual(0, tr.GetOrdinal(new FacetLabel()));
            tw.Dispose();
            tr.Dispose(true);
            indexDir.Dispose();
        }

        /// <summary>
        ///  Basic tests for TaxonomyReader's category <=> ordinal transformations
        ///  (getSize(), getCategory() and getOrdinal()).
        ///  We test that after writing the index, it can be read and all the
        ///  categories and ordinals are there just as we expected them to be.
        /// </summary>
        [Test]
        public virtual void TestReaderBasic()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            tw.Dispose();
            var tr = new DirectoryTaxonomyReader(indexDir);

            // test TaxonomyReader.getSize():
            Assert.AreEqual(ExpectedCategories.Length, tr.Count);

            // test round trips of ordinal => category => ordinal
            for (int i = 0; i < tr.Count; i++)
            {
                Assert.AreEqual(i, tr.GetOrdinal(tr.GetPath(i)));
            }

            // test TaxonomyReader.getCategory():
            for (int i = 1; i < tr.Count; i++)
            {
                FacetLabel expectedCategory = new FacetLabel(ExpectedCategories[i]);
                FacetLabel category = tr.GetPath(i);
                if (!expectedCategory.Equals(category))
                {
                    Fail("For ordinal " + i + " expected category " + Showcat(expectedCategory) + ", but got " + Showcat(category));
                }
            }
            //  (also test invalid ordinals:)
            Assert.Null(tr.GetPath(-1));
            Assert.Null(tr.GetPath(tr.Count));
            Assert.Null(tr.GetPath(TaxonomyReader.INVALID_ORDINAL));

            // test TaxonomyReader.GetOrdinal():
            for (int i = 1; i < ExpectedCategories.Length; i++)
            {
                int expectedOrdinal = i;
                int ordinal = tr.GetOrdinal(new FacetLabel(ExpectedCategories[i]));
                if (expectedOrdinal != ordinal)
                {
                    Fail("For category " + Showcat(ExpectedCategories[i]) + " expected ordinal " + expectedOrdinal + ", but got " + ordinal);
                }
            }
            // (also test invalid categories:)
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.GetOrdinal(new FacetLabel("non-existant")));
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.GetOrdinal(new FacetLabel("Author", "Jules Verne")));

            tr.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        ///  Tests for TaxonomyReader's getParent() method.
        ///  We check it by comparing its results to those we could have gotten by
        ///  looking at the category string paths (where the parentage is obvious).
        ///  Note that after testReaderBasic(), we already know we can trust the
        ///  ordinal <=> category conversions.
        ///  
        ///  Note: At the moment, the parent methods in the reader are deprecated,
        ///  but this does not mean they should not be tested! Until they are
        ///  removed (*if* they are removed), these tests should remain to see
        ///  that they still work correctly.
        /// </summary>

        [Test]
        public virtual void TestReaderParent()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            tw.Dispose();
            var tr = new DirectoryTaxonomyReader(indexDir);

            // check that the parent of the root ordinal is the invalid ordinal:
            int[] parents = tr.ParallelTaxonomyArrays.Parents;
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, parents[0]);

            // check parent of non-root ordinals:
            for (int ordinal = 1; ordinal < tr.Count; ordinal++)
            {
                FacetLabel me = tr.GetPath(ordinal);
                int parentOrdinal = parents[ordinal];
                FacetLabel parent = tr.GetPath(parentOrdinal);
                if (parent == null)
                {
                    Fail("Parent of " + ordinal + " is " + parentOrdinal + ", but this is not a valid category.");
                }
                // verify that the parent is indeed my parent, according to the strings
                if (!me.Subpath(me.Length - 1).Equals(parent))
                {
                    Fail("Got parent " + parentOrdinal + " for ordinal " + ordinal + " but categories are " + Showcat(parent) + " and " + Showcat(me) + " respectively.");
                }
            }

            tr.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        /// Tests for ITaxonomyWriter's getParent() method. We check it by comparing
        /// its results to those we could have gotten by looking at the category
        /// string paths using a TaxonomyReader (where the parentage is obvious).
        /// Note that after testReaderBasic(), we already know we can trust the
        /// ordinal <=> category conversions from TaxonomyReader.
        /// 
        /// The difference between testWriterParent1 and testWriterParent2 is that
        /// the former closes the taxonomy writer before reopening it, while the
        /// latter does not.
        /// 
        /// This test code is virtually identical to that of testReaderParent().
        /// </summary>
        [Test]
        public virtual void TestWriterParent1()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            tw.Dispose();
            tw = new DirectoryTaxonomyWriter(indexDir);
            var tr = new DirectoryTaxonomyReader(indexDir);

            CheckWriterParent(tr, tw);

            tw.Dispose();
            tr.Dispose();
            indexDir.Dispose();
        }

        [Test]
        public virtual void TestWriterParent2()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            tw.Commit();
            var tr = new DirectoryTaxonomyReader(indexDir);

            CheckWriterParent(tr, tw);

            tw.Dispose();
            tr.Dispose();
            indexDir.Dispose();
        }

        private void CheckWriterParent(TaxonomyReader tr, ITaxonomyWriter tw)
        {
            // check that the parent of the root ordinal is the invalid ordinal:
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tw.GetParent(0));

            // check parent of non-root ordinals:
            for (int ordinal = 1; ordinal < tr.Count; ordinal++)
            {
                FacetLabel me = tr.GetPath(ordinal);
                int parentOrdinal = tw.GetParent(ordinal);
                FacetLabel parent = tr.GetPath(parentOrdinal);
                if (parent == null)
                {
                    Fail("Parent of " + ordinal + " is " + parentOrdinal + ", but this is not a valid category.");
                }
                // verify that the parent is indeed my parent, according to the
                // strings
                if (!me.Subpath(me.Length - 1).Equals(parent))
                {
                    Fail("Got parent " + parentOrdinal + " for ordinal " + ordinal + " but categories are " + Showcat(parent) + " and " + Showcat(me) + " respectively.");
                }
            }

            // check parent of of invalid ordinals:
            try
            {
                tw.GetParent(-1);
                Fail("getParent for -1 should throw exception");
            }
            catch (System.IndexOutOfRangeException)
            {
                // ok
            }
            try
            {
                tw.GetParent(TaxonomyReader.INVALID_ORDINAL);
                Fail("getParent for INVALID_ORDINAL should throw exception");
            }
            catch (System.IndexOutOfRangeException)
            {
                // ok
            }
            try
            {
                int parent = tw.GetParent(tr.Count);
                Fail("getParent for getSize() should throw exception, but returned " + parent);
            }
            catch (System.IndexOutOfRangeException)
            {
                // ok
            }
        }

        /// <summary>
        /// Test TaxonomyReader's child browsing method, getChildrenArrays()
        /// This only tests for correctness of the data on one example - we have
        /// below further tests on data refresh etc.
        /// </summary>
        [Test]
        public virtual void TestChildrenArrays()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            tw.Dispose();
            var tr = new DirectoryTaxonomyReader(indexDir);
            ParallelTaxonomyArrays ca = tr.ParallelTaxonomyArrays;
            int[] youngestChildArray = ca.Children;
            Assert.AreEqual(tr.Count, youngestChildArray.Length);
            int[] olderSiblingArray = ca.Siblings;
            Assert.AreEqual(tr.Count, olderSiblingArray.Length);
            for (int i = 0; i < ExpectedCategories.Length; i++)
            {
                // find expected children by looking at all expectedCategories
                // for children
                List<int?> expectedChildren = new List<int?>();
                for (int j = ExpectedCategories.Length - 1; j >= 0; j--)
                {
                    if (ExpectedCategories[j].Length != ExpectedCategories[i].Length + 1)
                    {
                        continue; // not longer by 1, so can't be a child
                    }
                    bool ischild = true;
                    for (int k = 0; k < ExpectedCategories[i].Length; k++)
                    {
                        if (!ExpectedCategories[j][k].Equals(ExpectedCategories[i][k]))
                        {
                            ischild = false;
                            break;
                        }
                    }
                    if (ischild)
                    {
                        expectedChildren.Add(j);
                    }
                }
                // check that children and expectedChildren are the same, with the
                // correct reverse (youngest to oldest) order:
                if (expectedChildren.Count == 0)
                {
                    Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, youngestChildArray[i]);
                }
                else
                {
                    int child = youngestChildArray[i];
                    Assert.AreEqual((int)expectedChildren[0], child);
                    for (int j = 1; j < expectedChildren.Count; j++)
                    {
                        child = olderSiblingArray[child];
                        Assert.AreEqual((int)expectedChildren[j], child);
                        // if child is INVALID_ORDINAL we should stop, but
                        // AssertEquals would fail in this case anyway.
                    }
                    // When we're done comparing, olderSiblingArray should now point
                    // to INVALID_ORDINAL, saying there are no more children. If it
                    // doesn't, we found too many children...
                    Assert.AreEqual(-1, olderSiblingArray[child]);
                }
            }
            tr.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        /// Similar to testChildrenArrays, except rather than look at
        /// expected results, we test for several "invariants" that the results
        /// should uphold, e.g., that a child of a category indeed has this category
        /// as its parent. This sort of test can more easily be extended to larger
        /// example taxonomies, because we do not need to build the expected list
        /// of categories like we did in the above test.
        /// </summary>
        [Test]
        public virtual void TestChildrenArraysInvariants()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            tw.Dispose();
            var tr = new DirectoryTaxonomyReader(indexDir);
            ParallelTaxonomyArrays ca = tr.ParallelTaxonomyArrays;
            int[] children = ca.Children;
            Assert.AreEqual(tr.Count, children.Length);
            int[] olderSiblingArray = ca.Siblings;
            Assert.AreEqual(tr.Count, olderSiblingArray.Length);

            // test that the "youngest child" of every category is indeed a child:
            int[] parents = tr.ParallelTaxonomyArrays.Parents;
            for (int i = 0; i < tr.Count; i++)
            {
                int youngestChild = children[i];
                if (youngestChild != TaxonomyReader.INVALID_ORDINAL)
                {
                    Assert.AreEqual(i, parents[youngestChild]);
                }
            }

            // test that the "older sibling" of every category is indeed older (lower)
            // (it can also be INVALID_ORDINAL, which is lower than any ordinal)
            for (int i = 0; i < tr.Count; i++)
            {
                Assert.True(olderSiblingArray[i] < i, "olderSiblingArray[" + i + "] should be <" + i);
            }

            // test that the "older sibling" of every category is indeed a sibling
            // (they share the same parent)
            for (int i = 0; i < tr.Count; i++)
            {
                int sibling = olderSiblingArray[i];
                if (sibling == TaxonomyReader.INVALID_ORDINAL)
                {
                    continue;
                }
                Assert.AreEqual(parents[i], parents[sibling]);
            }

            // And now for slightly more complex (and less "invariant-like"...)
            // tests:

            // test that the "youngest child" is indeed the youngest (so we don't
            // miss the first children in the chain)
            for (int i = 0; i < tr.Count; i++)
            {
                // Find the really youngest child:
                int j;
                for (j = tr.Count - 1; j > i; j--)
                {
                    if (parents[j] == i)
                    {
                        break; // found youngest child
                    }
                }
                if (j == i) // no child found
                {
                    j = TaxonomyReader.INVALID_ORDINAL;
                }
                Assert.AreEqual(j, children[i]);
            }

            // test that the "older sibling" is indeed the least oldest one - and
            // not a too old one or -1 (so we didn't miss some children in the
            // middle or the end of the chain).
            for (int i = 0; i < tr.Count; i++)
            {
                // Find the youngest older sibling:
                int j;
                for (j = i - 1; j >= 0; j--)
                {
                    if (parents[j] == parents[i])
                    {
                        break; // found youngest older sibling
                    }
                }
                if (j < 0) // no sibling found
                {
                    j = TaxonomyReader.INVALID_ORDINAL;
                }
                Assert.AreEqual(j, olderSiblingArray[i]);
            }

            tr.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        /// Test how getChildrenArrays() deals with the taxonomy's growth:
        /// </summary>
        [Test]
        public virtual void TestChildrenArraysGrowth()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            tw.AddCategory(new FacetLabel("hi", "there"));
            tw.Commit();
            var tr = new DirectoryTaxonomyReader(indexDir);
            ParallelTaxonomyArrays ca = tr.ParallelTaxonomyArrays;
            Assert.AreEqual(3, tr.Count);
            Assert.AreEqual(3, ca.Siblings.Length);
            Assert.AreEqual(3, ca.Children.Length);
            Assert.True(Arrays.Equals(new int[] { 1, 2, -1 }, ca.Children));
            Assert.True(Arrays.Equals(new int[] { -1, -1, -1 }, ca.Siblings));
            tw.AddCategory(new FacetLabel("hi", "ho"));
            tw.AddCategory(new FacetLabel("hello"));
            tw.Commit();
            // Before refresh, nothing changed..
            ParallelTaxonomyArrays newca = tr.ParallelTaxonomyArrays;
            Assert.AreSame(newca, ca); // we got exactly the same object
            Assert.AreEqual(3, tr.Count);
            Assert.AreEqual(3, ca.Siblings.Length);
            Assert.AreEqual(3, ca.Children.Length);
            // After the refresh, things change:
            var newtr = TaxonomyReader.OpenIfChanged(tr);
            Assert.NotNull(newtr);
            tr.Dispose();
            tr = newtr;
            ca = tr.ParallelTaxonomyArrays;
            Assert.AreEqual(5, tr.Count);
            Assert.AreEqual(5, ca.Siblings.Length);
            Assert.AreEqual(5, ca.Children.Length);
            Assert.True(Arrays.Equals(new int[] { 4, 3, -1, -1, -1 }, ca.Children));
            Assert.True(Arrays.Equals(new int[] { -1, -1, -1, 2, 1 }, ca.Siblings));
            tw.Dispose();
            tr.Dispose();
            indexDir.Dispose();
        }

        // Test that getParentArrays is valid when retrieved during refresh
        [Test]
        public virtual void TestTaxonomyReaderRefreshRaces()
        {
            // compute base child arrays - after first chunk, and after the other
            var indexDirBase = NewDirectory();
            var twBase = new DirectoryTaxonomyWriter(indexDirBase);
            twBase.AddCategory(new FacetLabel("a", "0"));
            FacetLabel abPath = new FacetLabel("a", "b");
            twBase.AddCategory(abPath);
            twBase.Commit();
            var trBase = new DirectoryTaxonomyReader(indexDirBase);

            ParallelTaxonomyArrays ca1 = trBase.ParallelTaxonomyArrays;

            int abOrd = trBase.GetOrdinal(abPath);
            int abYoungChildBase1 = ca1.Children[abOrd];

            int numCategories = AtLeast(800);
            for (int i = 0; i < numCategories; i++)
            {
                twBase.AddCategory(new FacetLabel("a", "b", Convert.ToString(i)));
            }
            twBase.Dispose();

            var newTaxoReader = TaxonomyReader.OpenIfChanged(trBase);
            Assert.NotNull(newTaxoReader);
            trBase.Dispose();
            trBase = newTaxoReader;

            ParallelTaxonomyArrays ca2 = trBase.ParallelTaxonomyArrays;
            int abYoungChildBase2 = ca2.Children[abOrd];

            int numRetries = AtLeast(50);
            for (int retry = 0; retry < numRetries; retry++)
            {
                AssertConsistentYoungestChild(abPath, abOrd, abYoungChildBase1, abYoungChildBase2, retry, numCategories);
            }

            trBase.Dispose();
            indexDirBase.Dispose();
        }

        
        private void AssertConsistentYoungestChild(FacetLabel abPath, int abOrd, int abYoungChildBase1, int abYoungChildBase2, int retry, int numCategories)
        {
            var indexDir = new SlowRAMDirectory(-1, null); // no slowness for intialization
            var tw = new DirectoryTaxonomyWriter(indexDir);
            tw.AddCategory(new FacetLabel("a", "0"));
            tw.AddCategory(abPath);
            tw.Commit();

            
            var tr = new DirectoryTaxonomyReader(indexDir);
            for (int i = 0; i < numCategories; i++)
            {
                var cp = new FacetLabel("a", "b", Convert.ToString(i));
                tw.AddCategory(cp);
                Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.GetOrdinal(cp), "Ordinal of " + cp + " must be invalid until Taxonomy Reader was refreshed");
            }
            tw.Dispose();

            
            var stop = new AtomicBoolean(false);
            Exception[] error = new Exception[] { null };
            int[] retrieval = new int[] { 0 };

            var thread = new ThreadAnonymousInnerClassHelper(this, abPath, abOrd, abYoungChildBase1, abYoungChildBase2, retry, tr, stop, error, retrieval);
            thread.Start();

            indexDir.SleepMillis = 1; // some delay for refresh
            var newTaxoReader = TaxonomyReader.OpenIfChanged(tr);
            if (newTaxoReader != null)
            {
                newTaxoReader.Dispose();
            }

            stop.Set(true);
            thread.Join();
            Assert.Null(error[0], "Unexpcted exception at retry " + retry + " retrieval " + retrieval[0] + ": \n" + stackTraceStr(error[0]));

            tr.Dispose();
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestTaxonomyCombined outerInstance;

            private Lucene.Net.Facet.Taxonomy.FacetLabel abPath;
            private int abOrd;
            private int abYoungChildBase1;
            private int abYoungChildBase2;
            private int retry;
            private DirectoryTaxonomyReader tr;
            private AtomicBoolean stop;
            private Exception[] error;
            private int[] retrieval;

            public ThreadAnonymousInnerClassHelper(TestTaxonomyCombined outerInstance, Lucene.Net.Facet.Taxonomy.FacetLabel abPath, int abOrd, int abYoungChildBase1, int abYoungChildBase2, int retry, DirectoryTaxonomyReader tr, AtomicBoolean stop, Exception[] error, int[] retrieval)
                : base("Child Arrays Verifier")
            {
                this.outerInstance = outerInstance;
                this.abPath = abPath;
                this.abOrd = abOrd;
                this.abYoungChildBase1 = abYoungChildBase1;
                this.abYoungChildBase2 = abYoungChildBase2;
                this.retry = retry;
                this.tr = tr;
                this.stop = stop;
                this.error = error;
                this.retrieval = retrieval;
            }

            public override void Run()
            {
#if !NETSTANDARD
                Priority = 1 + Priority;
#endif 
                try
                {
                    while (!stop.Get())
                    {
                        int lastOrd = tr.ParallelTaxonomyArrays.Parents.Length - 1;
                        Assert.NotNull(tr.GetPath(lastOrd), "path of last-ord " + lastOrd + " is not found!");
                        AssertChildrenArrays(tr.ParallelTaxonomyArrays, retry, retrieval[0]++);
                        Thread.Sleep(10);// don't starve refresh()'s CPU, which sleeps every 50 bytes for 1 ms
                    }
                }
                catch (Exception e)
                {
                    error[0] = e;
                    stop.Set(true);
                }
            }

            private void AssertChildrenArrays(ParallelTaxonomyArrays ca, int retry, int retrieval)
            {
                int abYoungChild = ca.Children[abOrd];
                Assert.True(abYoungChildBase1 == abYoungChild || abYoungChildBase2 == ca.Children[abOrd], "Retry " + retry + ": retrieval: " + retrieval + ": wrong youngest child for category " + abPath + " (ord=" + abOrd + ") - must be either " + abYoungChildBase1 + " or " + abYoungChildBase2 + " but was: " + abYoungChild);
            }
        }

        /// <summary>
        /// Grab the stack trace into a string since the exception was thrown in a thread and we want the assert 
        /// outside the thread to show the stack trace in case of failure.   
        /// </summary>
        private string stackTraceStr(Exception error)
        {
            if (error == null)
            {
                return "";
            }

            error.printStackTrace();
            return error.StackTrace;
        }

        /// <summary>
        ///  Test that if separate reader and writer objects are opened, new
        ///  categories written into the writer are available to a reader only
        ///  after a commit().
        ///  Note that this test obviously doesn't cover all the different
        ///  concurrency scenarios, all different methods, and so on. We may
        ///  want to write more tests of this sort.
        /// 
        ///  This test simulates what would happen when there are two separate
        ///  processes, one doing indexing, and the other searching, and each opens
        ///  its own object (with obviously no connection between the objects) using
        ///  the same disk files. Note, though, that this test does not test what
        ///  happens when the two processes do their actual work at exactly the same
        ///  time.
        ///  It also doesn't test multi-threading.
        /// </summary>
        [Test]
        public virtual void TestSeparateReaderAndWriter()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            tw.Commit();
            var tr = new DirectoryTaxonomyReader(indexDir);

            Assert.AreEqual(1, tr.Count); // the empty taxonomy has size 1 (the root)
            tw.AddCategory(new FacetLabel("Author"));
            Assert.AreEqual(1, tr.Count); // still root only...
            Assert.Null(TaxonomyReader.OpenIfChanged(tr)); // this is not enough, because tw.Commit() hasn't been done yet
            Assert.AreEqual(1, tr.Count); // still root only...
            tw.Commit();
            Assert.AreEqual(1, tr.Count); // still root only...
            var newTaxoReader = TaxonomyReader.OpenIfChanged(tr);
            Assert.NotNull(newTaxoReader);
            tr.Dispose();
            tr = newTaxoReader;

            int author = 1;
            try
            {
                Assert.AreEqual(TaxonomyReader.ROOT_ORDINAL, tr.ParallelTaxonomyArrays.Parents[author]);
                // ok
            }
            catch (System.IndexOutOfRangeException)
            {
                Fail("After category addition, commit() and refresh(), getParent for " + author + " should NOT throw exception");
            }
            Assert.AreEqual(2, tr.Count); // finally, see there are two categories

            // now, add another category, and verify that after commit and refresh
            // the parent of this category is correct (this requires the reader
            // to correctly update its prefetched parent vector), and that the
            // old information also wasn't ruined:
            tw.AddCategory(new FacetLabel("Author", "Richard Dawkins"));
            int dawkins = 2;
            tw.Commit();
            newTaxoReader = TaxonomyReader.OpenIfChanged(tr);
            Assert.NotNull(newTaxoReader);
            tr.Dispose();
            tr = newTaxoReader;
            int[] parents = tr.ParallelTaxonomyArrays.Parents;
            Assert.AreEqual(author, parents[dawkins]);
            Assert.AreEqual(TaxonomyReader.ROOT_ORDINAL, parents[author]);
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, parents[TaxonomyReader.ROOT_ORDINAL]);
            Assert.AreEqual(3, tr.Count);
            tw.Dispose();
            tr.Dispose();
            indexDir.Dispose();
        }

        [Test]
        public virtual void TestSeparateReaderAndWriter2()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            tw.Commit();
            var tr = new DirectoryTaxonomyReader(indexDir);

            // Test getOrdinal():
            FacetLabel author = new FacetLabel("Author");

            Assert.AreEqual(1, tr.Count); // the empty taxonomy has size 1 (the root)
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.GetOrdinal(author));
            tw.AddCategory(author);
            // before commit and refresh, no change:
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.GetOrdinal(author));
            Assert.AreEqual(1, tr.Count); // still root only...
            Assert.Null(TaxonomyReader.OpenIfChanged(tr)); // this is not enough, because tw.Commit() hasn't been done yet
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.GetOrdinal(author));
            Assert.AreEqual(1, tr.Count); // still root only...
            tw.Commit();
            // still not enough before refresh:
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tr.GetOrdinal(author));
            Assert.AreEqual(1, tr.Count); // still root only...
            var newTaxoReader = TaxonomyReader.OpenIfChanged(tr);
            Assert.NotNull(newTaxoReader);
            tr.Dispose();
            tr = newTaxoReader;
            Assert.AreEqual(1, tr.GetOrdinal(author));
            Assert.AreEqual(2, tr.Count);
            tw.Dispose();
            tr.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        /// Test what happens if we try to write to a locked taxonomy writer,
        /// and see that we can unlock it and continue.
        /// </summary>
        [Test]
        public virtual void TestWriterLock()
        {
            // native fslock impl gets angry if we use it, so use RAMDirectory explicitly.
            var indexDir = new RAMDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            tw.AddCategory(new FacetLabel("hi", "there"));
            tw.Commit();
            // we deliberately not close the write now, and keep it open and
            // locked.
            // Verify that the writer worked:
            var tr = new DirectoryTaxonomyReader(indexDir);
            Assert.AreEqual(2, tr.GetOrdinal(new FacetLabel("hi", "there")));
            // Try to open a second writer, with the first one locking the directory.
            // We expect to get a LockObtainFailedException.
            try
            {
                Assert.Null(new DirectoryTaxonomyWriter(indexDir));
                Fail("should have failed to write in locked directory");
            }
            catch (LockObtainFailedException)
            {
                // this is what we expect to happen.
            }
            // Remove the lock, and now the open should succeed, and we can
            // write to the new writer.
            DirectoryTaxonomyWriter.Unlock(indexDir);
            var tw2 = new DirectoryTaxonomyWriter(indexDir);
            tw2.AddCategory(new FacetLabel("hey"));
            tw2.Dispose();
            // See that the writer indeed wrote:
            var newtr = TaxonomyReader.OpenIfChanged(tr);
            Assert.NotNull(newtr);
            tr.Dispose();
            tr = newtr;
            Assert.AreEqual(3, tr.GetOrdinal(new FacetLabel("hey")));
            tr.Dispose();
            tw.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        /// fillTaxonomyCheckPaths adds the categories in the categories[] array,
        /// and asserts that the additions return exactly paths specified in
        /// expectedPaths[]. This is the same add fillTaxonomy() but also checks
        /// the correctness of getParent(), not just addCategory().
        /// Note that this assumes that fillTaxonomyCheckPaths() is called on an empty
        /// taxonomy index. Calling it after something else was already added to the
        /// taxonomy index will surely have this method fail.
        /// </summary>
        public static void FillTaxonomyCheckPaths(ITaxonomyWriter tw)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                int ordinal = tw.AddCategory(new FacetLabel(categories[i]));
                int expectedOrdinal = ExpectedPaths[i][ExpectedPaths[i].Length - 1];
                if (ordinal != expectedOrdinal)
                {
                    Fail("For category " + Showcat(categories[i]) + " expected ordinal " + expectedOrdinal + ", but got " + ordinal);
                }
                for (int j = ExpectedPaths[i].Length - 2; j >= 0; j--)
                {
                    ordinal = tw.GetParent(ordinal);
                    expectedOrdinal = ExpectedPaths[i][j];
                    if (ordinal != expectedOrdinal)
                    {
                        Fail("For category " + Showcat(categories[i]) + " expected ancestor level " + (ExpectedPaths[i].Length - 1 - j) + " was " + expectedOrdinal + ", but got " + ordinal);
                    }
                }
            }
        }

        // After fillTaxonomy returned successfully, checkPaths() checks that
        // the getParent() calls return as expected, from the table
        public static void CheckPaths(ITaxonomyWriter tw)
        {
            for (int i = 0; i < categories.Length; i++)
            {
                int ordinal = ExpectedPaths[i][ExpectedPaths[i].Length - 1];
                for (int j = ExpectedPaths[i].Length - 2; j >= 0; j--)
                {
                    ordinal = tw.GetParent(ordinal);
                    int expectedOrdinal = ExpectedPaths[i][j];
                    if (ordinal != expectedOrdinal)
                    {
                        Fail("For category " + Showcat(categories[i]) + " expected ancestor level " + (ExpectedPaths[i].Length - 1 - j) + " was " + expectedOrdinal + ", but got " + ordinal);
                    }
                }
                Assert.AreEqual(TaxonomyReader.ROOT_ORDINAL, tw.GetParent(ExpectedPaths[i][0]));
            }
            Assert.AreEqual(TaxonomyReader.INVALID_ORDINAL, tw.GetParent(TaxonomyReader.ROOT_ORDINAL));
        }

        /// <summary>
        /// Basic test for ITaxonomyWriter.getParent(). This is similar to testWriter
        /// above, except we also check the parents of the added categories, not just
        /// the categories themselves.
        /// </summary>
        [Test]
        public virtual void TestWriterCheckPaths()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomyCheckPaths(tw);
            // Also check ITaxonomyWriter.getSize() - see that the taxonomy's size
            // is what we expect it to be.
            Assert.AreEqual(ExpectedCategories.Length, tw.Count);
            tw.Dispose();
            indexDir.Dispose();
        }

        /// <summary>
        /// testWriterCheckPaths2 is the path-checking variant of testWriterTwice
        /// and testWriterTwice2. After adding all the categories, we add them again,
        /// and see that we get the same old ids and paths. We repeat the path checking
        /// yet again after closing and opening the index for writing again - to see
        /// that the reading of existing data from disk works as well.
        /// </summary>
        [Test]
        public virtual void TestWriterCheckPaths2()
        {
            var indexDir = NewDirectory();
            var tw = new DirectoryTaxonomyWriter(indexDir);
            FillTaxonomy(tw);
            CheckPaths(tw);
            FillTaxonomy(tw);
            CheckPaths(tw);
            tw.Dispose();

            tw = new DirectoryTaxonomyWriter(indexDir);
            CheckPaths(tw);
            FillTaxonomy(tw);
            CheckPaths(tw);
            tw.Dispose();
            indexDir.Dispose();
        }

        [Test]
        public virtual void TestNrt()
        {
            var dir = NewDirectory();
            var writer = new DirectoryTaxonomyWriter(dir);
            var reader = new DirectoryTaxonomyReader(writer);

            FacetLabel cp = new FacetLabel("a");
            writer.AddCategory(cp);
            var newReader = TaxonomyReader.OpenIfChanged(reader);
            Assert.NotNull(newReader, "expected a new instance");
            Assert.AreEqual(2, newReader.Count);
            Assert.AreNotSame(TaxonomyReader.INVALID_ORDINAL, newReader.GetOrdinal(cp));
            reader.Dispose();
            reader = newReader;

            writer.Dispose();
            reader.Dispose();

            dir.Dispose();
        }

        //  TODO (Facet): test multiple readers, one writer. Have the multiple readers
        //  using the same object (simulating threads) or different objects
        //  (simulating processes).
    }

}