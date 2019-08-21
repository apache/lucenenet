using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Tests.Support
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

    public class TestListExtensions : LuceneTestCase
    {
        IList<int> ll;

        IList<IComparable<object>> myReversedLinkedList;

        static int[] objArray = LoadObjArray();
        static IComparable<object>[] myobjArray = LoadMyObjArray();

        private static int[] LoadObjArray()
        {
            var objArray = new int[1000];
            for (int i = 0; i < objArray.Length; i++)
            {
                objArray[i] = i;
            }
            return objArray;
        }

        private static IComparable<object>[] LoadMyObjArray()
        {
            var myobjArray = new IComparable<object>[1000];
            for (int i = 0; i < objArray.Length; i++)
            {
                myobjArray[i] = new MyInt(i);
            }
            return myobjArray;
        }

        public override void SetUp()
        {
            base.SetUp();
            ll = new List<int>();
            myReversedLinkedList = new List<IComparable<object>>(); // to be sorted in reverse

            for (int i = 0; i < objArray.Length; i++)
            {
                ll.Add(objArray[i]);
                //myll.add(myobjArray[i]);
                //s.add(objArray[i]);
                //mys.add(myobjArray[i]);
                //reversedLinkedList.add(objArray[objArray.length - i - 1]);
                myReversedLinkedList.Add(myobjArray[myobjArray.Length - i - 1]);
                //hm.put(objArray[i].toString(), objArray[i]);
            }
        }

        public class ReversedMyIntComparator : IComparer, IComparer<object>
        {

            public int Compare(Object o1, Object o2)
            {
                return -((MyInt)o1).CompareTo((MyInt)o2);
            }

            new public static int Equals(Object o1, Object o2)
            {
                return ((MyInt)o1).CompareTo((MyInt)o2);
            }
        }

        internal class MyInt : IComparable<object>
        {
            internal int data;

            public MyInt(int value)
            {
                data = value;
            }

            public int CompareTo(object obj)
            {
                return data > ((MyInt)obj).data ? 1 : (data < ((MyInt)obj).data ? -1 : 0);
            }
        }

        /**
         * @tests java.util.Collections#binarySearch(java.util.List,
         *        java.lang.Object)
         */
        [Test]
        public void Test_binarySearchLjava_util_ListLjava_lang_Object()
        {
            // Test for method int
            // java.util.Collections.binarySearch(java.util.List, java.lang.Object)
            // assumes ll is sorted and has no duplicate keys
            int llSize = ll.size();
            // Ensure a NPE is thrown if the list is NULL
            IList<IComparable<object>> list = null;
            try
            {
                list.BinarySearch(new MyInt(3));
                fail("Expected NullPointerException for null list parameter");
            }
            catch (ArgumentNullException e)
            {
                //Expected
            }
            for (int counter = 0; counter < llSize; counter++)
            {
                assertEquals("Returned incorrect binary search item position", ll[counter], ll[ll.BinarySearch(ll[counter])]);
            }
        }

        /**
         * @tests java.util.Collections#binarySearch(java.util.List,
         *        java.lang.Object, java.util.Comparator)
         */
        [Test]
        public void Test_binarySearchLSystem_Collections_Generic_IListLSystem_ObjectLSystem_Collections_Generic_IComparer()
        {
            // Test for method int
            // java.util.Collections.binarySearch(java.util.List, java.lang.Object,
            // java.util.Comparator)
            // assumes reversedLinkedList is sorted in reversed order and has no
            // duplicate keys
            int rSize = myReversedLinkedList.size();
            ReversedMyIntComparator comp = new ReversedMyIntComparator();
            // Ensure a NPE is thrown if the list is NULL
            IList<IComparable<object>> list = null;
            try
            {
                //Collections.binarySearch(null, new Object(), comp);
                list.BinarySearch(new MyInt(3), comp);
                fail("Expected NullPointerException for null list parameter");
            }
            catch (ArgumentNullException e)
            {
                //Expected
            }
            for (int counter = 0; counter < rSize; counter++)
            {
                assertEquals(
                        "Returned incorrect binary search item position using custom comparator",
                        myReversedLinkedList[counter], myReversedLinkedList[myReversedLinkedList.BinarySearch(myReversedLinkedList[counter], comp)]);
            }
        }
    }
}
