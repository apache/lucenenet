// Adapted from Apache Harmony tests via J2N: https://github.com/NightOwl888/J2N/blob/main/tests/NUnit/J2N.Tests/Collections/Support_CollectionTest.cs
using Lucene.Net.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

    public class Support_CollectionTest : LuceneTestCase
    {
        readonly ICollection<int> col; // must contain the Integers 0 to 99

        // LUCENENET: removed unused string argument and overload
        public Support_CollectionTest(/*String p1,*/ ICollection<int> c)
        //: base(p1)
        {
            col = c;
        }

        public void RunTest()
        {
            new Support_UnmodifiableCollectionTest(col).RunTest();

            // setup
            ICollection<int> myCollection = new JCG.SortedSet<int>();
            myCollection.Add(101);
            myCollection.Add(102);
            myCollection.Add(103);

            // add
            //assertTrue("CollectionTest - a) add did not work", col.Add(new Integer(
            //        101)));
            col.Add(101); // Does not return in .NET
            assertTrue("CollectionTest - b) add did not work", col
                    .Contains(101));

            // remove
            assertTrue("CollectionTest - a) remove did not work", col
                    .Remove(101));
            assertTrue("CollectionTest - b) remove did not work", !col
                    .Contains(101));

            if (col is ISet<int> set)
            {
                // addAll
                //assertTrue("CollectionTest - a) addAll failed", set
                //        .UnionWith(myCollection));
                set.UnionWith(myCollection); // Does not return in .NET
                assertTrue("CollectionTest - b) addAll failed", set
                        .IsSupersetOf(myCollection));

                // containsAll
                assertTrue("CollectionTest - a) containsAll failed", set
                        .IsSupersetOf(myCollection));
                col.Remove(101);
                assertTrue("CollectionTest - b) containsAll failed", !set
                        .IsSupersetOf(myCollection));

                // removeAll
                //assertTrue("CollectionTest - a) removeAll failed", set
                //        .ExceptWith(myCollection));
                //assertTrue("CollectionTest - b) removeAll failed", !set
                //        .ExceptWith(myCollection)); // should not change the colletion
                //                                   // the 2nd time around

                set.ExceptWith(myCollection); // Does not return in .NET
                assertTrue("CollectionTest - c) removeAll failed", !set
                        .Contains(102));
                assertTrue("CollectionTest - d) removeAll failed", !set
                        .Contains(103));

                // retianAll
                set.UnionWith(myCollection);
                //assertTrue("CollectionTest - a) retainAll failed", set
                //        .IntersectWith(myCollection));
                //assertTrue("CollectionTest - b) retainAll failed", !set
                //        .IntersectWith(myCollection)); // should not change the colletion
                //                                   // the 2nd time around

                set.IntersectWith(myCollection); // Does not return in .NET
                assertTrue("CollectionTest - c) retainAll failed", set
                        .IsSupersetOf(myCollection));
                assertTrue("CollectionTest - d) retainAll failed", !set
                        .Contains(0));
                assertTrue("CollectionTest - e) retainAll failed", !set
                        .Contains(50));

            }

            // clear
            col.Clear();
            assertTrue("CollectionTest - a) clear failed", col.Count == 0);
            assertTrue("CollectionTest - b) clear failed", !col
                    .Contains(101));

        }

    }
}
