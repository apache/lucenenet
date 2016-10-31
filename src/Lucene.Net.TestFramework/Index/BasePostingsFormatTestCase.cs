using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using Constants = Lucene.Net.Util.Constants;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
    using Field = Field;
    using FieldsConsumer = Lucene.Net.Codecs.FieldsConsumer;
    using FieldsProducer = Lucene.Net.Codecs.FieldsProducer;
    using FieldType = FieldType;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using FlushInfo = Lucene.Net.Store.FlushInfo;
    using IOContext = Lucene.Net.Store.IOContext;
    using PostingsConsumer = Lucene.Net.Codecs.PostingsConsumer;
    using TermsConsumer = Lucene.Net.Codecs.TermsConsumer;
    using TermStats = Lucene.Net.Codecs.TermStats;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Abstract class to do basic tests for a postings format.
    /// NOTE: this test focuses on the postings
    /// (docs/freqs/positions/payloads/offsets) impl, not the
    /// terms dict.  The [stretch] goal is for this test to be
    /// so thorough in testing a new PostingsFormat that if this
    /// test passes, then all Lucene/Solr tests should also pass.  Ie,
    /// if there is some bug in a given PostingsFormat that this
    /// test fails to catch then this test needs to be improved!
    /// </summary>

    // TODO can we make it easy for testing to pair up a "random terms dict impl" with your postings base format...

    // TODO test when you reuse after skipping a term or two, eg the block reuse case

    /* TODO
      - threads
      - assert doc=-1 before any nextDoc
      - if a PF passes this test but fails other tests then this
        test has a bug!!
      - test tricky reuse cases, eg across fields
      - verify you get null if you pass needFreq/needOffset but
        they weren't indexed
    */

    [TestFixture]
    public abstract class BasePostingsFormatTestCase : BaseIndexFileFormatTestCase
    {
        private enum Option
        {
            // Sometimes use .Advance():
            SKIPPING,

            // Sometimes reuse the Docs/AndPositionsEnum across terms:
            REUSE_ENUMS,

            // Sometimes pass non-null live docs:
            LIVE_DOCS,

            // Sometimes seek to term using previously saved TermState:
            TERM_STATE,

            // Sometimes don't fully consume docs from the enum
            PARTIAL_DOC_CONSUME,

            // Sometimes don't fully consume positions at each doc
            PARTIAL_POS_CONSUME,

            // Sometimes check payloads
            PAYLOADS,

            // Test w/ multiple threads
            THREADS
        }

        /// <summary>
        /// Given the same random seed this always enumerates the
        ///  same random postings
        /// </summary>
        private class SeedPostings : DocsAndPositionsEnum
        {
            // Used only to generate docIDs; this way if you pull w/
            // or w/o positions you get the same docID sequence:
            internal readonly Random DocRandom;

            internal readonly Random Random;
            public int DocFreq;
            internal readonly int MaxDocSpacing;
            internal readonly int PayloadSize;
            internal readonly bool FixedPayloads;
            internal readonly Bits LiveDocs;
            internal readonly BytesRef Payload_Renamed;
            internal readonly FieldInfo.IndexOptions Options;
            internal readonly bool DoPositions;

            internal int DocID_Renamed;
            internal int Freq_Renamed;
            public int Upto;

            internal int Pos;
            internal int Offset;
            internal int StartOffset_Renamed;
            internal int EndOffset_Renamed;
            internal int PosSpacing;
            internal int PosUpto;

            public SeedPostings(long seed, int minDocFreq, int maxDocFreq, Bits liveDocs, FieldInfo.IndexOptions options)
            {
                Random = new Random((int)seed);
                DocRandom = new Random(Random.Next());
                DocFreq = TestUtil.NextInt(Random, minDocFreq, maxDocFreq);
                this.LiveDocs = liveDocs;

                // TODO: more realistic to inversely tie this to numDocs:
                MaxDocSpacing = TestUtil.NextInt(Random, 1, 100);

                if (Random.Next(10) == 7)
                {
                    // 10% of the time create big payloads:
                    PayloadSize = 1 + Random.Next(3);
                }
                else
                {
                    PayloadSize = 1 + Random.Next(1);
                }

                FixedPayloads = Random.NextBoolean();
                var payloadBytes = new byte[PayloadSize];
                Payload_Renamed = new BytesRef(payloadBytes);
                this.Options = options;
                DoPositions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS.CompareTo(options) <= 0;
            }

            public override int NextDoc()
            {
                while (true)
                {
                    _nextDoc();
                    if (LiveDocs == null || DocID_Renamed == NO_MORE_DOCS || LiveDocs.Get(DocID_Renamed))
                    {
                        return DocID_Renamed;
                    }
                }
            }

            internal virtual int _nextDoc()
            {
                // Must consume random:
                while (PosUpto < Freq_Renamed)
                {
                    NextPosition();
                }

                if (Upto < DocFreq)
                {
                    if (Upto == 0 && DocRandom.NextBoolean())
                    {
                        // Sometimes index docID = 0
                    }
                    else if (MaxDocSpacing == 1)
                    {
                        DocID_Renamed++;
                    }
                    else
                    {
                        // TODO: sometimes have a biggish gap here!
                        DocID_Renamed += TestUtil.NextInt(DocRandom, 1, MaxDocSpacing);
                    }

                    if (Random.Next(200) == 17)
                    {
                        Freq_Renamed = TestUtil.NextInt(Random, 1, 1000);
                    }
                    else if (Random.Next(10) == 17)
                    {
                        Freq_Renamed = TestUtil.NextInt(Random, 1, 20);
                    }
                    else
                    {
                        Freq_Renamed = TestUtil.NextInt(Random, 1, 4);
                    }

                    Pos = 0;
                    Offset = 0;
                    PosUpto = 0;
                    PosSpacing = TestUtil.NextInt(Random, 1, 100);

                    Upto++;
                    return DocID_Renamed;
                }
                else
                {
                    return DocID_Renamed = NO_MORE_DOCS;
                }
            }

            public override int DocID()
            {
                return DocID_Renamed;
            }

            public override int Freq()
            {
                return Freq_Renamed;
            }

            public override int NextPosition()
            {
                if (!DoPositions)
                {
                    PosUpto = Freq_Renamed;
                    return 0;
                }
                Debug.Assert(PosUpto < Freq_Renamed);

                if (PosUpto == 0 && Random.NextBoolean())
                {
                    // Sometimes index pos = 0
                }
                else if (PosSpacing == 1)
                {
                    Pos++;
                }
                else
                {
                    Pos += TestUtil.NextInt(Random, 1, PosSpacing);
                }

                if (PayloadSize != 0)
                {
                    if (FixedPayloads)
                    {
                        Payload_Renamed.Length = PayloadSize;
                        Random.NextBytes((byte[])(Array)Payload_Renamed.Bytes);
                    }
                    else
                    {
                        int thisPayloadSize = Random.Next(PayloadSize);
                        if (thisPayloadSize != 0)
                        {
                            Payload_Renamed.Length = PayloadSize;
                            Random.NextBytes((byte[])(Array)Payload_Renamed.Bytes);
                        }
                        else
                        {
                            Payload_Renamed.Length = 0;
                        }
                    }
                }
                else
                {
                    Payload_Renamed.Length = 0;
                }

                StartOffset_Renamed = Offset + Random.Next(5);
                EndOffset_Renamed = StartOffset_Renamed + Random.Next(10);
                Offset = EndOffset_Renamed;

                PosUpto++;
                return Pos;
            }

            public override int StartOffset()
            {
                return StartOffset_Renamed;
            }

            public override int EndOffset()
            {
                return EndOffset_Renamed;
            }

            public override BytesRef Payload
            {
                get
                {
                    return Payload_Renamed.Length == 0 ? null : Payload_Renamed;
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public override long Cost()
            {
                return DocFreq;
            }
        }

        private class FieldAndTerm
        {
            internal string Field;
            internal BytesRef Term;

            public FieldAndTerm(string field, BytesRef term)
            {
                this.Field = field;
                this.Term = BytesRef.DeepCopyOf(term);
            }
        }

        // Holds all postings:
        private static SortedDictionary<string, SortedDictionary<BytesRef, long>> Fields;

        private static FieldInfos FieldInfos;

        private static FixedBitSet GlobalLiveDocs;

        private static IList<FieldAndTerm> AllTerms;
        private static int MaxDoc;

        private static long TotalPostings;
        private static long TotalPayloadBytes;

        private static SeedPostings GetSeedPostings(string term, long seed, bool withLiveDocs, FieldInfo.IndexOptions options)
        {
            int minDocFreq, maxDocFreq;
            if (term.StartsWith("big_"))
            {
                minDocFreq = RANDOM_MULTIPLIER * 50000;
                maxDocFreq = RANDOM_MULTIPLIER * 70000;
            }
            else if (term.StartsWith("medium_"))
            {
                minDocFreq = RANDOM_MULTIPLIER * 3000;
                maxDocFreq = RANDOM_MULTIPLIER * 6000;
            }
            else if (term.StartsWith("low_"))
            {
                minDocFreq = RANDOM_MULTIPLIER;
                maxDocFreq = RANDOM_MULTIPLIER * 40;
            }
            else
            {
                minDocFreq = 1;
                maxDocFreq = 3;
            }

            return new SeedPostings(seed, minDocFreq, maxDocFreq, withLiveDocs ? GlobalLiveDocs : null, options);
        }

        [OneTimeSetUp]
        public static void CreatePostings()
        {
            TotalPostings = 0;
            TotalPayloadBytes = 0;
            Fields = new SortedDictionary<string, SortedDictionary<BytesRef, long>>();

            int numFields = TestUtil.NextInt(Random(), 1, 5);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: " + numFields + " fields");
            }
            MaxDoc = 0;

            FieldInfo[] fieldInfoArray = new FieldInfo[numFields];
            int fieldUpto = 0;
            while (fieldUpto < numFields)
            {
                string field = TestUtil.RandomSimpleString(Random());
                if (Fields.ContainsKey(field))
                {
                    continue;
                }

                fieldInfoArray[fieldUpto] = new FieldInfo(field, true, fieldUpto, false, false, true, FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, null, DocValuesType.NUMERIC, null);
                fieldUpto++;

                SortedDictionary<BytesRef, long> postings = new SortedDictionary<BytesRef, long>();
                Fields[field] = postings;
                HashSet<string> seenTerms = new HashSet<string>();

                int numTerms;
                if (Random().Next(10) == 7)
                {
                    numTerms = AtLeast(50);
                }
                else
                {
                    numTerms = TestUtil.NextInt(Random(), 2, 20);
                }

                for (int termUpto = 0; termUpto < numTerms; termUpto++)
                {
                    string term = TestUtil.RandomSimpleString(Random());
                    if (seenTerms.Contains(term))
                    {
                        continue;
                    }
                    seenTerms.Add(term);

                    if (TEST_NIGHTLY && termUpto == 0 && fieldUpto == 1)
                    {
                        // Make 1 big term:
                        term = "big_" + term;
                    }
                    else if (termUpto == 1 && fieldUpto == 1)
                    {
                        // Make 1 medium term:
                        term = "medium_" + term;
                    }
                    else if (Random().NextBoolean())
                    {
                        // Low freq term:
                        term = "low_" + term;
                    }
                    else
                    {
                        // Very low freq term (don't multiply by RANDOM_MULTIPLIER):
                        term = "verylow_" + term;
                    }

                    long termSeed = Random().NextLong();
                    postings[new BytesRef(term)] = termSeed;

                    // NOTE: sort of silly: we enum all the docs just to
                    // get the maxDoc
                    DocsEnum docsEnum = GetSeedPostings(term, termSeed, false, FieldInfo.IndexOptions.DOCS_ONLY);
                    int doc;
                    int lastDoc = 0;
                    while ((doc = docsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                    {
                        lastDoc = doc;
                    }
                    MaxDoc = Math.Max(lastDoc, MaxDoc);
                }
            }

            FieldInfos = new FieldInfos(fieldInfoArray);

            // It's the count, not the last docID:
            MaxDoc++;

            GlobalLiveDocs = new FixedBitSet(MaxDoc);
            double liveRatio = Random().NextDouble();
            for (int i = 0; i < MaxDoc; i++)
            {
                if (Random().NextDouble() <= liveRatio)
                {
                    GlobalLiveDocs.Set(i);
                }
            }

            AllTerms = new List<FieldAndTerm>();
            foreach (KeyValuePair<string, SortedDictionary<BytesRef, long>> fieldEnt in Fields)
            {
                string field = fieldEnt.Key;
                foreach (KeyValuePair<BytesRef, long> termEnt in fieldEnt.Value.EntrySet())
                {
                    AllTerms.Add(new FieldAndTerm(field, termEnt.Key));
                }
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: done init postings; " + AllTerms.Count + " total terms, across " + FieldInfos.Size() + " fields");
            }
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            AllTerms = null;
            FieldInfos = null;
            Fields = null;
            GlobalLiveDocs = null;
        }

        // TODO maybe instead of @BeforeClass just make a single test run: build postings & index & test it?

        private FieldInfos CurrentFieldInfos;

        // maxAllowed = the "highest" we can index, but we will still
        // randomly index at lower IndexOption
        private FieldsProducer BuildIndex(Directory dir, FieldInfo.IndexOptions maxAllowed, bool allowPayloads, bool alwaysTestMax)
        {
            Codec codec = Codec;
            SegmentInfo segmentInfo = new SegmentInfo(dir, Constants.LUCENE_MAIN_VERSION, "_0", MaxDoc, false, codec, null);

            int maxIndexOption = Enum.GetValues(typeof(FieldInfo.IndexOptions)).Cast<FieldInfo.IndexOptions>().ToList().IndexOf(maxAllowed);
            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: now build index");
            }

            int maxIndexOptionNoOffsets = Enum.GetValues(typeof(FieldInfo.IndexOptions)).Cast<FieldInfo.IndexOptions>().ToList().IndexOf(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

            // TODO use allowPayloads

            var newFieldInfoArray = new FieldInfo[Fields.Count];
            for (int fieldUpto = 0; fieldUpto < Fields.Count; fieldUpto++)
            {
                FieldInfo oldFieldInfo = FieldInfos.FieldInfo(fieldUpto);

                string pf = TestUtil.GetPostingsFormat(codec, oldFieldInfo.Name);
                int fieldMaxIndexOption;
                if (DoesntSupportOffsets.Contains(pf))
                {
                    fieldMaxIndexOption = Math.Min(maxIndexOptionNoOffsets, maxIndexOption);
                }
                else
                {
                    fieldMaxIndexOption = maxIndexOption;
                }

                // Randomly picked the IndexOptions to index this
                // field with:
                FieldInfo.IndexOptions indexOptions = Enum.GetValues(typeof(FieldInfo.IndexOptions)).Cast<FieldInfo.IndexOptions>().ToArray()[alwaysTestMax ? fieldMaxIndexOption : Random().Next(1 + fieldMaxIndexOption)];
                bool doPayloads = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0 && allowPayloads;

                newFieldInfoArray[fieldUpto] = new FieldInfo(oldFieldInfo.Name, true, fieldUpto, false, false, doPayloads, indexOptions, null, DocValuesType.NUMERIC, null);
            }

            FieldInfos newFieldInfos = new FieldInfos(newFieldInfoArray);

            // Estimate that flushed segment size will be 25% of
            // what we use in RAM:
            long bytes = TotalPostings * 8 + TotalPayloadBytes;

            SegmentWriteState writeState = new SegmentWriteState(null, dir, segmentInfo, newFieldInfos, 32, null, new IOContext(new FlushInfo(MaxDoc, bytes)));
            FieldsConsumer fieldsConsumer = codec.PostingsFormat().FieldsConsumer(writeState);

            foreach (KeyValuePair<string, SortedDictionary<BytesRef, long>> fieldEnt in Fields)
            {
                string field = fieldEnt.Key;
                IDictionary<BytesRef, long> terms = fieldEnt.Value;

                FieldInfo fieldInfo = newFieldInfos.FieldInfo(field);

                FieldInfo.IndexOptions? indexOptions = fieldInfo.FieldIndexOptions;

                if (VERBOSE)
                {
                    Console.WriteLine("field=" + field + " indexOtions=" + indexOptions);
                }

                bool doFreq = indexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
                bool doPos = indexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                bool doPayloads = indexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS && allowPayloads;
                bool doOffsets = indexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;

                TermsConsumer termsConsumer = fieldsConsumer.AddField(fieldInfo);
                long sumTotalTF = 0;
                long sumDF = 0;
                FixedBitSet seenDocs = new FixedBitSet(MaxDoc);
                foreach (KeyValuePair<BytesRef, long> termEnt in terms)
                {
                    BytesRef term = termEnt.Key;
                    SeedPostings postings = GetSeedPostings(term.Utf8ToString(), termEnt.Value, false, maxAllowed);
                    if (VERBOSE)
                    {
                        Console.WriteLine("  term=" + field + ":" + term.Utf8ToString() + " docFreq=" + postings.DocFreq + " seed=" + termEnt.Value);
                    }

                    PostingsConsumer postingsConsumer = termsConsumer.StartTerm(term);
                    long totalTF = 0;
                    int docID = 0;
                    while ((docID = postings.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                    {
                        int freq = postings.Freq();
                        if (VERBOSE)
                        {
                            Console.WriteLine("    " + postings.Upto + ": docID=" + docID + " freq=" + postings.Freq_Renamed);
                        }
                        postingsConsumer.StartDoc(docID, doFreq ? postings.Freq_Renamed : -1);
                        seenDocs.Set(docID);
                        if (doPos)
                        {
                            totalTF += postings.Freq_Renamed;
                            for (int posUpto = 0; posUpto < freq; posUpto++)
                            {
                                int pos = postings.NextPosition();
                                BytesRef payload = postings.Payload;

                                if (VERBOSE)
                                {
                                    if (doPayloads)
                                    {
                                        Console.WriteLine("      pos=" + pos + " payload=" + (payload == null ? "null" : payload.Length + " bytes"));
                                    }
                                    else
                                    {
                                        Console.WriteLine("      pos=" + pos);
                                    }
                                }
                                postingsConsumer.AddPosition(pos, doPayloads ? payload : null, doOffsets ? postings.StartOffset() : -1, doOffsets ? postings.EndOffset() : -1);
                            }
                        }
                        else if (doFreq)
                        {
                            totalTF += freq;
                        }
                        else
                        {
                            totalTF++;
                        }
                        postingsConsumer.FinishDoc();
                    }
                    termsConsumer.FinishTerm(term, new TermStats(postings.DocFreq, doFreq ? totalTF : -1));
                    sumTotalTF += totalTF;
                    sumDF += postings.DocFreq;
                }

                termsConsumer.Finish(doFreq ? sumTotalTF : -1, sumDF, seenDocs.Cardinality());
            }

            fieldsConsumer.Dispose();

            if (VERBOSE)
            {
                Console.WriteLine("TEST: after indexing: files=");
                foreach (string file in dir.ListAll())
                {
                    Console.WriteLine("  " + file + ": " + dir.FileLength(file) + " bytes");
                }
            }

            CurrentFieldInfos = newFieldInfos;

            SegmentReadState readState = new SegmentReadState(dir, segmentInfo, newFieldInfos, IOContext.READ, 1);

            return codec.PostingsFormat().FieldsProducer(readState);
        }

        private class ThreadState
        {
            // Only used with REUSE option:
            public DocsEnum ReuseDocsEnum;

            public DocsAndPositionsEnum ReuseDocsAndPositionsEnum;
        }

        private void VerifyEnum(ThreadState threadState, string field, BytesRef term, TermsEnum termsEnum, FieldInfo.IndexOptions maxTestOptions, FieldInfo.IndexOptions maxIndexOptions, ISet<Option> options, bool alwaysTestMax)
        // Maximum options (docs/freqs/positions/offsets) to test:
        {
            if (VERBOSE)
            {
                Console.WriteLine("  verifyEnum: options=" + options + " maxTestOptions=" + maxTestOptions);
            }

            // Make sure TermsEnum really is positioned on the
            // expected term:
            Assert.AreEqual(term, termsEnum.Term());

            // 50% of the time time pass liveDocs:
            bool useLiveDocs = options.Contains(Option.LIVE_DOCS) && Random().NextBoolean();
            Bits liveDocs;
            if (useLiveDocs)
            {
                liveDocs = GlobalLiveDocs;
                if (VERBOSE)
                {
                    Console.WriteLine("  use liveDocs");
                }
            }
            else
            {
                liveDocs = null;
                if (VERBOSE)
                {
                    Console.WriteLine("  no liveDocs");
                }
            }

            FieldInfo fieldInfo = CurrentFieldInfos.FieldInfo(field);

            // NOTE: can be empty list if we are using liveDocs:
            SeedPostings expected = GetSeedPostings(term.Utf8ToString(), Fields[field][term], useLiveDocs, maxIndexOptions);
            Assert.AreEqual(expected.DocFreq, termsEnum.DocFreq());

            bool allowFreqs = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS && maxTestOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS) >= 0;
            bool doCheckFreqs = allowFreqs && (alwaysTestMax || Random().Next(3) <= 2);

            bool allowPositions = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS && maxTestOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
            bool doCheckPositions = allowPositions && (alwaysTestMax || Random().Next(3) <= 2);

            bool allowOffsets = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS && maxTestOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
            bool doCheckOffsets = allowOffsets && (alwaysTestMax || Random().Next(3) <= 2);

            bool doCheckPayloads = options.Contains(Option.PAYLOADS) && allowPositions && fieldInfo.HasPayloads() && (alwaysTestMax || Random().Next(3) <= 2);

            DocsEnum prevDocsEnum = null;

            DocsEnum docsEnum;
            DocsAndPositionsEnum docsAndPositionsEnum;

            if (!doCheckPositions)
            {
                if (allowPositions && Random().Next(10) == 7)
                {
                    // 10% of the time, even though we will not check positions, pull a DocsAndPositions enum

                    if (options.Contains(Option.REUSE_ENUMS) && Random().Next(10) < 9)
                    {
                        prevDocsEnum = threadState.ReuseDocsAndPositionsEnum;
                    }

                    int flags = 0;
                    if (alwaysTestMax || Random().NextBoolean())
                    {
                        flags |= DocsAndPositionsEnum.FLAG_OFFSETS;
                    }
                    if (alwaysTestMax || Random().NextBoolean())
                    {
                        flags |= DocsAndPositionsEnum.FLAG_PAYLOADS;
                    }

                    if (VERBOSE)
                    {
                        Console.WriteLine("  get DocsAndPositionsEnum (but we won't check positions) flags=" + flags);
                    }

                    threadState.ReuseDocsAndPositionsEnum = termsEnum.DocsAndPositions(liveDocs, (DocsAndPositionsEnum)prevDocsEnum, flags);
                    docsEnum = threadState.ReuseDocsAndPositionsEnum;
                    docsAndPositionsEnum = threadState.ReuseDocsAndPositionsEnum;
                }
                else
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("  get DocsEnum");
                    }
                    if (options.Contains(Option.REUSE_ENUMS) && Random().Next(10) < 9)
                    {
                        prevDocsEnum = threadState.ReuseDocsEnum;
                    }
                    threadState.ReuseDocsEnum = termsEnum.Docs(liveDocs, prevDocsEnum, doCheckFreqs ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
                    docsEnum = threadState.ReuseDocsEnum;
                    docsAndPositionsEnum = null;
                }
            }
            else
            {
                if (options.Contains(Option.REUSE_ENUMS) && Random().Next(10) < 9)
                {
                    prevDocsEnum = threadState.ReuseDocsAndPositionsEnum;
                }

                int flags = 0;
                if (alwaysTestMax || doCheckOffsets || Random().Next(3) == 1)
                {
                    flags |= DocsAndPositionsEnum.FLAG_OFFSETS;
                }
                if (alwaysTestMax || doCheckPayloads || Random().Next(3) == 1)
                {
                    flags |= DocsAndPositionsEnum.FLAG_PAYLOADS;
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  get DocsAndPositionsEnum flags=" + flags);
                }

                threadState.ReuseDocsAndPositionsEnum = termsEnum.DocsAndPositions(liveDocs, (DocsAndPositionsEnum)prevDocsEnum, flags);
                docsEnum = threadState.ReuseDocsAndPositionsEnum;
                docsAndPositionsEnum = threadState.ReuseDocsAndPositionsEnum;
            }

            Assert.IsNotNull(docsEnum, "null DocsEnum");
            int initialDocID = docsEnum.DocID();
            Assert.AreEqual(-1, initialDocID, "inital docID should be -1" + docsEnum);

            if (VERBOSE)
            {
                if (prevDocsEnum == null)
                {
                    Console.WriteLine("  got enum=" + docsEnum);
                }
                else if (prevDocsEnum == docsEnum)
                {
                    Console.WriteLine("  got reuse enum=" + docsEnum);
                }
                else
                {
                    Console.WriteLine("  got enum=" + docsEnum + " (reuse of " + prevDocsEnum + " failed)");
                }
            }

            // 10% of the time don't consume all docs:
            int stopAt;
            if (!alwaysTestMax && options.Contains(Option.PARTIAL_DOC_CONSUME) && expected.DocFreq > 1 && Random().Next(10) == 7)
            {
                stopAt = Random().Next(expected.DocFreq - 1);
                if (VERBOSE)
                {
                    Console.WriteLine("  will not consume all docs (" + stopAt + " vs " + expected.DocFreq + ")");
                }
            }
            else
            {
                stopAt = expected.DocFreq;
                if (VERBOSE)
                {
                    Console.WriteLine("  consume all docs");
                }
            }

            double skipChance = alwaysTestMax ? 0.5 : Random().NextDouble();
            int numSkips = expected.DocFreq < 3 ? 1 : TestUtil.NextInt(Random(), 1, Math.Min(20, expected.DocFreq / 3));
            int skipInc = expected.DocFreq / numSkips;
            int skipDocInc = MaxDoc / numSkips;

            // Sometimes do 100% skipping:
            bool doAllSkipping = options.Contains(Option.SKIPPING) && Random().Next(7) == 1;

            double freqAskChance = alwaysTestMax ? 1.0 : Random().NextDouble();
            double payloadCheckChance = alwaysTestMax ? 1.0 : Random().NextDouble();
            double offsetCheckChance = alwaysTestMax ? 1.0 : Random().NextDouble();

            if (VERBOSE)
            {
                if (options.Contains(Option.SKIPPING))
                {
                    Console.WriteLine("  skipChance=" + skipChance + " numSkips=" + numSkips);
                }
                else
                {
                    Console.WriteLine("  no skipping");
                }
                if (doCheckFreqs)
                {
                    Console.WriteLine("  freqAskChance=" + freqAskChance);
                }
                if (doCheckPayloads)
                {
                    Console.WriteLine("  payloadCheckChance=" + payloadCheckChance);
                }
                if (doCheckOffsets)
                {
                    Console.WriteLine("  offsetCheckChance=" + offsetCheckChance);
                }
            }

            while (expected.Upto <= stopAt)
            {
                if (expected.Upto == stopAt)
                {
                    if (stopAt == expected.DocFreq)
                    {
                        Assert.AreEqual(DocsEnum.NO_MORE_DOCS, docsEnum.NextDoc(), "DocsEnum should have ended but didn't");

                        // Common bug is to forget to set this.Doc=NO_MORE_DOCS in the enum!:
                        Assert.AreEqual(DocsEnum.NO_MORE_DOCS, docsEnum.DocID(), "DocsEnum should have ended but didn't");
                    }
                    break;
                }

                if (options.Contains(Option.SKIPPING) && (doAllSkipping || Random().NextDouble() <= skipChance))
                {
                    int targetDocID = -1;
                    if (expected.Upto < stopAt && Random().NextBoolean())
                    {
                        // Pick target we know exists:
                        int skipCount = TestUtil.NextInt(Random(), 1, skipInc);
                        for (int skip = 0; skip < skipCount; skip++)
                        {
                            if (expected.NextDoc() == DocsEnum.NO_MORE_DOCS)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Pick random target (might not exist):
                        int skipDocIDs = TestUtil.NextInt(Random(), 1, skipDocInc);
                        if (skipDocIDs > 0)
                        {
                            targetDocID = expected.DocID() + skipDocIDs;
                            expected.Advance(targetDocID);
                        }
                    }

                    if (expected.Upto >= stopAt)
                    {
                        int target = Random().NextBoolean() ? MaxDoc : DocsEnum.NO_MORE_DOCS;
                        if (VERBOSE)
                        {
                            Console.WriteLine("  now advance to end (target=" + target + ")");
                        }
                        Assert.AreEqual(DocsEnum.NO_MORE_DOCS, docsEnum.Advance(target), "DocsEnum should have ended but didn't");
                        break;
                    }
                    else
                    {
                        if (VERBOSE)
                        {
                            if (targetDocID != -1)
                            {
                                Console.WriteLine("  now advance to random target=" + targetDocID + " (" + expected.Upto + " of " + stopAt + ") current=" + docsEnum.DocID());
                            }
                            else
                            {
                                Console.WriteLine("  now advance to known-exists target=" + expected.DocID() + " (" + expected.Upto + " of " + stopAt + ") current=" + docsEnum.DocID());
                            }
                        }
                        int docID = docsEnum.Advance(targetDocID != -1 ? targetDocID : expected.DocID());
                        Assert.AreEqual(expected.DocID(), docID, "docID is wrong");
                    }
                }
                else
                {
                    expected.NextDoc();
                    if (VERBOSE)
                    {
                        Console.WriteLine("  now nextDoc to " + expected.DocID() + " (" + expected.Upto + " of " + stopAt + ")");
                    }
                    int docID = docsEnum.NextDoc();
                    Assert.AreEqual(expected.DocID(), docID, "docID is wrong");
                    if (docID == DocsEnum.NO_MORE_DOCS)
                    {
                        break;
                    }
                }

                if (doCheckFreqs && Random().NextDouble() <= freqAskChance)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("    now freq()=" + expected.Freq());
                    }
                    int freq = docsEnum.Freq();
                    Assert.AreEqual(expected.Freq(), freq, "freq is wrong");
                }

                if (doCheckPositions)
                {
                    int freq = docsEnum.Freq();
                    int numPosToConsume;
                    if (!alwaysTestMax && options.Contains(Option.PARTIAL_POS_CONSUME) && Random().Next(5) == 1)
                    {
                        numPosToConsume = Random().Next(freq);
                    }
                    else
                    {
                        numPosToConsume = freq;
                    }

                    for (int i = 0; i < numPosToConsume; i++)
                    {
                        int pos = expected.NextPosition();
                        if (VERBOSE)
                        {
                            Console.WriteLine("    now nextPosition to " + pos);
                        }
                        Assert.AreEqual(pos, docsAndPositionsEnum.NextPosition(), "position is wrong");

                        if (doCheckPayloads)
                        {
                            BytesRef expectedPayload = expected.Payload;
                            if (Random().NextDouble() <= payloadCheckChance)
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine("      now check expectedPayload length=" + (expectedPayload == null ? 0 : expectedPayload.Length));
                                }
                                if (expectedPayload == null || expectedPayload.Length == 0)
                                {
                                    Assert.IsNull(docsAndPositionsEnum.Payload, "should not have payload");
                                }
                                else
                                {
                                    BytesRef payload = docsAndPositionsEnum.Payload;
                                    Assert.IsNotNull(payload, "should have payload but doesn't");

                                    Assert.AreEqual(expectedPayload.Length, payload.Length, "payload length is wrong");
                                    for (int byteUpto = 0; byteUpto < expectedPayload.Length; byteUpto++)
                                    {
                                        Assert.AreEqual(expectedPayload.Bytes[expectedPayload.Offset + byteUpto], payload.Bytes[payload.Offset + byteUpto], "payload bytes are wrong");
                                    }

                                    // make a deep copy
                                    payload = BytesRef.DeepCopyOf(payload);
                                    Assert.AreEqual(payload, docsAndPositionsEnum.Payload, "2nd call to getPayload returns something different!");
                                }
                            }
                            else
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine("      skip check payload length=" + (expectedPayload == null ? 0 : expectedPayload.Length));
                                }
                            }
                        }

                        if (doCheckOffsets)
                        {
                            if (Random().NextDouble() <= offsetCheckChance)
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine("      now check offsets: startOff=" + expected.StartOffset() + " endOffset=" + expected.EndOffset());
                                }
                                Assert.AreEqual(expected.StartOffset(), docsAndPositionsEnum.StartOffset(), "startOffset is wrong");
                                Assert.AreEqual(expected.EndOffset(), docsAndPositionsEnum.EndOffset(), "endOffset is wrong");
                            }
                            else
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine("      skip check offsets");
                                }
                            }
                        }
                        else if (fieldInfo.FieldIndexOptions < FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
                        {
                            if (VERBOSE)
                            {
                                Console.WriteLine("      now check offsets are -1");
                            }
                            Assert.AreEqual(-1, docsAndPositionsEnum.StartOffset(), "startOffset isn't -1");
                            Assert.AreEqual(-1, docsAndPositionsEnum.EndOffset(), "endOffset isn't -1");
                        }
                    }
                }
            }
        }

        private class TestThread : ThreadClass
        {
            internal Fields FieldsSource;
            internal ISet<Option> Options;
            internal FieldInfo.IndexOptions MaxIndexOptions;
            internal FieldInfo.IndexOptions MaxTestOptions;
            internal bool AlwaysTestMax;
            internal BasePostingsFormatTestCase TestCase;

            public TestThread(BasePostingsFormatTestCase testCase, Fields fieldsSource, ISet<Option> options, FieldInfo.IndexOptions maxTestOptions, FieldInfo.IndexOptions maxIndexOptions, bool alwaysTestMax)
            {
                this.FieldsSource = fieldsSource;
                this.Options = options;
                this.MaxTestOptions = maxTestOptions;
                this.MaxIndexOptions = maxIndexOptions;
                this.AlwaysTestMax = alwaysTestMax;
                this.TestCase = testCase;
            }

            public override void Run()
            {
                try
                {
                    try
                    {
                        TestCase.TestTermsOneThread(FieldsSource, Options, MaxTestOptions, MaxIndexOptions, AlwaysTestMax);
                    }
                    catch (Exception t)
                    {
                        throw new Exception(t.Message, t);
                    }
                }
                finally
                {
                    FieldsSource = null;
                    TestCase = null;
                }
            }
        }

        private void TestTerms(Fields fieldsSource, ISet<Option> options, FieldInfo.IndexOptions maxTestOptions, FieldInfo.IndexOptions maxIndexOptions, bool alwaysTestMax)
        {
            if (options.Contains(Option.THREADS))
            {
                int numThreads = TestUtil.NextInt(Random(), 2, 5);
                ThreadClass[] threads = new ThreadClass[numThreads];
                for (int threadUpto = 0; threadUpto < numThreads; threadUpto++)
                {
                    threads[threadUpto] = new TestThread(this, fieldsSource, options, maxTestOptions, maxIndexOptions, alwaysTestMax);
                    threads[threadUpto].Start();
                }
                for (int threadUpto = 0; threadUpto < numThreads; threadUpto++)
                {
                    threads[threadUpto].Join();
                }
            }
            else
            {
                TestTermsOneThread(fieldsSource, options, maxTestOptions, maxIndexOptions, alwaysTestMax);
            }
        }

        private void TestTermsOneThread(Fields fieldsSource, ISet<Option> options, FieldInfo.IndexOptions maxTestOptions, FieldInfo.IndexOptions maxIndexOptions, bool alwaysTestMax)
        {
            ThreadState threadState = new ThreadState();

            // Test random terms/fields:
            IList<TermState> termStates = new List<TermState>();
            IList<FieldAndTerm> termStateTerms = new List<FieldAndTerm>();

            AllTerms = CollectionsHelper.Shuffle(AllTerms);
            int upto = 0;
            while (upto < AllTerms.Count)
            {
                bool useTermState = termStates.Count != 0 && Random().Next(5) == 1;
                FieldAndTerm fieldAndTerm;
                TermsEnum termsEnum;

                TermState termState = null;

                if (!useTermState)
                {
                    // Seek by random field+term:
                    fieldAndTerm = AllTerms[upto++];
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: seek to term=" + fieldAndTerm.Field + ":" + fieldAndTerm.Term.Utf8ToString());
                    }
                }
                else
                {
                    // Seek by previous saved TermState
                    int idx = Random().Next(termStates.Count);
                    fieldAndTerm = termStateTerms[idx];
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: seek using TermState to term=" + fieldAndTerm.Field + ":" + fieldAndTerm.Term.Utf8ToString());
                    }
                    termState = termStates[idx];
                }

                Terms terms = fieldsSource.Terms(fieldAndTerm.Field);
                Assert.IsNotNull(terms);
                termsEnum = terms.Iterator(null);

                if (!useTermState)
                {
                    Assert.IsTrue(termsEnum.SeekExact(fieldAndTerm.Term));
                }
                else
                {
                    termsEnum.SeekExact(fieldAndTerm.Term, termState);
                }

                bool savedTermState = false;

                if (options.Contains(Option.TERM_STATE) && !useTermState && Random().Next(5) == 1)
                {
                    // Save away this TermState:
                    termStates.Add(termsEnum.TermState());
                    termStateTerms.Add(fieldAndTerm);
                    savedTermState = true;
                }

                VerifyEnum(threadState, fieldAndTerm.Field, fieldAndTerm.Term, termsEnum, maxTestOptions, maxIndexOptions, options, alwaysTestMax);

                // Sometimes save term state after pulling the enum:
                if (options.Contains(Option.TERM_STATE) && !useTermState && !savedTermState && Random().Next(5) == 1)
                {
                    // Save away this TermState:
                    termStates.Add(termsEnum.TermState());
                    termStateTerms.Add(fieldAndTerm);
                    useTermState = true;
                }

                // 10% of the time make sure you can pull another enum
                // from the same term:
                if (alwaysTestMax || Random().Next(10) == 7)
                {
                    // Try same term again
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: try enum again on same term");
                    }

                    VerifyEnum(threadState, fieldAndTerm.Field, fieldAndTerm.Term, termsEnum, maxTestOptions, maxIndexOptions, options, alwaysTestMax);
                }
            }
        }

        private void TestFields(Fields fields)
        {
            IEnumerator<string> iterator = fields.GetEnumerator();
            while (iterator.MoveNext())
            {
                var dummy = iterator.Current;
                // .NET: Testing for iterator.Remove() isn't applicable
            }
            Assert.IsFalse(iterator.MoveNext());

            // .NET: Testing for NoSuchElementException with .NET iterators isn't applicable
        }

        /// <summary>
        /// Indexes all fields/terms at the specified
        ///  IndexOptions, and fully tests at that IndexOptions.
        /// </summary>
        private void TestFull(FieldInfo.IndexOptions options, bool withPayloads)
        {
            DirectoryInfo path = CreateTempDir("testPostingsFormat.testExact");
            using (Directory dir = NewFSDirectory(path))
            {
                // TODO test thread safety of buildIndex too
                var fieldsProducer = BuildIndex(dir, options, withPayloads, true);

                TestFields(fieldsProducer);

                var allOptions = (FieldInfo.IndexOptions[]) Enum.GetValues(typeof (FieldInfo.IndexOptions));
                    //IndexOptions_e.values();
                int maxIndexOption = Arrays.AsList(allOptions).IndexOf(options);

                for (int i = 0; i <= maxIndexOption; i++)
                {
                    ISet<Option> allOptionsHashSet = new HashSet<Option>(Enum.GetValues(typeof (Option)).Cast<Option>());
                    TestTerms(fieldsProducer, allOptionsHashSet, allOptions[i], options, true);
                    if (withPayloads)
                    {
                        // If we indexed w/ payloads, also test enums w/o accessing payloads:
                        ISet<Option> payloadsHashSet = new HashSet<Option>() {Option.PAYLOADS};
                        var complementHashSet = new HashSet<Option>(allOptionsHashSet.Except(payloadsHashSet));
                        TestTerms(fieldsProducer, complementHashSet, allOptions[i], options, true);
                    }
                }

                fieldsProducer.Dispose();
            }
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestDocsOnly()
        {
            TestFull(FieldInfo.IndexOptions.DOCS_ONLY, false);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestDocsAndFreqs()
        {
            TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS, false);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestDocsAndFreqsAndPositions()
        {
            TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, false);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestDocsAndFreqsAndPositionsAndPayloads()
        {
            TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, true);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestDocsAndFreqsAndPositionsAndOffsets()
        {
            TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, false);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestDocsAndFreqsAndPositionsAndOffsetsAndPayloads()
        {
            TestFull(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, true);
        }

        // [Test] // LUCENENET NOTE: For now, we are overriding this test in every subclass to pull it into the right context for the subclass
        public virtual void TestRandom()
        {
            int iters = 5;

            for (int iter = 0; iter < iters; iter++)
            {
                DirectoryInfo path = CreateTempDir("testPostingsFormat");
                Directory dir = NewFSDirectory(path);

                bool indexPayloads = Random().NextBoolean();
                // TODO test thread safety of buildIndex too
                FieldsProducer fieldsProducer = BuildIndex(dir, FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, indexPayloads, false);

                TestFields(fieldsProducer);

                // NOTE: you can also test "weaker" index options than
                // you indexed with:
                TestTerms(fieldsProducer, new HashSet<Option>(Enum.GetValues(typeof(Option)).Cast<Option>()), FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, false);

                fieldsProducer.Dispose();
                fieldsProducer = null;

                dir.Dispose();
                System.IO.Directory.Delete(path.FullName, true);
            }
        }

        protected internal override void AddRandomFields(Document doc)
        {
            foreach (FieldInfo.IndexOptions opts in Enum.GetValues(typeof(FieldInfo.IndexOptions)))
            {
                string field = "f_" + opts;
                string pf = TestUtil.GetPostingsFormat(Codec.Default, field);
                if (opts == FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS && DoesntSupportOffsets.Contains(pf))
                {
                    continue;
                }
                var ft = new FieldType {IndexOptions = opts, Indexed = true, OmitNorms = true};
                ft.Freeze();
                int numFields = Random().Next(5);
                for (int j = 0; j < numFields; ++j)
                {
                    doc.Add(new Field("f_" + opts, TestUtil.RandomSimpleString(Random(), 2), ft));
                }
            }
        }
    }
}