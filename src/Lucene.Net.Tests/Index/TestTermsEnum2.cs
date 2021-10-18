using J2N.Collections.Generic.Extensions;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
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

    using AutomatonQuery = Lucene.Net.Search.AutomatonQuery;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CheckHits = Lucene.Net.Search.CheckHits;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestTermsEnum2 : LuceneTestCase
    {
        private Directory dir;
        private IndexReader reader;
        private IndexSearcher searcher;
        private JCG.SortedSet<BytesRef> terms; // the terms we put in the index
        private Automaton termsAutomaton; // automata of the same
        internal int numIterations;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // we generate aweful regexps: good for testing.
            // but for preflex codec, the test can be very slow, so use less iterations.
            numIterations = Codec.Default.Name.Equals("Lucene3x", StringComparison.Ordinal) ? 10 * RandomMultiplier : AtLeast(50);
            dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, (IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 50, 1000)));
            Document doc = new Document();
            Field field = NewStringField("field", "", Field.Store.YES);
            doc.Add(field);
            terms = new JCG.SortedSet<BytesRef>();

            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                field.SetStringValue(s);
                terms.Add(new BytesRef(s));
                writer.AddDocument(doc);
            }

            termsAutomaton = BasicAutomata.MakeStringUnion(terms);

            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// tests a pre-intersected automaton against the original </summary>
        [Test]
        public virtual void TestFiniteVersusInfinite()
        {
            for (int i = 0; i < numIterations; i++)
            {
                string reg = AutomatonTestUtil.RandomRegexp(Random);
                Automaton automaton = (new RegExp(reg, RegExpSyntax.NONE)).ToAutomaton();
                IList<BytesRef> matchedTerms = new JCG.List<BytesRef>();
                foreach (BytesRef t in terms)
                {
                    if (BasicOperations.Run(automaton, t.Utf8ToString()))
                    {
                        matchedTerms.Add(t);
                    }
                }

                Automaton alternate = BasicAutomata.MakeStringUnion(matchedTerms);
                //System.out.println("match " + matchedTerms.Size() + " " + alternate.getNumberOfStates() + " states, sigma=" + alternate.getStartPoints().length);
                //AutomatonTestUtil.minimizeSimple(alternate);
                //System.out.println("minmize done");
                AutomatonQuery a1 = new AutomatonQuery(new Term("field", ""), automaton);
                AutomatonQuery a2 = new AutomatonQuery(new Term("field", ""), alternate);
                CheckHits.CheckEqual(a1, searcher.Search(a1, 25).ScoreDocs, searcher.Search(a2, 25).ScoreDocs);
            }
        }

        /// <summary>
        /// seeks to every term accepted by some automata </summary>
        [Test]
        public virtual void TestSeeking()
        {
            for (int i = 0; i < numIterations; i++)
            {
                string reg = AutomatonTestUtil.RandomRegexp(Random);
                Automaton automaton = (new RegExp(reg, RegExpSyntax.NONE)).ToAutomaton();
                TermsEnum te = MultiFields.GetTerms(reader, "field").GetEnumerator();
                IList<BytesRef> unsortedTerms = new JCG.List<BytesRef>(terms);
                unsortedTerms.Shuffle(Random);

                foreach (BytesRef term in unsortedTerms)
                {
                    if (BasicOperations.Run(automaton, term.Utf8ToString()))
                    {
                        // term is accepted
                        if (Random.NextBoolean())
                        {
                            // seek exact
                            Assert.IsTrue(te.SeekExact(term));
                        }
                        else
                        {
                            // seek ceil
                            Assert.AreEqual(SeekStatus.FOUND, te.SeekCeil(term));
                            Assert.AreEqual(term, te.Term);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// mixes up seek and next for all terms </summary>
        [Test]
        public virtual void TestSeekingAndNexting()
        {
            for (int i = 0; i < numIterations; i++)
            {
                TermsEnum te = MultiFields.GetTerms(reader, "field").GetEnumerator();

                foreach (BytesRef term in terms)
                {
                    int c = Random.Next(3);
                    if (c == 0)
                    {
                        Assert.IsTrue(te.MoveNext());
                        Assert.AreEqual(term, te.Term);
                    }
                    else if (c == 1)
                    {
                        Assert.AreEqual(SeekStatus.FOUND, te.SeekCeil(term));
                        Assert.AreEqual(term, te.Term);
                    }
                    else
                    {
                        Assert.IsTrue(te.SeekExact(term));
                    }
                }
            }
        }

        /// <summary>
        /// tests intersect: TODO start at a random term! </summary>
        [Test]
        public virtual void TestIntersect()
        {
            for (int i = 0; i < numIterations; i++)
            {
                string reg = AutomatonTestUtil.RandomRegexp(Random);
                Automaton automaton = (new RegExp(reg, RegExpSyntax.NONE)).ToAutomaton();
                CompiledAutomaton ca = new CompiledAutomaton(automaton, SpecialOperations.IsFinite(automaton), false);
                TermsEnum te = MultiFields.GetTerms(reader, "field").Intersect(ca, null);
                Automaton expected = BasicOperations.Intersection(termsAutomaton, automaton);
                JCG.SortedSet<BytesRef> found = new JCG.SortedSet<BytesRef>();
                while (te.MoveNext())
                {
                    found.Add(BytesRef.DeepCopyOf(te.Term));
                }

                Automaton actual = BasicAutomata.MakeStringUnion(found);
                Assert.IsTrue(BasicOperations.SameLanguage(expected, actual));
            }
        }
    }
}