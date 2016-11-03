using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using IntField = IntField;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
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
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
    public class TestTermsEnum : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            Random random = new Random(Random().Next());
            LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues());
            Directory d = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(Random());
            analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
            RandomIndexWriter w = new RandomIndexWriter(Random(), d, analyzer, Similarity, TimeZone);
            int numDocs = AtLeast(10);
            for (int docCount = 0; docCount < numDocs; docCount++)
            {
                w.AddDocument(docs.NextDoc());
            }
            IndexReader r = w.Reader;
            w.Dispose();

            List<BytesRef> terms = new List<BytesRef>();
            TermsEnum termsEnum = MultiFields.GetTerms(r, "body").Iterator(null);
            BytesRef term;
            while ((term = termsEnum.Next()) != null)
            {
                terms.Add(BytesRef.DeepCopyOf(term));
            }
            if (VERBOSE)
            {
                Console.WriteLine("TEST: " + terms.Count + " terms");
            }

            int upto = -1;
            int iters = AtLeast(200);
            for (int iter = 0; iter < iters; iter++)
            {
                bool isEnd;
                if (upto != -1 && Random().NextBoolean())
                {
                    // next
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: iter next");
                    }
                    isEnd = termsEnum.Next() == null;
                    upto++;
                    if (isEnd)
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("  end");
                        }
                        Assert.AreEqual(upto, terms.Count);
                        upto = -1;
                    }
                    else
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("  got term=" + termsEnum.Term().Utf8ToString() + " expected=" + terms[upto].Utf8ToString());
                        }
                        Assert.IsTrue(upto < terms.Count);
                        Assert.AreEqual(terms[upto], termsEnum.Term());
                    }
                }
                else
                {
                    BytesRef target;
                    string exists;
                    if (Random().NextBoolean())
                    {
                        // likely fake term
                        if (Random().NextBoolean())
                        {
                            target = new BytesRef(TestUtil.RandomSimpleString(Random()));
                        }
                        else
                        {
                            target = new BytesRef(TestUtil.RandomRealisticUnicodeString(Random()));
                        }
                        exists = "likely not";
                    }
                    else
                    {
                        // real term
                        target = terms[Random().Next(terms.Count)];
                        exists = "yes";
                    }

                    upto = terms.BinarySearch(target);

                    if (Random().NextBoolean())
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: iter seekCeil target=" + target.Utf8ToString() + " exists=" + exists);
                        }
                        // seekCeil
                        TermsEnum.SeekStatus status = termsEnum.SeekCeil(target);
                        if (VERBOSE)
                        {
                            Console.WriteLine("  got " + status);
                        }

                        if (upto < 0)
                        {
                            upto = -(upto + 1);
                            if (upto >= terms.Count)
                            {
                                Assert.AreEqual(TermsEnum.SeekStatus.END, status);
                                upto = -1;
                            }
                            else
                            {
                                Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, status);
                                Assert.AreEqual(terms[upto], termsEnum.Term());
                            }
                        }
                        else
                        {
                            Assert.AreEqual(TermsEnum.SeekStatus.FOUND, status);
                            Assert.AreEqual(terms[upto], termsEnum.Term());
                        }
                    }
                    else
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: iter seekExact target=" + target.Utf8ToString() + " exists=" + exists);
                        }
                        // seekExact
                        bool result = termsEnum.SeekExact(target);
                        if (VERBOSE)
                        {
                            Console.WriteLine("  got " + result);
                        }
                        if (upto < 0)
                        {
                            Assert.IsFalse(result);
                            upto = -1;
                        }
                        else
                        {
                            Assert.IsTrue(result);
                            Assert.AreEqual(target, termsEnum.Term());
                        }
                    }
                }
            }

            r.Dispose();
            d.Dispose();
            docs.Dispose();
        }

        private void AddDoc(RandomIndexWriter w, ICollection<string> terms, IDictionary<BytesRef, int?> termToID, int id)
        {
            Document doc = new Document();
            doc.Add(new IntField("id", id, Field.Store.NO));
            if (VERBOSE)
            {
                Console.WriteLine("TEST: addDoc id:" + id + " terms=" + terms);
            }
            foreach (string s2 in terms)
            {
                doc.Add(NewStringField("f", s2, Field.Store.NO));
                termToID[new BytesRef(s2)] = id;
            }
            w.AddDocument(doc);
            terms.Clear();
        }

        private bool Accepts(CompiledAutomaton c, BytesRef b)
        {
            int state = c.RunAutomaton.InitialState;
            for (int idx = 0; idx < b.Length; idx++)
            {
                Assert.IsTrue(state != -1);
                state = c.RunAutomaton.Step(state, b.Bytes[b.Offset + idx] & 0xff);
            }
            return c.RunAutomaton.IsAccept(state);
        }

        // Tests Terms.intersect
        [Test, LongRunningTest, MaxTime(int.MaxValue)]
        public virtual void TestIntersectRandom()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            int numTerms = AtLeast(300);
            //final int numTerms = 50;

            HashSet<string> terms = new HashSet<string>();
            ICollection<string> pendingTerms = new List<string>();
            IDictionary<BytesRef, int?> termToID = new Dictionary<BytesRef, int?>();
            int id = 0;
            while (terms.Count != numTerms)
            {
                string s = RandomString;
                if (!terms.Contains(s))
                {
                    terms.Add(s);
                    pendingTerms.Add(s);
                    if (Random().Next(20) == 7)
                    {
                        AddDoc(w, pendingTerms, termToID, id++);
                    }
                }
            }
            AddDoc(w, pendingTerms, termToID, id++);

            BytesRef[] termsArray = new BytesRef[terms.Count];
            HashSet<BytesRef> termsSet = new HashSet<BytesRef>();
            {
                int upto = 0;
                foreach (string s in terms)
                {
                    BytesRef b = new BytesRef(s);
                    termsArray[upto++] = b;
                    termsSet.Add(b);
                }
                Array.Sort(termsArray);
            }

            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: indexed terms (unicode order):");
                foreach (BytesRef t in termsArray)
                {
                    Console.WriteLine("  " + t.Utf8ToString() + " -> id:" + termToID[t]);
                }
            }

            IndexReader r = w.Reader;
            w.Dispose();

            // NOTE: intentional insanity!!
            FieldCache.Ints docIDToID = FieldCache.DEFAULT.GetInts(SlowCompositeReaderWrapper.Wrap(r), "id", false);

            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                // TODO: can we also test infinite As here...?

                // From the random terms, pick some ratio and compile an
                // automaton:
                HashSet<string> acceptTerms = new HashSet<string>();
                SortedSet<BytesRef> sortedAcceptTerms = new SortedSet<BytesRef>();
                double keepPct = Random().NextDouble();
                Automaton a;
                if (iter == 0)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: empty automaton");
                    }
                    a = BasicAutomata.MakeEmpty();
                }
                else
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: keepPct=" + keepPct);
                    }
                    foreach (string s in terms)
                    {
                        string s2;
                        if (Random().NextDouble() <= keepPct)
                        {
                            s2 = s;
                        }
                        else
                        {
                            s2 = RandomString;
                        }
                        acceptTerms.Add(s2);
                        sortedAcceptTerms.Add(new BytesRef(s2));
                    }
                    a = BasicAutomata.MakeStringUnion(sortedAcceptTerms);
                }

                if (Random().NextBoolean())
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: reduce the automaton");
                    }
                    a.Reduce();
                }

                CompiledAutomaton c = new CompiledAutomaton(a, true, false);

                BytesRef[] acceptTermsArray = new BytesRef[acceptTerms.Count];
                HashSet<BytesRef> acceptTermsSet = new HashSet<BytesRef>();
                int upto = 0;
                foreach (string s in acceptTerms)
                {
                    BytesRef b = new BytesRef(s);
                    acceptTermsArray[upto++] = b;
                    acceptTermsSet.Add(b);
                    Assert.IsTrue(Accepts(c, b));
                }
                Array.Sort(acceptTermsArray);

                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: accept terms (unicode order):");
                    foreach (BytesRef t in acceptTermsArray)
                    {
                        Console.WriteLine("  " + t.Utf8ToString() + (termsSet.Contains(t) ? " (exists)" : ""));
                    }
                    Console.WriteLine(a.ToDot());
                }

                for (int iter2 = 0; iter2 < 100; iter2++)
                {
                    BytesRef startTerm = acceptTermsArray.Length == 0 || Random().NextBoolean() ? null : acceptTermsArray[Random().Next(acceptTermsArray.Length)];

                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: iter2=" + iter2 + " startTerm=" + (startTerm == null ? "<null>" : startTerm.Utf8ToString()));

                        if (startTerm != null)
                        {
                            int state = c.RunAutomaton.InitialState;
                            for (int idx = 0; idx < startTerm.Length; idx++)
                            {
                                int label = startTerm.Bytes[startTerm.Offset + idx] & 0xff;
                                Console.WriteLine("  state=" + state + " label=" + label);
                                state = c.RunAutomaton.Step(state, label);
                                Assert.IsTrue(state != -1);
                            }
                            Console.WriteLine("  state=" + state);
                        }
                    }

                    TermsEnum te = MultiFields.GetTerms(r, "f").Intersect(c, startTerm);

                    int loc;
                    if (startTerm == null)
                    {
                        loc = 0;
                    }
                    else
                    {
                        loc = Array.BinarySearch(termsArray, BytesRef.DeepCopyOf(startTerm));
                        if (loc < 0)
                        {
                            loc = -(loc + 1);
                        }
                        else
                        {
                            // startTerm exists in index
                            loc++;
                        }
                    }
                    while (loc < termsArray.Length && !acceptTermsSet.Contains(termsArray[loc]))
                    {
                        loc++;
                    }

                    DocsEnum docsEnum = null;
                    while (loc < termsArray.Length)
                    {
                        BytesRef expected = termsArray[loc];
                        BytesRef actual = te.Next();
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST:   next() expected=" + expected.Utf8ToString() + " actual=" + (actual == null ? "null" : actual.Utf8ToString()));
                        }
                        Assert.AreEqual(expected, actual);
                        Assert.AreEqual(1, te.DocFreq());
                        docsEnum = TestUtil.Docs(Random(), te, null, docsEnum, DocsEnum.FLAG_NONE);
                        int docID = docsEnum.NextDoc();
                        Assert.IsTrue(docID != DocIdSetIterator.NO_MORE_DOCS);
                        Assert.AreEqual(docIDToID.Get(docID), (int)termToID[expected]);
                        do
                        {
                            loc++;
                        } while (loc < termsArray.Length && !acceptTermsSet.Contains(termsArray[loc]));
                    }
                    Assert.IsNull(te.Next());
                }
            }

            r.Dispose();
            dir.Dispose();
        }

        private readonly string FIELD = "field";

        private IndexReader MakeIndex(Directory d, params string[] terms)
        {
            var iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));

            /*
            iwc.SetCodec(new StandardCodec(minTermsInBlock, maxTermsInBlock));
            */

            using (var w = new RandomIndexWriter(Random(), d, iwc))
            {
                foreach (string term in terms)
                {
                    var doc = new Document();
                    var f = NewStringField(FIELD, term, Field.Store.NO);
                    doc.Add(f);
                    w.AddDocument(doc);
                }

                return w.Reader;
            }
        }

        private int DocFreq(IndexReader r, string term)
        {
            return r.DocFreq(new Term(FIELD, term));
        }

        [Test]
        public virtual void TestEasy()
        {
            // No floor arcs:
            using (var d = NewDirectory())
            using (var r = MakeIndex(d, "aa0", "aa1", "aa2", "aa3", "bb0", "bb1", "bb2", "bb3", "aa"))
            {
                // First term in block:
                Assert.AreEqual(1, DocFreq(r, "aa0"));

                // Scan forward to another term in same block
                Assert.AreEqual(1, DocFreq(r, "aa2"));

                Assert.AreEqual(1, DocFreq(r, "aa"));

                // Reset same block then scan forwards
                Assert.AreEqual(1, DocFreq(r, "aa1"));

                // Not found, in same block
                Assert.AreEqual(0, DocFreq(r, "aa5"));

                // Found, in same block
                Assert.AreEqual(1, DocFreq(r, "aa2"));

                // Not found in index:
                Assert.AreEqual(0, DocFreq(r, "b0"));

                // Found:
                Assert.AreEqual(1, DocFreq(r, "aa2"));

                // Found, rewind:
                Assert.AreEqual(1, DocFreq(r, "aa0"));

                // First term in block:
                Assert.AreEqual(1, DocFreq(r, "bb0"));

                // Scan forward to another term in same block
                Assert.AreEqual(1, DocFreq(r, "bb2"));

                // Reset same block then scan forwards
                Assert.AreEqual(1, DocFreq(r, "bb1"));

                // Not found, in same block
                Assert.AreEqual(0, DocFreq(r, "bb5"));

                // Found, in same block
                Assert.AreEqual(1, DocFreq(r, "bb2"));

                // Not found in index:
                Assert.AreEqual(0, DocFreq(r, "b0"));

                // Found:
                Assert.AreEqual(1, DocFreq(r, "bb2"));

                // Found, rewind:
                Assert.AreEqual(1, DocFreq(r, "bb0"));
            }
        }

        // tests:
        //   - test same prefix has non-floor block and floor block (ie, has 2 long outputs on same term prefix)
        //   - term that's entirely in the index

        [Test]
        public virtual void TestFloorBlocks()
        {
            var terms = new[] { "aa0", "aa1", "aa2", "aa3", "aa4", "aa5", "aa6", "aa7", "aa8", "aa9", "aa", "xx" };

            using (var d = NewDirectory())
            using (var r = MakeIndex(d, terms))
            {
                // First term in first block:
                Assert.AreEqual(1, DocFreq(r, "aa0"));
                Assert.AreEqual(1, DocFreq(r, "aa4"));

                // No block
                Assert.AreEqual(0, DocFreq(r, "bb0"));

                // Second block
                Assert.AreEqual(1, DocFreq(r, "aa4"));

                // Backwards to prior floor block:
                Assert.AreEqual(1, DocFreq(r, "aa0"));

                // Forwards to last floor block:
                Assert.AreEqual(1, DocFreq(r, "aa9"));

                Assert.AreEqual(0, DocFreq(r, "a"));
                Assert.AreEqual(1, DocFreq(r, "aa"));
                Assert.AreEqual(0, DocFreq(r, "a"));
                Assert.AreEqual(1, DocFreq(r, "aa"));

                // Forwards to last floor block:
                Assert.AreEqual(1, DocFreq(r, "xx"));
                Assert.AreEqual(1, DocFreq(r, "aa1"));
                Assert.AreEqual(0, DocFreq(r, "yy"));

                Assert.AreEqual(1, DocFreq(r, "xx"));
                Assert.AreEqual(1, DocFreq(r, "aa9"));

                Assert.AreEqual(1, DocFreq(r, "xx"));
                Assert.AreEqual(1, DocFreq(r, "aa4"));

                TermsEnum te = MultiFields.GetTerms(r, FIELD).Iterator(null);
                while (te.Next() != null)
                {
                    //System.out.println("TEST: next term=" + te.Term().Utf8ToString());
                }

                Assert.IsTrue(SeekExact(te, "aa1"));
                Assert.AreEqual("aa2", Next(te));
                Assert.IsTrue(SeekExact(te, "aa8"));
                Assert.AreEqual("aa9", Next(te));
                Assert.AreEqual("xx", Next(te));

                TestRandomSeeks(r, terms);
            }
        }

        [Test]
        public virtual void TestZeroTerms()
        {
            var d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), d, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewTextField("field", "one two three", Field.Store.NO));
            doc = new Document();
            doc.Add(NewTextField("field2", "one two three", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();
            w.DeleteDocuments(new Term("field", "one"));
            w.ForceMerge(1);
            IndexReader r = w.Reader;
            w.Dispose();
            Assert.AreEqual(1, r.NumDocs);
            Assert.AreEqual(1, r.MaxDoc);
            Terms terms = MultiFields.GetTerms(r, "field");
            if (terms != null)
            {
                Assert.IsNull(terms.Iterator(null).Next());
            }
            r.Dispose();
            d.Dispose();
        }

        private string RandomString
        {
            get
            {
                //return TestUtil.RandomSimpleString(Random());
                return TestUtil.RandomRealisticUnicodeString(Random());
            }
        }

        [Test]
        public virtual void TestRandomTerms()
        {
            var terms = new string[TestUtil.NextInt(Random(), 1, AtLeast(1000))];
            var seen = new HashSet<string>();

            var allowEmptyString = Random().NextBoolean();

            if (Random().Next(10) == 7 && terms.Length > 2)
            {
                // Sometimes add a bunch of terms sharing a longish common prefix:
                int numTermsSamePrefix = Random().Next(terms.Length / 2);
                if (numTermsSamePrefix > 0)
                {
                    string prefix;
                    while (true)
                    {
                        prefix = RandomString;
                        if (prefix.Length < 5)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (seen.Count < numTermsSamePrefix)
                    {
                        string t = prefix + RandomString;
                        if (!seen.Contains(t))
                        {
                            terms[seen.Count] = t;
                            seen.Add(t);
                        }
                    }
                }
            }

            while (seen.Count < terms.Length)
            {
                string t = RandomString;
                if (!seen.Contains(t) && (allowEmptyString || t.Length != 0))
                {
                    terms[seen.Count] = t;
                    seen.Add(t);
                }
            }

            using (var d = NewDirectory())
            using (var r = MakeIndex(d, terms))
            {
                TestRandomSeeks(r, terms);
            }
        }

        // sugar
        private bool SeekExact(TermsEnum te, string term)
        {
            return te.SeekExact(new BytesRef(term));
        }

        // sugar
        private string Next(TermsEnum te)
        {
            BytesRef br = te.Next();
            if (br == null)
            {
                return null;
            }
            else
            {
                return br.Utf8ToString();
            }
        }

        private BytesRef GetNonExistTerm(BytesRef[] terms)
        {
            BytesRef t = null;
            while (true)
            {
                string ts = RandomString;
                t = new BytesRef(ts);
                if (Array.BinarySearch(terms, t) < 0)
                {
                    return t;
                }
            }
        }

        private class TermAndState
        {
            public readonly BytesRef Term;
            public readonly TermState State;

            public TermAndState(BytesRef term, TermState state)
            {
                this.Term = term;
                this.State = state;
            }
        }

        private void TestRandomSeeks(IndexReader r, params string[] validTermStrings)
        {
            BytesRef[] validTerms = new BytesRef[validTermStrings.Length];
            for (int termIDX = 0; termIDX < validTermStrings.Length; termIDX++)
            {
                validTerms[termIDX] = new BytesRef(validTermStrings[termIDX]);
            }
            Array.Sort(validTerms);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: " + validTerms.Length + " terms:");
                foreach (BytesRef t in validTerms)
                {
                    Console.WriteLine("  " + t.Utf8ToString() + " " + t);
                }
            }
            TermsEnum te = MultiFields.GetTerms(r, FIELD).Iterator(null);

            int END_LOC = -validTerms.Length - 1;

            IList<TermAndState> termStates = new List<TermAndState>();

            for (int iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
            {
                BytesRef t;
                int loc;
                TermState termState;
                if (Random().Next(6) == 4)
                {
                    // pick term that doens't exist:
                    t = GetNonExistTerm(validTerms);
                    termState = null;
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: invalid term=" + t.Utf8ToString());
                    }
                    loc = Array.BinarySearch(validTerms, t);
                }
                else if (termStates.Count != 0 && Random().Next(4) == 1)
                {
                    TermAndState ts = termStates[Random().Next(termStates.Count)];
                    t = ts.Term;
                    loc = Array.BinarySearch(validTerms, t);
                    Assert.IsTrue(loc >= 0);
                    termState = ts.State;
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: valid termState term=" + t.Utf8ToString());
                    }
                }
                else
                {
                    // pick valid term
                    loc = Random().Next(validTerms.Length);
                    t = BytesRef.DeepCopyOf(validTerms[loc]);
                    termState = null;
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: valid term=" + t.Utf8ToString());
                    }
                }

                // seekCeil or seekExact:
                bool doSeekExact = Random().NextBoolean();
                if (termState != null)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  seekExact termState");
                    }
                    te.SeekExact(t, termState);
                }
                else if (doSeekExact)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  seekExact");
                    }
                    Assert.AreEqual(loc >= 0, te.SeekExact(t));
                }
                else
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  seekCeil");
                    }

                    TermsEnum.SeekStatus result = te.SeekCeil(t);
                    if (VERBOSE)
                    {
                        Console.WriteLine("  got " + result);
                    }

                    if (loc >= 0)
                    {
                        Assert.AreEqual(TermsEnum.SeekStatus.FOUND, result);
                    }
                    else if (loc == END_LOC)
                    {
                        Assert.AreEqual(TermsEnum.SeekStatus.END, result);
                    }
                    else
                    {
                        Debug.Assert(loc >= -validTerms.Length);
                        Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, result);
                    }
                }

                if (loc >= 0)
                {
                    Assert.AreEqual(t, te.Term());
                }
                else if (doSeekExact)
                {
                    // TermsEnum is unpositioned if seekExact returns false
                    continue;
                }
                else if (loc == END_LOC)
                {
                    continue;
                }
                else
                {
                    loc = -loc - 1;
                    Assert.AreEqual(validTerms[loc], te.Term());
                }

                // Do a bunch of next's after the seek
                int numNext = Random().Next(validTerms.Length);

                for (int nextCount = 0; nextCount < numNext; nextCount++)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: next loc=" + loc + " of " + validTerms.Length);
                    }
                    BytesRef t2 = te.Next();
                    loc++;
                    if (loc == validTerms.Length)
                    {
                        Assert.IsNull(t2);
                        break;
                    }
                    else
                    {
                        Assert.AreEqual(validTerms[loc], t2);
                        if (Random().Next(40) == 17 && termStates.Count < 100)
                        {
                            termStates.Add(new TermAndState(validTerms[loc], te.TermState()));
                        }
                    }
                }
            }
        }

        [Test]
        public virtual void TestIntersectBasic()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(new LogDocMergePolicy());
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
            Document doc = new Document();
            doc.Add(NewTextField("field", "aaa", Field.Store.NO));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("field", "bbb", Field.Store.NO));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(NewTextField("field", "ccc", Field.Store.NO));
            w.AddDocument(doc);

            w.ForceMerge(1);
            DirectoryReader r = w.Reader;
            w.Dispose();
            AtomicReader sub = GetOnlySegmentReader(r);
            Terms terms = sub.Fields.Terms("field");
            Automaton automaton = (new RegExp(".*", RegExp.NONE)).ToAutomaton();
            CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
            TermsEnum te = terms.Intersect(ca, null);
            Assert.AreEqual("aaa", te.Next().Utf8ToString());
            Assert.AreEqual(0, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.AreEqual("bbb", te.Next().Utf8ToString());
            Assert.AreEqual(1, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.AreEqual("ccc", te.Next().Utf8ToString());
            Assert.AreEqual(2, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.IsNull(te.Next());

            te = terms.Intersect(ca, new BytesRef("abc"));
            Assert.AreEqual("bbb", te.Next().Utf8ToString());
            Assert.AreEqual(1, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.AreEqual("ccc", te.Next().Utf8ToString());
            Assert.AreEqual(2, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.IsNull(te.Next());

            te = terms.Intersect(ca, new BytesRef("aaa"));
            Assert.AreEqual("bbb", te.Next().Utf8ToString());
            Assert.AreEqual(1, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.AreEqual("ccc", te.Next().Utf8ToString());
            Assert.AreEqual(2, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.IsNull(te.Next());

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIntersectStartTerm()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(new LogDocMergePolicy());

            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
            Document doc = new Document();
            doc.Add(NewStringField("field", "abc", Field.Store.NO));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("field", "abd", Field.Store.NO));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("field", "acd", Field.Store.NO));
            w.AddDocument(doc);

            doc = new Document();
            doc.Add(NewStringField("field", "bcd", Field.Store.NO));
            w.AddDocument(doc);

            w.ForceMerge(1);
            DirectoryReader r = w.Reader;
            w.Dispose();
            AtomicReader sub = GetOnlySegmentReader(r);
            Terms terms = sub.Fields.Terms("field");

            Automaton automaton = (new RegExp(".*d", RegExp.NONE)).ToAutomaton();
            CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
            TermsEnum te;

            // should seek to startTerm
            te = terms.Intersect(ca, new BytesRef("aad"));
            Assert.AreEqual("abd", te.Next().Utf8ToString());
            Assert.AreEqual(1, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.AreEqual("acd", te.Next().Utf8ToString());
            Assert.AreEqual(2, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.AreEqual("bcd", te.Next().Utf8ToString());
            Assert.AreEqual(3, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.IsNull(te.Next());

            // should fail to find ceil label on second arc, rewind
            te = terms.Intersect(ca, new BytesRef("add"));
            Assert.AreEqual("bcd", te.Next().Utf8ToString());
            Assert.AreEqual(3, te.Docs(null, null, DocsEnum.FLAG_NONE).NextDoc());
            Assert.IsNull(te.Next());

            // should reach end
            te = terms.Intersect(ca, new BytesRef("bcd"));
            Assert.IsNull(te.Next());
            te = terms.Intersect(ca, new BytesRef("ddd"));
            Assert.IsNull(te.Next());

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIntersectEmptyString()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(new LogDocMergePolicy());
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
            Document doc = new Document();
            doc.Add(NewStringField("field", "", Field.Store.NO));
            doc.Add(NewStringField("field", "abc", Field.Store.NO));
            w.AddDocument(doc);

            doc = new Document();
            // add empty string to both documents, so that singletonDocID == -1.
            // For a FST-based term dict, we'll expect to see the first arc is
            // flaged with HAS_FINAL_OUTPUT
            doc.Add(NewStringField("field", "abc", Field.Store.NO));
            doc.Add(NewStringField("field", "", Field.Store.NO));
            w.AddDocument(doc);

            w.ForceMerge(1);
            DirectoryReader r = w.Reader;
            w.Dispose();
            AtomicReader sub = GetOnlySegmentReader(r);
            Terms terms = sub.Fields.Terms("field");

            Automaton automaton = (new RegExp(".*", RegExp.NONE)).ToAutomaton(); // accept ALL
            CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);

            TermsEnum te = terms.Intersect(ca, null);
            DocsEnum de;

            Assert.AreEqual("", te.Next().Utf8ToString());
            de = te.Docs(null, null, DocsEnum.FLAG_NONE);
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(1, de.NextDoc());

            Assert.AreEqual("abc", te.Next().Utf8ToString());
            de = te.Docs(null, null, DocsEnum.FLAG_NONE);
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(1, de.NextDoc());

            Assert.IsNull(te.Next());

            // pass empty string
            te = terms.Intersect(ca, new BytesRef(""));

            Assert.AreEqual("abc", te.Next().Utf8ToString());
            de = te.Docs(null, null, DocsEnum.FLAG_NONE);
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(1, de.NextDoc());

            Assert.IsNull(te.Next());

            r.Dispose();
            dir.Dispose();
        }
    }
}