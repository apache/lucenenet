using System;
using System.Globalization;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FailOnNonBulkMergesInfoStream = Lucene.Net.Util.FailOnNonBulkMergesInfoStream;
    using Field = Field;
    using FieldType = FieldType;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using StoredField = StoredField;
    using StringField = StringField;
    using TextField = TextField;

    [TestFixture]
    public class TestConsistentFieldNumbers : LuceneTestCase
    {
        [Test]
        public virtual void TestSameFieldNumbersAcrossSegments()
        {
            for (int i = 0; i < 2; i++)
            {
                Directory dir = NewDirectory();
                IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));

                Document d1 = new Document();
                d1.Add(new StringField("f1", "first field", Field.Store.YES));
                d1.Add(new StringField("f2", "second field", Field.Store.YES));
                writer.AddDocument(d1);

                if (i == 1)
                {
                    writer.Dispose();
                    writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
                }
                else
                {
                    writer.Commit();
                }

                Document d2 = new Document();
                FieldType customType2 = new FieldType(TextField.TYPE_STORED);
                customType2.StoreTermVectors = true;
                d2.Add(new TextField("f2", "second field", Field.Store.NO));
                d2.Add(new Field("f1", "first field", customType2));
                d2.Add(new TextField("f3", "third field", Field.Store.NO));
                d2.Add(new TextField("f4", "fourth field", Field.Store.NO));
                writer.AddDocument(d2);

                writer.Dispose();

                SegmentInfos sis = new SegmentInfos();
                sis.Read(dir);
                Assert.AreEqual(2, sis.Count);

                FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis[0]);
                FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis[1]);

                Assert.AreEqual("f1", fis1.FieldInfo(0).Name);
                Assert.AreEqual("f2", fis1.FieldInfo(1).Name);
                Assert.AreEqual("f1", fis2.FieldInfo(0).Name);
                Assert.AreEqual("f2", fis2.FieldInfo(1).Name);
                Assert.AreEqual("f3", fis2.FieldInfo(2).Name);
                Assert.AreEqual("f4", fis2.FieldInfo(3).Name);

                writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
                writer.ForceMerge(1);
                writer.Dispose();

                sis = new SegmentInfos();
                sis.Read(dir);
                Assert.AreEqual(1, sis.Count);

                FieldInfos fis3 = SegmentReader.ReadFieldInfos(sis[0]);

                Assert.AreEqual("f1", fis3.FieldInfo(0).Name);
                Assert.AreEqual("f2", fis3.FieldInfo(1).Name);
                Assert.AreEqual("f3", fis3.FieldInfo(2).Name);
                Assert.AreEqual("f4", fis3.FieldInfo(3).Name);

                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestAddIndexes()
        {
            Directory dir1 = NewDirectory();
            Directory dir2 = NewDirectory();
            IndexWriter writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));

            Document d1 = new Document();
            d1.Add(new TextField("f1", "first field", Field.Store.YES));
            d1.Add(new TextField("f2", "second field", Field.Store.YES));
            writer.AddDocument(d1);

            writer.Dispose();
            writer = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));

            Document d2 = new Document();
            FieldType customType2 = new FieldType(TextField.TYPE_STORED);
            customType2.StoreTermVectors = true;
            d2.Add(new TextField("f2", "second field", Field.Store.YES));
            d2.Add(new Field("f1", "first field", customType2));
            d2.Add(new TextField("f3", "third field", Field.Store.YES));
            d2.Add(new TextField("f4", "fourth field", Field.Store.YES));
            writer.AddDocument(d2);

            writer.Dispose();

            writer = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
            writer.AddIndexes(dir2);
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir1);
            Assert.AreEqual(2, sis.Count);

            FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis[0]);
            FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis[1]);

            Assert.AreEqual("f1", fis1.FieldInfo(0).Name);
            Assert.AreEqual("f2", fis1.FieldInfo(1).Name);
            // make sure the ordering of the "external" segment is preserved
            Assert.AreEqual("f2", fis2.FieldInfo(0).Name);
            Assert.AreEqual("f1", fis2.FieldInfo(1).Name);
            Assert.AreEqual("f3", fis2.FieldInfo(2).Name);
            Assert.AreEqual("f4", fis2.FieldInfo(3).Name);

            dir1.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestFieldNumberGaps()
        {
            int numIters = AtLeast(13);
            for (int i = 0; i < numIters; i++)
            {
                Directory dir = NewDirectory();
                {
                    IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
                    Document d = new Document();
                    d.Add(new TextField("f1", "d1 first field", Field.Store.YES));
                    d.Add(new TextField("f2", "d1 second field", Field.Store.YES));
                    writer.AddDocument(d);
                    writer.Dispose();
                    SegmentInfos sis = new SegmentInfos();
                    sis.Read(dir);
                    Assert.AreEqual(1, sis.Count);
                    FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis[0]);
                    Assert.AreEqual("f1", fis1.FieldInfo(0).Name);
                    Assert.AreEqual("f2", fis1.FieldInfo(1).Name);
                }

                {
                    IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(Random.NextBoolean() ? NoMergePolicy.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
                    Document d = new Document();
                    d.Add(new TextField("f1", "d2 first field", Field.Store.YES));
                    d.Add(new StoredField("f3", new byte[] { 1, 2, 3 }));
                    writer.AddDocument(d);
                    writer.Dispose();
                    SegmentInfos sis = new SegmentInfos();
                    sis.Read(dir);
                    Assert.AreEqual(2, sis.Count);
                    FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis[0]);
                    FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis[1]);
                    Assert.AreEqual("f1", fis1.FieldInfo(0).Name);
                    Assert.AreEqual("f2", fis1.FieldInfo(1).Name);
                    Assert.AreEqual("f1", fis2.FieldInfo(0).Name);
                    Assert.IsNull(fis2.FieldInfo(1));
                    Assert.AreEqual("f3", fis2.FieldInfo(2).Name);
                }

                {
                    IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(Random.NextBoolean() ? NoMergePolicy.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
                    Document d = new Document();
                    d.Add(new TextField("f1", "d3 first field", Field.Store.YES));
                    d.Add(new TextField("f2", "d3 second field", Field.Store.YES));
                    d.Add(new StoredField("f3", new byte[] { 1, 2, 3, 4, 5 }));
                    writer.AddDocument(d);
                    writer.Dispose();
                    SegmentInfos sis = new SegmentInfos();
                    sis.Read(dir);
                    Assert.AreEqual(3, sis.Count);
                    FieldInfos fis1 = SegmentReader.ReadFieldInfos(sis[0]);
                    FieldInfos fis2 = SegmentReader.ReadFieldInfos(sis[1]);
                    FieldInfos fis3 = SegmentReader.ReadFieldInfos(sis[2]);
                    Assert.AreEqual("f1", fis1.FieldInfo(0).Name);
                    Assert.AreEqual("f2", fis1.FieldInfo(1).Name);
                    Assert.AreEqual("f1", fis2.FieldInfo(0).Name);
                    Assert.IsNull(fis2.FieldInfo(1));
                    Assert.AreEqual("f3", fis2.FieldInfo(2).Name);
                    Assert.AreEqual("f1", fis3.FieldInfo(0).Name);
                    Assert.AreEqual("f2", fis3.FieldInfo(1).Name);
                    Assert.AreEqual("f3", fis3.FieldInfo(2).Name);
                }

                {
                    IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(Random.NextBoolean() ? NoMergePolicy.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
                    writer.DeleteDocuments(new Term("f1", "d1"));
                    // nuke the first segment entirely so that the segment with gaps is
                    // loaded first!
                    writer.ForceMergeDeletes();
                    writer.Dispose();
                }

                IndexWriter writer_ = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(new LogByteSizeMergePolicy()).SetInfoStream(new FailOnNonBulkMergesInfoStream()));
                writer_.ForceMerge(1);
                writer_.Dispose();

                SegmentInfos sis_ = new SegmentInfos();
                sis_.Read(dir);
                Assert.AreEqual(1, sis_.Count);
                FieldInfos fis1_ = SegmentReader.ReadFieldInfos(sis_[0]);
                Assert.AreEqual("f1", fis1_.FieldInfo(0).Name);
                Assert.AreEqual("f2", fis1_.FieldInfo(1).Name);
                Assert.AreEqual("f3", fis1_.FieldInfo(2).Name);
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestManyFields()
        {
            int NUM_DOCS = AtLeast(200);
            int MAX_FIELDS = AtLeast(50);

            int[][] docs = RectangularArrays.ReturnRectangularArray<int>(NUM_DOCS, 4);
            for (int i = 0; i < docs.Length; i++)
            {
                for (int j = 0; j < docs[i].Length; j++)
                {
                    docs[i][j] = Random.Next(MAX_FIELDS);
                }
            }

            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            for (int i = 0; i < NUM_DOCS; i++)
            {
                Document d = new Document();
                for (int j = 0; j < docs[i].Length; j++)
                {
                    d.Add(GetField(docs[i][j]));
                }

                writer.AddDocument(d);
            }

            writer.ForceMerge(1);
            writer.Dispose();

            SegmentInfos sis = new SegmentInfos();
            sis.Read(dir);
            foreach (SegmentCommitInfo si in sis.Segments)
            {
                FieldInfos fis = SegmentReader.ReadFieldInfos(si);

                foreach (FieldInfo fi in fis)
                {
                    Field expected = GetField(Convert.ToInt32(fi.Name));
                    Assert.AreEqual(expected.FieldType.IsIndexed, fi.IsIndexed);
                    Assert.AreEqual(expected.FieldType.StoreTermVectors, fi.HasVectors);
                }
            }

            dir.Dispose();
        }

        private Field GetField(int number)
        {
            int mode = number % 16;
            string fieldName = "" + number;
            FieldType customType = new FieldType(TextField.TYPE_STORED);

            FieldType customType2 = new FieldType(TextField.TYPE_STORED);
            customType2.IsTokenized = false;

            FieldType customType3 = new FieldType(TextField.TYPE_NOT_STORED);
            customType3.IsTokenized = false;

            FieldType customType4 = new FieldType(TextField.TYPE_NOT_STORED);
            customType4.IsTokenized = false;
            customType4.StoreTermVectors = true;
            customType4.StoreTermVectorOffsets = true;

            FieldType customType5 = new FieldType(TextField.TYPE_NOT_STORED);
            customType5.StoreTermVectors = true;
            customType5.StoreTermVectorOffsets = true;

            FieldType customType6 = new FieldType(TextField.TYPE_STORED);
            customType6.IsTokenized = false;
            customType6.StoreTermVectors = true;
            customType6.StoreTermVectorOffsets = true;

            FieldType customType7 = new FieldType(TextField.TYPE_NOT_STORED);
            customType7.IsTokenized = false;
            customType7.StoreTermVectors = true;
            customType7.StoreTermVectorOffsets = true;

            FieldType customType8 = new FieldType(TextField.TYPE_STORED);
            customType8.IsTokenized = false;
            customType8.StoreTermVectors = true;
            customType8.StoreTermVectorPositions = true;

            FieldType customType9 = new FieldType(TextField.TYPE_NOT_STORED);
            customType9.StoreTermVectors = true;
            customType9.StoreTermVectorPositions = true;

            FieldType customType10 = new FieldType(TextField.TYPE_STORED);
            customType10.IsTokenized = false;
            customType10.StoreTermVectors = true;
            customType10.StoreTermVectorPositions = true;

            FieldType customType11 = new FieldType(TextField.TYPE_NOT_STORED);
            customType11.IsTokenized = false;
            customType11.StoreTermVectors = true;
            customType11.StoreTermVectorPositions = true;

            FieldType customType12 = new FieldType(TextField.TYPE_STORED);
            customType12.StoreTermVectors = true;
            customType12.StoreTermVectorOffsets = true;
            customType12.StoreTermVectorPositions = true;

            FieldType customType13 = new FieldType(TextField.TYPE_NOT_STORED);
            customType13.StoreTermVectors = true;
            customType13.StoreTermVectorOffsets = true;
            customType13.StoreTermVectorPositions = true;

            FieldType customType14 = new FieldType(TextField.TYPE_STORED);
            customType14.IsTokenized = false;
            customType14.StoreTermVectors = true;
            customType14.StoreTermVectorOffsets = true;
            customType14.StoreTermVectorPositions = true;

            FieldType customType15 = new FieldType(TextField.TYPE_NOT_STORED);
            customType15.IsTokenized = false;
            customType15.StoreTermVectors = true;
            customType15.StoreTermVectorOffsets = true;
            customType15.StoreTermVectorPositions = true;

            switch (mode)
            {
                case 0:
                    return new Field(fieldName, "some text", customType);

                case 1:
                    return new TextField(fieldName, "some text", Field.Store.NO);

                case 2:
                    return new Field(fieldName, "some text", customType2);

                case 3:
                    return new Field(fieldName, "some text", customType3);

                case 4:
                    return new Field(fieldName, "some text", customType4);

                case 5:
                    return new Field(fieldName, "some text", customType5);

                case 6:
                    return new Field(fieldName, "some text", customType6);

                case 7:
                    return new Field(fieldName, "some text", customType7);

                case 8:
                    return new Field(fieldName, "some text", customType8);

                case 9:
                    return new Field(fieldName, "some text", customType9);

                case 10:
                    return new Field(fieldName, "some text", customType10);

                case 11:
                    return new Field(fieldName, "some text", customType11);

                case 12:
                    return new Field(fieldName, "some text", customType12);

                case 13:
                    return new Field(fieldName, "some text", customType13);

                case 14:
                    return new Field(fieldName, "some text", customType14);

                case 15:
                    return new Field(fieldName, "some text", customType15);

                default:
                    return null;
            }
        }

        [Test]
        [LuceneNetSpecific]
        public void TestSegmentNumberToStringGeneration()
        {
            // We cover the 100 literal values that we return plus an additional 5 to ensure continuation
            const long MaxSegment = 105;

            bool temp = SegmentInfos.UseLegacySegmentNames;
            try
            {
                // Normal usage
                SegmentInfos.UseLegacySegmentNames = false;
                for (long seg = 0; seg < MaxSegment; seg++)
                {
                    string expected = J2N.IntegralNumberExtensions.ToString(seg, J2N.Character.MaxRadix);
                    string actual = SegmentInfos.SegmentNumberToString(seg);
                    Assert.AreEqual(expected, actual);
                }

                // This is for places where we were generating the names correctly. We don't want to flip
                // to radix 10 when the feature is enabled here.
                SegmentInfos.UseLegacySegmentNames = true;
                for (long seg = 0; seg < MaxSegment; seg++)
                {
                    string expected = J2N.IntegralNumberExtensions.ToString(seg, J2N.Character.MaxRadix);
                    string actual = SegmentInfos.SegmentNumberToString(seg, allowLegacyNames: false);
                    Assert.AreEqual(expected, actual);
                }

                // This is to generate names with radix 10 (to read indexes from beta 1 thru 15 only)
                SegmentInfos.UseLegacySegmentNames = true;
                for (long seg = 0; seg < MaxSegment; seg++)
                {
                    string expected = seg.ToString(CultureInfo.InvariantCulture);
                    string actual = SegmentInfos.SegmentNumberToString(seg);
                    Assert.AreEqual(expected, actual);
                }
            }
            finally
            {
                SegmentInfos.UseLegacySegmentNames = temp;
            }
        }
    }
}