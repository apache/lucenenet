using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Search.Suggest.Fst
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

    public class BytesRefSortersTest : LuceneTestCase
    {
        [Test]
        public void TestExternalRefSorter()
        {
            using ExternalRefSorter s = new ExternalRefSorter(new OfflineSorter());
            Check(s);
        }

        [Test]
        public void TestInMemorySorter()
        {
            Check(new InMemorySorter(BytesRef.UTF8SortedAsUnicodeComparer));
        }

        private void Check(IBytesRefSorter sorter)
        {
            for (int i = 0; i < 100; i++)
            {
                byte[] current = new byte[Random.Next(256)];
                Random.NextBytes(current);
                sorter.Add(new BytesRef(current));
            }

            // Create two iterators and check that they're aligned with each other.
            IBytesRefEnumerator i1 = sorter.GetEnumerator();
            IBytesRefEnumerator i2 = sorter.GetEnumerator();

            // Verify sorter contract.
            try
            {
                sorter.Add(new BytesRef(new byte[1]));
                fail("expected contract violation.");
            }
            catch (Exception e) when (e.IsIllegalStateException())
            {
                // Expected.
            }
            while (i1.MoveNext() && i2.MoveNext())
            {
                assertEquals(i1.Current, i2.Current);
            }
            assertFalse(i1.MoveNext());
            assertFalse(i2.MoveNext());
        }
    }
}
