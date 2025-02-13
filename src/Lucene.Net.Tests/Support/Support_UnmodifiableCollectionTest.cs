// Adapted from Apache Harmony tests via J2N: https://github.com/NightOwl888/J2N/blob/main/tests/NUnit/J2N.Tests/Collections/Support_UnmodifiableCollectionTest.cs

using Lucene.Net.Util;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net
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

    public class Support_UnmodifiableCollectionTest : LuceneTestCase
    {
        readonly ICollection<int> col;

        // must be a collection containing the Integers 0 to 99 (which will iterate
        // in order)

        // LUCENENET: removed unused string argument and overload
        public Support_UnmodifiableCollectionTest(/*String p1,*/ ICollection<int> c)
        //: base(p1)
        {
            col = c;
        }

        public void RunTest()
        {

            // contains
            assertTrue("UnmodifiableCollectionTest - should contain 0", col
                    .Contains(0));
            assertTrue("UnmodifiableCollectionTest - should contain 50", col
                    .Contains(50));
            assertTrue("UnmodifiableCollectionTest - should not contain 100", !col
                    .Contains(100));

            // containsAll
            HashSet<int> hs = new HashSet<int>();
            hs.Add(0);
            hs.Add(25);
            hs.Add(99);
            assertTrue(
                    "UnmodifiableCollectionTest - should contain set of 0, 25, and 99",
                    col.Intersect(hs).Count() == hs.Count); // Contains all
            hs.Add(100);
            assertTrue(
                    "UnmodifiableCollectionTest - should not contain set of 0, 25, 99 and 100",
                    col.Intersect(hs).Count() != hs.Count); // Doesn't contain all

            // isEmpty
            assertTrue("UnmodifiableCollectionTest - should not be empty", col.Count > 0);

            // iterator
            IEnumerator<int> it = col.GetEnumerator();
            SortedSet<int> ss = new SortedSet<int>();
            while (it.MoveNext())
            {
                ss.Add(it.Current);
            }
            it = ss.GetEnumerator();
            for (int counter = 0; it.MoveNext(); counter++)
            {
                int nextValue = it.Current;
                assertTrue(
                        "UnmodifiableCollectionTest - Iterator returned wrong value.  Wanted: "
                                + counter + " got: " + nextValue,
                        nextValue == counter);
            }

            // size
            assertTrue(
                    "UnmodifiableCollectionTest - returned wrong size.  Wanted 100, got: "
                            + col.Count, col.Count == 100);

            // toArray
            object[] objArray;
            objArray = col.Cast<object>().ToArray();
            it = ss.GetEnumerator(); // J2N: Bug in Harmony, this needs to be reset to run
            for (int counter = 0; it.MoveNext(); counter++)
            {
                assertTrue(
                        "UnmodifiableCollectionTest - toArray returned incorrect array",
                        (int)objArray[counter] == it.Current);
            }

            // toArray (Object[])
            var intArray = new int[100];
            col.CopyTo(intArray, 0);
            it = ss.GetEnumerator(); // J2N: Bug in Harmony, this needs to be reset to run
            for (int counter = 0; it.MoveNext(); counter++)
            {
                assertTrue(
                        "UnmodifiableCollectionTest - CopyTo(object[], int) filled array incorrectly",
                        intArray[counter] == it.Current);
            }
        }
    }
}
