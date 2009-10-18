/**
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

using NUnit.Framework;

using BitArray = System.Collections.BitArray;

using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

namespace Lucene.Net.Util
{
    [TestFixture]
    public class TestSortedVIntList
    {

        void tstIterator(
                SortedVIntList vintList,
                int[] ints)
        {
            for (int i = 0; i < ints.Length; i++)
            {
                if ((i > 0) && (ints[i - 1] == ints[i]))
                {
                    return; // DocNrSkipper should not skip to same document.
                }
            }
            DocIdSetIterator m = vintList.Iterator();
            for (int i = 0; i < ints.Length; i++)
            {
                Assert.IsTrue(m.Next(), "No end of Matcher at: " + i);
                Assert.AreEqual(ints[i], m.Doc());
            }
            Assert.IsTrue((!m.Next()), "End of Matcher");
        }

        void tstVIntList(
                SortedVIntList vintList,
                int[] ints,
                int expectedByteSize)
        {
            Assert.AreEqual(ints.Length, vintList.Size(), "Size");
            Assert.AreEqual(expectedByteSize, vintList.GetByteSize(), "Byte size");
            tstIterator(vintList, ints);
        }

        public void tstViaBitSet(int[] ints, int expectedByteSize)
        {
            int MAX_INT_FOR_BITSET = 1024 * 1024;
            SupportClass.CollectionsSupport.BitSet bs = new SupportClass.CollectionsSupport.BitSet(ints.Length);
            //BitArray bs = new BitArray(ints.Length);
            for (int i = 0; i < ints.Length; i++)
            {
                if (ints[i] > MAX_INT_FOR_BITSET)
                {
                    return; // BitArray takes too much memory
                }
                if ((i > 0) && (ints[i - 1] == ints[i]))
                {
                    return; // BitArray cannot store duplicate.
                }
                bs.Set(ints[i]);
            }
            SortedVIntList svil = new SortedVIntList(bs);
            tstVIntList(svil, ints, expectedByteSize);
            tstVIntList(new SortedVIntList(svil.Iterator()), ints, expectedByteSize);
        }

        private static int VB1 = 0x7F;
        private static int BIT_SHIFT = 7;
        private static int VB2 = (VB1 << BIT_SHIFT) | VB1;
        private static int VB3 = (VB2 << BIT_SHIFT) | VB1;
        private static int VB4 = (VB3 << BIT_SHIFT) | VB1;

        private int vIntByteSize(int i)
        {
            System.Diagnostics.Debug.Assert(i >= 0);
            if (i <= VB1) return 1;
            if (i <= VB2) return 2;
            if (i <= VB3) return 3;
            if (i <= VB4) return 4;
            return 5;
        }

        private int vIntListByteSize(int[] ints)
        {
            int byteSize = 0;
            int last = 0;
            for (int i = 0; i < ints.Length; i++)
            {
                byteSize += vIntByteSize(ints[i] - last);
                last = ints[i];
            }
            return byteSize;
        }

        public void tstInts(int[] ints)
        {
            int expectedByteSize = vIntListByteSize(ints);
            try
            {
                tstVIntList(new SortedVIntList(ints), ints, expectedByteSize);
                tstViaBitSet(ints, expectedByteSize);
            }
            catch (System.IO.IOException ioe)
            {
                throw new System.Exception(null, ioe);
            }
        }

        public void tstIllegalArgExc(int[] ints)
        {
            try
            {
                new SortedVIntList(ints);
            }
            catch (System.ArgumentException)
            {
                return;
            }
            Assert.Fail("Expected ArgumentException");
        }

        private int[] fibArray(int a, int b, int size)
        {
            int[] fib = new int[size];
            fib[0] = a;
            fib[1] = b;
            for (int i = 2; i < size; i++)
            {
                fib[i] = fib[i - 1] + fib[i - 2];
            }
            return fib;
        }

        private int[] reverseDiffs(int[] ints)
        { // reverse the order of the successive differences
            int[] res = new int[ints.Length];
            for (int i = 0; i < ints.Length; i++)
            {
                res[i] = ints[ints.Length - 1] + (ints[0] - ints[ints.Length - 1 - i]);
            }
            return res;
        }

        [Test]
        public void Test01()
        {
            tstInts(new int[] { });
        }
        [Test]
        public void Test02()
        {
            tstInts(new int[] { 0 });
        }
        [Test]
        public void Test03()
        {
            tstInts(new int[] { 0, int.MaxValue });
        }
        [Test]
        public void Test04a()
        {
            tstInts(new int[] { 0, VB2 - 1 });
        }
        [Test]
        public void Test04b()
        {
            tstInts(new int[] { 0, VB2 });
        }
        [Test]
        public void Test04c()
        {
            tstInts(new int[] { 0, VB2 + 1 });
        }
        [Test]
        public void Test05()
        {
            tstInts(fibArray(0, 1, 7)); // includes duplicate value 1
        }
        [Test]
        public void Test05b()
        {
            tstInts(reverseDiffs(fibArray(0, 1, 7)));
        }
        [Test]
        public void Test06()
        {
            tstInts(fibArray(1, 2, 45)); // no duplicates, size 46 exceeds max int.
        }
        [Test]
        public void Test06b()
        {
            tstInts(reverseDiffs(fibArray(1, 2, 45)));
        }
        [Test]
        public void Test07a()
        {
            tstInts(new int[] { 0, VB3 });
        }
        [Test]
        public void Test07b()
        {
            tstInts(new int[] { 1, VB3 + 2 });
        }
        [Test]
        public void Test07c()
        {
            tstInts(new int[] { 2, VB3 + 4 });
        }
        [Test]
        public void Test08a()
        {
            tstInts(new int[] { 0, VB4 + 1 });
        }
        [Test]
        public void Test08b()
        {
            tstInts(new int[] { 1, VB4 + 1 });
        }
        [Test]
        public void Test08c()
        {
            tstInts(new int[] { 2, VB4 + 1 });
        }
        [Test]
        public void Test10()
        {
            tstIllegalArgExc(new int[] { -1 });
        }
        [Test]
        public void Test11()
        {
            tstIllegalArgExc(new int[] { 1, 0 });
        }
        [Test]
        public void Test12()
        {
            tstIllegalArgExc(new int[] { 0, 1, 1, 2, 3, 5, 8, 0 });
        }
    }
}
