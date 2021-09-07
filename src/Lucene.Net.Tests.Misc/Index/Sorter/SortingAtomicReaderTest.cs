using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Console = Lucene.Net.Util.SystemConsole;

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
        public override void BeforeClass() // LUCENENET specific - renamed from BeforeClassSortingAtomicReaderTest() to ensure calling order vs base class
        {
            base.BeforeClass();

            // sort the index by id (as integer, in NUMERIC_DV_FIELD)
            Sort sort = new Sort(new SortField(NUMERIC_DV_FIELD, SortFieldType.INT32));
            Sorter.DocMap docMap = new Sorter(sort).Sort(reader);

            // Sorter.compute also sorts the values
            NumericDocValues dv = reader.GetNumericDocValues(NUMERIC_DV_FIELD);
            sortedValues = new int[reader.MaxDoc];
            for (int i = 0; i < reader.MaxDoc; ++i)
            {
                sortedValues[docMap.OldToNew(i)] = (int)dv.Get(i);
            }
            if (Verbose)
            {
                Console.WriteLine("docMap: " + docMap);
                Console.WriteLine("sortedValues: " + Arrays.ToString(sortedValues));
            }

            // sort the index by id (as integer, in NUMERIC_DV_FIELD)
            reader = SortingAtomicReader.Wrap(reader, sort);

            if (Verbose)
            {
                Console.WriteLine("mapped-deleted-docs: ");
                IBits mappedLiveDocs = reader.LiveDocs;
                for (int i = 0; i < mappedLiveDocs.Length; i++)
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
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                assertEquals("Cannot sort an index with a Sort that refers to the relevance score", e.Message);
            }
        }
    }
}
