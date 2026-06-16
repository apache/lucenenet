using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;
using Directory = Lucene.Net.Store.Directory;

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

    /// <summary>
    /// The single source of truth for the deterministic document set shared between
    /// Lucene.NET and Apache Lucene 4.8.1 (Java). This intentionally mirrors, field
    /// for field, the document schema produced by
    /// <c>TestBackwardsCompatibility.AddDoc</c> / <c>AddNoProxDoc</c> and the read
    /// back verification in <c>TestBackwardsCompatibility.SearchIndex</c>, so that an
    /// index written by either runtime can be validated by the other. See issue #270.
    /// <para/>
    /// Nothing here may use randomness: both runtimes must produce semantically
    /// identical indexes from the same inputs.
    /// </summary>
    internal static class CompatDocs
    {
        /// <summary>Number of "normal" (prox) documents indexed by <see cref="WriteIndex"/>.</summary>
        public const int DocCount = 35;

        /// <summary>The id that is deleted, matching the Java harness.</summary>
        public const int DeletedId = 7;

        // Exactly matches the Java literal: Lu + U+1D11E + ce + U+1D160 + ne + NUL + skull + astral + cd.
        public const string Utf8Value = "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\uD917\uDC17cd";
        public const string Content2Value = "here is more content with aaa aaa aaa";
        public const string NonAsciiFieldName = "fie\u2C77ld";
        public const string NonAsciiFieldValue = "field with non-ascii name";

        /// <summary>
        /// Writes the deterministic compatibility index into <paramref name="dir"/>. The
        /// result has 35 documents, with id 7 deleted, term vectors, offsets, norms, and
        /// the full DocValues matrix, in either compound-file or non-compound-file form.
        /// </summary>
        public static void WriteIndex(Directory dir, bool useCompoundFile)
        {
            Analyzer analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

            LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy
            {
                NoCFSRatio = useCompoundFile ? 1.0 : 0.0,
                MaxCFSSegmentSizeMB = double.PositiveInfinity
            };

            IndexWriterConfig conf = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
                // Force the real default 4.8.x codec. LuceneTestCase otherwise
                // randomizes Codec.Default to test-only impostors (e.g. a postings
                // format named "NestedPulsing") that a stock Lucene cannot load.
                .SetCodec(new Codecs.Lucene46.Lucene46Codec())
                .SetUseCompoundFile(useCompoundFile)
                .SetMaxBufferedDocs(10)
                .SetMergePolicy(mp);

            using (IndexWriter writer = new IndexWriter(dir, conf))
            {
                for (int i = 0; i < DocCount; i++)
                {
                    AddDoc(writer, i);
                }
            }

            // Delete id 7 in a fresh writer so the layout matches the Java harness.
            conf = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
                .SetCodec(new Codecs.Lucene46.Lucene46Codec())
                .SetUseCompoundFile(useCompoundFile)
                .SetMaxBufferedDocs(10)
                .SetOpenMode(OpenMode.APPEND);

            using (IndexWriter writer = new IndexWriter(dir, conf))
            {
                writer.DeleteDocuments(new Term("id", Convert.ToString(DeletedId)));
            }
        }

        private static void AddDoc(IndexWriter writer, int id)
        {
            Document doc = new Document();
            doc.Add(new TextField("content", "aaa", Field.Store.NO));
            doc.Add(new StringField("id", Convert.ToString(id), Field.Store.YES));

            FieldType customType2 = new FieldType(TextField.TYPE_STORED)
            {
                StoreTermVectors = true,
                StoreTermVectorPositions = true,
                StoreTermVectorOffsets = true
            };
            doc.Add(new Field("autf8", Utf8Value, customType2));
            doc.Add(new Field("utf8", Utf8Value, customType2));
            doc.Add(new Field("content2", Content2Value, customType2));
            doc.Add(new Field(NonAsciiFieldName, NonAsciiFieldValue, customType2));

            // numeric fields
            doc.Add(new Int32Field("trieInt", id, Field.Store.NO));
            doc.Add(new Int64Field("trieLong", id, Field.Store.NO));

            // docvalues fields
            doc.Add(new NumericDocValuesField("dvByte", (sbyte)id));
            sbyte[] bytes = { (sbyte)(id >>> 24), (sbyte)(id >>> 16), (sbyte)(id >>> 8), (sbyte)id };
            BytesRef @ref = new BytesRef((byte[])(Array)bytes);
            doc.Add(new BinaryDocValuesField("dvBytesDerefFixed", @ref));
            doc.Add(new BinaryDocValuesField("dvBytesDerefVar", @ref));
            doc.Add(new SortedDocValuesField("dvBytesSortedFixed", @ref));
            doc.Add(new SortedDocValuesField("dvBytesSortedVar", @ref));
            doc.Add(new BinaryDocValuesField("dvBytesStraightFixed", @ref));
            doc.Add(new BinaryDocValuesField("dvBytesStraightVar", @ref));
            doc.Add(new DoubleDocValuesField("dvDouble", id));
            doc.Add(new SingleDocValuesField("dvFloat", id));
            doc.Add(new NumericDocValuesField("dvInt", id));
            doc.Add(new NumericDocValuesField("dvLong", id));
            doc.Add(new NumericDocValuesField("dvPacked", id));
            doc.Add(new NumericDocValuesField("dvShort", (short)id));
            doc.Add(new SortedSetDocValuesField("dvSortedSet", @ref));

            // a field with both offsets and term vectors for a cross-check
            FieldType customType3 = new FieldType(TextField.TYPE_STORED)
            {
                StoreTermVectors = true,
                StoreTermVectorPositions = true,
                StoreTermVectorOffsets = true,
                IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
            };
            doc.Add(new Field("content5", Content2Value, customType3));

            // a field that omits only positions
            FieldType customType4 = new FieldType(TextField.TYPE_STORED)
            {
                StoreTermVectors = true,
                StoreTermVectorPositions = false,
                StoreTermVectorOffsets = true,
                IndexOptions = IndexOptions.DOCS_AND_FREQS
            };
            doc.Add(new Field("content6", Content2Value, customType4));

            writer.AddDocument(doc);
        }

        /// <summary>
        /// Runs the Lucene CheckIndex tool against <paramref name="dir"/> and throws if
        /// the index reports any problem. This is the codec integrity gate; it validates
        /// the stored per-file checksums written by the codec.
        /// </summary>
        public static void CheckIndex(Directory dir)
        {
            TestUtil.CheckIndex(dir);
        }

        /// <summary>
        /// The sorted, unique set of terms that <c>StandardAnalyzer(LUCENE_48)</c>
        /// produces for <see cref="Utf8Value"/>. An index written by either runtime must
        /// contain exactly this term set in the <c>utf8</c> field, which is the
        /// cross-runtime contract for the UTF-8 edge cases. Computed by re-running the
        /// analyzer so it stays correct if the constant changes.
        /// </summary>
        public static IList<string> ExpectedUtf8Terms()
        {
            var terms = new JCG.SortedSet<string>(StringComparer.Ordinal);
            using Analyzer analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            TokenStream ts = analyzer.GetTokenStream("utf8", new StringReader(Utf8Value));
            try
            {
                var termAttr = ts.AddAttribute<Analysis.TokenAttributes.ICharTermAttribute>();
                ts.Reset();
                while (ts.IncrementToken())
                {
                    terms.Add(termAttr.ToString());
                }
                ts.End();
            }
            finally
            {
                ts.Close();
            }
            return new JCG.List<string>(terms);
        }
    }
}
