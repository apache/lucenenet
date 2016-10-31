using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

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

    public class SortingAtomicReaderTest : SorterTestBase
    {
        [OneTimeSetUp]
        public static void BeforeClassSortingAtomicReaderTest()
        {

            // sort the index by id (as integer, in NUMERIC_DV_FIELD)
            Sort sort = new Sort(new SortField(NUMERIC_DV_FIELD, SortField.Type_e.INT));
            Sorter.DocMap docMap = new Sorter(sort).Sort(reader);

            // Sorter.compute also sorts the values
            NumericDocValues dv = reader.GetNumericDocValues(NUMERIC_DV_FIELD);
            sortedValues = new int[reader.MaxDoc];
            for (int i = 0; i < reader.MaxDoc; ++i)
            {
                sortedValues[docMap.OldToNew(i)] = (int)dv.Get(i);
            }
            if (VERBOSE)
            {
                Console.WriteLine("docMap: " + docMap);
                Console.WriteLine("sortedValues: " + Arrays.ToString(sortedValues));
            }

            // sort the index by id (as integer, in NUMERIC_DV_FIELD)
            reader = SortingAtomicReader.Wrap(reader, sort);

            if (VERBOSE)
            {
                Console.WriteLine("mapped-deleted-docs: ");
                Bits mappedLiveDocs = reader.LiveDocs;
                for (int i = 0; i < mappedLiveDocs.Length(); i++)
                {
                    if (!mappedLiveDocs.Get(i))
                    {
                        Console.WriteLine(i + " ");
                    }
                }
                Console.WriteLine();
            }

            TestUtil.CheckReader(reader);
        }

        [Test]
        public void TestBadSort()
        {
            try
            {
                SortingAtomicReader.Wrap(reader, Sort.RELEVANCE);
                fail("Didn't get expected exception");
            }
            catch (ArgumentException e)
            {
                assertEquals("Cannot sort an index with a Sort that refers to the relevance score", e.Message);
            }
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
