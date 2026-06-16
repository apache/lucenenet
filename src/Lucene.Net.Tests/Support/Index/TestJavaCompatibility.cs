using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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
    /// shared, deterministic document set lives in <see cref="CompatDocs"/>, which
    /// mirrors, field for field, the Java <c>CompatDocs</c> class (which in turn
    /// mirrors the schema in <c>TestBackwardsCompatibility</c>). Each runtime writes
    /// an index that the other opens, validates with <see cref="CheckIndex"/>, and
    /// reads back.
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
        private const string ReadDirEnvVar = "lucenenet.compat.read.dir";
        private const string WriteDirEnvVar = "lucenenet.compat.write.dir";

        // ------------------------------------------------------------------
        // Verification (mirror of Java TestDotNetCompatibility.assertContents)
        // ------------------------------------------------------------------

        internal static void AssertContents(Directory dir)
        {
            // Codec integrity gate (validates the per-file checksums the codec wrote).
            CompatDocs.CheckIndex(dir);

            using DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);

            IBits liveDocs = MultiFields.GetLiveDocs(reader);

            for (int i = 0; i < CompatDocs.DocCount; i++)
            {
                bool live = liveDocs is null || liveDocs.Get(i);
                if (!live)
                {
                    Assert.AreEqual(CompatDocs.DeletedId, i, "only id 7 should be deleted");
                    continue;
                }

                Document d = reader.Document(i);
                Assert.AreEqual(Convert.ToString(i), d.Get("id"), "id");
                Assert.AreEqual(CompatDocs.Utf8Value, d.Get("utf8"), "utf8");
                Assert.AreEqual(CompatDocs.Utf8Value, d.Get("autf8"), "autf8");
                Assert.AreEqual(CompatDocs.Content2Value, d.Get("content2"), "content2");
                Assert.AreEqual(CompatDocs.NonAsciiFieldValue, d.Get(CompatDocs.NonAsciiFieldName), CompatDocs.NonAsciiFieldName);

                Fields tvFields = reader.GetTermVectors(i);
                Assert.IsNotNull(tvFields, "term vectors missing for doc " + i);
                Assert.IsNotNull(tvFields.GetTerms("utf8"), "utf8 term vector missing for doc " + i);
            }

            AssertDocValues(reader, liveDocs);

            // content term should match every live doc (34 of 35).
            ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
            Assert.AreEqual(CompatDocs.DocCount - 1, hits.Length);
            Assert.AreEqual("0", searcher.IndexReader.Document(hits[0].Doc).Get("id"), "first hit should be id 0");

            // offsets/positions-bearing fields (StandardAnalyzer keeps "aaa" verbatim).
            Assert.AreEqual(CompatDocs.DocCount - 1,
                searcher.Search(new TermQuery(new Term("content5", "aaa")), null, 1000).ScoreDocs.Length);
            Assert.AreEqual(CompatDocs.DocCount - 1,
                searcher.Search(new TermQuery(new Term("content6", "aaa")), null, 1000).ScoreDocs.Length);

            // The utf8 field's term dictionary must be identical to what the writing
            // runtime produced. This is the cross-runtime contract for the UTF-8 edge
            // cases (astral planes, the skull, etc.).
            Assert.AreEqual(CompatDocs.ExpectedUtf8Terms(), CollectTerms(reader, "utf8"), "utf8 term set");
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

            for (int i = 0; i < CompatDocs.DocCount; i++)
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
                CompatDocs.WriteIndex(dir, useCompoundFile);
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
                CompatDocs.WriteIndex(dir, useCompoundFile);
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
