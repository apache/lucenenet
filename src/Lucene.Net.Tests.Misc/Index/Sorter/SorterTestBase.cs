using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index.Sorter
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

    [SuppressCodecs("Lucene3x")]
    public abstract class SorterTestBase : LuceneTestCase
    {
        internal class NormsSimilarity : Similarity
        {


            private readonly Similarity @in;

            public NormsSimilarity(Similarity @in)
            {
                this.@in = @in;
            }


            public override long ComputeNorm(FieldInvertState state)
            {
                if (state.Name.Equals(NORMS_FIELD, StringComparison.Ordinal))
                {
                    return J2N.BitConversion.SingleToInt32Bits(state.Boost);
                }
                else
                {
                    return @in.ComputeNorm(state);
                }
            }

            public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
            {
                return @in.ComputeWeight(queryBoost, collectionStats, termStats);
            }


            public override SimScorer GetSimScorer(SimWeight weight, AtomicReaderContext context)
            {
                return @in.GetSimScorer(weight, context);
            }

        }

        internal sealed class PositionsTokenStream : TokenStream
        {


            private readonly ICharTermAttribute term;
            private readonly IPayloadAttribute payload;
            private readonly IOffsetAttribute offset;

            private int pos, off;

            public PositionsTokenStream()
            {
                term = AddAttribute<ICharTermAttribute>();
                payload = AddAttribute<IPayloadAttribute>();
                offset = AddAttribute<IOffsetAttribute>();
            }

            public override bool IncrementToken()
            {
                if (pos == 0)
                {
                    return false;
                }

                ClearAttributes();
                term.Append(DOC_POSITIONS_TERM);
                payload.Payload = new BytesRef(pos.ToString());
                offset.SetOffset(off, off);
                --pos;
                ++off;
                return true;
            }

            internal void SetId(int id)
            {
                pos = id / 10 + 1;
                off = 0;
            }
        }

        protected static readonly string ID_FIELD = "id";
        protected static readonly string DOCS_ENUM_FIELD = "docs";
        protected static readonly string DOCS_ENUM_TERM = "$all$";
        protected static readonly string DOC_POSITIONS_FIELD = "positions";
        protected static readonly string DOC_POSITIONS_TERM = "$all$";
        protected static readonly string NUMERIC_DV_FIELD = "numeric";
        protected static readonly string NORMS_FIELD = "norm";
        protected static readonly string BINARY_DV_FIELD = "binary";
        protected static readonly string SORTED_DV_FIELD = "sorted";
        protected static readonly string SORTED_SET_DV_FIELD = "sorted_set";
        protected static readonly string TERM_VECTORS_FIELD = "term_vectors";

        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        private static readonly FieldType TERM_VECTORS_TYPE = new FieldType(TextField.TYPE_NOT_STORED) { StoreTermVectors = true }.Freeze();
        private static readonly FieldType POSITIONS_TYPE = new FieldType(TextField.TYPE_NOT_STORED) { IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS }.Freeze();


        protected static Directory dir;
        protected static AtomicReader reader;
        protected static int[] sortedValues;

        private static Document Doc(int id, PositionsTokenStream positions)
        {
            Document doc = new Document();
            doc.Add(new StringField(ID_FIELD, id.ToString(), Field.Store.YES));
            doc.Add(new StringField(DOCS_ENUM_FIELD, DOCS_ENUM_TERM, Field.Store.NO));
            positions.SetId(id);
            if (DoesntSupportOffsets.Contains(TestUtil.GetPostingsFormat(DOC_POSITIONS_FIELD)))
            {
                // codec doesnt support offsets: just index positions for the field
                doc.Add(new Field(DOC_POSITIONS_FIELD, positions, TextField.TYPE_NOT_STORED));
            }
            else
            {
                doc.Add(new Field(DOC_POSITIONS_FIELD, positions, POSITIONS_TYPE));
            }
            doc.Add(new NumericDocValuesField(NUMERIC_DV_FIELD, id));
            TextField norms = new TextField(NORMS_FIELD, id.ToString(), Field.Store.NO);
            norms.Boost = (J2N.BitConversion.Int32BitsToSingle(id));
            doc.Add(norms);
            doc.Add(new BinaryDocValuesField(BINARY_DV_FIELD, new BytesRef(id.ToString())));
            doc.Add(new SortedDocValuesField(SORTED_DV_FIELD, new BytesRef(id.ToString())));
            if (DefaultCodecSupportsSortedSet)
            {
                doc.Add(new SortedSetDocValuesField(SORTED_SET_DV_FIELD, new BytesRef(id.ToString())));
                doc.Add(new SortedSetDocValuesField(SORTED_SET_DV_FIELD, new BytesRef((id + 1).ToString())));
            }
            doc.Add(new Field(TERM_VECTORS_FIELD, id.ToString(), TERM_VECTORS_TYPE));
            return doc;
        }

        /** Creates an index for sorting. */
        public void CreateIndex(Directory dir, int numDocs, Random random)
        {
            IList<int> ids = new JCG.List<int>();
            for (int i = 0; i < numDocs; i++)
            {
                ids.Add(i * 10);
            }
            // shuffle them for indexing
            ids.Shuffle(Random);

            if (Verbose)
            {
                Console.WriteLine("Shuffled IDs for indexing: " + Collections.ToString(ids));
            }

            PositionsTokenStream positions = new PositionsTokenStream();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
            conf.SetMaxBufferedDocs(4); // create some segments
            conf.SetSimilarity(new NormsSimilarity(conf.Similarity)); // for testing norms field
            using RandomIndexWriter writer = new RandomIndexWriter(random, dir, conf);
            writer.DoRandomForceMerge = (false);
            foreach (int id in ids)
            {
                writer.AddDocument(Doc(id, positions));
            }
            // delete some documents
            writer.Commit();
            foreach (int id in ids)
            {
                if (random.NextDouble() < 0.2)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("delete doc_id " + id);
                    }
                    writer.DeleteDocuments(new Term(ID_FIELD, id.ToString()));
                }
            }
        }

        [OneTimeSetUp]
        public override void BeforeClass() // LUCENENET specific - renamed from BeforeClassSorterTestBase() to ensure calling order vs base class
        {
            base.BeforeClass();

            dir = NewDirectory();
            int numDocs = AtLeast(20);
            CreateIndex(dir, numDocs, Random);

            reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
        }

        [OneTimeTearDown]
        public override void AfterClass() // LUCENENET specific - renamed from AfterClassSorterTestBase() to ensure calling order vs base class
        {
            reader.Dispose();
            dir.Dispose();

            base.AfterClass();
        }

        [Test]
        public virtual void TestBinaryDocValuesField()
        {
            BinaryDocValues dv = reader.GetBinaryDocValues(BINARY_DV_FIELD);
            BytesRef bytes = new BytesRef();
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                dv.Get(i, bytes);
                assertEquals("incorrect binary DocValues for doc " + i, sortedValues[i].ToString(), bytes.Utf8ToString());
            }
        }

        [Test]
        public virtual void TestDocsAndPositionsEnum()
        {
            TermsEnum termsEnum = reader.GetTerms(DOC_POSITIONS_FIELD).GetEnumerator();
            assertEquals(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef(DOC_POSITIONS_TERM)));
            DocsAndPositionsEnum sortedPositions = termsEnum.DocsAndPositions(null, null);
            int doc;

            // test nextDoc()
            while ((doc = sortedPositions.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                int freq = sortedPositions.Freq;
                assertEquals("incorrect freq for doc=" + doc, sortedValues[doc] / 10 + 1, freq);
                for (int i = 0; i < freq; i++)
                {
                    assertEquals("incorrect position for doc=" + doc, i, sortedPositions.NextPosition());
                    if (!DoesntSupportOffsets.Contains(TestUtil.GetPostingsFormat(DOC_POSITIONS_FIELD)))
                    {
                        assertEquals("incorrect startOffset for doc=" + doc, i, sortedPositions.StartOffset);
                        assertEquals("incorrect endOffset for doc=" + doc, i, sortedPositions.EndOffset);
                    }
                    assertEquals("incorrect payload for doc=" + doc, freq - i, int.Parse(sortedPositions.GetPayload().Utf8ToString(), CultureInfo.InvariantCulture));
                }
            }

            // test advance()
            DocsAndPositionsEnum reuse = sortedPositions;
            sortedPositions = termsEnum.DocsAndPositions(null, reuse);
            if (sortedPositions is SortingAtomicReader.SortingDocsAndPositionsEnum)
            {
                assertTrue(((SortingAtomicReader.SortingDocsAndPositionsEnum)sortedPositions).Reused(reuse)); // make sure reuse worked
            }
            doc = 0;
            while ((doc = sortedPositions.Advance(doc + TestUtil.NextInt32(Random, 1, 5))) != DocIdSetIterator.NO_MORE_DOCS)
            {
                int freq = sortedPositions.Freq;
                assertEquals("incorrect freq for doc=" + doc, sortedValues[doc] / 10 + 1, freq);
                for (int i = 0; i < freq; i++)
                {
                    assertEquals("incorrect position for doc=" + doc, i, sortedPositions.NextPosition());
                    if (!DoesntSupportOffsets.Contains(TestUtil.GetPostingsFormat(DOC_POSITIONS_FIELD)))
                    {
                        assertEquals("incorrect startOffset for doc=" + doc, i, sortedPositions.StartOffset);
                        assertEquals("incorrect endOffset for doc=" + doc, i, sortedPositions.EndOffset);
                    }
                    assertEquals("incorrect payload for doc=" + doc, freq - i, int.Parse(sortedPositions.GetPayload().Utf8ToString(), CultureInfo.InvariantCulture));
                }
            }
        }

        internal IBits RandomLiveDocs(int maxDoc)
        {
            if (Rarely())
            {
                if (Random.nextBoolean())
                {
                    return null;
                }
                else
                {
                    return new Bits.MatchNoBits(maxDoc);
                }
            }
            FixedBitSet bits = new FixedBitSet(maxDoc);
            int bitsSet = TestUtil.NextInt32(Random, 1, maxDoc - 1);
            for (int i = 0; i < bitsSet; ++i)
            {
                while (true)
                {
                    int index = Random.nextInt(maxDoc);
                    if (!bits.Get(index))
                    {
                        bits.Set(index);
                        break;
                    }
                }
            }
            return bits;
        }

        [Test]
        public virtual void TestDocsEnum()
        {
            IBits mappedLiveDocs = RandomLiveDocs(reader.MaxDoc);
            TermsEnum termsEnum = reader.GetTerms(DOCS_ENUM_FIELD).GetEnumerator();
            assertEquals(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef(DOCS_ENUM_TERM)));
            DocsEnum docs = termsEnum.Docs(mappedLiveDocs, null);

            int doc;
            int prev = -1;
            while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                assertTrue("document " + doc + " marked as deleted", mappedLiveDocs is null || mappedLiveDocs.Get(doc));
                assertEquals("incorrect value; doc " + doc, sortedValues[doc], int.Parse(reader.Document(doc).Get(ID_FIELD)));
                while (++prev < doc)
                {
                    assertFalse("document " + prev + " not marked as deleted", mappedLiveDocs is null || mappedLiveDocs.Get(prev));
                }
            }
            while (++prev < reader.MaxDoc)
            {
                assertFalse("document " + prev + " not marked as deleted", mappedLiveDocs is null || mappedLiveDocs.Get(prev));
            }

            DocsEnum reuse = docs;
            docs = termsEnum.Docs(mappedLiveDocs, reuse);
            if (docs is SortingAtomicReader.SortingDocsEnum)
            {
                assertTrue(((SortingAtomicReader.SortingDocsEnum)docs).Reused(reuse)); // make sure reuse worked
            }
            doc = -1;
            prev = -1;
            while ((doc = docs.Advance(doc + 1)) != DocIdSetIterator.NO_MORE_DOCS)
            {
                assertTrue("document " + doc + " marked as deleted", mappedLiveDocs is null || mappedLiveDocs.Get(doc));
                assertEquals("incorrect value; doc " + doc, sortedValues[doc], int.Parse(reader.Document(doc).Get(ID_FIELD)));
                while (++prev < doc)
                {
                    assertFalse("document " + prev + " not marked as deleted", mappedLiveDocs is null || mappedLiveDocs.Get(prev));
                }
            }
            while (++prev < reader.MaxDoc)
            {
                assertFalse("document " + prev + " not marked as deleted", mappedLiveDocs is null || mappedLiveDocs.Get(prev));
            }
        }

        [Test]
        public virtual void TestNormValues()
        {
            NumericDocValues dv = reader.GetNormValues(NORMS_FIELD);
            int maxDoc = reader.MaxDoc;
            for (int i = 0; i < maxDoc; i++)
            {
                assertEquals("incorrect norm value for doc " + i, sortedValues[i], dv.Get(i));
            }
        }

        [Test]
        public virtual void TestNumericDocValuesField()
        {
            NumericDocValues dv = reader.GetNumericDocValues(NUMERIC_DV_FIELD);
            int maxDoc = reader.MaxDoc;
            for (int i = 0; i < maxDoc; i++)
            {
                assertEquals("incorrect numeric DocValues for doc " + i, sortedValues[i], dv.Get(i));
            }
        }

        [Test]
        public virtual void TestSortedDocValuesField()
        {
            SortedDocValues dv = reader.GetSortedDocValues(SORTED_DV_FIELD);
            int maxDoc = reader.MaxDoc;
            BytesRef bytes = new BytesRef();
            for (int i = 0; i < maxDoc; i++)
            {
                dv.Get(i, bytes);
                assertEquals("incorrect sorted DocValues for doc " + i, sortedValues[i].ToString(), bytes.Utf8ToString());
            }
        }

        [Test]
        public virtual void TestSortedSetDocValuesField()
        {
            AssumeTrue("default codec does not support SORTED_SET", DefaultCodecSupportsSortedSet);
            SortedSetDocValues dv = reader.GetSortedSetDocValues(SORTED_SET_DV_FIELD);
            int maxDoc = reader.MaxDoc;
            BytesRef bytes = new BytesRef();
            for (int i = 0; i < maxDoc; i++)
            {
                dv.SetDocument(i);
                dv.LookupOrd(dv.NextOrd(), bytes);
                int value = sortedValues[i];
                assertEquals("incorrect sorted-set DocValues for doc " + i, value.toString(), bytes.Utf8ToString());
                dv.LookupOrd(dv.NextOrd(), bytes);
                assertEquals("incorrect sorted-set DocValues for doc " + i, (value + 1).ToString(), bytes.Utf8ToString());
                assertEquals(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
            }
        }

        [Test]
        public virtual void TestTermVectors()
        {
            int maxDoc = reader.MaxDoc;
            for (int i = 0; i < maxDoc; i++)
            {
                Terms terms = reader.GetTermVector(i, TERM_VECTORS_FIELD);
                assertNotNull("term vectors not found for doc " + i + " field [" + TERM_VECTORS_FIELD + "]", terms);
                var iter = terms.GetEnumerator();
                iter.MoveNext();
                assertEquals("incorrect term vector for doc " + i, sortedValues[i].toString(), iter.Term.Utf8ToString());
            }
        }
    }
}
