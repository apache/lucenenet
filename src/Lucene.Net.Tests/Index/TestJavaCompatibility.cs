using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
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
    /// Verifies two-way index/codec compatibility between Lucene.NET and Apache
    /// Lucene 4.8.1 (Java).
    /// <para/>
    /// This is the .NET half of the harness in <c>src/java/index-compat</c>. The
    /// shared, deterministic document set defined here mirrors, field for field,
    /// the Java <c>CompatDocs</c> class (which in turn mirrors the schema in
    /// <c>TestBackwardsCompatibility</c>). Each runtime writes an index that
    /// the other opens, validates with <see cref="CheckIndex"/>, and reads back.
    /// <para/>
    /// We do not do byte-for-byte file comparison: some header fields legitimately
    /// differ between runtimes (for example <c>java.vendor</c>), so
    /// <see cref="CheckIndex"/> plus a semantic read back is the agreed approach.
    /// <para/>
    /// Two environment variables drive the cross-runtime directions, both set by
    /// the <c>run-compat</c> driver script:
    /// <list type="bullet">
    /// <item><description><c>lucenenet.compat.read.dir</c> - a Java-written index to
    /// read. If unset or missing, the read test is <b>inconclusive</b> (skipped),
    /// because a JDK may not be present in every environment.</description></item>
    /// <item><description><c>lucenenet.compat.write.dir</c> - where to write the .NET
    /// index for Java to read. Defaults to a temp directory.</description></item>
    /// </list>
    /// </summary>
    [LuceneNetSpecific]
    [TestFixture]
    public class TestJavaCompatibility : LuceneTestCase
    {
        internal const int DocCount = 35;
        internal const int DeletedId = 7;

        // Exactly matches the Java CompatDocs.UTF8_VALUE and the .NET
        // TestBackwardsCompatibility utf8 literal.
        internal const string Utf8Value = "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\uD917\uDC17cd";
        internal const string Content2Value = "here is more content with aaa aaa aaa";
        internal const string NonAsciiFieldName = "fie\u2C77ld";
        internal const string NonAsciiFieldValue = "field with non-ascii name";

        private const string ReadDirEnvVar = "lucenenet.compat.read.dir";
        private const string WriteDirEnvVar = "lucenenet.compat.write.dir";

        // ------------------------------------------------------------------
        // Shared document set (mirror of Java CompatDocs)
        // ------------------------------------------------------------------

        /// <summary>
        /// Writes the deterministic compatibility index into <paramref name="dir"/>.
        /// Mirrors <c>CompatDocs.writeIndex</c> on the Java side: 35 documents, id 7
        /// deleted, term vectors, offsets, norms, and the full DocValues matrix.
        /// Uses no randomness so both runtimes produce semantically identical output.
        /// </summary>
        internal static void WriteIndex(Directory dir, bool useCompoundFile)
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
        /// The sorted, unique set of terms that <c>StandardAnalyzer(LUCENE_48)</c>
        /// produces for <see cref="Utf8Value"/>, computed by re-running the analyzer
        /// so it stays correct if the constant changes.
        /// <para/>
        /// This is evaluated in whichever runtime is reading the index. When the
        /// reader and writer are different runtimes (the cross-runtime directions),
        /// comparing this against the terms actually stored in the index proves the
        /// two analyzers agree on the UTF-8 edge cases. When the same runtime both
        /// wrote and read (the pure-.NET round trip), expected and actual share an
        /// origin, so this degrades to a self-consistency check.
        /// </summary>
        internal static IList<string> ExpectedUtf8Terms()
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

        // ------------------------------------------------------------------
        // Verification (mirror of Java ReadDotNetIndexTest.assertContents)
        // ------------------------------------------------------------------

        internal static void AssertContents(Directory dir)
        {
            // Codec integrity gate (validates the per-file checksums the codec wrote).
            TestUtil.CheckIndex(dir);

            using DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);

            IBits liveDocs = MultiFields.GetLiveDocs(reader);

            for (int i = 0; i < DocCount; i++)
            {
                bool live = liveDocs is null || liveDocs.Get(i);
                if (!live)
                {
                    Assert.AreEqual(DeletedId, i, "only id 7 should be deleted");
                    continue;
                }

                Document d = reader.Document(i);
                Assert.AreEqual(Convert.ToString(i), d.Get("id"), "id");
                Assert.AreEqual(Utf8Value, d.Get("utf8"), "utf8");
                Assert.AreEqual(Utf8Value, d.Get("autf8"), "autf8");
                Assert.AreEqual(Content2Value, d.Get("content2"), "content2");
                Assert.AreEqual(NonAsciiFieldValue, d.Get(NonAsciiFieldName), NonAsciiFieldName);

                Fields tvFields = reader.GetTermVectors(i);
                Assert.IsNotNull(tvFields, "term vectors missing for doc " + i);
                Assert.IsNotNull(tvFields.GetTerms("utf8"), "utf8 term vector missing for doc " + i);
            }

            AssertDocValues(reader, liveDocs);

            // content term should match every live doc (34 of 35).
            ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
            Assert.AreEqual(DocCount - 1, hits.Length);
            Assert.AreEqual("0", searcher.IndexReader.Document(hits[0].Doc).Get("id"), "first hit should be id 0");

            // offsets/positions-bearing fields (StandardAnalyzer keeps "aaa" verbatim).
            Assert.AreEqual(DocCount - 1,
                searcher.Search(new TermQuery(new Term("content5", "aaa")), null, 1000).ScoreDocs.Length);
            Assert.AreEqual(DocCount - 1,
                searcher.Search(new TermQuery(new Term("content6", "aaa")), null, 1000).ScoreDocs.Length);

            // The utf8 field's term dictionary must be identical to what the writing
            // runtime produced. This is the cross-runtime contract for the UTF-8 edge
            // cases (astral planes, the skull, etc.).
            Assert.AreEqual(ExpectedUtf8Terms(), CollectTerms(reader, "utf8"), "utf8 term set");
        }

        private static JCG.List<string> CollectTerms(IndexReader reader, string field)
        {
            var result = new JCG.List<string>();
            Terms terms = MultiFields.GetTerms(reader, field);
            if (terms is null)
            {
                return result;
            }
            TermsEnum te = terms.GetEnumerator();
            while (te.MoveNext())
            {
                result.Add(te.Term.Utf8ToString());
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static void AssertDocValues(IndexReader reader, IBits liveDocs)
        {
            NumericDocValues dvByte = MultiDocValues.GetNumericValues(reader, "dvByte");
            BinaryDocValues dvBytesDerefFixed = MultiDocValues.GetBinaryValues(reader, "dvBytesDerefFixed");
            BinaryDocValues dvBytesDerefVar = MultiDocValues.GetBinaryValues(reader, "dvBytesDerefVar");
            SortedDocValues dvBytesSortedFixed = MultiDocValues.GetSortedValues(reader, "dvBytesSortedFixed");
            SortedDocValues dvBytesSortedVar = MultiDocValues.GetSortedValues(reader, "dvBytesSortedVar");
            BinaryDocValues dvBytesStraightFixed = MultiDocValues.GetBinaryValues(reader, "dvBytesStraightFixed");
            BinaryDocValues dvBytesStraightVar = MultiDocValues.GetBinaryValues(reader, "dvBytesStraightVar");
            NumericDocValues dvDouble = MultiDocValues.GetNumericValues(reader, "dvDouble");
            NumericDocValues dvFloat = MultiDocValues.GetNumericValues(reader, "dvFloat");
            NumericDocValues dvInt = MultiDocValues.GetNumericValues(reader, "dvInt");
            NumericDocValues dvLong = MultiDocValues.GetNumericValues(reader, "dvLong");
            NumericDocValues dvPacked = MultiDocValues.GetNumericValues(reader, "dvPacked");
            NumericDocValues dvShort = MultiDocValues.GetNumericValues(reader, "dvShort");
            SortedSetDocValues dvSortedSet = MultiDocValues.GetSortedSetValues(reader, "dvSortedSet");

            Assert.IsNotNull(dvByte, "dvByte");
            Assert.IsNotNull(dvSortedSet, "dvSortedSet");

            for (int i = 0; i < DocCount; i++)
            {
                bool live = liveDocs is null || liveDocs.Get(i);
                if (!live)
                {
                    continue;
                }
                int id = Convert.ToInt32(reader.Document(i).Get("id"));
                Assert.AreEqual(id, dvByte.Get(i), "dvByte");

                sbyte[] bytes = { (sbyte)(id >>> 24), (sbyte)(id >>> 16), (sbyte)(id >>> 8), (sbyte)id };
                BytesRef expectedRef = new BytesRef((byte[])(Array)bytes);
                BytesRef scratch = new BytesRef();

                dvBytesDerefFixed.Get(i, scratch);
                Assert.AreEqual(expectedRef, scratch, "dvBytesDerefFixed");
                dvBytesDerefVar.Get(i, scratch);
                Assert.AreEqual(expectedRef, scratch, "dvBytesDerefVar");
                dvBytesSortedFixed.Get(i, scratch);
                Assert.AreEqual(expectedRef, scratch, "dvBytesSortedFixed");
                dvBytesSortedVar.Get(i, scratch);
                Assert.AreEqual(expectedRef, scratch, "dvBytesSortedVar");
                dvBytesStraightFixed.Get(i, scratch);
                Assert.AreEqual(expectedRef, scratch, "dvBytesStraightFixed");
                dvBytesStraightVar.Get(i, scratch);
                Assert.AreEqual(expectedRef, scratch, "dvBytesStraightVar");

                Assert.AreEqual(id, J2N.BitConversion.Int64BitsToDouble(dvDouble.Get(i)), 0D, "dvDouble");
                Assert.AreEqual(id, J2N.BitConversion.Int32BitsToSingle((int)dvFloat.Get(i)), 0F, "dvFloat");
                Assert.AreEqual(id, dvInt.Get(i), "dvInt");
                Assert.AreEqual(id, dvLong.Get(i), "dvLong");
                Assert.AreEqual(id, dvPacked.Get(i), "dvPacked");
                Assert.AreEqual(id, dvShort.Get(i), "dvShort");

                dvSortedSet.SetDocument(i);
                long ord = dvSortedSet.NextOrd();
                Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dvSortedSet.NextOrd(), "dvSortedSet single ord");
                dvSortedSet.LookupOrd(ord, scratch);
                Assert.AreEqual(expectedRef, scratch, "dvSortedSet value");
            }
        }

        // ------------------------------------------------------------------
        // Tests
        // ------------------------------------------------------------------

        /// <summary>
        /// Pure-.NET round trip: write the shared doc set with the default 4.8.x
        /// codec, reopen it, run CheckIndex, and assert the contents. This is the
        /// CI-safe baseline that requires no JDK and guards the shared contract.
        /// </summary>
        [Test]
        public virtual void TestDotNetRoundTrip()
        {
            foreach (bool useCompoundFile in new[] { true, false })
            {
                using Directory dir = NewDirectory();
                WriteIndex(dir, useCompoundFile);
                AssertContents(dir);
            }
        }

        /// <summary>
        /// Java -&gt; .NET direction: open a Java 4.8.1-written index (path from the
        /// <c>lucenenet.compat.read.dir</c> environment variable), run CheckIndex,
        /// and assert the contents. If the variable is unset or the index is missing,
        /// the test is inconclusive (skipped): a JDK may not be present here.
        /// </summary>
        [Test]
        public virtual void TestReadJavaIndex()
        {
            string baseDir = Environment.GetEnvironmentVariable(ReadDirEnvVar);
            if (string.IsNullOrEmpty(baseDir) || !System.IO.Directory.Exists(baseDir))
            {
                NUnit.Framework.Assert.Inconclusive($"No Java index to read. Set the '{ReadDirEnvVar}' environment " +
                    "variable to a Java-generated index directory (see src/java/index-compat/README.md).");
                return;
            }

            bool readAny = false;
            foreach (string name in new[] { "index.481.nocfs", "index.481.cfs" })
            {
                string indexDir = Path.Combine(baseDir, name);
                if (!System.IO.Directory.Exists(indexDir) || !HasSegments(indexDir))
                {
                    continue;
                }
                readAny = true;
                using Directory dir = new SimpleFSDirectory(indexDir);
                AssertContents(dir);
            }

            if (!readAny)
            {
                NUnit.Framework.Assert.Inconclusive($"No Lucene index found under '{baseDir}'. " +
                    "Generate the Java index first (see src/java/index-compat/README.md).");
            }
        }

        /// <summary>
        /// .NET -&gt; Java direction (writer half): write the shared doc set into the
        /// <c>lucenenet.compat.write.dir</c> folder (or a temp folder) so the Java
        /// JUnit test can open it. The Java side performs the cross-runtime
        /// assertions; this test just produces the index and self-validates it.
        /// </summary>
        [Test]
        public virtual void TestWriteIndexForJava()
        {
            string baseDir = Environment.GetEnvironmentVariable(WriteDirEnvVar);
            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = Path.Combine(Path.GetTempPath(), "lucenenet-compat-" + Guid.NewGuid().ToString("N"));
            }
            System.IO.Directory.CreateDirectory(baseDir);

            foreach ((string name, bool useCompoundFile) in new[] { ("index.481.cfs", true), ("index.481.nocfs", false) })
            {
                string indexDir = Path.Combine(baseDir, name);
                if (System.IO.Directory.Exists(indexDir))
                {
                    System.IO.Directory.Delete(indexDir, recursive: true);
                }
                System.IO.Directory.CreateDirectory(indexDir);
                using Directory dir = new SimpleFSDirectory(indexDir);
                WriteIndex(dir, useCompoundFile);
                AssertContents(dir);
            }

            TestContext.Progress.WriteLine("Wrote .NET 4.8.x compatibility indexes under: " + baseDir);
        }

        private static bool HasSegments(string indexDir)
        {
            foreach (string f in System.IO.Directory.EnumerateFiles(indexDir))
            {
                if (Path.GetFileName(f).StartsWith("segments_", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
