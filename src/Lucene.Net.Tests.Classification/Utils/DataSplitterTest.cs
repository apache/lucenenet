using System;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Classification.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Classification
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

    /**
     * Testcase for <see cref="DatasetSplitter"/>
     */
    public class DataSplitterTest : Util.LuceneTestCase
    {
        private AtomicReader originalIndex;
        private RandomIndexWriter indexWriter;
        private Directory dir;

        private String textFieldName = "text";
        private String classFieldName = "class";
        private String idFieldName = "id";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            indexWriter = new RandomIndexWriter(Random, dir, new MockAnalyzer(Random));

            FieldType ft = new FieldType(TextField.TYPE_STORED);
            ft.StoreTermVectors = true;
            ft.StoreTermVectorOffsets = true;
            ft.StoreTermVectorPositions = true;

            Analyzer analyzer = new MockAnalyzer(Random);

            Document doc;
            for (int i = 0; i < 100; i++)
            {
                doc = new Document();
                doc.Add(new Field(idFieldName, Random.toString(), ft));
                doc.Add(new Field(textFieldName, new StringBuilder(Random.toString()).append(Random.toString()).append(
                    Random.toString()).toString(), ft));
                doc.Add(new Field(classFieldName, Random.toString(), ft));
                indexWriter.AddDocument(doc, analyzer);
            }

            indexWriter.Commit();

            originalIndex = SlowCompositeReaderWrapper.Wrap(indexWriter.GetReader());
        }

        [TearDown]
        public override void TearDown()
        {
            originalIndex.Dispose();
            indexWriter.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        [Test]
        public void TestSplitOnAllFields()
        {
            AssertSplit(originalIndex, 0.1, 0.1);
        }


        [Test]
        public void TestSplitOnSomeFields()
        {
            AssertSplit(originalIndex, 0.2, 0.35, idFieldName, textFieldName);
        }

        public static void AssertSplit(AtomicReader originalIndex, double testRatio, double crossValidationRatio, params string[] fieldNames)
        {
            BaseDirectoryWrapper trainingIndex = NewDirectory();
            BaseDirectoryWrapper testIndex = NewDirectory();
            BaseDirectoryWrapper crossValidationIndex = NewDirectory();

            try
            {
                DatasetSplitter datasetSplitter = new DatasetSplitter(testRatio, crossValidationRatio);
                datasetSplitter.Split(originalIndex, trainingIndex, testIndex, crossValidationIndex, new MockAnalyzer(Random), fieldNames);

                Assert.NotNull(trainingIndex);
                Assert.NotNull(testIndex);
                Assert.NotNull(crossValidationIndex);

                DirectoryReader trainingReader = DirectoryReader.Open(trainingIndex);
                Assert.True((int)(originalIndex.MaxDoc * (1d - testRatio - crossValidationRatio)) == trainingReader.MaxDoc);
                DirectoryReader testReader = DirectoryReader.Open(testIndex);
                Assert.True((int)(originalIndex.MaxDoc * testRatio) == testReader.MaxDoc);
                DirectoryReader cvReader = DirectoryReader.Open(crossValidationIndex);
                Assert.True((int)(originalIndex.MaxDoc * crossValidationRatio) == cvReader.MaxDoc);

                trainingReader.Dispose();
                testReader.Dispose();
                cvReader.Dispose();
                CloseQuietly(trainingReader);
                CloseQuietly(testReader);
                CloseQuietly(cvReader);
            }
            finally
            {
                trainingIndex.Dispose();
                testIndex.Dispose();
                crossValidationIndex.Dispose();
            }
        }

        private static void CloseQuietly(IndexReader reader)
        {
            try
            {
                if (reader != null)
                    reader.Dispose();
            }
            catch (Exception e) when (e.IsException())
            {
                // do nothing
            }
        }
    }
}