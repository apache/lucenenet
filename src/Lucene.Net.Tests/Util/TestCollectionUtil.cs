using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
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

    [TestFixture]
    public class TestCollectionUtil : LuceneTestCase
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int[] CreateRandomList(int maxSize)
        {
            Random rnd = Random;
            int[] a = new int[rnd.Next(maxSize) + 1];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = rnd.Next(a.Length);
            }
            return a;
        }

        [Test]
        public virtual void TestIntroSort()
        {
            // LUCENENET: Use array for comparison rather than list for better performance
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                IList<int> list1 = CreateRandomList(2000);
                int[] list2 = new int[list1.Count];
                list1.CopyTo(list2, 0);
                CollectionUtil.IntroSort(list1);
                Array.Sort(list2);
                assertEquals(list2, list1, aggressive: false);

                list1 = CreateRandomList(2000);
                list2 = new int[list1.Count];
                list1.CopyTo(list2, 0);

                CollectionUtil.IntroSort(list1, Collections.ReverseOrder<int>());
                Array.Sort(list2, Collections.ReverseOrder<int>());
                assertEquals(list2, list1, aggressive: false);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                CollectionUtil.IntroSort(list1);
                Array.Sort(list2);
                assertEquals(list2, list1, aggressive: false);
            }
        }

        [Test]
        public virtual void TestTimSort()
        {
            // LUCENENET: Use array for comparison rather than list for better performance
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                IList<int> list1 = CreateRandomList(2000);
                int[] list2 = new int[list1.Count];
                list1.CopyTo(list2, 0);

                CollectionUtil.TimSort(list1);
                Array.Sort(list2);
                assertEquals(list2, list1, aggressive: false);

                list1 = CreateRandomList(2000);
                list2 = new int[list1.Count];
                list1.CopyTo(list2, 0);
                CollectionUtil.TimSort(list1, Collections.ReverseOrder<int>());
                Array.Sort(list2, Collections.ReverseOrder<int>());
                assertEquals(list2, list1, aggressive: false);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                CollectionUtil.TimSort(list1);
                Array.Sort(list2);
                assertEquals(list2, list1, aggressive: false);
            }
        }

        [Test]
        public virtual void TestEmptyListSort()
        {
            // should produce no exceptions
            IList<int> list = new int[0]; // LUCENE-2989
            CollectionUtil.IntroSort(list);
            CollectionUtil.TimSort(list);
            CollectionUtil.IntroSort(list, Collections.ReverseOrder<int>());
            CollectionUtil.TimSort(list, Collections.ReverseOrder<int>());

            // check that empty non-random access lists pass sorting without ex (as sorting is not needed)
            list = new JCG.List<int>();
            CollectionUtil.IntroSort(list);
            CollectionUtil.TimSort(list);
            CollectionUtil.IntroSort(list, Collections.ReverseOrder<int>());
            CollectionUtil.TimSort(list, Collections.ReverseOrder<int>());
        }

        [Test]
        public virtual void TestOneElementListSort()
        {
            // check that one-element non-random access lists pass sorting without ex (as sorting is not needed)
            IList<int> list = new JCG.List<int>();
            list.Add(1);
            CollectionUtil.IntroSort(list);
            CollectionUtil.TimSort(list);
            CollectionUtil.IntroSort(list, Collections.ReverseOrder<int>());
            CollectionUtil.TimSort(list, Collections.ReverseOrder<int>());
        }
    }
}