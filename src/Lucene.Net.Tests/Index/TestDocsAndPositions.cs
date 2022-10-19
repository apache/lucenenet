using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Text;
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IBits = Lucene.Net.Util.IBits;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestDocsAndPositions : LuceneTestCase
    {
        private string fieldName;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            fieldName = "field" + Random.Next();
        }

        /// <summary>
        /// Simple testcase for <seealso cref="DocsAndPositionsEnum"/>
        /// </summary>
        [Test]
        public virtual void TestPositionsSimple()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            for (int i = 0; i < 39; i++)
            {
                Document doc = new Document();
                FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
                customType.OmitNorms = true;
                doc.Add(NewField(fieldName, "1 2 3 4 5 6 7 8 9 10 " + "1 2 3 4 5 6 7 8 9 10 " + "1 2 3 4 5 6 7 8 9 10 " + "1 2 3 4 5 6 7 8 9 10", customType));
                writer.AddDocument(doc);
            }
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            int num = AtLeast(13);
            for (int i = 0; i < num; i++)
            {
                BytesRef bytes = new BytesRef("1");
                IndexReaderContext topReaderContext = reader.Context;
                foreach (AtomicReaderContext atomicReaderContext in topReaderContext.Leaves)
                {
                    DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions((AtomicReader)atomicReaderContext.Reader, bytes, null);
                    Assert.IsNotNull(docsAndPosEnum);
                    if (atomicReaderContext.Reader.MaxDoc == 0)
                    {
                        continue;
                    }
                    int advance = docsAndPosEnum.Advance(Random.Next(atomicReaderContext.Reader.MaxDoc));
                    do
                    {
                        string msg = "Advanced to: " + advance + " current doc: " + docsAndPosEnum.DocID; // TODO: + " usePayloads: " + usePayload;
                        Assert.AreEqual(4, docsAndPosEnum.Freq, msg);
                        Assert.AreEqual(0, docsAndPosEnum.NextPosition(), msg);
                        Assert.AreEqual(4, docsAndPosEnum.Freq, msg);
                        Assert.AreEqual(10, docsAndPosEnum.NextPosition(), msg);
                        Assert.AreEqual(4, docsAndPosEnum.Freq, msg);
                        Assert.AreEqual(20, docsAndPosEnum.NextPosition(), msg);
                        Assert.AreEqual(4, docsAndPosEnum.Freq, msg);
                        Assert.AreEqual(30, docsAndPosEnum.NextPosition(), msg);
                    } while (docsAndPosEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                }
            }
            reader.Dispose();
            directory.Dispose();
        }

        public virtual DocsAndPositionsEnum GetDocsAndPositions(AtomicReader reader, BytesRef bytes, IBits liveDocs)
        {
            Terms terms = reader.GetTerms(fieldName);
            if (terms != null)
            {
                TermsEnum te = terms.GetEnumerator();
                if (te.SeekExact(bytes))
                {
                    return te.DocsAndPositions(liveDocs, null);
                }
            }
            return null;
        }

        /// <summary>
        /// this test indexes random numbers within a range into a field and checks
        /// their occurrences by searching for a number from that range selected at
        /// random. All positions for that number are saved up front and compared to
        /// the enums positions.
        /// </summary>
        [Test]
        public virtual void TestRandomPositions()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            int numDocs = AtLeast(47);
            int max = 1051;
            int term = Random.Next(max);
            int[][] positionsInDoc = new int[numDocs][];
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                JCG.List<int> positions = new JCG.List<int>();
                StringBuilder builder = new StringBuilder();
                int num = AtLeast(131);
                for (int j = 0; j < num; j++)
                {
                    int nextInt = Random.Next(max);
                    builder.Append(nextInt).Append(' ');
                    if (nextInt == term)
                    {
                        positions.Add(Convert.ToInt32(j));
                    }
                }
                if (positions.Count == 0)
                {
                    builder.Append(term);
                    positions.Add(num);
                }
                doc.Add(NewField(fieldName, builder.ToString(), customType));
                positionsInDoc[i] = positions.ToArray();
                writer.AddDocument(doc);
            }

            IndexReader reader = writer.GetReader();
            writer.Dispose();

            int num_ = AtLeast(13);
            for (int i = 0; i < num_; i++)
            {
                BytesRef bytes = new BytesRef("" + term);
                IndexReaderContext topReaderContext = reader.Context;
                foreach (AtomicReaderContext atomicReaderContext in topReaderContext.Leaves)
                {
                    DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions((AtomicReader)atomicReaderContext.Reader, bytes, null);
                    Assert.IsNotNull(docsAndPosEnum);
                    int initDoc = 0;
                    int maxDoc = atomicReaderContext.Reader.MaxDoc;
                    // initially advance or do next doc
                    if (Random.NextBoolean())
                    {
                        initDoc = docsAndPosEnum.NextDoc();
                    }
                    else
                    {
                        initDoc = docsAndPosEnum.Advance(Random.Next(maxDoc));
                    }
                    // now run through the scorer and check if all positions are there...
                    do
                    {
                        int docID = docsAndPosEnum.DocID;
                        if (docID == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }
                        int[] pos = positionsInDoc[atomicReaderContext.DocBase + docID];
                        Assert.AreEqual(pos.Length, docsAndPosEnum.Freq);
                        // number of positions read should be random - don't read all of them
                        // allways
                        int howMany = Random.Next(20) == 0 ? pos.Length - Random.Next(pos.Length) : pos.Length;
                        for (int j = 0; j < howMany; j++)
                        {
                            Assert.AreEqual(pos[j], docsAndPosEnum.NextPosition(), "iteration: " + i + " initDoc: " + initDoc + " doc: " + docID + " base: " + atomicReaderContext.DocBase + " positions: " + pos); /* TODO: + " usePayloads: "
                            + usePayload*/
                        }

                        if (Random.Next(10) == 0) // once is a while advance
                        {
                            if (docsAndPosEnum.Advance(docID + 1 + Random.Next((maxDoc - docID))) == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                        }
                    } while (docsAndPosEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                }
            }
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandomDocs()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            int numDocs = AtLeast(49);
            int max = 15678;
            int term = Random.Next(max);
            int[] freqInDoc = new int[numDocs];
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                StringBuilder builder = new StringBuilder();
                for (int j = 0; j < 199; j++)
                {
                    int nextInt = Random.Next(max);
                    builder.Append(nextInt).Append(' ');
                    if (nextInt == term)
                    {
                        freqInDoc[i]++;
                    }
                }
                doc.Add(NewField(fieldName, builder.ToString(), customType));
                writer.AddDocument(doc);
            }

            IndexReader reader = writer.GetReader();
            writer.Dispose();

            int num = AtLeast(13);
            for (int i = 0; i < num; i++)
            {
                BytesRef bytes = new BytesRef("" + term);
                IndexReaderContext topReaderContext = reader.Context;
                foreach (AtomicReaderContext context in topReaderContext.Leaves)
                {
                    int maxDoc = context.AtomicReader.MaxDoc;
                    DocsEnum docsEnum = TestUtil.Docs(Random, context.Reader, fieldName, bytes, null, null, DocsFlags.FREQS);
                    if (FindNext(freqInDoc, context.DocBase, context.DocBase + maxDoc) == int.MaxValue)
                    {
                        Assert.IsNull(docsEnum);
                        continue;
                    }
                    Assert.IsNotNull(docsEnum);
                    docsEnum.NextDoc();
                    for (int j = 0; j < maxDoc; j++)
                    {
                        if (freqInDoc[context.DocBase + j] != 0)
                        {
                            Assert.AreEqual(j, docsEnum.DocID);
                            Assert.AreEqual(docsEnum.Freq, freqInDoc[context.DocBase + j]);
                            if (i % 2 == 0 && Random.Next(10) == 0)
                            {
                                int next = FindNext(freqInDoc, context.DocBase + j + 1, context.DocBase + maxDoc) - context.DocBase;
                                int advancedTo = docsEnum.Advance(next);
                                if (next >= maxDoc)
                                {
                                    Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, advancedTo);
                                }
                                else
                                {
                                    Assert.IsTrue(next >= advancedTo, "advanced to: " + advancedTo + " but should be <= " + next);
                                }
                            }
                            else
                            {
                                docsEnum.NextDoc();
                            }
                        }
                    }
                    Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.DocID, "DocBase: " + context.DocBase + " maxDoc: " + maxDoc + " " + docsEnum.GetType());
                }
            }

            reader.Dispose();
            dir.Dispose();
        }

        private static int FindNext(int[] docs, int pos, int max)
        {
            for (int i = pos; i < max; i++)
            {
                if (docs[i] != 0)
                {
                    return i;
                }
            }
            return int.MaxValue;
        }

        /// <summary>
        /// tests retrieval of positions for terms that have a large number of
        /// occurrences to force test of buffer refill during positions iteration.
        /// </summary>
        [Test]
        public virtual void TestLargeNumberOfPositions()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            int howMany = 1000;
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            for (int i = 0; i < 39; i++)
            {
                Document doc = new Document();
                StringBuilder builder = new StringBuilder();
                for (int j = 0; j < howMany; j++)
                {
                    if (j % 2 == 0)
                    {
                        builder.Append("even ");
                    }
                    else
                    {
                        builder.Append("odd ");
                    }
                }
                doc.Add(NewField(fieldName, builder.ToString(), customType));
                writer.AddDocument(doc);
            }

            // now do searches
            IndexReader reader = writer.GetReader();
            writer.Dispose();

            int num = AtLeast(13);
            for (int i = 0; i < num; i++)
            {
                BytesRef bytes = new BytesRef("even");

                IndexReaderContext topReaderContext = reader.Context;
                foreach (AtomicReaderContext atomicReaderContext in topReaderContext.Leaves)
                {
                    DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions((AtomicReader)atomicReaderContext.Reader, bytes, null);
                    Assert.IsNotNull(docsAndPosEnum);

                    int initDoc = 0;
                    int maxDoc = atomicReaderContext.Reader.MaxDoc;
                    // initially advance or do next doc
                    if (Random.NextBoolean())
                    {
                        initDoc = docsAndPosEnum.NextDoc();
                    }
                    else
                    {
                        initDoc = docsAndPosEnum.Advance(Random.Next(maxDoc));
                    }
                    string msg = "Iteration: " + i + " initDoc: " + initDoc; // TODO: + " payloads: " + usePayload;
                    Assert.AreEqual(howMany / 2, docsAndPosEnum.Freq);
                    for (int j = 0; j < howMany; j += 2)
                    {
                        Assert.AreEqual(j, docsAndPosEnum.NextPosition(), "position missmatch index: " + j + " with freq: " + docsAndPosEnum.Freq + " -- " + msg);
                    }
                }
            }
            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDocsEnumStart()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewStringField("foo", "bar", Field.Store.NO));
            writer.AddDocument(doc);
            DirectoryReader reader = writer.GetReader();
            AtomicReader r = GetOnlySegmentReader(reader);
            DocsEnum disi = TestUtil.Docs(Random, r, "foo", new BytesRef("bar"), null, null, DocsFlags.NONE);
            int docid = disi.DocID;
            Assert.AreEqual(-1, docid);
            Assert.IsTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            // now reuse and check again
            TermsEnum te = r.GetTerms("foo").GetEnumerator();
            Assert.IsTrue(te.SeekExact(new BytesRef("bar")));
            disi = TestUtil.Docs(Random, te, null, disi, DocsFlags.NONE);
            docid = disi.DocID;
            Assert.AreEqual(-1, docid);
            Assert.IsTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            writer.Dispose();
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDocsAndPositionsEnumStart()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewTextField("foo", "bar", Field.Store.NO));
            writer.AddDocument(doc);
            DirectoryReader reader = writer.GetReader();
            AtomicReader r = GetOnlySegmentReader(reader);
            DocsAndPositionsEnum disi = r.GetTermPositionsEnum(new Term("foo", "bar"));
            int docid = disi.DocID;
            Assert.AreEqual(-1, docid);
            Assert.IsTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            // now reuse and check again
            TermsEnum te = r.GetTerms("foo").GetEnumerator();
            Assert.IsTrue(te.SeekExact(new BytesRef("bar")));
            disi = te.DocsAndPositions(null, disi);
            docid = disi.DocID;
            Assert.AreEqual(-1, docid);
            Assert.IsTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            writer.Dispose();
            r.Dispose();
            dir.Dispose();
        }
    }
}