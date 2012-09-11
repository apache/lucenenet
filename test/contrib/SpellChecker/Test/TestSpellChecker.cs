/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using NUnit.Framework;

using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using English = Lucene.Net.Test.Util.Spell.English;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using Directory = Lucene.Net.Store.Directory;
using LuceneDictionary = SpellChecker.Net.Search.Spell.LuceneDictionary;
using System.Collections;
using Lucene.Net.Store;
using System.Threading;
using SpellChecker.Net.Search.Spell;
using Lucene.Net.Search;

namespace SpellChecker.Net.Test.Search.Spell
{
    /// <summary> 
    /// Test case
    /// </summary>
    /// <author>Nicolas Maisonneuve</author>
    [TestFixture]
    public class TestSpellChecker
    {
        private SpellCheckerMock spellChecker;
        private Directory userindex, spellindex;
        private readonly Random random = new Random();
        public ArrayList searchers;

        [SetUp]
        public virtual void SetUp()
        {
            //create a user index
            userindex = new RAMDirectory();
            var writer = new IndexWriter(userindex, new SimpleAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);

            for (var i = 0; i < 1000; i++)
            {
                var doc = new Document();
                doc.Add(new Field("field1", English.IntToEnglish(i), Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field("field2", English.IntToEnglish(i + 1), Field.Store.YES, Field.Index.ANALYZED)); // + word thousand
                writer.AddDocument(doc);
            }
            writer.Close();

            // create the spellChecker
            spellindex = new RAMDirectory();
            searchers = ArrayList.Synchronized(new ArrayList()); 
            spellChecker = new SpellCheckerMock(spellindex, this);
        }

        [Test]
        public virtual void TestBuild()
        {
            try
            {
                IndexReader r = IndexReader.Open(userindex, true);

                spellChecker.ClearIndex();

                Addwords(r, "field1");
                int num_field1 = this.Numdoc();

                Addwords(r, "field2");
                int num_field2 = this.Numdoc();

                Assert.AreEqual (num_field2, num_field1 + 1);
                
                AssertLastSearcherOpen(4);

                CheckCommonSuggestions(r);
                CheckLevenshteinSuggestions(r);

                spellChecker.setStringDistance(new JaroWinklerDistance());
                spellChecker.SetAccuracy(0.8f);
                CheckCommonSuggestions(r);
                CheckJaroWinklerSuggestions();

                spellChecker.setStringDistance(new NGramDistance(2));
                spellChecker.SetAccuracy(0.5f);
                CheckCommonSuggestions(r);
                CheckNGramSuggestions();
            }
            catch (System.IO.IOException e)
            {
                System.Console.Error.WriteLine(e.StackTrace);
                Assert.Fail();
            }
        }

        private void CheckCommonSuggestions(IndexReader r)
        {
            String[] similar = spellChecker.SuggestSimilar("fvie", 2);
            Assert.True(similar.Length > 0);
            Assert.AreEqual(similar[0], "five");

            similar = spellChecker.SuggestSimilar("five", 2);
            if (similar.Length > 0)
            {
                Assert.False(similar[0].Equals("five")); // don't suggest a word for itself
            }

            similar = spellChecker.SuggestSimilar("fiv", 2);
            Assert.True(similar.Length > 0);
            Assert.AreEqual(similar[0], "five");

            similar = spellChecker.SuggestSimilar("fives", 2);
            Assert.True(similar.Length > 0);
            Assert.AreEqual(similar[0], "five");

            Assert.True(similar.Length > 0);
            similar = spellChecker.SuggestSimilar("fie", 2);
            Assert.AreEqual(similar[0], "five");

            //  test restraint to a field
            similar = spellChecker.SuggestSimilar("tousand", 10, r, "field1", false);
            Assert.AreEqual(0, similar.Length); // there isn't the term thousand in the field field1

            similar = spellChecker.SuggestSimilar("tousand", 10, r, "field2", false);
            Assert.AreEqual(1, similar.Length); // there is the term thousand in the field field2
        }

        private void CheckLevenshteinSuggestions(IndexReader r)
        {
            // test small word
            String[] similar = spellChecker.SuggestSimilar("fvie", 2);
            Assert.AreEqual(1, similar.Length);
            Assert.AreEqual(similar[0], "five");

            similar = spellChecker.SuggestSimilar("five", 2);
            Assert.AreEqual(1, similar.Length);
            Assert.AreEqual(similar[0], "nine");     // don't suggest a word for itself

            similar = spellChecker.SuggestSimilar("fiv", 2);
            Assert.AreEqual(1, similar.Length);
            Assert.AreEqual(similar[0], "five");

            similar = spellChecker.SuggestSimilar("ive", 2);
            Assert.AreEqual(2, similar.Length);
            Assert.AreEqual(similar[0], "five");
            Assert.AreEqual(similar[1], "nine");

            similar = spellChecker.SuggestSimilar("fives", 2);
            Assert.AreEqual(1, similar.Length);
            Assert.AreEqual(similar[0], "five");

            similar = spellChecker.SuggestSimilar("fie", 2);
            Assert.AreEqual(2, similar.Length);
            Assert.AreEqual(similar[0], "five");
            Assert.AreEqual(similar[1], "nine");

            similar = spellChecker.SuggestSimilar("fi", 2);
            Assert.AreEqual(1, similar.Length);
            Assert.AreEqual(similar[0], "five");

            // test restraint to a field
            similar = spellChecker.SuggestSimilar("tousand", 10, r, "field1", false);
            Assert.AreEqual(0, similar.Length); // there isn't the term thousand in the field field1

            similar = spellChecker.SuggestSimilar("tousand", 10, r, "field2", false);
            Assert.AreEqual(1, similar.Length); // there is the term thousand in the field field2

            similar = spellChecker.SuggestSimilar("onety", 2);
            Assert.AreEqual(2, similar.Length);
            Assert.AreEqual(similar[0], "ninety");
            Assert.AreEqual(similar[1], "one");
            try
            {
                spellChecker.SuggestSimilar("tousand", 10, r, null, false);
            }
            catch (NullReferenceException e)
            {
                Assert.True(false, "threw an NPE, and it shouldn't have");
            }
        }

        private void CheckJaroWinklerSuggestions()
        {
            String[] similar = spellChecker.SuggestSimilar("onety", 2);
            Assert.AreEqual(2, similar.Length);
            Assert.AreEqual(similar[0], "one");
            Assert.AreEqual(similar[1], "ninety");
        }

        private void CheckNGramSuggestions()
        {
            String[] similar = spellChecker.SuggestSimilar("onety", 2);
            Assert.AreEqual(2, similar.Length);
            Assert.AreEqual(similar[0], "one");
            Assert.AreEqual(similar[1], "ninety");
        }

        private void Addwords(IndexReader r, System.String field)
        {
            long time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
            spellChecker.IndexDictionary(new LuceneDictionary(r, field));
        }

        private int Numdoc()
        {
            var rs = IndexReader.Open(spellindex, true);
            int num = rs.NumDocs();
            Assert.IsTrue(num != 0);
            
            rs.Close();
            return num;
        }

        [Test]
        public void TestClose()
        {
            var r = IndexReader.Open(userindex, true);
            spellChecker.ClearIndex();
            const string field = "field1";

            Addwords(r, "field1");
            int num_field1 = this.Numdoc();

            Addwords(r, "field2");
            int num_field2 = this.Numdoc();

            Assert.AreEqual(num_field2, num_field1 + 1);

            CheckCommonSuggestions(r);
            AssertLastSearcherOpen(4);
            spellChecker.Close();
            AssertSearchersClosed();

            Assert.Throws<AlreadyClosedException>(() => spellChecker.Close(), "spellchecker was already closed");

            Assert.Throws<AlreadyClosedException>(() => CheckCommonSuggestions(r), "spellchecker was already closed");

            Assert.Throws<AlreadyClosedException>(() => spellChecker.ClearIndex(), "spellchecker was already closed");

            Assert.Throws<AlreadyClosedException>(() => spellChecker.IndexDictionary(new LuceneDictionary(r, field)),
                                                  "spellchecker was already closed");

            Assert.Throws<AlreadyClosedException>(() => spellChecker.SetSpellIndex(spellindex),
                                                  "spellchecker was already closed");

            Assert.AreEqual(4, searchers.Count);
            AssertSearchersClosed();
        }

        /*
         * tests if the internally shared indexsearcher is correctly closed 
         * when the spellchecker is concurrently accessed and closed.
         */
        [Test]
        public void TestConcurrentAccess()
        {
            Assert.AreEqual(1, searchers.Count);
            IndexReader r = IndexReader.Open(userindex, true);
            spellChecker.ClearIndex();
            Assert.AreEqual(2, searchers.Count);
            Addwords(r, "field1");
            Assert.AreEqual(3, searchers.Count);
            int num_field1 = this.Numdoc();
            Addwords(r, "field2");
            Assert.AreEqual(4, searchers.Count);
            int num_field2 = this.Numdoc();
            Assert.AreEqual(num_field2, num_field1 + 1);
            int numThreads = 5 + this.random.Next(5);
            SpellCheckWorker[] workers = new SpellCheckWorker[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                SpellCheckWorker spellCheckWorker = new SpellCheckWorker(r, this);
                spellCheckWorker.start();
                workers[i] = spellCheckWorker;

            }
            int iterations = 5 + random.Next(5);
            for (int i = 0; i < iterations; i++)
            {
                Thread.Sleep(100);
                // concurrently reset the spell index
                spellChecker.SetSpellIndex(this.spellindex);
                // for debug - prints the internal Open searchers 
                // showSearchersOpen();
            }

            spellChecker.Close();
            joinAll(workers, 5000);

            for (int i = 0; i < workers.Length; i++)
            {
                Assert.False(workers[i].failed);
                Assert.True(workers[i].terminated);
            }
            // 4 searchers more than iterations
            // 1. at creation
            // 2. ClearIndex()
            // 2. and 3. during Addwords
            Assert.AreEqual(iterations + 4, searchers.Count);
            AssertSearchersClosed();

        }

        private static void joinAll(SpellCheckWorker[] workers, long timeout)
        {
            for (int j = 0; j < workers.Length; j++)
            {
                long time = (long)DateTime.Now.TimeOfDay.TotalMilliseconds;
                if (timeout < 0)
                {
                    // this could be helpful if it Assert.Fails one day
                    Console.WriteLine("Warning: " + (workers.Length - j)
                        + " threads have not joined but joinall timed out");
                    break;
                }
                workers[j].join(timeout);
                timeout -= (long)DateTime.Now.TimeOfDay.TotalMilliseconds - time;
            }
        }

        private void AssertLastSearcherOpen(int numSearchers)
        {
            Assert.AreEqual(numSearchers, searchers.Count);
            Object[] searcherArray = searchers.ToArray();
            for (int i = 0; i < searcherArray.Length; i++)
            {
                if (i == searcherArray.Length - 1)
                {
                    Assert.True(
                        ((IndexSearcher)searcherArray[i]).IndexReader.RefCount > 0,
                        "expected last searcher Open but was closed");
                }
                else
                {
                    Assert.False(
                        ((IndexSearcher)searcherArray[i]).IndexReader.RefCount > 0,
                        "expected closed searcher but was Open - Index: " + i);
                }
            }
        }

        private void AssertSearchersClosed()
        {
            Object[] searcherArray = searchers.ToArray();
            for (int i = 0; i < searcherArray.Length; i++)
            {
                Assert.AreEqual(0, ((IndexSearcher)searcherArray[i]).IndexReader.RefCount);
            }
        }

        private void ShowSearchersOpen()
        {
            int count = 0;
            Object[] searcherArray = searchers.ToArray();
            for (int i = 0; i < searcherArray.Length; i++)
            {
                if (((IndexSearcher)searcherArray[i]).IndexReader.RefCount > 0)
                    ++count;
            }
            Console.WriteLine(count);
        }


        private class SpellCheckWorker
        {
            private readonly IndexReader reader;
            public bool terminated = false;
            public bool failed = false;
            private Thread m_thread;
            private TestSpellChecker enclosingInstance;

            public SpellCheckWorker(IndexReader reader, TestSpellChecker enclInstance)
                : base()
            {
                this.reader = reader;
                enclosingInstance = enclInstance;
                m_thread = new Thread(run);
            }

            public void run()
            {
                try
                {
                    while (true)
                    {
                        try
                        {
                            enclosingInstance.CheckCommonSuggestions(reader);
                        }
                        catch (AlreadyClosedException e)
                        {

                            return;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.StackTrace);
                            failed = true;
                            return;
                        }
                    }
                }
                finally
                {
                    this.terminated = true;
                }
            }

            public void join(long timeout)
            {
                m_thread.Join((int)timeout);
            }

            public void start()
            {
                m_thread.Start();
            }
        }

        public class SpellCheckerMock : SpellChecker.Net.Search.Spell.SpellChecker
        {
            private readonly TestSpellChecker enclosingInstance;
            private readonly ArrayList searchers = ArrayList.Synchronized(new ArrayList());
            public SpellCheckerMock(Directory spellIndex, TestSpellChecker inst)
                : base(spellIndex)
            {
                enclosingInstance = inst;
                enclosingInstance.searchers = searchers; //Note: this code is invoked after createSearcher
            }

            public SpellCheckerMock(Directory spellIndex, StringDistance sd)
                : base(spellIndex, sd)
            {
            }

            public override IndexSearcher CreateSearcher(Directory dir)
            {
                IndexSearcher searcher = base.CreateSearcher(dir);
                searchers.Add(searcher);
                return searcher;
            }
        }

    }
}