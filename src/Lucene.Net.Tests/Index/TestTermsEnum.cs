using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
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

    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using Int32Field = Int32Field;
    using LineFileDocs = Lucene.Net.Util.LineFileDocs;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
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
            Random random = new J2N.Randomizer(Random.NextInt64());
            LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues);
            Directory d = NewDirectory();
            MockAnalyzer analyzer = new MockAnalyzer(LuceneTestCase.Random);
            analyzer.MaxTokenLength = TestUtil.NextInt32(LuceneTestCase.Random, 1, IndexWriter.MAX_TERM_LENGTH);
            RandomIndexWriter w = new RandomIndexWriter(LuceneTestCase.Random, d, analyzer);
            int numDocs = AtLeast(10);
            for (int docCount = 0; docCount < numDocs; docCount++)
            {
                w.AddDocument(docs.NextDoc());
            }
            IndexReader r = w.GetReader();
            w.Dispose();

            JCG.List<BytesRef> terms = new JCG.List<BytesRef>();
            TermsEnum termsEnum = MultiFields.GetTerms(r, "body").GetEnumerator();
            while (termsEnum.MoveNext())
            {
                terms.Add(BytesRef.DeepCopyOf(termsEnum.Term));
            }
            if (Verbose)
            {
                Console.WriteLine("TEST: " + terms.Count + " terms");
            }

            int upto = -1;
            int iters = AtLeast(200);
            for (int iter = 0; iter < iters; iter++)
            {
                bool isEnd;
                if (upto != -1 && LuceneTestCase.Random.NextBoolean())
                {
                    // next
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: iter next");
                    }
                    isEnd = termsEnum.MoveNext() == false;
                    upto++;
                    if (isEnd)
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("  end");
                        }
                        Assert.AreEqual(upto, terms.Count);
                        upto = -1;
                    }
                    else
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("  got term=" + termsEnum.Term.Utf8ToString() + " expected=" + terms[upto].Utf8ToString());
                        }
                        Assert.IsTrue(upto < terms.Count);
                        Assert.AreEqual(terms[upto], termsEnum.Term);
                    }
                }
                else
                {
                    BytesRef target;
                    string exists;
                    if (LuceneTestCase.Random.NextBoolean())
                    {
                        // likely fake term
                        if (LuceneTestCase.Random.NextBoolean())
                        {
                            target = new BytesRef(TestUtil.RandomSimpleString(LuceneTestCase.Random));
                        }
                        else
                        {
                            target = new BytesRef(TestUtil.RandomRealisticUnicodeString(LuceneTestCase.Random));
                        }
                        exists = "likely not";
                    }
                    else
                    {
                        // real term
                        target = terms[LuceneTestCase.Random.Next(terms.Count)];
                        exists = "yes";
                    }

                    upto = terms.BinarySearch(target);

                    if (LuceneTestCase.Random.NextBoolean())
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: iter seekCeil target=" + target.Utf8ToString() + " exists=" + exists);
                        }
                        // seekCeil
                        TermsEnum.SeekStatus status = termsEnum.SeekCeil(target);
                        if (Verbose)
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
                                Assert.AreEqual(terms[upto], termsEnum.Term);
                            }
                        }
                        else
                        {
                            Assert.AreEqual(TermsEnum.SeekStatus.FOUND, status);
                            Assert.AreEqual(terms[upto], termsEnum.Term);
                        }
                    }
                    else
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: iter seekExact target=" + target.Utf8ToString() + " exists=" + exists);
                        }
                        // seekExact
                        bool result = termsEnum.SeekExact(target);
                        if (Verbose)
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
                            Assert.AreEqual(target, termsEnum.Term);
                        }
                    }
                }
            }

            r.Dispose();
            d.Dispose();
            docs.Dispose();
        }

        private void AddDoc(RandomIndexWriter w, ICollection<string> terms, IDictionary<BytesRef, int> termToID, int id)
        {
            Document doc = new Document();
            doc.Add(new Int32Field("id", id, Field.Store.NO));
            if (Verbose)
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
        [Test]
        public virtual void TestIntersectRandom()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            int numTerms = AtLeast(300);
            //final int numTerms = 50;

            ISet<string> terms = new JCG.HashSet<string>();
            ICollection<string> pendingTerms = new JCG.List<string>();
            IDictionary<BytesRef, int> termToID = new Dictionary<BytesRef, int>();
            int id = 0;
            while (terms.Count != numTerms)
            {
                string s = RandomString;
                if (!terms.Contains(s))
                {
                    terms.Add(s);
                    pendingTerms.Add(s);
                    if (Random.Next(20) == 7)
                    {
                        AddDoc(w, pendingTerms, termToID, id++);
                    }
                }
            }
            AddDoc(w, pendingTerms, termToID, id++);

            BytesRef[] termsArray = new BytesRef[terms.Count];
            ISet<BytesRef> termsSet = new JCG.HashSet<BytesRef>();
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

            if (Verbose)
            {
                Console.WriteLine("\nTEST: indexed terms (unicode order):");
                foreach (BytesRef t in termsArray)
                {
                    Console.WriteLine("  " + t.Utf8ToString() + " -> id:" + termToID[t]);
                }
            }

            IndexReader r = w.GetReader();
            w.Dispose();

            // NOTE: intentional insanity!!
            FieldCache.Int32s docIDToID = FieldCache.DEFAULT.GetInt32s(SlowCompositeReaderWrapper.Wrap(r), "id", false);

            for (int iter = 0; iter < 10 * RandomMultiplier; iter++)
            {
                // TODO: can we also test infinite As here...?

                // From the random terms, pick some ratio and compile an
                // automaton:
                ISet<string> acceptTerms = new JCG.HashSet<string>();
                JCG.SortedSet<BytesRef> sortedAcceptTerms = new JCG.SortedSet<BytesRef>();
                double keepPct = Random.NextDouble();
                Automaton a;
                if (iter == 0)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: empty automaton");
                    }
                    a = BasicAutomata.MakeEmpty();
                }
                else
                {
                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: keepPct=" + keepPct);
                    }
                    foreach (string s in terms)
                    {
                        string s2;
                        if (Random.NextDouble() <= keepPct)
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

                if (Random.NextBoolean())
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: reduce the automaton");
                    }
                    a.Reduce();
                }

                CompiledAutomaton c = new CompiledAutomaton(a, true, false);

                BytesRef[] acceptTermsArray = new BytesRef[acceptTerms.Count];
                ISet<BytesRef> acceptTermsSet = new JCG.HashSet<BytesRef>();
                int upto = 0;
                foreach (string s in acceptTerms)
                {
                    BytesRef b = new BytesRef(s);
                    acceptTermsArray[upto++] = b;
                    acceptTermsSet.Add(b);
                    Assert.IsTrue(Accepts(c, b));
                }
                Array.Sort(acceptTermsArray);

                if (Verbose)
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
                    BytesRef startTerm = acceptTermsArray.Length == 0 || Random.NextBoolean() ? null : acceptTermsArray[Random.Next(acceptTermsArray.Length)];

                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: iter2=" + iter2 + " startTerm=" + (startTerm is null ? "<null>" : startTerm.Utf8ToString()));

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
                    if (startTerm is null)
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
                        Assert.IsTrue(te.MoveNext());
                        BytesRef actual = te.Term;
                        if (Verbose)
                        {
                            Console.WriteLine("TEST:   next() expected=" + expected.Utf8ToString() + " actual=" + (actual is null ? "null" : actual.Utf8ToString()));
                        }
                        Assert.AreEqual(expected, actual);
                        Assert.AreEqual(1, te.DocFreq);
                        docsEnum = TestUtil.Docs(Random, te, null, docsEnum, DocsFlags.NONE);
                        int docID = docsEnum.NextDoc();
                        Assert.IsTrue(docID != DocIdSetIterator.NO_MORE_DOCS);
                        Assert.AreEqual(docIDToID.Get(docID), (int)termToID[expected]);
                        do
                        {
                            loc++;
                        } while (loc < termsArray.Length && !acceptTermsSet.Contains(termsArray[loc]));
                    }
                    Assert.IsFalse(te.MoveNext());
                }
            }

            r.Dispose();
            dir.Dispose();
        }

        private readonly string FIELD = "field";

        private IndexReader MakeIndex(Directory d, params string[] terms)
        {
            var iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));

            /*
            iwc.SetCodec(new StandardCodec(minTermsInBlock, maxTermsInBlock));
            */

            using var w = new RandomIndexWriter(Random, d, iwc);
            foreach (string term in terms)
            {
                var doc = new Document();
                var f = NewStringField(FIELD, term, Field.Store.NO);
                doc.Add(f);
                w.AddDocument(doc);
            }

            return w.GetReader();
        }

        private int DocFreq(IndexReader r, string term)
        {
            return r.DocFreq(new Term(FIELD, term));
        }

        [Test]
        public virtual void TestEasy()
        {
            // No floor arcs:
            using var d = NewDirectory();
            using var r = MakeIndex(d, "aa0", "aa1", "aa2", "aa3", "bb0", "bb1", "bb2", "bb3", "aa");
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

        // tests:
        //   - test same prefix has non-floor block and floor block (ie, has 2 long outputs on same term prefix)
        //   - term that's entirely in the index

        [Test]
        public virtual void TestFloorBlocks()
        {
            var terms = new[] { "aa0", "aa1", "aa2", "aa3", "aa4", "aa5", "aa6", "aa7", "aa8", "aa9", "aa", "xx" };

            using var d = NewDirectory();
            using var r = MakeIndex(d, terms);
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

            TermsEnum te = MultiFields.GetTerms(r, FIELD).GetEnumerator();
            while (te.MoveNext())
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

        [Test]
        public virtual void TestZeroTerms()
        {
            var d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document doc = new Document();
            doc.Add(NewTextField("field", "one two three", Field.Store.NO));
            doc = new Document();
            doc.Add(NewTextField("field2", "one two three", Field.Store.NO));
            w.AddDocument(doc);
            w.Commit();
            w.DeleteDocuments(new Term("field", "one"));
            w.ForceMerge(1);
            IndexReader r = w.GetReader();
            w.Dispose();
            Assert.AreEqual(1, r.NumDocs);
            Assert.AreEqual(1, r.MaxDoc);
            Terms terms = MultiFields.GetTerms(r, "field");
            if (terms != null)
            {
                Assert.IsFalse(terms.GetEnumerator().MoveNext());
            }
            r.Dispose();
            d.Dispose();
        }

        private string RandomString =>
            //return TestUtil.RandomSimpleString(Random);
            TestUtil.RandomRealisticUnicodeString(Random);

        [Test]
        public virtual void TestRandomTerms()
        {
            var terms = new string[TestUtil.NextInt32(Random, 1, AtLeast(1000))];
            var seen = new JCG.HashSet<string>();

            var allowEmptyString = Random.NextBoolean();

            if (Random.Next(10) == 7 && terms.Length > 2)
            {
                // Sometimes add a bunch of terms sharing a longish common prefix:
                int numTermsSamePrefix = Random.Next(terms.Length / 2);
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

            using var d = NewDirectory();
            using var r = MakeIndex(d, terms);
            TestRandomSeeks(r, terms);
        }

        // sugar
        private bool SeekExact(TermsEnum te, string term)
        {
            return te.SeekExact(new BytesRef(term));
        }

        // sugar
        private string Next(TermsEnum te)
        {
            if (!te.MoveNext())
            {
                return null;
            }
            else
            {
                return te.Term.Utf8ToString();
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
            public BytesRef Term { get; }
            public TermState State { get; }

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
            if (Verbose)
            {
                Console.WriteLine("TEST: " + validTerms.Length + " terms:");
                foreach (BytesRef t in validTerms)
                {
                    Console.WriteLine("  " + t.Utf8ToString() + " " + t);
                }
            }
            TermsEnum te = MultiFields.GetTerms(r, FIELD).GetEnumerator();

            int END_LOC = -validTerms.Length - 1;

            IList<TermAndState> termStates = new JCG.List<TermAndState>();

            for (int iter = 0; iter < 100 * RandomMultiplier; iter++)
            {
                BytesRef t;
                int loc;
                TermState termState;
                if (Random.Next(6) == 4)
                {
                    // pick term that doens't exist:
                    t = GetNonExistTerm(validTerms);
                    termState = null;
                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: invalid term=" + t.Utf8ToString());
                    }
                    loc = Array.BinarySearch(validTerms, t);
                }
                else if (termStates.Count != 0 && Random.Next(4) == 1)
                {
                    TermAndState ts = termStates[Random.Next(termStates.Count)];
                    t = ts.Term;
                    loc = Array.BinarySearch(validTerms, t);
                    Assert.IsTrue(loc >= 0);
                    termState = ts.State;
                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: valid termState term=" + t.Utf8ToString());
                    }
                }
                else
                {
                    // pick valid term
                    loc = Random.Next(validTerms.Length);
                    t = BytesRef.DeepCopyOf(validTerms[loc]);
                    termState = null;
                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: valid term=" + t.Utf8ToString());
                    }
                }

                // seekCeil or seekExact:
                bool doSeekExact = Random.NextBoolean();
                if (termState != null)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("  seekExact termState");
                    }
                    te.SeekExact(t, termState);
                }
                else if (doSeekExact)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("  seekExact");
                    }
                    Assert.AreEqual(loc >= 0, te.SeekExact(t));
                }
                else
                {
                    if (Verbose)
                    {
                        Console.WriteLine("  seekCeil");
                    }

                    TermsEnum.SeekStatus result = te.SeekCeil(t);
                    if (Verbose)
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
                        if (Debugging.AssertsEnabled) Debugging.Assert(loc >= -validTerms.Length);
                        Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, result);
                    }
                }

                if (loc >= 0)
                {
                    Assert.AreEqual(t, te.Term);
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
                    Assert.AreEqual(validTerms[loc], te.Term);
                }

                // Do a bunch of next's after the seek
                int numNext = Random.Next(validTerms.Length);

                for (int nextCount = 0; nextCount < numNext; nextCount++)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("\nTEST: next loc=" + loc + " of " + validTerms.Length);
                    }
                    bool moved = te.MoveNext();
                    //BytesRef t2 = te.Term;
                    loc++;
                    if (loc == validTerms.Length)
                    {
                        //Assert.IsNull(t2); // LUCENENET specific - accessing the Term after MoveNext() returns false results in an assertion failure
                        Assert.IsFalse(moved);
                        break;
                    }
                    else
                    {
                        Assert.AreEqual(validTerms[loc], te.Term);
                        if (Random.Next(40) == 17 && termStates.Count < 100)
                        {
                            termStates.Add(new TermAndState(validTerms[loc], te.GetTermState()));
                        }
                    }
                }
            }
        }

        [Test]
        public virtual void TestIntersectBasic()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(new LogDocMergePolicy());
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, iwc);
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
            DirectoryReader r = w.GetReader();
            w.Dispose();
            AtomicReader sub = GetOnlySegmentReader(r);
            Terms terms = sub.Fields.GetTerms("field");
            Automaton automaton = (new RegExp(".*", RegExpSyntax.NONE)).ToAutomaton();
            CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
            TermsEnum te = terms.Intersect(ca, null);
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("aaa", te.Term.Utf8ToString());
            Assert.AreEqual(0, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("bbb", te.Term.Utf8ToString());
            Assert.AreEqual(1, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("ccc", te.Term.Utf8ToString());
            Assert.AreEqual(2, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsFalse(te.MoveNext());

            te = terms.Intersect(ca, new BytesRef("abc"));
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("bbb", te.Term.Utf8ToString());
            Assert.AreEqual(1, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("ccc", te.Term.Utf8ToString());
            Assert.AreEqual(2, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsFalse(te.MoveNext());

            te = terms.Intersect(ca, new BytesRef("aaa"));
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("bbb", te.Term.Utf8ToString());
            Assert.AreEqual(1, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("ccc", te.Term.Utf8ToString());
            Assert.AreEqual(2, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsFalse(te.MoveNext());

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIntersectStartTerm()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(new LogDocMergePolicy());

            RandomIndexWriter w = new RandomIndexWriter(Random, dir, iwc);
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
            DirectoryReader r = w.GetReader();
            w.Dispose();
            AtomicReader sub = GetOnlySegmentReader(r);
            Terms terms = sub.Fields.GetTerms("field");

            Automaton automaton = (new RegExp(".*d", RegExpSyntax.NONE)).ToAutomaton();
            CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);
            TermsEnum te;

            // should seek to startTerm
            te = terms.Intersect(ca, new BytesRef("aad"));
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("abd", te.Term.Utf8ToString());
            Assert.AreEqual(1, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("acd", te.Term.Utf8ToString());
            Assert.AreEqual(2, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("bcd", te.Term.Utf8ToString());
            Assert.AreEqual(3, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsFalse(te.MoveNext());

            // should fail to find ceil label on second arc, rewind
            te = terms.Intersect(ca, new BytesRef("add"));
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("bcd", te.Term.Utf8ToString());
            Assert.AreEqual(3, te.Docs(null, null, DocsFlags.NONE).NextDoc());
            Assert.IsFalse(te.MoveNext());

            // should reach end
            te = terms.Intersect(ca, new BytesRef("bcd"));
            Assert.IsFalse(te.MoveNext());
            te = terms.Intersect(ca, new BytesRef("ddd"));
            Assert.IsFalse(te.MoveNext());

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestIntersectEmptyString()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(new LogDocMergePolicy());
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, iwc);
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
            DirectoryReader r = w.GetReader();
            w.Dispose();
            AtomicReader sub = GetOnlySegmentReader(r);
            Terms terms = sub.Fields.GetTerms("field");

            Automaton automaton = (new RegExp(".*", RegExpSyntax.NONE)).ToAutomaton(); // accept ALL
            CompiledAutomaton ca = new CompiledAutomaton(automaton, false, false);

            TermsEnum te = terms.Intersect(ca, null);
            DocsEnum de;

            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("", te.Term.Utf8ToString());
            de = te.Docs(null, null, DocsFlags.NONE);
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(1, de.NextDoc());

            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("abc", te.Term.Utf8ToString());
            de = te.Docs(null, null, DocsFlags.NONE);
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(1, de.NextDoc());

            Assert.IsFalse(te.MoveNext());

            // pass empty string
            te = terms.Intersect(ca, new BytesRef(""));

            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual("abc", te.Term.Utf8ToString());
            de = te.Docs(null, null, DocsFlags.NONE);
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(1, de.NextDoc());

            Assert.IsFalse(te.MoveNext());

            r.Dispose();
            dir.Dispose();
        }
    }
}