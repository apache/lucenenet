using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    [SuppressCodecs("SimpleText", "Memory", "Direct")]
    [TestFixture]
    public class TestLongPostings : LuceneTestCase
    {
        // Produces a realistic unicode random string that
        // survives MockAnalyzer unchanged:
        private string GetRandomTerm(string other)
        {
            Analyzer a = new MockAnalyzer(Random);
            while (true)
            {
                string s = TestUtil.RandomRealisticUnicodeString(Random);
                if (other != null && s.Equals(other, StringComparison.Ordinal))
                {
                    continue;
                }
                Exception priorException = null; // LUCENENET: No need to cast to IOExcpetion
                TokenStream ts = a.GetTokenStream("foo", new StringReader(s));
                try
                {
                    ITermToBytesRefAttribute termAtt = ts.GetAttribute<ITermToBytesRefAttribute>();
                    BytesRef termBytes = termAtt.BytesRef;
                    ts.Reset();

                    int count = 0;
                    bool changed = false;

                    while (ts.IncrementToken())
                    {
                        termAtt.FillBytesRef();
                        if (count == 0 && !termBytes.Utf8ToString().Equals(s, StringComparison.Ordinal))
                        {
                            // The value was changed during analysis.  Keep iterating so the
                            // tokenStream is exhausted.
                            changed = true;
                        }
                        count++;
                    }

                    ts.End();
                    // Did we iterate just once and the value was unchanged?
                    if (!changed && count == 1)
                    {
                        return s;
                    }
                }
                catch (Exception e) when (e.IsIOException())
                {
                    priorException = e;
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(priorException, ts);
                }
            }
        }

        [Test]
        public virtual void TestLongPostings_Mem()
        {
            // Don't use TestUtil.getTempDir so that we own the
            // randomness (ie same seed will point to same dir):
            Directory dir = NewFSDirectory(CreateTempDir("longpostings" + "." + Random.NextInt64()));

            int NUM_DOCS = AtLeast(2000);

            if (Verbose)
            {
                Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
            }

            string s1 = GetRandomTerm(null);
            string s2 = GetRandomTerm(s1);

            if (Verbose)
            {
                Console.WriteLine("\nTEST: s1=" + s1 + " s2=" + s2);
                /*
                for(int idx=0;idx<s1.length();idx++) {
                  System.out.println("  s1 ch=0x" + Integer.toHexString(s1.charAt(idx)));
                }
                for(int idx=0;idx<s2.length();idx++) {
                  System.out.println("  s2 ch=0x" + Integer.toHexString(s2.charAt(idx)));
                }
                */
            }

            FixedBitSet isS1 = new FixedBitSet(NUM_DOCS);
            for (int idx = 0; idx < NUM_DOCS; idx++)
            {
                if (Random.NextBoolean())
                {
                    isS1.Set(idx);
                }
            }

            IndexReader r;
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMergePolicy(NewLogMergePolicy());
            iwc.SetRAMBufferSizeMB(16.0 + 16.0 * Random.NextDouble());
            iwc.SetMaxBufferedDocs(-1);
            RandomIndexWriter riw = new RandomIndexWriter(Random, dir, iwc);

            for (int idx = 0; idx < NUM_DOCS; idx++)
            {
                Document doc = new Document();
                string s = isS1.Get(idx) ? s1 : s2;
                Field f = NewTextField("field", s, Field.Store.NO);
                int count = TestUtil.NextInt32(Random, 1, 4);
                for (int ct = 0; ct < count; ct++)
                {
                    doc.Add(f);
                }
                riw.AddDocument(doc);
            }

            r = riw.GetReader();
            riw.Dispose();

            /*
            if (VERBOSE) {
              System.out.println("TEST: terms");
              TermEnum termEnum = r.Terms();
              while(termEnum.Next()) {
                System.out.println("  term=" + termEnum.Term() + " len=" + termEnum.Term().Text().length());
                Assert.IsTrue(termEnum.DocFreq() > 0);
                System.out.println("    s1?=" + (termEnum.Term().Text().equals(s1)) + " s1len=" + s1.length());
                System.out.println("    s2?=" + (termEnum.Term().Text().equals(s2)) + " s2len=" + s2.length());
                final String s = termEnum.Term().Text();
                for(int idx=0;idx<s.length();idx++) {
                  System.out.println("      ch=0x" + Integer.toHexString(s.charAt(idx)));
                }
              }
            }
            */

            Assert.AreEqual(NUM_DOCS, r.NumDocs);
            Assert.IsTrue(r.DocFreq(new Term("field", s1)) > 0);
            Assert.IsTrue(r.DocFreq(new Term("field", s2)) > 0);

            int num = AtLeast(1000);
            for (int iter = 0; iter < num; iter++)
            {
                string term;
                bool doS1;
                if (Random.NextBoolean())
                {
                    term = s1;
                    doS1 = true;
                }
                else
                {
                    term = s2;
                    doS1 = false;
                }

                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + iter + " doS1=" + doS1);
                }

                DocsAndPositionsEnum postings = MultiFields.GetTermPositionsEnum(r, null, "field", new BytesRef(term));

                int docID = -1;
                while (docID < DocIdSetIterator.NO_MORE_DOCS)
                {
                    int what = Random.Next(3);
                    if (what == 0)
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: docID=" + docID + "; do next()");
                        }
                        // nextDoc
                        int expected = docID + 1;
                        while (true)
                        {
                            if (expected == NUM_DOCS)
                            {
                                expected = int.MaxValue;
                                break;
                            }
                            else if (isS1.Get(expected) == doS1)
                            {
                                break;
                            }
                            else
                            {
                                expected++;
                            }
                        }
                        docID = postings.NextDoc();
                        if (Verbose)
                        {
                            Console.WriteLine("  got docID=" + docID);
                        }
                        Assert.AreEqual(expected, docID);
                        if (docID == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }

                        if (Random.Next(6) == 3)
                        {
                            int freq = postings.Freq;
                            Assert.IsTrue(freq >= 1 && freq <= 4);
                            for (int pos = 0; pos < freq; pos++)
                            {
                                Assert.AreEqual(pos, postings.NextPosition());
                                if (Random.NextBoolean())
                                {
                                    var dummy = postings.GetPayload();
                                    if (Random.NextBoolean())
                                    {
                                        dummy = postings.GetPayload(); // get it again
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // advance
                        int targetDocID;
                        if (docID == -1)
                        {
                            targetDocID = Random.Next(NUM_DOCS + 1);
                        }
                        else
                        {
                            targetDocID = docID + TestUtil.NextInt32(Random, 1, NUM_DOCS - docID);
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: docID=" + docID + "; do advance(" + targetDocID + ")");
                        }
                        int expected = targetDocID;
                        while (true)
                        {
                            if (expected == NUM_DOCS)
                            {
                                expected = int.MaxValue;
                                break;
                            }
                            else if (isS1.Get(expected) == doS1)
                            {
                                break;
                            }
                            else
                            {
                                expected++;
                            }
                        }

                        docID = postings.Advance(targetDocID);
                        if (Verbose)
                        {
                            Console.WriteLine("  got docID=" + docID);
                        }
                        Assert.AreEqual(expected, docID);
                        if (docID == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }

                        if (Random.Next(6) == 3)
                        {
                            int freq = postings.Freq;
                            Assert.IsTrue(freq >= 1 && freq <= 4);
                            for (int pos = 0; pos < freq; pos++)
                            {
                                Assert.AreEqual(pos, postings.NextPosition());
                                if (Random.NextBoolean())
                                {
                                    var dummy = postings.GetPayload();
                                    if (Random.NextBoolean())
                                    {
                                        dummy = postings.GetPayload(); // get it again
                                    }
                                }
                            }
                        }
                    }
                }
            }
            r.Dispose();
            dir.Dispose();
        }

        // a weaker form of testLongPostings, that doesnt check positions
        [Test]
        public virtual void TestLongPostingsNoPositions()
        {
            DoTestLongPostingsNoPositions(IndexOptions.DOCS_ONLY);
            DoTestLongPostingsNoPositions(IndexOptions.DOCS_AND_FREQS);
        }

        public virtual void DoTestLongPostingsNoPositions(IndexOptions options)
        {
            // Don't use TestUtil.getTempDir so that we own the
            // randomness (ie same seed will point to same dir):
            Directory dir = NewFSDirectory(CreateTempDir("longpostings" + "." + Random.NextInt64()));

            int NUM_DOCS = AtLeast(2000);

            if (Verbose)
            {
                Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
            }

            string s1 = GetRandomTerm(null);
            string s2 = GetRandomTerm(s1);

            if (Verbose)
            {
                Console.WriteLine("\nTEST: s1=" + s1 + " s2=" + s2);
                /*
                for(int idx=0;idx<s1.length();idx++) {
                  System.out.println("  s1 ch=0x" + Integer.toHexString(s1.charAt(idx)));
                }
                for(int idx=0;idx<s2.length();idx++) {
                  System.out.println("  s2 ch=0x" + Integer.toHexString(s2.charAt(idx)));
                }
                */
            }

            FixedBitSet isS1 = new FixedBitSet(NUM_DOCS);
            for (int idx = 0; idx < NUM_DOCS; idx++)
            {
                if (Random.NextBoolean())
                {
                    isS1.Set(idx);
                }
            }

            IndexReader r;
            if (true)
            {
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMergePolicy(NewLogMergePolicy());
                iwc.SetRAMBufferSizeMB(16.0 + 16.0 * Random.NextDouble());
                iwc.SetMaxBufferedDocs(-1);
                RandomIndexWriter riw = new RandomIndexWriter(Random, dir, iwc);

                FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
                ft.IndexOptions = options;
                for (int idx = 0; idx < NUM_DOCS; idx++)
                {
                    Document doc = new Document();
                    string s = isS1.Get(idx) ? s1 : s2;
                    Field f = NewField("field", s, ft);
                    int count = TestUtil.NextInt32(Random, 1, 4);
                    for (int ct = 0; ct < count; ct++)
                    {
                        doc.Add(f);
                    }
                    riw.AddDocument(doc);
                }

                r = riw.GetReader();
                riw.Dispose();
            }
            else
            {
#pragma warning disable 162
                r = DirectoryReader.Open(dir);
#pragma warning restore 162
            }

            /*
            if (VERBOSE) {
              System.out.println("TEST: terms");
              TermEnum termEnum = r.Terms();
              while(termEnum.Next()) {
                System.out.println("  term=" + termEnum.Term() + " len=" + termEnum.Term().Text().length());
                Assert.IsTrue(termEnum.DocFreq() > 0);
                System.out.println("    s1?=" + (termEnum.Term().Text().equals(s1)) + " s1len=" + s1.length());
                System.out.println("    s2?=" + (termEnum.Term().Text().equals(s2)) + " s2len=" + s2.length());
                final String s = termEnum.Term().Text();
                for(int idx=0;idx<s.length();idx++) {
                  System.out.println("      ch=0x" + Integer.toHexString(s.charAt(idx)));
                }
              }
            }
            */

            Assert.AreEqual(NUM_DOCS, r.NumDocs);
            Assert.IsTrue(r.DocFreq(new Term("field", s1)) > 0);
            Assert.IsTrue(r.DocFreq(new Term("field", s2)) > 0);

            int num = AtLeast(1000);
            for (int iter = 0; iter < num; iter++)
            {
                string term;
                bool doS1;
                if (Random.NextBoolean())
                {
                    term = s1;
                    doS1 = true;
                }
                else
                {
                    term = s2;
                    doS1 = false;
                }

                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + iter + " doS1=" + doS1 + " term=" + term);
                }

                DocsEnum docs;
                DocsEnum postings;

                if (options == IndexOptions.DOCS_ONLY)
                {
                    docs = TestUtil.Docs(Random, r, "field", new BytesRef(term), null, null, DocsFlags.NONE);
                    postings = null;
                }
                else
                {
                    docs = postings = TestUtil.Docs(Random, r, "field", new BytesRef(term), null, null, DocsFlags.FREQS);
                    if (Debugging.AssertsEnabled) Debugging.Assert(postings != null);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(docs != null);

                int docID = -1;
                while (docID < DocIdSetIterator.NO_MORE_DOCS)
                {
                    int what = Random.Next(3);
                    if (what == 0)
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: docID=" + docID + "; do next()");
                        }
                        // nextDoc
                        int expected = docID + 1;
                        while (true)
                        {
                            if (expected == NUM_DOCS)
                            {
                                expected = int.MaxValue;
                                break;
                            }
                            else if (isS1.Get(expected) == doS1)
                            {
                                break;
                            }
                            else
                            {
                                expected++;
                            }
                        }
                        docID = docs.NextDoc();
                        if (Verbose)
                        {
                            Console.WriteLine("  got docID=" + docID);
                        }
                        Assert.AreEqual(expected, docID);
                        if (docID == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }

                        if (Random.Next(6) == 3 && postings != null)
                        {
                            int freq = postings.Freq;
                            Assert.IsTrue(freq >= 1 && freq <= 4);
                        }
                    }
                    else
                    {
                        // advance
                        int targetDocID;
                        if (docID == -1)
                        {
                            targetDocID = Random.Next(NUM_DOCS + 1);
                        }
                        else
                        {
                            targetDocID = docID + TestUtil.NextInt32(Random, 1, NUM_DOCS - docID);
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: docID=" + docID + "; do advance(" + targetDocID + ")");
                        }
                        int expected = targetDocID;
                        while (true)
                        {
                            if (expected == NUM_DOCS)
                            {
                                expected = int.MaxValue;
                                break;
                            }
                            else if (isS1.Get(expected) == doS1)
                            {
                                break;
                            }
                            else
                            {
                                expected++;
                            }
                        }

                        docID = docs.Advance(targetDocID);
                        if (Verbose)
                        {
                            Console.WriteLine("  got docID=" + docID);
                        }
                        Assert.AreEqual(expected, docID);
                        if (docID == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }

                        if (Random.Next(6) == 3 && postings != null)
                        {
                            int freq = postings.Freq;
                            Assert.IsTrue(freq >= 1 && freq <= 4, "got invalid freq=" + freq);
                        }
                    }
                }
            }
            r.Dispose();
            dir.Dispose();
        }
    }
}