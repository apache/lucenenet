using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

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

    public class IndexSortingTest : SorterTestBase
    {
        private static readonly Sort[] SORT = new Sort[]
        {
            new Sort(new SortField(NUMERIC_DV_FIELD, SortField.Type_e.LONG)),
            new Sort(new SortField(null, SortField.Type_e.DOC, true))
        };

        [OneTimeSetUp]
        public void BeforeClassSorterUtilTest()
        {
            // only read the values of the undeleted documents, since after addIndexes,
            // the deleted ones will be dropped from the index.
            Bits liveDocs = reader.LiveDocs;
            List<int> values = new List<int>();
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                if (liveDocs == null || liveDocs.Get(i))
                {
                    values.Add(int.Parse(reader.Document(i).Get(ID_FIELD), CultureInfo.InvariantCulture));
                }
            }
            int idx = Random().nextInt(SORT.Length);
            Sort sorter = SORT[idx];
            if (idx == 1)
            { // reverse doc sort
                values.Reverse();
            }
            else
            {
                values.Sort();
                if (Random().nextBoolean())
                {
                    sorter = new Sort(new SortField(NUMERIC_DV_FIELD, SortField.Type_e.LONG, true)); // descending
                    values.Reverse();
                }
            }
            sortedValues = values.ToArray();
            if (VERBOSE)
            {
                Console.WriteLine("sortedValues: " + sortedValues);
                Console.WriteLine("Sorter: " + sorter);
            }

            Directory target = NewDirectory();
            using (IndexWriter writer = new IndexWriter(target, NewIndexWriterConfig(TEST_VERSION_CURRENT, null)))
            {
                using (reader = SortingAtomicReader.Wrap(reader, sorter))
                {
                    writer.AddIndexes(reader);
                }
            }
            dir.Dispose();

            // CheckIndex the target directory
            dir = target;
            TestUtil.CheckIndex(dir);

            // set reader for tests
            reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
            assertFalse("index should not have deletions", reader.HasDeletions);
        }


        #region SorterTestBase
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestBinaryDocValuesField()
        {
            base.TestBinaryDocValuesField();
        }

        [Test]
        public override void TestDocsAndPositionsEnum()
        {
            base.TestDocsAndPositionsEnum();
        }

        [Test]
        public override void TestDocsEnum()
        {
            base.TestDocsEnum();
        }

        [Test]
        public override void TestNormValues()
        {
            base.TestNormValues();
        }

        [Test]
        public override void TestNumericDocValuesField()
        {
            base.TestNumericDocValuesField();
        }

        [Test]
        public override void TestSortedDocValuesField()
        {
            base.TestSortedDocValuesField();
        }

        [Test]
        public override void TestSortedSetDocValuesField()
        {
            base.TestSortedSetDocValuesField();
        }

        [Test]
        public override void TestTermVectors()
        {
            base.TestTermVectors();
        }

        #endregion
    }
}
