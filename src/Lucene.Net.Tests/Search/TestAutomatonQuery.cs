﻿using J2N.Threading;
using Lucene.Net.Documents;
using NUnit.Framework;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search
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

    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SingleTermsEnum = Lucene.Net.Index.SingleTermsEnum;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestAutomatonQuery : LuceneTestCase
    {
        private Directory directory;
        private IndexReader reader;
        private IndexSearcher searcher;

        private readonly string FN = "field";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            Document doc = new Document();
            Field titleField = NewTextField("title", "some title", Field.Store.NO);
            Field field = NewTextField(FN, "this is document one 2345", Field.Store.NO);
            Field footerField = NewTextField("footer", "a footer", Field.Store.NO);
            doc.Add(titleField);
            doc.Add(field);
            doc.Add(footerField);
            writer.AddDocument(doc);
            field.SetStringValue("some text from doc two a short piece 5678.91");
            writer.AddDocument(doc);
            field.SetStringValue("doc three has some different stuff" + " with numbers 1234 5678.9 and letter b");
            writer.AddDocument(doc);
            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        private Term NewTerm(string value)
        {
            return new Term(FN, value);
        }

        private int AutomatonQueryNrHits(AutomatonQuery query)
        {
            if (Verbose)
            {
                Console.WriteLine("TEST: run aq=" + query);
            }
            return searcher.Search(query, 5).TotalHits;
        }

        private void AssertAutomatonHits(int expected, Automaton automaton)
        {
            AutomatonQuery query = new AutomatonQuery(NewTerm("bogus"), automaton);

            query.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
            Assert.AreEqual(expected, AutomatonQueryNrHits(query));

            query.MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
            Assert.AreEqual(expected, AutomatonQueryNrHits(query));

            query.MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;
            Assert.AreEqual(expected, AutomatonQueryNrHits(query));

            query.MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;
            Assert.AreEqual(expected, AutomatonQueryNrHits(query));
        }

        /// <summary>
        /// Test some very simple automata.
        /// </summary>
        [Test]
        public virtual void TestBasicAutomata()
        {
            AssertAutomatonHits(0, BasicAutomata.MakeEmpty());
            AssertAutomatonHits(0, BasicAutomata.MakeEmptyString());
            AssertAutomatonHits(2, BasicAutomata.MakeAnyChar());
            AssertAutomatonHits(3, BasicAutomata.MakeAnyString());
            AssertAutomatonHits(2, BasicAutomata.MakeString("doc"));
            AssertAutomatonHits(1, BasicAutomata.MakeChar('a'));
            AssertAutomatonHits(2, BasicAutomata.MakeCharRange('a', 'b'));
            AssertAutomatonHits(2, BasicAutomata.MakeInterval(1233, 2346, 0));
            AssertAutomatonHits(1, BasicAutomata.MakeInterval(0, 2000, 0));
            AssertAutomatonHits(2, BasicOperations.Union(BasicAutomata.MakeChar('a'), BasicAutomata.MakeChar('b')));
            AssertAutomatonHits(0, BasicOperations.Intersection(BasicAutomata.MakeChar('a'), BasicAutomata.MakeChar('b')));
            AssertAutomatonHits(1, BasicOperations.Minus(BasicAutomata.MakeCharRange('a', 'b'), BasicAutomata.MakeChar('a')));
        }

        /// <summary>
        /// Test that a nondeterministic automaton works correctly. (It should will be
        /// determinized)
        /// </summary>
        [Test]
        public virtual void TestNFA()
        {
            // accept this or three, the union is an NFA (two transitions for 't' from
            // initial state)
            Automaton nfa = BasicOperations.Union(BasicAutomata.MakeString("this"), BasicAutomata.MakeString("three"));
            AssertAutomatonHits(2, nfa);
        }

        [Test]
        public virtual void TestEquals()
        {
            AutomatonQuery a1 = new AutomatonQuery(NewTerm("foobar"), BasicAutomata.MakeString("foobar"));
            // reference to a1
            AutomatonQuery a2 = a1;
            // same as a1 (accepts the same language, same term)
            AutomatonQuery a3 = new AutomatonQuery(NewTerm("foobar"), BasicOperations.Concatenate(BasicAutomata.MakeString("foo"), BasicAutomata.MakeString("bar")));
            // different than a1 (same term, but different language)
            AutomatonQuery a4 = new AutomatonQuery(NewTerm("foobar"), BasicAutomata.MakeString("different"));
            // different than a1 (different term, same language)
            AutomatonQuery a5 = new AutomatonQuery(NewTerm("blah"), BasicAutomata.MakeString("foobar"));

            Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
            Assert.AreEqual(a1, a2);

            Assert.AreEqual(a1.GetHashCode(), a3.GetHashCode());
            Assert.AreEqual(a1, a3);

            // different class
            AutomatonQuery w1 = new WildcardQuery(NewTerm("foobar"));
            // different class
            AutomatonQuery w2 = new RegexpQuery(NewTerm("foobar"));

            Assert.IsFalse(a1.Equals(w1));
            Assert.IsFalse(a1.Equals(w2));
            Assert.IsFalse(w1.Equals(w2));
            Assert.IsFalse(a1.Equals(a4));
            Assert.IsFalse(a1.Equals(a5));
            Assert.IsFalse(a1.Equals(null));
        }

        /// <summary>
        /// Test that rewriting to a single term works as expected, preserves
        /// MultiTermQuery semantics.
        /// </summary>
        [Test]
        public virtual void TestRewriteSingleTerm()
        {
            AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), BasicAutomata.MakeString("piece"));
            Terms terms = MultiFields.GetTerms(searcher.IndexReader, FN);
            Assert.IsTrue(aq.GetTermsEnum(terms) is SingleTermsEnum);
            Assert.AreEqual(1, AutomatonQueryNrHits(aq));
        }

        /// <summary>
        /// Test that rewriting to a prefix query works as expected, preserves
        /// MultiTermQuery semantics.
        /// </summary>
        [Test]
        public virtual void TestRewritePrefix()
        {
            Automaton pfx = BasicAutomata.MakeString("do");
            pfx.ExpandSingleton(); // expand singleton representation for testing
            Automaton prefixAutomaton = BasicOperations.Concatenate(pfx, BasicAutomata.MakeAnyString());
            AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), prefixAutomaton);
            Terms terms = MultiFields.GetTerms(searcher.IndexReader, FN);

            var en = aq.GetTermsEnum(terms);
            Assert.IsTrue(en is PrefixTermsEnum, "Expected type PrefixTermEnum but was {0}", en.GetType().Name);
            Assert.AreEqual(3, AutomatonQueryNrHits(aq));
        }

        /// <summary>
        /// Test handling of the empty language
        /// </summary>
        [Test]
        public virtual void TestEmptyOptimization()
        {
            AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), BasicAutomata.MakeEmpty());
            // not yet available: Assert.IsTrue(aq.getEnum(searcher.getIndexReader())
            // instanceof EmptyTermEnum);
            Terms terms = MultiFields.GetTerms(searcher.IndexReader, FN);
            Assert.AreSame(TermsEnum.EMPTY, aq.GetTermsEnum(terms));
            Assert.AreEqual(0, AutomatonQueryNrHits(aq));
        }

        [Test]
        public virtual void TestHashCodeWithThreads()
        {
            AutomatonQuery[] queries = new AutomatonQuery[1000];
            for (int i = 0; i < queries.Length; i++)
            {
                queries[i] = new AutomatonQuery(new Term("bogus", "bogus"), AutomatonTestUtil.RandomAutomaton(Random));
            }
            CountdownEvent startingGun = new CountdownEvent(1);
            int numThreads = TestUtil.NextInt32(Random, 2, 5);
            ThreadJob[] threads = new ThreadJob[numThreads];
            for (int threadID = 0; threadID < numThreads; threadID++)
            {
                ThreadJob thread = new ThreadAnonymousClass(this, queries, startingGun);
                threads[threadID] = thread;
                thread.Start();
            }
            startingGun.Signal();
            foreach (ThreadJob thread in threads)
            {
                thread.Join();
            }
        }

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly TestAutomatonQuery outerInstance;

            private readonly AutomatonQuery[] queries;
            private readonly CountdownEvent startingGun;

            public ThreadAnonymousClass(TestAutomatonQuery outerInstance, AutomatonQuery[] queries, CountdownEvent startingGun)
            {
                this.outerInstance = outerInstance;
                this.queries = queries;
                this.startingGun = startingGun;
            }

            public override void Run()
            {
                startingGun.Wait();
                for (int i = 0; i < queries.Length; i++)
                {
                    queries[i].GetHashCode();
                }
                // LUCENENET: The Rethrow.rethrow() method only is for tricking the Java compiler into letting it throw a "checked" exception.
                // In .NET, it is better just not to catch than deal with such nonsense.
            }
        }
    }
}