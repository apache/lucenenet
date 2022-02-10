using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;
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

    public class IndexSortingTest : SorterTestBase
    {
        private static readonly Sort[] SORT = new Sort[]
        {
            new Sort(new SortField(NUMERIC_DV_FIELD, SortFieldType.INT64)),
            new Sort(new SortField(null, SortFieldType.DOC, true))
        };

        [OneTimeSetUp]
        public override void BeforeClass() // LUCENENET specific - renamed from BeforeClassSorterUtilTest() to ensure calling order vs base class
        {
            base.BeforeClass();

            // only read the values of the undeleted documents, since after addIndexes,
            // the deleted ones will be dropped from the index.
            IBits liveDocs = reader.LiveDocs;
            JCG.List<int> values = new JCG.List<int>();
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                if (liveDocs is null || liveDocs.Get(i))
                {
                    values.Add(int.Parse(reader.Document(i).Get(ID_FIELD), CultureInfo.InvariantCulture));
                }
            }
            int idx = Random.nextInt(SORT.Length);
            Sort sorter = SORT[idx];
            if (idx == 1)
            { // reverse doc sort
                values.Reverse();
            }
            else
            {
                values.Sort();
                if (Random.nextBoolean())
                {
                    sorter = new Sort(new SortField(NUMERIC_DV_FIELD, SortFieldType.INT64, true)); // descending
                    values.Reverse();
                }
            }
            sortedValues = values.ToArray();
            if (Verbose)
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
    }
}
