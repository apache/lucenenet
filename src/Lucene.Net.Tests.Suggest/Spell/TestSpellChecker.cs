using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Lucene.Net.Search.Spell
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

    /// <summary>
    /// Spell checker test case
    /// </summary>
    public class TestSpellChecker : LuceneTestCase
    {
        private SpellCheckerMock spellChecker;
        private Directory userindex, spellindex;
        internal static ConcurrentBag<IndexSearcher> searchers;


        public override void SetUp()
        {
            base.SetUp();

            //create a user index
            userindex = NewDirectory();
            IndexWriter writer = new IndexWriter(userindex, new IndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            for (int i = 0; i < 1000; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField("field1", English.IntToEnglish(i), Field.Store.YES));
                doc.Add(NewTextField("field2", English.IntToEnglish(i + 1), Field.Store.YES)); // + word thousand
                doc.Add(NewTextField("field3", "fvei" + (i % 2 == 0 ? " five" : ""), Field.Store.YES)); // + word thousand
                writer.AddDocument(doc);
            }
            {
                Document doc = new Document();
                doc.Add(NewTextField("field1", "eight", Field.Store.YES)); // "eight" in
                                                                           // the index
                                                                           // twice
                writer.AddDocument(doc);
            }
            {
                Document doc = new Document();
                doc
                    .Add(NewTextField("field1", "twenty-one twenty-one", Field.Store.YES)); // "twenty-one" in the index thrice
                writer.AddDocument(doc);
            }
            {
                Document doc = new Document();
                doc.Add(NewTextField("field1", "twenty", Field.Store.YES)); // "twenty"
                                                                            // in the
                                                                            // index
                                                                            // twice
                writer.AddDocument(doc);
            }

            writer.Dispose();
            searchers = new ConcurrentBag<IndexSearcher>();
            // create the spellChecker
            spellindex = NewDirectory();
            spellChecker = new SpellCheckerMock(spellindex);
        }

        public override void TearDown()
        {
            userindex.Dispose();
            if (!spellChecker.IsDisposed)
                spellChecker.Dispose();
            spellindex.Dispose();
            base.TearDown();
        }

        [Test]
        public void TestBuild()
        {
            using (IndexReader r = DirectoryReader.Open(userindex))
            {
                spellChecker.ClearIndex();

                Addwords(r, spellChecker, "field1");
                int num_field1 = this.NumDoc();

                Addwords(r, spellChecker, "field2");
                int num_field2 = this.NumDoc();

                assertEquals(num_field2, num_field1 + 1);

                AssertLastSearcherOpen(4);

                CheckCommonSuggestions(r);
                CheckLevenshteinSuggestions(r);

                spellChecker.StringDistance = (new JaroWinklerDistance());
                spellChecker.Accuracy = (0.8f);
                CheckCommonSuggestions(r);
                CheckJaroWinklerSuggestions();
                // the accuracy is set to 0.8 by default, but the best result has a score of 0.925
                string[] similar = spellChecker.SuggestSimilar("fvie", 2, 0.93f);
                assertTrue(similar.Length == 0);
                similar = spellChecker.SuggestSimilar("fvie", 2, 0.92f);
                assertTrue(similar.Length == 1);

                similar = spellChecker.SuggestSimilar("fiv", 2);
                assertTrue(similar.Length > 0);
                assertEquals(similar[0], "five");

                spellChecker.StringDistance = (new NGramDistance(2));
                spellChecker.Accuracy = (0.5f);
                CheckCommonSuggestions(r);
                CheckNGramSuggestions();

            }
        }

        [Test]
        public void TestComparator()
        {
            using (Directory compIdx = NewDirectory())
            {
                SpellChecker compareSP = new SpellCheckerMock(compIdx, new LevensteinDistance(), new SuggestWordFrequencyComparator());
                try
                {
                    using (IndexReader r = DirectoryReader.Open(userindex))
                    {

                        Addwords(r, compareSP, "field3");

                        string[] similar = compareSP.SuggestSimilar("fvie", 2, r, "field3",
                            SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
                        assertTrue(similar.Length == 2);
                        //five and fvei have the same score, but different frequencies.
                        assertEquals("fvei", similar[0]);
                        assertEquals("five", similar[1]);
                    }
                }
                finally
                {
                    if (!compareSP.IsDisposed)
                        compareSP.Dispose();
                }
            }
        }

        [Test]
        public void TestBogusField()
        {
            using (Directory compIdx = NewDirectory())
            {
                SpellChecker compareSP = new SpellCheckerMock(compIdx, new LevensteinDistance(), new SuggestWordFrequencyComparator());
                try
                {
                    using (IndexReader r = DirectoryReader.Open(userindex))
                    {

                        Addwords(r, compareSP, "field3");

                        string[] similar = compareSP.SuggestSimilar("fvie", 2, r,
                            "bogusFieldBogusField", SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
                        assertEquals(0, similar.Length);
                    }
                }
                finally
                {
                    if (!compareSP.IsDisposed)
                        compareSP.Dispose();
                }
            }
        }

        [Test]
        public void TestSuggestModes()
        {
            using (IndexReader r = DirectoryReader.Open(userindex))
            {
                spellChecker.ClearIndex();
                Addwords(r, spellChecker, "field1");


                {
                    string[] similar = spellChecker.SuggestSimilar("eighty", 2, r, "field1",
                        SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
                    assertEquals(1, similar.Length);
                    assertEquals("eighty", similar[0]);
                }


                {
                    string[] similar = spellChecker.SuggestSimilar("eight", 2, r, "field1",
                        SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
                    assertEquals(1, similar.Length);
                    assertEquals("eight", similar[0]);
                }


                {
                    string[] similar = spellChecker.SuggestSimilar("eighty", 5, r, "field1",
                        SuggestMode.SUGGEST_MORE_POPULAR);
                    assertEquals(5, similar.Length);
                    assertEquals("eight", similar[0]);
                }


                {
                    string[] similar = spellChecker.SuggestSimilar("twenty", 5, r, "field1",
                        SuggestMode.SUGGEST_MORE_POPULAR);
                    assertEquals(1, similar.Length);
                    assertEquals("twenty-one", similar[0]);
                }


                {
                    string[] similar = spellChecker.SuggestSimilar("eight", 5, r, "field1",
                        SuggestMode.SUGGEST_MORE_POPULAR);
                    assertEquals(0, similar.Length);
                }


                {
                    string[] similar = spellChecker.SuggestSimilar("eighty", 5, r, "field1",
                        SuggestMode.SUGGEST_ALWAYS);
                    assertEquals(5, similar.Length);
                    assertEquals("eight", similar[0]);
                }


                {
                    string[] similar = spellChecker.SuggestSimilar("eight", 5, r, "field1",
                        SuggestMode.SUGGEST_ALWAYS);
                    assertEquals(5, similar.Length);
                    assertEquals("eighty", similar[0]);
                }
            }
        }
        private void CheckCommonSuggestions(IndexReader r)
        {
            string[]
            similar = spellChecker.SuggestSimilar("fvie", 2);
            assertTrue(similar.Length > 0);
            assertEquals(similar[0], "five");

            similar = spellChecker.SuggestSimilar("five", 2);
            if (similar.Length > 0)
            {
                assertFalse(similar[0].equals("five")); // don't suggest a word for itself
            }

            similar = spellChecker.SuggestSimilar("fiv", 2);
            assertTrue(similar.Length > 0);
            assertEquals(similar[0], "five");

            similar = spellChecker.SuggestSimilar("fives", 2);
            assertTrue(similar.Length > 0);
            assertEquals(similar[0], "five");

            assertTrue(similar.Length > 0);
            similar = spellChecker.SuggestSimilar("fie", 2);
            assertEquals(similar[0], "five");

            //  test restraint to a field
            similar = spellChecker.SuggestSimilar("tousand", 10, r, "field1",
                SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertEquals(0, similar.Length); // there isn't the term thousand in the field field1

            similar = spellChecker.SuggestSimilar("tousand", 10, r, "field2",
                SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertEquals(1, similar.Length); // there is the term thousand in the field field2
        }

        private void CheckLevenshteinSuggestions(IndexReader r)
        {
            // test small word
            string[]
            similar = spellChecker.SuggestSimilar("fvie", 2);
            assertEquals(1, similar.Length);
            assertEquals(similar[0], "five");

            similar = spellChecker.SuggestSimilar("five", 2);
            assertEquals(1, similar.Length);
            assertEquals(similar[0], "nine");     // don't suggest a word for itself

            similar = spellChecker.SuggestSimilar("fiv", 2);
            assertEquals(1, similar.Length);
            assertEquals(similar[0], "five");

            similar = spellChecker.SuggestSimilar("ive", 2);
            assertEquals(2, similar.Length);
            assertEquals(similar[0], "five");
            assertEquals(similar[1], "nine");

            similar = spellChecker.SuggestSimilar("fives", 2);
            assertEquals(1, similar.Length);
            assertEquals(similar[0], "five");

            similar = spellChecker.SuggestSimilar("fie", 2);
            assertEquals(2, similar.Length);
            assertEquals(similar[0], "five");
            assertEquals(similar[1], "nine");

            similar = spellChecker.SuggestSimilar("fi", 2);
            assertEquals(1, similar.Length);
            assertEquals(similar[0], "five");

            // test restraint to a field
            similar = spellChecker.SuggestSimilar("tousand", 10, r, "field1",
                SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertEquals(0, similar.Length); // there isn't the term thousand in the field field1

            similar = spellChecker.SuggestSimilar("tousand", 10, r, "field2",
                SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            assertEquals(1, similar.Length); // there is the term thousand in the field field2

            similar = spellChecker.SuggestSimilar("onety", 2);
            assertEquals(2, similar.Length);
            assertEquals(similar[0], "ninety");
            assertEquals(similar[1], "one");
            try
            {
                similar = spellChecker.SuggestSimilar("tousand", 10, r, null,
                    SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
            }
            catch (NullReferenceException /*e*/)
            {
                assertTrue("threw an NPE, and it shouldn't have", false);
            }
        }

        private void CheckJaroWinklerSuggestions()
        {
            string[]
            similar = spellChecker.SuggestSimilar("onety", 2);
            assertEquals(2, similar.Length);
            assertEquals(similar[0], "one");
            assertEquals(similar[1], "ninety");
        }

        private void CheckNGramSuggestions()
        {
            string[]
            similar = spellChecker.SuggestSimilar("onety", 2);
            assertEquals(2, similar.Length);
            assertEquals(similar[0], "one");
            assertEquals(similar[1], "ninety");
        }

        private void Addwords(IndexReader r, SpellChecker sc, string field)
        {
            long time = Environment.TickCount;
            sc.IndexDictionary(new LuceneDictionary(r, field), NewIndexWriterConfig(TEST_VERSION_CURRENT, null), false);
            time = Environment.TickCount - time;
            //System.out.println("time to build " + field + ": " + time);
        }

        private int NumDoc()
        {
            IndexReader rs = DirectoryReader.Open(spellindex);
            int num = rs.NumDocs;
            assertTrue(num != 0);
            //System.out.println("num docs: " + num);
            rs.Dispose();
            return num;
        }

        [Test]
        public void TestClose()
        {
            using (IndexReader r = DirectoryReader.Open(userindex))
            {
                spellChecker.ClearIndex();
                string field = "field1";
                Addwords(r, spellChecker, "field1");
                int num_field1 = this.NumDoc();
                Addwords(r, spellChecker, "field2");
                int num_field2 = this.NumDoc();
                assertEquals(num_field2, num_field1 + 1);
                CheckCommonSuggestions(r);
                AssertLastSearcherOpen(4);
                spellChecker.Dispose();
                AssertSearchersClosed();
                // LUCENENET NOTE: Per MSDN, calling Dispose() multiple times
                // should be a safe operation. http://stackoverflow.com/a/5306896/181087
                // Certainly, there shouldn't be a problem with calling Dispose() within
                // a using block if you decide to free up resources early.
                //try
                //{
                //    spellChecker.Dispose();
                //    fail("spellchecker was already closed");
                //}
                //catch (AlreadyClosedException e)
                //{
                //    // expected
                //}
                try
                {
                    CheckCommonSuggestions(r);
                    fail("spellchecker was already closed");
                }
                catch (AlreadyClosedException /*e*/)
                {
                    // expected
                }

                try
                {
                    spellChecker.ClearIndex();
                    fail("spellchecker was already closed");
                }
                catch (AlreadyClosedException /*e*/)
                {
                    // expected
                }

                try
                {
                    spellChecker.IndexDictionary(new LuceneDictionary(r, field), NewIndexWriterConfig(TEST_VERSION_CURRENT, null), false);
                    fail("spellchecker was already closed");
                }
                catch (AlreadyClosedException /*e*/)
                {
                    // expected
                }

                try
                {
                    spellChecker.SpellIndex = (spellindex);
                    fail("spellchecker was already closed");
                }
                catch (AlreadyClosedException /*e*/)
                {
                    // expected
                }
                assertEquals(4, searchers.Count);
                AssertSearchersClosed();
            }
        }

        /*
         * tests if the internally shared indexsearcher is correctly closed 
         * when the spellchecker is concurrently accessed and closed.
         */
        [Test]
        public void TestConcurrentAccess()
        {
            assertEquals(1, searchers.Count);
            using (IndexReader r = DirectoryReader.Open(userindex))
            {
                spellChecker.ClearIndex();
                assertEquals(2, searchers.Count);
                Addwords(r, spellChecker, "field1");
                assertEquals(3, searchers.Count);
                int num_field1 = this.NumDoc();
                Addwords(r, spellChecker, "field2");
                assertEquals(4, searchers.Count);
                int num_field2 = this.NumDoc();
                assertEquals(num_field2, num_field1 + 1);
                int numThreads = 5 + Random().nextInt(5);
                SpellCheckWorker[] workers = new SpellCheckWorker[numThreads];
                var stop = new AtomicBoolean(false);
                for (int i = 0; i < numThreads; i++)
                {
                    SpellCheckWorker spellCheckWorker = new SpellCheckWorker(this, r, stop);
                    workers[i] = spellCheckWorker;
                    spellCheckWorker.Start();
                }
                int iterations = 5 + Random().nextInt(5);
                for (int i = 0; i < iterations; i++)
                {
                    Thread.Sleep(100);
                    // concurrently reset the spell index
                    spellChecker.SpellIndex = (this.spellindex);
                    // for debug - prints the internal open searchers 
                    // showSearchersOpen();
                }

                spellChecker.Dispose();
                stop.Set(true);

                // wait for 60 seconds - usually this is very fast but coverage runs could take quite long
                //executor.awaitTermination(60L, TimeUnit.SECONDS);
                foreach (SpellCheckWorker worker in workers)
                {
                    worker.Join((long)TimeSpan.FromSeconds(60).TotalMilliseconds);
                }

                for (int i = 0; i < workers.Length; i++)
                {
                    assertFalse(string.Format(CultureInfo.InvariantCulture, "worker thread {0} failed \n" + workers[i].Error, i), workers[i].Error != null);
                    assertTrue(string.Format(CultureInfo.InvariantCulture, "worker thread {0} is still running but should be terminated", i), workers[i].terminated);
                }
                // 4 searchers more than iterations
                // 1. at creation
                // 2. clearIndex()
                // 2. and 3. during addwords
                assertEquals(iterations + 4, searchers.Count);
                AssertSearchersClosed();
            }
        }

        private void AssertLastSearcherOpen(int numSearchers)
        {
            assertEquals(numSearchers, searchers.Count);

            // LUCENENET NOTE: The ConcurrentBag.Add() method adds each item to the
            // beginning of the list, so we end up with a reverse order array.
            // We can correct that here, since this is the only part of the
            // test that cares about the order.
            IndexSearcher[] searcherArray = searchers.Reverse().ToArray();

            for (int i = 0; i < searcherArray.Length; i++)
            {
                if (i == searcherArray.Length - 1)
                {
                    assertTrue("expected last searcher open but was closed",
                        searcherArray[i].IndexReader.RefCount > 0);
                }
                else
                {
                    assertFalse("expected closed searcher but was open - Index: " + i,
                        searcherArray[i].IndexReader.RefCount > 0);
                }
            }
        }

        private void AssertSearchersClosed()
        {
            foreach (IndexSearcher searcher in searchers)
            {
                assertEquals(0, searcher.IndexReader.RefCount);
            }
        }

        // For debug
        //  private void showSearchersOpen() {
        //    int count = 0;
        //    for (IndexSearcher searcher : searchers) {
        //      if(searcher.getIndexReader().getRefCount() > 0)
        //        ++count;
        //    } 
        //    System.out.println(count);
        //  }

        private class SpellCheckWorker : ThreadClass
        {
            private readonly TestSpellChecker outerInstance;

            private readonly IndexReader reader;
            private readonly AtomicBoolean stop;
            private volatile Exception error;
            internal volatile bool terminated = false;

            public SpellCheckWorker(TestSpellChecker outerInstance, IndexReader reader, AtomicBoolean stop)
            {
                this.outerInstance = outerInstance;
                this.reader = reader;
                this.stop = stop;
            }

            public Exception Error
            {
                get { return error; }
            }

            public override void Run()
            {
#if !NETSTANDARD
                Priority += 1;
#endif
                try
                {
                    while (!stop.Get())
                    {
                        try
                        {
                            outerInstance.CheckCommonSuggestions(reader);

                            Thread.Sleep(10);// don't starve refresh()'s CPU, which sleeps every 50 bytes for 1 ms
                        }
                        catch (AlreadyClosedException /*e*/)
                        {
                            return;
                        }
                        catch (Exception e)
                        {
                            e.printStackTrace();
                            error = e;
                            return;
                        }
                    }
                }
                finally
                {
                    terminated = true;
                }
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

        internal class SpellCheckerMock : SpellChecker
        {
            public SpellCheckerMock(Directory spellIndex)
                : base(spellIndex)
            {
            }

            public SpellCheckerMock(Directory spellIndex, IStringDistance sd)
                    : base(spellIndex, sd)
            {
            }

            public SpellCheckerMock(Directory spellIndex, IStringDistance sd, IComparer<SuggestWord> comparator)
                    : base(spellIndex, sd, comparator)
            {
            }

            internal override IndexSearcher CreateSearcher(Directory dir)
            {
                IndexSearcher searcher = base.CreateSearcher(dir);
                TestSpellChecker.searchers.Add(searcher);
                return searcher;
            }
        }
    }
}
