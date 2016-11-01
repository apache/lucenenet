using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;

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
    using CannedTokenStream = Lucene.Net.Analysis.CannedTokenStream;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using FieldType = FieldType;
    using IntField = IntField;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockPayloadAnalyzer = Lucene.Net.Analysis.MockPayloadAnalyzer;
    using StringField = StringField;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using Token = Lucene.Net.Analysis.Token;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    // TODO: we really need to test indexingoffsets, but then getting only docs / docs + freqs.
    // not all codecs store prx separate...
    // TODO: fix sep codec to index offsets so we can greatly reduce this list!
    [SuppressCodecs("Lucene3x", "MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom")]
    [TestFixture]
    public class TestPostingsOffsets : LuceneTestCase
    {
        internal IndexWriterConfig Iwc;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
        }

        [Test]
        public virtual void TestBasic()
        {
            Directory dir = NewDirectory();

            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Iwc);
            Document doc = new Document();

            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            if (Random().NextBoolean())
            {
                ft.StoreTermVectors = true;
                ft.StoreTermVectorPositions = Random().NextBoolean();
                ft.StoreTermVectorOffsets = Random().NextBoolean();
            }
            Token[] tokens = new Token[] { MakeToken("a", 1, 0, 6), MakeToken("b", 1, 8, 9), MakeToken("a", 1, 9, 17), MakeToken("c", 1, 19, 50) };
            doc.Add(new Field("content", new CannedTokenStream(tokens), ft));

            w.AddDocument(doc);
            IndexReader r = w.Reader;
            w.Dispose();

            DocsAndPositionsEnum dp = MultiFields.GetTermPositionsEnum(r, null, "content", new BytesRef("a"));
            Assert.IsNotNull(dp);
            Assert.AreEqual(0, dp.NextDoc());
            Assert.AreEqual(2, dp.Freq());
            Assert.AreEqual(0, dp.NextPosition());
            Assert.AreEqual(0, dp.StartOffset());
            Assert.AreEqual(6, dp.EndOffset());
            Assert.AreEqual(2, dp.NextPosition());
            Assert.AreEqual(9, dp.StartOffset());
            Assert.AreEqual(17, dp.EndOffset());
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.NextDoc());

            dp = MultiFields.GetTermPositionsEnum(r, null, "content", new BytesRef("b"));
            Assert.IsNotNull(dp);
            Assert.AreEqual(0, dp.NextDoc());
            Assert.AreEqual(1, dp.Freq());
            Assert.AreEqual(1, dp.NextPosition());
            Assert.AreEqual(8, dp.StartOffset());
            Assert.AreEqual(9, dp.EndOffset());
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.NextDoc());

            dp = MultiFields.GetTermPositionsEnum(r, null, "content", new BytesRef("c"));
            Assert.IsNotNull(dp);
            Assert.AreEqual(0, dp.NextDoc());
            Assert.AreEqual(1, dp.Freq());
            Assert.AreEqual(3, dp.NextPosition());
            Assert.AreEqual(19, dp.StartOffset());
            Assert.AreEqual(50, dp.EndOffset());
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.NextDoc());

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSkipping()
        {
            DoTestNumbers(false);
        }

        [Test]
        public virtual void TestPayloads()
        {
            DoTestNumbers(true);
        }

        public virtual void DoTestNumbers(bool withPayloads)
        {
            Directory dir = NewDirectory();
            Analyzer analyzer = withPayloads ? (Analyzer)new MockPayloadAnalyzer() : new MockAnalyzer(Random());
            Iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            Iwc.SetMergePolicy(NewLogMergePolicy()); // will rely on docids a bit for skipping
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Iwc);

            FieldType ft = new FieldType(TextField.TYPE_STORED);
            ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            if (Random().NextBoolean())
            {
                ft.StoreTermVectors = true;
                ft.StoreTermVectorOffsets = Random().NextBoolean();
                ft.StoreTermVectorPositions = Random().NextBoolean();
            }

            int numDocs = AtLeast(500);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new Field("numbers", English.IntToEnglish(i), ft));
                doc.Add(new Field("oddeven", (i % 2) == 0 ? "even" : "odd", ft));
                doc.Add(new StringField("id", "" + i, Field.Store.NO));
                w.AddDocument(doc);
            }

            IndexReader reader = w.Reader;
            w.Dispose();

            string[] terms = new string[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "hundred" };

            foreach (string term in terms)
            {
                DocsAndPositionsEnum dp = MultiFields.GetTermPositionsEnum(reader, null, "numbers", new BytesRef(term));
                int doc;
                while ((doc = dp.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    string storedNumbers = reader.Document(doc).Get("numbers");
                    int freq = dp.Freq();
                    for (int i = 0; i < freq; i++)
                    {
                        dp.NextPosition();
                        int start = dp.StartOffset();
                        Debug.Assert(start >= 0);
                        int end = dp.EndOffset();
                        Debug.Assert(end >= 0 && end >= start);
                        // check that the offsets correspond to the term in the src text
                        Assert.IsTrue(storedNumbers.Substring(start, end - start).Equals(term));
                        if (withPayloads)
                        {
                            // check that we have a payload and it starts with "pos"
                            Assert.IsNotNull(dp.Payload);
                            BytesRef payload = dp.Payload;
                            Assert.IsTrue(payload.Utf8ToString().StartsWith("pos:"));
                        } // note: withPayloads=false doesnt necessarily mean we dont have them from MockAnalyzer!
                    }
                }
            }

            // check we can skip correctly
            int numSkippingTests = AtLeast(50);

            for (int j = 0; j < numSkippingTests; j++)
            {
                int num = TestUtil.NextInt(Random(), 100, Math.Min(numDocs - 1, 999));
                DocsAndPositionsEnum dp = MultiFields.GetTermPositionsEnum(reader, null, "numbers", new BytesRef("hundred"));
                int doc = dp.Advance(num);
                Assert.AreEqual(num, doc);
                int freq = dp.Freq();
                for (int i = 0; i < freq; i++)
                {
                    string storedNumbers = reader.Document(doc).Get("numbers");
                    dp.NextPosition();
                    int start = dp.StartOffset();
                    Debug.Assert(start >= 0);
                    int end = dp.EndOffset();
                    Debug.Assert(end >= 0 && end >= start);
                    // check that the offsets correspond to the term in the src text
                    Assert.IsTrue(storedNumbers.Substring(start, end - start).Equals("hundred"));
                    if (withPayloads)
                    {
                        // check that we have a payload and it starts with "pos"
                        Assert.IsNotNull(dp.Payload);
                        BytesRef payload = dp.Payload;
                        Assert.IsTrue(payload.Utf8ToString().StartsWith("pos:"));
                    } // note: withPayloads=false doesnt necessarily mean we dont have them from MockAnalyzer!
                }
            }

            // check that other fields (without offsets) work correctly

            for (int i = 0; i < numDocs; i++)
            {
                DocsEnum dp = MultiFields.GetTermDocsEnum(reader, null, "id", new BytesRef("" + i), 0);
                Assert.AreEqual(i, dp.NextDoc());
                Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dp.NextDoc());
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandom()
        {
            // token -> docID -> tokens
            IDictionary<string, IDictionary<int?, IList<Token>>> actualTokens = new Dictionary<string, IDictionary<int?, IList<Token>>>();

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Iwc);

            int numDocs = AtLeast(20);
            //final int numDocs = AtLeast(5);

            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);

            // TODO: randomize what IndexOptions we use; also test
            // changing this up in one IW buffered segment...:
            ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            if (Random().NextBoolean())
            {
                ft.StoreTermVectors = true;
                ft.StoreTermVectorOffsets = Random().NextBoolean();
                ft.StoreTermVectorPositions = Random().NextBoolean();
            }

            for (int docCount = 0; docCount < numDocs; docCount++)
            {
                Document doc = new Document();
                doc.Add(new IntField("id", docCount, Field.Store.NO));
                IList<Token> tokens = new List<Token>();
                int numTokens = AtLeast(100);
                //final int numTokens = AtLeast(20);
                int pos = -1;
                int offset = 0;
                //System.out.println("doc id=" + docCount);
                for (int tokenCount = 0; tokenCount < numTokens; tokenCount++)
                {
                    string text;
                    if (Random().NextBoolean())
                    {
                        text = "a";
                    }
                    else if (Random().NextBoolean())
                    {
                        text = "b";
                    }
                    else if (Random().NextBoolean())
                    {
                        text = "c";
                    }
                    else
                    {
                        text = "d";
                    }

                    int posIncr = Random().NextBoolean() ? 1 : Random().Next(5);
                    if (tokenCount == 0 && posIncr == 0)
                    {
                        posIncr = 1;
                    }
                    int offIncr = Random().NextBoolean() ? 0 : Random().Next(5);
                    int tokenOffset = Random().Next(5);

                    Token token = MakeToken(text, posIncr, offset + offIncr, offset + offIncr + tokenOffset);
                    if (!actualTokens.ContainsKey(text))
                    {
                        actualTokens[text] = new Dictionary<int?, IList<Token>>();
                    }
                    IDictionary<int?, IList<Token>> postingsByDoc = actualTokens[text];
                    if (!postingsByDoc.ContainsKey(docCount))
                    {
                        postingsByDoc[docCount] = new List<Token>();
                    }
                    postingsByDoc[docCount].Add(token);
                    tokens.Add(token);
                    pos += posIncr;
                    // stuff abs position into type:
                    token.Type = "" + pos;
                    offset += offIncr + tokenOffset;
                    //System.out.println("  " + token + " posIncr=" + token.getPositionIncrement() + " pos=" + pos + " off=" + token.StartOffset() + "/" + token.EndOffset() + " (freq=" + postingsByDoc.Get(docCount).Size() + ")");
                }
                doc.Add(new Field("content", new CannedTokenStream(tokens.ToArray()), ft));
                w.AddDocument(doc);
            }
            DirectoryReader r = w.Reader;
            w.Dispose();

            string[] terms = new string[] { "a", "b", "c", "d" };
            foreach (AtomicReaderContext ctx in r.Leaves)
            {
                // TODO: improve this
                AtomicReader sub = (AtomicReader)ctx.Reader;
                //System.out.println("\nsub=" + sub);
                TermsEnum termsEnum = sub.Fields.Terms("content").Iterator(null);
                DocsEnum docs = null;
                DocsAndPositionsEnum docsAndPositions = null;
                DocsAndPositionsEnum docsAndPositionsAndOffsets = null;
                FieldCache.Ints docIDToID = FieldCache.DEFAULT.GetInts(sub, "id", false);
                foreach (string term in terms)
                {
                    //System.out.println("  term=" + term);
                    if (termsEnum.SeekExact(new BytesRef(term)))
                    {
                        docs = termsEnum.Docs(null, docs);
                        Assert.IsNotNull(docs);
                        int doc;
                        //System.out.println("    doc/freq");
                        while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            IList<Token> expected = actualTokens[term][docIDToID.Get(doc)];
                            //System.out.println("      doc=" + docIDToID.Get(doc) + " docID=" + doc + " " + expected.Size() + " freq");
                            Assert.IsNotNull(expected);
                            Assert.AreEqual(expected.Count, docs.Freq());
                        }

                        // explicitly exclude offsets here
                        docsAndPositions = termsEnum.DocsAndPositions(null, docsAndPositions, DocsAndPositionsEnum.FLAG_PAYLOADS);
                        Assert.IsNotNull(docsAndPositions);
                        //System.out.println("    doc/freq/pos");
                        while ((doc = docsAndPositions.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            IList<Token> expected = actualTokens[term][docIDToID.Get(doc)];
                            //System.out.println("      doc=" + docIDToID.Get(doc) + " " + expected.Size() + " freq");
                            Assert.IsNotNull(expected);
                            Assert.AreEqual(expected.Count, docsAndPositions.Freq());
                            foreach (Token token in expected)
                            {
                                int pos = Convert.ToInt32(token.Type);
                                //System.out.println("        pos=" + pos);
                                Assert.AreEqual(pos, docsAndPositions.NextPosition());
                            }
                        }

                        docsAndPositionsAndOffsets = termsEnum.DocsAndPositions(null, docsAndPositions);
                        Assert.IsNotNull(docsAndPositionsAndOffsets);
                        //System.out.println("    doc/freq/pos/offs");
                        while ((doc = docsAndPositionsAndOffsets.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            IList<Token> expected = actualTokens[term][docIDToID.Get(doc)];
                            //System.out.println("      doc=" + docIDToID.Get(doc) + " " + expected.Size() + " freq");
                            Assert.IsNotNull(expected);
                            Assert.AreEqual(expected.Count, docsAndPositionsAndOffsets.Freq());
                            foreach (Token token in expected)
                            {
                                int pos = Convert.ToInt32(token.Type);
                                //System.out.println("        pos=" + pos);
                                Assert.AreEqual(pos, docsAndPositionsAndOffsets.NextPosition());
                                Assert.AreEqual(token.StartOffset(), docsAndPositionsAndOffsets.StartOffset());
                                Assert.AreEqual(token.EndOffset(), docsAndPositionsAndOffsets.EndOffset());
                            }
                        }
                    }
                }
                // TODO: test advance:
            }
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestWithUnindexedFields()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, Iwc);
            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                // ensure at least one doc is indexed with offsets
                if (i < 99 && Random().Next(2) == 0)
                {
                    // stored only
                    FieldType ft = new FieldType();
                    ft.Indexed = false;
                    ft.Stored = true;
                    doc.Add(new Field("foo", "boo!", ft));
                }
                else
                {
                    FieldType ft = new FieldType(TextField.TYPE_STORED);
                    ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    if (Random().NextBoolean())
                    {
                        // store some term vectors for the checkindex cross-check
                        ft.StoreTermVectors = true;
                        ft.StoreTermVectorPositions = true;
                        ft.StoreTermVectorOffsets = true;
                    }
                    doc.Add(new Field("foo", "bar", ft));
                }
                riw.AddDocument(doc);
            }
            CompositeReader ir = riw.Reader;
            AtomicReader slow = SlowCompositeReaderWrapper.Wrap(ir);
            FieldInfos fis = slow.FieldInfos;
            Assert.AreEqual(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, fis.FieldInfo("foo").FieldIndexOptions);
            slow.Dispose();
            ir.Dispose();
            riw.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestAddFieldTwice()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            FieldType customType3 = new FieldType(TextField.TYPE_STORED);
            customType3.StoreTermVectors = true;
            customType3.StoreTermVectorPositions = true;
            customType3.StoreTermVectorOffsets = true;
            customType3.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            doc.Add(new Field("content3", "here is more content with aaa aaa aaa", customType3));
            doc.Add(new Field("content3", "here is more content with aaa aaa aaa", customType3));
            iw.AddDocument(doc);
            iw.Dispose();
            dir.Dispose(); // checkindex
        }

        // NOTE: the next two tests aren't that good as we need an EvilToken...
        [Test]
        public virtual void TestNegativeOffsets()
        {
            try
            {
                CheckTokens(new Token[] { MakeToken("foo", 1, -1, -1) });
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                //expected
            }
        }

        [Test]
        public virtual void TestIllegalOffsets()
        {
            try
            {
                CheckTokens(new Token[] { MakeToken("foo", 1, 1, 0) });
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                //expected
            }
        }

        [Test]
        public virtual void TestBackwardsOffsets()
        {
            try
            {
                CheckTokens(new Token[] { MakeToken("foo", 1, 0, 3), MakeToken("foo", 1, 4, 7), MakeToken("foo", 0, 3, 6) });
                Assert.Fail();
            }
            catch (System.ArgumentException expected)
            {
                // expected
            }
        }

        [Test]
        public virtual void TestStackedTokens()
        {
            CheckTokens(new Token[] { MakeToken("foo", 1, 0, 3), MakeToken("foo", 0, 0, 3), MakeToken("foo", 0, 0, 3) });
        }

        [Test]
        public virtual void TestLegalbutVeryLargeOffsets()
        {
            Directory dir = NewDirectory();
            IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            Document doc = new Document();
            Token t1 = new Token("foo", 0, int.MaxValue - 500);
            if (Random().NextBoolean())
            {
                t1.Payload = new BytesRef("test");
            }
            Token t2 = new Token("foo", int.MaxValue - 500, int.MaxValue);
            TokenStream tokenStream = new CannedTokenStream(new Token[] { t1, t2 });
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            // store some term vectors for the checkindex cross-check
            ft.StoreTermVectors = true;
            ft.StoreTermVectorPositions = true;
            ft.StoreTermVectorOffsets = true;
            Field field = new Field("foo", tokenStream, ft);
            doc.Add(field);
            iw.AddDocument(doc);
            iw.Dispose();
            dir.Dispose();
        }

        // TODO: more tests with other possibilities

        private void CheckTokens(Token[] tokens)
        {
            Directory dir = NewDirectory();
            RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, Iwc);
            bool success = false;
            try
            {
                FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
                ft.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                // store some term vectors for the checkindex cross-check
                ft.StoreTermVectors = true;
                ft.StoreTermVectorPositions = true;
                ft.StoreTermVectorOffsets = true;

                Document doc = new Document();
                doc.Add(new Field("body", new CannedTokenStream(tokens), ft));
                riw.AddDocument(doc);
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(riw, dir);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(riw, dir);
                }
            }
        }

        private Token MakeToken(string text, int posIncr, int startOffset, int endOffset)
        {
            Token t = new Token();
            t.Append(text);
            t.PositionIncrement = posIncr;
            t.SetOffset(startOffset, endOffset);
            return t;
        }
    }
}