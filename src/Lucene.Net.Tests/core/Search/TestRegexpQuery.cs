using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using IAutomatonProvider = Lucene.Net.Util.Automaton.IAutomatonProvider;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Some simple regex tests, mostly converted from contrib's TestRegexQuery.
    /// </summary>
    [TestFixture]
    public class TestRegexpQuery : LuceneTestCase
    {
        private IndexSearcher Searcher;
        private IndexReader Reader;
        private Directory Directory;
        private readonly string FN = "field";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Directory, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewTextField(FN, "the quick brown fox jumps over the lazy ??? dog 493432 49344", Field.Store.NO));
            writer.AddDocument(doc);
            Reader = writer.Reader;
            writer.Dispose();
            Searcher = NewSearcher(Reader);
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Directory.Dispose();
            base.TearDown();
        }

        private Term NewTerm(string value)
        {
            return new Term(FN, value);
        }

        private int RegexQueryNrHits(string regex)
        {
            RegexpQuery query = new RegexpQuery(NewTerm(regex));
            return Searcher.Search(query, 5).TotalHits;
        }

        [Test]
        public virtual void TestRegex1()
        {
            Assert.AreEqual(1, RegexQueryNrHits("q.[aeiou]c.*"));
        }

        [Test]
        public virtual void TestRegex2()
        {
            Assert.AreEqual(0, RegexQueryNrHits(".[aeiou]c.*"));
        }

        [Test]
        public virtual void TestRegex3()
        {
            Assert.AreEqual(0, RegexQueryNrHits("q.[aeiou]c"));
        }

        [Test]
        public virtual void TestNumericRange()
        {
            Assert.AreEqual(1, RegexQueryNrHits("<420000-600000>"));
            Assert.AreEqual(0, RegexQueryNrHits("<493433-600000>"));
        }

        [Test]
        public virtual void TestRegexComplement()
        {
            Assert.AreEqual(1, RegexQueryNrHits("4934~[3]"));
            // not the empty lang, i.e. match all docs
            Assert.AreEqual(1, RegexQueryNrHits("~#"));
        }

        [Test]
        public virtual void TestCustomProvider()
        {
            IAutomatonProvider myProvider = new AutomatonProviderAnonymousInnerClassHelper(this);
            RegexpQuery query = new RegexpQuery(NewTerm("<quickBrown>"), RegExp.ALL, myProvider);
            Assert.AreEqual(1, Searcher.Search(query, 5).TotalHits);
        }

        private class AutomatonProviderAnonymousInnerClassHelper : IAutomatonProvider
        {
            private readonly TestRegexpQuery OuterInstance;

            public AutomatonProviderAnonymousInnerClassHelper(TestRegexpQuery outerInstance)
            {
                this.OuterInstance = outerInstance;
                quickBrownAutomaton = BasicOperations.Union(Arrays.AsList(BasicAutomata.MakeString("quick"), BasicAutomata.MakeString("brown"), BasicAutomata.MakeString("bob")));
            }

            // automaton that matches quick or brown
            private Automaton quickBrownAutomaton;

            public Automaton GetAutomaton(string name)
            {
                if (name.Equals("quickBrown"))
                {
                    return quickBrownAutomaton;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Test a corner case for backtracking: In this case the term dictionary has
        /// 493432 followed by 49344. When backtracking from 49343... to 4934, its
        /// necessary to test that 4934 itself is ok before trying to append more
        /// characters.
        /// </summary>
        [Test]
        public virtual void TestBacktracking()
        {
            Assert.AreEqual(1, RegexQueryNrHits("4934[314]"));
        }
    }
}