using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

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
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    [TestFixture]
    public class TestOmitNorms : LuceneTestCase
    {
        // Tests whether the DocumentWriter correctly enable the
        // omitNorms bit in the FieldInfo
        [Test]
        public virtual void TestOmitNorms_Mem()
        {
            Directory ram = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document d = new Document();

            // this field will have norms
            Field f1 = NewTextField("f1", "this field has norms", Field.Store.NO);
            d.Add(f1);

            // this field will NOT have norms
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            Field f2 = NewField("f2", "this field has NO norms in all docs", customType);
            d.Add(f2);

            writer.AddDocument(d);
            writer.ForceMerge(1);
            // now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
            // keep things constant
            d = new Document();

            // Reverse
            d.Add(NewField("f1", "this field has norms", customType));

            d.Add(NewTextField("f2", "this field has NO norms in all docs", Field.Store.NO));

            writer.AddDocument(d);

            // force merge
            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
            FieldInfos fi = reader.FieldInfos;
            Assert.IsTrue(fi.FieldInfo("f1").OmitsNorms, "OmitNorms field bit should be set.");
            Assert.IsTrue(fi.FieldInfo("f2").OmitsNorms, "OmitNorms field bit should be set.");

            reader.Dispose();
            ram.Dispose();
        }

        // Tests whether merging of docs that have different
        // omitNorms for the same field works
        [Test]
        public virtual void TestMixedMerge()
        {
            Directory ram = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(3).SetMergePolicy(NewLogMergePolicy(2)));
            Document d = new Document();

            // this field will have norms
            Field f1 = NewTextField("f1", "this field has norms", Field.Store.NO);
            d.Add(f1);

            // this field will NOT have norms
            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            Field f2 = NewField("f2", "this field has NO norms in all docs", customType);
            d.Add(f2);

            for (int i = 0; i < 30; i++)
            {
                writer.AddDocument(d);
            }

            // now we add another document which has norms for field f2 and not for f1 and verify if the SegmentMerger
            // keep things constant
            d = new Document();

            // Reverese
            d.Add(NewField("f1", "this field has norms", customType));

            d.Add(NewTextField("f2", "this field has NO norms in all docs", Field.Store.NO));

            for (int i = 0; i < 30; i++)
            {
                writer.AddDocument(d);
            }

            // force merge
            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
            FieldInfos fi = reader.FieldInfos;
            Assert.IsTrue(fi.FieldInfo("f1").OmitsNorms, "OmitNorms field bit should be set.");
            Assert.IsTrue(fi.FieldInfo("f2").OmitsNorms, "OmitNorms field bit should be set.");

            reader.Dispose();
            ram.Dispose();
        }

        // Make sure first adding docs that do not omitNorms for
        // field X, then adding docs that do omitNorms for that same
        // field,
        [Test]
        public virtual void TestMixedRAM()
        {
            Directory ram = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10).SetMergePolicy(NewLogMergePolicy(2)));
            Document d = new Document();

            // this field will have norms
            Field f1 = NewTextField("f1", "this field has norms", Field.Store.NO);
            d.Add(f1);

            // this field will NOT have norms

            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            Field f2 = NewField("f2", "this field has NO norms in all docs", customType);
            d.Add(f2);

            for (int i = 0; i < 5; i++)
            {
                writer.AddDocument(d);
            }

            for (int i = 0; i < 20; i++)
            {
                writer.AddDocument(d);
            }

            // force merge
            writer.ForceMerge(1);

            // flush
            writer.Dispose();

            SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
            FieldInfos fi = reader.FieldInfos;
            Assert.IsTrue(!fi.FieldInfo("f1").OmitsNorms, "OmitNorms field bit should not be set.");
            Assert.IsTrue(fi.FieldInfo("f2").OmitsNorms, "OmitNorms field bit should be set.");

            reader.Dispose();
            ram.Dispose();
        }

        private void AssertNoNrm(Directory dir)
        {
            string[] files = dir.ListAll();
            for (int i = 0; i < files.Length; i++)
            {
                // TODO: this relies upon filenames
                Assert.IsFalse(files[i].EndsWith(".nrm", StringComparison.Ordinal) || files[i].EndsWith(".len", StringComparison.Ordinal));
            }
        }

        // Verifies no *.nrm exists when all fields omit norms:
        [Test]
        public virtual void TestNoNrmFile()
        {
            Directory ram = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(3).SetMergePolicy(NewLogMergePolicy()));
            LogMergePolicy lmp = (LogMergePolicy)writer.Config.MergePolicy;
            lmp.MergeFactor = 2;
            lmp.NoCFSRatio = 0.0;
            Document d = new Document();

            FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
            customType.OmitNorms = true;
            Field f1 = NewField("f1", "this field has no norms", customType);
            d.Add(f1);

            for (int i = 0; i < 30; i++)
            {
                writer.AddDocument(d);
            }

            writer.Commit();

            AssertNoNrm(ram);

            // force merge
            writer.ForceMerge(1);
            // flush
            writer.Dispose();

            AssertNoNrm(ram);
            ram.Dispose();
        }

        /// <summary>
        /// Tests various combinations of omitNorms=true/false, the field not existing at all,
        /// ensuring that only omitNorms is 'viral'.
        /// Internally checks that MultiNorms.norms() is consistent (returns the same bytes)
        /// as the fully merged equivalent.
        /// </summary>
        [Test]
        public virtual void TestOmitNormsCombos()
        {
            // indexed with norms
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            Field norms = new Field("foo", "a", customType);
            // indexed without norms
            FieldType customType1 = new FieldType(TextField.TYPE_STORED);
            customType1.OmitNorms = true;
            Field noNorms = new Field("foo", "a", customType1);
            // not indexed, but stored
            FieldType customType2 = new FieldType();
            customType2.IsStored = true;
            Field noIndex = new Field("foo", "a", customType2);
            // not indexed but stored, omitNorms is set
            FieldType customType3 = new FieldType();
            customType3.IsStored = true;
            customType3.OmitNorms = true;
            Field noNormsNoIndex = new Field("foo", "a", customType3);
            // not indexed nor stored (doesnt exist at all, we index a different field instead)
            Field emptyNorms = new Field("bar", "a", customType);

            Assert.IsNotNull(GetNorms("foo", norms, norms));
            Assert.IsNull(GetNorms("foo", norms, noNorms));
            Assert.IsNotNull(GetNorms("foo", norms, noIndex));
            Assert.IsNotNull(GetNorms("foo", norms, noNormsNoIndex));
            Assert.IsNotNull(GetNorms("foo", norms, emptyNorms));
            Assert.IsNull(GetNorms("foo", noNorms, noNorms));
            Assert.IsNull(GetNorms("foo", noNorms, noIndex));
            Assert.IsNull(GetNorms("foo", noNorms, noNormsNoIndex));
            Assert.IsNull(GetNorms("foo", noNorms, emptyNorms));
            Assert.IsNull(GetNorms("foo", noIndex, noIndex));
            Assert.IsNull(GetNorms("foo", noIndex, noNormsNoIndex));
            Assert.IsNull(GetNorms("foo", noIndex, emptyNorms));
            Assert.IsNull(GetNorms("foo", noNormsNoIndex, noNormsNoIndex));
            Assert.IsNull(GetNorms("foo", noNormsNoIndex, emptyNorms));
            Assert.IsNull(GetNorms("foo", emptyNorms, emptyNorms));
        }

        /// <summary>
        /// Indexes at least 1 document with f1, and at least 1 document with f2.
        /// returns the norms for "field".
        /// </summary>
        internal virtual NumericDocValues GetNorms(string field, Field f1, Field f2)
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter riw = new RandomIndexWriter(Random, dir, iwc);

            // add f1
            Document d = new Document();
            d.Add(f1);
            riw.AddDocument(d);

            // add f2
            d = new Document();
            d.Add(f2);
            riw.AddDocument(d);

            // add a mix of f1's and f2's
            int numExtraDocs = TestUtil.NextInt32(Random, 1, 1000);
            for (int i = 0; i < numExtraDocs; i++)
            {
                d = new Document();
                d.Add(Random.NextBoolean() ? f1 : f2);
                riw.AddDocument(d);
            }

            IndexReader ir1 = riw.GetReader();
            // todo: generalize
            NumericDocValues norms1 = MultiDocValues.GetNormValues(ir1, field);

            // fully merge and validate MultiNorms against single segment.
            riw.ForceMerge(1);
            DirectoryReader ir2 = riw.GetReader();
            NumericDocValues norms2 = GetOnlySegmentReader(ir2).GetNormValues(field);

            if (norms1 is null)
            {
                Assert.IsNull(norms2);
            }
            else
            {
                for (int docID = 0; docID < ir1.MaxDoc; docID++)
                {
                    Assert.AreEqual(norms1.Get(docID), norms2.Get(docID));
                }
            }
            ir1.Dispose();
            ir2.Dispose();
            riw.Dispose();
            dir.Dispose();
            return norms1;
        }
    }
}