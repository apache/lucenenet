using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Field = Lucene.Net.Documents.Field;
using RandomInts = RandomizedTesting.Generators.RandomNumbers;

namespace Lucene.Net.Codecs.Compressing
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

    using BaseStoredFieldsFormatTestCase = Lucene.Net.Index.BaseStoredFieldsFormatTestCase;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using Int32Field = Int32Field;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    // LUCENENET: Moved @Repeat(iterations=5) to the method level
    [TestFixture]
    public class TestCompressingStoredFieldsFormat : BaseStoredFieldsFormatTestCase
    {
        // LUCENENET specific - repeat count is a constant for reuse below
        private const int RepeatCount = 5;

        protected override Codec GetCodec()
        {
            return CompressingCodec.RandomInstance(Random);
        }

        [Test]
        [Repeat(RepeatCount)] // LUCENENET: moved from class annotation due to NUnit's RepeatAttribute not allowed at class level
        public virtual void TestDeletePartiallyWrittenFilesIfAbort()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwConf.SetMaxBufferedDocs(RandomInts.RandomInt32Between(Random, 2, 30));
            iwConf.SetCodec(CompressingCodec.RandomInstance(Random));
            // disable CFS because this test checks file names
            iwConf.SetMergePolicy(NewLogMergePolicy(false));
            iwConf.SetUseCompoundFile(false);
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, iwConf);

            Document validDoc = new Document();
            validDoc.Add(new Int32Field("id", 0, Field.Store.YES));
            iw.AddDocument(validDoc);
            iw.Commit();

            // make sure that #writeField will fail to trigger an abort
            Document invalidDoc = new Document();
            FieldType fieldType = new FieldType();
            fieldType.IsStored = true;
            invalidDoc.Add(new FieldAnonymousClass(fieldType));

            try
            {
                Assert.Throws<ArgumentException>(() => {
                    iw.AddDocument(invalidDoc);
                    iw.Commit();
                });
            }
            finally
            {
                int counter = 0;
                foreach (string fileName in dir.ListAll())
                {
                    if (fileName.EndsWith(".fdt", StringComparison.Ordinal) || fileName.EndsWith(".fdx", StringComparison.Ordinal))
                    {
                        counter++;
                    }
                }
                // Only one .fdt and one .fdx files must have been found
                Assert.AreEqual(2, counter);
                iw.Dispose();
                dir.Dispose();
            }
        }

        private sealed class FieldAnonymousClass : Field
        {
            public FieldAnonymousClass(FieldType fieldType)
                : base("invalid", fieldType)
            {
            }

            public override string GetStringValue() => null;
        }

        #region LUCENENET specific repeating overrides
        // LUCENENET specific: these overrides are needed to be able to add the RepeatAttribute to the tests,
        // to match Lucene's behavior with the Repeat annotation at the class level.
        // This region can be removed if we create a custom NUnit attribute that allows this behavior.

        [Repeat(RepeatCount)]
        public override void TestBigDocuments() => base.TestBigDocuments();

        [Repeat(RepeatCount)]
        public override void TestConcurrentReads() => base.TestConcurrentReads();

        [Repeat(RepeatCount)]
        public override void TestEmptyDocs() => base.TestEmptyDocs();

        [Repeat(RepeatCount)]
        public override void TestIndexedBit() => base.TestIndexedBit();

        [Repeat(RepeatCount)]
        public override void TestMergeStability() => base.TestMergeStability();

        [Repeat(RepeatCount)]
        public override void TestNumericField() => base.TestNumericField();

        [Repeat(RepeatCount)]
        public override void TestReadSkip() => base.TestReadSkip();

        [Repeat(RepeatCount)]
        public override void TestRandomStoredFields() => base.TestRandomStoredFields();

        [Repeat(RepeatCount)]
        public override void TestStoredFieldsOrder() => base.TestStoredFieldsOrder();

        [Repeat(RepeatCount)]
        public override void TestWriteReadMerge() => base.TestWriteReadMerge();

        [Repeat(RepeatCount)]
        public override void TestBinaryFieldOffsetLength() => base.TestBinaryFieldOffsetLength();

        [Repeat(RepeatCount)]
        public override void TestBulkMergeWithDeletes() => base.TestBulkMergeWithDeletes();

        #endregion
    }
}
