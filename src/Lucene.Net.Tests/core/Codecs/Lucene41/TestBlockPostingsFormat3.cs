using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene41
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using System.IO;

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
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using FieldType = FieldType;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockFixedLengthPayloadFilter = Lucene.Net.Analysis.MockFixedLengthPayloadFilter;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using MockVariableLengthPayloadFilter = Lucene.Net.Analysis.MockVariableLengthPayloadFilter;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;
    using TokenFilter = Lucene.Net.Analysis.TokenFilter;
    using Tokenizer = Lucene.Net.Analysis.Tokenizer;

    /// <summary>
    /// Tests partial enumeration (only pulling a subset of the indexed data)
    /// </summary>
    [TestFixture]
    public class TestBlockPostingsFormat3 : LuceneTestCase
    {
        internal static readonly int MAXDOC = Lucene41PostingsFormat.BLOCK_SIZE * 20;

        // creates 8 fields with different options and does "duels" of fields against each other
        [Test, LongRunningTest, MaxTime(50000)]
        public virtual void Test()
        {
            Directory dir = NewDirectory();
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, Analyzer.PER_FIELD_REUSE_STRATEGY);
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
            // TODO we could actually add more fields implemented with different PFs
            // or, just put this test into the usual rotation?
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, (IndexWriterConfig)iwc.Clone());
            Document doc = new Document();
            FieldType docsOnlyType = new FieldType(TextField.TYPE_NOT_STORED);
            // turn this on for a cross-check
            docsOnlyType.StoreTermVectors = true;
            docsOnlyType.IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY;

            FieldType docsAndFreqsType = new FieldType(TextField.TYPE_NOT_STORED);
            // turn this on for a cross-check
            docsAndFreqsType.StoreTermVectors = true;
            docsAndFreqsType.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS;

            FieldType positionsType = new FieldType(TextField.TYPE_NOT_STORED);
            // turn these on for a cross-check
            positionsType.StoreTermVectors = true;
            positionsType.StoreTermVectorPositions = true;
            positionsType.StoreTermVectorOffsets = true;
            positionsType.StoreTermVectorPayloads = true;
            FieldType offsetsType = new FieldType(positionsType);
            offsetsType.IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            Field field1 = new Field("field1docs", "", docsOnlyType);
            Field field2 = new Field("field2freqs", "", docsAndFreqsType);
            Field field3 = new Field("field3positions", "", positionsType);
            Field field4 = new Field("field4offsets", "", offsetsType);
            Field field5 = new Field("field5payloadsFixed", "", positionsType);
            Field field6 = new Field("field6payloadsVariable", "", positionsType);
            Field field7 = new Field("field7payloadsFixedOffsets", "", offsetsType);
            Field field8 = new Field("field8payloadsVariableOffsets", "", offsetsType);
            doc.Add(field1);
            doc.Add(field2);
            doc.Add(field3);
            doc.Add(field4);
            doc.Add(field5);
            doc.Add(field6);
            doc.Add(field7);
            doc.Add(field8);
            for (int i = 0; i < MAXDOC; i++)
            {
                string stringValue = Convert.ToString(i) + " verycommon " + English.IntToEnglish(i).Replace('-', ' ') + " " + TestUtil.RandomSimpleString(Random());
                field1.StringValue = stringValue;
                field2.StringValue = stringValue;
                field3.StringValue = stringValue;
                field4.StringValue = stringValue;
                field5.StringValue = stringValue;
                field6.StringValue = stringValue;
                field7.StringValue = stringValue;
                field8.StringValue = stringValue;
                iw.AddDocument(doc);
            }
            iw.Dispose();
            Verify(dir);
            TestUtil.CheckIndex(dir); // for some extra coverage, checkIndex before we forceMerge
            iwc.SetOpenMode(OpenMode_e.APPEND);
            IndexWriter iw2 = new IndexWriter(dir, (IndexWriterConfig)iwc.Clone());
            iw2.ForceMerge(1);
            iw2.Dispose();
            Verify(dir);
            dir.Dispose();
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestBlockPostingsFormat3 OuterInstance;

            public AnalyzerAnonymousInnerClassHelper(TestBlockPostingsFormat3 outerInstance, Analyzer.ReuseStrategy PER_FIELD_REUSE_STRATEGY)
                : base(PER_FIELD_REUSE_STRATEGY)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader);
                if (fieldName.Contains("payloadsFixed"))
                {
                    TokenFilter filter = new MockFixedLengthPayloadFilter(new Random(0), tokenizer, 1);
                    return new TokenStreamComponents(tokenizer, filter);
                }
                else if (fieldName.Contains("payloadsVariable"))
                {
                    TokenFilter filter = new MockVariableLengthPayloadFilter(new Random(0), tokenizer);
                    return new TokenStreamComponents(tokenizer, filter);
                }
                else
                {
                    return new TokenStreamComponents(tokenizer);
                }
            }
        }

        private void Verify(Directory dir)
        {
            DirectoryReader ir = DirectoryReader.Open(dir);
            foreach (AtomicReaderContext leaf in ir.Leaves)
            {
                AtomicReader leafReader = (AtomicReader)leaf.Reader;
                AssertTerms(leafReader.Terms("field1docs"), leafReader.Terms("field2freqs"), true);
                AssertTerms(leafReader.Terms("field3positions"), leafReader.Terms("field4offsets"), true);
                AssertTerms(leafReader.Terms("field4offsets"), leafReader.Terms("field5payloadsFixed"), true);
                AssertTerms(leafReader.Terms("field5payloadsFixed"), leafReader.Terms("field6payloadsVariable"), true);
                AssertTerms(leafReader.Terms("field6payloadsVariable"), leafReader.Terms("field7payloadsFixedOffsets"), true);
                AssertTerms(leafReader.Terms("field7payloadsFixedOffsets"), leafReader.Terms("field8payloadsVariableOffsets"), true);
            }
            ir.Dispose();
        }

        // following code is almost an exact dup of code from TestDuelingCodecs: sorry!

        public virtual void AssertTerms(Terms leftTerms, Terms rightTerms, bool deep)
        {
            if (leftTerms == null || rightTerms == null)
            {
                Assert.IsNull(leftTerms);
                Assert.IsNull(rightTerms);
                return;
            }
            AssertTermsStatistics(leftTerms, rightTerms);

            // NOTE: we don't assert hasOffsets/hasPositions/hasPayloads because they are allowed to be different

            TermsEnum leftTermsEnum = leftTerms.Iterator(null);
            TermsEnum rightTermsEnum = rightTerms.Iterator(null);
            AssertTermsEnum(leftTermsEnum, rightTermsEnum, true);

            AssertTermsSeeking(leftTerms, rightTerms);

            if (deep)
            {
                int numIntersections = AtLeast(3);
                for (int i = 0; i < numIntersections; i++)
                {
                    string re = AutomatonTestUtil.RandomRegexp(Random());
                    CompiledAutomaton automaton = new CompiledAutomaton((new RegExp(re, RegExp.NONE)).ToAutomaton());
                    if (automaton.Type == CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
                    {
                        // TODO: test start term too
                        TermsEnum leftIntersection = leftTerms.Intersect(automaton, null);
                        TermsEnum rightIntersection = rightTerms.Intersect(automaton, null);
                        AssertTermsEnum(leftIntersection, rightIntersection, Rarely());
                    }
                }
            }
        }

        private void AssertTermsSeeking(Terms leftTerms, Terms rightTerms)
        {
            TermsEnum leftEnum = null;
            TermsEnum rightEnum = null;

            // just an upper bound
            int numTests = AtLeast(20);
            Random random = Random();

            // collect this number of terms from the left side
            HashSet<BytesRef> tests = new HashSet<BytesRef>();
            int numPasses = 0;
            while (numPasses < 10 && tests.Count < numTests)
            {
                leftEnum = leftTerms.Iterator(leftEnum);
                BytesRef term = null;
                while ((term = leftEnum.Next()) != null)
                {
                    int code = random.Next(10);
                    if (code == 0)
                    {
                        // the term
                        tests.Add(BytesRef.DeepCopyOf(term));
                    }
                    else if (code == 1)
                    {
                        // truncated subsequence of term
                        term = BytesRef.DeepCopyOf(term);
                        if (term.Length > 0)
                        {
                            // truncate it
                            term.Length = random.Next(term.Length);
                        }
                    }
                    else if (code == 2)
                    {
                        // term, but ensure a non-zero offset
                        var newbytes = new byte[term.Length + 5];
                        Array.Copy(term.Bytes, term.Offset, newbytes, 5, term.Length);
                        tests.Add(new BytesRef(newbytes, 5, term.Length));
                    }
                }
                numPasses++;
            }

            List<BytesRef> shuffledTests = new List<BytesRef>(tests);
            shuffledTests = (List<BytesRef>)CollectionsHelper.Shuffle(shuffledTests);

            foreach (BytesRef b in shuffledTests)
            {
                leftEnum = leftTerms.Iterator(leftEnum);
                rightEnum = rightTerms.Iterator(rightEnum);

                Assert.AreEqual(leftEnum.SeekExact(b), rightEnum.SeekExact(b));
                Assert.AreEqual(leftEnum.SeekExact(b), rightEnum.SeekExact(b));

                SeekStatus leftStatus;
                SeekStatus rightStatus;

                leftStatus = leftEnum.SeekCeil(b);
                rightStatus = rightEnum.SeekCeil(b);
                Assert.AreEqual(leftStatus, rightStatus);
                if (leftStatus != SeekStatus.END)
                {
                    Assert.AreEqual(leftEnum.Term(), rightEnum.Term());
                }

                leftStatus = leftEnum.SeekCeil(b);
                rightStatus = rightEnum.SeekCeil(b);
                Assert.AreEqual(leftStatus, rightStatus);
                if (leftStatus != SeekStatus.END)
                {
                    Assert.AreEqual(leftEnum.Term(), rightEnum.Term());
                }
            }
        }

        /// <summary>
        /// checks collection-level statistics on Terms
        /// </summary>
        public virtual void AssertTermsStatistics(Terms leftTerms, Terms rightTerms)
        {
            Debug.Assert(leftTerms.Comparator == rightTerms.Comparator);
            if (leftTerms.DocCount != -1 && rightTerms.DocCount != -1)
            {
                Assert.AreEqual(leftTerms.DocCount, rightTerms.DocCount);
            }
            if (leftTerms.SumDocFreq != -1 && rightTerms.SumDocFreq != -1)
            {
                Assert.AreEqual(leftTerms.SumDocFreq, rightTerms.SumDocFreq);
            }
            if (leftTerms.SumTotalTermFreq != -1 && rightTerms.SumTotalTermFreq != -1)
            {
                Assert.AreEqual(leftTerms.SumTotalTermFreq, rightTerms.SumTotalTermFreq);
            }
            if (leftTerms.Size() != -1 && rightTerms.Size() != -1)
            {
                Assert.AreEqual(leftTerms.Size(), rightTerms.Size());
            }
        }

        /// <summary>
        /// checks the terms enum sequentially
        /// if deep is false, it does a 'shallow' test that doesnt go down to the docsenums
        /// </summary>
        public virtual void AssertTermsEnum(TermsEnum leftTermsEnum, TermsEnum rightTermsEnum, bool deep)
        {
            BytesRef term;
            Bits randomBits = new RandomBits(MAXDOC, Random().NextDouble(), Random());
            DocsAndPositionsEnum leftPositions = null;
            DocsAndPositionsEnum rightPositions = null;
            DocsEnum leftDocs = null;
            DocsEnum rightDocs = null;

            while ((term = leftTermsEnum.Next()) != null)
            {
                Assert.AreEqual(term, rightTermsEnum.Next());
                AssertTermStats(leftTermsEnum, rightTermsEnum);
                if (deep)
                {
                    // with payloads + off
                    AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions));
                    AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions));

                    AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions));
                    AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions));
                    // with payloads only
                    AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));
                    AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));

                    AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));
                    AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));

                    // with offsets only
                    AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));
                    AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));

                    AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));
                    AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));

                    // with positions only
                    AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions, DocsEnum.FLAG_NONE));
                    AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions, DocsEnum.FLAG_NONE));

                    AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions, DocsEnum.FLAG_NONE));
                    AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions(randomBits, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions, DocsEnum.FLAG_NONE));

                    // with freqs:
                    AssertDocsEnum(leftDocs = leftTermsEnum.Docs(null, leftDocs), rightDocs = rightTermsEnum.Docs(null, rightDocs));
                    AssertDocsEnum(leftDocs = leftTermsEnum.Docs(randomBits, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs));

                    // w/o freqs:
                    AssertDocsEnum(leftDocs = leftTermsEnum.Docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsEnum.FLAG_NONE));
                    AssertDocsEnum(leftDocs = leftTermsEnum.Docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs, DocsEnum.FLAG_NONE));

                    // with freqs:
                    AssertDocsSkipping(leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(null, leftDocs), rightDocs = rightTermsEnum.Docs(null, rightDocs));
                    AssertDocsSkipping(leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(randomBits, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs));

                    // w/o freqs:
                    AssertDocsSkipping(leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsEnum.FLAG_NONE));
                    AssertDocsSkipping(leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs, DocsEnum.FLAG_NONE));
                }
            }
            Assert.IsNull(rightTermsEnum.Next());
        }

        /// <summary>
        /// checks term-level statistics
        /// </summary>
        public virtual void AssertTermStats(TermsEnum leftTermsEnum, TermsEnum rightTermsEnum)
        {
            Assert.AreEqual(leftTermsEnum.DocFreq(), rightTermsEnum.DocFreq());
            if (leftTermsEnum.TotalTermFreq() != -1 && rightTermsEnum.TotalTermFreq() != -1)
            {
                Assert.AreEqual(leftTermsEnum.TotalTermFreq(), rightTermsEnum.TotalTermFreq());
            }
        }

        /// <summary>
        /// checks docs + freqs + positions + payloads, sequentially
        /// </summary>
        public virtual void AssertDocsAndPositionsEnum(DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
        {
            if (leftDocs == null || rightDocs == null)
            {
                Assert.IsNull(leftDocs);
                Assert.IsNull(rightDocs);
                return;
            }
            Assert.AreEqual(-1, leftDocs.DocID());
            Assert.AreEqual(-1, rightDocs.DocID());
            int docid;
            while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(docid, rightDocs.NextDoc());
                int freq = leftDocs.Freq();
                Assert.AreEqual(freq, rightDocs.Freq());
                for (int i = 0; i < freq; i++)
                {
                    Assert.AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition());
                    // we don't assert offsets/payloads, they are allowed to be different
                }
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc());
        }

        /// <summary>
        /// checks docs + freqs, sequentially
        /// </summary>
        public virtual void AssertDocsEnum(DocsEnum leftDocs, DocsEnum rightDocs)
        {
            if (leftDocs == null)
            {
                Assert.IsNull(rightDocs);
                return;
            }
            Assert.AreEqual(-1, leftDocs.DocID());
            Assert.AreEqual(-1, rightDocs.DocID());
            int docid;
            while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.AreEqual(docid, rightDocs.NextDoc());
                // we don't assert freqs, they are allowed to be different
            }
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc());
        }

        /// <summary>
        /// checks advancing docs
        /// </summary>
        public virtual void AssertDocsSkipping(int docFreq, DocsEnum leftDocs, DocsEnum rightDocs)
        {
            if (leftDocs == null)
            {
                Assert.IsNull(rightDocs);
                return;
            }
            int docid = -1;
            int averageGap = MAXDOC / (1 + docFreq);
            int skipInterval = 16;

            while (true)
            {
                if (Random().NextBoolean())
                {
                    // nextDoc()
                    docid = leftDocs.NextDoc();
                    Assert.AreEqual(docid, rightDocs.NextDoc());
                }
                else
                {
                    // advance()
                    int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random().NextDouble() * averageGap));
                    docid = leftDocs.Advance(skip);
                    Assert.AreEqual(docid, rightDocs.Advance(skip));
                }

                if (docid == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return;
                }
                // we don't assert freqs, they are allowed to be different
            }
        }

        /// <summary>
        /// checks advancing docs + positions
        /// </summary>
        public virtual void AssertPositionsSkipping(int docFreq, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
        {
            if (leftDocs == null || rightDocs == null)
            {
                Assert.IsNull(leftDocs);
                Assert.IsNull(rightDocs);
                return;
            }

            int docid = -1;
            int averageGap = MAXDOC / (1 + docFreq);
            int skipInterval = 16;

            while (true)
            {
                if (Random().NextBoolean())
                {
                    // nextDoc()
                    docid = leftDocs.NextDoc();
                    Assert.AreEqual(docid, rightDocs.NextDoc());
                }
                else
                {
                    // advance()
                    int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random().NextDouble() * averageGap));
                    docid = leftDocs.Advance(skip);
                    Assert.AreEqual(docid, rightDocs.Advance(skip));
                }

                if (docid == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return;
                }
                int freq = leftDocs.Freq();
                Assert.AreEqual(freq, rightDocs.Freq());
                for (int i = 0; i < freq; i++)
                {
                    Assert.AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition());
                    // we don't compare the payloads, its allowed that one is empty etc
                }
            }
        }

        private class RandomBits : Bits
        {
            internal FixedBitSet Bits;

            internal RandomBits(int maxDoc, double pctLive, Random random)
            {
                Bits = new FixedBitSet(maxDoc);
                for (int i = 0; i < maxDoc; i++)
                {
                    if (random.NextDouble() <= pctLive)
                    {
                        Bits.Set(i);
                    }
                }
            }

            public bool Get(int index)
            {
                return Bits.Get(index);
            }

            public int Length()
            {
                return Bits.Length();
            }
        }
    }
}