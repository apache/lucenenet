using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using JCG = J2N.Collections.Generic;
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
        protected internal IndexSearcher searcher1;
        protected internal IndexSearcher searcher2;
        private IndexReader reader;
        private Directory dir;
        protected internal string fieldName;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            fieldName = Random.NextBoolean() ? "field" : ""; // sometimes use an empty string as field name
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 50, 1000)));
            Document doc = new Document();
            Field field = NewStringField(fieldName, "", Field.Store.NO);
            doc.Add(field);
            JCG.List<string> terms = new JCG.List<string>();
            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                field.SetStringValue(s);
                terms.Add(s);
                writer.AddDocument(doc);
            }

            if (Verbose)
            {
                // utf16 order
                terms.Sort(StringComparer.Ordinal);
                Console.WriteLine("UTF16 order:");
                foreach (string s in terms)
                {
                    Console.WriteLine("  " + UnicodeUtil.ToHexString(s));
                }
            }

            reader = writer.GetReader();
            searcher1 = NewSearcher(reader);
            searcher2 = NewSearcher(reader);
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
        /// a stupid regexp query that just blasts thru the terms </summary>
        private class DumbRegexpQuery : MultiTermQuery
        {
            private readonly Automaton automaton;

            internal DumbRegexpQuery(Term term, RegExpSyntax flags)
                : base(term.Field)
            {
                RegExp re = new RegExp(term.Text, flags);
                automaton = re.ToAutomaton();
            }

            protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
            {
                return new SimpleAutomatonTermsEnum(this, terms.GetEnumerator());
            }

            private sealed class SimpleAutomatonTermsEnum : FilteredTermsEnum
            {
                private readonly TestRegexpRandom2.DumbRegexpQuery outerInstance;

                private CharacterRunAutomaton runAutomaton;
                private readonly CharsRef utf16 = new CharsRef(10);

                internal SimpleAutomatonTermsEnum(TestRegexpRandom2.DumbRegexpQuery outerInstance, TermsEnum tenum)
                    : base(tenum)
                {
                    this.outerInstance = outerInstance;

                    runAutomaton = new CharacterRunAutomaton(outerInstance.automaton);
                    SetInitialSeekTerm(new BytesRef(""));
                }

                protected override AcceptStatus Accept(BytesRef term)
                {
                    UnicodeUtil.UTF8toUTF16(term.Bytes, term.Offset, term.Length, utf16);
                    return runAutomaton.Run(utf16.Chars, 0, utf16.Length) ? AcceptStatus.YES : AcceptStatus.NO;
                }
            }

            public override string ToString(string field)
            {
                return field.ToString() + automaton.ToString();
            }
        }

        /// <summary>
        /// test a bunch of random regular expressions </summary>
        [Test]
        public virtual void TestRegexps()
        {
            // we generate aweful regexps: good for testing.
            // but for preflex codec, the test can be very slow, so use less iterations.
            int num = Codec.Default.Name.Equals("Lucene3x", StringComparison.Ordinal) ? 100 * RandomMultiplier : AtLeast(1000);
            for (int i = 0; i < num; i++)
            {
                string reg = AutomatonTestUtil.RandomRegexp(Random);
                if (Verbose)
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
            RegexpQuery smart = new RegexpQuery(new Term(fieldName, regexp), RegExpSyntax.NONE);
            DumbRegexpQuery dumb = new DumbRegexpQuery(new Term(fieldName, regexp), RegExpSyntax.NONE);

            TopDocs smartDocs = searcher1.Search(smart, 25);
            TopDocs dumbDocs = searcher2.Search(dumb, 25);

            CheckHits.CheckEqual(smart, smartDocs.ScoreDocs, dumbDocs.ScoreDocs);
        }
    }
}