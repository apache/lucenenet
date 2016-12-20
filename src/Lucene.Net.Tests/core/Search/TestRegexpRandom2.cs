using System;
using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
    using CharsRef = Lucene.Net.Util.CharsRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Create an index with random unicode terms
    /// Generates random regexps, and validates against a simple impl.
    /// </summary>
    [TestFixture]
    public class TestRegexpRandom2 : LuceneTestCase
    {
        protected internal IndexSearcher Searcher1;
        protected internal IndexSearcher Searcher2;
        private IndexReader Reader;
        private Directory Dir;
        protected internal string FieldName;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
            FieldName = Random().NextBoolean() ? "field" : ""; // sometimes use an empty string as field name
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000)));
            Document doc = new Document();
            Field field = NewStringField(FieldName, "", Field.Store.NO);
            doc.Add(field);
            List<string> terms = new List<string>();
            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random());
                field.SetStringValue(s);
                terms.Add(s);
                writer.AddDocument(doc);
            }

            if (VERBOSE)
            {
                // utf16 order
                terms.Sort();
                Console.WriteLine("UTF16 order:");
                foreach (string s in terms)
                {
                    Console.WriteLine("  " + UnicodeUtil.ToHexString(s));
                }
            }

            Reader = writer.Reader;
            Searcher1 = NewSearcher(Reader);
            Searcher2 = NewSearcher(Reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// a stupid regexp query that just blasts thru the terms </summary>
        private class DumbRegexpQuery : MultiTermQuery
        {
            private readonly Automaton Automaton;

            internal DumbRegexpQuery(Term term, int flags)
                : base(term.Field)
            {
                RegExp re = new RegExp(term.Text(), flags);
                Automaton = re.ToAutomaton();
            }

            public override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
            {
                return new SimpleAutomatonTermsEnum(this, terms.Iterator(null));
            }

            private sealed class SimpleAutomatonTermsEnum : FilteredTermsEnum
            {
                private bool InstanceFieldsInitialized = false;

                private void InitializeInstanceFields()
                {
                    RunAutomaton = new CharacterRunAutomaton(OuterInstance.Automaton);
                }

                private readonly TestRegexpRandom2.DumbRegexpQuery OuterInstance;

                internal CharacterRunAutomaton RunAutomaton;
                internal CharsRef Utf16 = new CharsRef(10);

                internal SimpleAutomatonTermsEnum(TestRegexpRandom2.DumbRegexpQuery outerInstance, TermsEnum tenum)
                    : base(tenum)
                {
                    this.OuterInstance = outerInstance;

                    if (!InstanceFieldsInitialized)
                    {
                        InitializeInstanceFields();
                        InstanceFieldsInitialized = true;
                    }
                    InitialSeekTerm = new BytesRef("");
                }

                protected override AcceptStatus Accept(BytesRef term)
                {
                    UnicodeUtil.UTF8toUTF16(term.Bytes, term.Offset, term.Length, Utf16);
                    return RunAutomaton.Run(Utf16.Chars, 0, Utf16.Length) ? AcceptStatus.YES : AcceptStatus.NO;
                }
            }

            public override string ToString(string field)
            {
                return field.ToString() + Automaton.ToString();
            }
        }

        /// <summary>
        /// test a bunch of random regular expressions </summary>
        [Test]
        public virtual void TestRegexps()
        {
            // we generate aweful regexps: good for testing.
            // but for preflex codec, the test can be very slow, so use less iterations.
            int num = Codec.Default.Name.Equals("Lucene3x") ? 100 * RANDOM_MULTIPLIER : AtLeast(1000);
            for (int i = 0; i < num; i++)
            {
                string reg = AutomatonTestUtil.RandomRegexp(Random());
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: regexp=" + reg);
                }
                AssertSame(reg);
            }
        }

        /// <summary>
        /// check that the # of hits is the same as from a very
        /// simple regexpquery implementation.
        /// </summary>
        protected internal virtual void AssertSame(string regexp)
        {
            RegexpQuery smart = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);
            DumbRegexpQuery dumb = new DumbRegexpQuery(new Term(FieldName, regexp), RegExp.NONE);

            TopDocs smartDocs = Searcher1.Search(smart, 25);
            TopDocs dumbDocs = Searcher2.Search(dumb, 25);

            CheckHits.CheckEqual(smart, smartDocs.ScoreDocs, dumbDocs.ScoreDocs);
        }
    }
}